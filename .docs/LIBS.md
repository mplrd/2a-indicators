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
| `lib-series` | 0 | **`3`** | 2026-05-29 | — | publié (v2 ajoute `projectMean()` ; **v3** : ajoute le trio fractales Bill Williams — `fractalTop(src)`, `fractalBot(src)`, `fractalize(src) → int` (+1/-1/0). Pivot 5-bougie sur `src[2]`, building block pour `lib_divergence` et tout autre indicateur basé sur les swings) |
| `lib-bollinger` | 1 | **`4`** | 2026-05-29 | `lib-series` v3 | publié (v2 `projectBands()` + signatures multi-ligne ; v3 `bHtfLevels()` ; **v4** : bump du dep lib_series /2 → /3 pour cohérence, aucune signature changée) |
| `lib-ma` | 1 | **`4`** | 2026-05-29 | `lib-bollinger` v4, `lib-series` v3 | publié (v2 `projectSMA()` ; v3 trilogie zero-lag DEMA/TEMA/ZLEMA + enum `MaMode` + dispatcher `apply(mode, src, length)` ; **v4** : cascade bump des deps lib_series /2 → /3 et lib_bollinger /3 → /4 pour cohérence, aucune signature changée) |
| `lib-ichimoku` | 1 | **`1`** | 2026-05-18 | — | publié |
| `lib-supertrend` | 1 | **`1`** | 2026-05-18 | — | publié |
| `lib-signal` | 1 | **`4`** | 2026-06-04 | — | publié (v1 : `SignalKind` + `detectCMI()` ; v2 : retire la condition de direction de la SMA de référence, garde uniquement `open < ref` / `open > ref` ; **v3** : ajoute 3 détecteurs purs — `detectEngulfing()` (`low<=low[1] and close>high[1]` / miroir), `detectOpenExtreme()` (`open==low`/`open==high`, strict), `detectCOE()` — et les membres d'enum `ENGULF_BULL/BEAR`, `OPEN_LOW/HIGH`, `COE_BULL/BEAR` ; **v4** : `detectCOE()` devient un pattern 3 temps confirmé par le break (n-1 rouge clôture sur son bas → n ouvre sur son bas → n casse le high de la rouge ; miroir bear). **Additif** : les consommateurs existants (`lib_sd`, `lib_cmi_zone`, `zones-CMI`, `zones-MTF`) ne requièrent PAS le bump. Consommé par `signals-test.pine`) |
| `lib-sd` | 1 | **`2`** | 2026-05-29 | `lib-zone` v2, `lib-signal` v2 | publié. v1 : `updateZones(sdZones, cmiSignal, maxZones)` qui orchestre lifecycle avant création + anti-chaînage + anti-chevauchement par remplacement + cleanup. **v2** : bump du dep `lib-zone` v1 → v2 — Pine v6 traite `lib_zone/1.Zone` et `lib_zone/2.Zone` comme deux types DISTINCTS (CE10123), donc bump cascade obligatoire pour qu'un caller qui a bumpé lib_zone puisse encore appeler `sd.updateZones`. |
| `lib-fvg` | 1 | **`4`** | 2026-05-29 | `lib-zone` v2 | publié. v1-v3 historique (detect/update/filtre temporel/updateZones). **v4** : bump du dep `lib-zone` v1 → v2 — même raison que lib-sd v2 (CE10123 sur identité cross-version d'UDT). |
| `lib-gap` | 1 | **`8`** | 2026-05-29 | — | publié. v6 = réécriture portée de l'ancien indicateur après faux gaps sur 24h + crash distance : API `detectDaily()` + `checkFill()` exposées au consommateur, cycle de vie de la box côté indicateur. v7 : UDT `Gap` (top, bottom, dir, leftBarIndex) + `update(maxLookback) → array<Gap>` orchestre tout le cycle de vie (detect via request.security daily + push on day-change + FIFO trim + checkFill par barre, retrait si total / mutation directionnelle si partiel). `detectDaily` et `checkFill` deviennent **privés** (le consommateur n'utilise plus que `update`). **v8** : champ d'UDT `leftBarIndex` → `leftTime` (timestamp ms de la barre adjacente, capturé via `time[1]` au push). Fix du crash "Bar index too far" sur gros historique — l'ancre passe de `bar_index` à `time` (cf. `drawGapBox` v18). Consommé par `levels.pine`. |
| `lib-levels` | 1 | **`12`** | 2026-05-20 | `lib-time` v2 | publié (v1 `previousPeriodHL` + `ath()` ; v2 `sessionHL()` ; v3 `sessionOpen()` + `sessionIBR()` ; v4-v7 itérations IBR ; v8 `firstH1OfDay()` ; v9-v10 itérations OR + TZ ; v11 `sessionHL/Open` reset à minuit chart (param `chartTz`), `openRange(tz)` ; v12 `sessionHL` parse `sessionStr` pour borner sEnd dès début de session) |
| `lib-structure` | 1 | **`3`** | 2026-05-22 | — | publié (v1 : UDT `IPA` + `IPAState` + `updateIPAs()` ; v2 : retest = close stricte ; v3 : retire `IPAState` (state pending en `var` interne, pattern lib-sd), remet retest = mèche, `array.set` défensif pour le break) |
| `lib-macd` | 1 | **`2`** | 2026-05-29 | `lib-ma` v4 | publié. v1 : oscillateur MACD `zeroLag(src, fastLen, slowLen, signalLen, mode) → [macd, signal, hist, average]` paramétré par `ma.MaMode` (DEMA/TEMA/ZLEMA). **v2** : bump cascade du dep lib_ma /3 → /4 pour cohérence, aucune signature changée. Consommé par `macd-zr.pine`. |
| `lib-rsi` | 1 | **`1`** | 2026-05-29 | — | publié. **v1** : RSI (Wilder 1978) — `compute(src, length)` wrap de `ta.rsi`, + `movingAverage(rsi, length)` wrap de `ta.sma` sémantiquement scopé au RSI. Source unique de la computation RSI dans le projet — toute lib ou indicateur qui a besoin du RSI passe par ici (cohérence avec `lib_macd` / `lib_stochrsi`, portage cross-platform facilité). Consommé par `lib_stochrsi`, `divergences.pine`. |
| `lib-stochrsi` | 1 | **`1`** | 2026-05-29 | `lib-rsi` v1 | publié. **v1** : `compute(src, stochLen, rsiLen, smoothK, smoothD, log, useAvg) → [k, d]` — Stochastic RSI (oscillateur stochastique appliqué au RSI), portage de l'implémentation v5 validée. Pipeline : log(src) optionnel → `rsi.compute()` → Stoch → smooth K → smooth D, option `useAvg` qui remplace K par avg(K, D) pour lisser plus. Délègue le calcul RSI à `lib_rsi` (single source of truth). Consommé par `divergences.pine`. |
| `lib-divergence` | 1 | **`1`** | 2026-05-29 | `lib-series` v3 | publié. **v1** : `findDivergences(oscSrc, priceHigh, priceLow, topLimit, botLimit, useLimits) → [fractalTop, fractalBot, bearRegular, bullRegular, bearHidden, bullHidden]` — détection générique de divergences (régulières + cachées, bear + bull) sur n'importe quel oscillateur comparé au prix. Délègue les pivots à `lib_series.fractalTop`/`fractalBot`. Filtre optionnel par bornes (OB/OS) via `useLimits`. Réutilisable sur RSI, Stoch RSI, MACD, OBV, etc. Consommé par `divergences.pine`. |
| `lib-cmi-zone` | 1 | **`6`** | 2026-05-29 | `lib-signal` v2 | publié. CmiZone self-contained (champs à plat, pas de wrap `zone.Zone` — contourne CE10263/CE10293 cross-lib enum), enums `CmiSide`/`CmiState`. API : `updateOne(cmiSignal, tfMinutes, lookbackBars, interestStartTime, maxZones) → array<CmiZone>` (state machine per-TF, validations parallèles via queue `PendingCMI` interne — la CMI inverse n'annule PAS, invalidation uniquement sur cassure d'extrême low/high). Construction sur le corps : bull Top = lowest `min(open,close)`, Bottom = lowest `low` (bear miroir). `intraOverlapPass(zones, pendingMode)` : déclenchement pending = open dans zone OU chevauchement géométrique. `crossTfOverlapPass(lower, higher)` : chevauchement seul. `computeInterestStarts(chartTz)` exporte les bornes (incluant `< M15` = 1re bougie de J-1, `M15` = lundi de la semaine dernière). `interestStartForTf(tfMinutes, chartTz)` mappe tfMin → borne. Orchestration MTF côté indicateur (CE10051 : `request.security` ne peut dépendre d'args de fonction exportée). **v5** : ajoute `tickMtf(zones, tfMinutes, cmiSignal, htfO, htfH, htfL, htfC, htfT, lookbackBars, interestStartTime, maxZones)` — state machine MTF en chart-context. Le caller passe un `var array<CmiZone>` chart-context (instance unique, mutée in place, pas de snapshot série), les primitives HTF (OHLC + time + SignalKind) arrivent d'un seul `request.security` par TF retournant un TUPLE de primitives (au lieu d'un `array<CmiZone>` qui était snapshoté par chart-bar = explosion mémoire). Ring buffer interne (8 dernières barres HTF) pour la construction lookback (l'historique HTF n'est pas accessible depuis le chart-context). Détection de nouvelle barre HTF via change de `htfTime`. `updateOne` reste inchangé pour `zones-CMI.pine` (single-TF, pas de snapshot série). **v6** : fix `crossTfOverlapPass` — une zone basse n'est plus downgrade que si elle chevauche une zone haute **ACTIVE** (et non `non-EXPIRED` qui incluait les PENDING). Une zone haute PENDING ne peut pas justifier de cacher une zone basse, puisqu'elle-même n'est pas affichée. Élimine la cascade absurde "fantôme cache fantôme". Consommé par `zones-CMI.pine` (updateOne) et `zones-MTF.pine` (tickMtf). |
| `lib-draw` | 2 | **`18`** | 2026-05-29 | — | publié (v4-v15 historique ; v16 consolidation `drawZoneBox` primitive agnostique UDT ; v17 : ajoute `drawGapBox`, `drawProjection(show, x1, y1, x2, y2, col, width, lineArr)` (ligne pointillée pour projections MA/BB), et le trio cleanup `clearBoxes(boxArr)` / `clearLines(lineArr)` / `clearLabels(labelArr)` (factorise les boucles `delete + array.clear` des indicateurs). `lineStyle` / `withAlpha` retirés (unused), `resolveLevelStartAndExtend` demoted to private. **v18** : `drawGapBox(show, leftTime, top, bottom, col, bgAlpha, boxArr)` passe de `xloc.bar_index` à `xloc.bar_time` (param `leftBarIndex` → `leftTime`), aligné sur `drawZoneBox`. Fix du crash "Bar index value of the left argument is too far from the current bar index" quand un gap est trop ancien (limite de distance du bord gauche en `xloc.bar_index`). **Note plateforme conservée dans la lib** : `plot(..., linestyle/display = ...)` exige `input plot_line_style` / `input plot_display`, qu'une lib ne peut produire (CE10160/CE10123) → tout plot avec linestyle ou display dynamique reste inline dans l'indicateur. Consommateurs : layout, levels, zones-SD, zones-CMI, zones-MTF) |
| `lib-strat-range` | 1 | **`1`** | 2026-06-08 | — | publié. Setups d'une stratégie de RANGE (cassure/réintégration) sur un range quelconque (OR, Asian, London…). Exports : `swingStop(length, tolPct, dir)`, UDT `RangeSetup`, `detectRangeSetup(...)` (machine d'état d'excursion + géométrie SL/TP, range-agnostique, à appeler chaque barre hors conditionnel). Pur : aucun `input.*`, aucun dessin. Consommé par `strat-OR.pine`. |
| `lib-strat-draw` | 2 | **`1`** | 2026-06-08 | — | publié. Rendu GÉNÉRIQUE d'une position de stratégie (identique quelle que soit la strat) : box profit (entrée→TP3), box SL, lignes TP1/TP2, rayon d'exposition restante + étiquette. Exports : UDT `PositionDraw`, `newPositionDraw(...)`, `tickPositionDraw(...)` (extend/freeze au TP3/fade/BE/suppression à la sortie). Aucune logique métier, aucun appel `strategy.*`. Consommé par `strat-OR.pine`. |

## Imports actifs (à copier-coller)

Ce bloc reflète l'**état réel** publié — n'inclut PAS les libs `_non publié_`.

```pinescript
import mpilard/lib_time/2       as tm
import mpilard/lib_market/3     as market
import mpilard/lib_series/3     as series
import mpilard/lib_bollinger/4  as bb
import mpilard/lib_ma/4         as ma
import mpilard/lib_ichimoku/1   as ichi
import mpilard/lib_supertrend/1 as st
import mpilard/lib_zone/2       as zone
import mpilard/lib_signal/4     as signal
import mpilard/lib_sd/2         as sd
import mpilard/lib_fvg/4        as fvg
import mpilard/lib_gap/8        as gap
import mpilard/lib_levels/12    as levels
import mpilard/lib_structure/3  as structure
import mpilard/lib_macd/2       as macd
import mpilard/lib_rsi/1        as rsi
import mpilard/lib_stochrsi/1   as stochrsi
import mpilard/lib_divergence/1 as divergence
import mpilard/lib_cmi_zone/6   as cmiZone
import mpilard/lib_draw/18      as draw
import mpilard/lib_strat_range/1 as stratRange
import mpilard/lib_strat_draw/1  as stratDraw
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
| `lib_macd` | `macd` |
| `lib_rsi` | `rsi` |
| `lib_stochrsi` | `stochrsi` |
| `lib_divergence` | `divergence` |
| `lib_draw` | `draw` |
| `lib_strat_range` | `stratRange` |
| `lib_strat_draw` | `stratDraw` |
