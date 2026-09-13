# Modèle de données — Mini-ERP Charcuterie

| | |
|---|---|
| **Projet** | Mini-ERP Charcuterie (repo : `butcher-app`) |
| **Document** | Modèle de données détaillé (V1) |
| **Version** | 0.13 |
| **Date** | 14 septembre 2026 |
| **Statut** | Implémenté (backend, cœur métier V1 complet ; comptes nominatifs, journal et rapports sur `feat/backoffice`) |
| **Documents liés** | PRD v0.11, Journal ADR (11 décisions, ADR-011 accepté), `docs/etat-des-lieux.md`, `specs/005-backoffice/data-model.md` |

### Historique des révisions

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-09-02 | Modèle initial (nommage français) |
| 0.2 | 2026-09-02 | Passage du schéma en **anglais** ; ajout du champ `code` sur `product` ; définition du format de numéro de lot |
| 0.3 | 2026-09-03 | Documentation des règles apparues pendant l'implémentation du backend : contraintes d'unicité supplémentaires, politiques de mutabilité/suppression par entité, convention de casse des enums dans l'API. QM-01 résolu. |
| 0.5 | 2026-09-04 | **Entité `unit_of_measure` supprimée** et `product.sale_unit_id` avec elle (décision produit, voir §3.2) : le champ n'avait aucun rôle fonctionnel — RG-03 code le prix en €/kg en dur — et bloquait la création de produit sur une base vide. `sale_mode` suffit à piloter l'affichage. RF-03/RF-04/RF-05 et RG-08 retirés du périmètre V1. |
| 0.4 | 2026-09-04 | **QM-04 résolu et implémenté** : nouvelle entité `sale` (§3.7) regroupant les lignes d'une vente sous un numéro `V-YYMMDD-N`, un client obligatoire et un statut de paiement ; `stock_movement.customer_id` remplacé par `sale_id` ; suppression d'un client passée en `Restrict`. Répond à Q-04/Q-05 du PRD et aux exigences RF-17/RG-07 modifiées. |
| 0.6 | 2026-09-04 | Ajout de `product.allow_partial_sale` (booléen, défaut `false`, pertinent uniquement si `sale_mode = by_weight`) : la vente à la tranche (RF-19) n'est plus possible sur n'importe quel produit au poids, elle doit être explicitement autorisée. Contrôle appliqué côté serveur (`409` sinon), pas seulement dans l'UI. |
| 0.8 | 2026-09-09 | **Mutabilité du produit conditionnée à son usage** : `code` et `sale_mode` redeviennent modifiables tant qu'aucun lot n'est rattaché, et se figent au premier lot (§3.3). **Suppression d'un lot intact** ouverte (§3.4), avec ses unités. Nouvelle entité `batch_number_sequence` (§3.9) : la numérotation ne peut plus être dérivée d'un comptage, puisqu'un lot peut disparaître — un numéro émis n'est jamais réattribué (§4.1). **Désactivation d'un produit conditionnée au stock restant** (§3.3), assortie d'un solde en perte des unités restantes. Poids d'une sortie perso ou perte désormais **calculé par le serveur** (§3.8). |
| 0.9 | 2026-09-10 | **Le numéro d'étiquette descend du lot vers l'unité** : nouvelle colonne `stock_unit.unit_number` (unique, non nulle, §3.5), suppression de `production_batch.batch_number` (§3.4), registre requalifié en `unit_number_sequence` et comptant des unités (§3.9). Motif : le double numéro affiché, `SC-260910-2-1`, était lu comme un sous-lot. Une fournée n'a plus de numéro et s'annonce par sa date, son prix et son rang dans la journée. Rupture de contrat sur trois DTO. |
| 0.11 | 2026-09-12 | **Poids encore vendable exposé sur le contrat d'une unité** (§3.5), calculé à chaque lecture et **jamais stocké** — aucune migration. RG-05 révisée en conséquence (PRD v0.8) : l'interdiction porte sur la persistance, pas sur le calcul ni sur l'affichage. La règle est partagée avec le poids d'une sortie perso ou perte (§3.8) sous le nom `ComputeRemainingWeight`. |
| 0.10 | 2026-09-11 | Aucune modification de schéma. `raw_material_ref` et `expiry_date` (§3.4) documentés comme **non exposés en V1** : RF-08/RF-09 reportées en V2 (PRD v0.7) au nom de la prise en main par des utilisateurs non techniques. Colonnes et API conservées, réouverture sans coût. |
| 0.12 | 2026-09-13 | **Comptes nominatifs avec rôle** (ADR-011, RF-26 révisée) : `app_user` gagne `display_name`, `role` (`admin` / `user`), `is_active`, `last_login_at` et `updated_at` (§3.1). La migration `AddAccountRoles` reprend les comptes existants en administrateurs actifs. **`created_by` est désormais renseigné** (RF-27) par `AppDbContext.SaveChanges`, sans changement de schéma. Libellés des rôles ajoutés à la correspondance (§4.2). |
| 0.13 | 2026-09-14 | **Journal des gestes** (RF-32, `specs/005-backoffice` US4) : nouvelle table `audit_entry` (§3.10), append-only, écrite par `AppDbContext.SaveChanges` dans la transaction de l'opération, une entrée par geste et non par ligne modifiée. Migration `AddAuditEntries`. **Rapports de ventes** (RF-33, US5) : aucune table, lectures agrégées des montants enregistrés, jours et mois de Paris (§3.10, note finale). Natures d'opération et types d'objet ajoutés à la correspondance (§4.2). |
| 0.7 | 2026-09-04 | RG-05 précisée (pas remplacée) : garde-fou serveur empêchant la somme des `sold_weight` d'une unité entamée de dépasser son `weight` pesé, à la création comme à la modification d'un mouvement de vente. Calcul à la volée, aucune colonne « poids restant » ajoutée — conforme à l'intention initiale de RG-05. |

### Objet du document

Ce document décrit le **modèle de données de la V1** : entités, attributs, relations et règles. Il traduit les exigences du PRD (`RF-xx`) et les décisions de l'ADR (PostgreSQL, EF Core + Npgsql) en un schéma exploitable.

**Convention de langue** (décision actée) : le **schéma et le code sont en anglais** ; la **documentation reste en français** ; l'**interface utilisateur est en français** (couche d'affichage découplée des noms techniques — voir la table de correspondance §4). Le schéma DBML figure en §7.

---

## 1. Conventions

- **Langue du code** : identifiants en **anglais**, `snake_case` côté PostgreSQL (EF Core configuré en conséquence). Les classes C# reflètent ces entités en `PascalCase`.
- **Table utilisateur** : nommée `app_user` et non `user`, ce dernier étant un mot réservé de PostgreSQL.
- **Clés primaires** : entier auto-incrémenté (`id`) pour les entités métier ; `uuid` pour `app_user` (aligné sur ASP.NET Core Identity, cf. ADR-009).
- **Horodatage** : `created_at` / `updated_at` (`timestamptz`) pour l'audit et le tri.
- **Traçabilité auteur** : `created_by` (→ `app_user`) sur les tables clés (`production_batch`, `sale`, `stock_movement`), conformément à RF-27.
- **Types monétaires et poids** : `decimal` à précision fixe — `decimal(10,2)` pour les montants, `decimal(10,3)` pour les poids (précision au gramme).

---

## 2. Vue d'ensemble

Chaîne centrale porteuse de la valeur métier (production → traçabilité) :

```
product → production_batch → stock_unit → stock_movement → sale → customer
```

Un **product** est décliné en **production_batch** (fabrication datée, à un prix donné). Chaque lot est matérialisé par des **stock_unit** individuelles (un sachet, un jambon), suivies une à une. Toute sortie de stock — vente, usage personnel, perte — est un **stock_movement** rattaché à une unité précise ; un mouvement de type `sale` appartient à une **sale**, qui porte le client. On remonte ainsi, pour toute vente, jusqu'au lot d'origine et au client.

Le schéma comporte donc **deux couples parent/enfant symétriques** : `production_batch → stock_unit` côté production, `sale → stock_movement` côté vente.

Un référentiel complète l'ensemble : `app_user` (authentification). Le journal `audit_entry` (§3.10) se tient à côté de la chaîne, sans clé vers les objets qu'il raconte : il doit survivre à leur suppression.

---

## 3. Description des entités

### 3.1 `app_user`

Compte **nominatif** d'une personne (RF-26 révisée, ADR-011). Colonnes d'authentification gérées par **ASP.NET Core Identity** ; cette table en est la vue logique référencée par `created_by`.

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | uuid | PK | Identifiant (fourni par Identity) |
| `email` | varchar | unique, non nul | Identifiant de connexion ; aucun message n'y est envoyé |
| `display_name` | varchar(100) | non nul | Nom affiché (« Mireille ») |
| `role` | varchar(20) | non nul, `admin` \| `user` | Rôle du compte, stocké en `snake_case` |
| `is_active` | boolean | non nul | Faux : connexion, rafraîchissement et requêtes refusés |
| `last_login_at` | timestamptz | nullable | Dernière connexion réussie |
| `created_at` | timestamptz | non nul | Date de création |
| `updated_at` | timestamptz | nullable | Dernière modification |

**Règles** (appliquées par le serveur) :
- Un compte n'est **jamais supprimé**, seulement désactivé : il reste l'auteur de ce qu'il a saisi. Désactiver un compte ferme ses sessions.
- L'outil garde **toujours au moins un administrateur actif** : une désactivation ou une rétrogradation qui le retirerait est refusée (`409`), sous transaction sérialisable. Personne ne désactive son propre compte.
- **Mot de passe** : 20 caractères au minimum pour un utilisateur, 32 pour un administrateur, avec majuscule, minuscule, chiffre et caractère spécial. Promouvoir un utilisateur exige un nouveau mot de passe conforme au rôle cible.
- **Aucune valeur par défaut en base** pour `role`, `is_active` et `display_name` : l'application les écrit toujours explicitement. Le compte seedé sur une base vierge est administrateur ; `create-user` crée un utilisateur.
- **Droits relus en base** à chaque requête, jamais déduits du seul jeton : une rétrogradation ou une désactivation prend effet immédiatement.

### 3.2 ~~`unit_of_measure`~~ — supprimée (2026-09-04)

**Entité retirée du modèle**, avec `product.sale_unit_id`, ses endpoints (`/api/units-of-measure`) et RG-08.

*Pourquoi.* Le champ était documenté comme « unité d'expression du prix » (RF-03), mais RG-03 code le calcul en **€/kg en dur** pour `by_weight` : l'unité choisie n'avait donc aucun effet sur le prix, seulement sur un libellé d'affichage qui pouvait le contredire. L'écart est apparu à l'usage : sur une base vierge, aucune `unit_of_measure` n'existe, donc la création du **premier produit** était impossible — un référentiel obligatoire à alimenter avant de pouvoir rien faire, pour des utilisateurs non techniques (H-06, RNF-02).

*Ce qui la remplace.* `sale_mode` seul : `by_weight` → prix affiché en **€ / kg**, `by_piece` → **€ / pièce**. Plus aucun choix à faire à la création d'un produit.

*Ce qu'on perd, assumé.* La possibilité de nommer l'unité d'un produit `by_piece` (« pot », « bocal », « tranche ») — en pratique le **nom du produit** porte déjà cette information (« Terrine 200 g »). Si un besoin concret de conversion ou de libellé d'unité émerge, la réintroduction se fait par simple ajout (nouvelle table + colonne), sans refonte : rien dans la chaîne production → stock → vente n'en dépendait.

### 3.3 `product`

Un produit fabriqué. Le **mode de vente** est la propriété structurante (RG-01). Le **`code`** (nouveau) est un identifiant court saisi par l'utilisateur, utilisé pour composer le numéro de lot (§4).

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | integer | PK | Identifiant |
| `code` | varchar | unique, non nul | Code court (ex. `SC`), brique du numéro de lot |
| `name` | varchar | non nul | Désignation |
| `sale_mode` | enum | non nul | `by_weight` ou `by_piece` (RF-02) |
| `allow_partial_sale` | boolean | non nul, défaut `false` | Autorise la vente à la tranche (RF-19) sur les unités de ce produit. Pertinent uniquement si `sale_mode = by_weight` — rejeté sinon. |
| `is_active` | boolean | non nul, défaut `true` | Désactivation sans suppression (RF-01) |
| `created_at` / `updated_at` | timestamptz | | Audit |

**Règles complémentaires (implémentation, révisées en v0.8)** : `code` et `sale_mode` sont modifiables **tant qu'aucun `production_batch` n'est rattaché au produit**, et se figent dès le premier lot — le code est alors recopié sur des étiquettes existantes (§4.1), et le mode de vente détermine la lecture des ventes passées. `name` et `allow_partial_sale` restent modifiables à tout moment. L'état « utilisé » est **dérivé**, jamais stocké : il se calcule par l'existence d'un lot, et la suppression du dernier lot (§3.4) rend au produit sa modifiabilité. Une valeur identique à celle en base est acceptée sur un produit figé, afin que le client puisse renvoyer la ressource complète ; toute valeur différente est refusée (`409`). L'unicité du `code` s'applique désormais **aussi à la modification**, sur les produits actifs comme désactivés. La **désactivation d'un produit est bloquée** (`409`) tant qu'il lui reste une unité `available` ou `opened` ; un **solde** de ces unités en sorties de type `loss` est offert pour la débloquer, sur une sélection choisie par l'utilisateur. Un produit se crée sur une base entièrement vide : aucun référentiel préalable à alimenter (§3.2). `allow_partial_sale = true` sur un produit `by_piece` est refusé (`400`) à la création comme à la modification. La vente partielle (RF-19, mouvement `isFullSale: false`) est elle-même rejetée (`409`) si l'unité vendue appartient à un produit qui n'a pas `allow_partial_sale = true` — contrôle serveur, pas seulement une UI qui masque l'option.

### 3.4 `production_batch`

Une fabrication d'un produit, à une date, avec un **prix propre au lot** (RG-02). Depuis la v0.9, elle **ne porte plus de numéro** : c'est l'unité physique qui en porte un (§3.5, §4.1). Elle reste le lieu où le prix, la DLC et la matière première d'une fournée entière se rattachent en une fois. À l'écran, une fournée s'annonce par sa date de production, son prix et, lorsque plusieurs fournées partagent la date, son rang dans la journée — un libellé d'affichage, jamais stocké.

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | integer | PK | Identifiant |
| `product_id` | integer | FK → `product`, non nul | Produit fabriqué |
| `production_date` | date | non nul | Date de fabrication |
| `sale_price` | decimal(10,2) | non nul | Prix **par kg** (`by_weight`) ou **par pièce** (`by_piece`) (RF-07) |
| `raw_material_ref` | varchar | nullable | Texte libre (RF-08) — **non saisissable en V1**, voir la note ci-dessous |
| `expiry_date` | date | nullable | DLC éventuelle (RF-09) — **non saisissable en V1**, voir la note ci-dessous |
| `notes` | text | nullable | Observations |
| `created_by` | uuid | FK → `app_user`, nullable | Auteur (RF-27) |
| `created_at` / `updated_at` | timestamptz | | Audit |

> **`raw_material_ref` et `expiry_date` ne sont pas exposés en V1** *(décision du 2026-09-11, PRD v0.7)*. Les colonnes existent, l'API les accepte à la création comme à la modification d'un lot, et les alertes DLC de la V2 s'appuieront dessus (§8). Mais le formulaire d'ajout au stock les laisse de côté : deux saisies facultatives de plus sur le parcours le plus fragile (R-01), alors que la prise en main de l'outil est déjà le défi principal. Ne pas les rajouter au formulaire sans rouvrir RF-08/RF-09.

**Règles complémentaires (implémentation, RG-10, révisées en v0.9)** : `product_id` et `production_date` sont **définitifs** après création. `sale_price`, `raw_material_ref`, `expiry_date`, `notes` restent modifiables (correction d'erreur de saisie). Un lot **peut être supprimé**, avec l'intégralité de ses `stock_unit`, **tant qu'aucune de ces unités ne porte de `stock_movement`** — vente, perso ou perte confondues ; sinon la suppression est refusée (`409`). C'est une correction d'erreur de saisie, pas une opération de gestion : elle est la soupape qui rend vivable le gel du code produit (§3.3). Les unités sont supprimées explicitement par le service, dans une transaction ; le `RESTRICT` en base est conservé comme filet. Les numéros de ses unités **ne sont pas libérés** (§3.9). La création d'un lot est **bloquée** si le produit référencé est inactif ou inexistant.

### 3.5 `stock_unit`

Cœur du suivi de stock : **un objet physique distinct**, suivi individuellement (RF-11 à RF-14). Stock disponible = nombre d'unités `available` (ou `opened`).

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | integer | PK | Identifiant |
| `batch_id` | integer | FK → `production_batch`, non nul | Lot d'origine |
| `unit_number` | varchar | unique, non nul | Le numéro recopié à la main sur l'étiquette, auto-généré (§4.1) |
| `weight` | decimal(10,3) | nullable | Poids pesé (si `by_weight`, sinon `null`) (RF-12) |
| `status` | enum | non nul, défaut `available` | `available`, `opened`, `sold`, `personal`, `lost` (RF-13) |
| `created_at` / `updated_at` | timestamptz | | Audit |

**Règles complémentaires (implémentation)** : les unités d'un lot sont générées via un appel **distinct** de la création du lot (§ note RF-10 du PRD), pour permettre une pesée étalée dans le temps. Une unité au statut `available` peut être **supprimée** (correction d'une erreur de pesée) uniquement si aucun `stock_movement` n'y est rattaché. Le statut `opened` n'est atteignable que via une vente partielle (`stock_movement` de type `sale`) : il n'existe pas de moyen de créer directement une unité `opened`.

**Poids encore vendable (dérivé, v0.11)** : le contrat d'une unité expose `remaining_weight`, égal
au poids pesé moins la somme des `sold_weight` de ses mouvements **de type vente**. Il vaut le poids
pesé sur une unité intacte, zéro sur une unité entièrement vendue, `null` si l'unité n'a pas de
poids — produit `by_piece`, ou unité pas encore pesée. **Aucune colonne, aucun cache** : la valeur
est recalculée à chaque lecture, ce qui est toute la portée de RG-05 révisée. Le calcul est agrégé
en SQL par sous-requête corrélée, donc une lecture d'unités reste une requête quel que soit leur
nombre. Une sortie perso ou une perte **n'est pas** retranchée : elle finalise l'unité, qui quitte
le stock ; l'inclure masquerait un filtre trop large derrière un restant faussement nul.

**Numéro de l'unité (`unit_number`, v0.9)** : c'est **le** numéro du système, celui que l'utilisateur recopie à la main sur l'étiquette du sachet ou du jambon (§4.1). Il est attribué par le serveur au moment où l'unité est créée, c'est-à-dire au geste de pesée, et **jamais modifié ensuite** — l'étiquette physique, elle, ne se réécrit pas. Il est unique en base.

Jusqu'à la v0.8, le numéro appartenait au lot et l'affichage d'une unité le suffixait de son rang dans le lot, ce qui produisait `SC-260910-2-1`. Ce libellé à quatre segments a été lu par l'utilisateur comme la marque d'un sous-lot et jugé inutilement compliqué pour une activité artisanale. Le numéro est donc descendu d'un étage. Aucun numéro ne doit plus être **recomposé côté client** : il change au fil des ventes alors que le papier, lui, ne change pas.

### 3.6 `customer`

Fiche client pour la vente informelle et la traçabilité (RF-22 à RF-24).

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | integer | PK | Identifiant |
| `last_name` | varchar | non nul | Nom |
| `first_name` | varchar | nullable | Prénom |
| `phone` | varchar | nullable | Contact |
| `notes` | text | nullable | Observations |
| `created_at` | timestamptz | | Audit |

**Règle complémentaire (implémentation, modifiée le 2026-09-04)** : la suppression d'un client **sans aucune vente** est autorisée (pas de champ `is_active`) ; dès qu'il a au moins une `sale`, elle est refusée (`409`). La FK depuis `sale` est en `Restrict`. La version précédente (`SetNull` depuis `stock_movement`) effaçait silencieusement la traçabilité « quel lot vendu à quel client » (RF-24 / OBJ-3) sur tout l'historique du client — comportement corrigé.

### 3.7 `sale`

Une **vente** telle que l'utilisateur la vit : un numéro, une date, un client, un statut de paiement, un total — regroupant une ou plusieurs lignes (`stock_movement`), une par unité physique vendue. Pendant, côté vente, de `production_batch` côté production. Résout QM-04 (Q-04 et Q-05 du PRD).

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | integer | PK | Identifiant |
| `sale_number` | varchar | unique, non nul, auto-généré | Numéro communicable, format `V-YYMMDD-N` (§4.1) |
| `customer_id` | integer | FK → `customer`, **non nul**, `Restrict` | Client — obligatoire (RF-17/RG-07 modifiés) |
| `date` | timestamptz | non nul | Date de la vente (défaut : maintenant) |
| `paid` | boolean | non nul, défaut `false` | Statut de paiement (« Payée » / « À payer ») |
| `notes` | text | nullable | Observations |
| `created_by` | uuid | FK → `app_user`, nullable | Auteur (RF-27) |
| `created_at` / `updated_at` | timestamptz | | Audit |

**Règles complémentaires (implémentation)** :
- Contrairement au lot de production (dont les unités sont ajoutées par un **appel distinct**, la pesée pouvant s'étaler sur plusieurs jours), une vente est un **instant unique** : `POST /api/sales` la crée **avec ses lignes**, en une seule transaction. Si une seule ligne est invalide, rien n'est écrit et aucun statut d'unité n'est modifié.
- Une vente comporte **au moins une ligne**. Supprimer la dernière ligne d'une vente est refusé (`409`) : c'est la vente qu'il faut supprimer.
- Des lignes peuvent être ajoutées après coup via `POST /api/stock-units/{id}/movements` en passant le `saleId`.
- L'en-tête (client, date, paiement, notes) reste modifiable (`PUT /api/sales/{id}`), et le seul statut de paiement bascule en un geste (`POST /api/sales/{id}/payment`).
- Une vente est **supprimable** (RG-11) : ses lignes sont supprimées avec elle et chaque unité qui ne porte plus aucun mouvement redevient `available`.
- Le **total** de la vente est la somme des `amount` des lignes ; il n'est pas stocké (aucun risque de divergence), mais il est calculé et exposé par l'API.

### 3.8 `stock_movement`

Toute sortie de stock, rattachée à une **stock_unit précise** (RF-15). Journal qui portera, en V2, la valorisation (rentabilité, autoconsommation).

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | integer | PK | Identifiant |
| `stock_unit_id` | integer | FK → `stock_unit`, non nul | Unité concernée |
| `type` | enum | non nul | `sale`, `personal`, `loss` (RF-16) |
| `date` | timestamptz | non nul, défaut `now()` | Date de la sortie |
| `sold_weight` | decimal(10,3) | nullable | Poids concerné (`by_weight`) ; `null` pour `by_piece` |
| `amount` | decimal(10,2) | nullable | Encaissé — **uniquement** pour `type = sale` |
| `sale_id` | integer | FK → `sale`, **non nul si `type = sale`**, `null` sinon | Vente d'appartenance. Le **client** n'est plus porté ici : il vient de `sale.customer_id`, obligatoire — plus de vente anonyme (RF-17/RG-07 modifiés le 2026-09-04) |
| `notes` | text | nullable | Observations |
| `created_by` | uuid | FK → `app_user`, nullable | Auteur (RF-27) |
| `created_at` | timestamptz | | Audit |

**Règles complémentaires (implémentation)** :
- La distinction vente « en une fois » vs vente « partielle » (RF-18/RF-19) se pilote côté API par un indicateur fourni à la création du mouvement (`isFullSale`) : à `true` (défaut) sur une unité `available`, l'unité passe directement à `sold` ; à `false`, elle passe à `opened` et démarre une séquence de ventes partielles. Une fois `opened`, cet indicateur n'a plus d'effet — la clôture manuelle (RF-20) est une action séparée, qui ne crée pas de mouvement.
- Un mouvement peut être marqué `personal` ou `loss` aussi bien depuis `available` que depuis `opened` (RG-12).
- Aucun mouvement n'est possible sur une unité déjà `sold`, `personal` ou `lost` (statuts terminaux, RG-06).
- Contrairement à `production_batch`, un `stock_movement` reste **modifiable et supprimable** après création (RG-11). La suppression du dernier mouvement d'une unité la remet `available` ; dans les autres cas, le statut n'est pas recalculé (pas de machine à états inverse complète).

- Le **numéro communicable** est porté par la vente (`sale.sale_number`), pas par la ligne : c'est la vente que l'utilisateur retrouve et cite, pas le mouvement individuel. Un `stock_movement` n'a donc que son `id` technique. *(Écart identifié le 2026-09-04, résolu le jour même par l'ajout de `sale`.)*
- Un mouvement `personal` ou `loss` n'a **jamais** de `sale_id` (ni d'`amount`) : ce n'est pas une vente.
- **Le `sold_weight` d'une sortie `personal` ou `loss` est calculé par le serveur** (v0.8), jamais transmis par le client : poids pesé de l'unité si elle est `available`, poids pesé **moins la somme des `sold_weight` de ses ventes** si elle est `opened` — repartir du poids d'origine compterait deux fois la part déjà vendue (RG-05). `null` si l'unité n'a pas de poids. Seule exception, une unité `by_weight` **pas encore pesée** n'offre aucune base de calcul : le poids saisi par l'utilisateur est alors conservé. Un mouvement de type `sale` continue de porter le poids réellement pesé, que seul l'utilisateur connaît.
- Le calcul de ce poids est **le même** que celui du poids encore vendable affiché sur une unité
  (§3.5) : une seule méthode, `ComputeRemainingWeight`, deux appelants. Elle porte ce nom depuis
  la v0.11, l'ancien (`ComputeOutcomeWeight`) désignant son premier usage et non la valeur
  calculée.
- La règle ci-dessus vivait dans le frontend jusqu'en v0.7. Elle a été portée côté serveur au moment où le solde groupé (§3.3) en a eu besoin : deux implémentations de la même règle auraient fini par diverger.
- Pour éviter au frontend un aller-retour, l'API expose en lecture seule, sur chaque ligne : `saleNumber`, `customerId` et `customerName` (résolus via la vente) ainsi que `productName` et `batchNumber` (résolus via `stock_unit → production_batch → product`). Sans ces deux derniers, une vue « détail d'une vente » n'a d'autre choix que de déduire le produit du préfixe du numéro de lot — dérivation fragile, et le préfixe n'est qu'un code, pas un nom.

### 3.9 `unit_number_sequence` *(nouveau en v0.8 sous le nom `batch_number_sequence`, requalifié en v0.9)*

Registre des rangs de numéro d'unité déjà émis, pour un produit et une date de production. Son
unique raison d'être est qu'un numéro émis ne soit **jamais réattribué**, y compris après la
suppression de l'unité qui le portait ou de la fournée dont elle provenait (§3.4, §4.1).

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `product_id` | integer | PK composite, FK → `product`, `RESTRICT` | Produit concerné |
| `production_date` | date | PK composite | Journée de production |
| `last_sequence` | integer | non nul | Dernier `N` d'**unité** émis pour ce couple |

**Règles complémentaires (implémentation, v0.9)** :
- Les rangs sont réservés **par blocs**, à la génération des unités d'une fournée : une demande de
  dix unités prend dix rangs d'un coup. C'est le geste qui produit les objets à étiqueter ; une
  fournée enregistrée puis jamais pesée ne consomme donc aucun numéro.
- La ligne est créée à `0` si elle manque, en absorbant le conflit si quelqu'un vient de la créer,
  puis lue **sous verrou de ligne** dans la transaction qui écrit les unités. Sans ce verrou, deux
  pesées simultanées calculeraient le même rang et l'index unique renverrait une erreur à
  l'utilisateur au lieu du numéro suivant.
- La ligne n'est **jamais supprimée**, y compris lorsque toutes les unités du couple ont disparu.
  C'est précisément ce qui empêche la réémission.
- Le registre n'est **pas exposé par l'API** : c'est une mécanique interne, invisible de l'utilisateur.
- Cas limite assumé : un produit redevenu « jamais utilisé » peut changer de code alors que le
  registre garde des lignes issues des unités supprimées. Le nouveau code produit alors un `N` plus
  élevé que nécessaire. Il n'y a pas de collision, le numéro n'ayant jamais été émis.
- Autre conséquence assumée : une fournée pesée en plusieurs fois, entrecoupée d'une autre fournée
  du même jour, aura des numéros **non contigus**. L'unicité et la non-réémission sont les propriétés
  qui comptent, pas la contiguïté.

### 3.10 `audit_entry` *(nouveau en v0.13)*

Journal « qui a fait quoi » (RF-32). **Append-only** : aucune route ne le modifie ni ne le supprime, et
il se consulte par l'administrateur seul (`GET /api/audit-entries`, écran Journal).

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | bigint | PK, identité | |
| `occurred_at` | timestamptz | non nul | Date et heure du geste |
| `account_id` | uuid | FK → `app_user`, `RESTRICT`, nullable | Auteur ; `null` sur une connexion refusée à une adresse inconnue, ou hors requête (commande hors ligne) |
| `action` | varchar(30) | non nul, `snake_case` | `created`, `updated`, `deleted`, `login_succeeded`, `login_failed`, `locked_out`, `password_changed` |
| `entity_type` | varchar(30) | nullable, `snake_case` | `product`, `production_batch`, `stock_unit`, `sale`, `stock_movement`, `customer`, `account` |
| `entity_id` | varchar(64) | nullable | Identifiant de l'objet, en texte ; `null` pour un geste groupé |
| `entity_label` | varchar(200) | nullable | Libellé en français, figé au moment du geste (« V-260913-2 (3 lignes) ») |
| `deleted_content` | jsonb | nullable | Contenu complet de l'objet, **uniquement** pour une suppression |

**Règles** :
- **Une entrée par geste, pas par ligne modifiée.** Les lignes d'une vente suivent la vente, les unités
  d'une fournée supprimée suivent la fournée, des unités pesées ensemble ou un solde de stock donnent
  une seule entrée. Une unité dont le statut change sous l'effet d'un mouvement du même enregistrement
  n'a pas d'entrée : ce n'est pas un geste, c'est sa conséquence. Table complète des regroupements :
  `specs/005-backoffice/data-model.md` §3.1.
- **Écrite dans la transaction de l'opération**, par `AppDbContext.SaveChanges` à partir du
  `ChangeTracker` : une opération qui échoue ne laisse aucune entrée, aucune n'échappe au journal.
  Seules les connexions sont écrites explicitement, par `AuthService` : un échec sur une adresse
  inconnue ne change aucune ligne.
- **Contenu gardé pour les suppressions seulement**, suffisant pour ressaisir l'objet. Une
  modification n'enregistre ni l'avant ni l'après : l'objet se consulte dans son état actuel.
- **Comptes** : création, nom, rôle et état donnent une « Modification » ; un mot de passe changé et un
  verrouillage ont leur propre nature ; la dernière connexion et les jetons d'Identity n'en ont aucune.
- Pas de durée de conservation : le volume d'une activité artisanale le permet.

> **Rapports de ventes (RF-33) — aucune table.** Lectures agrégées de `sale` et des `stock_movement`
> de type `sale`, calculées à la demande sur les `amount` enregistrés, jamais depuis un poids et un
> prix. Jours et mois sont ceux de Paris, pas de l'UTC du serveur. Par produit, une unité vendue en
> plusieurs tranches compte **une** unité et autant de lignes que de tranches.

---

## 4. Numéro de lot & correspondance des libellés

### 4.1 Format du numéro d'étiquette (`stock_unit.unit_number`)

Le numéro est **auto-généré** puis **recopié à la main** sur l'étiquette : la contrainte de conception est donc la **lisibilité et la brièveté**. Depuis la v0.9, il identifie **l'objet physique**, pas la fabrication.

**Format retenu :** `{CODE}-{YYMMDD}-{N}`

| Segment | Description | Exemple |
|---|---|---|
| `CODE` | Code du produit (`product.code`), saisi par l'utilisateur, en majuscules | `SC` |
| `YYMMDD` | Date de production, 6 chiffres | `250831` |
| `N` | Séquence, réinitialisée par produit et par jour, démarre à 1, sans zéro initial | `1` |

**Exemple complet :** `SC-250831-1` — saucisse curry, produite le 31/08/2025, 1ᵉʳ **sachet** de ce produit ce jour-là. Dix sachets fabriqués le matin portent `-1` à `-10` ; une seconde fournée le même jour continue à `-11`.

**Règles de génération (révisées en v0.9) :**
- Les rangs sont pris dans le registre `unit_number_sequence` (§3.9), **par blocs**, à la génération des unités d'une fournée, sous verrou de ligne. Un comptage des unités existantes n'est pas possible : depuis que la suppression d'une fournée et d'une unité existent (§3.4, §3.5), il ferait retomber le compte et **réémettrait un numéro déjà écrit** sur une seconde série d'étiquettes manuscrites, indiscernable de la première.
- **Un numéro émis n'est jamais réattribué.** Conséquences assumées : les séries comportent des trous après une suppression, la première unité du jour peut porter un `N` supérieur à 1, et les numéros d'une même fournée pesée en plusieurs fois peuvent ne pas être contigus.
- L'unicité globale d'`unit_number` reste renforcée par la contrainte d'unicité en base, qui sert de filet plutôt que de mécanisme : le registre et son verrou rendent la collision impossible.
- Le numéro n'est **jamais recomposé côté client** : il est écrit sur du papier, il ne peut pas dépendre de l'état courant de la base.
- `product.code` est normalisé en majuscules **à la création du produit** (pas seulement au moment de composer le numéro) et ne doit pas contenir le séparateur `-`.

> *Le format encode volontairement le produit et la date : cela aide à identifier un sachet « à l'œil » sous vide, sans ouvrir l'application, tout en restant recopiable à la main.*

#### Format du numéro de vente (`sale_number`)

Même logique, appliquée à la vente — pas de code produit, une vente pouvant en regrouper plusieurs :

**Format retenu :** `V-{YYMMDD}-{N}`, où `N` est réinitialisé **chaque jour** (toutes ventes confondues). Exemple : `V-260904-1`.

Génération et garantie d'unicité de même esprit qu'`unit_number`, mais par comptage : comptage des ventes déjà enregistrées ce jour-là, contrainte d'unicité en base, et jusqu'à 3 tentatives en cas de création concurrente. À la différence des lots, une vente **peut** être supprimée (RG-11) : le comptage peut donc réattribuer un numéro déjà utilisé et libéré — l'unicité reste garantie par la base, mais un numéro n'est pas un identifiant d'archive au sens comptable (H-02 : activité informelle, aucune contrainte de facturation légale).

### 4.2 Correspondance code (anglais) ↔ affichage interface (français)

Les valeurs techniques sont en anglais ; l'interface les affiche en français. Cette table fait foi pour la couche de présentation.

| Concept | Valeur technique (code) | Affichage interface (FR) |
|---|---|---|
| Mode de vente — au poids | `by_weight` | Au poids |
| Mode de vente — à la pièce | `by_piece` | À la pièce |
| Prix d'un produit au poids | `by_weight` | € / kg |
| Prix d'un produit à la pièce | `by_piece` | € / pièce |
| Statut unité — disponible | `available` | Disponible |
| Statut unité — entamé | `opened` | Entamé |
| Statut unité — vendu | `sold` | Vendu |
| Statut unité — usage perso | `personal` | Perso |
| Statut unité — perdu/cassé | `lost` | Perdu |
| Mouvement — vente | `sale` | Vente |
| Mouvement — usage perso | `personal` | Perso |
| Mouvement — perte/casse | `loss` | Perte |
| Rôle d'un compte — administrateur | `admin` | Administrateur |
| Rôle d'un compte — utilisateur | `user` | Utilisateur |
| Auteur d'un enregistrement — connu | `created_by` renseigné | Saisie par *nom affiché* |
| Auteur d'un enregistrement — inconnu | `created_by` = `null` | Compte partagé (avant comptes nominatifs) |
| Journal — création | `created` | Création |
| Journal — modification | `updated` | Modification |
| Journal — suppression | `deleted` | Suppression |
| Journal — connexion réussie | `login_succeeded` | Connexion |
| Journal — connexion refusée | `login_failed` | Connexion refusée |
| Journal — verrouillage | `locked_out` | Compte verrouillé |
| Journal — mot de passe | `password_changed` | Mot de passe changé |
| Journal — objet produit | `product` | Produit |
| Journal — objet fournée | `production_batch` | Fournée |
| Journal — objet unité | `stock_unit` | Unité |
| Journal — objet vente | `sale` | Vente |
| Journal — objet sortie | `stock_movement` | Sortie de stock |
| Journal — objet client | `customer` | Client |
| Journal — objet compte | `account` | Compte |
| Journal — sans auteur, connexion | `account_id` = `null`, `login_failed` | Personne (adresse inconnue) |
| Journal — sans auteur, autre geste | `account_id` = `null` | Hors application |

---

## 5. Règles et contraintes clés

Certaines règles sont **garanties par la logique applicative** et, si pertinent, par des contraintes `CHECK`.

| Réf. | Règle |
|---|---|
| C-01 | `amount` et `customer_id` ne sont renseignés que si `type = sale`. Pour `personal`/`loss`, ils restent `null`. |
| C-02 | `weight` (sur `stock_unit`) et `sold_weight` (sur `stock_movement`) sont renseignés pour les produits `by_weight`, et `null` pour `by_piece`. |
| C-03 | **Vente en une fois** (sachet, jambon entier) : un unique `stock_movement` de type `sale` ; l'unité passe à `sold` (RG-04). |
| C-04 | **Vente partielle** (jambon à la tranche) : plusieurs `stock_movement` de type `sale` sur une même `stock_unit`, qui reste au statut `opened` jusqu'à clôture manuelle en `sold` (RF-19, RF-20). Le poids restant n'est pas suivi (RG-05), mais la somme des `sold_weight` déjà enregistrés sur l'unité ne peut jamais dépasser son `weight` pesé — vérifié à l'écriture (création **et** modification d'un mouvement de vente), calcul à la volée sans colonne dédiée, rejeté en `409` sinon. |
| C-05 | `stock_unit.unit_number` et `product.code` sont uniques. |
| C-06 | Les statuts de sortie (`sold`/`personal`/`lost`) sont exclusifs, posés à l'échelle de l'unité individuelle (RG-06). |
| ~~C-07~~ | ~~Unicité de `unit_of_measure.label` / `abbreviation`~~ — sans objet, entité supprimée (§3.2). Identifiant conservé, non réattribué. |
| ~~C-08~~ | ~~`product.sale_unit_id` doit référencer une unité active (RG-08)~~ — sans objet, champ supprimé (§3.2). Identifiant conservé, non réattribué. |
| C-09 | `product.code` et `product.sale_mode` sont figés **dès qu'un lot est rattaché au produit**, modifiables avant (v0.8) ; `production_batch.product_id` et `production_date` sont définitifs après création (RG-10), et `stock_unit.unit_number` l'est aussi (v0.9). Aucune suppression n'est possible sur `product`. Un `production_batch` est supprimable tant qu'aucune de ses unités ne porte de mouvement (v0.8). |
| C-12 | Un `unit_number` déjà émis n'est jamais réattribué, y compris après suppression de l'unité qui le portait ou de la fournée dont elle provenait (v0.9, §3.9). |
| C-13 | Un `product` ne peut être désactivé tant qu'il lui reste une `stock_unit` en `available` ou `opened` (v0.8). |
| C-10 | Une `stock_unit` n'est supprimable que si `status = available` et qu'aucun `stock_movement` ne lui est rattaché. |
| C-11 | Les enums (`sale_mode`, `status`, `type`) sont sérialisés et stockés en **snake_case** (`by_weight`, `available`, `sale`...), jamais en `PascalCase` — cohérent avec la table de correspondance FR (§4.2) et le reste du schéma. |

### Note d'architecture — le statut de l'unité physique

Le champ `status` est une **dénormalisation assumée** : l'état pourrait, pour une vente en une fois, se déduire des mouvements. Mais il est **indispensable** pour le jambon `opened` → `sold`, dont le passage à « terminé » est une **décision manuelle** non déductible (poids restant non suivi). Le `status` est donc la source de vérité de l'état de stock ; le backend garantit sa cohérence avec les mouvements.

### Note d'architecture — le montant est stocké, pas recalculé

`amount` est **enregistré** (et non recalculé depuis `sold_weight × sale_price`) : la vente est informelle et en espèces, le montant réellement encaissé peut différer du théorique. On pré-remplit avec la valeur calculée, mais on conserve la valeur réelle.

---

## 6. Index recommandés

| Table | Index | Justification |
|---|---|---|
| `product` | `code` (unique) | Unicité, génération du numéro d'étiquette |
| `stock_unit` | `unit_number` (unique) | Recherche par étiquette, unicité |
| `production_batch` | `product_id` | Lister les lots d'un produit |
| `stock_unit` | `batch_id` | Lister les unités d'un lot |
| `stock_unit` | `status` | Calcul du stock disponible (fréquent) |
| `stock_movement` | `stock_unit_id` | Historique d'une unité (jambon entamé) |
| `stock_movement` | `sale_id` | Lignes d'une vente |
| `sale` | `sale_number` | Recherche par numéro (unique) |
| `sale` | `customer_id` | Historique d'un client (RF-23) |
| `sale` | `date` | Liste chronologique des ventes |
| `stock_movement` | `date` | Vues chronologiques, rapports |
| `audit_entry` | `occurred_at` (décroissant) | Journal du plus récent au plus ancien |
| `audit_entry` | `(account_id, occurred_at)` | Journal filtré par auteur |

---

## 7. Schéma DBML

> À coller dans [dbdiagram.io](https://dbdiagram.io) pour le diagramme entité-relation.

```dbml
// ===== Mini-ERP Charcuterie — Model V1 =====

Enum sale_mode {
  by_weight
  by_piece
}

Enum stock_unit_status {
  available
  opened
  sold
  personal
  lost
}

Enum movement_type {
  sale
  personal
  loss
}

Table app_user {
  id uuid [pk]
  email varchar [unique, not null]
  display_name varchar(100) [not null]
  role varchar(20) [not null, note: 'admin | user']
  is_active boolean [not null]
  last_login_at timestamptz
  created_at timestamptz [default: `now()`]
  updated_at timestamptz
  Note: 'Authentication handled by ASP.NET Core Identity'
}

Table audit_entry {
  id bigint [pk, increment]
  occurred_at timestamptz [not null]
  account_id uuid [ref: > app_user.id, note: 'null: unknown address on login, or outside a request']
  action varchar(30) [not null, note: 'created | updated | deleted | login_succeeded | login_failed | locked_out | password_changed']
  entity_type varchar(30) [note: 'product | production_batch | stock_unit | sale | stock_movement | customer | account']
  entity_id varchar(64) [note: 'null for a grouped gesture']
  entity_label varchar(200) [note: 'French label frozen at the time of the gesture']
  deleted_content jsonb [note: 'only for deleted']

  Indexes {
    occurred_at
    (account_id, occurred_at)
  }

  Note: 'Append-only; one entry per gesture, written by SaveChanges in the same transaction (v0.13)'
}

Table product {
  id integer [pk, increment]
  code varchar [unique, not null, note: 'short code, e.g. SC — used in unit_number']
  name varchar [not null]
  sale_mode sale_mode [not null]
  allow_partial_sale boolean [not null, default: false, note: 'only meaningful when sale_mode = by_weight']
  is_active boolean [not null, default: true]
  created_at timestamptz [default: `now()`]
  updated_at timestamptz
}

Table unit_number_sequence {
  product_id integer [ref: > product.id, note: 'part of composite PK']
  production_date date [note: 'part of composite PK']
  last_sequence integer [not null, note: 'last unit rank issued; never decremented, never deleted']

  Note: 'Ensures an issued unit_number is never reissued after a unit or its batch is deleted (v0.9)'
}

Table production_batch {
  id integer [pk, increment]
  product_id integer [not null, ref: > product.id]
  production_date date [not null]
  sale_price "decimal(10,2)" [not null, note: 'per kg (by_weight) or per piece (by_piece)']
  raw_material_ref varchar [note: 'free text in V1']
  expiry_date date
  notes text
  created_by uuid [ref: > app_user.id]
  created_at timestamptz [default: `now()`]
  updated_at timestamptz
}

Table stock_unit {
  id integer [pk, increment]
  batch_id integer [not null, ref: > production_batch.id]
  unit_number varchar [unique, not null, note: 'format CODE-YYMMDD-N, auto-generated, written on the label']
  weight "decimal(10,3)" [note: 'weighed if by_weight, otherwise null']
  status stock_unit_status [not null, default: 'available']
  created_at timestamptz [default: `now()`]
  updated_at timestamptz

  Indexes {
    batch_id
    unit_number [unique]
    status
  }
}

Table customer {
  id integer [pk, increment]
  last_name varchar [not null]
  first_name varchar
  phone varchar
  notes text
  created_at timestamptz [default: `now()`]
}

Table sale {
  id integer [pk, increment]
  sale_number varchar [not null, unique, note: 'V-YYMMDD-N']
  customer_id integer [not null, ref: > customer.id]
  date timestamptz [not null, default: `now()`]
  paid boolean [not null, default: false]
  notes text
  created_by uuid [ref: > app_user.id]
  created_at timestamptz [default: `now()`]
  updated_at timestamptz

  Indexes {
    sale_number [unique]
    customer_id
    date
  }
}

Table stock_movement {
  id integer [pk, increment]
  stock_unit_id integer [not null, ref: > stock_unit.id]
  type movement_type [not null]
  sale_id integer [ref: > sale.id, note: 'not null iff type = sale']
  date timestamptz [not null, default: `now()`]
  sold_weight "decimal(10,3)" [note: 'concerned weight (by_weight); null for by_piece']
  amount "decimal(10,2)" [note: 'received, only for type = sale']
  notes text
  created_by uuid [ref: > app_user.id]
  created_at timestamptz [default: `now()`]

  Indexes {
    stock_unit_id
    sale_id
    date
  }
}
```

---

## 8. Points d'extension prévus (V2+)

Évolutions anticipées, greffables **par ajout** sans refonte du noyau :

**Coût de revient et matières premières**
- Tables `supplier` et `raw_material_purchase` (date, quantité, coût total, fournisseur).
- Table de liaison `raw_material_purchase` ↔ `production_batch`, remplaçant progressivement le champ texte `raw_material_ref`.
- Champ calculé `material_cost` sur `production_batch`, base du calcul de marge (encaissé − coût).

**Recettes versionnées**
- Tables `recipe` et `recipe_version`.
- FK `recipe_version_id` sur `production_batch`, matérialisant `batch → recipe_version → product`.

**Gestion fine des utilisateurs**
- ~~Enrichissement de `app_user` (rôles) et journalisation « qui a fait quoi »~~ — livrés en v0.12 et v0.13 (§3.1, §3.10). Restent possibles : des droits plus fins que deux rôles, et l'export du journal.

**Alertes**
- Seuils de stock bas et exploitation de `expiry_date` pour des alertes DLC.

---

## 9. Questions ouvertes sur le modèle

| Réf. | Question | Statut |
|---|---|---|
| QM-01 | **Modélisation des produits `by_piece`** : conserver le mécanisme uniforme (une ligne `stock_unit` par pièce, sans poids) ou un simple compteur sur le lot ? | ✅ Résolu — mécanisme uniforme implémenté : `POST /api/production-batches/{id}/stock-units` génère une `stock_unit` par pièce (`weight = null`) à partir d'une `quantity`, symétrique au cas `by_weight` (une par poids fourni). |
| QM-02 | Format du numéro de lot | ✅ Résolu (§4.1) |
| QM-03 | Comptage des tranches de jambon | ✅ Résolu — poids seul, pas de comptage |
| QM-04 | **Regroupement des ventes** (numéro unique + statut de paiement + plusieurs unités par vente) | ✅ **Résolu et implémenté (2026-09-04)** — entité `sale` (§3.7), `stock_movement.sale_id` (§3.8), numéro `V-YYMMDD-N` (§4.1), suppression client passée en `Restrict` (§3.6). Répond à Q-04 et Q-05 du PRD ; RF-17/RG-07 (client obligatoire) sont désormais garantis par le schéma. |

### Ce qui a été retenu pour QM-04, et ce qui a été écarté

La proposition initiale conservait `stock_movement.customer_id` **en plus** de `sale.customer_id`. Ça a été **écarté** : deux sources de vérité pour le même client divergent tôt ou tard (modifier le client d'une vente aurait obligé à propager sur chaque ligne). La colonne a donc été **supprimée** de `stock_movement` au profit de `sale_id` seul ; l'API continue d'exposer `customerId`/`customerName` sur les lignes, mais en lecture seule, résolus via la vente.

Deux points tranchés à l'implémentation, non couverts par la proposition :

- **Création atomique.** Le précédent `production_batch` (créer le lot, puis ajouter les unités dans un second appel) n'a **pas** été repris tel quel : il existe parce que la pesée peut s'étaler sur plusieurs jours. Une vente, elle, est un instant unique — `POST /api/sales` crée l'en-tête et ses lignes ensemble. L'ajout de lignes après coup reste possible.
- **Suppression d'un client.** Rendre le client obligatoire rendait intenable le `SetNull` existant (il vidait l'historique en silence). Passé en `Restrict` + refus explicite côté service. Un `is_active` sur `customer`, s'il devient nécessaire pour masquer d'anciens clients de la saisie, reste une extension possible sans refonte (§8).

---

*Fin du document — version 0.5. Le schéma physique est matérialisé par les migrations Entity Framework Core (`backend/src/Butcher.Api/Infrastructure/Data/Migrations/`), déjà appliquées pour l'ensemble du cœur métier V1.*