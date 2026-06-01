using System;
using cAlgo.API;

namespace _2Ai.Indicators.Core
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
        /// Niveaux HTF : bandes outer + flag "à dessiner" par côté = la bande est plate OU en
        /// fermeture (sur <paramref name="flatLen"/> barres, seuil <paramref name="flatThr"/> en %).
        /// Équivaut à Pine <c>bb.bHtfLevels</c>. Mêmes définitions de pente/plat/fermeture que
        /// <see cref="Series"/> (calculées ici inline car on n'a pas la bande outer sous forme de
        /// série — on évalue à <paramref name="index"/> et <c>index - flatLen</c>).
        /// </summary>
        /// <returns>(outerUpper, outerLower, drawUpper, drawLower).</returns>
        public static (double outerUpper, double outerLower, bool drawUpper, bool drawLower)
            HtfLevels(DataSeries src, int index, int length, double multInner, double multOuter, int flatLen, double flatThr)
        {
            var (_, _, _, oU, oL) = Bands(src, index, length, multInner, multOuter);
            if (index < length - 1 + flatLen || double.IsNaN(oU))
                return (oU, oL, false, false);

            var (_, _, _, oUp, oLp) = Bands(src, index - flatLen, length, multInner, multOuter);

            double slopeU = oUp == 0.0 ? double.NaN : (oU - oUp) / oUp * 100.0;
            double slopeL = oLp == 0.0 ? double.NaN : (oL - oLp) / oLp * 100.0;

            // flat = |pente| < seuil ; closing = |pente| >= seuil ET sens de fermeture
            // (bande haute : pente < 0 ; bande basse : pente > 0). Mutuellement exclusifs.
            bool drawU = !double.IsNaN(slopeU) && (Math.Abs(slopeU) < flatThr || slopeU < 0.0);
            bool drawL = !double.IsNaN(slopeL) && (Math.Abs(slopeL) < flatThr || slopeL > 0.0);
            return (oU, oL, drawU, drawL);
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
