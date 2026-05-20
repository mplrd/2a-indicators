# Libs — Versions publiées sur TradingView

**Ce fichier est la SOURCE DE VÉRITÉ pour les imports Pine.** Toute génération de `import` doit s'aligner sur ce tableau. Si une lib n'apparaît pas comme "publiée" ici, son import doit rester commenté dans le code (avec `TODO`). Quand une lib bump, propager la nouvelle version dans **toutes** les libs et indicateurs qui l'importent (cf. `/delivery-plan`).

## Publisher

`mpilard`

## État

| Lib | Couche | Version | Date | Dépendances | Notes |
|-----|--------|---------|------|-------------|-------|
| `lib-time` | 0 | **`2`** | 2026-05-19 | — | publié (v2 retire le UDT Session, signatures `simple string sessionStr, simple string tz` directes — cf. memory pine_typing § 4) |
| `lib-market` | 0 | **`3`** | 2026-05-19 | — | publié (v3 : `detect()` basé sur `syminfo.type` + `syminfo.timezone` — beaucoup plus fiable que la détection bougies) |
| `lib-zone` | 0 | _non publié_ | — | — | scaffold dispo |
| `lib-series` | 0 | **`2`** | 2026-05-19 | — | publié (v2 ajoute `projectMean()`) |
| `lib-bollinger` | 1 | **`2`** | 2026-05-19 | `lib-series` v2 | publié (v2 ajoute `projectBands()`, signatures multi-ligne) |
| `lib-ma` | 1 | **`2`** | 2026-05-19 | `lib-bollinger` v1, `lib-series` v2 | publié (v2 ajoute `projectSMA()`) |
| `lib-ichimoku` | 1 | **`1`** | 2026-05-18 | — | publié |
| `lib-supertrend` | 1 | **`1`** | 2026-05-18 | — | publié |
| `lib-cmi` | 1 | _non publié_ | — | `lib-zone`, `lib-series` | — |
| `lib-fvg` | 1 | _non publié_ | — | `lib-zone` | — |
| `lib-gap` | 1 | _non publié_ | — | `lib-time` | — |
| `lib-levels` | 1 | **`12`** | 2026-05-20 | `lib-time` v2 | publié (v1 `previousPeriodHL` + `ath()` ; v2 `sessionHL()` ; v3 `sessionOpen()` + `sessionIBR()` ; v4-v7 itérations IBR ; v8 `firstH1OfDay()` ; v9-v10 itérations OR + TZ ; v11 `sessionHL/Open` reset à minuit chart (param `chartTz`), `openRange(tz)` ; v12 `sessionHL` parse `sessionStr` pour borner sEnd dès début de session) |
| `lib-draw` | 2 | **`11`** | 2026-05-20 | — | publié (v4 `resolveLevelStartAndExtend` ; v5 `drawLevel` ; v6 `drawSessionLevel` ; v7 params `show` en `series bool` ; v8 `force_overlay` ; v9-v10 `drawSessionLevel` : params `endTime` + `ongoing`, label adaptatif ; v11 `drawLevel`/`drawSessionLevel` param `isHigh` + labels colorés `style_label_down/up` (body au-dessus pour H, en-dessous pour L, body en couleur du niveau, texte blanc)) |
| `lib-zone-draw` | 2 | _non publié_ | — | `lib-zone`, `lib-draw` | — |

## Imports actifs (à copier-coller)

Ce bloc reflète l'**état réel** publié — n'inclut PAS les libs `_non publié_`.

```pinescript
import mpilard/lib_time/2       as tm
import mpilard/lib_market/3     as market
import mpilard/lib_series/2     as series
import mpilard/lib_bollinger/2  as bb
import mpilard/lib_ma/2         as ma
import mpilard/lib_ichimoku/1   as ichi
import mpilard/lib_supertrend/1 as st
import mpilard/lib_levels/12    as levels
import mpilard/lib_draw/11      as draw
```

Les libs non publiées (`lib_zone`, `lib_cmi`, `lib_fvg`, `lib_gap`, `lib_zone_draw`) doivent garder leurs imports commentés (`// import mpilard/lib_X/<TODO> as X`) dans les fichiers qui les consomment.

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
| `lib_time` | `tm` *(éviter `time` qui shadow le builtin Pine)* |
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
