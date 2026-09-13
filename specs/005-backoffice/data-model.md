# Data Model — Backoffice PC, comptes nominatifs et rôles

**Feature**: `specs/005-backoffice` | **Date**: 2026-09-12

Conventions du projet : tables et colonnes en `snake_case`, enums sérialisés en `snake_case`,
horodatages en `timestamptz`, clé `uuid` pour `app_user` (CLAUDE.md §6).

---

## 1. `app_user` — enrichie (lot 1 : US1, US2)

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | uuid | PK | Inchangé (Identity) |
| `email` | varchar | unique, non nul | Identifiant de connexion (inchangé, clarification Q1) |
| `display_name` | varchar(100) | **nouveau**, non nul | Nom affiché (« Mireille ») |
| `role` | `account_role` | **nouveau**, non nul, défaut `user` | `admin` ou `user` (FR-002) |
| `is_active` | boolean | **nouveau**, non nul, défaut `true` | Faux = connexion et session refusées (FR-007) |
| `last_login_at` | timestamptz | **nouveau**, nullable | Dernière connexion réussie |
| `created_at` | timestamptz | inchangé | |
| `updated_at` | timestamptz | **nouveau**, nullable | Posé par `StampAuditDates` |

**Enum `account_role`** : `admin` → « Administrateur », `user` → « Utilisateur » (table de
correspondance, `docs/data-model.md` §4.2).

**Règles** :
- Un compte n'est **jamais supprimé** (FR-009) : aucune route ni commande de suppression.
- Il existe **toujours au moins un compte `admin` actif** (FR-008) : désactiver, réactiver ou changer
  le rôle vérifie cet invariant sous transaction, refus `409` sinon.
- **Mot de passe** : socle 20 caractères pour tous, 32 pour `admin` (FR-034). La promotion
  `user → admin` exige un nouveau mot de passe conforme au rôle cible (FR-035).
- **Migration** (FR-010) : comptes existants → `role = admin`, `is_active = true`,
  `display_name` = partie locale de l'email.

**Transitions** :

```text
         créer (admin)
            │
            ▼
   ┌──── actif ────┐  désactiver (admin, jamais le dernier admin actif)
   │               │ ───────────────────────────────▶ désactivé
   │  user ⇄ admin │ ◀─────────────────────────────── réactiver (admin)
   └───────────────┘
   promotion : nouveau mot de passe 32 caractères dans le même geste
   rétrogradation : refusée si elle retire le dernier admin actif
```

Désactiver révoque tous les refresh tokens du compte ; changer ou réinitialiser le mot de passe aussi
(FR-006).

## 2. Auteur des enregistrements — colonnes existantes, désormais renseignées (lot 1)

`production_batch.created_by`, `sale.created_by`, `stock_movement.created_by` existent déjà
(uuid → `app_user`, nullable). **Aucun changement de schéma** : `AppDbContext.SaveChanges` les pose à
l'insertion à partir du compte de la requête (research R-04).

- `null` = enregistrement antérieur aux comptes nominatifs, ou écrit hors requête HTTP (commande
  hors ligne). L'interface l'affiche « Compte partagé (avant comptes nominatifs) » (FR-026).
- Les DTO de lecture exposent `createdByName` (nom affiché, ou `null`), pour tous les rôles
  (FR-020a).

## 3. `audit_entry` — nouvelle (lot ultérieur : US4)

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | bigint | PK, identité | |
| `occurred_at` | timestamptz | non nul | Date et heure de l'opération |
| `account_id` | uuid | FK → `app_user`, nullable | Auteur (null hors requête) |
| `action` | `audit_action` | non nul | `created`, `updated`, `deleted`, `login_succeeded`, `login_failed`, `locked_out`, `password_changed` |
| `entity_type` | `audit_entity_type` | nullable | `product`, `production_batch`, `stock_unit`, `sale`, `stock_movement`, `customer`, `account` |
| `entity_id` | varchar(64) | nullable | Identifiant de l'objet (entier ou uuid en texte) |
| `entity_label` | varchar(200) | nullable | Libellé lisible au moment de l'opération, en français (« V-260912-1 (3 lignes) », « SC-260912-3 », « Saucisson (SC) — désactivé ») ; pour une connexion refusée sur un compte inconnu, l'adresse tapée |
| `deleted_content` | jsonb | nullable | Contenu complet, **uniquement** pour `deleted` (clarification Q3) |

Les enums sont stockés en texte `snake_case`, comme le reste du schéma (C-11).

**Règles** : table **append-only** — aucune route de modification ni de suppression (FR-024). Écrite
dans la même transaction que l'opération (research R-09). Index sur `occurred_at` décroissant, et sur
`(account_id, occurred_at)` pour les filtres (FR-023).

### 3.1 Une entrée par geste (clarification du 2026-09-13)

`AppDbContext.SaveChanges` lit le `ChangeTracker` avant l'écriture, et en déduit les gestes :

| Changements d'un même enregistrement | Entrée |
|---|---|
| `sale` ajoutée avec ses lignes | 1 `created` · `sale` · « V-… (n lignes) » |
| `sale` supprimée avec ses lignes | 1 `deleted` · `sale`, contenu : numéro, date, client, paiement, note, lignes (unité, produit, poids, montant) |
| `sale` modifiée | 1 `updated` · `sale` |
| ligne de vente (`stock_movement` de type `sale`) ajoutée, modifiée ou supprimée seule | 1 entrée · `stock_movement` · « V-… · SC-… », contenu à la suppression |
| sorties `personal` / `loss` ajoutées | 1 `created` · `stock_movement` par type ; une seule : « Perte · SC-… », plusieurs (solde) : « Perte · Saucisson (12 unités) » |
| `production_batch` ajoutée, modifiée | 1 entrée · `production_batch` · « Saucisson — 13/09/2026 » |
| `production_batch` supprimée avec ses unités | 1 `deleted` · `production_batch`, contenu : produit, date, prix, unités (numéro, poids) |
| `stock_unit` ajoutées à une fournée existante | 1 `created` · `stock_unit` par fournée · « Saucisson — 13/09/2026 (10 : SC-…-1 à SC-…-10) » |
| `stock_unit` supprimée seule | 1 `deleted` · `stock_unit`, contenu : numéro, produit, date, poids |
| `stock_unit` passée d'« entamée » à « vendue » sans mouvement | 1 `updated` · `stock_unit` · « SC-… — clôturée » |
| `stock_unit` modifiée par un mouvement du même enregistrement | **aucune** (effet induit) |
| `product`, `customer` ajouté, modifié, supprimé | 1 entrée ; une (dés)activation le précise dans le libellé ; contenu à la suppression |
| `app_user` ajouté, ou modifié sur son nom, son rôle, son état | 1 entrée · `account` |
| `app_user` : `password_hash` changé | `password_changed` (changement, réinitialisation, promotion, commande `set-password`) |
| `app_user` : `lockout_end` posé dans le futur | `locked_out` |
| `app_user` : seulement `last_login_at`, compteur d'échecs, jetons | **aucune** (champs techniques) |

`login_succeeded` et `login_failed` sont écrits explicitement par `AuthService` : un échec sur une
adresse inconnue ne modifie aucune ligne que le `ChangeTracker` pourrait voir.

**Auteur** : le compte de la requête. Pour une connexion, un verrouillage ou un mot de passe changé
hors requête authentifiée, le compte concerné lui-même.

**Écriture** : les identifiants des objets créés n'existent qu'après l'écriture. Les entrées sont
donc préparées avant, complétées et enregistrées juste après, dans une transaction qui englobe les
deux écritures (celle de l'appelant si elle existe). Un échec de l'opération n'en laisse aucune.

## 4. Rapports — aucune donnée stockée (lot US5)

Lectures agrégées sur `sale` et `stock_movement` de type `sale`, calculées à la demande à partir
des montants enregistrés (FR-030). Aucune table.

- **Période** : dates `from` / `to` incluses, jours du fuseau `Europe/Paris` (celui de l'activité).
- **Encaissé / à encaisser** : total des ventes payées / non payées de la période.
- **Par mois** : mois du fuseau `Europe/Paris`, seulement ceux qui portent des ventes.
- **Par produit** : `unitCount` = unités distinctes touchées par une ligne de vente de la période ;
  `lineCount` = lignes de vente ; `soldWeight` = somme des poids vendus (produits au poids) ;
  `total` = somme des montants.
- **À encaisser** : toutes les ventes non payées, sans période, regroupées par client, avec la date
  de la plus ancienne.

## 5. Correspondance code ↔ affichage à ajouter (`docs/data-model.md` §4.2)

| Code | Affichage |
|---|---|
| `admin` | Administrateur |
| `user` | Utilisateur |
| `created` / `updated` / `deleted` | Création / Modification / Suppression |
| `login_succeeded` / `login_failed` / `locked_out` / `password_changed` | Connexion / Connexion refusée / Compte verrouillé / Mot de passe changé |
