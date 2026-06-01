using cAlgo.API;
using cAlgo.API.Indicators;
using _2Ai.Indicators.Core;

namespace _2Ai.Indicators.Divergences
{
    /// <summary>
    /// 2Ai Divergences — portage de <c>tradingview/divergences.pine</c>. RSI + Stochastic RSI avec
    /// divergences régulières/cachées. Calcul délégué à Core (<see cref="StochRsi"/>,
    /// <see cref="Divergence"/>) ; RSI/SMA via built-ins cAlgo. L'indicateur orchestre + rend.
    /// <para>Limites cAlgo : couleur d'<c>[Output]</c> figée → RSI dédoublé en 3 (neutre/over/under) ;
    /// niveaux OB/OS via <c>[Levels]</c> (valeurs fixes, pas liées aux inputs) ; fills gradient OB/OS
    /// non portés (cAlgo n'a pas de fill gradient — décoratif). Stoch K/D : nuage bicolore natif.</para>
    /// </summary>
    [Indicator(IsOverlay = false, AccessRights = AccessRights.None)]
    [Levels(25, 38, 50, 62, 75)]
    [Cloud("Stoch K", "Stoch D", FirstColor = "#5021BBF3", SecondColor = "#50673AB7")]
    public class Divergences : Indicator
    {
        private const int Smooth = 3;

        [Parameter("Length", DefaultValue = 14, MinValue = 1, Group = "Indicator Settings")]
        public int IndicLength { get; set; }

        [Parameter("RSI Display", DefaultValue = true, Group = "RSI Settings")]
        public bool RsiShow { get; set; }
        [Parameter("RSI With MA", DefaultValue = true, Group = "RSI Settings")]
        public bool RsiShowMa { get; set; }
        [Parameter("RSI MA length", DefaultValue = 50, MinValue = 1, Group = "RSI Settings")]
        public int RsiMaLength { get; set; }
        [Parameter("RSI divergences", DefaultValue = true, Group = "RSI Settings")]
        public bool RsiShowDiv { get; set; }
        [Parameter("RSI hidden divergences", DefaultValue = false, Group = "RSI Settings")]
        public bool RsiShowHiddenDiv { get; set; }

        [Parameter("Stoch Display", DefaultValue = false, Group = "Stochastic RSI Settings")]
        public bool StochShow { get; set; }
        [Parameter("Stoch divergences", DefaultValue = false, Group = "Stochastic RSI Settings")]
        public bool StochShowDiv { get; set; }
        [Parameter("Stoch hidden divergences", DefaultValue = false, Group = "Stochastic RSI Settings")]
        public bool StochShowHiddenDiv { get; set; }

        [Parameter("Oversold", DefaultValue = 38, Group = "Levels")]
        public int LevelOversold1 { get; set; }
        [Parameter("Extremely Oversold", DefaultValue = 25, Group = "Levels")]
        public int LevelOversold2 { get; set; }
        [Parameter("Overbought", DefaultValue = 62, Group = "Levels")]
        public int LevelOverbought1 { get; set; }
        [Parameter("Extremely Overbought", DefaultValue = 75, Group = "Levels")]
        public int LevelOverbought2 { get; set; }

        // RSI : 3 sorties mutuellement exclusives (couleur figée par output).
        [Output("RSI", LineColor = "Gray", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries RsiNeutral { get; set; }
        [Output("RSI Overbought", LineColor = "Red", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries RsiOver { get; set; }
        [Output("RSI Oversold", LineColor = "Green", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries RsiUnder { get; set; }
        [Output("RSI MA", LineColor = "Blue", PlotType = PlotType.Line)]
        public IndicatorDataSeries RsiMa { get; set; }
        [Output("RSI Bearish Divergence", LineColor = "#AA0000", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries RsiBearMark { get; set; }
        [Output("RSI Bullish Divergence", LineColor = "#006403", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries RsiBullMark { get; set; }

        // Stoch K/D + marqueurs.
        [Output("Stoch K", LineColor = "#21BBF3", PlotType = PlotType.Line)]
        public IndicatorDataSeries StochK { get; set; }
        [Output("Stoch D", LineColor = "#673AB7", PlotType = PlotType.Line)]
        public IndicatorDataSeries StochD { get; set; }
        [Output("Stoch Bearish Divergence", LineColor = "#FF3535", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries StochBearMark { get; set; }
        [Output("Stoch Bullish Divergence", LineColor = "#44B847", PlotType = PlotType.DiscontinuousLine, Thickness = 2)]
        public IndicatorDataSeries StochBullMark { get; set; }

        private RelativeStrengthIndex _rsi;
        private SimpleMovingAverage _rsiMa;
        private IndicatorDataSeries _stochRawMem, _kMem;
        private DivergenceMemory _memRsi, _memStoch;

        protected override void Initialize()
        {
            _rsi   = Indicators.RelativeStrengthIndex(Bars.ClosePrices, IndicLength);
            _rsiMa = Indicators.SimpleMovingAverage(_rsi.Result, RsiMaLength);
            _stochRawMem = CreateDataSeries();
            _kMem        = CreateDataSeries();
            _memRsi   = NewMem();
            _memStoch = NewMem();
        }

        private DivergenceMemory NewMem() => new DivergenceMemory
        {
            LastTopOsc = CreateDataSeries(), LastTopPrice = CreateDataSeries(),
            LastBotOsc = CreateDataSeries(), LastBotPrice = CreateDataSeries(),
            LastTopBar = CreateDataSeries(), LastBotBar = CreateDataSeries(),
        };

        // Couleur RSI à l'index j : 0 neutre / 1 overbought / 2 oversold (confirmé sur 2 barres).
        private int RsiColorAt(int j)
        {
            if (j < 1) return 0;
            double v = _rsi.Result[j], p = _rsi.Result[j - 1];
            bool u = (v < LevelOversold2 && p <= LevelOversold2) || (v < LevelOversold1 && p <= LevelOversold1);
            bool o = (v > LevelOverbought2 && p >= LevelOverbought2) || (v > LevelOverbought1 && p >= LevelOverbought1);
            return u ? 2 : o ? 1 : 0;
        }

        // Trace une ligne droite (interpolée) du pivot barA au pivot barB sur une sortie
        // DiscontinuousLine : les barres consécutives se relient, et c'est discontinu entre divergences.
        private void DrawDivLine(IndicatorDataSeries outp, DataSeries osc, int barA, int barB)
        {
            if (barA < 0 || barB <= barA) return;
            double va = osc[barA], vb = osc[barB];
            for (int j = barA; j <= barB; j++)
                outp[j] = va + (vb - va) * (double)(j - barA) / (barB - barA);
        }

        public override void Calculate(int index)
        {
            double rsi = _rsi.Result[index];

            // --- RSI couleur (0 neutre / 1 overbought / 2 oversold), confirmé sur 2 barres ---
            // Pour une ligne continue malgré le dédoublement : chaque série porte aussi le point de
            // la barre précédente si sa couleur y était → recouvrement d'1 barre aux transitions
            // (sinon DiscontinuousLine ne relie pas les segments d'1 barre → trous).
            int c  = RsiColorAt(index);
            int cp = index >= 1 ? RsiColorAt(index - 1) : c;
            RsiNeutral[index] = (RsiShow && (c == 0 || cp == 0)) ? rsi : double.NaN;
            RsiOver[index]    = (RsiShow && (c == 1 || cp == 1)) ? rsi : double.NaN;
            RsiUnder[index]   = (RsiShow && (c == 2 || cp == 2)) ? rsi : double.NaN;
            RsiMa[index]      = (RsiShow && RsiShowMa) ? _rsiMa.Result[index] : double.NaN;

            // --- Stochastic RSI (toujours calculé ; affiché si StochShow) ---
            var (k, d) = StochRsi.Step(_rsi.Result, index, IndicLength, Smooth, Smooth, false, _stochRawMem, _kMem);
            StochK[index] = StochShow ? k : double.NaN;
            StochD[index] = StochShow ? d : double.NaN;

            // --- Divergences RSI (avec bornes OB/OS) — ligne reliant pivot précédent → pivot courant ---
            var (rTop, rBot, rBearReg, rBullReg, rBearHid, rBullHid, rPrevTopBar, rPrevBotBar) =
                Divergence.Step(_rsi.Result, Bars.HighPrices, Bars.LowPrices, index, LevelOverbought1, LevelOversold1, true, _memRsi);
            if (RsiShow && rTop && ((RsiShowDiv && rBearReg) || (RsiShowHiddenDiv && rBearHid)))
                DrawDivLine(RsiBearMark, _rsi.Result, rPrevTopBar, index - 2);
            if (RsiShow && rBot && ((RsiShowDiv && rBullReg) || (RsiShowHiddenDiv && rBullHid)))
                DrawDivLine(RsiBullMark, _rsi.Result, rPrevBotBar, index - 2);

            // --- Divergences Stoch (sans bornes) ---
            var (sTop, sBot, sBearReg, sBullReg, sBearHid, sBullHid, sPrevTopBar, sPrevBotBar) =
                Divergence.Step(_kMem, Bars.HighPrices, Bars.LowPrices, index, 0, 0, false, _memStoch);
            if (StochShow && sTop && ((StochShowDiv && sBearReg) || (StochShowHiddenDiv && sBearHid)))
                DrawDivLine(StochBearMark, _kMem, sPrevTopBar, index - 2);
            if (StochShow && sBot && ((StochShowDiv && sBullReg) || (StochShowHiddenDiv && sBullHid)))
                DrawDivLine(StochBullMark, _kMem, sPrevBotBar, index - 2);
        }
    }
}
