# Journal des décisions d'architecture (ADR) — Mini-ERP Charcuterie

| | |
|---|---|
| **Projet** | Mini-ERP Charcuterie (repo : `butcher-app`) |
| **Document** | Architecture Decision Record — Journal |
| **Version** | 0.2 |
| **Date** | 11 septembre 2026 |
| **Statut** | Cadrage technique clos pour la V1 — les 10 décisions sont acceptées et toutes implémentées |
| **Documents liés** | PRD Mini-ERP Charcuterie v0.7, `docs/data-model.md` v0.10 |

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
| ADR-009 | Authentification par jetons JWT, adossée à ASP.NET Core Identity | Accepté — remplacé en partie par ADR-011 |
| ADR-010 | Déploiement conteneurisé (Docker Compose + reverse proxy HTTPS) | Accepté |
| ADR-011 | Comptes nominatifs avec deux rôles (administrateur, utilisateur) | Accepté |

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

**Statut :** Accepté — spike réalisé et vérifié. **Remplacé en partie par ADR-011** (2026-09-12) :
le compte unique partagé et l'absence de rôles cèdent la place à des comptes nominatifs porteurs
d'un rôle. Le mécanisme de jetons, le refresh rotatif et l'absence d'inscription publique restent
en vigueur.

### Contexte

L'application est **exposée sur Internet**, ce qui rend l'authentification obligatoire dès la V1 (cf. PRD, RF-25). Avec une architecture front/back séparée (ADR-003), l'authentification se gère côté API. La V1 se contente d'un **compte simple** partagé, sans rôles différenciés, mais le modèle doit préparer une future distinction des utilisateurs (champ `created_by`, cf. PRD RF-27).

Ce sujet a fait l'objet d'un **spike technique dédié**, réalisé avant tout développement fonctionnel supplémentaire, conformément à la méthode de dé-risquage du projet (cf. CLAUDE.md §2). Il visait spécifiquement le point identifié comme le plus délicat : la gestion du cycle de vie des jetons (expiration, rafraîchissement, stockage côté client, révocation).

### Décision

L'authentification s'appuie sur **ASP.NET Core Identity**, en version **allégée** (`IdentityUserContext<AppUser, Guid>` plutôt que le `IdentityDbContext` complet) — pas de table de rôles, cohérent avec RF-26 (compte simple partagé, aucun rôle différencié en V1). Le mécanisme retenu combine deux types de jetons, avec des propriétés de sécurité différentes et complémentaires :

- **Access token JWT**, courte durée de vie (**15 minutes**), signé HMAC-SHA256, contenant les claims minimales (`sub`, `email`, `jti`). Conservé **en mémoire uniquement côté client** (jamais persisté en `localStorage` ni ailleurs), donc perdu au rechargement de page — c'est voulu, le refresh token sert précisément à en obtenir un nouveau silencieusement.
- **Refresh token**, longue durée de vie (**30 jours**), **stocké côté serveur** (table `refresh_token`, valeur brute jamais persistée : seul un hash SHA-256 est conservé, comme un mot de passe) et transmis au client via un **cookie `httpOnly` + `Secure`**, avec un `Path` restreint à `/api/auth` (le cookie n'est jamais envoyé aux autres routes de l'API). Étant `httpOnly`, il est inaccessible à tout JavaScript côté client — donc non exfiltrable par une faille XSS, contrairement à un jeton en `localStorage`.

Le refresh token est **rotatif** : chaque utilisation en émet un nouveau et révoque l'ancien (`revoked_at`, `replaced_by_token_hash`). Une **détection de rejeu** est intégrée : si un refresh token déjà révoqué est présenté à nouveau (signe qu'il a été volé et qu'un attaquant tente de l'utiliser après le légitime propriétaire, ou l'inverse), **tous les refresh tokens actifs de l'utilisateur sont révoqués immédiatement**, forçant une reconnexion complète.

Par ailleurs, l'API adopte une **politique d'autorisation par défaut fail-closed** : toute route est protégée par défaut (`FallbackPolicy` exigeant un utilisateur authentifié), et seules `login`/`refresh`/`logout` sont explicitement ouvertes (`[AllowAnonymous]`). Il n'existe **aucune route d'inscription publique** : l'unique compte de la V1 est **seedé au démarrage** de l'application depuis des variables d'environnement (`Seed:AdminEmail`/`Seed:AdminPassword`), uniquement si aucun `app_user` n'existe encore.

### Conséquences

**Positives**
- Identity est une brique éprouvée et intégrée à .NET : elle évite de réinventer la sécurité (hachage, gestion des comptes), point critique sur un service exposé.
- La séparation access token (mémoire) / refresh token (cookie httpOnly) combine les forces des deux approches : pas de jeton longue durée exposé au JavaScript, et pas de nécessité de renvoyer les identifiants à chaque expiration d'access token.
- La rotation avec détection de rejeu transforme le vol d'un refresh token en incident détectable et auto-corrigé (révocation globale), plutôt qu'en compromission silencieuse et durable.
- L'absence d'inscription publique supprime toute une classe de risques (créations de comptes non désirées, énumération d'utilisateurs) pour un gain fonctionnel nul en V1 (un seul compte partagé).
- Auto-hébergé, sans dépendance externe, cohérent avec ADR-002.

**Négatives / à surveiller**
- Le cookie `Secure` impose HTTPS **même en développement local**, ce qui a nécessité un profil `https` dédié et le certificat de développement .NET (`dotnet dev-certs https --trust`) — un peu de friction locale en échange de la sécurité en production.
- `SameSite` du cookie de refresh est fixé à `Lax` par défaut (configurable via `Auth:RefreshCookieSameSite`). **Point tranché le 2026-09-04** : la topologie retenue par l'ADR-010 place le frontend et `/api/*` derrière le même reverse proxy Caddy, donc sur **la même origine** — les appels d'authentification sont same-origin et `Lax` convient sans réserve. Le réglage reste configurable au cas où une topologie en sous-domaines séparés serait un jour adoptée, ce qui imposerait alors `None` + `Secure`.
- ~~La politique de mot de passe d'ASP.NET Core Identity utilisée est celle par défaut (non personnalisée).~~ **Revue le 2026-09-12**, voir le complément ci-dessous.
- Sur un compte partagé, le verrouillage après échecs offre une prise à qui voudrait bloquer l'accès : cinq essais ferment la porte quinze minutes. Compromis assumé, borné par la durée courte du verrou et par la limitation de débit par IP.
- Les tables Identity annexes (`app_user_claim`, `app_user_login`, `app_user_token`) sont créées mais **inutilisées** en V1 (pas de login externe, pas de claims personnalisées) — conservées telles quelles car standard et inoffensif, sans nettoyage particulier.

### Alternatives écartées

- **Fournisseur d'identité externe (OAuth/OpenID managé)** : réduirait le risque sécurité mais introduirait une dépendance externe, en tension avec l'objectif d'auto-hébergement et de montée en compétence.
- **Authentification par cookie de session classique (sans JWT)** : viable, mais moins naturelle pour une API découplée consommée par une PWA ; le choix final combine en réalité les deux logiques (JWT pour l'access token, cookie sécurisé pour le refresh token), plutôt que de trancher entre l'une ou l'autre.
- **JWT longue durée sans refresh token, ou refresh token stateless (non stocké en base)** : plus simple, mais sans possibilité de révocation individuelle ni de détection de rejeu — jugé trop fragile pour un service exposé sur Internet (RNF-04).
- **Access token persisté en `localStorage`** : rejeté d'emblée, vulnérable à l'exfiltration par XSS ; c'est précisément ce que le choix mémoire + cookie `httpOnly` évite.
- **Inscription publique en V1** : inutile (un seul compte partagé) et source de risque additionnel sans bénéfice ; reportée indéfiniment tant que le besoin de plusieurs comptes ne se matérialise pas.

### Complément — durcissement de la connexion (2026-09-12)

Suite de l'audit de sécurité du 2026-09-05, qui ne relevait aucune faille exploitable mais une connexion ouverte à l'essai en rafale. Ce complément **précise** la décision sans la remplacer : compte partagé, access token en mémoire et refresh token rotatif en cookie restent la règle.

- **Verrouillage du compte** (lockout Identity) : 5 mots de passe erronés verrouillent le compte 15 minutes. `AuthService.LoginAsync` consulte le verrou **avant** de vérifier le mot de passe, pour qu'un compte verrouillé ne révèle pas si l'essai était le bon ; une connexion réussie remet le compteur à zéro.
- **Limitation de débit** : 10 tentatives par minute et par adresse IP sur `POST /api/auth/login` (rate limiter ASP.NET Core). L'IP est lue dans `CF-Connecting-IP` : derrière le tunnel et Caddy, l'adresse de connexion est celle d'un conteneur, commune à tous. L'en-tête n'est pas falsifiable, Caddy n'exposant aucun port.
- **Réponse** : `429` avec un message en français, affiché tel quel par l'écran de connexion.
- **Politique de mot de passe** : 32 caractères minimum, majuscule, minuscule, chiffre, caractère spécial, 12 caractères distincts. On vise une **phrase de passe** (`Finlike-Scorer4-Wildfire-Grazing-Unbiased-Sessions`), plus facile à recopier qu'un mot de passe court et tordu, et bien plus coûteuse à deviner. Réglages centralisés dans `IdentityPolicy`, partagé avec les tests.
- **Rotation** : la politique ne s'applique qu'à l'écriture d'un mot de passe. La commande hors ligne `set-password <email> <mot-de-passe>` met un compte existant en conformité, révoque ses sessions et lève un éventuel verrou.
- **En-têtes HTTP** posés par Caddy (ADR-010) : HSTS, `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy` et une CSP stricte — aucun script inline, styles inline autorisés pour le thème Vuetify, polices Google seules origines externes.

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

## ADR-011 — Comptes nominatifs avec deux rôles (administrateur, utilisateur)

**Statut :** Accepté (2026-09-12). **Remplace en partie ADR-009** sur deux points : le compte unique
partagé (RF-26) et l'absence de rôles.

### Contexte

La V1 fonctionne avec un seul compte partagé par les trois personnes qui utilisent l'outil (ADR-009,
RF-26). Ce choix tenait tant que personne n'avait besoin de savoir qui a saisi quoi. Il bute
désormais sur trois besoins exprimés pour le backoffice (`specs/005-backoffice`) :

- **tracer l'auteur** des fabrications, ventes et sorties, prévu par RF-27 mais impossible à
  renseigner avec un compte commun ;
- **réserver** au porteur de projet la gestion des comptes, les gestes qui engagent le catalogue
  (désactivation et solde en perte d'un produit), le journal et les rapports ;
- **isoler les accès** : changer le mot de passe commun déconnecte tout le monde, et un exploitant
  ne peut pas être retiré seul.

### Décision

- **Comptes nominatifs.** Chaque personne a son compte : adresse email comme identifiant, nom
  affiché, état actif ou désactivé. Un compte n'est jamais supprimé, seulement désactivé, parce
  qu'il est l'auteur d'enregistrements dont la traçabilité doit survivre. Il n'y a toujours pas
  d'inscription publique : seul un administrateur crée un compte.
- **Deux rôles fixes, portés par une colonne** `app_user.role` (`admin`, `user`). Pas de tables de
  rôles Identity : deux rôles exclusifs, sans droits à la carte, n'ont pas besoin d'une relation
  plusieurs-à-plusieurs ni d'un `RoleManager`. Identity reste utilisé en version allégée.
- **Droits relus en base.** Le jeton d'accès porte l'identité ; la base porte les droits. Une
  politique d'autorisation `AdminOnly` et la politique par défaut relisent le compte (actif,
  administrateur) à chaque requête. Une rétrogradation ou une désactivation prend donc effet
  immédiatement, sans attendre l'expiration du jeton. Désactiver un compte révoque aussi ses
  refresh tokens.
- **Le dernier administrateur actif est protégé** : aucune opération ne peut laisser l'outil sans
  administrateur.
- **Auteur posé à l'enregistrement.** `AppDbContext.SaveChanges` renseigne `created_by` à partir du
  compte de la requête, au même endroit que les dates d'audit. Aucun service n'a à y penser.
- **Politique de mot de passe par rôle** : 20 caractères au minimum pour un utilisateur, 32 pour un
  administrateur, par un validateur Identity qui lit le rôle du compte. Promouvoir un compte exige
  un nouveau mot de passe conforme au rôle cible.
- **Le compte courant est exposé** par `GET /api/auth/me`, relu en base, pour que l'interface
  adapte ce qu'elle propose. L'interface ne fait que masquer : le serveur refuse (`403`).

### Conséquences

**Positives**
- RF-27 devient effective : 100 % des saisies portent un auteur, sans modifier chaque service.
- Un exploitant peut être ajouté, retiré ou voir son mot de passe réinitialisé seul, sans toucher
  aux autres.
- Le socle du journal « qui a fait quoi » et du multi-comptes prévu en V2 est posé.

**Négatives / à surveiller**
- Une lecture en base par requête authentifiée pour vérifier le compte. Négligeable à trois
  comptes ; à mettre en cache si le nombre de comptes ou le trafic changeait d'ordre de grandeur.
- Trois routes produit répondent désormais `403` à un utilisateur : rupture de contrat, versionnée
  comme telle.
- Les enregistrements antérieurs n'ont pas d'auteur et ne sont pas réattribués : l'interface les
  présente comme « Compte partagé (avant comptes nominatifs) ».
- Le compte partagé existant devient le compte administrateur à la migration : ses identifiants ne
  doivent plus être communiqués aux exploitants.

### Alternatives écartées

- **`IdentityDbContext` complet avec `IdentityRole`** : trois tables, un gestionnaire et un seed de
  rôles pour deux rôles exclusifs.
- **Rôle en claim Identity** (`app_user_claim`) : paire clé/valeur sans contrainte en base.
- **Rôle lu uniquement dans le jeton** (`[Authorize(Roles = …)]`) : un administrateur rétrogradé
  garderait ses droits jusqu'à l'expiration du jeton d'accès.
- **Garder le compte partagé et ajouter un « nom de saisie » libre** : déclaratif, non vérifié,
  et sans réponse au besoin de restriction.

---

## ADR-012 — Assistant vocal : Mistral hébergé en UE, le LLM comprend et le backend décide

**Statut :** Proposé (2026-09-23), à l'issue du spike `docs/spike-assistant-vocal.md`. Non fusionné :
le code vit sur la branche `feat/assistant-vocal`.

### Contexte

Un des deux utilisateurs parle beaucoup à son téléphone. Le spike a éprouvé un assistant vocal qui
répond à une question de stock et prépare une vente (`docs/cadrage-assistant-vocal.md`). Il faut
reconnaître la parole, comprendre une phrase libre et parler, trois choses que l'application ne sait
pas faire seule. Deux contraintes pèsent : l'application est auto-hébergée (ADR-010), et les phrases
dictées contiennent des noms de clients, donc des données personnelles.

### Décision

- **Fournisseur : Mistral (La Plateforme), hébergé en UE**, sous contrat de sous-traitance (DPA).
  Trois modèles : Voxtral Mini Transcribe 2 (transcription), Ministral 14B (compréhension, appel
  d'outils), Voxtral TTS, voix française « Marie » (réponse lue).
- **Seul le backend appelle Mistral.** Le téléphone envoie l'audio au backend (ADR-003) ; la clé
  d'API ne quitte pas le serveur ; les droits sont relus en base (ADR-011).
- **Le LLM comprend, le backend décide.** Le LLM choisit un outil (`get_stock`, `draft_sale`,
  `not_understood`) et en remplit les champs. Le backend choisit les unités, résout le client, calcule
  les chiffres et écrit la phrase dite. Aucun chiffre, aucune unité, aucun client ne vient du LLM.
- **L'assistant n'écrit rien.** Une vente dictée ouvre le formulaire existant pré-rempli ;
  l'utilisateur la relit et l'enregistre. Le montant est calculé par le formulaire, comme à la main.
- **Pseudonymisation locale avant le LLM.** Les noms de clients sont reconnus à l'oreille, en C#, et
  remplacés par des jetons ; un nom inconnu est retiré ; deux clients qui se ressemblent ne sont pas
  départagés au hasard. Le LLM ne voit jamais un nom ni la liste des clients.
- **Compte Mistral en paiement à l'usage** (confirmé le 2026-09-23) : les données sont exclues de
  l'entraînement d'office, à vérifier une fois dans la console. Mistral Small et Medium restaient
  fermés sur le compte pendant le spike (zéro requête par minute) ; Ministral 14B suffit aux seuils.

### Conséquences

**Positives**
- Une réponse de stock sans chiffre inventé et une vente sans mauvais client, par construction plutôt
  que par confiance dans un modèle (0 sur tout le banc du spike).
- Aucune nouvelle dépendance : appels HTTP sans SDK, pas de service Python, pas de GPU.
- Coût de l'ordre du dixième de centime par demande, hors voix.

**Négatives / à surveiller**
- **Nouveau sous-traitant de données personnelles.** L'audio, noms compris, part chez Mistral pour la
  transcription et y est conservé 30 jours (détection d'abus) ; la conservation zéro n'existe que sur
  l'offre Scale. La pseudonymisation ne protège que l'étape de compréhension.
- **Dépendance à un service extérieur** : sans Mistral, l'assistant est indisponible (`503`) ; le
  reste de l'application fonctionne.
- La transcription reste le maillon faible (homophones, noms propres) ; le formulaire pré-rempli
  absorbe ses erreurs, la réponse orale non.
- Les clients ne sont pas informés individuellement (choix assumé, `cadrage-assistant-vocal.md` §5).

### Alternatives écartées

- **Reconnaissance vocale du navigateur (Web Speech API)** : l'audio part chez Google sans contrat, et
  elle est souvent inopérante hors de Chrome.
- **Transcription locale (Whisper sur la Quadro P600)** : tient en mémoire, mais un client sur trois
  mal transcrit et un délai trop long. À rouvrir avec un meilleur modèle local.
- **Voxtral Small (audio directement compris)** : un seul appel, mais le modèle entendrait les noms,
  ce qui annule la pseudonymisation.
- **Service Python (Presidio, spaCy, espeak-ng)** pour la pseudonymisation : jeu égal sur les clients,
  meilleur seulement sur des noms inconnus écrits en minuscules, ce que Voxtral ne fait pas ; un
  conteneur et un langage de plus.
- **Phrase de réponse écrite par le LLM** : il a dit « 1 jambon entier » pour 2 au banc, erreur
  qu'aucun contrôle des chiffres ne voyait.
- **Analyseur à règles sans LLM** : aucune donnée envoyée pour la compréhension, mais à réécrire à
  chaque nouvelle demande, alors que le périmètre est appelé à grandir.
- **Fournisseurs hors UE** (OpenAI, Google Cloud) : transfert hors UE à justifier, pour un gain non
  démontré sur ce besoin.

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