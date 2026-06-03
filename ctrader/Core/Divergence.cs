using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>
    /// Mémoire (indexée) du tracker de divergences : valeurs du DERNIER pivot top/bot (oscillateur
    /// et prix), reportées barre à barre. Une instance par appel logique (RSI bornée, RSI libre,
    /// Stoch…). Créée par l'indicateur via <c>CreateDataSeries()</c>.
    /// </summary>
    public class DivergenceMemory
    {
        public IndicatorDataSeries LastTopOsc, LastTopPrice, LastBotOsc, LastBotPrice;
        public IndicatorDataSeries LastTopBar, LastBotBar;  // bar index du dernier pivot (pour tracer la ligne)
    }

    /// <summary>
    /// Équivalent de Pine <c>lib_divergence</c>. Détecte les divergences régulières/cachées d'un
    /// oscillateur vs le prix (pivots fractals via <see cref="Series"/>). Couche 1, ne dessine pas.
    ///
    /// <para>Le "pivot précédent" (idiome Pine <c>valuewhen(...)[2]</c>) est porté par report
    /// barre-à-barre dans <see cref="DivergenceMemory"/> : à un nouveau pivot, on compare au pivot
    /// reporté (= précédent), puis on enregistre le pivot courant. Tout vit en mémoire indexée →
    /// idempotent sur les reticks de la dernière barre.</para>
    /// </summary>
    public static class Divergence
    {
        /// <returns>
        /// (topPivot, botPivot, bearRegular, bullRegular, bearHidden, bullHidden, prevTopBar, prevBotBar).
        /// prevTopBar/prevBotBar = bar index du pivot PRÉCÉDENT (-1 si aucun) — pour tracer la ligne de
        /// divergence du pivot précédent au pivot courant (index-2).
        /// </returns>
        public static (bool topPivot, bool botPivot, bool bearReg, bool bullReg, bool bearHid, bool bullHid, int prevTopBar, int prevBotBar) Step(
            DataSeries osc, DataSeries priceHigh, DataSeries priceLow, int index,
            double topLimit, double botLimit, bool useLimits, DivergenceMemory m)
        {
            // Valeurs du pivot PRÉCÉDENT = report à la barre d'avant.
            double prevTopOsc   = index >= 1 ? m.LastTopOsc[index - 1]   : double.NaN;
            double prevTopPrice = index >= 1 ? m.LastTopPrice[index - 1] : double.NaN;
            double prevBotOsc   = index >= 1 ? m.LastBotOsc[index - 1]   : double.NaN;
            double prevBotPrice = index >= 1 ? m.LastBotPrice[index - 1] : double.NaN;
            double prevTopBarD  = index >= 1 ? m.LastTopBar[index - 1]   : double.NaN;
            double prevBotBarD  = index >= 1 ? m.LastBotBar[index - 1]   : double.NaN;
            int prevTopBar = double.IsNaN(prevTopBarD) ? -1 : (int)prevTopBarD;
            int prevBotBar = double.IsNaN(prevBotBarD) ? -1 : (int)prevBotBarD;

            bool topCond = false, botCond = false, bearReg = false, bullReg = false, bearHid = false, bullHid = false;

            if (index >= 4)  // fractal 5 bougies sur osc[index-2]
            {
                double pivOsc = osc[index - 2];
                topCond = Series.FractalTop(osc, index) && (!useLimits || pivOsc >= topLimit);
                botCond = Series.FractalBot(osc, index) && (!useLimits || pivOsc <= botLimit);

                if (topCond && !double.IsNaN(prevTopOsc))
                {
                    double curPrice = priceHigh[index - 2];
                    bearReg = curPrice > prevTopPrice && pivOsc < prevTopOsc;  // HH prix, LH osc
                    bearHid = curPrice < prevTopPrice && pivOsc > prevTopOsc;  // LH prix, HH osc
                }
                if (botCond && !double.IsNaN(prevBotOsc))
                {
                    double curPrice = priceLow[index - 2];
                    bullReg = curPrice < prevBotPrice && pivOsc > prevBotOsc;  // LL prix, HL osc
                    bullHid = curPrice > prevBotPrice && pivOsc < prevBotOsc;  // HL prix, LL osc
                }
            }

            // Report / mise à jour de la mémoire (un pivot enregistre sa valeur, sinon on reporte).
            double pOsc = index >= 2 ? osc[index - 2] : double.NaN;
            m.LastTopOsc[index]   = topCond ? pOsc                  : prevTopOsc;
            m.LastTopPrice[index] = topCond ? priceHigh[index - 2]  : prevTopPrice;
            m.LastBotOsc[index]   = botCond ? pOsc                  : prevBotOsc;
            m.LastBotPrice[index] = botCond ? priceLow[index - 2]   : prevBotPrice;
            m.LastTopBar[index]   = topCond ? (index - 2)           : prevTopBarD;
            m.LastBotBar[index]   = botCond ? (index - 2)           : prevBotBarD;

            return (topCond, botCond, bearReg, bullReg, bearHid, bullHid, prevTopBar, prevBotBar);
        }
    }
}
