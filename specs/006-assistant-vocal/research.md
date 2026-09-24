# Research — Assistant vocal

**Feature**: `specs/006-assistant-vocal` | **Date**: 2026-09-23

Chaque décision suit le format Décision / Justification / Alternatives écartées. Les mesures qui les
fondent sont dans `docs/spike-assistant-vocal.md` §7 et §8 ; le fonctionnement du code existant dans
`docs/assistant-vocal-fonctionnement.md`.

---

## R-01 — Partir du code du spike, pas de zéro

**Décision** : la fonctionnalité se construit sur la branche `feat/assistant-vocal`, à partir du code
du spike. Chaque pièce est classée : **gardée** (répond déjà à la spec, testée), **reprise** (répond
en partie), **nouvelle** (absente du spike).

| Exigence | Pièce du spike | Classement |
|---|---|---|
| FR-001 (« + » à deux choix) | `ActionFab.vue` | **Reprise** : n'afficher « Dicter » que pour un compte qui a l'assistant, sinon « + » à un appui |
| FR-002 à FR-004 (écoute, silence, micro libéré) | `useAssistant.ts` (`createVoiceActivity`) | Gardée |
| FR-005 (clavier) | `AssistantPanel.vue` | Gardée |
| FR-006, FR-007, FR-009 (issues, phrase entendue, lecture seule) | `AssistantEngine`, `AssistantPanel` | Gardée |
| FR-008 (voix naturelle, repli) | `MistralClient.SpeakAsync`, `useAssistant.speak` | **Reprise** : voir R-05 |
| FR-010, FR-011 (stock, chiffres du serveur) | `StockSummaryBuilder`, `AssistantService.LoadStockAsync` | **Reprise** : retirer la mise en phrase par le LLM (R-06) ; tester la lecture en base (R-09) |
| FR-012 (produit inconnu) | consignes du LLM, `AssistantEngine` | Gardée |
| FR-013 à FR-017 (brouillon, règles, formulaire) | `SaleDraftBuilder`, `useAssistantDraft`, `SaleAddView` | Gardée |
| FR-018, FR-019 (jamais un mauvais client, noms protégés) | `CustomerNameMatcher`, `FrenchPhonetic` | Gardée |
| FR-020, FR-021 (lecture limitée aux phrases de l'assistant) | route `/speech` ouverte | **Reprise** : R-05 |
| FR-022, FR-026 (accès, activation par compte) | — | **Nouvelle** : R-03 |
| FR-023 (limite par compte) | — | **Nouvelle** : R-04 |
| FR-024, FR-025 (journal, usage) | — | **Nouvelle** : R-02, R-08 |
| FR-027 (service indisponible) | `ServiceUnavailableException` → `503` | Gardée |
| Production | — | **Nouvelle** : R-07 |

**Justification** : le code du spike suit déjà les conventions du projet (tests backend et frontend,
droits relus en base, montant calculé par le formulaire, poids restant calculé par le serveur) et a
été éprouvé sur téléphone. Le réécrire reproduirait les mêmes décisions et ferait perdre les
corrections apprises à l'essai (audio de Chrome, silence, voix).

**Alternatives écartées** :
- Réécriture depuis `dev` : même résultat, plus lent, sans l'historique des mesures.
- Fusion du spike tel quel : la spec ajoute l'activation par compte, le journal, la limite et la
  production, sans lesquels l'assistant n'est pas livrable.

## R-02 — Journaliser les demandes dans une table dédiée, pas dans `audit_entry`

**Décision** : une table `voice_request`, écrite explicitement par `AssistantService` à la fin de
chaque demande (y compris en erreur ou limite atteinte). Elle porte le compte, la date, la phrase
entendue, l'issue, la phrase de réponse et la durée de traitement ; jamais l'audio.

**Justification** : `audit_entry` trace un **geste qui change des données**, déduit du `ChangeTracker`
par `SaveChanges` ; une demande vocale ne change rien (FR-009). Ses champs sont d'une autre nature
(phrase entendue, issue, durée) et son usage aussi (mesure de l'adoption et du délai, diagnostic des
ratés). La mêler au journal des gestes brouillerait les deux. `CLAUDE.md` §9 interdit d'écrire une
`AuditEntry` depuis un service métier : une table à part respecte la règle au lieu de la contourner.

**Alternatives écartées** :
- Une entrée `audit_entry` par demande : contraire à « une entrée par geste », sans colonne pour
  l'issue ni la durée.
- Des journaux applicatifs (logs) : pas de requête possible pour l'écran d'usage, et une durée de
  conservation qui dépend de l'hébergement.

## R-03 — Activation par compte : une colonne, vérifiée par une politique d'autorisation

**Décision** : `app_user.assistant_enabled` (booléen, faux par défaut), exposé dans `AccountDto` et
`GET /api/auth/me`, modifiable par l'administrateur dans `UpdateAccountRequest`. Les routes de
l'assistant portent une politique `AssistantEnabled`, servie par le handler existant
(`AccountAuthorizationHandler`), qui relit le compte en base à chaque requête : actif **et**
assistant activé. Refus : `403` avec un message en français.

**Justification** : même mécanisme que les droits d'administrateur (ADR-011) : la base porte le
droit, pas le jeton ; une désactivation prend effet à la requête suivante (FR-026). La modification
d'un compte est déjà tracée au journal des gestes (`AuditTrail.CollectAccounts`).

**Alternatives écartées** :
- Un interrupteur global en configuration : écarté par la clarification Q2 (compte par compte).
- Un droit lu dans le jeton : un compte désactivé garderait l'assistant jusqu'à 15 minutes
  (`CLAUDE.md` §9).

## R-04 — Limite par compte : comptée dans le journal des demandes

**Décision** : avant d'appeler Mistral, `AssistantService` compte les demandes du compte sur l'heure
glissante dans `voice_request`. Au-delà de la limite (`Assistant:MaxRequestsPerHour`, 30 par
défaut), la demande est journalisée avec l'issue `rate_limited` et refusée en `429`, message en
français. Index `(account_id, occurred_at)`.

**Justification** : la spec veut que la limite atteinte figure au journal (issue « limite atteinte »)
et qu'aucun appel extérieur ne parte (FR-023). Le compteur est le journal lui-même : rien à
synchroniser, et la règle survit à un redémarrage.

**Alternatives écartées** :
- Le limiteur d'ASP.NET Core (comme pour la connexion) : il refuse avant le contrôleur, donc sans
  entrée au journal, et son compteur en mémoire repart à zéro au redémarrage.

## R-05 — Lecture à voix haute : seulement la phrase d'une demande du même compte

**Décision** : la réponse porte l'identifiant de sa demande (`requestId`). La voix se demande par
`GET /api/assistant/requests/{id}/speech` : le serveur relit la phrase de réponse enregistrée dans
`voice_request`, vérifie que la demande appartient au compte et date de moins de 10 minutes, puis
appelle Voxtral TTS. La route `POST /api/assistant/speech` (texte libre) disparaît.

**Justification** : FR-020 interdit d'envoyer au service de synthèse une phrase qui ne vienne pas de
l'assistant. Relire la phrase en base la rend impossible à forger, sans secret ni cache : elle est
déjà journalisée (R-02). Le texte reste affiché tout de suite ; la voix suit.

**Alternatives écartées** :
- Audio renvoyé dans la réponse : retarde l'affichage du texte d'environ une seconde.
- Texte signé par le serveur (HMAC) : une clé de plus à gérer pour le même résultat.
- Cache mémoire des phrases : perdu au redémarrage, sans apport face à la table déjà présente.

## R-06 — La phrase dite est écrite par le serveur ; la mise en phrase par le LLM disparaît

**Décision** : suppression du réglage `Assistant:LlmSpeech`, du second appel au LLM, de la consigne de
mise en phrase et du contrôle des chiffres inventés (`StockSummaryBuilder.InventedNumbers`), qui ne
servaient qu'à elle. Le banc d'évaluation ne mesure plus que l'intention, les champs et le client.

**Justification** : au banc, le LLM a dit « 1 jambon entier » pour 2, erreur qu'aucun contrôle des
chiffres ne voyait (ADR-012). La phrase du serveur est juste par construction (FR-011). Garder une
option abandonnée, c'est garder du code à maintenir et une porte vers l'erreur.

**Alternatives écartées** :
- Garder l'option désactivée par défaut : code mort, et le contrôle des chiffres donnerait une
  fausse assurance.

## R-07 — Production : clé, en-têtes de Caddy, et déploiement du `Caddyfile`

**Décision** :
1. `docker-compose.prod.yml` passe au backend `MISTRAL_API_KEY` et les réglages `Assistant__*` ;
   `.env.example` les documente.
2. **Le `Caddyfile` change** : `Permissions-Policy` autorise le micro pour la même origine
   (`microphone=(self)`, il est aujourd'hui interdit : `microphone=()`) ; la CSP ajoute
   `media-src 'self' blob:`, sans quoi la voix de Mistral, jouée depuis un `blob:`, est bloquée.
3. Le `Caddyfile` n'est copié par aucun workflow (`CLAUDE.md` §9) : la mise à jour du VPS est une
   étape manuelle du quickstart, avant la première activation.

**Justification** : sans (2), l'assistant fonctionne en local et échoue en production, le micro
refusé et la voix rabattue sur celle du téléphone. C'est l'écart le plus probable entre les essais
et la production, d'où son traitement explicite.

**Alternatives écartées** :
- Ajouter la copie du `Caddyfile` au job `deploy` : souhaitable (question ouverte de `CLAUDE.md`
  §11), mais hors du périmètre de cette fonctionnalité.

## R-08 — Mesurer l'usage et le délai : un rapport, sur l'écran Rapports

**Décision** : `GET /api/reports/assistant?from&to` (administrateur) rend, par compte et par semaine
(semaines de Paris, `BusinessTime`), le nombre de demandes, la répartition des issues et la durée
médiane de traitement ; `GET /api/reports/assistant/requests` rend les dernières demandes avec leur
phrase entendue et leur réponse. L'écran Rapports gagne une section « Assistant vocal ».

La durée journalisée est **la durée de traitement par le serveur** (réception de l'audio → réponse
prête). Le délai ressenti de SC-003 y ajoute l'envoi de l'audio, mesuré une fois à la recette
(quickstart), pas en continu.

**Justification** : les rapports existants suivent déjà ce modèle (calcul serveur, période,
`BusinessTime`, `CLAUDE.md` §9). Un écran de plus pour trois comptes ne se justifie pas.

**Alternatives écartées** :
- Faire mesurer le délai par le téléphone et l'envoyer : une route et un état de plus pour une
  précision dont on n'a pas besoin au quotidien.
- Un écran « Assistant » dans la barre latérale : une entrée de plus, pour un usage ponctuel.

## R-09 — Tester la lecture du stock sur une vraie base

**Décision** : un test d'intégration de `AssistantService` sur PostgreSQL (Testcontainers, comme les
autres services), avec un faux `IMistralClient` : il vérifie que le stock lu (unités intactes,
entamées, poids restant) est celui des écrans de stock, et que le journal, la limite et l'activation
se comportent comme prévu.

**Justification** : la requête EF Core de lecture du stock n'a été vérifiée qu'à la main, sur la base
de dev. `CLAUDE.md` rappelle qu'une somme de ventes écrite hors de la requête a déjà été évaluée en
mémoire à tort (`StockUnitService.GetAllAsync`).

## R-10 — Ce que devient l'outillage du spike

**Décision** :
- **Gardés** : le banc d'évaluation du LLM (`AssistantEvaluation`, déclenché par `ASSISTANT_EVAL=1`,
  jamais en CI), le jeu de phrases et le jeu d'évaluation, le mode HTTPS de développement de Vite
  (`SALOIR_DEV_HTTPS`) pour les essais sur téléphone.
- **Conservés dans `spikes/assistant-vocal/`**, documentés, hors de toute build : l'enregistreur, le
  banc de transcription, le prototype Python. Ils rejouent les mesures si l'on change de modèle.
- **Réécrits** : les commentaires « spike R&D » du code, remplacés par les références de la spec
  (`FR-xx`) et de l'ADR-012.

**Justification** : le banc et le corpus sont ce qui permettra de juger un changement de modèle ou de
fournisseur ; les jeter obligerait à refaire le spike.
