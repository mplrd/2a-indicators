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
    [Cloud("Senkou A", "Senkou B", FirstColor = "Lime", SecondColor = "Maroon")]
    // Ruban BB (mode ribbon) : un cloud gris continu entre outer et inner. Géométrie continue
    // → suit la bande sans diagonale ni pont NaN. La mise en évidence de platitude se fait par
    // le BORD coloré (accent bull/bear par barre, cf. outputs Accent), PAS par la couleur du
    // fond : cAlgo ne sait pas colorer un cloud par barre sans rampe diagonale d'une barre.
    // En mode simple l'inner vaut NaN → le cloud ne se dessine pas (fill réservé au ribbon).
    [Cloud("BBc Outer Upper", "BBc Inner Upper", FirstColor = "#9C9C9C", Opacity = 0.15)]
    [Cloud("BBc Outer Lower", "BBc Inner Lower", FirstColor = "#9C9C9C", Opacity = 0.15)]
    [Cloud("BBm Outer Upper", "BBm Inner Upper", FirstColor = "#808080", Opacity = 0.15)]
    [Cloud("BBm Outer Lower", "BBm Inner Lower", FirstColor = "#808080", Opacity = 0.15)]
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

        private const int    StAtrPeriod = 10;
        private const double StFactor    = 3.0;

        private const int ProjectBars = 3;

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

        [Parameter("Supertrend activé", DefaultValue = false, Group = "Supertrend")]
        public bool StEnabled { get; set; }

        [Parameter("Projeter MA",  DefaultValue = true, Group = "Projections")]
        public bool ProjectMa { get; set; }

        [Parameter("Projeter BB",  DefaultValue = true, Group = "Projections")]
        public bool ProjectBb { get; set; }

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
        // Accent : bord bull/bear quand flat/closing, dans les 2 modes (NaN trick + DiscontinuousLine).

        [Output("BBc Outer Upper", LineColor = "#9c9c9c", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries BbcOuterUpper { get; set; }

        [Output("BBc Outer Lower", LineColor = "#9c9c9c", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries BbcOuterLower { get; set; }

        [Output("BBc Inner Upper", LineColor = "#9c9c9c", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries BbcInnerUpper { get; set; }

        [Output("BBc Inner Lower", LineColor = "#9c9c9c", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries BbcInnerLower { get; set; }

        // DiscontinuousLine (pas Line) : sinon cAlgo relie les segments plats par une corde droite
        // par-dessus les trous NaN (zones ouvertes) → trait fantôme décalé de la bande courbe.
        [Output("BBc Accent Upper", LineColor = "Red",   PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries BbcAccentUpper { get; set; }

        [Output("BBc Accent Lower", LineColor = "Green", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
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

        [Output("BBm Accent Upper", LineColor = "Red",   PlotType = PlotType.DiscontinuousLine, Thickness = 3)]
        public IndicatorDataSeries BbmAccentUpper { get; set; }

        [Output("BBm Accent Lower", LineColor = "Green", PlotType = PlotType.DiscontinuousLine, Thickness = 3)]
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
        // Outputs — Supertrend (broken line via DiscontinuousLine, color dynamique via 2 outputs)
        // ============================================================
        // cAlgo n'autorise pas de lier dynamiquement la couleur d'un [Output] à la direction
        // d'un calcul. Solution équivalente Pine : 2 outputs en DiscontinuousLine, le NaN trick
        // sélectionne lequel s'affiche selon la direction. Le NaN à la bougie de bascule (cf.
        // pattern Pine stLineBroken) coupe les deux lignes simultanément, créant l'effet break.

        [Output("Supertrend Bull", LineColor = "Green", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries StBull { get; set; }

        [Output("Supertrend Bear", LineColor = "Red",   PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries StBear { get; set; }

        // ============================================================
        // État interne (Supertrend mémoire)
        // ============================================================
        private cAlgo.API.Indicators.AverageTrueRange _atr;
        private IndicatorDataSeries _stLineMem;
        private IndicatorDataSeries _stDirMem;

        // ============================================================
        // Lifecycle
        // ============================================================

        protected override void Initialize()
        {
            _atr       = Indicators.AverageTrueRange(StAtrPeriod, MovingAverageType.WilderSmoothing);
            _stLineMem = CreateDataSeries();
            _stDirMem  = CreateDataSeries();
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
            CalculateSupertrend(index);

            // Projections : redraw sur la dernière barre (chaque tick en live), updates les
            // Chart.DrawTrendLine en place via leur nom unique. Aucun objet à nettoyer.
            if (index == Bars.Count - 1)
                DrawProjections(index);
        }

        /// <summary>
        /// Dessine les lignes de projection (MA basis + BB outer/inner) en pointillé sur les
        /// `ProjectBars` prochaines barres. Appelé uniquement sur la dernière barre — chaque
        /// `Chart.DrawTrendLine` avec un nom unique UPDATE la ligne existante (pas de leak).
        /// </summary>
        private void DrawProjections(int index)
        {
            var t1 = Bars.OpenTimes[index];
            // Durée d'une barre : différence entre 2 barres adjacentes. Fallback 1h si pas
            // assez d'historique (cas dégénéré, ne devrait jamais se produire à index == Bars.Count - 1).
            var barSpan = index > 0 ? t1 - Bars.OpenTimes[index - 1] : System.TimeSpan.FromHours(1);
            var t2 = t1.Add(System.TimeSpan.FromTicks(barSpan.Ticks * ProjectBars));

            if (ProjectMa)
            {
                if (Ma7Enabled)
                    Draw.DrawProjection(Chart, "Layout_MA7_proj",   t1, Ma7Basis[index],   t2,
                        MovingAverages.ProjectSma(Bars.ClosePrices, index, 7,   ProjectBars), Color.Aqua,   1);
                if (Ma20Enabled)
                    Draw.DrawProjection(Chart, "Layout_MA20_proj",  t1, Ma20Basis[index],  t2,
                        MovingAverages.ProjectSma(Bars.ClosePrices, index, 20,  ProjectBars), Color.Blue,   1);
                if (Ma50Enabled)
                    Draw.DrawProjection(Chart, "Layout_MA50_proj",  t1, Ma50Basis[index],  t2,
                        MovingAverages.ProjectSma(Bars.ClosePrices, index, 50,  ProjectBars), Color.Orange, 2);
                if (Ma200Enabled)
                    Draw.DrawProjection(Chart, "Layout_MA200_proj", t1, Ma200Basis[index], t2,
                        MovingAverages.ProjectSma(Bars.ClosePrices, index, 200, ProjectBars), Color.Gray,   2);
            }

            if (ProjectBb)
            {
                if (BbcEnabled)
                {
                    var (_, iU, iL, oU, oL) = Bollinger.ProjectBands(Bars.ClosePrices, index,
                        BbcLength, BbcMultInner, BbcMultOuter, ProjectBars);
                    var bbcCol = Color.FromHex("#9c9c9c");
                    Draw.DrawProjection(Chart, "Layout_BBc_OU_proj", t1, BbcOuterUpper[index], t2, oU, bbcCol, 1);
                    Draw.DrawProjection(Chart, "Layout_BBc_OL_proj", t1, BbcOuterLower[index], t2, oL, bbcCol, 1);
                    if (BbcMode == "ribbon")
                    {
                        Draw.DrawProjection(Chart, "Layout_BBc_IU_proj", t1, BbcInnerUpper[index], t2, iU, bbcCol, 1);
                        Draw.DrawProjection(Chart, "Layout_BBc_IL_proj", t1, BbcInnerLower[index], t2, iL, bbcCol, 1);
                    }
                }
                if (BbmEnabled)
                {
                    var (_, iU, iL, oU, oL) = Bollinger.ProjectBands(Bars.ClosePrices, index,
                        BbmLength, BbmMultInner, BbmMultOuter, ProjectBars);
                    var bbmCol = Color.FromHex("#808080");
                    Draw.DrawProjection(Chart, "Layout_BBm_OU_proj", t1, BbmOuterUpper[index], t2, oU, bbmCol, 2);
                    Draw.DrawProjection(Chart, "Layout_BBm_OL_proj", t1, BbmOuterLower[index], t2, oL, bbmCol, 2);
                    if (BbmMode == "ribbon")
                    {
                        Draw.DrawProjection(Chart, "Layout_BBm_IU_proj", t1, BbmInnerUpper[index], t2, iU, bbmCol, 1);
                        Draw.DrawProjection(Chart, "Layout_BBm_IL_proj", t1, BbmInnerLower[index], t2, iL, bbmCol, 1);
                    }
                }
            }
        }

        /// <summary>
        /// Calcule le block Supertrend pour un index donné. Délègue à <see cref="Supertrend.Calculate"/>
        /// de Core (qui maintient state via les 2 IndicatorDataSeries mémoire). Dispatche le résultat
        /// sur 2 outputs Bull/Bear selon la direction (couleurs figées en attribute). Bougie de bascule
        /// (changement de direction) → NaN sur les 2 outputs pour effet "broken line" (pattern Pine
        /// stLineBroken).
        /// </summary>
        private void CalculateSupertrend(int index)
        {
            if (!StEnabled || index < StAtrPeriod)
            {
                StBull[index] = double.NaN;
                StBear[index] = double.NaN;
                return;
            }

            var (line, dir) = _2Ai.Indicators.Core.Supertrend.Calculate(
                Bars.HighPrices, Bars.LowPrices, Bars.ClosePrices, _atr.Result,
                index, StFactor, _stLineMem, _stDirMem);

            int prevDir = index > 0 ? (int)_stDirMem[index - 1] : 0;
            bool dirChanged = prevDir != 0 && prevDir != dir;

            if (dirChanged)
            {
                StBull[index] = double.NaN;
                StBear[index] = double.NaN;
            }
            else if (dir == 1)
            {
                StBull[index] = line;
                StBear[index] = double.NaN;
            }
            else
            {
                StBull[index] = double.NaN;
                StBear[index] = line;
            }
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

            // Inner bands : visibles en mode ribbon uniquement. Elles bornent aussi le cloud gris
            // du ruban (cf. clouds en tête de classe) → NaN en simple = pas de fill.
            innerUpper[index] = isRibbon ? iU : double.NaN;
            innerLower[index] = isRibbon ? iL : double.NaN;

            // Détection flat / closing sur les bandes outer.
            bool upperFlat    = FlatEnabled && Series.IsFlatSeries(outerUpper,    index, FlatPeriod, FlatThreshold);
            bool upperClosing = FlatEnabled && Series.IsClosingSeries(outerUpper, index, FlatPeriod, FlatThreshold, true);
            bool lowerFlat    = FlatEnabled && Series.IsFlatSeries(outerLower,    index, FlatPeriod, FlatThreshold);
            bool lowerClosing = FlatEnabled && Series.IsClosingSeries(outerLower, index, FlatPeriod, FlatThreshold, false);

            // Accent = bord coloré bull/bear sur la bande outer quand plate/fermeture. Affiché
            // dans LES DEUX modes (en ribbon il colore le bord du ruban gris, par barre, sans
            // diagonale ; en simple il surligne la bande). DiscontinuousLine côté output.
            accentUpper[index] = (upperFlat || upperClosing) ? oU : double.NaN;
            accentLower[index] = (lowerFlat || lowerClosing) ? oL : double.NaN;
        }
    }
}
