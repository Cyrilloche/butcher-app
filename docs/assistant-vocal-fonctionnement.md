# Assistant vocal — Fonctionnement

> Comment marche l'assistant vocal du spike, de l'appui sur le bouton à la vente enregistrée : le parcours d'une demande, qui décide quoi, ce qui part chez Mistral, et où se trouve chaque pièce. Les décisions et leurs raisons sont dans `docs/cadrage-assistant-vocal.md` ; les mesures dans `docs/spike-assistant-vocal.md`.

| Version | Date | Objet |
|---|---|---|
| 0.1 | 2026-09-23 | État après le premier essai sur téléphone |
| 0.2 | 2026-09-23 | Voix de Mistral (Voxtral TTS) |

---

## 1. En une phrase

On appuie sur « + », puis « Dicter », on parle ; le téléphone envoie l'enregistrement au backend, qui le fait transcrire par Mistral, remplace les noms de clients par des jetons, demande au LLM **ce que veut dire la phrase**, puis calcule **lui-même** la réponse : une phrase de stock, ou un brouillon de vente que l'utilisateur vérifie et enregistre avec le formulaire habituel.

**Principe directeur** : le LLM comprend, le backend décide. Aucun chiffre, aucune unité, aucun client ne vient du LLM.

## 2. Le parcours d'une demande

```
TÉLÉPHONE                           BACKEND                                   MISTRAL (UE)
─────────                           ───────                                   ────────────
« + » → « Dicter »
 │ micro ouvert, jauge de volume
 │ fin de phrase = 2 s de silence
 ▼
[1] audio webm/opus ──────────────▶ POST /api/assistant/voice
                                     │
                                    [2] transcription ──────── audio ─────────▶ Voxtral Mini Transcribe 2
                                     │   « Vends deux saucissons à madame Martin »  ◀── texte
                                     │
                                    [3] lecture en base : clients, catalogue, stock
                                     │
                                    [4] pseudonymisation (local)
                                     │   « Vends deux saucissons à [CLIENT_1] »
                                     │   CLIENT_1 = client n° 42 (reste ici)
                                     │
                                    [5] compréhension ─── texte pseudonymisé ──▶ Ministral 14B
                                     │                    + catalogue + 3 outils
                                     │   draft_sale(client=CLIENT_1, SC × 2)  ◀── appel d'outil
                                     │
                                    [6] exécution par le backend
                                     │   - choix des unités (les plus anciennes…)
                                     │   - CLIENT_1 → client n° 42
                                     │   - phrase à dire
                                     ▼
[7] réponse ◀────────────────────── { kind, speech, heard, stock | draft }
 │ panneau (texte tout de suite)
 │ voix ─────────────────────────▶ POST /api/assistant/speech ── phrase ──────▶ Voxtral TTS (voix « Marie »)
 │      ◀──────────────────────── MP3 (≈ 1 s)  ◀─────────────────────────────
 │ « Ouvrir la vente »
 ▼
[8] « Nouvelle vente » pré-remplie : client, unités, montants calculés ici
 │ l'utilisateur relit, corrige
 ▼
[9] « Enregistrer la vente » ─────▶ POST /api/sales (inchangé)
```

### Étape par étape

| # | Où | Ce qui se passe | Fichier |
|---|---|---|---|
| 1 | Téléphone | « Dicter » ouvre le micro. Le volume est mesuré tous les dixièmes de seconde : la jauge bouge, et 2 s de silence après avoir parlé terminent la demande (30 s au plus ; 7 s sans parole : abandon). Le micro est rendu aussitôt après. | `composables/useAssistant.ts` |
| 2 | Backend → Mistral | L'audio part à Voxtral, qui rend le texte. | `Infrastructure/Mistral/MistralClient.cs` |
| 3 | Backend | Lecture seule en base : clients (nom, prénom), produits actifs, unités `available` / `opened` avec leur poids restant (même calcul que les écrans de stock). | `Application/Assistant/AssistantService.cs` |
| 4 | Backend | Les noms de clients sont retrouvés **à l'oreille** (« les moraux » = Moreau) et remplacés par des jetons. Deux clients trop proches (Martin / Martine) : aucun n'est choisi. Un nom inconnu est retiré. | `CustomerNameMatcher.cs`, `FrenchPhonetic.cs` |
| 5 | Backend → Mistral | Le LLM reçoit la phrase pseudonymisée, le catalogue et trois outils. Il en appelle un (ou deux) : `get_stock`, `draft_sale`, `not_understood`. | `AssistantEngine.cs` |
| 6 | Backend | **Le backend exécute** : `get_stock` → comptes et poids calculés, phrase construite ; `draft_sale` → unités choisies selon les règles du cadrage §6, jeton → client. Rien n'est écrit en base. | `StockSummaryBuilder.cs`, `SaleDraftBuilder.cs` |
| 7 | Téléphone | Le panneau affiche ce qui a été entendu, la phrase, le détail par fournée ou les avertissements d'une vente. La phrase est lue par la voix de Mistral (Voxtral TTS, environ 1 s de plus) ; à défaut, par celle du téléphone. | `components/domain/AssistantPanel.vue`, `composables/useAssistant.ts` |
| 8 | Téléphone | « Ouvrir la vente » passe le brouillon au formulaire, qui calcule les montants **exactement comme pour un choix à la main**. | `composables/useAssistantDraft.ts`, `views/SaleAddView.vue` |
| 9 | Téléphone → Backend | Enregistrement par le bouton habituel : même route, mêmes contrôles, même journal, même auteur. | inchangé |

## 3. Qui décide quoi

| Décision | LLM | Backend | Utilisateur |
|---|---|---|---|
| Question de stock, vente, ou rien | ✅ | | |
| Produit, quantité, poids, prix, tranche, payé | ✅ (extrait de la phrase) | contrôle (code du catalogue) | corrige dans le formulaire |
| Quel client | | ✅ (jeton → client, en local) | choisit si non reconnu |
| Quelles unités (quel sachet, quel jambon) | | ✅ (règles du cadrage §6) | peut retirer / changer |
| Chiffres dits (nombre, poids, dates) | | ✅ (phrase construite par le serveur) | |
| Montant | | | ✅ (calculé par le formulaire, modifiable) |
| Enregistrer | | | ✅ (toujours) |

Pourquoi la phrase dite ne vient plus du LLM : au banc, il a annoncé « 1 jambon entier » pour 2, avec un chiffre qui existait ailleurs dans les données ; le contrôle ne pouvait pas le voir (`spike-assistant-vocal.md` §7, étape 3).

## 4. Ce qui part chez Mistral

| Donnée | Voxtral (transcription) | LLM (compréhension) | Voxtral TTS (voix lue) |
|---|---|---|---|
| La voix de l'utilisateur | ✅ | ❌ | ❌ |
| Les noms de clients | ✅ (dans l'audio) | ❌ (jetons seulement) | ❌ |
| La liste des clients | ❌ | ❌ | ❌ |
| Le catalogue (codes, noms de produits) | ❌ | ✅ | noms cités dans la phrase |
| Le stock, les prix, les ventes | ❌ | ❌ (les chiffres restent au backend) | nombres et poids de la phrase de stock |

La phrase lue ne contient jamais de nom de client : les phrases de stock n'en ont pas, celles d'une vente sont fixes. La route `/api/assistant/speech` accepte en revanche n'importe quel texte d'un compte connecté (500 caractères au plus) : à restreindre avant tout usage réel.

Mistral conserve les entrées 30 jours (détection d'abus) ; pas d'entraînement en paiement à l'usage. Hébergement UE. Détail : `cadrage-assistant-vocal.md` §5 et §9.

## 5. Carte des fichiers

**Backend** (`backend/src/Butcher.Api/`)

| Fichier | Rôle |
|---|---|
| `Controllers/AssistantController.cs` | `POST /api/assistant/voice` (audio), `/text` (texte), `/speech` (voix lue, MP3) ; compte connecté exigé |
| `Application/Assistant/AssistantService.cs` | Charge clients, catalogue et stock ; transcrit ; confie à l'engine |
| `Application/Assistant/AssistantEngine.cs` | La chaîne : pseudonymisation, consignes et outils du LLM, exécution, réponse |
| `Application/Assistant/CustomerNameMatcher.cs`, `FrenchPhonetic.cs` | Retrouver les clients à l'oreille, pseudonymiser |
| `Application/Assistant/SaleDraftBuilder.cs` | Choisir les unités d'une vente dictée |
| `Application/Assistant/StockSummaryBuilder.cs` | Résumé de stock, phrase à dire, contrôle des chiffres |
| `Infrastructure/Mistral/MistralClient.cs` | Appels HTTP à Mistral (sans SDK) : transcription, compréhension, voix lue ; nouvel essai sur limite de débit |

**Frontend** (`frontend/src/`)

| Fichier | Rôle |
|---|---|
| `components/domain/ActionFab.vue` | Le « + » des listes : action de l'écran, ou « Dicter » |
| `composables/useAssistant.ts` | Micro, détection de fin de phrase, envoi, voix lue |
| `components/domain/AssistantPanel.vue` | Carte d'écoute (jauge), panneau de réponse, saisie au clavier |
| `composables/useAssistantDraft.ts` | Passage du brouillon au formulaire, montants calculés comme à la main |
| `views/SaleAddView.vue` | Applique le brouillon, affiche les avertissements |

**Spike** (`spikes/assistant-vocal/`, hors application) : `recorder/` (enregistrement du corpus), `transcription/` (banc Voxtral / Whisper), `names/` (jeu d'évaluation, prototype Python). Les enregistrements vivent dans `development/assistant-corpus/`, hors git.

## 6. Réglages

| Réglage | Où | Défaut |
|---|---|---|
| `MISTRAL_API_KEY` | `development/.env` | — (assistant indisponible sans clé : 503) |
| `Assistant__ChatModel` | idem | `ministral-14b-2512` |
| `Assistant__TranscriptionModel` | idem | `voxtral-mini-2602` |
| `Assistant__LlmSpeech` | idem | `false` (phrase dite par le backend) |
| `Assistant__SpeechModel` | idem | `voxtral-mini-tts-2603` |
| `Assistant__SpeechVoice` | idem | `fr_marie_neutral` (aussi : `fr_marie_happy`, `_curious`, `_excited`, `_sad`, `_angry`) |
| `SALOIR_DEV_HTTPS=1` | au lancement de `npm run dev` | absent : rien ne change. Présent : HTTPS et relais `/api`, pour l'essai sur téléphone |

## 7. Vérifier

```bash
dotnet test backend                                               # tout le backend (267 tests)
ASSISTANT_EVAL=1 dotnet test backend --filter "FullyQualifiedName~AssistantEvaluation" \
  --logger "console;verbosity=detailed"                          # banc LLM : appelle Mistral
```

Frontend : `npm run test:unit`, `npm run type-check`, `npm run lint` (depuis Windows, où `node_modules` est installé).

## 8. Limites connues

- **La transcription reste le maillon faible** : « deux » entendu « de », « terrine » entendu « terrain ». Le formulaire pré-rempli rattrape, pas la réponse orale.
- Un produit mal transcrit peut être remplacé par un produit proche (« corisaux » → saucisson), malgré la consigne.
- Un nom de client inconnu écrit en minuscules par la transcription passerait au LLM ; Voxtral met des majuscules aux noms propres (étape 2).
- La détection de fin de phrase se règle sur le volume : 2 s de silence peuvent couper une personne qui hésite longtemps. Le bouton « J'ai fini » reste là.
- La voix de Mistral ajoute environ 1 s avant d'entendre la réponse (le texte, lui, est immédiat). Sans elle, c'est la voix du téléphone (Android : Paramètres → Accessibilité → Synthèse vocale).
- Spike seulement : pas de journal des demandes vocales, pas de conversation à plusieurs tours, en local uniquement.
