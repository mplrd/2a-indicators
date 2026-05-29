using cAlgo.API;

namespace TwoAi.Indicators.Core
{
    /// <summary>
    /// Équivalent de Pine <c>lib_supertrend</c>. Couche 1 — ne dessine rien.
    /// Le Supertrend a un état recursif (ligne courante dépend de la précédente), donc le helper
    /// prend en paramètre deux <c>IndicatorDataSeries</c> qui mémorisent la ligne et la direction.
    /// L'indicateur consommateur les crée via <c>CreateDataSeries()</c> dans <c>Initialize()</c>.
    /// <para>L'ATR est passé pré-calculé : l'indicateur utilise <c>Indicators.AverageTrueRange(...)</c>
    /// (built-in cAlgo, Wilder smoothing) plutôt que ré-implémenter la récursion ici.</para>
    /// </summary>
    public static class Supertrend
    {
        /// <summary>
        /// Tick une étape Supertrend pour la barre <paramref name="index"/>.
        /// Lit l'état précédent dans <paramref name="lineMemory"/>[index-1] / <paramref name="directionMemory"/>[index-1],
        /// écrit le nouvel état à l'index courant, retourne <c>(line, direction)</c> pour usage immédiat.
        /// </summary>
        /// <param name="high">Série high.</param>
        /// <param name="low">Série low.</param>
        /// <param name="close">Série close.</param>
        /// <param name="atr">Série ATR pré-calculée (typiquement <c>Indicators.AverageTrueRange(period, MovingAverageType.Wilder).Result</c>).</param>
        /// <param name="index">Indice de la barre courante.</param>
        /// <param name="factor">Multiplicateur du Supertrend (typiquement 3.0).</param>
        /// <param name="lineMemory">Mémoire de la ligne Supertrend (writable).</param>
        /// <param name="directionMemory">Mémoire de la direction +1 (bull) / −1 (bear).</param>
        /// <returns>Tuple <c>(line, direction)</c> pour la barre courante.</returns>
        public static (double line, int direction) Calculate(
            DataSeries high, DataSeries low, DataSeries close, DataSeries atr,
            int index, double factor,
            IndicatorDataSeries lineMemory, IndicatorDataSeries directionMemory)
        {
            if (index < 1 || double.IsNaN(atr[index]))
            {
                lineMemory[index] = double.NaN;
                directionMemory[index] = 0;
                return (double.NaN, 0);
            }

            var hl2 = (high[index] + low[index]) / 2.0;
            var upperBand = hl2 + factor * atr[index];
            var lowerBand = hl2 - factor * atr[index];

            var prevLine = lineMemory[index - 1];
            var prevDir = (int)directionMemory[index - 1];
            var prevClose = close[index - 1];

            // Adjust bands selon le state précédent (formule standard ST)
            double finalUpper, finalLower;
            if (double.IsNaN(prevLine))
            {
                finalUpper = upperBand;
                finalLower = lowerBand;
            }
            else
            {
                finalUpper = (upperBand < prevLine || prevClose > prevLine) ? upperBand : prevLine;
                finalLower = (lowerBand > prevLine || prevClose < prevLine) ? lowerBand : prevLine;
            }

            // Direction logic
            int direction;
            double line;
            if (double.IsNaN(prevLine) || prevDir == 0)
            {
                direction = 1;
                line = finalLower;
            }
            else if (prevDir == 1 && close[index] < finalLower)
            {
                direction = -1;
                line = finalUpper;
            }
            else if (prevDir == -1 && close[index] > finalUpper)
            {
                direction = 1;
                line = finalLower;
            }
            else
            {
                direction = prevDir;
                line = prevDir == 1 ? finalLower : finalUpper;
            }

            lineMemory[index] = line;
            directionMemory[index] = direction;
            return (line, direction);
        }
    }
}
