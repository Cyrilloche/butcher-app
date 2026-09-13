# API Contract — Backoffice PC, comptes nominatifs et rôles

**Feature**: `specs/005-backoffice` | **Date**: 2026-09-12

Toutes les routes exigent une authentification (politique par défaut *fail-closed*, ADR-009), sauf
mention contraire. Les erreurs suivent le format `ProblemDetails` existant, avec un `detail` en
français. Nouveau code : **`403`** pour une action réservée à l'administrateur.

---

## 1. Compte courant (lot 1)

### `GET /api/auth/me`

Compte connecté, relu en base.

```json
200 { "id": "uuid", "email": "mireille@saloir.local", "displayName": "Mireille", "role": "user" }
```

`401` si le compte est désactivé.

### `POST /api/auth/change-password`

```json
{ "currentPassword": "…", "newPassword": "…" }
```

`204` ; `400` si le nouveau mot de passe ne respecte pas la politique du rôle (FR-034), ou si le
mot de passe actuel est faux — une erreur de saisie, pas une session invalide : un `401` ferait
tenter au client un rafraîchissement de session inutile. Révoque les sessions des **autres**
appareils ; la session en cours est conservée (FR-006).

### Changements sur l'existant

- `POST /api/auth/login`, `POST /api/auth/refresh` : `401` pour un compte désactivé, avec le
  message « Ce compte est désactivé. ». Réponse inchangée.

## 2. Gestion des comptes — administrateur (lot 1)

Toutes ces routes : `403` pour un compte `user`.

| Méthode | Route | Corps | Réponse |
|---|---|---|---|
| `GET` | `/api/accounts` | — | `200 AccountDto[]`, actifs et désactivés |
| `POST` | `/api/accounts` | `CreateAccountRequest` | `201 AccountDto` ; `409` email déjà pris ; `400` mot de passe non conforme |
| `PUT` | `/api/accounts/{id}` | `UpdateAccountRequest` | `200 AccountDto` ; `409` dernier admin ; `400` promotion sans mot de passe conforme |
| `POST` | `/api/accounts/{id}/deactivate` | — | `204` ; `409` dernier admin actif, ou soi-même |
| `POST` | `/api/accounts/{id}/reactivate` | — | `204` |
| `POST` | `/api/accounts/{id}/reset-password` | `{ "newPassword": "…" }` | `204` ; `400` non conforme |

```jsonc
// AccountDto
{
  "id": "uuid",
  "email": "mireille@saloir.local",
  "displayName": "Mireille",
  "role": "user",            // "admin" | "user"
  "isActive": true,
  "lastLoginAt": "2026-09-12T18:04:00Z", // ou null
  "createdAt": "2026-09-12T17:50:00Z"
}

// CreateAccountRequest
{ "email": "…", "displayName": "…", "role": "user", "password": "…" }

// UpdateAccountRequest — newPassword obligatoire si role passe de "user" à "admin" (FR-035)
{ "displayName": "…", "role": "admin", "newPassword": "…" }
```

## 3. Gestes réservés — changements sur l'existant (lot 1, US2)

| Route | Avant | Après |
|---|---|---|
| `POST /api/products/{id}/deactivate` | tout compte | **administrateur**, sinon `403` |
| `POST /api/products/{id}/reactivate` | tout compte | **administrateur**, sinon `403` |
| `POST /api/products/{id}/write-off` | tout compte | **administrateur**, sinon `403` |

Toutes les autres routes, **y compris les suppressions** de vente, ligne, fournée et unité, restent
ouvertes à tout compte actif (FR-011, clarification du 2026-09-12).

## 4. Auteur — ajout additif aux DTO de lecture (lot 1)

`SaleDto`, `ProductionBatchDto`, `StockMovementDto` gagnent :

```jsonc
"createdByName": "Mireille" // nom affiché de l'auteur, ou null (avant comptes nominatifs)
```

Ajout sans rupture : aucun champ renommé ni retiré.

## 5. Lots ultérieurs (esquisse, à détailler au démarrage de chaque lot)

- **US3** : `GET /api/sales` accepte déjà `customerId`, `paid`, `from`, `to` — aucun changement.

## 6. Journal — administrateur (lot US4)

### `GET /api/audit-entries`

Paramètres, tous facultatifs : `accountId` (uuid), `entityType`, `action`, `from`, `to` (dates
`YYYY-MM-DD` incluses, fuseau `Europe/Paris`), `page` (défaut 1), `pageSize` (défaut 50, 200 au
plus). Du plus récent au plus ancien. `403` pour un compte `user` ; aucune route d'écriture (FR-024).

```jsonc
200 {
  "items": [
    {
      "id": 42,
      "occurredAt": "2026-09-13T19:15:00Z",
      "accountId": "uuid",          // null : connexion sur adresse inconnue, commande hors ligne
      "accountName": "Mireille",    // null si accountId est null
      "action": "deleted",          // created | updated | deleted | login_succeeded | login_failed | locked_out | password_changed
      "entityType": "sale",         // product | production_batch | stock_unit | sale | stock_movement | customer | account, ou null
      "entityId": "17",
      "entityLabel": "V-260913-2 (3 lignes)",
      "deletedContent": { /* objet, seulement pour deleted */ }
    }
  ],
  "total": 318,
  "page": 1,
  "pageSize": 50
}
```

## 7. Rapports — administrateur (lot US5)

Toutes : `403` pour un compte `user`. Montants issus des lignes enregistrées (FR-030). `from` / `to` :
dates `YYYY-MM-DD` incluses, fuseau `Europe/Paris`, obligatoires sauf pour `receivables`.

```jsonc
// GET /api/reports/sales-summary?from=2026-01-01&to=2026-12-31
{ "saleCount": 12, "total": 431.50, "paidTotal": 400.00, "pendingTotal": 31.50,
  "months": [ { "month": "2026-09", "saleCount": 5, "total": 180.00, "paidTotal": 160.00, "pendingTotal": 20.00 } ] }

// GET /api/reports/sales-by-customer?from=…&to=…   (du plus gros total au plus petit)
[ { "customerId": 3, "customerName": "Marie Perrin", "saleCount": 4, "total": 120.00, "pendingTotal": 12.50 } ]

// GET /api/reports/sales-by-product?from=…&to=…    (du plus gros total au plus petit)
[ { "productId": 1, "productCode": "JB", "productName": "Jambon sec", "saleMode": "by_weight",
    "unitCount": 1, "lineCount": 5, "soldWeight": 2.350, "total": 94.00 } ]   // soldWeight null à la pièce

// GET /api/reports/receivables                     (du plus gros montant dû au plus petit)
{ "total": 31.50,
  "customers": [ { "customerId": 3, "customerName": "Marie Perrin", "pendingTotal": 12.50,
                   "oldestUnpaidDate": "2026-08-02T10:00:00Z",
                   "sales": [ { "id": 17, "saleNumber": "V-260802-1", "date": "2026-08-02T10:00:00Z", "total": 12.50 } ] } ] }
```
