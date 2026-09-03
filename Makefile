.PHONY: db-up db-down db-logs db-psql build run migration migrate

COMPOSE := docker compose -f development/database-dev.yml --env-file development/.env
API_PROJECT := backend/src/Butcher.Api

db-up:
	$(COMPOSE) up -d

db-down:
	$(COMPOSE) down

db-logs:
	$(COMPOSE) logs -f

db-psql:
	@set -a; . ./development/.env; set +a; \
	$(COMPOSE) exec postgres psql -U $$POSTGRES_USER -d $$POSTGRES_DB

build:
	dotnet build backend

run:
	@set -a; . ./development/.env; set +a; \
	dotnet run --project $(API_PROJECT)

migration:
	@set -a; . ./development/.env; set +a; \
	cd backend && dotnet ef migrations add $(name) --project src/Butcher.Api --startup-project src/Butcher.Api --output-dir Infrastructure/Data/Migrations

migrate:
	@set -a; . ./development/.env; set +a; \
	cd backend && dotnet ef database update --project src/Butcher.Api --startup-project src/Butcher.Api
