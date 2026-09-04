.PHONY: db-up db-down db-logs db-psql pgadmin-logs build run migration migrate test changelog release-frontend release-backend

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

pgadmin-logs:
	$(COMPOSE) logs -f pgadmin

build:
	dotnet build backend

test:
	dotnet test backend

run:
	@set -a; . ./development/.env; set +a; \
	dotnet run --project $(API_PROJECT)

migration:
	@set -a; . ./development/.env; set +a; \
	cd backend && dotnet ef migrations add $(name) --project src/Butcher.Api --startup-project src/Butcher.Api --output-dir Infrastructure/Data/Migrations

migrate:
	@set -a; . ./development/.env; set +a; \
	cd backend && dotnet ef database update --project src/Butcher.Api --startup-project src/Butcher.Api

# --- Releases -----------------------------------------------------------
# Le changelog est dérivé des messages de commit (Conventional Commits) par
# git-cliff, lancé via npx pour n'imposer aucune installation locale.
CLIFF := npx -y git-cliff@2 --config cliff.toml

changelog:
	$(CLIFF) --output CHANGELOG.md

# Pose un tag de release et régénère le changelog dans la foulée.
# Usage : make release-frontend version=0.2.0
release-frontend:
	@test -n "$(version)" || (echo "Usage: make release-frontend version=X.Y.Z" && exit 1)
	git tag frontend-v$(version)
	$(MAKE) changelog
	@echo "Tag frontend-v$(version) posé. Publier avec : git push origin frontend-v$(version)"

release-backend:
	@test -n "$(version)" || (echo "Usage: make release-backend version=X.Y.Z" && exit 1)
	git tag backend-v$(version)
	$(MAKE) changelog
	@echo "Tag backend-v$(version) posé. Publier avec : git push origin backend-v$(version)"
