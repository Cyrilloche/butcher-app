# Graph Report - .  (2026-09-04)

## Corpus Check
- 105 files · ~67,743 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1433 nodes · 2845 edges · 124 communities (83 shown, 41 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 279 edges (avg confidence: 0.8)
- Token cost: 180,284 input · 0 output

## Community Hubs (Navigation)
- Règles métier du stock
- Contrat API des ventes
- Authentification — jetons et DTO
- Client HTTP frontend
- Service produits
- Service unités de stock
- Service lots de production
- Entité vente (modèle)
- Modèle de données — produit et montants
- Contrat API des mouvements
- Dépendances frontend
- Décisions d'architecture (ADR)
- Écran Ajout Vente
- Service clients
- Middleware et sérialisation API
- Composants d'affichage de statut
- Projets et paquets .NET
- Couche services et EF Core
- Implémentation service clients
- Écran Ajout Stock
- Interfaces de services
- Composables stock et formatage
- État des lieux — écarts
- Configuration de lancement API
- Architecture client-serveur et extensions V2
- Chaîne de traçabilité lot → client
- Contrat REST et découplage
- CLAUDE.md — document racine
- Entité unit_of_measure supprimée
- Écran Détail Client
- Configuration TypeScript (app)
- Conventions snake_case et ruptures
- Écran Détail Produit
- Configuration TypeScript (node)
- Changelog et releases
- Vente en une fois vs partielle
- Configuration du linting
- README frontend
- Entité Sale (persistance)
- Entité StockMovement (persistance)
- ProductAddView
- BadRequestException
- Product
- phosphor-iconset
- AppBrandHeader
- CustomersView
- LoginView
- SalesView
- Customer
- RefreshToken
- CLAUDE
- PRD
- MovementType
- CLAUDE
- CLAUDE
- database-dev
- package
- useCustomers
- CustomerAddView
- SaleDetailView
- 20260903101726_InitialCreate.Designer
- 20260903101726_InitialCreate
- 20260903124709_AddUnitOfMeasureUniqueInd
- 20260903200317_AddIdentityAndRefreshToke
- 20260904130802_AddSaleEntity
- 20260904140057_RemoveUnitOfMeasure
- 20260904145501_AddAllowPartialSaleToProd
- AppDbContextModelSnapshot
- useSales
- .prettierrc
- 20260903124709_AddUnitOfMeasureUniqueInd
- 20260903200317_AddIdentityAndRefreshToke
- 20260904130802_AddSaleEntity.Designer
- 20260904140057_RemoveUnitOfMeasure.Desig
- 20260904145501_AddAllowPartialSaleToProd
- DatabaseCollection
- auth
- env.d
- tsconfig
- package
- package
- StockView
- package
- package
- package
- package
- package
- package
- package
- package
- package
- package
- package
- package
- package
- package
- package
- package
- package
- package
- package
- package
- DbUpdateException
- int
- UserManager
- pwa-192x192.png
- pwa-512x512.png

## God Nodes (most connected - your core abstractions)
1. `PRD — Mini-ERP Charcuterie Artisanale (v0.5)` - 93 edges
2. `Butcher.Api.Application.Dtos` - 47 edges
3. `Modèle de données — Mini-ERP Charcuterie (v0.7)` - 42 edges
4. `Butcher.Api.Domain.Entities` - 35 edges
5. `apiFetch()` - 35 edges
6. `Butcher.Api.Application.Services` - 34 edges
7. `StockMovementServiceTests` - 29 edges
8. `AppDbContext` - 23 edges
9. `vue` - 22 edges
10. `Butcher.Api.Infrastructure.Data` - 22 edges

## Surprising Connections (you probably didn't know these)
- `Stack Docker Compose de production` --semantically_similar_to--> `Stack Docker Compose de développement/build`  [INFERRED] [semantically similar]
  docker-compose.prod.yml → docker-compose.yml
- `Spike 3 — Fondations données EF Core + Npgsql` --conceptually_related_to--> `Service postgres dev (butcher-postgres-dev, port 5432)`  [INFERRED]
  CLAUDE.md → development/database-dev.yml
- `Service postgres dev (butcher-postgres-dev, port 5432)` --semantically_similar_to--> `Service postgres (postgres:17-alpine)`  [INFERRED] [semantically similar]
  development/database-dev.yml → docker-compose.yml
- `CHANGELOG — versions et releases` --conceptually_related_to--> `§2 État d'avancement & feuille de route`  [INFERRED]
  CHANGELOG.md → CLAUDE.md
- `Spike 2 — Socle de déploiement (ADR-010)` --implements--> `Stack Docker Compose de production`  [INFERRED]
  CLAUDE.md → docker-compose.prod.yml

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Chaîne de traçabilité lot ↔ client** — docs_data_model_product, docs_data_model_production_batch, docs_data_model_stock_unit, docs_data_model_stock_movement, docs_data_model_sale, docs_data_model_customer, docs_prd_rf_24 [EXTRACTED 1.00]
- **Cycle de vie des jetons d'authentification** — docs_adr_adr_009, docs_adr_access_token_jwt, docs_adr_refresh_token, docs_adr_rotation_detection_rejeu, docs_adr_fail_closed_policy, docs_adr_seed_compte_admin, docs_prd_rf_25 [EXTRACTED 1.00]
- **Mécanisme de vente à la tranche (unité entamée)** — docs_prd_rf_19, docs_prd_rf_20, docs_prd_rg_04, docs_prd_rg_05, docs_data_model_allow_partial_sale, docs_data_model_c_04, docs_data_model_is_full_sale, docs_data_model_statut_denormalise [EXTRACTED 1.00]
- **Chaîne de traçabilité production → vente** — claude_entite_product, claude_entite_production_batch, claude_entite_stock_unit, claude_entite_stock_movement, claude_entite_sale, claude_entite_customer [EXTRACTED 1.00]
- **Topologie réseau de production (tunnel → proxy → apps → base)** — docker_compose_prod_cloudflared, docker_compose_prod_caddy, docker_compose_prod_backend, docker_compose_prod_frontend, docker_compose_prod_postgres [EXTRACTED 1.00]
- **Discipline snake_case de bout en bout (schéma, enums, affichage FR)** — claude_convention_nommage, claude_piege_enums_pascalcase, claude_table_correspondance_fr, changelog_correction_enums_snake_case [INFERRED 0.85]

## Communities (124 total, 41 thin omitted)

### Community 0 - "Règles métier du stock"
Cohesion: 0.06
Nodes (33): StockMovementRules, StockUnit, StockMovementService, IQueryable, List, Task, AppDbContext, AppUser (+25 more)

### Community 1 - "Contrat API des ventes"
Cohesion: 0.07
Nodes (35): CreateSaleRequest, DateTimeOffset, List, SaleDto, DateTimeOffset, List, SetSalePaymentRequest, UpdateSaleRequest (+27 more)

### Community 2 - "Authentification — jetons et DTO"
Cohesion: 0.06
Nodes (35): AuthResponseDto, DateTimeOffset, LoginRequest, AccessTokenResult, AuthResult, AuthService, Guid, string (+27 more)

### Community 3 - "Client HTTP frontend"
Cohesion: 0.05
Nodes (64): RFC-7807, login(), logout(), refresh(), createCustomer(), getCustomer(), listCustomers(), updateCustomer() (+56 more)

### Community 4 - "Service produits"
Cohesion: 0.09
Nodes (21): CreateProductRequest, ProductDto, UpdateProductRequest, IProductService, List, Task, ProductService, List (+13 more)

### Community 5 - "Service unités de stock"
Cohesion: 0.09
Nodes (26): AddStockUnitsRequest, List, StockUnitDto, IStockUnitService, List, Task, StockUnitService, List (+18 more)

### Community 6 - "Service lots de production"
Cohesion: 0.10
Nodes (22): CreateProductionBatchRequest, DateOnly, ProductionBatchDto, DateOnly, UpdateProductionBatchRequest, DateOnly, IProductionBatchService, List (+14 more)

### Community 7 - "Entité vente (modèle)"
Cohesion: 0.10
Nodes (38): QM-04 — Regroupement des ventes (résolu : entité sale), Entité sale (sale_number, customer_id obligatoire, date, paid, total calculé), PRD — Mini-ERP Charcuterie Artisanale (v0.5), Contexte et problématique — activité annexe gérée sur papier, H-03 — Recettes non stockées en V1, H-05 — Application exposée sur Internet → authentification dès la V1, Lot de production (fabrication datée à prix propre), Mode de vente (poids_variable / piece_simple) (+30 more)

### Community 8 - "Modèle de données — produit et montants"
Cohesion: 0.12
Nodes (33): Modèle de données — Mini-ERP Charcuterie (v0.7), product.allow_partial_sale — autorisation explicite de la vente à la tranche, Note d'architecture — le montant est stocké, pas recalculé, Entité app_user (compte d'accès, uuid, Identity), C-01 — amount et customer_id uniquement si type = sale, C-02 — weight et sold_weight renseignés pour by_weight, null pour by_piece, C-05 — batch_number et product.code uniques, C-06 — Statuts de sortie exclusifs à l'échelle de l'unité (+25 more)

### Community 9 - "Contrat API des mouvements"
Cohesion: 0.13
Nodes (16): CreateStockMovementRequest, StockMovementDto, DateTimeOffset, UpdateStockMovementRequest, IStockMovementService, List, Task, StockMovementsController (+8 more)

### Community 10 - "Dépendances frontend"
Cohesion: 0.06
Nodes (30): @mdi/font, dependencies, @mdi/font, @phosphor-icons/vue, pinia, vue, vue-router, vuetify (+22 more)

### Community 11 - "Décisions d'architecture (ADR)"
Cohesion: 0.11
Nodes (30): Journal des décisions d'architecture (ADR), Access token JWT en mémoire (15 min, HMAC-SHA256), ADR-002 — Hébergement auto-géré (self-hosted) plutôt que BaaS, ADR-003 — Séparation frontend/backend via un contrat d'API REST, ADR-004 — Backend en ASP.NET Core (C#), ADR-005 — Frontend en Vue 3 + TypeScript, packagé en PWA, ADR-006 — Bibliothèque de composants UI : Vuetify, ADR-007 — PostgreSQL comme SGBD (+22 more)

### Community 12 - "Écran Ajout Vente"
Cohesion: 0.08
Nodes (26): addFullSaleToCart(), canSave, CartLine, clearPending(), client, clientResults, confirmSlice(), { data: customers } (+18 more)

### Community 13 - "Service clients"
Cohesion: 0.14
Nodes (15): CreateCustomerRequest, CustomerDto, UpdateCustomerRequest, ICustomerService, List, Task, CustomersController, ActionResult (+7 more)

### Community 14 - "Middleware et sérialisation API"
Cohesion: 0.10
Nodes (13): EnumSnakeCaseConverter, ExceptionHandlingMiddleware, Task, AppUserConfiguration, EntityTypeBuilder, ProductionBatchConfiguration, EntityTypeBuilder, StockUnitConfiguration (+5 more)

### Community 15 - "Composants d'affichage de statut"
Cohesion: 0.08
Nodes (15): config, props, config, current, props, route, showNavigation, useCounterStore (+7 more)

### Community 16 - "Projets et paquets .NET"
Cohesion: 0.10
Nodes (19): net10.0, net10.0, coverlet.collector (6.0.4), EFCore.NamingConventions (10.0.1), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.11), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.11), Microsoft.AspNetCore.OpenApi (10.0.11), Microsoft.EntityFrameworkCore (10.0.11) (+11 more)

### Community 17 - "Couche services et EF Core"
Cohesion: 0.22
Nodes (5): Butcher.Api.Tests.Support, Butcher.Api.Common.Exceptions, Butcher.Api.Domain.Entities, Butcher.Api.Infrastructure.Data, Butcher.Api.Tests.Application.Services

### Community 18 - "Implémentation service clients"
Cohesion: 0.23
Nodes (7): CustomerService, List, Task, CustomerServiceTests, Fact, Task, ICustomerService

### Community 19 - "Écran Ajout Stock"
Cohesion: 0.10
Nodes (15): batchDateCode, canSave, { data: productCatalog, loading: loadingCatalog, error: catalogError }, isPiece, isWeight, nextBatchPreview, product, router (+7 more)

### Community 20 - "Interfaces de services"
Cohesion: 0.22
Nodes (4): CreateSaleLineRequest, Butcher.Api.Application.Dtos, Butcher.Api.Application.Services, Butcher.Api.Controllers

### Community 21 - "Composables stock et formatage"
Cohesion: 0.20
Nodes (17): formatDateLabel(), formatPriceLabel(), formatWeight(), getStockDashboard(), getStockDetail(), isInStock(), listActiveProducts(), pluralize() (+9 more)

### Community 22 - "État des lieux — écarts"
Cohesion: 0.17
Nodes (17): État des lieux — écart entre le prévu et le réalisé (v1.0), Ce qui était prévu et qui est bien là (8 entités, ~35 routes, 84 tests), E-02 — RF-08/RF-09 : matière première et DLC non saisissables dans « Ajout Stock », E-03 — RG-10 : aucun écran d'édition de lot malgré PUT /api/production-batches/{id}, E-04 — RG-14 : vente non modifiable/supprimable côté interface, E-05 — RG-11 : mouvement modifiable/supprimable non exposé, E-06 — RF-31 : filtres de ventes partiellement exposés (texte + regroupement par mois), E-08 — Aucun test frontend face à 84 tests backend (+9 more)

### Community 23 - "Configuration de lancement API"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 24 - "Architecture client-serveur et extensions V2"
Cohesion: 0.14
Nodes (16): ADR-001 — Architecture client-serveur (sans mode hors-ligne), Points d'extension V2+ (matières premières, recettes versionnées, rôles, alertes), E-01 — RF-21 sorties perso/perte : API disponible, aucun écran ne la déclenche (bloquant V1), E-07 — RF-27 : created_by existe mais n'est jamais renseigné, Approche agile par vagues (V1 noyau, enrichissement incrémental), H-01 — Activité annexe à faible volume, OBJ-4 — Distinguer sorties vente / perso / perte pour préparer la rentabilité, Périmètre V1 (noyau) : produits, lots, stock à l'unité, ventes/sorties, clients, auth, PWA (+8 more)

### Community 25 - "Chaîne de traçabilité lot → client"
Cohesion: 0.23
Nodes (14): Correction — sold_weight ne peut dépasser le poids de l'unité (RG-05), Chaîne centrale product → batch → unit → movement → sale → customer, Entité customer (traçabilité, non supprimable), Entité product (code, sale_mode), Entité production_batch (prix propre au lot), Entité sale (V-YYMMDD-N, client obligatoire), Entité stock_movement (sale / personal / loss), Entité stock_unit (objet physique individuel) (+6 more)

### Community 26 - "Contrat REST et découplage"
Cohesion: 0.19
Nodes (14): Scalar API reference UI avec auth JWT bearer, Backend ASP.NET Core Web API + REST/OpenAPI, Contrainte — pas de mode hors-ligne en V1 (ADR-001), Piège — coupler frontend et backend hors du contrat REST, §4 Pile technique, Frontend Vue 3 + TS, PWA « Saloir » (Vuetify, ADR-006), Contrainte — séparation stricte frontend/backend (ADR-003), §5 Structure du dépôt (monorepo cible) (+6 more)

### Community 27 - "CLAUDE.md — document racine"
Cohesion: 0.18
Nodes (14): §3 Carte de la documentation (PRD, ADR, data-model), CLAUDE.md — document racine de contexte, §1 En une phrase — mini-ERP charcuterie artisanale, §2 État d'avancement & feuille de route, §10 Façon de travailler (accords), Méthode de dé-risquage par spikes, RG — Numéro de lot CODE-YYMMDD-N recopiable à la main, R-01 — friction de la saisie de pesée unité par unité (+6 more)

### Community 28 - "Entité unit_of_measure supprimée"
Cohesion: 0.20
Nodes (14): C-07 — Unicité unit_of_measure (sans objet, entité supprimée), C-08 — product.sale_unit_id référence une unité active (sans objet), Entité unit_of_measure — supprimée (2026-09-04), Hors périmètre : unités personnalisables, facturation légale, B2B, app native, OBJ-1 — Autonomie complète des exploitants sur la configuration et l'usage, R-04 — Adoption par des utilisateurs non techniques, Retrait des unités de mesure du périmètre V1 (2026-09-04), RF-03 — Unité d'affichage de vente (abandonné 2026-09-04) (+6 more)

### Community 29 - "Écran Détail Client"
Cohesion: 0.14
Nodes (12): canSave, customerId, { data: customer, loading, error, reload }, { data: sales }, dirty, lastSaleLabel, props, salesSorted (+4 more)

### Community 30 - "Configuration TypeScript (app)"
Cohesion: 0.15
Nodes (13): env.d.ts, src/**/__tests__/*, compilerOptions, noUncheckedIndexedAccess, paths, tsBuildInfoFile, exclude, extends (+5 more)

### Community 31 - "Conventions snake_case et ruptures"
Cohesion: 0.18
Nodes (13): Correction — enums sérialisés en snake_case, Release frontend-v0.1.0 (04/09/2026), Rupture — suppression de l'entité unit_of_measure, Piège — dupliquer le client sur stock_movement, Piège — compteur parallèle pour le stock by_piece, Piège — sérialiser les enums en PascalCase (C-11), Piège — recalculer amount en ignorant la valeur saisie, Piège — réintroduire unit_of_measure sur le produit (+5 more)

### Community 32 - "Écran Détail Produit"
Cohesion: 0.15
Nodes (9): canSave, { data: product, loading, error, reload }, { data: stockSummary }, dirty, props, saveError, saving, state (+1 more)

### Community 33 - "Configuration TypeScript (node)"
Cohesion: 0.15
Nodes (12): env.d.ts, src/**/__tests__/*, compilerOptions, lib, tsBuildInfoFile, types, exclude, extends (+4 more)

### Community 34 - "Changelog et releases"
Cohesion: 0.26
Nodes (12): Release backend-v0.2.0 (04/09/2026), CHANGELOG — versions et releases, Commande hors-ligne create-user, Génération par Conventional Commits (make changelog), Section « Non publié », Service backend (image Docker Hub versionnée), Service caddy (prod, aucun port publié), Service cloudflared (prod, seule porte d'entrée) (+4 more)

### Community 35 - "Vente en une fois vs partielle"
Cohesion: 0.23
Nodes (12): C-03 — Vente en une fois : un unique mouvement, unité passée à sold, C-04 — Vente partielle : plusieurs mouvements, unité opened, somme des sold_weight ≤ weight, Indicateur isFullSale à la création d'un mouvement de vente, QM-03 — Comptage des tranches de jambon (résolu : poids seul), Note d'architecture — le statut de l'unité physique est une dénormalisation assumée, Persona — l'exploitant vendeur, RF-18 — Vente d'une unité en une fois, RF-19 — Vente partielle d'une unité (jambon à la tranche) (+4 more)

### Community 36 - "Configuration du linting"
Cohesion: 0.17
Nodes (11): categories, correctness, env, browser, plugins, $schema, eslint, oxc (+3 more)

### Community 37 - "README frontend"
Cohesion: 0.18
Nodes (10): Compile and Hot-Reload for Development, Customize configuration, Lint with [ESLint](https://eslint.org/), Project Setup, Recommended Browser Setup, Recommended IDE Setup, Run Unit Tests with [Vitest](https://vitest.dev/), Type-Check, Compile and Minify for Production (+2 more)

### Community 38 - "Entité Sale (persistance)"
Cohesion: 0.22
Nodes (7): Sale, AppUser, DateTimeOffset, Guid, ICollection, SaleConfiguration, EntityTypeBuilder

### Community 39 - "Entité StockMovement (persistance)"
Cohesion: 0.22
Nodes (7): StockMovement, AppUser, DateTimeOffset, Guid, StockUnit, StockMovementConfiguration, EntityTypeBuilder

### Community 40 - "ProductAddView"
Cohesion: 0.20
Nodes (8): batchDateCode, batchPreview, canSave, modeHint, router, saveError, saving, state

### Community 41 - "BadRequestException"
Cohesion: 0.22
Nodes (5): BadRequestException, ConflictException, NotFoundException, UnauthorizedException, Exception

### Community 42 - "Product"
Cohesion: 0.25
Nodes (6): Product, DateTimeOffset, ICollection, ProductionBatch, ProductConfiguration, EntityTypeBuilder

### Community 43 - "phosphor-iconset"
Cohesion: 0.28
Nodes (4): app, phosphor, phosphorIcons, router

### Community 44 - "AppBrandHeader"
Cohesion: 0.22
Nodes (7): auth, confirmOpen, loggingOut, now, router, todayDate, todayWeekday

### Community 45 - "CustomersView"
Cohesion: 0.25
Nodes (8): ALPHABET, { data: customers, loading, error }, filtered, groups, jumpToLetter(), letterAnchor(), presentLetters, query

### Community 46 - "LoginView"
Cohesion: 0.22
Nodes (7): auth, email, errorMessage, password, route, router, submitting

### Community 47 - "SalesView"
Cohesion: 0.22
Nodes (8): currentYear, { data: allSales, loading, error }, filtered, groups, MonthGroup, query, yearRevenue, yearSales

### Community 48 - "Customer"
Cohesion: 0.29
Nodes (5): Customer, DateTimeOffset, ICollection, CustomerConfiguration, EntityTypeBuilder

### Community 49 - "RefreshToken"
Cohesion: 0.29
Nodes (5): RefreshToken, DateTimeOffset, Guid, RefreshTokenConfiguration, EntityTypeBuilder

### Community 50 - "CLAUDE"
Cohesion: 0.25
Nodes (8): Convention de nommage (snake_case PG, PascalCase C#), Convention de types (decimal argent/poids, timestamptz, clés), §6 Conventions, Entité app_user (compte Identity), §7 Modèle de domaine (résumé), Piège — stocker de l'argent en flottant, Piège — nommer une table user, Convention — table app_user, jamais user

### Community 51 - "PRD"
Cohesion: 0.32
Nodes (8): Entité customer (last_name, first_name, phone), E-09 — Bascule liste/grille des clients non implémentée (écart volontaire), E-10 — DELETE /api/customers/{id} sans usage frontend (volontaire, traçabilité), OBJ-3 — Historiser ventes et clients (qui a acheté quoi), RF-22 — Créer et gérer des fiches clients, RF-23 — Historique des ventes rattaché au client, RF-24 — Savoir quel lot a été vendu à quel client, Traçabilité vente → mouvement → unité physique → lot → client

### Community 52 - "MovementType"
Cohesion: 0.29
Nodes (3): MovementType, SaleMode, Butcher.Api.Domain.Enums

### Community 53 - "CLAUDE"
Cohesion: 0.33
Nodes (7): Authentification Identity allégé + JWT + refresh rotatif, Convention d'audit (created_by, created_at/updated_at), Question ouverte — created_by jamais renseigné (RF-27), Question ouverte — SameSite du cookie de refresh token, §11 Questions ouvertes, Spike 1 — Authentification JWT (fait, ADR-009), Configuration backend par variables d'environnement (ConnectionStrings, Jwt, Cors, Seed)

### Community 54 - "CLAUDE"
Cohesion: 0.33
Nodes (7): Convention de langue — code anglais, doc et UI français, Iconset Phosphor (iconset Vuetify custom), Mapping statuts métier ↔ couleurs sémantiques, Palette de couleurs Kraft (terracotta, bois, kraft), §12 Système de design « Kraft », Table de correspondance code ↔ affichage FR, Typographie Zilla Slab + Work Sans

### Community 55 - "database-dev"
Cohesion: 0.43
Nodes (7): Healthcheck pg_isready comme condition de démarrage, Service pgadmin dev (butcher-pgadmin-dev, port 5050), Service postgres dev (butcher-postgres-dev, port 5432), Stack base de données de développement, Volumes dev (butcher_postgres_dev_data, butcher_pgadmin_dev_data), Service postgres (postgres:17-alpine), Volume butcher_postgres_data

### Community 56 - "package"
Cohesion: 0.29
Nodes (7): eslint, eslint-config-prettier, devDependencies, eslint, eslint-config-prettier, vite-plugin-pwa, vite-plugin-pwa

### Community 57 - "useCustomers"
Cohesion: 0.43
Nodes (5): CustomerGroup, customerInitials(), customerSortKey(), groupCustomersByLetter(), stripDiacritics()

### Community 58 - "CustomerAddView"
Cohesion: 0.29
Nodes (5): canSave, router, saveError, saving, state

### Community 59 - "SaleDetailView"
Cohesion: 0.29
Nodes (5): { data: sale, loading, error, reload }, lineViews, props, saleId, togglingPayment

### Community 60 - "20260903101726_InitialCreate.Designer"
Cohesion: 0.33
Nodes (3): InitialCreate, ModelBuilder, Butcher.Api.Infrastructure.Data.Migrations

### Community 61 - "20260903101726_InitialCreate"
Cohesion: 0.50
Nodes (3): InitialCreate, MigrationBuilder, Migration

### Community 67 - "AppDbContextModelSnapshot"
Cohesion: 0.40
Nodes (3): AppDbContextModelSnapshot, ModelBuilder, ModelSnapshot

### Community 68 - "useSales"
Cohesion: 0.60
Nodes (4): listSellableLots(), SellableLot, unitDetail(), unitPrice()

### Community 69 - ".prettierrc"
Cohesion: 0.40
Nodes (4): printWidth, $schema, semi, singleQuote

### Community 75 - "DatabaseCollection"
Cohesion: 0.50
Nodes (3): DatabaseCollection, string, ICollectionFixture

## Knowledge Gaps
- **265 isolated node(s):** `MovementType`, `SaleMode`, `$schema`, `commandName`, `dotnetRunMessages` (+260 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **41 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Butcher.Api.Application.Dtos` connect `Interfaces de services` to `Contrat API des ventes`, `Authentification — jetons et DTO`, `Service produits`, `Service unités de stock`, `Service lots de production`, `Contrat API des mouvements`, `Service clients`, `Couche services et EF Core`, `MovementType`?**
  _High betweenness centrality (0.056) - this node is a cross-community bridge._
- **Why does `Butcher.Api.Infrastructure.Data` connect `Couche services et EF Core` to `AppDbContextModelSnapshot`, `20260903124709_AddUnitOfMeasureUniqueInd`, `20260903200317_AddIdentityAndRefreshToke`, `20260904130802_AddSaleEntity.Designer`, `20260904140057_RemoveUnitOfMeasure.Desig`, `20260904145501_AddAllowPartialSaleToProd`, `20260903101726_InitialCreate.Designer`?**
  _High betweenness centrality (0.053) - this node is a cross-community bridge._
- **Why does `Butcher.Api.Application.Services` connect `Interfaces de services` to `Couche services et EF Core`, `Authentification — jetons et DTO`?**
  _High betweenness centrality (0.037) - this node is a cross-community bridge._
- **What connects `MovementType`, `SaleMode`, `$schema` to the rest of the system?**
  _268 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Règles métier du stock` be split into smaller, more focused modules?**
  _Cohesion score 0.061501868841318384 - nodes in this community are weakly interconnected._
- **Should `Contrat API des ventes` be split into smaller, more focused modules?**
  _Cohesion score 0.06728395061728396 - nodes in this community are weakly interconnected._
- **Should `Authentification — jetons et DTO` be split into smaller, more focused modules?**
  _Cohesion score 0.05738615327656423 - nodes in this community are weakly interconnected._