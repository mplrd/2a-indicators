using System;
using cAlgo.API;
using _2Ai.Indicators.Core;
using CoreLevels = _2Ai.Indicators.Core.Levels;
using CoreTime = _2Ai.Indicators.Core.Time;  // alias : Indicator.Time (cAlgo) masque la classe Core.Time

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

        // === Niveaux dynamiques (Supertrend / BB / MA) ===
        [Parameter("Supertrend D", DefaultValue = true, Group = "Niveaux dynamiques")]
        public bool StDEnabled { get; set; }
        [Parameter("ST D width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Niveaux dynamiques")]
        public int StDWidth { get; set; }
        [Parameter("Supertrend W", DefaultValue = true, Group = "Niveaux dynamiques")]
        public bool StWEnabled { get; set; }
        [Parameter("ST W width", DefaultValue = 3, MinValue = 1, MaxValue = 5, Group = "Niveaux dynamiques")]
        public int StWWidth { get; set; }

        [Parameter("BB Magique", DefaultValue = true, Group = "Niveaux dynamiques")]
        public bool BbmEnabled { get; set; }
        [Parameter("BB Magique width", DefaultValue = 3, MinValue = 1, MaxValue = 5, Group = "Niveaux dynamiques")]
        public int BbmWidth { get; set; }
        [Parameter("BB Classique", DefaultValue = true, Group = "Niveaux dynamiques")]
        public bool BbcEnabled { get; set; }
        [Parameter("BB Classique width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Niveaux dynamiques")]
        public int BbcWidth { get; set; }

        [Parameter("MA 50", DefaultValue = true, Group = "Niveaux dynamiques")]
        public bool Ma50Enabled { get; set; }
        [Parameter("MA 50 width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Niveaux dynamiques")]
        public int Ma50Width { get; set; }
        [Parameter("MA 200", DefaultValue = true, Group = "Niveaux dynamiques")]
        public bool Ma200Enabled { get; set; }
        [Parameter("MA 200 width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Niveaux dynamiques")]
        public int Ma200Width { get; set; }

        // === Structure de marché (IPA) ===
        [Parameter("IPA", DefaultValue = true, Group = "Structure de marché")]
        public bool IpaEnabled { get; set; }
        [Parameter("IPA width", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "Structure de marché")]
        public int IpaWidth { get; set; }
        [Parameter("Pivot IPA", DefaultValue = 3, MinValue = 1, MaxValue = 50, Group = "Paramètres globaux")]
        public int IpaPivotN { get; set; }
        [Parameter("Max IPA", DefaultValue = 25, MinValue = 1, MaxValue = 200, Group = "Paramètres globaux")]
        public int IpaMaxN { get; set; }

        [Parameter("Gaps", DefaultValue = true, Group = "Structure de marché")]
        public bool GapsEnabled { get; set; }
        [Parameter("Gaps lookback", DefaultValue = 50, MinValue = 10, MaxValue = 500, Group = "Structure de marché")]
        public int GapsLookback { get; set; }

        // === Paramètres des Sessions (heures HHMM-HHMM + TZ IANA) ===
        [Parameter("Asiatique", DefaultValue = "0800-1400", Group = "Paramètres des Sessions")]
        public string AsianSession { get; set; }
        [Parameter("Asiatique TZ", DefaultValue = "Asia/Tokyo", Group = "Paramètres des Sessions")]
        public string AsianTz { get; set; }

        [Parameter("Européenne", DefaultValue = "0900-1400", Group = "Paramètres des Sessions")]
        public string EuSession { get; set; }
        [Parameter("Européenne TZ", DefaultValue = "Europe/Paris", Group = "Paramètres des Sessions")]
        public string EuTz { get; set; }

        [Parameter("Américaine", DefaultValue = "0930-1600", Group = "Paramètres des Sessions")]
        public string UsSession { get; set; }
        [Parameter("Américaine TZ", DefaultValue = "America/New_York", Group = "Paramètres des Sessions")]
        public string UsTz { get; set; }

        [Parameter("Future open", DefaultValue = "0800-0900", Group = "Paramètres des Sessions")]
        public string FutureSession { get; set; }
        [Parameter("Future TZ", DefaultValue = "Europe/Paris", Group = "Paramètres des Sessions")]
        public string FutureTz { get; set; }

        [Parameter("Afficher les labels", DefaultValue = true, Group = "Paramètres globaux")]
        public bool ShowLabels { get; set; }

        // ============================================================
        // Couleurs + styles (quadruplet spec Pine ; defaults = couleurs/styles d'origine).
        // Noms alignés sur les usages dans Calculate. ARGB hex opaque (l'alpha est réappliqué au rendu).
        // ============================================================
        [Parameter("ATH couleur", DefaultValue = "FF8B0000", Group = "Niveaux HTF")]
        public Color AthColor { get; set; }
        [Parameter("ATH style", DefaultValue = LineStyle.Lines, Group = "Niveaux HTF")]
        public LineStyle AthStyle { get; set; }
        [Parameter("Daily couleur", DefaultValue = "FF555555", Group = "Niveaux HTF")]
        public Color DailyColor { get; set; }
        [Parameter("Daily style", DefaultValue = LineStyle.Lines, Group = "Niveaux HTF")]
        public LineStyle DailyStyle { get; set; }
        [Parameter("Weekly couleur", DefaultValue = "FF555555", Group = "Niveaux HTF")]
        public Color WeeklyColor { get; set; }
        [Parameter("Weekly style", DefaultValue = LineStyle.Solid, Group = "Niveaux HTF")]
        public LineStyle WeeklyStyle { get; set; }
        [Parameter("Monthly couleur", DefaultValue = "FF555555", Group = "Niveaux HTF")]
        public Color MonthlyColor { get; set; }
        [Parameter("Monthly style", DefaultValue = LineStyle.Solid, Group = "Niveaux HTF")]
        public LineStyle MonthlyStyle { get; set; }

        [Parameter("Asian couleur", DefaultValue = "FFFFA500", Group = "Niveaux Intraday")]
        public Color AsianColor { get; set; }
        [Parameter("Asian style", DefaultValue = LineStyle.Lines, Group = "Niveaux Intraday")]
        public LineStyle AsianStyle { get; set; }
        [Parameter("EU couleur", DefaultValue = "FF0000FF", Group = "Niveaux Intraday")]
        public Color EuColor { get; set; }
        [Parameter("EU style", DefaultValue = LineStyle.Lines, Group = "Niveaux Intraday")]
        public LineStyle EuStyle { get; set; }
        [Parameter("US couleur", DefaultValue = "FF800080", Group = "Niveaux Intraday")]
        public Color UsColor { get; set; }
        [Parameter("US style", DefaultValue = LineStyle.Lines, Group = "Niveaux Intraday")]
        public LineStyle UsStyle { get; set; }
        [Parameter("OR couleur", DefaultValue = "FF000000", Group = "Niveaux Intraday")]
        public Color OrColor { get; set; }
        [Parameter("OR style", DefaultValue = LineStyle.Dots, Group = "Niveaux Intraday")]
        public LineStyle OrStyle { get; set; }
        [Parameter("Open Future couleur", DefaultValue = "FF555555", Group = "Niveaux Intraday")]
        public Color OpenFutureColor { get; set; }
        [Parameter("Open Future style", DefaultValue = LineStyle.Dots, Group = "Niveaux Intraday")]
        public LineStyle OpenFutureStyle { get; set; }
        [Parameter("Open Future width", DefaultValue = 1, MinValue = 1, MaxValue = 5, Group = "Niveaux Intraday")]
        public int OpenFutureWidth { get; set; }
        [Parameter("Open EU couleur", DefaultValue = "FF0000FF", Group = "Niveaux Intraday")]
        public Color OpenEuColor { get; set; }
        [Parameter("Open EU style", DefaultValue = LineStyle.Dots, Group = "Niveaux Intraday")]
        public LineStyle OpenEuStyle { get; set; }
        [Parameter("Open EU width", DefaultValue = 1, MinValue = 1, MaxValue = 5, Group = "Niveaux Intraday")]
        public int OpenEuWidth { get; set; }
        [Parameter("Open US couleur", DefaultValue = "FF800080", Group = "Niveaux Intraday")]
        public Color OpenUsColor { get; set; }
        [Parameter("Open US style", DefaultValue = LineStyle.Dots, Group = "Niveaux Intraday")]
        public LineStyle OpenUsStyle { get; set; }
        [Parameter("Open US width", DefaultValue = 1, MinValue = 1, MaxValue = 5, Group = "Niveaux Intraday")]
        public int OpenUsWidth { get; set; }

        [Parameter("Dyn Bull (ST/BB/MA)", DefaultValue = "FF008000", Group = "Niveaux dynamiques")]
        public Color DynBull { get; set; }
        [Parameter("Dyn Bear (ST/BB/MA)", DefaultValue = "FFFF0000", Group = "Niveaux dynamiques")]
        public Color DynBear { get; set; }
        [Parameter("ST D style", DefaultValue = LineStyle.Solid, Group = "Niveaux dynamiques")]
        public LineStyle StDStyle { get; set; }
        [Parameter("ST W style", DefaultValue = LineStyle.Solid, Group = "Niveaux dynamiques")]
        public LineStyle StWStyle { get; set; }
        [Parameter("BB Magique style", DefaultValue = LineStyle.Solid, Group = "Niveaux dynamiques")]
        public LineStyle BbmStyle { get; set; }
        [Parameter("BB Classique style", DefaultValue = LineStyle.Solid, Group = "Niveaux dynamiques")]
        public LineStyle BbcStyle { get; set; }
        [Parameter("MA 50 style", DefaultValue = LineStyle.Solid, Group = "Niveaux dynamiques")]
        public LineStyle Ma50Style { get; set; }
        [Parameter("MA 200 style", DefaultValue = LineStyle.Solid, Group = "Niveaux dynamiques")]
        public LineStyle Ma200Style { get; set; }

        [Parameter("IPA Bull", DefaultValue = "FF0000FF", Group = "Structure de marché")]
        public Color IpaBull { get; set; }
        [Parameter("IPA Bear", DefaultValue = "FFFFA500", Group = "Structure de marché")]
        public Color IpaBear { get; set; }
        [Parameter("IPA style", DefaultValue = LineStyle.Dots, Group = "Structure de marché")]
        public LineStyle IpaStyle { get; set; }
        [Parameter("Gaps couleur", DefaultValue = "FFCCCCCC", Group = "Structure de marché")]
        public Color GapsColor { get; set; }

        // ============================================================
        // Couleurs / styles (params — quadruplet spec ; cf. fin de classe pour les Color/LineStyle)
        // ============================================================
        private const string ChartTz = "Europe/Paris";
        // Defaults projet (hardcodés comme dans Layout) : ST ATR/factor, BB longueurs/mult, flat.
        private const int    StAtrPeriod = 10;
        private const double StFactor    = 3.0;
        private const int    BbmLength = 160; private const double BbmMultInner = 2.5, BbmMultOuter = 2.8;
        private const int    BbcLength = 20;  private const double BbcMultInner = 2.0, BbcMultOuter = 2.5;
        private const int    FlatPeriod = 2;  private const double FlatThreshold = 0.015;

        // ============================================================
        // État interne
        // ============================================================
        private Bars _daily, _weekly, _monthly, _h1, _h4;
        private AllTimeHigh _ath;
        private SessionRange _asian, _eu, _us;
        private SessionOpen _asianOpen, _euOpen, _usOpen, _futureOpen;
        private OpenRange _or;
        // ATR (Supertrend) et MA sur barres HTF.
        private cAlgo.API.Indicators.AverageTrueRange _atrD, _atrW;
        private cAlgo.API.Indicators.SimpleMovingAverage _ma50D, _ma50W, _ma200D, _ma200W;
        // Structure de marché (IPA) + Gaps.
        private MarketStructure _structure;
        private int _ipaPrevCount;
        private GapTracker _gapTracker;
        private int _gapPrevCount;
        private double _barSec;  // durée d'une barre (s), pour décaler les labels HTF

        protected override void Initialize()
        {
            _daily   = MarketData.GetBars(TimeFrame.Daily);
            _weekly  = MarketData.GetBars(TimeFrame.Weekly);
            _monthly = MarketData.GetBars(TimeFrame.Monthly);
            _h1      = MarketData.GetBars(TimeFrame.Hour);
            _h4      = MarketData.GetBars(TimeFrame.Hour4);
            _ath     = new AllTimeHigh();

            // ATR (Wilder) + MA sur barres HTF pour Supertrend / MA dynamiques.
            _atrD = Indicators.AverageTrueRange(_daily,  StAtrPeriod, MovingAverageType.WilderSmoothing);
            _atrW = Indicators.AverageTrueRange(_weekly, StAtrPeriod, MovingAverageType.WilderSmoothing);
            _ma50D  = Indicators.SimpleMovingAverage(_daily.ClosePrices,  50);
            _ma50W  = Indicators.SimpleMovingAverage(_weekly.ClosePrices, 50);
            _ma200D = Indicators.SimpleMovingAverage(_daily.ClosePrices,  200);
            _ma200W = Indicators.SimpleMovingAverage(_weekly.ClosePrices, 200);

            _structure = new MarketStructure(IpaPivotN, IpaMaxN);
            _gapTracker = new GapTracker(GapsLookback);

            _asian = new SessionRange(AsianSession, AsianTz, ChartTz);
            _eu    = new SessionRange(EuSession,    EuTz,    ChartTz);
            _us    = new SessionRange(UsSession,    UsTz,    ChartTz);

            _asianOpen  = new SessionOpen(AsianSession,  AsianTz,  ChartTz);
            _euOpen     = new SessionOpen(EuSession,     EuTz,     ChartTz);
            _usOpen     = new SessionOpen(UsSession,     UsTz,     ChartTz);
            _futureOpen = new SessionOpen(FutureSession, FutureTz, ChartTz);

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
            _structure.Update(Bars.HighPrices, Bars.LowPrices, Bars.ClosePrices, index);
            _gapTracker.Update(Bars, _daily, index);

            if (!IsLastBar) return;

            var now = Bars.OpenTimes[index];
            double tfSec = CoreTime.BarSpanSeconds(Bars.OpenTimes, index, 10, 86400);

            // Filtres TF : un niveau s'affiche si la TF courante <= période du niveau.
            bool showDaily   = tfSec <= 86400;       // <= D
            bool showWeekly  = tfSec <= 604800;      // <= W
            bool showMonthly = tfSec <= 2629800;     // <= M (mois moyen)
            bool showIntraday = tfSec < 3600;        // < H1 (sessions / opens / open range)
            // Dynamiques : strict < TF cible (on ne double pas avec Layout sur l'UT propre).
            bool showSTD = tfSec < 86400, showSTW = tfSec < 604800;
            bool showMaD = tfSec < 86400, showMaW = tfSec < 604800;
            bool showBBmH1 = tfSec < 3600, showBBmH4 = tfSec < 14400, showBBmD = tfSec < 86400, showBBmW = tfSec < 604800;
            bool showBBcH4 = tfSec < 14400, showBBcD = tfSec < 86400, showBBcW = tfSec < 604800;

            // Fin "extend.right" : on prolonge loin dans le futur (cAlgo n'a pas d'extension
            // droite native sur un segment ancré à gauche). ~200 barres suffit à dépasser le bord.
            _barSec = tfSec;
            var futureEnd = now.AddSeconds(_barSec * 200);  // gaps / IPA (sans label) : loin à droite
            var rightEnd = now.AddSeconds(_barSec * 5);     // niveaux à label : courante + 5 barres
            var chartStart = Bars.OpenTimes[0];             // ancrage gauche des dynamiques

            // ATH + previous : de la bougie d'origine → courante +5, label au bout.
            DrawHtf("ATH", AthEnabled, _ath.Value, _ath.Time ?? now, rightEnd, AthColor, AthWidth, AthStyle, "ATH", true);

            // PDH / PDL.
            var (pdH, pdL) = CoreLevels.PreviousPeriodHL(_daily, now);
            var pdStart = CoreLevels.PreviousPeriodStartUtc(_daily, now) ?? now;
            DrawHtf("PDH", DailyEnabled && showDaily, pdH, pdStart, rightEnd, DailyColor, DailyWidth, DailyStyle, "PDH", true);
            DrawHtf("PDL", DailyEnabled && showDaily, pdL, pdStart, rightEnd, DailyColor, DailyWidth, DailyStyle, "PDL", false);

            // PWH / PWL.
            var (pwH, pwL) = CoreLevels.PreviousPeriodHL(_weekly, now);
            var pwStart = CoreLevels.PreviousPeriodStartUtc(_weekly, now) ?? now;
            DrawHtf("PWH", WeeklyEnabled && showWeekly, pwH, pwStart, rightEnd, WeeklyColor, WeeklyWidth, WeeklyStyle, "PWH", true);
            DrawHtf("PWL", WeeklyEnabled && showWeekly, pwL, pwStart, rightEnd, WeeklyColor, WeeklyWidth, WeeklyStyle, "PWL", false);

            // PMH / PML.
            var (pmH, pmL) = CoreLevels.PreviousPeriodHL(_monthly, now);
            var pmStart = CoreLevels.PreviousPeriodStartUtc(_monthly, now) ?? now;
            DrawHtf("PMH", MonthlyEnabled && showMonthly, pmH, pmStart, rightEnd, MonthlyColor, MonthlyWidth, MonthlyStyle, "PMH", true);
            DrawHtf("PML", MonthlyEnabled && showMonthly, pmL, pmStart, rightEnd, MonthlyColor, MonthlyWidth, MonthlyStyle, "PML", false);

            // --- Intraday (strict < H1) ---
            // Sessions H/L : bornées à [start, end] (figées dès le début de session côté tracker).
            DrawSession("AsianH", AsianEnabled && showIntraday && _asian.SeenToday, _asian.High, _asian.StartUtc, _asian.EndUtc, AsianColor, AsianWidth, AsianStyle, "Asian H");
            DrawSession("AsianL", AsianEnabled && showIntraday && _asian.SeenToday, _asian.Low,  _asian.StartUtc, _asian.EndUtc, AsianColor, AsianWidth, AsianStyle, "Asian L");
            DrawSession("EuH",    EuEnabled    && showIntraday && _eu.SeenToday,    _eu.High,    _eu.StartUtc,    _eu.EndUtc,    EuColor,    EuWidth,    EuStyle, "EU H");
            DrawSession("EuL",    EuEnabled    && showIntraday && _eu.SeenToday,    _eu.Low,     _eu.StartUtc,    _eu.EndUtc,    EuColor,    EuWidth,    EuStyle, "EU L");
            DrawSession("UsH",    UsEnabled    && showIntraday && _us.SeenToday,    _us.High,    _us.StartUtc,    _us.EndUtc,    UsColor,    UsWidth,    UsStyle, "US H");
            DrawSession("UsL",    UsEnabled    && showIntraday && _us.SeenToday,    _us.Low,     _us.StartUtc,    _us.EndUtc,    UsColor,    UsWidth,    UsStyle, "US L");

            // Opens : ligne ongoing (start = open de session → maintenant), pointillé. Off par défaut.
            DrawSession("OpenFuture", OpenFutureEnabled && showIntraday && _futureOpen.SeenToday, _futureOpen.Price, _futureOpen.TimeUtc, rightEnd, OpenFutureColor, OpenFutureWidth, OpenFutureStyle, "Open Future");
            DrawSession("OpenEU",     OpenEuEnabled     && showIntraday && _euOpen.SeenToday,     _euOpen.Price,     _euOpen.TimeUtc,     rightEnd, OpenEuColor,     OpenEuWidth,     OpenEuStyle, "Open EU");
            DrawSession("OpenUS",     OpenUsEnabled     && showIntraday && _usOpen.SeenToday,     _usOpen.Price,     _usOpen.TimeUtc,     rightEnd, OpenUsColor,     OpenUsWidth,     OpenUsStyle, "Open US");

            // Open Range : H/L figés après 60 min, ligne ongoing toute la journée chart.
            DrawSession("OrH", OrEnabled && showIntraday && _or.StartUtc.HasValue, _or.High, _or.StartUtc, rightEnd, OrColor, OrWidth, OrStyle, "OR H");
            DrawSession("OrL", OrEnabled && showIntraday && _or.StartUtc.HasValue, _or.Low,  _or.StartUtc, rightEnd, OrColor, OrWidth, OrStyle, "OR L");

            // --- Niveaux dynamiques (Supertrend / BB / MA) — ligne horizontale dynStart→now ---
            double close = Bars.ClosePrices[index];
            void Dyn(string id, bool show, double value, Color color, int width, LineStyle style, string label)
                => DrawDynamic(id, show, value, color, width, style, label, chartStart, rightEnd);
            Color Pos(double v) => Draw.PositionColor(v, close, DynBull, DynBear);
            void Bb(string idU, string idL, bool show, Bars bars, int length, double mi, double mo, int width, LineStyle style, string lbl)
            {
                int li = bars.OpenTimes.GetIndexByTime(now);
                double oU = double.NaN, oL = double.NaN; bool dU = false, dL = false;
                if (li >= 0)
                {
                    var r = Bollinger.HtfLevels(bars.ClosePrices, li, length, mi, mo, FlatPeriod, FlatThreshold);
                    oU = r.outerUpper; oL = r.outerLower; dU = r.drawUpper; dL = r.drawLower;
                }
                Dyn(idU, show && dU, oU, Pos(oU), width, style, lbl);
                Dyn(idL, show && dL, oL, Pos(oL), width, style, lbl);
            }

            // Supertrend D/W : couleur par DIRECTION (bull/bear), pas par position.
            var (stDVal, stDDir) = Supertrend.RunLast(_daily,  _atrD.Result, StFactor, now);
            var (stWVal, stWDir) = Supertrend.RunLast(_weekly, _atrW.Result, StFactor, now);
            Dyn("StD", StDEnabled && showSTD, stDVal, stDDir > 0 ? DynBull : DynBear, StDWidth, StDStyle, "ST D");
            Dyn("StW", StWEnabled && showSTW, stWVal, stWDir > 0 ? DynBull : DynBear, StWWidth, StWStyle, "ST W");

            // MA 50 / 200 sur D/W : couleur par position.
            Dyn("Ma50D",  Ma50Enabled  && showMaD, _ma50D.Result.LastValue,  Pos(_ma50D.Result.LastValue),  Ma50Width,  Ma50Style,  "MA 50 D");
            Dyn("Ma50W",  Ma50Enabled  && showMaW, _ma50W.Result.LastValue,  Pos(_ma50W.Result.LastValue),  Ma50Width,  Ma50Style,  "MA 50 W");
            Dyn("Ma200D", Ma200Enabled && showMaD, _ma200D.Result.LastValue, Pos(_ma200D.Result.LastValue), Ma200Width, Ma200Style, "MA 200 D");
            Dyn("Ma200W", Ma200Enabled && showMaW, _ma200W.Result.LastValue, Pos(_ma200W.Result.LastValue), Ma200Width, Ma200Style, "MA 200 W");

            // BB Magique (H1/H4/D/W) & Classique (H4/D/W) : niveau si bande plate-ou-fermeture.
            Bb("BbmH1U", "BbmH1L", BbmEnabled && showBBmH1, _h1,     BbmLength, BbmMultInner, BbmMultOuter, BbmWidth, BbmStyle, "BB M H1");
            Bb("BbmH4U", "BbmH4L", BbmEnabled && showBBmH4, _h4,     BbmLength, BbmMultInner, BbmMultOuter, BbmWidth, BbmStyle, "BB M H4");
            Bb("BbmDU",  "BbmDL",  BbmEnabled && showBBmD,  _daily,  BbmLength, BbmMultInner, BbmMultOuter, BbmWidth, BbmStyle, "BB M D");
            Bb("BbmWU",  "BbmWL",  BbmEnabled && showBBmW,  _weekly, BbmLength, BbmMultInner, BbmMultOuter, BbmWidth, BbmStyle, "BB M W");
            Bb("BbcH4U", "BbcH4L", BbcEnabled && showBBcH4, _h4,     BbcLength, BbcMultInner, BbcMultOuter, BbcWidth, BbcStyle, "BB H4");
            Bb("BbcDU",  "BbcDL",  BbcEnabled && showBBcD,  _daily,  BbcLength, BbcMultInner, BbcMultOuter, BbcWidth, BbcStyle, "BB D");
            Bb("BbcWU",  "BbcWL",  BbcEnabled && showBBcW,  _weekly, BbcLength, BbcMultInner, BbcMultOuter, BbcWidth, BbcStyle, "BB W");

            // --- IPA (structure de marché) : ray ancré au pivot, couleur par position vs prix.
            // Seuls les IPA cassés se dessinent. Nettoyage des objets au-delà du compte courant.
            int ipaN = _structure.Ipas.Count;
            for (int i = 0; i < ipaN; i++)
            {
                var ipa = _structure.Ipas[i];
                string nm = "Lvl_Ipa_" + i;
                if (IpaEnabled && ipa.Broken && ipa.Bar >= 0 && ipa.Bar < Bars.Count)
                {
                    var col = Draw.PositionColor(ipa.Price, close, IpaBull, IpaBear);
                    Draw.DrawLevel(Chart, nm, Bars.OpenTimes[ipa.Bar], futureEnd, ipa.Price, col, IpaWidth, IpaStyle, "", false, futureEnd);
                }
                else
                {
                    Chart.RemoveObject(nm); Chart.RemoveObject(nm + "_lbl");
                }
            }
            for (int i = ipaN; i < _ipaPrevCount; i++)
            {
                Chart.RemoveObject("Lvl_Ipa_" + i); Chart.RemoveObject("Lvl_Ipa_" + i + "_lbl");
            }
            _ipaPrevCount = ipaN;

            // --- Gaps (daily) : boxes de la barre-avant-gap jusqu'à maintenant. ---
            int gapN = _gapTracker.Gaps.Count;
            for (int i = 0; i < gapN; i++)
            {
                var g = _gapTracker.Gaps[i];
                string nm = "Gap_" + i;
                if (GapsEnabled && g.LeftBarIndex >= 0 && g.LeftBarIndex < Bars.Count)
                    Draw.DrawGapBox(Chart, nm, Bars.OpenTimes[g.LeftBarIndex], futureEnd, g.Top, g.Bottom, GapsColor, 85);
                else
                    Chart.RemoveObject(nm);
            }
            for (int i = gapN; i < _gapPrevCount; i++)
                Chart.RemoveObject("Gap_" + i);
            _gapPrevCount = gapN;
        }

        /// <summary>
        /// Helper local : dessine un niveau HTF si activé et valide, sinon le retire (NaN).
        /// Délègue à <see cref="Draw.DrawLevel"/>.
        /// </summary>
        private void DrawHtf(string id, bool enabled, double price, DateTime start, DateTime end,
            Color color, int width, LineStyle style, string label, bool _)
        {
            double v = enabled ? price : double.NaN;
            Draw.DrawLevel(Chart, "Lvl_" + id, start, end, v, color, width, style, label, ShowLabels, end);
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
            var e = end ?? Bars.OpenTimes.LastValue;
            Draw.DrawLevel(Chart, "Lvl_" + id, start ?? Bars.OpenTimes.LastValue, e,
                v, color, width, style, label, ShowLabels, e);
        }

        /// <summary>
        /// Niveau dynamique (Supertrend / BB / MA) : ligne horizontale solide de
        /// <paramref name="start"/> à <paramref name="end"/>. Désactivé ou NaN → retiré.
        /// </summary>
        private void DrawDynamic(string id, bool show, double value, Color color, int width, LineStyle style, string label,
            DateTime start, DateTime end)
        {
            double v = (show && !double.IsNaN(value)) ? value : double.NaN;
            // Étend à gauche (chartStart) jusqu'à courante +5 (end), label au bout.
            Draw.DrawLevel(Chart, "Lvl_" + id, start, end, v, color, width, style, label, ShowLabels, end);
        }
    }
}
