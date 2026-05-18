---
name: new-indicator
description: Scaffold a new 2Ai indicator (Pine Script v6 for TradingView). Use when the user asks to create, add, or scaffold a new indicator, study, or chart overlay for the indicators project.
argument-hint: "[indicator-name]"
allowed-tools: "Read, Write, Edit, Grep, Glob"
---

# New 2Ai Indicator Scaffold (TradingView / Pine v6)

Create the Pine Script v6 indicator **$ARGUMENTS.pine** in `tradingview/`.

Référence des contraintes plateforme : voir la section "Contraintes TradingView / Pine Script v6" du `CLAUDE.md` racine.

## Steps

1. **Check naming**
   - File: kebab-case, e.g. `layout.pine`, `levels.pine`, `zones-MTF.pine`.
   - Display name on TradingView: `"2Ai <Name>"`.

2. **Read SPECIFICATIONS.md**
   - Locate (or create with `/update-specs` first) the section describing this indicator.
   - Identify required libraries, settings, defaults, calculation rules, drawing rules.
   - If the indicator isn't specified yet, STOP and ask the user to write the spec first.

3. **Create the file** in `tradingview/$ARGUMENTS.pine` with this skeleton:
   ```pinescript
   //@version=6
   // @description 2Ai <Name> — <one-line French description>
   indicator("2Ai <Name>", shorttitle = "2Ai <Short>", overlay = true, max_lines_count = 500, max_boxes_count = 500, max_labels_count = 500)

   // ============================================================
   // Imports
   // ============================================================
   // import <publisher>/lib_price/<n> as price
   // import <publisher>/lib_hours/<n> as hours
   // import <publisher>/lib_drawing/<n> as drawing

   // ============================================================
   // Inputs — <section 1 from specs>
   // ============================================================
   showFoo  = input.bool(true,   "Foo",         group = "<Section 1>")
   fooColor = input.color(color.gray, "Couleur", group = "<Section 1>")

   // ============================================================
   // Inputs — <section 2 from specs>
   // ============================================================

   // ============================================================
   // Calculs (déléguer aux libs autant que possible)
   // ============================================================

   // ============================================================
   // Rendu
   // ============================================================
   ```

4. **Conventions to enforce**
   - Input `group=` labels MUST match the section names from SPECIFICATIONS.md so settings appear in the same order as the spec.
   - Default values MUST match the "Défaut" columns in the spec tables.
   - All calculations delegated to libs whenever possible — the indicator file orchestrates, libs compute.
   - Heavy drawing (lines/boxes that don't need to update every bar) inside `if barstate.islast` blocks.
   - Explicit `na` handling.
   - **Indentation : 4 espaces uniquement** (jamais de tabs). Continuations hors parenthèses : 2 espaces.

5. **Contraintes TradingView à anticiper**
   - **Limites d'objets graphiques** : déclarer `max_lines_count=500`, `max_boxes_count=500`, `max_labels_count=500` dans `indicator(...)` dès qu'on crée des `line`/`box`/`label`. Au-delà de 500, suppression FIFO silencieuse.
   - **Lookback configurable** : si l'indicateur peut produire beaucoup d'objets historiques (gaps, zones, niveaux), exposer un input "Historique (lookback)" pour borner.
   - **`request.security`** : toujours commenter le choix `lookahead` / `gaps`. Combinaison anti-repaint standard : `request.security(sym, tf, close[1], lookahead=barmerge.lookahead_on)`.
   - **Timezones explicites** : pour toute session/horaire, utiliser des noms IANA (`"Europe/Paris"`), jamais d'offsets `"GMT+1"` (cassé en DST).
   - **`barstate.isconfirmed`** : pour toute logique qui doit valider à la clôture seulement (détection CMI, validation N bougies).

6. **After scaffolding**
   - List the cas de test (instruments, timeframes, scénarios) the user should run in TradingView to validate :
     - **Historique** : recharger le chart, vérifier le rendu sur barres fermées.
     - **Temps réel** : laisser tourner pendant N bougies pour détecter tout repaint ou divergence historique/live.
     - Couvrir au moins un cash (EU/US) ET un non-cash (future/crypto) si l'indicateur les distingue.
     - Couvrir plusieurs timeframes (M5, M15, H1, H4, D, W) si l'indicateur est multi-TF.
   - Remind the user to invoke `/doc-feature $ARGUMENTS` to produce the French doc once the implementation passes manual validation.
