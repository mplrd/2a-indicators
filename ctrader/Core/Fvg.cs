using System.Collections.Generic;
using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>
    /// Équivalent de Pine <c>lib_fvg</c>. Détection de Fair Value Gaps (3-bar pattern) + cycle de
    /// vie (comblement partiel/total). Couche 1, ne dessine pas. Stateful (même réserve live que
    /// <see cref="SupplyDemand"/>).
    /// </summary>
    public class FvgTracker
    {
        private readonly List<Zone> _zones = new List<Zone>();
        private readonly int _maxZones;
        private int _lastDetectIndex = -1;  // détection une seule fois par barre (anti-doublon reticks)

        public IReadOnlyList<Zone> Zones => _zones;

        public FvgTracker(int maxZones) { _maxZones = maxZones; }

        /// <summary>
        /// MAJ pour la barre courante. <paramref name="barSec"/> = durée nominale d'une barre (s),
        /// pour le filtre anti-faux-gap (rejet si un saut de temps &gt; 1.5× sur l'une des 2 transitions).
        /// </summary>
        public void Update(DataSeries high, DataSeries low, TimeSeries openTimes, int index, int tfMinutes, double barSec)
        {
            double hi = high[index], lo = low[index];

            // 1. Lifecycle des FVG existants.
            foreach (var z in _zones) UpdateOne(z, hi, lo);

            // 2. Détection 3-bar (avec filtre temporel anti-faux-gap). Une seule fois par barre :
            //    sinon, sur les reticks de la barre live, le pattern re-détecté empile des FVG
            //    dupliqués (même géométrie) → l'alpha cumulé donne l'impression que la transparence
            //    baisse à chaque tick. Le cycle de vie (étape 1) reste, lui, à chaque tick.
            if (index >= 2 && index > _lastDetectIndex)
            {
                _lastDetectIndex = index;
                double expMs = barSec * 1000.0;
                double d10 = (openTimes[index] - openTimes[index - 1]).TotalMilliseconds;
                double d21 = (openTimes[index - 1] - openTimes[index - 2]).TotalMilliseconds;
                bool timeGap = d10 > expMs * 1.5 || d21 > expMs * 1.5;

                if (!timeGap)
                {
                    if (lo > high[index - 2])
                        _zones.Add(new Zone(lo, high[index - 2], openTimes[index - 2], ZoneSide.Bull, tfMinutes));
                    else if (hi < low[index - 2])
                        _zones.Add(new Zone(low[index - 2], hi, openTimes[index - 2], ZoneSide.Bear, tfMinutes));
                }
            }

            // 3. Cleanup.
            ZoneOps.RemoveExpired(_zones);
            ZoneOps.TrimOldest(_zones, _maxZones);
        }

        // Cycle de vie d'un FVG avec la barre courante : partial fill resserre top/bottom, total fill expire.
        private static void UpdateOne(Zone z, double hi, double lo)
        {
            if (!z.IsActive) return;
            bool bull = z.Side == ZoneSide.Bull;
            bool bullFilled = bull && lo <= z.Bottom;
            bool bearFilled = !bull && hi >= z.Top;
            bool bullPartial = bull && !bullFilled && lo < z.Top;
            bool bearPartial = !bull && !bearFilled && hi > z.Bottom;

            if (bullFilled || bearFilled) z.Expire();
            if (bullPartial) z.Top = lo;
            if (bearPartial) z.Bottom = hi;
        }
    }
}
