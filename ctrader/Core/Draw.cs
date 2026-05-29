using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>
    /// Équivalent de Pine <c>lib_draw</c>. Couche 2 — helpers de rendu / décision de couleur.
    /// <para>NB : en cAlgo, les "plots" (équivalent de Pine <c>plot()</c>) sont déclarés via
    /// les propriétés <c>[Output]</c> dans l'indicateur — pas wrappables ici. Les "boxes" et
    /// "lines" arbitraires se font via l'API <c>Chart.DrawRectangle</c> / <c>Chart.DrawTrendLine</c>
    /// — wrappables (<see cref="DrawProjection"/> ci-dessous), à ajouter au fil du portage.</para>
    /// </summary>
    public static class Draw
    {
        /// <summary>
        /// Couleur d'une bande Bollinger (ou MA) selon son état (plate / en fermeture) et son
        /// côté (haute / basse). Quand la bande est "ouverte" → <paramref name="baseColor"/>.
        /// Quand elle est plate ou en fermeture → <paramref name="bullColor"/> (côté bas)
        /// ou <paramref name="bearColor"/> (côté haut).
        /// </summary>
        public static Color BandColor(Color baseColor, Color bullColor, Color bearColor, bool isFlat, bool isClosing, bool isUpper)
        {
            if (isFlat || isClosing)
                return isUpper ? bearColor : bullColor;
            return baseColor;
        }

        /// <summary>
        /// Applique une transparence à une couleur. Convention Pine portée :
        /// <paramref name="alpha"/> de 0 (opaque) à 100 (invisible).
        /// </summary>
        public static Color WithAlpha(Color baseColor, int alpha)
        {
            // Convert Pine alpha 0-100 to .NET alpha byte 0-255 (inverted).
            var dotNetAlpha = (byte)(((100 - alpha) * 255) / 100);
            return Color.FromArgb(dotNetAlpha, baseColor.R, baseColor.G, baseColor.B);
        }

        /// <summary>
        /// Dessine une ligne de projection (pointillée) entre deux points sur le chart.
        /// Wrapper de <c>Chart.DrawTrendLine</c> avec style hardcodé en pointillé — équivalent
        /// de Pine <c>drawProjection()</c>.
        /// </summary>
        /// <param name="chart">Référence Chart de l'indicateur consommateur (passé via <c>Indicator.Chart</c>).</param>
        /// <param name="name">Identifiant unique de l'objet (utilisé pour le retrouver/supprimer).</param>
        /// <param name="time1">Time du point 1.</param>
        /// <param name="price1">Prix du point 1.</param>
        /// <param name="time2">Time du point 2.</param>
        /// <param name="price2">Prix du point 2.</param>
        /// <param name="color">Couleur de la ligne.</param>
        /// <param name="thickness">Épaisseur (défaut 1).</param>
        public static ChartTrendLine DrawProjection(Chart chart, string name,
            System.DateTime time1, double price1, System.DateTime time2, double price2,
            Color color, int thickness = 1)
        {
            var line = chart.DrawTrendLine(name, time1, price1, time2, price2, color, thickness, LineStyle.DotsRare);
            return line;
        }
    }
}
