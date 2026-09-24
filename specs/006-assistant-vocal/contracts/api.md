# Contrat d'API — Assistant vocal

**Feature**: `specs/006-assistant-vocal` | **Date**: 2026-09-23

Propriétés en `camelCase`, enums en `snake_case` (`Program.cs`, `CLAUDE.md` §9). Erreurs au format
`ProblemDetails`, message en français dans `detail`. Toutes les routes exigent un compte connecté et
actif ; celles de l'assistant exigent en plus qu'il soit activé pour le compte (politique
`AssistantEnabled`, research R-03).

Codes communs aux routes de l'assistant :

| Code | Quand |
|---|---|
| `401` | Pas connecté, ou compte désactivé |
| `403` | Assistant non activé pour ce compte — « L'assistant vocal n'est pas activé pour ton compte. » |
| `429` | Limite du compte atteinte (FR-023) — « Tu as fait beaucoup de demandes : réessaie dans quelques minutes. » La demande est journalisée (`rate_limited`) |
| `503` | Service extérieur indisponible ou non configuré (FR-027). La demande est journalisée (`error`) |

---

## Assistant

### `POST /api/assistant/voice` — demande dictée

Corps `multipart/form-data`, champ `audio` : l'enregistrement du téléphone (`audio/webm`, avec ou sans
`;codecs=opus`, `audio/ogg`, `audio/mp4`). 5 Mo au plus.

→ `200` [`AssistantReply`](#assistantreply) · `400` enregistrement vide · codes communs.

### `POST /api/assistant/text` — demande écrite

```json
{ "text": "il me reste combien de saucissons ?" }
```

→ `200` [`AssistantReply`](#assistantreply) · `400` texte vide · codes communs.

### `GET /api/assistant/requests/{id}/speech` — voix de la réponse *(nouvelle)*

Rend la phrase de réponse de la demande `{id}`, lue par la voix de Mistral : `200`, `audio/mpeg`.
N'accepte qu'une demande **du compte connecté**, de **moins de 10 minutes**, qui a une phrase de
réponse ; sinon `404`. Aucun texte ne peut être passé en paramètre (FR-020, research R-05).

→ `200` MP3 · `404` · codes communs.

### Retirée : `POST /api/assistant/speech`

La route du spike qui lisait un texte libre disparaît. Elle n'a jamais été publiée : aucune rupture
de contrat.

### `AssistantReply`

```jsonc
{
  "requestId": 128,                 // identifiant de la demande, pour la voix (nouveau)
  "kind": "sale_draft",             // stock_answer | sale_draft | not_understood
  "heard": "Vends deux saucissons à madame Martin.",
  "speech": "Voilà la vente, vérifie-la avant d'enregistrer.",
  "stock": null,                    // ProductStock[] pour une question de stock
  "draft": {                        // SaleDraft pour une vente
    "customerId": 42,               // null : client à choisir
    "paid": false,
    "lines": [
      { "stockUnitId": 18, "isFullSale": true, "soldWeight": null },
      { "stockUnitId": 25, "isFullSale": false, "soldWeight": 0.2 }   // tranche, kg ; null : poids à saisir
    ],
    "warnings": ["Client à choisir : « madame Petitjean » n'a pas été reconnu."]
  }
}
```

Changements par rapport au spike : `requestId` ajouté ; `kind` passe de `answer` à `stock_answer`,
aligné sur l'issue journalisée. Aucun montant dans un brouillon (FR-016).

`ProductStock` : `code`, `name`, `saleMode`, `wholeCount`, `openedCount`, `remainingKg` (null à la
pièce), `oldestDate`, `batches[]` (`productionDate`, `salePrice`, `count`, `remainingKg`),
`opened[]` (`unitNumber`, `productionDate`, `remainingKg`). Inchangé.

---

## Comptes et session

### `GET /api/auth/me` · `GET /api/accounts`

Le compte gagne `assistantEnabled: boolean`. Le frontend s'en sert pour proposer ou non « Dicter »
(FR-001) ; le serveur, lui, refuse (`403`).

### `PUT /api/accounts/{id}` (administrateur)

`UpdateAccountRequest` gagne `assistantEnabled: boolean` (facultatif : absent, la valeur ne change
pas). Effet à la requête suivante du compte (FR-026).

---

## Rapports (administrateur)

### `GET /api/reports/assistant?from=2026-09-01&to=2026-09-30` — usage *(nouvelle)*

Période **obligatoire** (`from`, `to`, sinon `400`), en jours de Paris (`BusinessTime`), comme les autres
rapports ; c'est l'écran Rapports qui propose l'année en cours.

```jsonc
[
  {
    "accountId": "…", "accountName": "Gérard",
    "weekStart": "2026-09-21",      // lundi, heure de Paris
    "requests": 12,
    "stockAnswers": 5, "saleDrafts": 6, "notUnderstood": 1, "errors": 0, "rateLimited": 0,
    "medianDurationMs": 1480            // null si aucune demande traitée ; les refus par la limite n'y comptent pas
  }
]
```

### `GET /api/reports/assistant/requests?from&to&accountId&limit=50` — détail *(nouvelle)*

Période obligatoire ; `limit` entre 1 et 200. Les demandes les plus récentes d'abord : `id`, `occurredAt`, `accountName`, `inputMode`,
`heardText`, `outcome`, `replySpeech`, `durationMs`. Sert à comprendre un raté (FR-025).

> Les issues sont des champs nommés et non un dictionnaire : un enum en clé de dictionnaire ne suit pas
> la sérialisation `snake_case` du reste de l'API (`CLAUDE.md` §9).
