# Phase 1 — Guide de validation

**Feature**: Le numéro d'étiquette porté par l'unité
**Date**: 2026-09-10

Comment vérifier à la main que la fonctionnalité tient ses promesses. Les règles sont dans
[spec.md](./spec.md), le contrat dans [contracts/api.md](./contracts/api.md).

---

## Prérequis

La configuration de développement passe par `development/.env` et le `Makefile`, jamais par
`dotnet user-secrets`.

```bash
make db-up        # PostgreSQL + pgAdmin
make migrate      # applique la migration de renumérotation
make run          # API sur http://localhost:5045, Swagger exposé
```

Frontend, dans un second terminal :

```bash
cd frontend && npm run dev
```

Le jeu d'essai actuel peut être conservé : la migration le renumérote. Pour repartir de zéro,
réinitialiser la base de développement avant `make migrate`.

---

## Suite automatisée

```bash
make test         # xUnit, base PostgreSQL réelle
```

Sous Docker Desktop avec intégration WSL, si Testcontainers échoue à démarrer son conteneur de
nettoyage, utiliser `TESTCONTAINERS_RYUK_DISABLED=true make test`.

Les points qui doivent être couverts par la suite : l'attribution d'un bloc de rangs, la continuité
entre deux fournées d'un même jour, la reprise après suppression d'une fournée, l'indépendance des
suites entre deux produits, et le rétro-remplissage de la migration.

---

## Scénario 1 — Un numéro par objet

Couvre User Story 1, FR-001 à FR-003.

1. Créer un produit de code `SC`, vendu au poids.
2. Créer une fournée du jour, y générer dix unités pesées.
3. **Attendu** : les dix unités portent `SC-<date>-1` à `SC-<date>-10`, chacune sur une ligne, sans
   segment supplémentaire.
4. Créer une seconde fournée le même jour, à un autre prix, y générer cinq unités.
5. **Attendu** : elles portent `-11` à `-15`. La numérotation n'est pas repartie de 1.
6. Créer un autre produit, une fournée le même jour.
7. **Attendu** : sa première unité porte `-1`. Les suites sont indépendantes par produit.

---

## Scénario 2 — Retrouver un objet par son étiquette

Couvre User Story 2, FR-008 et FR-009.

1. Noter le numéro d'une unité, par exemple `SC-<date>-7`.
2. Ouvrir le détail du stock du produit. **Attendu** : l'unité y figure sous ce numéro, avec son
   poids et son statut.
3. La vendre, puis ouvrir la vente. **Attendu** : la ligne nomme l'unité par ce même numéro.
4. Sortir une autre unité en perte, puis consulter l'historique de ses mouvements. **Attendu** :
   même numéro que sur l'étiquette.

---

## Scénario 3 — La fournée sans numéro

Couvre User Story 3, FR-007 et FR-015.

1. Ouvrir la création d'une fabrication. **Attendu** : aucun champ ni aucune mention de numéro de
   lot.
2. Ouvrir le détail du stock d'un produit ayant deux fournées le même jour à des prix différents.
3. **Attendu** : deux groupes, distingués par leur date, leur prix et leur rang dans la journée,
   chacun listant ses unités par leur numéro.
4. Parcourir Stock, Produits et Ventes. **Attendu** : aucun numéro à plus de trois segments, nulle
   part, y compris dans les messages de confirmation et d'erreur.

---

## Scénario 4 — La suppression ne libère aucun numéro

Couvre User Story 4, FR-004, FR-010 et FR-012. **C'est le point le plus exposé aux régressions.**

1. Sur une fournée dont rien n'est sorti du stock, déclencher la suppression et confirmer.
2. **Attendu** : la fournée et ses unités disparaissent, et la confirmation ne nomme aucun numéro
   de lot.
3. Créer une nouvelle fournée le même jour pour le même produit, avec trois unités.
4. **Attendu** : les rangs reprennent **après le dernier émis**, jamais à ceux qui viennent d'être
   supprimés.
5. Supprimer une unité seule pour corriger une pesée, puis en générer une de plus.
6. **Attendu** : le rang libéré n'est pas repris.

---

## Vérification de la migration

À faire une fois, sur une base contenant déjà des fournées et des unités avant migration.

1. Noter le nombre d'unités en base et leurs fournées.
2. Appliquer la migration.
3. **Attendu** : chaque unité a un numéro, aucun doublon, les rangs repartent de 1 par produit et
   par date de production, et le registre porte le dernier rang attribué.
4. Créer une fournée sur une date déjà présente. **Attendu** : la numérotation continue après
   l'existant, sans collision.

---

## Points de vigilance

- **Rupture de contrat** : les deux applications doivent être déployées ensemble. Le commit backend
  porte un `!`.
- **Numéros non contigus** : une fournée pesée en plusieurs fois, entrecoupée d'une autre fournée
  du même jour, aura des numéros à trous. C'est attendu, pas un défaut.
- **Étiquettes déjà écrites** : aucune. Confirmé le 2026-09-10, l'instance déployée ne porte que
  des données de test recréables, l'exploitation réelle ne commençant qu'après la Vague 1. Le
  rétro-remplissage ne contredit donc aucun papier.
