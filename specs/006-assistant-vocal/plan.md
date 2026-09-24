# Implementation Plan: Assistant vocal

**Branch**: `feat/assistant-vocal` | **Date**: 2026-09-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/006-assistant-vocal/spec.md`

## Summary

L'utilisateur qui parle à son téléphone dicte une question de stock ou une vente depuis le « + » des
listes ; l'application répond à voix haute ou ouvre le formulaire « Nouvelle vente » pré-rempli. Le
spike a construit et éprouvé l'essentiel de la chaîne (ADR-012). Ce plan **part de ce code** et le
complète pour un usage réel (research R-01) :

- **Gardé tel quel** : écoute avec fin au silence, reconnaissance des clients à l'oreille,
  choix des unités, résumé de stock, pré-remplissage du formulaire, client Mistral, saisie au
  clavier.
- **Repris** : la voix n'est plus lue que pour une phrase de l'assistant, relue en base (R-05) ; la
  mise en phrase par le LLM disparaît (R-06) ; le « + » n'offre « Dicter » qu'aux comptes qui ont
  l'assistant.
- **Nouveau** :
  - **activation compte par compte**, relue en base à chaque requête (R-03) ;
  - **journal des demandes** `voice_request`, avec la phrase entendue et jamais l'audio (R-02) ;
  - **limite par compte**, comptée dans ce journal (R-04) ;
  - **rapport d'usage** sur l'écran Rapports (R-08) ;
  - **test d'intégration** de la lecture du stock (R-09) ;
  - **production** : clé Mistral dans le déploiement, et un `Caddyfile` qui cesse d'interdire le micro
    et la lecture audio (R-07).

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript 5 / Vue 3 (frontend)

**Primary Dependencies**: ASP.NET Core Web API, EF Core + Npgsql, ASP.NET Core Identity (allégé) ;
Vue 3, Vuetify 4, Pinia, Phosphor. Service extérieur : Mistral La Plateforme (Voxtral Mini
Transcribe 2, Ministral 14B, Voxtral TTS), appelé en HTTP par le backend seul, sans SDK (ADR-012).
Navigateur : `MediaRecorder`, Web Audio (mesure du volume), `speechSynthesis` en repli. **Aucune
nouvelle dépendance** NuGet ou npm.

**Storage**: PostgreSQL. Une migration : `app_user.assistant_enabled` et la table `voice_request`.
Détail : [data-model.md](./data-model.md).

**Testing**: xUnit, dont PostgreSQL réel via Testcontainers pour `AssistantService` (lecture du
stock, journal, limite, activation), avec un faux `IMistralClient`. Tests purs existants conservés
(reconnaissance des clients sur 256 phrases, choix des unités, résumé de stock, client Mistral).
Banc LLM à part, déclenché par `ASSISTANT_EVAL=1`, jamais en CI. Vitest côté frontend (détection du
silence, choix de la voix, « + » à deux choix, pré-remplissage) ; tests lancés hors du `node_modules`
Windows comme pour la 005. Recette : [quickstart.md](./quickstart.md).

**Target Platform**: PWA sur Android / Chrome en priorité, PC avec micro ; même origine que l'API
derrière Caddy (ADR-010). HTTPS obligatoire pour le micro.

**Project Type**: application web, deux applications séparées par un contrat REST (ADR-003)

**Performance Goals**: réponse affichée en moins de 4 s après la fin de la parole pour la moitié des
demandes, moins de 8 s pour 95 % (SC-003). Mesuré au spike : ≈ 0,5 s de transcription, ≈ 0,8 s de
compréhension, ≈ 1,2 s de plus pour la voix.

**Constraints**:
- jamais un mauvais client (SC-001) ;
- chiffres du serveur seulement (FR-011) ;
- aucun nom de client vers le service de compréhension (FR-019) ;
- lecture seule (FR-009) ;
- aucune régression du « + » pour un compte sans assistant (FR-001) ;
- coût mensuel sous 5 € (SC-007).

**Scale/Scope**: trois comptes, un utilisateur visé. Backend :
- 1 migration ;
- 2 routes nouvelles (voix d'une demande, rapports d'usage), 1 route retirée, 3 DTO enrichis ;
- 1 politique d'autorisation.

Frontend : 1 section de rapport, 1 case dans l'édition d'un compte, « + » conditionnel.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Simplicité (utilisateurs non techniques)** — ✅ Le besoin vient du porteur de projet pour un
  exploitant qui parle à son téléphone ; il entre au PRD comme RF-34 à RF-36 (voir « Documents à
  mettre à jour »). L'assistant n'est proposé qu'au compte qui le veut : les autres gardent le « + »
  à un appui (Q2, Q3). Tous les libellés sont en français, issues comprises (table de correspondance,
  data-model §2). L'assistant ne remplace aucun parcours : il pré-remplit le formulaire existant.
- **II. Backend garant des règles métier** — ✅ Le serveur choisit les unités, résout le client,
  calcule les chiffres, écrit la phrase dite, applique l'activation et la limite ; le LLM ne rend
  qu'une intention et des champs. Aucune `RG-xx` ne change : l'enregistrement passe par
  `POST /api/sales`, inchangé (montant saisi, client obligatoire). Chaque règle nouvelle (choix des
  unités, jamais un mauvais client, activation, limite, voix limitée aux phrases de l'assistant)
  arrive avec ses tests backend.
- **III. Frontière contractuelle** — ✅ Tout passe par l'API REST ([contracts/api.md](./contracts/api.md)).
  Changements additifs (`assistantEnabled`, `requestId`, routes nouvelles). Le renommage `answer` →
  `stock_answer` et le retrait de `POST /api/assistant/speech` ne touchent que des routes du spike,
  jamais publiées : pas de rupture de contrat publié.
- **IV. Traçabilité** — ✅ ADR-012 accepté ; exigences citées (`FR-xx` de la spec, RF-34 à RF-36 au
  PRD). Aucune décision structurante nouvelle hors ADR-012 : la table `voice_request` et l'activation
  par compte en découlent. Documents mis à jour dans le même lot (voir plus bas).
- **V. Vagues et spikes** — ✅ Le risque technique a été levé par le spike avant la construction
  (principe V appliqué à la lettre). L'assistant est une **fonctionnalité hors Vague 1**, décidée par
  le porteur de projet et activée compte par compte. Il n'anticipe aucune fonctionnalité V2
  (ni mot déclencheur, ni conversation, ni écriture). Risque résiduel assumé : l'adoption, non
  éprouvée par l'utilisateur visé, suivie après livraison (SC-006).

**Re-check après la phase 1** : ✅ inchangé. La conception n'ajoute ni dépendance ni écart ; le
seul point d'exploitation nouveau (le `Caddyfile`) est traité dans le quickstart (R-07).

## Project Structure

### Documentation (this feature)

```text
specs/006-assistant-vocal/
├── spec.md
├── plan.md              # ce fichier
├── research.md          # R-01 à R-10
├── data-model.md        # app_user.assistant_enabled, voice_request
├── quickstart.md        # recette locale, mise en production, suivi
├── contracts/api.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

Légende : **G** gardé du spike, **R** repris, **N** nouveau, **M** fichier existant modifié.

```text
backend/src/Butcher.Api/
├── Application/Assistant/
│   ├── AssistantEngine.cs          R  sans mise en phrase par le LLM (R-06) ; issue stock_answer
│   ├── AssistantService.cs         R  journal, limite, voix d'une demande (R-02, R-04, R-05)
│   ├── CustomerNameMatcher.cs      G
│   ├── FrenchPhonetic.cs           G
│   ├── SaleDraftBuilder.cs         G
│   ├── StockSummaryBuilder.cs      R  sans InventedNumbers (R-06)
│   ├── SellableUnit.cs             G
│   └── IMistralClient.cs           G
├── Application/Services/ReportService.cs      M  usage de l'assistant (R-08)
├── Application/Services/AccountService.cs     M  assistantEnabled
├── Application/Dtos/                          M  AccountDto, UpdateAccountRequest ; N rapports d'usage
├── Common/Authorization/                      M  politique AssistantEnabled (R-03)
├── Controllers/AssistantController.cs         R  route de la voix par demande ; retrait de /speech
├── Controllers/ReportsController.cs           M  /assistant, /assistant/requests
├── Domain/Entities/VoiceRequest.cs            N
├── Domain/Entities/AppUser.cs                 M  AssistantEnabled
├── Domain/Enums/VoiceRequestOutcome.cs, VoiceInputMode.cs   N
├── Infrastructure/Data/                       M  configuration, migration AddVoiceRequests
└── Infrastructure/Mistral/MistralClient.cs    G

backend/tests/Butcher.Api.Tests/
├── Application/Assistant/*                    G  (+ AssistantEvaluation R : sans mesure de la phrase du LLM)
├── Application/Assistant/AssistantServiceTests.cs   N  Testcontainers (R-09)
├── Application/Services/ReportServiceTests.cs       M
└── Common/Authorization/*                           M  politique AssistantEnabled

frontend/src/
├── components/domain/ActionFab.vue            R  « Dicter » seulement si assistantEnabled
├── components/domain/AssistantPanel.vue       G
├── composables/useAssistant.ts                R  voix par requestId (R-05)
├── composables/useAssistantDraft.ts           G
├── api/assistant.ts, api/types.ts             R  requestId, stock_answer, route de la voix
├── api/reports.ts                             M  usage de l'assistant
├── views/ReportsView.vue                      M  section « Assistant vocal »
├── components/domain/AccountEditDialog.vue    M  case « Assistant vocal »
└── views/SaleAddView.vue                      G

Caddyfile                                      M  microphone=(self), media-src 'self' blob: (R-07)
docker-compose.prod.yml, .env.example          M  MISTRAL_API_KEY, Assistant__* (R-07)
spikes/assistant-vocal/                        G  outillage du spike, hors build (R-10)
```

**Structure Decision** : les deux applications existantes, sans nouveau projet ni service. Le code de
l'assistant reste regroupé dans `Application/Assistant/` côté backend et dans ses composants et
composables côté frontend.

### Documents à mettre à jour (principe IV)

| Document | Changement |
|---|---|
| `docs/PRD.md` | **RF-34** : dicter une question de stock ; **RF-35** : dicter une vente, ouverte pré-remplie ; **RF-36** : activation par compte et suivi de l'usage. Assistant ajouté au périmètre (§4). |
| `docs/data-model.md` | `app_user.assistant_enabled`, table `voice_request`, libellés des issues et des modes à la correspondance (§4.2). |
| `CLAUDE.md` | État d'avancement ; pièges : ne jamais faire lire un texte libre par le service de voix, ne jamais transmettre un nom de client au LLM, `Caddyfile` à mettre à jour à la main pour le micro. |
| `docs/assistant-vocal-fonctionnement.md` | Activation, journal, limite, voix par demande. |
| `docs/cadrage-assistant-vocal.md`, `docs/spike-assistant-vocal.md` | Figés : documents du spike, renvoyant vers la spec. |

## Complexity Tracking

Aucune violation de la constitution à justifier.
