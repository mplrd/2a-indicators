using System.Collections.Generic;
using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>
    /// Gap journalier avec orientation et ancrage. Équivalent du UDT Pine <c>Gap</c>.
    /// </summary>
    public class Gap
    {
        public double Top;        // borne haute (mutée si comblement partiel haussier)
        public double Bottom;     // borne basse (mutée si comblement partiel baissier)
        public int Dir;           // 1 = haussier (gap up), -1 = baissier
        public int LeftBarIndex;  // bar_index chart de la barre AVANT le gap (ancre)

        public Gap(double top, double bottom, int dir, int leftBarIndex)
        {
            Top = top; Bottom = bottom; Dir = dir; LeftBarIndex = leftBarIndex;
        }
    }

    /// <summary>
    /// Équivalent de Pine <c>lib_gap</c> v7. Détection des gaps daily + cycle de vie complet
    /// (création au changement de jour, comblement partiel/total par barre, FIFO). Couche 1,
    /// ne dessine rien. Stateful (la <c>var array</c> interne Pine devient une liste d'instance).
    ///
    /// <para>Anti-repaint : en cAlgo, les barres daily sont déjà complètes pendant l'itération du
    /// chart, donc <c>daily[dIdx]</c> donne les valeurs full-day — équivalent du
    /// <c>request.security("D", detectDaily(), lookahead_on)</c> Pine.</para>
    /// </summary>
    public class GapTracker
    {
        private readonly int _maxLookback;
        private readonly List<Gap> _gaps = new List<Gap>();
        private int _lastDailyIdx = -1;

        public IReadOnlyList<Gap> Gaps => _gaps;

        public GapTracker(int maxLookback) { _maxLookback = maxLookback; }

        /// <summary>À appeler chaque barre du chart. <paramref name="daily"/> = barres daily du symbole.</summary>
        public void Update(Bars chart, Bars daily, int index)
        {
            int dIdx = daily.OpenTimes.GetIndexByTime(chart.OpenTimes[index]);

            // Changement de jour : détection du gap sur la nouvelle barre daily vs la précédente.
            if (dIdx >= 1 && dIdx > _lastDailyIdx && _lastDailyIdx >= 0)
            {
                double prevClose = daily.ClosePrices[dIdx - 1];
                double dLow = daily.LowPrices[dIdx];
                double dHigh = daily.HighPrices[dIdx];
                if (dLow > prevClose)               // gap haussier : zone [prevClose, low]
                    _gaps.Add(new Gap(dLow, prevClose, 1, index - 1));
                else if (dHigh < prevClose)         // gap baissier : zone [high, prevClose]
                    _gaps.Add(new Gap(prevClose, dHigh, -1, index - 1));

                while (_gaps.Count > _maxLookback) _gaps.RemoveAt(0);
            }
            if (dIdx >= 0) _lastDailyIdx = dIdx;

            // Cycle de vie : comblement par la barre courante du chart (iter descendant pour retrait).
            double hi = chart.HighPrices[index], lo = chart.LowPrices[index];
            for (int i = _gaps.Count - 1; i >= 0; i--)
            {
                var g = _gaps[i];
                bool filled = false;
                if (g.Dir == 1)
                {
                    if (lo <= g.Top && lo > g.Bottom) g.Top = lo;  // partiel : la borne haute descend
                    if (lo <= g.Bottom) filled = true;             // total
                }
                else if (g.Dir == -1)
                {
                    if (hi >= g.Bottom && hi < g.Top) g.Bottom = hi;
                    if (hi >= g.Top) filled = true;
                }
                if (filled) _gaps.RemoveAt(i);
            }
        }
    }
}
