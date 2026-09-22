# Cadrage — Assistant vocal (R&D)

> Document de travail, vivant. Il consigne les décisions de cadrage au fil des discussions, avant l'ADR-012 et la spec `specs/006-assistant-vocal`. Les décisions structurantes redescendront dans `ADR.md` et `PRD.md` ; ce document n'en tient pas lieu.

| Version | Date | Objet |
|---|---|---|
| 0.1 | 2026-09-22 | Premier cadrage : besoin, architecture du POC, protection des données, vente à la voix |
| 0.2 | 2026-09-22 | Réponse à une question de stock, R&D en local, essais de transcription sur le PC |

---

## 1. Le besoin

Un des deux utilisateurs parle beaucoup à son téléphone (« Ok Google, mets des chansons pour enfants »). L'idée est de reproduire ce geste dans Saloir : **parler à l'application plutôt que naviguer dans les écrans**.

Deux intentions pour le POC :

1. **Question sur le stock** — « il me reste combien de saucisson ? »
2. **Vente** — « vends deux saucissons à madame Martin ». Il s'agit bien d'une **vente** (`sale`), pas d'une commande : l'application n'a pas de commandes (RU-06 sans suite).

Attendu pour la suite : si l'outil est adopté, les demandes vont s'élargir. Le choix d'architecture doit le permettre sans réécriture.

## 2. Ce qui est hors d'atteinte

- **« Ok Google » / Gemini** : une PWA ne s'intègre pas à l'assistant du système (il faudrait une application Android native).
- **Écoute en arrière-plan** : une PWA n'écoute pas quand elle n'est pas ouverte.

→ Le POC part d'un **bouton micro** dans l'application : on appuie, on parle. Un mot déclencheur limité à l'application ouverte reste une piste pour plus tard.

## 3. Décisions prises

| # | Décision | Raison |
|---|---|---|
| D-01 | Le POC traite la **vente** et la **question de stock**, rien d'autre. | Périmètre minimal qui valide toute la chaîne, en lecture puis en écriture. |
| D-02 | **L'assistant ne crée rien seul** : il ouvre le formulaire de vente existant, à moitié rempli, et l'utilisateur valide. | Une erreur de transcription ne crée pas de vente fausse ; les garde-fous du formulaire et du serveur s'appliquent tels quels ; `created_by` et le journal restent justes. |
| D-03 | La compréhension passe par un **LLM avec appel d'outils**, pas par des règles. | Formulations libres, et le périmètre va grandir (§1) : un analyseur à règles serait à réécrire à chaque demande. |
| D-04 | **POC entièrement déporté** : transcription et LLM chez un fournisseur externe, hébergé en UE (piste : Mistral). | Valider l'usage vite, sans exploiter de GPU. La transcription locale (§4, option A) reste l'évolution visée si l'outil est adopté. |
| D-05 | **Pseudonymisation maximale** avant tout appel au LLM (§5). | Minimisation : ce qui n'est jamais envoyé ne peut pas fuiter. |
| D-06 | La voix ne passe **pas** par la Web Speech API du navigateur : le téléphone enregistre (`MediaRecorder`) et envoie l'audio au backend. | Navigateur incertain (Chrome probable, pas garanti) ; dans les dérivés de Chromium, l'API est souvent présente mais inopérante. Et l'audio partirait chez Google sans contrat de sous-traitance. |
| D-07 | Tout appel externe passe par le **backend**, jamais directement depuis le frontend. | Clés d'API hors du téléphone, droits relus en base (ADR-011), pseudonymisation centralisée, contrat REST (ADR-003). |
| D-08 | Un nom de client **non reconnu est retiré** du texte ; on garde ce qui a été compris. | Aucun nom ne part en clair vers le LLM, et la phrase reste exploitable : le client se choisit à l'écran (D-02). |
| D-09 | La R&D se fait **entièrement en local**, sans déploiement prévu. Le bouton micro est visible par **tous les comptes**. | Pas d'utilisateur réel exposé : aucun besoin d'activation par compte. La branche `feat/assistant-vocal` n'est pas fusionnée dans `dev` tant que l'assistant n'est pas décidé. |
| D-10 | La transcription locale (option A) se mesure d'abord sur le **PC de développement**, avec la mémoire vidéo **bridée à 2 Go**. | Le serveur de la P600 est aussi le serveur de prod : on n'y fait pas d'essais. |

## 4. Architecture

```
Téléphone (PWA)                Backend (VPS)                          Externe (UE)
───────────────                ─────────────                          ────────────
[micro] MediaRecorder ──audio──▶ /api/assistant
                                  1. transcription ─────────────────▶ STT (Mistral ?)
                                  2. pseudonymisation (local)
                                  3. LLM + outils ──texte pseudonymisé─▶ LLM (Mistral ?)
                                  4. exécution des outils (services existants,
                                     droits du compte connecté)
                                  5. dépseudonymisation
réponse texte + lecture ◀─────── réponse / brouillon de vente
à voix haute (speechSynthesis)
ou formulaire pré-rempli
```

| | A — Transcription chez nous (cible) | B — Tout déporté (POC, D-04) |
|---|---|---|
| Voix → texte | `faster-whisper` sur la Quadro P600 (2 Go : *small* en int8 probable, *medium* incertain) | API du fournisseur |
| Texte → intention | LLM, texte pseudonymisé | LLM, texte pseudonymisé |
| Nom du client | Ne sort jamais | Sort dans l'**audio**, vers la transcription |
| Complexité | GPU, réseau vers la machine, repli si elle est éteinte | Faible |

## 5. Protection des données

**Données en jeu** : la voix de l'utilisateur ; le nom des clients (dans l'audio puis le texte) ; le catalogue et le stock (non personnels).

**Pseudonymisation** (D-05) : après transcription, le backend reconnaît les noms de clients en local, par correspondance approximative avec la table `customer`, et les remplace par un jeton avant d'appeler le LLM.

```
"vends deux saucissons à madame Martin"
  → "vends deux saucissons à [CLIENT_1]"                   envoyé au LLM
  ← create_sale_draft(client=CLIENT_1, produit=SC, qté=2)
  → CLIENT_1 = customer.id 42                              résolu en local
```

- Le LLM **ne reçoit jamais la liste des clients**, seulement des jetons.
- C'est de la **pseudonymisation**, pas de l'anonymisation au sens RGPD : le traitement reste un traitement de données personnelles.
- **Limite du POC (option B)** : la transcription reçoit l'audio, nom compris. La pseudonymisation ne protège que l'étape LLM ; seule l'option A ferme cette porte.
- **Nom non reconnu** : il est **retiré** du texte avant l'envoi, et le reste de la phrase est traité normalement. Le formulaire s'ouvre sans client, à choisir à l'écran (Q-04, D-08).
- La correspondance doit tenir compte de la prononciation (« Martin » / « Martine », « la Josette »).

**Fournisseur** : exiger hébergement UE, contrat de sous-traitance (DPA), conditions de conservation et d'usage pour l'entraînement connues. À vérifier dans la documentation officielle au moment de l'ADR-012.

**Information des clients** : pas d'information individuelle prévue (choix assumé, activité de petite taille). L'équivalent retenu est le choix d'un sous-traitant conforme (UE, DPA).

## 6. La vente à la voix

Règles de choix des unités, appliquées **par le backend** (le LLM transmet l'intention, il ne choisit pas les unités) :

| Cas | Règle |
|---|---|
| Rien de précisé (« deux saucissons ») | Les unités **les plus anciennes** d'abord. |
| Poids précisé (« un saucisson d'environ 300 g ») | L'unité dont le poids est **le plus proche**. |
| Prix précisé (« un saucisson à 8 € ») | L'unité dont le montant théorique (poids × prix de fournée) est **le plus proche**. |
| Jambon à la tranche (« 200 g de jambon ») | Le **jambon entamé le plus ancien** ; le poids demandé devient le poids vendu. |
| Information manquante ou ambiguë | **Formulaire à moitié rempli** : ce qui est compris est pré-rempli, le reste est à compléter à l'écran. Pas de question vocale en retour pour le POC. |
| Paiement non dit | **Non payé** par défaut. |

Le montant pré-rempli suit le calcul habituel ; l'utilisateur garde la main dessus (`CLAUDE.md` §8, règle 7 : le montant saisi est conservé, jamais recalculé).

Point d'appui existant : `SaleAddView` accepte déjà un client pré-rempli (`/sales/add?client=12`, RU-04). Il reste à pré-remplir le panier et le paiement.

## 7. Réponse à une question de stock (Q-06)

Une réponse détaillée est l'intérêt même de l'assistant, mais **à l'oral, une réponse longue se perd** : au-delà de deux ou trois chiffres, on ne retient plus rien. Proposition : **deux niveaux**.

| Niveau | Contenu | Exemple pour « il me reste combien de saucissons ? » |
|---|---|---|
| **Dit à voix haute** | L'essentiel, en une ou deux phrases : nombre d'unités, poids total, et un seul fait notable s'il y en a un | « Il te reste 12 saucissons, environ 3,4 kilos. Les plus anciens datent du 2 septembre. » |
| **Affiché à l'écran** | Le détail : fournée par fournée (date, nombre, poids, prix), unité entamée et son restant, avec un lien vers Détail Stock | Tableau par fournée |

Règles :
- **Les chiffres viennent du serveur**, jamais d'un calcul du LLM (même principe que `remaining_weight`, `CLAUDE.md` §9). Le LLM met en phrase les valeurs que l'outil lui renvoie.
- **Les chiffres dits sont vérifiés** pendant le spike : chaque nombre prononcé doit figurer dans la réponse de l'outil. Un LLM qui arrondit ou invente un chiffre est éliminatoire.
- **Poids arrondis à l'oral** (« environ 3,4 kilos »), exacts à l'écran.
- **Jambon** : dire les entiers et les entamés séparément (« 2 jambons entiers, et un entamé dont il reste environ 4 kilos »).
- Les questions de suivi (« et la plus vieille fournée ? ») supposent une conversation à plusieurs tours : hors POC, à garder en tête dans l'architecture.

## 8. Questions ouvertes

| Réf. | Question | Statut |
|---|---|---|
| Q-01 | Choix du fournisseur — meilleur compromis qualité / prix, hébergement UE, DPA, conservation | En cours (§9) |
| Q-02 | Le formulaire manuel coche « payé » par défaut, la voix « non payé » : l’écart est-il voulu ? | ✅ Non : le formulaire passe à « À payer » par défaut (branche `fix/vente-a-payer-par-defaut`) |
| Q-03 | Phrases réelles de l'utilisateur, pour constituer le jeu de test | ✅ Jeu de phrases type proposé (`docs/assistant-vocal-phrases-test.md`), testé par plusieurs voix |
| Q-04 | Nom de client non reconnu : bloquer l'envoi, retirer le nom, ou envoyer en clair ? | ✅ Retiré, le reste est traité (D-08) |
| Q-05 | Machine de la P600 (option A) : VPS ou machine à domicile ? Conditionne le réseau et le repli | ✅ Serveur à domicile, qui est aussi le serveur de prod ; essais sur le PC bridé à 2 Go (D-10). Charge sur la prod à évaluer avant toute mise en service |
| Q-06 | Réponse vocale à la question de stock : quel niveau de détail (total, par fournée, poids) ? | Proposé : deux niveaux, l'essentiel à voix haute et le détail à l'écran (§7) |
| Q-07 | Qui voit le bouton micro : tous les comptes, ou activé par compte pendant la R&D ? | ✅ Tous les comptes, R&D en local uniquement (D-09) |

## 9. Choix du fournisseur (Q-01) — premiers éléments

Relevé du 2026-09-22, à reconfirmer sur la documentation officielle au moment de l'ADR-012.

**Mistral (La Plateforme)** — offre retenue comme piste :

| Brique | Modèle | Prix public |
|---|---|---|
| Transcription | Voxtral Mini Transcribe 2 (`voxtral-mini-transcribe-2`, v26.02) | 0,003 $ / minute |
| LLM | Mistral Small 4 (`mistral-small-2603`), appel d'outils pris en charge | 0,15 $ / M jetons en entrée, 0,60 $ / M en sortie |
| LLM de repli | Mistral Medium 3.5 (`mistral-medium-2604`) | Plus cher, si Small se trompe trop sur les outils |

**Ordre de grandeur du coût** : une demande, c'est environ 10 s d'audio (0,0005 $) et environ 2 000 jetons en entrée et 200 en sortie (environ 0,0004 $), soit **environ 0,001 $ par demande**. À 50 demandes par jour, cela fait environ 1,50 $ par mois. **Le prix n'est pas un critère discriminant ; la qualité l'est.** Le compromis se joue donc sur la justesse de la transcription (noms, nombres) et de l'appel d'outils, à mesurer avec le jeu de phrases.

**Protection des données** :
- Hébergement en UE par défaut ; DPA disponible, avec les clauses contractuelles types.
- Pas d'entraînement sur les données en paiement à l'usage (désactivé par défaut) ; à vérifier sur le compte.
- **Conservation de 30 jours** des entrées et sorties, pour la détection d'abus. La conservation zéro n'existe que sur l'offre Scale. Conséquence pour le POC : l'audio, noms compris, reste 30 jours chez Mistral. À accepter explicitement dans l'ADR-012.

**Vocabulaire guidé de Voxtral** : la transcription accepte une liste de 100 mots au plus pour guider l'orthographe (optimisée pour l'anglais, expérimentale en français).
- Utile pour les **produits** (« saucisson », codes).
- **Pas pour les clients** : envoyer la liste des clients à chaque demande contredirait D-05. La reconnaissance des noms reste locale, après transcription.

**Option A (P600)** : le modèle de transcription en poids ouverts de Mistral (Voxtral Realtime, 4 milliards de paramètres) ne tiendra pas dans 2 Go. Pour la transcription locale, la piste reste `faster-whisper`.

Sources : [mistral.ai — Voxtral Transcribe 2](https://mistral.ai/news/voxtral-transcribe-2/), [docs.mistral.ai — modèles](https://docs.mistral.ai/getting-started/models/models_overview/), [DPA Mistral](https://legal.mistral.ai/terms/data-processing-addendum/), [Aide Mistral — entraînement](https://help.mistral.ai/en/articles/455207-can-i-opt-out-of-my-input-or-output-data-being-used-for-training), [prix Mistral Small 4](https://openrouter.ai/mistralai/mistral-small-2603).
