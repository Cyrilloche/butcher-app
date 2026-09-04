# Changelog

Généré à partir des messages de commit ([Conventional Commits](https://www.conventionalcommits.org/fr/)).
Ne pas éditer à la main : régénérer avec `make changelog`.

## frontend-v0.2.0 — 04/09/2026

### Nouveautés

- **frontend** : Affiche la version du build sur l'écran de connexion (a918d87)
- **frontend** : Déconnexion, bandeau contrasté et index A-Z des clients (ad0e0db)

### Documentation

- Analyse d'écart prévu/réalisé et remise à niveau de la documentation (cdeb0d7)

### Intégration et déploiement

- Génère le changelog et les notes de release avec git-cliff (dc60f88)

## backend-v0.2.0 — 04/09/2026

### Nouveautés

- **backend** : Commande hors-ligne create-user pour ajouter un compte en prod (a03be82)

### Corrections

- **deploy** : Drop caddy host port publish in prod compose (6e642b9)

## frontend-v0.1.0 — 04/09/2026

### Nouveautés

- Add domain entities and enums (4772ba9)
- Wire EF Core, Postgres dev container and env-based config (a4bde0b)
- Expose UnitOfMeasure API with error-handling middleware and tests (543d000)
- Expose Product API (CRUD, deactivate/reactivate) with tests (5ffc3c8)
- Expose ProductionBatch API with auto-generated batch numbers (22e9926)
- Expose StockUnit generation and listing API (04ae3b3)
- Expose StockMovement API (sale/personal/loss) with status transitions (da81731)
- Expose Customer API (CRUD) (15876ba)
- Add authentication spike (ASP.NET Core Identity + JWT + refresh token) (9180de8)
- Add Scalar API reference UI with JWT bearer auth support (ca77dbb)
- **frontend** : Implement Stock views and reusable Kraft components (a649b3f)
- **frontend** : Add brand header to Dashboard views (8221fe8)
- **frontend** : Wire Stock views to the real API + auth (60a2891)
- **frontend** : Implement Products views + document Ventes/Clients gaps (a11e61e)
- **frontend** : Implement Customers views (670adc5)
- **frontend** : Skeleton Sales views (Dashboard/Détail/Ajout) (ad48777)
- **backend** : Add the sale entity (QM-04 / Q-04 / Q-05) (4c06ab5)
- **frontend** : Rewire Ventes onto the new /api/sales entity (b6360fd)
- **backend** : Expose productName and batchNumber on sale lines (9dabc01)
- **backend** : Remove the unit_of_measure entity (product decision) ⚠️ **rupture** (8d1e258)
- **frontend** : Drop unit_of_measure following backend removal ⚠️ **rupture** (1d31576)
- **backend** : Add allow_partial_sale to product, enforce it server-side (7b7317b)
- **frontend** : Partial sale flow (vente à la tranche) (2c60520)
- **frontend** : Show remaining weight estimate on partial sales (02dc991)
- **frontend** : Close stock units, drop unused customer delete; docs refresh (522947d)
- **deploy** : Containerize backend/frontend, reverse proxy + Cloudflare tunnel (ADR-010) (bc8e5cc)

### Corrections

- Serialize and store enums in snake_case, not PascalCase (cbf8cf1)
- **backend** : Pin RazorLangVersion to unblock build on .NET 10 SDK (790f57d)
- **frontend** : Send soldWeight when creating a sale line (8514172)
- **backend** : Enforce sold_weight cannot exceed unit weight (RG-05) (6f75b10)
- **frontend** : Drop empty batch cards from Détail Stock (30d77cd)

### Refactorisations

- **frontend** : Use StockMovementDto.productName/batchNumber directly (827430e)

### Documentation

- Create PRD (17a4e15)
- Create ARD (85c85b6)
- Create data-model (c882c60)
- Create CLAUDE.md (55a81d5)
- Pull ADR-006 and progress updates from frontend-init (adac6cd)
- Reconcile PRD, data-model and CLAUDE.md with backend implementation (19c7f2a)
- Formalize ADR-009 (authentication) as Accepted (ea36d99)

### Intégration et déploiement

- Adapt CI/release GitHub Actions template to butcher-app (41ccbee)

