# Banc de transcription — étape 1

Compare les moteurs de transcription sur le corpus (`development/assistant-corpus/`, étape 0). Plan et résultats : `docs/spike-assistant-vocal.md`.

```bash
python3 bench.py voxtral voxtral-vocab      # Mistral : bibliothèque standard, clé dans development/.env
python3 score.py --detail                   # client, nombres et produits justes, par moteur
```

Pour faster-whisper (GPU NVIDIA), dans un environnement virtuel :

```bash
python3 -m venv .venv && .venv/bin/pip install -r requirements.txt
SP=$(.venv/bin/python -c "import site; print(site.getsitepackages()[0])")
export LD_LIBRARY_PATH=$SP/nvidia/cublas/lib:$SP/nvidia/cudnn/lib
.venv/bin/python bench.py whisper-small whisper-medium whisper-medium-vocab
```

Les transcriptions vont dans `development/assistant-corpus/results/`, hors de git. Une prise déjà transcrite n'est pas refaite : supprimer le CSV d'un moteur pour le relancer.
