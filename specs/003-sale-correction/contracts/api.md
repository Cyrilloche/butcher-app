# Phase 1 — Contrat d'API

**Feature**: Corriger et supprimer une vente
**Date**: 2026-09-11

**Aucune rupture de contrat, aucune route nouvelle.** Le commit backend n'existera pas ; le
frontend se contente de consommer cinq routes déjà publiées et déjà décrites par le Swagger. Ce
document fixe ce que l'écran envoie et ce qu'il attend en retour, pour que l'implémentation n'ait
pas à relire les services.

---

## 1. Routes consommées

### `PUT /api/sales/{id}` — corriger l'en-tête

Remplacement complet : les quatre champs sont toujours envoyés.

```jsonc
// UpdateSaleRequest
{
  "customerId": 12,              // requis (RG-07)
  "date": "2026-09-10T00:00:00Z",// requis
  "paid": true,
  "notes": "Réglé en espèces"    // 2000 caractères max, null accepté
}
```

Réponse `200` : `SaleDto` complet, lignes comprises. Le `saleNumber` revient inchangé (FR-012).

Refus possibles :

| Statut | Cause | Traitement |
|---|---|---|
| `409` | le client n'existe pas ou a été supprimé | afficher le message, garder le brouillon |
| `404` | la vente a été supprimée entre-temps | afficher le message |
| `400` | notes trop longues, champ requis manquant | afficher le message |

### `POST /api/sales/{id}/payment` — bascule rapide du paiement

Conservée telle quelle pour le bouton « Marquer comme payée » de l'écran de lecture. La correction
de l'en-tête passe, elle, par `PUT` : les deux chemins coexistent sans se contredire, `paid` étant
porté par les deux requêtes.

### `DELETE /api/sales/{id}` — supprimer la vente

Réponse `204`, sans corps. Supprime les lignes et libère les unités qui ne portent plus de
mouvement. Refus : `404` si la vente n'existe plus.

### `PUT /api/stock-movements/{id}` — corriger une ligne

Remplacement complet, là aussi : les trois champs sont toujours envoyés, y compris ceux que la
boîte de dialogue n'a pas modifiés.

```jsonc
// UpdateStockMovementRequest
{
  "soldWeight": 0.85,  // requis et positif au poids ; à omettre (null) pour un produit à la pièce
  "amount": 18.50,     // requis et positif pour une vente
  "notes": null
}
```

Réponse `200` : `StockMovementDto`. Attention, la réponse porte la ligne seule : le total de la
vente n'y figure pas, d'où le rechargement de la vente (FR-009, D7).

Refus possibles :

| Statut | Cause | Message serveur (français, affiché tel quel) |
|---|---|---|
| `409` | le poids vendu ferait dépasser le poids pesé de l'unité | « La somme des poids vendus (x,xxx kg) dépasserait le poids de l'unité (y,yyy kg). » |
| `400` | poids absent ou nul sur un produit au poids | « « SoldWeight » est requis et doit être positif pour un produit vendu au poids. » |
| `400` | poids fourni sur un produit à la pièce | « « SoldWeight » n'est pas applicable pour un produit vendu à la pièce. » |
| `400` | montant absent ou nul | « « Amount » est requis et doit être positif pour une vente. » |

### `DELETE /api/stock-movements/{id}` — retirer une ligne

Réponse `204`. Refus :

| Statut | Cause | Message serveur |
|---|---|---|
| `409` | c'est la dernière ligne de la vente | « C'est la dernière ligne de la vente : supprimez la vente elle-même plutôt que cette ligne. » |
| `404` | la ligne n'existe plus | — |

---

## 2. Client HTTP frontend

`frontend/src/api/sales.ts` couvre déjà les trois routes de vente. Deux fonctions manquent dans
`frontend/src/api/stockMovements.ts` :

```ts
updateStockMovement(id: number, payload: UpdateStockMovementRequest): Promise<StockMovementDto>
deleteStockMovement(id: number): Promise<void>
```

Les types `UpdateStockMovementRequest` et `UpdateSaleRequest` existent déjà dans
`frontend/src/api/types.ts` et correspondent aux DTO ci-dessus. Aucun type nouveau n'est requis.

---

## 3. Ce que l'écran lit

`GET /api/sales/{id}` fournit tout ce dont la correction a besoin, sans appel supplémentaire :
`customerId`, `customerName`, `date`, `paid`, `notes`, `total`, `itemCount`, et pour chaque ligne
`id`, `unitNumber`, `productName`, `productIsActive`, `soldWeight`, `amount`.

Une donnée manque, volontairement : le **prix au kilo** du lot. C'est ce qui rend impossible — et
inutile — tout recalcul de montant côté client (décision D3, RG-05).

La liste des clients vient de `GET /api/customers`, comme dans l'écran de saisie d'une vente.
