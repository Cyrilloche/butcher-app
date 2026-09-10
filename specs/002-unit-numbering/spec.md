# Feature Specification: Le numéro d'étiquette porté par l'unité

**Feature Branch**: `feat/unit-numbering` *(à créer ; le travail précédent est sur `feat/update-product`)*

**Created**: 2026-09-10

**Status**: Draft

**Input**: User description: "Le numéro recopié à la main sur l'étiquette doit identifier l'unité physique (le sachet, le jambon), et non plus la fabrication. Chaque unité porte CODE-YYMMDD-N, avec N qui court par produit et par date de production, sans jamais être réattribué, y compris après la suppression d'une fabrication. Dix sachets fabriqués le matin du 10/09 prennent SC-260910-1 à SC-260910-10 ; une seconde fournée le même jour continue à SC-260910-11. La fabrication reste une entité, pour saisir en une fois le prix, la date, la DLC et la matière première d'une fournée, mais perd son numéro visible."

## Contexte

Le numéro `CODE-YYMMDD-N` a été conçu pour être recopié à la main sur l'étiquette collée sur un
sachet ou un jambon. Il identifie aujourd'hui la **fabrication**, pas l'objet. Comme une fabrication
donne plusieurs objets, l'écran de stock a dû inventer un second niveau pour les distinguer : le
numéro de la fabrication suivi du rang de l'objet dans celle-ci, ce qui produit `SC-260910-2-1`.

Ce double numéro a été lu, lors de la validation du 10 septembre 2026, comme la marque d'un
sous-lot. La réaction de l'utilisateur a été sans ambiguïté : « ce n'est pas de l'industriel, il
faut faire simple ». Le modèle mental attendu est plus direct. Un numéro, un objet. Le matin on
fabrique dix sachets, ils portent 1 à 10 ; l'après-midi on en fait cinq de plus, ils portent 11 à
15.

La fabrication ne disparaît pas pour autant. Elle reste le moment où l'on saisit **une seule fois**
le prix, la date, la date limite de consommation et la matière première d'une fournée entière.
Cette saisie groupée est précisément ce qui rend l'outil supportable face au carnet papier. Elle
cesse simplement de porter un numéro que personne n'écrit sur une étiquette.

## Clarifications

### Session 2026-09-10

- Q : Qu'écrit-on à la main sur l'étiquette d'un sachet, quand dix sachets sortent de la même
  fournée ? → R : Un numéro par sachet. Les dix vont de `SC-260910-1` à `SC-260910-10`, et une
  seconde fournée le même jour continue à `SC-260910-11`. La fabrication reste en coulisse pour la
  saisie groupée et ne s'affiche plus par un numéro.
- Q : Faut-il renuméroter les unités déjà en base, dont les étiquettes physiques existent ? → R :
  Non. Ce qui est en base est du jeu d'essai, effaçable, sur le poste de développement comme sur
  l'instance déployée. Aucune étiquette réelle n'est en circulation, l'exploitation ne commençant
  qu'après la Vague 1.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Étiqueter chaque objet fabriqué (Priority: P1)

Après avoir fabriqué et pesé une fournée, la personne prend son stylo et recopie sur chaque
étiquette le numéro que l'application affiche en face de l'objet correspondant. Chaque objet a son
numéro, et deux objets n'ont jamais le même.

**Why this priority**: C'est la raison d'être de la fonctionnalité. Sans elle, le geste central de
l'activité, écrire un numéro sur une étiquette, reste ambigu.

**Independent Test**: Créer une fournée, générer ses unités, ouvrir le détail du stock et lire les
numéros. Livre déjà toute la valeur, même si rien d'autre ne change.

**Acceptance Scenarios**:

1. **Given** un produit de code `SC` et aucune fabrication ce jour, **When** on crée une fournée du
   10/09/2026 et qu'on y génère dix unités, **Then** ces unités portent `SC-260910-1` à
   `SC-260910-10`.
2. **Given** la fournée du matin déjà numérotée jusqu'à `SC-260910-10`, **When** on crée une seconde
   fournée le même jour pour le même produit et qu'on y génère cinq unités, **Then** ces unités
   portent `SC-260910-11` à `SC-260910-15`.
3. **Given** deux produits différents fabriqués le même jour, **When** on lit leurs numéros,
   **Then** chaque produit a sa propre suite repartant de 1.
4. **Given** une unité affichée à l'écran, **When** on lit son numéro, **Then** il ne comporte que
   trois segments : le code du produit, la date, le rang.

---

### User Story 2 - Retrouver un objet à partir de son étiquette (Priority: P1)

Un sachet sort du congélateur, son étiquette porte `SC-260910-7`. La personne doit pouvoir le
retrouver dans l'application pour le vendre, le sortir en perso ou le déclarer perdu, et savoir de
quelle fournée il vient.

**Why this priority**: Un numéro qui ne permet pas de remonter à l'objet ne sert à rien. C'est la
traçabilité, valeur métier centrale du projet.

**Independent Test**: Prendre un numéro au hasard dans le stock, le chercher à l'écran, vérifier
qu'on tombe sur la bonne unité avec le bon poids et la bonne fournée.

**Acceptance Scenarios**:

1. **Given** une unité `SC-260910-7` en stock, **When** on ouvre le détail du stock de son produit,
   **Then** l'unité apparaît sous ce numéro, avec son poids et son statut.
2. **Given** cette unité vendue, **When** on ouvre la vente qui la contient, **Then** la ligne
   nomme l'unité par son numéro d'étiquette.
3. **Given** cette unité sortie en perte, **When** on consulte l'historique des mouvements,
   **Then** le numéro affiché est celui écrit sur l'étiquette physique.

---

### User Story 3 - Saisir une fournée sans lui inventer un numéro (Priority: P2)

La personne enregistre une fabrication : le produit, la date, le prix du jour, éventuellement la
date limite de consommation et la matière première. Elle ne se voit proposer aucun numéro de lot,
et n'en cherche aucun à l'écran.

**Why this priority**: La saisie groupée doit survivre au changement. Cette histoire vérifie que la
fabrication reste utile après avoir perdu son numéro.

**Independent Test**: Créer une fournée et parcourir tous les écrans où elle apparaît, en vérifiant
qu'aucun numéro de fabrication n'est demandé ni affiché.

**Acceptance Scenarios**:

1. **Given** l'écran de création d'une fabrication, **When** on le remplit, **Then** aucun champ ni
   aucune mention de numéro de lot n'apparaît.
2. **Given** un produit ayant deux fournées le même jour à des prix différents, **When** on ouvre
   le détail de son stock, **Then** les deux groupes sont distingués par leur date, leur prix et
   leur rang dans la journée, et chacun liste ses unités par leur numéro.
3. **Given** un prix propre à une fournée, **When** on vend une unité qui en provient, **Then** le
   prix appliqué reste celui de sa fournée d'origine.

---

### User Story 4 - Supprimer une fournée créée par erreur (Priority: P2)

Une fabrication saisie à tort est supprimée, avec les objets qu'elle avait générés. Les numéros
qu'elle avait consommés ne reviennent jamais dans la circulation.

**Why this priority**: La règle anti-réémission existe déjà au niveau de la fabrication. Elle doit
descendre au niveau de l'unité sans se perdre en route, sous peine de faire coexister deux
étiquettes manuscrites identiques.

**Independent Test**: Supprimer une fournée intacte, en recréer une le même jour, vérifier que la
numérotation reprend après le dernier numéro émis.

**Acceptance Scenarios**:

1. **Given** une fournée dont les unités portent `SC-260910-1` à `SC-260910-10` et dont rien n'est
   sorti du stock, **When** on la supprime, **Then** ses unités disparaissent.
2. **Given** cette suppression, **When** on crée une nouvelle fournée le même jour pour le même
   produit avec trois unités, **Then** ces unités portent `SC-260910-11` à `SC-260910-13`.
3. **Given** une unité seule supprimée pour corriger une erreur de pesée, **When** on génère une
   unité de plus dans la même fournée, **Then** elle ne reprend pas le numéro libéré.

---

### Edge Cases

- **Pesée étalée dans le temps.** Les unités d'une fournée sont générées par un geste distinct de
  la création de la fournée. Si une seconde fournée du même jour est numérotée entre-temps, les
  numéros d'une même fournée peuvent ne pas être contigus. C'est accepté : l'unicité et la
  non-réémission sont les propriétés qui comptent, pas la contiguïté.
- **Fabrication saisie rétroactivement.** Une fournée enregistrée le 12 pour une production du 10
  prend des numéros de la suite du 10, pas de celle du 12.
- **Onzième unité d'une journée à un chiffre.** La suite n'est pas rembourrée de zéros : après
  `SC-260910-9` vient `SC-260910-10`. Le tri à l'écran suit l'ordre de création, pas l'ordre
  alphabétique du numéro.
- **Produit désactivé.** Les numéros de ses unités restent lisibles dans les ventes et l'historique.
- **Deux fournées du même produit, même jour, même prix.** Leurs groupes portent le même intitulé à
  l'écran ; leur rang dans la journée les distingue.
- **Correction du code d'un produit.** Elle n'est possible que tant qu'aucune fabrication n'existe,
  donc tant qu'aucun numéro n'a été émis. Aucun numéro déjà écrit ne peut donc devenir faux.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Toute unité de stock DOIT recevoir un numéro d'étiquette au moment de sa création.
- **FR-002**: Ce numéro DOIT suivre le format `CODE-YYMMDD-N` : le code du produit, la date de
  production de sa fournée d'origine sur six chiffres, et un rang entier sans rembourrage.
- **FR-003**: Le rang DOIT courir par produit et par date de production, en repartant de 1 pour
  chaque nouveau couple produit / date.
- **FR-004**: Un rang émis NE DOIT jamais être réattribué, y compris après la suppression de
  l'unité qui le portait ou de la fournée dont elle provenait.
- **FR-005**: Le numéro DOIT être unique dans toute l'application, et cette unicité DOIT être
  garantie par le système, pas par la vigilance de l'utilisateur.
- **FR-006**: Le numéro DOIT être attribué par le système. L'utilisateur NE DOIT jamais le saisir
  ni pouvoir le modifier après coup.
- **FR-007**: La fabrication NE DOIT plus porter de numéro visible, ni en saisie, ni en affichage,
  ni dans un message d'erreur ou de confirmation.
- **FR-008**: Le détail du stock DOIT présenter chaque unité par son numéro d'étiquette, et
  regrouper les unités par fournée en annonçant chaque groupe par sa date de production, son prix
  et, si plusieurs fournées partagent la date, son rang dans la journée.
- **FR-009**: Tout écran nommant une unité — saisie d'une vente, détail d'une vente, historique des
  mouvements, solde du stock d'un produit — DOIT la nommer par son numéro d'étiquette.
- **FR-010**: La suppression d'une fournée DOIT rester possible tant qu'aucune de ses unités n'est
  sortie du stock, supprimer ses unités, et ne libérer aucun numéro.
- **FR-011**: Les numéros DOIVENT rester lisibles dans l'historique après la vente d'une unité, la
  suppression de sa fournée impossible, ou la désactivation de son produit.
- **FR-012**: Le registre qui garantit la non-réémission DOIT compter des unités par produit et par
  date de production, et NE DOIT jamais être vidé.
- **FR-013**: Le numéro publié par le service aux écrans DOIT désigner l'unité, et celui de la
  fabrication DOIT cesser de l'être. C'est une rupture de compatibilité assumée entre les deux
  applications, à signaler comme telle à la publication.
- **FR-014**: Aucune reprise des données existantes N'EST requise. Les données de développement
  sont un jeu d'essai que le passage à ce modèle peut effacer.
- **FR-015**: Le système NE DOIT afficher aucun numéro comportant plus de trois segments.

### Key Entities

- **Unité de stock** : l'objet physique, sachet ou jambon. Gagne un numéro d'étiquette, unique et
  définitif, qui devient son identité aux yeux de l'utilisateur. Conserve son poids, son statut et
  son rattachement à une fournée.
- **Fabrication (fournée)** : une production datée d'un produit, portant le prix, la date limite de
  consommation et la matière première communs à ses unités. Perd son numéro visible ; reste le lieu
  de la saisie groupée et le porteur du prix appliqué aux ventes.
- **Registre de numérotation** : la mémoire du dernier rang émis pour un produit et une date de
  production. Ne stocke rien d'autre, n'est jamais purgé, et est la seule source du prochain rang.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Le numéro à recopier à la main comporte trois segments au lieu de quatre, et reste
  strictement plus court que le numéro affiché aujourd'hui pour la même unité.
- **SC-002**: Sur un jeu de dix fabrications réparties sur trois jours et deux produits, aucun
  numéro n'apparaît deux fois.
- **SC-003**: Après suppression d'une fournée puis création d'une nouvelle le même jour, aucun
  numéro déjà émis n'est réutilisé, vérifié en rejouant le scénario dix fois de suite.
- **SC-004**: Un utilisateur à qui l'on tend une étiquette retrouve l'unité correspondante à
  l'écran en moins de trente secondes, sans aide.
- **SC-005**: Aucun écran de l'application ne mentionne un numéro de fabrication, vérifié par un
  parcours complet des vues Stock, Produits et Ventes.

## Assumptions

- Les données présentes en base, en développement comme sur l'instance déployée, sont un jeu
  d'essai recréable : l'exploitation réelle ne commence qu'après la Vague 1. Aucune étiquette
  physique portant l'ancien format n'est en circulation, donc aucune cohabitation des deux formats
  n'est à gérer.
- Deux fournées d'un même produit le même jour restent rares. Les distinguer par leur date, leur
  prix et leur rang dans la journée suffit ; leur inventer un identifiant propre reviendrait à
  réintroduire le numéro qu'on supprime.
- Le prix reste porté par la fournée et non par l'unité. Une unité n'a pas de prix propre.
- La recherche d'une unité par saisie de son numéro n'est pas dans ce périmètre. On retrouve une
  unité en ouvrant le stock de son produit, comme aujourd'hui.
- La numérotation reste calée sur la date de production de la fournée, jamais sur la date de
  génération des unités, afin que le numéro écrit sur l'étiquette parle de la fabrication.
- Le rang n'est pas rembourré de zéros. Le format reste celui déjà validé auprès de l'utilisateur,
  seul son porteur change.
- Cette fonctionnalité remplace la règle de numérotation décrite dans `docs/data-model.md` §3.9 et
  la contrainte C-12. Ces documents seront mis à jour dans le même lot de travail, conformément au
  principe IV de la constitution.
