# Graph Report - .  (2026-09-03)

## Corpus Check
- Corpus is ~33,454 words - fits in a single context window. You may not need a graph.

## Summary
- 609 nodes · 1443 edges · 59 communities (20 shown, 39 thin omitted)
- Extraction: 86% EXTRACTED · 14% INFERRED · 0% AMBIGUOUS · INFERRED: 200 edges (avg confidence: 0.8)
- Token cost: 40,000 input · 25,475 output

## Community Hubs (Navigation)
- Core Application API
- Unit of Measure Module
- Stock Unit Module
- Production Batch Module
- Product Module
- Stock Movement Module
- Customer Module
- User & Data Config
- Stock Movement Tests
- DB Migrations
- External Dependencies
- Project Context & Requirements
- Execution Settings
- ADR-008: Data Access
- ADR-004: Backend Framework
- ADR-005: Frontend Framework
- ADR-006: UI Library
- ADR-007: Database
- ADR-009: Authentication
- ADR-010: Deployment
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Requirement
- Non-Functional Requirement
- Non-Functional Requirement
- Non-Functional Requirement
- Non-Functional Requirement
- Non-Functional Requirement
- Non-Functional Requirement
- Non-Functional Requirement
- Non-Functional Requirement
- Frontend Tech

## God Nodes (most connected - your core abstractions)
1. `Butcher.Api.Application.Dtos` - 41 edges
2. `Butcher.Api.Domain.Entities` - 26 edges
3. `Butcher.Api.Application.Services` - 25 edges
4. `Butcher.Api.Domain.Enums` - 23 edges
5. `StockMovementServiceTests` - 22 edges
6. `AppDbContext` - 17 edges
7. `ProductServiceTests` - 17 edges
8. `StockMovementDto` - 16 edges
9. `Butcher.Api.Common.Exceptions` - 16 edges
10. `ProductionBatch` - 16 edges

## Surprising Connections (you probably didn't know these)
- `CreateProductRequest` --references--> `SaleMode`  [EXTRACTED]
  backend/src/Butcher.Api/Application/Dtos/CreateProductRequest.cs → backend/src/Butcher.Api/Domain/Enums/SaleMode.cs
- `ProductDto` --references--> `SaleMode`  [EXTRACTED]
  backend/src/Butcher.Api/Application/Dtos/ProductDto.cs → backend/src/Butcher.Api/Domain/Enums/SaleMode.cs
- `StockMovement` --references--> `AppUser`  [EXTRACTED]
  backend/src/Butcher.Api/Domain/Entities/StockMovement.cs → backend/src/Butcher.Api/Domain/Entities/AppUser.cs
- `Customer` --references--> `StockMovement`  [EXTRACTED]
  backend/src/Butcher.Api/Domain/Entities/Customer.cs → backend/src/Butcher.Api/Domain/Entities/StockMovement.cs
- `Product` --references--> `UnitOfMeasure`  [EXTRACTED]
  backend/src/Butcher.Api/Domain/Entities/Product.cs → backend/src/Butcher.Api/Domain/Entities/UnitOfMeasure.cs

## Import Cycles
- None detected.

## Communities (59 total, 39 thin omitted)

### Community 0 - "Core Application API"
Cohesion: 0.08
Nodes (18): ExceptionHandlingMiddleware, Task, BadRequestException, ConflictException, NotFoundException, Butcher.Api.Tests.Support, Butcher.Api.Application.Dtos, Butcher.Api.Infrastructure.Data.Configurations (+10 more)

### Community 1 - "Unit of Measure Module"
Cohesion: 0.09
Nodes (23): CreateUnitOfMeasureRequest, UnitOfMeasureDto, UpdateUnitOfMeasureRequest, IUnitOfMeasureService, List, Task, UnitOfMeasureService, List (+15 more)

### Community 2 - "Stock Unit Module"
Cohesion: 0.09
Nodes (26): AddStockUnitsRequest, List, StockUnitDto, IStockUnitService, List, Task, StockUnitService, List (+18 more)

### Community 3 - "Production Batch Module"
Cohesion: 0.08
Nodes (26): CreateProductionBatchRequest, DateOnly, ProductionBatchDto, DateOnly, UpdateProductionBatchRequest, DateOnly, IProductionBatchService, List (+18 more)

### Community 4 - "Product Module"
Cohesion: 0.10
Nodes (20): CreateProductRequest, ProductDto, UpdateProductRequest, IProductService, List, Task, ProductService, List (+12 more)

### Community 5 - "Stock Movement Module"
Cohesion: 0.08
Nodes (25): CreateStockMovementRequest, StockMovementDto, DateTimeOffset, UpdateStockMovementRequest, IStockMovementService, List, Task, StockMovementService (+17 more)

### Community 6 - "Customer Module"
Cohesion: 0.10
Nodes (21): CreateCustomerRequest, CustomerDto, UpdateCustomerRequest, CustomerService, List, Task, ICustomerService, List (+13 more)

### Community 7 - "User & Data Config"
Cohesion: 0.06
Nodes (29): EnumSnakeCaseConverter, AppUser, DateTimeOffset, Guid, Customer, DateTimeOffset, ICollection, Product (+21 more)

### Community 8 - "Stock Movement Tests"
Cohesion: 0.19
Nodes (10): StockMovementServiceTests, Fact, Task, DatabaseCollection, PostgresDatabaseFixture, Task, IAsyncLifetime, ICollectionFixture (+2 more)

### Community 9 - "DB Migrations"
Cohesion: 0.09
Nodes (13): InitialCreate, MigrationBuilder, InitialCreate, ModelBuilder, AddUnitOfMeasureUniqueIndexes, MigrationBuilder, AddUnitOfMeasureUniqueIndexes, ModelBuilder (+5 more)

### Community 10 - "External Dependencies"
Cohesion: 0.12
Nodes (15): net10.0, net10.0, coverlet.collector (6.0.4), EFCore.NamingConventions (10.0.1), Microsoft.AspNetCore.OpenApi (10.0.11), Microsoft.EntityFrameworkCore (10.0.11), Microsoft.EntityFrameworkCore.Design (10.0.11), Microsoft.EntityFrameworkCore.Relational (10.0.11) (+7 more)

### Community 11 - "Project Context & Requirements"
Cohesion: 0.17
Nodes (17): ADR-001: Architecture Client-Serveur, ProductionBatch, Customer, StockMovement, Product, StockUnit, UnitOfMeasure, AppUser (+9 more)

### Community 12 - "Execution Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 13 - "ADR-008: Data Access"
Cohesion: 0.67
Nodes (3): ADR-008: EF Core + Npgsql, EF Core, Npgsql

## Knowledge Gaps
- **24 isolated node(s):** `net10.0`, `EFCore.NamingConventions (10.0.1)`, `Microsoft.AspNetCore.OpenApi (10.0.11)`, `Microsoft.EntityFrameworkCore.Design (10.0.11)`, `Npgsql.EntityFrameworkCore.PostgreSQL (10.0.3)` (+19 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **39 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Butcher.Api.Application.Dtos` connect `Core Application API` to `Unit of Measure Module`, `Stock Unit Module`, `Production Batch Module`, `Product Module`, `Stock Movement Module`, `Customer Module`?**
  _High betweenness centrality (0.119) - this node is a cross-community bridge._
- **Why does `Butcher.Api.Infrastructure.Data` connect `Core Application API` to `DB Migrations`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `User & Data Config` to `Core Application API`, `Unit of Measure Module`, `Stock Unit Module`, `Production Batch Module`, `Product Module`, `Stock Movement Module`, `Stock Movement Tests`?**
  _High betweenness centrality (0.069) - this node is a cross-community bridge._
- **What connects `net10.0`, `EFCore.NamingConventions (10.0.1)`, `Microsoft.AspNetCore.OpenApi (10.0.11)` to the rest of the system?**
  _24 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Core Application API` be split into smaller, more focused modules?**
  _Cohesion score 0.0798076923076923 - nodes in this community are weakly interconnected._
- **Should `Unit of Measure Module` be split into smaller, more focused modules?**
  _Cohesion score 0.08579234972677596 - nodes in this community are weakly interconnected._
- **Should `Stock Unit Module` be split into smaller, more focused modules?**
  _Cohesion score 0.08892921960072596 - nodes in this community are weakly interconnected._