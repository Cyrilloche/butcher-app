# Implementation Plan: Poids encore vendable d'une unité entamée

**Branch**: `feat/remaining-weight` | **Date**: 2026-09-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-remaining-weight/spec.md`

## Summary

Le poids encore vendable d'une unité pesée devient une information lisible : le serveur le calcule
à chaque lecture et l'expose sur le contrat d'une unité de stock, sans jamais le stocker.

La bonne nouvelle est que **la règle existe déjà côté serveur**. `StockMovementRules` contient
`ComputeOutcomeWeight`, qui retranche du poids pesé la somme des poids vendus, exactement le calcul
demandé. Elle y a été portée quand le solde groupé d'un produit en a eu besoin. Cette
fonctionnalité ne crée donc pas une règle, elle en ouvre la lecture : la méthode est renommée
`ComputeRemainingWeight`, le service des unités l'appelle dans sa projection, et les deux écrans de
stock somment le champ au lieu de sommer les poids d'origine.

Le travail se répartit ainsi : une sous-requête agrégée côté serveur, un champ de plus dans le
contrat, trois affichages côté client, un calcul client supprimé, et la révision de RG-05, dont la
rédaction actuelle interdit noir sur blanc ce que la fonctionnalité affiche.

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript 5 / Vue 3 (frontend)

**Primary Dependencies**: ASP.NET Core Web API, EF Core + Npgsql ; Vue 3, Vuetify, Pinia

**Storage**: PostgreSQL. **Aucune migration** : le poids encore vendable n'est pas persisté
(FR-002), il est calculé à chaque lecture.

**Testing**: xUnit côté backend, sur une base PostgreSQL réelle via Testcontainers
(`Support/PostgresDatabaseFixture.cs`). Aucun test automatisé côté frontend, conformément à
l'existant : validation manuelle par `quickstart.md`.

**Target Platform**: PWA mobile d'abord, servie par Caddy sur la même origine que l'API (ADR-010)

**Project Type**: application web, deux applications séparées par un contrat REST (ADR-003)

**Performance Goals**: aucun objectif chiffré. Contrainte réelle : **ne pas introduire de N+1**.
La lecture des unités d'un produit doit rester une requête, l'agrégat des poids vendus compris.

**Constraints**: le champ ne doit jamais être négatif (FR-003) ni persisté (FR-002). Aucune valeur
technique anglaise à l'écran (principe I).

**Scale/Scope**: activité artisanale annexe. Quelques dizaines d'unités en stock, quelques jambons
entamés à la fois. Trois écrans touchés, aucun nouveau point d'entrée d'API.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Simplicité (utilisateurs non techniques)** — ✅ Le besoin vient de l'exploitant lui-même,
  qui ne pouvait répondre à « puis-je encore vendre une tranche » qu'en allant voir le jambon. Tous
  les libellés ajoutés sont en français (« 800 g restants », « pesé 3,000 kg », « à clôturer »).
  Aucun statut ni enum anglais n'apparaît. La maquette a été validée par l'exploitant avant
  écriture du plan, et elle **réduit** le nombre de lignes qui débordent sur un téléphone.
- **II. Backend garant des règles métier** — ✅ Le calcul vit côté serveur, dans la classe qui
  porte déjà les règles des mouvements, et il est couvert par des tests. Le client ne fait qu'une
  somme d'affichage. Mieux : cette fonctionnalité **supprime** un calcul de restant qui vivait
  encore côté client (`getRemainingWeightKg`), donc elle rapproche le code du principe au lieu de
  s'en éloigner.
- **III. Frontière contractuelle** — ✅ Le seul couplage est un champ ajouté à `StockUnitDto`.
  L'ajout est **additif** : aucun champ renommé ni supprimé, aucun type modifié. Ce n'est donc pas
  une rupture de contrat et le commit ne porte pas de `!`.
- **IV. Traçabilité** — ✅ RG-05 est révisée par cette vague, et la révision redescend dans le PRD,
  `docs/data-model.md` et `CLAUDE.md` (FR-013). RG-04 est citée pour la clôture manuelle. Aucune
  décision d'architecture structurante nouvelle, donc **aucun ADR** : on n'ajoute ni technologie,
  ni couche, ni point d'entrée. Le mode de calcul d'un champ dérivé relève du modèle de données,
  qui le documentera.
- **V. Vagues et spikes** — ✅ Le périmètre reste dans la Vague 1, qu'il complète sans l'élargir :
  aucune alerte, aucun seuil, aucun historique du restant, tous renvoyés en Vague 2. Aucun risque
  technique non validé : la sous-requête agrégée est un usage ordinaire d'EF Core, et le calcul
  métier tourne déjà en production depuis la v0.8.

**Verdict** : les cinq portes passent. Aucune entrée dans le suivi de complexité.

## Project Structure

### Documentation (this feature)

```text
specs/004-remaining-weight/
├── plan.md              # Ce fichier
├── research.md          # Phase 0 : les décisions et leurs alternatives
├── data-model.md        # Phase 1 : le champ dérivé, sans changement de schéma
├── quickstart.md        # Phase 1 : comment valider à la main
├── contracts/
│   └── api.md           # Phase 1 : l'ajout au contrat d'une unité de stock
├── checklists/
│   └── requirements.md  # Qualité de la spécification
└── tasks.md             # Phase 2 (/speckit-tasks), non créé ici
```

### Source Code (repository root)

```text
backend/src/Butcher.Api/
├── Application/
│   ├── Dtos/
│   │   └── StockUnitDto.cs                  # + RemainingWeight (decimal?)
│   └── Services/
│       ├── StockMovementRules.cs            # ComputeOutcomeWeight → ComputeRemainingWeight
│       └── StockUnitService.cs              # projection avec l'agrégat des poids vendus
└── (aucune migration, aucun changement d'entité)

backend/tests/Butcher.Api.Tests/Application/Services/
└── StockUnitServiceTests.cs                 # + cas du poids encore vendable (RG-05)

frontend/src/
├── api/types.ts                             # + remainingWeight sur StockUnitDto
├── composables/useStock.ts                  # totaux = somme des restants ; StockDetailUnit enrichi
├── composables/useSales.ts                  # lit le champ au lieu de recalculer
├── components/domain/
│   ├── StockUnitRow.vue                     # deux lignes, restant, corbeille par unité
│   └── StockUnitOutcomeMenu.vue             # nouvelle icône pour « Déclarer une perte »
└── views/StockDetailView.vue                # date en titre de section au-dessus de la carte

docs/
├── PRD.md                                   # RG-05 révisée
├── data-model.md                            # le champ dérivé, §3.5
└── CLAUDE.md                                # règle métier 4 et pièges connus
```

**Structure Decision** : monorepo à deux applications, structure inchangée. Aucun fichier créé
côté backend, aucun composant nouveau côté frontend : la fonctionnalité se pose entièrement sur
des fichiers existants. C'est le signe qu'elle est au bon endroit.

## Complexity Tracking

> Aucune violation de la constitution à justifier. Section laissée vide volontairement.
