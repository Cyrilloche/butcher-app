# Spike — Assistant vocal

> Plan du spike de R&D, branche `feat/assistant-vocal`. Les décisions de fond sont dans `docs/cadrage-assistant-vocal.md` (références `D-xx`, `Q-xx`) ; ce document dit **comment on les éprouve**, dans quel ordre, et à quoi on reconnaît un succès. Issue attendue : un **go / no-go** argumenté et le brouillon de l'**ADR-012**.

| Version | Date | Objet |
|---|---|---|
| 0.1 | 2026-09-22 | Premier plan : questions, étapes, mesures, critères |

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

Seuils proposés, à confirmer avant l'étape 1 pour ne pas les ajuster aux résultats :

| Critère | Seuil |
|---|---|
| Nom du client juste à la transcription, au calme | ≥ 90 % |
| Nom du client juste à la transcription, en cuisine | ≥ 75 % |
| **Mauvais client choisi** par la reconnaissance | **0** sur tout le jeu |
| Intention juste (texte bien transcrit) | ≥ 95 % |
| Champs justes d'un brouillon de vente | ≥ 90 % |
| **Chiffre inventé** dans une réponse | **0** sur tout le jeu |
| Délai fin de parole → réponse, sur le téléphone | ≤ 4 s en médiane |
| Coût par demande | ≤ 0,01 $ |

Les deux critères à zéro sont éliminatoires : une vente au mauvais client ou un stock faux dit avec assurance coûtent plus que l'assistant ne fait gagner.

## 6. Risques

| Risque | Parade |
|---|---|
| Les noms propres sont mal transcrits, quel que soit le moteur | Vocabulaire guidé (produits), prudence du `CustomerNameMatcher`, client à choisir à l'écran : le parcours reste utile même sans nom. |
| Le repérage d'un nom inconnu laisse passer un nom vers le LLM | Mesuré à l'étape 2 ; si le taux de fuite n'est pas nul, l'écrire dans l'ADR-012 plutôt que le taire. |
| Le LLM reformule un chiffre | Critère éliminatoire ; repli possible : une phrase construite par le backend à partir d'un modèle, sans LLM pour la réponse orale. |
| Chrome Android enregistre dans un format refusé par un moteur | Vérifié dès l'étape 0 ; conversion par `ffmpeg` côté backend si besoin. |
| Latence cumulée (envoi, transcription, LLM, synthèse) trop longue | Mesurée par tranche à l'étape 4 ; pistes : transcription temps réel, modèle plus petit. |
