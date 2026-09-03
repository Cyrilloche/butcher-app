# Graph Report - .  (2026-09-03)

## Corpus Check
- Corpus is ~38,314 words - fits in a single context window. You may not need a graph.

## Summary
- 712 nodes · 1691 edges · 37 communities (24 shown, 13 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 225 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Application Core
- Stock Unit Management
- Production Batch Management
- Unit of Measure Management
- Product Management
- Stock Movement Management
- Customer Management
- Functional Requirements
- Authentication Logic
- Stock Movement Tests
- Database Migrations
- Domain Entities
- Authentication API
- Project Config
- App Settings
- High Level Architecture
- Deployment Infrastructure
- Data Access Layer
- Backend Framework
- JWT Strategy
- PostgreSQL Database
- Vue 3 Frontend
- Vuetify UI
- RNF-01
- RNF-02
- RNF-03
- RNF-04
- RNF-05
- RNF-06
- RNF-07
- RNF-08

## God Nodes (most connected - your core abstractions)
1. `Butcher.Api.Application.Dtos` - 44 edges
2. `Butcher.Api.Domain.Entities` - 34 edges
3. `Butcher.Api.Application.Services` - 33 edges
4. `Butcher.Api.Domain.Enums` - 23 edges
5. `StockMovementServiceTests` - 22 edges
6. `AppDbContext` - 21 edges
7. `Butcher.Api.Common.Exceptions` - 20 edges
8. `Butcher.Api.Infrastructure.Data` - 19 edges
9. `ProductServiceTests` - 17 edges
10. `StockMovementDto` - 16 edges

## Surprising Connections (you probably didn't know these)
- `AuthService` --implements--> `IAuthService`  [EXTRACTED]
  backend/src/Butcher.Api/Application/Services/AuthService.cs → backend/src/Butcher.Api/Application/Services/IAuthService.cs
- `ProductionBatch` --references--> `AppUser`  [EXTRACTED]
  backend/src/Butcher.Api/Domain/Entities/ProductionBatch.cs → backend/src/Butcher.Api/Domain/Entities/AppUser.cs
- `StockMovement` --references--> `AppUser`  [EXTRACTED]
  backend/src/Butcher.Api/Domain/Entities/StockMovement.cs → backend/src/Butcher.Api/Domain/Entities/AppUser.cs
- `Customer` --references--> `StockMovement`  [EXTRACTED]
  backend/src/Butcher.Api/Domain/Entities/Customer.cs → backend/src/Butcher.Api/Domain/Entities/StockMovement.cs
- `Product` --references--> `ProductionBatch`  [EXTRACTED]
  backend/src/Butcher.Api/Domain/Entities/Product.cs → backend/src/Butcher.Api/Domain/Entities/ProductionBatch.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **V1_Scope** — /mnt/e/perso/butcher-app/docs/PRD.md:RF-01, /mnt/e/perso/butcher-app/docs/PRD.md:RF-02, /mnt/e/perso/butcher-app/docs/PRD.md:RF-03, /mnt/e/perso/butcher-app/docs/PRD.md:RF-04, /mnt/e/perso/butcher-app/docs/PRD.md:RF-05, /mnt/e/perso/butcher-app/docs/PRD.md:RF-06, /mnt/e/perso/butcher-app/docs/PRD.md:RF-07, /mnt/e/perso/butcher-app/docs/PRD.md:RF-08, /mnt/e/perso/butcher-app/docs/PRD.md:RF-09, /mnt/e/perso/butcher-app/docs/PRD.md:RF-10, /mnt/e/perso/butcher-app/docs/PRD.md:RF-11, /mnt/e/perso/butcher-app/docs/PRD.md:RF-12, /mnt/e/perso/butcher-app/docs/PRD.md:RF-13, /mnt/e/perso/butcher-app/docs/PRD.md:RF-14, /mnt/e/perso/butcher-app/docs/PRD.md:RF-15, /mnt/e/perso/butcher-app/docs/PRD.md:RF-16, /mnt/e/perso/butcher-app/docs/PRD.md:RF-17, /mnt/e/perso/butcher-app/docs/PRD.md:RF-18, /mnt/e/perso/butcher-app/docs/PRD.md:RF-19, /mnt/e/perso/butcher-app/docs/PRD.md:RF-20, /mnt/e/perso/butcher-app/docs/PRD.md:RF-21, /mnt/e/perso/butcher-app/docs/PRD.md:RF-22, /mnt/e/perso/butcher-app/docs/PRD.md:RF-23, /mnt/e/perso/butcher-app/docs/PRD.md:RF-24, /mnt/e/perso/butcher-app/docs/PRD.md:RF-25, /mnt/e/perso/butcher-app/docs/PRD.md:RF-26, /mnt/e/perso/butcher-app/docs/PRD.md:RF-27, /mnt/e/perso/butcher-app/docs/PRD.md:RG-01, /mnt/e/perso/butcher-app/docs/PRD.md:RG-02, /mnt/e/perso/butcher-app/docs/PRD.md:RG-03, /mnt/e/perso/butcher-app/docs/PRD.md:RG-04, /mnt/e/perso/butcher-app/docs/PRD.md:RG-05, /mnt/e/perso/butcher-app/docs/PRD.md:RG-06, /mnt/e/perso/butcher-app/docs/PRD.md:RG-07, /mnt/e/perso/butcher-app/docs/PRD.md:RG-08, /mnt/e/perso/butcher-app/docs/PRD.md:RG-09, /mnt/e/perso/butcher-app/docs/PRD.md:RG-10, /mnt/e/perso/butcher-app/docs/PRD.md:RG-11, /mnt/e/perso/butcher-app/docs/PRD.md:RG-12 [EXTRACTED]

## Communities (37 total, 13 thin omitted)

### Community 0 - "Application Core"
Cohesion: 0.07
Nodes (19): ExceptionHandlingMiddleware, Task, BadRequestException, ConflictException, NotFoundException, UnauthorizedException, Butcher.Api.Tests.Support, Butcher.Api.Application.Dtos (+11 more)

### Community 1 - "Stock Unit Management"
Cohesion: 0.07
Nodes (33): AddStockUnitsRequest, List, StockUnitDto, IStockUnitService, List, Task, StockUnitService, List (+25 more)

### Community 2 - "Production Batch Management"
Cohesion: 0.07
Nodes (31): CreateProductionBatchRequest, DateOnly, ProductionBatchDto, DateOnly, UpdateProductionBatchRequest, DateOnly, IProductionBatchService, List (+23 more)

### Community 3 - "Unit of Measure Management"
Cohesion: 0.09
Nodes (23): CreateUnitOfMeasureRequest, UnitOfMeasureDto, UpdateUnitOfMeasureRequest, IUnitOfMeasureService, List, Task, UnitOfMeasureService, List (+15 more)

### Community 4 - "Product Management"
Cohesion: 0.10
Nodes (21): CreateProductRequest, ProductDto, UpdateProductRequest, IProductService, List, Task, ProductService, List (+13 more)

### Community 5 - "Stock Movement Management"
Cohesion: 0.08
Nodes (26): CreateStockMovementRequest, StockMovementDto, DateTimeOffset, UpdateStockMovementRequest, IStockMovementService, List, Task, StockMovementService (+18 more)

### Community 6 - "Customer Management"
Cohesion: 0.10
Nodes (21): CreateCustomerRequest, CustomerDto, UpdateCustomerRequest, CustomerService, List, Task, ICustomerService, List (+13 more)

### Community 7 - "Functional Requirements"
Cohesion: 0.04
Nodes (46): RF-01, RF-02, RF-03, RF-04, RF-05, RF-06, RF-07, RF-08 (+38 more)

### Community 8 - "Authentication Logic"
Cohesion: 0.11
Nodes (18): AccessTokenResult, AuthResult, AuthService, Guid, string, Task, ITokenService, TimeSpan (+10 more)

### Community 9 - "Stock Movement Tests"
Cohesion: 0.19
Nodes (10): StockMovementServiceTests, Fact, Task, DatabaseCollection, string, PostgresDatabaseFixture, Task, IAsyncLifetime (+2 more)

### Community 10 - "Database Migrations"
Cohesion: 0.07
Nodes (17): InitialCreate, MigrationBuilder, InitialCreate, ModelBuilder, AddUnitOfMeasureUniqueIndexes, MigrationBuilder, AddUnitOfMeasureUniqueIndexes, ModelBuilder (+9 more)

### Community 11 - "Domain Entities"
Cohesion: 0.08
Nodes (23): AppUser, DateTimeOffset, Guid, Customer, DateTimeOffset, ICollection, RefreshToken, DateTimeOffset (+15 more)

### Community 12 - "Authentication API"
Cohesion: 0.14
Nodes (12): AuthResponseDto, DateTimeOffset, LoginRequest, IAuthService, Task, AuthController, ActionResult, DateTimeOffset (+4 more)

### Community 13 - "Project Config"
Cohesion: 0.10
Nodes (18): net10.0, net10.0, coverlet.collector (6.0.4), EFCore.NamingConventions (10.0.1), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.11), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.11), Microsoft.AspNetCore.OpenApi (10.0.11), Microsoft.EntityFrameworkCore (10.0.11) (+10 more)

### Community 14 - "App Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 15 - "High Level Architecture"
Cohesion: 0.50
Nodes (4): Backend, Frontend, ADR-001, ADR-003

### Community 16 - "Deployment Infrastructure"
Cohesion: 0.67
Nodes (3): Docker Compose, ADR-002, ADR-010

### Community 17 - "Data Access Layer"
Cohesion: 0.67
Nodes (3): Entity Framework Core, Npgsql, ADR-008

## Knowledge Gaps
- **27 isolated node(s):** `net10.0`, `EFCore.NamingConventions (10.0.1)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.11)`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.11)`, `Microsoft.AspNetCore.OpenApi (10.0.11)` (+22 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **13 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Butcher.Api.Application.Dtos` connect `Application Core` to `Stock Unit Management`, `Production Batch Management`, `Unit of Measure Management`, `Product Management`, `Stock Movement Management`, `Customer Management`, `Authentication API`?**
  _High betweenness centrality (0.112) - this node is a cross-community bridge._
- **Why does `Butcher.Api.Infrastructure.Data` connect `Application Core` to `Database Migrations`?**
  _High betweenness centrality (0.080) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `Domain Entities` to `Application Core`, `Stock Unit Management`, `Production Batch Management`, `Unit of Measure Management`, `Product Management`, `Stock Movement Management`, `Authentication Logic`, `Stock Movement Tests`?**
  _High betweenness centrality (0.078) - this node is a cross-community bridge._
- **What connects `net10.0`, `EFCore.NamingConventions (10.0.1)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.11)` to the rest of the system?**
  _27 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Application Core` be split into smaller, more focused modules?**
  _Cohesion score 0.07267884322678843 - nodes in this community are weakly interconnected._
- **Should `Stock Unit Management` be split into smaller, more focused modules?**
  _Cohesion score 0.07236544549977386 - nodes in this community are weakly interconnected._
- **Should `Production Batch Management` be split into smaller, more focused modules?**
  _Cohesion score 0.07242063492063493 - nodes in this community are weakly interconnected._