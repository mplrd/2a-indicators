---
name: new-library
description: Scaffold a new Pine Script v6 shared library for TradingView (lib-xxx.pine). Use when the user asks to create, add, or scaffold a new shared library, helper module, or domain library for the indicators project.
argument-hint: "[lib-name]"
allowed-tools: "Read, Write, Edit, Grep, Glob"
---

# New Pine v6 Library Scaffold (TradingView)

Create the Pine Script v6 library **lib-$ARGUMENTS.pine** in `tradingview/`.

Référence des contraintes plateforme : voir la section "Contraintes TradingView / Pine Script v6" du `CLAUDE.md` racine.

## Steps

1. **Check naming**
   - File name MUST start with `lib-` and use kebab-case: `lib-price.pine`, `lib-hours.pine`, `lib-drawing.pine`, etc.
   - The name should represent a clear **business domain** (price, hours, drawing, session, level…), not an indicator name.

2. **Read SPECIFICATIONS.md**
   - Identify which calculations or primitives belong to this domain.
   - If the domain isn't clearly defined in specs, ask the user before scaffolding.

3. **Create the file** in `tradingview/lib-$ARGUMENTS.pine` with this skeleton:
   ```pinescript
   //@version=6
   // @description <one-line French description of the library's purpose>
   library("lib_$ARGUMENTS", overlay = true)

   // ============================================================
   // <Section name>
   // ============================================================

   // @function <short English description>
   // @param <name> <type> <description>
   // @returns <type> <description>
   export <funcName>(<params>) =>
       // implementation
   ```

4. **Conventions to enforce**
   - Pure functions when possible: inputs → output, no global side effects.
   - Explicit `na` handling for any value that may be absent.
   - `camelCase` for functions and params.
   - Group exports by section with a comment banner.
   - English identifiers and inline comments.
   - **Indentation : 4 espaces uniquement** (jamais de tabs). Continuations hors parenthèses : 2 espaces.
   - Pas de `varip` dans une lib (effet de bord intra-tick, divergence historique/live).

5. **Do NOT** put indicator logic, `input.*` declarations, or `plot()`/`bgcolor()`/etc. calls in a library. Libraries expose reusable building blocks; indicators orchestrate them.

6. **request.security** : si la lib expose une fonction qui appelle `request.security`, **toujours** documenter le choix de `lookahead` / `gaps` et la combinaison anti-repaint utilisée.

7. **After scaffolding**
   - Remind the user that the lib must be **publiée sur TradingView** (au moins en privé) avant qu'un indicateur puisse l'importer via `import publisher/libName/version as alias`.
   - Chaque modification publique de la lib = bump de version = à propager dans tous les indicateurs qui l'importent.
   - Suggest adding an entry to the Architecture section of `CLAUDE.md` if the library is a new top-level domain.
