# Research — Backoffice PC, comptes nominatifs et rôles

**Feature**: `specs/005-backoffice` | **Date**: 2026-09-12

Chaque décision suit le format Décision / Justification / Alternatives écartées.

---

## R-01 — Porter le rôle : colonne sur `app_user`, pas les tables de rôles Identity

**Décision** : `app_user` reçoit une colonne `role` (enum `account_role` : `admin` | `user`, sérialisé
en `snake_case`), une colonne `display_name` et une colonne `is_active`. `AppDbContext` reste un
`IdentityUserContext<AppUser, Guid>`, sans `IdentityRole`.

**Justification** : la spec fixe exactement deux rôles, sans droits à la carte (FR-002). Un compte
n'a qu'un rôle à la fois. Une colonne exprime cela directement, se lit dans une requête sans
jointure et se contraint en base. Les tables `AspNetRoles` / `AspNetUserRoles` apporteraient une
relation plusieurs-à-plusieurs, un `RoleManager` et un seed de rôles pour un besoin qui n'en a pas.

**Alternatives écartées** :
- `IdentityDbContext` complet avec rôles : trois tables et un gestionnaire de plus, une
  relation N-N inutile, et un écart avec ADR-009 (« Identity allégé ») plus large que nécessaire.
- Claims Identity (`app_user_claim`) : le rôle deviendrait une paire clé/valeur libre, sans
  contrainte en base, facile à dupliquer ou à mal orthographier.

## R-02 — Vérifier le rôle en base à chaque action réservée

**Décision** : une politique d'autorisation `AdminOnly`, servie par un `AuthorizationHandler` qui
relit le compte en base (actif **et** administrateur) à partir du `sub` du jeton. Les contrôleurs
posent `[Authorize(Policy = AuthorizationPolicies.AdminOnly)]` sur les actions réservées. Le rôle
figure aussi dans le jeton, mais seulement comme indication pour l'interface.

**Justification** : la spec exige qu'une rétrogradation ou une désactivation prenne effet
immédiatement sur les actions réservées (cas limite « rôle changé en cours de session »), alors que
le jeton d'accès vit 15 minutes. Une lecture par clé primaire sur une table de trois lignes ne coûte
rien. Le jeton reste la source de l'identité, la base la source des droits.

**Alternatives écartées** :
- `[Authorize(Roles = "admin")]` sur le seul claim du jeton : un administrateur rétrogradé
  garderait ses droits jusqu'à 15 minutes.
- Révoquer les jetons d'accès : impossible sans liste noire, les JWT étant sans état (ADR-009).

## R-03 — Refuser un compte désactivé

**Décision** : `AuthService.LoginAsync` et `RefreshAsync` refusent un compte `is_active = false`.
La désactivation révoque les refresh tokens du compte. Toute requête authentifiée vérifie aussi
l'état actif, via la même lecture que R-02 appliquée à la politique par défaut.

**Justification** : sans cela, un compte désactivé garderait l'accès 15 minutes par son jeton
d'accès, et 30 jours par son refresh token. La vérification sur chaque requête ferme la première
fenêtre ; la révocation ferme la seconde.

**Alternatives écartées** : se reposer sur `LockoutEnd` d'Identity (verrou à durée, conçu pour
l'anti-brute-force et déjà utilisé comme tel : le détourner mêlerait deux sens).

## R-04 — Renseigner l'auteur dans `AppDbContext.SaveChanges`

**Décision** : un service `ICurrentAccount` (implémenté sur `IHttpContextAccessor`) expose l'id du
compte connecté. `AppDbContext` le reçoit en dépendance facultative et, dans `SaveChanges`, pose
`CreatedById` sur toute entité ajoutée qui porte cette propriété et ne l'a pas déjà.

**Justification** : le projet a déjà fait ce choix pour `created_at` / `updated_at`
(`StampAuditDates`), précisément parce qu'aucun service ne le faisait. Poser l'auteur au même endroit
garantit SC-001 (100 % des saisies avec auteur) sans toucher chaque service, et les futures entités
en bénéficieront. La dépendance facultative laisse fonctionner les tests et les commandes hors
ligne (`create-user`, migrations), où il n'y a pas de requête HTTP.

**Alternatives écartées** :
- Passer l'utilisateur à chaque méthode de service : une vingtaine de signatures modifiées, et un
  oubli possible à chaque nouvelle méthode.
- Intercepteur EF (`SaveChangesInterceptor`) : équivalent fonctionnel, mais crée une seconde façon
  de faire à côté de `StampAuditDates`. À reconsidérer si le journal (US4) grossit au point de
  mériter son propre intercepteur.

## R-05 — Politique de mot de passe par rôle

**Décision** : `IdentityPolicy` fixe le socle commun (20 caractères, composition, 10 caractères
distincts). Un `IPasswordValidator<AppUser>` supplémentaire, `AdminPasswordValidator`, exige 32
caractères et 12 caractères distincts lorsque le compte est administrateur. La promotion d'un compte
passe par un geste qui prend le nouveau mot de passe et le valide **avec le rôle cible** (FR-035).

**Justification** : `PasswordOptions` d'Identity est global et ne connaît pas l'utilisateur. Un
validateur reçoit l'utilisateur et peut donc lire son rôle. Le socle reste déclaratif et partagé
avec les tests, comme décidé au durcissement du 2026-09-12.

**Alternatives écartées** : deux configurations Identity distinctes (impossible, une seule par
type d'utilisateur) ; valider la longueur dans le contrôleur (contournable par `set-password`).

## R-06 — L'interface connaît le compte par `GET /api/auth/me`

**Décision** : un endpoint `GET /api/auth/me` renvoie `id`, `email`, `displayName`, `role`. Le store
d'authentification l'appelle à l'ouverture de la session — connexion, ou rétablissement après un F5
— et expose `account` et `isAdmin`. *Précisé à l'implémentation* : pas à chaque rafraîchissement de
jeton, car `me()` passe par `apiFetch`, qui rafraîchit lui-même sur un `401` ; le rappeler à ce
moment ouvrirait une boucle. Un changement de rôle se voit donc à l'écran au chargement suivant, ce
qui est sans risque puisque le serveur relit les droits à chaque requête (R-02).

**Justification** : le jeton est opaque pour le client dans ce projet (aucun décodage JWT côté
frontend), et le nom affiché n'y figure pas. Un endpoint dédié garde le contrat explicite et
documenté (principe III), et renvoie l'état à jour, pas celui d'il y a 15 minutes.

**Alternatives écartées** : décoder le JWT côté client (couple le frontend au format du jeton, et
le nom affiché grossirait le jeton) ; renvoyer le compte dans la réponse de `login` seulement (perdu
au rafraîchissement silencieux après un F5).

## R-07 — Le compte partagé existant devient administrateur

**Décision** : la migration pose `role = 'admin'`, `is_active = true` et `display_name` égal à la
partie locale de l'email sur tous les comptes existants. Les nouveaux comptes sont `user` par défaut.

**Justification** : FR-010, sans interruption d'accès. En pratique un seul compte existe en prod.

**Alternatives écartées** : un script de reprise manuel (risque d'oubli au déploiement).

## R-08 — Mise en page PC : un seul gabarit, deux présentations

**Décision** : `AppLayout.vue` choisit la présentation avec `useDisplay()` de Vuetify : barre
latérale permanente (`v-navigation-drawer`) à partir du point de rupture `md` (840 px dans Vuetify 4), barre en bas
d'écran en dessous. Les vues gardent leurs routes ; les formulaires sont bornés en largeur par un
conteneur commun. La maquette `Backoffice Overview.dc.html` (projet Claude Design
`5d1f2fde-8c50-45fc-8970-925e8c9df3b2`) fait référence : barre latérale de 248 px, cartes chiffrées,
graphique mensuel, dernière vente, ventes récentes. Les textes d'exemple sont remplacés (« Saloir »,
« Vue d'ensemble »), et la carte « Stock bas » affiche « Arrivera en V2 ».

**Justification** : FR-015 / FR-016 — même application, aucun parcours mobile ne régresse. Le point
de rupture `md` est la frontière naturelle entre tablette portrait et écran de travail.

**Alternatives écartées** : un second frontend (écarté par la spec, Q3 du 2026-09-12) ; des media
queries CSS seules (la navigation change de composant, pas seulement de style).

## R-09 — Journal : table append-only écrite par le même `SaveChanges`

**Décision** (US4, lot ultérieur) : une table `audit_entry` alimentée dans `AppDbContext.SaveChanges`
à partir du `ChangeTracker`, dans la même transaction que la modification. Le contenu JSON (`jsonb`)
n'est rempli que pour les suppressions (clarification Q3 : option B).

**Justification** : écrire le journal dans la même transaction garantit qu'aucune opération ne lui
échappe et qu'aucune entrée n'existe pour une opération annulée.

**Alternatives écartées** : journaliser dans chaque service (oublis), triggers PostgreSQL (logique
métier hors du backend, contraire au principe II, et sans connaissance de l'auteur).
