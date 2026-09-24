# Quickstart — Assistant vocal

**Feature**: `specs/006-assistant-vocal` | **Date**: 2026-09-23

Guide de validation de bout en bout. Contrat : [contracts/api.md](./contracts/api.md) ; modèle :
[data-model.md](./data-model.md) ; fonctionnement : `docs/assistant-vocal-fonctionnement.md`.

---

## 1. Vérifications automatiques

```bash
dotnet test backend                                   # toute la suite, sans appel à Mistral
```

Frontend, depuis Windows (où `node_modules` est installé) : `npm run test:unit`, `npm run type-check`,
`npm run lint`.

Banc du LLM, sur le jeu de phrases (appelle Mistral, quelques centimes) :

```bash
ASSISTANT_EVAL=1 dotnet test backend --filter "FullyQualifiedName~AssistantEvaluation" \
  --logger "console;verbosity=detailed"
```

Attendu : intention juste ≥ 95 % et champs justes ≥ 90 % sur les phrases écrites (SC-004), aucun
mauvais client (SC-001).

## 2. Essai en local sur téléphone

Prérequis : `MISTRAL_API_KEY` dans `development/.env`, base de dev avec des **clients fictifs**.

1. Backend (WSL) : `make run`.
2. Frontend (PowerShell) : `$env:SALOIR_DEV_HTTPS="1"; npm run dev -- --host`.
3. Pare-feu Windows, le temps de l'essai : règle entrante TCP 5173, profil privé.
4. Téléphone, même Wi-Fi : `https://<ip-du-pc>:5173`, accepter le certificat, se connecter.

| # | Scénario | Attendu |
|---|---|---|
| 1 | Compte **sans** assistant, « + » de l'écran Ventes | « Nouvelle vente » s'ouvre en un appui ; pas de « Dicter » (FR-001) |
| 2 | Écran Comptes : activer l'assistant pour ce compte, recharger | Le « + » propose « Dicter » et « Nouvelle vente » |
| 3 | « Dicter » : « il me reste combien de *\<produit\>* ? », puis se taire | Envoi seul après ~2 s ; texte affiché, puis lu par la voix de Mistral ; chiffres identiques à l'écran Stock (SC-002) |
| 4 | « vends deux *\<produit\>* à *\<client\>* » → « Ouvrir la vente » | Formulaire pré-rempli : client, unités les plus anciennes, montants, « À payer » ; rien n'est enregistré avant « Enregistrer » |
| 5 | Vente pour un client absent de la base | Client à choisir, nom entendu affiché, reste pré-rempli (FR-018) |
| 6 | « Dicter » puis silence | Abandon au bout de 7 s, « Je n'ai rien entendu », rien d'envoyé |
| 7 | Désactiver l'assistant du compte pendant la session, redicter | `403`, message en français ; au rechargement, plus de « Dicter » |
| 8 | Dans `development/.env` : `Assistant__MaxRequestsPerHour=2`, relancer, faire 3 demandes | La troisième est refusée en français, sans appel extérieur (FR-023) |
| 9 | Clé Mistral vidée, relancer, dicter | « L'assistant ne répond pas pour le moment » ; le reste de l'application fonctionne (FR-027) |
| 10 | Écran Rapports, section « Assistant vocal » (administrateur) | Demandes des scénarios par compte et par semaine, issues, durée médiane ; détail avec la phrase entendue (FR-025) |

**Délai (SC-003)** : chronométrer 10 demandes, de la fin de la phrase à l'affichage du texte. Noter la
médiane et le maximum dans `docs/spike-assistant-vocal.md` (ou le document de recette).

Après l'essai : retirer la règle de pare-feu, arrêter les serveurs.

## 3. Mise en production

Dans cet ordre :

1. **VPS, `/opt/butcher-app/.env`** : ajouter `MISTRAL_API_KEY` (clé du compte en paiement à
   l'usage). Les réglages `ASSISTANT_*` sont facultatifs (valeurs par défaut du code).
2. **VPS, `Caddyfile`** (aucun workflow ne le copie, `CLAUDE.md` §9) : copier la version du dépôt,
   qui autorise le micro pour le site (`microphone=(self)`) et la lecture audio (`media-src 'self'
   blob:`), puis redémarrer Caddy. Sans cette étape, le micro est refusé et la voix de Mistral bloquée.
3. **Releases** : `make release-backend version=…` puis `make release-frontend version=…`, tags
   poussés ; la migration (`app_user.assistant_enabled`, `voice_request`) s'applique au démarrage.
4. **Vérification** : sur le téléphone de l'utilisateur visé, en production, ouvrir l'outil,
   vérifier qu'aucun « Dicter » n'apparaît avant activation.
5. **Activation** : écran Comptes, activer l'assistant pour ce seul compte.
6. **Premier usage accompagné** : dérouler les scénarios 3 et 4 avec lui.

## 4. Suivi après livraison

- **Chaque semaine, 4 semaines** : écran Rapports, section « Assistant vocal ». Adoption (SC-006) :
  au moins 3 demandes par semaine en moyenne. Relire les demandes « Pas compris » et « Erreur ».
- **Chaque mois** : coût dans la console Mistral (SC-007 : moins de 5 €).
- **Tout mauvais client signalé** (SC-001) : désactiver l'assistant pour le compte, retrouver la
  demande dans le détail (phrase entendue), corriger avant de réactiver.
