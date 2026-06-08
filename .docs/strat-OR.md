# 2Ai_Strat_OR — Stratégie Open Range

Stratégie de **range** appliquée à l'**Open Range** (les 60 premières minutes du jour). C'est la 1re
*stratégie* de la suite (`strategy(...)`, pas `indicator(...)`) : elle produit un rapport de backtest
TradingView. Référence comportementale : `SPECIFICATIONS.md` § « Stratégie 1 : 2Ai_Strat_OR ».

## 1. Fonctionnalités

### Range source
- **Open Range** = High/Low des 60 premières minutes du jour, via `lib_levels.openRange(tz)`. La TZ
  (« minuit » de référence) est un **input** (Pine n'expose pas la TZ du chart).
- Le range est exploitable une fois **figé** (après `orEnd`). TF d'exécution attendue : **M5 / M15**.

### Deux setups (chacun activable)
- **Cassure** (continuation) : 1re bougie qui **clôture hors** du range → long si au-dessus, short si
  en-dessous.
- **Réintégration** (fade) : après une cassure confirmée, une clôture qui **revient dans** le range →
  short (depuis le haut) ou long (depuis le bas).

### Entrée — modèle bulle → flèche
- La bougie de signal (« bulle ») fige le niveau (son extrême) ; on entre **au marché** quand la bougie
  suivante (« flèche ») **casse cet extrême** dans le sens du trade.
- `Cassure validée en clôture` : décoché → la mèche suffit ; coché → la flèche doit clôturer au-delà.
- Garde-fou : on n'entre que si **TP1 est encore devant l'entrée**.

### Stop-loss — mode par setup
- **Cassure** : `% range` depuis la borne cassée **ou** `Plus haut/plus bas` (swing N bougies).
- **Réintégration** : `% mèche` de l'extrême **ou** `Plus haut/plus bas`.
- Mode « plus haut/plus bas » : plus bas (long) / plus haut (short) sur N bougies (courante incluse) ±
  une tolérance (% du range de la bougie qui a marqué l'extrême). `Lookback` + `Tolérance` par setup.

### Cibles & gestion
- **TP1 / TP2 / TP3** en multiples du range (par setup) + **runner**. Répartition `% TP1/TP2/TP3` (somme
  ≤ 100 ; reliquat = runner, ou ajouté à TP3 si runner off).
- **Break-even** : `Aucune` / `Au TP1` / `Anticipée (% prix)`.
- **Runner** : pas de cible programmatique, sort au stop (BE après TP1, ou SL).

### Sizing & exposition
- **Sizing par risque constant** : taille telle que la perte au SL = `Risque par trade (%)` de l'equity.
- **Levier (×)** : plafonne la taille (`notionnel ≤ levier × equity`) — sinon un SL serré produit une qty
  dont le notionnel dépasse l'equity et l'ordre est réduit/rejeté.
- **Sens / hedge** : `Les deux` / `Long` / `Short` / `Sens unique (net)`.
- **Plafonds** : `Trades max / jour`, `Positions max en cours` (un runner de-riské ne compte plus).

### Rendu
- **Par position** (`lib_strat_draw`) : box verte entrée→TP3, lignes TP1/TP2, box SL (grisée au BE / à la
  sortie), **rayon d'exposition restante** (niveau d'entrée, étiquette = taille live signée). **Dessin
  différé** : rien n'est tracé tant que le fill n'est pas confirmé → un ordre non filé ne laisse aucune
  trace, le chart reflète le backtest.
- **Open Range** : **zone** (rectangle bleu sans bordure, transparent) qui se construit en live pendant
  la 1re heure (ancrée à la 1re bougie, bornée au range courant) ; **lignes OR High/Low** ancrées au même
  début (orStart) et étendues vers la droite.
- **Debug** (toggle) : bulles (ronds sur les setups détectés) + flèches (entrées). N'altère ni la
  détection ni les entrées.

### Limites
- TF < H1 (l'OR reste la 1re heure). En `Les deux sens (hedge)`, le moteur Pine **nette** les positions
  opposées → backtest fiable surtout en Long/Short/Net.
- Levier ≤ 100× (plafond technique de la marge `const`).

## 2. Choix d'implémentation

### Découpage en 4 libs — la strat est un orchestrateur moteur
| Lib | Couche | Rôle |
|-----|--------|------|
| `lib_strat_range` | 1 | détection setups (machine d'état d'excursion + cassure/réintég) + géométrie SL/TP + `swingStop` + `entryTrigger` — **range-agnostique** |
| `lib_risk` | 1 | `riskSize` (sizing + cap levier), `positionEvents` (BE/runner/sortie), `directionAllowed` (hedge/net), enums `BeMode`/`DirMode` |
| `lib_strat_draw` | 2 | tout le rendu de position (box/lignes/rayon + cycle extend/freeze/fade) |
| `lib_levels` | 1 | source du range (`openRange`) |

- **Pourquoi ce découpage** : une lib Pine **ne peut pas appeler `strategy.*`**. Tout le métier *pur*
  (détection, risque, rendu) est donc extrait en libs réutilisables ; seule la **glue moteur**
  (`strategy.entry/exit`, `opentrades`, arrays, dessin différé) reste dans la strat — c'est le plancher
  incompressible. Un futur `2Ai_Strat_Asian` réutilise les 3 libs à 100 % (range = `sessionHL`).
- **Range-agnostique** : `lib_strat_range` prend `rangeHigh/rangeLow` en paramètres ; elle ne « sait » pas
  d'où vient le range.

### Entrée au marché (et non ordre stop)
Une 1re version posait un ordre **stop** sur la mèche de la bougie — il ne filait quasi jamais. Abandonné
au profit du modèle **bulle→flèche** (entrée marché à la cassure de l'extrême du signal),
`process_orders_on_close = true`.

### Levier — `const` imposé par Pine
`margin_long`/`margin_short` exigent une **constante** (un `input` lève **CE10123**). On fixe donc
`margin = 1` (plafond technique 100×) et le **levier opérationnel est un input** qui **cape la qty** dans
`risk.riskSize` (`notionnel ≤ levier × equity`). Évite les rejets broker tout en gardant le réglage en UI.

### Dessin différé au fill
À l'entrée on **place** l'ordre + on enregistre un `Pending` (rien dessiné). La box/lignes/rayon ne sont
créés qu'à la barre où `liveSize ≠ 0` (fill confirmé via `strategy.opentrades`), ancrés à la bougie
d'entrée. Un ordre non filé (qty capée à 0 par le capital) est purgé sans trace.

### Exposition restante = rayon + label live
La taille restante affichée provient de `strategy.opentrades.size()` (signée), recalculée chaque barre,
donc elle **décroît à chaque TP filé**. Le rayon est porté `RUN_LEAD` (5) barres devant la barre courante.

### Zone OR & lignes ancrées au début
`openRange` renvoie des `orH/orL` **running** pendant la fenêtre → la zone (box) se construit en live et
les lignes OR sont des **objets `line`** ancrés à `orStart` (et non des `plot` démarrant à `orEnd`).
`bgcolor`/`plot`/`box` restent **inline** dans la strat (Pine interdit ces fonctions d'affichage globales
en lib ; c'est de la visualisation de *range*, pas de *position*).

### Contrainte position nette
Une stratégie Pine ne porte qu'**une position nette**. Un setup de sens **opposé** à la position en cours
est **ignoré** (sinon `strategy.entry` inverserait la position).

### Performance
- `ta.*` (swing) calculés hors bloc conditionnel. Détection sur bougie fermée (`barstate.isconfirmed`)
  côté lib → pas de repaint.
- `max_boxes_count = max_lines_count = max_labels_count = 500` ; au-delà, FIFO (positions/jours anciens).

## 3. Cas de test (à valider manuellement dans TradingView)

| # | Instrument | TF | Scénario | Comportement attendu | Statut |
|---|------------|----|----------|----------------------|--------|
| 1 | cash US (ES/NQ) | M5 | Cassure haute nette | Entrée long à la flèche, SL/TP tracés, 3 TP + runner | ⏳ |
| 2 | cash US | M5 | Cassure basse | Entrée short symétrique | ⏳ |
| 3 | cash EU (DAX) | M15 | Mèche au-dessus de orH puis clôture dedans | Réintégration haute → short ; SL selon le mode choisi | ⏳ |
| 4 | tout | M5 | Mode SL « Plus haut/plus bas » sur la cassure | SL = plus bas N bougies − tolérance (long) ; idem miroir | ⏳ |
| 5 | tout | M5 | TP1 atteint, BE = « Au TP1 » | Box SL grisée, stop du reliquat ramené à l'entrée | ⏳ |
| 6 | tout | M5 | TP3 atteint avec runner | Box principale figée ; rayon d'exposition continue ; label = taille runner | ⏳ |
| 7 | tout | M5 | Position fermée (stop) | Box estompée, rayon + label **supprimés** (aucune trace d'expo) | ⏳ |
| 8 | tout | M5 | SL serré (notionnel > equity) à levier faible | Qty capée par le levier ; aucun label vide / box fantôme | ⏳ |
| 9 | crypto 24h | M15 | Changement de TZ | OR cohérent avec le « minuit » de la TZ choisie | ⏳ |
| 10 | tout | M5 | Plafond `Trades max / jour` atteint | Setups suivants ignorés ce jour | ⏳ |
| 11 | tout | M5 | Mode `Sens unique (net)`, setup opposé à la position | Setup ignoré (pas de flip) | ⏳ |
| 12 | tout | M5 | Visuel Open Range | Zone bleue construite en live sur la 1re heure + lignes OR démarrant à `orStart` | ⏳ |

> **Non testé automatiquement** : Pine n'a pas de framework de tests. La fiabilité du backtest en mode
> `Les deux sens (hedge)` est limitée par le netting moteur (cf. § Limites) — valider les perfs en
> Long/Short/Net.
