# 2Ai Zones MTF — Documentation

Indicateur multi-timeframe des CMI Zones (Pine v6, TradingView). Source : `tradingview/zones-MTF.pine`. Spec de référence : `docs/SPECIFICATIONS.md` § "Indicateur 3 : 2Ai Zones MTF".

Lib portant **toute** la logique métier (lifecycle, anti-chevauchement, orchestration MTF) : `tradingview/lib-cmi-zone.pine` (Couche 1, à publier sur TradingView avant que l'indicateur soit utilisable). L'indicateur est un simple orchestrateur de rendu.

## Fonctionnalités

L'indicateur projette sur le chart les **CMI Zones** détectées sur 6 timeframes simultanément (Monthly, Weekly, Daily, H4, H1, M15), à partir d'un signal source : le **CMI** (Continuation Movement Indicator, cf. `lib_signal`).

### Détection et validation
- **CMI brut** : détecté par `lib_signal.detectCMI()` (SMA 7 de hl2 comme référence, ouverture/clôture des bons côtés, casse de mèche, anti-extrême — cf. SPECIFICATIONS § Indicateur 3 / Detection).
- **Validation 3 bougies** :
  - Bull : invalidée si close passe sous l'open de la CMI dans les 3 bougies suivantes.
  - Bear : invalidée si close passe au-dessus de l'open.
  - **CMI opposée pendant la validation** → l'ancienne est annulée, la nouvelle démarre sa propre validation.
- Une zone n'est créée qu'**après validation complète**.

### Construction de la zone (lookback)
Fenêtre de `lookbackBars` bougies (input global `Drawing CMI Zone Lookback`, défaut 3, plage 1-5) en partant de la barre de la CMI :
- **Bull** : `top` = lowest close, `bottom` = lowest low (mèche).
- **Bear** : `top` = highest high (mèche), `bottom` = highest close.
- `leftTime` = la plus ancienne des deux barres d'extremum.

### Anti-chevauchement — 2 dimensions

**Intra-TF** (toggle `Mode chevauchement intra-TF`, défaut `Pending`) :
- `Pending` : nouvelle zone qui chevauche une zone non-expirée (ACTIVE OU PENDING) du même TF → marquée PENDING. **Permet le chaînage** : N pending peuvent s'empiler sur le même prix et se réactiver une à une dans l'ordre FIFO au fil des expirations.
- `Replacement` : toutes les anciennes non-expirées qui chevauchent la nouvelle sont marquées EXPIRED ; la nouvelle prend leur place ACTIVE.

**Cross-TF** (toggle `Mode chevauchement cross-TF`, défaut `Pending`) :
- `Pending` : une zone qui chevauche une zone non-expirée d'un TF **supérieur** est marquée PENDING. Priorité : Monthly > Weekly > Daily > H4 > H1 > M15.
- `Autorise` : aucune règle cross-TF, les zones de TF différents peuvent se superposer.

Les deux dimensions sont indépendantes et se combinent.

### Périodes d'intérêt par TF (expiration automatique, en chartTz)

| TF | Période d'intérêt |
|----|-------------------|
| Monthly | illimitée (jamais expirée par période) |
| Weekly | depuis le 1er janvier de (année − 3) |
| Daily | depuis le 1er du mois (mois − 6) |
| H4 | depuis le 1er du mois (mois − 1) |
| H1 | depuis le lundi d'il y a 2 semaines |
| M15 | depuis aujourd'hui 00h − 5 jours |

**Tout est calculé en `chartTz`** (setting `Chart timezone`, défaut `Europe/Paris`) — pas en `syminfo.timezone`. Pine n'expose pas la TZ d'affichage, il faut donc la fournir explicitement (même piège que sur 2Ai Levels pour les sessions / Open Range).

### Affichage par TF
Chaque TF : toggle on/off, couleur, style de bordure, épaisseur (0-4). Transparence fond 85%, bordure 40%. Defaults :

| TF | Default | Couleur | Bordure | Épaisseur |
|----|---------|---------|---------|-----------|
| Monthly | on | olive | solid | 1 |
| Weekly | on | teal | solid | 1 |
| Daily | on | bleu | solid | 1 |
| H4 | on | `rgb(91,156,246)` | dashed | 1 |
| H1 | off | `rgb(244,143,177)` | dotted | 1 |
| M15 | off | `rgb(252,185,200)` | dotted | 0 |

### Filtre TF chart vs TF zone
Une zone TF X ne s'affiche que si la chart TF est ≤ X. Exception : Monthly toujours visible.

## Choix d'implémentation

### Découpage par couches — tout dans la lib

| Lib (couche) | Rôle dans Zones MTF |
|--------------|---------------------|
| `lib-signal` (1) | `detectCMI()` — détection brute, réutilisée depuis 2Ai Zones |
| `lib-zone` (0) | UDT `Zone` + states (ACTIVE / PENDING / EXPIRED) + helpers (`overlap`, `expire`, `setPending`, `activate`, `removeExpired`, `trimOldest`) |
| `lib-cmi-zone` (1) | **Tout le métier** : `updateAllTfs(...)` orchestre 6 `request.security` + post-pass anti-chevauchement intra + cross-TF + calcul des bornes en chartTz. Helpers exposés : `updateOne`, `intraOverlapPass`, `crossTfOverlapPass` (utilisables par une future stratégie sans MTF) |
| `lib-draw` (2) | `drawZone()` |

L'indicateur ne contient **aucune logique métier**, aucun calcul de date, aucun `request.security`. Juste : inputs + un appel à `cmiZone.updateAllTfs(...)` + rendu via `renderTf` helper local.

### Architecture du pipeline (dans la lib)

1. **`computeInterestStarts(chartTz)`** (privé) : calcule les 6 bornes `interestStartTime` à partir de `year(time, chartTz)` / `month(time, chartTz)` / `dayofmonth(time, chartTz)` / `dayofweek(time, chartTz)` et `timestamp(chartTz, ...)`. Tuple en sortie.

2. **6 × `request.security(symbol, tf, updateOne(detectCMI(), tfMinutes, lookbackBars, interestStart, maxZones))`** : chaque TF a son contexte série isolé. `updateOne` maintient une `var array<Zone>` interne par site d'appel (state machine validation 3-bar + lookback + cleanup). **Aucune logique d'anti-chevauchement dans `updateOne`** : les zones sortent toujours ACTIVE.

3. **`intraOverlapPass` × 6** : forward iteration dans chaque array, chaque zone évaluée vs ses précédentes non-expirées. Pending mode → marque PENDING si blocker ; Replacement mode → expire les blockers, garde la nouvelle ACTIVE.

4. **`crossTfOverlapPass` × 15** (si crossTfPending) : pour chaque TF descendant, vérifier vs tous les TFs supérieurs. Downgrade ACTIVE → PENDING si overlap.

5. **L'indicateur** reçoit le tuple et appelle `renderTf` 6 fois — rendu uniquement des zones ACTIVE qui passent le filtre chart-TF ≤ zone-TF.

### Idempotence du post-pass et stabilité d'état

`intraOverlapPass` et `crossTfOverlapPass` sont **idempotents** : appelées à chaque chart-bar, elles produisent le même état stable pour un même contenu d'arrays. C'est ce qui permet la **réactivation automatique** : quand un blocker expire (cycle de vie ou période d'intérêt), `intraOverlapPass` réactive le pending (forward iteration, plus de prior non-expiré). Si un blocker cross-TF demeure, `crossTfOverlapPass` re-downgrade. Convergence en un passage.

### Cap mémoire

- `max_lines_count = max_boxes_count = max_labels_count = 500` indicateur.
- `MAX_ZONES_PER_TF = 100` par TF (cap FIFO via `zone.trimOldest`). Total mémoire : 600 zones max. Le filtre rendu (ACTIVE + chart-TF + toggle) garde les boxes dessinées bien sous 500.

### Pièges connus

- **`syminfo.timezone` est l'exchange TZ, pas le chart** — j'ai fait l'erreur en v0 ; fixé en passant `chartTz` à la lib. Même piège que Levels (cf. memory [[pine-chart-timezone]]).
- **`request.security` ne supporte pas le pattern `[1] + lookahead_on` quand le retour est `array<UDT>`** : pas de shift possible. Conséquence assumée : la barre HTF courante (encore ouverte) peut flicker en temps réel. Les zones ne se stabilisent qu'après 3 bougies HTF de validation, l'impact visuel est contenu.
- **`var array<UDT>` doit vivre DANS la lib**, pas dans l'indicateur. Sinon chaque chart-bar mute la même collection depuis 6 contextes HTF différents → état corrompu.

## Cas de test

Validation manuelle sur TradingView. À cocher au fur et à mesure.

| # | Instrument | Timeframe | Scénario | Comportement attendu | Statut |
|---|------------|-----------|----------|----------------------|--------|
| 1 | EURUSD | M15 | Chargement initial | 6 TFs évalués, Monthly/Weekly/Daily/H4 ON visibles selon historique disponible | ⏳ |
| 2 | EURUSD | M15 | Toggle H1 ON | Zones H1 apparaissent (filtre chart H1 OK car M15 ≤ H1) | ⏳ |
| 3 | EURUSD | H4 | Toggle H1 ON | H1 reste masqué (chart H4 > H1) | ⏳ |
| 4 | EURUSD | Monthly | Toggles Monthly ON, Weekly ON | Monthly toujours visible (exception). Weekly masqué (chart > Weekly) | ⏳ |
| 5 | EURUSD | H1 | CMI bull, close < open de la CMI à N+1 | Zone NON créée (validation invalidée). Pas de box | ⏳ |
| 6 | EURUSD | H1 | CMI bull, CMI bear 2 bougies après | Bull annulée, bear entame sa validation depuis sa propre barre | ⏳ |
| 7 | EURUSD | H1 | CMI bull validée (aucune invalidation) | Zone bull dessinée à la 3e bougie post-CMI. Top = lowest close, bottom = lowest low du lookback | ⏳ |
| 8 | EURUSD | H1 | Mode intra `Pending`, nouvelle zone chevauche une ACTIVE | Nouvelle marquée PENDING (non dessinée), ancienne inchangée | ⏳ |
| 9 | EURUSD | H1 | **Chaînage** : 3 CMI validées successives, toutes chevauchent en prix, mode intra `Pending` | Z1 ACTIVE, Z2 PENDING, Z3 PENDING. Z1 expire → Z2 devient ACTIVE, Z3 reste PENDING. Z2 expire → Z3 devient ACTIVE | ⏳ |
| 10 | EURUSD | H1 | Mode intra `Replacement`, nouvelle zone chevauche une ACTIVE | Ancienne expirée immédiatement, nouvelle ACTIVE | ⏳ |
| 11 | EURUSD | H1 | Mode intra `Replacement`, nouvelle chevauche 2 anciennes ACTIVE | Les 2 anciennes expirées, seule la nouvelle ACTIVE | ⏳ |
| 12 | EURUSD | H1 | **Cross-TF Pending** : zone H1 ACTIVE qui chevauche une zone D ACTIVE | Zone H1 marquée PENDING (non rendue). Zone D inchangée | ⏳ |
| 13 | EURUSD | H1 | Cross-TF Pending : zone D expire (par mort ou période d'intérêt) | Zone H1 précédemment PENDING redevient ACTIVE automatiquement | ⏳ |
| 14 | EURUSD | H1 | Cross-TF `Autorise` | La même zone H1 reste ACTIVE même si elle chevauche une zone D — superposition visuelle assumée | ⏳ |
| 15 | EURUSD | H1 | Cross-TF Pending + chevauchement avec une zone PENDING d'un TF supérieur (pas seulement ACTIVE) | La zone basse est aussi mise en attente. PENDING d'un TF supérieur compte comme blocker cross-TF | ⏳ |
| 16 | EURUSD | M15 | `Drawing CMI Zone Lookback` = 1 | Zone construite à partir de la barre CMI uniquement | ⏳ |
| 17 | EURUSD | M15 | `Drawing CMI Zone Lookback` = 5 | Fenêtre élargie : extrêmes sur 5 bougies depuis la CMI | ⏳ |
| 18 | EURUSD | M15 | Laisser tourner > 5 jours | Zones M15 plus anciennes que J-5 disparaissent automatiquement | ⏳ |
| 19 | EURUSD | H1 | Chart TZ "Europe/Paris" puis "America/New_York" | Les bornes "aujourd'hui", "lundi", "1er du mois" décalent en conséquence. M15 conservait des zones différentes selon TZ | ⏳ |
| 20 | BTCUSDT | M15 | Crypto 24/7, scénarios week-end | Validation continue, pas de pseudo-trou | ⏳ |
| 21 | ES1! | H1 | Future US, gap de session | Validation ne traverse pas le gap silencieusement | ⏳ |
| 22 | EURUSD | H1 | Bascule Mode intra Pending ↔ Replacement à chaud | Les états recalculés au prochain tick. Pas de zone fantôme | ⏳ |
| 23 | EURUSD | H1 | Bascule Cross-TF Pending ↔ Autorise à chaud | Zones cross-bloquées réapparaissent (en Autorise) ou disparaissent (en Pending) | ⏳ |
| 24 | EURUSD | M5 | Live (laisser tourner 30 min) | Flicker contenu sur la HTF courante uniquement ; zones historiques figées | ⏳ |

**Non testé / hors scope v1** :
- Comportement avec `lookbackBars > 5` (input bridé).
- Performance avec 6 TFs ON et > 5000 barres d'historique (plafond `max_bars_back`).
- Repaint live de la barre HTF courante : connu et documenté, non corrigé en v1.
