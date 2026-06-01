using System;
using System.Collections.Generic;
using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>Sens de la CMI à l'origine de la zone (équiv Pine <c>CmiSide</c>).</summary>
    public enum CmiSide { Bull, Bear }

    /// <summary>État de cycle de vie d'une zone CMI (équiv Pine <c>CmiState</c>).</summary>
    public enum CmiState { Active, Pending, Expired }

    /// <summary>
    /// Zone de prix construite à partir d'une CMI validée (équiv UDT Pine <c>CmiZone</c>,
    /// self-contained). <c>CmiOpen</c> sert au déclencheur Pending (open d'une nouvelle CMI dans
    /// la zone).
    /// </summary>
    public class CmiZone
    {
        public double Top, Bottom;
        public DateTime LeftTime;
        public CmiSide Side;
        public CmiState State;
        public int TfMinutes;
        public double CmiOpen;

        public CmiZone(double top, double bottom, DateTime leftTime, CmiSide side, CmiState state, int tfMinutes, double cmiOpen)
        {
            Top = top; Bottom = bottom; LeftTime = leftTime; Side = side;
            State = state; TfMinutes = tfMinutes; CmiOpen = cmiOpen;
        }

        /// <summary>Chevauchement géométrique en prix.</summary>
        public bool Overlaps(CmiZone b) => !(Top < b.Bottom || Bottom > b.Top);
    }

    // CMI détectée en cours de validation 3 bougies (plusieurs en parallèle).
    internal class PendingCmi
    {
        public int BarIndex, BarsLeft;
        public CmiSide Side;
        public double Open, Low, High;
        public PendingCmi(int barIndex, int barsLeft, CmiSide side, double open, double low, double high)
        { BarIndex = barIndex; BarsLeft = barsLeft; Side = side; Open = open; Low = low; High = high; }
    }

    /// <summary>
    /// Équivalent de Pine <c>lib_cmi_zone</c> (state machine per-TF, <c>updateOne</c>). Couche 1,
    /// ne dessine pas. Stateful : détection CMI (déléguée), validation 3 bougies PARALLÈLES (une CMI
    /// opposée n'invalide pas la précédente ; invalidation = clôture sous le low (bull) / au-dessus
    /// du high (bear) de la CMI), construction par lookback sur le CORPS, expiration par période
    /// d'intérêt, FIFO. PAS d'anti-chevauchement (passes séparées). Opère sur des <c>Bars</c>
    /// quelconques (chart courant ou HTF via <c>MarketData.GetBars</c>).
    ///
    /// <para>Garde <c>_lastIndex</c> : <c>Update</c> ne traite chaque barre qu'une fois (la validation
    /// décrémente <c>BarsLeft</c> → non idempotent sur les reticks). Exact en historique ; le live
    /// forming bar est traité au 1er tick (réserve mineure, cf. autres trackers).</para>
    /// </summary>
    public class CmiZones
    {
        private readonly List<CmiZone> _zones = new List<CmiZone>();
        private readonly List<PendingCmi> _pending = new List<PendingCmi>();
        private int _lastIndex = -1;

        public IReadOnlyList<CmiZone> Zones => _zones;

        /// <summary>
        /// Tick la state machine pour la barre <paramref name="index"/> de <paramref name="bars"/>.
        /// </summary>
        /// <param name="cmi">Sortie de <see cref="Signal.DetectCmi"/> sur cette barre.</param>
        /// <param name="interestStart">Zones dont le leftTime est antérieur expirent (MinValue = illimité).</param>
        public void Update(Bars bars, int index, SignalKind cmi, int tfMinutes,
            int lookbackBars, DateTime interestStart, int maxZones)
        {
            if (index <= _lastIndex) return;  // une seule passe par barre
            _lastIndex = index;

            double close = bars.ClosePrices[index];

            // 1. Expiration par période d'intérêt.
            foreach (var cz in _zones)
                if (cz.State != CmiState.Expired && cz.LeftTime < interestStart)
                    cz.State = CmiState.Expired;

            // 2+3. Tick validation sur chaque pending (descendant). Invalidation = cassure d'extrême
            //      → retrait ; sinon décrément, et à 0 → construction + push + retrait.
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var p = _pending[i];
                bool invalidated = (p.Side == CmiSide.Bull && close < p.Low)
                                || (p.Side == CmiSide.Bear && close > p.High);
                if (invalidated) { _pending.RemoveAt(i); continue; }

                p.BarsLeft--;
                if (p.BarsLeft == 0)
                {
                    var z = Construct(bars, p, lookbackBars, tfMinutes);
                    if (z != null) _zones.Add(z);
                    _pending.RemoveAt(i);
                }
            }

            // 4. Nouvelle CMI → nouvelle validation (aucune annulation des pendings : parallèles).
            if (cmi == SignalKind.CmiBull || cmi == SignalKind.CmiBear)
            {
                var side = cmi == SignalKind.CmiBull ? CmiSide.Bull : CmiSide.Bear;
                _pending.Add(new PendingCmi(index, 3, side, bars.OpenPrices[index], bars.LowPrices[index], bars.HighPrices[index]));
            }

            // 5. Cleanup.
            for (int i = _zones.Count - 1; i >= 0; i--)
                if (_zones[i].State == CmiState.Expired) _zones.RemoveAt(i);
            while (_zones.Count > maxZones) _zones.RemoveAt(0);
        }

        // Construit la zone d'une pending validée : fenêtre lookback = barre CMI + (lookback-1) avant.
        // Bull : Top = plus bas min(open,close), Bottom = plus bas low. Bear : Top = plus haut high,
        // Bottom = plus haut max(open,close). leftTime = min des temps des 2 extrêmes.
        private static CmiZone Construct(Bars bars, PendingCmi p, int lookbackBars, int tfMinutes)
        {
            if (lookbackBars <= 0) return null;
            double top = double.NaN, bottom = double.NaN;
            DateTime topTime = default, bottomTime = default;
            for (int k = 0; k < lookbackBars; k++)
            {
                int b = p.BarIndex - k;
                if (b < 0) break;
                double o = bars.OpenPrices[b], c = bars.ClosePrices[b], h = bars.HighPrices[b], l = bars.LowPrices[b];
                DateTime t = bars.OpenTimes[b];
                if (p.Side == CmiSide.Bull)
                {
                    double bodyLow = Math.Min(o, c);
                    if (double.IsNaN(top) || bodyLow < top) { top = bodyLow; topTime = t; }
                    if (double.IsNaN(bottom) || l < bottom) { bottom = l; bottomTime = t; }
                }
                else
                {
                    if (double.IsNaN(top) || h > top) { top = h; topTime = t; }
                    double bodyHigh = Math.Max(o, c);
                    if (double.IsNaN(bottom) || bodyHigh > bottom) { bottom = bodyHigh; bottomTime = t; }
                }
            }
            if (double.IsNaN(top) || double.IsNaN(bottom)) return null;
            DateTime leftT = topTime < bottomTime ? topTime : bottomTime;
            return new CmiZone(top, bottom, leftT, p.Side, CmiState.Active, tfMinutes, p.Open);
        }
    }
}
