# Phase 1 — Modèle de données

**Feature**: Corriger et supprimer une vente
**Date**: 2026-09-11

**Aucun changement de schéma.** Aucune table, colonne, contrainte ni migration n'est ajoutée ou
modifiée. Ce document décrit les entités telles qu'elles existent, sous l'angle de ce que l'écran
de correction en lit et en écrit. La référence reste `docs/data-model.md` v0.7.

---

## 1. Entités touchées, en écriture

### `sale` — l'en-tête

| Champ | Corrigeable | Contrainte |
|---|---|---|
| `sale_number` | ❌ | immuable (FR-012) ; généré à la création, jamais réécrit par `UpdateAsync` |
| `customer_id` | ✅ | obligatoire (RG-07) ; un client inexistant fait échouer la correction (`409`) |
| `date` | ✅ | `timestamptz` ; ne régénère pas le numéro, qui reste celui du jour de saisie |
| `paid` | ✅ | corrigeable depuis l'en-tête, en plus de la bascule rapide existante |
| `notes` | ✅ | 2000 caractères maximum |
| `updated_at` | — | écrit par le serveur |
| `total`, `item_count` | ❌ | dérivés des lignes, jamais saisis (RG-05, FR-009) |

### `stock_movement` — la ligne de vente

| Champ | Corrigeable | Contrainte |
|---|---|---|
| `sold_weight` | ✅ si produit au poids | requis et positif au poids, interdit à la pièce ; la somme des poids vendus d'une unité ne peut dépasser son poids pesé |
| `amount` | ✅ | requis et positif pour une vente ; conservé tel que saisi |
| `notes` | ✅ | 2000 caractères maximum |
| `type`, `stock_unit_id`, `sale_id`, `date` | ❌ | non exposés à la correction |

`PUT /api/stock-movements/{id}` remplace les trois champs corrigeables d'un coup : l'écran renvoie
donc toujours les trois, y compris ceux qu'il n'a pas modifiés.

### `stock_unit` — l'objet physique

Jamais écrite directement par cette fonctionnalité. Son `status` est recalculé par le serveur,
selon une règle unique : **une unité qui ne porte plus aucun mouvement repasse à `available`.**

---

## 2. Transitions de statut d'une unité, déclenchées indirectement

| Situation avant | Geste | Statut après |
|---|---|---|
| `sold`, un seul mouvement (vente en entier) | retrait de la ligne, ou suppression de la vente | `available` |
| `opened`, plusieurs ventes partielles | retrait d'une ligne parmi d'autres | inchangé (`opened`) |
| `sold` après clôture manuelle, plusieurs mouvements | retrait d'une ligne | inchangé (`sold`) — une unité clôturée ne rouvre pas |
| `sold`, mais l'unité porte aussi une sortie perso ou perte | retrait de la ligne de vente | inchangé |

Ces transitions sont celles du serveur. L'interface ne les anticipe pas et ne les reproduit pas :
elle recharge et affiche ce que le serveur renvoie (FR-011).

---

## 3. Suppression d'une vente : effet en cascade

`DELETE /api/sales/{id}` supprime la vente **et toutes ses lignes** dans une seule transaction,
puis applique la règle du §2 à chaque unité concernée. Il n'existe pas d'état intermédiaire où une
vente survivrait sans ligne : la contrainte du §4 le garantit dans les deux sens.

---

## 4. Invariant conservé par l'interface

**Une vente porte au moins une ligne.** Le serveur refuse le retrait de la dernière (`409`), et
l'écran n'offre aucun chemin pour vider une vente autrement. Une vente devenue inutile se supprime,
elle ne se dépeuple pas.

---

## 5. Vue de l'écran (état local, non persisté)

| État | Portée | Rôle |
|---|---|---|
| mode édition de l'en-tête | écran | ouvert par « Corriger », fermé par « Enregistrer » ou « Annuler » (D1) |
| brouillon d'en-tête | écran | copie de `customer_id`, `date`, `paid`, `notes` ; abandonnée sans effet (FR-002) |
| ligne en cours de correction | boîte de dialogue | numéro d'étiquette, montant, poids vendu (D2) |
| confirmation en attente | boîte de dialogue | retrait d'une ligne, ou suppression de la vente (FR-008) |
| message de refus | par geste | texte du serveur, affiché sans écraser le brouillon (D5) |

Aucun de ces états ne survit à un rechargement de page, et c'est voulu : il n'existe pas de
brouillon persistant, une correction non validée est une correction non faite.
