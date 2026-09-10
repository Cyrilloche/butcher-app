# Phase 0 — Décisions techniques

**Feature**: Le numéro d'étiquette porté par l'unité
**Date**: 2026-09-10

Le contexte technique est entièrement connu : même pile, même base, même contrat que le reste du
projet. Il n'y a donc aucune inconnue à lever, mais huit décisions à prendre et à figer avant de
concevoir.

---

## D1 — Où vit le numéro d'une unité

**Décision** : une colonne `unit_number` sur `stock_unit`, non nulle, unique, écrite une fois et
jamais modifiée.

**Rationale** : la non-réémission (FR-004) et l'immuabilité (FR-006) exigent une valeur persistée.
Un numéro recalculé à la lecture, à partir du rang de l'unité dans sa fournée, est exactement le
mécanisme qu'on supprime : il change dès qu'une unité voisine disparaît, alors que l'étiquette
physique, elle, ne change pas.

**Alternatives écartées** : calculer le numéro à l'affichage, rejeté pour la raison ci-dessus ;
stocker seulement le rang entier et recomposer la chaîne à la lecture, rejeté parce que la chaîne
dépendrait alors du code produit courant, et qu'un numéro déjà écrit ne doit dépendre de rien.

---

## D2 — Le sort du numéro de fabrication

**Décision** : supprimer `production_batch.batch_number` et son index unique. Une fabrication est
identifiée en base par sa clé technique, et à l'écran par sa date et son prix.

**Rationale** : la colonne n'aurait plus aucun lecteur. La garder obligerait à continuer de générer
une valeur unique que personne ne lit, donc à maintenir un second registre de séquences pour rien.
Le principe I de la constitution tranche : ce qui n'est pas utilisé par l'utilisateur ne doit pas
survivre en coulisse.

**Alternatives écartées** : conserver la colonne comme identifiant technique lisible, rejetée car
la clé primaire remplit déjà ce rôle ; la conserver le temps d'une transition, rejetée car aucune
étiquette de l'ancien format n'est en circulation.

---

## D3 — Le registre de séquences

**Décision** : renommer `batch_number_sequence` en `unit_number_sequence`. La clé primaire reste le
couple produit / date de production ; `last_sequence` compte désormais des unités.

**Rationale** : la table a été créée hier soir pour une seule raison, garantir qu'un rang émis ne
soit jamais réattribué. Cette raison est intacte, seul l'objet compté change. Renommer plutôt que
créer une seconde table évite de laisser deux registres cohabiter.

**Alternatives écartées** : une séquence PostgreSQL native par produit et par date, rejetée car le
nombre de séquences croîtrait sans limite et qu'aucune ne serait supprimable ; un compteur porté
par `product`, rejeté car le rang doit repartir de 1 chaque jour.

---

## D4 — Le moment de l'attribution

**Décision** : le numéro est attribué à la création des unités, c'est-à-dire dans l'appel qui
génère et pèse une fournée, pas à la création de la fabrication. Une demande de dix unités prend
dix rangs d'un coup et incrémente le registre d'autant, dans une seule transaction.

**Rationale** : c'est le geste qui produit les objets à étiqueter. La fabrication peut être
enregistrée avant que la pesée ne commence ; réserver des rangs à ce moment-là les gaspillerait si
la pesée n'a jamais lieu.

**Alternatives écartées** : réserver une plage à la création de la fabrication, rejetée pour la
raison ci-dessus ; attribuer un rang par unité en autant d'allers-retours, rejeté car dix
incréments successifs multiplient les occasions de conflit sans rien apporter.

---

## D5 — La concurrence sur le registre

**Décision** : lire la ligne du registre avec un verrou de ligne dans la transaction qui l'incrémente.

**Rationale** : la lecture puis l'écriture sans verrou laisse deux appels simultanés calculer le même
rang. L'index unique sur `unit_number` rattraperait le coup, mais en renvoyant une erreur à
l'utilisateur au lieu du numéro suivant. Le verrou fait sortir les deux appels l'un après l'autre.
C'est aussi la correction d'une faiblesse relevée dans la revue du code livré hier soir, où la boucle
de réessai avait disparu sans être remplacée.

**Alternatives écartées** : réessayer sur violation d'unicité, rejetée car elle transforme une
garantie simple en boucle à réessais ; sérialiser toute la transaction, rejetée comme
disproportionnée pour deux utilisateurs.

---

## D6 — La reprise des données existantes

**Décision** : la migration rétro-remplit `unit_number` pour les unités déjà en base, en parcourant
les fournées par date de production puis les unités par ordre de création, et initialise le registre
sur le dernier rang attribué. Aucune donnée n'est effacée.

**Rationale** : l'utilisateur a autorisé l'effacement du jeu d'essai de développement, mais une
migration est aussi jouée sur l'instance déployée. Une migration destructive y détruirait des
données réelles. Un rétro-remplissage déterministe coûte quelques lignes de SQL et rend la question
sans objet. En développement, la base peut de toute façon être réinitialisée si le résultat déplaît.

**Alternatives écartées** : vider les tables de stock dans la migration, rejetée comme dangereuse
hors du poste de développement ; laisser `unit_number` nullable pour les anciennes lignes, rejetée
car une unité sans numéro serait une unité sans étiquette possible.

**Point clos le 2026-09-10** : l'instance déployée ne contient elle aussi que des données de test,
recréables. Rien n'est en exploitation réelle avant la fin de la Vague 1. Aucune étiquette
manuscrite n'est donc en circulation, ni en développement ni sur le serveur, et le rétro-remplissage
n'a aucune étiquette papier à contredire. Il est conservé malgré tout : il coûte quelques lignes,
évite une migration destructive, et gardera sa valeur le jour où l'exploitation réelle commencera.

---

## D7 — Comment une fournée s'annonce à l'écran

**Décision** : un groupe est intitulé par sa date de production et son prix. Quand plusieurs
fournées d'un même produit partagent une date, leur rang dans la journée est ajouté au titre. Ce
rang est un libellé d'affichage, calculé à la lecture depuis l'ordre des fournées ; il n'est ni
stocké, ni exposé comme identifiant.

**Rationale** : c'est la seule information dont l'utilisateur a besoin pour dire « la fournée du
matin » plutôt que « la fournée SC-260910-2 ». Le stocker le transformerait en numéro, donc en ce
qu'on supprime.

**Alternatives écartées** : afficher la plage de numéros du groupe, séduisante mais fausse dès que
les numéros d'une fournée ne sont pas contigus ; ajouter une heure de fabrication, rejetée car ce
serait un champ de saisie de plus (principe I).

---

## D8 — Faut-il un ADR

**Décision** : non. La documentation de référence à mettre à jour est `docs/data-model.md` (§3.5,
§3.9 et la contrainte C-12) et `CLAUDE.md` (§8 règle 9, §9).

**Rationale** : le principe IV de la constitution réserve l'ADR aux décisions d'architecture
structurantes. Aucune décision de pile, de frontière ni de déploiement ne change ici. Ce qui change
est une règle de domaine, dont le lieu de référence est le modèle de données.

**Alternative écartée** : un ADR de remplacement, rejeté car il n'existe aucun ADR sur la
numérotation à remplacer.
