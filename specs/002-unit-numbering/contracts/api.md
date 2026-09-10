# Phase 1 — Contrat REST

**Feature**: Le numéro d'étiquette porté par l'unité
**Date**: 2026-09-10

Le contrat est publié en OpenAPI par le backend. Ce document décrit ce que la fonctionnalité change,
et sert de référence pour aligner `frontend/src/api/`.

---

## Résumé des changements

| Élément | Nature | Rupture |
|---|---|---|
| `StockUnitDto.batchNumber` | Remplacé par `unitNumber` | **Oui** |
| `StockMovementDto.batchNumber` | Remplacé par `unitNumber` | **Oui** |
| `ProductionBatchDto.batchNumber` | Supprimé | **Oui** |
| Aucun endpoint | Ni ajout, ni suppression, ni changement de route | — |

Aucune route ne change. La rupture porte uniquement sur la forme des ressources, ce qui suffit à
imposer un `!` sur le commit backend et un déploiement conjoint des deux applications (principe III
de la constitution).

---

## `StockUnitDto`

```jsonc
{
  "id": 41,
  "batchId": 7,
  "unitNumber": "SC-260910-7",  // remplace batchNumber
  "weight": 0.32,
  "status": "available"
}
```

`batchId` reste exposé : le frontend en a besoin pour regrouper les unités par fournée. Ce qui
disparaît est le numéro de la fournée, pas son identifiant technique.

---

## `StockMovementDto`

```jsonc
{
  "id": 88,
  "stockUnitId": 41,
  "productName": "Saucisse curry",
  "productIsActive": true,
  "unitNumber": "SC-260910-7",  // remplace batchNumber
  "type": "sale",
  "date": "2026-09-12T09:30:00Z",
  "soldWeight": 0.32,
  "amount": 6.40,
  "saleId": 12,
  "saleNumber": "V-260912-1"
}
```

Le mouvement nomme désormais l'unité par l'étiquette que l'utilisateur a sous les yeux (FR-009).

---

## `ProductionBatchDto`

Le champ `batchNumber` disparaît. La ressource conserve `id`, `productId`, `productName`,
`productionDate`, `salePrice`, `rawMaterialRef`, `expiryDate` et `notes`.

Le rang d'une fournée dans sa journée n'est **pas** exposé : c'est un libellé d'affichage que le
frontend calcule à partir des fournées qu'il a déjà reçues, triées par date de production (D7).

---

## Erreurs

Aucun nouveau code d'erreur. Le refus de supprimer une fournée dont des unités sont sorties reste un
`409`, dont le message cesse simplement de nommer un numéro de lot.
