# Feature Specification: Corriger et supprimer une vente

**Feature Branch**: `feat/sale-correction`

**Created**: 2026-09-11

**Status**: Draft

**Input**: User description: "Corriger et supprimer une vente depuis l'interface (RG-14, écart E-04). L'écran de détail d'une vente ne permet aujourd'hui que de changer le statut de paiement, alors que l'API sait déjà tout faire. Périmètre : corriger l'en-tête (client, date, notes, paiement) ; corriger les lignes (ajuster montant ou poids vendu, retirer une ligne, le serveur refusant le retrait de la dernière ligne) ; supprimer la vente entière, ce qui supprime ses lignes et rend disponible toute unité ne portant plus aucun mouvement."

## Contexte

Une vente se saisit en un seul geste, debout, souvent pendant que le client attend. C'est
exactement le moment où l'on se trompe : le mauvais client dans la liste, un montant tapé de
travers, un sachet ajouté au panier puis finalement pas emporté.

L'application n'offre aujourd'hui aucun recours. L'écran de détail d'une vente affiche le client,
la date, les lignes et le total, mais le seul geste possible est de basculer le statut de paiement.
Une erreur est donc définitive pour l'utilisateur, alors que le serveur, lui, sait déjà corriger et
supprimer. C'est l'écart E-04 de l'état des lieux du 4 septembre 2026, et le dernier manque
important de la Vague 1 côté interface.

Cette absence est d'autant plus gênante que les utilisateurs sont deux particuliers non
techniques, sans accès à la base, sans autre carnet où rectifier. Le filet de rattrapage est ici
une exigence d'adoption, pas un confort.

La règle métier existe déjà et ne change pas : RG-14 rend une vente modifiable sur son client, sa
date, son paiement et ses notes, et supprimable. RG-11 rend chaque ligne modifiable et supprimable,
en remettant `disponible` toute unité physique qui ne porte plus aucun mouvement. Le travail est
donc entièrement d'interface : rendre atteignable ce que le serveur autorise.

## Clarifications

### Session 2026-09-11

- Q : Que doit couvrir la correction d'une vente depuis l'interface ? → R : Les trois niveaux.
  L'en-tête (client, date, notes, paiement), les lignes (ajuster, retirer) et la suppression de la
  vente entière. C'est le filet de rattrapage complet décrit par RG-14, et ce que le serveur expose
  déjà.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Corriger l'en-tête d'une vente (Priority: P1)

Une vente a été enregistrée au nom du mauvais client, ou à la mauvaise date parce qu'elle est
saisie le lendemain. La personne ouvre la vente, corrige le champ fautif, et la vente est à jour.

**Why this priority**: Se tromper de client est l'erreur la plus probable et la plus grave. Elle
fausse l'historique d'achats et la traçabilité lot ↔ client, qui est la valeur métier centrale du
projet.

**Independent Test**: Enregistrer une vente au nom d'un client, la rouvrir, la réaffecter à un
autre client, vérifier que les deux historiques d'achats reflètent le changement. Livre déjà toute
la valeur, même si les lignes restent intouchables.

**Acceptance Scenarios**:

1. **Given** une vente affectée au client A, **When** on l'ouvre et qu'on la réaffecte au client B,
   **Then** la vente apparaît dans l'historique d'achats de B et disparaît de celui de A.
2. **Given** une vente datée du jour, **When** on corrige sa date pour la veille, **Then** la vente
   s'affiche à sa nouvelle date dans la liste des ventes.
3. **Given** une vente sans note, **When** on lui ajoute une note, **Then** la note est visible à la
   réouverture de la vente.
4. **Given** une vente marquée « à payer », **When** on la marque « payée » depuis le même écran de
   correction, **Then** le statut change sans que le reste de la vente soit modifié.
5. **Given** une correction en cours, **When** on l'abandonne sans valider, **Then** la vente est
   inchangée.

---

### User Story 2 - Retirer ou ajuster une ligne (Priority: P2)

Le client repose un sachet au dernier moment, ou le montant encaissé sur une tranche de jambon
n'est pas celui qui a été tapé. La personne ouvre la vente, retire la ligne de trop ou corrige son
montant, et le total suit.

**Why this priority**: Corrige l'erreur au bon endroit, sans détruire le reste d'une vente juste.
Vient après l'en-tête parce qu'une vente entièrement fausse peut, en dernier recours, être
supprimée et ressaisie.

**Independent Test**: Créer une vente de trois unités, en retirer une, vérifier que le total baisse
et que l'unité retirée est de nouveau vendable.

**Acceptance Scenarios**:

1. **Given** une vente de trois lignes, **When** on retire une ligne et qu'on confirme, **Then** la
   vente n'en porte plus que deux et son total est diminué du montant retiré.
2. **Given** une ligne retirée d'une vente, **When** on ouvre le stock du produit concerné,
   **Then** l'unité physique y figure de nouveau comme disponible.
3. **Given** une vente d'une seule ligne, **When** on tente de retirer cette ligne, **Then** le
   geste est refusé et l'écran explique qu'il faut supprimer la vente elle-même.
4. **Given** une ligne dont le montant encaissé est erroné, **When** on corrige ce montant,
   **Then** le total de la vente est recalculé à partir des montants réellement saisis.
5. **Given** une ligne de vente à la tranche, **When** on corrige le poids vendu au-delà du poids
   pesé de l'unité, **Then** le geste est refusé avec un message compréhensible.

---

### User Story 3 - Supprimer une vente entière (Priority: P2)

Une vente a été saisie deux fois, ou pour de bon par erreur. La personne la supprime, et le stock
revient à ce qu'il était.

**Why this priority**: C'est le recours ultime, celui qui garantit qu'aucune saisie n'est
définitive. Il double partiellement l'histoire 2, d'où sa priorité identique.

**Independent Test**: Créer une vente, la supprimer, vérifier qu'elle a disparu de la liste et que
ses unités sont de nouveau en stock.

**Acceptance Scenarios**:

1. **Given** une vente enregistrée, **When** on la supprime et qu'on confirme, **Then** elle
   disparaît de la liste des ventes et de l'historique d'achats de son client.
2. **Given** cette vente supprimée, **When** on ouvre le stock des produits concernés, **Then**
   chaque unité qui ne porte plus aucun mouvement est de nouveau disponible.
3. **Given** une demande de suppression, **When** l'écran de confirmation s'affiche, **Then** il
   annonce en français le nombre de lignes supprimées et le retour des unités en stock.
4. **Given** une demande de suppression, **When** on l'annule, **Then** la vente est intacte.

---

### Edge Cases

- Une unité vendue puis sortie en perso ou en perte hors de cette vente ne redevient pas
  disponible quand la ligne de vente est retirée : elle porte encore un mouvement. Le
  comportement est celui du serveur, l'interface ne le contredit pas.
- Une unité entamée puis clôturée manuellement ne « rouvre » pas quand une de ses ventes
  partielles est retirée. Le statut n'est recalculé que lorsqu'il ne reste aucun mouvement.
- Réaffecter une vente à un client supprimé entre-temps, ou à un client qui n'existe plus, est
  refusé par le serveur. L'écran affiche le refus sans perdre la saisie en cours.
- Deux corrections simultanées depuis deux appareils : la dernière écriture gagne, conformément au
  reste de l'application. Aucun verrouillage n'est introduit.
- Une vente dont toutes les lignes ont été retirées ne peut pas exister : le retrait de la dernière
  est refusé.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: L'écran de détail d'une vente DOIT permettre de corriger le client, la date, les
  notes et le statut de paiement de cette vente.
- **FR-002**: La correction de l'en-tête DOIT se valider en un seul geste, et DOIT pouvoir être
  abandonnée sans effet.
- **FR-003**: Le choix du client dans la correction DOIT offrir le même confort de recherche que la
  saisie d'une vente, le client restant obligatoire (RG-07).
- **FR-004**: L'écran de détail DOIT permettre de retirer une ligne de la vente.
- **FR-005**: L'écran de détail DOIT permettre de corriger le montant encaissé d'une ligne et, pour
  une vente au poids, le poids vendu.
- **FR-006**: Le retrait de la dernière ligne d'une vente DOIT être refusé, avec un message
  indiquant qu'il faut supprimer la vente elle-même.
- **FR-007**: L'écran de détail DOIT permettre de supprimer la vente entière.
- **FR-008**: Tout geste destructif — retirer une ligne, supprimer la vente — DOIT demander une
  confirmation explicite annonçant ses conséquences en français.
- **FR-009**: Après toute correction, le total et le nombre d'articles affichés DOIVENT refléter
  les montants réellement enregistrés, jamais un calcul théorique (RG-05).
- **FR-010**: Tout refus du serveur DOIT être présenté en français, en conservant la saisie en
  cours, sans message technique ni valeur anglaise à l'écran.
- **FR-011**: Aucune correction ne DOIT être opérée localement sans être confirmée par le serveur :
  l'écran affiche l'état que le serveur renvoie.
- **FR-012**: Le numéro de la vente DOIT rester inchangé par toute correction.

### Key Entities

- **Vente**: regroupe ses lignes, porte le numéro, la date, le client obligatoire, le statut de
  paiement et les notes. Corrigeable et supprimable (RG-14).
- **Ligne de vente**: un mouvement de stock de type vente, rattaché à une unité physique et à une
  vente. Corrigeable sur son montant et son poids vendu, supprimable sauf si elle est la dernière
  de sa vente (RG-11).
- **Unité de stock**: l'objet physique. Redevient disponible dès qu'elle ne porte plus aucun
  mouvement.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Une vente affectée au mauvais client se corrige en moins de trente secondes, sans
  passer par une suppression suivie d'une ressaisie.
- **SC-002**: Une erreur de saisie sur une vente se rattrape entièrement depuis l'application, sans
  aucune intervention sur la base de données.
- **SC-003**: Aucun geste destructif ne s'exécute sans une confirmation qui en énonce les
  conséquences.
- **SC-004**: Après le retrait d'une ligne ou la suppression d'une vente, le stock affiché
  correspond exactement au stock physique, vérifiable en comptant les unités à l'écran.
- **SC-005**: L'écart E-04 de l'état des lieux est clos, ne laissant en Vague 1 que la saisie de la
  date limite de consommation et de la matière première (E-02).

## Assumptions

- **Aucune évolution serveur n'est attendue.** La modification et la suppression d'une vente, comme
  celles d'une ligne, sont déjà exposées et couvertes par des tests. Si une règle manquait, elle
  serait ajoutée côté serveur et testée là (principe II de la constitution).
- **Ajouter une ligne à une vente déjà enregistrée est hors périmètre.** Le serveur le permet, mais
  le geste demande de traverser la sélection d'unités vendables, donc l'essentiel de l'écran de
  saisie d'une vente. Un oubli se rattrape en enregistrant une seconde vente au même client. À
  rouvrir si l'usage réel le réclame.
- **Aucun historique des corrections n'est conservé.** L'activité est informelle et sans contrainte
  comptable (H-02). Le champ `created_by` reste non renseigné en Vague 1, compte partagé oblige.
- **Le numéro de vente peut être réattribué** après une suppression, le serveur le dérivant d'un
  comptage journalier. Ce n'est pas un identifiant d'archive au sens comptable, et cette
  fonctionnalité ne le change pas.
- **Pas de corbeille ni d'annulation.** Une suppression confirmée est définitive, la confirmation
  étant le seul garde-fou.
