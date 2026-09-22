# Enregistreur du corpus vocal — étape 0

Page d'enregistrement des phrases de test (`docs/assistant-vocal-phrases-test.md`), à ouvrir **sur le téléphone** : le corpus doit avoir la voix, le micro et le format du vrai usage (`webm/opus` sous Chrome Android). Plan d'ensemble : `docs/spike-assistant-vocal.md`.

Les prises sont rangées dans `development/assistant-corpus/`, **ignoré par git** :

| Fichier | Contenu |
|---|---|
| `<testeur>-<condition>-<phrase>.webm` | Une prise ; refaire une phrase remplace la précédente |
| `corpus.csv` | Une ligne par prise : fichier, testeur, condition, phrase lue, résultat attendu, format, date |
| `testers.csv` | Une ligne par testeur : tranche d'âge, voix, accent, téléphone, date de l'accord |

## Lancer

Python 3, bibliothèque standard seulement. OpenSSL sert une fois, à créer le certificat (`development/assistant-corpus/.cert/`).

```bash
python3 spikes/assistant-vocal/recorder/server.py            # HTTPS sur le port 8443, avec code d'accès
python3 spikes/assistant-vocal/recorder/server.py --http      # essai sur le PC seul, via http://localhost:8443
```

Le serveur affiche un **code d'accès** différent à chaque lancement, à saisir sur la page d'accueil (ou à passer dans l'adresse : `https://<ip-du-pc>:8443/?code=<code>`).

Sur le téléphone, le certificat est auto-signé : Chrome affiche un avertissement. Choisir **Paramètres avancés**, puis **Continuer vers le site**. Le micro fonctionne ensuite normalement.

## Joindre le PC depuis le téléphone (WSL2)

Le téléphone et le PC doivent être sur le même Wi-Fi. WSL2 étant en réseau NAT, un serveur lancé dans WSL n'est pas visible du réseau local. Trois solutions, au choix :

1. **Lancer le serveur depuis Windows** (le plus simple) : le dépôt est sur `E:\`, et le certificat déjà créé est réutilisé.
   ```powershell
   py E:\perso\butcher-app\spikes\assistant-vocal\recorder\server.py
   ```
2. **Réseau WSL en miroir** : dans `%UserProfile%\.wslconfig`, ajouter `[wsl2]` puis `networkingMode=mirrored`, puis `wsl --shutdown`.
3. **Redirection de port** (PowerShell administrateur) :
   ```powershell
   netsh interface portproxy add v4tov4 listenport=8443 connectport=8443 connectaddress=$(wsl hostname -I).Trim()
   ```

Dans tous les cas, autoriser le port 8443 dans le pare-feu Windows (réseau privé) :

```powershell
New-NetFirewallRule -DisplayName "Saloir corpus" -Direction Inbound -Protocol TCP -LocalPort 8443 -Profile Private -Action Allow
```

Adresse du PC : `ipconfig`, ligne « Adresse IPv4 » de la carte Wi-Fi.

## Pendant les enregistrements

- Chaque testeur donne son **accord** sur la page d'accueil. Le texte précise que les prises peuvent être envoyées à Mistral pendant les essais.
- Trois conditions : au calme, en cuisine, téléphone posé. La page reprend là où le testeur s'était arrêté.
- Après les essais : arrêter le serveur (Ctrl+C), retirer la règle de pare-feu, et la redirection de port si elle a été créée.
