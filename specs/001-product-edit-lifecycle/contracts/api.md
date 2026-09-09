# Phase 1 — Contrat REST

**Feature**: Modification et fin de vie d'un produit
**Date**: 2026-09-09

Le contrat est publié en OpenAPI par le backend. Ce document décrit ce que la fonctionnalité ajoute
ou modifie, et sert de référence pour aligner `frontend/src/api/`.

Toutes les erreurs suivent le format `ProblemDetails` déjà produit par le middleware d'exceptions,
avec un `detail` rédigé en français. Les enums circulent en `snake_case`.

---

## Résumé des changements

| Endpoint | Nature | Rupture |
|---|---|---|
| `PUT /api/products/{id}` | Deux champs requis ajoutés | **Oui** |
| `POST /api/products/{id}/deactivate` | Nouveau cas de refus `409` | Non, comportement |
| `POST /api/products/{id}/write-off` | Nouveau | Non |
| `DELETE /api/production-batches/{id}` | Nouveau | Non |
| `GET /api/stock-units` | Filtre optionnel ajouté | Non |
| `ProductDto` | Deux champs ajoutés | Non |

---

## `ProductDto` — champs ajoutés

```jsonc
{
  "id": 12,
  "code": "SC",
  "name": "Saucisson sec",
  "saleMode": "by_weight",
  "allowPartialSale": true,
  "isActive": true,

  // Ajouts
  "isUsed": true,                 // au moins un lot de production rattaché (FR-001, FR-002)
  "remainingStockUnitCount": 3    // unités available + opened, pilote le garde-fou FR-016
}
```

`isUsed` décrit un fait, pas une politique : c'est l'interface qui en déduit les champs à griser.

---

## `PUT /api/products/{id}` — mise à jour, rupture de contrat

**Corps de requête** (les deux derniers champs sont les ajouts, tous requis) :

```jsonc
{
  "name": "Saucisson sec",
  "allowPartialSale": true,
  "code": "SEC",
  "saleMode": "by_weight"
}
```

**Comportement**.

- Produit jamais utilisé : les quatre champs sont appliqués.
- Produit utilisé : `code` et `saleMode` doivent être **identiques aux valeurs en base**. Le client
  peut donc renvoyer la ressource complète sans raisonner sur le gel. Toute différence est refusée.

**Réponses**.

| Code | Cas |
|---|---|
| `200` | Mise à jour appliquée, `ProductDto` retourné |
| `400` | `allowPartialSale` vrai sur un produit qui n'est pas vendu au poids (FR-008) |
| `404` | Produit inexistant |
| `409` | Code déjà porté par un autre produit (FR-007), ou tentative de modifier `code` ou `saleMode` sur un produit utilisé (FR-004) |

---

## `POST /api/products/{id}/deactivate` — garde-fou ajouté

Signature inchangée, corps vide, `204` en cas de succès.

| Code | Cas |
|---|---|
| `204` | Produit désactivé |
| `404` | Produit inexistant |
| `409` | **Nouveau** — il reste des unités `available` ou `opened`. Le `detail` nomme le nombre d'unités restant à écouler (FR-016) |

Un produit déjà désactivé renvoie `204` sans effet (idempotent).

---

## `POST /api/products/{id}/write-off` — solde des unités restantes

**Corps de requête** :

```jsonc
{
  "stockUnitIds": [41, 42, 47]
}
```

**Comportement**. Crée une sortie de type `loss` pour chaque unité de la sélection appartenant au
produit et encore `available` ou `opened`. Le poids est calculé par le serveur (FR-020) et n'est
jamais transmis par le client. L'ensemble est traité en une transaction. Les unités déjà sorties
sont ignorées sans faire échouer l'opération, et une sélection vide est sans effet.

**Réponses**.

| Code | Cas |
|---|---|
| `200` | Solde appliqué. Corps : `{ "writtenOffCount": 3 }` |
| `400` | Une unité de la sélection n'appartient pas à ce produit |
| `404` | Produit inexistant |

---

## `DELETE /api/production-batches/{id}` — suppression d'un lot

Corps vide, `204` en cas de succès.

**Comportement**. Supprime le lot et toutes ses unités de stock, en une transaction. Le numéro de lot
n'est pas libéré et ne sera jamais réattribué (FR-013).

| Code | Cas |
|---|---|
| `204` | Lot et unités supprimés |
| `404` | Lot inexistant |
| `409` | Au moins une unité du lot porte une sortie de stock. Le `detail` nomme le nombre d'unités concernées (FR-012) |

---

## `GET /api/stock-units` — filtre ajouté

| Paramètre | Type | Existant |
|---|---|---|
| `batchId` | `integer?` | Oui |
| `status` | `stock_unit_status?` | Oui |
| `productId` | `integer?` | **Nouveau** |

Les filtres se combinent. Ajout rétrocompatible : l'absence du paramètre laisse le comportement
actuel inchangé. Sert à alimenter la sélection d'unités de la boîte de dialogue de solde.
