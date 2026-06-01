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

    /// <summary>
    /// Passes d'anti-chevauchement des zones CMI (équiv Pine <c>intraOverlapPass</c> /
    /// <c>crossTfOverlapPass</c>). Couche 1, ne dessine pas. Idempotentes : ré-évaluent l'état de
    /// chaque zone depuis zéro → sûres à appeler chaque barre (et sur les reticks). Mutent
    /// uniquement <see cref="CmiZone.State"/> (jamais la structure de la liste).
    /// </summary>
    public static class CmiZoneOps
    {
        /// <summary>
        /// Résout les chevauchements INTRA-TF. Forward iteration : chaque zone non-expirée est
        /// évaluée contre les précédentes encore non-expirées. Bloquée si son <c>CmiOpen</c> tombe
        /// dans <c>[bottom, top]</c> d'une autre OU si les zones se chevauchent géométriquement.
        /// <para><paramref name="pendingMode"/> = true → la nouvelle passe en PENDING (réactivée
        /// quand le bloqueur expire). false (Replacement) → les bloqueurs deviennent EXPIRED et la
        /// nouvelle devient ACTIVE.</para>
        /// </summary>
        public static void IntraOverlapPass(IReadOnlyList<CmiZone> zones, bool pendingMode)
        {
            int n = zones.Count;
            for (int i = 0; i < n; i++)
            {
                var cz = zones[i];
                if (cz.State == CmiState.Expired) continue;
                bool hasBlocker = false;
                for (int j = 0; j < i; j++)
                {
                    var other = zones[j];
                    if (other.State == CmiState.Expired) continue;
                    bool openIn = cz.CmiOpen >= other.Bottom && cz.CmiOpen <= other.Top;
                    if (openIn || other.Overlaps(cz))
                    {
                        if (pendingMode) { hasBlocker = true; break; }
                        other.State = CmiState.Expired;
                    }
                }
                cz.State = hasBlocker ? CmiState.Pending : CmiState.Active;
            }
        }

        /// <summary>
        /// Downgrade en PENDING toute zone ACTIVE de <paramref name="lower"/> (TF inférieur) qui
        /// chevauche géométriquement une zone ACTIVE de <paramref name="higher"/> (TF supérieur).
        /// Une zone haute PENDING ne bloque pas. Pas de critère open-in-zone (contextes de TF
        /// différents). Idempotent (la réactivation se fait au tour suivant d'<see cref="IntraOverlapPass"/>).
        /// </summary>
        public static void CrossTfOverlapPass(IReadOnlyList<CmiZone> lower, IReadOnlyList<CmiZone> higher)
        {
            if (lower.Count == 0 || higher.Count == 0) return;
            for (int i = 0; i < lower.Count; i++)
            {
                var cz = lower[i];
                if (cz.State != CmiState.Active) continue;
                for (int j = 0; j < higher.Count; j++)
                {
                    var other = higher[j];
                    if (other.State == CmiState.Active && other.Overlaps(cz))
                    {
                        cz.State = CmiState.Pending;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Calcule la borne d'obsolescence (période d'intérêt) d'un TF, dans la TZ du chart (équiv Pine
    /// <c>computeInterestStarts</c> / <c>interestStartForTf</c>). Stateful pour la borne &lt; M15 :
    /// suit la 1re barre de J-1 par détection de changement de jour. Appeler <see cref="OnBar"/>
    /// une fois par barre (chronologique) avant <see cref="StartForTf"/>.
    /// <para><c>DateTime.MinValue</c> = illimité (jamais d'expiration, cas Monthly / J-1 pas encore
    /// vu) — cohérent avec <see cref="CmiZones.Update"/> qui teste <c>leftTime &lt; interestStart</c>.</para>
    /// </summary>
    public class InterestPeriods
    {
        private int _lastDom = -1;
        private DateTime _todayFirstBar = DateTime.MinValue;
        private DateTime _yesterdayFirstBar = DateTime.MinValue;

        /// <summary>Suit le changement de jour (TZ chart) pour la borne &lt; M15. Idempotent sur les reticks.</summary>
        public void OnBar(DateTime barUtc, string chartTz)
        {
            int dom = Time.InTz(barUtc, chartTz).Day;
            if (dom != _lastDom)
            {
                if (_lastDom != -1)  // pas la 1re barre du dataset (ta.change est na sur la 1re)
                {
                    _yesterdayFirstBar = _todayFirstBar;
                    _todayFirstBar = barUtc;
                }
                else
                {
                    _todayFirstBar = barUtc;
                }
                _lastDom = dom;
            }
        }

        /// <summary>Borne d'intérêt (UTC) pour <paramref name="tfMinutes"/>, vue depuis <paramref name="nowUtc"/>.</summary>
        public DateTime StartForTf(DateTime nowUtc, int tfMinutes, string chartTz)
        {
            var c = Time.InTz(nowUtc, chartTz);
            int yC = c.Year, moC = c.Month, domC = c.Day;

            // Monthly : illimité.
            if (tfMinutes >= 43200) return DateTime.MinValue;

            // Weekly : 1er janvier de (année - 3).
            if (tfMinutes >= 10080) return Time.TimestampInTz(chartTz, yC - 3, 1, 1, 0, 0);

            // Daily : 1er du mois (mois - 6, rollover).
            if (tfMinutes >= 1440)
            {
                int yOff = moC - 7 < 0 ? -1 : 0;
                int mo = moC - 6 <= 0 ? moC - 6 + 12 : moC - 6;
                return Time.TimestampInTz(chartTz, yC + yOff, mo, 1, 0, 0);
            }

            // H4 : 1er du mois (mois - 1, rollover).
            if (tfMinutes >= 240)
            {
                int yOff = moC - 2 < 0 ? -1 : 0;
                int mo = moC - 1 <= 0 ? moC - 1 + 12 : moC - 1;
                return Time.TimestampInTz(chartTz, yC + yOff, mo, 1, 0, 0);
            }

            // H1 / M15 : lundi de cette semaine ± semaines (minuit chart TZ).
            var todayMidnight = Time.TimestampInTz(chartTz, yC, moC, domC, 0, 0);
            int daysFromMonday = c.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)c.DayOfWeek - 1;
            var mondayThisWeek = todayMidnight.AddDays(-daysFromMonday);
            if (tfMinutes >= 60)  return mondayThisWeek.AddDays(-14);  // lundi d'il y a 2 semaines
            if (tfMinutes >= 15)  return mondayThisWeek.AddDays(-7);   // lundi de la semaine dernière

            // < M15 : 1re barre de J-1 (MinValue tant que pas encore vue).
            return _yesterdayFirstBar;
        }
    }
}
