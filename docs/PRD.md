# Product Requirements Document — Mini-ERP Charcuterie Artisanale

| | |
|---|---|
| **Nom de projet** | Mini-ERP Charcuterie (nom de code : *à définir*) |
| **Version du document** | 0.2 |
| **Date** | 3 septembre 2026 |
| **Statut** | Cadrage validé, en cours d'implémentation (backend) |
| **Auteur** | Cyril, avec assistance à l'architecture |
| **Destinataires** | Utilisateurs finaux (exploitants), équipe de développement |

### Historique des révisions

| Version | Date | Auteur | Description |
|---|---|---|---|
| 0.1 | 2026-09-02 | Cyril | Rédaction initiale à partir des ateliers de cadrage métier |
| 0.2 | 2026-09-03 | Cyril, avec assistance à l'implémentation | Ajout des règles de gestion RG-08 à RG-12, apparues pendant l'implémentation du backend (cœur métier V1 entièrement exposé en API à cette date) ; précision sur RF-10 |

---

## 1. Résumé exécutif

Ce document décrit les exigences d'une application de gestion légère (« mini-ERP ») destinée à une activité annexe de **charcuterie artisanale** opérée par deux particuliers. L'activité, aujourd'hui gérée intégralement à la main (recettes sur feuilles volantes, aucun suivi de stock, aucune visibilité sur la rentabilité), doit être outillée par une application web progressive (PWA) accessible en mobilité comme sur poste fixe.

L'ambition n'est **pas** de reproduire un ERP industriel, mais de fournir un outil **robuste, simple et évolutif** couvrant d'abord la gestion de la production, du stock et des ventes, puis, par vagues successives, la traçabilité fine, le calcul de coût de revient et l'analyse de rentabilité.

La méthodologie retenue est **itérative et incrémentale** : une V1 fonctionnelle centrée sur le cœur métier (production → stock → ventes/clients), conçue pour ne fermer aucune porte aux évolutions financières et de traçabilité prévues en V2 et au-delà.

---

## 2. Contexte et problématique

### 2.1 Situation actuelle

Les exploitants produisent de la charcuterie artisanale (saucisses, jambons, et potentiellement d'autres produits comme des terrines) en tant qu'activité secondaire. La production est réalisée par lots, sur un ou deux week-ends bloqués selon leur disponibilité, à partir de matière première (viande) achetée en gros chez un grossiste. La répartition entre recettes se fait « au feeling » (par exemple, sur 100 kg de viande : une partie en saucisse curry, une partie en saucisse andalouse, etc.).

La vente est **entièrement informelle** : vente directe à des particuliers (amis, connaissances), sans commande formelle, sans facture, paiement en espèces. Une partie de la production est par ailleurs **autoconsommée** ou destinée à des événements personnels.

### 2.2 Points de douleur identifiés

- **Recettes non capitalisées** : conservées sur feuilles volantes, avec risque de perte et absence d'historisation des ajustements.
- **Absence de suivi de stock** : aucune visibilité sur ce qui est disponible, vendu, consommé ou perdu.
- **Rentabilité inconnue** : les sorties « hors vente » (autoconsommation, cadeaux, événements) empêchent de distinguer ce qui est réellement vendu de ce qui est produit, et donc de connaître le gain réel de l'activité.
- **Traçabilité inexistante** : impossible de savoir quel produit a été vendu à quel client, alors même que cette information a une valeur à la fois commerciale et sanitaire (rappel produit, sécurité alimentaire).

### 2.3 Opportunité

Un outil simple, accessible depuis un téléphone comme depuis un ordinateur, permettrait de structurer l'activité sans en alourdir l'exploitation : suivre le stock produit par produit, enregistrer les ventes et les sorties, conserver un historique client, et — à terme — mesurer la marge réelle pour ajuster les volumes de production.

---

## 3. Objectifs et indicateurs de succès

### 3.1 Objectifs produit

| Réf. | Objectif |
|---|---|
| OBJ-1 | Permettre aux exploitants d'être **pleinement autonomes** dans la configuration et l'usage de l'outil, sans intervention de développement pour les opérations courantes (création de produits, d'unités, saisie des lots et ventes). |
| OBJ-2 | Offrir une **visibilité en temps réel sur le stock** de produits finis, unité par unité. |
| OBJ-3 | **Historiser les ventes et les clients**, avec la capacité de savoir qui a acheté quoi. |
| OBJ-4 | Distinguer explicitement les sorties **vente**, **personnelle** et **perte**, afin de préparer le calcul de rentabilité. |
| OBJ-5 | Construire une fondation technique **évolutive**, permettant l'ajout ultérieur du coût de revient, de la gestion des matières premières et des recettes versionnées sans refonte du modèle de données. |

### 3.2 Indicateurs de succès

- Les exploitants créent eux-mêmes leurs produits et unités sans assistance.
- 100 % des ventes et sorties sont saisies dans l'outil plutôt que « de tête ».
- À partir de la V2, l'exploitant peut répondre à la question : *« Combien cette activité m'a-t-elle réellement rapporté sur la période ? »*
- L'outil est effectivement utilisé sur le terrain (mobilité) et non abandonné au profit du papier.

---

## 4. Périmètre

Le projet suit une approche **agile par vagues**. Le périmètre ci-dessous distingue ce qui est livré en V1, ce qui est planifié pour les versions ultérieures, et ce qui est explicitement hors périmètre à ce stade.

### 4.1 Dans le périmètre — V1 (noyau)

- Gestion des **produits** et de leur mode de vente.
- Gestion des **unités de mesure** personnalisables par l'utilisateur.
- Gestion des **lots de production** (produit, date, prix de vente, référence libre de matière première).
- Suivi du **stock à l'unité physique** (chaque sachet, chaque jambon), avec poids et statut individuels.
- Enregistrement des **ventes**, des **sorties personnelles** et des **pertes/casses**.
- Gestion des **clients** et de l'historique associé.
- **Authentification** obligatoire (compte simple).
- Interface **PWA responsive** (mobile prioritaire, vue PC prévue).

### 4.2 Planifié — V2 et au-delà

- **Coût de revient et rentabilité** : coût de la matière première réparti sur les lots, marge réelle (encaissé − coût), valorisation de la production.
- **Gestion des achats de matière première** (viande et autres intrants) comme entité à part entière, reliée aux lots.
- **Recettes** : capitalisation des recettes, puis versionnement des recettes rattaché aux lots (lien `lot → version de recette → produit`) pour ajuster les productions futures selon les retours.
- **Gestion multi-comptes avec journalisation** (« qui a fait quoi »).
- **Alertes** (seuil de stock bas, DLC approchante).

### 4.3 Hors périmètre (à ce stade)

- Facturation légale, comptabilité, obligations fiscales/déclaratives.
- Vente à des professionnels (B2B), gestion de commandes formelles.
- Coût du temps de travail, amortissement du matériel, charges indirectes (énergie, etc.).
- Conversion automatique entre unités hétérogènes non nécessaires au modèle (voir §6.2).
- Application mobile native (le choix est une PWA).

---

## 5. Utilisateurs et personas

| Persona | Description | Usage principal |
|---|---|---|
| **L'exploitant producteur** | Réalise la production, pèse et conditionne, enregistre les lots. | Création de lots et d'unités physiques, saisie des poids. |
| **L'exploitant vendeur** | Gère les ventes informelles et le contact client. | Enregistrement des ventes, sorties perso, consultation du stock. |

En pratique, les deux exploitants (la mère et le beau-père de l'auteur) peuvent assumer indifféremment ces deux rôles. La V1 ne différencie pas les rôles techniquement (compte partagé simple), mais l'architecture prévoit la traçabilité de l'auteur des actions pour une évolution ultérieure.

**Hypothèse d'usage** : usage non intensif, faible probabilité d'opérations concurrentes simultanées, mais cette éventualité ne peut être totalement exclue et doit être gérée proprement par le backend.

---

## 6. Exigences fonctionnelles

Les exigences sont identifiées `RF-xx`. Les règles de gestion associées sont identifiées `RG-xx` (§7).

### 6.1 Produits

| Réf. | Exigence |
|---|---|
| RF-01 | L'utilisateur peut créer, modifier et désactiver un produit de manière autonome. |
| RF-02 | Un produit possède un **mode de vente** défini à la création : `poids_variable` (chaque unité est pesée individuellement, prix calculé au poids) ou `piece_simple` (unité comptée, prix à la pièce). |
| RF-03 | Un produit possède une **unité d'affichage de vente** (ex. kg, tranche, pièce), choisie parmi les unités disponibles. |

### 6.2 Unités de mesure

| Réf. | Exigence |
|---|---|
| RF-04 | L'utilisateur peut créer et gérer des **unités de mesure** de manière autonome, sans intervention de développement ni redéploiement. |
| RF-05 | Une unité comporte au minimum un libellé et une abréviation (ex. « kilogramme » / « kg »). |

> **Note d'architecture** — En V1, les unités sont **déclaratives** (pas de moteur de conversion automatique entre unités hétérogènes). Le modèle de gestion « unité physique pesée » (§6.4) rend la conversion superflue pour le cœur métier : le stock se compte à l'unité physique, et le prix se calcule sur le poids réel de chaque unité. Un moteur de conversion pourra être ajouté ultérieurement si un besoin concret émerge, sans remettre en cause le modèle.

### 6.3 Lots de production

| Réf. | Exigence |
|---|---|
| RF-06 | L'utilisateur peut créer un **lot de production**, caractérisé par : un produit, une date de production, un prix de vente (au kg ou à la pièce selon le mode du produit). |
| RF-07 | Le prix de vente est défini **au niveau du lot** (et non figé au niveau du produit) : deux lots d'un même produit peuvent avoir des prix différents selon la demande ou le coût des matières. |
| RF-08 | Un lot porte une **référence libre de matière première** (champ texte, ex. « porc — grossiste X »), à titre informatif en V1. |
| RF-09 | Un lot peut porter une **date de péremption (DLC)**. |
| RF-10 | La création d'un lot génère les **unités physiques de stock** correspondantes (§6.4). |

> **Note d'implémentation** — Au niveau de l'API, la création du lot et la génération des unités physiques sont **deux actions distinctes** (deux appels), pas une seule opération atomique : ça permet d'ajouter des unités en plusieurs fois (pesée étalée sur plusieurs jours pour un même lot). Le frontend peut tout à fait enchaîner les deux appels pour donner l'impression d'un flux unique à l'utilisateur ; ça reste un choix d'implémentation, pas un changement du besoin exprimé par RF-10.

> **Note d'architecture** — La notion de « session de production » regroupant plusieurs lots d'un même week-end est **écartée en V1** : chaque lot est autonome. La référence matière première reste un simple texte, mais le modèle est conçu pour qu'une future table « Achats de matière première » puisse être reliée aux lots par simple ajout (nouvelle table + clé étrangère), sans refonte.

### 6.4 Stock — unités physiques

Le stock n'est pas un compteur abstrait : il représente des **objets physiques distincts**, chacun suivi individuellement.

| Réf. | Exigence |
|---|---|
| RF-11 | Chaque lot génère un ensemble d'**unités physiques** (ex. 40 sachets de saucisse, 3 jambons). |
| RF-12 | Pour un produit en mode `poids_variable`, chaque unité physique porte un **poids saisi individuellement** au conditionnement (pesée précise, obligatoire, car elle détermine le prix de vente). |
| RF-13 | Chaque unité physique porte un **statut** : `disponible`, `entamé`, `vendu`, `perso`, `perdu`. |
| RF-14 | Le stock disponible d'un produit correspond au nombre d'unités physiques au statut `disponible` (ou `entamé`). |

### 6.5 Ventes et sorties de stock

| Réf. | Exigence |
|---|---|
| RF-15 | Toute sortie de stock (vente, usage personnel, perte) est enregistrée comme un **mouvement rattaché à une unité physique précise**. |
| RF-16 | Un mouvement porte un **type** : `vente`, `perso`, `casse`. |
| RF-17 | Un mouvement de type `vente` doit être **rattaché à un client** (décision 2026-09-04 : V1 limitée à la vente à des particuliers, plus de vente anonyme — remplace la version initiale de cette exigence, cf. RG-07). |
| RF-18 | **Vente d'une unité « en une fois »** (cas standard, ex. sachet de saucisse ou jambon entier) : le montant est calculé (poids × prix/kg du lot, ou prix à la pièce), l'unité passe au statut `vendu`, et le stock diminue automatiquement. |
| RF-19 | **Vente partielle d'une unité (ex. jambon à la tranche)** : plusieurs mouvements de vente peuvent être rattachés à une même unité physique. L'unité passe au statut `entamé` et **ne quitte pas le stock automatiquement**. Chaque vente est pesée (poids de tranche × prix/kg du lot) et rattachée au client concerné. |
| RF-20 | L'utilisateur peut clôturer manuellement une unité `entamé` en la passant au statut `vendu` (ou `perdu`) lorsqu'elle est épuisée. Le poids restant n'est pas suivi. |
| RF-21 | L'utilisateur peut marquer une unité physique en `perso` (autoconsommation) ou `perdu` (invendable/jeté) sans passer par une vente. Ces statuts s'appliquent **unité par unité**. |

### 6.6 Clients

| Réf. | Exigence |
|---|---|
| RF-22 | L'utilisateur peut créer et gérer des **fiches clients** (nom, prénom, téléphone). |
| RF-23 | L'historique des ventes est **rattaché au client**, permettant de retrouver ce qu'un client a acheté. |
| RF-24 | Via le rattachement mouvement → unité physique → lot, le système permet de savoir **quel lot a été vendu à quel client** (base de la traçabilité sanitaire). |

### 6.7 Authentification

| Réf. | Exigence |
|---|---|
| RF-25 | L'accès à l'application requiert une **authentification** (l'application est exposée sur Internet). |
| RF-26 | La V1 fonctionne avec un **compte simple** (partagé), sans gestion de rôles différenciés. |
| RF-27 | Les tables métier clés (lot, mouvement/vente) portent un champ **auteur de création** (`cree_par`), afin de préparer sans coût une future journalisation « qui a fait quoi ». |

---

## 7. Règles de gestion

| Réf. | Règle |
|---|---|
| RG-01 | Le mode de vente (`poids_variable` / `piece_simple`) est une propriété du **produit**, fixée à sa création, et détermine le comportement de saisie du stock et de calcul du prix. |
| RG-02 | Le prix de vente s'applique **par lot** ; il n'existe pas de prix « catalogue » figé au niveau du produit. |
| RG-03 | Pour un produit `poids_variable`, le **prix d'une vente = poids réel × prix au kg du lot**. |
| RG-04 | Une unité physique vendue « en une fois » sort du stock ; une unité vendue en plusieurs fois reste en stock au statut `entamé` jusqu'à clôture manuelle. |
| RG-05 | Le poids restant d'une unité `entamé` n'est **pas** suivi : seule la somme des ventes rattachées est significative (chiffre d'affaires généré par l'unité). |
| RG-06 | Les statuts de sortie (`vendu`, `perso`, `perdu`) sont exclusifs et s'appliquent à l'échelle de l'unité physique individuelle. |
| RG-07 | **(Modifié 2026-09-04, remplace la règle initiale)** Un mouvement de vente doit être rattaché à un client — plus de vente anonyme. V1 limitée à la vente à des particuliers (nom + prénom) ; la vente à des professionnels (raison sociale) est reportée à une évolution ultérieure si le besoin se confirme. |
| RG-08 | Une **unité de mesure** ne peut pas être désactivée tant qu'elle est utilisée comme unité de vente par un **produit actif** (évite qu'un produit en cours de vente référence une unité qu'on retire de l'usage). |
| RG-09 | La **désactivation d'un produit** n'est jamais bloquée, y compris s'il a déjà des lots de production. Elle n'empêche que la création de **nouveaux** lots pour ce produit à l'avenir ; l'historique (lots, stock, ventes) reste consultable normalement. |
| RG-10 | Un **lot de production** reste partiellement modifiable après création (prix de vente, référence matière première, DLC, notes), pour corriger une erreur de saisie. Le produit, la date de production et le numéro de lot sont **définitifs** dès la création : ils sont indissociables du numéro de lot lui-même (§4.1 du modèle de données) et de l'identité du lot. Aucune suppression de lot n'est possible. |
| RG-11 | Contrairement au lot de production, un **mouvement de stock** (vente, perso, perte) reste **modifiable et supprimable** après création — choix assumé pour une activité amateur sans contrainte comptable formelle, plutôt qu'un principe strict d'immuabilité de l'historique. Supprimer le dernier mouvement rattaché à une unité physique la remet au statut `disponible` ; dans les autres cas (ex. une vente parmi plusieurs sur un jambon entamé), le statut de l'unité n'est pas recalculé automatiquement. |
| RG-12 | Une unité physique peut être marquée `perso` ou `perdu` aussi bien depuis le statut `disponible` que depuis `entamé` (ex. un jambon entamé qui tourne peut être déclaré perdu sans repasser par une vente complète). |

---

## 8. Exigences non fonctionnelles

| Réf. | Catégorie | Exigence |
|---|---|---|
| RNF-01 | **Plateforme** | Application web progressive (PWA), pensée **mobile-first** pour l'usage sur le terrain, avec une **vue PC** adaptée (responsive). |
| RNF-02 | **Utilisabilité** | Interface simple et intuitive, adaptée à des utilisateurs non techniques ; parcours de saisie (lot, pesée, vente) optimisés pour la rapidité. |
| RNF-03 | **Autonomie** | Aucune opération courante (création de produits, unités, lots, ventes, clients) ne doit nécessiter d'intervention de développement ou de redéploiement. |
| RNF-04 | **Sécurité** | Accès protégé par authentification ; données hébergées et exposées de manière sécurisée (HTTPS). |
| RNF-05 | **Concurrence** | Bien qu'un usage concurrent simultané soit peu probable, le backend doit gérer proprement les accès concurrents pour éviter toute corruption de données. |
| RNF-06 | **Robustesse** | Solution fiable et maintenable, dimensionnée pour un volume modeste — « robuste, pas un tank ». |
| RNF-07 | **Évolutivité** | Le modèle de données et l'architecture doivent permettre l'ajout des fonctionnalités V2+ (coût de revient, matières premières, recettes versionnées, multi-comptes) **par extension**, sans refonte. |
| RNF-08 | **Performance** | Temps de réponse fluides sur mobile, y compris en conditions de réseau dégradées (usage terrain). |

---

## 9. Vue d'ensemble du modèle de données (indicatif)

> Le modèle détaillé (schéma physique) fait l'objet d'un livrable dédié. Cette section donne une vue conceptuelle des entités et de leurs relations, pour validation métier.

**Entités du noyau V1**

- **Produit** — nom, mode de vente, unité de vente.
- **Unité de mesure** — libellé, abréviation.
- **Lot de production** — produit, date, prix de vente, référence matière première (texte), DLC, auteur de création.
- **Unité physique** — rattachée à un lot ; poids (si applicable), statut.
- **Mouvement** — rattaché à une unité physique ; type (vente/perso/casse), montant, poids vendu, client (optionnel), date, auteur de création.
- **Client** — nom, prénom, téléphone.
- **Utilisateur** — compte d'authentification.

**Relations clés**

- Un `Produit` possède plusieurs `Lots de production`.
- Un `Lot` possède plusieurs `Unités physiques`.
- Une `Unité physique` possède un ou plusieurs `Mouvements` (un seul pour une vente « en une fois », plusieurs pour une unité `entamé`).
- Un `Mouvement` de type vente référence optionnellement un `Client`.
- La chaîne `Mouvement → Unité physique → Lot` assure la traçabilité « client ↔ lot ».

**Points d'extension prévus (V2+)** — sans impact sur le noyau V1 :

- Table `Achat de matière première` reliée aux `Lots` (coût de revient).
- Tables `Recette` et `Version de recette`, avec relation `Lot → Version de recette → Produit`.
- Enrichissement de `Utilisateur` (rôles) et journalisation exploitant le champ `cree_par` déjà présent.

---

## 10. Hypothèses et contraintes

| Réf. | Hypothèse / Contrainte |
|---|---|
| H-01 | Activité annexe, à faible volume ; l'outil n'a pas vocation à gérer une production industrielle. |
| H-02 | La vente reste informelle (particuliers, espèces) ; aucune contrainte de facturation légale en V1. |
| H-03 | Les recettes ne sont pas stockées en V1 ; elles restent hors application jusqu'à une vague ultérieure. |
| H-04 | La matière première n'est pas tracée en V1 (champ texte informatif uniquement). |
| H-05 | L'application sera exposée sur Internet, ce qui impose l'authentification dès la V1. |
| H-06 | Les utilisateurs finaux sont non techniques : la simplicité prime sur la richesse fonctionnelle. |

---

## 11. Risques

| Réf. | Risque | Mitigation |
|---|---|---|
| R-01 | **Charge de saisie** (pesée unité par unité) perçue comme trop lourde et abandon de l'outil. | Optimiser fortement le parcours de saisie mobile ; envisager des saisies groupées/rapides. |
| R-02 | **Sur-ingénierie** au regard d'un besoin modeste. | Discipline de périmètre V1 stricte ; fonctionnalités financières repoussées en V2. |
| R-03 | **Verrouillage du modèle** empêchant les évolutions prévues. | Points d'extension identifiés dès la conception (§9) ; revue d'architecture avant figement du schéma. |
| R-04 | **Adoption** par des utilisateurs non techniques. | Priorité à l'ergonomie (RNF-02) et à l'autonomie (RNF-03). |

---

## 12. Questions ouvertes

| Réf. | Question |
|---|---|
| Q-01 | Choix de la plateforme d'hébergement et de la stratégie d'authentification (à traiter en phase technique). |
| Q-02 | Faut-il, à l'usage, passer à deux comptes distincts avec journalisation dès la V1 ou attendre une vague ultérieure ? (décision reportée à la phase de développement). |
| Q-03 | Existe-t-il des produits futurs (terrines, etc.) dont le mode de vente n'entre pas dans `poids_variable` / `piece_simple` ? (à valider avec les exploitants). |
| Q-04 | **(2026-09-04)** Statut de paiement de la vente (`payée` / `à payer`) : identifié en concevant les vues Ventes, absent du modèle actuel. À ajouter côté backend — voir proposition de schéma dans `data-model.md` §9 (QM-04). |
| Q-05 | **(2026-09-04)** Une vente peut regrouper plusieurs unités physiques vendues en une fois à un client (un numéro, une date, un statut de paiement, un total) — le modèle actuel n'a **aucune entité de regroupement** : chaque `stock_movement` est indépendant. Proposition : entité `sale` (miroir de `production_batch` côté vente), `stock_movement.sale_id` en FK. Voir `data-model.md` §9 (QM-04). Implique aussi RF-17/RG-07 (client désormais obligatoire). |

---

## 13. Glossaire

| Terme | Définition |
|---|---|
| **Lot de production** | Ensemble d'unités d'un même produit fabriquées à une date donnée, avec un prix de vente propre. |
| **Unité physique** | Objet individuel de stock (un sachet, un jambon), suivi séparément avec son poids et son statut. |
| **Mode de vente** | Propriété d'un produit déterminant s'il se vend au poids (`poids_variable`) ou à la pièce (`piece_simple`). |
| **Sortie perso** | Retrait de stock pour autoconsommation ou usage personnel, distinct d'une vente. |
| **DLC** | Date Limite de Consommation. |
| **PWA** | Progressive Web App — application web installable et utilisable en mobilité. |
| **Traçabilité** | Capacité à relier un produit vendu à son lot de production et au client destinataire. |

---

*Fin du document — version 0.2. Ce PRD est un document vivant, appelé à évoluer au fil des vagues de développement.*