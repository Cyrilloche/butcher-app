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

## Lot 2 — Mise en page PC (US3)

1. Ouvrir l'application dans une fenêtre de plus de 960 px : barre latérale, « Vue d'ensemble »
   conforme à la maquette `Backoffice Overview.dc.html`, carte « Stock bas » marquée
   « Arrivera en V2 ».
2. Réduire la fenêtre sous 960 px : barre en bas d'écran, écrans mobiles identiques à aujourd'hui.
3. Ouvrir « Nouvelle vente » sur PC : formulaire actuel, centré et borné en largeur.

## Lots suivants

US4 (journal) et US5 (rapports) : scénarios d'acceptation de la spec, à compléter ici au démarrage
de chaque lot.
