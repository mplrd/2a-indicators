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
        /// Couleur d'un niveau dynamique selon sa position vs le prix : au-dessus → <paramref name="bearColor"/>
        /// (résistance), au-dessous ou égal → <paramref name="bullColor"/> (support). Équivaut à la
        /// logique de Pine <c>lib_draw.drawDynamicLevel</c>.
        /// </summary>
        public static Color PositionColor(double value, double price, Color bullColor, Color bearColor)
            => value > price ? bearColor : bullColor;

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

        /// <summary>
        /// Dessine un niveau de prix horizontal de <paramref name="startTime"/> à
        /// <paramref name="endTime"/> (typiquement origine de période → dernière barre), avec un
        /// label optionnel à droite. Équivalent simplifié de Pine <c>drawLevel</c> (le dessin
        /// cAlgo est temporel : pas de bar_index, on passe des <see cref="System.DateTime"/>).
        /// Idempotent : nom unique → l'objet est mis à jour en place à chaque redraw. Si
        /// <paramref name="price"/> est <c>NaN</c>, l'objet (et son label) est retiré.
        /// </summary>
        public static void DrawLevel(Chart chart, string name,
            System.DateTime startTime, System.DateTime endTime, double price,
            Color color, int thickness, LineStyle style, string label, bool showLabel,
            System.DateTime labelTime)
        {
            if (double.IsNaN(price))
            {
                chart.RemoveObject(name);
                chart.RemoveObject(name + "_lbl");
                return;
            }

            chart.DrawTrendLine(name, startTime, price, endTime, price, color, thickness, style);

            // Label à labelTime, distinct de la fin de ligne : permet d'arrêter la ligne avant le
            // label pour éviter le chevauchement (HTF), ou de coller le label à la fin (sessions).
            if (showLabel && !string.IsNullOrEmpty(label))
            {
                var txt = chart.DrawText(name + "_lbl", label, labelTime, price, color);
                txt.VerticalAlignment = VerticalAlignment.Center;
                txt.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                chart.RemoveObject(name + "_lbl");
            }
        }

        /// <summary>
        /// Dessine un niveau horizontal PLEINE LARGEUR (traverse tout le chart), pour les niveaux
        /// dynamiques (Supertrend / BB / MA) qui n'ont pas d'origine de période. Label optionnel
        /// ancré au temps <paramref name="labelTime"/> (typiquement la dernière barre). NaN → retiré.
        /// </summary>
        public static void DrawHorizontalLevel(Chart chart, string name, double price,
            Color color, int thickness, LineStyle style, string label, bool showLabel, System.DateTime labelTime)
        {
            if (double.IsNaN(price))
            {
                chart.RemoveObject(name);
                chart.RemoveObject(name + "_lbl");
                return;
            }

            chart.DrawHorizontalLine(name, price, color, thickness, style);

            if (showLabel && !string.IsNullOrEmpty(label))
            {
                var txt = chart.DrawText(name + "_lbl", label, labelTime, price, color);
                txt.VerticalAlignment = VerticalAlignment.Center;
                txt.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                chart.RemoveObject(name + "_lbl");
            }
        }

        /// <summary>
        /// Dessine un gap sous forme de rectangle rempli de <paramref name="startTime"/> à
        /// <paramref name="endTime"/>, entre <paramref name="top"/> et <paramref name="bottom"/>.
        /// <paramref name="alpha"/> suit la convention Pine (0 opaque → 100 invisible).
        /// Équivalent de Pine <c>drawGapBox</c>.
        /// </summary>
        public static void DrawGapBox(Chart chart, string name,
            System.DateTime startTime, System.DateTime endTime, double top, double bottom,
            Color color, int alpha)
        {
            var rect = chart.DrawRectangle(name, startTime, top, endTime, bottom, WithAlpha(color, alpha));
            rect.IsFilled = true;
        }

        /// <summary>
        /// Dessine une zone S/D ou FVG : un rectangle de remplissage (faible, <paramref name="fillAlpha"/>)
        /// + une bordure plus marquée (<paramref name="borderAlpha"/>, épaisseur/style donnés) si
        /// <paramref name="borderWidth"/> &gt; 0. Deux objets (<c>name</c>+"_f"/"_b") car cAlgo n'a
        /// pas d'alpha bordure/fond distincts sur un seul rectangle. Équiv Pine <c>drawZoneBox</c>.
        /// </summary>
        public static void DrawZoneBox(Chart chart, string name,
            System.DateTime leftTime, System.DateTime rightTime, double top, double bottom,
            Color color, int borderWidth, LineStyle borderStyle, int fillAlpha, int borderAlpha)
        {
            var fill = chart.DrawRectangle(name + "_f", leftTime, top, rightTime, bottom, WithAlpha(color, fillAlpha));
            fill.IsFilled = true;

            if (borderWidth > 0)
            {
                var border = chart.DrawRectangle(name + "_b", leftTime, top, rightTime, bottom,
                    WithAlpha(color, borderAlpha), borderWidth, borderStyle);
                border.IsFilled = false;
            }
            else
            {
                chart.RemoveObject(name + "_b");
            }
        }
    }
}
