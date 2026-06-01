using System;
using System.Collections.Generic;

namespace _2Ai.Indicators.Core
{
    /// <summary>
    /// Équivalent de Pine <c>lib_time</c>. Primitives temporelles avec timezone IANA explicite
    /// (ex : "Europe/Paris", "America/New_York", "Asia/Tokyo"). Pas d'offsets bruts ("GMT+1"),
    /// cassés en DST. Couche 0 — aucune dépendance, ne dessine rien.
    ///
    /// <para>Convention cAlgo : les temps de barre (<c>Bars.OpenTimes[index]</c>) et
    /// <c>Server.Time</c> sont en <b>UTC</b> ; on convertit vers la TZ IANA demandée via
    /// <see cref="TimeZoneInfo"/>. .NET 6 résout les IDs IANA sur Windows via ICU.</para>
    ///
    /// <para>Pas de support cross-midnight pour les sessions (start &lt; end requis) — cohérent
    /// avec Pine <c>lib_levels</c> (Asian/EU/US/Future ne traversent pas minuit).</para>
    /// </summary>
    public static class Time
    {
        // FindSystemTimeZoneById est coûteux : on cache les TimeZoneInfo par ID IANA.
        private static readonly Dictionary<string, TimeZoneInfo> _tzCache = new Dictionary<string, TimeZoneInfo>();

        private static TimeZoneInfo Tz(string ianaId)
        {
            if (!_tzCache.TryGetValue(ianaId, out var tz))
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(ianaId);
                _tzCache[ianaId] = tz;
            }
            return tz;
        }

        /// <summary>
        /// Convertit un instant UTC vers la TZ IANA donnée (DST géré par le nom IANA).
        /// </summary>
        public static DateTime InTz(DateTime utc, string tz)
            => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Tz(tz));

        /// <summary>
        /// Date au format YYYYMMDD pour l'instant <paramref name="utc"/>, dans la TZ donnée.
        /// Équivalent Pine <c>dateInTz</c>. Sert de clé de comparaison pour la détection de
        /// changement de jour (robuste aux bornes de mois, contrairement au seul <c>dayofmonth</c>).
        /// </summary>
        public static int DateInTz(DateTime utc, string tz)
        {
            var t = InTz(utc, tz);
            return t.Year * 10000 + t.Month * 100 + t.Day;
        }

        /// <summary>
        /// Vrai si <paramref name="utc"/> tombe un jour différent de <paramref name="prevUtc"/>
        /// (dans la TZ). Si <paramref name="prevUtc"/> est <c>null</c> (première barre du dataset),
        /// retourne <c>true</c>. Équivalent Pine <c>isNewDay</c>.
        /// </summary>
        public static bool IsNewDay(DateTime utc, DateTime? prevUtc, string tz)
        {
            if (prevUtc == null) return true;
            return DateInTz(utc, tz) != DateInTz(prevUtc.Value, tz);
        }

        /// <summary>
        /// Vrai si l'instant <paramref name="utc"/> est à l'intérieur de la session, c.-à-d. si
        /// la minute du jour (dans la TZ) appartient à <c>[start, end)</c>. Une barre est "dans la
        /// session" si son timestamp d'ouverture l'est. Équivalent Pine <c>inSession</c>.
        /// Pas de cross-midnight.
        /// </summary>
        public static bool InSession(DateTime utc, string sessionStr, string tz)
        {
            ParseSession(sessionStr, out int startMin, out int endMin);
            var t = InTz(utc, tz);
            int min = t.Hour * 60 + t.Minute;
            return min >= startMin && min < endMin;
        }

        /// <summary>
        /// Construit l'instant UTC correspondant à <paramref name="hour"/>:<paramref name="minute"/>
        /// du jour de <paramref name="utc"/> (dans la TZ <paramref name="tz"/>). Sert à figer la fin
        /// d'une session (équiv Pine <c>timestamp(tz, year, month, day, hh, mm)</c> appliqué au jour
        /// courant). DST géré : on construit en heure locale puis on reconvertit en UTC.
        /// </summary>
        public static DateTime SameDayTimeUtc(DateTime utc, int hour, int minute, string tz)
        {
            var local = InTz(utc, tz);
            var localTarget = new DateTime(local.Year, local.Month, local.Day, hour, minute, 0, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(localTarget, Tz(tz));
        }

        /// <summary>
        /// Parse "HHMM-HHMM" (ex : "0900-1400") en minutes depuis minuit : <c>[startMin, endMin)</c>.
        /// </summary>
        public static void ParseSession(string sessionStr, out int startMin, out int endMin)
        {
            int startHH = int.Parse(sessionStr.Substring(0, 2));
            int startMM = int.Parse(sessionStr.Substring(2, 2));
            int endHH   = int.Parse(sessionStr.Substring(5, 2));
            int endMM   = int.Parse(sessionStr.Substring(7, 2));
            startMin = startHH * 60 + startMM;
            endMin   = endHH * 60 + endMM;
        }
    }
}
