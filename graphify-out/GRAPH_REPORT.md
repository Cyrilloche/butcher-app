# Graph Report - butcher-app  (2026-09-04)

## Corpus Check
- 170 files · ~64,534 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1357 nodes · 2484 edges · 192 communities (74 shown, 118 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 251 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `6e642b92`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- API Controllers
- Application DTOs & Services
- Service Tests
- Frontend Dependencies
- Domain Entities & Setup
- PRD Requirements
- Database Migrations
- Product DTOs & Requests
- Frontend Config
- Common Utilities
- Community 10
- Community 11
- Community 12
- Community 13
- Community 14
- Community 15
- Community 16
- Community 17
- Community 18
- Community 19
- Community 20
- Community 21
- Community 22
- Community 23
- Community 24
- Community 25
- Community 26
- Community 27
- Community 28
- Community 29
- Community 30
- Community 31
- Community 32
- Community 33
- Community 34
- Community 35
- Community 36
- Community 37
- Community 38
- Community 39
- Community 40
- Community 41
- Community 42
- Community 43
- Community 44
- Community 45
- Community 46
- Community 47
- Community 48
- Community 49
- Community 50
- Community 51
- Community 52
- Community 53
- Community 54
- Community 55
- Community 56
- Community 57
- Community 58
- Community 59
- Community 60
- Community 61
- Community 63
- Community 64
- Community 65
- Community 66
- Community 67
- Community 68
- Community 69
- Community 70
- Community 71
- Community 72
- Community 73
- Community 74
- Community 75
- Community 76
- Community 77
- Community 78
- Community 79
- Community 80
- Community 81
- Community 82
- Community 83
- Community 84
- Community 85
- Community 86
- Community 87
- Community 88
- Community 89
- Community 90
- Community 91
- Community 92
- Community 93
- Community 94
- Community 95
- Community 96
- Community 97
- Community 100
- Community 102
- Community 103
- Community 104
- Community 105
- Community 106
- Community 107
- Community 108
- Community 109
- Community 110
- Community 111
- Community 112
- Community 113
- Community 114
- Community 115
- Community 116
- Community 117
- Community 118
- Community 119
- Community 120
- Community 121
- Community 122
- Community 123
- Community 124
- Community 125
- Community 126
- Community 127
- Community 128
- Community 129
- Community 130
- Community 131
- Community 132
- Community 133
- Community 134
- Community 135
- Community 136
- Community 137
- Community 138
- Community 139
- Community 140
- Community 141
- Community 142
- Community 143
- Community 144
- Community 145
- Community 146
- Community 147
- Community 148
- Community 149
- Community 150
- Community 151
- Community 152
- Community 153
- Community 154
- Community 155
- Community 156
- Community 157
- Community 158
- Community 159
- Community 160
- Community 161
- Community 162
- Community 163
- Community 164
- Community 165
- Community 166
- Community 167
- Community 168
- RF-19: Vente partielle
- RF-20: Clôture manuelle unité
- RF-21: Sorties perso/perdu
- RF-22: Gestion fiches clients
- RF-23: Historique ventes client
- RF-24: Traçabilité lot-client
- RF-25: Authentification obligatoire
- RF-26: Compte simple partagé
- RF-27: Traçabilité auteur creation

## God Nodes (most connected - your core abstractions)
1. `Butcher.Api.Application.Dtos` - 47 edges
2. `Butcher.Api.Domain.Entities` - 35 edges
3. `apiFetch()` - 35 edges
4. `Butcher.Api.Application.Services` - 34 edges
5. `StockMovementServiceTests` - 29 edges
6. `Butcher.Api.Domain.Enums` - 26 edges
7. `AppDbContext` - 23 edges
8. `Butcher.Api.Infrastructure.Data` - 22 edges
9. `Butcher.Api.Common.Exceptions` - 21 edges
10. `SaleServiceTests` - 21 edges

## Surprising Connections (you probably didn't know these)
- `CreateSaleRequest` --references--> `CreateSaleLineRequest`  [EXTRACTED]
  backend/src/Butcher.Api/Application/Dtos/CreateSaleRequest.cs → backend/src/Butcher.Api/Application/Dtos/CreateSaleLineRequest.cs
- `CreateStockMovementRequest` --references--> `MovementType`  [EXTRACTED]
  backend/src/Butcher.Api/Application/Dtos/CreateStockMovementRequest.cs → backend/src/Butcher.Api/Domain/Enums/MovementType.cs
- `SaleDto` --references--> `StockMovementDto`  [EXTRACTED]
  backend/src/Butcher.Api/Application/Dtos/SaleDto.cs → backend/src/Butcher.Api/Application/Dtos/StockMovementDto.cs
- `AuthService` --implements--> `IAuthService`  [EXTRACTED]
  backend/src/Butcher.Api/Application/Services/AuthService.cs → backend/src/Butcher.Api/Application/Services/IAuthService.cs
- `StockMovementService` --implements--> `IStockMovementService`  [EXTRACTED]
  backend/src/Butcher.Api/Application/Services/StockMovementService.cs → backend/src/Butcher.Api/Application/Services/IStockMovementService.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Chaîne de Traçabilité Production** — docs_data_model_product, docs_data_model_production_batch, docs_data_model_stock_unit, docs_data_model_stock_movement, docs_data_model_customer [EXTRACTED 1.00]

## Communities (192 total, 118 thin omitted)

### Community 0 - "API Controllers"
Cohesion: 0.14
Nodes (14): CreateStockMovementRequest, UpdateStockMovementRequest, IStockMovementService, List, Task, StockMovementsController, ActionResult, HttpDelete (+6 more)

### Community 1 - "Application DTOs & Services"
Cohesion: 0.07
Nodes (19): ExceptionHandlingMiddleware, Task, BadRequestException, ConflictException, NotFoundException, UnauthorizedException, Butcher.Api.Tests.Support, Butcher.Api.Application.Dtos (+11 more)

### Community 2 - "Service Tests"
Cohesion: 0.40
Nodes (4): { data: customers, loading, error }, filtered, groups, query

### Community 3 - "Frontend Dependencies"
Cohesion: 0.06
Nodes (30): @mdi/font, dependencies, @mdi/font, @phosphor-icons/vue, pinia, vue, vue-router, vuetify (+22 more)

### Community 8 - "Frontend Config"
Cohesion: 0.17
Nodes (11): categories, correctness, env, browser, plugins, $schema, eslint, oxc (+3 more)

### Community 9 - "Common Utilities"
Cohesion: 0.08
Nodes (23): EnumSnakeCaseConverter, Product, DateTimeOffset, ICollection, ProductionBatch, DateOnly, DateTimeOffset, Guid (+15 more)

### Community 11 - "Community 11"
Cohesion: 0.07
Nodes (35): CreateSaleLineRequest, CreateSaleRequest, DateTimeOffset, List, SaleDto, DateTimeOffset, List, SetSalePaymentRequest (+27 more)

### Community 18 - "Community 18"
Cohesion: 0.15
Nodes (13): env.d.ts, src/**/__tests__/*, compilerOptions, noUncheckedIndexedAccess, paths, tsBuildInfoFile, exclude, extends (+5 more)

### Community 22 - "Community 22"
Cohesion: 0.10
Nodes (19): net10.0, net10.0, coverlet.collector (6.0.4), EFCore.NamingConventions (10.0.1), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.11), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.11), Microsoft.AspNetCore.OpenApi (10.0.11), Microsoft.EntityFrameworkCore (10.0.11) (+11 more)

### Community 23 - "Community 23"
Cohesion: 0.15
Nodes (12): env.d.ts, src/**/__tests__/*, compilerOptions, lib, tsBuildInfoFile, types, exclude, extends (+4 more)

### Community 28 - "Community 28"
Cohesion: 0.05
Nodes (64): RFC-7807, login(), logout(), refresh(), createCustomer(), getCustomer(), listCustomers(), updateCustomer() (+56 more)

### Community 32 - "Community 32"
Cohesion: 0.29
Nodes (7): eslint, eslint-config-prettier, devDependencies, eslint, eslint-config-prettier, vite-plugin-pwa, vite-plugin-pwa

### Community 35 - "Community 35"
Cohesion: 0.08
Nodes (33): StockMovementDto, DateTimeOffset, StockMovementRules, StockMovementService, IQueryable, List, Task, Customer (+25 more)

### Community 58 - "Community 58"
Cohesion: 0.10
Nodes (21): AddStockUnitsRequest, List, StockUnitDto, IStockUnitService, List, Task, StockUnitService, List (+13 more)

### Community 60 - "Community 60"
Cohesion: 0.09
Nodes (21): CreateProductRequest, ProductDto, UpdateProductRequest, IProductService, List, Task, ProductService, List (+13 more)

### Community 63 - "Community 63"
Cohesion: 0.08
Nodes (26): CreateProductionBatchRequest, DateOnly, ProductionBatchDto, DateOnly, UpdateProductionBatchRequest, DateOnly, IProductionBatchService, List (+18 more)

### Community 64 - "Community 64"
Cohesion: 0.10
Nodes (21): CreateCustomerRequest, CustomerDto, UpdateCustomerRequest, CustomerService, List, Task, ICustomerService, List (+13 more)

### Community 65 - "Community 65"
Cohesion: 0.06
Nodes (30): 10. Hypothèses et contraintes, 11. Risques, 12. Questions ouvertes, 13. Glossaire, 1. Résumé exécutif, 2.1 Situation actuelle, 2.2 Points de douleur identifiés, 2.3 Opportunité (+22 more)

### Community 66 - "Community 66"
Cohesion: 0.08
Nodes (26): addFullSaleToCart(), canSave, CartLine, clearPending(), client, clientResults, confirmSlice(), { data: customers } (+18 more)

### Community 100 - "Community 100"
Cohesion: 0.14
Nodes (8): ADR-0009, config, props, useAuthStore, useCounterStore, { data: products, loading, error, reload }, includeInactive, vue

### Community 102 - "Community 102"
Cohesion: 0.22
Nodes (7): auth, email, errorMessage, password, route, router, submitting

### Community 103 - "Community 103"
Cohesion: 0.07
Nodes (26): 1. Conventions, 2. Vue d'ensemble, 3.1 `app_user`, 3.2 ~~`unit_of_measure`~~ — supprimée (2026-09-04), 3.3 `product`, 3.4 `production_batch`, 3.5 `stock_unit`, 3.6 `customer` (+18 more)

### Community 104 - "Community 104"
Cohesion: 0.22
Nodes (8): currentYear, { data: allSales, loading, error }, filtered, groups, MonthGroup, query, yearRevenue, yearSales

### Community 106 - "Community 106"
Cohesion: 0.09
Nodes (21): 10. Façon de travailler (accords), 11. Questions ouvertes, 12. Système de design (« Kraft »), 1. En une phrase, 2. État d'avancement & feuille de route, 3. Carte de la documentation, 4. Pile technique, 5. Structure du dépôt (cible) (+13 more)

### Community 107 - "Community 107"
Cohesion: 0.10
Nodes (15): batchDateCode, canSave, { data: productCatalog, loading: loadingCatalog, error: catalogError }, isPiece, isWeight, nextBatchPreview, product, router (+7 more)

### Community 108 - "Community 108"
Cohesion: 0.07
Nodes (32): AccessTokenResult, AuthResult, AuthService, Guid, string, Task, ITokenService, TimeSpan (+24 more)

### Community 109 - "Community 109"
Cohesion: 0.20
Nodes (17): formatDateLabel(), formatPriceLabel(), formatWeight(), getStockDashboard(), getStockDetail(), isInStock(), listActiveProducts(), pluralize() (+9 more)

### Community 110 - "Community 110"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 111 - "Community 111"
Cohesion: 0.14
Nodes (12): AuthResponseDto, DateTimeOffset, LoginRequest, IAuthService, Task, AuthController, ActionResult, DateTimeOffset (+4 more)

### Community 112 - "Community 112"
Cohesion: 0.14
Nodes (12): canSave, customerId, { data: customer, loading, error, reload }, { data: sales }, dirty, lastSaleLabel, props, salesSorted (+4 more)

### Community 113 - "Community 113"
Cohesion: 0.15
Nodes (9): canSave, { data: product, loading, error, reload }, { data: stockSummary }, dirty, props, saveError, saving, state (+1 more)

### Community 116 - "Community 116"
Cohesion: 0.18
Nodes (10): Compile and Hot-Reload for Development, Customize configuration, Lint with [ESLint](https://eslint.org/), Project Setup, Recommended Browser Setup, Recommended IDE Setup, Run Unit Tests with [Vitest](https://vitest.dev/), Type-Check, Compile and Minify for Production (+2 more)

### Community 118 - "Community 118"
Cohesion: 0.20
Nodes (8): batchDateCode, batchPreview, canSave, modeHint, router, saveError, saving, state

### Community 120 - "Community 120"
Cohesion: 0.28
Nodes (4): app, phosphor, phosphorIcons, router

### Community 123 - "Community 123"
Cohesion: 0.40
Nodes (3): AppDbContextModelSnapshot, ModelBuilder, ModelSnapshot

### Community 125 - "Community 125"
Cohesion: 0.43
Nodes (5): CustomerGroup, customerInitials(), customerSortKey(), groupCustomersByLetter(), stripDiacritics()

### Community 126 - "Community 126"
Cohesion: 0.29
Nodes (5): canSave, router, saveError, saving, state

### Community 127 - "Community 127"
Cohesion: 0.29
Nodes (5): { data: sale, loading, error, reload }, lineViews, props, saleId, togglingPayment

### Community 133 - "Community 133"
Cohesion: 0.33
Nodes (5): Décisions restant à trancher, Index des décisions, Journal des décisions d'architecture (ADR) — Mini-ERP Charcuterie, Synthèse de la pile technique retenue, À propos de ce document

### Community 134 - "Community 134"
Cohesion: 0.33
Nodes (6): ADR-006 — Bibliothèque de composants UI (Vuetify ou PrimeVue), Alternatives écartées, Alternatives écartées, Conséquences, Contexte, Décision

### Community 135 - "Community 135"
Cohesion: 0.33
Nodes (4): closeError, closingId, {
  data: detail,
  loading,
  error,
  reload,
}, props

### Community 138 - "Community 138"
Cohesion: 0.50
Nodes (3): InitialCreate, MigrationBuilder, Migration

### Community 144 - "Community 144"
Cohesion: 0.40
Nodes (5): ADR-001 — Architecture applicative client-serveur (sans mode hors-ligne), Alternatives écartées, Conséquences, Contexte, Décision

### Community 145 - "Community 145"
Cohesion: 0.40
Nodes (5): ADR-002 — Hébergement auto-géré (self-hosted) plutôt que managé (BaaS), Alternatives écartées, Conséquences, Contexte, Décision

### Community 146 - "Community 146"
Cohesion: 0.40
Nodes (5): ADR-003 — Séparation frontend / backend via un contrat d'API REST, Alternatives écartées, Conséquences, Contexte, Décision

### Community 147 - "Community 147"
Cohesion: 0.40
Nodes (5): ADR-004 — Backend en ASP.NET Core (C#), Alternatives écartées, Conséquences, Contexte, Décision

### Community 148 - "Community 148"
Cohesion: 0.40
Nodes (5): ADR-005 — Frontend en Vue 3 + TypeScript, packagé en PWA, Alternatives écartées, Conséquences, Contexte, Décision

### Community 149 - "Community 149"
Cohesion: 0.40
Nodes (5): ADR-007 — PostgreSQL comme système de gestion de base de données, Alternatives écartées, Conséquences, Contexte, Décision

### Community 150 - "Community 150"
Cohesion: 0.40
Nodes (5): ADR-008 — Entity Framework Core + Npgsql comme couche d'accès aux données, Alternatives écartées, Conséquences, Contexte, Décision

### Community 151 - "Community 151"
Cohesion: 0.40
Nodes (5): ADR-009 — Authentification par jetons JWT, adossée à ASP.NET Core Identity, Alternatives écartées, Conséquences, Contexte, Décision

### Community 152 - "Community 152"
Cohesion: 0.40
Nodes (5): ADR-010 — Déploiement conteneurisé (Docker Compose + reverse proxy HTTPS), Alternatives écartées, Conséquences, Contexte, Décision

### Community 153 - "Community 153"
Cohesion: 0.60
Nodes (4): listSellableLots(), SellableLot, unitDetail(), unitPrice()

### Community 155 - "Community 155"
Cohesion: 0.40
Nodes (4): printWidth, $schema, semi, singleQuote

### Community 156 - "Community 156"
Cohesion: 0.33
Nodes (3): InitialCreate, ModelBuilder, Butcher.Api.Infrastructure.Data.Migrations

### Community 162 - "Community 162"
Cohesion: 0.50
Nodes (3): now, todayDate, todayWeekday

### Community 163 - "Community 163"
Cohesion: 0.50
Nodes (3): config, current, props

## Knowledge Gaps
- **432 isolated node(s):** `net10.0`, `EFCore.NamingConventions (10.0.1)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.11)`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.11)`, `Microsoft.AspNetCore.OpenApi (10.0.11)` (+427 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **118 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Butcher.Api.Infrastructure.Data` connect `Application DTOs & Services` to `Community 160`, `Community 161`, `Community 123`, `Community 156`, `Community 157`, `Community 158`, `Community 159`?**
  _High betweenness centrality (0.042) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `Community 35` to `Application DTOs & Services`, `Common Utilities`, `Community 11`, `Community 108`, `Community 58`, `Community 63`?**
  _High betweenness centrality (0.032) - this node is a cross-community bridge._
- **Why does `Butcher.Api.Application.Dtos` connect `Application DTOs & Services` to `Community 64`, `API Controllers`, `Community 11`, `Community 111`, `Community 58`, `Community 60`, `Community 63`?**
  _High betweenness centrality (0.032) - this node is a cross-community bridge._
- **What connects `net10.0`, `EFCore.NamingConventions (10.0.1)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.11)` to the rest of the system?**
  _432 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `API Controllers` be split into smaller, more focused modules?**
  _Cohesion score 0.1396011396011396 - nodes in this community are weakly interconnected._
- **Should `Application DTOs & Services` be split into smaller, more focused modules?**
  _Cohesion score 0.07330618289522399 - nodes in this community are weakly interconnected._
- **Should `Frontend Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.06451612903225806 - nodes in this community are weakly interconnected._