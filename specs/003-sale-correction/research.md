# Phase 0 — Décisions techniques

**Feature**: Corriger et supprimer une vente
**Date**: 2026-09-11

Aucune inconnue technique : même pile, même contrat, et le serveur expose déjà tout ce dont
l'écran a besoin. Le travail de cette phase est donc de figer sept décisions d'interface avant
de concevoir, et de vérifier l'hypothèse centrale de la spécification — que rien ne manque côté
serveur.

---

## D0 — Vérification de l'hypothèse « aucune évolution serveur »

**Décision** : l'hypothèse tient. La fonctionnalité est entièrement frontend.

**Constat**, relevé dans le code :

| Geste de l'écran | Route existante | Règle appliquée | Test |
|---|---|---|---|
| Corriger l'en-tête | `PUT /api/sales/{id}` | client inexistant → `409` | `SaleServiceTests.UpdateAsync_ChangesCustomerAndNotes` |
| Basculer le paiement | `POST /api/sales/{id}/payment` | — | `SaleServiceTests.SetPaymentAsync_TogglesPaidFlag` |
| Corriger une ligne | `PUT /api/stock-movements/{id}` | poids vendu ≤ poids pesé (RG-05) | `StockMovementServiceTests.UpdateAsync_SoldWeight_ExceedingUnitWeight_ThrowsConflictException`, `UpdateAsync_UpdatesAmountAndNotes` |
| Retirer une ligne | `DELETE /api/stock-movements/{id}` | dernière ligne refusée ; unité sans mouvement → `available` | `DeleteAsync_LastLineOfSale_ThrowsConflictException`, `DeleteAsync_OnlyMovement_RevertsUnitToAvailable`, `DeleteAsync_OneOfSeveralPartialMovements_KeepsUnitOpened` |
| Supprimer la vente | `DELETE /api/sales/{id}` | lignes supprimées, unités libérées | `SaleServiceTests.DeleteAsync_RevertsUnitsToAvailable` |

**Conséquence** : aucun test backend nouveau n'est exigé par le principe II, puisque aucune règle
de gestion n'est ajoutée ni modifiée. Les messages de refus sont déjà rédigés en français côté
serveur, y compris celui de la dernière ligne — l'écran les affiche tels quels (D5).

---

## D1 — Où vit la correction : sur l'écran de détail, en mode édition explicite

**Décision** : pas de nouvelle route. L'écran `/sales/:id` reste un écran de lecture, et une
action « Corriger » y fait basculer l'en-tête en édition, avec « Enregistrer » et « Annuler ».

**Rationale** : FR-002 demande une validation en un seul geste et un abandon sans effet. Un mode
explicite rend l'abandon visible et réversible, là où des champs perpétuellement éditables
laisseraient croire que chaque frappe est enregistrée. L'écran de lecture reste calme pour son
usage courant, qui est de relire une vente (principe I).

**Alternatives écartées** : une route `/sales/:id/edit`, rejetée parce qu'elle sépare la
correction des lignes (qui restent, elles, sur l'écran de détail) et double la navigation ;
l'édition inline permanente à la manière de `ProductDetailView`, rejetée parce qu'une vente se
consulte bien plus souvent qu'elle ne se corrige, à l'inverse d'une fiche produit.

---

## D2 — Correction d'une ligne : une boîte de dialogue par ligne

**Décision** : un appui sur une ligne ouvre une boîte de dialogue portant le numéro d'étiquette,
le montant encaissé, le poids vendu si le produit est au poids, et l'action « Retirer cette
ligne ».

**Rationale** : la ligne est courte et l'écran est un téléphone. Une boîte dédiée offre des champs
à la bonne taille, un titre qui nomme l'unité concernée, et un endroit naturel pour le geste
destructif. Elle isole aussi le refus serveur (poids hors capacité) du reste de la vente.

**Alternatives écartées** : édition inline des lignes, rejetée pour l'encombrement sur mobile et
parce qu'elle mélangerait deux appels serveur distincts dans un même « Enregistrer ».

---

## D3 — Aucun recalcul automatique du montant quand le poids change

**Décision** : montant et poids vendu sont deux champs indépendants. Corriger le poids ne
retouche pas le montant.

**Rationale** : RG-05 et le piège n° 4 de `CLAUDE.md` §9 — le montant stocké est celui qui a été
réellement encaissé, jamais un théorique. Techniquement, `StockMovementDto` ne porte pas le prix
au kilo du lot : recalculer exigerait d'aller chercher la fournée, c'est-à-dire de reconstruire
côté client un calcul que le serveur ne fait pas.

**Alternatives écartées** : pré-remplir le montant à partir du nouveau poids, rejetée car elle
écraserait silencieusement un montant arrondi à la main, ce qui est le cas nominal d'une vente
en espèces.

---

## D4 — Le choix du client est extrait dans un composant partagé

**Décision** : la recherche de client de `SaleAddView` devient `components/domain/CustomerPicker.vue`,
utilisée par l'écran de saisie et par la correction.

**Rationale** : FR-003 exige explicitement « le même confort de recherche ». Deux copies de la
même recherche divergeraient, et la promesse d'identité serait fausse au premier ajustement.
L'extraction est un déplacement de balisage et de trois lignes de filtrage, sans changement de
comportement pour l'écran de saisie.

**Alternatives écartées** : dupliquer la recherche dans l'écran de correction, rejetée pour la
divergence ; un `v-autocomplete` Vuetify, rejeté parce qu'il ne donnerait pas le même rendu que
l'écran de saisie, donc pas le même geste pour l'utilisateur.

---

## D5 — Les refus serveur sont affichés tels quels

**Décision** : chaque geste attrape `ApiError` et affiche `err.message`, en conservant la saisie
en cours. Aucun message n'est réécrit côté client, aucun statut HTTP n'est montré.

**Rationale** : les messages du serveur sont déjà en français et nomment la donnée en cause (le
poids de l'unité, le fait qu'il s'agit de la dernière ligne). C'est le motif déjà retenu pour la
désactivation d'un produit et la suppression d'une fournée. Répliquer les règles côté client pour
formuler nos propres messages contredirait le principe II.

**Conséquence sur FR-006** : le refus du retrait de la dernière ligne n'est pas anticipé par une
désactivation du bouton. Le geste part, le serveur refuse, la boîte affiche sa phrase — qui dit
déjà d'utiliser la suppression de la vente.

---

## D6 — La confirmation destructive annonce ses conséquences chiffrées

**Décision** : la boîte de suppression d'une vente nomme le numéro de la vente, le nombre de
lignes supprimées et le retour des unités en stock. Le retrait d'une ligne nomme le numéro
d'étiquette de l'unité concernée.

**Rationale** : FR-008 et SC-003. Le motif est celui de `BatchDeleteAction.vue`, qui a déjà fait
ses preuves sur une suppression de fournée.

**Réserve tenue** : l'annonce « redevient disponible » est une prévision, pas un calcul. Une
unité qui porte un autre mouvement ne reviendra pas en stock, et l'interface ne peut pas le savoir
sans interroger les mouvements de chaque unité. Le libellé reste donc général — « les unités qui
n'ont pas d'autre sortie reviennent en stock » — plutôt que faussement précis.

---

## D7 — Après suppression, retour à la liste des ventes

**Décision** : la suppression de la vente renvoie sur `/sales`. Toute autre correction recharge
la vente depuis le serveur (`reload()`), sans mise à jour optimiste.

**Rationale** : FR-011. La vente n'existe plus, rester sur son écran afficherait une erreur de
chargement. Pour les autres gestes, recharger garantit que le total et le nombre d'articles
affichés sont ceux du serveur (FR-009), y compris après un refus partiel.

---

## Ce que cette phase ne tranche pas

- **Tests frontend** : l'écart E-08 reste ouvert. Cette fonctionnalité n'introduit aucune fonction
  pure nouvelle qui appellerait un test unitaire ; la validation passe par `quickstart.md`.
- **Ajouter une ligne à une vente enregistrée** : hors périmètre, acté en hypothèse dans la
  spécification.
