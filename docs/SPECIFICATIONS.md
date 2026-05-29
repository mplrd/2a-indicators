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

### 1. Gaps

Detecte les **gaps journaliers** et les affiche sur n'importe quel timeframe du chart. La detection
se fait sur la **barre daily** (via `request.security(sym, "D", ...)`), pas au minuit-chart : la meche
de la barre daily inclut tout le trading du jour, donc la comparaison ci-dessous traduit un vrai gap
non comble — **pas de faux gap sur les instruments 24h** (DAX IG, futures, crypto), ou le mouvement
nocturne fait partie de la barre daily et n'est pas un gap.

**Regles de detection** (sur la barre daily) :
- **Gap haussier** : `low > close[1]` (le plus bas du jour est au-dessus de la cloture de la veille) → zone `[close[1], low]`
- **Gap baissier** : `high < close[1]` (le plus haut du jour est en-dessous de la cloture de la veille) → zone `[high, close[1]]`

**Cycle de vie d'un gap** :
1. Detection au changement de jour chart (`timeframe.change("D")`). La box est creee **une seule fois**, entre `gap_top` et `gap_bottom`, **ancree sur la barre precedant le gap** (`bar_index - 1`, en `xloc.bar_index` — adjacente a la barre courante au moment de la creation, donc aucune limite de distance), etendue a droite, **sans bordure** (fond gris clair translucide uniquement). Elle persiste ensuite (pas de recreation).
2. **Comblement partiel** : si le prix rentre dans la zone sans la traverser entierement, la box est **retrecie sur place** (`box.set_top` descend pour un gap haussier, `box.set_bottom` monte pour un baissier).
3. **Comblement total** : si le prix traverse entierement la zone, la box est supprimee.
4. Les gaps les plus anciens sont supprimes (FIFO) quand le nombre depasse le lookback.

| Setting | Type | Defaut | Plage |
|---------|------|--------|-------|
| Gaps | bool | true | - |
| Couleur | color | `#CCCCCC` (gris clair) | - |
| Historique (lookback) | int | 50 | 10-500 |

### 2. Niveaux de prix

Lignes horizontales dessinees sur `barstate.islast` uniquement. Chaque niveau a un toggle, une couleur, un style de ligne (`solid`, `dashed`, `dotted`), une epaisseur (1-5) et un label optionnel.

#### Niveaux de periodes precedentes

| Niveau | Label | Defaut on | Couleur | Style | Epaisseur | Condition d'affichage |
|--------|-------|-----------|---------|-------|-----------|-----------------------|
| PDH / PDL | Previous Day High/Low | oui | gris foncé (#555555) | dashed | 2 | TF <= Daily |
| PWH / PWL | Previous Week High/Low | oui | gris foncé (#555555) | solid | 2 | TF <= Weekly |
| PMH / PML | Previous Month High/Low | oui | gris foncé (#555555) | solid | 3 | TF <= Monthly |
| ATH | All-Time High | oui | rouge foncé (#8B0000) | dashed | 2 | toujours |

**Regle de timeframe** : chaque niveau de periode n'est affiche que si le timeframe du graphique est inferieur ou egal a la periode du niveau. Ex : PDH/PDL n'apparait pas sur un graphique weekly.

**Regle de dessin** : si le point de depart de la ligne est dans les 365 dernieres barres, la ligne part du debut de la periode precedente et s'etend a droite. Sinon, la ligne s'etend dans les deux directions (extend.both).

#### Niveaux de sessions

Affichent le High/Low de la session en cours ou de la derniere session terminee. Uniquement visible en intraday.

| Session | Horaires defaut | Timezone | Defaut on | Couleur | Style | Epaisseur |
|---------|----------------|----------|-----------|---------|-------|-----------|
| Asiatique | 08:00-14:00 | Asia/Tokyo | oui | orange | dashed | 2 |
| Europeenne | 09:00-14:00 | Europe/Paris | oui | bleu | dashed | 2 |
| Americaine | 09:30-16:00 | America/New_York | oui | violet | dashed | 2 |

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

| Setting | Defaut on | Couleur | Style | Epaisseur |
|---------|-----------|---------|-------|-----------|
| Open Range (OPR / IBR) | oui | noir | dotted | 2 |
| Open Future | non | gris | dotted | 1 |
| Open EU | non | bleu | dotted | 1 |
| Open US | non | violet | dotted | 1 |

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
2. La bougie **cloture au-dessus** de son open (bougie verte)
3. La cloture **depasse le high de la meche precedente** (`close > high[1]`)
4. L'open n'est **pas egal au low** (pas d'ouverture en extreme)

**CMI Bearish (baissiere)** - toutes les conditions doivent etre reunies :
1. L'open est **au-dessus** de la reference CMI
2. La bougie **cloture en-dessous** de son open (bougie rouge)
3. La cloture **passe sous le low de la meche precedente** (`close < low[1]`)
4. L'open n'est **pas egal au high** (pas d'ouverture en extreme)

### Validation 3 bougies

La CMI est la **bougie 0** ; on compte les bougies **1, 2 et 3** qui la suivent pour (in)valider :
- **CMI bullish** : invalidée si une cloture passe **sous le `low` de la CMI** (son extreme bas) pendant les bougies 1 a 3
- **CMI bearish** : invalidée si une cloture passe **au-dessus du `high` de la CMI** (son extreme haut) pendant les bougies 1 a 3
- **Une CMI opposee pendant la validation n'est PAS un critere d'invalidation en soi.** Elle n'invalide la premiere que si sa cloture casse aussi son extreme. Sinon, les deux CMIs valident **independamment** (validations paralleles) et creent chacune leur zone — typique d'un range qui se forme.
- Si la CMI survit aux bougies 1-2-3 → **validée**, la zone est tracée.

> On teste l'**extreme** (`high`/`low`) de la CMI, **pas** son open : la CMI n'est invalidée que si le prix la « reprend » entierement (cloture au-dela de sa meche). Plusieurs CMIs peuvent donc etre en validation en parallele.

### Construction de la zone

Une fois validee, la zone est construite sur le **corps** (≠ S/D qui prend la cloture), a partir des extremes de la fenetre de lookback (`Drawing CMI Zone Lookback`, defaut 3) — c'est la plus petite zone, la plus a l'extreme :

- **Zone bullish** : Top = **plus bas `min(open, close)`** du lookback (bas de corps le plus bas) ; Bottom = **plus bas `low`** (meche la plus basse).
- **Zone bearish** : Top = **plus haut `high`** (meche la plus haute) ; Bottom = **plus haut `max(open, close)`** (haut de corps le plus haut).

Les deux bornes peuvent provenir de bougies differentes du lookback. Le timestamp de debut de la zone = le plus ancien des deux extremes retenus.

### Regles anti-chevauchement et mise en attente

Une CMI dont le prix reagit dans une zone existante doit quand meme passer la validation 3 bougies.
Si validee, deux dimensions de regle s'appliquent : **intra-TF** (chevauchement avec une zone du meme TF) et **cross-TF** (chevauchement avec une zone d'un TF different). Toutes deux sont configurables via les settings.

#### Intra-TF (chevauchement avec une zone du meme timeframe)

Setting : `Mode chevauchement intra-TF` (`Pending` | `Replacement`, defaut `Pending`).

**Mode `Replacement`** :
- Quand une nouvelle zone chevauche en prix une zone existante non-expiree du meme TF, l'ancienne est rendue obsolete (EXPIRED), la nouvelle prend sa place (ACTIVE).
- Si plusieurs anciennes chevauchent la nouvelle, **toutes** sont expirees.

**Mode `Pending`** :
- Une nouvelle zone validee est **mise en attente** (PENDING) — donc **non dessinee** — si **l'open de la CMI tombe dans une zone non-expiree (ACTIVE ou PENDING) du meme TF**, **OU** si la **zone reconstruite chevauche** une telle zone (l'un ou l'autre suffit). C'est la traduction de « la zone precedente a tenu » : on n'affiche pas de doublon.
- **Chainage** : N zones peuvent s'empiler en pending sur le meme prix. Elles se reactivent une a une dans l'ordre d'insertion (FIFO) au fur et a mesure que les zones plus anciennes expirent (obsolescence).
- Les zones en attente trop anciennes (hors periode d'interet) sont supprimees comme n'importe quelle autre zone.

#### Cross-TF (chevauchement avec une zone d'un TF different)

Setting : `Mode chevauchement cross-TF` (`Pending` | `Autorise`, defaut `Pending`).

**Mode `Pending`** :
- Une zone ACTIVE est mise en PENDING si elle chevauche en prix une zone non-expiree (ACTIVE ou PENDING) d'un TF **superieur**.
- Priorite : Monthly > Weekly > Daily > H4 > H1 > M15. Une zone H1 bloquee par une zone D devient pending ; jamais l'inverse.
- Reactivation automatique quand le bloquant superieur expire (cycle de vie ou periode d'interet).

**Mode `Autorise`** :
- Aucune regle cross-TF : les zones de differents TF peuvent se superposer visuellement.

### Timeframes et periodes d'interet

Chaque timeframe a une profondeur historique maximale pour ses zones :

| Timeframe | Periode d'interet | Lookback |
|-----------|-------------------|----------|
| Monthly (1M) | illimite | pas de limite |
| Weekly (1W) | 3 ans | depuis le 1er janvier, annee - 3 |
| Daily (1D) | 6 mois | depuis le 1er du mois, mois - 6 |
| H4 (60) | 1 mois | depuis le 1er du mois, mois - 1 |
| H1 (60) | 2 semaines | depuis le lundi d'il y a 2 semaines |
| M15 (15) | semaine derniere | depuis le lundi de la semaine derniere |

En **mode test single-TF**, pour une UT **< M15** (M1, M5, M10...), la periode d'interet = **depuis la premiere bougie de la journee J-1** (la veille). Zones anterieures = obsoletes.

Les zones obsoletes (hors de leur periode d'interet) sont nettoyees automatiquement a chaque barre.

**Timezone** : tous les calculs de bornes ("aujourd'hui", "ce lundi", "1er du mois", "annee courante") utilisent la timezone du chart (setting `Chart timezone`, defaut `Europe/Paris`). Pine n'expose pas la TZ d'affichage du chart ; il faut donc la fournir explicitement (meme contrainte que sur `2Ai Levels` pour les sessions et l'Open Range).

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
| h1 | non | `rgb(244,143,177)` (rose) | dotted | 1 |
| M15 | non | `rgb(252, 185, 200)` (rose clair) | dotted | 0 |

Les zones sont dessinees avec une transparence de fond de 85% et une transparence de bordure de 40%.

| Setting misc | Type | Defaut | Plage |
|-------------|------|--------|-------|
| Drawing CMI Zone Lookback | int | 3 | 1-5 |
| Mode chevauchement intra-TF | enum (Pending / Replacement) | Pending | — |
| Mode chevauchement cross-TF | enum (Pending / Autorise) | Pending | — |
| Chart timezone | string | `Europe/Paris` | TZ IANA |

---

## Indicateur 4 : 2Ai Zones (`zones.pine`)

### 1. Supply/Demand

Détecte les CMI dans l'UT affichée (cf. règles CMI section 3.1 sur zones-MTF).
**Pas de validation en 3 bougies** (différence vs zones-MTF).

**Construction de la zone** :
- Quand une CMI est détectée sur la bougie courante, la **zone = bougie n-1 dans son intégralité** (mèches incluses) :
  - `top = high[1]`
  - `bottom = low[1]`
- CMI bullish → zone **Demand** (= bullish, support potentiel). Label `Demand`.
- CMI bearish → zone **Supply** (= bearish, résistance potentielle). Label `Supply`.
- Terminologie : "Supply" et "Demand" sont les deux faces du même mécanisme, on ne dit pas "S/D bull/bear".

**Lifecycle** :
- La zone est créée `active` à la barre suivant la CMI.
- **Morte au comblement total** :
  - zone demand → mort quand `low <= bottom` (le prix a entièrement traversé la zone vers le bas).
  - zone supply → mort quand `high >= top` (le prix a entièrement traversé vers le haut).
- **Anti-chaînage** : une CMI qui tombe sur la barre **immédiatement après** une CMI ayant déjà
  créé une zone est ignorée — pas de nouvelle zone créée. Justification : c'est une continuation
  du même mouvement de retournement, pas une nouvelle interaction du marché.
- **Anti-chevauchement par remplacement** : si une nouvelle zone S/D chevauche en prix une zone
  active **du même side** déjà existante, l'ancienne est expirée et la nouvelle prend sa place.
  Justification : les S/D sont court terme (réaction quasi-instantanée attendue) ; si le marché
  revient sur la même zone, c'est la dernière interaction qui compte.
- Les zones MTF ont des règles de persistance plus avancées (pending + réactivation +
  expiration par TF) — pas applicables ici.

### 2. Fair Value Gaps (FVG)

Detecte les desequilibres de prix (zone non tradee entre 3 bougies consecutives).

#### Detection

**FVG Bullish** : `low > high[2]` (le low de la bougie actuelle est au-dessus du high de 2 bougies avant)
**FVG Bearish** : `high < low[2]` (le high de la bougie actuelle est en-dessous du low de 2 bougies avant)

**Filtre anti-faux-gap** : le pattern 3-bougies est rejeté si l'une des 2 transitions
(`bar[2] → bar[1]` ou `bar[1] → bar[0]`) franchit une période non tradée — détectée
par un delta temporel `> 1.5 ×` la durée normale d'une barre. Couvre les fermetures
overnight des marchés cash, les week-ends, et les breaks de session sans paramétrage
ni fenêtre arbitraire de barres.

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

Il s'agit de zones de supply/demand qui ont déclenché la formation d'un FVG sur la CMI.
Le FVG est alors "collé" à la zone de supply/demand et en fait une zone d'intérêt "5 étoiles".

**Implémentation** : pas de rendu spécifique — une 5★ est simplement la superposition visuelle
d'une zone S/D et d'un FVG du **même side** sur la même plage de prix. La combinaison émerge
naturellement du rendu des deux types.

### 4. Filtre "Safe mode"

Si ce filtre est activé, seules les zones 5 étoiles (S/D + FVG alignés, même side, chevauchement
en prix) sont affichées. Les S/D isolés et les FVG isolés sont masqués au rendu.

#### Settings

| Setting | Type | Defaut |
|---------|------|--------|
| Safe mode | bool | false |
| Supply/Demand | bool | true |
| Bordure S/D | string | solid |
| Epaisseur S/D | int | 1 |
| Couleur S/D Bullish | color | bleu |
| Couleur S/D Bearish | color | orange |
| FVG | bool | true |
| Couleur FVG Bullish | color | vert |
| Couleur FVG Bearish | color | rouge |
| Bordure FVG | string | dashed |
| Epaisseur FVG | int | 0 |

Transparence de fond : 85% / Transparence de bordure : 40% (alignement avec zones-MTF).
| Extend Right | bool | false |
| Delete After Fill | bool | false |
| FVG History | int | 100 (max 365) |