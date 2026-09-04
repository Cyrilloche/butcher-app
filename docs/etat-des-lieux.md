# État des lieux — écart entre le prévu et le réalisé

| | |
|---|---|
| **Projet** | Mini-ERP Charcuterie (`butcher-app`) — application « Saloir » |
| **Document** | Analyse d'écart documentation ↔ implémentation |
| **Version** | 1.0 |
| **Date** | 4 septembre 2026 |
| **Méthode** | Relecture de `PRD.md`, `ADR.md`, `data-model.md`, `CLAUDE.md`, confrontée au code réellement présent (contrôleurs, entités, vues, workflows) |

> Ce document est une **photographie datée**, pas une référence permanente : il constate, il ne décide pas. Les décisions qu'il appelle doivent redescendre dans le PRD, les ADR ou `CLAUDE.md`.

---

## 1. Verdict en trois lignes

Le **backend V1 est complet et conforme** au PRD : toutes les entités, règles de gestion et exigences fonctionnelles de la Vague 1 sont implémentées et couvertes par 84 tests. Le **socle de déploiement (ADR-010) est non seulement décidé mais livré** — ce que la documentation présentait encore comme « le prochain spike ». L'écart réel n'est donc ni dans le modèle ni dans l'exploitation : il est dans le **frontend, qui n'expose qu'une partie de l'API déjà disponible**.

---

## 2. Ce qui était prévu et qui est bien là

| Prévu | Réalisé | Preuve dans le code |
|---|---|---|
| 8 entités métier (`product`, `production_batch`, `stock_unit`, `stock_movement`, `sale`, `customer`, `app_user`, `refresh_token`) | ✅ | `backend/src/Butcher.Api/Domain/Entities/` |
| API REST couvrant le cœur métier | ✅ 7 contrôleurs, ~35 routes | `backend/src/Butcher.Api/Controllers/` |
| Mécanisme de stock uniforme, y compris `by_piece` (QM-01) | ✅ | `StockUnitService`, tests dédiés |
| Vente à la tranche + garde-fou de poids (RF-19/RF-20, RG-05) | ✅ back **et** front | `SaleService`, `StockDetailView.vue` |
| Vente comme entité avec numéro et statut de paiement (RF-29/RF-30) | ✅ | `SalesController`, `SaleDetailView.vue` |
| Client obligatoire sur une vente (RF-17/RG-07) | ✅ garanti par le modèle | `Sale.CustomerId` non nul |
| Authentification JWT + refresh rotatif en cookie (ADR-009) | ✅ | `AuthController`, `stores/auth.ts` |
| Déploiement conteneurisé + reverse proxy HTTPS (ADR-010) | ✅ **livré**, pas seulement décidé | `docker-compose.prod.yml`, `Caddyfile`, tunnel Cloudflare, CI/CD |
| Système de design « Kraft » appliqué | ✅ | `plugins/vuetify.ts`, composants `base/` et `domain/` |
| 4 dashboards branchés sur l'API réelle | ✅ | `views/{Stock,Sales,Customers,Products}View.vue` |

---

## 3. Les écarts — API disponible, interface absente

C'est le motif dominant : le backend sait faire, l'utilisateur ne peut pas le déclencher. Aucun de ces écarts ne demande de travail serveur.

| # | Exigence | État backend | État frontend | Gravité |
|---|---|---|---|---|
| E-01 | **RF-21** — marquer une unité en `perso` (autoconsommation) ou `perdu` | ✅ `POST /api/stock-units/{id}/movements` accepte `personal` / `loss` | ✅ **traité le 2026-09-04** — menu d'actions par unité dans Détail Stock (`StockUnitOutcomeMenu.vue`), avec confirmation | Clos |
| E-02 | **RF-08 / RF-09** — référence matière première et DLC d'un lot | ✅ colonnes `raw_material_ref`, `expiry_date` | ❌ non saisissables dans « Ajout Stock » | Moyenne : données de traçabilité perdues à la saisie |
| E-03 | **RG-10** — lot partiellement modifiable après création | ✅ `PUT /api/production-batches/{id}` | ❌ aucun écran d'édition de lot | Moyenne : une erreur de prix ne se corrige pas depuis l'app |
| E-04 | **RG-14** — vente modifiable et supprimable | ✅ `PUT` et `DELETE /api/sales/{id}` | ❌ seul le paiement est modifiable | Moyenne : une vente saisie par erreur est définitive côté utilisateur |
| E-05 | **RG-11** — mouvement modifiable et supprimable | ✅ `PUT` / `DELETE /api/stock-movements/{id}` | ❌ non exposé | Faible : recouvert en partie par E-04 |
| E-06 | **RF-31** — ventes filtrables par client, paiement et période | ✅ `GET /api/sales` accepte ces filtres | ⚠️ partiel : recherche texte (nom, numéro) et regroupement par mois seulement | Faible : l'usage réel reste couvert |

### Autres écarts

| # | Constat | Analyse |
|---|---|---|
| E-07 | **RF-27** — `created_by` existe sur `production_batch`, `sale` et `stock_movement` mais n'est jamais renseigné | Connu et documenté. Le champ prépare la journalisation V2 ; avec un compte partagé en V1, le renseigner n'apporterait rien. Non bloquant, mais le remplir coûterait quelques lignes maintenant que l'utilisateur authentifié est disponible. |
| E-08 | **Aucun test frontend** (la CI passe `--passWithNoTests`) face à 84 tests backend | Asymétrie assumée jusqu'ici. Le risque se concentre désormais côté frontend, où vit la logique d'affichage métier (`useCustomers`, `useStock`, formatage des poids/prix). Ces fonctions pures sont le point d'entrée naturel de premiers tests. |
| E-09 | La bascule **liste / grille** des clients (maquette « Clients Dashboard ») n'est pas implémentée | Écart volontaire : peu de valeur pour deux utilisateurs et un répertoire de quelques dizaines de fiches. À acter comme abandonné plutôt qu'à laisser en dette implicite. |
| E-10 | `DELETE /api/customers/{id}` existe côté API, sans usage frontend | Volontaire (commit `522947d`) : la suppression d'un client casserait la traçabilité lot ↔ client (RF-24) dès qu'il a une vente. L'API refuse déjà (`409`). |

---

## 4. Écarts documentaires (corrigés dans la foulée)

| Document | Écart constaté | Correction |
|---|---|---|
| `CLAUDE.md` §2 | Présentait le socle de déploiement comme « le prochain spike » alors qu'ADR-010 est **accepté et livré** (compose de prod, Caddy, tunnel Cloudflare, CI/CD, commande `create-user`) | Feuille de route réalignée |
| `CLAUDE.md` §5 | Arborescence cible obsolète : ni `docker-compose.prod.yml`, ni `Caddyfile`, ni `Makefile`, ni `development/`, ni `.github/`, ni `CHANGELOG.md` | Arborescence mise à jour |
| `CLAUDE.md` §11 / `ADR.md` (ADR-009) | Question ouverte sur le `SameSite` du cookie de refresh, « à trancher une fois la topologie tranchée par ADR-010 » | **Tranchée** : le `Caddyfile` sert le frontend et `/api/*` sur **la même origine** — les requêtes sont same-origin, `Lax` est le bon réglage et le reste. Question close. |
| `docs/PRD.md` | Statut « backend exposé, frontend en cours » — en retard sur la réalité | Statut réaligné |
| Partout | Aucun document ne décrivait **comment on publie une version** | Ajout du processus de release et du changelog automatique (`cliff.toml`, `make changelog`, `make release-*`) |

---

## 5. Ce qu'il reste pour clore la Vague 1

Dans l'ordre de valeur décroissante :

1. ~~**E-01 — sorties `perso` et `perte` depuis l'interface.**~~ ✅ **Fait le 2026-09-04** : menu d'actions sur chaque unité de Détail Stock (usage perso, perte, clôture), confirmation obligatoire, poids enregistré = **restant estimé** et non poids d'origine (voir §7).
2. **E-04 — corriger ou supprimer une vente.** Filet de rattrapage d'une saisie erronée, dans une app utilisée par des personnes non techniques qui n'ont aucun autre recours.
3. **E-02 — DLC et référence matière première à la création d'un lot.** Deux champs facultatifs dans un formulaire existant ; sans eux, la traçabilité annoncée est amputée à la source.
4. **E-03 — édition d'un lot.** Même logique de rattrapage que E-04, moins fréquente.
5. **E-08 — premiers tests frontend** sur les composables purs, pour équilibrer la couverture.

E-05, E-06, E-07 et E-09 relèvent du confort ou de la V2 : à laisser tels quels, mais tracés ici.

---

## 6. Ce que cette analyse ne remet pas en cause

Aucune décision d'architecture n'est contredite par l'implémentation : pas d'ADR à remplacer. Le modèle de données correspond à ce qui tourne (`data-model.md` v0.7), les conventions de nommage sont tenues (`app_user`, `snake_case`, enums en `snake_case`, `decimal` pour l'argent), et les pièges listés en `CLAUDE.md` §9 n'ont pas été commis. L'écart est un **retard du frontend sur le backend**, pas une dérive de conception.

---

## 7. Note d'implémentation — le poids d'une sortie perso/perte

Le backend impose un `sold_weight` strictement positif sur **tout** mouvement d'un produit vendu au poids, y compris `personal` et `loss` (`StockMovementRules.ValidateSoldWeight`). Le client doit donc fournir un poids ; deux valeurs étaient candidates.

- **Le poids pesé de l'unité** : faux dès que l'unité est entamée — la part déjà vendue serait comptée une seconde fois.
- **Le restant estimé** (poids pesé − somme des `sold_weight` des mouvements de vente) : c'est la valeur retenue. Elle est recalculée à la demande, jamais stockée, ce qui reste conforme à RG-05 (« le poids restant n'est pas suivi » : aucune colonne, aucun affichage permanent).

Ce calcul existait déjà, dupliqué dans « Ajout Vente » ; il est désormais partagé (`getRemainingWeightKg` dans `useStock.ts`) et utilisé par les deux parcours.

Cas limite : une unité entamée dont tout le poids a déjà été vendu a un restant nul. Le backend refuserait un `sold_weight` à zéro ; l'interface le détecte avant l'envoi et oriente vers la clôture (RF-20), qui est le geste correct dans cette situation.
