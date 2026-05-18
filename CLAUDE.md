# 2Ai Indicators

## Project Overview
Suite d'indicateurs TradingView (Pine Script v6) bâtis sur des bibliothèques partagées.
Portage prévu ensuite vers cTrader, MetaTrader, PRT, etc.
Specs complètes dans `docs/SPECIFICATIONS.md`.

## Quick Reference
- **Langage**: Pine Script v6 (`//@version=6`)
- **Plateforme actuelle**: TradingView
- **Cibles futures**: cTrader, MetaTrader (MQL5), ProRealTime
- **Code source**: `tradingview/*.pine`
- **Specs**: `docs/SPECIFICATIONS.md` (référence unique du comportement attendu)
- **Documentation feature**: `docs/<feature>.md` (français)

## Architecture

Découpage en **3 couches**. Objectif : qu'une stratégie (à venir) puisse importer un signal pur (ex : CMI) **sans embarquer une ligne de code de dessin**.

```
indicators/
├── tradingview/
│   │  --- Couche 0 : fondations (types & utils) ---
│   ├── lib-time.pine        # Timezones, isNewDay, sessions, sessionStart/End, isFirstH1OfDay
│   ├── lib-market.pine      # Détection cashEU / cashUS / cashAsia / nonCash         (dep: lib-time)
│   ├── lib-zone.pine        # UDT Zone, overlap, contains, expired, helpers FIFO
│   │
│   │  --- Couche 1 : signaux & calculs purs (importable par les stratégies) ---
│   ├── lib-ma.pine          # SMA, slope%, isFlatSeries, ribbon (SMA ± std)
│   ├── lib-bollinger.pine   # BB inner/outer, bandState (open/flat/closing)         (dep: lib-ma)
│   ├── lib-ichimoku.pine    # Tenkan / Kijun / Senkou A&B / Chikou
│   ├── lib-supertrend.pine  # Line + dir normalisée ±1
│   ├── lib-cmi.pine         # Signal bull/bear, validation 3 bougies, construit Zone (dep: lib-zone)
│   ├── lib-fvg.pine         # Signal, niveaux, comblement, retourne une Zone        (dep: lib-zone)
│   ├── lib-gap.pine         # Détection gap daily + cycle de vie                    (dep: lib-time)
│   ├── lib-levels.pine      # PDH/PDL/PWH/PWL/PMH/PML/ATH, Opens, IBR               (dep: lib-time)
│   │
│   │  --- Couche 2 : rendering (importée uniquement par les indicateurs) ---
│   ├── lib-draw.pine        # Palette, styles, withAlpha, drawLevel, drawBox, fillCloud, FIFO
│   ├── lib-zone-draw.pine   # renderZone(Zone, style)                               (dep: lib-zone, lib-draw)
│   │
│   │  --- Indicateurs ---
│   ├── layout.pine          # 2Ai Layout
│   ├── levels.pine          # 2Ai Levels
│   ├── zones.pine           # 2Ai Zones
│   ├── zones-MTF.pine       # 2Ai Zones MTF
│   └── *.pine               # Tout autre 2Ai XXX
├── docs/
│   ├── SPECIFICATIONS.md    # Référence du comportement attendu
│   ├── LIBS.md              # Tableau des versions publiées de chaque lib
│   └── <feature>.md         # Doc par feature (français)
└── .claude/skills/          # Skills invocables
```

### Règle d'or — isolation des couches

1. **Une lib de Couche 0 ou 1 ne référence JAMAIS** : `line.new`, `box.new`, `label.new`, `polyline.new`, `plot*`, `fill`, `bgcolor`, `hline`, ni de littéral `color.*`. Tout ce qui dessine est interdit. Vérifiable par grep.
2. **Une lib de Couche 0 ou 1 ne lit JAMAIS d'`input.*`** — l'indicateur (ou la stratégie) passe tout en paramètre.
3. **Une lib de Couche 2** n'a **aucune logique métier** : elle dessine ce qu'on lui passe, point.
4. **Indicateurs** : importent ce qu'il faut, déclarent les inputs, orchestrent. Aucune logique de calcul dupliquée.
5. **UDT** : créés seulement quand on persiste un état (`Zone`, `Gap`, `PendingZone`). Pour des valeurs de barre : tuples.
6. Une lib **peut** importer une autre lib de la même couche ou de la couche inférieure ; jamais d'une couche supérieure.

### Versioning des libs
- Chaque lib a sa version publiée TradingView indépendante (`import <publisher>/<libName>/<version>`).
- Quand une lib bump, propager le `/<version>` dans **toutes** les libs/indicateurs qui l'importent.
- Le tableau de versions vit dans `docs/LIBS.md` (créé au premier publish).

## Conventions
- Code et identifiants : **anglais** (variables, fonctions, types, commentaires Pine)
- Documentation : **français** (`docs/`)
- Commits : **anglais**, conventional (`feat:`, `fix:`, `refactor:`, `docs:`, `chore:`)
- Commits : pas de `Co-Authored-By`, pas de mention auteur AI
- Naming : `camelCase` (vars/fonctions), `PascalCase` (types/UDT), `UPPER_SNAKE_CASE` (constantes)
- Inputs Pine : regroupés par `group=` cohérent avec les sections de SPECIFICATIONS.md

## Méthodologie — Spec-driven
- **SPECIFICATIONS.md est la source de vérité.** Toute feature commence par lire la section concernée.
- Si le comportement attendu n'est pas dans les specs, on les met à jour AVANT d'implémenter.
- Cycle : specs (alignement) → implémentation (lib puis indicateur) → cas de test → doc.
- Pas de framework de test auto pour Pine. À la place : chaque feature produit une **liste de cas de test** (instruments, timeframes, scénarios) validés manuellement dans TradingView, conservée dans `docs/<feature>.md`.

## Documentation — Systématique
- Chaque feature produit ou met à jour un doc dans `docs/` (français)
- La doc est livrée DANS le même flow que l'implémentation, pas après
- Si une feature modifie un comportement déjà documenté dans SPECIFICATIONS.md, on met à jour les specs aussi
- Format des docs feature : Fonctionnalités, Choix d'implémentation, Cas de test (table)

## Git Workflow
- Jamais de commit, push ou opération git destructive sans demande explicite de l'utilisateur
- L'utilisateur contrôle toutes les opérations git

## Règles d'architecture Pine
- Les calculs lourds (boucles, `request.security`, parcours d'arrays) doivent être confinés ; éviter de les exécuter à chaque barre quand ce n'est pas nécessaire
- Les opérations d'affichage coûteuses (création de lignes/boxes globales) souvent restreintes à `barstate.islast`
- Gestion explicite de `na` partout où une valeur peut être absente
- Pas de constantes magiques dans le code des indicateurs : passer par des `input.*` ou des constantes nommées dans une lib
- Une fonction lib doit être pure quand possible (entrées → sortie, pas d'effet de bord global)

## Contraintes TradingView / Pine Script v6
Contraintes plateforme qui dictent une bonne partie des choix d'implémentation. À garder en tête en permanence — un indicateur qui ignore ces contraintes plante en runtime ou repaint silencieusement.

### Limites d'objets graphiques
- `max_lines_count`, `max_boxes_count`, `max_labels_count` : plafond **500** chacun ; `max_polylines_count` plafond **100**. Au-delà, les plus anciens sont supprimés en FIFO (perte silencieuse !).
- Déclarer la limite voulue dans `indicator(..., max_boxes_count=500, max_lines_count=500, max_labels_count=500)` sinon le défaut (~50) est très bas.
- Pour les indicateurs riches (Levels, Zones MTF), prévoir un **lookback configurable** côté input pour borner le nombre d'objets vivants.

### Historique de barres
- `max_bars_back` : par défaut Pine auto-détecte ; au-delà de **5000 barres**, indexer `série[N]` lève une erreur. Forcer explicitement si nécessaire via `indicator(..., max_bars_back=5000)`.
- Toute fenêtre de lookback (CMI lookback, zones, etc.) doit rester compatible avec ce plafond.

### `request.security()` et repaint
- Par défaut, `request.security()` peut **repaint** sur les TF supérieurs (la valeur change rétroactivement quand la barre HTF se ferme).
- Règle : pour des valeurs "historiques fiables", utiliser `request.security(sym, tf, close[1], lookahead=barmerge.lookahead_on)` (technique standard), OU travailler sur la barre déjà fermée.
- `barmerge.lookahead_on` SANS `[1]` = lecture du futur = interdit en production.
- Toujours préciser et commenter le choix de `lookahead` / `gaps` dans le code.

### Real-time vs historique
- Un script s'exécute **une fois par barre fermée** en historique, et **à chaque tick** en live.
- `barstate.isconfirmed` : barre fermée (vrai en historique, vrai uniquement à la clôture en live).
- `barstate.islast` : dernière barre du dataset (la "live bar" en temps réel).
- `barstate.isrealtime` / `ishistory` : pour brancher selon le contexte.
- Toute logique qui ne doit s'exécuter qu'à la clôture (détection CMI, validation 3 bougies, etc.) doit le faire explicitement.

### `var` / `varip`
- `var x = ...` : initialisé une seule fois, persiste entre barres, **se reset** au rollback intrabar (cohérent historique/live).
- `varip x = ...` : NE se reset PAS au rollback — utile uniquement pour cumuls intra-tick (volumes, ticks). Diverge entre historique et live, **à éviter** sauf cas explicite.

### Indentation
Pine v6 a trois cas d'indentation distincts. Ne pas les confondre :

- **Bloc local** (corps de fonction, `if`, `for`, `while`, `switch`) : indentation = **4 espaces OU 1 tab**. Les deux sont valides. Pas 8 espaces, pas 2 tabs.
- **Continuation de ligne HORS parenthèses** (expression longue cassée sur plusieurs lignes, sans `(` ouvrant) : indentation = **un nombre d'espaces qui n'est PAS un multiple de 4** (typiquement 2 ou 6). **Tab interdit ici** — un tab serait interprété comme un nouveau bloc local.
- **Continuation de ligne DANS parenthèses** (appel de fonction ou déclaration de params éclaté sur plusieurs lignes) : indentation **libre** en v6 (zéro ou plus d'espaces, multiples de 4 autorisés).

**Convention projet** : utiliser **uniquement des espaces** dans tous les `.pine` (4 espaces pour les blocs, 2 espaces pour les continuations hors parenthèses). Pas de tabs du tout, pas de mélange. Configurer l'éditeur : `"insertSpaces": true`, `"tabSize": 4` pour `*.pine`.

En cas d'erreur de compilation inexpliquée sur un `if`/`for` ou une expression multi-ligne, vérifier en premier l'indentation (rendre les whitespaces visibles).

### Sessions et timezones
- Pas de timezone système : toutes les fonctions horaires acceptent une timezone en string (`"Europe/Paris"`, `"America/New_York"`, `"Asia/Tokyo"`).
- DST : géré automatiquement par les noms IANA, **pas** par les offsets `"GMT+1"`.
- Toujours préciser la timezone explicitement (jamais de `time()` sans timezone si la session compte).

### Plateforme
- Pas de stockage persistant (pas de DB, pas de fichiers).
- Pas d'appels réseau (uniquement `request.*`).
- Les libs sont **publiées sur TradingView** (au moins en privé) avant d'être importables : `import <publisher>/<libName>/<version> as alias`.
- Une modification de lib = nouvelle version à publier = bump du `/<version>` dans tous les indicateurs qui l'importent.

### Portage futur
Les futures cibles (cTrader, MetaTrader, PRT) n'ont **pas** d'équivalent direct des `box`/`line`/`label` Pine ni du modèle de série temporelle Pine. Le portage = réécriture, pas conversion auto. C'est précisément pour ça que toute la logique métier vit dans les libs : pour avoir une "spec exécutable" claire à porter.

## Skills
- `/new-library` — scaffold d'une nouvelle `lib-xxx.pine`
- `/new-indicator` — scaffold d'un nouvel indicateur 2Ai
- `/doc-feature` — génère/maj doc française d'une feature dans `docs/`
- `/update-specs` — met à jour `docs/SPECIFICATIONS.md` quand un comportement change
- `/check-quality` — audit Pine : conventions, gestion `na`, perf, séparation lib/indicateur

Si l'utilisateur demande quelque chose qui mériterait un skill réutilisable, proposer de le créer.
