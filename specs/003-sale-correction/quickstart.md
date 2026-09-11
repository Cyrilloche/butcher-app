# Phase 1 — Guide de validation

**Feature**: Corriger et supprimer une vente
**Date**: 2026-09-11

Comment vérifier à la main que la fonctionnalité tient ses promesses. Les exigences sont dans
[spec.md](./spec.md), le contrat dans [contracts/api.md](./contracts/api.md).

La validation est manuelle : la fonctionnalité est entièrement d'interface, et les règles qu'elle
rend atteignables sont déjà couvertes par les tests backend (voir la décision D0 de
[research.md](./research.md)).

---

## Prérequis

La configuration de développement passe par `development/.env` et le `Makefile`, jamais par
`dotnet user-secrets`.

```bash
make db-up     # PostgreSQL + pgAdmin
make run       # API sur http://localhost:5045
```

Frontend, dans un second terminal :

```bash
cd frontend && npm run dev
```

Jeu d'essai à préparer depuis l'application, une fois connecté :

1. Deux clients, par exemple « Martin » et « Durand ».
2. Un produit au poids autorisant la vente à la tranche (un jambon), et un produit au poids
   ordinaire (des saucisses), chacun avec une fournée et au moins trois unités pesées.
3. Trois ventes : une de trois sachets au client Martin, une d'un seul sachet, une tranche de
   jambon.

---

## Scénario 1 — Corriger l'en-tête (US1, FR-001 à FR-003)

1. Ouvrir la vente de trois sachets, appuyer sur **Corriger**.
2. Changer le client pour Durand, reculer la date d'un jour, ajouter une note, valider.
3. **Attendu** : le numéro de vente est inchangé, la date affichée est la nouvelle, la note
   apparaît, et la vente figure dans l'historique d'achats de Durand — plus dans celui de Martin.
4. Rouvrir **Corriger**, changer le client, appuyer sur **Annuler**.
5. **Attendu** : la vente est intacte, le client d'origine est conservé.
6. Depuis le même écran de correction, basculer le statut de paiement et valider.
7. **Attendu** : seul le statut change ; le total, les lignes et le numéro sont identiques.

---

## Scénario 2 — Corriger une ligne (US2, FR-005, FR-009)

1. Ouvrir la vente de trois sachets, appuyer sur une ligne.
2. Corriger le montant encaissé, valider.
3. **Attendu** : la ligne affiche le nouveau montant, et le total de la vente est la somme des
   montants réellement enregistrés — pas un calcul poids × prix.
4. Ouvrir la ligne de tranche de jambon, corriger le poids vendu **sans toucher au montant**,
   valider.
5. **Attendu** : le poids change, le montant reste celui qui était saisi (RG-05).
6. Sur cette même ligne, saisir un poids vendu supérieur au poids pesé de l'unité, valider.
7. **Attendu** : refus affiché en français nommant les deux poids, la saisie reste à l'écran, rien
   n'est enregistré.

---

## Scénario 3 — Retirer une ligne (US2, FR-004, FR-006, FR-008)

1. Ouvrir la vente de trois sachets, ouvrir une ligne, choisir **Retirer cette ligne**.
2. **Attendu** : une confirmation nomme le numéro d'étiquette de l'unité et annonce son retour en
   stock.
3. Annuler. **Attendu** : la vente est intacte, trois lignes.
4. Recommencer et confirmer. **Attendu** : deux lignes, total diminué du montant retiré.
5. Ouvrir le stock du produit concerné. **Attendu** : l'unité y figure de nouveau comme disponible,
   sous son numéro d'étiquette d'origine.
6. Ouvrir la vente d'un seul sachet, tenter de retirer sa ligne unique.
7. **Attendu** : refus en français indiquant qu'il faut supprimer la vente elle-même. La vente est
   intacte.

---

## Scénario 4 — Supprimer la vente (US3, FR-007, FR-008)

1. Ouvrir une vente, choisir **Supprimer cette vente**.
2. **Attendu** : la confirmation nomme le numéro de la vente, le nombre de lignes supprimées et
   annonce le retour en stock des unités sans autre sortie.
3. Annuler. **Attendu** : la vente est intacte.
4. Recommencer et confirmer.
5. **Attendu** : retour à la liste des ventes, la vente a disparu, l'historique d'achats de son
   client ne la montre plus, et ses unités sont de nouveau disponibles dans le stock.

---

## Scénario 5 — Les cas limites du serveur (Edge Cases)

1. **Unité portant une autre sortie** : vendre un sachet, puis retirer la ligne de vente, puis
   vérifier le stock. L'unité revient. Refaire l'opération sur une unité ayant en plus une sortie
   perso enregistrée : elle ne revient pas. **Attendu** : l'écran ne contredit pas le serveur, il
   affiche l'état rechargé.
2. **Unité entamée puis clôturée** : vendre deux tranches d'un même jambon, clôturer l'unité,
   retirer l'une des deux ventes. **Attendu** : l'unité reste vendue, elle ne rouvre pas.
3. **Vente supprimée ailleurs** : ouvrir la même vente dans deux onglets, la supprimer dans le
   premier, corriger dans le second. **Attendu** : message de refus lisible, sans écran blanc.

---

## Contrôles avant de considérer la fonctionnalité livrée

```bash
cd frontend && npm run type-check && npm run lint
```

- [ ] Aucun mot anglais ni valeur technique (`sold`, `available`, `sale`) visible à l'écran.
- [ ] Aucun statut HTTP ni message technique affiché à l'utilisateur.
- [ ] Le numéro de vente n'a changé dans aucun scénario.
- [ ] Tout geste destructif est passé par une confirmation qui en énonçait les conséquences.
- [ ] L'écran de saisie d'une vente fonctionne toujours à l'identique après l'extraction du
      sélecteur de client (décision D4).
