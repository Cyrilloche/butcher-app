# Assistant vocal — Jeu de phrases de test

> Rattaché à `docs/cadrage-assistant-vocal.md` (Q-03). Sert deux fois : **maintenant**, pour éprouver la transcription sur des voix différentes ; **pendant le spike**, comme jeu de référence pour comparer fournisseurs et réglages sur les mêmes enregistrements.

| Version | Date | Objet |
|---|---|---|
| 0.1 | 2026-09-22 | Premier jeu : 40 phrases, protocole et grille de relevé |

---

## 1. Avant de tester

**Catalogue supposé** — à ajuster au catalogue réel avant les tests :

| Produit | Code | Mode | Particularité |
|---|---|---|---|
| Saucisson | `SC` | Au poids | Sachets pesés un à un |
| Jambon | `JB` | Au poids | Vente à la tranche autorisée |
| Terrine | `TR` | À la pièce | Pas de poids |

**Clients de test** — à créer dans la base de test, choisis pour piéger la reconnaissance :

| Client | Piège visé |
|---|---|
| Mme Martin | Homophone de « Martine » |
| Martine Roux | Prénom = nom d'un autre client |
| Josette Dubois | Appelée « la Josette » à l'oral |
| Paul Lefèvre | Accent et orthographe (Lefèvre / Lefebvre) |
| les Moreau | Un foyer, pas une personne |
| Gérard | Prénom seul, sans nom |

Un nom **absent** de la liste est volontairement utilisé (« Mme Petitjean ») : il doit être retiré, pas deviné (D-08).

## 2. Protocole

**Ne pas lire les phrases d'une voix de lecture.** Lire la phrase une fois, poser la feuille, puis la dire comme on parlerait au téléphone. Les hésitations, les « euh » et les reprises font partie du test.

Pour chaque testeur, relever une fois : tranche d'âge, voix grave ou aiguë, accent éventuel, modèle de téléphone.

Trois conditions, si possible :

| Condition | Description |
|---|---|
| **Calme** | Pièce silencieuse, téléphone à 20–30 cm |
| **Cuisine** | Bruit de fond réel (hotte, eau, radio basse) |
| **Loin** | Téléphone posé sur la table, à 1 m |

**Garder les enregistrements** (avec l'accord de chaque testeur) : ce sont eux qui permettront de rejouer exactement les mêmes voix sur chaque fournisseur, au lieu de refaire parler tout le monde.

## 3. Les phrases

La colonne « Attendu » décrit ce que l'assistant doit comprendre, pas ce qu'il doit répondre.

### A. Questions sur le stock

| # | Phrase | Attendu |
|---|---|---|
| A1 | Il me reste combien de saucissons ? | Stock `SC` |
| A2 | Combien j'ai de jambon ? | Stock `JB`, entamés compris |
| A3 | Est-ce qu'il reste des terrines ? | Stock `TR` |
| A4 | Qu'est-ce qu'il me reste en stock ? | Stock de tous les produits |
| A5 | Il en reste combien, du saucisson de la semaine dernière ? | Stock `SC`, fournées de la semaine précédente |
| A6 | Le jambon entamé, il en reste combien à peu près ? | Poids restant du jambon entamé |
| A7 | J'ai encore du sauciss… du saucisson ? | Stock `SC` (mot repris) |
| A8 | Dis-moi voir combien il reste de terrines | Stock `TR` (tournure familière) |

### B. Ventes simples

| # | Phrase | Attendu |
|---|---|---|
| B1 | Vends deux saucissons à madame Martin | 2 × `SC`, les plus anciens ; client Mme Martin ; non payé |
| B2 | Mets un saucisson pour Martine | 1 × `SC` ; client Martine Roux (et pas Mme Martin) |
| B3 | La Josette a pris trois terrines | 3 × `TR` ; client Josette Dubois |
| B4 | Une terrine pour Gérard | 1 × `TR` ; client Gérard |
| B5 | J'ai vendu un saucisson et une terrine aux Moreau | 1 × `SC` + 1 × `TR` ; client les Moreau |
| B6 | Paul Lefèvre, deux saucissons | 2 × `SC` ; client Paul Lefèvre (ordre inversé) |
| B7 | Euh… vends… attends… deux saucissons à madame Martin | Comme B1 (hésitations) |
| B8 | Vends trois saucissons, non deux, à madame Martin | 2 × `SC` (correction en cours de phrase) |

### C. Poids et prix précisés

| # | Phrase | Attendu |
|---|---|---|
| C1 | Un saucisson d'environ 300 grammes pour madame Martin | 1 × `SC`, le plus proche de 300 g |
| C2 | Donne un gros saucisson à Gérard | 1 × `SC` ; « gros » non chiffré → plus ancien, à vérifier à l'écran |
| C3 | Un saucisson à 8 euros pour la Josette | 1 × `SC`, montant théorique le plus proche de 8 € |
| C4 | Un saucisson de trois cent cinquante pour Paul | 1 × `SC`, le plus proche de 350 g (unité sous-entendue) |
| C5 | Un demi-kilo de saucisson pour les Moreau | 500 g de `SC` → sachet(s) le(s) plus proche(s), à vérifier à l'écran |

### D. Jambon à la tranche

| # | Phrase | Attendu |
|---|---|---|
| D1 | 200 grammes de jambon pour madame Martin | Jambon entamé le plus ancien, 200 g vendus |
| D2 | Coupe deux cent cinquante grammes de jambon à Gérard | Idem, 250 g |
| D3 | Une livre de jambon pour les Moreau | Idem, 500 g (« livre » = 500 g) |
| D4 | Quatre tranches de jambon pour la Josette | Jambon entamé ; poids **non** déductible → à saisir à l'écran |
| D5 | Un jambon entier pour Paul Lefèvre | 1 jambon entier (non entamé), vente en une fois |

### E. Paiement

| # | Phrase | Attendu |
|---|---|---|
| E1 | Deux saucissons à madame Martin, elle a payé | Payé |
| E2 | Une terrine pour Gérard, il me paiera la semaine prochaine | Non payé |
| E3 | Madame Martin a payé en liquide pour ses deux saucissons | 2 × `SC`, payé (ordre inversé) |
| E4 | Trois terrines aux Moreau | Non payé (rien dit → défaut) |

### F. Cas difficiles

| # | Phrase | Attendu |
|---|---|---|
| F1 | Vends deux saucissons à madame Petitjean | 2 × `SC` ; **client retiré** (inconnu), à choisir à l'écran |
| F2 | Vends deux saucissons | 2 × `SC` ; sans client |
| F3 | Vends quelque chose à madame Martin | Client Mme Martin ; produit à choisir |
| F4 | Madame Martin | Rien d'exploitable seul : l'assistant dit ne pas avoir compris |
| F5 | Vends dix saucissons à Gérard | Plus que le stock ? L'assistant le signale, pré-remplit le disponible |
| F6 | Vends deux chorizos à madame Martin | Produit inconnu : signalé, pas remplacé par un autre |
| F7 | Mets des chansons pour enfants | Hors périmètre : refus poli, pas d'action |
| F8 | Annule la dernière vente | Hors périmètre du POC : refus poli, pas d'action |
| F9 | Vends deux saucissons à madame Martin et une terrine à Gérard | Deux clients dans une phrase : une seule vente possible → signalé |
| F10 | Combien il reste de saucissons, et mets-en deux pour Gérard | Question + vente dans la même phrase |

## 4. Grille de relevé

Une ligne par phrase, par testeur et par condition. Pour la phase de transcription seule, seules les trois premières colonnes comptent.

| Testeur | Condition | # | Transcription obtenue | Transcription juste ? (oui / nom faux / autre erreur) | Intention juste ? | Remarques |
|---|---|---|---|---|---|---|
| | | | | | | |

Points à surveiller en priorité :
- **Les noms de clients** : c'est l'erreur la plus coûteuse, et la pseudonymisation en dépend (`cadrage-assistant-vocal.md` §5).
- **Les nombres** : « deux cent cinquante » transcrit en « 200 50 », « un demi » perdu.
- **Les voix âgées et le bruit de cuisine** : ce sont les conditions réelles d'usage.
