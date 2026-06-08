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
strat-OR.pine           Stratégie  "2Ai_Strat_OR"     (Open Range 1re heure : cassure + réintégration)
... strat-xxx.pine      Toute autre stratégie "2Ai_Strat_XXX"
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
1. Detection au changement de jour chart (`timeframe.change("D")`). La box est creee **une seule fois**, entre `gap_top` et `gap_bottom`, **ancree sur la barre precedant le gap** par son **timestamp** (`time[1]` capture au moment de la creation, dessine en `xloc.bar_time`), etendue a droite, **sans bordure** (fond gris clair translucide uniquement). Elle persiste ensuite (pas de recreation). L'ancrage est en **temps** (et non `bar_index`) car une box en `xloc.bar_index` a une limite de distance du bord gauche au `bar_index` courant : sur gros historique, un gap ancien finissait par lever `Bar index value of the left argument is too far from the current bar index`. `xloc.bar_time` n'a pas cette limite (meme convention que les zones S/D et CMI).
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

---

## Bougies de signal (`lib_signal`)

`lib_signal` (Couche 1, pur, sans dessin ni input) centralise la détection des **bougies de signal** :
des patterns ponctuels sur la barre courante qui déclenchent un setup. Les détecteurs sont
réutilisables par les indicateurs et par les futures **stratégies** (qui importent le signal pur
sans embarquer de code de dessin). Chaque détecteur est sans état (entrée = la barre courante et
son historique implicite, sortie = un `SignalKind`) ; toute validation multi-barres (ex. validation
3 bougies de la CMI) reste à la charge du consommateur.

`SignalKind` : `NONE`, `CMI_BULL`, `CMI_BEAR`, `ENGULF_BULL`, `ENGULF_BEAR`, `OPEN_LOW`, `OPEN_HIGH`,
`COE_BULL`, `COE_BEAR`.

### CMI — `detectCMI()`

Cf. section « Detection d'une CMI » (Indicateur 3). Référence SMA 7 de HL2, 4 conditions par sens.
La CMI exige une mèche du côté de l'open (`open != low` en bull, `open != high` en bear) : une CMI
ne peut donc **jamais** être un signal « open en extrême ».

### Englobante — `detectEngulfing()`

Bougie dont l'amplitude « avale » entièrement la précédente, avec clôture qui franchit l'extrême opposé.
Deux formes, le signal se déclenche si **l'une OU l'autre** est vraie :

- **Forme idéale (corps englobe tout n-1)** : le corps de N couvre toute la bougie n-1, mèches
  incluses — bull : `open <= low[1] and close >= high[1]`. Rare (n'arrive qu'avec un gap, donc sur
  marchés cash).
- **Forme mèches + clôture au-delà** : l'amplitude (mèches) de N englobe totalement n-1 **et** la
  clôture franchit la mèche opposée de n-1 — bull : `low <= low[1] and close > high[1]`.

La 2ᵉ forme **contient** la 1ʳᵉ (si `close > high[1]` alors `high > high[1]` mécaniquement), donc la
condition retenue se réduit à :

- **`ENGULF_BULL`** : `low <= low[1] and close > high[1]`
- **`ENGULF_BEAR`** : `high >= high[1] and close < low[1]`

### Open en extrême — `detectOpenExtreme()`

La bougie ouvre **pile sur son extrême**, sans aucune mèche du côté de l'open (test **strict**) :

- **`OPEN_LOW`** : `open == low` — pas de mèche basse → pression acheteuse (bull).
- **`OPEN_HIGH`** : `open == high` — pas de mèche haute → pression vendeuse (bear).

Aucune autre condition (ni couleur, ni amplitude).

### Clôture-puis-open en extrême (COE) — `detectCOE()`

Pattern **2 barres en 3 temps** — retournement par épuisement puis prise de contrôle inverse,
**confirmé par le break** de la première bougie :

- **`COE_BULL`** : (1) bougie n-1 **rouge** qui clôture sur son bas (`close[1] == low[1]` et
  `open[1] > close[1]`, vendeur épuisé) → (2) bougie n qui **ouvre sur son bas** (`open == low`,
  reprise acheteuse) → (3) n **casse le high de la rouge** (`close > high[1]`, confirmation).
- **`COE_BEAR`** : miroir — n-1 **verte** clôture sur son haut (`close[1] == high[1]` et
  `close[1] > open[1]`) → n **ouvre sur son haut** (`open == high`) → n **casse le low de la verte**
  (`close < low[1]`).

> La barre n d'une COE est aussi un `OPEN_LOW` / `OPEN_HIGH` (open en extrême). Les détecteurs
> restent indépendants ; ils peuvent marquer la même barre.

> **Rejection / liquidity grab** : explorée puis **abandonnée** (non figée). Elle dépend d'une
> brique « swing point » (structure de marché) non résolue. Aucun détecteur de rejet n'est livré
> dans `lib_signal` ; à reprendre de zéro quand la définition du swing point sera tranchée.

---

## Indicateur 5 : 2Ai Signals (test) (`signals-test.pine`)

### Concept

Indicateur de **contrôle visuel** des détecteurs de `lib_signal`. Il n'a pas de logique métier
propre : il appelle les détecteurs purs et marque les barres correspondantes. Sert à valider
manuellement chaque signal (instrument / TF / scénario) avant de bâtir les stratégies qui les
consommeront. Overlay.

### Rendu

**Un label fléché par barre et par sens** (pas un marqueur par signal — on évite les marqueurs
empilés). Tous les signaux **du même sens** actifs sur la barre sont concaténés dans le **texte**
d'un seul label : flèche **vers le haut sous la barre** pour le sens **bull**, **vers le bas
au-dessus** pour le sens **bear**. Le sens est aussi porté par la couleur.

Codes **empilés sur plusieurs lignes** (un par ligne, `\n`), ordre fixe du plus fort au plus faible :
`COE`, `ENG`, `CMI`, `OE`. Exemple : une barre à la fois CMI bull et englo bull affiche un seul label
`ENG` / `CMI` (ENG au-dessus de CMI) sous la barre.

| Signal | Code dans le texte | Sens → position |
|--------|--------------------|-----------------|
| `CMI_BULL` / `CMI_BEAR` | `CMI` | bull (sous) / bear (au-dessus) |
| `ENGULF_BULL` / `ENGULF_BEAR` | `ENG` | bull / bear |
| `OPEN_LOW` / `OPEN_HIGH` | `OE` | bull / bear |
| `COE_BULL` / `COE_BEAR` | `COE` | bull / bear |

Ordre d'empilement (haut → bas) : `COE`, `ENG`, `CMI`, `OE`.

> Contrainte plateforme : `plotshape(..., text=)` exige un `const string` → impossible d'y
> concaténer un texte dynamique. On utilise donc `label.new` (texte série). Labels créés
> uniquement sur barre **confirmée** (`barstate.isconfirmed`) pour éviter le repaint intrabar
> (les conditions reposent sur `close` / `low` / `open`). Plafond `max_labels_count = 500` (FIFO).

### Settings

| Setting | Type | Defaut | Rôle |
|---------|------|--------|------|
| CMI | bool | true | Affiche / masque les marqueurs CMI |
| Englobantes | bool | true | Affiche / masque les marqueurs englobante |
| Open en extrême | bool | true | Affiche / masque les marqueurs open-extrême |
| COE | bool | true | Affiche / masque les marqueurs COE |

> Ces toggles d'affichage sont propres au harnais de test (isoler un signal pendant la validation).
> Aucun réglage n'altère la détection elle-même.

---

## Stratégie 1 : 2Ai_Strat_OR (`strat-OR.pine`)

Première **stratégie** de la suite (`strategy(...)`, pas `indicator(...)`) — elle produit donc un
rapport de backtest TradingView. Elle trade l'**Open Range de la première heure du jour** : deux
setups seulement, **cassure** (continuation) et **réintégration** (fade). Comme un indicateur, c'est
un **orchestrateur** : elle importe l'Open Range (`lib_levels`), embarque sa propre logique de gestion
de position, et dessine ses boxes inline. L'entrée se fait **au marché à la clôture** de la bougie de
signal (cassure/réintégration), pas par un pattern `lib_signal`.

### 1. Open Range (source)

L'Open Range = High/Low des **60 premières minutes du jour**, calculé par
`lib_levels.openRange(tz)` (cf. Indicateur 2, « Niveaux d'ouverture »). `tz` définit le « minuit »
de référence et est exposé en **input** (Pine n'expose pas la TZ d'affichage du chart — même
contrainte que `2Ai Levels` et `2Ai Zones MTF`).

- `R = orH − orL` (amplitude du range).
- Le range est exploitable **une fois figé** (après `orEnd`). Aucun trade tant que l'OR se construit.
- TF d'exécution attendue : **M5 ou M15** (granularité pour les signaux et les boxes ; l'OR reste
  la 1re heure quelle que soit l'UT < H1).

### 2. Suivi de l'excursion (machine d'état)

À chaque **bougie fermée** (`barstate.isconfirmed`), une fois l'OR figé, l'état suivant est tenu en
`var`, **réinitialisé au changement de jour chart** (dans `tz`) :

- **Sortie par le haut** : dès que `high > orH` (mèche **ou** clôture). On mémorise `exHigh` = plus
  haut de l'excursion et `exHighWick` = mèche haute (`high − max(open, close)`) de la bougie qui l'a
  marqué. Tant qu'on reste sorti, `exHigh` est poussé à chaque nouveau plus-haut.
- **Sortie par le bas** : miroir avec `low < orL`, `exLow`, `exLowWick` (`min(open, close) − low`).

`exHigh` / `exLow` sont l'**extrême de l'excursion** = le sommet/creux (au sens **Dow** : le point
d'inflexion d'où part le retournement) que la réintégration vient fader. Ils servent d'ancre au SL
de réintégration (§4).

### 3. Détection des deux setups

Évaluée sur bougie fermée, OR figé :

- **Cassure** = bougie qui **clôture hors** du range :
  - `close > orH` → setup **cassure haute** (long, continuation).
  - `close < orL` → setup **cassure basse** (short, continuation).
- **Réintégration** = bougie qui **clôture dans** le range alors que le prix **était sorti** (mèche
  ou N clôtures dehors) :
  - sortie haute puis retour dedans → setup **réintégration haute** (short, fade).
  - sortie basse puis retour dedans → setup **réintégration basse** (long, fade).
  - Le cas « mèche pure » (sortie + retour sur la même bougie) est le sous-cas où sortie et clôture
    intérieure tombent sur la même barre.

Le setup est **armé** sur sa bougie déclencheuse (on retient `bar_index`). La cassure ne s'arme que
sur la **1ʳᵉ clôture au-delà de la borne** (la « bougie qui casse ») — **pas** à chaque barre restée
dehors (sinon le setup resterait armé des heures et entrerait très loin de la cassure). Le prix doit
**revenir puis recasser** pour ré-armer.

### 4. Entrée — ordre MARCHÉ à la clôture de la bougie de signal

L'entrée n'est **pas** déclenchée par un pattern `lib_signal`. Dès que la **bougie de signal** clôture
(la bougie qui casse pour une cassure ; celle qui réintègre pour une réintégration), on entre **au
marché à sa clôture**, dans le sens du trade (`strategy.entry` sans `stop=`, avec
`process_orders_on_close = true`) :

| Setup | Sens | Déclencheur (clôture) |
|-------|------|------------------------|
| Cassure haute | long  | `close > orH` (1ʳᵉ clôture au-dessus) |
| Cassure basse | short | `close < orL` (1ʳᵉ clôture en-dessous) |
| Réintégration haute | short | sortie haute puis `close` revient dans le range |
| Réintégration basse | long  | sortie basse puis `close` revient dans le range |

- Le **prix d'entrée = la clôture** de la bougie de signal (donc **BE = ce close**). SL/TP figés au
  moment de l'entrée.
- **Pas d'ordre stop en attente, donc pas d'invalidation pré-entrée** : la bougie qui clôture
  hors/dans le range **EST** l'exécution. (Historique : une 1ʳᵉ version posait un ordre stop sur la
  mèche de la bougie — il ne filait quasi jamais, ~2 trades sur 6 mois. Abandonné.)
- **Garde-fou** : on n'entre que si le **TP1 est encore devant l'entrée** (`tpAhead`) — sécurité pour
  garantir toutes les cibles du bon côté.
- Chaque famille de setup a son toggle (`Cassure`, `Réintégration`) pour l'isoler en backtest.
- `lib_signal` n'est **plus** utilisé par cette stratégie.

### 5. Géométrie SL / TP (`R = orH − orL`)

Les multiples ci-dessous sont les **défauts** ; ils sont tous exposés en settings (groupe
*Multiples SL / TP*) pour adapter au comportement de chaque actif. La borne opposée (TP2
réintégration) reste fixe (`orL` / `orH`).

| Setup | Sens | SL | TP1 (50 %) | TP2 (20 %) | TP3 (20 %) | Runner (10 %) |
|-------|------|----|------------|------------|------------|---------------|
| Cassure haute | long  | `orL + 0.45·R` (= 55 % du range sous le haut) | `orH + 1·R` | `orH + 2·R` | `orH + 5·R` | BE only |
| Cassure basse | short | `orH − 0.45·R` (= 55 % au-dessus du bas) | `orL − 1·R` | `orL − 2·R` | `orL − 5·R` | BE only |
| Réintég. haute | short | `exHigh + 0.5·exHighWick` | `orH − 0.5·R` (milieu) | `orL` (borne opposée) | `orH − 2·R` | BE only |
| Réintég. basse | long  | `exLow − 0.5·exLowWick`   | `orL + 0.5·R` (milieu) | `orH` (borne opposée) | `orL + 2·R` | BE only |

**Mode SL — choisi indépendamment PAR setup** (`Mode SL` dans chaque groupe), pour tester selon l'actif :

- **Cassure** — `% range` *(défaut)* **ou** `Plus haut/plus bas`.
- **Réintégration** — `% mèche` *(défaut)* **ou** `Plus haut/plus bas`.

Détail des modes :

- **`% range` (cassure)** : SL à **% du range depuis la borne cassée** (`SL (% range)`, défaut **55 %**
  → `orH − 0.55·R` en long ; `orL + 0.55·R` en short). Saisie **non capée** : > 100 % autorisé.
- **`% mèche` (réintégration)** : l'extrême de l'excursion fadée (§2) + une marge de `SL (% mèche)` de
  la mèche de la bougie qui a marqué cet extrême (défaut 100 %).
- **`Plus haut/plus bas` (les deux setups)** : SL sur le **plus bas** (long) / **plus haut** (short)
  des **N dernières bougies** (`Lookback`, défaut 20, **bougie courante incluse**, mèche), **± un buffer**
  = `Tolérance` % du **range de la bougie qui a marqué cet extrême** (défaut 25 %). `Lookback` et
  `Tolérance` sont **propres à chaque setup**. SL **figé à la bougie de signal** (pas trailing). Formule :
  `slLong = lowest(low,N) − rangeBougieExtrême · tol%` ; `slShort = highest(high,N) + rangeBougieExtrême · tol%`.

### 6. Gestion de position

- **Sizing par risque constant** : la taille est calculée pour que la **perte au SL = `Risque par
  trade (%)`** de l'equity, quelle que soit la largeur du range/SL (`qty = equity·risk% / (distance
  entrée→SL · pointvalue)`). Comparable entre actifs. Écrase l'*Order size* de l'onglet Properties ;
  le rapport de perfs TradingView reste exact (calculé sur les fills réels).
- **Levier (marge)** : le sizing par risque produit un **notionnel** (`qty·prix`) bien supérieur au
  risque ; sans levier, TradingView **réduit ou rejette** tout ordre dont le notionnel dépasse
  l'equity (cas fréquent quand le SL est serré). Le **levier est un input** (`Levier (×)`, défaut **10×**)
  qui **cape la taille** : `qty` telle que `notionnel (qty·prix) ≤ levier · equity`. Tant que la qty par
  risque tient sous ce plafond, elle est filée telle quelle ; au-delà elle est **capée** (le risque réel
  devient alors < cible — contrainte réaliste du levier). Mise en œuvre : `margin_long = margin_short = 1`
  (**const** imposé par Pine — un input lève CE10123 ; sert de **plafond technique** ~100×), et le cap
  `levier · equity` est appliqué dans `riskQty`, ce qui évite tout rejet broker tant que `Levier (×) ≤ 100`.
  Le **risque reste borné par le SL** (le levier n'augmente pas la perte au SL).
- Répartition par défaut **50 / 20 / 20 / 10 %** (TP1, TP2, TP3, runner), **paramétrable**. TP1+TP2+TP3
  doivent totaliser ≤ 100 % ; le reliquat alimente le **runner**, ou est ajouté à TP3 si le runner
  est désactivé.
- **Passage à BE au TP1** (toggle, défaut on) : dès TP1 atteint, le SL du reliquat est ramené au
  **prix d'entrée exact** (frais ignorés). Si off, le stop reste au SL d'origine.
- **Runner** (toggle, défaut on) : aucune cible programmatique — il ne sort que par le **stop** (BE
  après TP1, ou SL d'origine si BE off) ou à la main. Une position encore ouverte au changement de
  jour est **laissée vivre** (pas de clôture forcée de fin de journée).
- **Pyramiding** : plusieurs entrées **de même sens** peuvent coexister (chacune son `id`, son
  SL/TP/BE et ses boxes via `from_entry`), jusqu'au plafond **Trades max / jour**. Les entrées sont
  **bloquées hors de la journée en cours**.
- **Contrainte position nette unique** : une stratégie Pine ne porte qu'**une position nette** (pas
  de long et short simultanés). Un setup de **sens opposé** à la position en cours est donc
  **ignoré** (on ne flippe pas la position — cela casserait la gestion indépendante par trade).

### 7. Rendu (façon position long/short TV)

Rendu **inline** dans la stratégie (le cycle de vie est couplé à l'état du trade), ancré à la bougie
d'entrée :

- **1 box position** verte : entrée → **TP3** (la zone de profit globale).
- **2 lignes** pointillées vertes aux niveaux **TP1** et **TP2** (cibles partielles dans la zone).
- **1 box SL** rouge : entrée → SL.
- **Rayon « taille restante »** : tracé **dès l'entrée** au **niveau d'entrée**, **vert** pour un long,
  **rouge** pour un short. Son étiquette affiche la **taille réellement filée encore ouverte**
  (interrogée sur la position moteur `strategy.opentrades`, **pas** la taille *demandée* qui peut être
  capée par le capital), **signée** (« - » pour un short) et qui **décroît à chaque TP filé** (entrée
  → −TP1 → −TP2 → −TP3 → runner). L'étiquette est **vide** tant que le fill n'est pas comptabilisé (la
  barre d'entrée). Le rayon est porté **`RUN_LEAD` = 5 barres devant la barre courante** (étiquette à
  son extrémité) et avance bougie par bougie jusqu'à la sortie, où il est **supprimé** (rayon + label)
  — **aucune trace de taille restante sur une position fermée** (seules les boxes restent, estompées).
- **Runner** : tant que le TP3 n'est pas atteint, le reliquat fait partie de la box principale. **Au
  TP3**, la box position (et les lignes TP) **se figent et s'estompent** (le gros du trade est fini) ;
  seul le **rayon « taille restante »** continue de courir (il porte alors la taille du runner) jusqu'à
  la sortie du runner (stop / BE).
- Chaque élément **étend son bord droit bougie par bougie** tant que sa cible n'est pas atteinte / le
  trade pas clôturé, puis se fige.
- **Grisage de la box SL** dès qu'elle n'est plus le risque vivant : au **passage à BE** (TP1 atteint,
  si BE activé) **ou** à la **sortie** du trade.
- Tous les éléments d'un trade partagent le **même bord droit** (ils s'étendent jusqu'à la clôture du
  trade) — la box SL ne fait que changer de couleur, elle ne s'arrête pas avant les autres.
- **Dessin uniquement pour les trades réellement exécutés** : à l'entrée on **place** l'ordre marché et
  on enregistre une entrée **en attente** (`Pending`), **sans rien dessiner**. La box, les lignes TP et
  le rayon ne sont créés qu'à la **barre où le fill est confirmé** (la position apparaît dans
  `strategy.opentrades`, `liveSize ≠ 0`), **ancrés à la bougie d'entrée**. Un ordre **non filé** (`qty`
  capée à 0 par le capital) est **purgé dès la barre suivante** et ne laisse **aucune trace** → le chart
  reflète exactement le backtest (pas de box ni de label vide pour une position qui n'existe pas).
- **Tracé de l'Open Range** (OR High / OR Low) du jour, pour contrôler visuellement la bougie / TZ
  retenue par la stratégie.

### 8. Garde-fous Pine

- Détection **uniquement sur bougie fermée** (`barstate.isconfirmed`) → pas de repaint. L'entrée est
  un **ordre marché** exécuté à la clôture de la bougie de signal (`process_orders_on_close = true`) ;
  la `qty` est passée explicitement (sizing par risque), donc `default_qty_*` du `strategy(...)` est ignoré.
- Le nombre de boxes vivantes est borné par `Trades max / jour` × jours backtestés (FIFO au-delà
  de 500).

### Settings

Les inputs sont regroupés à l'écran dans cet ordre (libellés courts + détails en tooltip ; les triplets
TP, qty et lookback/tolérance sont posés sur une seule ligne via `inline`).

**Open Range**

| Setting | Type | Défaut | Plage |
|---------|------|--------|-------|
| TZ Open Range | string | `Europe/Paris` | TZ IANA |

**Signaux & exécution**

| Setting | Type | Défaut | Plage |
|---------|------|--------|-------|
| Sens / hedge | string | `Les deux sens (hedge)` | hedge · long · short · net |
| Cassure validée en CLÔTURE | bool | false | — |
| Trades max / jour | int | 3 | 1-10 |
| Positions max en cours | int | 2 | 1-10 |

**Risque & levier**

| Setting | Type | Défaut | Plage |
|---------|------|--------|-------|
| Risque par trade (%) | float | 1.0 | 0.05-100 |
| Levier (×) | float | 10.0 | 1-100 |

> Le **levier** cape la taille (`notionnel ≤ levier · equity`). `margin_long`/`margin_short` restent
> `const` (≈ plafond 100×, imposé par Pine — CE10123 si input) ; c'est l'input `Levier (×)` qui pilote.

**Setup — Cassure**

| Setting | Type | Défaut | Plage |
|---------|------|--------|-------|
| Activer la cassure | bool | true | — |
| TP1 / TP2 / TP3 (×range) | float | 1.0 / 2.0 / 5.0 | ≥ 0.1 |
| Mode SL | string | `% range` | `% range` · `Plus haut/plus bas` |
| SL (% range) | float | 55.0 | ≥ 0 (non capé) |
| Lookback / Tolérance (plus haut-bas) | int / float | 20 / 25.0 | ≥ 2 / ≥ 0 |

**Setup — Réintégration**

| Setting | Type | Défaut | Plage |
|---------|------|--------|-------|
| Activer la réintégration | bool | true | — |
| TP1 / TP2 / TP3 (×range) | float | 0.5 / 1.0 / 2.0 | ≥ 0.1 |
| Mode SL | string | `% mèche` | `% mèche` · `Plus haut/plus bas` |
| SL (% mèche) | float | 100.0 | ≥ 0 (non capé) |
| Lookback / Tolérance (plus haut-bas) | int / float | 20 / 25.0 | ≥ 2 / ≥ 0 |

**Gestion de position**

| Setting | Type | Défaut | Plage |
|---------|------|--------|-------|
| Sortie % — TP1 / TP2 / TP3 | int | 50 / 20 / 20 | 0-100 (somme ≤ 100) |
| Runner (reliquat, sort au stop) | bool | true | — |
| Break-even | string | `Au TP1` | `Aucune` · `Au TP1` · `Anticipée (% prix)` |
| BE anticipée — % prix dans notre sens | float | 1.0 | ≥ 0.1 |

**Debug**

| Setting | Type | Défaut | Plage |
|---------|------|--------|-------|
| Afficher les bulles de signal | bool | false | — |

### Cas de test (à valider manuellement dans TradingView)

| # | Scénario | Attendu |
|---|----------|---------|
| 1 | Cassure haute nette + CMI bull sur la bougie de cassure | Entrée long, SL à 55 % sous orH, 3 TP + runner, boxes tracées |
| 2 | Cassure basse + signal bear sur la bougie suivante (pas sur la cassure) | Entrée short dans la fenêtre 2 bougies |
| 3 | Cassure sans aucun signal sur 2 bougies | Aucune entrée (setup expiré) |
| 4 | Mèche au-dessus de orH puis clôture dedans + signal bear | Réintégration haute → short, SL = mèche extrême + 50 % |
| 5 | N clôtures au-dessus de orH puis retour en clôture dans le range + signal bear | Réintégration haute (pas seulement le cas mèche) |
| 6 | TP1 atteint | SL du reliquat ramené à BE (prix d'entrée) |
| 7 | Plusieurs setups dans la journée jusqu'au plafond | Pyramiding jusqu'à « Trades max / jour », puis blocage |
| 8 | Changement de jour avec position ouverte | Position laissée vivre, runner non clôturé d'office |
| 9 | Instrument cash EU vs crypto 24 h | OR cohérent avec la TZ choisie (minuit de `tz`) |