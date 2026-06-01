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

        [Parameter("Afficher les labels", DefaultValue = true, Group = "Paramètres globaux")]
        public bool ShowLabels { get; set; }

        // ============================================================
        // État interne
        // ============================================================
        private Bars _daily, _weekly, _monthly;
        private AllTimeHigh _ath;

        protected override void Initialize()
        {
            _daily   = MarketData.GetBars(TimeFrame.Daily);
            _weekly  = MarketData.GetBars(TimeFrame.Weekly);
            _monthly = MarketData.GetBars(TimeFrame.Monthly);
            _ath     = new AllTimeHigh();
        }

        public override void Calculate(int index)
        {
            // Running max ATH sur toutes les barres ; dessin uniquement sur la dernière (chaque tick).
            _ath.Update(Bars.HighPrices[index], index, Bars.OpenTimes[index]);

            if (!IsLastBar) return;

            var now = Bars.OpenTimes[index];
            double tfSec = index > 0 ? (Bars.OpenTimes[index] - Bars.OpenTimes[index - 1]).TotalSeconds : 0;

            // Filtres TF : un niveau s'affiche si la TF courante <= période du niveau.
            bool showDaily   = tfSec <= 86400;       // <= D
            bool showWeekly  = tfSec <= 604800;      // <= W
            bool showMonthly = tfSec <= 2629800;     // <= M (mois moyen)

            // ATH (toujours TF-allowed).
            DrawHtf("ATH", AthEnabled, _ath.Value, _ath.Time ?? now, now, AthColor, AthWidth, LineStyle.Lines, "ATH", true);

            // PDH / PDL.
            var (pdH, pdL) = CoreLevels.PreviousPeriodHL(_daily, now);
            var pdStart = CoreLevels.CurrentPeriodStartUtc(_daily, now) ?? now;
            DrawHtf("PDH", DailyEnabled && showDaily, pdH, pdStart, now, HtfColor, DailyWidth, LineStyle.Lines, "PDH", true);
            DrawHtf("PDL", DailyEnabled && showDaily, pdL, pdStart, now, HtfColor, DailyWidth, LineStyle.Lines, "PDL", false);

            // PWH / PWL.
            var (pwH, pwL) = CoreLevels.PreviousPeriodHL(_weekly, now);
            var pwStart = CoreLevels.CurrentPeriodStartUtc(_weekly, now) ?? now;
            DrawHtf("PWH", WeeklyEnabled && showWeekly, pwH, pwStart, now, HtfColor, WeeklyWidth, LineStyle.Solid, "PWH", true);
            DrawHtf("PWL", WeeklyEnabled && showWeekly, pwL, pwStart, now, HtfColor, WeeklyWidth, LineStyle.Solid, "PWL", false);

            // PMH / PML.
            var (pmH, pmL) = CoreLevels.PreviousPeriodHL(_monthly, now);
            var pmStart = CoreLevels.CurrentPeriodStartUtc(_monthly, now) ?? now;
            DrawHtf("PMH", MonthlyEnabled && showMonthly, pmH, pmStart, now, HtfColor, MonthlyWidth, LineStyle.Solid, "PMH", true);
            DrawHtf("PML", MonthlyEnabled && showMonthly, pmL, pmStart, now, HtfColor, MonthlyWidth, LineStyle.Solid, "PML", false);
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
    }
}
