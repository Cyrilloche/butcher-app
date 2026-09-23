# Data Model — Assistant vocal

**Feature**: `specs/006-assistant-vocal` | **Date**: 2026-09-23

Une migration : une colonne sur `app_user`, une table `voice_request`. Aucune entité métier existante
ne change : l'assistant lit le stock et prépare des brouillons, il n'écrit rien (FR-009).

---

## 1. `app_user` — activation de l'assistant

| Colonne | Type | Contraintes | Rôle |
|---|---|---|---|
| `assistant_enabled` | `boolean` | non nul, défaut `false` | L'assistant est proposé à ce compte (FR-001, FR-022, FR-026) |

- Faux par défaut : un nouveau compte n'a pas l'assistant ; la migration le laisse à faux pour les
  comptes existants. L'administrateur l'active dans l'écran Comptes.
- Relu en base à chaque demande par la politique `AssistantEnabled` (research R-03).
- Sa modification est tracée au journal des gestes comme toute modification de compte.

## 2. `voice_request` — journal des demandes à l'assistant (nouvelle)

Une ligne par demande, quelle qu'en soit l'issue. Jamais l'audio (FR-024).

| Colonne | Type | Contraintes | Rôle |
|---|---|---|---|
| `id` | `bigint` | clé, auto-incrémentée | Identifiant ; sert aussi à demander la voix de la réponse (R-05) |
| `account_id` | `uuid` | non nul, FK → `app_user` | Auteur de la demande |
| `occurred_at` | `timestamptz` | non nul | Réception de la demande |
| `input_mode` | `voice_input_mode` | non nul | `voice` (dictée) ou `text` (écrite au clavier) |
| `heard_text` | `text` | nullable | Phrase entendue (ou écrite) ; `null` si la transcription a échoué ou si la limite était atteinte |
| `outcome` | `voice_request_outcome` | non nul | Issue de la demande (ci-dessous) |
| `reply_speech` | `text` | nullable | Phrase de réponse, telle que dite ; `null` en erreur ou limite atteinte |
| `duration_ms` | `integer` | non nul, ≥ 0 | Durée de traitement par le serveur, de la réception à la réponse |

**Index** : `(account_id, occurred_at)` — compte des demandes de l'heure (limite, R-04) et rapports
par compte et par période (R-08).

**Immuable** : aucune route ne modifie ni ne supprime une demande. Pas de purge automatique, comme
le journal des gestes.

### Enum `voice_request_outcome`

| Valeur (code) | Affichage | Quand |
|---|---|---|
| `stock_answer` | « Stock » | Réponse à une question de stock |
| `sale_draft` | « Vente » | Brouillon de vente préparé (y compris avec une réponse de stock dans la même phrase) |
| `not_understood` | « Pas compris » | Ni stock ni vente, ou demande inexploitable |
| `error` | « Erreur » | Service extérieur indisponible, transcription impossible, erreur interne |
| `rate_limited` | « Limite atteinte » | Refusée par la limite du compte, sans appel extérieur |

### Enum `voice_input_mode`

| Valeur (code) | Affichage |
|---|---|
| `voice` | « Dictée » |
| `text` | « Écrite » |

Sérialisés en `snake_case`, comme tous les enums (`CLAUDE.md` §9) ; libellés français ajoutés à la
table de correspondance de `docs/data-model.md` §4.2.

## 3. Objets non persistés

Rappel des formes échangées, détaillées dans [contracts/api.md](./contracts/api.md) :

- **Réponse de l'assistant** : issue, phrase entendue, phrase à dire, identifiant de la demande,
  réponse de stock ou brouillon de vente.
- **Brouillon de vente** : client (ou aucun), payé ou non, unités proposées (entière, ou tranche avec
  son poids), avertissements. **Aucun montant** : le formulaire le calcule (FR-016).
- **Réponse de stock** : par produit, unités intactes et entamées, poids encore vendable, fournées
  (date, prix, nombre, poids), unités entamées (numéro, poids restant). Même calcul que les écrans de
  stock (`ComputeRemainingWeight`).

## 4. Données transmises à l'extérieur

| Donnée | Transcription | Compréhension | Voix lue |
|---|---|---|---|
| Audio de la demande | ✅ | ❌ | ❌ |
| Noms de clients | ✅ (dans l'audio) | ❌ (jetons) | ❌ |
| Liste des clients | ❌ | ❌ | ❌ |
| Catalogue (codes, noms) | ❌ | ✅ | cités dans la phrase |
| Chiffres de stock | ❌ | ❌ | cités dans la phrase |

Fondement : ADR-012 et clarifications de la spec. `heard_text` peut contenir un nom de client : il
reste dans la base de l'application (clarification Q1).
