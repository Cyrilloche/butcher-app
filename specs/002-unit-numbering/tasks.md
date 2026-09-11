---

description: "Task list for feature implementation"
---

# Tasks: Le numéro d'étiquette porté par l'unité

**Input**: Design documents from `/specs/002-unit-numbering/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/api.md](./contracts/api.md)

**Tests**: obligatoires. Le principe II de la constitution impose un test backend pour toute règle
de gestion nouvelle ou modifiée, et la non-réémission d'un numéro est précisément ce genre de règle.

**Organization**: les tâches sont groupées par parcours utilisateur. Une particularité ici : les
quatre parcours partagent un même socle de schéma, qui doit être posé avant tout le reste.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable, fichier distinct et sans dépendance en attente
- **[Story]** : parcours concerné (US1 à US4)

## Path Conventions

Monorepo à deux applications : `backend/src/Butcher.Api/`, `backend/tests/Butcher.Api.Tests/`,
`frontend/src/`. Voir `CLAUDE.md` §5.

---

## Phase 1: Setup

**Purpose**: préparer le terrain, sans toucher au code.

- [X] T001 Créer la branche `feat/unit-numbering` depuis `feat/update-product` une fois celle-ci fusionnée ou validée, en tenant compte du fait que l'arbre de travail peut être partagé avec une autre session (constitution, section Processus)
- [ ] T002 Réinitialiser la base de développement avec `make db-down && make db-up` pour partir d'un état connu, le jeu d'essai étant recréable — **non fait, et volontairement** : garder les données a permis de vérifier le rétro-remplissage de la migration sur des fournées réelles

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: déplacer le numéro dans le schéma. Tant que cette phase n'est pas finie, le projet ne
compile pas et aucun parcours n'est testable.

**⚠️ CRITICAL**: aucun travail de parcours ne peut commencer avant la fin de cette phase.

- [X] T003 Renommer `backend/src/Butcher.Api/Domain/Entities/BatchNumberSequence.cs` en `UnitNumberSequence.cs`, renommer la classe, et réécrire son commentaire de classe pour dire qu'elle compte des unités par produit et par date de production (D3, FR-012)
- [X] T004 Renommer `backend/src/Butcher.Api/Infrastructure/Data/Configurations/BatchNumberSequenceConfiguration.cs` en `UnitNumberSequenceConfiguration.cs` et faire pointer `ToTable` sur `unit_number_sequence`
- [X] T005 Renommer le `DbSet` correspondant dans `backend/src/Butcher.Api/Infrastructure/Data/AppDbContext.cs`
- [X] T006 [P] Ajouter la propriété `UnitNumber` (chaîne requise) à `backend/src/Butcher.Api/Domain/Entities/StockUnit.cs`, avec un commentaire disant qu'elle est écrite une fois et jamais modifiée (D1, FR-006)
- [X] T007 [P] Déclarer `UnitNumber` requis et poser son index unique dans `backend/src/Butcher.Api/Infrastructure/Data/Configurations/StockUnitConfiguration.cs` (FR-005)
- [X] T008 Retirer la propriété `BatchNumber` de `backend/src/Butcher.Api/Domain/Entities/ProductionBatch.cs` (D2)
- [X] T009 Retirer la propriété et l'index unique correspondants de `backend/src/Butcher.Api/Infrastructure/Data/Configurations/ProductionBatchConfiguration.cs`
- [X] T010 Retirer de `backend/src/Butcher.Api/Application/Services/ProductionBatchService.cs` la génération du numéro de lot, la détection de conflit d'unicité associée et la transaction qui n'existait que pour elle ; la création d'une fabrication redevient une écriture simple
- [X] T011 Créer la migration `AddUnitNumber` dans `backend/src/Butcher.Api/Infrastructure/Data/Migrations/`, en quatre gestes ordonnés : renommage de la table de séquences, ajout de `unit_number` nullable, rétro-remplissage SQL déterministe, puis passage en non nul avec index unique et suppression de `production_batch.batch_number` (data-model.md §4)
- [X] T012 Écrire dans la migration de `backend/src/Butcher.Api/Infrastructure/Data/Migrations/` le rétro-remplissage annoncé en T011, en parcourant les fournées par `production_date` croissante puis les unités par identifiant croissant, en repartant de 1 par couple produit / date, et en réécrivant `last_sequence` sur le dernier rang attribué (D6)
- [X] T013 Écrire le `Down` de la migration de `backend/src/Butcher.Api/Infrastructure/Data/Migrations/` : retrait de la colonne, restauration de `batch_number` et de l'ancien nom de table, sans prétendre reconstituer les anciens numéros de fournée
- [X] T014 Mettre à jour la liste des tables purgées entre deux tests dans `backend/tests/Butcher.Api.Tests/Support/PostgresDatabaseFixture.cs`
- [X] T015 Ajuster `backend/tests/Butcher.Api.Tests/Application/Services/ProductionBatchServiceTests.cs` pour retirer les assertions sur le numéro de lot généré, sans encore traiter la non-réémission qui revient en US4
- [X] T016 Vérifier que `dotnet build` passe sur `backend/Butcher.sln` et que `make migrate` applique la migration sur une base contenant déjà des fournées et des unités

**Checkpoint**: le schéma porte le numéro au bon endroit, le projet compile, rien ne le remplit encore.

---

## Phase 3: User Story 1 - Étiqueter chaque objet fabriqué (Priority: P1) 🎯 MVP

**Goal**: chaque unité créée reçoit son numéro, unique, continu par produit et par jour.

**Independent Test**: créer une fournée, y générer dix unités, lire les numéros dans le détail du
stock. Le scénario 1 de [quickstart.md](./quickstart.md) suffit à valider ce parcours seul.

### Tests for User Story 1

- [X] T017 [P] [US1] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/StockUnitServiceTests.cs` : générer dix unités sur une fournée neuve donne les rangs 1 à 10 au format `CODE-YYMMDD-N` (FR-001, FR-002)
- [X] T018 [P] [US1] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/StockUnitServiceTests.cs` : une seconde fournée du même produit le même jour reprend à 11, sans repartir de 1 (FR-003)
- [X] T019 [P] [US1] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/StockUnitServiceTests.cs` : deux produits fabriqués le même jour ont des suites indépendantes repartant chacune de 1 (FR-003)
- [X] T020 [P] [US1] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/StockUnitServiceTests.cs` : une fournée dont la date de production est antérieure au jour de saisie prend les rangs de sa date de production, pas de la date du jour
- [X] T021 [P] [US1] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/StockUnitServiceTests.cs` : deux générations d'unités concurrentes sur le même produit et la même date produisent des numéros disjoints, sans erreur d'unicité (D5)
- [X] T022 [P] [US1] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/StockUnitServiceTests.cs` : un produit vendu à la pièce reçoit lui aussi des numéros, le mécanisme de stock restant uniforme (`CLAUDE.md` §8 règle 2)

### Implementation for User Story 1

- [X] T023 [US1] Ajouter dans `backend/src/Butcher.Api/Application/Services/StockUnitService.cs` la réservation d'un bloc de rangs : lecture verrouillée de la ligne de `unit_number_sequence`, création à zéro si absente, incrément du nombre d'unités demandées, le tout dans une transaction (D4, D5)
- [X] T024 [US1] Dans `backend/src/Butcher.Api/Application/Services/StockUnitService.cs`, numéroter les unités construites par `BuildWeightedUnits` et `BuildCountedUnits` sur la plage réservée, dans leur ordre de création, en composant le numéro depuis le code du produit et la date de production de la fournée
- [X] T025 [US1] Dans `backend/src/Butcher.Api/Application/Services/StockUnitService.cs` et `backend/src/Butcher.Api/Controllers/StockUnitsController.cs`, vérifier qu'aucun chemin de mise à jour d'une unité n'expose `UnitNumber` en écriture (FR-006)

**Checkpoint**: les objets fabriqués portent leur numéro. Le parcours principal est livrable.

---

## Phase 4: User Story 2 - Retrouver un objet par son étiquette (Priority: P1)

**Goal**: partout où l'application nomme une unité, elle la nomme par son numéro d'étiquette.

**Independent Test**: prendre un numéro dans le stock, vendre l'unité, la retrouver dans la vente
puis dans l'historique des mouvements. Scénario 2 de [quickstart.md](./quickstart.md).

**Dépendance assumée** : ce parcours suppose US1 livré, puisqu'il affiche ce qu'US1 attribue.

### Tests for User Story 2

- [X] T026 [P] [US2] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/StockMovementServiceTests.cs` : un mouvement projeté porte le numéro de son unité (FR-009)
- [X] T027 [P] [US2] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/SaleServiceTests.cs` : les lignes d'une vente nomment leurs unités par leur numéro d'étiquette

### Implementation for User Story 2

- [X] T028 [P] [US2] Remplacer `BatchNumber` par `UnitNumber` dans `backend/src/Butcher.Api/Application/Dtos/StockUnitDto.cs`, en conservant `BatchId` dont le frontend a besoin pour regrouper (contracts/api.md)
- [X] T029 [P] [US2] Même remplacement dans `backend/src/Butcher.Api/Application/Dtos/StockMovementDto.cs`
- [X] T030 [US2] Adapter la projection de `backend/src/Butcher.Api/Application/Services/StockUnitService.cs` pour lire le numéro sur l'unité et non sur la fournée
- [X] T031 [P] [US2] Adapter la projection de `backend/src/Butcher.Api/Application/Services/StockMovementRules.cs`
- [X] T032 [P] [US2] Adapter la projection de `backend/src/Butcher.Api/Application/Services/SaleService.cs`
- [X] T033 [US2] Aligner `frontend/src/api/types.ts` sur le contrat : `unitNumber` sur les deux DTO concernés, avec un commentaire disant que c'est le numéro écrit sur l'étiquette
- [X] T034 [US2] Dans `frontend/src/composables/useStock.ts`, supprimer la composition « numéro de lot + rang dans le lot » et utiliser le numéro reçu du serveur (FR-015)
- [X] T035 [P] [US2] Aligner `frontend/src/composables/useSales.ts` sur le nouveau champ
- [X] T036 [P] [US2] Aligner `frontend/src/views/SaleDetailView.vue` pour nommer chaque ligne par le numéro de son unité
- [X] T037 [P] [US2] Aligner `frontend/src/components/domain/ProductWriteOffDialog.vue`, dont le libellé d'unité affiche aujourd'hui le numéro de la fournée

**Checkpoint**: un numéro lu sur une étiquette se retrouve dans le stock, dans une vente et dans l'historique.

---

## Phase 5: User Story 3 - La fournée sans numéro (Priority: P2)

**Goal**: plus aucun numéro de fabrication, ni en saisie, ni en affichage, ni dans un message.

**Independent Test**: parcourir Stock, Produits et Ventes en cherchant un numéro de lot, et n'en
trouver aucun. Scénario 3 de [quickstart.md](./quickstart.md).

### Tests for User Story 3

- [X] T038 [P] [US3] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/ProductionBatchServiceTests.cs` : la ressource d'une fabrication n'expose plus de numéro (FR-007)

### Implementation for User Story 3

- [X] T039 [P] [US3] Retirer `BatchNumber` de `backend/src/Butcher.Api/Application/Dtos/ProductionBatchDto.cs` et de sa projection
- [X] T040 [P] [US3] Retirer le champ correspondant de `frontend/src/api/types.ts`
- [X] T041 [US3] Dans `frontend/src/composables/useStock.ts`, calculer le rang d'une fournée dans sa journée à partir des fournées reçues, triées par date de production, et l'exposer comme libellé d'affichage sans jamais le stocker ni le traiter comme un identifiant (D7)
- [X] T042 [US3] Dans `frontend/src/views/StockDetailView.vue`, intituler chaque groupe par sa date, son prix et, si plusieurs fournées partagent la date, son rang dans la journée (FR-008)
- [X] T043 [US3] Réécrire la confirmation de `frontend/src/components/domain/BatchDeleteAction.vue`, qui nomme aujourd'hui le lot par son numéro, pour la fonder sur la date de la fournée et le nombre d'unités concernées
- [X] T044 [US3] Reformuler côté serveur le refus `409` de suppression d'une fournée pour qu'il cesse de nommer un numéro de lot, dans `backend/src/Butcher.Api/Application/Services/ProductionBatchService.cs`

**Checkpoint**: la fournée a perdu son numéro sans perdre son rôle de saisie groupée.

---

## Phase 6: User Story 4 - La suppression ne libère aucun numéro (Priority: P2)

**Goal**: un rang émis reste consommé, quoi qu'il arrive à l'unité ou à la fournée qui le portait.

**Independent Test**: supprimer une fournée intacte, en recréer une le même jour, vérifier que la
numérotation reprend après le dernier rang émis. Scénario 4 de [quickstart.md](./quickstart.md).
C'est le point le plus exposé aux régressions.

### Tests for User Story 4

- [X] T045 [P] [US4] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/ProductionBatchServiceTests.cs` : après suppression d'une fournée intacte, une nouvelle fournée du même jour repart après le dernier rang émis et non à 1 (FR-004, FR-010)
- [X] T046 [P] [US4] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/StockUnitServiceTests.cs` : la suppression d'une unité seule ne libère pas son rang
- [X] T047 [P] [US4] Test dans `backend/tests/Butcher.Api.Tests/Application/Services/ProductionBatchServiceTests.cs` : la ligne de `unit_number_sequence` survit à la suppression de toutes les fournées d'un couple produit / date (FR-012)

### Implementation for User Story 4

- [X] T048 [US4] Vérifier que la suppression d'une fournée dans `backend/src/Butcher.Api/Application/Services/ProductionBatchService.cs` ne touche jamais le registre, et le dire en commentaire à l'endroit où la tentation existera
- [X] T049 [US4] Vérifier le même point sur le chemin de suppression d'une unité dans `backend/src/Butcher.Api/Application/Services/StockUnitService.cs`

**Checkpoint**: deux étiquettes manuscrites identiques sont devenues impossibles.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T050 [P] Mettre à jour `docs/data-model.md` : le numéro d'unité en §3.5, le remplacement de la règle de numérotation en §3.9, et la contrainte C-12 réécrite au niveau de l'unité (principe IV)
- [X] T051 [P] Mettre à jour `CLAUDE.md` : la règle 9 de §8, le piège de §9 sur la dérivation d'un numéro par comptage, et la ligne d'avancement de §2
- [X] T052 Faire passer `make test` en entier sur `backend/` et `npm run type-check` sur `frontend/`
- [X] T053 **À faire par vous** — dérouler les quatre scénarios et la vérification de migration de [quickstart.md](./quickstart.md) sur la pile locale, avec une attention particulière au scénario 4
- [X] T054 Committer en Conventional Commits, en séparant backend, frontend et documentation, avec un `feat(backend)!` portant la rupture des trois DTO (constitution, principes III et IV)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : sans dépendance.
- **Foundational (Phase 2)** : bloque tout. Le projet ne compile pas tant qu'elle n'est pas finie.
- **US1 (Phase 3)** : après la Phase 2. C'est le MVP.
- **US2 (Phase 4)** : après US1, dont elle affiche le résultat.
- **US3 (Phase 5)** : après la Phase 2. Indépendante d'US1 et US2 sur le plan technique.
- **US4 (Phase 6)** : après US1, qui installe le mécanisme dont US4 vérifie la propriété.
- **Polish (Phase 7)** : après tout le reste.

### Within Each User Story

Les tests d'abord, et ils doivent échouer avant l'implémentation. Puis le schéma, puis les services,
puis les projections, puis le frontend.

### Parallel Opportunities

- T006 et T007 en parallèle de T003 à T005 : entités et configurations distinctes.
- T017 à T022 en parallèle : six tests dans deux fichiers déjà séparés.
- T028, T029, T031, T032 en parallèle : quatre fichiers distincts.
- T035, T036, T037 en parallèle : trois fichiers frontend distincts.
- T045, T046, T047 en parallèle.
- T050 et T051 en parallèle : deux documents distincts.

Le point de non-retour est T011. Avant la migration, tout se défait par simple `git checkout` ;
après, il faut la révoquer.

---

## Implementation Strategy

**MVP** : Phases 1 à 3. À ce stade, les objets fabriqués portent un numéro unique et continu, ce qui
est la valeur entière de la fonctionnalité pour la personne qui tient le stylo.

**Incrément suivant** : Phase 4, qui rend ce numéro utile partout où l'on cherche un objet.

**Finition** : Phases 5 et 6, qui retirent l'ancien numéro de l'affichage et verrouillent la
non-réémission par des tests.
