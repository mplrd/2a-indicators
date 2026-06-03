# All-In-One — Indicateurs tout-en-un (compte TradingView gratuit)

## Contexte / Objectif

Le plan **TradingView gratuit (Basic)** limite à **2 indicateurs par chart**. Les deux scripts
All-In-One regroupent une **sélection** de fonctionnalités de la suite 2Ai dans **2 indicateurs**
(1 overlay + 1 volet) = pile la limite, au lieu des ~7 indicateurs séparés.

Principe : **assemblage pur** des appels de libs existantes, **aucune logique dupliquée** (tout le
métier reste dans les libs `lib_*`). Le périmètre a été choisi par l'utilisateur final via un
tableau feature-par-feature (oui/non).

Fichiers :
- `tradingview/all-in-one-layout.pine` — overlay (`shorttitle = "AIO Layout"`)
- `tradingview/all-in-one-oscillators.pine` — volet (`shorttitle = "AIO Oscillators"`)

## Fonctionnalités

### AIO Layout (overlay)

| Source | Catégorie | Inclus |
|--------|-----------|--------|
| Layout | Bollinger Classique (+ mode ribbon) | ✅ |
| Layout | Bollinger Magique (mode simple) | ✅ |
| Layout | Détection bandes plates | ✅ |
| Layout | Projection des bandes | ✅ |
| Layout | MA 7 / 20 / 50 / 200 + projection | ✅ |
| Layout | Ichimoku complet + filtre Chikou-only | ✅ |
| Layout | Supertrend | ✅ |
| Levels | ATH | ✅ |
| Levels | PDH/PDL, PWH/PWL, PMH/PML | ✅ |
| Levels | Session Asiatique | ✅ |
| Levels | Open Range | ✅ |
| Zones S/D | Supply, Demand | ✅ |
| Zones S/D | Filtre 5★ (*Safe mode*) | ✅ |

**Volontairement exclus** (cochés "Non" au tableau) : Bollinger Magique ribbon, IPA, Gaps, niveaux
dynamiques HTF (Supertrend D/W, BB H4+, MA 50/200 D/W), sessions EU/US, Opens (Future/EU/US), FVG
**affichés**, toutes les zones CMI, zones MTF.

### AIO Oscillators (volet)

| Source | Catégorie | Inclus |
|--------|-----------|--------|
| MACD ZR | Oscillateur complet (lissage DEMA/TEMA/ZLEMA) | ✅ |
| Divergences | RSI : base, moyenne mobile, divergences, divergences cachées | ✅ |
| Divergences | Stochastic RSI : base, divergences, divergences cachées | ✅ |
| Divergences | Niveaux surachat/survente (normal + extrême) | ✅ |

Chaque oscillateur (MACD / RSI / Stoch) possède son propre interrupteur **Display**.

## Choix d'implémentation

- **Orchestration pure** : les 2 scripts importent les libs publiées et appellent leurs fonctions ;
  aucun calcul recopié. Imports (versions au moment de la livraison) :
  `series/3, bollinger/4, ma/4, ichimoku/1, supertrend/1, levels/12, signal/2, zone/2, sd/2, fvg/4,
  draw/18` (overlay) et `ma/4, macd/2, rsi/1, stochrsi/1, divergence/1` (volet).
- **Filtre 5★ sans afficher les FVG** : le 5★ = zone S/D confirmée par un FVG aligné (même side,
  chevauchement prix). Les FVG sont donc **calculés** (`fvg.updateZones`) pour évaluer l'alignement,
  mais **jamais dessinés** (l'utilisateur ne veut pas voir les FVG). Le toggle *Safe mode* est **OFF
  par défaut** (toutes les S/D visibles) ; **ON** → seulement les 5★.
- **Rebase d'échelle conditionnel du MACD** : le MACD (échelle "prix") et le RSI/Stoch (0-100) sont
  incompatibles dans un seul volet. Règle retenue :
  - MACD affiché **seul** (RSI et Stoch off) → valeurs **brutes**, histogramme en colonnes (baseline 0).
  - MACD affiché **avec** RSI et/ou Stoch → MACD **rebasé** dans la bande 0-100 (sa ligne zéro → 50)
    via un **softsign** borné `r/(1+|r|)` (Pine v6 n'a pas `math.tanh`), échelle = volatilité récente
    `ta.stdev(macd, 100)` (adaptatif, sans constante par instrument).
  - Histogramme rebasé : rendu en **aire centrée sur 50** (`fill`), car `plot(style_columns)` part
    toujours de la baseline 0 et ne peut pas se recentrer (cf. note plateforme ci-dessous).
- **Défauts alignés sur le tableau** : Bollinger Magique en *simple*, Classique en *ribbon*,
  Supertrend ON, Stoch + divergences cachées ON.
- **Accès libs pour le compte tiers** : le compte qui charge les AIO doit avoir accès aux libs
  importées. Comme l'utilisateur final utilise déjà les indicateurs 2Ai existants (qui importent les
  mêmes libs), l'accès est déjà acquis — rien à republier. Publier les 2 AIO + inviter suffit.

### Note plateforme (Pine v6)

- Pas de `math.tanh` → normalisation bornée via **softsign**.
- `plot(style_columns)` / `style_histogram` partent **toujours de 0** (pas de baseline configurable)
  → un histogramme centré sur 50 se rend via `fill()` (aire) ou `box.new` (vraies barres, plafonné
  par `max_boxes_count`). Choix retenu ici : **aire**.

## Cas de test (validés manuellement dans TradingView)

| # | Script | Scénario | Attendu |
|---|--------|----------|---------|
| 1 | Les deux | Compilation dans le Pine Editor | Aucune erreur (imports résolus) |
| 2 | Layout | UT intraday (< H1) | Sessions Asian + Open Range visibles ; PD/PW/PM/ATH selon UT |
| 3 | Layout | Bollinger : Magique=simple, Classique=ribbon | Magique en lignes, Classique en ruban ; bandes plates colorées bull/bear |
| 4 | Layout | *Safe mode* OFF puis ON | OFF : toutes les S/D ; ON : seulement les zones S/D alignées avec un FVG (FVG jamais dessinés) |
| 5 | Oscillators | MACD seul (RSI + Stoch off) | MACD sur échelle "prix" réelle, histogramme en colonnes |
| 6 | Oscillators | MACD + RSI (et/ou Stoch) affichés | MACD rebasé 0-100 (zéro sur la médiane 50), histogramme en aire centrée sur 50 ; RSI/Stoch lisibles |
| 7 | Oscillators | Toggles Display MACD / RSI / Stoch | Chaque oscillateur s'affiche/se masque indépendamment |
| 8 | Oscillators | Divergences RSI + cachées | Marqueurs de divergence régulières et cachées sur le RSI |
