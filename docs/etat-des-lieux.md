# État des lieux — écart entre le prévu et le réalisé

| | |
|---|---|
| **Projet** | Mini-ERP Charcuterie (`butcher-app`) — application « Saloir » |
| **Document** | Analyse d'écart documentation ↔ implémentation |
| **Version** | 2.0 |
| **Date** | 12 septembre 2026 |
| **Méthode** | Relecture de `PRD.md`, `ADR.md`, `data-model.md`, `CLAUDE.md`, confrontée au code réellement présent (contrôleurs, services, vues, workflows, `Caddyfile`) et à la réponse de la prod |
| **Remplace** | Version 1.0 du 4 septembre 2026 |

> Ce document est une **photographie datée**, pas une référence permanente : il constate, il ne décide pas. Les décisions qu'il appelle doivent redescendre dans le PRD, les ADR ou `CLAUDE.md`.

---

## 1. Verdict en trois lignes

Le **périmètre fonctionnel de la Vague 1 est livré**, backend comme frontend : l'écart « API disponible, interface absente » qui dominait la version 1.0 est résorbé pour tout ce qui compte à l'usage. Le **durcissement de l'authentification** relevé par l'audit du 5 septembre est fait dans le code. Ce qui sépare encore l'outil d'un usage réel n'est plus du fonctionnel : c'est de l'**exploitation** — pas de sauvegarde, et un `Caddyfile` que la CI ne dépose pas sur le VPS.

---

## 2. Ce qui était prévu et qui est bien là

| Prévu | Réalisé | Preuve dans le code |
|---|---|---|
| Entités métier, dont `sale` et le registre `unit_number_sequence` | ✅ | `backend/src/Butcher.Api/Domain/Entities/` |
| API REST couvrant le cœur métier | ✅ 7 contrôleurs | `backend/src/Butcher.Api/Controllers/` |
| Tests backend | ✅ **145 tests** (84 au 04/09) | `backend/tests/Butcher.Api.Tests/` |
| Produits : création, correction, gel du code et du mode de vente au premier lot, désactivation avec solde en perte (RG-09, RG-16) | ✅ back **et** front | `ProductDetailView.vue`, `ProductWriteOffDialog.vue` |
| Lots : création avec pesée, **correction du prix** (RG-10), suppression d'un lot intact | ✅ back **et** front | `StockAddView.vue`, `BatchPriceEditAction.vue`, `BatchDeleteAction.vue` |
| Numéro d'étiquette porté par l'unité (RG-17) | ✅ | `unit_number_sequence`, `StockUnitRow.vue` |
| Stock à l'unité, poids restant d'une unité entamée calculé à la lecture (RG-05 révisée) | ✅ | `remaining_weight`, `StockDetailView.vue` |
| Vente à la tranche, clôture, sorties perso et perte (RF-19 à RF-21) | ✅ | `StockUnitOutcomeMenu.vue` |
| Vente comme entité, correction et suppression (RF-28 à RF-30, RG-14) | ✅ | `SaleDetailView.vue`, `SaleLineEditDialog.vue`, `SaleDeleteAction.vue` |
| Client obligatoire, suppression refusée dès qu'il a une vente (RF-17, RF-24) | ✅ | `Sale.CustomerId` non nul, `409` |
| Authentification JWT + refresh rotatif en cookie (ADR-009) | ✅ | `AuthController`, `stores/auth.ts` |
| Protection de la connexion contre l'essai en rafale | ✅ **nouveau** | `IdentityPolicy`, `RateLimitPolicies` — voir §4 |
| Déploiement conteneurisé, release par tag, changelog généré (ADR-010) | ✅ | `docker-compose.prod.yml`, `.github/workflows/`, `cliff.toml` |

---

## 3. Les écarts restants

### 3.1 Écarts fonctionnels

| # | Exigence | État | Gravité |
|---|---|---|---|
| E-01 | RF-21 — sorties perso et perte | ✅ Clos le 2026-09-04 | — |
| E-02 | RF-08 / RF-09 — référence matière première et DLC | ⛔ Clos par décision le 2026-09-11 : reportées en V2, le modèle et l'API portent déjà les champs | — |
| E-03 | RG-10 — correction d'un lot | ✅ **Clos le 2026-09-12** : le prix se corrige depuis l'en-tête de la fournée dans Détail Stock. Les champs non exposés (DLC, matière première, notes) repartent tels qu'ils ont été lus, le `PUT` remplaçant le lot en entier. Les ventes déjà faites gardent leur montant. | — |
| E-04 | RG-14 — vente modifiable et supprimable | ✅ Clos le 2026-09-11 | — |
| E-05 | RG-11 — mouvement modifiable et supprimable | ⚠️ Clos pour les lignes de vente ; **une sortie perso ou perte reste non corrigeable** depuis l'interface | Faible : rare, et contournable par une seconde sortie |
| E-06 | RF-31 — ventes filtrables par client, paiement et période | ⚠️ Partiel : l'API et le client HTTP acceptent les filtres, `SalesView` ne les expose pas (recherche texte et regroupement par mois seulement) | Faible ; candidat naturel pour le backoffice PC |

### 3.2 Exploitation et sécurité

| # | Constat | Analyse |
|---|---|---|
| X-01 | **Aucune sauvegarde PostgreSQL** | Le point le plus urgent : les vraies données arrivent avec la recette utilisateur. Les migrations s'appliquent au démarrage du backend, sans filet. Traitement en cours hors dépôt (workflow n8n). |
| X-02 | **Le `Caddyfile` n'est pas déployé par la CI** | Les workflows `release-*` copient `docker-compose.prod.yml` sur le VPS, jamais le `Caddyfile`, monté depuis `/opt/butcher-app`. Les en-têtes de sécurité (§4) n'y seront qu'après une copie manuelle suivie d'un redémarrage de Caddy. |
| X-03 | **Mot de passe du compte de prod** | La nouvelle politique ne s'applique qu'à l'écriture d'un mot de passe : le compte existant garde l'ancien tant que `set-password` n'est pas lancé sur le VPS. |
| X-04 | **HTTP ne redirige pas vers HTTPS** (audit du 05/09) | Réglage Cloudflare « Always Use HTTPS », hors dépôt. HSTS (§4) couvre les visites suivantes une fois le `Caddyfile` déployé. |

### 3.3 Dette connue

| # | Constat | Analyse |
|---|---|---|
| E-07 | RF-27 — `created_by` jamais renseigné | Inchangé. `created_at` / `updated_at` sont renseignés depuis le 2026-09-12. Le backoffice multi-comptes rendra ce champ nécessaire. |
| E-08 | **Aucun test frontend** (la CI passe `--passWithNoTests`) face à 145 tests backend | L'asymétrie s'est creusée : la logique d'affichage métier a grossi côté frontend (`useStock`, totaux de poids, libellés de fournée). |
| E-09 | Bascule liste / grille des clients | Abandon volontaire, peu de valeur pour deux utilisateurs. |
| E-10 | `DELETE /api/customers/{id}` sans usage frontend | Volontaire : la suppression casserait la traçabilité lot ↔ client. |
| E-11 | `frontend/index.html` porte encore `<title>Vite App</title>` | Cosmétique, relevé par l'audit du 05/09 ; le manifest PWA, lui, dit bien « Saloir ». |

---

## 4. Durcissement de l'authentification (2026-09-12)

Réponse aux deux points « code » de l'audit du 5 septembre, et à la politique de mot de passe laissée aux valeurs par défaut d'Identity.

| Mesure | Réglage | Où |
|---|---|---|
| Verrouillage du compte | 5 mots de passe erronés → 15 minutes. Vérifié **avant** le mot de passe : un compte verrouillé ne dit pas si l'essai était le bon. Une connexion réussie remet le compteur à zéro. | `IdentityPolicy`, `AuthService.LoginAsync` |
| Limitation de débit | 10 tentatives de connexion par minute et par adresse IP, lue dans `CF-Connecting-IP` derrière le tunnel | `RateLimitPolicies`, `[EnableRateLimiting]` sur `login` |
| Réponse | `429` avec un message en français, affiché tel quel par l'écran de connexion | `ExceptionHandlingMiddleware`, `LoginView.vue` |
| Politique de mot de passe | 32 caractères minimum, majuscule, minuscule, chiffre, caractère spécial, 12 caractères distincts — une phrase de passe du type `Finlike-Scorer4-Wildfire-Grazing-Unbiased-Sessions` | `IdentityPolicy` |
| Rotation d'un mot de passe | `set-password <email> <mot-de-passe>`, hors ligne dans le conteneur ; révoque les sessions ouvertes et lève un verrouillage | `Program.cs` |
| En-têtes HTTP | HSTS, `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, CSP stricte (aucun script inline, polices Google seules origines externes), `Server` retiré | `Caddyfile` — **non déployé**, voir X-02 |

**Vérification.** Verrouillage et politique couverts par des tests. Limitation de débit éprouvée sur une API locale : dix `401`, puis `429` à la onzième tentative depuis la même IP, une autre IP non freinée. En-têtes constatés sur un Caddy local, côté frontend comme côté API. `set-password` joué sur la base de dev : mot de passe court refusé, nouveau mot de passe accepté à la connexion, ancien refusé.

**Compromis assumé.** Sur un compte partagé, le verrouillage offre une prise à qui voudrait bloquer l'accès : cinq essais suffisent à fermer la porte quinze minutes. La durée courte limite ce risque, et la limitation par IP le freine.

---

## 5. Ce qu'il reste avant un usage réel

Dans l'ordre de valeur décroissante :

1. **X-01 — sauvegarde PostgreSQL**, avant que les vraies données n'arrivent.
2. **X-02 — déposer le `Caddyfile` sur le VPS**, puis redémarrer Caddy ; idéalement, le faire copier par la CI comme `docker-compose.prod.yml`.
3. **X-03 — `set-password` sur le compte de prod** avec une phrase de passe, une fois le backend publié.
4. **X-04 — « Always Use HTTPS »** côté Cloudflare.

Ensuite, hors usage réel : E-08 (premiers tests frontend), puis le backoffice PC, qui absorbera E-06 et E-07.

---

## 6. Ce que cette analyse ne remet pas en cause

Aucune décision d'architecture n'est contredite. Le durcissement complète ADR-009 sans le remplacer : compte partagé, JWT en mémoire et refresh rotatif en cookie restent la règle. Le multi-comptes annoncé par le backoffice appellera, lui, un ADR de remplacement sur l'absence de rôles.

---

## 7. Note d'implémentation — le poids d'une sortie perso/perte

Le backend impose un `sold_weight` strictement positif sur **tout** mouvement d'un produit vendu au poids, y compris `personal` et `loss`. Le poids enregistré est le **restant** de l'unité : poids pesé moins la somme des poids déjà vendus. Le prendre pour le poids pesé compterait deux fois la part vendue d'un jambon entamé.

Depuis le 2026-09-12, ce restant est calculé par le serveur seul (`ComputeRemainingWeight`) et exposé sous `remaining_weight` ; le frontend ne fait plus aucune soustraction (`CLAUDE.md` §9).

Cas limite : une unité entamée dont tout le poids a déjà été vendu a un restant nul. Le backend refuserait un `sold_weight` à zéro ; l'interface oriente alors vers la clôture (RF-20), qui est le geste correct.
