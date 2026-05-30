using cAlgo.API;
using _2Ai.Indicators.Core;

namespace _2Ai.Indicators.Layout
{
    /// <summary>
    /// 2Ai Layout — portage de <c>tradingview/layout.pine</c> vers cAlgo.
    /// <para>Étape 1 — squelette : Bollinger Classique uniquement (outer + inner + accent
    /// + détection flat/closing). À enrichir incrémentalement : BBm, MA × 4, Ichimoku,
    /// Supertrend, projections.</para>
    /// <para>Limite cAlgo connue : la couleur d'un <c>[Output]</c> est figée à la déclaration
    /// (impossible de la lier à un <c>[Parameter]</c> de couleur comme Pine permet via
    /// <c>input.color()</c>). L'utilisateur peut customiser via le clic-droit cAlgo
    /// → Settings → output color.</para>
    /// </summary>
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None, AutoRescale = false)]
    public class Layout : Indicator
    {
        // ============================================================
        // Constantes (defaults projet, figées par les specs Pine)
        // ============================================================
        private const int    BbcLength    = 20;
        private const double BbcMultInner = 2.0;
        private const double BbcMultOuter = 2.5;

        private const int    BbmLength    = 160;
        private const double BbmMultInner = 2.5;
        private const double BbmMultOuter = 2.8;

        // ============================================================
        // Parameters
        // ============================================================

        [Parameter("BBc activé", DefaultValue = true, Group = "Bollinger Classique")]
        public bool BbcEnabled { get; set; }

        [Parameter("BBc mode", DefaultValue = "ribbon", Group = "Bollinger Classique")]
        public string BbcMode { get; set; }  // "ribbon" | "simple"

        [Parameter("BBm activé", DefaultValue = true, Group = "Bollinger Magique")]
        public bool BbmEnabled { get; set; }

        [Parameter("BBm mode", DefaultValue = "simple", Group = "Bollinger Magique")]
        public string BbmMode { get; set; }  // "ribbon" | "simple"

        [Parameter("Bandes plates activées", DefaultValue = true, Group = "Bandes Plates")]
        public bool FlatEnabled { get; set; }

        [Parameter("Période flat", DefaultValue = 2, MinValue = 1, MaxValue = 20, Group = "Bandes Plates")]
        public int FlatPeriod { get; set; }

        [Parameter("Seuil flat %", DefaultValue = 0.015, MinValue = 0.001, MaxValue = 1.0, Step = 0.001, Group = "Bandes Plates")]
        public double FlatThreshold { get; set; }

        // ============================================================
        // Outputs — Bollinger Classique
        // ============================================================
        // Outer : bandes principales, gris (base) — toujours plottées si BBc activé.
        // Inner : bandes intermédiaires, visibles uniquement en mode ribbon (NaN trick).
        // Accent : overlay bull/bear quand flat/closing en mode non-ribbon (NaN trick).

        [Output("BBc Outer Upper", LineColor = "#9c9c9c", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries BbcOuterUpper { get; set; }

        [Output("BBc Outer Lower", LineColor = "#9c9c9c", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries BbcOuterLower { get; set; }

        [Output("BBc Inner Upper", LineColor = "#9c9c9c", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries BbcInnerUpper { get; set; }

        [Output("BBc Inner Lower", LineColor = "#9c9c9c", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries BbcInnerLower { get; set; }

        [Output("BBc Accent Upper", LineColor = "Red",   PlotType = PlotType.Line, Thickness = 2)]
        public IndicatorDataSeries BbcAccentUpper { get; set; }

        [Output("BBc Accent Lower", LineColor = "Green", PlotType = PlotType.Line, Thickness = 2)]
        public IndicatorDataSeries BbcAccentLower { get; set; }

        // ============================================================
        // Outputs — Bollinger Magique (longueur 160, multipls 2.5/2.8, plus épais que BBc)
        // ============================================================

        [Output("BBm Outer Upper", LineColor = "#808080", PlotType = PlotType.Line, Thickness = 2)]
        public IndicatorDataSeries BbmOuterUpper { get; set; }

        [Output("BBm Outer Lower", LineColor = "#808080", PlotType = PlotType.Line, Thickness = 2)]
        public IndicatorDataSeries BbmOuterLower { get; set; }

        [Output("BBm Inner Upper", LineColor = "#808080", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries BbmInnerUpper { get; set; }

        [Output("BBm Inner Lower", LineColor = "#808080", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries BbmInnerLower { get; set; }

        [Output("BBm Accent Upper", LineColor = "Red",   PlotType = PlotType.Line, Thickness = 3)]
        public IndicatorDataSeries BbmAccentUpper { get; set; }

        [Output("BBm Accent Lower", LineColor = "Green", PlotType = PlotType.Line, Thickness = 3)]
        public IndicatorDataSeries BbmAccentLower { get; set; }

        // ============================================================
        // Lifecycle
        // ============================================================

        protected override void Initialize()
        {
            // Pas d'état persistant à initialiser pour BBc seul.
            // Les Supertrend / IndicatorDataSeries mémoire seront créés ici aux étapes suivantes.
        }

        public override void Calculate(int index)
        {
            CalculateBb(
                index, BbcEnabled, BbcMode == "ribbon",
                BbcLength, BbcMultInner, BbcMultOuter,
                BbcOuterUpper, BbcOuterLower, BbcInnerUpper, BbcInnerLower,
                BbcAccentUpper, BbcAccentLower);

            CalculateBb(
                index, BbmEnabled, BbmMode == "ribbon",
                BbmLength, BbmMultInner, BbmMultOuter,
                BbmOuterUpper, BbmOuterLower, BbmInnerUpper, BbmInnerLower,
                BbmAccentUpper, BbmAccentLower);
        }

        /// <summary>
        /// Calcule un block Bollinger (outer + inner + accent) pour un index donné.
        /// Extrait pour éviter la duplication BBc / BBm (et futurs BB si extension projet).
        /// Note : pas extrait en Core lib parce que la signature manipule des
        /// <c>IndicatorDataSeries</c> de l'indicateur — ce serait coupler Core à un
        /// indicateur précis. Helper privé local, factorise dans le scope Layout.
        /// </summary>
        private void CalculateBb(
            int index, bool enabled, bool isRibbon,
            int length, double multInner, double multOuter,
            IndicatorDataSeries outerUpper, IndicatorDataSeries outerLower,
            IndicatorDataSeries innerUpper, IndicatorDataSeries innerLower,
            IndicatorDataSeries accentUpper, IndicatorDataSeries accentLower)
        {
            if (!enabled || index < length - 1)
            {
                outerUpper[index]  = double.NaN;
                outerLower[index]  = double.NaN;
                innerUpper[index]  = double.NaN;
                innerLower[index]  = double.NaN;
                accentUpper[index] = double.NaN;
                accentLower[index] = double.NaN;
                return;
            }

            var (_, iU, iL, oU, oL) = Bollinger.Bands(Bars.ClosePrices, index, length, multInner, multOuter);

            outerUpper[index] = oU;
            outerLower[index] = oL;

            // Inner bands : visibles en mode ribbon uniquement.
            innerUpper[index] = isRibbon ? iU : double.NaN;
            innerLower[index] = isRibbon ? iL : double.NaN;

            // Détection flat / closing sur les bandes outer.
            bool upperFlat    = FlatEnabled && Series.IsFlatSeries(outerUpper,    index, FlatPeriod, FlatThreshold);
            bool upperClosing = FlatEnabled && Series.IsClosingSeries(outerUpper, index, FlatPeriod, FlatThreshold, true);
            bool lowerFlat    = FlatEnabled && Series.IsFlatSeries(outerLower,    index, FlatPeriod, FlatThreshold);
            bool lowerClosing = FlatEnabled && Series.IsClosingSeries(outerLower, index, FlatPeriod, FlatThreshold, false);

            // Accent : visible en mode non-ribbon quand la bande est plate ou en fermeture.
            accentUpper[index] = (!isRibbon && (upperFlat || upperClosing)) ? oU : double.NaN;
            accentLower[index] = (!isRibbon && (lowerFlat || lowerClosing)) ? oL : double.NaN;
        }
    }
}
