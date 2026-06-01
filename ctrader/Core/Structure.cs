using System.Collections.Generic;
using cAlgo.API;

namespace _2Ai.Indicators.Core
{
    /// <summary>
    /// Un IPA (Inefficacité Prix-Action) = un swing pivot cassé (clôture stricte au-delà) en
    /// attente de retest. Équivalent du UDT Pine <c>IPA</c>.
    /// </summary>
    public class Ipa
    {
        public double Price;
        public int Bar;        // bar_index de formation du pivot
        public bool IsRes;     // true = résistance (pivot high) ; false = support (pivot low)
        public bool Broken;    // true dès que close franchit price dans la bonne direction
        public int BreakBar;   // bar_index de la cassure ; -1 = pas cassé (≡ na Pine)

        public Ipa(double price, int bar, bool isRes, bool broken, int breakBar)
        {
            Price = price; Bar = bar; IsRes = isRes; Broken = broken; BreakBar = breakBar;
        }
    }

    /// <summary>
    /// Équivalent de Pine <c>lib_structure</c>. Gère une collection d'IPA via une state machine
    /// appelée à chaque barre. Couche 1 — ne dessine rien. Stateful (l'état pending, tenu en
    /// <c>var</c> interne côté Pine, devient des champs d'instance ici).
    ///
    /// <para>Lifecycle par barre : (1) détection pivots high/low (profondeur <c>pivotN</c>),
    /// (2) pending validé quand un pivot OPPOSÉ se forme → push, (3) breaks (close franchit),
    /// (4) retests (mèche revient depuis le côté opposé, sur barre &gt; breakBar) → remove,
    /// (5) anti-amas same-side sur la barre courante, (6) FIFO sur <c>maxIPAs</c>.</para>
    /// </summary>
    public class MarketStructure
    {
        private readonly int _pivotN, _maxIpas;
        private readonly List<Ipa> _ipas = new List<Ipa>();

        private double _pendPrice = double.NaN;
        private int _pendBar = -1;
        private bool _pendIsRes;

        /// <summary>Collection courante d'IPA (lecture seule pour le rendu).</summary>
        public IReadOnlyList<Ipa> Ipas => _ipas;

        public MarketStructure(int pivotN, int maxIpas)
        {
            _pivotN = pivotN;
            _maxIpas = maxIpas;
        }

        public void Update(DataSeries high, DataSeries low, DataSeries close, int index)
        {
            int n = _pivotN;
            int pivotBar = index - n;

            double ph = PivotHigh(high, index, n);
            double pl = PivotLow(low, index, n);

            // Pending : push le précédent si un pivot OPPOSÉ vient de se former, puis remplace.
            if (!double.IsNaN(ph))
            {
                if (!double.IsNaN(_pendPrice) && !_pendIsRes)
                    _ipas.Add(new Ipa(_pendPrice, _pendBar, _pendIsRes, false, -1));
                _pendPrice = ph; _pendBar = pivotBar; _pendIsRes = true;
            }
            if (!double.IsNaN(pl))
            {
                if (!double.IsNaN(_pendPrice) && _pendIsRes)
                    _ipas.Add(new Ipa(_pendPrice, _pendBar, _pendIsRes, false, -1));
                _pendPrice = pl; _pendBar = pivotBar; _pendIsRes = false;
            }

            double c = close[index], hi = high[index], lo = low[index];

            // Pass 1 — breaks (close franchit le niveau dans la bonne direction).
            foreach (var ipa in _ipas)
            {
                if (!ipa.Broken && ((ipa.IsRes && c > ipa.Price) || (!ipa.IsRes && c < ipa.Price)))
                {
                    ipa.Broken = true;
                    ipa.BreakBar = index;
                }
            }

            // Pass 2 — retests (mèche revient depuis le côté opposé), uniquement sur barres > breakBar.
            for (int i = _ipas.Count - 1; i >= 0; i--)
            {
                var ipa = _ipas[i];
                if (ipa.Broken && ipa.BreakBar >= 0 && ipa.BreakBar < index)
                {
                    bool retest = (ipa.IsRes && lo <= ipa.Price) || (!ipa.IsRes && hi >= ipa.Price);
                    if (retest) _ipas.RemoveAt(i);
                }
            }

            // Anti-amas : si plusieurs IPA same-side cassées sur la BARRE COURANTE, ne garder
            // que celle dont `Bar` est le plus récent.
            foreach (bool side in new[] { true, false })
            {
                int latestBar = -1;
                foreach (var ipa in _ipas)
                    if (ipa.IsRes == side && ipa.BreakBar == index && ipa.Bar > latestBar)
                        latestBar = ipa.Bar;
                if (latestBar >= 0)
                    for (int i = _ipas.Count - 1; i >= 0; i--)
                    {
                        var ipa = _ipas[i];
                        if (ipa.IsRes == side && ipa.BreakBar == index && ipa.Bar != latestBar)
                            _ipas.RemoveAt(i);
                    }
            }

            // FIFO.
            while (_ipas.Count > _maxIpas) _ipas.RemoveAt(0);
        }

        // Pivot high à p = index - n : high[p] strictement supérieur aux n highs de chaque côté.
        private static double PivotHigh(DataSeries high, int index, int n)
        {
            int p = index - n;
            if (p - n < 0) return double.NaN;
            double v = high[p];
            for (int k = 1; k <= n; k++)
                if (!(v > high[p - k]) || !(v > high[p + k])) return double.NaN;
            return v;
        }

        private static double PivotLow(DataSeries low, int index, int n)
        {
            int p = index - n;
            if (p - n < 0) return double.NaN;
            double v = low[p];
            for (int k = 1; k <= n; k++)
                if (!(v < low[p - k]) || !(v < low[p + k])) return double.NaN;
            return v;
        }
    }
}
