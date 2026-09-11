# Feature Specification: Poids encore vendable d'une unité entamée

**Feature Branch**: `feat/remaining-weight`

**Created**: 2026-09-11

**Status**: Draft

**Input**: User description: "Le poids encore vendable d'un jambon entamé doit être visible. Le serveur calcule ce restant (poids pesé de l'unité moins la somme des poids déjà vendus sur elle) et l'expose sur le contrat d'une unité de stock, sans jamais le stocker. Le jambon est désossé et prêt à trancher, donc le chiffre est exact. Trois endroits : la ligne d'une unité entamée dans le détail d'un produit, le total de poids du résumé de cet écran, et le poids affiché par produit dans la liste de stock. Le décompte d'unités ne change pas. RG-05 doit être révisée."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Savoir ce qu'il reste sur un jambon entamé (Priority: P1)

Un jambon est entamé depuis dix jours. Plusieurs tranches ont été vendues à des clients
différents, à des jours différents. L'exploitant ouvre le détail du produit pour préparer la
tournée du samedi et veut savoir s'il peut encore promettre une tranche d'un kilo, ou s'il doit
sortir un jambon neuf.

Aujourd'hui, l'écran n'affiche que le poids pesé à la fabrication. Le jambon de trois kilos
affiche « 3 kg » qu'il soit intact ou qu'il n'en reste que deux tranches. L'exploitant doit
ouvrir chaque vente passée et faire la soustraction de tête, ou aller regarder le jambon dans le
saloir.

**Why this priority**: c'est la demande d'origine et le seul moyen aujourd'hui de répondre à la
question « puis-je encore vendre » sans aller voir physiquement. Elle apporte aussi la donnée dont
les deux histoires suivantes dépendent.

**Independent Test**: vendre deux tranches d'un jambon pesé, puis ouvrir le détail du produit et
lire le restant sur la ligne de ce jambon. Testable seul, et livrable seul.

**Acceptance Scenarios**:

1. **Given** un jambon pesé à 3 kg dont 2,2 kg ont été vendus en plusieurs tranches,
   **When** l'exploitant ouvre le détail du produit,
   **Then** la ligne de ce jambon annonce 800 g encore vendables, en plus de son numéro
   d'étiquette et de son statut.
2. **Given** un jambon pesé à 3 kg sur lequel aucune vente n'a été enregistrée,
   **When** l'exploitant ouvre le détail du produit,
   **Then** la ligne annonce le poids pesé, sans mention d'un restant qui serait identique.
3. **Given** un jambon entamé dont toutes les ventes ont été retirées par correction,
   **When** l'exploitant recharge l'écran,
   **Then** le jambon est redevenu disponible et son poids affiché est de nouveau le poids pesé.
4. **Given** un produit vendu à la pièce,
   **When** l'exploitant ouvre son détail,
   **Then** aucune notion de poids n'apparaît, ni restant ni poids pesé.

---

### User Story 2 — Un total de stock qui dit ce qui est vendable (Priority: P2)

Le résumé en tête du détail d'un produit annonce un nombre d'unités et un poids total. Ce poids
additionne aujourd'hui le poids pesé à la fabrication de chaque unité encore en stock, un jambon
entamé compris. Il surestime donc la marchandise disponible de tout ce qui a déjà été vendu à la
tranche.

L'exploitant lit ce chiffre pour savoir ce qu'il lui reste à écouler. Il doit pouvoir s'y fier.

**Why this priority**: ce chiffre est consulté bien plus souvent que la ligne d'une unité, mais il
n'a de sens qu'une fois le restant disponible (US1). Il corrige une surestimation existante.

**Independent Test**: avec dix sachets de 500 g et un jambon de 3 kg dont 2,2 kg sont vendus,
vérifier que le résumé annonce 5,8 kg et non 8 kg. Le nombre d'unités reste 11.

**Acceptance Scenarios**:

1. **Given** dix sachets pesés à 500 g et un jambon pesé à 3 kg dont 2,2 kg ont été vendus,
   **When** l'exploitant ouvre le détail du produit,
   **Then** le résumé annonce 11 unités en stock et 5,8 kg.
2. **Given** le même stock,
   **When** une tranche de 300 g est vendue sur le jambon entamé,
   **Then** le résumé annonce toujours 11 unités et désormais 5,5 kg.
3. **Given** un jambon entamé sur lequel la totalité du poids pesé a été vendue mais qui n'a pas
   été clôturé,
   **When** l'exploitant ouvre le détail du produit,
   **Then** le jambon compte encore pour une unité et contribue zéro au poids total.

---

### User Story 3 — Le même chiffre sur la liste de stock (Priority: P3)

La liste de stock, écran d'accueil de l'application, affiche pour chaque produit au poids un
poids total. Il souffre exactement de la même surestimation que le résumé du détail.

Deux écrans qui annoncent des poids incohérents pour le même produit useraient la confiance dans
l'outil plus vite que l'absence d'information.

**Why this priority**: sans elle, la correction apportée par US2 crée une contradiction entre deux
écrans. Elle est donc indissociable à terme, mais ne bloque pas la mise en service de US1 et US2.

**Independent Test**: ouvrir la liste de stock et le détail du même produit, et vérifier que les
deux poids sont identiques.

**Acceptance Scenarios**:

1. **Given** un produit dont le détail annonce 5,8 kg,
   **When** l'exploitant ouvre la liste de stock,
   **Then** la ligne de ce produit annonce le même 5,8 kg.
2. **Given** un produit vendu à la pièce,
   **When** l'exploitant ouvre la liste de stock,
   **Then** sa ligne n'affiche aucun poids, comme aujourd'hui.

---

### Edge Cases

- **Unité entamée entièrement tranchée, non clôturée.** Le restant vaut zéro, l'unité compte
  toujours pour une unité en stock. Un résumé du type « 1 unité · 0 g » est le comportement
  attendu : il signale une clôture manuelle à faire, conformément à la clôture manuelle prévue par
  RG-04. Ce n'est pas une incohérence à masquer.
- **Unité jamais pesée** (produit au poids dont la pesée est étalée). Aucun poids pesé, donc aucun
  restant : elle contribue zéro au total, comme aujourd'hui.
- **Unité sortie du stock** (vendue en une fois, perso, perdue). Elle n'apparaît sur aucun de ces
  trois écrans et n'entre dans aucun total, comportement inchangé.
- **Correction ou retrait d'une ligne de vente.** Le restant est recalculé à la demande, donc il
  suit la correction sans geste supplémentaire et sans risque de valeur périmée.
- **Somme des ventes supérieure au poids pesé.** Interdit à l'écriture par le garde-fou serveur de
  RG-05, mais le restant affiché ne doit jamais être négatif, y compris sur une donnée héritée.
- **Produit désactivé conservant du stock.** Aucun changement : les mêmes règles d'affichage
  s'appliquent.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le serveur DOIT exposer, pour chaque unité de stock portant un poids pesé, le poids
  encore vendable de cette unité, égal au poids pesé moins la somme des poids vendus de ses
  mouvements de vente.
- **FR-002**: Ce poids NE DOIT JAMAIS être stocké. Il est calculé à la demande, à chaque lecture.
  Aucune colonne, aucun champ persistant, aucune valeur mise en cache.
- **FR-003**: Le poids encore vendable NE DOIT JAMAIS être négatif. La valeur plancher est zéro.
- **FR-004**: Pour une unité sans poids pesé — produit vendu à la pièce, ou unité au poids pas
  encore pesée — le poids encore vendable DOIT être absent, et non zéro.
- **FR-005**: Pour une unité au statut disponible, sur laquelle aucune vente n'a été enregistrée,
  le poids encore vendable DOIT être égal au poids pesé. La donnée est uniforme pour toute unité
  pesée, elle n'est pas réservée aux unités entamées.
- **FR-006**: Dans le détail d'un produit en stock, la ligne d'une unité entamée DOIT annoncer son
  poids encore vendable.
- **FR-007**: La ligne d'une unité disponible DOIT continuer d'annoncer son poids pesé, sans
  répéter un restant qui lui est identique.
- **FR-008**: Le libellé du restant DOIT énoncer une valeur exacte. Aucune formulation
  d'approximation, du type « environ » ou « estimé », ne DOIT être employée.
- **FR-009**: Le poids total du résumé, en tête du détail d'un produit, DOIT être la somme des
  poids encore vendables des unités en stock, et non la somme de leurs poids pesés.
- **FR-010**: Le poids affiché pour un produit dans la liste de stock DOIT suivre la même règle
  que FR-009, de sorte que les deux écrans annoncent toujours la même valeur.
- **FR-011**: Le décompte d'unités en stock NE DOIT PAS changer. Une unité entamée compte pour une
  unité, y compris lorsque son poids encore vendable est nul.
- **FR-012**: Aucune valeur technique anglaise, aucun statut HTTP et aucun format de donnée brut
  NE DOIVENT apparaître à l'écran du fait de cette fonctionnalité.
- **FR-013**: La règle de gestion RG-05 DOIT être révisée. Sa rédaction actuelle interdit tout
  champ et tout affichage dédiés au poids restant ; la nouvelle rédaction DOIT autoriser un
  restant calculé à la demande et affiché, tout en maintenant l'interdiction de le stocker et le
  garde-fou d'écriture existant. La révision DOIT redescendre dans le PRD, `docs/data-model.md` et
  `CLAUDE.md` dans le même lot de travail.
- **FR-014**: Les produits vendus à la pièce NE DOIVENT être affectés par aucun des changements
  ci-dessus.

### Key Entities

- **Unité de stock** : l'objet physique individuel, porteur de son numéro d'étiquette, de son
  poids pesé et de son statut. Gagne une information dérivée, le poids encore vendable, qui n'est
  pas un attribut mais une lecture.
- **Mouvement de vente** : chaque sortie de type vente rattachée à une unité, porteuse du poids
  vendu. C'est la somme de ces poids qui se retranche du poids pesé.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: l'exploitant obtient le poids encore vendable d'un jambon entamé en ouvrant un seul
  écran, contre autant de consultations de ventes passées qu'il y a eu de tranches aujourd'hui.
- **SC-002**: aucune soustraction mentale n'est nécessaire pour répondre à « puis-je encore vendre
  une tranche d'un kilo ».
- **SC-003**: pour un même produit, le poids annoncé par la liste de stock et celui annoncé par le
  détail sont identiques dans 100 % des cas.
- **SC-004**: le poids total annoncé pour un produit correspond, au gramme près, à la somme des
  poids réellement vendables de ses unités en stock.
- **SC-005**: aucune information de poids restant n'est persistée : une relecture après
  redémarrage du service donne la même valeur, recalculée.

## Assumptions

- **Le jambon est désossé et prêt à trancher.** Il n'y a donc ni os, ni chute de découpe, ni perte
  au séchage à déduire : le poids pesé moins le poids vendu est la quantité réellement vendable.
  C'est ce qui autorise un libellé exact plutôt qu'une estimation (FR-008). Si des produits à
  parer entraient un jour au catalogue, cette hypothèse et FR-008 seraient à rouvrir.
- **La donnée est portée uniformément par toute unité pesée** (FR-005) plutôt que réservée aux
  unités entamées. Un total devient alors une simple somme, sans condition sur le statut, ce qui
  évite deux règles de calcul divergentes.
- **Le garde-fou d'écriture existant reste la seule protection** contre une vente au-delà du poids
  pesé. Cette fonctionnalité ne fait que lire ; elle n'ajoute aucun contrôle.
- **Aucune alerte ni aucun seuil** n'est prévu sur le restant — pas de « jambon presque fini ».
  Les alertes relèvent de la Vague 2.
- **Aucun historique du restant** n'est conservé ni affiché. La question posée est « combien
  reste-t-il maintenant », pas « comment le jambon s'est vidé ».
- **Le nombre d'unités entamées simultanément reste petit** (quelques jambons), une activité
  artisanale annexe. Le volume de données à parcourir pour le calcul n'est pas un enjeu.

## Présentation retenue *(validée le 2026-09-11)*

Maquette validée par l'exploitant pour le détail d'un produit en stock. Elle fixe la mise en
forme, pas la technique.

```
 ‹ Stock

 Jambon sec
 4 unités en stock · 6,45 kg

 10 septembre · 2ᵉ fournée        24,00 €/kg  🗑
 ┌───────────────────────────────────────────────┐
 │ SC-260910-3                          3,000 kg │
 │ ⟨Disponible⟩                            🗑  ⋮ │
 │ ───────────────────────────────────────────── │
 │ SC-260910-4                    pesé 3,000 kg  │
 │ ⟨Entamé⟩  800 g restants                🗑  ⋮ │
 └───────────────────────────────────────────────┘

 8 septembre                      22,00 €/kg  🗑
 ┌───────────────────────────────────────────────┐
 │ SC-260908-1                          2,650 kg │
 │ ⟨Disponible⟩                            🗑  ⋮ │
 │ ───────────────────────────────────────────── │
 │ SC-260908-2                    pesé 2,800 kg  │
 │ ⟨Entamé⟩  0 g restants  à clôturer      🗑  ⋮ │
 └───────────────────────────────────────────────┘
```

- **La date sort de la carte et la surmonte**, en gros, comme un titre de section. Le rang dans la
  journée la suit quand plusieurs fournées partagent la date. Le prix de la fournée et sa
  corbeille s'alignent à droite, sur la même ligne que ce titre.
- **La carte ne contient plus que les unités**, séparées par un filet léger.
- **Chaque unité tient sur deux lignes de hauteur fixe**, pour qu'aucune information ne passe à la
  ligne sur un téléphone. En haut, le numéro d'étiquette, le plus gros caractère de la ligne
  puisque c'est lui qu'on recopie à la main, et à droite le poids pesé. En bas, l'état, puis le
  poids encore vendable, puis les deux boutons.
- **Le poids figure deux fois sur une unité entamée**, et une seule sur une unité intacte. En haut
  en gris, le poids pesé à la fabrication, précédé de « pesé » sur une unité entamée pour lever
  l'ambiguïté. En bas en noir, ce qu'il reste à vendre. Sur une unité intacte, la ligne du bas n'a
  rien à ajouter.
- **Deux corbeilles, deux portées.** Celle du titre supprime la fournée entière, comportement
  existant. Celle d'une ligne supprime cette unité seule, correction d'une erreur de pesée, et
  s'éteint dès que l'unité porte un mouvement, ce que le serveur refuse déjà.
- **« Déclarer une perte » change d'icône** dans le menu à trois points. Sa corbeille actuelle
  entrerait en collision de sens avec la corbeille de suppression, à quelques millimètres d'elle.
- **La mention « à clôturer »** accompagne un poids restant nul. Elle est validée sur l'esthétique
  et reste décorative pour l'instant : aucune action ne lui est rattachée dans cette vague. La
  clôture continue de passer par le menu à trois points.
