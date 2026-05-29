using System;
using cAlgo.API;

namespace TwoAi.Indicators.Core
{
    /// <summary>
    /// Équivalent de Pine <c>lib_bollinger</c>. Couche 1 — calculs purs, ne dessine rien.
    /// Bandes de Bollinger inner+outer avec multiplicateurs configurables, et projection
    /// forward sous l'hypothèse <c>src</c> constant.
    /// </summary>
    public static class Bollinger
    {
        /// <summary>
        /// Calcule des bandes de Bollinger inner + outer à partir d'une SMA et d'un écart-type.
        /// Détection de l'état (plate / fermeture) volontairement hors scope : déléguée à
        /// <see cref="Series.IsFlatSeries"/> / <see cref="Series.IsClosingSeries"/>.
        /// </summary>
        /// <param name="src">Série source (typiquement close).</param>
        /// <param name="index">Indice de la barre courante.</param>
        /// <param name="length">Période SMA.</param>
        /// <param name="multInner">Multiplicateur d'écart-type pour la bande interne.</param>
        /// <param name="multOuter">Multiplicateur d'écart-type pour la bande externe (≥ multInner).</param>
        public static (double basis, double innerUpper, double innerLower, double outerUpper, double outerLower)
            Bands(DataSeries src, int index, int length, double multInner, double multOuter)
        {
            if (index < length - 1)
                return (double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);

            double sum = 0.0;
            for (int i = 0; i < length; i++)
                sum += src[index - i];
            var basis = sum / length;

            double sqDiff = 0.0;
            for (int i = 0; i < length; i++)
            {
                var d = src[index - i] - basis;
                sqDiff += d * d;
            }
            var dev = Math.Sqrt(sqDiff / length);

            return (
                basis,
                basis + multInner * dev,
                basis - multInner * dev,
                basis + multOuter * dev,
                basis - multOuter * dev
            );
        }

        /// <summary>
        /// Projette les bandes de Bollinger <paramref name="barsAhead"/> barres dans le futur,
        /// sous l'hypothèse que <paramref name="src"/> reste constant à sa valeur courante.
        /// <para>Basis : projection exacte via <see cref="Series.ProjectMean"/>.</para>
        /// <para>Stdev : approximation par la valeur courante (impact marginal pour barsAhead ∈ [1, 5]).</para>
        /// </summary>
        public static (double basis, double innerUpper, double innerLower, double outerUpper, double outerLower)
            ProjectBands(DataSeries src, int index, int length, double multInner, double multOuter, int barsAhead)
        {
            var basis = Series.ProjectMean(src, index, length, barsAhead);

            // Stdev courante (même calcul que Bands)
            double sum = 0.0;
            for (int i = 0; i < length; i++)
                sum += src[index - i];
            var meanNow = sum / length;
            double sqDiff = 0.0;
            for (int i = 0; i < length; i++)
            {
                var d = src[index - i] - meanNow;
                sqDiff += d * d;
            }
            var dev = Math.Sqrt(sqDiff / length);

            return (
                basis,
                basis + multInner * dev,
                basis - multInner * dev,
                basis + multOuter * dev,
                basis - multOuter * dev
            );
        }
    }
}
