---
name: delivery-plan
description: Produit la checklist de livraison TradingView (publish/bump des libs, propagation des versions d'import, validation chart) pour les changements de la branche courante par rapport à une branche de base (défaut develop). À INVOQUER systématiquement avant toute proposition de merge ou de PR vers develop/main.
argument-hint: "[base-branch]"
allowed-tools: "Read, Grep, Glob, Bash"
---

# Delivery Plan — TradingView Publishing Checklist

Génère la procédure de livraison TradingView pour les libs et indicateurs modifiés/ajoutés sur la branche courante vs **$ARGUMENTS** (défaut : `develop`).

**À invoquer AVANT toute proposition de merge / PR** (cf. règle dans `CLAUDE.md` § Git Workflow).

## Steps

### 1. Identifier les changements

```bash
BASE="${ARGUMENTS:-develop}"
git diff --name-status "$BASE"...HEAD -- tradingview/ docs/LIBS.md
```

Classer les fichiers en trois catégories :
- **Libs** : `tradingview/lib-*.pine`
- **Indicateurs** : autres `tradingview/*.pine`
- **Docs / Config** : tout le reste

Pour chaque fichier, noter le statut : `A` (ajouté), `M` (modifié), `D` (supprimé).

### 2. Pour chaque lib touchée

Établir :
- **Couche** (0 / 1 / 2) en se référant à l'Architecture de `CLAUDE.md`.
- **Dépendances directes** : `grep -nE '^//?\s*import .*/lib_[a-z]+/' tradingview/lib-X.pine`. Lister les libs importées (même commentées).
- **Version courante** : lire la ligne correspondante dans `docs/LIBS.md`.
- **Action requise** :
  - Statut `A` ou version `_non publié_` → **publish initial**
  - Statut `M` et version existante → **bump v(N) → v(N+1)**

### 3. Ordre topologique de publication

Une lib ne peut être publiée que **après** que toutes ses dépendances l'aient été (et soient à la bonne version). Ordre global :
1. Couche 0 sans dep (`lib_time`, `lib_zone`, `lib_series`) — en parallèle
2. Couche 0 avec deps (`lib_market` après `lib_time`)
3. Couche 1 sans dep (`lib_bollinger`, `lib_ichimoku`, `lib_supertrend`, `lib_fvg` après `lib_zone`…)
4. Couche 1 avec deps internes (`lib_ma` après `lib_bollinger`, `lib_cmi` après `lib_zone` + `lib_series`, etc.)
5. Couche 2 (`lib_draw`, puis `lib_zone_draw`)

### 4. Identifier la propagation des versions d'import

Pour chaque lib bumpée, lister **tous** ses consommateurs :
```bash
grep -rnE "import\s+\S+/lib_X/[0-9]+" tradingview/
```
Pour chaque match, indiquer le remplacement à effectuer : `lib_X/<ancienne>` → `lib_X/<nouvelle>`.

Inclure aussi bien les autres libs (qui devront re-publier) que les indicateurs (qui devront être rechargés sur le chart).

### 5. Audit d'isolation des couches

Avant de proposer la livraison, vérifier qu'aucune lib Couche 0/1 ne viole les règles :
```bash
grep -nE '\b(plot|plotshape|fill|bgcolor|hline|line\.new|box\.new|label\.new|polyline\.new|input\.|color\.(red|green|blue|black|white|yellow|aqua|orange|gray|purple|olive|teal|navy|silver|maroon|lime|fuchsia))\b' \
  tradingview/lib-{time,market,zone,series,ma,bollinger,ichimoku,supertrend,cmi,fvg,gap,levels}.pine 2>/dev/null
```
- Tolérer les matches dans des commentaires (mention pédagogique).
- Tout match dans du code exécutable d'une lib Couche 0/1 = **BLOQUANT**.

Vérifier aussi qu'aucune lib Couche 2 ne contienne de logique métier (calculs sur séries, état persistant non purement visuel).

### 6. Construire la checklist Markdown

Format attendu de la sortie présentée au user :

```markdown
# Delivery Plan — <branche> → <base>

## 🚨 Bloquants
<liste, ou "(aucun)">

## 📦 À publier sur TradingView (ordre topologique)

### Étape 1 — `lib_X` (Couche N) — <publish initial | bump v(K) → v(K+1)>
**Prérequis** : <deps publiées et OK, ou liste à publier avant>
1. Pine Editor → coller `tradingview/lib-X.pine`
2. Save → "Publish library" → visibilité <Private/Public>
3. Noter la version assignée : `___`
4. Mettre à jour `docs/LIBS.md` ligne `lib-X` : version + date

### Étape 2 — ...

## 🔁 Propagation des versions d'import

Après publication des libs ci-dessus, mettre à jour :
- `tradingview/lib-Y.pine` : `import .../lib_X/<old>` → `lib_X/<new>` puis re-publier (cf. Étape …)
- `tradingview/indicator.pine` : `import .../lib_X/<old>` → `lib_X/<new>`

## 📈 Indicateurs à (re)charger

Pour chaque indicateur ajouté ou modifié :
- `tradingview/foo.pine` — Pine Editor → coller → Add to chart → vérifier la compilation et le rendu visuel.

## ✅ Cas de test à valider

(Repris de `docs/<feature>.md` ou suggérés en fonction de la feature)
- [ ] Cas 1 : ...
- [ ] Cas 2 : ...

## Estimations
- Total étapes publish : N
- Total imports à propager : M
- Temps estimé : ≈ <1 min × N + 30s × M> minutes hors validation chart
```

### 7. Conventions de présentation
- Annoncer le total d'étapes et le temps estimé en tête.
- Mentionner explicitement les bloquants en premier (avec lien vers les lignes fautives).
- Si aucune lib n'est touchée, sauter directement à "Indicateurs à recharger".
- Si rien de pertinent n'a changé (uniquement docs), produire un plan vide : « Aucune publication nécessaire ».

## Quand invoquer

- **Obligatoire** avant tout `git merge`, ouverture de PR, ou message du type "on peut merger maintenant".
- L'utilisateur peut aussi déclencher manuellement via `/delivery-plan` ou `/delivery-plan main`.

## Anti-patterns à éviter

- Ne **pas** invoquer ce skill pour des changements `docs/`-only.
- Ne **pas** proposer un merge sans avoir présenté la checklist.
- Ne **pas** considérer la livraison faite tant que le user n'a pas confirmé les versions assignées par TradingView et mis à jour `docs/LIBS.md`.
