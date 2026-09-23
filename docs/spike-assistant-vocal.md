# Spike — Assistant vocal

> Plan du spike de R&D, branche `feat/assistant-vocal`. Les décisions de fond sont dans `docs/cadrage-assistant-vocal.md` (références `D-xx`, `Q-xx`) ; ce document dit **comment on les éprouve**, dans quel ordre, et à quoi on reconnaît un succès. Issue attendue : un **go / no-go** argumenté et le brouillon de l'**ADR-012**. Le fonctionnement de ce qui a été construit est expliqué dans `docs/assistant-vocal-fonctionnement.md`.

| Version | Date | Objet |
|---|---|---|
| 0.1 | 2026-09-22 | Premier plan : questions, étapes, mesures, critères |
| 0.2 | 2026-09-22 | Seuils de réussite arrêtés et justifiés |
| 0.3 | 2026-09-22 | Résultats des étapes 0 et 1 |
| 0.4 | 2026-09-22 | Résultats de l’étape 2, comparaison C# et Python |

---

## 1. Les questions auxquelles le spike répond

| # | Question | Étape |
|---|---|---|
| S-1 | La transcription comprend-elle **nos** voix, nos noms de clients et nos nombres, au calme comme en cuisine ? | 1 |
| S-2 | La transcription locale (`faster-whisper`, 2 Go) fait-elle jeu égal avec Voxtral, et en combien de temps ? | 1 |
| S-3 | Retrouve-t-on le bon client à partir d'un nom transcrit approximativement, sans en confondre deux ? | 2 |
| S-4 | Un LLM qui ne voit que des jetons comprend-il l'intention et remplit-il les bons champs ? Small suffit-il, ou faut-il Medium ? | 3 |
| S-5 | Les chiffres prononcés sont-ils **exactement** ceux du serveur ? | 3 |
| S-6 | Le parcours complet (appuyer, parler, obtenir une réponse ou un formulaire rempli) est-il assez rapide et agréable pour être adopté ? | 4 |

Ordre choisi : **du moins cher au plus cher**. Les étapes 1 à 3 se mesurent sur des fichiers, sans écran ; on ne construit l'interface qu'une fois la chaîne jugée crédible.

## 2. Hors périmètre du spike

- Toute écriture en base par l'assistant (D-02) : il lit, et il **propose** un brouillon de vente.
- Conversation à plusieurs tours, question vocale en retour, mot déclencheur.
- Déploiement (D-09), activation par compte, journalisation des demandes vocales.
- Qualité de code de production : le spike peut être jeté. Seules deux pièces ont vocation à survivre, et sont donc testées comme du code de production : la **reconnaissance des clients** (étape 2) et le **choix des unités** (étape 3).

## 3. Préalables

| Préalable | Détail |
|---|---|
| Base de test | Base de dev avec le catalogue et les **clients fictifs** du jeu de phrases (`assistant-vocal-phrases-test.md` §1). **Jamais de vrai nom de client** envoyé à Mistral pendant le spike. |
| Compte Mistral | Paiement à l'usage (entraînement désactivé par défaut, à vérifier dans la console). Plafond de dépense bas. |
| Clé d'API | Dans `development/.env` (`MISTRAL_API_KEY`), lue par `make run` — comme le reste de la configuration de dev, pas de `dotnet user-secrets`. |
| Enregistrements | Dossier `development/assistant-corpus/`, **ignoré par git** : ce sont des voix de personnes réelles. Un fichier par phrase, nommé `<testeur>-<condition>-<n° phrase>.webm`, avec un `corpus.csv` qui porte la phrase attendue. |
| HTTPS sur le téléphone | Le micro (`getUserMedia`) n'est accessible qu'en contexte sécurisé. `localhost` suffit sur le PC ; pour tester depuis le téléphone en réseau local, il faut du HTTPS (Caddy local avec certificat interne, ou un tunnel de dev). À régler avant l'étape 4. |

## 4. Étapes

### Étape 0 — Constituer le corpus

- Enregistrer le jeu de phrases avec plusieurs voix et dans les trois conditions (calme, cuisine, loin), **au format que produira le téléphone** : `webm/opus` depuis Chrome Android. Une page d'enregistrement minimale (bouton, phrase affichée, fichier téléchargé) évite de tricher avec un autre micro.
- Livrable : le corpus et son `corpus.csv`.

### Étape 1 — Banc de transcription (S-1, S-2)

Script hors application, `spikes/assistant-vocal/transcription/` (Python : `faster-whisper` est une bibliothèque Python, et le banc n'a pas à vivre dans le backend).

- Passe chaque fichier du corpus dans : **Voxtral Mini Transcribe 2** (API), avec et sans vocabulaire guidé (produits seulement, jamais les clients — cadrage §9) ; **faster-whisper** *small* puis *medium*, int8, GPU bridé à 2 Go.
- Mesure, par moteur et par condition :
  - taux d'erreur sur les mots ;
  - **nom du client juste** (oui / non), mesure la plus importante ;
  - **nombres justes** (« deux cent cinquante », « un demi-kilo ») ;
  - durée de transcription (le PC est plus rapide que la P600 : ces durées sont un plancher, cadrage D-10).
- Livrable : un tableau de résultats par moteur, par condition et par testeur.

### Étape 2 — Reconnaissance des clients (S-3)

Code C#, dans le backend : un `CustomerNameMatcher` qui reçoit un texte transcrit et la liste des clients, et rend le texte pseudonymisé et la table jeton → client.

- Normalisation : minuscules, accents, civilités et articles (« madame », « la », « les »).
- Comparaison approximative : distance d'édition et encodage phonétique adapté au français, à comparer.
- Règle de prudence : **deux candidats trop proches (« Martin » / « Martine ») donnent un client non choisi**, pas un pari. Mieux vaut un client à choisir à l'écran qu'une vente au mauvais nom.
- Un nom non reconnu est retiré (D-08). Difficulté connue : repérer qu'un morceau de phrase **est** un nom quand il ne ressemble à aucun client (« madame Petitjean ») ; le LLM ne peut pas aider, puisqu'il ne doit pas le voir. Piste : repérer les civilités et les prépositions (« à », « pour ») suivies d'un mot inconnu.
- Tests unitaires sur les transcriptions réelles de l'étape 1.
- Mesure : bon client trouvé, **mauvais client choisi** (doit être nul), nom inconnu effectivement retiré.

### Étape 3 — Compréhension et outils, sur du texte (S-4, S-5)

Point d'entrée de spike `POST /api/assistant/text` (texte en entrée, pas encore d'audio) : pseudonymisation (étape 2), appel au LLM avec outils, exécution, réponse.

**Outils exposés au LLM** — le LLM voit le catalogue (non personnel), jamais les clients :

| Outil | Paramètres | Ce que fait le backend |
|---|---|---|
| `get_stock` | `product_code?` (absent = tout le stock) | Compte les unités `available` / `opened`, somme `remaining_weight`, détail par fournée. Calcul **serveur**, sur les mêmes règles que les écrans de stock. |
| `draft_sale` | `customer_token?`, `lines[]` : `product_code`, `quantity?`, `weight_g?`, `price_eur?`, `slice?` ; `paid?` | Applique les règles de choix des unités (cadrage §6) et rend un brouillon. **N'écrit rien.** |
| `not_understood` | `reason` | Demande hors périmètre ou inexploitable (phrases F4, F7, F8). |

**Choix des unités** : un `SaleDraftBuilder` déterministe, testé unitairement, qui applique les règles du cadrage §6 (les plus anciennes, poids le plus proche, prix le plus proche, jambon entamé le plus ancien, non payé par défaut) et signale ce qu'il n'a pas pu faire (stock insuffisant, produit inconnu, poids non déductible des « tranches »).

**Réponse du point d'entrée** :

```jsonc
// Question de stock
{ "kind": "answer", "speech": "Il te reste 12 saucissons, environ 3,4 kilos…", "stock": { /* détail par fournée */ } }
// Vente
{ "kind": "sale_draft", "customerId": 42 /* ou null */, "paid": false,
  "lines": [ /* stockUnitId, isFullSale, soldWeight, amount */ ], "warnings": [ "…" ] }
// Incompris
{ "kind": "not_understood", "speech": "Je n'ai pas compris…" }
```

**Mesures**, sur les transcriptions de l'étape 1 et sur les phrases écrites telles quelles :
- intention juste ; champs justes (produit, quantité, poids, prix, paiement, jeton client) ;
- **fidélité des chiffres** : chaque nombre de `speech` doit figurer dans le résultat de l'outil. Un seul chiffre inventé sur le jeu est éliminatoire pour le modèle ;
- Mistral Small 4 contre Medium 3.5 ; latence et nombre de jetons par demande.

### Étape 4 — Parcours complet (S-6)

- **Frontend** : bouton micro (visible par tous les comptes, D-09), enregistrement `MediaRecorder` avec retour visuel (« je t'écoute »), envoi à `POST /api/assistant` (audio).
- **Backend** : transcription Voxtral, puis la chaîne de l'étape 3.
- **Réponse de stock** : `speech` lu par `speechSynthesis` (voix française du téléphone), détail affiché (cadrage §7).
- **Brouillon de vente** : ouverture de `SaleAddView` pré-remplie (client, panier, « À payer »), avertissements affichés au-dessus. Le formulaire accepte déjà un client (RU-04) ; il faut lui passer aussi le panier.
- Mesure : délai entre la fin de la parole et la réponse, sur le téléphone, et ressenti des testeurs.

### Étape 5 — Bilan

- Tableau de résultats des étapes 1 à 4, face aux critères du §5.
- Brouillon de l'**ADR-012** (fournisseur, pseudonymisation, conservation de 30 jours, option A ou B).
- Go / no-go, et si go, la liste de ce qui passe en spec `specs/006-assistant-vocal`.

## 5. Critères de réussite

Seuils **arrêtés le 2026-09-22, avant toute mesure** : ils ne se changent pas au vu des résultats. Un seuil manqué se discute dans le bilan, il ne se déplace pas.

| Critère | Seuil | Pourquoi ce niveau |
|---|---|---|
| Nom du client juste à la transcription, au calme | ≥ 90 % | Au-delà d'une erreur sur dix, on choisit le client à l'écran presque à chaque fois, et l'assistant n'apporte plus rien. |
| Nom du client juste à la transcription, en cuisine | ≥ 75 % | Condition réelle d'usage, mais un nom raté n'est pas grave : il est retiré et se choisit à l'écran. |
| **Mauvais client choisi** par la reconnaissance | **0** sur tout le jeu | Une vente au mauvais nom fausse les impayés et la traçabilité sans que personne ne s'en aperçoive. |
| Intention juste (texte bien transcrit) | ≥ 95 % | Le texte est juste : une erreur ici est celle du LLM seul, et elle se corrige par le choix du modèle. |
| Champs justes d'un brouillon de vente | ≥ 90 % | Le formulaire se relit avant validation (D-02) : une correction de temps en temps est acceptable. |
| **Chiffre inventé** dans une réponse | **0** sur tout le jeu | Une réponse orale n'est pas relue : un stock faux dit avec assurance fait refuser une vente ou promettre ce qui n'existe pas. |
| Délai fin de parole → réponse, sur le téléphone | ≤ 4 s en médiane | Ordre de grandeur des assistants grand public : au-delà, on croit que ça n'a pas marché et on recommence. |
| Coût par demande | ≤ 0,01 $ | Dix fois l'estimation (cadrage §9) : ce seuil ne sert qu'à détecter une erreur de conception. |

Les deux critères à zéro sont éliminatoires : une vente au mauvais client ou un stock faux dit avec assurance coûtent plus que l'assistant ne fait gagner.

## 6. Risques

| Risque | Parade |
|---|---|
| Les noms propres sont mal transcrits, quel que soit le moteur | Vocabulaire guidé (produits), prudence du `CustomerNameMatcher`, client à choisir à l'écran : le parcours reste utile même sans nom. |
| Le repérage d'un nom inconnu laisse passer un nom vers le LLM | Mesuré à l'étape 2 ; si le taux de fuite n'est pas nul, l'écrire dans l'ADR-012 plutôt que le taire. |
| Le LLM reformule un chiffre | Critère éliminatoire ; repli possible : une phrase construite par le backend à partir d'un modèle, sans LLM pour la réponse orale. |
| Chrome Android enregistre dans un format refusé par un moteur | Vérifié dès l'étape 0 ; conversion par `ffmpeg` côté backend si besoin. |
| Latence cumulée (envoi, transcription, LLM, synthèse) trop longue | Mesurée par tranche à l'étape 4 ; pistes : transcription temps réel, modèle plus petit. |

## 7. Résultats

### Étape 0 — Corpus (2026-09-22)

Une voix (porteur du projet, 30–50 ans, voix moyenne, Samsung A55, Chrome), 40 phrases au calme. Format confirmé : `webm/opus`, 17 à 52 Ko par prise. Manquent : d'autres voix, dont celle de l'utilisateur visé, et les conditions « cuisine » et « loin ».

**Biais à garder en tête** : le testeur connaît le but de chaque phrase. Une voix qui l'ignore sera plus révélatrice.

### Étape 1 — Transcription (2026-09-22)

Ce qui est mesuré : la présence, dans la transcription, du **client**, des **nombres** et des **produits** attendus (`transcription/score.py`). Pas d'écart mot à mot : la phrase est reformulée librement, par protocole.

| Moteur | Client juste | Nombres justes | Produits justes | Durée médiane | Mémoire vidéo |
|---|---|---|---|---|---|
| **Voxtral Mini Transcribe 2** | **27/30 (90 %)** | 20/23 (87 %) | 35/37 (95 %) | **0,48 s** | — |
| Voxtral, vocabulaire guidé | 25/30 (83 %) | 20/23 (87 %) | 36/37 (97 %) | 0,51 s | — |
| faster-whisper *medium*, vocabulaire guidé | 20/30 (67 %) | 18/23 (78 %) | 37/37 (100 %) | 2,56 s | 1,28 Go |
| faster-whisper *medium* | 20/30 (67 %) | 19/23 (83 %) | 32/37 (86 %) | 2,43 s | 1,16 Go |
| faster-whisper *small* | 15/30 (50 %) | 19/23 (83 %) | 19/37 (51 %) | 0,95 s | 1,10 Go |

Durées Whisper mesurées sur la T1200 du PC de développement, en int8 : la P600 sera plus lente. Durées Voxtral : aller-retour réseau compris.

**Lecture** :
- **Voxtral atteint le seuil de 90 % au calme.** Ses trois clients manqués sont tous « les Moreau », transcrit « les moraux », « au mot » : même prononciation, que la reconnaissance phonétique de l'étape 2 doit rattraper. Aucun client n'est transcrit en un **autre** client connu.
- **« deux » devient « de »** (« Vent de saucisson », « Paul Lefebvre de saucisson ») et « dix » se perd une fois : homophones du français. Le nombre manquant se voit dans le formulaire pré-rempli (D-02) ; à surveiller à l'étape 3.
- **Le verbe « vends » est fragile** (« Bon », « Vent », « Mais »). Il pèse peu : « deux saucissons à madame Martin » dit déjà une vente. À vérifier à l'étape 3.
- **Le vocabulaire guidé n'apporte rien à Voxtral** : il aide les produits d'une unité mais fait perdre deux clients (« Mme Barthin »). Conforme à la documentation, qui le dit expérimental hors anglais. **Écarté.**
- **Whisper *medium* tient dans 2 Go** (1,3 Go au pic) mais reste loin de Voxtral sur les clients, et sa durée sur la T1200 (≈ 2,5 s) laisse prévoir un dépassement du délai de 4 s sur la P600. **L'option A n'est pas viable au niveau de qualité visé** ; elle ne reviendrait qu'avec un meilleur modèle local.

**Conclusion provisoire** : Voxtral Mini Transcribe 2, sans vocabulaire guidé. À confirmer sur d'autres voix et en cuisine.

### Étape 2 — Reconnaissance des clients (2026-09-22)

Deux versions, mêmes règles de prudence, même jeu : les 200 transcriptions de l'étape 1 et 16 phrases écrites pour éprouver les noms inconnus (source « écrit » : noms absents de la base, avec et sans majuscule, et pièges comme « pour Noël »).

- **C#** (`CustomerNameMatcher`) : phonétique française et règles écrites à la main, aucune dépendance.
- **Python** (`spikes/assistant-vocal/names/prototype.py`) : espeak-ng pour la prononciation, rapidfuzz, spaCy `fr_core_news_md` pour repérer les noms. Presidio écarté : en français, sa détection des noms repose sur spaCy, qui est testé directement.

| Mesure | C# | Python |
|---|---|---|
| **Mauvais client choisi** (216 phrases) | **0** | **0** |
| Clients trouvés — Voxtral | 27/29 (93 %) | 27/29 (93 %) |
| Clients trouvés — Voxtral, vocabulaire guidé | 27/29 | 26/29 |
| Clients trouvés — Whisper *medium* | 25/29 | 25/29 |
| Nom inconnu du corpus retiré (Petitjean) | oui | oui |
| **Noms inconnus restés dans le texte** — phrases écrites | **4/13** | **1/13** |
| Fausses alertes (« Noël », « Pâques » retirés) | 2 | 2 |
| Durée par phrase | < 1 ms | 8 ms (médiane) |

**Lecture** :
- **Le critère éliminatoire tient dans les deux versions** : aucun mauvais client. La règle de prudence a servi sur un vrai cas : Whisper a entendu « Martin » pour « Martine », et aucun client n'a été choisi.
- **Sur les clients connus, les deux versions font jeu égal.** Les deux manques Voxtral sont communs : « pour Martine », non choisi par prudence (Martin existe), et « au mot » pour « aux Moreau », trop loin pour être rattrapé sans risque.
- **Le seul apport de Python est la détection des noms inconnus écrits en minuscules** (« pour bernadette », « jean-pierre »), que spaCy repère et que les règles C# laissent passer. Or Voxtral, le moteur retenu, met une majuscule aux noms propres ; les minuscules viennent de Whisper, écarté.
- **espeak-ng n'est pas meilleur que les règles écrites à la main** sur ce corpus : il prononce le « b » de « Lefebvre », et bascule en anglais sur un nom qu'il croit étranger (« Barthin »).
- Les fausses alertes viennent de la règle des majuscules, commune aux deux versions ; elles retirent un mot sans danger pour la confidentialité.

**Conclusion provisoire** : **garder la version C#**, dans le backend. Un service Python ajouterait un conteneur, un langage et un modèle de 40 Mo pour un gain qui ne concerne pas le moteur retenu. À rouvrir si les voix suivantes montrent Voxtral écrivant des noms en minuscules ; la parade C# la plus simple serait alors de retirer tout mot inconnu après « à » ou « pour », au prix de quelques mots retirés à tort.

**Limite** : un seul nom inconnu dans le corpus réel. Les phrases écrites mesurent la mécanique, pas la façon dont un vrai moteur transcrit un nom qu'il n'a jamais entendu.

### Étape 3 — Compréhension et outils (nuit du 2026-09-22 au 23)

**Écart au plan** : Mistral Small 4 et Medium 3.5 sont **fermés sur le compte** (limite à zéro requête par minute, offre sans facturation) ; Large est refusé. Ministral 3B, 8B et 14B sont ouverts. Le banc a donc comparé **Ministral 14B et 8B**. Aucune facturation n'a été activée : c'est une décision du titulaire du compte. Le banc relance Small et Medium sans changement (`ASSISTANT_EVAL_MODELS`).

Banc : `AssistantEvaluation` (`ASSISTANT_EVAL=1`), 40 phrases écrites et leurs 40 transcriptions Voxtral, stock et clients fictifs. Résultats après la correction des consignes :

| Modèle | Source | Intention | Champs | Produit (stock) | Client juste | **Mauvais client** | Délai LLM médian |
|---|---|---|---|---|---|---|---|
| **Ministral 14B** | phrases écrites | **100 %** | **100 %** | 100 % | 96 % | **0** | 0,87 s |
| **Ministral 14B** | Voxtral | 90 % | 88 % | 88 % | 92 % | **0** | 0,66 s |
| Ministral 8B | phrases écrites | 85 % | 88 % | 100 % | 95 % | 0 | 0,85 s |
| Ministral 8B | Voxtral | 75 % | 74 % | 100 % | 95 % | 0 | 0,87 s |

Environ 1 150 jetons par demande : de l'ordre du dixième de centime.

**Lecture** :
- **Ministral 14B atteint tous les seuils** sur un texte bien transcrit ; 8B ajoute des questions de stock que personne n'a posées et tombe sous les seuils. **14B retenu** (`Assistant:ChatModel`).
- **Les erreurs restantes de 14B viennent de la transcription** (« terrain », « Vent de saucisson », « de » pour « deux »), sauf une : « corisaux » (chorizo mal transcrit) devient du saucisson, malgré la consigne. Visible dans le formulaire, à surveiller.
- **Un résultat qui change la conception** : au premier passage, 14B a dit « 1 jambon entier » pour 2. Le chiffre 1 existait ailleurs dans les données (1 entamé), et le garde-fou, qui vérifie qu'un chiffre existe et non qu'il est à sa place, ne l'a pas vu. **La phrase dite est désormais construite par le backend** ; le LLM ne sert plus qu'à comprendre la demande. La mise en phrase par le LLM reste mesurable (`Assistant:LlmSpeech`), mais n'est plus utilisée.
- Corrections faites en cours de nuit : une réponse pour chaque outil appelé (Mistral l'exige), garde-fou admettant « 3 kilos et 660 », consignes sur les corrections (« trois, non deux »), les produits inconnus et les ventes seules.

Vérifié ensuite sur la base de dev (point d'entrée réel) : question de stock, vente entière avec paiement, tranche de jambon cru. Le catalogue réel a révélé un défaut de rédaction (« 11 saucisse currys ») : le nom d'un produit n'est plus accordé (« Saucisse curry : il t'en reste 11 »).

### Étape 4 — Bouton micro

Livré, à essayer sur téléphone :
- bouton micro rond en bas à gauche, sur tous les écrans (le coin droit est au bouton « + ») ; un appui pour parler, un second pour envoyer, arrêt à 15 s ;
- la réponse s'affiche dans un panneau : ce qui a été entendu, la phrase (lue à voix haute), le détail par fournée ou les avertissements d'une vente ;
- « Ouvrir la vente » ouvre « Nouvelle vente » pré-remplie ; les montants sont calculés par le formulaire, comme pour un choix à la main ; rien n'est enregistré sans le bouton habituel ;
- « Écrire plutôt » : la même chose au clavier, utile sur PC.

Le résultat de l'essai reste à mesurer : délai réel sur le téléphone, et ressenti.

### Essais sur téléphone (2026-09-23)

Porteur du projet, Samsung A55, Chrome, base de dev.

- **Verdict** : « pas mal franchement pour un POC ». La chaîne complète fonctionne sur téléphone.
- **Bouton micro** jugé trop encombrant → intégré au « + » des listes (« Dicter » à côté de l'action de l'écran). Coût : un appui de plus pour l'action habituelle, à confirmer à l'usage.
- **Coupure du micro** (arrêt fixe à 15 s) → remplacée par la détection du silence (2 s après la parole, 30 s au plus).
- **Compteur irrégulier** (un second minuteur lancé par « Reparler ») → remplacé par une jauge du volume.
- **Voix du téléphone** jugée médiocre → **voix « Marie » de Voxtral TTS : « ça change absolument tout »**. Environ 1 s de plus avant d'entendre la réponse, le texte restant immédiat. Seule voix française préréglée chez Mistral ; le clonage de voix est possible (2 à 3 s d'enregistrement, avec l'accord de la personne).
- Défaut trouvé à l'essai et corrigé : l'audio de Chrome (`audio/webm;codecs=opus`) était refusé par le backend (erreur 500) ; aucun test ne passait par un vrai enregistrement.

Reste à mesurer : le délai de bout en bout chronométré, et surtout l'essai par **l'utilisateur visé**.

## 8. Bilan (2026-09-23)

### Face aux critères (§5)

| Critère | Seuil | Résultat | Verdict |
|---|---|---|---|
| Nom du client juste, au calme | ≥ 90 % | 90 % à la transcription (Voxtral), 93 % après reconnaissance phonétique | ✅ |
| Nom du client juste, en cuisine | ≥ 75 % | **non mesuré** (une seule voix, au calme) | ⏳ |
| **Mauvais client choisi** | **0** | **0** sur les 216 phrases de l'étape 2 et les 80 de l'étape 3 | ✅ |
| Intention juste (texte bien transcrit) | ≥ 95 % | 100 % (Ministral 14B) | ✅ |
| Champs justes d'un brouillon de vente | ≥ 90 % | 100 % sur texte bien transcrit, 88 % sur transcription Voxtral | ✅ |
| **Chiffre inventé** dans une réponse | **0** | 0 dit à l'utilisateur, **par construction** : la phrase est écrite par le backend. Le LLM, lui, a commis une erreur de chiffre au banc | ✅ (par conception) |
| Délai fin de parole → réponse | ≤ 4 s médiane | **non chronométré**. Somme des mesures : ≈ 0,5 s (transcription) + ≈ 0,8 s (LLM) + réseau pour le texte, ≈ 1,2 s de plus pour la voix. Ressenti jugé bon à l'essai | ⏳ probable |
| Coût par demande | ≤ 0,01 $ | ≈ 0,001 $ hors voix ; prix de Voxtral TTS non relevé | ✅ probable |

### Ce que le spike a appris

1. **La chaîne tient.** De la voix au formulaire pré-rempli, sur un vrai téléphone, avec des temps de réponse compatibles avec l'usage.
2. **Le LLM comprend, il ne doit pas décider.** Il a dit « 1 jambon entier » pour 2 et remplacé « corisaux » par du saucisson. D'où la règle retenue : le LLM rend une intention et des champs, le backend choisit les unités, résout le client, écrit la phrase ; l'utilisateur valide.
3. **La transcription est le maillon faible**, pas le LLM : « de » pour « deux », « terrain » pour « terrine ». Le formulaire pré-rempli absorbe ces erreurs ; la réponse orale, non.
4. **La voix compte.** La voix de Mistral a été jugée « ça change absolument tout » face à celle du téléphone.
5. **Le local n'est pas prêt** : Whisper tient dans la P600 mais rate un client sur trois ; la pseudonymisation en C#, sans dépendance, vaut le prototype Python.
6. **Les tests sur texte ne suffisent pas** : le premier essai réel a révélé un format audio refusé que 266 tests n'avaient pas vu.

### Ce qui n'a pas été éprouvé

- **L'utilisateur visé.** Tout a été dit par le porteur du projet, qui connaissait les phrases. C'est le risque principal du projet (adoption, R-01) et le premier test à faire.
- Le bruit de cuisine, le téléphone posé, d'autres voix.
- Mistral Small et Medium (fermés sur le compte gratuit).
- Le délai chronométré de bout en bout.

### Recommandation

**Go conditionnel** vers une spec `specs/006-assistant-vocal`, aux conditions suivantes, dans l'ordre :

1. **Un essai par l'utilisateur visé**, sur la branche en local, avant d'écrire la spec : s'il ne s'en sert pas, la spec n'a pas d'objet.
2. **ADR-012 accepté**, avec la question du compte Mistral tranchée (voir l'ADR).
3. Dans la spec : restreindre `/api/assistant/speech` aux phrases de l'assistant, journaliser les demandes vocales (sans l'audio), décider du « + » à deux choix (un appui de plus pour l'action habituelle), mesurer le délai.

La décision revient au porteur du projet.
