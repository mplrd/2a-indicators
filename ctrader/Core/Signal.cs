using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>Type de signal détecté sur une barre (équiv Pine <c>SignalKind</c>).</summary>
    public enum SignalKind { None, CmiBull, CmiBear }

    /// <summary>
    /// Équivalent de Pine <c>lib_signal</c>. Détection de "bougies de signal". Couche 1, pure
    /// (lit des barres, pas d'état), ne dessine rien.
    /// </summary>
    public static class Signal
    {
        /// <summary>
        /// Détecte un CMI (Continuation Movement Indicator) sur la barre <paramref name="index"/>.
        /// Référence = SMA 7 de hl2. Bull : open &lt; ref, close &gt; open, close &gt; high[1],
        /// open ≠ low. Bear : symétrique. Pas de validation 3 bougies (faite côté MTF).
        /// </summary>
        public static SignalKind DetectCmi(DataSeries high, DataSeries low, DataSeries close, DataSeries open, int index)
        {
            if (index < 7) return SignalKind.None;  // 7 hl2 + accès à high[1]/low[1]

            double sum = 0.0;
            for (int i = 0; i < 7; i++) sum += (high[index - i] + low[index - i]) / 2.0;
            double refv = sum / 7.0;

            double o = open[index], c = close[index];
            bool bull = o < refv && c > o && c > high[index - 1] && o != low[index];
            bool bear = o > refv && c < o && c < low[index - 1] && o != high[index];
            return bull ? SignalKind.CmiBull : bear ? SignalKind.CmiBear : SignalKind.None;
        }
    }
}
