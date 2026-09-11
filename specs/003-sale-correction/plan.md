# Implementation Plan: Corriger et supprimer une vente

**Branch**: `feat/sale-correction` | **Date**: 2026-09-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-sale-correction/spec.md`

## Summary

L'écran de détail d'une vente cesse d'être en lecture seule. Une action « Corriger » y ouvre
l'en-tête à l'édition (client, date, paiement, notes) ; un appui sur une ligne ouvre une boîte de
dialogue qui corrige son montant et son poids vendu, ou la retire ; une action de bas de page
supprime la vente entière après confirmation. Le serveur sait déjà tout faire et applique déjà
toutes les règles : le travail est entièrement frontend, et consiste à rendre atteignable ce qui
est exposé.

Trois gestes techniques : deux fonctions manquantes dans le client HTTP des mouvements de stock,
l'extraction du sélecteur de client de l'écran de saisie en composant partagé, et la refonte de
`SaleDetailView.vue`. Les sept décisions qui cadrent ce travail sont dans
[research.md](./research.md).

Cette fonctionnalité clôt l'écart E-04 de l'état des lieux, et accessoirement E-05, la correction
d'une ligne étant le geste qui manquait à `stock_movement`.

## Technical Context

**Language/Version**: TypeScript 5 / Vue 3 côté frontend. Aucun code C# touché.

**Primary Dependencies**: Vuetify 4, Vue Router 5, Pinia — aucune dépendance nouvelle

**Storage**: PostgreSQL, inchangé. Aucune migration.

**Testing**: validation manuelle par [quickstart.md](./quickstart.md). Les règles métier
concernées sont couvertes par sept tests xUnit existants, recensés dans la décision D0.

**Target Platform**: PWA mobile-first servie par Caddy (ADR-010) ; l'écran est utilisé debout, sur
téléphone.

**Project Type**: Application web, deux applications séparées par un contrat REST (ADR-003)

**Performance Goals**: sans objet. Une vente porte quelques lignes, un rechargement complet après
chaque correction coûte un appel.

**Constraints**: aucune règle métier ne doit être répliquée côté client ; aucun montant ne doit
être recalculé (RG-05) ; aucun libellé anglais à l'écran.

**Scale/Scope**: 1 vue refondue, 2 composants créés, 1 composant extrait, 1 client HTTP complété.
Aucun fichier backend.

## Constitution Check

*GATE: passé avant Phase 0, repassé après Phase 1. Résultat identique aux deux passages.*

- **I. Simplicité (utilisateurs non techniques)** — ✅ Le besoin est porté par RG-14 et par l'écart
  E-04, deuxième priorité de l'état des lieux. C'est un filet de rattrapage pour deux personnes
  sans accès à la base. L'édition est un mode explicite et non des champs perpétuellement ouverts
  (D1), et les gestes destructifs annoncent leurs conséquences en français (D6). Aucune valeur
  technique anglaise n'atteint l'écran.
- **II. Backend garant des règles métier** — ✅ Aucune règle nouvelle, donc aucun test backend
  nouveau exigé. RG-11, RG-14, RG-07 et RG-05 sont appliquées et testées côté serveur ; la
  vérification est faite et tracée en D0. L'interface n'anticipe aucun refus, elle affiche celui
  du serveur (D5) — y compris celui du retrait de la dernière ligne (FR-006).
- **III. Frontière contractuelle** — ✅ Aucune route nouvelle, aucun DTO modifié, aucune rupture :
  voir [contracts/api.md](./contracts/api.md). Le commit sera un `feat(frontend)` sans `!`, et
  seul le frontend sera publié.
- **IV. Traçabilité** — ✅ RG-14, RG-11, RG-07, RG-05, RF-17 cités dans la spécification et le
  code. Aucune décision d'architecture structurante : pas d'ADR. `docs/etat-des-lieux.md` sera mis
  à jour dans le même lot pour clore E-04 et E-05, et `CLAUDE.md` §2 pour la ligne de feuille de
  route correspondante.
- **V. Vagues et spikes** — ✅ Périmètre Vague 1, dernier manque important de l'interface avec
  E-02. Aucun risque technique : le serveur tourne, les appels sont du même genre que ceux déjà
  faits par l'écran. Aucun spike préalable. L'ajout d'une ligne à une vente enregistrée est
  explicitement reporté plutôt qu'anticipé dans le code.

Aucune violation. La section Complexity Tracking est donc vide et retirée.

## Project Structure

### Documentation (this feature)

```text
specs/003-sale-correction/
├── plan.md              # ce fichier
├── spec.md              # exigences
├── research.md          # les sept décisions techniques, et la vérification D0
├── data-model.md        # entités touchées, transitions de statut, invariant
├── quickstart.md        # validation manuelle, cinq scénarios
├── contracts/api.md     # routes consommées, refus et messages
├── checklists/
│   └── requirements.md
└── tasks.md             # produit par /speckit-tasks, pas par cette commande
```

### Source Code (repository root)

```text
frontend/src/
├── api/
│   └── stockMovements.ts              # + updateStockMovement, + deleteStockMovement
├── components/domain/
│   ├── CustomerPicker.vue             # créé — extrait de SaleAddView (D4)
│   ├── SaleLineEditDialog.vue         # créé — corriger ou retirer une ligne (D2)
│   └── SaleDeleteAction.vue           # créé — confirmation de suppression (D6)
└── views/
    ├── SaleDetailView.vue             # refondu — mode édition, lignes cliquables, suppression
    └── SaleAddView.vue                # allégé — utilise CustomerPicker

docs/
└── etat-des-lieux.md                  # E-04 et E-05 clos

CLAUDE.md                              # §2 feuille de route
```

**Structure Decision**: monorepo à deux applications, conforme à `CLAUDE.md` §5. Aucun répertoire
nouveau, aucun fichier backend. Les trois composants créés vont dans `components/domain/`, qui
héberge déjà les composants métier du même genre (`BatchDeleteAction.vue`,
`ProductWriteOffDialog.vue`, `StockUnitOutcomeMenu.vue`), dont ils reprennent la forme.

## Ordre d'exécution recommandé

Le détail par tâche viendra de `/speckit-tasks`. L'ordre qui livre de la valeur au plus tôt suit
les priorités de la spécification.

1. **Socle** : les deux fonctions manquantes du client HTTP des mouvements. Quelques lignes, rien
   ne dépend encore d'elles.
2. **Extraction du sélecteur de client** (D4), et bascule de `SaleAddView` dessus. À faire avant
   l'en-tête, qui en dépend. Point de vigilance : l'écran de saisie doit se comporter exactement
   comme avant.
3. **US1 — correction de l'en-tête** : mode édition, brouillon, enregistrement, abandon, refus.
   Livrable indépendant : la correction du client, l'erreur la plus grave, est rattrapable dès
   cette étape même si les lignes restent intouchables.
4. **US3 — suppression de la vente** : composant de confirmation et retour à la liste. Vient avant
   les lignes parce qu'il est plus simple et qu'il donne son issue au refus de FR-006.
5. **US2 — correction et retrait d'une ligne** : boîte de dialogue, deux appels, rechargement.
6. **Documentation** : `docs/etat-des-lieux.md` et `CLAUDE.md`, dans le même lot (principe IV).

Aucun point de non-retour : chaque étape est additive et réversible, aucune donnée n'est migrée.
