"""Jeu d'évaluation de la reconnaissance des clients (spike assistant vocal, étape 2).

Une seule source pour les deux versions comparées :
- evaluation.json, lu par le prototype Python ;
- backend/tests/.../Assistant/CorpusTranscripts.cs, lu par les tests C#.

Contenu : les transcriptions réelles de l'étape 1 (development/assistant-corpus/results/,
hors de git) et des phrases écrites pour éprouver les noms inconnus, marquées « ecrit ».

    python dataset.py
"""

import csv
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
RESULTS_DIR = ROOT / "development" / "assistant-corpus" / "results"
CSHARP = ROOT / "backend/tests/Butcher.Api.Tests/Application/Assistant/CorpusTranscripts.cs"

MARTIN, MARTINE, JOSETTE, PAUL, MOREAU, GERARD = 1, 2, 3, 4, 5, 6

CUSTOMERS = [
    {"id": MARTIN, "last_name": "Martin", "first_name": None},
    {"id": MARTINE, "last_name": "Roux", "first_name": "Martine"},
    {"id": JOSETTE, "last_name": "Dubois", "first_name": "Josette"},
    {"id": PAUL, "last_name": "Lefèvre", "first_name": "Paul"},
    {"id": MOREAU, "last_name": "Moreau", "first_name": None},
    {"id": GERARD, "last_name": "Gérard", "first_name": None},
]

# Clients réellement cités, par phrase du corpus. F1 cite un nom inconnu.
CORPUS_TRUTH = {
    "B1": [MARTIN], "B2": [MARTINE], "B3": [JOSETTE], "B4": [GERARD], "B5": [MOREAU],
    "B6": [PAUL], "B7": [MARTIN], "B8": [MARTIN], "C1": [MARTIN], "C2": [GERARD],
    "C3": [JOSETTE], "C4": [PAUL], "C5": [MOREAU], "D1": [MARTIN], "D2": [GERARD],
    "D3": [MOREAU], "D4": [JOSETTE], "D5": [PAUL], "E1": [MARTIN], "E2": [GERARD],
    "E3": [MARTIN], "E4": [MOREAU], "F3": [MARTIN], "F4": [MARTIN], "F5": [GERARD],
    "F6": [MARTIN], "F9": [MARTIN, GERARD], "F10": [GERARD],
}
CORPUS_UNKNOWN = {"F1": ["Petitjean"]}

# Phrases écrites : (id, texte, clients cités, noms inconnus qui ne doivent pas rester).
WRITTEN = [
    ("X1", "Deux saucissons pour Madame Lambert.", [], ["Lambert"]),
    ("X2", "deux saucissons pour madame lambert", [], ["lambert"]),
    ("X3", "Une terrine pour Jean-Pierre.", [], ["Jean", "Pierre"]),
    ("X4", "une terrine pour jean-pierre", [], ["jean", "pierre"]),
    ("X5", "Mets un jambon pour les Dupont.", [], ["Dupont"]),
    ("X6", "mets un jambon pour les dupont", [], ["dupont"]),
    ("X7", "Vends trois saucissons à Bernadette.", [], ["Bernadette"]),
    ("X8", "vends trois saucissons à bernadette", [], ["bernadette"]),
    ("X9", "Un saucisson pour Monsieur Garcia, il a payé.", [], ["Garcia"]),
    ("X10", "Deux saucissons pour la Simone.", [], ["Simone"]),
    ("X11", "Mets deux saucissons pour Gérard et un pour Madame Lambert.", [GERARD], ["Lambert"]),
    ("X12", "Une livre de jambon pour Paul.", [PAUL], []),
    # Pièges : rien n'est un client.
    ("X13", "Combien il reste de saucissons pour Noël ?", [], []),
    ("X14", "Un saucisson pour Pâques.", [], []),
    ("X15", "Deux terrines pour demain.", [], []),
    ("X16", "Un jambon pour la fête du village.", [], []),
]


def rows():
    out = []
    for path in sorted(RESULTS_DIR.glob("*.csv")):
        for r in csv.DictReader(path.open(encoding="utf-8")):
            phrase_id = r["file"].rsplit("-", 1)[1].split(".")[0]
            out.append({"source": path.stem, "id": phrase_id, "text": r["text"],
                        "truth": CORPUS_TRUTH.get(phrase_id, []), "unknown": CORPUS_UNKNOWN.get(phrase_id, [])})
    for phrase_id, text, truth, unknown in WRITTEN:
        out.append({"source": "ecrit", "id": phrase_id, "text": text, "truth": truth, "unknown": unknown})
    return out


def cs_string(value):
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'


def cs_array(values, cast=str):
    return "[" + ", ".join(cs_string(v) if cast is str else str(v) for v in values) + "]"


def write_csharp(data):
    lines = [
        "namespace Butcher.Api.Tests.Application.Assistant;",
        "",
        "/// <summary>",
        "/// Jeu d'évaluation de la reconnaissance des clients (docs/spike-assistant-vocal.md) :",
        "/// transcriptions réelles de l'étape 1 et phrases écrites (source « ecrit ») pour les noms inconnus.",
        "/// Généré par spikes/assistant-vocal/names/dataset.py : ne pas modifier à la main.",
        "/// </summary>",
        "public static class CorpusTranscripts",
        "{",
        "    public static readonly (string Source, string PhraseId, string Text, int[] Truth, string[] Unknown)[] All =",
        "    [",
    ]
    for r in data:
        lines.append(f"        ({cs_string(r['source'])}, {cs_string(r['id'])}, {cs_string(r['text'])}, "
                     f"{cs_array(r['truth'], int)}, {cs_array(r['unknown'])}),")
    lines += ["    ];", "}", ""]
    CSHARP.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    data = rows()
    (HERE / "evaluation.json").write_text(
        json.dumps({"customers": CUSTOMERS, "rows": data}, ensure_ascii=False, indent=1), encoding="utf-8")
    write_csharp(data)
    print(f"{len(data)} phrases : evaluation.json et {CSHARP.name}")
