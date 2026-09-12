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
| `entity_label` | varchar(200) | nullable | Libellé lisible au moment de l'opération (« V-260912-1 », « SC-260912-3 ») |
| `deleted_content` | jsonb | nullable | Contenu complet, **uniquement** pour `deleted` (clarification Q3) |

**Règles** : table **append-only** — aucune route de modification ni de suppression (FR-024). Écrite
dans la même transaction que l'opération (research R-09). Index sur `occurred_at` décroissant, et sur
`(account_id, occurred_at)` pour les filtres (FR-023).

## 4. Rapports — aucune donnée stockée (lot ultérieur : US5)

Lectures agrégées sur `sale` et `stock_movement`, calculées à la demande à partir des montants
enregistrés (FR-030). Aucune table.

## 5. Correspondance code ↔ affichage à ajouter (`docs/data-model.md` §4.2)

| Code | Affichage |
|---|---|
| `admin` | Administrateur |
| `user` | Utilisateur |
| `created` / `updated` / `deleted` | Création / Modification / Suppression |
| `login_succeeded` / `login_failed` / `locked_out` / `password_changed` | Connexion / Connexion refusée / Compte verrouillé / Mot de passe changé |
