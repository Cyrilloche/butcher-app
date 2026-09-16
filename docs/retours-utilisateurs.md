# Retours utilisateurs

> Suivi des retours de test en condition réelle. Chaque retour reçoit un identifiant `RU-xx`, repris dans le nom de la branche qui le traite (`fix/ru-02-…`) et dans les messages de commit.
>
> **Méthode** : tous les retours sont triés avant de coder le premier. Une branche par sujet, créée depuis `dev`, fusionnée sans squash (le changelog est généré depuis les commits). Un sujet qui change une règle de gestion passe par un spec (`specs/`). Les releases partent par paquets, pas après chaque ticket.

---

## Recette du 2026-09-16

| Réf. | Retour (reformulé) | Type | Impact | Effort | Statut |
|---|---|---|---|---|---|
| RU-01 | Vente impossible à valider : le bouton « Enregistrer » restait grisé | Bug ? | Bloquant | ? | En attente d'informations |
| RU-02 | La recherche d'une vente cache des produits : « que 8 affichés », « en tapant chipo on ne voit pas toutes les chipos » | Gêne d'usage | Bloquant en pratique | M | Corrigé, à valider par les utilisateurs |
| RU-03 | Ajouter un produit à une vente déjà enregistrée, quand le client demande un complément | Demande | Fréquente | M | Fait, à valider par les utilisateurs |
| RU-04 | Lancer une nouvelle vente depuis la fiche d'un client, client déjà rempli | Demande | Fréquente | S | Fait, à valider par les utilisateurs |
| RU-05 | Sur PC, la barre latérale défile | Bug d'affichage | Cosmétique | S | Fait, à valider par les utilisateurs |
| RU-06 | « Pouvoir modifier des commandes » | À préciser | ? | ? | À préciser |
| RU-07 | « Intéressant d'avoir des allergies » | Demande nouvelle | Idée | M à L | À préciser |

**Ordre de passage** : RU-02 → RU-04 → RU-03 → RU-05, puis RU-06 et RU-07 une fois précisés. RU-01 attend un cas reproductible.

---

## Détail

### RU-01 — Vente impossible à valider

Retour brut : « Vente impossible à valider ». Précision obtenue : le bouton était grisé, sans autre information.

Le bouton ne s'active qu'avec un client **et** au moins un produit au panier (`SaleAddView.vue`, `canSave`). Deux gestes laissent ces conditions non remplies sans que l'écran le dise :
- le nom du client tapé dans la recherche mais pas choisi dans la liste ;
- pour un produit vendable à la tranche (jambon), le choix « Vendre en entier » / « Vendre une tranche » resté sans réponse : le produit n'est pas au panier.

Piste indépendante de la cause exacte : dire sous le bouton grisé ce qui manque (« Choisis un client », « Ajoute au moins un produit »).

### RU-02 — La recherche d'une vente cache des produits

Retours bruts : « Dans une vente, on ne voit pas tout le stock (que 8 affichés) » et « S'il tape juste chipo on ne voit pas toutes les catégories de chipo. L'idée serait de taper chipo, voir les catégories de chipo. Donc peut-être rajouter une catégorie ».

Cause : la recherche liste des **unités** (un sachet par ligne), triées par produit, et s'arrête à 8. Le premier produit qui a 8 sachets en stock occupe toute la liste, et les autres produits qui correspondent n'apparaissent jamais. Une catégorie ne changerait rien : le correctif est de regrouper les résultats par produit.

Critère de validation : en tapant « chipo », on voit tous les produits dont le nom contient « chipo », avec leur nombre d'unités en stock ; on en choisit un, et on voit **toutes** ses unités.

### RU-03 — Ajouter un produit à une vente existante

Retour brut : « Rajouter des produits à une vente si un client demande un complément ».

Le serveur sait déjà rattacher une ligne à une vente existante (`POST /api/stock-units/{id}/movements` avec `saleId`). Il manque le geste dans le détail d'une vente, avec la même recherche que la saisie.

### RU-04 — Nouvelle vente depuis la fiche client

Retour brut : « Rajouter une vente depuis la vente client ».

Un bouton « Nouvelle vente » sur la fiche client, qui ouvre la saisie avec ce client déjà choisi.

### RU-05 — Barre latérale qui défile sur PC

Retour brut : « Sur PC, la sidebar est scrollable ».

### RU-06 — Modifier des commandes

Retour brut : « Pouvoir modifier des commandes ». À préciser. La correction d'une vente existe déjà (client, date, paiement, montant d'une ligne, retrait d'une ligne). Trois lectures possibles : la correction existante n'a pas été trouvée ; il s'agit de RU-03 ; ou il s'agit de vraies commandes (un client réserve, récupère plus tard), un concept nouveau qui relèverait de la V2.

### RU-07 — Allergies

Retour brut : « Intéressant d'avoir des allergies ». À préciser : allergies d'un **client** (une note sur sa fiche) ou allergènes d'un **produit** (lié aux recettes, prévues en V2) ?

---

## Points relevés en passant

- Le choix du client dans une vente (`CustomerPicker.vue`) s'arrête aussi à 5 résultats. Sans conséquence tant que le nom tapé est assez précis, à surveiller quand la liste de clients grandira.
