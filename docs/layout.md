# 2Ai Layout — Documentation

Indicateur de surcouche graphique (Pine v6, TradingView). Source : `tradingview/layout.pine`. Spec de référence : `docs/SPECIFICATIONS.md` § "Indicateur 1 : 2Ai Layout".

## Fonctionnalités

Du point de vue trader, l'indicateur superpose au chart 4 familles d'outils, toutes activables/désactivables et configurables indépendamment :

### Bandes de Bollinger
- **Bollinger Classique** (SMA 20, multiplicateurs 2.0 / 2.5) et **Bollinger Magique** (SMA 160, multiplicateurs 2.5 / 2.8) en parallèle.
- Pour chaque BB : toggle, couleur, mode (`simple` ou `ribbon`), style de ligne (`solid` / `dashed` / `dotted`).
- **Mode simple** : seule la bande outer est tracée.
- **Mode ribbon** : bande outer + bande inner + fill entre les deux. L'inner est dessinée à 50% de transparence avec le même linestyle que l'outer pour rester subordonnée visuellement à l'outer.
- **Médiane non tracée** : on n'affiche jamais la SMA centrale, par choix de lisibilité.
- **Linewidth** : 1px pour BBc, 2px pour BBm. Permet de distinguer les deux d'un coup d'œil sans regarder la position.

### Détection de bandes plates / en fermeture (transversal aux deux BB)
- Mesure la pente en pourcentage de chaque bande externe sur N barres (`flatPeriod`, défaut 2).
- **Plate** : `|slope%| < seuil`. **En fermeture** : pente dans le sens de la fermeture (outer haute descendante / outer basse ascendante).
- **Rendu en mode simple** : un overlay 2px (BBc) / 3px (BBm) en couleur bull (bande basse) ou bear (bande haute) vient s'ajouter par-dessus la bande grise.
- **Rendu en mode ribbon** : le fill change de couleur (bull en bas, bear en haut) et passe d'une transparence 85 (ouverte) à 70 (plate/fermeture) pour la lisibilité.

### Moyennes mobiles
Quatre SMA (7, 20, 50, 200), chacune avec toggle, couleur, mode et style de ligne propres. Defaults :

| MA | Périodes | Linewidth | Style | Actif | Couleur |
|----|----------|-----------|-------|-------|---------|
| MA7 | 7 | 1px | dashed | off | aqua |
| MA20 | 20 | 1px | solid | off | bleu |
| MA50 | 50 | 2px | solid | **on** | orange |
| MA200 | 200 | 2px | dashed | off | gris |

Mode `ribbon` : trace une enveloppe `SMA ± 0.236 * stdev` autour de la basis, fill gris très atténué (alpha 85).

### Ichimoku
- Composants standards (Tenkan 9, Kijun 26, Senkou 52).
- Deux modes : **complet** (toutes les lignes + nuage) et **Chikou uniquement** (juste la Chikou décalée -26).
- **Lignes du nuage** (Senkou A/B) : transparence 60 — visibles mais discrètes, le focus va sur le fill.
- **Nuage** : bleu si A > B (haussier), jaune si B > A (baissier), transparence 85.
- **Tenkan** : jaune moutarde (#d4a017), volontairement distinct de l'orange MA50 pour ne pas se confondre.

### Supertrend
- ATR period = 10, multiplicateur = 3.
- **Ligne brisée** à chaque changement de direction : la barre de bascule injecte un `na` et `plot.style_linebr` empêche le bridging visuel. Résultat : pas de saut diagonal disgracieux entre le dernier point baissier et le premier point haussier (ou inversement).
- Couleur bull si direction = +1, bear sinon.

### Couleurs globales bull / bear
Inputs à part en haut du panneau "Apparence Globale". Utilisés par : détection bandes plates, Senkou A/B (couleur de la ligne au-dessus / en-dessous), Supertrend. Permet de cohérencer toute la palette de l'indicateur via 2 réglages.

## Choix d'implémentation

### Découpage par couches
L'indicateur n'embarque aucun calcul métier — uniquement des inputs, l'orchestration et le rendu. Les calculs vivent dans les libs :

| Lib (couche) | Rôle dans Layout |
|--------------|------------------|
| `lib-series` (0) | `slopePercent`, `isFlatSeries`, `isClosingSeries` pour la détection des bandes plates |
| `lib-bollinger` (1) | `bb.bands()` pour les bornes inner/outer |
| `lib-ma` (1) | `ma.ribbon()` pour basis + upper/lower du ruban MA (délègue à `bb.bands` en interne) |
| `lib-ichimoku` (1) | `ichi.components()` pour Tenkan/Kijun/Senkou/Chikou |
| `lib-supertrend` (1) | `st.run()` pour la ligne + direction normalisée ±1 |
| `lib-draw` (2) | `bandColor()` pour mapper état → couleur de bande ; helpers de couleurs/styles |

### Plot count Pine — contrainte clé
Pine limite à **64 plot counts par script**. Chaque `plot()` consomme 1 ou 2 counts selon les qualifiers de ses paramètres (color/series notamment). `fill()` consomme aussi quand son color est `series`. Pour tenir sous la limite avec toutes les features actives :

1. **Hidden plots pour les fills MA** : `color = na`, `display = display.none`, `editable = false`. Sans color réel ils ne contribuent quasiment pas au compte.
2. **Médiane BB non plottée** : économise 2 plots.
3. **Pre-compute des display constants** (`bbcDisplay = bbcEnabled ? display.all : display.none`) : un qualifier `input display` plutôt qu'un `series bool` sur la valeur. Couplé à `value = série brute` (pas de ternaire `condition ? série : na`).
4. **`color.new(simple, simple)`** plutôt que `draw.withAlpha(...)` (lib) pour les couleurs simples : la signature `series color` de la lib inflate le qualifier en série, alors que `color.new` natif préserve le qualifier le plus restrictif.
5. **MA fills en `color.new(simple, simple)`** quand non-actif → couleur reste `input/simple`, fill ne compte pas.

Source de cette architecture : `_tv-indicators/layout.pine` (legacy ~787 lignes) qui tient sous 64 avec MORE features grâce au même pattern.

### CW10002 — appels dans une conditionnelle
Les fonctions `series.isFlatSeries` / `series.isClosingSeries` font du lookback historique (`src[length]`). Pine warne si on les appelle uniquement dans une branche conditionnelle (`flatEnabled and series.isFlatSeries(...)`). Solution : appel inconditionnel d'abord, puis combinaison avec le toggle :
```pinescript
bbcUpperFlatRaw = series.isFlatSeries(bbcOuterU, flatPeriod, flatThreshold)
bbcUpperFlat    = flatEnabled and bbcUpperFlatRaw
```

### CE10123 — qualifier de `linestyle`
`plot(..., linestyle = ...)` exige un qualifier `input plot_line_style`. Un helper de lib qui prend un `simple string` retourne `simple plot_line_style` et est rejeté. Solution actuelle : conversion **inline** dans l'indicateur (`bbcLineStyle == "----" ? plot.linestyle_dashed : ...`). Côté `lib-draw` v2 publié, l'export `plotLineStyle()` a la signature buggée ; la signature corrigée (`input string`) attend en v3.

### Supertrend ligne brisée
Pour casser visuellement la ligne aux pivots, on détecte le changement de direction et on injecte `na` sur cette barre. `plot.style_linebr` empêche Pine de relier les deux segments à travers le `na`. Sans ce traitement, on aurait un trait diagonal disgracieux entre le dernier point baissier et le premier haussier.

## Cas de test

Validation manuelle sur TradingView. À cocher au fur et à mesure.

| # | Instrument | Timeframe | Scénario | Comportement attendu | Statut |
|---|------------|-----------|----------|----------------------|--------|
| 1 | EURUSD | H1 | Range serré, BBc en ribbon | Ribbon visible, outer + inner + fill atténué | ⏳ |
| 2 | EURUSD | H1 | Range très plat, détection bandes plates active | Fill BBc passe en bull/bear (alpha 70), accent overlay 2px en simple | ⏳ |
| 3 | EURUSD | M15 | Tendance forte | BB ouvertes, fill gris très atténué (alpha 85), pas d'accent | ⏳ |
| 4 | BTCUSDT | H1 | Cryptos, instrument H24 | Toutes les MAs s'affichent, MA50 par défaut active | ⏳ |
| 5 | ES1! | D | Future US, daily | Ichimoku complet visible, Senkou A/B atténués, nuage marqué | ⏳ |
| 6 | NQ1! | H4 | Future US | Supertrend cassée aux pivots (pas de diagonal au flip) | ⏳ |
| 7 | AAPL | D | Cash US, daily | Tenkan moutarde (#d4a017) ≠ MA50 orange ; lisibles ensemble | ⏳ |
| 8 | EURUSD | M5 | Live (laisser tourner 30 minutes) | Aucun repaint des bandes plates / Supertrend en temps réel | ⏳ |
| 9 | EURUSD | H1 | Toggle BBc off, BBm on | Seule BBm visible, panneau de réglages cohérent | ⏳ |
| 10 | EURUSD | H1 | Changer linestyle BBc en dashed | Outer + inner BBc dashed, fill inchangé | ⏳ |

**Non testé / hors scope v1** :
- Performance sur > 5000 barres historiques (Pine `max_bars_back` plafond).
- Cas de toutes les MAs en mode ribbon simultanément avec détection plate active : potentiel risque de saturation visuelle, à itérer au feedback utilisateur.
