# Modèle de données — Mini-ERP Charcuterie

| | |
|---|---|
| **Projet** | Mini-ERP Charcuterie (repo : `butcher-app`) |
| **Document** | Modèle de données détaillé (V1) |
| **Version** | 0.4 |
| **Date** | 4 septembre 2026 |
| **Statut** | Implémenté (backend, cœur métier V1 complet) |
| **Documents liés** | PRD v0.2, Journal ADR v0.1 |

### Historique des révisions

| Version | Date | Description |
|---|---|---|
| 0.1 | 2026-09-02 | Modèle initial (nommage français) |
| 0.2 | 2026-09-02 | Passage du schéma en **anglais** ; ajout du champ `code` sur `product` ; définition du format de numéro de lot |
| 0.3 | 2026-09-03 | Documentation des règles apparues pendant l'implémentation du backend : contraintes d'unicité supplémentaires, politiques de mutabilité/suppression par entité, convention de casse des enums dans l'API. QM-01 résolu. |
| 0.4 | 2026-09-04 | **QM-04 résolu et implémenté** : nouvelle entité `sale` (§3.7) regroupant les lignes d'une vente sous un numéro `V-YYMMDD-N`, un client obligatoire et un statut de paiement ; `stock_movement.customer_id` remplacé par `sale_id` ; suppression d'un client passée en `Restrict`. Répond à Q-04/Q-05 du PRD et aux exigences RF-17/RG-07 modifiées. |

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

Deux référentiels complètent l'ensemble : `unit_of_measure` (unités personnalisables) et `app_user` (authentification).

---

## 3. Description des entités

### 3.1 `app_user`

Compte d'accès. V1 : compte simple partagé (RF-26). Colonnes d'authentification gérées par **ASP.NET Core Identity** ; cette table en est la vue logique référencée par `created_by`.

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | uuid | PK | Identifiant (fourni par Identity) |
| `email` | varchar | unique, non nul | Identifiant de connexion |
| `created_at` | timestamptz | défaut `now()` | Date de création |

### 3.2 `unit_of_measure`

Référentiel des unités, **créées et gérées par l'utilisateur** sans développement (RF-04, RF-05). Déclaratif, sans conversion en V1.

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | integer | PK | Identifiant |
| `label` | varchar | non nul | Nom complet (ex. « kilogramme ») |
| `abbreviation` | varchar | non nul | Forme courte (ex. « kg ») |
| `is_active` | boolean | non nul, défaut `true` | Retrait de l'usage sans suppression |

**Règles complémentaires (implémentation)** : `label` et `abbreviation` sont chacun **uniques** (évite les doublons accidentels, ex. deux fois « kilogramme »/« kg »). La désactivation (`is_active = false`) est **bloquée** si l'unité est référencée comme unité de vente par un `product` actif (RG-08).

### 3.3 `product`

Un produit fabriqué. Le **mode de vente** est la propriété structurante (RG-01). Le **`code`** (nouveau) est un identifiant court saisi par l'utilisateur, utilisé pour composer le numéro de lot (§4).

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | integer | PK | Identifiant |
| `code` | varchar | unique, non nul | Code court (ex. `SC`), brique du numéro de lot |
| `name` | varchar | non nul | Désignation |
| `sale_mode` | enum | non nul | `by_weight` ou `by_piece` (RF-02) |
| `sale_unit_id` | integer | FK → `unit_of_measure`, non nul | Unité d'expression du prix (RF-03) |
| `is_active` | boolean | non nul, défaut `true` | Désactivation sans suppression (RF-01) |
| `created_at` / `updated_at` | timestamptz | | Audit |

**Règles complémentaires (implémentation)** : `code` et `sale_mode` sont **définitifs** après création (RG-01 pour `sale_mode` ; `code` porte le numéro de lot, §4.1, donc figé pour la même raison) — `name` et `sale_unit_id` restent modifiables. `sale_unit_id` doit référencer une `unit_of_measure` **active** (vérifié à la création et à la modification). La **désactivation d'un produit n'est jamais bloquée** (RG-09), même s'il a déjà des lots — asymétrie assumée avec `unit_of_measure` (§3.2), qui elle bloque.

### 3.4 `production_batch`

Une fabrication d'un produit, à une date, avec un **prix propre au lot** (RG-02), identifiée par un **numéro de lot** lisible (§4).

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | integer | PK | Identifiant |
| `batch_number` | varchar | unique, non nul | Référence humaine (étiquette, traçabilité), auto-générée (§4) |
| `product_id` | integer | FK → `product`, non nul | Produit fabriqué |
| `production_date` | date | non nul | Date de fabrication |
| `sale_price` | decimal(10,2) | non nul | Prix **par kg** (`by_weight`) ou **par pièce** (`by_piece`) (RF-07) |
| `raw_material_ref` | varchar | nullable | Texte libre en V1 (RF-08) |
| `expiry_date` | date | nullable | DLC éventuelle (RF-09) |
| `notes` | text | nullable | Observations |
| `created_by` | uuid | FK → `app_user`, nullable | Auteur (RF-27) |
| `created_at` / `updated_at` | timestamptz | | Audit |

**Règles complémentaires (implémentation, RG-10)** : `product_id`, `production_date` et `batch_number` sont **définitifs** après création. `sale_price`, `raw_material_ref`, `expiry_date`, `notes` restent modifiables (correction d'erreur de saisie). **Aucune suppression** de lot n'est possible. La création d'un lot est **bloquée** si le produit référencé est inactif ou inexistant.

### 3.5 `stock_unit`

Cœur du suivi de stock : **un objet physique distinct**, suivi individuellement (RF-11 à RF-14). Stock disponible = nombre d'unités `available` (ou `opened`).

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | integer | PK | Identifiant |
| `batch_id` | integer | FK → `production_batch`, non nul | Lot d'origine |
| `weight` | decimal(10,3) | nullable | Poids pesé (si `by_weight`, sinon `null`) (RF-12) |
| `status` | enum | non nul, défaut `available` | `available`, `opened`, `sold`, `personal`, `lost` (RF-13) |
| `created_at` / `updated_at` | timestamptz | | Audit |

**Règles complémentaires (implémentation)** : les unités d'un lot sont générées via un appel **distinct** de la création du lot (§ note RF-10 du PRD), pour permettre une pesée étalée dans le temps. Une unité au statut `available` peut être **supprimée** (correction d'une erreur de pesée) uniquement si aucun `stock_movement` n'y est rattaché. Le statut `opened` n'est atteignable que via une vente partielle (`stock_movement` de type `sale`) : il n'existe pas de moyen de créer directement une unité `opened`.

**Numéro affiché par unité (pas une colonne)** : contrairement au lot (`batch_number`, unique et auto-généré, § 4.1), une `stock_unit` individuelle n'a **aucun numéro stocké en base** — seuls `id`, `batch_id`, `weight`, `status` existent. Le libellé affiché à l'utilisateur pour une unité (ex. `SC-260902-1`, `SC-260902-2`...) est le `batch_number` du lot parent, suffixé par le **rang de l'unité dans ce lot** (ordre de pesée/création). C'est un identifiant d'affichage recalculé côté client à partir de l'ordre des unités renvoyées pour un lot donné — pas un identifiant persistant ni garanti stable si des unités sont supprimées puis recréées.

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
- Pour éviter au frontend un aller-retour, l'API expose en lecture seule `saleNumber`, `customerId` et `customerName` sur chaque ligne, résolus via la vente.

---

## 4. Numéro de lot & correspondance des libellés

### 4.1 Format du numéro de lot (`batch_number`)

Le numéro est **auto-généré** puis **recopié à la main** sur l'étiquette : la contrainte de conception est donc la **lisibilité et la brièveté**.

**Format retenu :** `{CODE}-{YYMMDD}-{N}`

| Segment | Description | Exemple |
|---|---|---|
| `CODE` | Code du produit (`product.code`), saisi par l'utilisateur, en majuscules | `SC` |
| `YYMMDD` | Date de production, 6 chiffres | `250831` |
| `N` | Séquence, réinitialisée par produit et par jour, démarre à 1, sans zéro initial | `1` |

**Exemple complet :** `SC-250831-1` — saucisse curry, produite le 31/08/2025, 1ᵉʳ lot de ce produit ce jour-là.

**Règles de génération :**
- À la création d'un lot, l'application calcule `N = (nombre de lots déjà existants pour ce produit à cette date) + 1` (simple comptage, pas de parsing des numéros existants — possible car aucun lot n'est jamais supprimé, §5 C-09).
- L'unicité globale de `batch_number` est garantie par construction et renforcée par la contrainte d'unicité en base. En cas de création concurrente sur le même produit/jour (RNF-05), l'application retente le calcul + l'insertion jusqu'à 3 fois si la contrainte d'unicité est violée.
- `product.code` est normalisé en majuscules **à la création du produit** (pas seulement au moment de composer le numéro de lot) et ne doit pas contenir le séparateur `-`.

> *Le format encode volontairement le produit et la date : cela aide à identifier un lot « à l'œil » sur un sachet sous vide, sans ouvrir l'application, tout en restant recopiable à la main.*

#### Format du numéro de vente (`sale_number`)

Même logique, appliquée à la vente — pas de code produit, une vente pouvant en regrouper plusieurs :

**Format retenu :** `V-{YYMMDD}-{N}`, où `N` est réinitialisé **chaque jour** (toutes ventes confondues). Exemple : `V-260904-1`.

Génération et garantie d'unicité identiques à `batch_number` : comptage des ventes déjà enregistrées ce jour-là, contrainte d'unicité en base, et jusqu'à 3 tentatives en cas de création concurrente. À la différence des lots, une vente **peut** être supprimée (RG-11) : le comptage peut donc réattribuer un numéro déjà utilisé et libéré — l'unicité reste garantie par la base, mais un numéro n'est pas un identifiant d'archive au sens comptable (H-02 : activité informelle, aucune contrainte de facturation légale).

### 4.2 Correspondance code (anglais) ↔ affichage interface (français)

Les valeurs techniques sont en anglais ; l'interface les affiche en français. Cette table fait foi pour la couche de présentation.

| Concept | Valeur technique (code) | Affichage interface (FR) |
|---|---|---|
| Mode de vente — au poids | `by_weight` | Au poids |
| Mode de vente — à la pièce | `by_piece` | À la pièce |
| Statut unité — disponible | `available` | Disponible |
| Statut unité — entamé | `opened` | Entamé |
| Statut unité — vendu | `sold` | Vendu |
| Statut unité — usage perso | `personal` | Perso |
| Statut unité — perdu/cassé | `lost` | Perdu |
| Mouvement — vente | `sale` | Vente |
| Mouvement — usage perso | `personal` | Perso |
| Mouvement — perte/casse | `loss` | Perte |

---

## 5. Règles et contraintes clés

Certaines règles sont **garanties par la logique applicative** et, si pertinent, par des contraintes `CHECK`.

| Réf. | Règle |
|---|---|
| C-01 | `amount` et `customer_id` ne sont renseignés que si `type = sale`. Pour `personal`/`loss`, ils restent `null`. |
| C-02 | `weight` (sur `stock_unit`) et `sold_weight` (sur `stock_movement`) sont renseignés pour les produits `by_weight`, et `null` pour `by_piece`. |
| C-03 | **Vente en une fois** (sachet, jambon entier) : un unique `stock_movement` de type `sale` ; l'unité passe à `sold` (RG-04). |
| C-04 | **Vente partielle** (jambon à la tranche) : plusieurs `stock_movement` de type `sale` sur une même `stock_unit`, qui reste au statut `opened` jusqu'à clôture manuelle en `sold` (RF-19, RF-20). Le poids restant n'est pas suivi (RG-05). |
| C-05 | `batch_number` et `product.code` sont uniques. |
| C-06 | Les statuts de sortie (`sold`/`personal`/`lost`) sont exclusifs, posés à l'échelle de l'unité individuelle (RG-06). |
| C-07 | `unit_of_measure.label` et `unit_of_measure.abbreviation` sont uniques. |
| C-08 | `product.sale_unit_id` doit référencer une `unit_of_measure` active ; une unité utilisée par un `product` actif ne peut pas être désactivée (RG-08). |
| C-09 | `product.code` et `product.sale_mode` sont définitifs après création ; `production_batch.product_id`, `production_date` et `batch_number` sont définitifs après création (RG-10). Aucune suppression n'est possible sur `product` ni `production_batch`. |
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
| `unit_of_measure` | `label` (unique) | Évite les doublons de libellé (C-07) |
| `unit_of_measure` | `abbreviation` (unique) | Évite les doublons d'abréviation (C-07) |
| `product` | `code` (unique) | Unicité, génération du numéro de lot |
| `production_batch` | `batch_number` (unique) | Recherche, unicité |
| `production_batch` | `product_id` | Lister les lots d'un produit |
| `stock_unit` | `batch_id` | Lister les unités d'un lot |
| `stock_unit` | `status` | Calcul du stock disponible (fréquent) |
| `stock_movement` | `stock_unit_id` | Historique d'une unité (jambon entamé) |
| `stock_movement` | `sale_id` | Lignes d'une vente |
| `sale` | `sale_number` | Recherche par numéro (unique) |
| `sale` | `customer_id` | Historique d'un client (RF-23) |
| `sale` | `date` | Liste chronologique des ventes |
| `stock_movement` | `date` | Vues chronologiques, futurs rapports |

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
  created_at timestamptz [default: `now()`]
  Note: 'Authentication handled by ASP.NET Core Identity'
}

Table unit_of_measure {
  id integer [pk, increment]
  label varchar [not null]
  abbreviation varchar [not null]
  is_active boolean [not null, default: true]
}

Table product {
  id integer [pk, increment]
  code varchar [unique, not null, note: 'short code, e.g. SC — used in batch_number']
  name varchar [not null]
  sale_mode sale_mode [not null]
  sale_unit_id integer [not null, ref: > unit_of_measure.id]
  is_active boolean [not null, default: true]
  created_at timestamptz [default: `now()`]
  updated_at timestamptz
}

Table production_batch {
  id integer [pk, increment]
  batch_number varchar [unique, not null, note: 'format CODE-YYMMDD-N, auto-generated']
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
  weight "decimal(10,3)" [note: 'weighed if by_weight, otherwise null']
  status stock_unit_status [not null, default: 'available']
  created_at timestamptz [default: `now()`]
  updated_at timestamptz

  Indexes {
    batch_id
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
- Enrichissement de `app_user` (rôles/permissions) et exploitation de `created_by` pour une journalisation « qui a fait quoi ».

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

*Fin du document — version 0.4. Le schéma physique est matérialisé par les migrations Entity Framework Core (`backend/src/Butcher.Api/Infrastructure/Data/Migrations/`), déjà appliquées pour l'ensemble du cœur métier V1.*