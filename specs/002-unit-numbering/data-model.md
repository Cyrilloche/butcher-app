# Phase 1 — Modèle de données

**Feature**: Le numéro d'étiquette porté par l'unité
**Date**: 2026-09-10

Ce document décrit ce que la fonctionnalité change. Le modèle complet reste `docs/data-model.md`,
qui sera mis à jour dans le même lot de travail.

---

## 1. `stock_unit` — ajout de `unit_number`

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `unit_number` | `varchar` | non nul, **unique** | Le numéro recopié à la main sur l'étiquette (FR-001, FR-005) |

**Format** : `CODE-YYMMDD-N`. `CODE` est le code du produit tel qu'il est au moment de l'attribution,
`YYMMDD` la date de production de la fournée d'origine, `N` un entier sans rembourrage (FR-002).

**Règles** :

- Écrit à la création de l'unité, jamais ensuite (FR-006). Aucun chemin de mise à jour ne l'expose.
- Le code figé du produit garantit qu'un numéro déjà écrit ne peut pas devenir faux : le code n'est
  modifiable que tant qu'aucune fournée n'existe, donc qu'aucun numéro n'a été émis.
- La suppression d'une unité ou de sa fournée n'affecte pas le registre : le rang reste consommé.

---

## 2. `production_batch` — suppression de `batch_number`

La colonne et son index unique disparaissent (D2). La fabrication garde tout le reste : produit,
date de production, prix, date limite de consommation, matière première, notes.

**Conséquence sur la génération** : il n'y a plus de numéro à composer à la création d'une
fabrication. Le service perd sa dépendance au registre, qui passe côté unités.

---

## 3. `batch_number_sequence` → `unit_number_sequence`

| Attribut | Type | Contraintes | Rôle |
|---|---|---|---|
| `product_id` | `integer` | PK (avec `production_date`), FK → `product` | Produit concerné |
| `production_date` | `date` | PK (avec `product_id`) | Journée de production |
| `last_sequence` | `integer` | non nul | Dernier rang **d'unité** émis pour ce couple |

Même structure, même clé, sémantique déplacée des fournées vers les unités (D3). La ligne n'est
jamais supprimée : c'est ce qui garantit qu'un rang n'est jamais réattribué (FR-004, FR-012).

**Attribution d'un bloc de rangs** (D4, D5) :

1. Ouvrir une transaction.
2. Lire la ligne du couple produit / date **avec un verrou de ligne**, la créer à zéro si absente.
3. Ajouter le nombre d'unités demandées à `last_sequence`.
4. Numéroter les unités sur la plage ainsi réservée, dans leur ordre de création.
5. Écrire les unités et valider la transaction.

---

## 4. Migration

Une seule migration, en quatre gestes, dans cet ordre :

1. Renommer `batch_number_sequence` en `unit_number_sequence`.
2. Ajouter `unit_number` sur `stock_unit`, d'abord nullable.
3. Rétro-remplir (D6) : pour chaque produit, parcourir ses fournées par date de production
   croissante puis ses unités par identifiant croissant, en attribuant les rangs à partir de 1 par
   couple produit / date. Puis réécrire `last_sequence` sur le dernier rang attribué, en remplaçant
   le comptage de fournées qui s'y trouvait.
4. Passer `unit_number` en non nul, poser l'index unique, puis supprimer
   `production_batch.batch_number` et son index.

La migration inverse retire la colonne, restaure `batch_number` et rend au registre son ancien nom.
Elle ne prétend pas retrouver les numéros de fournée d'origine : elle existe pour permettre un
retour en arrière en développement, pas une exploitation de l'ancien format.

---

## 5. Ce qui ne change pas

- Le statut de l'unité reste la source de vérité de l'état de stock.
- Le prix reste porté par la fournée. Une unité n'a pas de prix propre.
- La suppression d'une fournée reste conditionnée à l'absence de sortie de stock sur ses unités.
- Les mouvements de stock ne portent aucun numéro propre : ils héritent de celui de leur unité.
