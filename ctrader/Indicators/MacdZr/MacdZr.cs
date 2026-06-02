using cAlgo.API;
using _2Ai.Indicators.Core;

namespace _2Ai.Indicators.MacdZr
{
    /// <summary>
    /// 2Ai MACD ZR — portage de <c>tradingview/macd-zr.pine</c>. Oscillateur MACD "zero-lag"
    /// paramétrable DEMA/TEMA/ZLEMA. Tout le calcul vit dans <see cref="Macd"/> (Core) ;
    /// l'indicateur n'orchestre que inputs + rendu.
    /// <para>Couleur par signe (vert hist&gt;0 / rouge sinon) : cAlgo fige la couleur d'un
    /// <c>[Output]</c>, donc histogramme et signal sont dédoublés (up/down) via le NaN-trick.</para>
    /// </summary>
    [Indicator(IsOverlay = false, AccessRights = AccessRights.None)]
    // Nuage MACD↔Signal : le cloud bicolore colore selon la série du dessus. hist>0 ⟺ MACD>Signal
    // → FirstColor (vert) ; hist<0 → SecondColor (rouge). Reproduit le fill() Pine sans toggle.
    [Cloud("MACD ZR", "Signal", FirstColor = "#4CAF4F", SecondColor = "#FF5252", Opacity = 0.35)]
    public class MacdZr : Indicator
    {
        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Fast length", DefaultValue = 12, MinValue = 1)]
        public int FastLength { get; set; }

        [Parameter("Slow length", DefaultValue = 26, MinValue = 1)]
        public int SlowLength { get; set; }

        [Parameter("Signal length", DefaultValue = 9, MinValue = 1)]
        public int SignalLength { get; set; }

        [Parameter("Méthode de lissage", DefaultValue = MaMode.DEMA)]
        public MaMode Mode { get; set; }

        // Histogramme : colonnes vertes (hist>0) / rouges (sinon) via 2 outputs mutuellement exclusifs.
        [Output("Histogram Up",   LineColor = "#4CAF4F", PlotType = PlotType.Histogram, Thickness = 4)]
        public IndicatorDataSeries HistUp { get; set; }

        [Output("Histogram Down", LineColor = "#FF5252", PlotType = PlotType.Histogram, Thickness = 4)]
        public IndicatorDataSeries HistDown { get; set; }

        [Output("MACD ZR", LineColor = "Gray", PlotType = PlotType.Line)]
        public IndicatorDataSeries MacdLine { get; set; }

        // Ligne signal : couleur par signe de l'hist → 2 outputs DiscontinuousLine (pas de pont NaN).
        [Output("Signal Up",   LineColor = "#4CAF4F", PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries SignalUp { get; set; }

        [Output("Signal Down", LineColor = "#FF5252", PlotType = PlotType.DiscontinuousLine)]
        public IndicatorDataSeries SignalDown { get; set; }

        // Signal continu (transparent) : sert uniquement d'arête au cloud (les Up/Down ont des NaN).
        [Output("Signal", LineColor = "Transparent", PlotType = PlotType.Line)]
        public IndicatorDataSeries Signal { get; set; }

        [Output("MA Line", LineColor = "#527AFF", PlotType = PlotType.Line)]
        public IndicatorDataSeries MaLine { get; set; }

        private MacdMemory _mem;

        protected override void Initialize()
        {
            _mem = new MacdMemory
            {
                FastE1 = CreateDataSeries(), FastE2 = CreateDataSeries(), FastE3 = CreateDataSeries(),
                SlowE1 = CreateDataSeries(), SlowE2 = CreateDataSeries(), SlowE3 = CreateDataSeries(),
                SigE1  = CreateDataSeries(), SigE2  = CreateDataSeries(), SigE3  = CreateDataSeries(),
                MacdLine = CreateDataSeries(),
            };
        }

        public override void Calculate(int index)
        {
            var (macd, signal, hist, average) =
                Macd.Step(Mode, Source, index, FastLength, SlowLength, SignalLength, _mem);

            bool up = hist > 0;
            HistUp[index]   = up ? hist : double.NaN;
            HistDown[index] = up ? double.NaN : hist;
            MacdLine[index] = macd;
            SignalUp[index]   = up ? signal : double.NaN;
            SignalDown[index] = up ? double.NaN : signal;
            Signal[index]   = signal;  // arête continue du cloud
            MaLine[index]   = average;
        }
    }
}
