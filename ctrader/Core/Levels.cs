using System;
using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>
    /// Équivalent de Pine <c>lib_levels</c>. Extraction des niveaux de prix passés : H/L de
    /// période précédente (Daily/Weekly/Monthly), ATH, H/L et Open de sessions, Open Range.
    /// Couche 1 — dépend de <see cref="Time"/>, ne dessine rien.
    ///
    /// <para>Découpage cAlgo (cf. décision d'archi du port) :</para>
    /// <list type="bullet">
    /// <item>Multi-TF anti-repaint (<see cref="PreviousPeriodHL"/>, <see cref="CurrentPeriodStartUtc"/>)
    /// = helpers PURS recevant les <c>Bars</c> du TF supérieur. C'est l'indicateur qui fait
    /// <c>MarketData.GetBars(TimeFrame.Daily/Weekly/Monthly)</c> (MarketData est une propriété
    /// d'indicateur, indisponible en static). Équivaut au
    /// <c>request.security(sym, tf, high[1], lookahead=barmerge.lookahead_on)</c> Pine : on lit la
    /// barre HTF PRÉCÉDENTE déjà clôturée → valeur figée dès l'ouverture de la période courante,
    /// donc anti-repaint par construction.</item>
    /// <item>Trackers par barre (<see cref="AllTimeHigh"/>, <see cref="SessionRange"/>,
    /// <see cref="SessionOpen"/>, <see cref="OpenRange"/>) = classes STATEFUL (les <c>var</c> Pine
    /// par site d'appel n'ont pas d'équivalent static). L'indicateur les instancie une fois et
    /// appelle <c>Update(...)</c> à chaque barre.</item>
    /// </list>
    /// </summary>
    public static class Levels
    {
        /// <summary>
        /// High et Low de la période PRÉCÉDENTE du TF supérieur (anti-repaint). On localise la
        /// barre HTF contenant <paramref name="barTimeUtc"/> et on renvoie le H/L de la barre
        /// d'avant (déjà clôturée). Équivaut à Pine <c>previousPeriodHL(tf)</c>.
        /// </summary>
        /// <param name="htfBars">Bars du TF supérieur (Daily/Weekly/Monthly) du symbole courant.</param>
        /// <param name="barTimeUtc">Open time (UTC) de la barre courante du chart.</param>
        /// <returns>(prevHigh, prevLow), ou (NaN, NaN) si pas assez d'historique HTF.</returns>
        public static (double high, double low) PreviousPeriodHL(Bars htfBars, DateTime barTimeUtc)
        {
            int i = htfBars.OpenTimes.GetIndexByTime(barTimeUtc);
            if (i < 1) return (double.NaN, double.NaN);
            return (htfBars.HighPrices[i - 1], htfBars.LowPrices[i - 1]);
        }

        /// <summary>
        /// Open time (UTC) du DÉBUT de la période PRÉCÉDENTE du TF supérieur — point d'ancrage du
        /// niveau dont on trace le H/L (ex : PDH ancré au début d'hier, étendu vers la droite).
        /// Équivaut à Pine <c>previousPeriodStartBar</c> (qui renvoie <c>prevStart</c>, le début de
        /// la période d'avant, et NON la période courante).
        /// </summary>
        /// <returns>Open time de la barre HTF précédente, ou <c>null</c> si pas assez d'historique.</returns>
        public static DateTime? PreviousPeriodStartUtc(Bars htfBars, DateTime barTimeUtc)
        {
            int i = htfBars.OpenTimes.GetIndexByTime(barTimeUtc);
            if (i < 1) return null;
            return htfBars.OpenTimes[i - 1];
        }
    }

    /// <summary>
    /// Running max du high (All-Time High) avec mémorisation de la barre où il a été atteint.
    /// Équivaut à Pine <c>ath()</c> côté chart-courant. NB : la portée temporelle dépend de
    /// l'historique chargé par cAlgo ; pour étendre au-delà, l'indicateur peut amorcer le tracker
    /// avec des highs daily (via <see cref="Seed"/>) avant la première barre chart.
    /// </summary>
    public class AllTimeHigh
    {
        /// <summary>Valeur du plus haut historique observé (NaN tant qu'aucune barre vue).</summary>
        public double Value { get; private set; } = double.NaN;
        /// <summary>Index de la barre chart où l'ATH a été atteint (-1 si jamais).</summary>
        public int BarIndex { get; private set; } = -1;
        /// <summary>Open time (UTC) de la barre où l'ATH a été atteint.</summary>
        public DateTime? Time { get; private set; }

        /// <summary>Amorce le running max sans index/temps de barre (ex : highs daily). </summary>
        public void Seed(double high)
        {
            if (double.IsNaN(high)) return;
            if (double.IsNaN(Value) || high > Value) Value = high;
        }

        /// <summary>MAJ à chaque barre chart. Met à jour la valeur, l'index et le temps si record.</summary>
        public void Update(double high, int barIndex, DateTime barTimeUtc)
        {
            if (double.IsNaN(high)) return;
            if (double.IsNaN(Value) || high >= Value)
            {
                Value = high;
                BarIndex = barIndex;
                Time = barTimeUtc;
            }
        }
    }

    /// <summary>
    /// H/L de la session courante. Reset à chaque changement de date dans <c>chartTz</c> (pas de
    /// session de la veille visible le lendemain). MAJ en temps réel pendant la session, figé
    /// après. Équivaut à Pine <c>sessionHL</c>. Pas de cross-midnight.
    /// </summary>
    public class SessionRange
    {
        private readonly string _session, _sessionTz, _chartTz;
        private readonly int _endMin;
        private int _lastDate = -1;
        private bool _wasInSession;

        public double High { get; private set; } = double.NaN;
        public double Low { get; private set; } = double.NaN;
        public DateTime? StartUtc { get; private set; }
        public DateTime? EndUtc { get; private set; }
        public bool SeenToday { get; private set; }
        public bool InSession { get; private set; }

        public SessionRange(string sessionStr, string sessionTz, string chartTz)
        {
            _session = sessionStr;
            _sessionTz = sessionTz;
            _chartTz = chartTz;
            Time.ParseSession(sessionStr, out _, out _endMin);
        }

        public void Update(DateTime barTimeUtc, double high, double low)
        {
            int date = Time.DateInTz(barTimeUtc, _chartTz);
            // Pas de reset sur la toute première barre (parité Pine : ta.change = na au 1er bar).
            if (_lastDate != -1 && date != _lastDate)
            {
                High = double.NaN; Low = double.NaN;
                StartUtc = null; EndUtc = null;
                SeenToday = false; _wasInSession = false;
            }
            _lastDate = date;

            bool inS = Time.InSession(barTimeUtc, _session, _sessionTz);
            if (inS && !_wasInSession)
            {
                StartUtc = barTimeUtc;
                EndUtc = Time.SameDayTimeUtc(barTimeUtc, _endMin / 60, _endMin % 60, _sessionTz);
                High = high; Low = low;
                SeenToday = true;
            }
            else if (inS)
            {
                High = Math.Max(High, high);
                Low = Math.Min(Low, low);
            }
            _wasInSession = inS;
            InSession = inS;
        }
    }

    /// <summary>
    /// Prix (et temps) d'ouverture de la session pour la journée chart courante. Reset au
    /// changement de date <c>chartTz</c>. Équivaut à Pine <c>sessionOpen</c>.
    /// </summary>
    public class SessionOpen
    {
        private readonly string _session, _sessionTz, _chartTz;
        private int _lastDate = -1;
        private bool _wasInSession;

        public double Price { get; private set; } = double.NaN;
        public DateTime? TimeUtc { get; private set; }
        public bool SeenToday { get; private set; }

        public SessionOpen(string sessionStr, string sessionTz, string chartTz)
        {
            _session = sessionStr;
            _sessionTz = sessionTz;
            _chartTz = chartTz;
        }

        public void Update(DateTime barTimeUtc, double open)
        {
            int date = Time.DateInTz(barTimeUtc, _chartTz);
            if (_lastDate != -1 && date != _lastDate)
            {
                Price = double.NaN; TimeUtc = null;
                SeenToday = false; _wasInSession = false;
            }
            _lastDate = date;

            bool inS = Time.InSession(barTimeUtc, _session, _sessionTz);
            if (inS && !_wasInSession)
            {
                Price = open;
                TimeUtc = barTimeUtc;
                SeenToday = true;
            }
            _wasInSession = inS;
        }
    }

    /// <summary>
    /// H/L des 60 PREMIÈRES MINUTES de la journée (Open Range), "minuit" défini dans la TZ donnée.
    /// Tracke jusqu'à start + 60 min puis fige. À utiliser sur TF &lt; H1. Équivaut à Pine
    /// <c>openRange</c>. La TZ est explicite (Pine n'expose pas la TZ d'affichage du chart).
    /// </summary>
    public class OpenRange
    {
        private readonly string _tz;
        private int _lastDate = -1;

        public double High { get; private set; } = double.NaN;
        public double Low { get; private set; } = double.NaN;
        public DateTime? StartUtc { get; private set; }
        public DateTime? EndUtc { get; private set; }
        public bool SeenToday { get; private set; }

        public OpenRange(string tz) { _tz = tz; }

        public void Update(DateTime barTimeUtc, double high, double low)
        {
            int date = Time.DateInTz(barTimeUtc, _tz);
            bool isNewDay = _lastDate != -1 && date != _lastDate;
            _lastDate = date;

            if (isNewDay)
            {
                StartUtc = barTimeUtc;
                EndUtc = barTimeUtc.AddHours(1);
                High = high; Low = low;
                SeenToday = true;
            }
            else if (SeenToday && EndUtc.HasValue && barTimeUtc < EndUtc.Value)
            {
                High = Math.Max(High, high);
                Low = Math.Min(Low, low);
            }
        }
    }
}
