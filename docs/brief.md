# Brief fonctionnel — Indicateurs 2Ai

Descriptifs prêts à coller pour accompagner chaque publication TradingView.

---

## 2Ai Levels

Réunit sur une seule surcouche les niveaux de marché clés à surveiller au quotidien.

**Gaps journaliers** — détectés sur la barre daily (et non au minuit-chart), ce qui évite les faux gaps sur les instruments qui cotent 24h (DAX, futures, crypto). Un gap est tracé entre la clôture de la veille et l'extrême du jour, puis suivi : il se rétrécit en cas de comblement partiel et disparaît une fois comblé entièrement.

**Niveaux de périodes précédentes** — PDH/PDL (jour), PWH/PWL (semaine), PMH/PML (mois) et ATH (plus haut historique). Chaque niveau ne s'affiche que si le timeframe du graphique est cohérent avec sa période.

**High / Low de sessions** — sessions asiatique, européenne et américaine. Les extrêmes sont mis à jour en temps réel pendant la session, puis figés jusqu'à la suivante.

**Niveaux d'ouverture** — détection automatique du type d'instrument (cash vs marché 24h) :
- Cash : Open du jour + IB Range (première bougie horaire).
- Non-cash : Open Future, Open EU, Open US + IB Range.

Chaque famille de niveaux dispose de ses propres toggles, couleurs, styles de ligne et labels.

---

## 2Ai Layout

Surcouche de contexte de tendance regroupant les outils de lecture de marché.

**Bandes de Bollinger** — deux jeux indépendants : Classique (20 périodes) et Magique (160 périodes), en mode simple ou ruban. Détection des bandes **plates** ou **en fermeture** (changement de couleur / ligne accentuée) pour repérer les phases de compression et de retournement.

**Moyennes mobiles** — quatre SMA configurables (7 / 20 / 50 / 200), chacune avec toggle, couleur et style propres, affichables en ligne simple ou en ruban.

**Ichimoku** — paramètres traditionnels (9 / 26 / 52). Deux modes : Chikou seule (par défaut) ou affichage complet avec nuage (Kumo) à couleur dynamique.

**Supertrend** — basé sur l'ATR (10, ×3), ligne colorée selon la direction et brisée à chaque retournement.

**Projection** — prolonge en pointillé les moyennes mobiles et/ou les bandes de Bollinger sur les prochaines bougies, pour anticiper leur trajectoire à close constant.

---

## 2Ai Zones (Supply / Demand + FVG)

Met en évidence les zones d'intérêt sur le timeframe affiché.

**Supply / Demand** — construites à partir d'une bougie de signal (CMI) : la zone reprend la bougie précédente dans son intégralité. Une CMI haussière crée une zone Demand (support potentiel), une CMI baissière une zone Supply (résistance potentielle). Les zones meurent une fois entièrement traversées, avec gestion de l'anti-chaînage et du remplacement en cas de chevauchement.

**Fair Value Gaps** — détecte les déséquilibres de prix sur trois bougies consécutives, avec un filtre qui élimine les faux gaps dus aux fermetures overnight, week-ends et breaks de session. Comblement optionnel (la box se réduit puis disparaît).

**Zones « 5 étoiles »** — lorsqu'une zone Supply/Demand et un FVG de même sens se superposent, la zone devient une zone d'intérêt renforcée. Un mode **Safe** permet de n'afficher que ces zones.

---

## 2Ai Zones MTF (CMI multi-timeframes)

Détecte les zones de retournement (CMI) simultanément sur plusieurs timeframes — de M15 à Monthly — et les projette toutes sur le graphique courant.

**Détection** — une CMI est une bougie de retournement identifiée par rapport à une référence (SMA 7 de HL2).

**Validation** — chaque CMI doit survivre aux trois bougies suivantes (son extrême ne doit pas être repris) pour que sa zone soit tracée. Plusieurs CMI peuvent valider en parallèle (typique d'un range).

**Construction** — la zone est bâtie sur le corps des bougies via une fenêtre de lookback, au plus près de l'extrême.

**Hiérarchie des zones** — règles anti-chevauchement intra-timeframe (mise en attente ou remplacement) et cross-timeframe (un timeframe supérieur masque l'inférieur), avec réactivation automatique. Chaque zone n'est visible que sur les timeframes inférieurs ou égaux au sien, et reste active dans sa période d'intérêt propre (de la semaine passée pour le M15 à illimité pour le Monthly).

---

## 2Ai CMI Zones

Détecte les zones de retournement (CMI) sur le timeframe affiché.

**Détection** — une CMI est une bougie de retournement identifiée par rapport à une référence (SMA 7 de HL2), signalant un changement d'intention du marché.

**Validation** — chaque CMI doit survivre aux trois bougies suivantes (son extrême ne doit pas être repris) avant que sa zone soit tracée. Plusieurs CMI peuvent valider en parallèle.

**Construction** — la zone est bâtie sur le corps des bougies via une fenêtre de lookback, au plus près de l'extrême du mouvement.

Chaque zone reste active tant que le prix n'a pas repris son extrême, avec toggle, couleur, style et épaisseur de bordure configurables.
