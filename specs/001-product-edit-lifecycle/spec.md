# Feature Specification: Modification et fin de vie d'un produit

**Feature Branch**: `feat/update-product`

**Created**: 2026-09-09

**Status**: Draft

**Input**: User description: "Nous allons mettre à jour la gestion des produits. Actuellement il n'est pas possible de modifier un produit créé. Cependant les utilisateurs tests m'ont spécifié qu'il devrait être possible de modifier un produit nouvellement créé si celui-ci n'est pas encore associé à un stock / vente. Si c'est le cas, le produit devra être désactivé une fois tous les stocks vendus pour en recréer un nouveau. Je voudrais que l'on prépare cette fonctionnalité." Complété en session : possibilité de supprimer un lot de production, unicité du code maintenue globalement, désactivation bloquante assortie d'une action de solde des unités restantes.

## Contexte

Les utilisateurs se trompent en créant un produit : une faute dans le nom, un code mal choisi, un
mode de vente pris à l'envers. Aujourd'hui l'écran de détail d'un produit ne laisse corriger que le
nom et l'autorisation de vente à la tranche. Le code et le mode de vente restent figés à vie, alors
qu'ils sont précisément les champs qu'on se trompe à saisir la première fois.

L'enjeu métier est la traçabilité : dès qu'un produit a servi à fabriquer un lot, son code apparaît
dans des numéros de lot recopiés à la main sur des étiquettes physiques, et son mode de vente
détermine la lecture des ventes passées. Le corriger rétroactivement mentirait sur l'historique.

D'où trois régimes complémentaires. Un produit qui n'a jamais servi se corrige librement. Un produit
qui a servi se fige, sauf sur ses libellés. Et parce qu'un lot créé par erreur ne doit pas figer un
produit à vie, un lot dont rien n'est encore sorti du stock peut être supprimé, ce qui ramène le
produit à son état d'origine.

## Clarifications

### Session 2026-09-09

- Q: Quel poids est enregistré sur la sortie de type perte produite par le solde des unités restantes ? → A: Le poids pesé de l'unité si elle est disponible, le restant estimé (poids pesé moins ce qui a déjà été vendu) si elle est entamée — même convention que le menu de sortie unité par unité de Détail Stock.
- Q: Le solde porte-t-il sur toutes les unités restantes en bloc, ou sur une sélection, et le type de sortie est-il choisi ? → A: Sélection des unités par l'utilisatrice, type de sortie toujours « perte ». Le choix d'un autre type sur l'action groupée est écarté pour cette version et rouvrable si l'usage le demande.
- Q: Comment sont traitées deux modifications concurrentes d'une même fiche produit ? → A: Dernière écriture gagnante, aucun jeton de version. Les garde-fous métier (verrouillage, stock restant, unicité du code) étant revérifiés au moment de l'enregistrement, un formulaire périmé ne peut produire qu'une saisie perdue, jamais un état illégal.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Corriger un produit qui n'a jamais servi (Priority: P1)

Une utilisatrice vient de créer le produit « Saucisson sec » avec le code `SC` et se rend compte,
avant toute fabrication, qu'elle voulait le code `SEC` et le mode « au poids » plutôt qu'« à la
pièce ». Elle ouvre le détail du produit, corrige les champs et enregistre. Le produit est corrigé
comme s'il venait d'être créé.

**Why this priority**: c'est la demande directe des utilisateurs tests et le cas de loin le plus
fréquent, une erreur de saisie détectée dans les minutes qui suivent la création. Sans elle, la
seule issue est de vivre avec l'erreur ou de demander une intervention technique.

**Independent Test**: créer un produit, ne lui rattacher aucun lot, modifier ses quatre champs et
vérifier que la fiche reflète les nouvelles valeurs. Livre à lui seul la correction d'erreur de
saisie.

**Acceptance Scenarios**:

1. **Given** un produit sans aucun lot de production, **When** l'utilisatrice modifie son nom, son
   code, son mode de vente et l'autorisation de vente à la tranche, **Then** les quatre valeurs sont
   enregistrées et affichées sur la fiche.
2. **Given** un produit sans aucun lot de production, **When** l'utilisatrice saisit un code déjà
   porté par un autre produit, actif ou désactivé, **Then** l'enregistrement est refusé avec un
   message en français nommant le conflit, et aucune valeur n'est modifiée.
3. **Given** un produit vendu à la pièce et sans lot, **When** l'utilisatrice active la vente à la
   tranche sans passer en vente au poids, **Then** l'enregistrement est refusé avec une explication
   en français.
4. **Given** un produit sans lot, **When** l'utilisatrice ouvre sa fiche, **Then** tous les champs
   sont modifiables et aucun avertissement de verrouillage n'est affiché.

---

### User Story 2 - Comprendre ce qui est verrouillé sur un produit déjà utilisé (Priority: P1)

Un utilisateur ouvre la fiche d'un produit qui a déjà servi à fabriquer plusieurs lots. Le nom et
l'autorisation de vente à la tranche restent modifiables, le code et le mode de vente sont
présentés comme figés, avec l'explication de la raison. Il comprend immédiatement que pour changer
le code, il faut désactiver ce produit et en créer un nouveau.

**Why this priority**: sans cette lisibilité, un champ grisé sans explication est vécu comme un
bug par des utilisateurs non techniques, et le contournement à inventer n'est pas devinable.

**Independent Test**: ouvrir la fiche d'un produit rattaché à au moins un lot et vérifier que les
champs figés sont clairement identifiés et expliqués. Testable sans la partie désactivation.

**Acceptance Scenarios**:

1. **Given** un produit rattaché à au moins un lot de production, **When** l'utilisateur ouvre sa
   fiche, **Then** le code et le mode de vente sont affichés en lecture seule avec une explication
   en français, et le nom et l'autorisation de vente à la tranche restent modifiables.
2. **Given** un produit rattaché à au moins un lot, **When** une modification du code ou du mode de
   vente est tout de même soumise, **Then** elle est refusée et le produit reste inchangé.
3. **Given** un produit rattaché à au moins un lot, **When** l'utilisateur modifie son nom, **Then**
   la modification est enregistrée et les numéros de lot déjà émis restent inchangés.

---

### User Story 3 - Supprimer un lot de production créé par erreur (Priority: P1)

Une utilisatrice crée un lot sur le mauvais produit, ou saisit une date de production erronée avant
d'avoir vendu quoi que ce soit. Elle ouvre le lot et le supprime. Les unités de stock qu'il avait
générées disparaissent avec lui. Si ce lot était le seul du produit, le produit redevient
entièrement modifiable.

**Why this priority**: c'est la soupape qui rend le verrouillage de l'histoire 2 acceptable. Sans
elle, un seul lot créé par mégarde condamne un produit à garder un code erroné pour toujours.

**Independent Test**: créer un lot, ne vendre aucune de ses unités, le supprimer, et vérifier la
disparition du lot et de ses unités ainsi que le retour du produit à l'état modifiable.

**Acceptance Scenarios**:

1. **Given** un lot dont aucune unité n'a été vendue, consommée en perso ni perdue, **When**
   l'utilisatrice le supprime après confirmation, **Then** le lot et toutes ses unités de stock
   disparaissent et n'apparaissent plus dans le stock disponible.
2. **Given** un lot dont au moins une unité a fait l'objet d'une sortie de stock, **When**
   l'utilisatrice tente de le supprimer, **Then** la suppression est refusée avec un message en
   français indiquant le nombre d'unités déjà sorties.
3. **Given** un produit dont l'unique lot vient d'être supprimé, **When** l'utilisatrice ouvre sa
   fiche, **Then** le code et le mode de vente redeviennent modifiables.
4. **Given** un lot supprimé portant le numéro `SC-250831-2`, **When** un nouveau lot est créé pour
   le même produit et la même date, **Then** il reçoit un numéro non encore émis et jamais
   `SC-250831-2`.
5. **Given** un produit ayant deux lots, **When** l'un des deux est supprimé, **Then** le produit
   reste verrouillé sur son code et son mode de vente.

---

### User Story 4 - Retirer un produit du service une fois son stock écoulé (Priority: P2)

Le produit dont le code était erroné a déjà servi et ne peut plus être supprimé par la suppression
de ses lots. Ses dernières unités restent en stock sans perspective de vente. L'utilisatrice les
solde en perte depuis la fiche produit, puis désactive le produit. Il disparaît des listes de choix
pour une nouvelle production ou une nouvelle vente, mais reste visible dans l'historique des lots et
des ventes. Elle crée ensuite le produit de remplacement, avec un code distinct.

**Why this priority**: c'est la fin de vie propre d'un produit, mais elle n'a de valeur qu'une fois
le verrouillage compris et la suppression de lot disponible.

**Independent Test**: amener toutes les unités d'un produit hors du stock disponible, le désactiver,
vérifier son absence des écrans de saisie et sa présence dans l'historique.

**Acceptance Scenarios**:

1. **Given** un produit dont aucune unité n'est disponible ni entamée, **When** l'utilisatrice le
   désactive après confirmation, **Then** il n'apparaît plus dans le choix d'un produit pour une
   nouvelle production ni pour une nouvelle vente.
2. **Given** un produit désactivé, **When** l'utilisateur consulte un lot ou une vente historique le
   concernant, **Then** le produit y est affiché normalement, signalé comme désactivé.
3. **Given** un produit ayant encore au moins une unité disponible ou entamée, **When**
   l'utilisatrice tente de le désactiver, **Then** l'opération est refusée avec un message indiquant
   en français le nombre d'unités restant à écouler, et l'action de solde en perte lui est proposée.
4. **Given** un produit ayant encore des unités disponibles ou entamées, **When** l'utilisatrice
   sélectionne toutes ces unités et confirme leur solde, **Then** chaque unité sélectionnée reçoit
   une sortie de stock de type perte, portant son poids pesé si elle était disponible et son restant
   estimé si elle était entamée, et le produit devient désactivable.
5. **Given** un produit ayant trois unités restantes, **When** l'utilisatrice n'en sélectionne que
   deux et confirme, **Then** seules ces deux unités sortent du stock et la désactivation du produit
   reste refusée à cause de la troisième.
6. **Given** un produit vendu à la pièce, dont les unités n'ont pas de poids, **When** l'utilisatrice
   solde ses unités restantes, **Then** les sorties de type perte sont enregistrées sans poids.
7. **Given** un produit désactivé, **When** l'utilisatrice le réactive après confirmation, **Then**
   il redevient disponible dans les écrans de saisie.
8. **Given** un produit désactivé portant le code `SC`, **When** l'utilisatrice crée un nouveau
   produit avec ce même code, **Then** la création est refusée et un code distinct lui est demandé.

---

### Edge Cases

- Un lot de production existe mais n'a jamais généré d'unité de stock : il compte comme utilisation
  du produit, et sa suppression est possible puisque aucune unité n'est sortie.
- Deux sessions modifient la même fiche produit en même temps : la dernière écriture l'emporte, sans
  détection de conflit. Le cas est jugé rare et sans conséquence sur la cohérence, les garde-fous
  étant revérifiés à l'enregistrement.
- Un produit devient utilisé, ou un lot devient non supprimable, entre l'ouverture de l'écran et
  l'enregistrement : la vérification faisant foi est celle du moment de l'enregistrement, et
  l'opération est refusée avec l'explication correspondante.
- Passer un produit de « au poids » à « à la pièce » alors que la vente à la tranche était autorisée
  doit désactiver cette autorisation, les deux étant incompatibles.
- Un produit déjà désactivé pour lequel on demande une nouvelle désactivation : l'opération reste
  sans effet plutôt que d'échouer.
- Le code saisi en minuscules doit être traité comme le même code que sa version en majuscules pour
  le contrôle d'unicité.
- Une unité entamée, donc partiellement vendue, appartient à un lot non supprimable et interdit à
  elle seule la désactivation du produit tant qu'elle n'est pas clôturée ou soldée.
- Le solde en perte est demandé sur un produit dont toutes les unités sont déjà sorties : l'action
  reste sans effet plutôt que d'échouer.
- L'action de solde est confirmée sans qu'aucune unité n'ait été sélectionnée : elle reste sans
  effet plutôt que d'échouer.
- Une unité sort du stock par une autre voie entre l'affichage de la liste de solde et la
  confirmation : les unités déjà sorties sont ignorées sans faire échouer le solde des autres.

## Requirements *(mandatory)*

### Functional Requirements

#### Modification d'un produit

- **FR-001**: Le système MUST distinguer deux états de cycle de vie d'un produit, « jamais utilisé »
  et « utilisé », et exposer cet état sur la fiche produit.
- **FR-002**: Un produit MUST être considéré comme utilisé dès lors qu'au moins un lot de production
  lui est rattaché, indépendamment des unités de stock ou des ventes qui en découlent.
- **FR-003**: Un produit jamais utilisé MUST être modifiable sur l'ensemble de ses champs
  descriptifs : nom, code, mode de vente et autorisation de vente à la tranche.
- **FR-004**: Un produit utilisé MUST refuser toute modification de son code et de son mode de
  vente, et MUST rester modifiable sur son nom et son autorisation de vente à la tranche.
- **FR-005**: La distinction MUST être appliquée côté serveur indépendamment de ce que présente
  l'interface, une interface permissive ne devant jamais suffire à contourner le verrouillage.
- **FR-006**: L'interface MUST présenter les champs verrouillés en lecture seule, accompagnés d'une
  explication en français de la raison du verrouillage et de la marche à suivre alternative.
- **FR-007**: Le code d'un produit MUST rester unique sur l'ensemble des produits, actifs comme
  désactivés, la comparaison étant insensible à la casse, à la création comme à la modification.
- **FR-008**: Le système MUST refuser d'autoriser la vente à la tranche sur un produit qui n'est pas
  vendu au poids, à la création comme à la modification.
- **FR-009**: Le passage d'un mode de vente au poids vers un mode à la pièce, sur un produit jamais
  utilisé, MUST désactiver l'autorisation de vente à la tranche.

#### Suppression d'un lot de production

- **FR-010**: Un lot de production MUST pouvoir être supprimé tant qu'aucune de ses unités de stock
  n'a fait l'objet d'une sortie de stock, qu'elle soit vente, usage personnel ou perte.
- **FR-011**: La suppression d'un lot MUST supprimer avec lui l'intégralité des unités de stock
  qu'il a générées.
- **FR-012**: Le système MUST refuser la suppression d'un lot dont au moins une unité a fait l'objet
  d'une sortie de stock, et MUST indiquer le nombre d'unités concernées.
- **FR-013**: Un numéro de lot déjà émis MUST ne jamais être réattribué à un lot ultérieur, y
  compris après la suppression du lot qui le portait.
- **FR-014**: La suppression d'un lot MUST demander une confirmation explicite mentionnant le nombre
  d'unités de stock qui disparaîtront.
- **FR-015**: Un produit dont le dernier lot est supprimé MUST redevenir « jamais utilisé » et donc
  entièrement modifiable.

#### Désactivation d'un produit

- **FR-016**: Le système MUST refuser la désactivation d'un produit tant qu'il lui reste au moins
  une unité de stock disponible ou entamée, et MUST indiquer le nombre d'unités concernées.
- **FR-017**: Le système MUST offrir une action de solde qui enregistre une sortie de stock de type
  perte sur les unités disponibles ou entamées d'un produit, afin de rendre sa désactivation
  possible.
- **FR-018**: L'action de solde MUST présenter les unités restantes du produit et laisser
  l'utilisatrice choisir celles à solder, la sélection de la totalité devant rester immédiate.
- **FR-019**: Le type de sortie produit par l'action de solde MUST toujours être « perte ». Aucun
  autre type n'est proposé sur cette action groupée ; une sortie d'un autre type reste enregistrable
  unité par unité depuis Détail Stock.
- **FR-020**: Le poids porté par chaque sortie de type perte issue du solde MUST être le poids pesé
  de l'unité lorsqu'elle est disponible, et le restant estimé, soit son poids pesé diminué de ce qui
  en a déjà été vendu, lorsqu'elle est entamée. Cette convention MUST être identique à celle du menu
  de sortie unité par unité.
- **FR-021**: Le solde d'une unité entamée MUST la faire passer à un état sorti du stock, au même
  titre qu'une clôture manuelle, sans laisser d'unité entamée résiduelle sur le produit.
- **FR-022**: L'action de solde MUST demander une confirmation explicite mentionnant le nombre
  d'unités sélectionnées qui seront déclarées perdues, et MUST être irréversible une fois confirmée.
- **FR-023**: Un produit désactivé MUST être exclu des listes de sélection de la création d'un lot
  de production et de la saisie d'une vente.
- **FR-024**: Un produit désactivé MUST rester consultable et affiché dans l'historique des lots,
  des unités de stock et des ventes qui le référencent, signalé comme désactivé.
- **FR-025**: Un produit désactivé MUST pouvoir être réactivé.
- **FR-026**: La désactivation et la réactivation MUST demander une confirmation explicite avant
  d'être appliquées.

#### Transverse

- **FR-027**: Toute modification, désactivation ou réactivation MUST horodater la mise à jour du
  produit.
- **FR-028**: Le système MUST revérifier au moment de l'enregistrement l'état de verrouillage du
  produit, l'unicité de son code, l'absence de sortie sur les unités d'un lot supprimé et le stock
  restant avant désactivation, sans se fier à l'état affiché au chargement de l'écran.
- **FR-029**: Aucune détection de conflit d'édition concurrente n'est requise : sur deux écritures
  simultanées d'une même fiche, la dernière l'emporte.
- **FR-030**: Les messages d'erreur et de refus MUST être rédigés en français et décrire l'action à
  entreprendre, sans exposer de vocabulaire technique anglais.

### Key Entities

- **Produit**: le produit fabriqué. Porte un code court destiné à être recopié à la main sur une
  étiquette, un nom affiché, un mode de vente (au poids ou à la pièce), une autorisation de vente à
  la tranche et un indicateur d'activité. Cette fonctionnalité ajoute à sa lecture un état dérivé
  « jamais utilisé / utilisé », calculé depuis ses lots et jamais saisi.
- **Lot de production**: rattaché à un produit, porte le numéro de lot dérivé du code produit. Son
  existence fige le code et le mode de vente du produit ; sa suppression, quand elle est permise,
  lève ce gel.
- **Unité de stock**: objet physique issu d'un lot. L'absence de sortie de stock sur toutes les
  unités d'un lot conditionne la suppression de ce lot. L'absence d'unité disponible ou entamée
  conditionne la désactivation du produit.
- **Sortie de stock**: vente, usage personnel ou perte rattachée à une unité. Sa présence est ce qui
  rend un lot non supprimable, et le solde en perte est la façon de la produire en masse.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Une utilisatrice corrige une erreur de saisie sur un produit qu'elle vient de créer en
  moins de 30 secondes, sans quitter la fiche du produit.
- **SC-002**: 100 % des tentatives de modification du code ou du mode de vente d'un produit déjà
  utilisé sont refusées, y compris lorsqu'elles contournent l'interface.
- **SC-003**: Sur une fiche de produit verrouillé, un utilisateur non technique explique sans aide
  extérieure pourquoi le champ est figé et quelle est la marche à suivre.
- **SC-004**: Aucun numéro de lot déjà émis ne change de valeur ni n'est réattribué à la suite d'une
  modification de produit ou d'une suppression de lot, vérifiable sur l'ensemble de l'historique.
- **SC-005**: Une utilisatrice annule un lot créé par erreur et retrouve un produit entièrement
  modifiable en moins d'une minute, sans intervention technique.
- **SC-006**: Aucune sortie de stock existante n'est perdue par une suppression de lot, sur la
  totalité des tentatives.
- **SC-007**: Un produit désactivé n'apparaît dans aucun écran de saisie et reste visible dans
  100 % des écrans d'historique qui le référencent.
- **SC-008**: Le nombre de demandes d'intervention technique pour corriger un produit ou un lot mal
  saisi tombe à zéro sur les cas couverts par cette fonctionnalité.

## Assumptions

- Le verrouillage porte sur le code et le mode de vente uniquement. Le nom et l'autorisation de
  vente à la tranche restent modifiables à tout moment : le nom est un libellé d'affichage sans
  effet rétroactif, et l'autorisation de vente à la tranche ne régit que les ventes futures.
- L'unicité globale du code, y compris sur les produits désactivés, est retenue pour cette version.
  Elle protège la lisibilité des étiquettes manuscrites au prix d'un code de remplacement à inventer.
  Le point est explicitement rouvrable si l'usage le demande.
- La suppression d'un lot est une correction d'erreur de saisie, pas une opération de gestion. Elle
  n'est ouverte que sur un lot intact, dont rien n'est encore sorti du stock.
- La suppression d'un lot est définitive et n'est pas journalisée en V1, le compte étant partagé et
  la journalisation renvoyée en Vague 2.
- Le solde en perte enregistre des sorties de type perte plutôt qu'un état d'annulation nouveau,
  afin de ne pas introduire de statut supplémentaire sur les unités de stock. Il réutilise la
  convention de poids déjà appliquée au menu de sortie unité par unité, aucune seconde règle de
  poids n'étant introduite.
- L'action groupée de solde ne propose que le type « perte » pour cette version. Une unité réellement
  consommée en perso se sort unité par unité depuis Détail Stock. Le point est rouvrable si les
  utilisateurs le demandent à l'usage.
- La désactivation existante conserve son comportement d'exclusion des listes de saisie ; cette
  fonctionnalité y ajoute une condition d'accès et une confirmation, elle ne la redéfinit pas.
- Aucune migration des produits existants n'est nécessaire : leur état « utilisé » se déduit de
  leurs lots actuels.
- Aucune suppression définitive de produit n'est introduite. La désactivation est la seule fin de
  vie d'un produit, conformément au refus de casser la traçabilité.
- La modification reste réservée aux utilisateurs authentifiés, sur le compte partagé de la V1.
  Aucune notion de rôle ou de permission différenciée n'est introduite.
- Les deux utilisateurs travaillent rarement en même temps sur la même fiche. L'édition concurrente
  est donc traitée en dernière écriture gagnante, sans jeton de version : le coût d'une détection de
  conflit dépasse le risque, qui se limite à une saisie perdue.
- Les deux utilisateurs finaux travaillent sur un catalogue de quelques dizaines de produits au
  plus ; aucune contrainte de volumétrie ou de recherche n'est en jeu.
- La fonctionnalité s'inscrit dans la Vague 1 et ne préjuge pas des évolutions de Vague 2 sur les
  matières premières ou les recettes.
