"""Serveur d'enregistrement du corpus vocal (spike assistant vocal, étape 0).

Sert la page d'enregistrement en HTTPS (le micro l'exige hors de localhost) et range
chaque prise dans development/assistant-corpus/, avec corpus.csv et testers.csv.
Les phrases sont lues dans docs/assistant-vocal-phrases-test.md : une seule source.

Bibliothèque standard uniquement. Lancement : voir README.md.
"""

import argparse
import csv
import json
import re
import secrets
import ssl
import subprocess
import sys
import threading
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlparse

ROOT = Path(__file__).resolve().parents[3]
PHRASES_DOC = ROOT / "docs" / "assistant-vocal-phrases-test.md"
CORPUS_DIR = ROOT / "development" / "assistant-corpus"
INDEX_HTML = Path(__file__).resolve().parent / "index.html"

CONDITIONS = {"calme", "cuisine", "loin"}
EXTENSIONS = {"audio/webm": "webm", "audio/ogg": "ogg", "audio/mp4": "m4a", "audio/mpeg": "mp3"}
MAX_UPLOAD = 10 * 1024 * 1024
PHRASE_ROW = re.compile(r"^\| ([A-F]\d+) \| (.+?) \| (.+?) \|\s*$")

CORPUS_FIELDS = ["file", "tester", "condition", "phrase_id", "phrase", "expected", "mime", "recorded_at"]
TESTER_FIELDS = ["tester", "age_range", "voice", "accent", "phone", "consent_at"]

lock = threading.Lock()


def load_phrases():
    phrases = []
    for line in PHRASES_DOC.read_text(encoding="utf-8").splitlines():
        match = PHRASE_ROW.match(line)
        if match:
            phrase_id, phrase, expected = match.groups()
            phrases.append({"id": phrase_id, "phrase": phrase, "expected": expected})
    return phrases


def slug(value):
    """Identifiant de testeur sûr pour un nom de fichier : lettres, chiffres, tirets."""
    value = re.sub(r"[^a-z0-9-]+", "-", value.strip().lower()).strip("-")
    return value[:30]


def read_csv(path):
    if not path.exists():
        return []
    with path.open(newline="", encoding="utf-8") as f:
        return list(csv.DictReader(f))


def write_csv(path, fields, rows):
    tmp = path.with_suffix(".tmp")
    with tmp.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)
    tmp.replace(path)


def upsert(path, fields, key, row):
    """Remplace la ligne de même clé (une prise refaite écrase la précédente), ou l'ajoute."""
    rows = [r for r in read_csv(path) if tuple(r[k] for k in key) != tuple(row[k] for k in key)]
    rows.append(row)
    write_csv(path, fields, rows)


def ensure_certificate():
    cert, key = CORPUS_DIR / ".cert" / "cert.pem", CORPUS_DIR / ".cert" / "key.pem"
    if not cert.exists():
        cert.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run(
            ["openssl", "req", "-x509", "-newkey", "rsa:2048", "-nodes", "-days", "365",
             "-subj", "/CN=saloir-corpus", "-keyout", str(key), "-out", str(cert)],
            check=True, capture_output=True,
        )
    return cert, key


class Handler(BaseHTTPRequestHandler):
    token = None
    phrases = []

    def _send(self, status, body, content_type="application/json; charset=utf-8"):
        data = body if isinstance(body, bytes) else json.dumps(body, ensure_ascii=False).encode()
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(data)

    def _authorized(self):
        return self.token is None or secrets.compare_digest(self.headers.get("X-Corpus-Token", ""), self.token)

    def _json_body(self):
        length = int(self.headers.get("Content-Length", 0))
        if length > 64 * 1024:
            return None
        try:
            return json.loads(self.rfile.read(length) or b"{}")
        except json.JSONDecodeError:
            return None

    def do_GET(self):
        url = urlparse(self.path)
        if url.path == "/":
            return self._send(200, INDEX_HTML.read_bytes(), "text/html; charset=utf-8")
        if not self._authorized():
            return self._send(401, {"error": "Code d'accès incorrect."})
        if url.path == "/api/phrases":
            return self._send(200, self.phrases)
        if url.path == "/api/progress":
            query = parse_qs(url.query)
            tester = slug(query.get("tester", [""])[0])
            condition = query.get("condition", [""])[0]
            done = [r["phrase_id"] for r in read_csv(CORPUS_DIR / "corpus.csv")
                    if r["tester"] == tester and r["condition"] == condition]
            return self._send(200, done)
        return self._send(404, {"error": "Introuvable."})

    def do_POST(self):
        url = urlparse(self.path)
        if not self._authorized():
            return self._send(401, {"error": "Code d'accès incorrect."})
        if url.path == "/api/testers":
            return self._save_tester()
        if url.path == "/api/recordings":
            return self._save_recording(parse_qs(url.query))
        return self._send(404, {"error": "Introuvable."})

    def _save_tester(self):
        body = self._json_body()
        tester = slug((body or {}).get("tester", ""))
        if not tester or not body.get("consent"):
            return self._send(400, {"error": "Il faut un nom et l'accord du testeur."})
        row = {"tester": tester, "consent_at": datetime.now(timezone.utc).isoformat()}
        for field in ("age_range", "voice", "accent", "phone"):
            row[field] = str(body.get(field, ""))[:60]
        with lock:
            CORPUS_DIR.mkdir(parents=True, exist_ok=True)
            upsert(CORPUS_DIR / "testers.csv", TESTER_FIELDS, ["tester"], row)
        return self._send(200, {"tester": tester})

    def _save_recording(self, query):
        tester = slug(query.get("tester", [""])[0])
        condition = query.get("condition", [""])[0]
        phrase = next((p for p in self.phrases if p["id"] == query.get("id", [""])[0]), None)
        mime = self.headers.get("Content-Type", "").split(";")[0].strip()
        length = int(self.headers.get("Content-Length", 0))
        if not tester or condition not in CONDITIONS or phrase is None:
            return self._send(400, {"error": "Testeur, condition ou phrase inconnus."})
        if mime not in EXTENSIONS or not 0 < length <= MAX_UPLOAD:
            return self._send(400, {"error": f"Enregistrement refusé ({mime or 'format inconnu'})."})

        audio = self.rfile.read(length)
        name = f"{tester}-{condition}-{phrase['id']}.{EXTENSIONS[mime]}"
        with lock:
            if not any(r["tester"] == tester for r in read_csv(CORPUS_DIR / "testers.csv")):
                return self._send(400, {"error": "Testeur inconnu : recommence depuis l'accueil."})
            for old in CORPUS_DIR.glob(f"{tester}-{condition}-{phrase['id']}.*"):
                old.unlink()
            (CORPUS_DIR / name).write_bytes(audio)
            upsert(CORPUS_DIR / "corpus.csv", CORPUS_FIELDS, ["tester", "condition", "phrase_id"], {
                "file": name, "tester": tester, "condition": condition, "phrase_id": phrase["id"],
                "phrase": phrase["phrase"], "expected": phrase["expected"], "mime": mime,
                "recorded_at": datetime.now(timezone.utc).isoformat(),
            })
        return self._send(200, {"file": name})

    def log_message(self, fmt, *args):
        print(f"{self.address_string()} {fmt % args}")


def main():
    sys.stdout.reconfigure(line_buffering=True)
    parser = argparse.ArgumentParser(description="Serveur d'enregistrement du corpus vocal")
    parser.add_argument("--port", type=int, default=8443)
    parser.add_argument("--http", action="store_true", help="sans HTTPS (tests sur le PC en localhost)")
    parser.add_argument("--no-token", action="store_true", help="sans code d'accès (réseau de confiance)")
    args = parser.parse_args()

    Handler.phrases = load_phrases()
    Handler.token = None if args.no_token else secrets.token_urlsafe(6)
    CORPUS_DIR.mkdir(parents=True, exist_ok=True)

    server = ThreadingHTTPServer(("0.0.0.0", args.port), Handler)
    scheme = "http"
    if not args.http:
        context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
        context.load_cert_chain(*ensure_certificate())
        server.socket = context.wrap_socket(server.socket, server_side=True)
        scheme = "https"

    print(f"{len(Handler.phrases)} phrases lues dans {PHRASES_DOC.relative_to(ROOT)}")
    print(f"Enregistrements rangés dans {CORPUS_DIR.relative_to(ROOT)}/")
    print(f"Page : {scheme}://<adresse-du-pc>:{args.port}/")
    if Handler.token:
        print(f"Code d'accès : {Handler.token}")
    server.serve_forever()


if __name__ == "__main__":
    main()
