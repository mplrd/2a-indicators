using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>
    /// Équivalent de Pine <c>lib_stochrsi</c>. Stochastic RSI : oscillateur stochastique appliqué
    /// au RSI. Couche 1, ne dessine pas. <see cref="Step"/> par barre, mémoire indexée (les SMA de
    /// lissage sont fenêtrées → recalcul idempotent sur les reticks de la dernière barre).
    ///
    /// <para>Le RSI est fourni par l'indicateur (built-in cAlgo <c>RelativeStrengthIndex</c>, qui
    /// gère déjà les reticks). Pipeline : stochRaw = stoch(rsi, stochLen) ; K = SMA(stochRaw, smoothK) ;
    /// D = SMA(K, smoothD) ; si useAvg, K = avg(K_pre, D).</para>
    /// </summary>
    public static class StochRsi
    {
        /// <param name="rsi">Série RSI (built-in), source du stoch.</param>
        /// <param name="stochRawMem">Mémoire du stoch brut (créée par l'indicateur).</param>
        /// <param name="kMem">Mémoire de K avant moyenne (pour calculer D = SMA(K, smoothD)).</param>
        public static (double k, double d) Step(DataSeries rsi, int index,
            int stochLen, int smoothK, int smoothD, bool useAvg,
            IndicatorDataSeries stochRawMem, IndicatorDataSeries kMem)
        {
            // stoch brut du RSI sur stochLen.
            stochRawMem[index] = StochOf(rsi, index, stochLen);
            // K = SMA(stochRaw, smoothK).
            kMem[index] = Sma(stochRawMem, index, smoothK);
            double kPre = kMem[index];
            // D = SMA(K, smoothD).
            double d = Sma(kMem, index, smoothD);
            double k = useAvg && !double.IsNaN(d) ? (kPre + d) / 2.0 : kPre;
            return (k, d);
        }

        // (val − lowest)/(highest − lowest)·100 du RSI sur `len`. NaN si fenêtre incomplète/RSI na.
        private static double StochOf(DataSeries rsi, int index, int len)
        {
            if (index < len - 1) return double.NaN;
            double hi = double.MinValue, lo = double.MaxValue, cur = rsi[index];
            if (double.IsNaN(cur)) return double.NaN;
            for (int i = 0; i < len; i++)
            {
                double r = rsi[index - i];
                if (double.IsNaN(r)) return double.NaN;
                if (r > hi) hi = r;
                if (r < lo) lo = r;
            }
            double denom = hi - lo;
            return denom == 0.0 ? 0.0 : (cur - lo) / denom * 100.0;
        }

        // SMA fenêtrée sur une série, NaN si fenêtre incomplète ou valeur na dedans.
        private static double Sma(DataSeries s, int index, int len)
        {
            if (index < len - 1) return double.NaN;
            double sum = 0.0;
            for (int i = 0; i < len; i++)
            {
                double v = s[index - i];
                if (double.IsNaN(v)) return double.NaN;
                sum += v;
            }
            return sum / len;
        }
    }
}
