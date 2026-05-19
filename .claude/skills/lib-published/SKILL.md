---
name: lib-published
description: Notifie le projet qu'une lib Pine vient d'être (re)publiée sur TradingView. Met à jour docs/LIBS.md, active/bumpe l'import dans tous les consommateurs (libs et indicateurs), signale les libs dépendantes à republier. À invoquer dès que TradingView assigne une version à une lib, OU quand l'utilisateur dit en langage naturel « lib_X est publiée en vN ».
argument-hint: "<lib_name> <version>"
allowed-tools: "Read, Edit, Grep, Glob, Bash"
---

# Lib Published — Track version + propagate imports

L'utilisateur a publié (ou bumpé) une lib sur TradingView. Arguments : `$ARGUMENTS` = `<lib_name> <version>`.

**Exemples d'invocation** :
- `/lib-published lib_bollinger 1` (publish initial)
- `/lib-published lib_ma 2` (bump)
- `/lib-published lib-zone 3` (kebab-case accepté, normalisé)

## Steps

### 1. Parser les arguments
- Premier arg = nom de la lib. Accepter `lib_x` ou `lib-x` ; normaliser :
  - **Nom kebab** (pour `docs/LIBS.md`) : `lib-x`
  - **Nom snake** (pour les `import` Pine) : `lib_x`
- Deuxième arg = version (integer ≥ 1).
- Si l'un manque ou est invalide → demander à l'utilisateur.

### 2. Lire l'état actuel
- Lire `docs/LIBS.md` pour récupérer :
  - Le publisher (sous la section `## Publisher`)
  - La ligne actuelle de la lib (notamment la version courante si elle existe, et l'alias)
- Date du jour : utiliser la valeur `currentDate` du contexte system (`Today's date is YYYY-MM-DD`).

### 3. Mettre à jour la table dans `docs/LIBS.md`
- Ligne `| \`lib-x\` | <couche> | <version actuelle> | <date> | <deps> | <notes> |`
  - Remplacer `_non publié_` (ou l'ancienne version) par `**\`<nouvelle>\`**`
  - Mettre à jour la date

### 4. Mettre à jour la section "Imports actifs" de `docs/LIBS.md`
- Si la lib n'y était pas → ajouter une ligne `import <publisher>/lib_x/<version> as <alias>` (alias = colonne d'alias)
- Sinon → bumper le `/<version>` dans la ligne existante

### 5. Propager dans le code Pine
Grep dans `tradingview/` :
```bash
grep -rnE "import\s+\S+/lib_x/[0-9]+" tradingview/      # imports actifs (à bumper)
grep -rnE "//\s*import\s+\S+/lib_x/" tradingview/        # imports commentés (à activer)
```
Pour chaque match :
- **Import actif (bump)** : remplacer `lib_x/<ancien>` par `lib_x/<nouveau>`
- **Import commenté (activation)** :
  - Décommenter la ligne (supprimer les `//`)
  - Remplacer `<publisher>` par le publisher réel (lu depuis LIBS.md)
  - Remplacer `<n>` ou `1` par `<nouvelle version>`

### 6. Signaler les libs dépendantes à republier
- Identifier les libs qui importent `lib_x` (via grep dans `tradingview/lib-*.pine`).
- Pour chaque lib dépendante DÉJÀ publiée (donc présente dans "Imports actifs"), prévenir l'utilisateur :
  > « `lib_y` consomme `lib_x` et est déjà publiée en v<N>. Il faut la republier (elle deviendra v<N+1>) pour que ses utilisateurs prennent en compte le bump. »

### 7. Rapporter à l'utilisateur
Sortie attendue, format markdown court :

```
## /lib-published lib_x v<version>

✅ `docs/LIBS.md` mis à jour (table + imports actifs)
✅ <N> fichier(s) Pine mis à jour :
   - tradingview/foo.pine : import bumpé/activé
   - tradingview/lib-bar.pine : import bumpé/activé

⚠️ Libs dépendantes déjà publiées à republier (sinon stack désynchronisée) :
   - lib_bar v<old> → republier
   (ou : « (aucune) »)

🔁 Prochaine étape suggérée : <instruction concrète, ex. « tu peux recharger layout.pine sur ton chart »>
```

## Anti-patterns à éviter
- Ne **jamais** inventer un publisher ou une version. Si LIBS.md n'a pas le publisher renseigné, demander à l'utilisateur.
- Ne **pas** propager dans les fichiers `docs/` (sauf LIBS.md) ni dans `.claude/`.
- Ne **pas** toucher les fichiers `tradingview/lib-x.pine` (la lib elle-même) — sa version sur disque ≠ sa version publiée, par construction.
