---
name: update-specs
description: Update docs/SPECIFICATIONS.md (Pine Script v6 / TradingView indicators) when an indicator's behavior changes, is added, or is removed. Use when the user asks to update specs, add a new indicator section, modify default values, or document a behavior change.
argument-hint: "[indicator-or-section]"
allowed-tools: "Read, Write, Edit, Grep, Glob"
---

# Update SPECIFICATIONS.md

Met à jour `docs/SPECIFICATIONS.md` pour refléter un changement de comportement, un nouvel indicateur, ou une nouvelle règle.
Cible : **$ARGUMENTS**.

## Principes

- **SPECIFICATIONS.md est la source de vérité.** Le code doit suivre les specs, jamais l'inverse.
- Une feature dont les specs ne sont pas à jour est considérée comme **incomplète**.
- Les specs sont en **français**.

## Format à respecter

Le document a une structure stable qu'il faut conserver :

1. En-tête : description courte du projet.
2. Section **Architecture** : arbre des fichiers + dépendances.
3. Pour chaque indicateur, un bloc `## Indicateur N : 2Ai <Nom> (\`fichier.pine\`)` avec :
   - Description courte
   - Sous-sections numérotées (`### 1. <Feature>`, `### 2. <Feature>`, …)
   - Règles de calcul et de rendu en prose
   - **Tableaux de settings** avec colonnes : `Setting | Type | Défaut | Plage`
   - Conditions d'affichage par timeframe quand pertinent

## Steps

1. **Identifier la zone à modifier**
   - Si c'est un nouvel indicateur : ajouter une section `## Indicateur N : ...` à la fin, après le dernier indicateur existant.
   - Si c'est une feature dans un indicateur existant : trouver la sous-section correspondante.
   - Si c'est une nouvelle règle transversale : la placer là où elle est cohérente (ex : règle de timeframe).

2. **Lire la zone existante** avant de modifier — préserver le ton, le niveau de détail et le formatage des tableaux.

3. **Modifier de manière minimale**
   - Ne pas réécrire des sections inchangées.
   - Si une valeur par défaut change, modifier UNIQUEMENT la cellule du tableau concernée.
   - Si une règle est ajoutée, l'insérer au bon endroit dans la prose, pas en bloc séparé incohérent.

4. **Tableaux de settings**
   - Tous les settings exposés à l'utilisateur (inputs Pine) doivent apparaître dans un tableau.
   - Colonnes minimales : `Setting | Type | Défaut`. Ajouter `Plage` pour les numériques.
   - Les noms de settings dans les tableaux doivent **matcher** les labels Pine du fichier `.pine`.

5. **Synchroniser**
   - Après modification, vérifier que :
     - Le code Pine reflète les specs (valeurs par défaut, plages, libellés)
     - Le doc de feature `docs/<feature>.md` est cohérent (sinon proposer `/doc-feature`)

6. **Ne PAS toucher** aux indicateurs/sections non concernés par le changement.

## Edge cases

- Si la modification demandée contredit une règle existante, signaler le conflit à l'utilisateur avant d'éditer.
- Si la modification implique un nouveau type d'objet graphique (ex : labels persistants, tableaux), proposer d'enrichir la section "Architecture" et éventuellement `CLAUDE.md`.
