---
description: "Task list for the voice assistant (spec 006)"
---

# Tasks: Assistant vocal

**Input**: Design documents from `specs/006-assistant-vocal/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: inclus. La constitution (principe II) exige un test backend pour toute règle nouvelle ou
modifiée ; les tests sont posés dans la tâche qui introduit la règle.

**Point de départ**: le code du spike, sur la branche `feat/assistant-vocal` (research R-01). Une
tâche « reprend » ou « vérifie » ce qui existe ; elle ne le réécrit pas.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1 à US5, spec.md)
- Chemins : `backend/src/Butcher.Api/`, `backend/tests/Butcher.Api.Tests/`, `frontend/src/`

---

## Phase 1: Setup — documents de référence

**Purpose**: les documents de `docs/` font foi et se mettent à jour dans le même lot (principe IV).

- [X] T001 Ajouter à `docs/PRD.md` **RF-34** (dicter une question de stock, réponse dite et affichée, chiffres du serveur), **RF-35** (dicter une vente : brouillon ouvert pré-rempli dans « Nouvelle vente », jamais enregistré par l'assistant, jamais un mauvais client), **RF-36** (activation compte par compte par l'administrateur, journal des demandes sans audio, limite par compte, suivi de l'usage) ; inscrire l'assistant vocal au périmètre (§4) avec renvoi à ADR-012 et `specs/006-assistant-vocal`
- [X] T002 [P] Mettre à jour `docs/data-model.md` : `app_user.assistant_enabled` (§3.1), nouvelle section `voice_request` (colonnes, index, immuabilité, données transmises à l'extérieur) d'après `specs/006-assistant-vocal/data-model.md`, libellés des enums `voice_request_outcome` et `voice_input_mode` à la correspondance (§4.2), ligne d'historique de version

---

## Phase 2: Foundational — activation, journal, contrat

**Purpose**: ce dont tous les récits dépendent : l'activation par compte (toutes les routes de
l'assistant l'exigent), le journal (identifiant de demande, issue) et le contrat de réponse.

**⚠️ CRITICAL**: aucun récit ne commence avant la fin de cette phase.

- [X] T003 Modèle et migration : propriété `AssistantEnabled` (défaut `false`) dans `backend/src/Butcher.Api/Domain/Entities/AppUser.cs` et sa configuration ; entité `backend/src/Butcher.Api/Domain/Entities/VoiceRequest.cs` (`Id` long, `AccountId`, `OccurredAt`, `InputMode`, `HeardText?`, `Outcome`, `ReplySpeech?`, `DurationMs`) ; enums `VoiceRequestOutcome` (`StockAnswer`, `SaleDraft`, `NotUnderstood`, `Error`, `RateLimited`) et `VoiceInputMode` (`Voice`, `Text`) dans `backend/src/Butcher.Api/Domain/Enums/`, stockés en texte `snake_case` comme les autres enums ; `DbSet<VoiceRequest>` et configuration (FK vers `app_user`, index `(account_id, occurred_at)`) dans `backend/src/Butcher.Api/Infrastructure/Data/` ; migration `AddVoiceRequests` (data-model §1–§2) appliquée sur la base de dev
- [X] T004 Politique `AssistantEnabled` (research R-03) : `AccountRequirement` gagne une exigence « assistant activé » dans `backend/src/Butcher.Api/Common/Authorization/AccountRequirement.cs` ; `AccountAuthorizationHandler.cs` refuse un compte sans assistant avec une raison dédiée ; `AccountAuthorizationResultHandler.cs` la traduit en `403` « L'assistant vocal n'est pas activé pour ton compte. » ; déclarer la politique dans `AuthorizationPolicies.cs` et `backend/src/Butcher.Api/Program.cs` ; tests dans `backend/tests/Butcher.Api.Tests/Common/Authorization/AccountAuthorizationHandlerTests.cs` : assistant activé accepté, non activé refusé, compte désactivé refusé même avec l'assistant, relu en base sans nouveau jeton
- [X] T005 Poser `[Authorize(Policy = AuthorizationPolicies.AssistantEnabled)]` sur `backend/src/Butcher.Api/Controllers/AssistantController.cs` ; vérifier que `backend/tests/Butcher.Api.Tests/Controllers/ReservedActionsTests.cs` passe toujours et y ajouter l'assertion que toutes les actions de l'assistant portent cette politique (FR-022)
- [X] T006 Exposer et modifier l'activation (contracts/api.md, « Comptes et session ») : `AssistantEnabled` dans `AccountDto`, `MeDto` et `UpdateAccountRequest` (facultatif : absent, inchangé) dans `backend/src/Butcher.Api/Application/Dtos/` ; prise en compte dans `AccountService.UpdateAsync` et les projections de `AuthService` ; tests dans `backend/tests/Butcher.Api.Tests/Application/Services/AccountServiceTests.cs` : activation, désactivation, valeur absente conservée, nouveau compte sans assistant (FR-026)
- [X] T007 Journal des demandes (research R-02) dans `backend/src/Butcher.Api/Application/Assistant/AssistantService.cs` : pour chaque demande (dictée ou écrite), mesurer la durée de traitement et écrire une `VoiceRequest` (compte courant via `ICurrentAccount`, phrase entendue, issue, phrase de réponse), y compris en erreur (issue `error`, puis l'exception repart) ; jamais l'audio ; `AssistantReply` gagne `RequestId` ; `AssistantReplyKind.Answer` devient `StockAnswer` dans `AssistantEngine.cs`
- [X] T008 Test d'intégration sur PostgreSQL (research R-09) dans `backend/tests/Butcher.Api.Tests/Application/Assistant/AssistantServiceTests.cs`, avec un faux `IMistralClient` qui rend une transcription et un appel d'outil fixés : le stock lu (unités intactes, entamées, poids restant après une vente en tranche) est celui de `StockUnitService.GetAllAsync` ; une demande écrit une ligne `voice_request` avec la bonne issue et la phrase entendue ; une erreur du service extérieur écrit l'issue `error` et rend `503` ; aucun champ ne contient l'audio
- [X] T009 [P] Contrat côté frontend : `requestId`, `kind` `stock_answer`, `assistantEnabled` (compte et `MeDto`) dans `frontend/src/api/types.ts` ; `frontend/src/stores/auth.ts` expose `assistantEnabled` du compte connecté ; adapter `frontend/src/components/domain/AssistantPanel.vue` au nouveau `kind`

**Checkpoint**: un compte avec l'assistant l'utilise comme au spike, et chaque demande est
journalisée ; un compte sans assistant reçoit `403`.

---

## Phase 3: User Story 1 — Demander ce qu'il reste en stock (Priority: P1) 🎯 MVP

**Goal**: une question de stock reçoit une phrase courte, dite et affichée, avec des chiffres calculés
par le serveur, et le détail par fournée.

**Independent Test**: dicter « il me reste combien de *\<produit\>* ? » ; les nombres dits et affichés
sont ceux de l'écran Stock (quickstart, scénario 3).

- [X] T010 [US1] Retirer la mise en phrase par le LLM (research R-06) : paramètre `llmSpeech`, second appel, consigne de mise en phrase et sérialisation du résultat d'outil dans `backend/src/Butcher.Api/Application/Assistant/AssistantEngine.cs` ; réglage `Assistant:LlmSpeech` dans `AssistantService.cs` ; `InventedNumbers` et ses dépendances dans `StockSummaryBuilder.cs`, et leurs tests dans `backend/tests/Butcher.Api.Tests/Application/Assistant/StockSummaryBuilderTests.cs` ; mesure de la phrase du LLM dans `AssistantEvaluation.cs`
- [X] T011 [US1] Tests du moteur avec un faux `IMistralClient` dans `backend/tests/Butcher.Api.Tests/Application/Assistant/AssistantEngineTests.cs` : `get_stock` d'un produit rend l'issue `stock_answer`, la phrase de `StockSummaryBuilder.Speech` et le détail ; `get_stock` sans produit couvre tout le stock ; un code hors catalogue n'en donne jamais un autre (FR-012) ; `not_understood` rend la phrase de rappel ; le LLM n'est appelé qu'une fois (FR-011)
- [X] T012 [P] [US1] Vérifier que `backend/tests/Butcher.Api.Tests/Application/Assistant/StockSummaryBuilderTests.cs` couvre les scénarios d'acceptation 1 à 3 de US1 (fournée la plus ancienne, entier et entamé séparés, tout le stock sans accorder le nom du produit) et compléter s'il en manque un

**Checkpoint**: US1 livrable seule sur un compte activé.

---

## Phase 4: User Story 2 — Préparer une vente à la voix (Priority: P1)

**Goal**: une vente dictée ouvre « Nouvelle vente » pré-rempli selon les règles de choix des unités,
sans montant venu du serveur, enregistrée seulement par l'utilisateur.

**Independent Test**: dicter une vente pour un client connu ; le formulaire s'ouvre pré-rempli ; rien
n'existe tant que « Enregistrer » n'est pas touché (quickstart, scénario 4).

- [ ] T013 [US2] Compléter `backend/tests/Butcher.Api.Tests/Application/Assistant/SaleDraftBuilderTests.cs` pour FR-014 : tranche sur l'unité intacte la plus ancienne quand aucun entamé n'existe ; entamé jamais proposé en vente entière ; même unité jamais deux fois sur plusieurs lignes du même produit
- [ ] T014 [US2] Dans `AssistantEngineTests.cs` : `draft_sale` rend l'issue `sale_draft`, « À payer » sans mention du paiement et « Payée » quand l'outil le dit (FR-015) ; deux appels `draft_sale` ne préparent que le premier, avec l'avertissement ; une question de stock et une vente dans la même phrase rendent les deux (FR-006) ; aucune ligne ne porte de montant (FR-016)
- [ ] T015 [P] [US2] Vérifier que `frontend/src/composables/__tests__/useAssistantDraft.spec.ts` et `frontend/src/views/__tests__/SaleAddView.spec.ts` couvrent les scénarios 1, 4, 5 et 6 de US2 et l'unité disparue (FR-017) ; compléter s'il en manque un

**Checkpoint**: US1 et US2 livrables ensemble.

---

## Phase 5: User Story 3 — Ne jamais se tromper de client (Priority: P1)

**Goal**: aucun brouillon ne propose un autre client que celui cité ; un nom incertain ou inconnu
laisse le client à choisir, avec le nom entendu affiché.

**Independent Test**: dicter des ventes pour des clients aux noms proches ou absents ; aucun n'est
pré-rempli à tort (quickstart, scénario 5).

- [ ] T016 [US3] Dans `AssistantEngineTests.cs` : le texte transmis au faux LLM ne contient ni nom de client ni liste de clients (FR-019) ; un jeton rendu par le LLM est résolu en client ; un jeton inventé par le LLM (`[CLIENT_9]`) ne donne aucun client ; un nom inconnu ou ambigu produit l'avertissement « Client à choisir : « … » n'a pas été reconnu » (FR-017, FR-018)
- [ ] T017 [P] [US3] Relancer `CustomerNameMatcherTests` sur le jeu d'évaluation complet (`backend/tests/Butcher.Api.Tests/Application/Assistant/CorpusTranscripts.cs`) et vérifier zéro mauvais client (SC-001) ; aucun changement de code attendu

**Checkpoint**: les trois récits P1 sont livrables.

---

## Phase 6: User Story 4 — Parler naturellement, sans manipuler l'écran (Priority: P2)

**Goal**: l'écoute finit seule au silence, la réponse est lue avec la voix de Mistral, et cette voix ne
lit que des phrases de l'assistant.

**Independent Test**: dicter sans toucher l'écran après « Dicter » ; la réponse part après le silence
et elle est lue ; aucun texte libre ne peut être lu (quickstart, scénarios 3 et 6).

- [ ] T018 [US4] Voix d'une demande (research R-05) : `GET /api/assistant/requests/{id}/speech` dans `backend/src/Butcher.Api/Controllers/AssistantController.cs` ; dans `AssistantService.cs`, relire la `VoiceRequest` (même compte, moins de 10 minutes, phrase de réponse présente, sinon `NotFoundException`) puis `IMistralClient.SpeakAsync` ; supprimer `POST /api/assistant/speech` et la méthode de service qui acceptait un texte ; tests dans `AssistantServiceTests.cs` : demande d'un autre compte → 404, demande trop ancienne → 404, demande sans réponse → 404, demande valide → le faux client reçoit exactement la phrase journalisée (FR-020, FR-021)
- [ ] T019 [US4] Côté frontend : `speakWithAssistantVoice(requestId)` dans `frontend/src/api/assistant.ts` ; `speak` de `frontend/src/composables/useAssistant.ts` demande la voix par `requestId` et garde le repli sur la voix du téléphone avec le texte ; adapter `frontend/src/api/__tests__/http.spec.ts` si besoin et tester dans `frontend/src/composables/__tests__/useAssistant.spec.ts` que l'échec de la voix Mistral déclenche la voix du téléphone (FR-008)
- [ ] T020 [P] [US4] Vérifier que `useAssistant.spec.ts` couvre FR-002 à FR-004 (fin au silence, hésitation tolérée, abandon à 7 s, 30 s au plus) et que `AssistantPanel.vue` explique un micro refusé et propose d'écrire (scénario 5 de US4)

**Checkpoint**: expérience vocale complète pour un compte activé.

---

## Phase 7: User Story 5 — Suivre l'usage et garder la maîtrise (Priority: P2)

**Goal**: l'administrateur active l'assistant compte par compte, voit l'usage, le délai et les ratés ;
un compte ne peut pas dépasser la limite ; un compte sans assistant garde son « + » à un appui.

**Independent Test**: activer l'assistant pour un seul compte ; l'autre compte n'a jamais « Dicter » ;
la limite refuse la demande de trop ; l'écran Rapports montre les demandes (quickstart, scénarios 1,
2, 7, 8, 10).

- [ ] T021 [US5] Limite par compte (research R-04) dans `AssistantService.cs` : avant tout appel extérieur, compter les `VoiceRequest` du compte sur l'heure glissante ; au-delà de `Assistant:MaxRequestsPerHour` (30 par défaut), journaliser l'issue `rate_limited` et lever `TooManyRequestsException` avec « Tu as fait beaucoup de demandes : réessaie dans quelques minutes. » ; tests dans `AssistantServiceTests.cs` : limite atteinte → 429, journalisée, faux client jamais appelé ; demandes d'un autre compte non comptées (FR-023)
- [ ] T022 [US5] Rapports d'usage (research R-08) : `GetAssistantUsageAsync(from, to)` (par compte et par semaine de Paris via `BusinessTime` : nombre, répartition des issues, durée médiane) et `GetAssistantRequestsAsync(from, to, accountId, limit)` (plus récentes d'abord, phrase entendue et réponse) dans `backend/src/Butcher.Api/Application/Services/ReportService.cs` et `IReportService.cs` ; DTO dans `backend/src/Butcher.Api/Application/Dtos/` ; routes `GET /api/reports/assistant` et `/api/reports/assistant/requests` (administrateur) dans `backend/src/Butcher.Api/Controllers/ReportsController.cs` ; tests dans `backend/tests/Butcher.Api.Tests/Application/Services/ReportServiceTests.cs` : semaine d'une demande de dimanche 23 h 30 à Paris, médiane, filtre par compte (FR-025)
- [ ] T023 [P] [US5] « + » conditionnel dans `frontend/src/components/domain/ActionFab.vue` : sans `assistantEnabled`, l'action de l'écran se déclenche en un appui, comme `AppFab` ; avec, les deux choix ; tests dans `frontend/src/components/__tests__/ActionFab.spec.ts` (FR-001, scénarios 2 et 3 de US5)
- [ ] T024 [P] [US5] Case « Assistant vocal » dans `frontend/src/components/domain/AccountEditDialog.vue`, envoyée par `frontend/src/api/accounts.ts` ; indication de l'état dans la liste de `frontend/src/views/AccountsView.vue` (FR-026)
- [ ] T025 [US5] Section « Assistant vocal » dans `frontend/src/views/ReportsView.vue`, sur la période de l'écran : tableau par compte et par semaine (demandes, issues, durée médiane), puis les dernières demandes (date, compte, dictée ou écrite, phrase entendue, issue, réponse) ; fonctions d'appel dans `frontend/src/api/reports.ts` ; libellés français des issues et des modes (data-model §2) ; test des libellés et du formatage dans un composable pur si la mise en forme en demande un (FR-025)
- [ ] T026 [US5] Messages de refus côté frontend dans `frontend/src/composables/useAssistant.ts` : `403` (assistant désactivé en cours de session) et `429` (limite) affichent le message du serveur ; test dans `useAssistant.spec.ts` si la logique le permet, sinon vérification au quickstart (scénarios 7 et 8)

**Checkpoint**: tous les récits livrés.

---

## Phase 8: Polish & production

- [ ] T027 [P] `Caddyfile` (research R-07) : `Permissions-Policy` passe de `microphone=()` à `microphone=(self)` ; la CSP ajoute `media-src 'self' blob:` ; commentaire rappelant que ce fichier se copie à la main sur le VPS (`CLAUDE.md` §9)
- [ ] T028 [P] `docker-compose.prod.yml` : variables `MISTRAL_API_KEY`, `Assistant__ChatModel`, `Assistant__SpeechVoice`, `Assistant__MaxRequestsPerHour` passées au backend (valeurs par défaut du code si absentes) ; documenter dans `.env.example` et `development/.env.example`
- [ ] T029 [P] Remplacer les commentaires « spike R&D » du code de l'assistant par les références de la spec (`FR-xx`) et de l'ADR-012 : `AssistantEngine.cs`, `AssistantService.cs`, `AssistantController.cs`, `MistralClient.cs`, `ActionFab.vue`, `AssistantPanel.vue`, `useAssistant.ts`, `useAssistantDraft.ts`, `SaleAddView.vue`, `AppLayout.vue`, `vite.config.ts`
- [ ] T030 [P] Mettre à jour `docs/assistant-vocal-fonctionnement.md` (activation par compte, journal, limite, voix par demande, route `/speech` retirée) et `CLAUDE.md` (état d'avancement, ligne du tableau, pièges : jamais de texte libre vers le service de voix, jamais de nom de client vers le LLM, `Caddyfile` à copier pour le micro, `voice_request` écrite par `AssistantService` seul)
- [ ] T031 Vérifications complètes : `dotnet test backend` ; `npm run test:unit`, `npm run type-check`, `npm run lint`, build du frontend ; banc `ASSISTANT_EVAL=1` une fois (SC-004, SC-001)
- [ ] T032 Recette locale sur téléphone selon `specs/006-assistant-vocal/quickstart.md` §2, scénarios 1 à 10 ; chronométrer 10 demandes (SC-003) et noter médiane et maximum dans `specs/006-assistant-vocal/quickstart.md` ou le bilan du spike
- [ ] T033 Mise en production par le porteur de projet, selon `quickstart.md` §3 : `.env` du VPS, `Caddyfile` copié et Caddy redémarré, releases backend et frontend, activation pour le seul compte visé ; puis suivi de `quickstart.md` §4 sur 4 semaines (SC-006, SC-007)

---

## Dependencies & Execution Order

- **Phase 1** (T001, T002) : indépendante, peut se faire en parallèle de tout.
- **Phase 2** bloque tous les récits : T003 → T004 → T005 ; T006 après T003 ; T007 après T003 ;
  T008 après T007 ; T009 après T006 et T007 (contrat).
- **US1, US2, US3** (P1) : après la phase 2, indépendants entre eux ; ils partagent
  `AssistantEngineTests.cs`, donc T011, T014, T016 se font l'un après l'autre.
- **US4** : T018 dépend de T007 (identifiant de demande, phrase journalisée) ; T019 après T018.
- **US5** : T021 après T007 ; T022 après T003 ; T023 et T024 après T009 ; T025 après T022.
- **Phase 8** : T027 à T030 à tout moment ; T031 après tous les récits ; T032 après T031 ;
  T033 après T032.

## Parallel Opportunities

- Phase 1 : T001 et T002.
- Phase 2 : T009 (frontend) pendant T008 (test d'intégration).
- Après la phase 2 : US1 (T010), US2 (T013, T015), US3 (T017) et US5 frontend (T023, T024) touchent
  des fichiers différents.
- Phase 8 : T027, T028, T029, T030.

Exemple, juste après la phase 2 :

```text
T010 [US1] AssistantEngine.cs — retirer la mise en phrase par le LLM
T013 [US2] SaleDraftBuilderTests.cs — compléter FR-014
T023 [US5] ActionFab.vue — « + » conditionnel
T027       Caddyfile — micro et audio
```

## Implementation Strategy

**MVP** : phases 1 et 2, puis US1. Un compte activé demande son stock, la demande est journalisée, les
autres comptes ne voient rien. C'est déjà l'usage le plus sûr et le test d'adoption le plus simple.

**Incréments** :
1. + US2 et US3 : la vente dictée, avec ses garde-fous. Les trois récits P1 forment la version
   utile.
2. + US4 : la voix limitée aux phrases de l'assistant, prérequis de la mise en production.
3. + US5 : limite, rapports, activation depuis l'écran Comptes.
4. Phase 8 : production (`Caddyfile` en tête), recette chronométrée, puis suivi sur 4 semaines.

**Avant la mise en production**, sont indispensables : T004 et T005 (activation), T018 (voix limitée),
T021 (limite) et T027 (`Caddyfile`).
