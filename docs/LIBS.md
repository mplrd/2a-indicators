# Libs — Versions publiées sur TradingView

**Ce fichier est la SOURCE DE VÉRITÉ pour les imports Pine.** Toute génération de `import` doit s'aligner sur ce tableau. Si une lib n'apparaît pas comme "publiée" ici, son import doit rester commenté dans le code (avec `TODO`). Quand une lib bump, propager la nouvelle version dans **toutes** les libs et indicateurs qui l'importent (cf. `/delivery-plan`).

## Publisher

`mpilard`

## État

| Lib | Couche | Version | Date | Dépendances | Notes |
|-----|--------|---------|------|-------------|-------|
| `lib-time` | 0 | **`2`** | 2026-05-19 | — | publié (v2 retire le UDT Session, signatures `simple string sessionStr, simple string tz` directes — cf. memory pine_typing § 4) |
| `lib-market` | 0 | **`3`** | 2026-05-19 | — | publié (v3 : `detect()` basé sur `syminfo.type` + `syminfo.timezone` — beaucoup plus fiable que la détection bougies) |
| `lib-zone` | 0 | **`2`** | 2026-05-29 | — | publié (v1 : UDT `Zone` + enums `ZoneSide`/`ZoneState` + helpers `overlap`/`contains`/`expire`/`pushFIFO`/`removeExpired`/`trimOldest`/`findOverlapping` ; **v2** : ajoute `findOverlappingActiveSameSide(arr, z) → bool` — variante booléenne de `findOverlapping` restreinte aux zones ACTIVE de même `side`, sort le helper local éponyme de zones-SD.pine pour le filtre "5★") |
| `lib-series` | 0 | **`2`** | 2026-05-19 | — | publié (v2 ajoute `projectMean()`) |
| `lib-bollinger` | 1 | **`3`** | 2026-05-29 | `lib-series` v2 | publié (v2 ajoute `projectBands()`, signatures multi-ligne ; **v3** : ajoute `bHtfLevels(src, length, multInner, multOuter, flatLen, flatThr) → [outerUpper, outerLower, drawUpper, drawLower]` — orchestre `bands` + analyse flat/closing via `series.isFlatSeries/isClosingSeries`, sort les helpers locaux `bbmHtfLevels`/`bbcHtfLevels` dupliqués de levels.pine) |
| `lib-ma` | 1 | **`2`** | 2026-05-19 | `lib-bollinger` v1, `lib-series` v2 | publié (v2 ajoute `projectSMA()`) |
| `lib-ichimoku` | 1 | **`1`** | 2026-05-18 | — | publié |
| `lib-supertrend` | 1 | **`1`** | 2026-05-18 | — | publié |
| `lib-signal` | 1 | **`2`** | 2026-05-21 | — | publié (v1 : `SignalKind` + `detectCMI()` ; v2 : retire la condition de direction de la SMA de référence, garde uniquement `open < ref` / `open > ref`) |
| `lib-sd` | 1 | **`2`** | 2026-05-29 | `lib-zone` v2, `lib-signal` v2 | publié. v1 : `updateZones(sdZones, cmiSignal, maxZones)` qui orchestre lifecycle avant création + anti-chaînage + anti-chevauchement par remplacement + cleanup. **v2** : bump du dep `lib-zone` v1 → v2 — Pine v6 traite `lib_zone/1.Zone` et `lib_zone/2.Zone` comme deux types DISTINCTS (CE10123), donc bump cascade obligatoire pour qu'un caller qui a bumpé lib_zone puisse encore appeler `sd.updateZones`. |
| `lib-fvg` | 1 | **`4`** | 2026-05-29 | `lib-zone` v2 | publié. v1-v3 historique (detect/update/filtre temporel/updateZones). **v4** : bump du dep `lib-zone` v1 → v2 — même raison que lib-sd v2 (CE10123 sur identité cross-version d'UDT). |
| `lib-gap` | 1 | **`7`** | 2026-05-29 | — | publié. v6 = réécriture portée de l'ancien indicateur après faux gaps sur 24h + crash distance : API `detectDaily()` + `checkFill()` exposées au consommateur, cycle de vie de la box côté indicateur. **v7** : UDT `Gap` (top, bottom, dir, leftBarIndex) + `update(maxLookback) → array<Gap>` orchestre tout le cycle de vie (detect via request.security daily + push on day-change + FIFO trim + checkFill par barre, retrait si total / mutation directionnelle si partiel). `detectDaily` et `checkFill` deviennent **privés** (le consommateur n'utilise plus que `update`). Consommé par `levels.pine`. |
| `lib-levels` | 1 | **`12`** | 2026-05-20 | `lib-time` v2 | publié (v1 `previousPeriodHL` + `ath()` ; v2 `sessionHL()` ; v3 `sessionOpen()` + `sessionIBR()` ; v4-v7 itérations IBR ; v8 `firstH1OfDay()` ; v9-v10 itérations OR + TZ ; v11 `sessionHL/Open` reset à minuit chart (param `chartTz`), `openRange(tz)` ; v12 `sessionHL` parse `sessionStr` pour borner sEnd dès début de session) |
| `lib-structure` | 1 | **`3`** | 2026-05-22 | — | publié (v1 : UDT `IPA` + `IPAState` + `updateIPAs()` ; v2 : retest = close stricte ; v3 : retire `IPAState` (state pending en `var` interne, pattern lib-sd), remet retest = mèche, `array.set` défensif pour le break) |
| `lib-cmi-zone` | 1 | **`5`** | 2026-05-29 | `lib-signal` v2 | publié. CmiZone self-contained (champs à plat, pas de wrap `zone.Zone` — contourne CE10263/CE10293 cross-lib enum), enums `CmiSide`/`CmiState`. API : `updateOne(cmiSignal, tfMinutes, lookbackBars, interestStartTime, maxZones) → array<CmiZone>` (state machine per-TF, validations parallèles via queue `PendingCMI` interne — la CMI inverse n'annule PAS, invalidation uniquement sur cassure d'extrême low/high). Construction sur le corps : bull Top = lowest `min(open,close)`, Bottom = lowest `low` (bear miroir). `intraOverlapPass(zones, pendingMode)` : déclenchement pending = open dans zone OU chevauchement géométrique. `crossTfOverlapPass(lower, higher)` : chevauchement seul. `computeInterestStarts(chartTz)` exporte les bornes (incluant `< M15` = 1re bougie de J-1, `M15` = lundi de la semaine dernière). `interestStartForTf(tfMinutes, chartTz)` mappe tfMin → borne. Orchestration MTF côté indicateur (CE10051 : `request.security` ne peut dépendre d'args de fonction exportée). **v5** : ajoute `tickMtf(zones, tfMinutes, cmiSignal, htfO, htfH, htfL, htfC, htfT, lookbackBars, interestStartTime, maxZones)` — state machine MTF en chart-context. Le caller passe un `var array<CmiZone>` chart-context (instance unique, mutée in place, pas de snapshot série), les primitives HTF (OHLC + time + SignalKind) arrivent d'un seul `request.security` par TF retournant un TUPLE de primitives (au lieu d'un `array<CmiZone>` qui était snapshoté par chart-bar = explosion mémoire). Ring buffer interne (8 dernières barres HTF) pour la construction lookback (l'historique HTF n'est pas accessible depuis le chart-context). Détection de nouvelle barre HTF via change de `htfTime`. `updateOne` reste inchangé pour `zones-CMI.pine` (single-TF, pas de snapshot série). Consommé par `zones-CMI.pine` (updateOne) et `zones-MTF.pine` (tickMtf). |
| `lib-draw` | 2 | **`17`** | 2026-05-29 | — | publié (v4-v15 historique ; v16 consolidation `drawZoneBox` primitive agnostique UDT ; **v17** : ajoute `drawGapBox(show, leftBarIndex, top, bottom, col, bgAlpha, boxArr)` (rendu gap en `xloc.bar_index`, pas de bordure), `drawProjection(show, x1, y1, x2, y2, col, width, lineArr)` (ligne pointillée pour projections MA/BB), et le trio cleanup `clearBoxes(boxArr)` / `clearLines(lineArr)` / `clearLabels(labelArr)` (factorise les boucles `delete + array.clear` des indicateurs). `lineStyle` / `withAlpha` retirés (unused), `resolveLevelStartAndExtend` demoted to private. **Note plateforme conservée dans la lib** : `plot(..., linestyle/display = ...)` exige `input plot_line_style` / `input plot_display`, qu'une lib ne peut produire (CE10160/CE10123) → tout plot avec linestyle ou display dynamique reste inline dans l'indicateur. Consommateurs : layout, levels, zones-SD, zones-CMI, zones-MTF) |

## Imports actifs (à copier-coller)

Ce bloc reflète l'**état réel** publié — n'inclut PAS les libs `_non publié_`.

```pinescript
import mpilard/lib_time/2       as tm
import mpilard/lib_market/3     as market
import mpilard/lib_series/2     as series
import mpilard/lib_bollinger/3  as bb
import mpilard/lib_ma/2         as ma
import mpilard/lib_ichimoku/1   as ichi
import mpilard/lib_supertrend/1 as st
import mpilard/lib_zone/2       as zone
import mpilard/lib_signal/2     as signal
import mpilard/lib_sd/2         as sd
import mpilard/lib_fvg/4        as fvg
import mpilard/lib_gap/7        as gap
import mpilard/lib_levels/12    as levels
import mpilard/lib_structure/3  as structure
import mpilard/lib_cmi_zone/5   as cmiZone
import mpilard/lib_draw/17      as draw
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
