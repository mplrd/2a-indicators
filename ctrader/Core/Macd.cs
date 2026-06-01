using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>
    /// Mémoire (indexée par barre) du <see cref="Macd"/> zero-lag. L'indicateur la crée dans
    /// <c>Initialize()</c> via <c>CreateDataSeries()</c> et la passe à <see cref="Macd.Step"/>.
    /// L'état vit dans des <c>IndicatorDataSeries</c> (pas des champs scalaires) → recalcul
    /// idempotent quand cAlgo rappelle <c>Calculate</c> à chaque tick sur la dernière barre.
    /// </summary>
    public class MacdMemory
    {
        public IndicatorDataSeries FastE1, FastE2, FastE3;
        public IndicatorDataSeries SlowE1, SlowE2, SlowE3;
        public IndicatorDataSeries SigE1, SigE2, SigE3;
        public IndicatorDataSeries MacdLine;
    }

    /// <summary>
    /// Équivalent de Pine <c>lib_macd</c> + <c>lib_ma.apply</c>. Couche 1, ne dessine rien.
    /// MACD "zero-lag" paramétrable (DEMA/TEMA/ZLEMA). Une étape par barre via <see cref="Step"/>,
    /// état en mémoire indexée (cf. <see cref="MacdMemory"/>).
    ///
    /// <para>Formule : fast = MA(mode, src, fast) ; slow = MA(mode, src, slow) ; macd = fast − slow ;
    /// signal = MA(mode, macd, signal) ; hist = macd − signal ; average = SMA(macd, signal).</para>
    /// </summary>
    public static class Macd
    {
        /// <summary>
        /// Tick une étape MACD zero-lag pour la barre <paramref name="index"/>.
        /// </summary>
        public static (double macd, double signal, double hist, double average) Step(
            MaMode mode, DataSeries src, int index,
            int fastLen, int slowLen, int signalLen, MacdMemory m)
        {
            double fast = ZeroLagMa(mode, src, index, fastLen, m.FastE1, m.FastE2, m.FastE3);
            double slow = ZeroLagMa(mode, src, index, slowLen, m.SlowE1, m.SlowE2, m.SlowE3);
            double macd = fast - slow;
            m.MacdLine[index] = macd;

            double signal = ZeroLagMa(mode, m.MacdLine, index, signalLen, m.SigE1, m.SigE2, m.SigE3);
            double hist = macd - signal;

            // average = SMA(macd, signalLen) : NaN tant que la fenêtre n'est pas pleine (parité ta.sma).
            double average = double.NaN;
            if (index >= signalLen - 1)
            {
                double sum = 0.0;
                for (int i = 0; i < signalLen; i++) sum += m.MacdLine[index - i];
                average = sum / signalLen;
            }

            return (macd, signal, hist, average);
        }

        // Un pas de MA zero-lag (DEMA/TEMA/ZLEMA) sur `src` à `index`, état dans e1/e2/e3.
        private static double ZeroLagMa(MaMode mode, DataSeries src, int index, int length,
            IndicatorDataSeries e1, IndicatorDataSeries e2, IndicatorDataSeries e3)
        {
            double a = 2.0 / (length + 1);
            switch (mode)
            {
                case MaMode.DEMA:
                {
                    double v1 = EmaInto(src[index], index, a, e1);
                    double v2 = EmaInto(e1[index], index, a, e2);
                    return 2.0 * v1 - v2;
                }
                case MaMode.TEMA:
                {
                    double v1 = EmaInto(src[index], index, a, e1);
                    double v2 = EmaInto(e1[index], index, a, e2);
                    double v3 = EmaInto(e2[index], index, a, e3);
                    return 3.0 * v1 - 3.0 * v2 + v3;
                }
                default: // ZLEMA = EMA(2·src − src[lag]), lag = (length−1)/2
                {
                    int lag = (length - 1) / 2;
                    double delayed = index >= lag ? src[index - lag] : src[index];
                    return EmaInto(2.0 * src[index] - delayed, index, a, e1);
                }
            }
        }

        // EMA récursive idempotente : écrit ema[index] depuis ema[index-1] (amorçage = 1re valeur).
        private static double EmaInto(double x, int index, double a, IndicatorDataSeries ema)
        {
            double prev = index >= 1 ? ema[index - 1] : double.NaN;
            ema[index] = double.IsNaN(prev) ? x : a * x + (1.0 - a) * prev;
            return ema[index];
        }
    }
}
