# Changelog

Généré à partir des messages de commit ([Conventional Commits](https://www.conventionalcommits.org/fr/)).
Ne pas éditer à la main : régénérer avec `make changelog`.

## frontend-v0.8.0 — 24/09/2026

### Nouveautés

- **backend** : Reconnaissance des clients dans une phrase dictée (spike assistant vocal, étape 2) (0ecdf44)
- **backend** : Chaîne de l'assistant vocal sur du texte (spike assistant vocal, étape 3) (e74d587)
- **frontend** : Bouton micro de l'assistant vocal (spike assistant vocal, étape 4) (4944efc)
- L'assistant répond avec la voix de Mistral (Voxtral TTS) (64ff661)
- **backend** : Assistant vocal activé compte par compte, table voice_request (5b78998)
- **backend** : Chaque demande à l'assistant est journalisée, la phrase dite vient du serveur (d27a5e4)
- **frontend** : Contrat de l'assistant, activation lue sur le compte connecté (2b16f43)
- La voix de Mistral ne lit que la réponse d'une demande du compte (6418af4)
- **backend** : Limite de demandes à l'assistant par compte (56566ef)
- **backend** : Rapports d'usage de l'assistant vocal (de99d11)
- **frontend** : Assistant activé compte par compte, usage dans les Rapports (5720e8d)
- L'assistant vocal prêt pour la production (80e74f8)

### Corrections

- **backend** : L'assistant dit une phrase de stock construite par le serveur (834ee7b)
- **frontend** : Une nouvelle vente est « À payer » par défaut (6f29836)
- **backend** : La réponse de stock n'accorde plus le nom du produit (87f60d3)
- **backend** : L'assistant accepte l'audio envoyé par Chrome (c29e213)
- **frontend** : L'assistant se lance depuis le « + » et s'arrête quand on se tait (1e30339)

### Documentation

- Cadrage de l'assistant vocal (R&D) et jeu de phrases de test (150fbf6)
- Assistant vocal — réponse de stock en deux niveaux, R&D en local (1c8c667)
- Plan du spike de l'assistant vocal (b64b933)
- Seuils de réussite du spike de l'assistant vocal arrêtés (6be942a)
- Fonctionnement de l'assistant vocal, de l'appui sur le bouton à la vente enregistrée (d2d9f22)
- Retours des essais de l'assistant vocal sur téléphone (17c81d8)
- Bilan du spike de l'assistant vocal et ADR-012 proposé (9f43ac7)
- Go vers la spec de l'assistant vocal, compte Mistral en paiement à l'usage (86da775)
- ADR-012 au sommaire du journal des décisions (a22cca1)
- ADR-012 accepté (4f073d0)
- Spec 006, assistant vocal (aee342e)
- Plan de la spec 006, assistant vocal (267cad4)
- Tâches de la spec 006, assistant vocal (0d0f50f)
- RF-34 à RF-36 et voice_request dans les documents de référence (bc245c2)
- L'assistant vocal dans CLAUDE.md et l'explication du fonctionnement (3c79ee6)

### Tests

- **backend** : La question de stock dictée, avec un LLM simulé (1bad67e)
- La vente dictée et le client, jamais un autre que celui cité (a3e5551)

## frontend-v0.7.0 — 17/09/2026

### Nouveautés

- **frontend** : Nouvelle vente depuis la fiche d'un client, client déjà choisi (RU-04) (8279b90)
- **frontend** : Ajouter un produit à une vente enregistrée, quand le client demande un complément (RU-03) (eca7194)

### Corrections

- **frontend** : La recherche d'une vente propose tous les produits, puis toutes leurs unités (RU-02) (f196cd1)
- **frontend** : La barre latérale tient sans défiler sur un écran de portable (RU-05) (0ffb887)
- **frontend** : La saisie d'une vente dit ce qui manque tant que le bouton est grisé (RU-01) (4098cd6)

### Documentation

- Retours utilisateurs de la recette du 2026-09-16 (1c696cc)
- Retours utilisateurs référencés dans CLAUDE.md (09f3e0c)
- RU-06 et RU-07 sans suite, correctifs de la recette publiés en frontend-v0.7.0 (2cd003b)

## frontend-v0.6.0 — 14/09/2026

### Nouveautés

- **backend** : Comptes nominatifs, rôle et état actif sur app_user (ADR-011) (3833593)
- **backend** : Compte de la requête courante (ADR-011) (1f6d0a2)
- **backend** : Réponse 403 pour une action réservée (5c5634a)
- **backend** : L'auteur des fabrications, ventes et sorties est enregistré (RF-27) (5337064)
- **backend** : Droits relus en base à chaque requête (ADR-011) (a49b8c6)
- **backend** : Politique de mot de passe par rôle, messages en français ⚠️ **rupture** (b7c3db6)
- **backend** : Un compte désactivé ne se connecte plus (FR-007) (34a6250)
- **backend** : Compte connecté et changement de son mot de passe (dbdfce2)
- **backend** : Gestion des comptes, dernier administrateur protégé (ADR-011) (7553eef)
- **backend** : Routes de gestion des comptes, réservées à l'administrateur (a0db95d)
- **frontend** : Client des comptes et du compte connecté (ADR-011) (e53c46b)
- **backend** : Le nom de l'auteur est exposé sur les ventes, fabrications et sorties (6422f4a)
- **frontend** : La session connaît le compte connecté (ADR-011) (0e47f0c)
- **frontend** : L'auteur d'une vente et d'une fournée est affiché (RF-27) (bfb725f)
- **backend** : Désactiver ou solder un produit est réservé à l'administrateur ⚠️ **rupture** (4bf12ab)
- **frontend** : Écrans Comptes et Mon compte (ADR-011) (35c99bd)
- **frontend** : Les gestes qui retirent un produit sont réservés à l'administrateur (cd43452)
- **frontend** : Barre latérale sur écran large (backoffice PC) (229f062)
- **frontend** : Calculs de la vue d'ensemble, premiers tests frontend (ba13d35)
- **frontend** : Les formulaires restent lisibles sur écran large (b45f1e2)
- **frontend** : Vue d'ensemble du backoffice PC (62acb60)
- **frontend** : Ventes filtrables et triables sur écran large (E-06) (95dd35c)
- **frontend** : Formulaires d'ajout en fenêtre sur écran large (57e447a)
- **frontend** : Stock en tableau sur écran large (eaa9bdd)
- **frontend** : Clients en tableau sur écran large, avec leurs achats (bbb7ece)
- **frontend** : Produits en tableau sur écran large (8b5de2d)
- **backend** : Table du journal audit_entry (US4) (a2f4e9a)
- **backend** : Journal des gestes écrit à chaque enregistrement (US4) (dd854ed)
- **backend** : Connexions réussies et refusées au journal (US4) (5051532)
- **backend** : Consultation du journal, réservée à l'administrateur (US4) (3fa3e1e)
- **frontend** : Écran Journal pour l'administrateur (US4) (9e33042)
- **backend** : Rapports de ventes, réservés à l'administrateur (US5) (2df6927)
- **frontend** : Écran Rapports, et vue d'ensemble lue sur les rapports (US5) (cececb5)

### Corrections

- **backend** : Using manquant pour le compte de la requête (99efa77)
- **backend** : Le refus d'un email déjà pris ne se répète plus (b036c87)
- **frontend** : Les éléments fixés en bas d'écran suivent la barre latérale (9618480)
- **frontend** : Les pages de consultation prennent la largeur de la vue d'ensemble (f3d7f5d)
- **frontend** : Toutes les fenêtres ont la même largeur (5dc7480)
- **backend** : Borner le numéro de vente sur la journée de Paris (db8ecb4)
- **frontend** : Titre Saloir et favicon provisoire dans l'onglet (e6bb425)

### Refactorisations

- **frontend** : Tableau triable commun aux écrans de consultation (6c72b29)

### Documentation

- **specs** : Spécification du backoffice PC, comptes nominatifs et rôles (b522ecc)
- **specs** : Clarifications du backoffice PC (3b3fc17)
- **specs** : Plan du backoffice PC, comptes nominatifs et rôles (ca034cb)
- **specs** : Tâches du backoffice PC (315be64)
- **adr** : ADR-011, comptes nominatifs avec deux rôles (cd6ca60)
- Comptes nominatifs et rôles dans le PRD, le modèle et la constitution (7640c47)
- **specs** : Résultat du quickstart du lot 1 (comptes et rôles) (da88f07)
- **design** : Maquette PC de la vue d'ensemble du backoffice (01b556e)
- **specs** : Plan et recherche du backoffice réalignés sur le code livré (3d0668a)
- **specs** : Journal et rapports du backoffice détaillés (88afb02)
- Journal et rapports dans le PRD, le modèle et CLAUDE.md (1649630)
- **specs** : Résultat du quickstart des lots 3 et 4 (journal et rapports) (86031f7)
- Backoffice fusionné, tests frontend et titre clos dans l'état des lieux (ed436c8)

### Tests

- **frontend** : Couvrir le client HTTP, la session, la garde de navigation et les écrans critiques (b381cab)

## frontend-v0.5.0 — 12/09/2026

### Nouveautés

- **frontend** : Corriger le prix d'une fournée depuis Détail Stock (RG-10) (859f1f9)
- **backend** : Protège la connexion contre l'essai en rafale (5fe83a5)
- **deploy** : En-têtes de sécurité sur toutes les réponses Caddy (27ce927)
- **backend** : Politique de mot de passe forte et commande set-password ⚠️ **rupture** (8996289)

### Documentation

- État des lieux v2.0 et durcissement de la connexion (37400c6)
- En-têtes de sécurité et redirection HTTPS constatés en prod (3b3a81d)

## frontend-v0.4.0 — 12/09/2026

### Nouveautés

- **backend** : Modification et fin de vie d'un produit ⚠️ **rupture** (fdca710)
- **frontend** : Corriger un produit, supprimer un lot, solder le stock (0c19cde)
- **backend** : Le numéro d'étiquette est porté par l'unité ⚠️ **rupture** (b4be76d)
- **frontend** : Afficher le numéro d'étiquette de chaque unité (e43f2f1)
- **frontend** : Corriger et supprimer une vente (RG-14, RG-11) (163f4c6)
- **backend** : Expose le poids encore vendable d'une unité (RG-05) (156f2ba)
- **frontend** : Affiche ce qu'il reste à vendre sur un jambon entamé (52e0bca)

### Corrections

- **frontend** : Trier les unités vendables par numéro d'étiquette (4ffdc19)
- **frontend** : Un refus de paiement se voit, un article n'est plus un lot (d9007c9)
- **frontend** : Retouches de mise en forme du détail stock (c569fe8)
- **backend** : Le poids restant lu en liste compte enfin les tranches vendues (RG-05) (8ca50e1)
- **frontend** : Un jambon entamé compte comme une unité en stock (6b05914)
- **frontend** : La saisie d'une vente ne coupe plus le numéro ni le libellé de la tranche (5257e6c)
- **backend** : Renseigne created_at et updated_at à chaque enregistrement (RF-27) (ca42747)

### Documentation

- Update docs with speckit & specify and plan new feature (dfc112d)
- Modèle de données et spec de la modification d'un produit (6463a11)
- Le numéro d'étiquette identifie l'unité, pas la fabrication (32633cc)
- Coche les tâches réalisées de la renumérotation (6816220)
- Spécification et plan de la correction d'une vente (24dfd91)
- Reporte la DLC et la matière première en V2, réaligne les règles (e220b33)
- La recette de la correction d'une vente est déroulée et validée (a6669e4)
- Spécification du poids encore vendable d'une unité entamée (dce1b62)
- Fige la maquette validée du détail d'un produit en stock (0d97085)
- Plan technique du poids encore vendable (b2dbe92)
- Découpe le poids encore vendable en tâches (394863f)
- RG-05 interdit le stockage du restant, pas son calcul (163f69f)

## frontend-v0.3.1 — 04/09/2026

### Corrections

- **deploy** : Le Cache-Control de sw.js ne s'appliquait jamais (6294576)

## frontend-v0.3.0 — 04/09/2026

### Nouveautés

- **frontend** : Sorties perso et perte depuis Détail Stock (RF-21) (ed9acdd)

### Corrections

- **frontend** : Le rail alphabétique décalait toute la page Clients (9f56af6)

## frontend-v0.2.0 — 04/09/2026

### Nouveautés

- **frontend** : Affiche la version du build sur l'écran de connexion (a918d87)
- **frontend** : Déconnexion, bandeau contrasté et index A-Z des clients (ad0e0db)

### Documentation

- Analyse d'écart prévu/réalisé et remise à niveau de la documentation (cdeb0d7)

### Intégration et déploiement

- Génère le changelog et les notes de release avec git-cliff (dc60f88)

## backend-v0.2.0 — 04/09/2026

### Nouveautés

- **backend** : Commande hors-ligne create-user pour ajouter un compte en prod (a03be82)

### Corrections

- **deploy** : Drop caddy host port publish in prod compose (6e642b9)

## frontend-v0.1.0 — 04/09/2026

### Nouveautés

- Add domain entities and enums (4772ba9)
- Wire EF Core, Postgres dev container and env-based config (a4bde0b)
- Expose UnitOfMeasure API with error-handling middleware and tests (543d000)
- Expose Product API (CRUD, deactivate/reactivate) with tests (5ffc3c8)
- Expose ProductionBatch API with auto-generated batch numbers (22e9926)
- Expose StockUnit generation and listing API (04ae3b3)
- Expose StockMovement API (sale/personal/loss) with status transitions (da81731)
- Expose Customer API (CRUD) (15876ba)
- Add authentication spike (ASP.NET Core Identity + JWT + refresh token) (9180de8)
- Add Scalar API reference UI with JWT bearer auth support (ca77dbb)
- **frontend** : Implement Stock views and reusable Kraft components (a649b3f)
- **frontend** : Add brand header to Dashboard views (8221fe8)
- **frontend** : Wire Stock views to the real API + auth (60a2891)
- **frontend** : Implement Products views + document Ventes/Clients gaps (a11e61e)
- **frontend** : Implement Customers views (670adc5)
- **frontend** : Skeleton Sales views (Dashboard/Détail/Ajout) (ad48777)
- **backend** : Add the sale entity (QM-04 / Q-04 / Q-05) (4c06ab5)
- **frontend** : Rewire Ventes onto the new /api/sales entity (b6360fd)
- **backend** : Expose productName and batchNumber on sale lines (9dabc01)
- **backend** : Remove the unit_of_measure entity (product decision) ⚠️ **rupture** (8d1e258)
- **frontend** : Drop unit_of_measure following backend removal ⚠️ **rupture** (1d31576)
- **backend** : Add allow_partial_sale to product, enforce it server-side (7b7317b)
- **frontend** : Partial sale flow (vente à la tranche) (2c60520)
- **frontend** : Show remaining weight estimate on partial sales (02dc991)
- **frontend** : Close stock units, drop unused customer delete; docs refresh (522947d)
- **deploy** : Containerize backend/frontend, reverse proxy + Cloudflare tunnel (ADR-010) (bc8e5cc)

### Corrections

- Serialize and store enums in snake_case, not PascalCase (cbf8cf1)
- **backend** : Pin RazorLangVersion to unblock build on .NET 10 SDK (790f57d)
- **frontend** : Send soldWeight when creating a sale line (8514172)
- **backend** : Enforce sold_weight cannot exceed unit weight (RG-05) (6f75b10)
- **frontend** : Drop empty batch cards from Détail Stock (30d77cd)

### Refactorisations

- **frontend** : Use StockMovementDto.productName/batchNumber directly (827430e)

### Documentation

- Create PRD (17a4e15)
- Create ARD (85c85b6)
- Create data-model (c882c60)
- Create CLAUDE.md (55a81d5)
- Pull ADR-006 and progress updates from frontend-init (adac6cd)
- Reconcile PRD, data-model and CLAUDE.md with backend implementation (19c7f2a)
- Formalize ADR-009 (authentication) as Accepted (ea36d99)

### Intégration et déploiement

- Adapt CI/release GitHub Actions template to butcher-app (41ccbee)

