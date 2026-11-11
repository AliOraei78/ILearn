"""Inventory repeated questions and positional answer references for review."""

from collections import defaultdict
from difflib import SequenceMatcher
from itertools import combinations
import json
from pathlib import Path
import re
import sys
from unicodedata import normalize


ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / "ILearn/Content"
WORDS = re.compile(r"[\w\u200c]+")
NUMBERS = re.compile(r"\d+(?:[.٫]\d+)?")
POSITION = re.compile(
    r"گزینه[ٔ‌یی]?\s*(?:اوّل|اول|دوم|سوم|چهارم|پنجم|آخر|بعدی|قبلی|[\d]+)"
    r"|(?:اوّل|اول|دوم|سوم|چهارم|آخر)ین گزینه"
)
FENCE = re.compile(r"^```([^\n]*)\n(.*?)^```[ \t]*$", re.MULTILINE | re.DOTALL)
CODE = re.compile(r'"(?:\\.|[^"\\])*"|[A-Za-z_]\w*|\d+(?:\.\d+)?[mMdDlLfF]?|\+\+|--|=>|==|!=|<=|>=|&&|\|\||\?\?|\S')
FRAGMENT = re.compile(r"""[A-Za-z0-9_@"'(+-][A-Za-z0-9_@"'.()[\]{},;=?!%*/<>|&^~:+\-\t ]*""")


def read(name):
    return json.loads((CONTENT / name).read_text(encoding="utf-8-sig"))


def words(value):
    value = normalize("NFKC", value).translate(str.maketrans("كي", "کی"))
    return tuple(WORDS.findall(value.replace("\u200c", " ").replace("\u200f", "")))


def question_records(teaching):
    revisions = read("revisions.json")
    for filename in ("csharp-v2.json", "lessons.json"):
        for chapter in read(filename):
            lesson = chapter["Id"]
            for index, session in enumerate(teaching[lesson], 1):
                key = f"{lesson}/session-{index}"
                yield lesson, key, session["Question"], ""
                if "Practice" in session:
                    yield lesson, key + "/practice", session["Practice"]["Prompt"], ""
            for activity in chapter["Activities"]:
                key = f"{lesson}/{activity['Id']}"
                prompt = revisions.get(key, activity)["Prompt"]
                yield lesson, key, prompt, activity.get("Code", "")
    for exercise in read("code-room.json"):
        activity = exercise["Activity"]
        key = f"code-room/{exercise['LessonId']}/{activity['Id']}"
        yield exercise["LessonId"], key, activity["Prompt"], activity.get("Code", "")
    for bank in read("interviews.json"):
        for item in bank["Questions"]:
            activity = item["Activity"]
            yield bank["Id"], f"interview/{activity['Id']}", activity["Prompt"], activity.get("Code", "")


def positional_references(value, path=()):
    if isinstance(value, dict):
        for key, child in value.items():
            yield from positional_references(child, (*path, key))
    elif isinstance(value, list):
        for index, child in enumerate(value, 1):
            key = child.get("Id", index) if isinstance(child, dict) else index
            yield from positional_references(child, (*path, str(key)))
    elif isinstance(value, str) and path[-1] in {"Feedback", "Solution", "Explanation", "Hint", "Criteria", "Reason"}:
        if POSITION.search(value):
            yield {"location": "/".join(path), "text": value}


def lexical_candidates(records):
    by_prompt = defaultdict(list)
    by_lesson = defaultdict(list)
    phrases = defaultdict(set)
    normalized = {}
    for lesson, key, prompt, code in records:
        fingerprint = words(prompt)
        shape = tuple(NUMBERS.sub("#", token) for token in fingerprint)
        by_prompt[fingerprint].append((key, prompt, bool(code)))
        by_lesson[lesson].append((key, fingerprint))
        normalized[key] = (lesson, fingerprint, shape)
        if len(shape) >= 7:
            for offset in range(len(shape) - 3):
                phrases[shape[offset:offset + 4]].add(key)
    repeated = [
        {"locations": [key for key, _, _ in items], "prompt": items[0][1], "all_have_code": all(item[2] for item in items)}
        for items in by_prompt.values()
        if len(items) > 1
    ]
    pairs = {tuple(sorted((left, right))) for items in by_lesson.values()
             for (left, _), (right, _) in combinations(items, 2)}
    for group in phrases.values():
        pairs.update(combinations(sorted(group), 2))

    within, across = [], []
    for left, right in sorted(pairs):
        lesson, a, shape_a = normalized[left]
        other_lesson, b, shape_b = normalized[right]
        if a == b or min(len(a), len(b)) < 5:
            continue
        same_lesson = lesson == other_lesson
        score = SequenceMatcher(None, shape_a, shape_b, autojunk=False).ratio()
        if score >= (.69 if same_lesson else .81):
            (within if same_lesson else across).append({"locations": [left, right], "score": round(score, 3)})
    return repeated, sorted(within, key=lambda item: -item["score"]), sorted(across, key=lambda item: -item["score"])


def code_fragments(text, fenced=False):
    if fenced:
        lines = [line for tag, block in FENCE.findall(text) if tag.startswith(("csharp", "cs-snippet", "file-csharp"))
                 for line in block.splitlines()]
    else:
        lines = FRAGMENT.findall(text)
    for line in lines:
        line = line.strip().rstrip(";")
        if line.startswith("Console.WriteLine(") and line.endswith(")"):
            line = line[len("Console.WriteLine("):-1]
        tokens = CODE.findall(line)
        if len(tokens) >= 3 and re.search(r"[=<>+*/%!?:()\[\]]", line):
            yield line, tuple(tokens)


def code_candidates(teaching, records):
    examples = defaultdict(list)
    for chapter, sessions in teaching.items():
        for index, session in enumerate(sessions, 1):
            for line, tokens in code_fragments(session["Text"], fenced=True):
                examples[chapter].append((f"{chapter}/session-{index}/example", line, tokens))
    matches = set()
    for chapter, key, prompt, code in records:
        for fragment, tokens in code_fragments(prompt + "\n" + code):
            shape = tuple(NUMBERS.sub("#", token) if token[0].isdigit() else token for token in tokens)
            for source, example, other in examples[chapter]:
                same_literals = tokens == other
                same_shape = shape == tuple(NUMBERS.sub("#", token) if token[0].isdigit() else token for token in other)
                if same_literals or same_shape:
                    matches.add((key, source, fragment, example, "exact" if same_literals else "number variant"))
    return [
        {"question": key, "example": source, "question_fragment": fragment, "example_fragment": sample, "match": match}
        for key, source, fragment, sample, match in sorted(matches)
    ]


def repeated_question_code(records):
    groups = defaultdict(list)
    for _, key, _, code in records:
        tokens = CODE.findall(code)
        if len(tokens) < 8:
            continue
        shape = tuple(
            '"…"' if token.startswith('"') else NUMBERS.sub("#", token) if token[0].isdigit() else token
            for token in tokens
        )
        groups[shape].append((key, tokens))
    return [
        {"locations": [key for key, _ in items],
         "match": "exact" if all(tokens == items[0][1] for _, tokens in items) else "literal variant"}
        for items in groups.values()
        if len(items) > 1
    ]


def prose_matches_with_teaching(teaching, records):
    sentences = defaultdict(list)
    for chapter, sessions in teaching.items():
        for index, session in enumerate(sessions, 1):
            prose = FENCE.sub("", session["Text"])
            for sentence in re.split(r"(?<=[.!؟?])\s+|\n+", prose):
                if sentence.lstrip().startswith("|"):
                    continue
                fingerprint = words(sentence)
                if len(fingerprint) >= 7:
                    sentences[chapter].append((index, sentence, fingerprint))

    matches = []
    for chapter, key, prompt, _ in records:
        fingerprint = words(prompt)
        if len(fingerprint) < 7:
            continue
        for index, sentence, other in sentences[chapter]:
            matcher = SequenceMatcher(None, fingerprint, other, autojunk=False)
            if matcher.quick_ratio() < .73:
                continue
            score = matcher.ratio()
            if score >= .73:
                matches.append({"question": key, "example": f"{chapter}/session-{index}/text",
                                "sentence": sentence, "score": round(score, 3)})
    return sorted(matches, key=lambda item: -item["score"])


def workshop_answers_in_teaching(teaching):
    revisions = read("revisions.json")
    matches = []
    for filename in ("csharp-v2.json", "lessons.json"):
        for chapter in read(filename):
            lesson = chapter["Id"]
            samples = [
                (index, tuple(CODE.findall(code)))
                for index, session in enumerate(teaching[lesson], 1)
                for tag, code in FENCE.findall(session["Text"])
                if tag.startswith(("csharp", "cs-snippet", "file-csharp"))
            ]
            for activity in chapter["Activities"]:
                if activity["Kind"] != "workshop":
                    continue
                key = f"{lesson}/{activity['Id']}"
                solution = revisions.get(key, activity).get("SolutionCode", "")
                tokens = CODE.findall(solution)
                if len(tokens) < 12:
                    continue
                for index, example in samples:
                    longest = SequenceMatcher(None, tokens, example, autojunk=False).find_longest_match().size
                    if longest / len(tokens) >= .75:
                        matches.append({"question": key, "example": f"{lesson}/session-{index}/example",
                                        "answer_tokens": len(tokens), "matched_tokens": longest})
    return matches


def main():
    teaching = read("teaching.json")
    records = list(question_records(teaching))
    repeated, similar, across = lexical_candidates(records)
    positions = []
    for filename in ("teaching.json", "csharp-v2.json", "lessons.json", "revisions.json", "interviews.json", "code-room.json"):
        positions.extend(positional_references(read(filename), (filename,)))
    report = {
        "chapter_count": len(teaching),
        "session_count": sum(map(len, teaching.values())),
        "question_count": len(records),
        "positional_references": positions,
        "repeated_prompts": repeated,
        "similar_prompts_within_chapter": similar,
        "similar_prompts_across_chapters": across,
        "code_matches_with_teaching_examples": code_candidates(teaching, records),
        "repeated_question_code": repeated_question_code(records),
        "prose_matches_with_teaching": prose_matches_with_teaching(teaching, records),
        "workshop_answers_in_teaching": workshop_answers_in_teaching(teaching),
    }
    sys.stdout.reconfigure(encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
