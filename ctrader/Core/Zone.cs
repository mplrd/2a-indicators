using System;
using System.Collections.Generic;

namespace _2Ai.Indicators.Core
{
    /// <summary>Orientation logique d'une zone (équiv Pine <c>ZoneSide</c>).</summary>
    public enum ZoneSide { Bull, Bear }

    /// <summary>
    /// État du cycle de vie d'une zone (équiv Pine <c>ZoneState</c>).
    /// ACTIVE : affichable + impactée par les events. PENDING : validée mais en attente
    /// (chevauchement…). EXPIRED : à supprimer au prochain nettoyage.
    /// </summary>
    public enum ZoneState { Active, Pending, Expired }

    /// <summary>
    /// Équivalent du UDT Pine <c>Zone</c> (de <c>lib_zone</c>) + ses helpers. Zone de prix
    /// horizontale avec horizon temporel et cycle de vie. Couche 0 — ne dessine rien. Consommée
    /// par lib_signal/lib_sd/lib_fvg et le rendu.
    /// </summary>
    public class Zone
    {
        public double Top;
        public double Bottom;
        public DateTime LeftTime;   // début temporel de la zone (Pine : ms epoch ; ici DateTime)
        public ZoneSide Side;
        public ZoneState State;
        public int TfMinutes;       // timeframe d'origine en minutes (filtrage MTF)

        /// <summary>Crée une zone ACTIVE (équiv <c>newZone</c>).</summary>
        public Zone(double top, double bottom, DateTime leftTime, ZoneSide side, int tfMinutes)
        {
            Top = top; Bottom = bottom; LeftTime = leftTime; Side = side;
            State = ZoneState.Active; TfMinutes = tfMinutes;
        }

        /// <summary>Chevauchement vertical (prix) avec une autre zone. Ignore leftTime et l'état.</summary>
        public bool Overlaps(Zone b) => !(Top < b.Bottom || Bottom > b.Top);

        /// <summary>Vrai si le prix est contenu dans la zone (inclusif).</summary>
        public bool Contains(double price) => price >= Bottom && price <= Top;

        /// <summary>Hauteur (top − bottom).</summary>
        public double Height => Top - Bottom;

        public void Expire() => State = ZoneState.Expired;
        public void SetPending() => State = ZoneState.Pending;
        public void Activate() => State = ZoneState.Active;

        public bool IsActive => State == ZoneState.Active;
        public bool IsPending => State == ZoneState.Pending;
        public bool IsExpired => State == ZoneState.Expired;
    }

    /// <summary>Helpers sur collections de <see cref="Zone"/> (équiv des exports array de lib_zone).</summary>
    public static class ZoneOps
    {
        /// <summary>Supprime les zones expirées (parcours arrière, mutation in-place).</summary>
        public static void RemoveExpired(List<Zone> zones)
        {
            for (int i = zones.Count - 1; i >= 0; i--)
                if (zones[i].State == ZoneState.Expired)
                    zones.RemoveAt(i);
        }

        /// <summary>Garde au plus <paramref name="maxCount"/> zones, supprime les plus anciennes (FIFO).</summary>
        public static void TrimOldest(List<Zone> zones, int maxCount)
        {
            while (zones.Count > maxCount) zones.RemoveAt(0);
        }

        /// <summary>Première zone ACTIVE qui chevauche <paramref name="z"/>, ou <c>null</c>.</summary>
        public static Zone FindOverlapping(IReadOnlyList<Zone> zones, Zone z)
        {
            foreach (var c in zones)
                if (c.State == ZoneState.Active && c.Overlaps(z)) return c;
            return null;
        }

        /// <summary>
        /// Vrai s'il existe une zone ACTIVE de MÊME side que <paramref name="z"/> qui la chevauche.
        /// Sert au filtre "5 étoiles" (S/D et FVG du même côté superposés).
        /// </summary>
        public static bool FindOverlappingActiveSameSide(IReadOnlyList<Zone> zones, Zone z)
        {
            foreach (var other in zones)
                if (other.State == ZoneState.Active && other.Side == z.Side && other.Overlaps(z))
                    return true;
            return false;
        }
    }
}
