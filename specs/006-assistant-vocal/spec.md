# Feature Specification: Assistant vocal

**Feature Branch**: `feat/assistant-vocal`

**Created**: 2026-09-23

**Status**: Draft

**Input**: User description: "Assistant vocal (specs/006-assistant-vocal). Un utilisateur qui parle beaucoup à son téléphone doit pouvoir, depuis l'application Saloir, dicter une demande au lieu de naviguer : poser une question sur le stock (combien il reste d'un produit, ou de tout le stock) et préparer une vente (produits, quantités, poids ou prix visé, tranche de jambon, client, payé ou non). Déclenchement depuis le bouton « + » des listes (choix « Dicter » à côté de l'action de l'écran) ; l'écoute s'arrête d'elle-même au silence ; la réponse s'affiche et est lue à voix haute avec une voix naturelle ; une vente dictée ouvre le formulaire « Nouvelle vente » pré-rempli, que l'utilisateur relit et enregistre lui-même (l'assistant n'écrit jamais rien). Règles de choix des unités : les plus anciennes par défaut, la plus proche du poids ou du prix dit, le jambon entamé le plus ancien pour une tranche, « À payer » par défaut. Aucun mauvais client ne doit jamais être choisi : un nom incertain ou inconnu laisse le client à choisir. Les chiffres annoncés viennent toujours du serveur. Données : voix et compréhension chez Mistral (UE), noms de clients jamais transmis au modèle de langage (pseudonymisation locale), audio conservé 30 jours chez Mistral. Réutilise les décisions et mesures du spike : docs/cadrage-assistant-vocal.md, docs/spike-assistant-vocal.md (bilan §8), docs/assistant-vocal-fonctionnement.md, ADR-012. Manques à couvrir pour un usage réel : journalisation des demandes vocales sans l'audio, limite de débit/coût par compte, restriction de la lecture vocale aux phrases de l'assistant, configuration de production, activation maîtrisée, mesure de l'adoption et du délai après livraison. Écrire la spec comme pour une fonctionnalité neuve (le quoi, pas le code du spike) ; c'est le plan qui fera le rapprochement avec l'existant sur la branche feat/assistant-vocal."

## Clarifications

### Session 2026-09-23

- Q: Le journal des demandes vocales garde-t-il la phrase entendue, qui peut contenir un nom de client ? → A: Oui, la phrase entendue est conservée (jamais l'audio) : elle vit dans la base de l'application, comme les ventes qui portent déjà ces noms, et elle permet de comprendre et corriger les ratés.
- Q: L'assistant s'active-t-il pour tous les comptes d'un coup, ou compte par compte ? → A: Compte par compte, par l'administrateur. Un compte sans assistant garde le « + » habituel, à un appui.
- Q: Le « + » à deux choix fait passer l'action habituelle à deux appuis : accepté ? → A: Oui, pour les comptes où l'assistant est activé ; les autres gardent le « + » à un appui.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Demander ce qu'il reste en stock (Priority: P1)

Un des exploitants parle volontiers à son téléphone et peine à naviguer entre les écrans. Une
cliente appelle : « il te reste du saucisson ? ». Plutôt que d'ouvrir l'écran Stock et de chercher
le produit, il appuie sur « + », choisit « Dicter », et demande : « il me reste combien de
saucissons ? ». L'application lui répond à voix haute, en une phrase : combien d'unités, environ
quel poids, et depuis quand date la plus ancienne. Le détail par fournée s'affiche en dessous.

**Why this priority**: c'est l'usage le plus simple et le moins risqué — l'assistant ne prépare
rien, il lit. Il suffit à valider que l'utilisateur visé adopte la voix comme manière d'utiliser
l'outil.

**Independent Test**: sur un stock connu, dicter « il me reste combien de saucissons ? » ; la
réponse dite et affichée donne exactement les mêmes nombres que l'écran Stock.

**Acceptance Scenarios**:

1. **Given** 12 saucissons en stock issus de deux fournées, **When** l'utilisateur demande combien
   il en reste, **Then** l'application dit et affiche le nombre, le poids restant arrondi et la date
   de la fournée la plus ancienne, et montre le détail de chaque fournée.
2. **Given** un jambon entier et un jambon entamé en stock, **When** l'utilisateur demande combien
   il reste de jambon, **Then** la réponse distingue le jambon entier de l'entamé et donne le poids
   encore vendable de l'entamé.
3. **Given** un stock de plusieurs produits, **When** l'utilisateur demande « qu'est-ce qu'il me
   reste en stock ? », **Then** la réponse cite chaque produit avec son nombre d'unités.
4. **Given** un produit qui n'existe pas au catalogue, **When** l'utilisateur en demande le stock,
   **Then** l'application répond qu'elle ne connaît pas ce produit, sans donner le stock d'un autre.
5. **Given** une question hors sujet (« mets des chansons pour enfants »), **When** l'utilisateur la
   dicte, **Then** l'application dit qu'elle n'a pas compris et rappelle ce qu'on peut lui demander.

---

### User Story 2 — Préparer une vente à la voix (Priority: P1)

L'exploitant vend deux saucissons à une voisine. Il appuie sur « + », « Dicter », et dit : « vends
deux saucissons à madame Martin ». L'application lui répond « Voilà la vente, vérifie-la avant
d'enregistrer » et lui propose d'ouvrir le formulaire « Nouvelle vente », déjà rempli : la cliente,
les deux sachets les plus anciens, les montants, « À payer ». Il relit, corrige si besoin, et
enregistre avec le bouton habituel.

**Why this priority**: c'est le geste le plus fréquent et le plus long à la main. Il est au même
niveau que la question de stock, mais porte plus de risque (une vente au mauvais client), d'où ses
garde-fous.

**Independent Test**: dicter une vente pour un client connu ; le formulaire s'ouvre pré-rempli selon
les règles de choix des unités ; rien n'est enregistré tant que l'utilisateur n'a pas validé.

**Acceptance Scenarios**:

1. **Given** plusieurs saucissons en stock et une cliente « Martin », **When** l'utilisateur dicte
   « vends deux saucissons à madame Martin », **Then** le formulaire « Nouvelle vente » s'ouvre avec
   la cliente, les deux saucissons les plus anciens, leurs montants calculés comme pour un choix à la
   main, et « À payer ».
2. **Given** des saucissons de poids différents, **When** l'utilisateur dicte « un saucisson
   d'environ 300 grammes pour Gérard », **Then** le sachet proposé est celui dont le poids est le plus
   proche de 300 g.
3. **Given** des saucissons à des prix différents, **When** l'utilisateur dicte « un saucisson à
   8 euros pour la Josette », **Then** le sachet proposé est celui dont le prix est le plus proche de
   8 €.
4. **Given** deux jambons entamés, **When** l'utilisateur dicte « 200 grammes de jambon pour madame
   Martin », **Then** la vente propose une tranche de 200 g sur le jambon entamé le plus ancien.
5. **Given** une phrase qui dit que c'est payé (« elle a payé »), **When** le brouillon s'ouvre,
   **Then** la vente est marquée « Payée » ; sinon elle est « À payer ».
6. **Given** un brouillon ouvert, **When** l'utilisateur quitte le formulaire sans enregistrer,
   **Then** aucune vente n'existe et le stock est inchangé.

---

### User Story 3 — Ne jamais se tromper de client (Priority: P1)

Deux clientes s'appellent Martin et Martine. La transcription d'une phrase est imparfaite. Plutôt que
de parier, l'application laisse le client à choisir et le dit : « Client à choisir : « Martin » n'a
pas été reconnu ». Un nom inconnu de la base est traité de même.

**Why this priority**: une vente attribuée au mauvais client fausse les impayés et la traçabilité
lot ↔ client sans que personne ne s'en aperçoive. C'est la condition pour que l'assistant soit
acceptable, pas une amélioration.

**Independent Test**: dicter des ventes pour des clients aux noms proches ou absents de la base ;
aucune n'est pré-remplie avec un autre client que celui cité.

**Acceptance Scenarios**:

1. **Given** deux clients qui se prononcent presque pareil (Martin, Martine), **When** le nom entendu
   ne permet pas de les départager, **Then** aucun client n'est pré-rempli et un avertissement
   invite à le choisir.
2. **Given** un client absent de la base, **When** l'utilisateur dicte une vente pour lui, **Then**
   aucun client n'est pré-rempli, le reste de la vente l'est, et le nom entendu est affiché pour
   aider à choisir ou créer le client.
3. **Given** un client prononcé autrement qu'il ne s'écrit (« les Moreau » transcrit « les
   moraux »), **When** le son correspond à un seul client, **Then** ce client est pré-rempli.

---

### User Story 4 — Parler naturellement, sans manipuler l'écran (Priority: P2)

L'utilisateur appuie une fois, parle, se tait : la demande part toute seule. Une jauge montre que le
micro l'entend. La réponse est lue avec une voix naturelle. S'il préfère, il peut écrire sa demande
au clavier.

**Why this priority**: c'est ce qui rapproche l'usage de celui d'un assistant de téléphone, déjà
familier à l'utilisateur visé. Le premier essai a montré que la voix de la réponse « change
absolument tout ».

**Independent Test**: dicter une demande sans toucher l'écran après « Dicter » ; elle part après le
silence et la réponse est lue.

**Acceptance Scenarios**:

1. **Given** l'écoute en cours, **When** l'utilisateur parle puis se tait deux secondes, **Then** la
   demande est envoyée sans autre geste.
2. **Given** l'écoute en cours, **When** l'utilisateur hésite brièvement au milieu de sa phrase,
   **Then** l'écoute continue.
3. **Given** l'écoute en cours, **When** rien n'est dit pendant 7 secondes, **Then** l'écoute
   s'arrête et l'application dit qu'elle n'a rien entendu, sans rien envoyer.
4. **Given** une réponse reçue, **When** elle s'affiche, **Then** elle est lue avec une voix
   naturelle en français ; si cette voix n'est pas disponible, la voix du téléphone prend le relais.
5. **Given** un micro refusé ou indisponible, **When** l'utilisateur choisit « Dicter », **Then**
   l'application l'explique et propose d'écrire la demande.

---

### User Story 5 — Suivre l'usage et garder la maîtrise (Priority: P2)

L'administrateur veut savoir si l'assistant sert vraiment, combien il coûte, et pouvoir le couper.
Il voit, pour chaque compte, combien de demandes vocales ont été faites, avec quelle issue, en
combien de temps, et ce qui a été entendu. Il active l'assistant pour les comptes qui en veulent.

**Why this priority**: l'assistant a été lancé sur l'hypothèse que la voix suffira à son adoption,
sans essai préalable par l'utilisateur visé. Cette hypothèse doit pouvoir être vérifiée, et
l'assistant coupé si elle ne se vérifie pas ou si le service extérieur pose problème.

**Independent Test**: activer l'assistant pour un compte seulement ; faire quelques demandes ;
l'administrateur retrouve leur nombre, leur issue, leur délai et la phrase entendue ; l'autre compte
n'a jamais vu « Dicter ».

**Acceptance Scenarios**:

1. **Given** des demandes vocales faites par plusieurs comptes, **When** l'administrateur consulte
   l'usage de l'assistant, **Then** il voit par compte et par semaine le nombre de demandes, leur
   issue (stock, vente, pas compris, erreur) et le délai médian.
2. **Given** un compte pour lequel l'assistant n'est pas activé, **When** il appuie sur le « + »
   d'une liste, **Then** l'action habituelle se déclenche directement, en un appui, comme avant.
3. **Given** un compte pour lequel l'assistant est activé, **When** il appuie sur le « + », **Then**
   « Dicter » et l'action habituelle sont proposés ; **When** l'administrateur désactive l'assistant
   pour ce compte, **Then** le « + » revient à un appui.
4. **Given** une demande qui a échoué, **When** l'administrateur la consulte, **Then** il voit la
   phrase entendue, pour comprendre le raté.
5. **Given** un compte qui enchaîne les demandes au-delà de la limite, **When** il en fait une de
   plus, **Then** l'application lui dit en français de patienter, sans appeler le service extérieur.

---

### Edge Cases

- **Service extérieur indisponible** : l'application dit que l'assistant ne répond pas pour le
  moment ; tout le reste de l'application fonctionne normalement.
- **Transcription approximative** : « de saucisson » pour « deux saucissons », « terrain » pour
  « terrine ». La phrase entendue est toujours affichée ; le formulaire pré-rempli se corrige.
- **Mot mal transcrit ressemblant à un produit** (« corisaux ») : jamais remplacé par un produit du
  catalogue ; l'application dit ne pas avoir compris ou laisse le produit à choisir.
- **Plus demandé qu'en stock** (« dix saucissons » quand il en reste quatre) : la vente propose ce qui
  existe et le signale.
- **Tranche sans poids** (« quatre tranches de jambon ») : le jambon est choisi, le poids reste à
  saisir, et c'est signalé.
- **Tranche plus lourde que ce qui reste** : le poids reste à saisir, et c'est signalé.
- **Correction en cours de phrase** (« trois, non deux ») : la dernière valeur l'emporte.
- **Deux clients dans une phrase** (« deux saucissons à madame Martin et une terrine à Gérard ») : une
  seule vente est préparée, pour le premier client, et c'est signalé.
- **Question et vente dans la même phrase** : les deux sont traitées, la réponse de stock puis le
  brouillon.
- **Demande hors périmètre** (annuler ou modifier une vente, autre sujet) : pas compris, avec le
  rappel de ce qu'on peut demander ; rien n'est modifié.
- **Unité vendue entre la réponse et l'ouverture du formulaire** : le formulaire écarte l'unité
  disparue et le signale.
- **Panneau fermé pendant l'écoute ou la réponse** : l'écoute est abandonnée, rien n'est envoyé, la
  voix s'arrête.

## Requirements *(mandatory)*

### Functional Requirements

**Déclencher et écouter**

- **FR-001**: Pour un compte où l'assistant est activé, le bouton « + » des écrans de liste (Stock,
  Ventes, Clients, Produits) DOIT proposer « Dicter » à côté de l'action habituelle de l'écran, sur
  téléphone comme sur PC ; l'action habituelle passe alors à deux appuis, ce qui est accepté. Pour un
  compte sans assistant, le « + » DOIT rester à un appui, inchangé.
- **FR-002**: Pendant l'écoute, l'application DOIT montrer qu'elle entend (niveau du micro) et
  proposer de terminer ou d'annuler.
- **FR-003**: L'écoute DOIT s'arrêter d'elle-même deux secondes après la fin de la parole, au plus
  tard au bout de 30 secondes, et s'abandonner sans rien envoyer si rien n'est dit pendant
  7 secondes.
- **FR-004**: Le micro DOIT être libéré après chaque demande.
- **FR-005**: L'utilisateur DOIT pouvoir écrire sa demande au clavier, avec le même résultat qu'à la
  voix.

**Comprendre et répondre**

- **FR-006**: Chaque demande DOIT aboutir à l'une de trois issues : une réponse de stock, un
  brouillon de vente, ou « pas compris » avec le rappel de ce qu'on peut demander. Une phrase qui
  combine question de stock et vente DOIT donner les deux.
- **FR-007**: L'application DOIT afficher la phrase entendue avec chaque réponse.
- **FR-008**: La réponse DOIT être affichée dès qu'elle est prête, puis lue avec une voix naturelle
  en français ; la voix du téléphone DOIT prendre le relais si cette voix n'est pas disponible.
- **FR-009**: L'assistant NE DOIT rien enregistrer ni modifier : il lit le stock et prépare des
  brouillons.

**Stock**

- **FR-010**: Une réponse de stock DOIT donner, pour un produit ou pour tout le stock, le nombre
  d'unités intactes et entamées et le poids encore vendable, calculés selon les mêmes règles que les
  écrans de stock ; la phrase dite DOIT rester courte (l'essentiel) et le détail par fournée
  DOIT être affiché.
- **FR-011**: Tous les chiffres dits ou affichés DOIVENT être calculés par le serveur. Aucun chiffre
  ne DOIT provenir du service de compréhension.
- **FR-012**: Un produit absent du catalogue NE DOIT jamais être remplacé par un autre.

**Vente**

- **FR-013**: Un brouillon de vente DOIT reprendre de la phrase les produits, les quantités, et le
  cas échéant le poids ou le prix visé, la vente à la tranche, le client et le paiement.
- **FR-014**: Les unités d'un brouillon DOIVENT être choisies ainsi : les plus anciennes par défaut ;
  la plus proche du poids dit ; la plus proche du prix dit ; pour une tranche, l'unité entamée la plus
  ancienne, à défaut l'unité intacte la plus ancienne. Une unité entamée NE DOIT jamais être proposée
  en vente entière, et une même unité jamais deux fois.
- **FR-015**: Un brouillon DOIT être « À payer » sauf si la phrase dit que c'est payé.
- **FR-016**: Un brouillon DOIT s'ouvrir dans le formulaire « Nouvelle vente » existant, pré-rempli ;
  les montants DOIVENT y être calculés comme pour un choix à la main, et la vente n'est enregistrée
  que par le bouton habituel.
- **FR-017**: Le formulaire DOIT afficher les avertissements du brouillon : client à choisir (avec le
  nom entendu), stock insuffisant, poids de tranche à saisir, produit inconnu, vente limitée au premier
  client cité, unité disparue.

**Clients et données personnelles**

- **FR-018**: L'assistant NE DOIT jamais pré-remplir un autre client que celui cité. Quand le nom est
  inconnu, ou quand plusieurs clients peuvent correspondre sans que la phrase permette de trancher,
  aucun client n'est pré-rempli.
- **FR-019**: Les noms de clients et la liste des clients NE DOIVENT jamais être transmis au service
  de compréhension ; un nom cité y est remplacé par une référence anonyme, ou retiré s'il est inconnu.
- **FR-020**: La lecture à voix haute NE DOIT s'appliquer qu'aux phrases produites par l'assistant ;
  aucune autre phrase ne DOIT pouvoir être envoyée au service de synthèse.
- **FR-021**: Les phrases lues NE DOIVENT contenir aucun nom de client.

**Maîtrise, suivi et coût**

- **FR-022**: L'assistant DOIT être réservé aux comptes connectés, actifs, et pour lesquels il est
  activé ; le serveur DOIT refuser une demande d'un compte sans assistant, même si l'interface ne la
  propose pas.
- **FR-023**: Le nombre de demandes vocales DOIT être limité par compte (voir Assumptions) ; au-delà,
  l'application DOIT le dire en français sans appeler le service extérieur.
- **FR-024**: Chaque demande DOIT être journalisée — compte, date, phrase entendue, issue, délai —
  sans jamais conserver l'audio.
- **FR-025**: L'administrateur DOIT pouvoir consulter l'usage de l'assistant : par compte et par
  semaine, nombre de demandes, répartition des issues, délai médian, et le détail de chaque demande
  avec sa phrase entendue.
- **FR-026**: L'administrateur DOIT pouvoir activer ou désactiver l'assistant compte par compte. Un
  nouveau compte n'a pas l'assistant ; la désactivation prend effet à la requête suivante.
- **FR-027**: Si le service extérieur est indisponible ou non configuré, l'application DOIT le dire,
  et tout le reste de l'application DOIT fonctionner normalement.

### Key Entities *(include if feature involves data)*

- **Demande vocale** : une demande faite à l'assistant. Porte le compte, la date, la phrase entendue,
  l'issue (stock, vente, pas compris, erreur, limite atteinte), le délai de réponse. Jamais l'audio.
  Sert au suivi de l'usage, du délai et des ratés.
- **Brouillon de vente** : proposition de vente issue d'une demande, jamais enregistrée en l'état.
  Porte le client (ou aucun), le paiement, les unités proposées (entières ou tranche avec son poids)
  et les avertissements. N'a pas de montant : le formulaire le calcule.
- **Réponse de stock** : pour chaque produit concerné, unités intactes et entamées, poids encore
  vendable, fournées avec leur date, leur prix et leur nombre d'unités, unités entamées avec leur
  poids restant.
- **Accès à l'assistant** : pour chaque compte, assistant activé ou non ; décidé par
  l'administrateur (FR-026).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Aucun brouillon ne propose un autre client que celui cité — 0 sur toute la recette et
  sur les trois premiers mois d'usage.
- **SC-002**: Aucun chiffre dit ou affiché par l'assistant ne diffère de l'écran Stock au même
  instant — 0 sur toute la recette.
- **SC-003**: La réponse s'affiche en moins de 4 secondes après la fin de la parole pour la moitié
  des demandes, et en moins de 8 secondes pour 95 % d'entre elles.
- **SC-004**: Sur le jeu de phrases de recette bien transcrites, au moins 95 % des demandes aboutissent
  à la bonne issue, et au moins 90 % des brouillons de vente ont tous leurs champs justes.
- **SC-005**: Préparer à la voix une vente de deux produits pour un client connu, jusqu'au formulaire
  prêt à enregistrer, prend moins de 20 secondes.
- **SC-006**: Adoption : l'utilisateur visé fait au moins 3 demandes par semaine en moyenne sur les
  4 premières semaines d'usage réel.
- **SC-007**: Le coût du service extérieur reste sous 5 € par mois à l'usage constaté.

## Assumptions

- Le fournisseur, le partage des rôles entre service extérieur et serveur, et la protection des noms
  de clients sont ceux de l'ADR-012 : transcription, compréhension et voix chez Mistral (UE), compte
  en paiement à l'usage, données exclues de l'entraînement, audio conservé 30 jours chez Mistral.
- Les clients ne sont pas informés individuellement de l'usage de l'assistant (choix assumé,
  `docs/cadrage-assistant-vocal.md` §5).
- Limite par défaut : 30 demandes par heure et par compte, réglable ; à l'usage prévu, bien au-delà
  du besoin et bien en deçà d'un coût gênant.
- Seuls les quatre écrans de liste portent « Dicter » ; les écrans de détail et les formulaires n'en
  ont pas.
- Une demande ne prépare qu'une vente ; pas de conversation à plusieurs tours (l'assistant ne pose
  pas de question en retour).
- Français uniquement. Pas de mot déclencheur : l'écoute commence toujours par un appui.
- Téléphone Android avec Chrome en priorité ; PC équipé d'un micro pris en charge. Une connexion
  réseau est nécessaire (ADR-001).
- Le délai et l'adoption se mesurent après livraison, sur le journal des demandes (FR-024, FR-025).
- Hors périmètre : annuler ou modifier une vente à la voix, créer un client ou un produit à la voix,
  saisir une fabrication à la voix, voix clonée.
