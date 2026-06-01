using System.Collections.Generic;
using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>
    /// Équivalent de Pine <c>lib_sd</c>. Cycle de vie d'une collection de zones Supply/Demand sur
    /// l'UT courante : détection via CMI, anti-chaînage same-bar, anti-chevauchement par
    /// remplacement, expiration au comblement total, FIFO. Couche 1, ne dessine pas. Stateful.
    ///
    /// <para>Correct en historique (une passe par barre). En live, comme les trackers Levels, les
    /// reticks de la dernière barre peuvent expirer une zone via une mèche transitoire — exact dès
    /// la clôture de la barre.</para>
    /// </summary>
    public class SupplyDemand
    {
        private readonly List<Zone> _zones = new List<Zone>();
        private readonly int _maxZones;
        private bool _hadCmiPrev;

        public IReadOnlyList<Zone> Zones => _zones;

        public SupplyDemand(int maxZones) { _maxZones = maxZones; }

        /// <summary>MAJ pour la barre courante. <paramref name="cmi"/> = sortie de <see cref="Signal.DetectCmi"/>.</summary>
        public void Update(DataSeries high, DataSeries low, TimeSeries openTimes, int index, SignalKind cmi, int tfMinutes)
        {
            double hi = high[index], lo = low[index];

            // 1. Lifecycle AVANT création (une zone créée ce bar ne doit pas être tuée par sa propre CMI).
            foreach (var z in _zones)
            {
                if (!z.IsActive) continue;
                if (z.Side == ZoneSide.Bull && lo <= z.Bottom) z.Expire();
                else if (z.Side == ZoneSide.Bear && hi >= z.Top) z.Expire();
            }

            // 2. Création : CMI ce bar ET pas de CMI au bar précédent (anti-chaînage). Zone = range
            //    complète de n−1. Si chevauchement avec une zone active même side → l'ancienne expire.
            bool cmiNow = cmi == SignalKind.CmiBull || cmi == SignalKind.CmiBear;
            if (cmiNow && !_hadCmiPrev && index >= 1)
            {
                var side = cmi == SignalKind.CmiBull ? ZoneSide.Bull : ZoneSide.Bear;
                var newZ = new Zone(high[index - 1], low[index - 1], openTimes[index - 1], side, tfMinutes);
                foreach (var ex in _zones)
                    if (ex.IsActive && ex.Side == side && ex.Overlaps(newZ)) ex.Expire();
                _zones.Add(newZ);
            }
            _hadCmiPrev = cmiNow;

            // 3. Cleanup.
            ZoneOps.RemoveExpired(_zones);
            ZoneOps.TrimOldest(_zones, _maxZones);
        }
    }
}
