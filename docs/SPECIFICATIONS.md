Suite d'indicateurs TradingView (Pine Script v6) construits sur de bibliotheques partagees
Seront par la suite portées pour d'autres plateformes (cTrader, MetaTrader, PRT, etc.)

## Architecture

```
lib-price.pine          Calculs purs : indicateurs, detection CMI, gaps, sessions, niveaux
lib-hours.pine          Gestion des horaires
lib-drawing.pine        Affichage : configuration visuelle, dessin de lignes/zones/boxes, nettoyage
... lib-xxx.pine        Tout autre bibliothèque nécessaire représentant un "domaine métier"
layout.pine             Indicateur "2Ai Layout"       (BB, MA, Ichimoku, Supertrend, niveaux, gaps)
levels.pine             Indicateur "2Ai Levels"       (Gaps Daily, PDL/PDH, PWL/PWH, PML/PMH, Sessions précédentes, Première Bougie H1, Niveaux d'open )
zones.pine              Indicateur "2Ai Zones"        (zones CMI, FVG)
zones-MTF.pine          Indicateur "2Ai Zones MTF"    (zones CMI multi-timeframe)
... xxx.pine            Tout autre indicateur "2Ai XXX"
```

### Dependances

Les indicateurs importent les bibliotheques.
Tous les calculs y sont gérés pour des raisons de maintenabilitié

---

## Indicateur 1 : 2Ai Layout (`layout.pine`)

### 1. Bandes de Bollinger

Deux jeux de Bollinger sont proposes, chacun activable/desactivable independamment.

**Note importante** : la médiane (SMA centrale) n'est PAS tracée. Seules les bandes outer et (en mode ribbon) inner sont affichées.

**Rendu en mode ribbon** :
- Bordure **outer** : opaque, couleur de base (gris). Mêmes linestyle et linewidth qu'en simple.
- Bordure **inner** : même linestyle que l'outer, linewidth 1px, **transparence 50** pour distinguer de l'outer sans surcharger.
- **Fill entre outer et inner** :
  - Bande **ouverte** : couleur de base à transparence 85 (très atténué)
  - Bande **plate / en fermeture** : couleur bull/bear à transparence 70 (lisible, plus marqué que l'ouvert)

#### Bollinger Classique

- **SMA** : 20 periodes (non tracée)
- **Bandes externes** (outer) : ecart-type x2.5, 1px
- **Bandes internes** (inner) : ecart-type x2.0
- **Mode** : `simple` (une seule ligne par bande) ou `ribbon` (remplissage entre bande interne et externe)
- **Style de ligne** : `solid` / `dashed` / `dotted` (défaut solid)
- **Couleur par defaut** : `#9c9c9c` (gris)
- **Actif par defaut** : oui, en mode `ribbon`

#### Bollinger Magique

- **SMA** : 160 periodes (non tracée)
- **Bandes externes** (outer) : ecart-type x2.8, 2px
- **Bandes internes** (inner) : ecart-type x2.5
- **Mode** : `simple` ou `ribbon`
- **Style de ligne** : `solid` / `dashed` / `dotted` (défaut solid)
- **Couleur par defaut** : `#808080` (gris fonce)
- **Actif par defaut** : oui, en mode `simple`

#### Detection des bandes plates / fermees

Fonctionnalite transversale aux deux Bollinger. Detecte quand une bande est **plate** (horizontale) ou **en fermeture** (la bande superieure descend, ou la bande inferieure monte).

**Regle de calcul** : on mesure la pente en pourcentage sur N periodes (`slopePeriod`, defaut 2). Si la valeur absolue de la pente est inferieure au seuil (`slopeThreshold`, defaut 0.015%), la bande est consideree "plate". Si la pente va dans le sens de la fermeture (bande superieure descendante, bande inferieure ascendante), elle est consideree "fermee".

**Rendu visuel** :
- **Mode ribbon** : le remplissage entre bande interne et externe change de couleur (bearColor pour la bande superieure, bullColor pour la bande inferieure)
- **Mode simple** : une ligne accentuee de couleur bull/bear est superposee a la bande (epaisseur +1px)

| Setting | Type | Defaut | Plage |
|---------|------|--------|-------|
| Bandes Plates | bool | true | - |
| Periode | int | 2 | 1-20 |
| Seuil % | float | 0.015 | 0.001-1.0 |

### 2. Moyennes Mobiles

Quatre SMA disponibles. Chacune a un toggle on/off, une couleur, un mode (`simple` / `ribbon`) et un style de ligne (`solid` / `dashed` / `dotted`). Le ruban est construit avec un ecart-type de 0.236 autour de la SMA.

| MA | Periodes | Epaisseur | Style defaut | Defaut on | Couleur defaut |
|----|----------|-----------|--------------|-----------|----------------|
| MA7 | 7 | 1px | dashed | oui | bleu clair / aqua |
| MA20 | 20 | 1px | solid | oui | bleu |
| MA50 | 50 | 2px | solid | oui | orange |
| MA200 | 200 | 2px | dashed | oui | gris |

### 3. Ichimoku

Parametres traditionnels (9/26/52). Deux modes d'affichage :
- **Complet** : Tenkan, Kijun, Chikou, Senkou A, Senkou B, nuage (Kumo)
- **Chikou uniquement** : seule la Chikou Span est affichee

**Chikou Span** : decalee de 26 periodes dans le passe.
**Senkou A/B** : decalees de 26 periodes dans le futur. Couleur dynamique : la Senkou au-dessus prend la couleur bull, celle en-dessous la couleur bear. **Lignes du nuage atténuées** (transparence 60) pour ne pas surcharger visuellement.
**Nuage** : remplissage entre Senkou A et B, bleu si A > B (haussier), jaune si B > A (baissier). Transparence 85%.

| Setting | Type | Defaut |
|---------|------|--------|
| Afficher Ichimoku | bool | true |
| Chikou Uniquement | bool | **true** (par défaut on n'affiche que la Chikou — full Ichimoku sur opt-in) |
| Couleur Tenkan | color | jaune moutarde (#d4a017) |
| Couleur Kijun | color | bleu |
| Couleur Chikou | color | noir |

### 4. Supertrend

Utilise `ta.supertrend` natif, ATR Period = 10, multiplicateur = 3.
Direction normalisee : 1 = haussier, -1 = baissier.

**Rendu** : ligne de 2px, brisée à chaque changement de direction (la barre de bascule baissier↔haussier injecte un `na` et `plot.style_linebr` empêche le bridging visuel). Couleur bull si direction = 1, bear sinon.

| Setting | Type | Defaut | Plage |
|---------|------|--------|-------|
| Supertrend | bool | false | - |

### 5. Couleurs globales

| Setting | Defaut |
|---------|--------|
| Couleur Haussiere | vert |
| Couleur Baissiere | rouge |

Ces couleurs sont utilisees par : Bollinger (bandes plates), Ichimoku (nuage, senkou), Supertrend.

### 6. Projection MA / BB (optionnelle)

Quand activée, l'indicateur prolonge visuellement les MA et/ou BB sur les **3 prochaines bougies** (constante figée, pas exposée au user). Toggle indépendant pour les MA et pour les BB.

**Hypothèse** : `close` reste constant à sa valeur courante sur les 3 barres futures (projection neutre, pas spéculative).

**Math** :
- SMA projetée à `+k` barres : `SMA(N) + (k · close − Σ_{i=1..k} close[N−i]) / N`. Calcul fermé exact sous l'hypothèse close constant.
- BB projetée : basis projetée comme ci-dessus, écart-type gardé constant à sa valeur courante (approximation marginale pour `k = 3`).

**Rendu** :
- Dessiné sur la dernière barre uniquement via `line.new`, rafraîchi à chaque tick live.
- Segment du point actuel `(bar_index, valeur_courante)` jusqu'à `(bar_index + 3, valeur_projetée)`.
- Couleur et linewidth de l'élément de base, mais **style toujours pointillé** (`line.style_dotted`) pour distinguer du tracé "réel".

**Composants projetés** :
- **Si Projeter les MM est ON** : MA7 / MA20 / MA50 / MA200 basis (uniquement celles activées)
- **Si Projeter les bandes est ON** :
  - BB Classique outer upper + outer lower (toujours)
  - BB Classique inner upper + inner lower (uniquement si mode `ribbon`)
  - BB Magique outer upper + outer lower (toujours)
  - BB Magique inner upper + inner lower (uniquement si mode `ribbon`)

| Setting | Groupe | Type | Défaut |
|---------|--------|------|--------|
| Projeter les bandes | Bandes de Bollinger | bool | **true** |
| Projeter les MM | Moyennes Mobiles | bool | **true** |

---

## Indicateur 2 : 2Ai Levels (`levels.pine`)

### 1. Gaps journaliers

Detecte les gaps entre la cloture de la veille et l'ouverture du jour (donnees daily via `request.security`).

**Regles de detection** :
- **Gap haussier** : `low > close[1]` (le plus bas du jour est au-dessus de la cloture precedente)
- **Gap baissier** : `high < close[1]` (le plus haut du jour est en-dessous de la cloture precedente)
- Les 3 premieres bougies intraday apres un changement de jour sont exclues (pour eviter de confondre gap overnight et gap de prix)

**Cycle de vie d'un gap** :
1. Detection au changement de jour. Dessin d'un rectangle (box) entre `gap_top` et `gap_bottom`, decale d'une bougie dans le passe, etendu a droite.
2. **Comblement partiel** : si le prix rentre dans la zone du gap sans la traverser entierement, le rectangle se reduit (le haut descend pour un gap haussier, le bas monte pour un gap baissier).
3. **Comblement total** : si le prix traverse entierement la zone, le rectangle est supprime.
4. Les gaps les plus anciens sont supprimes quand le nombre depasse le lookback.

| Setting | Type | Defaut | Plage |
|---------|------|--------|-------|
| Gaps | bool | true | - |
| Couleur | color | `#fbc02d` (jaune) | - |
| Historique (lookback) | int | 50 | 10-500 |

### 2. Niveaux de prix

Lignes horizontales dessinees sur `barstate.islast` uniquement. Chaque niveau a un toggle, une couleur, un style de ligne (`solid`, `dashed`, `dotted`), une epaisseur (1-5) et un label optionnel.

#### Niveaux de periodes precedentes

| Niveau | Label | Defaut on | Couleur | Style | Epaisseur | Condition d'affichage |
|--------|-------|-----------|---------|-------|-----------|-----------------------|
| PDH / PDL | Previous Day High/Low | oui | gris foncé (#555555) | solid | 1 | TF <= Daily |
| PWH / PWL | Previous Week High/Low | oui | gris foncé (#555555) | solid | 2 | TF <= Weekly |
| PMH / PML | Previous Month High/Low | oui | gris foncé (#555555) | solid | 3 | TF <= Monthly |
| ATH | All-Time High | oui | rouge foncé (#8B0000) | dashed | 2 | toujours |

**Regle de timeframe** : chaque niveau de periode n'est affiche que si le timeframe du graphique est inferieur ou egal a la periode du niveau. Ex : PDH/PDL n'apparait pas sur un graphique weekly.

**Regle de dessin** : si le point de depart de la ligne est dans les 365 dernieres barres, la ligne part du debut de la periode precedente et s'etend a droite. Sinon, la ligne s'etend dans les deux directions (extend.both).

#### Niveaux de sessions

Affichent le High/Low de la session en cours ou de la derniere session terminee. Uniquement visible en intraday.

| Session | Horaires defaut | Timezone | Defaut on | Couleur | Style | Epaisseur |
|---------|----------------|----------|-----------|---------|-------|-----------|
| Asiatique | 08:00-14:00 | Asia/Tokyo | oui | orange | solid | 1 |
| Europeenne | 09:00-14:00 | Europe/Paris | non | bleu | solid | 1 |
| Americaine | 09:30-16:00 | America/New_York | non | violet | solid | 1 |

**Regles de session** :
- Les High/Low sont reinitialises a chaque changement de jour.
- Pendant la session, les niveaux sont mis a jour en temps reel.
- A la fin de la session, les derniers niveaux sont conserves jusqu'a la prochaine session.
- Les lignes partent du timestamp de debut de la session du jour et s'etendent a droite.

#### Niveaux d'ouverture

Le systeme detecte automatiquement si l'instrument est **cash** (sa premiere bougie du jour correspond a l'ouverture d'un marche) ou **non-cash** (futures, crypto, etc. qui tradent en continu).
Utilise la première bougie h1 selon les horaires de cotation du marché comme référence (ex : 9h-10h heure de paris pour les cash EU, 9h30-10h30 heure de new york pour les cash US, 0h-1h pour les cryptos, ...)

**Detection automatique** :
- Si la premiere bougie du jour tombe dans la session EU, c'est un cash EU ; 
- si dans la session US, c'est un cash US
- si dans la session Asian, c'est un cash Asian
- sinon c'est une future/CFD/Crypto ou tout autre marché qui côté h24

**Instruments cash ** : affichage de l'**OPR** (Open Price Range)
- **Open** : prix d'ouverture au debut de la journée
- **IB Range** : plus hauts et plus bas de la première bougie h1

**Instruments non-cash** : affichage des niveaux **Open Reference**
- **Open Future** : prix d'ouverture a 8h00 Europe/Paris
- **Open EU** : prix d'ouverture au debut de la session europeenne
- **Open US** : prix d'ouverture au debut de la session americaine
- **IB Range** : plus hauts et plus bas de la première bougie h1
- Réinitialisés chaque jour, détectés a l'heure exacte

| Setting | Defaut on | Couleur |
|---------|-----------|---------|
| IBR | oui | gris |
| Open Future | oui | gris |
| Open EU | oui | gris |
| Open US | oui | gris |

#### Setting global niveaux

| Setting | Type | Defaut |
|---------|------|--------|
| Afficher les labels | bool | true |

---

## Indicateur 3 : 2Ai Zones MTF (`zones-MTF.pine`)

### Concept

Une CMI est une bougie de retournement qui signale un changement d'intention du marche. 
La detection identifie ces bougies sur plusieurs timeframes simultanement puis construit des zones de prix a partir des extremes precedents.

### Detection d'une CMI

**Reference** : SMA 7 de HL2 (moyenne des high-low, dite "CMI reference").

**CMI Bullish (haussiere)** - toutes les conditions doivent etre reunies :
1. L'open est **sous** la reference CMI
2. La reference CMI est **descendante** (valeur actuelle < valeur precedente)
3. La bougie **cloture au-dessus** de son open (bougie verte)
4. La cloture **depasse le high de la meche precedente** (`close > high[1]`)
5. L'open n'est **pas egal au low** (pas d'ouverture en extreme)

**CMI Bearish (baissiere)** - toutes les conditions doivent etre reunies :
1. L'open est **au-dessus** de la reference CMI
2. La reference CMI est **ascendante** (valeur actuelle > valeur precedente)
3. La bougie **cloture en-dessous** de son open (bougie rouge)
4. La cloture **passe sous le low de la meche precedente** (`close < low[1]`)
5. L'open n'est **pas egal au high** (pas d'ouverture en extreme)

### Validation 3 bougies

Apres detection, la CMI entre en **phase de validation** pendant 3 bougies :
- **CMI bullish** : invalidée si une cloture passe sous l'open de la CMI pendant les 3 bougies suivantes
- **CMI bearish** : invalidée si une cloture passe au-dessus de l'open de la CMI pendant les 3 bougies suivantes
- Si une CMI opposee est detectee pendant la validation, la CMI en cours est annulee et la nouvelle CMI prend sa place

### Construction de la zone

Une fois validee, la zone est construite a partir des extremes dans une fenetre de lookback (parametre `cmiLookbackPeriod`, defaut 3 bougies) en partant de la barre de la CMI :

- **Zone bullish** : entre le plus bas close et le plus bas low (meche) dans la fenetre. Top = lowest close, Bottom = lowest wick.
- **Zone bearish** : entre le plus haut close et le plus haut high (meche) dans la fenetre. Top = highest wick, Bottom = highest close.

Le timestamp de debut de la zone est le plus ancien des deux extremes trouves.

### Regles anti-chevauchement et mise en attente

Une CMI dont le prix reagit dans une zone existante de la même UT doit quand meme passer la validation 3 bougies. 
Si valideé, 2 cas possible :
- remplacement de la zone
- **mise en attente** (pending). 
Le comportement est configurable via les settings

**Mécanisme de remplacement** :
- Quand une zone apparait dans une zone existante, la zone existante est rendue obsolète, laissant place à la nouvelle zone

**Mecanisme de pending (deja implemente pour les chevauchements de zone)** :
- Une nouvelle zone validee est **mise en attente** (pending) si elle chevauche geometriquement une zone existante du meme timeframe
- Les zones en attente sont **reactivees** quand le chevauchement disparait (la zone existante expire)
- Les zones en attente trop anciennes (hors periode d'interet) sont supprimees

### Timeframes et periodes d'interet

Chaque timeframe a une profondeur historique maximale pour ses zones :

| Timeframe | Periode d'interet | Lookback |
|-----------|-------------------|----------|
| Monthly (1M) | illimite | pas de limite |
| Weekly (1W) | 3 ans | depuis le 1er janvier, annee - 3 |
| Daily (1D) | 6 mois | depuis le 1er du mois, mois - 6 |
| H4 (60) | 1 mois | depuis le 1er du mois, mois - 1 |
| H1 (60) | 2 semaines | depuis le lundi d'il y a 2 semaines |
| M15 (15) | 5 jours | depuis J-5 |

Les zones obsoletes (hors de leur periode d'interet) sont nettoyees automatiquement a chaque barre.

### Regle d'affichage par timeframe

Une zone ne s'affiche que si le timeframe du graphique est **inferieur ou egal** au timeframe de la zone. Ex : une zone H1 s'affiche en M15, M5, M1... mais pas en Daily ou Weekly. Exception : les zones Monthly s'affichent partout.

### Settings

Chaque timeframe a un toggle on/off, une couleur, un style de bordure (`solid`, `dashed`, `dotted`), et une epaisseur de bordure (0-4).

| Timeframe | Defaut on | Couleur | Bordure | Epaisseur |
|-----------|-----------|---------|---------|-----------|
| Monthly | oui | olive | solid | 1 |
| Weekly | oui | teal | solid | 1 |
| Daily | oui | bleu | solid | 1 |
| H4 | oui | `rgb(91,156,246)` (bleu clair) | dashed | 1 |
| M15 | oui | `rgb(244,143,177)` (rose) | dotted | 1 |
| M15 | oui | `rgb(252, 185, 200)` (rose) | dotted | 0 |

Les zones sont dessinees avec une transparence de fond de 85% et une transparence de bordure de 40%.

| Setting misc | Type | Defaut | Plage |
|-------------|------|--------|-------|
| Drawing CMI Zone Lookback | int | 3 | 1-5 |

---

## Indicateur 4 : 2Ai Zones (`zones.pine`)

### 1. Supply/Demand

Détecte les CMI dans l'UT affichée.
La validation en 3 bougies n'est pas nécessaire.
Identifie la bougie précédent la CMI, elle représente la zone de supply/demande

### 2. Fair Value Gaps (FVG)

Detecte les desequilibres de prix (zone non tradee entre 3 bougies consecutives).

#### Detection

**FVG Bullish** : `low > high[2]` (le low de la bougie actuelle est au-dessus du high de 2 bougies avant)
**FVG Bearish** : `high < low[2]` (le high de la bougie actuelle est en-dessous du low de 2 bougies avant)

**Exclusion** : les FVG ne sont pas detectes dans les 3 premieres bougies d'une nouvelle journee (en intraday), pour eviter les faux gaps overnight.

#### Niveaux du FVG

- **Bullish** : Top = low de la bougie actuelle, Bottom = high de la bougie [2]
- **Bearish** : Top = low de la bougie [2], Bottom = high de la bougie actuelle

Le rectangle est dessine a partir de la bougie [2] (debut du pattern).

#### Comblement (optionnel)

Si "Delete After Fill" est actif :
- **Bullish FVG** : comble quand `low <= bottomLevel`. Comblement partiel si le low entre dans la zone sans la traverser (le top de la box se reduit).
- **Bearish FVG** : comble quand `high >= topLevel`. Comblement partiel si le high entre dans la zone sans la traverser (le bottom de la box se reduit).
- Les FVG totalement combles sont supprimes du graphique.

### 3. Zones "5 étoiles"

Il s'agit de zones de supply/demand qui a déclenché la formation d'un FVG sur la CMI.
Le FVG est alors "collé" à la zone de supply/demand et en fait une zone d'intéret "5 étoiles"

### 4. Filtre "Safe mode"

Si ce filtre est activé, seules les zones 5 étoiles et le FVG associé sont affichés

#### Settings

| Setting | Type | Defaut |
|---------|------|--------|
| Safe mode | bool | false |
| Supply/Demand | bool | true |
| Bordure | string | solid |
| Epaisseur | int | 1 |
| Couleur Bullish | color | bleu |
| Couleur Bullish | color | orange |
| FVG | bool | true |
| Couleur Bullish | color | vert |
| Couleur Bearish | color | rouge |
| Bordure FVG | string | dashed |
| Epaisseur FVG | int | 0 |
| Extend Right | bool | false |
| Delete After Fill | bool | false |
| FVG History | int | 100 (max 365) |