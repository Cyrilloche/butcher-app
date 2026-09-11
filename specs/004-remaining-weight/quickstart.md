# Phase 1 — Guide de validation

**Feature**: Poids encore vendable d'une unité entamée
**Date**: 2026-09-11

Comment vérifier que la fonctionnalité tient ses promesses. Les exigences sont dans
[spec.md](./spec.md), le contrat dans [contracts/api.md](./contracts/api.md), les décisions dans
[research.md](./research.md).

Contrairement aux trois fonctionnalités précédentes, **la règle métier est ici côté serveur** :
elle arrive donc avec ses tests automatisés (principe II de la constitution). Seule la mise en
forme se valide à la main.

---

## Prérequis

La configuration de développement passe par `development/.env` et le `Makefile`, jamais par
`dotnet user-secrets`.

```bash
make db-up     # PostgreSQL + pgAdmin
make run       # API sur http://localhost:5045
```

Frontend, dans un second terminal :

```bash
cd frontend && npm run dev
```

Jeu d'essai à préparer depuis l'application, une fois connecté :

1. Un produit au poids autorisant la vente à la tranche, appelons-le **Jambon sec**, code `JS`.
2. Deux fournées à des dates différentes, avec des prix différents, pour voir deux titres de
   section. Trois unités pesées en tout : 3,000 kg, 3,000 kg et 2,800 kg.
3. Un produit au poids ordinaire, des saucisses, avec quelques sachets pesés.
4. Un produit vendu à la pièce, avec au moins une unité.
5. Un client, pour pouvoir enregistrer des ventes.

---

## Contrôle automatisé — la règle de calcul

```bash
make test
```

**Attendu** : la suite passe, avec les nouveaux cas du poids encore vendable. Ils couvrent l'unité
intacte, l'unité entamée, l'unité entièrement vendue, l'unité d'un produit à la pièce, l'unité pas
encore pesée et le plancher à zéro. Un cas vérifie qu'une sortie perso ou une perte **n'est pas**
retranchée du restant.

Si ces tests ne sont pas là, la fonctionnalité n'est pas livrable : RG-05 est touchée, la
constitution exige sa couverture côté serveur.

---

## Scénario 1 — Le restant d'un jambon entamé (US1, FR-001 à FR-008)

1. Vendre une tranche de 1,200 kg sur le jambon `JS-...-1`, puis une autre de 1,000 kg.
2. Ouvrir le détail du produit Jambon sec.
3. **Attendu** : la ligne de ce jambon annonce **800 g restants** à côté de son étiquette
   « Entamé », et rappelle en gris « pesé 3,000 kg » en haut à droite.
4. Regarder la ligne d'un jambon intact.
5. **Attendu** : son poids pesé s'affiche en haut à droite, et la ligne du bas ne porte que
   l'étiquette « Disponible ». Aucun restant n'est répété.
6. Vérifier le libellé du restant.
7. **Attendu** : aucun mot du type « environ » ou « estimé ». Le jambon est désossé et prêt à
   trancher, le chiffre est exact.

---

## Scénario 2 — Le jambon fini mais non clôturé (Edge Case)

1. Sur le jambon entamé ci-dessus, vendre une dernière tranche de 800 g.
2. Ouvrir le détail du produit.
3. **Attendu** : la ligne annonce **0 g restants** accompagné de la mention « à clôturer ». Le
   jambon est toujours là, il compte toujours pour une unité.
4. Appuyer sur la mention « à clôturer ».
5. **Attendu** : rien ne se passe. Elle est décorative dans cette vague (D9). La clôture se fait
   par le menu à trois points.
6. Tenter de vendre une tranche de plus sur ce jambon.
7. **Attendu** : refus du serveur en français nommant les deux poids. Le garde-fou est inchangé.

---

## Scénario 3 — Les totaux (US2, US3, FR-009 à FR-011)

1. Noter le nombre d'unités et le poids affichés en tête du détail du produit.
2. **Attendu** : le poids est la somme des restants, pas des poids pesés. Avec deux jambons de
   3,000 kg dont un entamé à 800 g, et un de 2,800 kg, le total est **6,600 kg** et non 8,800 kg.
3. **Attendu** : le décompte d'unités est inchangé. Un jambon entamé compte pour un, même à 0 g.
4. Revenir à la liste de stock.
5. **Attendu** : la ligne du produit annonce exactement le même poids que le détail. C'est le
   critère SC-003.
6. Vendre une tranche de 300 g, revenir sur les deux écrans.
7. **Attendu** : les deux poids ont baissé de 300 g, ensemble.

---

## Scénario 4 — La mise en forme validée (Présentation retenue)

1. Ouvrir le détail d'un produit ayant deux fournées, sur un écran étroit de téléphone.
2. **Attendu** : chaque date s'affiche en gros au-dessus de sa carte, avec le prix de la fournée et
   sa corbeille alignés à droite sur la même ligne. La carte ne contient que les unités.
3. **Attendu** : chaque unité tient sur deux lignes. Rien ne passe à la ligne, rien n'est tronqué,
   le numéro d'étiquette est le plus gros caractère de sa ligne.
4. Appuyer sur la corbeille d'une unité **disponible et sans vente**.
5. **Attendu** : une confirmation nommant son numéro d'étiquette, puis l'unité disparaît. Le total
   et le décompte se mettent à jour.
6. Regarder la corbeille d'une unité entamée.
7. **Attendu** : elle est éteinte. Le serveur refuserait, la corbeille le dit avant.
8. Ouvrir le menu à trois points.
9. **Attendu** : « Déclarer une perte » ne porte plus une corbeille. Deux corbeilles voisines ne
   peuvent pas vouloir dire deux choses (D7).

---

## Scénario 5 — Ce qui ne doit pas bouger

1. Ouvrir le détail d'un produit vendu à la pièce.
2. **Attendu** : aucune notion de poids nulle part, ni restant, ni total. Sa ligne dans la liste de
   stock n'affiche aucun poids non plus (FR-014).
3. Créer une fournée d'un produit au poids et y ajouter des unités **sans les peser**.
4. **Attendu** : aucun restant affiché sur ces unités, et elles contribuent zéro au total.
5. Ouvrir l'écran de saisie d'une vente et sélectionner un jambon entamé pour une tranche.
6. **Attendu** : le poids restant proposé est le bon, et l'écran se comporte comme avant. Le calcul
   vient désormais du serveur (D8), le geste ne change pas.
7. Retirer, depuis le détail d'une vente, une ligne portant sur un jambon entamé.
8. **Attendu** : le restant de ce jambon **remonte** d'autant sur l'écran de stock, sans aucun
   geste supplémentaire. C'est la conséquence directe d'un calcul à la demande.

---

## Contrôles avant de considérer la fonctionnalité livrée

```bash
make test
cd frontend && npm run type-check && npm run lint
```

- [ ] Les nouveaux tests backend du poids encore vendable passent.
- [ ] Aucun mot anglais ni valeur technique (`opened`, `available`, `sale`) visible à l'écran.
- [ ] Aucun statut HTTP ni message technique affiché à l'utilisateur.
- [ ] Le poids affiché par la liste de stock et par le détail sont identiques pour chaque produit.
- [ ] Aucune migration n'a été créée : `git status` ne montre aucun fichier sous `Migrations/`.
- [ ] RG-05 est révisée dans `docs/PRD.md`, `docs/data-model.md` et `CLAUDE.md`, dans le même lot
      de travail que le code.
- [ ] `getRemainingWeightKg` n'existe plus dans le frontend.
