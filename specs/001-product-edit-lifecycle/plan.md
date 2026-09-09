# Implementation Plan: Modification et fin de vie d'un produit

**Branch**: `feat/update-product` | **Date**: 2026-09-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-product-edit-lifecycle/spec.md`

## Summary

Ouvrir la modification complète d'un produit tant qu'aucun lot de production ne lui est rattaché,
figer son code et son mode de vente dès le premier lot, et fournir les deux portes de sortie qui
rendent ce gel vivable : la suppression d'un lot intact, et le solde en perte des unités restantes
avant désactivation.

L'approche technique tient en quatre gestes. Le service produit gagne un état dérivé « utilisé »,
calculé par l'existence d'un lot, qui pilote côté serveur ce que la mise à jour accepte. Le service
lot gagne une suppression explicite, unité par unité, refusée dès qu'une sortie de stock existe. La
numérotation de lot passe d'un comptage des lots existants à un registre de séquences persistant,
sans quoi supprimer un lot ferait réémettre son numéro sur une seconde série d'étiquettes
manuscrites. Enfin la désactivation gagne un garde-fou de stock et une action de solde groupée.

Côté interface, tout se loge dans des écrans existants : la fiche produit pour l'édition, le gel et
le solde, et le détail stock pour la suppression d'un lot, qui y est déjà affiché par lot.

## Technical Context

**Language/Version**: C# / .NET 10 côté backend, TypeScript / Vue 3 côté frontend

**Primary Dependencies**: ASP.NET Core Web API, EF Core + Npgsql, Vuetify 3, Pinia

**Storage**: PostgreSQL, accès EF Core, nommage `snake_case`, migrations dans
`backend/src/Butcher.Api/Infrastructure/Data/Migrations`

**Testing**: xUnit côté backend, tests de service sur une base PostgreSQL réelle
(`PostgresDatabaseFixture`, collection partagée) ; Vitest disponible côté frontend

**Target Platform**: PWA mobile-first servie par Caddy sur la même origine que `/api/*`, déployée en
Docker Compose sur VPS

**Project Type**: application web, deux applications séparées par un contrat REST (ADR-003)

**Performance Goals**: aucune contrainte spécifique, catalogue de quelques dizaines de produits et
deux utilisateurs simultanés au plus

**Constraints**: pas de mode hors-ligne (ADR-001) ; interface intégralement en français ; aucune
détection de conflit d'édition concurrente (FR-029) ; les numéros de lot déjà émis ne doivent jamais
être réattribués (FR-013)

**Scale/Scope**: 2 utilisateurs, ~30 produits, 4 écrans touchés, 6 endpoints touchés ou créés

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

*Gates derived from `.specify/memory/constitution.md` v1.0.0.*

- **I. Simplicité (utilisateurs non techniques)** — ✅ Le besoin vient directement des utilisateurs
  tests. Aucun écran nouveau n'est créé : l'édition et le solde se logent dans la fiche produit, la
  suppression de lot dans le détail stock qui affiche déjà les lots. Tous les libellés et messages
  d'erreur sont en français (FR-006, FR-030), y compris ceux produits par le serveur, le middleware
  d'exceptions existant renvoyant déjà des `ProblemDetails` en français.
- **II. Backend garant des règles métier** — ✅ Les quatre garde-fous sont côté serveur : refus de
  modifier code et mode de vente sur un produit utilisé, refus de supprimer un lot ayant une sortie,
  refus de désactiver avec du stock restant, non-réémission d'un numéro de lot. Chacun reçoit son
  test dans `ProductServiceTests` ou `ProductionBatchServiceTests`. L'interface ne fait que refléter
  ces règles.
- **III. Frontière contractuelle** — ✅ Tout passe par le contrat REST. Une rupture est identifiée :
  `PUT /api/products/{id}` gagne deux champs requis, `code` et `saleMode`. Elle sera portée par un
  commit `feat(backend)!` et un tag `backend-v*` en conséquence, le frontend étant mis à jour dans
  la même vague.
- **IV. Traçabilité** — ✅ La fonctionnalité prolonge RF-01 et RF-02 (référentiel produits) et
  RF-08 (lot de production) ; elle ne remet en cause aucun ADR accepté. Le registre de séquences de
  numéro de lot est une évolution du modèle de données, pas une décision d'architecture : elle est
  documentée dans `docs/data-model.md` (passage en v0.8) plutôt que dans un ADR. La règle de format
  du numéro de lot y est déjà décrite et doit y être amendée.
- **V. Vagues et spikes** — ✅ Périmètre Vague 1, dans le prolongement du référentiel produits déjà
  livré. Aucun risque technique nouveau : pas de brique inconnue, pas de spike requis. La
  journalisation de la suppression est explicitement renvoyée en Vague 2 avec le reste de RF-27.

**Verdict avant Phase 0**: aucune violation.

### Re-évaluation après Phase 1

La conception n'introduit aucune violation nouvelle, et deux points méritent d'être notés.

- **Principe II renforcé** : le calcul du poids soldé reste côté serveur (D-05) plutôt que d'être
  transmis par le client, et la règle est extraite pour être partagée avec le chemin unité par unité
  au lieu d'être réécrite. Deux implémentations de la même règle finiraient par diverger.
- **Principe I préservé** : la conception ne crée aucune route ni aucun écran nouveau. La suppression
  de lot se loge sur un en-tête déjà affiché (D-07), le solde dans une boîte de dialogue de la fiche
  produit.
- **Principe IV, décision assumée** : le registre de séquences est traité comme une évolution du
  modèle de données, documentée dans `docs/data-model.md`, et non comme un ADR. Si cette lecture est
  jugée trop étroite, un ADR court sur la stratégie de numérotation serait le bon geste, à trancher
  avant l'implémentation.

**Verdict après Phase 1**: aucune violation. Complexity Tracking sans objet.

## Project Structure

### Documentation (this feature)

```text
specs/001-product-edit-lifecycle/
├── plan.md              # Ce fichier
├── research.md          # Phase 0 — décisions techniques et alternatives écartées
├── data-model.md        # Phase 1 — évolutions du schéma et états dérivés
├── quickstart.md        # Phase 1 — scénarios de validation de bout en bout
├── contracts/
│   └── api.md           # Phase 1 — contrat REST touché
├── checklists/
│   └── requirements.md  # Qualité de la spécification
└── tasks.md             # Phase 2 — produit par /speckit-tasks, pas par /speckit-plan
```

### Source Code (repository root)

```text
backend/
├── src/Butcher.Api/
│   ├── Domain/Entities/
│   │   └── BatchNumberSequence.cs          # nouveau — registre de séquences
│   ├── Application/
│   │   ├── Dtos/
│   │   │   ├── ProductDto.cs               # + isUsed, + remainingStockUnitCount
│   │   │   ├── UpdateProductRequest.cs     # + code, + saleMode (rupture de contrat)
│   │   │   └── WriteOffProductStockRequest.cs  # nouveau
│   │   └── Services/
│   │       ├── ProductService.cs           # gel conditionnel, garde-fou de désactivation, solde
│   │       └── ProductionBatchService.cs   # suppression de lot, numérotation par registre
│   ├── Controllers/
│   │   ├── ProductsController.cs           # + POST {id}/write-off
│   │   ├── ProductionBatchesController.cs  # + DELETE {id}
│   │   └── StockUnitsController.cs         # + filtre productId
│   └── Infrastructure/Data/
│       ├── Configurations/
│       │   └── BatchNumberSequenceConfiguration.cs  # nouveau
│       └── Migrations/                     # nouvelle migration
└── tests/Butcher.Api.Tests/Application/Services/
    ├── ProductServiceTests.cs              # gel, désactivation, solde
    └── ProductionBatchServiceTests.cs      # suppression, non-réémission du numéro

frontend/
├── src/
│   ├── api/
│   │   ├── products.ts                     # + writeOffProductStock
│   │   ├── productionBatches.ts            # + deleteProductionBatch
│   │   └── stockUnits.ts                   # + filtre productId
│   ├── components/domain/
│   │   ├── ProductWriteOffDialog.vue       # nouveau — sélection des unités à solder
│   │   └── BatchDeleteAction.vue           # nouveau — suppression d'un lot avec confirmation
│   └── views/
│       ├── ProductDetailView.vue           # champs gelés, explication, solde avant désactivation
│       └── StockDetailView.vue             # action de suppression sur l'en-tête de lot

docs/
└── data-model.md                           # v0.8 — registre de séquences, mutabilité produit
```

**Structure Decision**: monorepo à deux applications, structure inchangée. La fonctionnalité
s'insère dans les couches existantes du backend (entité, DTO, service, contrôleur, configuration EF)
et dans les vues existantes du frontend, avec deux composants de domaine nouveaux. Aucun découpage
ni module nouveau n'est introduit.

## Complexity Tracking

Aucune violation du Constitution Check. Section sans objet.
