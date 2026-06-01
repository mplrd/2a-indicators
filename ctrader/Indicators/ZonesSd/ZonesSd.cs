using System;
using cAlgo.API;
using _2Ai.Indicators.Core;
using CoreTime = _2Ai.Indicators.Core.Time;  // Indicator.Time (cAlgo) masque Core.Time

namespace _2Ai.Indicators.ZonesSd
{
    /// <summary>
    /// 2Ai Zones — portage de <c>tradingview/zones-SD.pine</c>. Zones d'intérêt sur l'UT courante :
    /// Supply/Demand (via CMI) + Fair Value Gaps, avec safe mode 5★ (S/D et FVG alignés même side).
    /// Toute la logique vit dans Core (<see cref="SupplyDemand"/>, <see cref="FvgTracker"/>,
    /// <see cref="Signal"/>, <see cref="ZoneOps"/>) ; l'indicateur orchestre update + rendu (boxes).
    /// <para>Limite cAlgo (cf. Layout) : pas d'input couleur/style → enable+width en [Parameter],
    /// couleurs/styles hardcodés aux defaults spec.</para>
    /// </summary>
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None, AutoRescale = false)]
    public class ZonesSd : Indicator
    {
        private const int MaxZones = 100;
        private const int FillAlpha = 85, BorderAlpha = 40;
        private static readonly Color DemandColor  = Color.Blue;
        private static readonly Color SupplyColor   = Color.Orange;
        private static readonly Color FvgBullColor = Color.Green;
        private static readonly Color FvgBearColor = Color.Red;

        [Parameter("Demand", DefaultValue = true, Group = "Supply / Demand")]
        public bool DemandEnabled { get; set; }
        [Parameter("Demand bordure", DefaultValue = 1, MinValue = 0, MaxValue = 4, Group = "Supply / Demand")]
        public int DemandBorderWidth { get; set; }

        [Parameter("Supply", DefaultValue = true, Group = "Supply / Demand")]
        public bool SupplyEnabled { get; set; }
        [Parameter("Supply bordure", DefaultValue = 1, MinValue = 0, MaxValue = 4, Group = "Supply / Demand")]
        public int SupplyBorderWidth { get; set; }

        [Parameter("FVG Bullish", DefaultValue = true, Group = "Fair Value Gaps")]
        public bool FvgBullEnabled { get; set; }
        [Parameter("FVG Bull bordure", DefaultValue = 0, MinValue = 0, MaxValue = 4, Group = "Fair Value Gaps")]
        public int FvgBullBorderWidth { get; set; }

        [Parameter("FVG Bearish", DefaultValue = true, Group = "Fair Value Gaps")]
        public bool FvgBearEnabled { get; set; }
        [Parameter("FVG Bear bordure", DefaultValue = 0, MinValue = 0, MaxValue = 4, Group = "Fair Value Gaps")]
        public int FvgBearBorderWidth { get; set; }

        [Parameter("Safe mode (5★ uniquement)", DefaultValue = false, Group = "Paramètres globaux")]
        public bool SafeMode { get; set; }

        private SupplyDemand _sd;
        private FvgTracker _fvg;
        private int _sdPrevCount, _fvgPrevCount;

        protected override void Initialize()
        {
            _sd  = new SupplyDemand(MaxZones);
            _fvg = new FvgTracker(MaxZones);
        }

        public override void Calculate(int index)
        {
            double barSec = CoreTime.BarSpanSeconds(Bars.OpenTimes, index, 10, 86400);
            int tfMinutes = (int)Math.Round(barSec / 60.0);

            var cmi = Signal.DetectCmi(Bars.HighPrices, Bars.LowPrices, Bars.ClosePrices, Bars.OpenPrices, index);
            _sd.Update(Bars.HighPrices, Bars.LowPrices, Bars.OpenTimes, index, cmi, tfMinutes);
            _fvg.Update(Bars.HighPrices, Bars.LowPrices, Bars.OpenTimes, index, tfMinutes, barSec);

            if (!IsLastBar) return;

            var futureEnd = Bars.OpenTimes[index].AddSeconds(barSec * 200);  // zones extend.right

            // S/D : bull → Demand, bear → Supply. Safe mode : seulement si un FVG aligné (même side).
            var sd = _sd.Zones;
            for (int i = 0; i < sd.Count; i++)
            {
                var z = sd[i];
                string nm = "Sd_" + i;
                bool keep = z.IsActive && (!SafeMode || ZoneOps.FindOverlappingActiveSameSide(_fvg.Zones, z));
                bool bull = z.Side == ZoneSide.Bull;
                if (keep && DemandEnabled && bull)
                    Draw.DrawZoneBox(Chart, nm, z.LeftTime, futureEnd, z.Top, z.Bottom, DemandColor, DemandBorderWidth, LineStyle.Solid, FillAlpha, BorderAlpha);
                else if (keep && SupplyEnabled && !bull)
                    Draw.DrawZoneBox(Chart, nm, z.LeftTime, futureEnd, z.Top, z.Bottom, SupplyColor, SupplyBorderWidth, LineStyle.Solid, FillAlpha, BorderAlpha);
                else { Chart.RemoveObject(nm + "_f"); Chart.RemoveObject(nm + "_b"); }
            }
            for (int i = sd.Count; i < _sdPrevCount; i++) { Chart.RemoveObject("Sd_" + i + "_f"); Chart.RemoveObject("Sd_" + i + "_b"); }
            _sdPrevCount = sd.Count;

            // FVG : bull/bear (bordure dashed). Safe mode : seulement si un S/D aligné existe.
            var fv = _fvg.Zones;
            for (int i = 0; i < fv.Count; i++)
            {
                var z = fv[i];
                string nm = "Fvg_" + i;
                bool keep = z.IsActive && (!SafeMode || ZoneOps.FindOverlappingActiveSameSide(_sd.Zones, z));
                bool bull = z.Side == ZoneSide.Bull;
                if (keep && FvgBullEnabled && bull)
                    Draw.DrawZoneBox(Chart, nm, z.LeftTime, futureEnd, z.Top, z.Bottom, FvgBullColor, FvgBullBorderWidth, LineStyle.Lines, FillAlpha, BorderAlpha);
                else if (keep && FvgBearEnabled && !bull)
                    Draw.DrawZoneBox(Chart, nm, z.LeftTime, futureEnd, z.Top, z.Bottom, FvgBearColor, FvgBearBorderWidth, LineStyle.Lines, FillAlpha, BorderAlpha);
                else { Chart.RemoveObject(nm + "_f"); Chart.RemoveObject(nm + "_b"); }
            }
            for (int i = fv.Count; i < _fvgPrevCount; i++) { Chart.RemoveObject("Fvg_" + i + "_f"); Chart.RemoveObject("Fvg_" + i + "_b"); }
            _fvgPrevCount = fv.Count;
        }
    }
}
