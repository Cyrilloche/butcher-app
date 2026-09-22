"""Scores du banc de transcription (spike assistant vocal, étape 1).

On ne mesure pas l'écart mot à mot avec la phrase écrite : le protocole demande de la
reformuler. On vérifie ce dont l'assistant a besoin, présent dans la transcription :
le client, les nombres, les produits. Les formes admises sont celles qu'on entend
pareil (« Lefèvre » / « Lefebvre ») ; « Martin » n'est pas « Martine ».

    python score.py            # tableau par moteur
    python score.py --detail   # et le détail des manqués
"""

import csv
import re
import statistics
import sys
import unicodedata
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
RESULTS_DIR = ROOT / "development" / "assistant-corpus" / "results"

# Par phrase : client attendu (formes admises), nombres attendus, produits attendus.
# Chaque élément est une liste d'alternatives ; « un / une » n'est pas compté comme nombre.
KEY = {
    "A1": ([], [], ["saucisson"]), "A2": ([], [], ["jambon"]), "A3": ([], [], ["terrine"]),
    "A4": ([], [], []), "A5": ([], [], ["saucisson"]), "A6": ([], [], ["jambon"]),
    "A7": ([], [], ["saucisson"]), "A8": ([], [], ["terrine"]),
    "B1": (["madame martin", "mme martin"], [["2", "deux"]], ["saucisson"]),
    "B2": (["martine"], [], ["saucisson"]),
    "B3": (["josette"], [["3", "trois"]], ["terrine"]),
    "B4": (["gerard"], [], ["terrine"]),
    "B5": (["moreau"], [], ["saucisson", "terrine"]),
    "B6": (["lefevre", "lefebvre"], [["2", "deux"]], ["saucisson"]),
    "B7": (["madame martin", "mme martin"], [["2", "deux"]], ["saucisson"]),
    "B8": (["madame martin", "mme martin"], [["3", "trois"], ["2", "deux"]], ["saucisson"]),
    "C1": (["madame martin", "mme martin"], [["300", "trois cents", "trois cent"]], ["saucisson"]),
    "C2": (["gerard"], [], ["saucisson"]),
    "C3": (["josette"], [["8", "huit"]], ["saucisson"]),
    "C4": (["paul"], [["350", "trois cent cinquante"]], ["saucisson"]),
    "C5": (["moreau"], [["demi"]], ["saucisson"]),
    "D1": (["madame martin", "mme martin"], [["200", "deux cents", "deux cent"]], ["jambon"]),
    "D2": (["gerard"], [["250", "deux cent cinquante"]], ["jambon"]),
    "D3": (["moreau"], [["livre"]], ["jambon"]),
    "D4": (["josette"], [["4", "quatre"]], ["jambon"]),
    "D5": (["lefevre", "lefebvre"], [], ["jambon"]),
    "E1": (["madame martin", "mme martin"], [["2", "deux"]], ["saucisson"]),
    "E2": (["gerard"], [], ["terrine"]),
    "E3": (["madame martin", "mme martin"], [["2", "deux"]], ["saucisson"]),
    "E4": (["moreau"], [["3", "trois"]], ["terrine"]),
    "F1": (["petitjean", "petit jean"], [["2", "deux"]], ["saucisson"]),
    "F2": ([], [["2", "deux"]], ["saucisson"]),
    "F3": (["madame martin", "mme martin"], [], []),
    "F4": (["madame martin", "mme martin"], [], []),
    "F5": (["gerard"], [["10", "dix"]], ["saucisson"]),
    "F6": (["madame martin", "mme martin"], [["2", "deux"]], ["chorizo"]),
    "F7": ([], [], []),
    "F8": ([], [], []),
    "F9": (["madame martin", "mme martin"], [["2", "deux"]], ["saucisson", "terrine"]),
    "F10": (["gerard"], [["2", "deux"]], ["saucisson"]),
}
# F9 cite deux clients : on vérifie aussi Gérard.
SECOND_CLIENT = {"F9": ["gerard"]}


def normalize(text):
    text = unicodedata.normalize("NFKD", text.lower())
    text = "".join(c for c in text if not unicodedata.combining(c))
    return " " + re.sub(r"[^a-z0-9]+", " ", text).strip() + " "


def has(text, forms):
    """Une des formes, en mots entiers (« martin » ne valide pas « martine »)."""
    return any(f" {normalize(form).strip()} " in text for form in forms)


def has_product(text, product):
    return re.search(rf" {product}s? ", text) is not None


def score(rows):
    client = numbers = products = 0
    client_total = numbers_total = products_total = 0
    misses = []
    for row in rows:
        phrase_id = row["file"].rsplit("-", 1)[1].split(".")[0]
        clients, expected_numbers, expected_products = KEY[phrase_id]
        text = normalize(row["text"])
        missed = []
        for forms in ([clients] if clients else []) + ([SECOND_CLIENT[phrase_id]] if phrase_id in SECOND_CLIENT else []):
            client_total += 1
            if has(text, forms):
                client += 1
            else:
                missed.append(f"client {forms[0]}")
        for alternatives in expected_numbers:
            numbers_total += 1
            if has(text, alternatives):
                numbers += 1
            else:
                missed.append(f"nombre {alternatives[0]}")
        for product in expected_products:
            products_total += 1
            if has_product(text, product):
                products += 1
            else:
                missed.append(f"produit {product}")
        if missed:
            misses.append((phrase_id, ", ".join(missed), row["text"]))
    seconds = [float(r["seconds"]) for r in rows]
    return {
        "client": (client, client_total), "numbers": (numbers, numbers_total),
        "products": (products, products_total),
        "median_s": statistics.median(seconds), "max_s": max(seconds), "misses": misses,
    }


def pct(pair):
    return f"{pair[0]}/{pair[1]} ({100 * pair[0] / pair[1]:.0f} %)"


def main():
    detail = "--detail" in sys.argv
    results = {path.stem: score(list(csv.DictReader(path.open(encoding="utf-8"))))
               for path in sorted(RESULTS_DIR.glob("*.csv"))}
    print(f"| Moteur | Client juste | Nombres justes | Produits justes | Durée médiane | Durée max |")
    print("|---|---|---|---|---|---|")
    for engine, s in results.items():
        print(f"| {engine} | {pct(s['client'])} | {pct(s['numbers'])} | {pct(s['products'])} "
              f"| {s['median_s']:.2f} s | {s['max_s']:.2f} s |")
    if detail:
        for engine, s in results.items():
            print(f"\n### {engine}")
            for phrase_id, missed, text in s["misses"]:
                print(f"- {phrase_id} — {missed} — « {text} »")


if __name__ == "__main__":
    main()
