using cAlgo.API;

namespace _2Ai.Indicators.Core
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

            var (line, direction) = Step(
                high[index], low[index], close[index], close[index - 1], atr[index],
                factor, lineMemory[index - 1], (int)directionMemory[index - 1]);

            lineMemory[index] = line;
            directionMemory[index] = direction;
            return (line, direction);
        }

        /// <summary>
        /// Une étape de la récurrence Supertrend (formule standard). Pure : à partir de l'état
        /// précédent <paramref name="prevLine"/>/<paramref name="prevDir"/>, renvoie le nouvel état.
        /// Partagée par <see cref="Calculate"/> (état en IndicatorDataSeries) et <see cref="RunLast"/>
        /// (état local sur barres HTF).
        /// </summary>
        private static (double line, int direction) Step(
            double high, double low, double close, double prevClose, double atr,
            double factor, double prevLine, int prevDir)
        {
            var hl2 = (high + low) / 2.0;
            var upperBand = hl2 + factor * atr;
            var lowerBand = hl2 - factor * atr;

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

            int direction;
            double line;
            if (double.IsNaN(prevLine) || prevDir == 0)
            {
                direction = 1;
                line = finalLower;
            }
            else if (prevDir == 1 && close < finalLower)
            {
                direction = -1;
                line = finalUpper;
            }
            else if (prevDir == -1 && close > finalUpper)
            {
                direction = 1;
                line = finalLower;
            }
            else
            {
                direction = prevDir;
                line = prevDir == 1 ? finalLower : finalUpper;
            }

            return (line, direction);
        }

        /// <summary>
        /// Déroule le Supertrend sur des barres ARBITRAIRES (typiquement HTF : Daily/Weekly via
        /// <c>MarketData.GetBars</c>) jusqu'à la barre contenant <paramref name="uptoTimeUtc"/>, et
        /// renvoie sa dernière valeur+direction. État local (pas d'IndicatorDataSeries, qui sont
        /// alignées sur le chart). Équivaut au <c>request.security(tf, st.run(...), lookahead_on)</c>
        /// Pine : on lit la valeur du Supertrend du TF supérieur à l'instant courant (niveau live).
        /// </summary>
        public static (double line, int direction) RunLast(
            Bars bars, DataSeries atr, double factor, System.DateTime uptoTimeUtc)
        {
            int last = bars.OpenTimes.GetIndexByTime(uptoTimeUtc);
            if (last < 1) return (double.NaN, 0);

            double prevLine = double.NaN;
            int prevDir = 0;
            double line = double.NaN;
            int dir = 0;

            for (int i = 1; i <= last; i++)
            {
                if (double.IsNaN(atr[i]))
                {
                    prevLine = double.NaN; prevDir = 0;
                    line = double.NaN; dir = 0;
                    continue;
                }
                (line, dir) = Step(
                    bars.HighPrices[i], bars.LowPrices[i], bars.ClosePrices[i], bars.ClosePrices[i - 1],
                    atr[i], factor, prevLine, prevDir);
                prevLine = line; prevDir = dir;
            }
            return (line, dir);
        }
    }
}
