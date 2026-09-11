# Phase 0 — Recherche et décisions

**Feature**: Poids encore vendable d'une unité entamée
**Date**: 2026-09-11

La spécification ne portait aucun marqueur de clarification : les trois arbitrages de fond avaient
été tranchés par l'exploitant avant rédaction. Ce document tranche les questions de réalisation, en
partant de ce que le code fait déjà.

---

## D0 — La règle de calcul existe déjà, il ne faut pas l'écrire une seconde fois

**Décision** : réutiliser `StockMovementRules.ComputeOutcomeWeight`, en la renommant
`ComputeRemainingWeight`. Le calcul du poids encore vendable ne sera écrit nulle part ailleurs.

**Rationale** : cette méthode retranche déjà du poids pesé la somme des poids vendus, avec le
plancher à zéro exigé par FR-003 et le `null` pour une unité sans poids exigé par FR-004. Elle a
été portée côté serveur en v0.8, quand le solde groupé d'un produit en a eu besoin, précisément
parce que deux implémentations de la même règle finissent par diverger. Son nom actuel décrit son
premier appelant — le poids à inscrire sur une sortie perso ou perte — et non ce qu'elle calcule.
Le renommage rend le partage lisible ; le commentaire de la méthode hérite de la double lecture.

**Alternatives écartées** :
- *Écrire un second calcul dans `StockUnitService`* : deux vérités pour une règle, l'erreur que la
  v0.8 avait justement corrigée.
- *Laisser le nom `ComputeOutcomeWeight`* : un lecteur cherchant le poids restant ne le trouverait
  pas sous un nom qui parle de sortie de stock.

---

## D1 — Le poids déjà vendu s'agrège en SQL, dans la projection

**Décision** : dans `StockUnitService.GetAllAsync` et `GetByIdAsync`, projeter chaque unité avec la
somme de ses poids vendus, obtenue par une sous-requête sur la navigation `StockMovements` filtrée
sur le type vente, puis passer cette somme à `ComputeRemainingWeight`.

**Rationale** : la navigation existe déjà sur l'entité `StockUnit`, et EF Core traduit une somme
sur une collection en sous-requête corrélée. Une seule requête part donc, quel que soit le nombre
d'unités, ce qui respecte la seule contrainte de performance du plan. La projection finale accepte
par ailleurs un appel de méthode côté client, ce dont le service se sert déjà avec `ToDto` : le
calcul métier reste donc en C#, seule l'agrégation descend en SQL.

**Alternatives écartées** :
- *`Include` des mouvements* : chargerait toutes les lignes de vente de toutes les unités pour
  n'en garder qu'une somme. Volume inutile, et une jointure qui grossit avec l'historique.
- *Un appel par unité au garde-fou existant* (`SumSoldWeightForSaleMovementsAsync`) : un N+1
  assumé, sur un écran que l'exploitant ouvre plusieurs fois par jour.
- *Une vue SQL ou une colonne calculée en base* : interdit par FR-002, et inutile à cette échelle.

---

## D2 — Le champ est porté par toute unité pesée, pas seulement par les unités entamées

**Décision** : `remainingWeight` vaut le poids encore vendable pour toute unité portant un poids
pesé, donc le poids pesé lui-même quand rien n'a été vendu. Il vaut `null`, et non zéro, pour une
unité sans poids : produit vendu à la pièce, ou unité au poids pas encore pesée.

**Rationale** : un total devient alors une simple somme du champ, sans condition sur le statut. La
variante « le champ n'existe que sur une unité entamée » obligerait chaque appelant à écrire
« prends le restant s'il existe, sinon le poids pesé », soit la même règle recopiée dans les deux
écrans, et un oubli garanti au troisième. Le `null` distingue par ailleurs « aucun poids dans ce
produit » de « plus rien à vendre », deux situations que zéro confondrait.

**Alternatives écartées** :
- *Champ présent seulement si le statut est entamé* : déplace la règle chez l'appelant (voir
  ci-dessus).
- *Zéro pour une unité sans poids* : ferait entrer les produits à la pièce dans un calcul de poids
  qui n'a pas de sens pour eux, et FR-014 les exclut explicitement.

---

## D3 — Les deux totaux sont sommés côté client, et ce n'est pas une entorse au principe II

**Décision** : `useStock.ts` somme le champ `remainingWeight` des unités en stock, pour le résumé
du détail d'un produit comme pour la ligne de la liste de stock. Aucun nouvel agrégat serveur.

**Rationale** : la règle métier est la soustraction, et elle reste côté serveur. Ce qui se passe
côté client est une addition de valeurs déjà calculées par le serveur, au même titre que le
décompte d'unités qu'il additionne depuis toujours. Le principe II interdit au client d'être
l'unique gardien d'une règle, pas de mettre en forme ce qu'il a reçu. Les deux écrans lisent déjà
la liste des unités pour d'autres raisons, donc la somme ne coûte aucune requête supplémentaire —
et faire passer les deux écrans par la même fonction garantit le chiffre identique exigé par
SC-003.

**Alternatives écartées** :
- *Un agrégat par produit exposé par le serveur* : nouveau point d'entrée, donc nouvelle surface de
  contrat, pour une addition. Hors périmètre, et à rouvrir seulement si le volume de données
  devenait un sujet, ce qu'il n'est pas.
- *Deux calculs séparés, un par écran* : c'est l'état actuel, et c'est précisément ce qui a laissé
  les deux écrans surestimer le stock de la même façon sans que personne ne s'en aperçoive.

---

## D4 — Ajout additif au contrat, donc pas de rupture

**Décision** : un champ nullable de plus sur `StockUnitDto`, aucun champ renommé, supprimé ni
retypé. Les commits sont `feat(backend)` et `feat(frontend)`, **sans** `!`.

**Rationale** : le principe III impose de signaler une rupture de contrat par un `!` et de
versionner le composant en conséquence. Un client qui ignore le nouveau champ continue de
fonctionner à l'identique, donc il n'y a pas de rupture. Les deux composants gardent des cycles de
publication indépendants : le backend peut partir avant le frontend sans rien casser, et c'est
l'ordre de livraison retenu.

---

## D5 — RG-05 est révisée, pas remplacée

**Décision** : réécrire RG-05 pour autoriser un poids restant **calculé à la demande et affiché**,
en maintenant les deux interdits qui font le sens de la règle : aucune persistance, et le garde-fou
d'écriture inchangé. La révision redescend dans le PRD, `docs/data-model.md` et `CLAUDE.md`, dans
le même lot de travail que le code (principe IV).

**Rationale** : la rédaction actuelle dit « le poids restant n'est pas suivi (aucun champ ni
affichage dédié) ». Le mot qui compte est *suivi* : l'intention d'origine était de ne pas créer une
donnée à maintenir en cohérence, pas d'interdire une soustraction. Le garde-fou ajouté en v0.7
faisait déjà cette soustraction sans que personne y voie une contradiction. Laisser la règle telle
quelle serait le vrai danger : une session ultérieure lirait l'interdit, prendrait l'affichage pour
une dérive et le supprimerait.

**Alternatives écartées** :
- *Une nouvelle règle RG-18 laissant RG-05 en place* : deux règles contradictoires sur le même
  sujet, le pire des deux mondes.
- *Ne rien changer dans la documentation* : contraire au principe IV, et la fonctionnalité
  contredirait un document qui fait foi.

---

## D6 — La corbeille par unité n'a besoin d'aucun travail serveur

**Décision** : brancher la corbeille de la maquette sur `DELETE /api/stock-units/{id}`, qui existe,
via `deleteStockUnit` du client HTTP, qui existe aussi. Le bouton est désactivé dès que l'unité
porte un mouvement.

**Rationale** : le point d'entrée et sa règle sont livrés depuis la v0.8, seul l'écran manquait. Le
serveur refuse déjà la suppression d'une unité porteuse de mouvement, donc le bouton désactivé
n'est qu'une politesse : le refus reste garanti côté serveur. Une unité `disponible` sans mouvement
est le seul cas où il s'active, ce qui correspond exactement à la correction d'une erreur de pesée
que la règle visait.

---

## D7 — « Déclarer une perte » change d'icône

**Décision** : remplacer la corbeille de l'entrée « Déclarer une perte » du menu par
`phosphor:warning-octagon`. La corbeille devient réservée à la suppression.

**Rationale** : la maquette place une corbeille de suppression à quelques millimètres d'un menu
dont une entrée porte la même corbeille pour un sens tout autre. Déclarer une perte est un
**événement de gestion** qui conserve la trace de l'unité ; supprimer une unité **efface** une
saisie erronée. Confondre les deux sur des utilisateurs non techniques coûterait une perte
enregistrée à tort, donc un stock faux et une traçabilité trouée. L'octogone d'alerte dit
« incident » sans dire « effacer ».

**Alternatives écartées** :
- *Garder les deux corbeilles* : proposé à l'exploitant, écarté par lui.
- *Renoncer à la corbeille de suppression* : la maquette la demande, et elle rend accessible une
  règle déjà livrée.

---

## D8 — Le calcul de restant côté client disparaît

**Décision** : `getRemainingWeightKg` est supprimée. La saisie d'une vente à la tranche lit
`remainingWeight` sur l'unité qu'elle a déjà chargée.

**Rationale** : cette fonction interrogeait les mouvements d'une unité pour refaire côté client la
soustraction que le serveur sait faire, soit une requête par jambon sélectionné. `CLAUDE.md` liste
déjà le calcul client d'un poids de sortie parmi les pièges connus ; celui-ci en est le dernier
reste. Sa disparition supprime une requête, une duplication de règle et une source de divergence.

**Point d'attention** : la prévision affichée pendant la saisie d'une tranche devient donc la
valeur du serveur au moment du chargement de l'écran. C'est déjà le cas aujourd'hui, et le
garde-fou revalide à l'écriture : rien ne change sur la garantie.

---

## D9 — « à clôturer » reste décoratif

**Décision** : la mention affichée à côté d'un restant nul est un simple libellé. Aucune action,
aucun bouton, aucune logique. La clôture continue de passer par le menu à trois points.

**Rationale** : validée par l'exploitant sur l'esthétique seule, et assumée comme telle dans la
spécification. Elle explique le zéro, qui sans elle passerait pour un défaut. En faire un raccourci
d'action serait un ajout de périmètre non demandé, à rouvrir plus tard si l'usage le réclame.

---

## D10 — Tests backend exigés par le principe II

**Décision** : couvrir le poids encore vendable dans `StockUnitServiceTests`, sur six cas : unité
intacte pesée, unité entamée après une vente partielle, unité entièrement vendue mais non clôturée,
unité d'un produit vendu à la pièce, unité au poids pas encore pesée, et non-régression du plancher
à zéro. Les sorties perso et perte ne doivent pas entrer dans la somme retranchée : seul le type
vente compte.

**Rationale** : RG-05 est touchée, donc la constitution exige un test côté serveur. Le cas des
sorties non-vente mérite son test : une unité entamée puis déclarée perdue quitte le stock, mais
rien n'empêcherait un filtre trop large de retrancher son poids de perte du restant, ce qui
donnerait un restant faussement nul.
