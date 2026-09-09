# Phase 0 — Recherche et décisions techniques

**Feature**: Modification et fin de vie d'un produit
**Date**: 2026-09-09
**Plan**: [plan.md](./plan.md)

Aucun marqueur `NEEDS CLARIFICATION` ne subsistait dans la spécification à l'entrée de cette phase.
Les questions traitées ici sont des questions de réalisation, ouvertes par la confrontation de la
spécification au code existant.

---

## D-01 — Ne jamais réémettre un numéro de lot après suppression

**Contexte**. `ProductionBatchService.GenerateBatchNumberAsync` construit aujourd'hui le numéro en
comptant les lots existants du produit pour la date, puis en ajoutant un. Une boucle de trois essais
rattrape les collisions via l'index unique sur `batch_number`. Cette stratégie suppose qu'un lot
n'est jamais supprimé. Dès que la suppression existe, supprimer l'unique lot du jour ramène le
compte à zéro et le numéro suivant réémet `SC-250831-1`, sur une seconde série d'étiquettes
manuscrites indiscernables de la première. FR-013 l'interdit.

**Décision**. Introduire un registre de séquences persistant, table `batch_number_sequence`, clé
composite `(product_id, production_date)`, portant `last_sequence`. La génération lit et incrémente
cette ligne dans la transaction de création du lot, en la créant si elle n'existe pas. Le registre
survit à la suppression du lot, donc un numéro émis n'est jamais repris. L'index unique sur
`batch_number` reste en place comme filet, et la boucle de rattrapage existante peut disparaître.

**Rationale**. C'est la seule option qui garantit la propriété demandée sans conserver de trace du
lot supprimé dans les tables métier. Le coût est une table de deux colonnes et une écriture de plus
par création de lot, sur un volume de quelques lots par semaine.

**Conséquence acceptée**. Après suppression du seul lot d'un jour, le lot suivant du même jour porte
le numéro 2 et non 1. Le numéro 1 n'est jamais réutilisé, ce qui est exactement l'intention. Les
séries de numéros comportent donc des trous, ce qui doit être dit dans `docs/data-model.md`.

**Cas limite du code produit**. Un produit redevenu jamais utilisé peut changer de code alors que le
registre garde des lignes issues du lot supprimé. Le nouveau code produit alors `SEC-250831-2`. Il
n'y a pas de collision, le numéro n'ayant jamais été émis, et la prudence joue dans le bon sens.

**Alternatives écartées**.

- *Dériver du maximum des numéros existants plutôt que du compte* : ne survit pas davantage à la
  suppression du dernier lot du jour, le maximum retombant à zéro.
- *Suppression logique du lot, avec une colonne `deleted_at`* : préserve la numérotation, mais
  impose un filtre sur toutes les lectures de lot, dans le service comme dans les rapports futurs.
  Un filtre oublié fait réapparaître un lot supprimé dans le stock. Risque disproportionné pour un
  bénéfice que le registre obtient plus sûrement.
- *Séquence PostgreSQL par produit et par date* : ingérable, il en faudrait une par couple.

---

## D-02 — Suppression explicite des unités plutôt que cascade base de données

**Contexte**. `StockUnitConfiguration` déclare `OnDelete(DeleteBehavior.Restrict)` entre
`stock_unit` et `production_batch`. Supprimer un lot exige donc de traiter ses unités.

**Décision**. Garder `Restrict` au niveau de la base et supprimer les unités explicitement dans
`ProductionBatchService.DeleteAsync`, à l'intérieur d'une transaction, après avoir vérifié
qu'aucune unité du lot ne porte de `stock_movement`.

**Rationale**. Le principe II de la constitution fait du backend le garant des règles. Une cascade
au niveau de la base déplacerait la décision hors du service et supprimerait silencieusement les
unités quelles que soient leurs sorties, si un jour la cascade s'étendait aux mouvements. Le
`Restrict` conservé devient un filet : si la vérification applicative était contournée, la base
refuserait la suppression plutôt que de détruire de l'historique.

**Alternatives écartées**.

- *Cascade EF Core sur `stock_unit`* : plus court à écrire, mais fait de la destruction le
  comportement par défaut d'une relation qui protège la traçabilité.
- *Vérification côté frontend uniquement* : contraire au principe II.

---

## D-03 — État « utilisé » dérivé, non stocké

**Décision**. Ne pas ajouter de colonne sur `product`. L'état se calcule par l'existence d'au moins
un lot rattaché, et s'expose sur `ProductDto` sous le nom `isUsed`.

**Rationale**. Une colonne dénormalisée devrait être maintenue à la création comme à la suppression
d'un lot, et divergerait au premier oubli. FR-015 exige de surcroît le retour à l'état modifiable
après suppression du dernier lot, ce qu'un calcul donne gratuitement. Le volume rend la requête sans
enjeu.

**Nuance par rapport à `stock_unit.status`**. La constitution assume une dénormalisation sur le
statut d'unité, parce que le passage `opened → sold` est une décision manuelle non déductible. Ici
au contraire l'état est entièrement déductible, la dénormalisation n'aurait donc pas de
justification équivalente.

**Alternatives écartées**.

- *Exposer `canEditCode` et `canEditSaleMode`* : expose la politique plutôt que le fait, et oblige à
  faire évoluer le contrat à chaque changement de règle. `isUsed` décrit le monde, l'interface en
  déduit ce qu'elle grise.

---

## D-04 — Rupture de contrat assumée sur la mise à jour d'un produit

**Contexte**. `UpdateProductRequest` ne porte aujourd'hui que `Name` et `AllowPartialSale`.
FR-003 exige que `code` et `saleMode` soient modifiables sur un produit jamais utilisé.

**Décision**. Ajouter les deux champs en requis sur `PUT /api/products/{id}`. Le serveur refuse la
requête si le produit est utilisé et que les valeurs soumises diffèrent des valeurs en base ; il
l'accepte si elles sont identiques, ce qui laisse le client renvoyer la ressource complète sans
raisonner sur le gel.

**Rationale**. Une requête de mise à jour qui décrit la ressource entière est plus simple à
raisonner que des champs optionnels dont l'absence signifie « ne pas toucher ». Le seul
consommateur du contrat est le frontend du dépôt, mis à jour dans la même vague.

**Conséquence**. Rupture au sens des Conventional Commits, portée par un commit `feat(backend)!` et
répercutée sur la version du composant backend au moment de la release.

**Alternatives écartées**.

- *Champs optionnels, `null` signifiant inchangé* : évite la rupture mais rend indistinguables « ne
  pas modifier » et « vider », et laisse le doute sur ce qui est réellement appliqué.
- *Endpoint séparé pour l'identité du produit* : deux endpoints pour une seule fiche, sans bénéfice.

---

## D-05 — Solde des unités restantes en une opération serveur

**Contexte**. FR-018 et FR-019 demandent de solder une sélection d'unités en sorties de type perte.
Le service de mouvement existant crée les sorties une par une, en calculant déjà le garde-fou de
poids.

**Décision**. Exposer `POST /api/products/{id}/write-off`, recevant la liste des identifiants
d'unités, et traiter l'ensemble dans une seule transaction côté serveur. Le poids de chaque sortie
est calculé par le serveur selon FR-020, jamais transmis par le client. Les unités déjà sorties
présentes dans la sélection sont ignorées sans faire échouer l'opération.

**Rationale**. Une boucle d'appels côté client laisserait le produit dans un état intermédiaire au
premier échec réseau, et confierait au client le calcul du restant estimé, qui est une règle métier.

**Réutilisation**. La règle de poids est celle déjà appliquée par le menu de sortie unité par unité.
Elle doit être extraite là où elle vit aujourd'hui pour être partagée, plutôt que réécrite, sous
peine de voir les deux chemins diverger.

**Alternatives écartées**.

- *Boucle d'appels unitaires depuis le frontend* : non transactionnel, et déplace une règle métier
  dans le client.
- *Solder implicitement à la désactivation* : détruirait du stock sans geste explicite, exactement
  ce que la confirmation de FR-022 cherche à éviter.

---

## D-06 — Lister les unités restantes d'un produit

**Contexte**. La boîte de dialogue de solde vit sur la fiche produit et doit lister les unités
restantes. `GET /api/stock-units` filtre aujourd'hui par `batchId` et par `status`, pas par produit.

**Décision**. Ajouter un filtre `productId` à cet endpoint, combinable avec `status`.

**Rationale**. Ajout rétrocompatible d'un paramètre optionnel, aligné sur les filtres existants.
L'alternative consistant à charger les lots du produit puis leurs unités multiplierait les allers
et retours pour la même information.

---

## D-07 — Emplacement de la suppression d'un lot dans l'interface

**Contexte**. Le frontend n'a pas de route de détail de lot. `StockDetailView` affiche déjà le stock
d'un produit groupé par lot, avec un en-tête portant la date de fabrication et le prix.

**Décision**. Loger l'action de suppression sur cet en-tête de lot, avec une confirmation qui annonce
le nombre d'unités qui disparaîtront (FR-014).

**Rationale**. Aucun écran ni route nouvelle, conformément au principe I. L'utilisateur voit les
unités qu'il s'apprête à détruire au moment où il déclenche l'action, ce qui est la meilleure
prévention possible d'une suppression accidentelle.

**Alternatives écartées**.

- *Créer une route de détail de lot* : un écran de plus pour deux utilisateurs non techniques,
  alors que l'information est déjà à l'écran.

---

## Points laissés hors de cette phase

- La journalisation de qui supprime un lot reste en Vague 2, avec le reste de RF-27. Le champ
  `created_by` existe mais n'est renseigné nulle part aujourd'hui.
- Aucune détection de conflit d'édition concurrente n'est mise en place (FR-029), décision prise en
  session de clarification.
