using System;
using cAlgo.API;

namespace TwoAi.Indicators.Core
{
    /// <summary>
    /// Équivalent de Pine <c>lib_series</c>. Analytics génériques sur séries :
    /// pente, planéité, fermeture, projection, fractales (Bill Williams).
    /// Couche 0 — aucune dépendance, ne dessine rien.
    ///
    /// Convention cAlgo : les helpers prennent <c>(DataSeries src, int index, ...)</c> —
    /// <c>index</c> est l'indice de la barre courante au moment de l'appel (typiquement passé
    /// depuis <c>Indicator.Calculate(int index)</c>). Les accès historiques se font en
    /// <c>src[index - n]</c> (équivalent Pine <c>src[n]</c>).
    /// </summary>
    public static class Series
    {
        /// <summary>
        /// Pente d'une série en %, entre la barre <paramref name="index"/> et celle d'il y a
        /// <paramref name="length"/> barres. Retourne <c>NaN</c> si une valeur est <c>NaN</c>
        /// ou si la valeur passée vaut 0.
        /// </summary>
        public static double SlopePercent(DataSeries src, int index, int length)
        {
            var current = src[index];
            var past = src[index - length];
            if (double.IsNaN(current) || double.IsNaN(past) || past == 0.0)
                return double.NaN;
            return (current - past) / past * 100.0;
        }

        /// <summary>
        /// Vrai si la pente en valeur absolue est sous le seuil. Détection "série plate".
        /// Retourne <c>false</c> si la pente est <c>NaN</c>.
        /// </summary>
        public static bool IsFlatSeries(DataSeries src, int index, int length, double threshold)
        {
            var slope = SlopePercent(src, index, length);
            return !double.IsNaN(slope) && Math.Abs(slope) < threshold;
        }

        /// <summary>
        /// Vrai si la pente va dans le sens de la fermeture, ET la série n'est pas plate.
        /// <paramref name="isUpper"/> = true → closing si la série descend (slope &lt; 0).
        /// <paramref name="isUpper"/> = false → closing si la série monte (slope &gt; 0).
        /// Mutuellement exclusif avec <see cref="IsFlatSeries"/> pour les mêmes paramètres.
        /// </summary>
        public static bool IsClosingSeries(DataSeries src, int index, int length, double threshold, bool isUpper)
        {
            var slope = SlopePercent(src, index, length);
            if (double.IsNaN(slope) || Math.Abs(slope) < threshold)
                return false;
            return isUpper ? slope < 0.0 : slope > 0.0;
        }

        /// <summary>
        /// Projette une SMA <paramref name="barsAhead"/> barres dans le futur sous l'hypothèse
        /// que <paramref name="src"/> reste constant à sa valeur courante. Calcul fermé exact
        /// sous cette hypothèse : on retire les <paramref name="barsAhead"/> valeurs les plus
        /// anciennes de la fenêtre et on les remplace par la valeur courante.
        /// <para>Formule : <c>SMA_proj(k) = SMA(N) + (k·src − Σ_{i=1..k} src[N−i]) / N</c>.</para>
        /// </summary>
        public static double ProjectMean(DataSeries src, int index, int length, int barsAhead)
        {
            if (index < length - 1) return double.NaN;

            double sumDropped = 0.0;
            for (int i = 1; i <= barsAhead; i++)
            {
                var v = src[index - (length - i)];
                if (!double.IsNaN(v)) sumDropped += v;
            }

            double sum = 0.0;
            for (int i = 0; i < length; i++)
            {
                sum += src[index - i];
            }
            var sma = sum / length;

            return sma + (barsAhead * src[index] - sumDropped) / length;
        }

        // ============================================================
        // Fractales (Bill Williams) — pivot 5 bougies sur src[index - 2]
        // ============================================================

        /// <summary>
        /// Vrai si <c>src[index-2]</c> est un sommet local sur la fenêtre
        /// <c>[index-4, index]</c> — "top fractal" Bill Williams.
        /// </summary>
        public static bool FractalTop(DataSeries src, int index)
        {
            return src[index - 4] < src[index - 2]
                && src[index - 3] < src[index - 2]
                && src[index - 2] > src[index - 1]
                && src[index - 2] > src[index];
        }

        /// <summary>
        /// Vrai si <c>src[index-2]</c> est un creux local sur la fenêtre
        /// <c>[index-4, index]</c> — "bot fractal" Bill Williams.
        /// </summary>
        public static bool FractalBot(DataSeries src, int index)
        {
            return src[index - 4] > src[index - 2]
                && src[index - 3] > src[index - 2]
                && src[index - 2] < src[index - 1]
                && src[index - 2] < src[index];
        }

        /// <summary>
        /// Encode l'état fractal de <c>src[index-2]</c> en un int signé :
        /// +1 (top), -1 (bot), 0 (rien).
        /// </summary>
        public static int Fractalize(DataSeries src, int index)
        {
            if (FractalTop(src, index)) return 1;
            if (FractalBot(src, index)) return -1;
            return 0;
        }
    }
}
