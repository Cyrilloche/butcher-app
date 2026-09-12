# Feature Specification: Backoffice PC — comptes nominatifs et rôles

**Feature Branch**: `feat/backoffice`

**Created**: 2026-09-12

**Status**: Draft

**Input**: User description: "Backoffice PC avec comptes séparés et rôles admin / user. Même application, avec une mise en page desktop et des écrans réservés à l'admin. Le porteur de projet est admin, les deux exploitants (non techniques) sont users. Fin du compte partagé : chacun son compte. L'admin peut tout faire ; lui sont réservés la gestion des comptes, les gestes destructifs, le journal « qui a fait quoi » (RF-27) et des rapports chiffrés (ventes par période, par client, montants à encaisser). Déclencheurs : saisie confortable sur PC (tableaux, filtres de ventes E-06), comptes séparés, contrôle et correction de la saisie terrain, socle multi-comptes pour la V2. ADR-009 (compte partagé sans rôles) devra être remplacé par un nouvel ADR."

## Clarifications

### Session 2026-09-12

- Q: Avec quoi les exploitants se connectent-ils ? → A: Adresse email, comme aujourd'hui (pas d'identifiant court).
- Q: Que couvre la mise en page PC ? → A: Listes en tableaux et navigation latérale ; les formulaires existants sont centrés et bornés en largeur, sans refonte. La maquette PC réalisée avec Claude Design fait référence pour la présentation.
- Q: Jusqu'où va le journal ? → A: Qui, quand, quoi pour toutes les opérations ; contenu complet uniquement pour les suppressions ; pas de détail avant/après des modifications.
- Q: Qui voit l'auteur d'une saisie ? → A: Tous les comptes, sur le détail d'une vente, d'une fournée et d'une sortie ; le journal reste réservé à l'administrateur.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Chacun son compte (Priority: P1)

Aujourd'hui, les trois personnes qui utilisent l'outil partagent un seul identifiant. Personne ne
sait qui a enregistré une vente, et changer le mot de passe déconnecte tout le monde à la fois.

L'administrateur — le porteur de projet — crée un compte pour chacun des deux exploitants. Chacun
se connecte avec son propre identifiant, sur son téléphone comme sur un PC, et retrouve exactement
l'outil qu'il connaît. Tout ce qu'il enregistre porte désormais son nom.

**Why this priority**: c'est le socle de tout le reste. Sans compte nominatif, ni rôle, ni journal,
ni restriction n'a de sens. C'est aussi la condition pour renseigner enfin l'auteur des
enregistrements (RF-27).

**Independent Test**: l'administrateur crée deux comptes depuis l'interface ; chaque exploitant se
connecte avec le sien et enregistre une vente ; la fiche de chaque vente indique son auteur.

**Acceptance Scenarios**:

1. **Given** l'administrateur connecté, **When** il crée un compte en saisissant un nom, une
   adresse email et en choisissant le rôle « Utilisateur », **Then** le compte apparaît dans la
   liste des comptes et la personne peut se connecter avec le mot de passe que l'administrateur
   lui a transmis.
2. **Given** un exploitant connecté avec son compte, **When** il enregistre une fabrication, une
   vente ou une sortie perso/perte, **Then** l'enregistrement mémorise ce compte comme auteur, et
   son nom s'affiche sur le détail de l'enregistrement pour tous les comptes, utilisateurs
   compris.
3. **Given** un exploitant dont le compte a été désactivé, **When** il tente de se connecter ou
   poursuit une session déjà ouverte, **Then** l'accès lui est refusé avec un message en français,
   et ses enregistrements passés conservent son nom.
4. **Given** un exploitant qui a oublié son mot de passe, **When** l'administrateur le
   réinitialise, **Then** l'ancien mot de passe ne fonctionne plus, les sessions ouvertes de ce
   compte sont fermées, et le nouveau mot de passe permet de se connecter.
5. **Given** le compte partagé existant avant cette fonctionnalité, **When** la fonctionnalité est
   mise en service, **Then** ce compte devient le compte administrateur, sans perte d'accès ni de
   données.

---

### User Story 2 — Les gestes sensibles réservés à l'administrateur (Priority: P2)

Retirer un produit du catalogue ou solder tout son stock en perte engage l'activité entière, pas
une saisie. Ces gestes passent par l'administrateur, tout comme la gestion des comptes, le journal
et les rapports. En revanche, un exploitant qui s'est trompé en pesant un sachet ou en
enregistrant une vente continue de corriger lui-même son erreur, comme aujourd'hui : son autonomie
sur le terrain prime, et le journal (US4) permet à l'administrateur de retrouver toute suppression.

**Why this priority**: c'est ce que les rôles apportent concrètement. Une fois les comptes en
place (US1), c'est la protection attendue sur les gestes qui touchent au catalogue et aux comptes,
sans retirer aux exploitants la correction de leurs erreurs.

**Independent Test**: se connecter avec un compte utilisateur et constater que les gestes réservés
ne sont ni proposés à l'écran, ni acceptés par le serveur s'ils sont tentés autrement ; se
connecter en administrateur et constater qu'ils fonctionnent comme aujourd'hui.

**Acceptance Scenarios**:

1. **Given** un compte utilisateur, **When** il ouvre le détail d'un produit, **Then** la
   désactivation, la réactivation et le solde en perte n'apparaissent pas, et aucune entrée de menu
   ne mène aux comptes, au journal ni aux rapports.
2. **Given** un compte utilisateur, **When** une action réservée parvient malgré tout au serveur,
   **Then** elle est refusée et rien n'est modifié.
3. **Given** un compte administrateur, **When** il effectue une action réservée, **Then** elle se
   déroule comme aujourd'hui, avec la même confirmation.
4. **Given** un compte utilisateur, **When** il crée une fabrication, une vente, un client ou un
   produit, **Then** le geste aboutit comme aujourd'hui.
5. **Given** un compte utilisateur, **When** il supprime une vente, retire une ligne de vente,
   supprime une fournée ou une unité mal pesée, y compris saisie par un autre compte ou un autre
   jour, **Then** le geste aboutit comme aujourd'hui, et l'opération figure au journal avec son nom.

---

### User Story 3 — Travailler confortablement sur un PC (Priority: P3)

Relire un mois de ventes ou retrouver toutes les ventes impayées d'un client est pénible sur un
téléphone. Sur un écran large, l'application présente ses listes en tableaux lisibles, avec des
filtres, et une navigation latérale au lieu de la barre en bas d'écran.

**Why this priority**: c'est le déclencheur le plus visible, mais il n'exige ni compte ni rôle
pour apporter de la valeur. Il est donc indépendant et peut être livré avant ou après US2.

**Independent Test**: ouvrir l'application sur un écran d'ordinateur, afficher la liste des
ventes, filtrer par client, statut de paiement et période, et vérifier que le même écran reste
utilisable sur téléphone.

**Acceptance Scenarios**:

1. **Given** un écran large, **When** l'utilisateur ouvre l'application, **Then** la navigation
   est présentée sur le côté et les listes (ventes, stock, clients, produits) occupent la largeur
   disponible sous forme de tableau.
2. **Given** la liste des ventes sur écran large, **When** l'utilisateur filtre par client, par
   statut de paiement et par période, **Then** seules les ventes correspondantes sont affichées,
   avec leur nombre et leur total.
3. **Given** un téléphone, **When** l'utilisateur ouvre les mêmes écrans, **Then** il retrouve la
   présentation mobile actuelle, sans régression.
4. **Given** un écran large, **When** l'utilisateur ouvre un formulaire (nouvelle vente, ajout au
   stock), **Then** il retrouve le formulaire actuel, centré et d'une largeur lisible, et non
   étiré sur toute la largeur de l'écran.
5. **Given** un compte utilisateur sur écran large, **When** il navigue, **Then** aucune entrée
   de menu réservée à l'administrateur n'est visible.

---

### User Story 4 — Savoir qui a fait quoi (Priority: P4)

Une vente a disparu, un prix a changé. L'administrateur consulte un journal chronologique qui dit
qui a créé, modifié ou supprimé quoi, et quand, pour comprendre et corriger sans accuser au hasard.

**Why this priority**: il répond au besoin « contrôle et correction de la saisie terrain », mais
suppose les comptes nominatifs (US1). Il n'a d'intérêt qu'une fois l'outil utilisé à plusieurs.

**Independent Test**: deux comptes différents effectuent chacun une création, une modification et
une suppression ; l'administrateur retrouve les six opérations dans le journal, avec l'auteur,
la date et l'objet concerné.

**Acceptance Scenarios**:

1. **Given** des opérations effectuées par plusieurs comptes, **When** l'administrateur ouvre le
   journal, **Then** il voit pour chacune la date et l'heure, l'auteur, la nature de l'opération
   (création, modification, suppression) et l'objet concerné, dans un vocabulaire en français.
2. **Given** le journal, **When** l'administrateur filtre par auteur, par type d'objet ou par
   période, **Then** seules les opérations correspondantes sont affichées.
3. **Given** une vente supprimée, **When** l'administrateur la retrouve dans le journal, **Then**
   il lit ce qu'elle contenait au moment de sa suppression (client, date, lignes, montant).
4. **Given** une vente dont le montant a été corrigé, **When** l'administrateur la retrouve dans
   le journal, **Then** il lit qui l'a modifiée et quand, et peut ouvrir la vente pour voir sa
   valeur actuelle ; l'ancienne valeur n'est pas conservée.
5. **Given** un compte utilisateur, **When** il cherche à consulter le journal, **Then** l'accès
   lui est refusé.
6. **Given** des enregistrements antérieurs à la mise en service des comptes, **When** ils sont
   affichés, **Then** leur auteur apparaît comme « Compte partagé (avant comptes nominatifs) ».

---

### User Story 5 — Des chiffres pour piloter (Priority: P5)

L'administrateur veut répondre sans calculatrice à « combien a-t-on vendu ce mois-ci », « qui sont
nos meilleurs clients » et « combien nous doit-on encore ».

**Why this priority**: forte valeur, mais purement en lecture sur des données qui existent déjà ;
aucun autre récit n'en dépend. C'est aussi l'antichambre de la rentabilité prévue en V2.

**Independent Test**: sur un jeu de ventes connu, ouvrir les rapports et vérifier que les totaux
par période, par client et le reste à encaisser sont exacts au centime.

**Acceptance Scenarios**:

1. **Given** des ventes sur plusieurs mois, **When** l'administrateur choisit une période,
   **Then** il voit le nombre de ventes, le montant total encaissé et le montant à encaisser sur
   cette période, ainsi que leur répartition par mois.
2. **Given** la même période, **When** il affiche la répartition par client, **Then** chaque
   client apparaît avec son nombre de ventes, son total et son reste à payer, classés du plus
   gros au plus petit.
3. **Given** des ventes « À payer », **When** il ouvre le rapport des montants à encaisser,
   **Then** il voit chaque client débiteur, le montant dû et l'ancienneté de la plus vieille vente
   impayée, et peut ouvrir chaque vente concernée.
4. **Given** la même période, **When** il affiche la répartition par produit, **Then** chaque
   produit apparaît avec le nombre d'unités vendues, le poids vendu pour les produits au poids et
   le montant total.
5. **Given** un compte utilisateur, **When** il cherche à consulter les rapports, **Then** l'accès
   lui est refusé.

---

### Edge Cases

- **Dernier administrateur.** Désactiver le seul compte administrateur actif, ou lui retirer son
  rôle, est refusé : l'outil ne doit jamais se retrouver sans personne capable de gérer les
  comptes.
- **L'administrateur se désactive lui-même.** Refusé, pour la même raison.
- **Compte désactivé avec session ouverte.** La session cesse de fonctionner au plus tard à
  l'expiration de son jeton d'accès court ; aucune nouvelle session ne peut être obtenue.
- **Suppression d'un compte.** Un compte n'est jamais supprimé, seulement désactivé : il est
  l'auteur d'enregistrements dont la traçabilité doit survivre.
- **Adresse email déjà prise.** La création d'un compte avec une adresse déjà utilisée par un
  autre compte, même désactivé, est refusée avec un message clair.
- **Rôle changé en cours de session.** Le nouveau rôle s'applique au plus tard à l'expiration du
  jeton d'accès court ; le serveur, lui, vérifie le rôle à chaque action réservée.
- **Mot de passe non conforme.** La création ou la réinitialisation est refusée avec la règle
  du rôle énoncée en français (longueur minimale, caractères requis).
- **Promotion d'un utilisateur.** Son mot de passe de 20 caractères ne suffit plus : la promotion
  exige un nouveau mot de passe de 32 caractères dans le même geste (FR-035), faute de quoi elle
  est refusée.
- **Suppression abusive ou accidentelle par un exploitant.** Elle aboutit (FR-011). Le journal en
  conserve le contenu (FR-022), ce qui permet à l'administrateur de ressaisir l'enregistrement ;
  aucune restauration automatique n'est prévue dans cette vague.
- **Écran de largeur intermédiaire** (tablette, fenêtre réduite). La présentation bascule d'un mode
  à l'autre sans perte de fonctionnalité ni contenu tronqué.
- **Rapport sur une période vide.** Les totaux affichent zéro, sans message d'erreur.
- **Vente corrigée après coup.** Les rapports reflètent toujours le montant actuellement enregistré,
  jamais une valeur antérieure.
- **Journal volumineux.** Consulté sur plusieurs années, il reste paginé et filtrable.

## Requirements *(mandatory)*

### Functional Requirements

**Comptes et rôles**

- **FR-001**: Le système DOIT gérer des comptes nominatifs, chacun porteur d'un nom affiché, d'une
  adresse email unique servant d'identifiant de connexion, d'un rôle et d'un état
  actif/désactivé. L'adresse ne sert qu'à se connecter : aucun message n'y est envoyé.
- **FR-002**: Le système DOIT connaître exactement deux rôles : « Administrateur » et
  « Utilisateur ». Aucun autre rôle ni droit à la carte n'est prévu dans cette vague.
- **FR-003**: Seul un administrateur DOIT pouvoir créer un compte, modifier son nom ou son rôle,
  le désactiver, le réactiver et réinitialiser son mot de passe, depuis l'interface.
- **FR-004**: Tout utilisateur connecté DOIT pouvoir changer son propre mot de passe, en
  fournissant l'actuel.
- **FR-005**: Tout mot de passe créé ou changé DOIT respecter la politique de mot de passe du rôle
  du compte (FR-034), et le refus DOIT énoncer la règle en français.
- **FR-006**: La réinitialisation ou le changement d'un mot de passe DOIT fermer toutes les
  sessions ouvertes du compte concerné.
- **FR-007**: Un compte désactivé NE DOIT plus pouvoir se connecter ni prolonger une session ;
  ses enregistrements passés DOIVENT rester attribués à son nom.
- **FR-008**: Le système DOIT refuser toute opération qui laisserait l'outil sans administrateur
  actif.
- **FR-009**: Un compte NE DOIT jamais être supprimé, seulement désactivé.
- **FR-010**: À la mise en service, le compte partagé existant DOIT devenir un compte
  administrateur, sans interruption d'accès ni perte de données.

**Gestes réservés**

- **FR-011**: Les gestes suivants DOIVENT être réservés à l'administrateur : désactiver et
  réactiver un produit, et solder le stock d'un produit en perte. Les gestes de correction d'une
  erreur de saisie — supprimer une vente, retirer une ligne de vente, supprimer une fournée,
  supprimer une unité de stock — DOIVENT rester accessibles aux utilisateurs, comme aujourd'hui,
  sur toutes les saisies et sans limite d'ancienneté. *(Clarifié le 2026-09-12 : l'autonomie des
  exploitants prime ; le journal, et non une restriction, sert de filet de contrôle.)*
- **FR-012**: La restriction DOIT être appliquée par le serveur, indépendamment de l'interface : une
  action réservée tentée par un utilisateur est refusée et ne modifie rien.
- **FR-013**: L'interface NE DOIT pas proposer à un utilisateur les actions qui lui sont refusées.
- **FR-014**: Tous les autres gestes existants — créer et corriger produits, fabrications, ventes,
  clients, sorties perso et perte, clôture d'une unité entamée, statut de paiement, et les
  suppressions de correction citées en FR-011 — DOIVENT rester accessibles aux utilisateurs. Sont
  en outre réservés à l'administrateur la gestion des comptes (FR-003), le journal (FR-023) et les
  rapports (FR-027 à FR-029).

**Mise en page PC**

- **FR-015**: Sur écran large, l'application DOIT présenter une navigation latérale et afficher ses
  listes (stock, ventes, clients, produits) sous forme de tableaux exploitant la largeur. La
  présentation DOIT suivre la maquette PC réalisée avec Claude Design.
- **FR-015a**: Sur écran large, les formulaires existants (ajout au stock et pesée, nouvelle vente,
  fiche client, fiche produit) DOIVENT rester ceux d'aujourd'hui, centrés et bornés à une largeur
  lisible ; aucune refonte de leur parcours n'est prévue dans cette vague.
- **FR-016**: Sur téléphone, l'application DOIT conserver sa présentation actuelle ; aucun parcours
  mobile existant NE DOIT régresser.
- **FR-017**: La liste des ventes DOIT pouvoir être filtrée par client, par statut de paiement et
  par période, et afficher le nombre et le total des ventes filtrées (clôt E-06).
- **FR-018**: Les tableaux DOIVENT pouvoir être triés sur leurs colonnes principales (date, client,
  montant, statut).
- **FR-019**: Les écrans réservés à l'administrateur (comptes, journal, rapports) DOIVENT n'être
  visibles et accessibles qu'aux administrateurs.

**Journal**

- **FR-020**: Le système DOIT enregistrer l'auteur de chaque création d'une fabrication, d'une
  vente et d'un mouvement de stock (RF-27).
- **FR-020a**: Le nom de l'auteur DOIT s'afficher, pour tous les comptes quel que soit leur rôle,
  sur le détail d'une vente, d'une fournée et d'une sortie de stock. Cet affichage ne donne accès
  à rien d'autre : le journal reste réservé à l'administrateur (FR-023).
- **FR-021**: Le système DOIT tenir un journal des opérations de création, modification et
  suppression portant sur les produits, fabrications, unités de stock, ventes, lignes de vente,
  sorties de stock, clients et comptes, avec pour chacune : date et heure, auteur, nature de
  l'opération, objet concerné.
- **FR-022**: Pour une suppression, le journal DOIT conserver le contenu complet de l'objet au
  moment où il a été supprimé, suffisant pour le ressaisir (pour une vente : client, date, statut
  de paiement, lignes avec unité, poids et montant). Pour une création ou une modification, il
  n'enregistre que l'opération et l'objet concerné, sans détail des valeurs avant/après.
- **FR-023**: Le journal DOIT être consultable par l'administrateur seul, du plus récent au plus
  ancien, filtrable par auteur, type d'objet et période, et paginé.
- **FR-024**: Le journal NE DOIT être ni modifiable ni supprimable depuis l'interface.
- **FR-025**: Les connexions réussies et échouées, les verrouillages de compte et les changements
  de mot de passe DOIVENT figurer au journal.
- **FR-026**: Les enregistrements antérieurs à la mise en service DOIVENT afficher un auteur
  explicite « Compte partagé (avant comptes nominatifs) » plutôt qu'un vide.

**Rapports**

- **FR-027**: L'administrateur DOIT disposer, sur une période qu'il choisit, du nombre de ventes, du
  montant total, du montant déjà encaissé et du montant restant à encaisser, avec une répartition
  par mois.
- **FR-028**: L'administrateur DOIT disposer d'une répartition des ventes par client et par
  produit sur la même période (nombre, poids vendu pour les produits au poids, montant).
- **FR-029**: L'administrateur DOIT disposer de la liste des clients débiteurs, avec le montant dû
  et l'ancienneté de la plus ancienne vente impayée, chaque vente étant accessible depuis le
  rapport.
- **FR-030**: Tous les montants des rapports DOIVENT être calculés à partir des montants enregistrés
  sur les lignes de vente, jamais recalculés depuis un poids et un prix.
- **FR-031**: Les rapports NE DOIVENT porter aucune notion de coût, de marge ou de rentabilité,
  réservées à la Vague 2.

**Transverse**

- **FR-032**: Aucune valeur technique anglaise (rôle, nature d'opération, type d'objet) NE DOIT
  apparaître à l'écran ; le vocabulaire affiché passe par la table de correspondance française.
- **FR-033**: La décision de passer d'un compte partagé sans rôles à des comptes nominatifs avec
  rôles DOIT faire l'objet d'un ADR remplaçant ADR-009 sur ce point, et RF-26 DOIT être révisée
  dans le PRD, dans le même lot de travail.
- **FR-034**: La politique de mot de passe DOIT dépendre du rôle. Administrateur : 32 caractères
  minimum. Utilisateur : 20 caractères minimum (ex. `Jambon-Saloir-2026-Mamie`). Dans les deux
  cas, majuscule, minuscule, chiffre et caractère spécial restent exigés, et le verrouillage après
  échecs comme la limitation de débit s'appliquent à tous les comptes. *(Clarifié le 2026-09-12 :
  l'administrateur, qui détient le plus de pouvoir, garde la règle forte ; les exploitants saisissent
  une phrase plus courte sur téléphone.)*
- **FR-035**: Promouvoir un utilisateur au rôle d'administrateur DOIT exiger, dans le même geste,
  un nouveau mot de passe conforme à la règle administrateur. Rétrograder un administrateur ne
  change pas son mot de passe.

### Key Entities

- **Compte** : une personne qui accède à l'outil. Porte un nom affiché, une adresse email servant
  d'identifiant de connexion, un rôle, un état actif/désactivé, et la date de sa dernière connexion. N'est jamais
  supprimé.
- **Rôle** : « Administrateur » ou « Utilisateur ». Détermine les gestes et les écrans accessibles.
- **Entrée de journal** : la trace d'une opération. Porte sa date, son auteur, sa nature
  (création, modification, suppression, connexion…), le type et l'identité de l'objet concerné, et
  pour une suppression seulement, le contenu de l'objet supprimé. Immuable.
- **Auteur d'un enregistrement** : le compte qui a créé une fabrication, une vente ou un mouvement
  de stock (RF-27), déjà prévu par le modèle et désormais renseigné.
- **Rapport** : une lecture agrégée des ventes sur une période ; ce n'est pas une donnée stockée.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100 % des fabrications, ventes et sorties enregistrées après la mise en service
  portent un auteur nominatif.
- **SC-002**: Aucun geste réservé n'aboutit depuis un compte utilisateur, que la tentative passe par
  l'interface ou non.
- **SC-003**: L'administrateur crée un compte opérationnel en moins de 2 minutes, sans ligne de
  commande.
- **SC-004**: Sur PC, retrouver toutes les ventes impayées d'un client sur une période prend moins
  de 30 secondes, contre un défilement manuel de la liste aujourd'hui.
- **SC-005**: Pour toute opération de création, modification ou suppression, l'administrateur
  identifie son auteur et sa date depuis le journal en moins d'une minute.
- **SC-006**: Les totaux des rapports sont exacts au centime par rapport à la somme des montants
  enregistrés sur les ventes concernées.
- **SC-007**: Les deux exploitants continuent d'effectuer leurs saisies terrain sur téléphone sans
  aide supplémentaire après la bascule vers leurs comptes nominatifs.
- **SC-008**: Aucun parcours mobile existant ne demande davantage d'étapes qu'avant la
  fonctionnalité.

## Assumptions

- **Trois comptes au démarrage** : un administrateur et deux utilisateurs. Le système n'est pas
  dimensionné pour des dizaines de comptes, ni pour une inscription libre, qui reste exclue.
- **Le compte partagé existant devient celui de l'administrateur** (FR-010). Les exploitants
  reçoivent chacun un nouveau compte ; le mot de passe partagé cesse de leur être communiqué.
- **Les enregistrements passés ne sont pas réattribués** : leur auteur reste inconnu, affiché
  comme tel (FR-026). Aucune reconstitution a posteriori n'est tentée.
- **Le mot de passe initial est choisi par l'administrateur** et transmis de vive voix. Aucun envoi
  d'email ni lien de réinitialisation automatique n'est prévu : l'outil n'envoie pas de courrier.
- **L'identifiant de connexion reste une adresse email**, comme aujourd'hui, même si elle ne sert
  pas à envoyer de message *(confirmé le 2026-09-12)*.
- **Un utilisateur a accès à la mise en page PC** : elle n'est pas réservée à l'administrateur.
  Seuls les écrans de comptes, journal et rapports le sont.
- **Le journal est conservé sans limite de durée** : le volume d'une activité artisanale annexe
  le permet, et sa valeur est justement de remonter loin.
- **Créer et corriger un produit reste ouvert aux exploitants** (OBJ-1 du PRD : autonomie dans la
  création de produits). Seuls la désactivation, la réactivation et le solde en perte, qui
  engagent le catalogue entier, sont réservés (FR-011).
- **Les rapports sont consultés à l'écran** ; aucun export (tableur, PDF) n'est prévu dans cette
  vague.
- **La séparation frontend / backend est préservée** : les rôles et restrictions sont portés par
  le contrat d'API, et le serveur reste le seul garant des droits (principe II de la
  constitution).
- **Dépendance — maquette PC.** La maquette PC réalisée avec Claude Design (FR-015) n'est pas
  encore versionnée : `design/` ne contient que le guide de style. Elle DOIT être exportée dans
  `design/` avant la planification du récit 3, pour que le plan s'appuie sur elle et non sur une
  interprétation.
- **Découpage de livraison** : US1 et US2 forment le premier lot livrable ; US3 peut être livrée
  indépendamment ; US4 et US5 suivent. Chaque récit reste testable et livrable seul.
