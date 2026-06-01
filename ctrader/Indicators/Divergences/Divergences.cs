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
        [Output("RSI Bearish Divergence", LineColor = "#AA0000", PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries RsiBearMark { get; set; }
        [Output("RSI Bullish Divergence", LineColor = "#006403", PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries RsiBullMark { get; set; }

        // Stoch K/D + marqueurs.
        [Output("Stoch K", LineColor = "#21BBF3", PlotType = PlotType.Line)]
        public IndicatorDataSeries StochK { get; set; }
        [Output("Stoch D", LineColor = "#673AB7", PlotType = PlotType.Line)]
        public IndicatorDataSeries StochD { get; set; }
        [Output("Stoch Bearish Divergence", LineColor = "#FF3535", PlotType = PlotType.Points, Thickness = 3)]
        public IndicatorDataSeries StochBearMark { get; set; }
        [Output("Stoch Bullish Divergence", LineColor = "#44B847", PlotType = PlotType.Points, Thickness = 3)]
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
        };

        public override void Calculate(int index)
        {
            double rsi = _rsi.Result[index];

            // --- RSI couleur (neutre / overbought / oversold), confirmé sur 2 barres ---
            double rsiPrev = index >= 1 ? _rsi.Result[index - 1] : double.NaN;
            bool under = (rsi < LevelOversold2 && rsiPrev <= LevelOversold2) || (rsi < LevelOversold1 && rsiPrev <= LevelOversold1);
            bool over  = (rsi > LevelOverbought2 && rsiPrev >= LevelOverbought2) || (rsi > LevelOverbought1 && rsiPrev >= LevelOverbought1);

            RsiUnder[index]   = (RsiShow && under) ? rsi : double.NaN;
            RsiOver[index]    = (RsiShow && !under && over) ? rsi : double.NaN;
            RsiNeutral[index] = (RsiShow && !under && !over) ? rsi : double.NaN;
            RsiMa[index]      = (RsiShow && RsiShowMa) ? _rsiMa.Result[index] : double.NaN;

            // --- Stochastic RSI (toujours calculé ; affiché si StochShow) ---
            var (k, d) = StochRsi.Step(_rsi.Result, index, IndicLength, Smooth, Smooth, false, _stochRawMem, _kMem);
            StochK[index] = StochShow ? k : double.NaN;
            StochD[index] = StochShow ? d : double.NaN;

            // --- Divergences RSI (avec bornes OB/OS) ---
            var (rTop, rBot, rBearReg, rBullReg, rBearHid, rBullHid) =
                Divergence.Step(_rsi.Result, Bars.HighPrices, Bars.LowPrices, index, LevelOverbought1, LevelOversold1, true, _memRsi);
            bool rBear = (RsiShowDiv && rBearReg) || (RsiShowHiddenDiv && rBearHid);
            bool rBull = (RsiShowDiv && rBullReg) || (RsiShowHiddenDiv && rBullHid);

            // --- Divergences Stoch (sans bornes) ---
            var (sTop, sBot, sBearReg, sBullReg, sBearHid, sBullHid) =
                Divergence.Step(_kMem, Bars.HighPrices, Bars.LowPrices, index, 0, 0, false, _memStoch);
            bool sBear = (StochShowDiv && sBearReg) || (StochShowHiddenDiv && sBearHid);
            bool sBull = (StochShowDiv && sBullReg) || (StochShowHiddenDiv && sBullHid);

            // Marqueurs au pivot (osc[index-2]), équiv plot offset -2 Pine.
            if (index >= 2)
            {
                RsiBearMark[index - 2]   = (RsiShow   && rTop && rBear) ? _rsi.Result[index - 2] : double.NaN;
                RsiBullMark[index - 2]   = (RsiShow   && rBot && rBull) ? _rsi.Result[index - 2] : double.NaN;
                StochBearMark[index - 2] = (StochShow && sTop && sBear) ? _kMem[index - 2] : double.NaN;
                StochBullMark[index - 2] = (StochShow && sBot && sBull) ? _kMem[index - 2] : double.NaN;
            }
        }
    }
}
