using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>
    /// Équivalent de Pine <c>lib_ma</c>. Couche 1 — moyennes mobiles et rubans, ne dessine rien.
    /// Pour l'instant : <see cref="Ribbon"/> et <see cref="ProjectSma"/> nécessaires à Layout.
    /// Les variantes zero-lag (DEMA/TEMA/ZLEMA) et l'enum <c>MaMode</c> seront ajoutés quand
    /// le portage de MACD ZR sera attaqué.
    /// </summary>
    public static class MovingAverages
    {
        /// <summary>
        /// Ruban autour d'une SMA : <c>[basis − k·stdev , basis + k·stdev]</c>.
        /// Implémenté en délégant à <see cref="Bollinger.Bands"/> avec le même multiplicateur
        /// inner et outer — un ruban MA est mathématiquement une bande de Bollinger à multiplicateur unique.
        /// </summary>
        /// <param name="src">Série source (typiquement close).</param>
        /// <param name="index">Indice de la barre courante.</param>
        /// <param name="length">Période SMA.</param>
        /// <param name="stdFactor">Multiplicateur d'écart-type pour la demi-largeur du ruban. Défaut 0.236.</param>
        public static (double basis, double upper, double lower) Ribbon(DataSeries src, int index, int length, double stdFactor = 0.236)
        {
            var (basis, upper, lower, _, _) = Bollinger.Bands(src, index, length, stdFactor, stdFactor);
            return (basis, upper, lower);
        }

        /// <summary>
        /// Projette la SMA <paramref name="barsAhead"/> barres dans le futur (hypothèse close constant).
        /// Wrapper de cohérence : délègue à <see cref="Series.ProjectMean"/>.
        /// </summary>
        public static double ProjectSma(DataSeries src, int index, int length, int barsAhead)
            => Series.ProjectMean(src, index, length, barsAhead);
    }
}
