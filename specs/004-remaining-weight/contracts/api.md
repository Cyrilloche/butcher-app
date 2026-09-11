# Phase 1 — Contrat d'API

**Feature**: Poids encore vendable d'une unité entamée
**Date**: 2026-09-11

**Aucun point d'entrée créé, modifié ni supprimé.** Un seul champ s'ajoute à une réponse
existante. L'ajout est additif : un client qui l'ignore continue de fonctionner à l'identique,
donc ce n'est pas une rupture de contrat et le commit ne porte pas de `!` (D4 de `research.md`).

---

## 1. Le champ ajouté

`StockUnitDto` gagne `remainingWeight`.

| | |
|---|---|
| **Nom** | `remainingWeight` |
| **Type** | `decimal?` côté serveur, `number \| null` côté client |
| **Unité** | kilogrammes, précision au gramme (`decimal(10,3)`, comme `weight`) |
| **Sens** | Poids encore vendable de cette unité |
| **`null` quand** | L'unité n'a pas de poids pesé : produit vendu à la pièce, ou unité au poids pas encore pesée |
| **Jamais** | Négatif. Le plancher est `0` |

### Réponse, unité intacte

```json
{
  "id": 41,
  "batchId": 12,
  "unitNumber": "JS-260910-3",
  "weight": 3.000,
  "remainingWeight": 3.000,
  "status": "available"
}
```

### Réponse, unité entamée

```json
{
  "id": 42,
  "batchId": 12,
  "unitNumber": "JS-260910-4",
  "weight": 3.000,
  "remainingWeight": 0.800,
  "status": "opened"
}
```

### Réponse, unité entièrement tranchée mais non clôturée

```json
{
  "id": 43,
  "batchId": 11,
  "unitNumber": "JS-260908-2",
  "weight": 2.800,
  "remainingWeight": 0.000,
  "status": "opened"
}
```

L'unité reste en stock : la clôture est manuelle (RG-04). C'est `remainingWeight` à zéro qui
signale qu'il n'y a plus rien à vendre.

### Réponse, produit vendu à la pièce

```json
{
  "id": 77,
  "batchId": 20,
  "unitNumber": "TR-260906-5",
  "weight": null,
  "remainingWeight": null,
  "status": "available"
}
```

---

## 2. Points d'entrée concernés

Tous ceux qui renvoient déjà un `StockUnitDto`, sans changement de signature ni de paramètres.

| Verbe et route | Effet de la fonctionnalité |
|---|---|
| `GET /api/stock-units` | Chaque élément porte `remainingWeight` |
| `GET /api/stock-units/{id}` | La réponse porte `remainingWeight` |
| `POST /api/production-batches/{batchId}/stock-units` | Les unités créées le portent, égal à leur poids pesé |

Les filtres `batchId`, `status` et `productId` sont inchangés. Aucun filtre sur le restant n'est
ajouté : personne n'a demandé « montre-moi les jambons presque finis », et ce serait une alerte de
Vague 2.

### Points d'entrée volontairement laissés de côté

| Route | Pourquoi |
|---|---|
| `GET /api/stock-movements` | Une ligne de vente porte son propre poids vendu, pas le restant de l'unité. Rien à y ajouter |
| `GET /api/sales/{id}` | Idem : une vente est un instant, pas un état de stock |
| `DELETE /api/stock-units/{id}` | Existe déjà et suffit. C'est ce que la corbeille par unité appelle (D6) |
| `POST /api/stock-units/{id}/close` | Existe déjà. La clôture reste dans le menu à trois points |

---

## 3. Effet sur le client HTTP

`frontend/src/api/types.ts` ajoute `remainingWeight: number | null` à l'interface `StockUnitDto`.

Une suppression s'ensuit, côté composables et non côté contrat : `getRemainingWeightKg` disparaît
(D8 de `research.md`). Elle appelait `GET /api/stock-movements?stockUnitId=` pour refaire la
soustraction côté client, soit une requête par jambon sélectionné dans l'écran de saisie d'une
vente. Ces requêtes disparaissent avec elle.

---

## 4. Contrôles de non-régression du contrat

- Le champ `weight` conserve exactement son sens : le poids **pesé à la fabrication**. Il ne doit
  jamais être remplacé par le restant, sur aucune route.
- Aucun champ existant n'est renommé, retypé ni supprimé.
- Le backend peut être déployé **avant** le frontend : l'ancien client ignore le champ. L'ordre
  inverse est en revanche à éviter, le frontend afficherait des restants absents.
