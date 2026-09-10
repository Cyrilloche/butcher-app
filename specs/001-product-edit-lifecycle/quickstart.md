# Phase 1 — Guide de validation

**Feature**: Modification et fin de vie d'un produit
**Date**: 2026-09-09

Comment vérifier, à la main, que la fonctionnalité tient ses promesses de bout en bout. Le détail des
règles est dans [spec.md](./spec.md), celui du contrat dans [contracts/api.md](./contracts/api.md).

---

## Prérequis

La configuration de développement passe par `development/.env` et le `Makefile`, jamais par
`dotnet user-secrets`.

```bash
make db-up        # PostgreSQL + pgAdmin
make migrate      # applique la migration AddBatchNumberSequence
make run          # API sur http://localhost:5045 (https://localhost:7209), Swagger exposé
```

Frontend, dans un second terminal :

```bash
cd frontend && npm run dev
```

Se connecter avec le compte de seed défini dans `development/.env`.

---

## Suite automatisée

```bash
make test         # xUnit, base PostgreSQL réelle via PostgresDatabaseFixture
```

Sous Docker Desktop avec intégration WSL, Testcontainers peut échouer à démarrer son conteneur de
nettoyage (`ryuk`), avec une erreur `DockerContainerNotFoundException`. Le contournement est
`TESTCONTAINERS_RYUK_DISABLED=true make test` ; les conteneurs de test sont alors à supprimer à la
main s'ils survivent à un plantage.

La suite doit rester verte avant toute release. Les règles métier nouvelles doivent y être couvertes,
en particulier les quatre refus serveur : modification d'un champ figé, suppression d'un lot ayant
une sortie, désactivation avec du stock restant, réémission d'un numéro de lot.

---

## Scénario 1 — Corriger un produit qui n'a jamais servi

Couvre User Story 1, FR-003 et FR-007.

1. Créer un produit « Saucisson » de code `SC`, vendu à la pièce.
2. Ouvrir sa fiche. **Attendu** : les quatre champs sont modifiables, aucun avertissement de gel.
3. Changer le code en `SEC`, le mode en « au poids », activer la vente à la tranche, enregistrer.
4. **Attendu** : la fiche affiche les nouvelles valeurs.
5. Créer un second produit, lui donner le code `sec` en minuscules.
6. **Attendu** : refus, message français nommant le conflit, aucun produit créé.

---

## Scénario 2 — Le gel après le premier lot

Couvre User Story 2, FR-004 et FR-005.

1. Sur le produit `SEC`, créer un lot de production avec trois unités pesées.
2. Rouvrir la fiche produit. **Attendu** : code et mode de vente en lecture seule, avec l'explication
   en français ; nom et vente à la tranche toujours modifiables.
3. Modifier le nom, enregistrer. **Attendu** : accepté, et le numéro du lot déjà émis est inchangé.
4. Contourner l'interface, par Swagger, en envoyant `PUT /api/products/{id}` avec un `code` différent.
5. **Attendu** : `409`, produit inchangé. C'est le point de contrôle du principe II de la
   constitution : l'interface n'est pas le gardien.

---

## Scénario 3 — Supprimer un lot intact, et le refus quand il ne l'est pas

Couvre User Story 3, FR-010 à FR-015.

1. Sur le détail stock du produit, repérer l'en-tête du lot et déclencher la suppression.
2. **Attendu** : la confirmation annonce le nombre d'unités qui disparaîtront.
3. Confirmer. **Attendu** : le lot et ses trois unités disparaissent du stock.
4. Rouvrir la fiche produit. **Attendu** : code et mode de vente redeviennent modifiables (FR-015).
5. Recréer un lot le même jour pour le même produit.
6. **Attendu** : le numéro porte une séquence **non réutilisée**, donc `-2` et non `-1` (FR-013).
   C'est le point le plus facile à casser par une régression : le vérifier explicitement.
7. Vendre une unité de ce nouveau lot, puis tenter de supprimer le lot.
8. **Attendu** : refus, message français nommant le nombre d'unités déjà sorties.

---

## Scénario 4 — Solder le stock restant puis désactiver

Couvre User Story 4, FR-016 à FR-022.

1. Sur un produit ayant trois unités restantes, tenter la désactivation depuis sa fiche.
2. **Attendu** : refus, message indiquant les trois unités restant à écouler, et l'action de solde
   proposée.
3. Ouvrir le solde, ne sélectionner que deux unités, confirmer.
4. **Attendu** : deux sorties de type perte créées, la désactivation reste refusée à cause de la
   troisième (FR-018).
5. Solder la dernière, puis désactiver.
6. **Attendu** : le produit disparaît des listes de choix de la création d'un lot et de la saisie
   d'une vente, et reste visible, signalé comme désactivé, dans les lots et ventes historiques.
7. Créer un produit reprenant le code du produit désactivé.
8. **Attendu** : refus, un code distinct est demandé (FR-007).

### Vérification du poids soldé

Sur une unité entamée, dont une partie a déjà été vendue, la sortie de perte doit porter le
**restant estimé**, soit le poids pesé moins ce qui a été vendu, et non le poids pesé entier
(FR-020). À vérifier dans l'historique des mouvements de l'unité.

---

## Points de vigilance

- **Migration** : la table de séquences doit être initialisée depuis les lots existants, sinon le
  premier lot créé après la migration réémet un numéro déjà porté. Voir
  [data-model.md](./data-model.md) §6.
- **Rupture de contrat** : `PUT /api/products/{id}` gagne deux champs requis. Le frontend doit être
  déployé avec le backend correspondant, et le commit porter la marque `!`.
- **Édition concurrente** : la dernière écriture l'emporte, sans avertissement. C'est une décision
  assumée (FR-028), pas un défaut à corriger si le cas est observé.
