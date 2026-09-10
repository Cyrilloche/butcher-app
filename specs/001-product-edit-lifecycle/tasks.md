---

description: "Task list for feature implementation"
---

# Tasks: Modification et fin de vie d'un produit

**Input**: Design documents from `/specs/001-product-edit-lifecycle/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/api.md](./contracts/api.md)

**Tests**: Chaque tâche touchant une règle de gestion porte sa tâche de test backend. Ce n'est pas
optionnel ici : le principe II de la constitution fait du backend le garant des règles et exige leur
couverture. Les tests frontend restent hors périmètre, le projet n'en ayant pas de convention établie.

**Organization**: Tâches groupées par histoire utilisateur, pour que chacune soit implémentable et
testable indépendamment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: parallélisable, fichiers distincts et aucune dépendance sur une tâche inachevée
- **[Story]**: histoire utilisateur concernée (US1, US2, US3, US4)
- Chemins de fichiers exacts dans chaque description

## Path Conventions

Application web, deux applications : `backend/src/Butcher.Api/`, `backend/tests/Butcher.Api.Tests/`,
`frontend/src/`. Documentation de référence dans `docs/`.

---

## Phase 1: Setup

**Purpose**: préparer le terrain de développement. Aucune initialisation de projet n'est nécessaire,
le socle existant est complet.

- [X] T001 Démarrer la base de développement et appliquer les migrations existantes avec `make db-up` puis `make migrate`, en vérifiant que `development/.env` est présent
- [X] T002 Lancer `make test` pour constater une suite verte avant toute modification, et noter le nombre de tests de référence

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: exposer l'état de cycle de vie du produit, dont dépendent toutes les histoires. À
terminer avant d'entamer US1.

- [X] T003 Ajouter `IsUsed` et `RemainingStockUnitCount` à `backend/src/Butcher.Api/Application/Dtos/ProductDto.cs`
- [X] T004 Calculer ces deux champs dans `backend/src/Butcher.Api/Application/Services/ProductService.cs`, `IsUsed` par l'existence d'un `production_batch` rattaché et `RemainingStockUnitCount` par le nombre d'unités `available` ou `opened`, sans ajouter de colonne en base (voir research.md D-03)
- [X] T005 Couvrir le calcul des deux champs dans `backend/tests/Butcher.Api.Tests/Application/Services/ProductServiceTests.cs` : produit sans lot, produit avec lot, produit dont toutes les unités sont sorties
- [X] T006 [P] Refléter les deux champs sur le type `ProductDto` de `frontend/src/api/types.ts`

**Checkpoint**: la fiche produit peut lire l'état « utilisé » et le stock restant, sans encore en
tirer de conséquence.

---

## Phase 3: User Story 1 - Corriger un produit qui n'a jamais servi (Priority: P1)

**Goal**: rendre les quatre champs descriptifs modifiables tant qu'aucun lot n'est rattaché.

**Independent Test**: créer un produit, ne lui rattacher aucun lot, modifier nom, code, mode de vente
et vente à la tranche, vérifier que la fiche reflète les nouvelles valeurs.

- [X] T007 [US1] Ajouter les champs requis `Code` et `SaleMode` à `backend/src/Butcher.Api/Application/Dtos/UpdateProductRequest.cs` (rupture de contrat assumée, voir research.md D-04)
- [X] T008 [US1] Étendre `UpdateAsync` dans `backend/src/Butcher.Api/Application/Services/ProductService.cs` pour appliquer les quatre champs sur un produit jamais utilisé, en normalisant le code en majuscules
- [X] T009 [US1] Étendre le contrôle d'unicité du code de `ProductService.EnsureCodeIsUniqueAsync` à la mise à jour, en excluant le produit courant, sur les produits actifs comme désactivés (FR-007)
- [X] T010 [US1] Forcer `AllowPartialSale` à faux lors d'un passage de `by_weight` à `by_piece` dans `ProductService.UpdateAsync` (FR-009)
- [X] T011 [US1] Corriger l'appel à `EnsureAllowPartialSaleIsApplicable` dans `backend/src/Butcher.Api/Application/Services/ProductService.cs` : la validation reçoit aujourd'hui le mode de vente **en base**, ce qui laisserait passer un passage en mode pièce avec vente à la tranche activée dans la même requête ; elle doit recevoir le mode **demandé** (FR-008)
- [X] T012 [US1] Couvrir dans `backend/tests/Butcher.Api.Tests/Application/Services/ProductServiceTests.cs` : mise à jour des quatre champs sur un produit sans lot, refus sur code en doublon insensible à la casse, refus de la vente à la tranche hors mode poids, extinction automatique de la vente à la tranche au passage en mode pièce
- [X] T013 [P] [US1] Aligner `updateProduct` sur le nouveau contrat dans `frontend/src/api/products.ts`
- [X] T014 [US1] Rendre code et mode de vente saisissables dans le formulaire de `frontend/src/views/ProductDetailView.vue`, en réutilisant le composant `SaleModeToggle` de `frontend/src/components/domain/`
- [X] T015 [US1] Afficher le message d'erreur français renvoyé par le serveur en cas de refus dans `frontend/src/views/ProductDetailView.vue`, sans le reformuler côté client

**Checkpoint**: une erreur de saisie sur un produit tout juste créé se corrige entièrement depuis sa
fiche.

---

## Phase 4: User Story 2 - Comprendre ce qui est verrouillé (Priority: P1)

**Goal**: refuser côté serveur la modification du code et du mode de vente d'un produit utilisé, et
rendre ce gel lisible à l'écran.

**Independent Test**: ouvrir la fiche d'un produit rattaché à au moins un lot, constater les champs
en lecture seule et leur explication, puis vérifier par Swagger que le refus tient hors interface.

**Dependency**: s'appuie sur la méthode de mise à jour étendue en US1. US1 apporte le chemin
d'acceptation, US2 le chemin de refus.

- [X] T016 [US2] Refuser dans `ProductService.UpdateAsync` toute valeur de `Code` ou `SaleMode` différente de celle en base lorsque le produit est utilisé, en levant une `ConflictException` au message français (FR-004, FR-005)
- [X] T017 [US2] Accepter dans `backend/src/Butcher.Api/Application/Services/ProductService.cs` une mise à jour dont `Code` et `SaleMode` sont identiques aux valeurs en base, afin que le client puisse renvoyer la ressource complète sans raisonner sur le gel (research.md D-04)
- [X] T018 [US2] Couvrir dans `ProductServiceTests.cs` : refus du changement de code sur un produit ayant un lot, refus du changement de mode de vente, acceptation du changement de nom, acceptation d'un envoi à valeurs identiques
- [X] T019 [US2] Passer code et mode de vente en lecture seule dans `frontend/src/views/ProductDetailView.vue` lorsque `isUsed` est vrai
- [X] T020 [US2] Ajouter sous ces champs une explication française du gel et de la marche à suivre, désactiver puis recréer, dans `frontend/src/views/ProductDetailView.vue` (FR-006)

**Checkpoint**: le gel est appliqué par le serveur et compris par l'utilisateur sans aide extérieure.

---

## Phase 5: User Story 3 - Supprimer un lot créé par erreur (Priority: P1)

**Goal**: permettre la suppression d'un lot intact, garantir qu'un numéro émis n'est jamais réémis,
et rendre au produit sa modifiabilité quand son dernier lot disparaît.

**Independent Test**: créer un lot, ne rien en vendre, le supprimer, vérifier la disparition du lot et
de ses unités, le retour du produit à l'état modifiable, et qu'un nouveau lot du même jour ne reprend
pas le numéro libéré.

**Note**: le registre de séquences existe uniquement pour cette histoire. Il est traité ici plutôt
qu'en phase fondation.

### Registre de numérotation (prérequis interne à US3)

- [X] T021 [US3] Créer l'entité `BatchNumberSequence` dans `backend/src/Butcher.Api/Domain/Entities/BatchNumberSequence.cs` avec `ProductId`, `ProductionDate` et `LastSequence`
- [X] T022 [US3] Créer `backend/src/Butcher.Api/Infrastructure/Data/Configurations/BatchNumberSequenceConfiguration.cs` : table `batch_number_sequence`, clé composite `(product_id, production_date)`, clé étrangère vers `product` en `Restrict`
- [X] T023 [US3] Déclarer le `DbSet` dans `backend/src/Butcher.Api/Infrastructure/Data/AppDbContext.cs`
- [X] T024 [US3] Remplacer `GenerateBatchNumberAsync` dans `backend/src/Butcher.Api/Application/Services/ProductionBatchService.cs` par une lecture-incrément du registre dans la transaction de création, et retirer la boucle de rattrapage devenue inutile en gardant l'index unique sur `batch_number` comme filet
- [X] T025 [US3] Générer la migration avec `make migration name=AddBatchNumberSequence`, puis compléter son `Up` par l'initialisation du registre depuis les lots existants, sans laquelle le premier lot créé après déploiement réémet un numéro déjà porté (voir data-model.md §6)
- [X] T026 [US3] Couvrir dans `backend/tests/Butcher.Api.Tests/Application/Services/ProductionBatchServiceTests.cs` : deux lots le même jour reçoivent 1 puis 2, et après suppression du lot 2 un nouveau lot du même jour reçoit 3 et jamais 2 (FR-013, SC-004)

### Suppression du lot

- [X] T027 [US3] Ajouter `DeleteAsync` à `backend/src/Butcher.Api/Application/Services/ProductionBatchService.cs` et à son interface : refus par `ConflictException` si une unité du lot porte un `stock_movement`, message français nommant le nombre d'unités concernées (FR-012)
- [X] T028 [US3] Supprimer explicitement les unités du lot puis le lot dans une seule transaction, au sein de `DeleteAsync` de `backend/src/Butcher.Api/Application/Services/ProductionBatchService.cs`, en laissant le `Restrict` de `backend/src/Butcher.Api/Infrastructure/Data/Configurations/StockUnitConfiguration.cs` comme filet (research.md D-02)
- [X] T029 [US3] Exposer `DELETE /api/production-batches/{id}` dans `backend/src/Butcher.Api/Controllers/ProductionBatchesController.cs`, retour `204`
- [X] T030 [US3] Couvrir dans `ProductionBatchServiceTests.cs` : suppression d'un lot intact avec ses unités, refus sur un lot dont une unité est vendue, refus sur un lot dont une unité est sortie en perso ou en perte, et produit redevenu jamais utilisé après suppression de son dernier lot (FR-015, SC-006)
- [X] T031 [P] [US3] Ajouter `deleteProductionBatch` à `frontend/src/api/productionBatches.ts`
- [X] T032 [US3] Créer `frontend/src/components/domain/BatchDeleteAction.vue` : action de suppression et confirmation française annonçant le nombre d'unités qui disparaîtront (FR-014)
- [X] T033 [US3] Brancher ce composant sur l'en-tête de lot de `frontend/src/views/StockDetailView.vue`, et rafraîchir le détail après suppression (research.md D-07)

**Checkpoint**: un lot créé par erreur s'annule, et le gel du produit n'est plus une impasse.

---

## Phase 6: User Story 4 - Retirer un produit du service (Priority: P2)

**Goal**: interdire la désactivation tant qu'il reste du stock, offrir le solde en perte d'une
sélection d'unités, et garder le produit désactivé lisible dans l'historique.

**Independent Test**: sur un produit ayant trois unités restantes, constater le refus de
désactivation, solder deux unités puis constater que le refus tient, solder la troisième et
désactiver.

- [X] T034 [US4] Écrire la règle de poids des sorties côté serveur, dans `backend/src/Butcher.Api/Application/Services/StockMovementRules.cs` : poids pesé si l'unité est `available`, poids pesé moins la somme des `sold_weight` de ses ventes si elle est `opened`, `null` si l'unité n'a pas de poids. Elle n'existe pas encore côté serveur, elle est aujourd'hui calculée par le frontend (FR-020, research.md D-05)
- [X] T035 [US4] Utiliser cette règle dans `CreateAsync` de `backend/src/Butcher.Api/Application/Services/StockMovementService.cs` pour les mouvements de type `personal` et `loss` : le poids est calculé par le serveur et le `SoldWeight` reçu du client est ignoré pour ces deux types. Le type `sale` continue de recevoir le poids réellement pesé
- [X] T036 [US4] Couvrir dans `backend/tests/Butcher.Api.Tests/Application/Services/StockMovementServiceTests.cs` le calcul serveur du poids pour une sortie perso ou perte sur unité disponible, sur unité entamée, et sur unité sans poids
- [X] T037 [P] [US4] Retirer le calcul du restant de `frontend/src/components/domain/StockUnitOutcomeMenu.vue` et de `getRemainingWeightKg` dans `frontend/src/composables/useStock.ts` pour la partie perso et perte, le serveur en étant désormais le seul auteur. L'usage de cette fonction par `frontend/src/views/SaleAddView.vue` reste en place : c'est une aide à la saisie d'une vente, que l'utilisateur peut corriger, pas une règle enregistrée
- [X] T038 [US4] Ajouter un filtre `productId` à `GetAllAsync` de `backend/src/Butcher.Api/Application/Services/StockUnitService.cs` et au paramétrage de `backend/src/Butcher.Api/Controllers/StockUnitsController.cs`, combinable avec `status`
- [X] T039 [US4] Créer `backend/src/Butcher.Api/Application/Dtos/WriteOffProductStockRequest.cs` portant la liste des identifiants d'unités
- [X] T040 [US4] Ajouter `WriteOffStockAsync` à `backend/src/Butcher.Api/Application/Services/ProductService.cs` : une sortie de type `loss` par unité sélectionnée encore `available` ou `opened`, poids calculé par le serveur, unités déjà sorties ignorées, sélection vide sans effet, le tout en une transaction. Une unité entamée dont les ventes couvrent déjà tout le poids pesé donne un restant nul : elle est clôturée en `sold` au lieu de recevoir une perte de poids nul, que `StockMovementRules.ValidateSoldWeight` rejetterait. Le passage au statut `lost` est obtenu en réutilisant `StockMovementRules.DetermineNextStatus`, qui traite déjà le cas d'une unité entamée, sans code nouveau (FR-017, FR-019, FR-021)
- [X] T041 [US4] Refuser dans `WriteOffStockAsync` de `backend/src/Butcher.Api/Application/Services/ProductService.cs` une unité qui n'appartient pas au produit, par `BadRequestException` au message français
- [X] T042 [US4] Exposer `POST /api/products/{id}/write-off` dans `backend/src/Butcher.Api/Controllers/ProductsController.cs`, retour `200` avec le nombre d'unités soldées
- [X] T043 [US4] Ajouter le garde-fou de `DeactivateAsync` dans `ProductService.cs` : `ConflictException` tant qu'il reste une unité `available` ou `opened`, message français nommant le nombre restant, et idempotence conservée sur un produit déjà désactivé (FR-016)
- [X] T044 [US4] Couvrir dans `ProductServiceTests.cs` : refus de désactivation avec du stock restant, désactivation acceptée une fois tout sorti, solde d'une unité disponible portant son poids pesé, solde d'une unité entamée portant le restant estimé, solde d'une unité sans poids, solde d'une unité entamée au restant nul qui doit être clôturée et non perdue, solde partiel laissant le refus de désactivation en place
- [X] T045 [P] [US4] Ajouter `writeOffProductStock` à `frontend/src/api/products.ts` et le filtre `productId` à `frontend/src/api/stockUnits.ts`
- [X] T046 [US4] Créer `frontend/src/components/domain/ProductWriteOffDialog.vue` : liste des unités restantes du produit, sélection, sélection totale immédiate, confirmation française annonçant le nombre d'unités déclarées perdues (FR-018, FR-022)
- [X] T047 [US4] Brancher la boîte de dialogue sur `frontend/src/views/ProductDetailView.vue` et la proposer depuis le message de refus de désactivation
- [X] T048 [US4] Ajouter la confirmation de désactivation et de réactivation dans `frontend/src/views/ProductDetailView.vue` (FR-025, FR-026)
- [X] T049 [US4] Vérifier qu'un produit désactivé reste affiché et signalé comme tel dans les écrans d'historique, `frontend/src/views/StockDetailView.vue` et `frontend/src/views/SaleDetailView.vue`, et corriger si le libellé manque (FR-024)

**Checkpoint**: un produit en fin de vie se retire proprement, sans laisser de stock orphelin ni
effacer d'historique.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T050 [P] Mettre à jour `docs/data-model.md` en v0.8 : entité `batch_number_sequence`, politique de mutabilité du produit, suppression de lot, trous assumés dans les séries de numéros
- [X] T051 [P] Mettre à jour `CLAUDE.md` : ligne d'avancement de la Vague 1, et ajout aux pièges connus de la réémission d'un numéro de lot après suppression
- [X] T052 Dérouler les quatre scénarios de [quickstart.md](./quickstart.md) sur la pile locale, en portant une attention particulière au scénario 3 étape 6, le point le plus exposé aux régressions — **déroulé à la main le 2026-09-10**. Sans défaut, à une réserve d'affichage près : le numéro d'une unité, composé du numéro de son lot suivi de son rang, a été lu comme un sous-lot. Décision prise le même jour de faire porter le numéro par l'unité plutôt que par la fabrication, traitée comme une fonctionnalité distincte
- [X] T053 Lancer `make test` et confirmer une suite verte, en comparant au nombre relevé en T002
- [X] T054 Committer en Conventional Commits, avec un `feat(backend)!` portant la rupture de `PUT /api/products/{id}` pour que le changelog et la version du backend la reflètent (constitution, principes III et IV)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : sans dépendance.
- **Foundational (Phase 2)** : dépend de Setup. **Bloque toutes les histoires.**
- **US1 (Phase 3)** : dépend de Foundational.
- **US2 (Phase 4)** : dépend de US1, dont elle prolonge la méthode de mise à jour.
- **US3 (Phase 5)** : dépend de Foundational seulement. Peut avancer en parallèle de US1 et US2.
- **US4 (Phase 6)** : dépend de Foundational seulement. Peut avancer en parallèle de US1, US2 et US3.
- **Polish (Phase 7)** : dépend de toutes les histoires livrées.

### User Story Dependencies

US1 et US2 partagent `ProductService.UpdateAsync` et se suivent. US3 et US4 sont indépendantes l'une
de l'autre et des deux premières : elles touchent des services distincts, `ProductionBatchService`
pour l'une, la désactivation et le solde pour l'autre.

Une réserve pratique : US3 et US4 modifient toutes deux `ProductServiceTests.cs`, et US2 et US4
modifient toutes deux `ProductDetailView.vue`. Menées en parallèle, elles se croiseront sur ces deux
fichiers.

### Within Each Story

Ordre constant : contrat et DTO, puis service et règle métier, puis test backend, puis endpoint,
puis client HTTP, puis interface.

### Parallel Opportunities

- T006 pendant T003 à T005, le type frontend étant indépendant du service backend.
- T013 pendant T014 et T015.
- T031 pendant T032, T045 pendant T046.
- T050 et T051 ensemble, fichiers de documentation distincts.
- À l'échelle des histoires : une personne sur US1 puis US2, une autre sur US3, une troisième sur
  US4, une fois la phase fondation terminée.

## Implementation Strategy

### MVP

**US1 plus US2**, soit les phases 1 à 4. C'est la demande directe des utilisateurs tests : corriger
un produit tout juste créé, et comprendre pourquoi on ne peut plus le corriger ensuite. Livrable et
démontrable sans les deux autres histoires.

Réserve à assumer si l'on s'arrête là : le gel est une impasse tant que US3 n'est pas livrée. Un lot
créé par mégarde condamne le code du produit. C'est pourquoi US3 est en priorité P1 et non P2, et ne
devrait pas rester longtemps en attente derrière le MVP.

### Incremental Delivery

1. Phases 1 et 2, socle de lecture de l'état produit.
2. US1 puis US2, le MVP.
3. US3, qui lève l'impasse du gel.
4. US4, la fin de vie propre.
5. Polish, documentation et release.

### Release

La rupture de contrat sur la mise à jour d'un produit impose de déployer backend et frontend
ensemble. Poser le tag `backend-v*` et le tag `frontend-v*` dans la même fenêtre, et pousser chaque
tag dans la foulée.

## Notes

- Chaque tâche touchant une règle de gestion porte sa tâche de test backend, conformément au principe
  II de la constitution.
- Référencer les identifiants `FR-xxx` dans les messages de commit, conformément au principe IV.
- Aucune détection de conflit d'édition concurrente n'est à implémenter : c'est une décision prise en
  clarification (FR-028), pas un oubli.
