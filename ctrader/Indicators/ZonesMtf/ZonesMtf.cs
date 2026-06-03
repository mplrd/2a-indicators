using System;
using System.Collections.Generic;
using cAlgo.API;
using _2Ai.Indicators.Core;
using CoreTime = _2Ai.Indicators.Core.Time;  // Indicator.Time (cAlgo) masque Core.Time

namespace _2Ai.Indicators.ZonesMtf
{
    /// <summary>
    /// 2Ai Zones MTF — portage de <c>tradingview/zones-MTF.pine</c>. CMI Zones multi-timeframe
    /// (M / W / D / H4 / H1 / M15 / M5) : une <see cref="CmiZones"/> par TF, anti-chevauchement
    /// intra-TF (<see cref="CmiZoneOps.IntraOverlapPass"/>) et cross-TF descendant
    /// (<see cref="CmiZoneOps.CrossTfOverlapPass"/>, priorité M&gt;W&gt;D&gt;H4&gt;H1&gt;M15&gt;M5),
    /// obsolescence par période d'intérêt (<see cref="InterestPeriods"/>).
    ///
    /// <para>cAlgo vs Pine : pas de <c>request.security</c> ni ring buffer (<c>tickMtf</c>) — on lit
    /// directement les barres HTF via <see cref="MarketData"/>.GetBars et on ticke la state machine
    /// sur chaque barre HTF CLÔTURÉE (anti-repaint = on exclut la barre HTF en formation, équiv Pine
    /// <c>close[1] + lookahead_on</c>). Rattrapage one-shot à la dernière barre (état déterministe),
    /// borné par la période d'intérêt (on ne construit pas de zones qu'on expirerait). Filtre
    /// d'affichage Pine <c>chartMin ≤ tfMinutes</c> → on ne traite même pas les TF &lt; UT chart.</para>
    ///
    /// <para>Limite cAlgo (cf. Layout) : pas d'input couleur/style → enable+bordure en [Parameter],
    /// couleurs/styles hardcodés aux defaults spec. TZ chart hardcodée (cf. Levels/ZonesCmi).</para>
    /// </summary>
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None, AutoRescale = false)]
    public class ZonesMtf : Indicator
    {
        private const string ChartTz = "Europe/Paris";
        private const int FillAlpha = 85, BorderAlpha = 40;

        // --- Inputs par TF : quadruplet [Enabled][Couleur][Style][Épaisseur] (spec Pine). ---
        [Parameter("Monthly", DefaultValue = true, Group = "Monthly")]
        public bool MEnabled { get; set; }
        [Parameter("Monthly couleur", DefaultValue = "FF808000", Group = "Monthly")]  // olive
        public Color MColor { get; set; }
        [Parameter("Monthly style", DefaultValue = LineStyle.Solid, Group = "Monthly")]
        public LineStyle MStyle { get; set; }
        [Parameter("Monthly bordure", DefaultValue = 1, MinValue = 0, MaxValue = 4, Group = "Monthly")]
        public int MWidth { get; set; }

        [Parameter("Weekly", DefaultValue = true, Group = "Weekly")]
        public bool WEnabled { get; set; }
        [Parameter("Weekly couleur", DefaultValue = "FF008080", Group = "Weekly")]  // teal
        public Color WColor { get; set; }
        [Parameter("Weekly style", DefaultValue = LineStyle.Solid, Group = "Weekly")]
        public LineStyle WStyle { get; set; }
        [Parameter("Weekly bordure", DefaultValue = 1, MinValue = 0, MaxValue = 4, Group = "Weekly")]
        public int WWidth { get; set; }

        [Parameter("Daily", DefaultValue = true, Group = "Daily")]
        public bool DEnabled { get; set; }
        [Parameter("Daily couleur", DefaultValue = "FF0000FF", Group = "Daily")]  // blue
        public Color DColor { get; set; }
        [Parameter("Daily style", DefaultValue = LineStyle.Solid, Group = "Daily")]
        public LineStyle DStyle { get; set; }
        [Parameter("Daily bordure", DefaultValue = 1, MinValue = 0, MaxValue = 4, Group = "Daily")]
        public int DWidth { get; set; }

        [Parameter("H4", DefaultValue = true, Group = "H4")]
        public bool H4Enabled { get; set; }
        [Parameter("H4 couleur", DefaultValue = "FF5B9CF6", Group = "H4")]
        public Color H4Color { get; set; }
        [Parameter("H4 style", DefaultValue = LineStyle.Lines, Group = "H4")]  // dashed
        public LineStyle H4Style { get; set; }
        [Parameter("H4 bordure", DefaultValue = 1, MinValue = 0, MaxValue = 4, Group = "H4")]
        public int H4Width { get; set; }

        [Parameter("H1", DefaultValue = false, Group = "H1")]
        public bool H1Enabled { get; set; }
        [Parameter("H1 couleur", DefaultValue = "FFF48FB1", Group = "H1")]
        public Color H1Color { get; set; }
        [Parameter("H1 style", DefaultValue = LineStyle.Dots, Group = "H1")]  // dotted
        public LineStyle H1Style { get; set; }
        [Parameter("H1 bordure", DefaultValue = 1, MinValue = 0, MaxValue = 4, Group = "H1")]
        public int H1Width { get; set; }

        [Parameter("M15", DefaultValue = false, Group = "M15")]
        public bool M15Enabled { get; set; }
        [Parameter("M15 couleur", DefaultValue = "FFFCB9C8", Group = "M15")]
        public Color M15Color { get; set; }
        [Parameter("M15 style", DefaultValue = LineStyle.Dots, Group = "M15")]  // dotted
        public LineStyle M15Style { get; set; }
        [Parameter("M15 bordure", DefaultValue = 0, MinValue = 0, MaxValue = 4, Group = "M15")]
        public int M15Width { get; set; }

        [Parameter("M5", DefaultValue = false, Group = "M5")]
        public bool M5Enabled { get; set; }
        [Parameter("M5 couleur", DefaultValue = "FFFFDCE6", Group = "M5")]
        public Color M5Color { get; set; }
        [Parameter("M5 style", DefaultValue = LineStyle.Dots, Group = "M5")]  // dotted
        public LineStyle M5Style { get; set; }
        [Parameter("M5 bordure", DefaultValue = 0, MinValue = 0, MaxValue = 4, Group = "M5")]
        public int M5Width { get; set; }

        [Parameter("Drawing CMI Zone Lookback", DefaultValue = 3, MinValue = 1, MaxValue = 5, Group = "Paramètres globaux")]
        public int Lookback { get; set; }
        [Parameter("Mode Pending intra-TF (sinon Replacement)", DefaultValue = true, Group = "Paramètres globaux")]
        public bool IntraPendingMode { get; set; }
        [Parameter("Mode Pending cross-TF (sinon Autorisé)", DefaultValue = true, Group = "Paramètres globaux")]
        public bool CrossTfPending { get; set; }
        [Parameter("Max zones par TF (FIFO)", DefaultValue = 100, MinValue = 1, MaxValue = 500, Group = "Paramètres globaux")]
        public int MaxZones { get; set; }

        // Descripteur d'une TF : bars HTF, state machine, état de rattrapage, style de rendu.
        private class TfSlot
        {
            public string Name;
            public TimeFrame Tf;
            public int Minutes;
            public Bars Bars;
            public CmiZones Zones;
            public int LastHtf = -1;     // dernière barre HTF close déjà tickée (rattrapage incrémental)
            public int PrevCount;        // nb d'objets dessinés au tour précédent (cleanup)
            public bool Enabled;
            public int Width;
            public Color Color;
            public LineStyle Style;
        }

        private TfSlot[] _slots;          // ordre DESCENDANT M→W→D→H4→H1→M15→M5 (priorité cross-TF)
        private InterestPeriods _interest;

        protected override void Initialize()
        {
            _interest = new InterestPeriods();
            _slots = new[]
            {
                Slot("M",   TimeFrame.Monthly,  43200, MEnabled,   MWidth,   MColor,   MStyle),
                Slot("W",   TimeFrame.Weekly,   10080, WEnabled,   WWidth,   WColor,   WStyle),
                Slot("D",   TimeFrame.Daily,    1440,  DEnabled,   DWidth,   DColor,   DStyle),
                Slot("H4",  TimeFrame.Hour4,    240,   H4Enabled,  H4Width,  H4Color,  H4Style),
                Slot("H1",  TimeFrame.Hour,     60,    H1Enabled,  H1Width,  H1Color,  H1Style),
                Slot("M15", TimeFrame.Minute15, 15,    M15Enabled, M15Width, M15Color, M15Style),
                Slot("M5",  TimeFrame.Minute5,  5,     M5Enabled,  M5Width,  M5Color,  M5Style),
            };
        }

        private TfSlot Slot(string name, TimeFrame tf, int minutes, bool enabled, int width, Color color, LineStyle style)
            => new TfSlot
            {
                Name = name, Tf = tf, Minutes = minutes, Bars = MarketData.GetBars(tf),
                Zones = new CmiZones(), Enabled = enabled, Width = width, Color = color, Style = style,
            };

        public override void Calculate(int index)
        {
            // Suivi du changement de jour chart (borne d'intérêt < M15, contexte chart comme Pine).
            _interest.OnBar(Bars.OpenTimes[index], ChartTz);

            if (!IsLastBar) return;

            var now = Bars.OpenTimes[index];
            double barSec = CoreTime.BarSpanSeconds(Bars.OpenTimes, index, 10, 86400);
            int chartMin = CoreTime.SnapTfMinutes(barSec);

            // 1. Rattrapage de la state machine sur chaque TF affichable (TF zone >= UT chart).
            foreach (var s in _slots)
            {
                if (s.Minutes < chartMin) continue;  // TF < chart → masquée (Pine chartMin <= tfMinutes)

                DateTime interestStart = _interest.StartForTf(now, s.Minutes, ChartTz);

                // Anti-repaint : barre HTF en formation = celle qui contient `now` → on s'arrête à la
                // précédente (dernière close), équiv Pine close[1] + lookahead_on.
                int forming = s.Bars.OpenTimes.GetIndexByTime(now);
                int lastClosed = forming - 1;
                if (lastClosed < 0) continue;

                // Borne le rattrapage à la période d'intérêt : inutile de construire des zones plus
                // vieilles (elles seraient expirées). Recul de (lookback + 4) pour capter la
                // validation 3 bougies + la fenêtre lookback d'une zone juste à l'intérieur.
                int from = s.LastHtf + 1;
                if (interestStart > DateTime.MinValue)
                {
                    int isi = s.Bars.OpenTimes.GetIndexByTime(interestStart);
                    if (isi > 0) from = Math.Max(from, isi - (Lookback + 4));
                }
                if (from < 0) from = 0;

                for (int hi = from; hi <= lastClosed; hi++)
                {
                    var cmi = Signal.DetectCmi(s.Bars.HighPrices, s.Bars.LowPrices, s.Bars.ClosePrices, s.Bars.OpenPrices, hi);
                    s.Zones.Update(s.Bars, hi, cmi, s.Minutes, Lookback, interestStart, MaxZones);
                }
                s.LastHtf = Math.Max(s.LastHtf, lastClosed);
            }

            // 2. Anti-chevauchement intra-TF (idempotent, recalculé chaque tick).
            foreach (var s in _slots)
                if (s.Minutes >= chartMin)
                    CmiZoneOps.IntraOverlapPass(s.Zones.Zones, IntraPendingMode);

            // 3. Cross-TF descendant : chaque TF est bloquée par les TF supérieures (indices < i,
            //    liste ordonnée M→…→M5). Réactivation auto au tour suivant (idempotent + intra).
            if (CrossTfPending)
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].Minutes < chartMin) continue;
                    for (int j = 0; j < i; j++)
                    {
                        if (_slots[j].Minutes < chartMin) continue;
                        CmiZoneOps.CrossTfOverlapPass(_slots[i].Zones.Zones, _slots[j].Zones.Zones);
                    }
                }

            // 4. Rendu : zones ACTIVE des TF affichables, extend.right (cf. ZonesSd).
            var futureEnd = now.AddSeconds(barSec * 200);
            foreach (var s in _slots)
            {
                var zones = s.Zones.Zones;
                bool show = s.Enabled && s.Minutes >= chartMin;
                int drawn = show ? zones.Count : 0;
                for (int i = 0; i < drawn; i++)
                {
                    var z = zones[i];
                    string nm = "Mtf_" + s.Name + "_" + i;
                    if (z.State == CmiState.Active)
                        Draw.DrawZoneBox(Chart, nm, z.LeftTime, futureEnd, z.Top, z.Bottom, s.Color, s.Width, s.Style, FillAlpha, BorderAlpha);
                    else { Chart.RemoveObject(nm + "_f"); Chart.RemoveObject(nm + "_b"); }
                }
                for (int i = drawn; i < s.PrevCount; i++)
                {
                    Chart.RemoveObject("Mtf_" + s.Name + "_" + i + "_f");
                    Chart.RemoveObject("Mtf_" + s.Name + "_" + i + "_b");
                }
                s.PrevCount = drawn;
            }
        }
    }
}
