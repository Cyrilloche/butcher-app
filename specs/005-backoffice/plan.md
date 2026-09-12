# Implementation Plan: Backoffice PC — comptes nominatifs et rôles

**Branch**: `feat/backoffice` | **Date**: 2026-09-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-backoffice/spec.md`

## Summary

Le compte partagé cède la place à des comptes nominatifs porteurs d'un rôle, « Administrateur » ou
« Utilisateur ». C'est le socle du backoffice : sans lui, ni restriction, ni auteur, ni journal
n'ont de sens. La mise en page PC, le journal et les rapports s'y posent ensuite.

L'approche reste au plus près de l'existant :

- **Le rôle est une colonne** sur `app_user`, avec le nom affiché et l'état actif. Pas de tables de
  rôles Identity : deux rôles fixes n'en ont pas besoin (research R-01).
- **Les droits se vérifient en base** à chaque action réservée, par une politique d'autorisation.
  Le jeton porte l'identité, la base porte les droits, ce qui rend immédiate une rétrogradation ou
  une désactivation (R-02, R-03).
- **L'auteur se pose dans `AppDbContext.SaveChanges`**, là où les dates d'audit le sont déjà. Les
  colonnes `created_by` existent depuis la V1 ; elles sont enfin renseignées, sans toucher chaque
  service (R-04).
- **La politique de mot de passe dépend du rôle** : 20 caractères pour tous, 32 pour un
  administrateur, par un validateur Identity qui lit le rôle du compte (R-05).
- **L'interface connaît le compte** par `GET /api/auth/me` (R-06), et masque ce qu'un utilisateur ne
  peut pas faire. Le serveur, lui, refuse.
- **La mise en page PC** bascule à 840 px (point de rupture `md` de Vuetify 4) entre barre en bas d'écran et barre latérale, d'après la
  maquette Claude Design (R-08).

Les suppressions de correction restent ouvertes à tous (clarification Q1) : les seuls gestes métier
réservés sont la désactivation, la réactivation et le solde en perte d'un produit.

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript 5 / Vue 3 (frontend)

**Primary Dependencies**: ASP.NET Core Web API, ASP.NET Core Identity (allégé), EF Core + Npgsql ;
Vue 3, Vuetify 3 (`useDisplay`, `v-navigation-drawer`), Pinia, Phosphor

**Storage**: PostgreSQL. **Lot 1** : une migration sur `app_user` (`display_name`, `role`,
`is_active`, `last_login_at`, `updated_at`) avec reprise des comptes existants en administrateurs.
**Lot US4** : table `audit_entry`. Détail : [data-model.md](./data-model.md).

**Testing**: xUnit sur PostgreSQL réel via Testcontainers (`Support/PostgresDatabaseFixture.cs`).
Tests de service pour les comptes, la politique de mot de passe, l'invariant du dernier
administrateur, l'auteur posé par `SaveChanges`, et le handler d'autorisation. Pas de test frontend
(existant) : validation par [quickstart.md](./quickstart.md), typage et build.

**Target Platform**: PWA mobile d'abord, et écran de travail à partir de 840 px, servie par Caddy
sur la même origine que l'API (ADR-010)

**Project Type**: application web, deux applications séparées par un contrat REST (ADR-003)

**Performance Goals**: aucun objectif chiffré. Contrainte : la vérification des droits en base ne
doit coûter qu'une lecture par clé primaire par requête authentifiée.

**Constraints**: aucun parcours mobile ne régresse (FR-016) ; aucune valeur technique anglaise à
l'écran (FR-032) ; le dernier administrateur actif ne peut jamais disparaître (FR-008) ; un compte
n'est jamais supprimé (FR-009).

**Scale/Scope**: trois comptes. Lot 1 : 1 migration, ~8 routes nouvelles, 3 routes restreintes,
3 DTO enrichis, 1 écran « Comptes », 1 écran « Mon compte », navigation adaptée.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Simplicité (utilisateurs non techniques)** — ✅ Le besoin est exprimé par le porteur de
  projet et ouvre le point d'extension prévu par le PRD (§4.2 « multi-comptes avec journalisation »,
  §9). Les exploitants ne perdent aucun geste de correction (Q1), gardent une phrase de passe de
  20 caractères (Q2) et leur identifiant email (Q1 de la clarification). Libellés en français via la
  table de correspondance (`admin` → « Administrateur »). Les écrans d'administration n'apparaissent
  jamais pour eux.
- **II. Backend garant des règles métier** — ✅ Toutes les restrictions sont appliquées par le
  serveur (politique `AdminOnly`, invariant du dernier administrateur, validateur de mot de passe,
  refus du compte désactivé). L'interface ne fait que masquer. Chaque règle nouvelle arrive avec ses
  tests. Les règles `RG-xx` existantes ne changent pas.
- **III. Frontière contractuelle** — ✅ Tout passe par l'API REST documentée
  ([contracts/api.md](./contracts/api.md)). **Rupture identifiée** : trois routes produit
  répondent désormais `403` à un utilisateur. Le commit correspondant porte `!` et la release
  backend sera versionnée en conséquence. Les ajouts aux DTO (`createdByName`) sont additifs.
- **IV. Traçabilité** — ✅ Cite RF-26 (révisée), RF-27 (enfin appliquée), ADR-009. **Décision
  structurante nouvelle** : comptes nominatifs avec rôles. Elle remet en cause un point accepté
  d'ADR-009 (« compte partagé, sans rôles ») et exige donc un **ADR-011 de remplacement partiel**
  référençant ADR-009, ainsi que la mise à jour du PRD (RF-26), de `docs/data-model.md` (§3.1, §4.2)
  et de `CLAUDE.md` dans le même lot.
- **V. Vagues et spikes** — ✅ Le multi-comptes est un point d'extension documenté, que l'utilisateur
  a explicitement avancé. Aucun risque technique non validé : colonnes EF, politique d'autorisation
  et validateur Identity sont des usages ordinaires, et le socle d'authentification a été éprouvé par
  le spike ADR-009. La carte « Stock bas » de la maquette, qui relève des alertes V2, n'est **pas**
  implémentée : elle affiche « Arrivera en V2 ». Aucun export, coût ou marge (FR-031).

**Verdict** : les cinq portes passent, sous condition de livrer ADR-011 et les révisions
documentaires avec le lot 1. Aucune entrée dans le suivi de complexité.

**Re-check post-design** : ✅ inchangé. Le modèle ([data-model.md](./data-model.md)) ne crée qu'une
table, et seulement au lot US4 ; le contrat isole la seule rupture (`403`).

## Project Structure

### Documentation (this feature)

```text
specs/005-backoffice/
├── plan.md              # Ce fichier
├── research.md          # Phase 0 : décisions R-01 à R-09 et alternatives
├── data-model.md        # Phase 1 : app_user enrichie, auteur, audit_entry
├── quickstart.md        # Phase 1 : validation manuelle par lot
├── contracts/
│   └── api.md           # Phase 1 : routes nouvelles, restreintes, DTO enrichis
├── checklists/
│   └── requirements.md  # Qualité de la spécification
└── tasks.md             # Phase 2 (/speckit-tasks), non créé ici
```

### Source Code (repository root)

```text
backend/src/Butcher.Api/
├── Domain/Entities/
│   ├── AppUser.cs                        # + DisplayName, Role, IsActive, LastLoginAt, UpdatedAt
│   └── AccountRole.cs                    # nouveau : enum Admin | User
├── Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs               # + pose de CreatedById dans SaveChanges
│   │   ├── Configurations/AppUserConfiguration.cs   # nouveau : colonnes, enum snake_case
│   │   └── Migrations/…_AddAccountRoles.cs          # nouveau : colonnes + reprise en admin
│   └── Identity/
│       ├── IdentityPolicy.cs             # socle 20 caractères
│       └── AdminPasswordValidator.cs     # nouveau : 32 caractères pour un admin
├── Common/
│   ├── Authorization/
│   │   ├── AuthorizationPolicies.cs      # nouveau : AdminOnly
│   │   ├── ActiveAccountHandler.cs       # nouveau : compte actif et rôle relus en base
│   │   └── ICurrentAccount.cs, HttpCurrentAccount.cs  # nouveau : compte de la requête
│   ├── Exceptions/ForbiddenException.cs  # nouveau : 403
│   └── ExceptionHandlingMiddleware.cs    # + 403
├── Application/
│   ├── Dtos/                             # AccountDto, Create/UpdateAccountRequest, MeDto,
│   │                                     # ChangePasswordRequest, ResetPasswordRequest ;
│   │                                     # + CreatedByName sur Sale/ProductionBatch/StockMovement
│   └── Services/
│       ├── AccountService.cs, IAccountService.cs    # nouveau : comptes, dernier admin
│       ├── AuthService.cs                # compte désactivé, last_login_at, change-password
│       └── Sale/ProductionBatch/StockMovement services  # projection de CreatedByName
├── Controllers/
│   ├── AccountsController.cs             # nouveau, AdminOnly
│   ├── AuthController.cs                 # + me, change-password
│   └── ProductsController.cs             # deactivate / reactivate / write-off → AdminOnly
└── Program.cs                            # enregistrements, politique, validateur

backend/tests/Butcher.Api.Tests/
├── Application/Services/AccountServiceTests.cs      # nouveau
├── Application/Services/AuthServiceTests.cs         # + compte désactivé, change-password
├── Infrastructure/Data/CreatedByStampingTests.cs     # nouveau
├── Infrastructure/Identity/IdentityPolicyTests.cs   # politique par rôle
└── Common/Authorization/ActiveAccountHandlerTests.cs # nouveau

frontend/src/
├── api/accounts.ts, api/auth.ts, api/types.ts       # comptes, me, change-password, createdByName
├── stores/auth.ts                         # + account, isAdmin, chargement de /me
├── layouts/AppLayout.vue                  # barre latérale ≥ md, barre du bas sinon (lot US3)
├── views/AccountsView.vue                 # nouveau, administrateur
├── views/MyAccountView.vue                # nouveau : changer son mot de passe
├── views/OverviewView.vue                 # nouveau, lot US3, d'après la maquette
├── components/domain/AuthorLabel.vue      # nouveau : « Saisie par … »
├── views/ProductDetailView.vue            # gestes réservés masqués pour un utilisateur
├── views/SaleDetailView.vue, StockDetailView.vue    # affichage de l'auteur
└── router/index.ts                        # routes comptes / mon compte, garde administrateur

docs/
├── ADR.md                                 # ADR-011, statut d'ADR-009 mis à jour
├── PRD.md                                 # RF-26 révisée
├── data-model.md                          # §3.1 app_user, §4.2 correspondances
└── CLAUDE.md                              # authentification, pièges, avancement
```

**Structure Decision** : monorepo à deux applications, structure inchangée. Côté backend, un dossier
`Common/Authorization` regroupe ce qui décide des droits, pour qu'une restriction ne se cherche pas
dans chaque contrôleur. Côté frontend, les écrans d'administration sont des vues ordinaires protégées
par une garde de route ; aucun second point d'entrée.

### Découpage en lots livrables

| Lot | Récits | Contenu | Livrable seul |
|---|---|---|---|
| 1 | US1, US2 | Comptes, rôles, auteur, gestes réservés, ADR-011 | ✅ |
| 2 | US3 | Barre latérale, vue d'ensemble, tableaux et filtres de ventes | ✅ |
| 3 | US4 | Journal `audit_entry` | après lot 1 |
| 4 | US5 | Rapports | après lot 1 |

## Complexity Tracking

> Aucune violation de la constitution à justifier. Section laissée vide volontairement.
