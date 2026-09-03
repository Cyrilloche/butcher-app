# Journal des décisions d'architecture (ADR) — Mini-ERP Charcuterie

| | |
|---|---|
| **Projet** | Mini-ERP Charcuterie (repo : `butcher-app`) |
| **Document** | Architecture Decision Record — Journal |
| **Version** | 0.1 |
| **Date** | 2 septembre 2026 |
| **Statut** | En cours de cadrage technique |
| **Document lié** | PRD Mini-ERP Charcuterie v0.1 |

### À propos de ce document

Un **ADR** (Architecture Decision Record) documente une décision d'architecture significative : son contexte, la décision prise, ses conséquences, et les alternatives écartées. L'objectif n'est pas de décrire *comment* le code fonctionne, mais de tracer *pourquoi* les grands choix ont été faits — l'information qui, sinon, se perd et coûte cher à reconstituer.

Chaque décision porte un **statut** : `Proposé` (en débat), `Accepté` (validé), `Déprécié` (abandonné) ou `Remplacé` (par un ADR ultérieur). Une décision acceptée n'est pas gravée dans le marbre : elle peut être remplacée par un nouvel ADR qui la référence explicitement.

### Index des décisions

| N° | Décision | Statut |
|---|---|---|
| ADR-001 | Architecture applicative client-serveur (sans mode hors-ligne) | Accepté |
| ADR-002 | Hébergement auto-géré (self-hosted) plutôt que managé (BaaS) | Accepté |
| ADR-003 | Séparation frontend / backend via un contrat d'API REST | Accepté |
| ADR-004 | Backend en ASP.NET Core (C#) | Accepté |
| ADR-005 | Frontend en Vue 3 + TypeScript, packagé en PWA | Accepté |
| ADR-006 | Bibliothèque de composants UI (Vuetify ou PrimeVue) | Accepté |
| ADR-007 | PostgreSQL comme système de gestion de base de données | Accepté |
| ADR-008 | Entity Framework Core + Npgsql comme couche d'accès aux données | Accepté |
| ADR-009 | Authentification par jetons JWT, adossée à ASP.NET Core Identity | Proposé |
| ADR-010 | Déploiement conteneurisé (Docker Compose + reverse proxy HTTPS) | Accepté |

---

## ADR-001 — Architecture applicative client-serveur (sans mode hors-ligne)

**Statut :** Accepté

### Contexte

L'application sera utilisée principalement au domicile des exploitants, où un réseau (wifi ou 4G) est disponible. La vente est informelle et se déroule quasi exclusivement sur place. La question d'une architecture *local-first* (fonctionnement hors-ligne avec synchronisation et résolution de conflits, comme le moteur CRDT utilisé sur un autre projet de l'auteur) s'est posée.

### Décision

L'application adopte une architecture **client-serveur classique** : le client (PWA) communique avec un serveur central via le réseau à chaque opération significative. **Aucun mode hors-ligne** (offline-first) n'est développé en V1.

### Conséquences

**Positives**
- Réduction majeure de la complexité : la synchronisation et la résolution de conflits sont, de loin, la partie la plus risquée et coûteuse d'une application. Les écarter est cohérent avec l'objectif « robuste, pas un tank ».
- Modèle de données et logique métier plus simples à raisonner et à faire évoluer.
- Cohérent avec un usage à faible probabilité d'accès concurrent (cf. PRD, RNF-05).

**Négatives / à surveiller**
- L'application requiert une connexion active pour enregistrer une vente ou une pesée. En cas de coupure réseau, la saisie est bloquée.
- Si un besoin de mobilité hors-couverture émerge (ex. vente sur un marché sans réseau), il faudra le traiter dans une vague ultérieure. Le caractère PWA préserve la porte : un cache applicatif et une file d'attente locale pourront être ajoutés sans refonte du serveur.

### Alternatives écartées

- **Local-first / offline-first (synchro + CRDT)** : rejeté comme sur-ingénierie au regard du besoin réel et du profil d'usage.

---

## ADR-002 — Hébergement auto-géré (self-hosted) plutôt que managé (BaaS)

**Statut :** Accepté

### Contexte

Deux philosophies d'hébergement étaient envisageables : une approche **managée** (Backend-as-a-Service type Supabase, qui fournit base, authentification et API clés en main), ou une approche **auto-gérée** (serveur privé virtuel, base et services opérés par l'auteur). Un objectif explicite de l'auteur est la **montée en compétence** technique, y compris sur les aspects d'exploitation (ops).

### Décision

L'ensemble de la solution est **auto-hébergé** sur une infrastructure maîtrisée par l'auteur (VPS), sans dépendance à un fournisseur de BaaS.

### Conséquences

**Positives**
- Contrôle total sur les données, la configuration et le cycle de vie de l'application.
- Absence de *vendor lock-in* : aucune dépendance forte à un fournisseur propriétaire.
- Sert directement l'objectif de développement de compétences (déploiement, réseau, HTTPS, base de données, sécurité).
- Coûts prévisibles et faibles pour un VPS modeste.

**Négatives / à surveiller**
- L'auteur porte la responsabilité de l'exploitation : mises à jour de sécurité, sauvegardes, disponibilité, supervision.
- L'**authentification exposée sur Internet** doit être conçue et opérée avec soin (cf. ADR-009), sans le filet d'un service managé.
- La mise en place initiale demande davantage de travail qu'un BaaS.

### Alternatives écartées

- **BaaS managé (ex. Supabase)** : rapide à mettre en œuvre et sécurisant sur l'auth, mais en contradiction avec l'objectif de montée en compétence et introduisant une dépendance fournisseur.

---

## ADR-003 — Séparation frontend / backend via un contrat d'API REST

**Statut :** Accepté

### Contexte

L'auteur maîtrise le C# côté backend mais n'est pas développeur frontend, et souhaite néanmoins une interface soignée. Le frontend et le backend seront donc réalisés dans deux écosystèmes distincts (JavaScript/TypeScript et .NET). Il faut une organisation qui rende ce découplage sain plutôt que source de friction permanente.

### Décision

Le projet est structuré en **monorepo** avec une **séparation nette entre deux applications** — un dossier `/frontend` et un dossier `/backend` — dont la **frontière est un contrat d'API REST** (endpoints HTTP, formats JSON échangés). Le backend ne connaît rien du frontend et réciproquement ; ils communiquent exclusivement via ce contrat.

### Conséquences

**Positives**
- Chaque application est un projet cohérent, avec son propre cycle de vie, sa configuration et son conteneur.
- Le contrat d'API agit comme point de stabilité : tant qu'il est respecté, les deux côtés évoluent indépendamment.
- Approche « API-first » : bénéfique pédagogiquement et pérenne (un autre client — mobile natif, etc. — pourrait consommer la même API plus tard).
- Facilite la délégation : le frontend peut être largement piloté avec assistance sans perturber le backend.

**Négatives / à surveiller**
- Nécessite de définir et maintenir le contrat d'API avec rigueur (documentation des endpoints, cohérence des formats). L'usage d'OpenAPI/Swagger côté ASP.NET Core est recommandé pour formaliser ce contrat.
- Deux langages à gérer dans un même dépôt : l'apprentissage de l'auteur se concentre sur le backend, le frontend restant principalement assisté (compromis assumé).

### Alternatives écartées

- **Application full-stack unifiée (ex. Blazor en C#)** : aurait concentré l'apprentissage sur un seul langage, mais l'auteur a privilégié la richesse de l'écosystème UI JavaScript pour la qualité de l'interface (cf. ADR-005).

---

## ADR-004 — Backend en ASP.NET Core (C#)

**Statut :** Accepté

### Contexte

Le C# est la technologie backend la mieux maîtrisée par l'auteur, et l'un de ses objectifs est de la retravailler et d'y progresser. Le backend doit exposer une API REST, gérer la logique métier, la persistance et l'authentification, en auto-hébergement.

### Décision

Le backend est développé avec **ASP.NET Core** (Web API), en **C#**.

### Conséquences

**Positives**
- Capitalise sur la compétence existante de l'auteur et sert son objectif de montée en compétence.
- Framework mature, performant, multiplateforme, adapté à un déploiement conteneurisé sous Linux.
- Écosystème riche et intégré pour les besoins du projet (API, ORM via EF Core, authentification via Identity).

**Négatives / à surveiller**
- Aucune contrainte majeure identifiée ; choix aligné avec l'ensemble des décisions.

### Alternatives écartées

- **Node.js / autres backends** : écartés car ils n'apporteraient rien face au C# déjà maîtrisé, et disperseraient l'apprentissage.

---

## ADR-005 — Frontend en Vue 3 + TypeScript, packagé en PWA

**Statut :** Accepté

### Contexte

L'interface doit être « sympa » visuellement, mobile-first (usage terrain) avec une vue PC, et packagée en PWA (cf. PRD, RNF-01). L'auteur n'a pas d'expérience frontend et s'appuiera largement sur l'assistance pour cette partie. Le choix devait donc privilégier une technologie **accessible**, bien documentée et à écosystème riche.

### Décision

Le frontend est développé avec **Vue 3** et **TypeScript**, et packagé en **Progressive Web App**.

### Conséquences

**Positives**
- Vue est réputé pour sa **courbe d'apprentissage douce** et son excellente documentation, ce qui limite le coût d'entrée pour un non-spécialiste.
- Écosystème mûr et large, propice à l'assistance et à l'ajout de composants soignés.
- TypeScript apporte un typage statique qui sécurise le code et fluidifie la collaboration front/back autour du contrat d'API.
- Le packaging PWA répond au besoin d'installation sur l'écran d'accueil et de responsive mobile/PC.

**Négatives / à surveiller**
- Introduit un second langage dans le projet : l'auteur maîtrisera moins cette partie, qui restera principalement assistée (compromis assumé au titre de la qualité d'interface).
- La partie **PWA + authentification** est le point le plus délicat du montage (gestion des jetons, rafraîchissement, sécurité) ; à traiter en priorité (cf. ADR-009 et section « Suite »).

### Alternatives écartées

- **Blazor WebAssembly (C#)** : aurait unifié le langage de bout en bout et concentré l'apprentissage sur C#, mais l'écosystème UI est moins fourni et l'auteur a privilégié la richesse du monde JavaScript pour l'interface.
- **React / Svelte** : React a une courbe d'apprentissage plus raide pour un débutant ; Svelte a un écosystème plus restreint. Vue offre le meilleur compromis accessibilité / richesse pour ce contexte.

---

## ADR-006 — Bibliothèque de composants UI (Vuetify ou PrimeVue)

**Statut :** Accepté

### Contexte

Pour obtenir une interface soignée sans repartir d'une page blanche, l'usage d'une bibliothèque de composants prêts à l'emploi (boutons, formulaires, tableaux, navigation, dialogues) est nécessaire. Deux candidats principaux se dégageaient dans l'écosystème Vue 3 : Vuetify et PrimeVue.

### Décision

**Vuetify** est retenu.

### Conséquences

**Positives**
- Implémentation Material Design cohérente « prête à l'emploi » : peu de décisions esthétiques à prendre soi-même, ce qui convient à un auteur non-designer.
- Documentation excellente, écosystème mature, forte adéquation avec une UI mobile propre et lisible (composants `v-bottom-navigation`, `v-card`, etc. déjà alignés avec la direction visuelle validée en maquette).
- Bon support de l'accessibilité et des grandes zones tactiles, pertinent pour des utilisateurs âgés (RNF-02).

**Négatives / à surveiller**
- Signature visuelle Material assez reconnaissable ; écarté au profit de la cohérence et de la rapidité de mise en œuvre plutôt qu'une identité graphique sur mesure.

### Alternatives écartées

- **PrimeVue** — catalogue plus riche et plus flexible, mais davantage de décisions de design à la charge de l'auteur ; écarté au profit de la simplicité d'usage de Vuetify.
- **UI entièrement sur mesure (sans bibliothèque)** : rejeté — coût de développement disproportionné pour le besoin.

### Alternatives écartées

- **UI entièrement sur mesure (sans bibliothèque)** : rejeté — coût de développement disproportionné pour le besoin, et contraire à l'objectif de rapidité.

---

## ADR-007 — PostgreSQL comme système de gestion de base de données

**Statut :** Accepté

### Contexte

Le modèle de données est fortement **relationnel** : la traçabilité repose sur des chaînes de clés étrangères (`mouvement → unité physique → lot → produit`, `mouvement → client`). L'intégrité référentielle est au cœur de la valeur métier (savoir précisément quel lot a été vendu à quel client). L'hébergement est auto-géré.

### Décision

La base de données est **PostgreSQL**.

### Conséquences

**Positives**
- SGBD relationnel robuste, open source, sans coût de licence, éprouvé en production.
- Garanties d'intégrité (contraintes, clés étrangères, transactions) parfaitement adaptées aux besoins de traçabilité.
- Compagnon naturel de l'écosystème .NET en auto-hébergement ; excellent support via Npgsql (cf. ADR-008).
- Marge de manœuvre confortable pour les évolutions V2+ (coût de revient, historisation, requêtes analytiques).

**Négatives / à surveiller**
- L'auto-hébergement implique la responsabilité des **sauvegardes** et de la maintenance de la base (à intégrer au plan d'exploitation).

### Alternatives écartées

- **SQLite** : suffisant en volume, mais moins adapté à un service exposé avec accès potentiellement concurrents, et moins formateur sur l'ops.
- **Bases NoSQL** : inadaptées à un modèle aussi relationnel ; compliqueraient la traçabilité au lieu de la servir.

---

## ADR-008 — Entity Framework Core + Npgsql comme couche d'accès aux données

**Statut :** Accepté

### Contexte

Le backend C# doit lire et écrire dans PostgreSQL. Deux approches : écrire du SQL à la main, ou passer par un ORM (mapping objet-relationnel) qui permet de manipuler la base via des classes C#.

### Décision

L'accès aux données se fait via **Entity Framework Core** (l'ORM standard de .NET), avec le provider **Npgsql** (`Npgsql.EntityFrameworkCore.PostgreSQL`) qui assure la communication avec PostgreSQL.

### Conséquences

**Positives**
- Le modèle de données est défini en **classes C#** ; EF Core génère et fait évoluer le schéma via ses **migrations**, ce qui versionne proprement la structure de la base.
- Réduit fortement le SQL manuel et les erreurs associées ; accélère le développement.
- Npgsql est le driver de référence pour .NET + PostgreSQL, mature et sans réelle alternative concurrente : choix sans risque.

**Négatives / à surveiller**
- Un ORM ajoute une couche d'abstraction : sur certaines requêtes complexes (futurs rapports de rentabilité), il faudra veiller à la performance et éventuellement descendre en SQL ciblé.
- La discipline des migrations doit être tenue dès le début pour éviter les dérives de schéma.

### Alternatives écartées

- **Micro-ORM (ex. Dapper)** : plus proche du SQL et très performant, mais davantage de code manuel ; EF Core est mieux adapté à la productivité recherchée et à un apprentissage structuré.
- **ADO.NET / SQL brut** : trop bas niveau pour ce projet.

---

## ADR-009 — Authentification par jetons JWT, adossée à ASP.NET Core Identity

**Statut :** Proposé — à préciser

### Contexte

L'application sera **exposée sur Internet**, ce qui rend l'authentification obligatoire dès la V1 (cf. PRD, RF-25). Avec une architecture front/back séparée (ADR-003), l'authentification se gère côté API. La V1 se contente d'un **compte simple** partagé, sans rôles différenciés, mais le modèle doit préparer une future distinction des utilisateurs (champ `cree_par`, cf. PRD RF-27).

### Décision (proposée)

L'authentification s'appuie sur **ASP.NET Core Identity** pour la gestion des utilisateurs (stockage sécurisé des identifiants, hachage des mots de passe), avec un échange par **jetons JWT** (JSON Web Tokens) entre le frontend et l'API : le client s'authentifie, reçoit un jeton, et le présente à chaque appel d'API.

### Conséquences

**Positives**
- Identity est une brique éprouvée et intégrée à .NET : elle évite de réinventer la sécurité (hachage, gestion des comptes), point critique sur un service exposé.
- Le modèle par jetons est le standard pour une API consommée par un client séparé (SPA/PWA).
- Auto-hébergé, sans dépendance externe, cohérent avec ADR-002.

**Négatives / à surveiller**
- La **gestion du cycle de vie des jetons** (expiration, rafraîchissement, stockage côté client, révocation) est le point le plus délicat de tout le montage. Un stockage inadéquat du jeton côté navigateur est une source classique de vulnérabilité.
- Ce sujet doit faire l'objet d'un **spike technique dédié et prioritaire** avant le développement des fonctionnalités métier, afin de valider une approche sûre (mécanisme de *refresh token*, stratégie de stockage, HTTPS strict).

### Alternatives écartées

- **Fournisseur d'identité externe (OAuth/OpenID managé)** : réduirait le risque sécurité mais introduirait une dépendance externe, en tension avec l'objectif d'auto-hébergement et de montée en compétence.
- **Authentification par cookie de session classique** : viable, mais moins naturelle pour une API découplée consommée par une PWA ; le choix entre cookie sécurisé et JWT sera définitivement tranché lors du spike d'authentification.

---

## ADR-010 — Déploiement conteneurisé (Docker Compose + reverse proxy HTTPS)

**Statut :** Accepté

### Contexte

La solution auto-hébergée réunit plusieurs composants (frontend, backend, base de données) qui doivent être déployés ensemble sur un VPS, avec un accès sécurisé en HTTPS. L'auteur souhaite également progresser sur les aspects d'exploitation.

### Décision

Chaque composant est **conteneurisé (Docker)** et l'ensemble est orchestré par **Docker Compose** sur le VPS. Un **reverse proxy** (Caddy, ou Nginx) place le service derrière **HTTPS**, avec obtention et renouvellement automatiques des certificats (Let's Encrypt).

### Conséquences

**Positives**
- Environnements reproductibles et cohérents entre développement et production.
- Déploiement simplifié d'une pile multi-composants par une configuration unique.
- HTTPS automatisé (Caddy le gère nativement), indispensable pour un service exposé et pour la sécurité de l'authentification.
- Progression sur les compétences ops visée par l'auteur.

**Négatives / à surveiller**
- La responsabilité de l'exploitation (sauvegardes de la base, mises à jour, supervision, sécurité du VPS) incombe à l'auteur.
- Une stratégie de **sauvegarde de la base PostgreSQL** doit être définie dès la mise en production.

### Alternatives écartées

- **Déploiement sans conteneurs (services installés directement sur le VPS)** : plus fragile, moins reproductible, et moins formateur sur les pratiques actuelles.
- **Plateforme managée (PaaS)** : contraire à l'objectif d'auto-hébergement (cf. ADR-002).

---

## Synthèse de la pile technique retenue

| Couche | Technologie |
|---|---|
| **Frontend** | Vue 3 + TypeScript, packagé en PWA + bibliothèque de composants (Vuetify ou PrimeVue — *à trancher*) |
| **Backend** | ASP.NET Core Web API (C#) |
| **Contrat** | API REST (documentée via OpenAPI/Swagger) |
| **Accès données** | Entity Framework Core + Npgsql |
| **Base de données** | PostgreSQL |
| **Authentification** | ASP.NET Core Identity + jetons JWT (*à préciser via spike*) |
| **Déploiement** | Docker Compose sur VPS, reverse proxy Caddy/Nginx, HTTPS Let's Encrypt |

---

## Décisions restant à trancher

| Réf. | Point ouvert | Pour quand |
|---|---|---|
| ADR-006 | Choix définitif de la bibliothèque de composants (Vuetify vs PrimeVue) | Avant démarrage du frontend |
| ADR-009 | Validation de la stratégie d'authentification (JWT + refresh, stockage client) via un **spike technique prioritaire** | Avant le développement des fonctionnalités métier |
| — | Choix du VPS et mise en place du socle de déploiement | Phase de mise en place technique |
| — | Stratégie de sauvegarde de la base | Avant mise en production |

---

*Fin du document — version 0.1. Journal vivant : toute nouvelle décision structurante fait l'objet d'un ADR additionnel ; toute remise en cause d'une décision acceptée donne lieu à un ADR de remplacement qui référence le précédent.*