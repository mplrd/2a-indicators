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
    [Cloud("Kumo", "Senkou A", "Senkou B", FirstColor = "Lime", SecondColor = "Maroon", Opacity = 0.3)]
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

        private const int IchiTenkanLen = 9;
        private const int IchiKijunLen  = 26;
        private const int IchiSenkouLen = 52;

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

        [Parameter("MA7 activée",   DefaultValue = true,     Group = "MA7")]
        public bool Ma7Enabled { get; set; }

        [Parameter("MA7 mode",      DefaultValue = "simple", Group = "MA7")]
        public string Ma7Mode { get; set; }

        [Parameter("MA20 activée",  DefaultValue = true,     Group = "MA20")]
        public bool Ma20Enabled { get; set; }

        [Parameter("MA20 mode",     DefaultValue = "simple", Group = "MA20")]
        public string Ma20Mode { get; set; }

        [Parameter("MA50 activée",  DefaultValue = true,     Group = "MA50")]
        public bool Ma50Enabled { get; set; }

        [Parameter("MA50 mode",     DefaultValue = "simple", Group = "MA50")]
        public string Ma50Mode { get; set; }

        [Parameter("MA200 activée", DefaultValue = true,     Group = "MA200")]
        public bool Ma200Enabled { get; set; }

        [Parameter("MA200 mode",    DefaultValue = "simple", Group = "MA200")]
        public string Ma200Mode { get; set; }

        [Parameter("Ichimoku activé",   DefaultValue = true, Group = "Ichimoku")]
        public bool IchiEnabled { get; set; }

        // Si activé, masque Tenkan/Kijun/Senkou A&B/Kumo et n'affiche que le Chikou Span.
        [Parameter("Chikou uniquement", DefaultValue = true, Group = "Ichimoku")]
        public bool IchiChikouOnly { get; set; }

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
        // Outputs — Moyennes Mobiles (basis + upper/lower invisible pour ref fill ribbon)
        // ============================================================

        [Output("MA7 Basis", LineColor = "Aqua",   PlotType = PlotType.Line, Thickness = 1, LineStyle = LineStyle.Lines)]
        public IndicatorDataSeries Ma7Basis { get; set; }

        [Output("MA7 Upper", LineColor = "Aqua",   PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries Ma7Upper { get; set; }

        [Output("MA7 Lower", LineColor = "Aqua",   PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries Ma7Lower { get; set; }

        [Output("MA20 Basis", LineColor = "Blue",  PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries Ma20Basis { get; set; }

        [Output("MA20 Upper", LineColor = "Blue",  PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries Ma20Upper { get; set; }

        [Output("MA20 Lower", LineColor = "Blue",  PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries Ma20Lower { get; set; }

        [Output("MA50 Basis", LineColor = "Orange", PlotType = PlotType.Line, Thickness = 2)]
        public IndicatorDataSeries Ma50Basis { get; set; }

        [Output("MA50 Upper", LineColor = "Orange", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries Ma50Upper { get; set; }

        [Output("MA50 Lower", LineColor = "Orange", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries Ma50Lower { get; set; }

        [Output("MA200 Basis", LineColor = "Gray", PlotType = PlotType.Line, Thickness = 2, LineStyle = LineStyle.Lines)]
        public IndicatorDataSeries Ma200Basis { get; set; }

        [Output("MA200 Upper", LineColor = "Gray", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries Ma200Upper { get; set; }

        [Output("MA200 Lower", LineColor = "Gray", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries Ma200Lower { get; set; }

        // ============================================================
        // Outputs — Ichimoku (Senkou A/B décalés +kijun, Chikou décalé −kijun)
        // ============================================================
        // Décalages temporels appliqués au WRITE :
        //   - Senkou A/B : on écrit à l'index `index + IchiKijunLen` (forward shift),
        //     cAlgo gère l'extension de la série future.
        //   - Chikou     : on écrit à l'index `index - IchiKijunLen` (backward shift)
        //     dans la même Calculate() — chaque appel rétro-actualise le slot ancien.

        [Output("Tenkan",   LineColor = "#d4a017", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries IchiTenkan { get; set; }

        [Output("Kijun",    LineColor = "Blue",    PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries IchiKijun { get; set; }

        [Output("Senkou A", LineColor = "Lime",    PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries IchiSenkouA { get; set; }

        [Output("Senkou B", LineColor = "Maroon",  PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries IchiSenkouB { get; set; }

        [Output("Chikou",   LineColor = "Black",   PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries IchiChikou { get; set; }

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

            CalculateMa(index, Ma7Enabled,   Ma7Mode   == "ribbon", 7,   Ma7Basis,   Ma7Upper,   Ma7Lower);
            CalculateMa(index, Ma20Enabled,  Ma20Mode  == "ribbon", 20,  Ma20Basis,  Ma20Upper,  Ma20Lower);
            CalculateMa(index, Ma50Enabled,  Ma50Mode  == "ribbon", 50,  Ma50Basis,  Ma50Upper,  Ma50Lower);
            CalculateMa(index, Ma200Enabled, Ma200Mode == "ribbon", 200, Ma200Basis, Ma200Upper, Ma200Lower);

            CalculateIchimoku(index);
        }

        /// <summary>
        /// Calcule le block Ichimoku pour un index donné. Senkou A/B forward-shift de +kijun bars,
        /// Chikou backward-shift de −kijun bars (via écriture aux index décalés). Si l'option
        /// "Chikou uniquement" est activée, masque Tenkan/Kijun/Senkou A/B (Kumo s'éteint avec eux).
        /// </summary>
        private void CalculateIchimoku(int index)
        {
            if (!IchiEnabled || index < IchiSenkouLen - 1)
                return;

            var (tenkan, kijun, senkouA, senkouB, chikou) = Ichimoku.Components(
                Bars.HighPrices, Bars.LowPrices, Bars.ClosePrices, index,
                IchiTenkanLen, IchiKijunLen, IchiSenkouLen);

            bool mainOn = !IchiChikouOnly;

            IchiTenkan[index] = mainOn ? tenkan : double.NaN;
            IchiKijun[index]  = mainOn ? kijun  : double.NaN;

            // Senkou A/B : forward shift +kijun bars (cAlgo gère l'extension de la série).
            int senkouIdx = index + IchiKijunLen;
            IchiSenkouA[senkouIdx] = mainOn ? senkouA : double.NaN;
            IchiSenkouB[senkouIdx] = mainOn ? senkouB : double.NaN;

            // Chikou : backward shift -kijun bars. Toujours rendu si Ichimoku activé.
            if (index >= IchiKijunLen)
                IchiChikou[index - IchiKijunLen] = chikou;
        }

        /// <summary>
        /// Calcule un block MA Ribbon (basis + upper + lower) pour un index donné.
        /// Basis toujours visible si enabled ; upper/lower visibles uniquement en mode ribbon
        /// (NaN trick, comme pour les inner Bollinger). stdFactor = 0.236 (default projet,
        /// hardcodé pour parité Pine).
        /// </summary>
        private void CalculateMa(int index, bool enabled, bool isRibbon, int length,
            IndicatorDataSeries basisOut, IndicatorDataSeries upperOut, IndicatorDataSeries lowerOut)
        {
            if (!enabled || index < length - 1)
            {
                basisOut[index] = double.NaN;
                upperOut[index] = double.NaN;
                lowerOut[index] = double.NaN;
                return;
            }

            var (basis, upper, lower) = MovingAverages.Ribbon(Bars.ClosePrices, index, length);

            basisOut[index] = basis;
            upperOut[index] = isRibbon ? upper : double.NaN;
            lowerOut[index] = isRibbon ? lower : double.NaN;
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
