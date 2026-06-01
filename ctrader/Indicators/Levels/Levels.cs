using System;
using cAlgo.API;
using _2Ai.Indicators.Core;
using CoreLevels = _2Ai.Indicators.Core.Levels;

namespace _2Ai.Indicators.Levels
{
    /// <summary>
    /// 2Ai Levels — portage de <c>tradingview/levels.pine</c> vers cAlgo.
    /// <para>Jalon 1 — niveaux HTF : ATH + niveaux de période précédente PDH/PDL, PWH/PWL,
    /// PMH/PML. À enrichir : sessions H/L, Opens, Open Range, puis Supertrend/BB/MA, puis
    /// IPA (lib_structure) et Gaps (lib_gap).</para>
    /// <para>Limite cAlgo (cf. Layout) : pas d'input couleur/enum-style. On expose enable (bool)
    /// + width (int) en [Parameter] et on hardcode couleurs/styles aux defaults de la spec ;
    /// customisation via clic-droit cAlgo sur l'objet.</para>
    /// </summary>
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None, AutoRescale = false)]
    public class Levels : Indicator
    {
        // Couleurs/styles figés (defaults spec Pine).
        private static readonly Color AthColor = Color.FromHex("#8B0000");
        private static readonly Color HtfColor = Color.FromHex("#555555");

        // ============================================================
        // Parameters (enable + width ; cf. limite couleur/style)
        // ============================================================
        [Parameter("ATH", DefaultValue = true, Group = "Niveaux HTF")]
        public bool AthEnabled { get; set; }
        [Parameter("ATH width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Niveaux HTF")]
        public int AthWidth { get; set; }

        [Parameter("Daily (PDH/PDL)", DefaultValue = true, Group = "Niveaux HTF")]
        public bool DailyEnabled { get; set; }
        [Parameter("Daily width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Niveaux HTF")]
        public int DailyWidth { get; set; }

        [Parameter("Weekly (PWH/PWL)", DefaultValue = true, Group = "Niveaux HTF")]
        public bool WeeklyEnabled { get; set; }
        [Parameter("Weekly width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Niveaux HTF")]
        public int WeeklyWidth { get; set; }

        [Parameter("Monthly (PMH/PML)", DefaultValue = true, Group = "Niveaux HTF")]
        public bool MonthlyEnabled { get; set; }
        [Parameter("Monthly width", DefaultValue = 3, MinValue = 1, MaxValue = 5, Group = "Niveaux HTF")]
        public int MonthlyWidth { get; set; }

        // === Niveaux Intraday (sessions / opens / open range) — strict < H1 ===
        [Parameter("Session Asiatique", DefaultValue = true, Group = "Niveaux Intraday")]
        public bool AsianEnabled { get; set; }
        [Parameter("Asian width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Niveaux Intraday")]
        public int AsianWidth { get; set; }

        [Parameter("Session Européenne", DefaultValue = true, Group = "Niveaux Intraday")]
        public bool EuEnabled { get; set; }
        [Parameter("EU width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Niveaux Intraday")]
        public int EuWidth { get; set; }

        [Parameter("Session Américaine", DefaultValue = true, Group = "Niveaux Intraday")]
        public bool UsEnabled { get; set; }
        [Parameter("US width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Niveaux Intraday")]
        public int UsWidth { get; set; }

        [Parameter("Open Range", DefaultValue = true, Group = "Niveaux Intraday")]
        public bool OrEnabled { get; set; }
        [Parameter("OR width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Niveaux Intraday")]
        public int OrWidth { get; set; }

        [Parameter("Open Future", DefaultValue = false, Group = "Niveaux Intraday")]
        public bool OpenFutureEnabled { get; set; }
        [Parameter("Open EU", DefaultValue = false, Group = "Niveaux Intraday")]
        public bool OpenEuEnabled { get; set; }
        [Parameter("Open US", DefaultValue = false, Group = "Niveaux Intraday")]
        public bool OpenUsEnabled { get; set; }

        [Parameter("Afficher les labels", DefaultValue = true, Group = "Paramètres globaux")]
        public bool ShowLabels { get; set; }

        // ============================================================
        // Constantes Intraday (defaults spec ; cAlgo pourra exposer les strings plus tard)
        // ============================================================
        private const string ChartTz = "Europe/Paris";
        private static readonly Color AsianColor = Color.Orange;
        private static readonly Color EuColor     = Color.Blue;
        private static readonly Color UsColor      = Color.Purple;
        private static readonly Color OrColor      = Color.Black;

        // ============================================================
        // État interne
        // ============================================================
        private Bars _daily, _weekly, _monthly;
        private AllTimeHigh _ath;
        private SessionRange _asian, _eu, _us;
        private SessionOpen _asianOpen, _euOpen, _usOpen, _futureOpen;
        private OpenRange _or;

        protected override void Initialize()
        {
            _daily   = MarketData.GetBars(TimeFrame.Daily);
            _weekly  = MarketData.GetBars(TimeFrame.Weekly);
            _monthly = MarketData.GetBars(TimeFrame.Monthly);
            _ath     = new AllTimeHigh();

            _asian = new SessionRange("0800-1400", "Asia/Tokyo",       ChartTz);
            _eu    = new SessionRange("0900-1400", "Europe/Paris",     ChartTz);
            _us    = new SessionRange("0930-1600", "America/New_York", ChartTz);

            _asianOpen  = new SessionOpen("0800-1400", "Asia/Tokyo",       ChartTz);
            _euOpen     = new SessionOpen("0900-1400", "Europe/Paris",     ChartTz);
            _usOpen     = new SessionOpen("0930-1600", "America/New_York", ChartTz);
            _futureOpen = new SessionOpen("0800-0900", "Europe/Paris",     ChartTz);

            _or = new OpenRange(ChartTz);
        }

        public override void Calculate(int index)
        {
            // Trackers stateful : MAJ sur TOUTES les barres (chronologique), dessin sur la dernière.
            var t = Bars.OpenTimes[index];
            double hi = Bars.HighPrices[index], lo = Bars.LowPrices[index], op = Bars.OpenPrices[index];
            _ath.Update(hi, index, t);
            _asian.Update(t, hi, lo); _eu.Update(t, hi, lo); _us.Update(t, hi, lo);
            _asianOpen.Update(t, op); _euOpen.Update(t, op); _usOpen.Update(t, op); _futureOpen.Update(t, op);
            _or.Update(t, hi, lo);

            if (!IsLastBar) return;

            var now = Bars.OpenTimes[index];
            double tfSec = index > 0 ? (Bars.OpenTimes[index] - Bars.OpenTimes[index - 1]).TotalSeconds : 0;

            // Filtres TF : un niveau s'affiche si la TF courante <= période du niveau.
            bool showDaily   = tfSec <= 86400;       // <= D
            bool showWeekly  = tfSec <= 604800;      // <= W
            bool showMonthly = tfSec <= 2629800;     // <= M (mois moyen)
            bool showIntraday = tfSec < 3600;        // < H1 (sessions / opens / open range)

            // ATH (toujours TF-allowed).
            DrawHtf("ATH", AthEnabled, _ath.Value, _ath.Time ?? now, now, AthColor, AthWidth, LineStyle.Lines, "ATH", true);

            // PDH / PDL.
            var (pdH, pdL) = CoreLevels.PreviousPeriodHL(_daily, now);
            var pdStart = CoreLevels.PreviousPeriodStartUtc(_daily, now) ?? now;
            DrawHtf("PDH", DailyEnabled && showDaily, pdH, pdStart, now, HtfColor, DailyWidth, LineStyle.Lines, "PDH", true);
            DrawHtf("PDL", DailyEnabled && showDaily, pdL, pdStart, now, HtfColor, DailyWidth, LineStyle.Lines, "PDL", false);

            // PWH / PWL.
            var (pwH, pwL) = CoreLevels.PreviousPeriodHL(_weekly, now);
            var pwStart = CoreLevels.PreviousPeriodStartUtc(_weekly, now) ?? now;
            DrawHtf("PWH", WeeklyEnabled && showWeekly, pwH, pwStart, now, HtfColor, WeeklyWidth, LineStyle.Solid, "PWH", true);
            DrawHtf("PWL", WeeklyEnabled && showWeekly, pwL, pwStart, now, HtfColor, WeeklyWidth, LineStyle.Solid, "PWL", false);

            // PMH / PML.
            var (pmH, pmL) = CoreLevels.PreviousPeriodHL(_monthly, now);
            var pmStart = CoreLevels.PreviousPeriodStartUtc(_monthly, now) ?? now;
            DrawHtf("PMH", MonthlyEnabled && showMonthly, pmH, pmStart, now, HtfColor, MonthlyWidth, LineStyle.Solid, "PMH", true);
            DrawHtf("PML", MonthlyEnabled && showMonthly, pmL, pmStart, now, HtfColor, MonthlyWidth, LineStyle.Solid, "PML", false);

            // --- Intraday (strict < H1) ---
            // Sessions H/L : bornées à [start, end] (figées dès le début de session côté tracker).
            DrawSession("AsianH", AsianEnabled && showIntraday && _asian.SeenToday, _asian.High, _asian.StartUtc, _asian.EndUtc, AsianColor, AsianWidth, LineStyle.Lines, "Asian H");
            DrawSession("AsianL", AsianEnabled && showIntraday && _asian.SeenToday, _asian.Low,  _asian.StartUtc, _asian.EndUtc, AsianColor, AsianWidth, LineStyle.Lines, "Asian L");
            DrawSession("EuH",    EuEnabled    && showIntraday && _eu.SeenToday,    _eu.High,    _eu.StartUtc,    _eu.EndUtc,    EuColor,    EuWidth,    LineStyle.Lines, "EU H");
            DrawSession("EuL",    EuEnabled    && showIntraday && _eu.SeenToday,    _eu.Low,     _eu.StartUtc,    _eu.EndUtc,    EuColor,    EuWidth,    LineStyle.Lines, "EU L");
            DrawSession("UsH",    UsEnabled    && showIntraday && _us.SeenToday,    _us.High,    _us.StartUtc,    _us.EndUtc,    UsColor,    UsWidth,    LineStyle.Lines, "US H");
            DrawSession("UsL",    UsEnabled    && showIntraday && _us.SeenToday,    _us.Low,     _us.StartUtc,    _us.EndUtc,    UsColor,    UsWidth,    LineStyle.Lines, "US L");

            // Opens : ligne ongoing (start = open de session → maintenant), pointillé. Off par défaut.
            DrawSession("OpenFuture", OpenFutureEnabled && showIntraday && _futureOpen.SeenToday, _futureOpen.Price, _futureOpen.TimeUtc, now, HtfColor, 1, LineStyle.Dots, "Open Future");
            DrawSession("OpenEU",     OpenEuEnabled     && showIntraday && _euOpen.SeenToday,     _euOpen.Price,     _euOpen.TimeUtc,     now, EuColor,  1, LineStyle.Dots, "Open EU");
            DrawSession("OpenUS",     OpenUsEnabled     && showIntraday && _usOpen.SeenToday,     _usOpen.Price,     _usOpen.TimeUtc,     now, UsColor,  1, LineStyle.Dots, "Open US");

            // Open Range : H/L figés après 60 min, ligne ongoing toute la journée chart.
            DrawSession("OrH", OrEnabled && showIntraday && _or.StartUtc.HasValue, _or.High, _or.StartUtc, now, OrColor, OrWidth, LineStyle.Dots, "OR H");
            DrawSession("OrL", OrEnabled && showIntraday && _or.StartUtc.HasValue, _or.Low,  _or.StartUtc, now, OrColor, OrWidth, LineStyle.Dots, "OR L");
        }

        /// <summary>
        /// Helper local : dessine un niveau HTF si activé et valide, sinon le retire (NaN).
        /// Délègue à <see cref="Draw.DrawLevel"/>.
        /// </summary>
        private void DrawHtf(string id, bool enabled, double price, DateTime start, DateTime end,
            Color color, int width, LineStyle style, string label, bool _)
        {
            double v = enabled ? price : double.NaN;
            Draw.DrawLevel(Chart, "Lvl_" + id, start, end, v, color, width, style, label, ShowLabels);
        }

        /// <summary>
        /// Variante intraday : bornes <see cref="DateTime"/> nullables (issues des trackers). Si
        /// désactivé ou bornes manquantes → NaN (l'objet est retiré).
        /// </summary>
        private void DrawSession(string id, bool enabled, double price, DateTime? start, DateTime? end,
            Color color, int width, LineStyle style, string label)
        {
            bool ok = enabled && start.HasValue && end.HasValue;
            double v = ok ? price : double.NaN;
            Draw.DrawLevel(Chart, "Lvl_" + id, start ?? Bars.OpenTimes.LastValue, end ?? Bars.OpenTimes.LastValue,
                v, color, width, style, label, ShowLabels);
        }
    }
}
