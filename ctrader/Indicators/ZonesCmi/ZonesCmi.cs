using System;
using cAlgo.API;
using _2Ai.Indicators.Core;
using CoreTime = _2Ai.Indicators.Core.Time;  // Indicator.Time (cAlgo) masque Core.Time

namespace _2Ai.Indicators.ZonesCmi
{
    /// <summary>
    /// 2Ai Zones CMI (test single-TF) — portage de <c>tradingview/zones-CMI.pine</c>. Valide la
    /// state machine de <see cref="CmiZones"/> sur l'UT du chart : détection CMI (<see cref="Signal"/>)
    /// + validation 3 bougies parallèles + construction par lookback + anti-chevauchement intra-TF
    /// (<see cref="CmiZoneOps.IntraOverlapPass"/>) + expiration par période d'intérêt
    /// (<see cref="InterestPeriods"/>). Pas de MTF ici (isole le cœur logique avant <c>2Ai Zones MTF</c>).
    /// Toute la logique vit dans Core ; l'indicateur orchestre update + rendu (boxes via <see cref="Draw"/>).
    /// <para>Limite cAlgo (cf. Layout/ZonesSd) : pas d'input couleur/style → enable+bordure en
    /// [Parameter], couleur (teal) et style (solid) hardcodés au défaut spec. TZ chart non exposée
    /// par cAlgo → hardcodée (cf. Levels), sert au calcul des bornes d'intérêt.</para>
    /// </summary>
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None, AutoRescale = false)]
    public class ZonesCmi : Indicator
    {
        private const string ChartTz = "Europe/Paris";
        private const int FillAlpha = 85, BorderAlpha = 40;
        private static readonly Color ZoneColor = Color.Teal;

        [Parameter("Zones", DefaultValue = true, Group = "Zones CMI")]
        public bool Enabled { get; set; }
        [Parameter("Bordure", DefaultValue = 1, MinValue = 0, MaxValue = 4, Group = "Zones CMI")]
        public int BorderWidth { get; set; }

        [Parameter("Drawing CMI Zone Lookback", DefaultValue = 3, MinValue = 1, MaxValue = 5, Group = "Paramètres globaux")]
        public int Lookback { get; set; }
        [Parameter("Mode Pending (sinon Replacement)", DefaultValue = true, Group = "Paramètres globaux")]
        public bool PendingMode { get; set; }
        [Parameter("Max zones (FIFO)", DefaultValue = 100, MinValue = 1, MaxValue = 500, Group = "Paramètres globaux")]
        public int MaxZones { get; set; }

        private CmiZones _cmi;
        private InterestPeriods _interest;
        private int _prevCount;

        protected override void Initialize()
        {
            _cmi = new CmiZones();
            _interest = new InterestPeriods();
        }

        public override void Calculate(int index)
        {
            double barSec = CoreTime.BarSpanSeconds(Bars.OpenTimes, index, 10, 86400);
            int tfMinutes = CoreTime.SnapTfMinutes(barSec);  // TF nominal (robuste DST), pas le span ±1 h
            DateTime barTime = Bars.OpenTimes[index];

            _interest.OnBar(barTime, ChartTz);  // suit le changement de jour (borne < M15)

            var cmi = Signal.DetectCmi(Bars.HighPrices, Bars.LowPrices, Bars.ClosePrices, Bars.OpenPrices, index);
            DateTime interestStart = _interest.StartForTf(barTime, tfMinutes, ChartTz);
            _cmi.Update(Bars, index, cmi, tfMinutes, Lookback, interestStart, MaxZones);
            CmiZoneOps.IntraOverlapPass(_cmi.Zones, PendingMode);

            if (!IsLastBar) return;

            // Zones extend.right (cf. ZonesSd) : seules les zones ACTIVE sont dessinées.
            var futureEnd = barTime.AddSeconds(barSec * 200);
            var zones = _cmi.Zones;
            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                string nm = "Cmi_" + i;
                if (Enabled && z.State == CmiState.Active)
                    Draw.DrawZoneBox(Chart, nm, z.LeftTime, futureEnd, z.Top, z.Bottom, ZoneColor, BorderWidth, LineStyle.Solid, FillAlpha, BorderAlpha);
                else { Chart.RemoveObject(nm + "_f"); Chart.RemoveObject(nm + "_b"); }
            }
            for (int i = zones.Count; i < _prevCount; i++) { Chart.RemoveObject("Cmi_" + i + "_f"); Chart.RemoveObject("Cmi_" + i + "_b"); }
            _prevCount = zones.Count;
        }
    }
}
