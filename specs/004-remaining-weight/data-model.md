# Phase 1 — Modèle de données

**Feature**: Poids encore vendable d'une unité entamée
**Date**: 2026-09-11

**Aucune migration. Aucun changement de schéma. Aucune entité modifiée.**

C'est le point le plus important de ce document : la fonctionnalité n'ajoute aucune colonne.
Le poids encore vendable est une **lecture**, pas un attribut.

---

## 1. Le champ dérivé

| | |
|---|---|
| **Nom** | Poids encore vendable d'une unité de stock |
| **Nature** | Valeur dérivée, calculée à chaque lecture |
| **Formule** | `poids pesé de l'unité − somme des poids vendus de ses mouvements de type vente` |
| **Plancher** | Zéro. Jamais négatif (FR-003) |
| **Absence** | `null` si l'unité n'a pas de poids pesé (produit à la pièce, ou unité pas encore pesée) — FR-004 |
| **Persistance** | Aucune. Ni colonne, ni vue matérialisée, ni cache (FR-002) |
| **Porté par** | Toute unité pesée, quel que soit son statut. Égal au poids pesé quand rien n'a été vendu (FR-005) |

### Ce qui entre dans la soustraction, et ce qui n'y entre pas

| Mouvement rattaché à l'unité | Retranché du poids pesé ? |
|---|---|
| Vente en une fois | Oui, mais l'unité quitte alors le stock |
| Vente à la tranche | **Oui** — c'est le cas qui motive la fonctionnalité |
| Usage perso | Non |
| Perte | Non |

Seul le type vente compte. Une sortie perso ou une perte **finalise** l'unité : son statut devient
`personal` ou `lost`, elle quitte le stock et n'est plus affichée. Retrancher son poids ne
servirait à rien et masquerait une erreur de filtre derrière un restant faussement nul.

---

## 2. Une règle, un seul endroit

Le calcul vit dans `StockMovementRules`, où il tourne déjà depuis la v0.8 sous le nom
`ComputeOutcomeWeight`. Il est renommé `ComputeRemainingWeight` et gagne un second appelant.

| Appelant | Ce qu'il en fait |
|---|---|
| Enregistrement d'une sortie perso ou perte | Le poids à **inscrire** sur le mouvement (`data-model.md` §3.8) |
| Lecture d'une unité de stock *(nouveau)* | Le poids à **afficher** comme encore vendable |

Deux usages, un calcul. C'est ce partage qui justifie le renommage : l'ancien nom décrivait le
premier appelant, pas la valeur calculée.

---

## 3. Ce que RG-05 devient

La rédaction actuelle interdit ce que la fonctionnalité affiche. Elle doit être révisée, pas
remplacée.

**Avant** — le poids restant d'une unité entamée n'est pas suivi, aucun champ ni affichage dédié ;
seule la somme des ventes rattachées est significative. Un garde-fou à l'écriture empêche la somme
des poids vendus de dépasser le poids pesé.

**Après** — le poids restant d'une unité entamée n'est **jamais stocké** : aucune colonne, aucune
valeur mise en cache, aucune donnée à maintenir en cohérence. Il est **calculé à la demande** à
partir du poids pesé et des poids vendus, et il **peut être affiché** comme tel. Le garde-fou à
l'écriture est inchangé, et reste la seule protection contre une vente au-delà du poids pesé.

Le mot qui a bougé est *suivi*. L'intention d'origine était de ne pas créer une donnée à
synchroniser, et elle est intégralement préservée. Le garde-fou faisait déjà cette soustraction
depuis la v0.7 sans que personne y voie une contradiction.

Cette révision redescend dans trois documents, dans le même lot que le code (principe IV) :
`docs/PRD.md` §7 pour la règle elle-même, `docs/data-model.md` §3.5 pour le champ dérivé, et
`CLAUDE.md` §8 pour la règle métier 4, dont la phrase « le poids restant n'est pas suivi » devient
fausse.

---

## 4. Entités touchées

| Entité | Changement |
|---|---|
| `stock_unit` | **Aucun.** Gagne une lecture dérivée, pas un attribut |
| `stock_movement` | **Aucun.** C'est la source de la soustraction, déjà en place |
| `product` | **Aucun** |
| `production_batch` | **Aucun** |

---

## 5. Points d'extension laissés ouverts

- **Alertes de fin de jambon** (« il reste moins de 500 g ») : le champ est exactement la donnée
  dont une alerte aurait besoin. Reporté en Vague 2 avec les autres alertes.
- **Raccourci de clôture** sur un restant nul : la mention « à clôturer » est le point d'accroche
  naturel. Décorative dans cette vague (D9 de `research.md`).
- **Historique du restant** (comment un jambon s'est vidé) : reconstituable depuis les mouvements,
  qui portent tous leur date. Rien à prévoir, rien à stocker.
- **Produits à parer** (avec os ou chute de découpe) : rouvriraient l'hypothèse d'exactitude du
  chiffre, et donc FR-008. Le champ resterait juste, c'est son libellé qui devrait nuancer.
