"""Banc de transcription (spike assistant vocal, étape 1).

Passe chaque prise du corpus dans un moteur et écrit results/<moteur>.csv :
fichier, texte transcrit, durée de transcription. Une prise déjà transcrite
n'est pas refaite (pas de double facturation Mistral) : supprimer le CSV pour
tout relancer.

    python bench.py voxtral voxtral-vocab whisper-small whisper-medium ...
"""

import csv
import json
import os
import subprocess
import sys
import threading
import time
import urllib.error
import urllib.request
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
CORPUS_DIR = ROOT / "development" / "assistant-corpus"
RESULTS_DIR = CORPUS_DIR / "results"
# Modèles et caches de téléchargement rangés avec le corpus, hors de git et hors du dossier personnel.
MODELS_DIR = CORPUS_DIR / ".models"
os.environ.setdefault("HF_HOME", str(MODELS_DIR / "huggingface"))

# Vocabulaire guidé : produits, gestes et unités. Jamais les clients (cadrage §9, D-05).
VOCABULARY = [
    "saucisson", "saucissons", "jambon", "terrine", "terrines",
    "vends", "vendu", "mets", "tranche", "tranches", "entamé",
    "grammes", "kilo", "livre", "payé", "stock",
]

VOXTRAL_URL = "https://api.mistral.ai/v1/audio/transcriptions"
VOXTRAL_MODEL = "voxtral-mini-2602"


def mistral_key():
    key = os.environ.get("MISTRAL_API_KEY")
    if not key:
        for line in (ROOT / "development" / ".env").read_text(encoding="utf-8").splitlines():
            if line.startswith("MISTRAL_API_KEY="):
                key = line.split("=", 1)[1].strip()
    if not key:
        sys.exit("MISTRAL_API_KEY absente (development/.env)")
    return key


def multipart(fields, file_path):
    boundary = uuid.uuid4().hex
    parts = []
    for name, value in fields:
        parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n{value}\r\n'.encode())
    parts.append(
        f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{file_path.name}"\r\n'
        f"Content-Type: audio/webm\r\n\r\n".encode() + file_path.read_bytes() + b"\r\n"
    )
    parts.append(f"--{boundary}--\r\n".encode())
    return b"".join(parts), f"multipart/form-data; boundary={boundary}"


def voxtral(with_vocabulary):
    key = mistral_key()
    fields = [("model", VOXTRAL_MODEL), ("language", "fr")]
    if with_vocabulary:
        fields += [("context_bias", word) for word in VOCABULARY]

    def transcribe(path):
        body, content_type = multipart(fields, path)
        request = urllib.request.Request(VOXTRAL_URL, data=body, method="POST", headers={
            "Authorization": f"Bearer {key}", "Content-Type": content_type})
        for attempt in range(6):
            try:
                with urllib.request.urlopen(request, timeout=60) as response:
                    return json.load(response)["text"]
            except urllib.error.HTTPError as error:
                if error.code != 429 or attempt == 5:
                    raise
                wait = 2 ** attempt
                print(f"  limite de débit Mistral (429), nouvel essai dans {wait} s")
                time.sleep(wait)

    return transcribe, None


class GpuMemoryProbe:
    """Pic de mémoire vidéo pendant le banc, relevé par nvidia-smi (CTranslate2 ne se bride pas)."""

    def __init__(self):
        self.peak = 0
        self._stop = threading.Event()
        self._thread = threading.Thread(target=self._run, daemon=True)

    def _run(self):
        while not self._stop.is_set():
            out = subprocess.run(["nvidia-smi", "--query-gpu=memory.used", "--format=csv,noheader,nounits"],
                                 capture_output=True, text=True).stdout.strip()
            if out:
                self.peak = max(self.peak, int(out.splitlines()[0]))
            time.sleep(0.2)

    def start(self):
        self.baseline = int(subprocess.run(["nvidia-smi", "--query-gpu=memory.used", "--format=csv,noheader,nounits"],
                                           capture_output=True, text=True).stdout.strip().splitlines()[0])
        self._thread.start()
        return self

    def stop(self):
        self._stop.set()
        self._thread.join()
        return self.peak - self.baseline


def whisper(size, with_vocabulary):
    from faster_whisper import WhisperModel

    probe = GpuMemoryProbe().start()  # avant le chargement : le modèle compte dans le pic
    # int8 : ce que la P600 (Pascal) exécute efficacement ; le float16 y est lent.
    model = WhisperModel(size, device="cuda", compute_type="int8", download_root=str(MODELS_DIR))
    hotwords = " ".join(VOCABULARY) if with_vocabulary else None

    def transcribe(path):
        segments, _ = model.transcribe(str(path), language="fr", beam_size=5, hotwords=hotwords,
                                       vad_filter=False)
        return " ".join(s.text.strip() for s in segments)

    return transcribe, probe


ENGINES = {
    "voxtral": lambda: voxtral(False),
    "voxtral-vocab": lambda: voxtral(True),
    "whisper-small": lambda: whisper("small", False),
    "whisper-small-vocab": lambda: whisper("small", True),
    "whisper-medium": lambda: whisper("medium", False),
    "whisper-medium-vocab": lambda: whisper("medium", True),
}


def save(out_path, rows):
    RESULTS_DIR.mkdir(exist_ok=True)
    tmp = out_path.with_suffix(".tmp")
    with tmp.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=["file", "text", "seconds"])
        writer.writeheader()
        writer.writerows(sorted(rows, key=lambda r: r["file"]))
    tmp.replace(out_path)


def run(engine):
    corpus = list(csv.DictReader((CORPUS_DIR / "corpus.csv").open(encoding="utf-8")))
    out_path = RESULTS_DIR / f"{engine}.csv"
    done = {}
    if out_path.exists():
        done = {r["file"]: r for r in csv.DictReader(out_path.open(encoding="utf-8"))}
    todo = [r for r in corpus if r["file"] not in done]
    if not todo:
        print(f"{engine} : rien à faire ({len(done)} prises déjà transcrites)")
        return

    transcribe, probe = ENGINES[engine]()
    rows = list(done.values())
    for i, row in enumerate(todo, 1):
        path = CORPUS_DIR / row["file"]
        started = time.perf_counter()
        text = transcribe(path)
        seconds = time.perf_counter() - started
        rows.append({"file": row["file"], "text": text, "seconds": f"{seconds:.2f}"})
        save(out_path, rows)  # après chaque prise : une interruption ne perd rien de payé
        print(f"{engine} {i}/{len(todo)} {row['phrase_id']:>4} {seconds:5.2f}s  {text}")

    if probe:
        print(f"{engine} : pic de mémoire vidéo ≈ {probe.stop()} Mo")


if __name__ == "__main__":
    sys.stdout.reconfigure(line_buffering=True)
    for name in sys.argv[1:] or ["voxtral"]:
        if name not in ENGINES:
            sys.exit(f"Moteur inconnu : {name} (connus : {', '.join(ENGINES)})")
        run(name)
