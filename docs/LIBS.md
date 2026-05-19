# Libs — Versions publiées sur TradingView

**Ce fichier est la SOURCE DE VÉRITÉ pour les imports Pine.** Toute génération de `import` doit s'aligner sur ce tableau. Si une lib n'apparaît pas comme "publiée" ici, son import doit rester commenté dans le code (avec `TODO`). Quand une lib bump, propager la nouvelle version dans **toutes** les libs et indicateurs qui l'importent (cf. `/delivery-plan`).

## Publisher

`mpilard`

## État

| Lib | Couche | Version | Date | Dépendances | Notes |
|-----|--------|---------|------|-------------|-------|
| `lib-time` | 0 | _non publié_ | — | — | scaffold dispo |
| `lib-market` | 0 | _non publié_ | — | `lib-time` | scaffold stub (`detect()` retourne `NON_CASH`) |
| `lib-zone` | 0 | _non publié_ | — | — | scaffold dispo |
| `lib-series` | 0 | **`2`** | 2026-05-19 | — | publié (v2 ajoute `projectMean()`) |
| `lib-bollinger` | 1 | **`2`** | 2026-05-19 | `lib-series` v2 | publié (v2 ajoute `projectBands()`, signatures multi-ligne) |
| `lib-ma` | 1 | **`2`** | 2026-05-19 | `lib-bollinger` v1, `lib-series` v2 | publié (v2 ajoute `projectSMA()`) |
| `lib-ichimoku` | 1 | **`1`** | 2026-05-18 | — | publié |
| `lib-supertrend` | 1 | **`1`** | 2026-05-18 | — | publié |
| `lib-cmi` | 1 | _non publié_ | — | `lib-zone`, `lib-series` | — |
| `lib-fvg` | 1 | _non publié_ | — | `lib-zone` | — |
| `lib-gap` | 1 | _non publié_ | — | `lib-time` | — |
| `lib-levels` | 1 | _non publié_ | — | `lib-time`, `lib-market` | — |
| `lib-draw` | 2 | **`3`** | 2026-05-19 | — | publié (v3 ajoute `enum LineStyle` + `lineStyle()` typé, retire `plotLineStyle()` — non implémentable en lib, cf. CE10160) |
| `lib-zone-draw` | 2 | _non publié_ | — | `lib-zone`, `lib-draw` | — |

## Imports actifs (à copier-coller)

Ce bloc reflète l'**état réel** publié — n'inclut PAS les libs `_non publié_`.

```pinescript
import mpilard/lib_series/2     as series
import mpilard/lib_bollinger/2  as bb
import mpilard/lib_ma/2         as ma
import mpilard/lib_ichimoku/1   as ichi
import mpilard/lib_supertrend/1 as st
import mpilard/lib_draw/3       as draw
```

Les libs non publiées (`lib_time`, `lib_market`, `lib_zone`, `lib_cmi`, `lib_fvg`, `lib_gap`, `lib_levels`, `lib_zone_draw`) doivent garder leurs imports commentés (`// import mpilard/lib_X/<TODO> as X`) dans les fichiers qui les consomment.

## Workflow de publication

1. Ouvrir la lib dans Pine Editor sur TradingView.
2. Cliquer sur **Add to chart** pour vérifier qu'elle compile.
3. Cliquer sur **Publish library** puis choisir la visibilité (Private au début, Public quand stable).
4. TradingView assigne un numéro de version (`1`, puis incréments à chaque republish).
5. **Mettre à jour ce fichier** : passer la ligne de la lib de `_non publié_` → version assignée + date + ajouter l'import au bloc "Imports actifs".
6. Bumper le `/<version>` dans tous les `import` consommateurs (autres libs ET indicateurs) — voir aussi `/delivery-plan`.

## Convention d'alias

Les alias courts à utiliser systématiquement dans les `import` :

| Lib | Alias |
|-----|-------|
| `lib_time` | `time` |
| `lib_market` | `market` |
| `lib_zone` | `zone` |
| `lib_series` | `series` |
| `lib_bollinger` | `bb` |
| `lib_ma` | `ma` |
| `lib_ichimoku` | `ichi` |
| `lib_supertrend` | `st` |
| `lib_cmi` | `cmi` |
| `lib_fvg` | `fvg` |
| `lib_gap` | `gap` |
| `lib_levels` | `levels` |
| `lib_draw` | `draw` |
| `lib_zone_draw` | `zoneDraw` |
