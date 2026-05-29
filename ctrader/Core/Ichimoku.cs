using cAlgo.API;

namespace TwoAi.Indicators.Core
{
    /// <summary>
    /// Équivalent de Pine <c>lib_ichimoku</c>. Couche 1 — calcul pur, ne dessine rien.
    /// Fournit Tenkan / Kijun / Senkou A / Senkou B / Chikou pour une barre donnée.
    /// <para>NB : le décalage temporel de Senkou A/B (+kijun bars) et Chikou (−kijun bars)
    /// est appliqué au RENDU côté indicateur (offset des plots cAlgo), pas dans le calcul.</para>
    /// </summary>
    public static class Ichimoku
    {
        /// <summary>
        /// Calcule les 5 composantes Ichimoku pour la barre <paramref name="index"/>.
        /// </summary>
        /// <param name="high">Série high (typiquement Bars.HighPrices).</param>
        /// <param name="low">Série low.</param>
        /// <param name="close">Série close.</param>
        /// <param name="index">Indice de la barre courante.</param>
        /// <param name="tenkanLen">Période Tenkan (défaut 9).</param>
        /// <param name="kijunLen">Période Kijun (défaut 26).</param>
        /// <param name="senkouLen">Période Senkou B (défaut 52).</param>
        public static (double tenkan, double kijun, double senkouA, double senkouB, double chikou)
            Components(DataSeries high, DataSeries low, DataSeries close, int index,
                int tenkanLen = 9, int kijunLen = 26, int senkouLen = 52)
        {
            var tenkan = DonchianMid(high, low, index, tenkanLen);
            var kijun  = DonchianMid(high, low, index, kijunLen);
            var senkouA = (tenkan + kijun) / 2.0;
            var senkouB = DonchianMid(high, low, index, senkouLen);
            // Chikou Span = close de la barre courante (le décalage de plot −kijun bars
            // est appliqué au rendu, pas ici).
            var chikou = close[index];
            return (tenkan, kijun, senkouA, senkouB, chikou);
        }

        /// <summary>
        /// (privé) Milieu du canal de Donchian sur <paramref name="length"/> barres :
        /// (highest_high + lowest_low) / 2.
        /// </summary>
        private static double DonchianMid(DataSeries high, DataSeries low, int index, int length)
        {
            if (index < length - 1) return double.NaN;

            double hh = double.MinValue;
            double ll = double.MaxValue;
            for (int i = 0; i < length; i++)
            {
                var h = high[index - i];
                var l = low[index - i];
                if (h > hh) hh = h;
                if (l < ll) ll = l;
            }
            return (hh + ll) / 2.0;
        }
    }
}
