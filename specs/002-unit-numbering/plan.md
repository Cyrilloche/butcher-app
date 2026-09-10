# Implementation Plan: Le numéro d'étiquette porté par l'unité

**Branch**: `feat/unit-numbering` *(à créer)* | **Date**: 2026-09-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-unit-numbering/spec.md`

## Summary

Le numéro `CODE-YYMMDD-N` cesse d'identifier une fabrication pour identifier l'objet physique qui
porte l'étiquette. Concrètement : une colonne `unit_number` non nulle et unique sur `stock_unit`,
la suppression de `production_batch.batch_number`, et le registre de séquences livré hier soir qui
compte désormais des unités au lieu de fournées. L'attribution se fait à la génération des unités,
par blocs, sous verrou de ligne. Le frontend cesse de composer un numéro à deux niveaux et affiche
celui que le serveur lui donne ; une fournée s'annonce par sa date, son prix et son rang dans la
journée.

Les huit décisions qui cadrent ce travail sont dans [research.md](./research.md).

## Technical Context

**Language/Version**: C# / .NET 9 côté backend, TypeScript 5 / Vue 3 côté frontend

**Primary Dependencies**: ASP.NET Core Web API, EF Core + Npgsql, Vuetify 3, Pinia

**Storage**: PostgreSQL, nommage `snake_case`, migrations EF Core

**Testing**: xUnit sur une base PostgreSQL réelle via Testcontainers (`PostgresDatabaseFixture`) ;
`vue-tsc` pour le typage frontend

**Target Platform**: PWA mobile-first servie par Caddy, backend conteneurisé sur VPS (ADR-010)

**Project Type**: Application web, deux applications séparées par un contrat REST (ADR-003)

**Performance Goals**: sans objet à cette échelle. Deux utilisateurs, quelques centaines d'unités
par an. La seule contrainte de concurrence est que deux générations d'unités simultanées ne se
marchent pas dessus.

**Constraints**: aucune étiquette de l'ancien format ne doit rester référencée par l'application ;
la migration doit être jouable sur l'instance déployée sans détruire de données

**Scale/Scope**: 2 entités touchées, 1 migration, 4 services backend, 6 fichiers frontend,
2 documents de référence

## Constitution Check

*GATE: passé avant Phase 0, repassé après Phase 1. Résultat identique aux deux passages.*

- **I. Simplicité (utilisateurs non techniques)** — ✅ C'est la raison d'être de la fonctionnalité.
  Elle retire un niveau de numérotation que l'utilisateur a lu comme un sous-lot lors de la
  validation du 10/09. Aucun libellé anglais n'apparaît : `unit_number` est un nom de colonne, ce
  que l'écran affiche est un numéro d'étiquette sans mot autour.
- **II. Backend garant des règles métier** — ✅ Le numéro est attribué et rendu unique par le
  serveur (FR-005). Le frontend ne compose plus rien, il affiche. La non-réémission (FR-004) et la
  continuité intra-journée (FR-003) arrivent avec leurs tests sur base PostgreSQL réelle.
- **III. Frontière contractuelle** — ✅ Rupture identifiée et documentée dans
  [contracts/api.md](./contracts/api.md) : trois DTO changent de forme, aucune route ne bouge. Le
  commit backend portera un `!` et les deux composants seront publiés ensemble.
- **IV. Traçabilité** — ✅ La fonctionnalité remplace la règle de numérotation de
  `docs/data-model.md` §3.9 et la contrainte C-12, ainsi que la règle 9 de `CLAUDE.md` §8. Ces
  documents sont mis à jour dans le même lot de travail. Pas d'ADR : voir la décision D8, aucune
  décision d'architecture structurante ne change.
- **V. Vagues et spikes** — ✅ Périmètre Vague 1. Aucun risque technique nouveau : le registre de
  séquences, le seul mécanisme délicat, tourne déjà en production depuis hier soir et ne fait que
  changer d'objet compté. Aucun spike préalable requis.

Aucune violation. La section Complexity Tracking est donc vide et retirée.

## Project Structure

### Documentation (this feature)

```text
specs/002-unit-numbering/
├── plan.md              # ce fichier
├── spec.md              # exigences
├── research.md          # les huit décisions techniques
├── data-model.md        # colonnes, registre, migration
├── quickstart.md        # validation manuelle
├── contracts/api.md     # rupture de contrat
├── checklists/
│   └── requirements.md
└── tasks.md             # produit par /speckit-tasks, pas par cette commande
```

### Source Code (repository root)

```text
backend/src/Butcher.Api/
├── Domain/Entities/
│   ├── StockUnit.cs                       # + UnitNumber
│   ├── ProductionBatch.cs                 # − BatchNumber
│   └── UnitNumberSequence.cs              # renommé depuis BatchNumberSequence.cs
├── Infrastructure/Data/
│   ├── Configurations/
│   │   ├── StockUnitConfiguration.cs      # unicité de unit_number
│   │   ├── ProductionBatchConfiguration.cs# − index unique batch_number
│   │   └── UnitNumberSequenceConfiguration.cs
│   └── Migrations/                        # 1 migration : renommage, colonne, rétro-remplissage
├── Application/
│   ├── Dtos/                              # StockUnitDto, StockMovementDto, ProductionBatchDto
│   └── Services/
│       ├── StockUnitService.cs            # attribution des rangs à la génération des unités
│       ├── ProductionBatchService.cs      # − génération du numéro de lot
│       ├── StockMovementRules.cs          # projection du numéro d'unité
│       └── SaleService.cs                 # projection du numéro d'unité
└── tests/Butcher.Api.Tests/Application/Services/

frontend/src/
├── api/types.ts                           # unitNumber sur deux DTO, retrait sur le troisième
├── composables/
│   ├── useStock.ts                        # plus de composition « lot + rang », + rang du jour
│   └── useSales.ts
├── components/domain/
│   ├── BatchDeleteAction.vue              # confirmation sans numéro de lot
│   └── ProductWriteOffDialog.vue          # libellé d'unité par son numéro
└── views/
    ├── StockDetailView.vue                # en-tête de groupe : date, prix, rang du jour
    └── SaleDetailView.vue
```

**Structure Decision**: monorepo à deux applications, conforme à `CLAUDE.md` §5. Aucun répertoire
nouveau. Le seul fichier créé est la migration ; les entrées `UnitNumberSequence*` sont des
renommages de fichiers livrés hier soir.

## Ordre d'exécution recommandé

Le détail par tâche viendra de `/speckit-tasks`. L'ordre qui minimise les états intermédiaires
cassés est le suivant.

1. **Base et domaine** : entité, configurations, migration avec rétro-remplissage. À ce stade la
   suite de tests ne compile plus, c'est attendu.
2. **Attribution des rangs** dans le service des unités, sous verrou, avec ses tests. C'est le cœur
   métier, et le seul endroit où une régression coûte des étiquettes en double.
3. **Retrait du numéro de fabrication** dans le service des lots et ses tests, une fois que les
   unités savent se numéroter seules.
4. **Projections** : les trois DTO, le service des ventes, les règles de mouvement.
5. **Frontend** : types, composables, puis vues et composants.
6. **Documentation** : `docs/data-model.md` et `CLAUDE.md`, dans le même lot (principe IV).

Le point de non-retour est l'étape 3 : tant que `batch_number` existe, un retour en arrière est une
simple révocation de migration.
