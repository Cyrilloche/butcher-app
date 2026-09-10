# Phase 1 — Modèle de données

**Feature**: Modification et fin de vie d'un produit
**Date**: 2026-09-09
**Référence**: `docs/data-model.md` fait foi pour le modèle global. Ce document décrit uniquement
les évolutions apportées par cette fonctionnalité, à reporter dans la v0.8 du document de référence.

---

## 1. Nouvelle entité — `batch_number_sequence`

Registre des séquences de numéro de lot déjà émises. Son unique raison d'être est que la suppression
d'un lot ne libère jamais son numéro (FR-013). Voir [research.md](./research.md) D-01.

| Colonne | Type | Contraintes |
|---|---|---|
| `product_id` | `integer` | Clé primaire composite, `FK → product(id)`, `ON DELETE RESTRICT` |
| `production_date` | `date` | Clé primaire composite |
| `last_sequence` | `integer` | `NOT NULL`, `> 0` |

**Clé primaire** : `(product_id, production_date)`.

**Règles**.

- La ligne est créée à `1` lors de la première création de lot pour ce couple, puis incrémentée.
- La lecture et l'incrément se font dans la transaction de création du lot, afin que deux créations
  concurrentes ne produisent pas le même numéro.
- La ligne n'est **jamais** supprimée, y compris lorsque tous les lots du couple sont supprimés.
  C'est précisément ce qui empêche la réémission.
- Le registre n'est pas exposé par l'API. Il est une mécanique interne, invisible de l'utilisateur.

**Conséquence visible** : les numéros de lot d'un même produit et d'une même journée peuvent
comporter des trous, par exemple `SC-250831-1` puis `SC-250831-3`. C'est le comportement voulu.

---

## 2. Entité `product` — mutabilité conditionnelle

Aucune colonne ajoutée. Le schéma reste `id`, `code`, `name`, `sale_mode`, `allow_partial_sale`,
`is_active`, `created_at`, `updated_at`.

### État dérivé « utilisé »

Calculé, jamais stocké. Voir D-03.

```
isUsed(product) := il existe au moins un production_batch dont product_id = product.id
```

### Politique de mutabilité par champ

| Champ | Produit jamais utilisé | Produit utilisé |
|---|---|---|
| `name` | Modifiable | Modifiable |
| `allow_partial_sale` | Modifiable | Modifiable |
| `code` | Modifiable | **Figé** (FR-004) |
| `sale_mode` | Modifiable | **Figé** (FR-004) |
| `is_active` | Modifiable sous condition de stock (FR-016) | Idem |

### Contraintes conservées ou précisées

- **Unicité du code** : globale, sur les produits actifs comme désactivés, comparaison insensible à
  la casse (FR-007). Le code est normalisé en majuscules avant contrôle et stockage, comme
  aujourd'hui à la création. La vérification s'applique désormais aussi à la modification.
- **Cohérence `allow_partial_sale`** : ne peut être vrai que si `sale_mode = by_weight` (FR-008).
  Le passage de `by_weight` à `by_piece` force `allow_partial_sale` à faux (FR-009).

### Transitions d'état

```
jamais utilisé  --(création d'un premier lot)-->  utilisé
utilisé  --(suppression du dernier lot)-->  jamais utilisé          (FR-015)

actif  --(désactivation, si aucune unité disponible ni entamée)-->  désactivé   (FR-016)
désactivé  --(réactivation)-->  actif                                            (FR-025)
```

---

## 3. Entité `production_batch` — suppression

Aucune colonne ajoutée. La suppression est **physique**, pas logique (D-02).

### Condition de suppression

Un lot est supprimable si et seulement si aucune de ses `stock_unit` ne porte de `stock_movement`,
quel qu'en soit le type (FR-010, FR-012).

### Effet

- Les `stock_unit` du lot sont supprimées explicitement dans la même transaction (FR-011).
- La ligne de `batch_number_sequence` du couple est laissée intacte.
- Le comportement `ON DELETE RESTRICT` entre `stock_unit` et `production_batch` est conservé comme
  filet de sécurité.

---

## 4. Entité `stock_movement` — sorties produites par le solde

Aucune colonne ajoutée. Le solde crée des mouvements ordinaires de type `loss`.

| Champ | Valeur produite par le solde |
|---|---|
| `type` | `loss`, toujours (FR-019) |
| `sold_weight` | Poids pesé de l'unité si `available` ; poids pesé moins la somme des `sold_weight` des mouvements de vente de l'unité si `opened` ; `null` si l'unité n'a pas de poids (FR-020) |
| `amount` | `null` — réservé aux mouvements de type `sale` |
| `sale_id` | `null` — réservé aux mouvements de type `sale` |
| `date` | Horodatage du solde |
| `notes` | `null` |

L'unité soldée passe à un statut sorti du stock, y compris lorsqu'elle était `opened` (FR-021).

---

## 5. Règles de validation, par exigence

| Règle | Exigence | Lieu d'application |
|---|---|---|
| Code et mode de vente figés sur un produit utilisé | FR-004, FR-005 | Service produit, mise à jour |
| Unicité globale du code, insensible à la casse | FR-007 | Service produit, création et mise à jour |
| Vente à la tranche réservée au poids | FR-008 | Service produit, création et mise à jour |
| Passage au mode pièce désactivant la vente à la tranche | FR-009 | Service produit, mise à jour |
| Lot supprimable seulement si aucune sortie | FR-010, FR-012 | Service lot, suppression |
| Numéro de lot jamais réémis | FR-013 | Service lot, génération du numéro |
| Désactivation interdite avec du stock restant | FR-016 | Service produit, désactivation |
| Poids des sorties de solde calculé par le serveur | FR-020 | Service produit, solde |
| Garde-fous revérifiés à l'enregistrement | FR-027 | Tous les services concernés |

---

## 6. Migration

Une seule migration EF Core, à créer avec `make migration name=AddBatchNumberSequence`.

Contenu : création de la table `batch_number_sequence`, et **initialisation depuis les données
existantes**, sans quoi les lots déjà en base ne seraient pas comptés et le premier lot créé après
la migration réémettrait un numéro déjà porté.

```
INSERT INTO batch_number_sequence (product_id, production_date, last_sequence)
SELECT product_id, production_date, COUNT(*)
FROM production_batch
GROUP BY product_id, production_date;
```

Ce remplissage suppose que la numérotation actuelle, fondée sur le comptage, est intacte, ce qui est
vrai tant qu'aucun lot n'a jamais été supprimé — et rien ne permet aujourd'hui d'en supprimer un.
