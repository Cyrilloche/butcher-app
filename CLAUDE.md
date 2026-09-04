# CLAUDE.md — Mini-ERP Charcuterie (`butcher-app`)

> Document racine de contexte pour les sessions Claude Code. Il oriente, fixe les conventions et pointe vers la documentation de référence. À lire en premier, à chaque session.

---

## 1. En une phrase

Application de gestion (« mini-ERP ») pour une activité **annexe de charcuterie artisanale** tenue par deux particuliers : elle remplace un suivi 100 % papier par un outil simple de gestion de **production, stock et ventes**, appelé à s'enrichir par vagues (rentabilité, matières premières, recettes).

**Utilisateurs finaux** : la mère et le beau-père du porteur de projet. Non techniques, potentiellement peu à l'aise avec le numérique. → **La simplicité et la lisibilité priment sur la richesse fonctionnelle.**

---

## 2. État d'avancement & feuille de route

**Phase actuelle : backend Vague 1 complet, socle de déploiement livré (ADR-010), frontend en rattrapage — quelques parcours du périmètre V1 restent à exposer dans l'interface (voir `docs/etat-des-lieux.md`).**

| Étape | Statut |
|---|---|
| Cadrage métier (discovery) | ✅ Terminé |
| PRD | ✅ Rédigé (v0.4) |
| Décisions d'architecture (ADR) | ✅ Rédigées (ADR-006 tranché : Vuetify) |
| Modèle de données | ✅ Rédigé (v0.7, aligné sur l'implémentation) |
| Maquettes (Claude Design) | ✅ Toutes vues Vague 1 maquettées (Stock, Produits, Clients, Ventes) ; itération ensuite en code (voir §10) |
| Backend — cœur métier (8 entités dont `sale`, CRUD + logique métier) | ✅ Exposé en API, 84 tests |
| Spike authentification (JWT) | ✅ Réalisé et vérifié — ADR-009 accepté (Identity allégé, refresh token rotatif en base, cookie httpOnly/Secure, seed par variable d'environnement) |
| Frontend — Stock, Produits, Clients, Ventes (dashboard/détail/ajout) | ✅ Branchés sur l'API réelle (branche `frontend-init`, worktree séparé) |
| Socle de déploiement (ADR-010) | ✅ **Livré** : Docker Compose de prod, Caddy en reverse proxy, tunnel Cloudflare, images versionnées poussées par la CI, déploiement sur VPS déclenché par tag |
| Chaîne de release | ✅ Tags `backend-v*` / `frontend-v*`, changelog et notes de release générés par git-cliff (§13) |
| Vente à la tranche (RF-19/RF-20) | ✅ Vendre/clôturer une unité entamée ; `allow_partial_sale` par produit, garde-fou poids côté serveur |
| Développement Vague 1 | 🔄 Quasi complet — reste, côté interface, les sorties `perso`/`perte` (RF-21), la correction d'une vente (RG-14) et la saisie DLC/matière première d'un lot (RF-08/RF-09) |
| Analyse d'écart doc ↔ code | ✅ `docs/etat-des-lieux.md` (04/09/2026) |

**Méthode : dé-risquage avant développement.** On valide les points techniques risqués par des *spikes* isolés **avant** de construire les fonctionnalités. Spikes prévus, dans l'ordre :

1. ~~**Authentification** (JWT + refresh + stockage sécurisé du jeton côté PWA)~~ — ✅ **fait**, voir ADR-009 (accepté).
2. ~~**Socle de déploiement** : Docker Compose (backend + frontend + PostgreSQL) derrière un reverse proxy HTTPS~~ — ✅ **fait**, voir ADR-010 (accepté) : Caddy sert le frontend et `/api/*` sur la même origine, exposé par un tunnel Cloudflare.
3. **Fondations données** : EF Core + Npgsql, première migration, connexion Postgres. ✅ **déjà en place** (utilisé par toutes les entités et par l'authentification).

**Vague 1 (MVP) — périmètre visé** (détail dans le PRD §4.1) :
- Authentification (compte simple partagé).
- Référentiels : produits (avec `code`), clients.
- Production : création de lot → génération des unités physiques → pesée.
- Tableau de bord stock (disponible par produit).
- Sorties : vente (unité entière + jambon « entamé » en plusieurs fois), usage perso, perte.
- Historique d'achats par client + traçabilité lot ↔ client.

**Reporté en Vague 2+** : coût de revient et rentabilité, gestion des achats de matière première, recettes versionnées, multi-comptes avec journalisation, alertes (stock bas, DLC).

---

## 3. Carte de la documentation

La documentation de référence vit dans `docs/`. **En cas de doute, ces documents font foi.**

| Document | Contient |
|---|---|
| `docs/PRD.md` | Le *quoi* : contexte, objectifs, périmètre par vagues, exigences fonctionnelles (`RF-xx`), règles de gestion (`RG-xx`), exigences non fonctionnelles (`RNF-xx`), risques. |
| `docs/ADR.md` | Le *avec quoi* : les 10 décisions d'architecture, chacune avec son contexte, ses conséquences et les alternatives écartées. |
| `docs/data-model.md` | Le modèle de données détaillé (entités, contraintes, format du numéro de lot, DBML, table de correspondance FR/EN, points d'extension V2+). |
| `docs/etat-des-lieux.md` | Le *où on en est* : analyse d'écart entre ce qui était prévu et ce qui tourne, datée. Photographie, pas référence — les décisions qu'elle appelle redescendent dans les documents ci-dessus. |
| `CHANGELOG.md` | Journal des versions publiées, **généré** depuis les messages de commit (`make changelog`). Ne jamais l'éditer à la main. |

> Les exigences sont référencées par identifiant (`RF-07`, `RG-02`, `ADR-005`…). Utiliser ces références dans le code, les commits et les discussions pour la traçabilité.

---

## 4. Pile technique

| Couche | Technologie |
|---|---|
| **Frontend** | Vue 3 + TypeScript, packagé en **PWA**, mobile-first + vue PC. Bibliothèque de composants : **Vuetify** (ADR-006, tranché). Nom d'application (manifest PWA) : **Saloir**. |
| **Backend** | ASP.NET Core Web API (**C#**). |
| **Contrat** | API **REST**, documentée via OpenAPI/Swagger. |
| **Accès données** | Entity Framework Core + **Npgsql**. |
| **Base de données** | **PostgreSQL**. |
| **Authentification** | ASP.NET Core Identity (allégé, sans rôles) + access token **JWT** en mémoire (15 min) + refresh token rotatif en base, cookie httpOnly/Secure (30 jours). Voir ADR-009 (accepté). |
| **Déploiement** | Docker Compose sur VPS, reverse proxy Caddy/Nginx, HTTPS Let's Encrypt. **Auto-hébergé** (pas de BaaS). |

**Contraintes d'architecture structurantes** :
- **Pas de mode hors-ligne** en V1 (client-serveur classique — ADR-001). La PWA préserve la porte pour l'ajouter plus tard.
- **Séparation stricte frontend/backend** via le contrat d'API REST (ADR-003). Le backend ne connaît rien de Vue, et réciproquement. La frontière est le contrat.

---

## 5. Structure du dépôt

```
butcher-app/
├── CLAUDE.md              # ce fichier
├── CHANGELOG.md           # généré (git-cliff) — ne pas éditer à la main
├── cliff.toml             # configuration du changelog
├── Makefile               # base de dev, build/test, migrations, releases
├── docs/
│   ├── PRD.md
│   ├── ADR.md
│   ├── data-model.md
│   └── etat-des-lieux.md
├── design/                # exports Claude Design (style-guide.html)
├── development/           # compose Postgres + pgAdmin et .env de dev (non commité)
├── .github/workflows/     # CI (tests) + release-backend / release-frontend
├── backend/               # ASP.NET Core Web API (C#)
│   ├── src/Butcher.Api/
│   ├── tests/Butcher.Api.Tests/
│   └── Dockerfile
├── frontend/              # Vue 3 + TS (PWA)
│   ├── src/
│   │   ├── components/
│   │   │   ├── base/      # wrappers Vuetify (AppButton, AppCard...)
│   │   │   └── domain/    # composants métier (StockUnitCard, WeighInput...)
│   │   ├── composables/   # logique réutilisable (useAuth, useStock...)
│   │   ├── api/           # client HTTP typé, aligné sur le Swagger backend
│   │   ├── layouts/       # AppLayout.vue (navigation bas d'écran)
│   │   ├── stores/        # Pinia (auth)
│   │   ├── views/         # StockView, SalesView, CustomersView, ProductsView, LoginView
│   │   └── router/
│   └── Dockerfile
├── Caddyfile              # reverse proxy : frontend + /api/* sur la même origine
├── docker-compose.yml     # pile locale complète
├── docker-compose.prod.yml# pile de production (images versionnées + tunnel Cloudflare)
└── .env.example
```

Monorepo, deux applications indépendantes avec chacune son cycle de vie et son conteneur, orchestrées par Docker Compose.

**Organisation de développement** : backend et frontend sont développés dans deux **worktrees Git séparés** du même dépôt (`backend-init` et `frontend-init`), permettant de travailler sur les deux en parallèle sans interférence, avant fusion sur `main`.

---

## 6. Conventions

### Langue
- **Code & schéma : anglais** (identifiants, tables, colonnes, enums).
- **Documentation : français.**
- **Interface utilisateur : français** — couche d'affichage **découplée** des noms techniques. La table de correspondance code↔affichage fait foi (`data-model.md` §4.2). Exemples : enum `by_weight` → « Au poids » ; statut `personal` → « Perso ». **Les utilisateurs ne voient jamais d'anglais.**

### Nommage
- **PostgreSQL** : `snake_case`. Configurer EF Core en conséquence (convention de nommage snake_case).
- **C#** : `PascalCase` pour les entités/propriétés.
- **Table utilisateur** : `app_user` et **jamais** `user` (mot réservé PostgreSQL). ⚠️
- **Vue/TS** : conventions standards de l'écosystème (composants `PascalCase`, etc.).

### Types
- **Argent** : `decimal(10,2)`. **Jamais** de flottant pour de la monnaie.
- **Poids** : `decimal(10,3)` (précision au gramme).
- **Horodatage** : `timestamptz`.
- **Clés** : `integer` auto-incrémenté pour les entités métier ; `uuid` pour `app_user` (Identity).

### Audit
- Champ `created_by` (→ `app_user`) sur `production_batch` et `stock_movement` (RF-27), pour préparer une future journalisation « qui a fait quoi ».
- `created_at` / `updated_at` sur les entités qui évoluent.

---

## 7. Modèle de domaine (résumé)

Chaîne centrale, porteuse de toute la valeur métier (production → traçabilité) :

```
product → production_batch → stock_unit → stock_movement → sale → customer
```

| Entité | Rôle |
|---|---|
| `product` | Produit fabriqué. Porte un `code` court (ex. `SC`) et un `sale_mode` (`by_weight` / `by_piece`), qui détermine seul l'affichage du prix (€/kg ou €/pièce). |
| `production_batch` | Une fabrication datée d'un produit, à un **prix propre au lot**. Identifiée par un `batch_number` auto-généré. |
| `stock_unit` | **Objet physique individuel** (un sachet, un jambon), avec son `weight` pesé et son `status`. |
| `stock_movement` | Toute sortie de stock (`sale` / `personal` / `loss`), rattachée à une `stock_unit` — et, pour une vente, à une `sale`. |
| `sale` | Une **vente** : numéro `V-YYMMDD-N`, date, client (obligatoire), statut de paiement, regroupant ses lignes (`stock_movement`). Pendant de `production_batch` côté vente. |
| `customer` | Client (vente informelle, traçabilité). Non supprimable dès qu'il a une vente. |
| `app_user` | Compte d'accès (Identity). |

> Détail complet, contraintes et DBML : `docs/data-model.md` (qui fait foi).

---

## 8. Règles métier critiques (à respecter dans le code)

Ces règles sont le cœur de la logique. Le backend en est le garant.

1. **Stock à l'unité physique.** Le stock n'est pas un compteur abstrait : chaque sachet / jambon est une ligne `stock_unit` distincte, avec son poids pesé individuellement (`by_weight`). Le stock disponible = nombre d'unités au statut `available` (ou `opened`).

2. **Mécanisme uniforme, y compris `by_piece`.** *(QM-01, résolu et implémenté.)* Même les produits à la pièce (futures terrines) génèrent une ligne `stock_unit` par pièce (sans poids), plutôt qu'un compteur. Objectif : une seule logique de stock, pas deux.

3. **Vente en une fois vs vente partielle.**
   - *En une fois* (sachet, jambon entier) : un `stock_movement` de type `sale` ; l'unité passe à `sold`.
   - *Partielle* (jambon à la tranche) : plusieurs `stock_movement` de type `sale` sur la **même** `stock_unit`, qui reste `opened` jusqu'à une **clôture manuelle** en `sold` (`POST /api/stock-units/{id}/close`, exposé dans Détail Stock). Le poids restant **n'est pas suivi**, mais un garde-fou serveur empêche la somme des `sold_weight` de dépasser le poids pesé de l'unité. Seuls les produits avec `allow_partial_sale = true` (pertinent uniquement en `by_weight`) autorisent ce mode.

4. **Le `status` de `stock_unit` est la source de vérité de l'état de stock.** C'est une dénormalisation assumée : le passage `opened → sold` d'un jambon est une décision manuelle non déductible des mouvements. Le backend maintient la cohérence status ↔ mouvements.

5. **Une vente est une entité, pas une ligne isolée.** Une `sale` regroupe les unités vendues en une fois au même client, et porte le numéro, la date, le client (**obligatoire** — plus de vente anonyme, RF-17/RG-07) et le statut de paiement. Chaque unité vendue reste une ligne (`stock_movement`). Contrairement au lot de production, une vente est créée **avec ses lignes en un seul appel** (`POST /api/sales`) : c'est un instant unique, pas une saisie étalée.

6. **Le montant de vente (`amount`) est stocké, pas recalculé.** Vente informelle en espèces : le montant réellement encaissé peut différer du théorique (`sold_weight × sale_price`). On pré-remplit avec le calcul, on conserve la valeur réelle saisie.

7. **Champs réservés à la vente.** `amount` et `sale_id` ne sont renseignés que si `type = sale`. `null` pour `personal` / `loss`. Le client vient de `sale.customer_id`, jamais dupliqué sur le mouvement.

8. **Numéro de lot** `CODE-YYMMDD-N` (ex. `SC-250831-1`) : auto-généré, `N` réinitialisé par produit et par jour, unicité garantie. Format pensé pour être **recopié à la main** sur l'étiquette → rester court et lisible.

---

## 9. Pièges connus (à ne pas commettre)

- ❌ Nommer une table `user` → utiliser `app_user`.
- ❌ Stocker de l'argent en flottant → `decimal(10,2)`.
- ❌ Afficher des valeurs techniques anglaises à l'utilisateur → passer par la table de correspondance FR.
- ❌ Recalculer `amount` à la volée en ignorant la valeur saisie → conserver le montant réel.
- ❌ Modéliser le stock `by_piece` avec un compteur parallèle → garder le mécanisme uniforme.
- ❌ Réintroduire une unité de mesure sur le produit → supprimée le 2026-09-04 (`sale_mode` suffit, voir `data-model.md` §3.2). Un produit doit rester créable sur une base vierge.
- ❌ Dupliquer le client sur `stock_movement` (une seule source de vérité : `sale.customer_id`).
- ❌ Supprimer un client qui a des ventes → refusé (`409`), ça effacerait la traçabilité lot ↔ client (RF-24).
- ❌ Coupler frontend et backend autrement que par le contrat d'API REST.
- ❌ Traiter l'authentification à la légère (service exposé) → suivre le spike auth avant tout.
- ❌ Sérialiser/stocker les enums en `PascalCase` (`ByWeight`) → toujours `snake_case` (`by_weight`), cohérent avec la table de correspondance FR et le reste du schéma (bug réel rencontré et corrigé, cf. `data-model.md` C-11).

---

## 10. Façon de travailler (accords)

- **Par vagues, façon agile** : on livre le noyau, puis on enrichit. On ne construit pas la V2 en V1, mais on **ne se ferme aucune porte** (points d'extension documentés dans `data-model.md` §8).
- **Décisions tracées** : toute décision d'architecture structurante donne lieu à un ADR ; toute remise en cause d'une décision acceptée donne lieu à un ADR de remplacement qui référence le précédent.
- **Spikes d'abord** sur les zones risquées, avant le développement des fonctionnalités.
- **UX = priorité de conception**, car le vrai risque du projet est l'adoption par des utilisateurs non techniques. Le point de friction n°1 identifié est la **saisie de la pesée unité par unité** (R-01) : optimiser fortement ce parcours (saisie groupée / rapide).

---

## 11. Questions ouvertes

| Réf. | Question | Statut |
|---|---|---|
| ADR-009 | `SameSite` du cookie de refresh token | ✅ **Close (2026-09-04)** : Caddy sert le frontend et `/api/*` sur la même origine (ADR-010), les requêtes sont same-origin — `Lax` est le bon réglage et reste la valeur par défaut. |
| RF-27 | `created_by` existe sur `production_batch`, `sale` et `stock_movement` mais **n'est jamais renseigné** : le champ prépare la journalisation V2, il ne la fait pas | Ouvert, non bloquant (compte partagé en V1) |
| — | Politique de mot de passe Identity (valeurs par défaut, non revues pour 2 utilisateurs non techniques) | Ouvert, non bloquant |
| — | Stratégie de sauvegarde PostgreSQL (le VPS et le déploiement sont en place) | Ouvert — seul point d'exploitation non traité par ADR-010 |
| RF-21 | Sorties `perso` / `perte` : implémentées côté API, non déclenchables depuis l'interface | Ouvert, **bloquant pour clore la Vague 1** (voir `docs/etat-des-lieux.md`, E-01) |

---

## 12. Système de design (« Kraft »)

Référence visuelle : `design/style-guide.html` (export Claude Design, format « Bundled Page » — le CSS utile est dans les balises `<style>`/attributs `style` du début du fichier, le reste est du bruit encodé en base64). Le thème est appliqué dans `frontend/src/plugins/vuetify.ts`, `frontend/src/plugins/phosphor-iconset.ts` et `frontend/src/assets/main.css`.

**Thème clair uniquement pour l'instant** — le fichier de référence montre aussi une variante sombre, volontairement non implémentée (prévue pour plus tard).

### Couleurs

| Rôle | Couleur | Hex |
|---|---|---|
| Fond (`background`) | Kraft — fond | `#ECE2D0` |
| Surface (cartes) | Surface — carte | `#FBF7EE` |
| Texte principal | Texte principal | `#2B241E` |
| `primary` | Terracotta — primaire | `#C4623C` |
| `primary-darken-1` | Terracotta — survol | `#A54E2E` |
| `secondary` | Bois — secondaire | `#6E5A45` |
| `success` | Succès / Prête | `#4E7A4E` (fond `#DCE9D6`) |
| `warning` | Attention / Bas | `#8A6A16` (fond `#F1E4C4`) |
| `error` | Critique | `#B0362A` (fond `#F3D9D3`) |

### Mapping statuts métier ↔ couleurs sémantiques

Documenté en commentaire dans `vuetify.ts`, à respecter dans tous les composants d'affichage de statut :

| Statut `stock_unit` | Couleur |
|---|---|
| `available` | succès |
| `opened` | attention |
| `sold` | neutre (`status-neutral`, `#6E5A45`) |
| `personal` | neutre (`status-neutral`) |
| `lost` | critique |

### Typographie

- Titres (`text-h*`, titres de carte/toolbar) : **Zilla Slab** (serif).
- Interface (corps de texte, boutons, labels) : **Work Sans** (sans-serif).
- Chargées via Google Fonts (`@import` en tête de `main.css`).

### Icônes

Iconset **Phosphor** (`@phosphor-icons/vue`), enregistré comme iconset Vuetify custom nommé `phosphor` dans `frontend/src/plugins/phosphor-iconset.ts` (utilisation : `<v-icon>phosphor:nom-icone</v-icon>`). Les icônes sont importées explicitement une à une (pas d'`import *`) pour rester tree-shakées. `mdi` reste le set par défaut (icônes internes Vuetify — pagination, cases à cocher, etc.) ; seules les icônes de navigation métier (`AppLayout.vue`) sont passées en Phosphor pour l'instant. Composants `base/` et `domain/` non encore migrés.

---

## 13. Versions et releases

Backend et frontend ont des **cycles de vie indépendants**, chacun avec son préfixe de tag.

| Geste | Commande |
|---|---|
| Publier le frontend | `make release-frontend version=0.2.0` puis `git push origin frontend-v0.2.0` |
| Publier le backend | `make release-backend version=0.3.0` puis `git push origin backend-v0.3.0` |
| Régénérer le changelog seul | `make changelog` |

Ce que le tag déclenche (`.github/workflows/release-*.yaml`) : construction de l'image Docker, publication sur Docker Hub (version **et** SHA, jamais `latest`), déploiement sur le VPS, puis création d'une **GitHub Release** dont les notes couvrent la plage depuis le tag précédent du même composant.

**Le changelog est dérivé des messages de commit**, d'où la règle : les commits suivent [Conventional Commits](https://www.conventionalcommits.org/fr/) (`feat(frontend): ...`, `fix(backend): ...`, `!` pour une rupture). Un `chore:` n'apparaît pas dans le journal — c'est voulu. Écrire le message du commit, c'est écrire le changelog.

La version du frontend est **injectée au build** depuis le tag (`APP_VERSION` → `__APP_VERSION__`) et affichée sur l'écran de connexion : un `-dev` en suffixe signale un build local, jamais une image publiée.

---

*Ce fichier est vivant : le tenir à jour à chaque décision ou évolution structurante. Il est le point d'entrée de toute session Claude Code sur ce projet.*