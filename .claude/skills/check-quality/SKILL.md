---
name: check-quality
description: Audit Pine Script v6 (TradingView) files for project conventions, indentation rules, na handling, repaint risks, performance, and lib/indicator separation. Use when the user asks to review code quality, check conventions, audit a Pine file, or verify project standards.
argument-hint: "[file-or-scope]"
allowed-tools: "Read, Grep, Glob"
---

# Pine Quality Check (TradingView / Pine v6)

Audit du / des fichiers Pine v6 pour conformité aux conventions projet ET aux contraintes plateforme TradingView.
Cible : **$ARGUMENTS** (par défaut : tous les `.pine` dans `tradingview/`).

Référence : voir la section "Contraintes TradingView / Pine Script v6" du `CLAUDE.md` racine pour les règles complètes.

## Checks

### 1. Versioning et déclaration
- Première ligne : `//@version=6` (refuser v5 ou inférieur).
- Une seule déclaration `library(...)` OU `indicator(...)` par fichier.
- `library()` UNIQUEMENT dans les fichiers de `tradingview/libraries/` ; le nom déclaré doit être `lib_X` (ex. `library("lib_price")`).
- `indicator()` UNIQUEMENT dans les fichiers `tradingview/*.pine` (hors `libraries/`).

### 2. Séparation lib / indicateur
- **Libs** ne contiennent PAS :
  - `input.*` (les libs ne déclarent pas de réglages utilisateur)
  - `plot()`, `plotshape()`, `bgcolor()` (les libs ne dessinent pas directement leur propre output ; elles exposent des fonctions que l'indicateur appelle)
  - Logique métier propre à un indicateur précis
- **Indicateurs** ne contiennent PAS :
  - Calculs lourds dupliqués qui devraient vivre dans une lib
  - Constantes magiques (extraire en `input.*` ou en constantes lib)

### 3. Naming conventions
- Variables et fonctions : `camelCase`
- Types / UDT : `PascalCase`
- Constantes : `UPPER_SNAKE_CASE`
- Identifiants et commentaires : **anglais**
- Fichiers : kebab-case. Libs : `tradingview/libraries/price.pine` (sans préfixe `lib-`). Indicateurs : `tradingview/zones-MTF.pine`, etc.

### 4. Inputs
- Tout `input.*` a un `title` explicite et un `group=` non vide.
- Les `group=` MATCHENT les sections de `docs/SPECIFICATIONS.md` pour cet indicateur.
- Les valeurs par défaut MATCHENT les colonnes "Défaut" des tableaux de specs.
- Les plages `minval` / `maxval` MATCHENT les colonnes "Plage" des specs.

### 5. Indentation (piège classique Pine)
- **Blocs locaux** (corps de fonction, `if`, `for`, `while`, `switch`) : 4 espaces OU 1 tab (conventions projet : **4 espaces uniquement**, jamais de tabs).
- **Continuations de ligne HORS parenthèses** : indentation = N espaces NON multiple de 4 (typiquement 2 ou 6). **Aucun tab autorisé** ici (interprété comme nouveau bloc local).
- **Continuations de ligne DANS parenthèses** : v6 autorise n'importe quelle indentation. Préférer 4 espaces pour cohérence.
- **Aucun mélange tabs/espaces** dans un même fichier. Signaler tout tab trouvé : `\t` dans un `.pine` → FAIL.
- Si une erreur de compilation est signalée sur un `if`/`for`/multi-ligne, vérifier l'indentation en priorité.

### 6. Repaint et `request.security`
- Tout appel à `request.security(...)` doit être audité pour le risque de repaint :
  - Sans `[1]` sur la série lue ET sans `lookahead=barmerge.lookahead_off` → **WARN/FAIL** sauf justification commentée dans le code.
  - `lookahead=barmerge.lookahead_on` SANS `[1]` sur la série lue = lecture du futur = **FAIL** en production.
  - Combinaison sûre standard : `request.security(sym, tf, close[1], lookahead=barmerge.lookahead_on)`.
- Tout `request.security` doit avoir un commentaire qui justifie le choix de `lookahead` / `gaps`.

### 7. `var` / `varip`
- `varip` doit être justifié par un commentaire (cumuls intra-tick uniquement). Sinon → WARN.
- Pas de `varip` dans une lib (effet de bord intra-tick + divergence historique/live).

### 8. Limites d'objets graphiques
- Tout indicateur qui crée des `line`/`box`/`label` doit déclarer `max_lines_count`, `max_boxes_count`, `max_labels_count` dans son `indicator(...)`.
- Tout création non bornée d'objets graphiques (boucle sans `delete` ni lookback configurable) → **WARN**.
- Préférer `line.set_*()` / `box.set_*()` plutôt que `delete` + `new` à chaque barre quand l'objet existe déjà.

### 9. Gestion du `na`
- Toute valeur issue de `request.security`, d'un array, ou d'un calcul conditionnel doit être protégée par `na(x) ? ... : ...` ou `nz(x, default)` avant utilisation arithmétique ou de rendu.
- Pas de comparaison `x == na` (utiliser `na(x)`).

### 10. Performance
- Les calculs lourds (boucles, accès historique long, parcours d'arrays) ne doivent pas s'exécuter à chaque barre s'ils ne servent qu'à l'affichage final → encapsuler dans `if barstate.islast`.
- Toute logique qui ne doit valider qu'à la clôture (détection CMI, validation N bougies) doit explicitement utiliser `barstate.isconfirmed` ou s'exécuter sur barre fermée.
- Accès historique : vérifier que les `série[N]` restent dans la limite `max_bars_back` (5000 par défaut). Si N approche cette limite, déclarer `max_bars_back=5000` dans `indicator(...)`.

### 11. Cohérence avec les specs
- Pour chaque indicateur : tous les settings listés dans `docs/SPECIFICATIONS.md` ont un `input.*` correspondant dans le code, et inversement.
- Les règles de timeframe (ex : "PDH/PDL n'apparaît pas sur weekly") sont implémentées via `timeframe.in_seconds(...)` ou équivalent.
- Les défauts dans les tableaux de specs MATCHENT les `defval=` du code.

## Output

Pour chaque finding, indiquer :
- Niveau : **PASS** / **WARN** / **FAIL**
- Fichier:ligne
- Convention violée
- Suggestion de correction

À la fin : résumé court par fichier (`layout.pine : 2 WARN, 0 FAIL`).

Ne PAS modifier le code automatiquement — c'est un audit, pas un fix.
