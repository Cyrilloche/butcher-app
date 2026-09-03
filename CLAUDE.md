# CLAUDE.md — Mini-ERP Charcuterie (`butcher-app`)

> Document racine de contexte pour les sessions Claude Code. Il oriente, fixe les conventions et pointe vers la documentation de référence. À lire en premier, à chaque session.

---

## 1. En une phrase

Application de gestion (« mini-ERP ») pour une activité **annexe de charcuterie artisanale** tenue par deux particuliers : elle remplace un suivi 100 % papier par un outil simple de gestion de **production, stock et ventes**, appelé à s'enrichir par vagues (rentabilité, matières premières, recettes).

**Utilisateurs finaux** : la mère et le beau-père du porteur de projet. Non techniques, potentiellement peu à l'aise avec le numérique. → **La simplicité et la lisibilité priment sur la richesse fonctionnelle.**

---

## 2. État d'avancement & feuille de route

**Phase actuelle : cœur métier backend exposé, spike authentification à venir immédiatement.**

| Étape | Statut |
|---|---|
| Cadrage métier (discovery) | ✅ Terminé |
| PRD | ✅ Rédigé (v0.2) |
| Décisions d'architecture (ADR) | ✅ Rédigées (ADR-006 tranché : Vuetify) |
| Modèle de données | ✅ Rédigé (v0.3, aligné sur l'implémentation) |
| Maquettes (Claude Design) | ✅ Direction visuelle validée (écran Stock) ; arrêtées volontairement au profit de l'itération en code (voir §10) |
| Backend — cœur métier (6 entités, CRUD + logique métier) | ✅ Exposé en API, 63 tests (branche `backend-init`) |
| Spike authentification (JWT) | 🔄 Prochaine étape immédiate — ADR-009 |
| Frontend — scaffold | 🔄 En cours (branche `frontend-init`, worktree séparé) |
| Développement Vague 1 | 🔄 Démarré |

**Méthode : dé-risquage avant développement.** On valide les points techniques risqués par des *spikes* isolés **avant** de construire les fonctionnalités. Spikes prévus, dans l'ordre :

1. **Authentification** (JWT + refresh + stockage sécurisé du jeton côté PWA) — **prioritaire**, c'est le point le plus délicat (service exposé sur Internet). Voir ADR-009.
2. **Socle de déploiement** : Docker Compose (backend + frontend + PostgreSQL) derrière un reverse proxy HTTPS (Caddy/Nginx). Voir ADR-010.
3. **Fondations données** : EF Core + Npgsql, première migration, connexion Postgres.

**Vague 1 (MVP) — périmètre visé** (détail dans le PRD §4.1) :
- Authentification (compte simple partagé).
- Référentiels : unités de mesure, produits (avec `code`), clients.
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
| **Authentification** | ASP.NET Core Identity + jetons **JWT** (*à préciser via spike — ADR-009*). |
| **Déploiement** | Docker Compose sur VPS, reverse proxy Caddy/Nginx, HTTPS Let's Encrypt. **Auto-hébergé** (pas de BaaS). |

**Contraintes d'architecture structurantes** :
- **Pas de mode hors-ligne** en V1 (client-serveur classique — ADR-001). La PWA préserve la porte pour l'ajouter plus tard.
- **Séparation stricte frontend/backend** via le contrat d'API REST (ADR-003). Le backend ne connaît rien de Vue, et réciproquement. La frontière est le contrat.

---

## 5. Structure du dépôt (cible)

```
butcher-app/
├── CLAUDE.md              # ce fichier
├── docs/
│   ├── PRD.md
│   ├── ADR.md
│   └── data-model.md
├── backend/               # ASP.NET Core Web API (C#)
│   └── Dockerfile
├── frontend/              # Vue 3 + TS (PWA)
│   ├── src/
│   │   ├── components/
│   │   │   ├── base/      # wrappers Vuetify (AppButton, AppCard...)
│   │   │   └── domain/    # composants métier (StockUnitCard, WeighInput...)
│   │   ├── composables/   # logique réutilisable (useAuth, useStock...)
│   │   ├── api/           # client généré depuis le Swagger backend
│   │   ├── layouts/       # AppLayout.vue (navigation bas d'écran)
│   │   ├── views/         # StockView, SalesView, CustomersView, ProductsView, LoginView
│   │   └── router/
│   └── Dockerfile
├── docker-compose.yml
└── README.md
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
product → production_batch → stock_unit → stock_movement → customer
```

| Entité | Rôle |
|---|---|
| `product` | Produit fabriqué. Porte un `code` court (ex. `SC`) et un `sale_mode` (`by_weight` / `by_piece`). |
| `production_batch` | Une fabrication datée d'un produit, à un **prix propre au lot**. Identifiée par un `batch_number` auto-généré. |
| `stock_unit` | **Objet physique individuel** (un sachet, un jambon), avec son `weight` pesé et son `status`. |
| `stock_movement` | Toute sortie de stock (`sale` / `personal` / `loss`), rattachée à une `stock_unit`. |
| `customer` | Client (vente informelle, traçabilité). |
| `unit_of_measure` | Référentiel d'unités personnalisables. |
| `app_user` | Compte d'accès (Identity). |

> Détail complet, contraintes et DBML : `docs/data-model.md` (qui fait foi).

---

## 8. Règles métier critiques (à respecter dans le code)

Ces règles sont le cœur de la logique. Le backend en est le garant.

1. **Stock à l'unité physique.** Le stock n'est pas un compteur abstrait : chaque sachet / jambon est une ligne `stock_unit` distincte, avec son poids pesé individuellement (`by_weight`). Le stock disponible = nombre d'unités au statut `available` (ou `opened`).

2. **Mécanisme uniforme, y compris `by_piece`.** *(QM-01, résolu et implémenté.)* Même les produits à la pièce (futures terrines) génèrent une ligne `stock_unit` par pièce (sans poids), plutôt qu'un compteur. Objectif : une seule logique de stock, pas deux.

3. **Vente en une fois vs vente partielle.**
   - *En une fois* (sachet, jambon entier) : un `stock_movement` de type `sale` ; l'unité passe à `sold`.
   - *Partielle* (jambon à la tranche) : plusieurs `stock_movement` de type `sale` sur la **même** `stock_unit`, qui reste `opened` jusqu'à une **clôture manuelle** en `sold`. Le poids restant **n'est pas suivi**.

4. **Le `status` de `stock_unit` est la source de vérité de l'état de stock.** C'est une dénormalisation assumée : le passage `opened → sold` d'un jambon est une décision manuelle non déductible des mouvements. Le backend maintient la cohérence status ↔ mouvements.

5. **Le montant de vente (`amount`) est stocké, pas recalculé.** Vente informelle en espèces : le montant réellement encaissé peut différer du théorique (`sold_weight × sale_price`). On pré-remplit avec le calcul, on conserve la valeur réelle saisie.

6. **Champs réservés à la vente.** `amount` et `customer_id` ne sont renseignés que si `type = sale`. `null` pour `personal` / `loss`.

7. **Numéro de lot** `CODE-YYMMDD-N` (ex. `SC-250831-1`) : auto-généré, `N` réinitialisé par produit et par jour, unicité garantie. Format pensé pour être **recopié à la main** sur l'étiquette → rester court et lisible.

---

## 9. Pièges connus (à ne pas commettre)

- ❌ Nommer une table `user` → utiliser `app_user`.
- ❌ Stocker de l'argent en flottant → `decimal(10,2)`.
- ❌ Afficher des valeurs techniques anglaises à l'utilisateur → passer par la table de correspondance FR.
- ❌ Recalculer `amount` à la volée en ignorant la valeur saisie → conserver le montant réel.
- ❌ Modéliser le stock `by_piece` avec un compteur parallèle → garder le mécanisme uniforme.
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
| ADR-009 | Stratégie d'authentification (JWT + refresh + stockage) | 🔄 Spike démarre maintenant — l'API métier est prête |
| — | CORS : aucune politique configurée côté backend | À ajouter dès que le frontend appelle réellement l'API |
| — | Choix du VPS, stratégie de sauvegarde PostgreSQL | Phase de mise en place |

---

*Ce fichier est vivant : le tenir à jour à chaque décision ou évolution structurante. Il est le point d'entrée de toute session Claude Code sur ce projet.*