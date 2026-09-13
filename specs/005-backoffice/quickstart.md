# Quickstart — valider le backoffice PC

**Feature**: `specs/005-backoffice` | **Date**: 2026-09-12

Guide de validation manuelle, en complément des tests backend. Détail des routes :
[contracts/api.md](./contracts/api.md) ; schéma : [data-model.md](./data-model.md).

## Prérequis

```bash
make db-up           # Postgres de dev
make test            # la suite backend doit passer
make run             # API sur http://localhost:5045 (applique les migrations)
cd frontend && npm run dev
```

Le compte seedé existant devient administrateur à la migration.

## Lot 1 — Comptes et rôles (US1, US2)

1. **Le compte existant est administrateur.** Se connecter avec le compte seedé ;
   `GET /api/auth/me` renvoie `"role": "admin"`. L'entrée « Comptes » est visible.
2. **Créer un utilisateur.** Écran Comptes → créer « Mireille », rôle Utilisateur, mot de passe de
   20 caractères. Un mot de passe de 19 caractères est refusé avec la règle en français.
3. **Se connecter en utilisateur.** Dans une fenêtre privée, se connecter avec Mireille.
   L'entrée « Comptes » est absente.
4. **L'auteur est enregistré.** En tant que Mireille, enregistrer une vente. Son détail affiche
   « Saisie par Mireille », pour Mireille comme pour l'administrateur. Une vente antérieure affiche
   « Compte partagé (avant comptes nominatifs) ».
5. **Geste réservé masqué et refusé.** En tant que Mireille, le détail d'un produit ne propose ni
   désactivation ni solde en perte. Un `POST /api/products/{id}/deactivate` avec le jeton de
   Mireille renvoie `403` et ne change rien.
6. **Correction toujours permise.** En tant que Mireille, supprimer une vente : le geste aboutit.
7. **Désactivation.** En administrateur, désactiver Mireille. Sa session cesse de fonctionner à la
   requête suivante ; une reconnexion est refusée avec « Ce compte est désactivé. ».
8. **Dernier administrateur.** Tenter de désactiver ou de rétrograder le seul administrateur :
   refus `409`.
9. **Promotion.** Promouvoir Mireille sans nouveau mot de passe : refus. Avec un mot de passe de
   32 caractères : accepté.

### Résultat — 2026-09-13 (API locale, comptes jetables)

Déroulé au niveau de l'API sur `feat/backoffice`, avec un administrateur et un utilisateur créés
puis supprimés pour l'occasion. L'écran lui-même n'a pas été éprouvé dans un navigateur.

| Étape | Attendu | Constaté |
|---|---|---|
| 1 | `me` renvoie le rôle administrateur | ✅ `"role":"admin"` |
| 2 | Mot de passe utilisateur de 19 caractères refusé en français ; 20 et plus acceptés | ✅ `400` « Le mot de passe doit compter au moins 20 caractères. » ; `201` |
| 3 | L'utilisateur se connecte ; les comptes lui sont fermés | ✅ `me` `"role":"user"` ; `GET /api/accounts` `403` |
| 4 | L'auteur d'une vente est enregistré et affiché | ⚠️ Non rejoué ici (il aurait fallu créer produit, lot et unités dans la base de dev) ; couvert par `CreatedByNameProjectionTests` |
| 5 | Geste réservé refusé à l'utilisateur | ✅ `403` sur `POST /api/products/{id}/deactivate` |
| 6 | Les écrans métier restent ouverts à l'utilisateur | ✅ `GET /api/sales` `200` |
| 7 | Désactivation effective à la requête suivante, reconnexion refusée | ✅ `204` ; puis `401` « Ce compte est désactivé. » sur la requête suivante et à la reconnexion |
| 8 | Le dernier administrateur actif est protégé | ⚠️ Non démontrable ici : l'administrateur de la base de dev restait actif. Couvert par `AccountServiceTests` (`409`) |
| 9 | Promotion refusée sans nouveau mot de passe, acceptée avec 32 caractères | ✅ `400` puis `200` ; le compte promu administre à son tour |

## Lot 2 — Mise en page PC (US3)

1. Ouvrir l'application dans une fenêtre de plus de 840 px : barre latérale, « Vue d'ensemble »
   conforme à la maquette `Backoffice Overview.dc.html`, carte « Stock bas » marquée
   « Arrivera en V2 ».
2. Réduire la fenêtre sous 840 px : barre en bas d'écran, écrans mobiles identiques à aujourd'hui.
3. Ouvrir « Nouvelle vente » sur PC : formulaire actuel, centré et borné en largeur.

## Lot 3 — Journal (US4)

1. **Une ligne par geste.** En utilisateur, créer une fournée de 3 unités, puis une vente de
   2 lignes. En administrateur, `GET /api/audit-entries` : « Fournée », « Unités ajoutées (3) »,
   « Vente (2 lignes) », sans « Modification » d'unité.
2. **Modification.** Corriger le paiement de la vente : une entrée « Modification », auteur et heure.
3. **Suppression.** Supprimer la vente : l'entrée porte son contenu (client, date, lignes, montants).
4. **Connexions.** Un mauvais mot de passe, puis une adresse inconnue : deux « Connexion refusée »,
   la seconde avec l'adresse tapée et sans auteur.
5. **Filtres.** Filtrer par auteur, par type « Vente », par période : seules les entrées
   correspondantes restent.
6. **Réservé.** Avec le jeton de l'utilisateur : `403`.

## Lot 4 — Rapports (US5)

1. **Synthèse.** Sur l'année, nombre de ventes, total, encaissé et à encaisser égaux à la somme des
   ventes affichées dans Ventes filtrées sur la même période.
2. **Par client et par produit.** Classés du plus gros total au plus petit ; un jambon vendu en
   tranches compte 1 unité et autant de lignes que de tranches.
3. **À encaisser.** Chaque client débiteur, son montant dû, sa plus ancienne vente impayée ; chaque
   vente s'ouvre.
4. **Période vide.** Totaux à zéro, sans erreur.
5. **Réservé.** Avec le jeton de l'utilisateur : `403`.
