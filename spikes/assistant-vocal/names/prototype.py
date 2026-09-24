"""Prototype Python de la reconnaissance des clients (spike assistant vocal, étape 2).

Même rôle et mêmes règles de prudence que CustomerNameMatcher (C#), avec des briques toutes
faites à la place du code écrit à la main :
- phonétique : espeak-ng (vraie prononciation en API) au lieu des règles de FrenchPhonetic ;
- comparaison : rapidfuzz (distance d'édition sur l'API) ;
- noms inconnus : les entités « personne » ou « lieu » de spaCy (fr_core_news_md), en plus de
  la règle des civilités.

Évalué sur evaluation.json, le même jeu que les tests C#. Tout s'exécute dans un venv ; voir README.

    python prototype.py [--detail]
"""

import json
import re
import sys
import time
import unicodedata
from pathlib import Path

import espeakng_loader
import spacy
from phonemizer.backend import EspeakBackend
from phonemizer.backend.espeak.wrapper import EspeakWrapper
from rapidfuzz.distance import Levenshtein

EspeakWrapper.set_library(espeakng_loader.get_library_path())
EspeakWrapper.set_data_path(espeakng_loader.get_data_path())

HERE = Path(__file__).resolve().parent
UNKNOWN = "[CLIENT_INCONNU]"

CIVILITIES = {"madame", "mme", "monsieur", "m", "mr", "mademoiselle", "mlle"}
PREPOSITIONS = {"a", "pour", "aux", "au", "chez"}
ARTICLES = {"la", "le", "les"}
# Même liste que la version C#, pour ne comparer que les briques.
COMMON = set("""un une deux trois quatre cinq six sept huit neuf dix onze douze vingt trente quarante
cinquante soixante cent cents mille demi saucisson saucissons jambon jambons terrine terrines tranche
tranches gramme grammes kilo kilos livre livres euro euros stock vends vend vendu vendre mets met donne
coupe pris prend annule paye payee payera paiera reste restent entier entame gros petit il elle ils elles
on nous vous je j tu me moi toi lui eux de du des d l s n qu que quoi et ou en y ce ces ses son sa mes mon
ma est ai as avons a combien encore environ semaine prochaine derniere dernier vente liquide chansons
enfants non oui bon alors euh attends dis voir quelque chose""".split())

EXACT, SAME_SOUND, CLOSE_SOUND = 1.0, 0.9, 0.7


def normalize(text):
    text = unicodedata.normalize("NFKD", text.lower())
    return "".join(c for c in text if not unicodedata.combining(c))


class Phonemes:
    """Prononciation API par espeak-ng, mise en cache (un appel espeak coûte quelques millisecondes)."""

    def __init__(self):
        self._backend = EspeakBackend("fr-fr", language_switch="remove-flags")
        self._cache = {}

    def __call__(self, text):
        if text not in self._cache:
            ipa = self._backend.phonemize([text], strip=True)[0]
            self._cache[text] = re.sub(r"[\sˈˌː-]", "", ipa)
        return self._cache[text]


class Matcher:
    def __init__(self, customers, nlp, phonemes):
        self.nlp, self.ipa = nlp, phonemes
        self.forms = []
        for c in customers:
            self._add(c, "last", c["last_name"])
            if c["first_name"]:
                self._add(c, "first", c["first_name"])
                self._add(c, "full", f"{c['first_name']} {c['last_name']}")
                self._add(c, "full", f"{c['last_name']} {c['first_name']}")

    def _add(self, customer, kind, value):
        joined = "".join(re.findall(r"\w+", normalize(value)))
        self.forms.append((customer["id"], kind, joined, self.ipa(value)))

    def _score(self, heard, heard_ipa, form, form_ipa):
        if heard == form:
            return EXACT
        if heard_ipa == form_ipa:
            return SAME_SOUND
        if min(len(heard_ipa), len(form_ipa)) >= 4 and Levenshtein.distance(heard_ipa, form_ipa) == 1:
            return CLOSE_SOUND
        return 0

    def _decide(self, words, in_context, after_civility):
        heard = "".join(w["norm"] for w in words)
        heard_ipa = self.ipa(" ".join(w["text"] for w in words))
        best_by_customer = {}
        for customer_id, kind, form, form_ipa in self.forms:
            score = self._score(heard, heard_ipa, form, form_ipa)
            if score >= (CLOSE_SOUND if in_context else EXACT) and score > best_by_customer.get(customer_id, (0,))[0]:
                best_by_customer[customer_id] = (score, kind)
        if not best_by_customer:
            return None
        ranked = sorted(best_by_customer.items(), key=lambda kv: -kv[1][0])
        best_id, (best_score, best_kind) = ranked[0]
        if any(score >= best_score - 0.1 for _, (score, _) in ranked[1:]):
            return ("ambiguous", None)
        confusable = len(heard_ipa) >= 4 and any(
            cid != best_id and kind != "full" and Levenshtein.distance(heard_ipa, f_ipa) <= 2
            for cid, kind, _, f_ipa in self.forms)
        if confusable and not (best_score == EXACT and (best_kind == "full" or (best_kind == "last" and after_civility))):
            return ("ambiguous", None)
        return ("matched", best_id)

    def pseudonymize(self, text):
        doc = self.nlp(text)
        # Mots où spaCy voit une personne ou un lieu : candidats « nom » même sans majuscule.
        entity_chars = set()
        for ent in doc.ents:
            if ent.label_ in ("PER", "LOC"):
                entity_chars.update(range(ent.start_char, ent.end_char))

        words = [{"start": m.start(), "end": m.end(), "text": m.group(), "norm": normalize(m.group())}
                 for m in re.finditer(r"[^\W\d_]+", text)]
        out, mentions, cursor, count, i = [], [], 0, 0, 0
        while i < len(words):
            w = words[i]
            prev = words[i - 1]["norm"] if i else None
            skip = w["norm"] in COMMON | CIVILITIES | PREPOSITIONS | ARTICLES
            found = None
            if not skip:
                after_civility = prev in CIVILITIES
                sentence_start = i == 0 or any(p in text[words[i - 1]["end"]:w["start"]] for p in ".!?,")
                in_context = after_civility or sentence_start or prev in PREPOSITIONS | ARTICLES
                for length in (2, 1):
                    window = words[i:i + length]
                    if len(window) < length or any(x["norm"] in COMMON for x in window[1:]):
                        continue
                    decision = self._decide(window, in_context, after_civility)
                    if decision:
                        found = (length, *decision)
                        break
                is_entity = w["start"] in entity_chars
                capitalized = w["text"][0].isupper() and not sentence_start
                if not found and (after_civility or (in_context and prev and (capitalized or is_entity))):
                    length = 1
                    while (i + length < len(words) and words[i + length]["norm"] not in COMMON
                           and (words[i + length]["start"] in entity_chars or words[i + length]["text"][0].isupper())):
                        length += 1
                    found = (length, "unknown", None)
            if found:
                length, status, customer_id = found
                first = i - 1 if i and (prev in CIVILITIES or prev in ARTICLES) else i
                start, end = words[first]["start"], words[i + length - 1]["end"]
                token = f"[CLIENT_{count + 1}]" if status == "matched" else UNKNOWN
                count += status == "matched"
                out.append(text[cursor:start] + token)
                cursor = end
                mentions.append({"status": status, "customer_id": customer_id})
                i += length
            else:
                i += 1
        out.append(text[cursor:])
        return "".join(out), mentions


def main():
    detail = "--detail" in sys.argv
    data = json.loads((HERE / "evaluation.json").read_text(encoding="utf-8"))
    matcher = Matcher(data["customers"], spacy.load("fr_core_news_md"), Phonemes())

    wrong, by_source, timings = [], {}, []
    for row in data["rows"]:
        started = time.perf_counter()
        text, mentions = matcher.pseudonymize(row["text"])
        timings.append(time.perf_counter() - started)
        chosen = [m["customer_id"] for m in mentions if m["status"] == "matched"]
        s = by_source.setdefault(row["source"], {"expected": 0, "found": 0, "unknown": 0, "leaks": 0, "alarms": 0, "lines": []})
        wrong += [f"{row['source']} {row['id']} « {row['text']} » → client {c}" for c in chosen if c not in row["truth"]]
        s["expected"] += len(row["truth"])
        s["found"] += sum(t in chosen for t in row["truth"])
        s["unknown"] += len(row["unknown"])
        leaked = [n for n in row["unknown"] if normalize(n) in normalize(text)]
        s["leaks"] += len(leaked)
        alarm = not row["truth"] and not row["unknown"] and mentions
        s["alarms"] += bool(alarm)
        if any(t not in chosen for t in row["truth"]) or leaked or alarm:
            s["lines"].append(f"  {row['source']} {row['id']} : « {row['text']} » → « {text} »")

    for source, s in by_source.items():
        if detail:
            print("\n".join(s["lines"]))
        print(f"{source} : {s['found']}/{s['expected']} clients trouvés, "
              f"{s['leaks']}/{s['unknown']} noms inconnus restés dans le texte, {s['alarms']} fausses alertes")
    timings.sort()
    print(f"Durée par phrase : médiane {1000 * timings[len(timings) // 2]:.0f} ms, max {1000 * timings[-1]:.0f} ms")
    print("Mauvais client choisi :", "aucun" if not wrong else "\n" + "\n".join(wrong))


if __name__ == "__main__":
    main()
