---
name: doc-feature
description: Generate or update French documentation for a Pine Script v6 (TradingView) feature in the docs/ directory. Use after completing any feature implementation (library or indicator) or when the user asks to document, write docs, or update docs for a feature.
argument-hint: "[feature-name]"
allowed-tools: "Read, Write, Edit, Grep, Glob"
---

# Feature Documentation

Generate documentation for **$ARGUMENTS** in `docs/$ARGUMENTS.md`.

## Language
Toute la documentation MUST be written in **French**.

## Required sections

### 1. Fonctionnalités
- Décrire ce que la feature fait du **point de vue utilisateur** (trader regardant son graphique TradingView).
- Lister tous les comportements et capacités.
- Inclure les edge cases et limites (timeframes supportés, instruments, etc.).

### 2. Choix d'implémentation
- Décrire les **décisions techniques** et leur justification.
- Découpage lib / indicateur : quelle logique vit dans quelle lib, pourquoi.
- Algorithmes non triviaux (detection CMI, gestion des chevauchements, anti-double-comptage, etc.).
- Trade-offs et alternatives écartées.
- Considérations de performance (calculs en `barstate.islast`, limites `max_*_count`, etc.).

### 3. Cas de test
Table des cas à valider **manuellement** dans TradingView (Pine n'a pas de framework de tests auto) :

| # | Instrument | Timeframe | Scénario | Comportement attendu | Statut |
|---|------------|-----------|----------|----------------------|--------|
| 1 | EURUSD | H1 | Bougie verte forte après SMA descendante | CMI bullish détectée et zone construite | ✅ / ❌ / ⏳ |

- Inclure au moins un cas par règle métier décrite dans les specs.
- Couvrir des instruments cash ET non-cash si la feature les distingue.
- Couvrir plusieurs timeframes si la feature est multi-TF.
- Préciser ce qui n'est PAS testé et pourquoi (limite plateforme, scénario rare, etc.).

## Processus

1. Lire `docs/SPECIFICATIONS.md` (section concernée par la feature).
2. Lire l'implémentation Pine (libs utilisées + indicateur).
3. Générer le doc en suivant le template ci-dessus.
4. Si le comportement implémenté diverge des specs, signaler l'écart à l'utilisateur AVANT d'écrire le doc et proposer `/update-specs`.
