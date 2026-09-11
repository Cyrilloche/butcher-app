---

description: "Tâches d'implémentation — poids encore vendable d'une unité entamée"
---

# Tasks: Poids encore vendable d'une unité entamée

**Input**: documents de conception dans `specs/004-remaining-weight/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/api.md](./contracts/api.md),
[quickstart.md](./quickstart.md)

**Tests** : les tâches de test backend **ne sont pas optionnelles ici**. RG-05 est touchée, et le
principe II de la constitution impose qu'une règle métier modifiée arrive avec sa couverture côté
serveur. Le frontend n'a pas de socle de tests : il se valide par le guide de validation.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable, fichier distinct, sans dépendance sur une tâche inachevée
- **[Story]** : l'histoire utilisateur servie (US1, US2, US3)

## Path Conventions

Monorepo à deux applications : `backend/src/Butcher.Api/`, `backend/tests/Butcher.Api.Tests/`,
`frontend/src/`. Documentation de référence dans `docs/`.

---

## Phase 1: Setup

**Purpose**: pouvoir observer l'écran qu'on modifie, avec des données qui exercent les cas.

- [ ] T001 Démarrer la pile de développement en suivant la section « Prérequis » de [quickstart.md](./quickstart.md) : `make db-up`, `make run`, puis `npm run dev` dans `frontend/`
- [ ] T002 Préparer le jeu d'essai décrit dans [quickstart.md](./quickstart.md) depuis l'application elle-même : un jambon à la tranche avec deux fournées et trois unités pesées, un produit au poids ordinaire, un produit à la pièce, un client

---

## Phase 2: Foundational (prérequis bloquant les trois histoires)

**Purpose**: le champ calculé par le serveur. Les trois histoires en dépendent, aucune ne peut
commencer avant. C'est ici que vit la règle, et ici qu'elle est testée.

- [ ] T003 Renommer `ComputeOutcomeWeight` en `ComputeRemainingWeight` dans `backend/src/Butcher.Api/Application/Services/StockMovementRules.cs`, mettre à jour ses appelants dans `StockMovementService.cs` et `SaleService.cs`, et réécrire son commentaire pour décrire la **valeur calculée** et ses deux usages — le poids à inscrire sur une sortie perso ou perte, et le poids à afficher comme encore vendable (D0 de [research.md](./research.md))
- [ ] T004 Ajouter `RemainingWeight` (`decimal?`) à `backend/src/Butcher.Api/Application/Dtos/StockUnitDto.cs`, avec un commentaire disant qu'il est **calculé à chaque lecture et jamais stocké** (FR-002)
- [ ] T005 Dans `StockUnitService.GetAllAsync` (`backend/src/Butcher.Api/Application/Services/StockUnitService.cs`), projeter chaque unité avec la somme de ses poids vendus, obtenue par une sous-requête sur la navigation `StockMovements` filtrée sur `MovementType.Sale`, puis renseigner `RemainingWeight` via `ComputeRemainingWeight`. **Une seule requête doit partir**, quel que soit le nombre d'unités (D1)
- [ ] T006 Appliquer le même calcul à `GetByIdAsync` et au retour de `AddUnitsAsync` dans `backend/src/Butcher.Api/Application/Services/StockUnitService.cs` — sur des unités qui viennent d'être créées, la somme vendue est nulle et le restant vaut le poids pesé (FR-005)
- [ ] T007 [P] Tester dans `backend/tests/Butcher.Api.Tests/Application/Services/StockUnitServiceTests.cs` : une unité pesée sans aucune vente renvoie un restant égal à son poids pesé ; une unité entamée après une vente partielle renvoie la différence
- [ ] T008 [P] Tester dans `backend/tests/Butcher.Api.Tests/Application/Services/StockUnitServiceTests.cs` : une unité dont la totalité du poids a été vendue renvoie un restant nul et reste au statut entamé ; le restant ne descend jamais sous zéro (FR-003)
- [ ] T009 [P] Tester dans `backend/tests/Butcher.Api.Tests/Application/Services/StockUnitServiceTests.cs` : une unité d'un produit vendu à la pièce et une unité au poids pas encore pesée renvoient un restant **absent**, et non zéro (FR-004)
- [ ] T010 [P] Tester dans `backend/tests/Butcher.Api.Tests/Application/Services/StockUnitServiceTests.cs` qu'une sortie perso ou une perte **n'est pas** retranchée du restant : seul le type vente compte. Un filtre trop large donnerait un restant faussement nul (D10)
- [ ] T011 Vérifier l'absence de N+1 sur la projection de `backend/src/Butcher.Api/Application/Services/StockUnitService.cs` en lisant les requêtes émises par EF Core sur `GET /api/stock-units` avec une dizaine d'unités : une seule requête, agrégat compris
- [ ] T012 [P] Ajouter `remainingWeight: number | null` à l'interface `StockUnitDto` de `frontend/src/api/types.ts`, en respectant le contrat de [contracts/api.md](./contracts/api.md)

**Checkpoint** : à ce stade le serveur expose le champ et le prouve par ses tests. Rien n'a changé
à l'écran, et rien n'est cassé : l'ajout est additif, l'ancien client fonctionne à l'identique.

---

## Phase 3: User Story 1 — Savoir ce qu'il reste sur un jambon entamé (P1)

**Goal**: l'exploitant lit le poids encore vendable d'un jambon sans ouvrir une seule vente passée.

**Independent Test**: vendre deux tranches d'un jambon pesé, ouvrir le détail du produit, lire le
restant sur sa ligne. Livrable seul, sans les deux histoires suivantes.

Cette phase porte aussi la mise en forme validée par l'exploitant (section « Présentation retenue »
de [spec.md](./spec.md)) : c'est elle qui fait de la place au restant sur une ligne d'unité.

- [ ] T013 [US1] Exposer le restant dans `StockDetailUnit` et le renseigner dans `getStockDetail` (`frontend/src/composables/useStock.ts`), en réutilisant le formatage de poids existant
- [ ] T014 [US1] Refondre `frontend/src/components/domain/StockUnitRow.vue` en **deux lignes de hauteur fixe** : en haut le numéro d'étiquette, plus gros caractère de la ligne, et à droite le poids pesé ; en bas l'étiquette de statut, le poids encore vendable, puis les actions. Sur une unité entamée, le poids du haut est préfixé de « pesé » pour lever l'ambiguïté (FR-006, FR-007)
- [ ] T015 [US1] Dans `frontend/src/views/StockDetailView.vue`, sortir la date de fabrication de la carte et la placer **au-dessus, en gros, comme un titre de section**, avec le rang dans la journée, le prix de la fournée et sa corbeille alignés à droite sur la même ligne. La carte ne contient plus que les unités
- [ ] T016 [US1] Brancher une corbeille par unité sur `deleteStockUnit` (`frontend/src/api/stockUnits.ts`, déjà écrite), avec une confirmation nommant le numéro d'étiquette. Le bouton est **désactivé sauf sur une unité disponible** : tout mouvement fait quitter ce statut, donc « disponible » vaut « sans mouvement ». Le refus reste garanti côté serveur, la désactivation n'est qu'une politesse (D6)
- [ ] T017 [US1] [P] Remplacer l'icône de « Déclarer une perte » par `phosphor:warning-octagon` dans `frontend/src/components/domain/StockUnitOutcomeMenu.vue`, et enregistrer cette icône dans `frontend/src/plugins/phosphor-iconset.ts`. La corbeille devient réservée à la suppression (D7)
- [ ] T018 [US1] Afficher la mention « à clôturer » à côté d'un restant nul, en couleur critique, dans `frontend/src/components/domain/StockUnitRow.vue`. **Décorative** : aucune action rattachée, la clôture reste dans le menu à trois points (D9)
- [ ] T019 [US1] Relire `frontend/src/views/StockDetailView.vue` et `frontend/src/components/domain/StockUnitRow.vue` sur mobile, en vérifiant qu'aucune valeur technique anglaise, aucun statut HTTP et aucune date au format technique n'y apparaissent (principe I, FR-012)
- [ ] T020 [US1] Dérouler les scénarios 1 et 2 de [quickstart.md](./quickstart.md) sur la pile locale

**Checkpoint** : US1 est livrable. Les totaux surestiment encore le stock, comme aujourd'hui.

---

## Phase 4: User Story 2 — Un total de stock qui dit ce qui est vendable (P2)

**Goal**: le résumé en tête du détail d'un produit annonce le poids réellement vendable.

**Independent Test**: dix sachets de 500 g plus un jambon de 3 kg dont 2,2 kg sont vendus donnent
5,8 kg et onze unités, et non 8 kg.

- [ ] T021 [US2] Dans `getStockDetail` (`frontend/src/composables/useStock.ts`), remplacer l'accumulation du poids pesé par la somme du poids encore vendable des unités en stock (FR-009). Le décompte d'unités reste strictement inchangé (FR-011)
- [ ] T022 [US2] Vérifier, sur le résumé produit par `frontend/src/composables/useStock.ts`, qu'une unité entamée à restant nul compte toujours pour une unité et contribue zéro au poids, donnant un résumé du type « 1 unité · 0 g » — comportement attendu et non incohérence
- [ ] T023 [US2] Dérouler le scénario 3 de [quickstart.md](./quickstart.md), partie détail du produit

---

## Phase 5: User Story 3 — Le même chiffre sur la liste de stock (P3)

**Goal**: les deux écrans de stock n'annoncent jamais deux poids différents pour un même produit.

**Independent Test**: ouvrir la liste de stock et le détail du même produit, comparer les poids.

- [ ] T024 [US3] Dans le calcul par produit de la liste de stock (`frontend/src/composables/useStock.ts`), sommer le poids encore vendable au lieu du poids pesé, en suivant exactement la règle de T021 (FR-010)
- [ ] T025 [US3] Vérifier que les produits vendus à la pièce n'affichent toujours aucun poids (FR-014), et dérouler le scénario 3 de [quickstart.md](./quickstart.md), partie liste de stock

---

## Phase 6: Polish & documentation

**Purpose**: retirer le calcul devenu inutile et remettre les documents en cohérence. Les tâches de
documentation **ne sont pas facultatives** : la rédaction actuelle de RG-05 interdit ce que la
fonctionnalité affiche, et une session ultérieure la lirait comme un interdit (principe IV).

- [ ] T026 Supprimer `getRemainingWeightKg` de `frontend/src/composables/useStock.ts` et adapter ses appelants (`frontend/src/views/SaleAddView.vue`, `frontend/src/composables/useSales.ts`) pour lire `remainingWeight` sur l'unité déjà chargée. Cela retire une requête par jambon sélectionné et le dernier calcul de restant vivant côté client (D8)
- [ ] T027 [P] Réviser RG-05 dans `docs/PRD.md` §7 : le restant n'est **jamais stocké**, il est **calculé à la demande** et **peut être affiché** ; le garde-fou d'écriture est inchangé. Marquer la révision et sa date, sans effacer l'intention d'origine, et ajouter une ligne à l'historique des révisions
- [ ] T028 [P] Mettre à jour `docs/data-model.md` : le champ dérivé et son mode de calcul en §3.5, le partage de la règle avec le poids d'une sortie en §3.8, et une ligne d'historique de version rappelant qu'**aucune migration** n'accompagne ce changement
- [ ] T029 [P] Mettre à jour `CLAUDE.md` : la règle métier 4 du §8, dont la phrase « le poids restant n'est pas suivi » devient fausse, et le §9 des pièges connus, où le calcul client d'un poids de restant peut désormais être énoncé comme définitivement retiré
- [ ] T030 Dérouler les scénarios 4 et 5 de [quickstart.md](./quickstart.md), en particulier le retrait d'une ligne de vente qui doit faire **remonter** le restant du jambon sans aucun geste supplémentaire
- [ ] T031 Passer la liste de contrôle finale de [quickstart.md](./quickstart.md) : `make test`, `npm run type-check`, `npm run lint`, et vérifier que `git status` ne montre **aucun fichier sous `Migrations/`**

---

## Dependencies

```
Phase 1 (Setup)
   └─> Phase 2 (Foundational) ── bloque les trois histoires
          ├─> Phase 3 (US1, P1) ── livrable seul
          ├─> Phase 4 (US2, P2) ── livrable après US1 ou seule
          └─> Phase 5 (US3, P3) ── doit suivre US2, sinon les deux écrans se contredisent
                 └─> Phase 6 (Polish)
```

- **T003 avant T005** : la méthode doit porter son nouveau nom avant d'être appelée ailleurs.
- **T004 avant T005 et T006** : le champ doit exister avant d'être renseigné.
- **T012 avant toute tâche frontend** : le type doit connaître le champ.
- **T021 avant T024** : la seconde reprend la règle de la première, à l'identique.
- **T026 après T012** : le champ doit être lisible avant de supprimer ce qui le recalculait.

## Parallel Opportunities

- **Phase 2** : T007, T008, T009 et T010 portent sur le même fichier de tests mais sur des cas
  indépendants ; T012 est côté frontend et ne touche rien de ce que les autres modifient.
- **Phase 3** : T017 est isolée dans le menu de sortie, elle ne croise ni la ligne d'unité ni la
  vue de détail.
- **Phase 6** : T027, T028 et T029 sont trois documents distincts.

## Implementation Strategy

**MVP** : Phase 1 + Phase 2 + Phase 3. L'exploitant obtient alors la réponse à « puis-je encore
vendre une tranche », qui est la demande d'origine. Les totaux restent ce qu'ils sont aujourd'hui,
donc rien ne régresse.

**Incrément suivant** : Phases 4 et 5 **ensemble**. Les livrer séparément laisserait pendant un
temps le détail d'un produit et la liste de stock annoncer deux poids différents pour le même
produit, ce qui est pire que la surestimation actuelle, cohérente entre les deux écrans.

**Ordre de déploiement** : le backend peut partir avant le frontend, l'ajout au contrat étant
additif. L'inverse est à éviter, le frontend afficherait des restants absents.
