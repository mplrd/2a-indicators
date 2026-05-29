# Libs — Versions publiées sur TradingView

**Ce fichier est la SOURCE DE VÉRITÉ pour les imports Pine.** Toute génération de `import` doit s'aligner sur ce tableau. Si une lib n'apparaît pas comme "publiée" ici, son import doit rester commenté dans le code (avec `TODO`). Quand une lib bump, propager la nouvelle version dans **toutes** les libs et indicateurs qui l'importent (cf. `/delivery-plan`).

## Publisher

`mpilard`

## État

| Lib | Couche | Version | Date | Dépendances | Notes |
|-----|--------|---------|------|-------------|-------|
| `lib-time` | 0 | **`2`** | 2026-05-19 | — | publié (v2 retire le UDT Session, signatures `simple string sessionStr, simple string tz` directes — cf. memory pine_typing § 4) |
| `lib-market` | 0 | **`3`** | 2026-05-19 | — | publié (v3 : `detect()` basé sur `syminfo.type` + `syminfo.timezone` — beaucoup plus fiable que la détection bougies) |
| `lib-zone` | 0 | **`1`** | 2026-05-20 | — | publié (v1 : UDT `Zone` + enums `ZoneSide`/`ZoneState` + helpers `overlap`/`contains`/`expire`/`pushFIFO`/`removeExpired`/`trimOldest`/`findOverlapping`) |
| `lib-series` | 0 | **`2`** | 2026-05-19 | — | publié (v2 ajoute `projectMean()`) |
| `lib-bollinger` | 1 | **`2`** | 2026-05-19 | `lib-series` v2 | publié (v2 ajoute `projectBands()`, signatures multi-ligne) |
| `lib-ma` | 1 | **`2`** | 2026-05-19 | `lib-bollinger` v1, `lib-series` v2 | publié (v2 ajoute `projectSMA()`) |
| `lib-ichimoku` | 1 | **`1`** | 2026-05-18 | — | publié |
| `lib-supertrend` | 1 | **`1`** | 2026-05-18 | — | publié |
| `lib-signal` | 1 | **`2`** | 2026-05-21 | — | publié (v1 : `SignalKind` + `detectCMI()` ; v2 : retire la condition de direction de la SMA de référence, garde uniquement `open < ref` / `open > ref`) |
| `lib-sd` | 1 | **`1`** | 2026-05-21 | `lib-zone` v1, `lib-signal` v2 | publié (v1 : `updateZones(sdZones, cmiSignal, maxZones)` qui orchestre lifecycle avant création + anti-chaînage + anti-chevauchement par remplacement + cleanup) |
| `lib-fvg` | 1 | **`3`** | 2026-05-21 | `lib-zone` v1 | publié (v1 : `detect()` + `update()` avec exclusion N barres post-day-change ; v2 : remplace l'exclusion par un filtre transition temporelle `> 1.5×` durée bar ; v3 : ajoute `updateZones(fvgZones, maxZones)` qui orchestre lifecycle + detect + cleanup) |
| `lib-gap` | 1 | **`6`** | 2026-05-26 | — | publié. v6 = **réécriture** portée de l'ancien indicateur (`_tv-indicators`) après faux gaps sur 24h + crash distance : API `detectDaily()` → [hasGap, top, bottom, dir(1/-1/0)] (détection sur barre daily `low/high` vs `close[1]`, appelée via `request.security(ticker,"D",…)`) + `checkFill(top,bottom,dir)` → [isFilled, isPartial, newTop, newBottom]. La box est créée 1× au day-change côté indicateur (`bar_index-1`) puis mutée. (v1-v5 obsolètes : detect/update/updateGaps/Gap/startBar, approche minuit-chart abandonnée.) Consommé par `levels.pine`. |
| `lib-levels` | 1 | **`12`** | 2026-05-20 | `lib-time` v2 | publié (v1 `previousPeriodHL` + `ath()` ; v2 `sessionHL()` ; v3 `sessionOpen()` + `sessionIBR()` ; v4-v7 itérations IBR ; v8 `firstH1OfDay()` ; v9-v10 itérations OR + TZ ; v11 `sessionHL/Open` reset à minuit chart (param `chartTz`), `openRange(tz)` ; v12 `sessionHL` parse `sessionStr` pour borner sEnd dès début de session) |
| `lib-structure` | 1 | **`3`** | 2026-05-22 | — | publié (v1 : UDT `IPA` + `IPAState` + `updateIPAs()` ; v2 : retest = close stricte ; v3 : retire `IPAState` (state pending en `var` interne, pattern lib-sd), remet retest = mèche, `array.set` défensif pour le break) |
| `lib-cmi-zone` | 1 | **`4`** | 2026-05-28 | `lib-signal` v2 | publié. CmiZone self-contained (champs à plat, pas de wrap `zone.Zone` — contourne CE10263/CE10293 cross-lib enum), enums `CmiSide`/`CmiState`. API : `updateOne(cmiSignal, tfMinutes, lookbackBars, interestStartTime, maxZones) → array<CmiZone>` (state machine per-TF, validations parallèles via queue `PendingCMI` interne — la CMI inverse n'annule PAS, invalidation uniquement sur cassure d'extrême low/high). Construction sur le corps : bull Top = lowest `min(open,close)`, Bottom = lowest `low` (bear miroir). `intraOverlapPass(zones, pendingMode)` : déclenchement pending = open dans zone OU chevauchement géométrique. `crossTfOverlapPass(lower, higher)` : chevauchement seul. `computeInterestStarts(chartTz)` exporte les bornes (incluant `< M15` = 1re bougie de J-1, `M15` = lundi de la semaine dernière). `interestStartForTf(tfMinutes, chartTz)` mappe tfMin → borne. Orchestration MTF côté indicateur (CE10051 : `request.security` ne peut dépendre d'args de fonction exportée). Consommé par `zones-cmi.pine` ; `zones-MTF.pine` à wirer. |
| `lib-draw` | 2 | **`16`** | 2026-05-28 | — | publié (v4 `resolveLevelStartAndExtend` ; v5 `drawLevel` ; v6 `drawSessionLevel` ; v7 params `show` en `series bool` ; v8 `force_overlay` ; v9-v10 `drawSessionLevel` params `endTime` + `ongoing` + label adaptatif ; v11 `isHigh` + labels colorés ; v12 `col` en `series color` ; v13 ajout `drawZone()` + import `lib_zone` ; v14 `drawZone.lbl` en `series string` ; v15 ajout `drawDynamicLevel()` ; **v16** : consolidation — `drawZone` + `drawCmiZone` remplacés par une seule primitive `drawZoneBox(show, isActive, leftTime, top, bottom, col, borderWidth, borderStyle, bgAlpha, borderAlpha, showLbl, lbl, boxArr, labelArr)` agnostique du type UDT ; le caller déballe son UDT et passe `zone.isActive(z)` ou `cz.state == CmiState.ACTIVE` comme bool. Imports `lib_zone` et `lib_cmi_zone` RETIRÉS — la lib redevient pure primitive. Consommateurs : layout, levels, zones-SD, zones-CMI, zones-MTF) |

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
import mpilard/lib_zone/1       as zone
import mpilard/lib_signal/2     as signal
import mpilard/lib_sd/1         as sd
import mpilard/lib_fvg/3        as fvg
import mpilard/lib_gap/6        as gap
import mpilard/lib_levels/12    as levels
import mpilard/lib_structure/3  as structure
import mpilard/lib_cmi_zone/4   as cmiZone
import mpilard/lib_draw/16      as draw
```

Toutes les libs sont publiées — aucun import à garder commenté à ce jour.

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
| `lib_signal` | `signal` |
| `lib_sd` | `sd` |
| `lib_fvg` | `fvg` |
| `lib_gap` | `gap` |
| `lib_cmi_zone` | `cmiZone` |
| `lib_levels` | `levels` |
| `lib_structure` | `structure` |
| `lib_draw` | `draw` |
