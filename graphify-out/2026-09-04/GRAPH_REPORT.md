# Graph Report - .  (2026-09-04)

## Corpus Check
- Corpus is ~42,763 words - fits in a single context window. You may not need a graph.

## Summary
- 870 nodes · 971 edges · 245 communities (34 shown, 211 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 1 edges (avg confidence: 0.95)
- Token cost: 60,000 input · 16,933 output

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
- Community 169
- Community 170
- Community 171
- Community 172
- Community 173
- Community 174
- Community 175
- Community 176
- Community 177
- Community 178
- Community 179
- Community 180
- Community 181
- Community 182
- Community 183
- Community 184
- Community 185
- Community 186
- Community 187
- Community 188
- Community 189
- Community 190
- Community 191
- Community 192
- Community 193
- Community 194
- Community 195
- Community 196
- Community 197
- Community 198
- Community 199
- Community 200
- Community 201
- Community 202
- Community 203
- Community 204
- Community 205
- Community 206
- Community 207
- Community 208
- Community 209
- Community 210
- Community 211
- Community 212
- Community 213
- Community 214
- Community 215
- Community 216
- Community 217
- Community 218
- Community 219
- Community 220
- Community 221
- Community 222
- Community 223
- Community 224
- Community 225
- Community 226
- Community 227
- Community 228
- Community 229
- Community 230
- Community 231
- Community 232
- Community 233
- Community 234
- Community 235
- Community 236
- Community 237
- Community 238
- Community 239
- Community 240
- Community 241
- Community 242
- Community 243
- Community 244

## God Nodes (most connected - your core abstractions)
1. `Butcher.Api.Application.Dtos` - 44 edges
2. `Butcher.Api.Domain.Entities` - 34 edges
3. `Butcher.Api.Application.Services` - 33 edges
4. `Vague 1 (MVP)` - 28 edges
5. `Butcher.Api.Domain.Enums` - 23 edges
6. `StockMovementServiceTests` - 22 edges
7. `Butcher.Api.Common.Exceptions` - 20 edges
8. `Butcher.Api.Infrastructure.Data` - 19 edges
9. `ProductServiceTests` - 17 edges
10. `StockUnitServiceTests` - 15 edges

## Surprising Connections (you probably didn't know these)
- `ADR-001: Client-serveur (no offline)` --rationale_for--> `Mini-ERP Charcuterie (butcher-app)`  [EXTRACTED]
  docs/ADR.md → CLAUDE.md
- `ADR-002: Hébergement auto-géré` --rationale_for--> `Mini-ERP Charcuterie (butcher-app)`  [EXTRACTED]
  docs/ADR.md → CLAUDE.md
- `ADR-003: Séparation Frontend/Backend` --rationale_for--> `Mini-ERP Charcuterie (butcher-app)`  [EXTRACTED]
  docs/ADR.md → CLAUDE.md
- `ADR-004: Backend ASP.NET Core` --rationale_for--> `Mini-ERP Charcuterie (butcher-app)`  [EXTRACTED]
  docs/ADR.md → CLAUDE.md
- `ADR-005: Frontend Vue 3 + TS PWA` --rationale_for--> `Mini-ERP Charcuterie (butcher-app)`  [EXTRACTED]
  docs/ADR.md → CLAUDE.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Chaîne de Traçabilité Production** — docs_data_model_product, docs_data_model_production_batch, docs_data_model_stock_unit, docs_data_model_stock_movement, docs_data_model_customer [EXTRACTED 1.00]

## Communities (245 total, 211 thin omitted)

### Community 0 - "API Controllers"
Cohesion: 0.05
Nodes (7): ControllerBase, CustomersController, ProductionBatchesController, ProductsController, StockMovementsController, StockUnitsController, UnitsOfMeasureController

### Community 1 - "Application DTOs & Services"
Cohesion: 0.08
Nodes (13): Butcher.Api.Application.Dtos, Butcher.Api.Application.Services, Butcher.Api.Controllers, AddStockUnitsRequest, CreateCustomerRequest, CreateProductionBatchRequest, CreateUnitOfMeasureRequest, CustomerDto (+5 more)

### Community 2 - "Service Tests"
Cohesion: 0.08
Nodes (5): IAsyncLifetime, CustomerServiceTests, ProductServiceTests, PostgresDatabaseFixture, PostgreSqlContainer

### Community 3 - "Frontend Dependencies"
Cohesion: 0.06
Nodes (30): dependencies, @mdi/font, @phosphor-icons/vue, pinia, vue, vue-router, vuetify, engines (+22 more)

### Community 4 - "Domain Entities & Setup"
Cohesion: 0.10
Nodes (10): Butcher.Api.Infrastructure.Data.Configurations, Butcher.Api.Domain.Entities, Butcher.Api.Common, Customer, Product, ProductionBatch, RefreshToken, StockMovement (+2 more)

### Community 5 - "PRD Requirements"
Cohesion: 0.07
Nodes (28): Vague 1 (MVP), RF-01: Création/Modification Produits, RF-02: Mode de vente, RF-03: Unité d'affichage de vente, RF-04: Gestion Unités de mesure, RF-05: Attributs Unité de mesure, RF-06: Création Lot de production, RF-07: Prix au niveau du lot (+20 more)

### Community 6 - "Database Migrations"
Cohesion: 0.08
Nodes (10): Butcher.Api.Infrastructure.Data.Migrations, Migration, InitialCreate, InitialCreate, AddUnitOfMeasureUniqueIndexes, AddUnitOfMeasureUniqueIndexes, AddIdentityAndRefreshTokens, AddIdentityAndRefreshTokens (+2 more)

### Community 7 - "Product DTOs & Requests"
Cohesion: 0.11
Nodes (11): Butcher.Api.Tests.Support, Butcher.Api.Tests.Application.Services, Butcher.Api.Domain.Enums, CreateProductRequest, CreateStockMovementRequest, ProductDto, StockMovementDto, StockUnitDto (+3 more)

### Community 8 - "Frontend Config"
Cohesion: 0.09
Nodes (17): categories, correctness, env, browser, plugins, $schema, app, phosphor (+9 more)

### Community 9 - "Common Utilities"
Cohesion: 0.12
Nodes (10): IEntityTypeConfiguration, EnumSnakeCaseConverter, AppUserConfiguration, CustomerConfiguration, ProductConfiguration, ProductionBatchConfiguration, RefreshTokenConfiguration, StockMovementConfiguration (+2 more)

### Community 11 - "Community 11"
Cohesion: 0.11
Nodes (19): Mini-ERP Charcuterie (butcher-app), Système de design « Kraft », Phosphor Icons, Saloir (PWA Application Name), Vague 2+, Vuetify, Work Sans (Font), Zilla Slab (Font) (+11 more)

### Community 14 - "Community 14"
Cohesion: 0.24
Nodes (4): DbContext, IConfiguration, AuthServiceTests, Service

### Community 15 - "Community 15"
Cohesion: 0.18
Nodes (4): DbUpdateException, int, IProductionBatchService, ProductionBatchService

### Community 17 - "Community 17"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 18 - "Community 18"
Cohesion: 0.15
Nodes (13): compilerOptions, noUncheckedIndexedAccess, paths, tsBuildInfoFile, exclude, extends, include, env.d.ts (+5 more)

### Community 22 - "Community 22"
Cohesion: 0.17
Nodes (10): net10.0, coverlet.collector (6.0.4), Microsoft.EntityFrameworkCore (10.0.11), Microsoft.EntityFrameworkCore.Relational (10.0.11), Microsoft.Extensions.Identity.Core (10.0.11), Microsoft.NET.Test.Sdk (17.14.1), Testcontainers.PostgreSql (4.14.0), xunit (2.9.3) (+2 more)

### Community 23 - "Community 23"
Cohesion: 0.15
Nodes (12): compilerOptions, lib, tsBuildInfoFile, types, exclude, extends, include, env.d.ts (+4 more)

### Community 28 - "Community 28"
Cohesion: 0.20
Nodes (9): net10.0, EFCore.NamingConventions (10.0.1), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.11), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.11), Microsoft.AspNetCore.OpenApi (10.0.11), Microsoft.EntityFrameworkCore.Design (10.0.11), Npgsql.EntityFrameworkCore.PostgreSQL (10.0.3), Scalar.AspNetCore (2.17.2) (+1 more)

### Community 29 - "Community 29"
Cohesion: 0.22
Nodes (5): Exception, BadRequestException, ConflictException, NotFoundException, UnauthorizedException

### Community 30 - "Community 30"
Cohesion: 0.31
Nodes (3): AuthResponseDto, LoginRequest, AuthController

### Community 32 - "Community 32"
Cohesion: 0.29
Nodes (7): eslint, eslint-config-prettier, devDependencies, eslint, eslint-config-prettier, @types/node, @types/node

### Community 33 - "Community 33"
Cohesion: 0.33
Nodes (6): Format Numéro de Lot, customer (Entity), product (Entity), production_batch (Entity), stock_movement (Entity), stock_unit (Entity)

### Community 34 - "Community 34"
Cohesion: 0.40
Nodes (4): printWidth, $schema, semi, singleQuote

### Community 35 - "Community 35"
Cohesion: 0.50
Nodes (3): DbSet, IdentityUserContext, AppDbContext

## Knowledge Gaps
- **209 isolated node(s):** `AddStockUnitsRequest`, `CreateCustomerRequest`, `CreateProductRequest`, `CreateProductionBatchRequest`, `CreateStockMovementRequest` (+204 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **211 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Butcher.Api.Application.Dtos` connect `Application DTOs & Services` to `Community 64`, `Community 65`, `Product DTOs & Requests`, `Community 26`, `Community 30`, `Community 63`?**
  _High betweenness centrality (0.075) - this node is a cross-community bridge._
- **Why does `Butcher.Api.Application.Services` connect `Application DTOs & Services` to `Community 66`, `Domain Entities & Setup`, `Product DTOs & Requests`, `Community 26`, `Community 27`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **Why does `Butcher.Api.Domain.Entities` connect `Domain Entities & Setup` to `Application DTOs & Services`, `Community 26`, `Product DTOs & Requests`, `Community 39`?**
  _High betweenness centrality (0.059) - this node is a cross-community bridge._
- **What connects `AddStockUnitsRequest`, `CreateCustomerRequest`, `CreateProductRequest` to the rest of the system?**
  _209 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `API Controllers` be split into smaller, more focused modules?**
  _Cohesion score 0.05263157894736842 - nodes in this community are weakly interconnected._
- **Should `Application DTOs & Services` be split into smaller, more focused modules?**
  _Cohesion score 0.07823613086770982 - nodes in this community are weakly interconnected._
- **Should `Service Tests` be split into smaller, more focused modules?**
  _Cohesion score 0.08333333333333333 - nodes in this community are weakly interconnected._