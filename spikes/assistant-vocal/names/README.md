# Reconnaissance des clients — étape 2

Deux versions de la même pièce, comparées sur le même jeu (`evaluation.json`) :

| Version | Où | Briques |
|---|---|---|
| C# | `backend/src/Butcher.Api/Application/Assistant/` | Phonétique et règles écrites à la main, sans dépendance |
| Python | `prototype.py` | espeak-ng (prononciation API), rapidfuzz, spaCy `fr_core_news_md` (noms de personnes) |

Résultats et conclusion : `docs/spike-assistant-vocal.md` §7.

## Jeu d'évaluation

`dataset.py` produit `evaluation.json` et `CorpusTranscripts.cs` (tests C#) à partir des transcriptions de l'étape 1 et de phrases écrites pour les noms inconnus. Une seule source : c'est là qu'on ajoute des phrases.

```bash
python3 dataset.py
```

## Lancer

Tout s'installe dans un environnement virtuel ; aucun cache dans le dossier personnel (espeak-ng est embarqué par `espeakng-loader`, rien à installer sur le système).

```bash
python3 -m venv .venv && .venv/bin/pip install --no-cache-dir -r requirements.txt
.venv/bin/python prototype.py --detail                     # version Python
dotnet test ../../../backend/tests/Butcher.Api.Tests \
  --filter "FullyQualifiedName~CustomerNameMatcher" \
  --logger "console;verbosity=detailed"                    # version C#
```
