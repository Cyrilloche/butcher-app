---

description: "Task list — Backoffice PC, comptes nominatifs et rôles"
---

# Tasks: Backoffice PC — comptes nominatifs et rôles

**Input**: Design documents from `specs/005-backoffice/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/api.md](./contracts/api.md)

**Tests**: backend obligatoires pour toute règle nouvelle (constitution, principe II) — xUnit sur
PostgreSQL réel via `backend/tests/Butcher.Api.Tests/Support/PostgresDatabaseFixture.cs`. Pas de test
frontend (existant) : chaque tâche frontend se vérifie par `vue-tsc`, ESLint et le build Docker, puis
par [quickstart.md](./quickstart.md).

**Organization**: une phase par récit, dans l'ordre de priorité de la spec. **Une tâche = un commit
vérifié**, poussé sur `feat/backoffice`, sans merge ni tag.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers distincts, aucune dépendance sur une tâche non terminée)
- **[Story]** : récit servi (US1…US5)
- Chemins relatifs à la racine du dépôt

## Vérification commune à chaque tâche

- **Backend** : `make test` passe ; une tâche avec migration l'applique sur la base de dev (`make run`).
- **Frontend** : `npx vue-tsc --noEmit -p tsconfig.app.json` et `npx eslint <fichiers>` passent ;
  build Docker `docker build -f "$PWD/frontend/Dockerfile" "$PWD/frontend"` (le build Vite local
  échoue sous WSL, `node_modules` étant installé côté Windows).
- **Commit** : Conventional Commits, chemins explicites (arbre de travail partagé), identifiants
  `RF-xx` / `ADR-xxx` cités.

---

## Phase 1: Setup — décision tracée

**Purpose**: poser la décision structurante avant le code (constitution, principe IV)

- [X] T001 Rédiger ADR-011 « Comptes nominatifs avec deux rôles » dans `docs/ADR.md` : contexte (compte partagé, besoin de traçabilité et de restriction), décision (rôle en colonne `admin`/`user`, droits relus en base par la politique `AdminOnly`, auteur posé dans `SaveChanges`, mot de passe 20/32 caractères, `GET /api/auth/me`), conséquences, alternatives écartées (research R-01 à R-06) ; ajouter la ligne à l'index ; passer ADR-009 au statut « Accepté — remplacé en partie par ADR-011 (compte partagé, absence de rôles) » avec un renvoi dans sa section Conséquences

---

## Phase 2: Foundational — socle des comptes

**Purpose**: schéma, compte de la requête, autorisation. **Bloque tous les récits.**

- [X] T002 Ajouter l'enum `AccountRole` (`Admin`, `User`) dans `backend/src/Butcher.Api/Domain/Entities/AccountRole.cs` et les propriétés `DisplayName`, `Role`, `IsActive`, `LastLoginAt`, `UpdatedAt` à `backend/src/Butcher.Api/Domain/Entities/AppUser.cs` ; créer `backend/src/Butcher.Api/Infrastructure/Data/Configurations/AppUserConfiguration.cs` (`display_name` varchar(100) non nul, `role` stocké en texte `snake_case` défaut `user`, `is_active` défaut `true`) ; générer la migration `AddAccountRoles` qui reprend les comptes existants en `admin`, actifs, `display_name` = partie locale de l'email (data-model §1, FR-010) ; appliquer sur la base de dev et vérifier `role = 'admin'` sur le compte seedé
- [X] T003 [P] Créer `ICurrentAccount` et `HttpCurrentAccount` (id du compte lu dans le claim `sub`, `null` hors requête) dans `backend/src/Butcher.Api/Common/Authorization/` ; enregistrer `IHttpContextAccessor` et le service dans `backend/src/Butcher.Api/Program.cs`
- [X] T004 Poser `CreatedById` à l'insertion dans `backend/src/Butcher.Api/Infrastructure/Data/AppDbContext.cs` (dépendance facultative `ICurrentAccount`, à côté de `StampAuditDates`, sans écraser une valeur déjà posée) ; tests dans `backend/tests/Butcher.Api.Tests/Infrastructure/Data/CreatedByStampingTests.cs` : auteur posé sur lot, vente et mouvement ; `null` sans compte ; valeur explicite respectée (RF-27, FR-020) ; adapter `PostgresDatabaseFixture.CreateDbContext` pour accepter un compte courant
- [X] T005 [P] Ajouter `ForbiddenException` dans `backend/src/Butcher.Api/Common/Exceptions/ForbiddenException.cs` et sa correspondance `403 « Accès réservé »` dans `backend/src/Butcher.Api/Common/ExceptionHandlingMiddleware.cs`
- [X] T006 Créer `AuthorizationPolicies` (`AdminOnly`), `ActiveAccountRequirement`, `AdminRequirement` et leur handler relisant le compte en base (actif ; actif et administrateur) dans `backend/src/Butcher.Api/Common/Authorization/` ; brancher `ActiveAccountRequirement` dans la politique par défaut et déclarer `AdminOnly` dans `backend/src/Butcher.Api/Program.cs` (research R-02, R-03) ; tests dans `backend/tests/Butcher.Api.Tests/Common/Authorization/AccountAuthorizationHandlerTests.cs` : compte actif accepté, désactivé refusé, utilisateur refusé sur `AdminOnly`, administrateur accepté, compte inconnu refusé
- [X] T007 Politique de mot de passe par rôle : socle 20 caractères et 10 caractères distincts dans `backend/src/Butcher.Api/Infrastructure/Identity/IdentityPolicy.cs`, `AdminPasswordValidator` (32 caractères, 12 distincts, messages en français) dans `backend/src/Butcher.Api/Infrastructure/Identity/AdminPasswordValidator.cs`, enregistré dans `Program.cs` et dans `PostgresDatabaseFixture.CreateUserManager` ; réécrire `backend/tests/Butcher.Api.Tests/Infrastructure/Identity/IdentityPolicyTests.cs` par rôle (FR-034) ; mettre à jour `.env.example` et `development/.env.example`

**Checkpoint**: schéma migré, auteur posé, autorisation prête. Aucun changement visible encore.

---

## Phase 3: User Story 1 — Chacun son compte (Priority: P1) 🎯 MVP

**Goal**: l'administrateur crée des comptes nominatifs ; chacun se connecte avec le sien et signe ses saisies.

**Independent Test**: créer deux comptes depuis l'interface, se connecter avec chacun, enregistrer une vente, lire « Saisie par … » sur son détail (quickstart, lot 1, étapes 1 à 4 et 7 à 9).

### Backend

- [X] T008 [US1] Dans `backend/src/Butcher.Api/Application/Services/AuthService.cs` : refuser un compte désactivé à `LoginAsync` et `RefreshAsync` (« Ce compte est désactivé. »), renseigner `LastLoginAt` à la connexion ; tests dans `backend/tests/Butcher.Api.Tests/Application/Services/AuthServiceTests.cs` (FR-007)
- [X] T009 [US1] Ajouter `GET /api/auth/me` (`MeDto`) et `POST /api/auth/change-password` (`ChangePasswordRequest` : mot de passe actuel requis, politique du rôle, révocation des autres sessions) dans `backend/src/Butcher.Api/Controllers/AuthController.cs`, `AuthService.cs`, `IAuthService.cs` et `backend/src/Butcher.Api/Application/Dtos/` ; tests : `me` renvoie nom et rôle, changement refusé avec un mauvais mot de passe actuel ou un nouveau mot de passe non conforme, sessions révoquées (FR-004, FR-006)
- [X] T010 [US1] Créer `IAccountService` / `AccountService` dans `backend/src/Butcher.Api/Application/Services/` : lister, créer (email unique, rôle, politique), modifier nom et rôle (promotion avec nouveau mot de passe validé pour le rôle cible), désactiver (jamais soi-même ni le dernier administrateur actif, révocation des refresh tokens), réactiver, réinitialiser le mot de passe (révocation) — invariant du dernier administrateur vérifié sous transaction ; tests dans `backend/tests/Butcher.Api.Tests/Application/Services/AccountServiceTests.cs` couvrant chaque refus (FR-003, FR-008, FR-009, FR-035)
- [X] T011 [US1] Créer `backend/src/Butcher.Api/Controllers/AccountsController.cs` (`[Authorize(Policy = AdminOnly)]`, routes de `contracts/api.md` §2) et les DTO `AccountDto`, `CreateAccountRequest`, `UpdateAccountRequest`, `ResetPasswordRequest` dans `backend/src/Butcher.Api/Application/Dtos/` ; enregistrer le service dans `Program.cs` ; vérifier les routes à la main sur l'API locale (`403` en utilisateur, `201` en administrateur)
- [X] T012 [US1] Exposer `CreatedByName` sur `SaleDto`, `ProductionBatchDto`, `StockMovementDto` et leurs projections dans `SaleService.cs`, `ProductionBatchService.cs`, `StockMovementService.cs` (`backend/src/Butcher.Api/Application/`) ; étendre les tests de service existants pour vérifier le nom de l'auteur et `null` avant comptes nominatifs (FR-020a, FR-026)

### Frontend

- [X] T013 [P] [US1] Ajouter les types `AccountRole`, `AccountDto`, `MeDto`, les requêtes de comptes et `createdByName` dans `frontend/src/api/types.ts` ; créer `frontend/src/api/accounts.ts` ; ajouter `me()` et `changePassword()` dans `frontend/src/api/auth.ts` ; ajouter les libellés `admin` → « Administrateur », `user` → « Utilisateur » à côté des correspondances existantes
- [X] T014 [US1] Étendre `frontend/src/stores/auth.ts` : `account`, `isAdmin`, chargement de `/api/auth/me` après connexion, rafraîchissement et `ensureReady`, effacement à la déconnexion ; afficher le nom du compte connecté à la place de l'email là où il apparaît
- [X] T015 [P] [US1] Créer `frontend/src/components/domain/AuthorLabel.vue` (« Saisie par Mireille », « Compte partagé (avant comptes nominatifs) » si `null`) et l'afficher dans `frontend/src/views/SaleDetailView.vue` (vente et lignes) et `frontend/src/views/StockDetailView.vue` (en-tête de fournée)
- [X] T016 [US1] Créer `frontend/src/views/AccountsView.vue` : liste (nom, email, rôle, état, dernière connexion), création, modification du nom et du rôle avec saisie du nouveau mot de passe à la promotion, désactivation et réactivation sous confirmation, réinitialisation du mot de passe ; route `/accounts` avec garde administrateur dans `frontend/src/router/index.ts` ; entrée « Comptes » réservée à l'administrateur dans `frontend/src/layouts/AppLayout.vue`
- [X] T017 [US1] Créer `frontend/src/views/MyAccountView.vue` (changer son mot de passe, règle du rôle énoncée) ; route `/my-account` et accès depuis la navigation

**Checkpoint**: US1 fonctionnelle et testable seule (quickstart lot 1, étapes 1 à 4 et 7 à 9).

---

## Phase 4: User Story 2 — Gestes sensibles réservés (Priority: P2)

**Goal**: désactivation, réactivation et solde en perte d'un produit passent par l'administrateur ; les corrections restent ouvertes à tous.

**Independent Test**: quickstart lot 1, étapes 5 et 6.

- [X] T018 [US2] Poser `[Authorize(Policy = AuthorizationPolicies.AdminOnly)]` sur `Deactivate`, `Reactivate` et `WriteOffStock` dans `backend/src/Butcher.Api/Controllers/ProductsController.cs` ; test dans `backend/tests/Butcher.Api.Tests/Controllers/ReservedActionsTests.cs` vérifiant que ces trois actions, et elles seules parmi les contrôleurs métier, portent la politique (FR-011, FR-012) — commit `feat(backend)!:` (rupture : `403`)
- [X] T019 [US2] Masquer la désactivation, la réactivation et le solde en perte pour un utilisateur dans `frontend/src/views/ProductDetailView.vue` (et `frontend/src/components/domain/ProductWriteOffDialog.vue` si déclenché ailleurs), à partir de `auth.isAdmin` ; afficher le message du serveur si un `403` survient malgré tout (FR-013)

**Checkpoint**: lot 1 (US1 + US2) complet.

---

## Phase 5: User Story 3 — Travailler confortablement sur un PC (Priority: P3)

**Goal**: barre latérale, vue d'ensemble et tableaux sur écran large, d'après la maquette Claude Design ; mobile inchangé.

**Independent Test**: quickstart, lot 2.

- [X] T020 [US3] Exporter la maquette depuis le projet Claude Design `5d1f2fde-8c50-45fc-8970-925e8c9df3b2` (`Backoffice Overview.dc.html`, `Ventes Dashboard.dc.html`, `Stock Dashboard.dc.html`, `Produits Dashboard.dc.html`, `Clients Dashboard.dc.html`) dans `design/backoffice/` ; remplacer la dépendance notée dans `specs/005-backoffice/spec.md` (hypothèses) par le chemin
- [X] T021 [US3] Refondre `frontend/src/layouts/AppLayout.vue` avec `useDisplay()` : à partir de `md`, `v-navigation-drawer` permanent de 248 px (marque « Saloir », date du jour, entrées Vue d'ensemble / Ventes / Stock / Produits / Clients, Comptes pour l'administrateur, compte connecté en pied avec accès à « Mon compte » et déconnexion) ; en dessous, `v-bottom-navigation` actuelle inchangée (FR-015, FR-016) ; ajouter les icônes Phosphor nécessaires dans `frontend/src/plugins/phosphor-iconset.ts`
- [X] T022 [P] [US3] Borner la largeur des formulaires sur écran large (conteneur centré commun) dans `frontend/src/assets/main.css` ou un composant `frontend/src/components/base/AppFormContainer.vue`, appliqué à `StockAddView.vue`, `SaleAddView.vue`, `CustomerAddView.vue`, `ProductAddView.vue` (FR-015a)
- [X] T023 [US3] Créer `frontend/src/composables/useOverview.ts` (fonctions pures : CA de l'année et panier moyen, CA du mois et tendance vs mois précédent, montant et nombre de ventes à encaisser, CA par mois, meilleur mois, dernière vente, ventes récentes) à partir de `listSales()`
- [X] T024 [US3] Créer `frontend/src/views/OverviewView.vue` d'après `Backoffice Overview.dc.html` : titre « Vue d'ensemble » et salutation avec le nom du compte, bouton « Nouvelle vente », quatre cartes (CA avec choix de l'année, Ce mois, À encaisser, **Stock bas marquée « Arrivera en V2 »**), graphique du CA mensuel, dernière vente, ventes récentes ; route `/overview` réservée à l'administrateur (chiffres = rapports, FR-027) dans `frontend/src/router/index.ts`
- [X] T025 [US3] Présenter `frontend/src/views/SalesView.vue` en tableau sur écran large (numéro, date, client, statut, montant, tri) avec filtres client, statut de paiement et période, nombre et total des ventes filtrées, dans le style de `Backoffice Overview.dc.html` (le projet n'a pas de maquette PC de cet écran : `Ventes Dashboard.dc.html` est la maquette mobile, déjà en place) ; présentation mobile inchangée (FR-017, FR-018, clôt E-06)
- [X] T026 [P] [US3] Présenter `frontend/src/views/StockView.vue` en tableau sur écran large dans le style de `Backoffice Overview.dc.html` (le projet n'a pas de maquette PC de cet écran : `Stock Dashboard.dc.html` est la maquette mobile, déjà en place)
- [ ] T027 [P] [US3] Présenter `frontend/src/views/CustomersView.vue` en tableau sur écran large dans le style de `Backoffice Overview.dc.html` (le projet n'a pas de maquette PC de cet écran : `Clients Dashboard.dc.html` est la maquette mobile, déjà en place)
- [ ] T028 [P] [US3] Présenter `frontend/src/views/ProductsView.vue` en tableau sur écran large dans le style de `Backoffice Overview.dc.html` (le projet n'a pas de maquette PC de cet écran : `Produits Dashboard.dc.html` est la maquette mobile, déjà en place)

**Checkpoint**: lot 2 complet, mobile sans régression.

---

## Phase 6: User Story 4 — Savoir qui a fait quoi (Priority: P4) — esquisse

**Goal**: journal consultable par l'administrateur. À détailler au démarrage du lot.

- [ ] T029 [US4] Créer l'entité `AuditEntry`, ses enums et la migration `AddAuditEntries` dans `backend/src/Butcher.Api/` (data-model §3)
- [ ] T030 [US4] Écrire les entrées de création, modification et suppression dans `AppDbContext.SaveChanges`, contenu JSON pour les suppressions seulement ; tests (FR-021, FR-022)
- [ ] T031 [US4] Journaliser connexions, refus, verrouillages et changements de mot de passe dans `AuthService.cs` et `AccountService.cs` ; tests (FR-025)
- [ ] T032 [US4] Exposer `GET /api/audit-entries` filtré et paginé, administrateur, dans `backend/src/Butcher.Api/Controllers/AuditEntriesController.cs` ; tests (FR-023, FR-024)
- [ ] T033 [US4] Créer `frontend/src/views/JournalView.vue` (filtres auteur, type, période ; contenu d'une suppression) et son entrée de navigation administrateur

---

## Phase 7: User Story 5 — Des chiffres pour piloter (Priority: P5) — esquisse

**Goal**: rapports de ventes pour l'administrateur. À détailler au démarrage du lot.

- [ ] T034 [US5] Créer `ReportService` et `ReportsController` (synthèse par période et par mois, par client, par produit, débiteurs) dans `backend/src/Butcher.Api/`, montants issus des lignes enregistrées ; tests au centime (FR-027 à FR-030, SC-006)
- [ ] T035 [US5] Créer `frontend/src/views/ReportsView.vue` et faire lire la vue d'ensemble (T024) sur ces rapports plutôt que sur la liste brute des ventes

---

## Phase 8: Polish & Cross-Cutting Concerns

- [X] T036 Clore la documentation du lot 1 : RF-26 révisée dans `docs/PRD.md` (nouvelle version d'historique), `docs/data-model.md` §3.1 (`app_user` enrichie) et §4.2 (correspondances des rôles), `CLAUDE.md` (pile d'authentification, avancement, pièges : droits relus en base, auteur posé par `SaveChanges`, politique par rôle) — **à faire avant de considérer le lot 1 livrable**
- [X] T037 Dérouler `specs/005-backoffice/quickstart.md` (lot 1) sur l'API et le frontend locaux, consigner le résultat dans le rapport de fin de session

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001)** : aucune dépendance.
- **Foundational (T002–T007)** : T002 d'abord (schéma) ; T003 et T005 en parallèle ; T004 après T003 ; T006 après T002, T003, T005 ; T007 après T002. **Bloque tous les récits.**
- **US1 (T008–T017)** : après la phase 2. Backend T008 → T009 → T010 → T011, T012 indépendante de T010/T011. Frontend : T013 après T009/T011 (contrat stable) ; T014 après T013 ; T015 après T012 et T013 ; T016 après T011 et T014 ; T017 après T009 et T014.
- **US2 (T018–T019)** : après T006 (politique) et T014 (`isAdmin`) ; indépendante du reste d'US1.
- **US3 (T020–T028)** : T021 après T014 (nom du compte, `isAdmin`) ; T022, T026–T028 indépendantes ; T024 après T023 et T021 ; T025 après T021.
- **US4, US5** : après le lot 1.
- **T036** : clôt le lot 1, après T019.

### Parallel Opportunities

- T003 et T005 (phase 2).
- T013 et T012 une fois le contrat stable ; T015 en parallèle de T016.
- T022, T026, T027, T028 (US3), chacune sur ses propres fichiers.

---

## Parallel Example: Phase 2

```text
Task: "T003 ICurrentAccount / HttpCurrentAccount dans backend/src/Butcher.Api/Common/Authorization/"
Task: "T005 ForbiddenException et 403 dans backend/src/Butcher.Api/Common/"
```

## Parallel Example: User Story 3

```text
Task: "T026 StockView en tableau sur écran large"
Task: "T027 CustomersView en tableau sur écran large"
Task: "T028 ProductsView en tableau sur écran large"
```

---

## Implementation Strategy

### MVP — lot 1 (US1 + US2)

1. T001 (ADR-011), puis la phase 2.
2. Backend d'US1 (T008–T012), puis frontend (T013–T017).
3. US2 (T018–T019), puis T036 (documentation).
4. **Valider** : quickstart lot 1 (T037).

### Incremental Delivery

1. Lot 1 → release backend `!` + frontend, rotation des comptes en prod.
2. Lot 2 (US3) → la vue PC.
3. Lots 3 et 4 (US4, US5) → détaillés à leur démarrage.

---

## Notes

- Constitution II : chaque tâche backend qui introduit une règle (dernier administrateur, compte
  désactivé, politique par rôle, action réservée) porte son test dans la même tâche.
- Constitution III : seule T018 rompt le contrat (`403`) et porte `!`.
- Constitution IV : T001 et T036 livrent ADR-011 et les révisions documentaires du lot 1.
- Un compte n'est jamais supprimé : aucune tâche ne crée de route ou de commande de suppression.
