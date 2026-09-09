<!--
Sync Impact Report
- Version change: (aucune, gabarit non rempli) → 1.0.0
- Ratification initiale : tous les placeholders du gabarit ont été remplacés par des valeurs
  dérivées de CLAUDE.md, docs/PRD.md, docs/ADR.md et docs/data-model.md.
- Principes définis (aucun renommage, aucune suppression) :
  I. Simplicité au service d'utilisateurs non techniques
  II. Le backend est le garant des règles métier
  III. Frontière contractuelle frontend / backend
  IV. Décisions tracées et traçabilité par identifiants
  V. Livraison par vagues, dé-risquage par spikes
- Sections ajoutées : « Contraintes techniques », « Processus de développement et portes de qualité »
- Sections supprimées : aucune
- Templates à mettre à jour :
  ✅ .specify/templates/plan-template.md — mis à jour : la section « Constitution Check » liste
     désormais les cinq portes explicites au lieu d'un placeholder
  ✅ .specify/templates/spec-template.md — vérifié : aucune section rendue obligatoire ou caduque
  ✅ .specify/templates/tasks-template.md — mis à jour : test backend obligatoire pour toute tâche
     touchant une `RG-xx` (principe II), référencement des identifiants (principe IV)
  ✅ CLAUDE.md (source des principes, aucune contradiction introduite)
- TODO différés : aucun
-->

# Constitution du projet Saloir (`butcher-app`)

## Core Principles

### I. Simplicité au service d'utilisateurs non techniques

Les utilisateurs finaux sont deux particuliers non techniques. Le vrai risque du projet est
l'adoption, pas la performance. Toute fonctionnalité DOIT être justifiée par un besoin réel
exprimé dans le PRD, et toute interface DOIT privilégier la lisibilité sur la richesse.
L'interface est intégralement en français : aucune valeur technique anglaise (enum, statut,
code) ne DOIT apparaître à l'écran, la table de correspondance de `docs/data-model.md` §4.2
faisant foi. Les parcours de saisie répétitive, en premier lieu la pesée unité par unité (R-01),
DOIVENT être optimisés avant d'être enrichis.

*Rationale* : une fonctionnalité que la mère et le beau-père du porteur de projet n'utilisent pas
remplace mal le carnet papier qu'elle prétend supprimer.

### II. Le backend est le garant des règles métier

Les règles de gestion (`RG-xx`) DOIVENT être appliquées côté serveur, quelle que soit la
validation faite côté client. Le frontend PEUT dupliquer une validation pour le confort de
saisie ; il ne DOIT jamais en être l'unique gardien. Sont spécifiquement non négociables :
le stock modélisé à l'unité physique (`stock_unit`), y compris pour les produits `by_piece` ;
le `status` de `stock_unit` comme source de vérité de l'état de stock ; le montant de vente
(`amount`) stocké tel que saisi et jamais recalculé ; le client obligatoire sur une vente ; le
refus (`409`) de supprimer un client porteur de ventes. Toute règle métier ajoutée DOIT être
couverte par au moins un test automatisé côté backend.

*Rationale* : la traçabilité lot ↔ client est la valeur métier centrale ; un client permissif ou
hors ligne ne peut pas en être le dépositaire.

### III. Frontière contractuelle frontend / backend

Le couplage entre les deux applications DOIT passer exclusivement par le contrat d'API REST
documenté en OpenAPI (ADR-003). Le backend NE DOIT rien connaître de Vue, et réciproquement.
Le client HTTP du frontend (`frontend/src/api/`) DOIT rester aligné sur le Swagger backend.
Une rupture de contrat DOIT être signalée par un commit `!` (Conventional Commits) et versionner
le composant concerné en conséquence.

*Rationale* : les deux applications ont des cycles de vie, des conteneurs et des tags de release
indépendants ; seule une frontière explicite rend cette indépendance réelle.

### IV. Décisions tracées et traçabilité par identifiants

Toute décision d'architecture structurante DOIT donner lieu à un ADR dans `docs/ADR.md` ; toute
remise en cause d'une décision acceptée DOIT donner lieu à un ADR de remplacement référençant le
précédent. Le code, les messages de commit et les discussions DOIVENT référencer les exigences
par identifiant (`RF-07`, `RG-02`, `ADR-005`). Les documents de `docs/` font foi en cas de doute
et DOIVENT être mis à jour dans le même lot de travail que le changement qu'ils décrivent.
`CHANGELOG.md` est généré et NE DOIT jamais être édité à la main.

*Rationale* : le projet est mené par vagues espacées dans le temps ; sans traçabilité écrite,
chaque reprise repart d'une reconstitution coûteuse et faillible.

### V. Livraison par vagues, dé-risquage par spikes

Le périmètre est découpé en vagues : on livre le noyau, puis on enrichit. La V2 NE DOIT pas être
construite en V1, mais aucune porte NE DOIT être fermée : les points d'extension sont documentés
dans `docs/data-model.md` §8. Un point technique risqué DOIT être validé par un spike isolé
**avant** que les fonctionnalités qui en dépendent soient construites. Une fonctionnalité hors
périmètre de la vague en cours DOIT être reportée explicitement plutôt qu'anticipée dans le code.

*Rationale* : deux développeurs à temps partiel ne peuvent pas absorber simultanément un risque
technique et un risque fonctionnel.

## Contraintes techniques

Ces contraintes sont fixées par les ADR acceptés et NE DOIVENT pas être contournées sans ADR de
remplacement.

- **Pile** : Vue 3 + TypeScript en PWA avec Vuetify (ADR-006) ; ASP.NET Core Web API en C# ;
  EF Core + Npgsql ; PostgreSQL ; déploiement Docker Compose auto-hébergé derrière Caddy
  (ADR-010). Pas de BaaS.
- **Pas de mode hors-ligne** en V1 (ADR-001). La PWA préserve la possibilité de l'ajouter.
- **Authentification** : Identity allégé sans rôles, access token JWT en mémoire (15 min),
  refresh token rotatif en base, cookie httpOnly/Secure (ADR-009). Le secret de seed vient d'une
  variable d'environnement, jamais du dépôt.
- **Langue** : code et schéma en anglais, documentation et interface en français.
- **Nommage** : `snake_case` en base, `PascalCase` en C#, conventions standards en Vue/TS. La
  table utilisateur s'appelle `app_user` et jamais `user`. Les enums sont sérialisés en
  `snake_case`, jamais en `PascalCase`.
- **Types** : argent en `decimal(10,2)` et jamais en flottant ; poids en `decimal(10,3)` ;
  horodatage en `timestamptz` ; clés métier en `integer` auto-incrémenté, `uuid` pour `app_user`.
- **Design** : le système « Kraft » (`design/style-guide.html`, `frontend/src/plugins/vuetify.ts`)
  fixe couleurs, typographie et mapping statut ↔ couleur sémantique. Thème clair uniquement pour
  l'instant.

## Processus de développement et portes de qualité

- **Commits** : format [Conventional Commits](https://www.conventionalcommits.org/fr/)
  (`feat(frontend):`, `fix(backend):`, `!` pour une rupture). Le changelog en dérive : écrire le
  message de commit, c'est écrire le changelog.
- **Worktrees** : backend et frontend sont développés dans des worktrees Git séparés du même
  dépôt. Quand plusieurs sessions partagent un arbre de travail, les commits DOIVENT lister des
  chemins explicites plutôt que `git add -A`.
- **Tests** : la suite backend DOIT passer avant toute release. Toute règle de gestion nouvelle ou
  modifiée DOIT arriver avec son test.
- **Releases** : tags préfixés `backend-v*` / `frontend-v*`, images Docker publiées par version et
  par SHA (jamais `latest`), déploiement VPS déclenché par le tag. Un tag posé DOIT être poussé
  dans la foulée.
- **Configuration de développement** : elle passe par `development/.env` et le `Makefile`, pas par
  `dotnet user-secrets`. Aucun secret NE DOIT être commité.

## Governance

Cette constitution prime sur les pratiques ad hoc. En cas de conflit entre elle et une habitude de
code, c'est la constitution qui s'applique, ou elle est amendée.

- **Amendement** : toute modification DOIT être portée par un changement de ce fichier, accompagnée
  de la mise à jour des documents dépendants (`CLAUDE.md`, `docs/ADR.md`, gabarits `.specify/`) et
  d'un rapport de synchronisation en tête de fichier.
- **Versionnage** : sémantique. MAJEUR pour la suppression ou la redéfinition incompatible d'un
  principe ; MINEUR pour l'ajout d'un principe ou d'une section, ou un élargissement matériel des
  règles ; CORRECTIF pour les clarifications et corrections de forme.
- **Conformité** : toute revue de plan ou de code DOIT vérifier l'alignement avec les cinq
  principes. Un écart DOIT être justifié explicitement dans la section de suivi de complexité du
  plan, ou corrigé.
- **Guidance d'exécution** : `CLAUDE.md` reste le point d'entrée opérationnel de chaque session et
  DOIT rester cohérent avec ce document.

**Version**: 1.0.0 | **Ratified**: 2026-09-03 | **Last Amended**: 2026-09-09
