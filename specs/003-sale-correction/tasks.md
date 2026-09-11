---
description: "Liste de tâches — Corriger et supprimer une vente"
---

# Tasks: Corriger et supprimer une vente

**Input**: documents de conception dans `/specs/003-sale-correction/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/api.md](./contracts/api.md),
[quickstart.md](./quickstart.md)

**Tests** : aucune tâche de test automatisé. La fonctionnalité n'ajoute ni ne modifie aucune règle
de gestion — la décision D0 de `research.md` recense les sept tests xUnit existants qui couvrent
RG-05, RG-07, RG-11 et RG-14 côté serveur. Le principe II n'exige donc pas de test nouveau. La
validation passe par les cinq scénarios de `quickstart.md`.

**Organization** : tâches groupées par histoire utilisateur, chacune livrable et vérifiable seule.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- **[Story]** : US1, US2, US3 — l'histoire de `spec.md` que la tâche sert
- Chemins de fichiers exacts dans chaque description

## Path Conventions

Monorepo à deux applications (`CLAUDE.md` §5). **Aucun fichier backend n'est touché.** Tous les
chemins de code sont sous `frontend/src/`.

---

## Phase 1: Setup

**Purpose**: partir d'un environnement qui tourne. Rien à installer, aucune dépendance nouvelle.

- [ ] T001 Démarrer la pile de développement et se connecter à l'application : `make db-up`, `make run`, puis `npm run dev` dans `frontend/`, en suivant la section « Prérequis » de `specs/003-sale-correction/quickstart.md`
- [ ] T002 Préparer le jeu d'essai décrit dans `specs/003-sale-correction/quickstart.md` (deux clients, deux produits au poids dont un à la tranche, trois ventes), depuis l'application elle-même

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: les briques dont les trois histoires dépendent. Aucune ne change de comportement
visible à elle seule.

**⚠️ CRITICAL**: aucune histoire ne peut commencer avant la fin de cette phase.

- [X] T003 [P] Ajouter `updateStockMovement(id, payload)` et `deleteStockMovement(id)` dans `frontend/src/api/stockMovements.ts`, sur le modèle des fonctions voisines, avec les types `UpdateStockMovementRequest` et `StockMovementDto` déjà présents dans `frontend/src/api/types.ts` (contrats : `specs/003-sale-correction/contracts/api.md` §1)
- [X] T004 [P] Créer `frontend/src/composables/useApiError.ts` exposant `apiErrorMessage(err, fallback)` qui renvoie `err.message` pour une `ApiError` et le message de repli sinon — le motif déjà répété dans `ProductDetailView.vue` et `BatchDeleteAction.vue`, factorisé parce que les quatre gestes de cette fonctionnalité l'utilisent (décision D5)
- [X] T005 Créer `frontend/src/components/domain/CustomerPicker.vue` en extrayant la recherche de client de `frontend/src/views/SaleAddView.vue` (balisage, filtrage sur `customerFullName`, limite à cinq résultats, styles) — `v-model` sur l'identifiant du client, aucun changement de comportement (décision D4, FR-003)
- [X] T006 Remplacer la recherche de client inline de `frontend/src/views/SaleAddView.vue` par `CustomerPicker`, et retirer le code et les styles devenus morts (dépend de T005)
- [ ] T007 Vérifier que l'écran de saisie d'une vente se comporte exactement comme avant : recherche, sélection, bouton « Changer », enregistrement d'une vente complète

**Checkpoint**: le client HTTP est complet, le sélecteur de client est partagé, l'écran de saisie
est intact. Les trois histoires peuvent démarrer.

---

## Phase 3: User Story 1 — Corriger l'en-tête d'une vente (Priority: P1) 🎯 MVP

**Goal**: rendre corrigeables le client, la date, le statut de paiement et les notes d'une vente
enregistrée, sans toucher à ses lignes (FR-001, FR-002, FR-003, RG-14, RG-07).

**Independent Test**: enregistrer une vente au nom d'un client, la rouvrir, la réaffecter à un
autre client, vérifier que les deux historiques d'achats reflètent le changement. Scénario 1 de
`quickstart.md`.

### Implementation for User Story 1

- [X] T008 [US1] Ajouter à `frontend/src/views/SaleDetailView.vue` l'état de correction de l'en-tête : indicateur de mode édition, brouillon réactif des quatre champs (`customerId`, `date`, `paid`, `notes`) initialisé depuis la vente chargée, réinitialisé à chaque ouverture du mode (décision D1, FR-002)
- [X] T009 [US1] Ajouter dans `frontend/src/views/SaleDetailView.vue` l'action « Corriger » qui ouvre le mode édition, et la carte d'édition de l'en-tête : `CustomerPicker` pour le client, sélecteur de date, bascule « Payée / À payer », champ de notes — libellés français, style « Kraft » des cartes existantes (dépend de T005, T008)
- [X] T010 [US1] Implémenter dans `frontend/src/views/SaleDetailView.vue` l'enregistrement de l'en-tête : appel unique à `updateSale` avec les quatre champs, puis `reload()` de la vente, puis fermeture du mode édition (FR-002, FR-011 ; contrat `PUT /api/sales/{id}`)
- [X] T011 [US1] Implémenter dans `frontend/src/views/SaleDetailView.vue` l'abandon : « Annuler » ferme le mode édition en jetant le brouillon, sans aucun appel serveur ni modification affichée (FR-002)
- [X] T012 [US1] Afficher dans `frontend/src/views/SaleDetailView.vue` le refus serveur de la correction d'en-tête via `apiErrorMessage`, sans fermer le mode édition ni vider le brouillon — cas du client inexistant (`409`) et de la vente disparue (`404`) (FR-010, décision D5 ; dépend de T004)
- [ ] T013 [US1] Vérifier que le numéro de vente affiché est inchangé après correction, et que le bouton rapide « Marquer comme payée » de l'écran de lecture coexiste sans contradiction avec la bascule de l'en-tête (FR-012)

**Checkpoint**: une vente affectée au mauvais client se rattrape entièrement. Scénario 1 de
`quickstart.md` passe. Livrable seul.

---

## Phase 4: User Story 3 — Supprimer une vente entière (Priority: P2)

**Goal**: offrir le recours ultime — supprimer une vente, ses lignes, et rendre au stock les
unités qui ne portent plus aucune sortie (FR-007, FR-008, RG-14).

**Independent Test**: créer une vente, la supprimer, vérifier qu'elle a disparu de la liste et que
ses unités sont de nouveau en stock. Scénario 4 de `quickstart.md`.

**Note d'ordonnancement**: traitée avant US2 bien que de priorité identique — elle est plus simple,
et c'est elle que désigne le refus du retrait de la dernière ligne (FR-006), donc l'issue doit
exister avant que le refus soit atteignable.

### Implementation for User Story 3

- [X] T014 [P] [US3] Créer `frontend/src/components/domain/SaleDeleteAction.vue` sur le modèle de `BatchDeleteAction.vue` : bouton de suppression, boîte de confirmation nommant le numéro de vente et le nombre de lignes supprimées, annonce générale du retour en stock des unités sans autre sortie, événements `done` et `failed` (FR-008, décision D6 ; la réserve de D6 interdit d'annoncer un décompte d'unités faussement précis)
- [X] T015 [US3] Câbler `SaleDeleteAction` en bas de `frontend/src/views/SaleDetailView.vue`, avec appel à `deleteSale` et redirection vers `/sales` en cas de succès (FR-007, décision D7 ; dépend de T014)
- [X] T016 [US3] Afficher le refus serveur d'une suppression via `apiErrorMessage` sans quitter l'écran, et vérifier que « Annuler » laisse la vente intacte (FR-008, FR-010 ; dépend de T004)

**Checkpoint**: US1 et US3 fonctionnent indépendamment. Scénarios 1 et 4 de `quickstart.md`
passent.

---

## Phase 5: User Story 2 — Retirer ou ajuster une ligne (Priority: P2)

**Goal**: corriger le montant encaissé et le poids vendu d'une ligne, ou retirer la ligne, le
total suivant les montants réellement enregistrés (FR-004, FR-005, FR-006, FR-009, RG-11, RG-05).

**Independent Test**: créer une vente de trois unités, en retirer une, vérifier que le total baisse
et que l'unité retirée est de nouveau vendable. Scénarios 2 et 3 de `quickstart.md`.

### Implementation for User Story 2

- [X] T017 [P] [US2] Créer `frontend/src/components/domain/SaleLineEditDialog.vue` : boîte de dialogue titrée par le numéro d'étiquette de l'unité, champ montant encaissé, champ poids vendu affiché seulement si la ligne en porte un, action « Retirer cette ligne », événements `saved`, `removed`, sans aucun recalcul de montant à partir du poids (décision D2 et D3, FR-005, RG-05)
- [X] T018 [US2] Implémenter dans `SaleLineEditDialog.vue` l'enregistrement d'une correction : appel à `updateStockMovement` renvoyant **les trois champs** `soldWeight`, `amount`, `notes`, y compris ceux non modifiés, `soldWeight` restant absent pour un produit à la pièce (contrat `specs/003-sale-correction/contracts/api.md` §1 ; dépend de T003, T017)
- [X] T019 [US2] Implémenter dans `SaleLineEditDialog.vue` le retrait d'une ligne : confirmation explicite nommant le numéro d'étiquette de l'unité et son retour en stock, puis appel à `deleteStockMovement` (FR-004, FR-008, décision D6 ; dépend de T003, T017)
- [X] T020 [US2] Afficher dans `SaleLineEditDialog.vue` les refus serveur via `apiErrorMessage` en conservant la saisie : poids au-delà du poids pesé (`409`), montant ou poids invalide (`400`), et **dernière ligne de la vente** (`409`) dont le message renvoie déjà vers la suppression de la vente — aucune règle n'est répliquée côté client, aucun bouton n'est désactivé par anticipation (FR-006, FR-010, décision D5)
- [X] T021 [US2] Rendre les lignes de `frontend/src/views/SaleDetailView.vue` actionnables (zone tactile d'au moins 48 px, libellé accessible nommant l'unité) et y câbler `SaleLineEditDialog`, avec `reload()` de la vente après une correction comme après un retrait (dépend de T017)
- [ ] T022 [US2] Vérifier après rechargement que le total et le nombre de lots affichés viennent de `SaleDto` et non d'un calcul local, sur une vente mêlant une unité entière et une tranche (FR-009, FR-011, RG-05)

**Checkpoint**: les trois histoires fonctionnent indépendamment. Scénarios 1 à 4 de
`quickstart.md` passent.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T023 Dérouler le scénario 5 de `specs/003-sale-correction/quickstart.md` (unité portant une autre sortie, unité entamée puis clôturée, vente supprimée depuis un autre onglet) et vérifier que l'écran affiche l'état rechargé sans contredire le serveur (`data-model.md` §2)
- [ ] T024 Relire l'écran sur mobile : aucune valeur technique anglaise (`sold`, `available`, `sale`), aucun statut HTTP, aucune date au format ISO visible (principe I, FR-010)
- [X] T025 `cd frontend && npm run type-check && npm run lint`
- [X] T026 [P] Clore les écarts E-04 et E-05 dans `docs/etat-des-lieux.md`, et retirer E-04 de la liste des priorités §5
- [X] T027 [P] Mettre à jour `CLAUDE.md` §2 : ligne de feuille de route de la correction d'une vente, et état d'avancement ne laissant plus que la saisie DLC / matière première (RF-08/RF-09) en Vague 1
- [ ] T028 Dérouler la liste de contrôle finale de `specs/003-sale-correction/quickstart.md` avant de considérer la fonctionnalité livrée

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance.
- **Foundational (Phase 2)** : dépend du Setup. **Bloque les trois histoires.**
- **US1 (Phase 3)** : dépend de la Phase 2. Aucune dépendance sur les autres histoires.
- **US3 (Phase 4)** : dépend de la Phase 2. Indépendante d'US1.
- **US2 (Phase 5)** : dépend de la Phase 2. Indépendante d'US1 et d'US3 sur le plan technique ;
  son message de refus FR-006 renvoie vers US3, d'où l'ordre retenu.
- **Polish (Phase 6)** : dépend des trois histoires.

### Dépendances internes notables

- T006 dépend de T005 (le composant doit exister avant d'être utilisé).
- T009 dépend de T005 et T008.
- T012, T016 et T020 dépendent de T004.
- T015 dépend de T014 ; T018, T019 et T021 dépendent de T017 ; T018 et T019 dépendent de T003.
- Toutes les tâches qui touchent `SaleDetailView.vue` (T008 à T013, T015, T016, T021, T022) sont
  **séquentielles entre elles** : même fichier, jamais de `[P]`.

### Parallel Opportunities

- T003 et T004 : fichiers distincts, aucune dépendance — parallélisables.
- T014 et T017 : deux composants nouveaux, indépendants l'un de l'autre et de la vue tant qu'ils
  ne sont pas câblés.
- T026 et T027 : deux documents distincts.
- Avec deux personnes, une fois la Phase 2 finie : l'une prend US1 sur `SaleDetailView.vue`,
  l'autre crée `SaleDeleteAction.vue` et `SaleLineEditDialog.vue` — le câblage dans la vue se fait
  ensuite, en série.

---

## Parallel Example: Phase 2

```bash
Task: "Ajouter updateStockMovement et deleteStockMovement dans frontend/src/api/stockMovements.ts"
Task: "Créer frontend/src/composables/useApiError.ts"
```

## Parallel Example: composants des histoires 2 et 3

```bash
Task: "Créer frontend/src/components/domain/SaleDeleteAction.vue"
Task: "Créer frontend/src/components/domain/SaleLineEditDialog.vue"
```

---

## Implementation Strategy

### MVP d'abord (US1 seule)

1. Phase 1 : Setup.
2. Phase 2 : Foundational — bloque tout le reste.
3. Phase 3 : US1.
4. **Arrêt et validation** : scénario 1 de `quickstart.md`.
5. La correction du mauvais client, l'erreur la plus grave et la plus probable, est livrée. Les
   lignes restent intouchables, et c'est un état cohérent : la spécification l'a prévu comme
   livrable indépendant.

### Livraison incrémentale

1. Setup + Foundational → socle prêt, aucun changement visible.
2. + US1 → validation → livrable (MVP).
3. + US3 → validation → toute vente est supprimable, donc aucune saisie n'est définitive.
4. + US2 → validation → la correction fine est possible, l'écart E-04 est clos.
5. Phase 6 → documentation et contrôles, dans le même lot que le code (principe IV).

---

## Notes

- `[P]` = fichiers différents, aucune dépendance en attente.
- Aucune tâche backend : si une règle manquait en cours de route, elle serait ajoutée **et testée**
  côté serveur (principe II), et cette liste serait amendée plutôt que contournée côté client.
- Commits en Conventional Commits, portée `frontend`, avec les identifiants concernés
  (`feat(frontend): corriger l'en-tête d'une vente (RG-14)`). Pas de `!` : aucune rupture de
  contrat (principe III).
- L'arbre de travail est partagé entre plusieurs sessions : committer avec des chemins explicites,
  jamais `git add -A`.
- Hors périmètre, à ne pas anticiper dans le code : ajouter une ligne à une vente enregistrée,
  historique des corrections, corbeille.
