# 2Ai_Strat_Asian — Stratégie session asiatique

Stratégie de **range** appliquée au **range de la session asiatique** (High/Low de la session). C'est la
**même stratégie que `2Ai_Strat_OR`** — voir **`strat-OR.md`** pour le détail des fonctionnalités, du
découpage en libs et de la gestion de position. Ce document ne couvre que **ce qui diffère**. Référence
comportementale : `SPECIFICATIONS.md` § « Stratégie 2 : 2Ai_Strat_Asian ».

## 1. Ce qui change vs 2Ai_Strat_OR

### Source du range
- Range = **High/Low de la session asiatique**, via `lib_levels.sessionHL(session, tzSession, tzChart)`
  (au lieu de `openRange(tz)` qui prend la 1re heure du jour).
- `sessionHL` renvoie le H/L **running** pendant la session (figé après la fin de session) — même logique
  d'alimentation que l'OR.

### Inputs de range (groupe « Range (session asiatique) »)
| Input | Défaut | Note |
|-------|--------|------|
| Session asiatique | `0800-1400` (`input.session`) | défaut repris de `2Ai Levels` |
| TZ session | `Asia/Tokyo` | TZ IANA de la session |
| TZ chart (reset jour) | `Europe/Paris` | « minuit » chart : borne la session au jour + reset l'état |

⚠ La session **ne doit pas traverser minuit** dans sa TZ (`sessionHL` ne supporte pas le cross-midnight).
L'asiatique en heure de Tokyo (08:00-14:00) est OK.

### Rendu du range
- Zone (rectangle sans bordure) construite **en live pendant la session** (flag `inSession`), bornée aux
  bornes du range, ancrée au début de session ; lignes High/Low démarrant au début de session. Couleur
  **orange** (cohérence avec la session asiatique dans `2Ai Levels`).

### Reset
- `dayChange` calé sur `chartTz` (= reset interne de `sessionHL`).

## 2. Choix d'implémentation
Tout le métier (détection cassure/réintégration, géométrie SL/TP, swingStop, trigger, sizing/risque, rendu
de position) est **partagé via les libs** (`lib_strat_range`, `lib_risk`, `lib_strat_draw`) — voir
`strat-OR.md`. La strat n'est qu'un orchestrateur ; **seule la ligne de source du range diffère**
(`sessionHL` au lieu de `openRange`). C'est précisément l'objectif de l'extraction en libs : une nouvelle
strat de range = un nouvel orchestrateur mince qui change la source.

## 3. Cas de test (à valider manuellement)

Reprendre **tous les cas de `strat-OR.md`** (cassure, réintégration, modes SL, BE, runner, plafonds,
dessin différé, levier), **plus** les cas spécifiques à la session :

| # | Instrument | TF | Scénario | Attendu | Statut |
|---|-----------|----|----------|---------|--------|
| A1 | indice/crypto 24 h | M5/M15 | Session `0800-1400` Asia/Tokyo | Zone tracée sur 08:00-14:00 Tokyo, range figé à 14:00 | ⏳ |
| A2 | tout | M5/M15 | Passage heure été/hiver | L'open de session suit la TZ Tokyo (aucun décalage) | ⏳ |
| A3 | tout | M5/M15 | Session définie traversant minuit | Comportement non garanti — à éviter (limite `sessionHL`) | ⏳ |
| A4 | tout | H4 | Chart au-dessus de H1 | Strat bypassée : aucun ordre, aucun tracé (`tfSupported`) | ⏳ |

> **À adresser (cf. suivi projet)** : fidélité du backtest en mode **hedge** (Pine nette les positions
> opposées — le Strategy Tester ne reflète pas un vrai hedge) ; **commission/slippage** absents. Communs
> aux deux strats.
