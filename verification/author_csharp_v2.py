"""Build the reviewed, finite C# lesson data. Never evaluates learner input."""
import importlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UNITS = {}
GAME_HINTS = {
    "counterexample": "مقدارهای مرزی و ورودی‌هایی را مقایسه کن که ادعای مسئله دربارهٔ آن‌ها نتیجهٔ متفاوتی می‌دهد.",
    "repair": "نام تست‌ها را بخوان؛ فقط جایگاه‌های لازم را تغییر بده و نسخهٔ تازه را صریحاً آزمایش کن.",
    "simulation": "وضعیت آغاز و هدف را مقایسه کن؛ اثر هر اقدام را روی وضعیت فعلی دنبال کن.",
    "pipeline": "خروجی هر مرحله، ورودی مرحلهٔ بعد است؛ ترتیب و تعداد عملیات را بررسی کن.",
    "groups": "ویژگی مشترک کارت‌ها را پیدا کن؛ شباهت نوشتاری به‌تنهایی برای هم‌گروه بودن کافی نیست.",
    "questions": "برای هر پاسخ بله یا خیر، نامزدهای ناسازگار را خودت کنار بگذار؛ پرسشی را انتخاب کن که بین نامزدهای باقی‌مانده فرق بگذارد.",
    "contradiction": "گزاره‌ها را دربارهٔ همان داده و همان اجرا بخوان؛ مجموعه وقتی حداقلی است که با حذف هر عضو، تناقض از بین برود.",
    "argument": "شاهد باید ادعا را پشتیبانی کند و دلیل باید ارتباط آن دو را توضیح بدهد.",
    "analogy": "رابطهٔ جفت مرجع را با همان جهت روی جفت دوم اعمال کن؛ نقش دو عضو را جابه‌جا نکن.",
    "constraints": "وابستگی‌ها را از صورت مسئله پیدا کن؛ هر قطعه باید پس از پیش‌نیازهای خودش قرار بگیرد.",
    "domino": "معنای خروجی کارت فعلی و ورودی کارت بعدی را مقایسه کن؛ جهت اتصال و کارت‌های لازم را نگه دار.",
    "spectrum": "از معیار و لنگرهای مسئله مقدار را محاسبه یا برآورد کن؛ نتیجهٔ آزمایش را برای اصلاح مقدار به کار ببر.",
    "concept": "ویژگی‌های مطابق و متفاوتِ حدس‌های ثبت‌شده را با هم مقایسه کن؛ فرصت‌ها را روی گزینهٔ تکراری مصرف نکن.",
    "experiment": "قانونی را انتخاب کن که تمام مشاهده‌های آزمایش‌های بازشده را توضیح بدهد.",
    "budget": "هزینه و پوشش هر کارت را هم‌زمان بسنج؛ همهٔ هدف‌ها باید در همان بودجه پوشش داده شوند.",
    "mission": "هر تصمیم را با قرارداد مسئله و پیامد مرحلهٔ فعلی بسنج؛ مسیر را تا پایان ادامه بده.",
    "assessment": "هر ردیف را مستقل بخوان و قرارداد آن را با گزینهٔ مناسب تطبیق بده.",
    "deduction": "سرنخ‌های قطعی را در جدول علامت بزن و سازگاری تمام ردیف‌ها را با هم بررسی کن.",
    "slots": "نقش هر جایگاه را از صورت مسئله مشخص کن؛ پرشدن جایگاه به‌تنهایی به معنی درست بودن آن نیست.",
    "trace": "وضعیت را قدم‌به‌قدم به‌روز کن؛ مقدار یک قدم را به‌جای مقدار قدم بعد ننویس.",
}


def activity(kind, title, prompt, options=(), answers=(), **fields):
    return dict(Id=kind, Kind=kind, Title=title, Prompt=prompt, Options=list(options), Answers=list(answers), **fields)


def constraints(title, prompt, cards, edges):
    return activity("constraints", title, prompt, cards, Puzzle=dict(Edges=edges))


def repair(title, prompt, options, initial, tests, cases, rows=("جایگاه قابل تغییر",), code="", limit=1):
    return activity("repair", title, prompt, options, Rows=list(rows), Code=code,
                    Puzzle=dict(Initial=initial, Limit=limit, Tests=tests,
                                Cases=[dict(Input=i, Passed=p) for i, p in cases]))


def counter(title, claim, options, observations):
    return activity("counterexample", title, claim, options,
                    Puzzle=dict(Cases=[dict(Input=[i], Actual=a, Expected=e, Success=s, Reason=r)
                                       for i, (a, e, s, r) in enumerate(observations)]))


def simulation(title, prompt, actions, states, transitions, targets, limit=6):
    return activity("simulation", title, prompt, actions,
                    Puzzle=dict(States=states, Transitions=transitions, Targets=targets, Limit=limit))


def groups(title, prompt, labels, members):
    cards = [card for row in zip(*members) for card in row]
    return activity("groups", title, prompt, cards, Multiple=True,
                    Puzzle=dict(Groups=[[cards.index(c) for c in g] for g in members], Labels=labels, Limit=4))


def questions(title, prompt, candidates, features, traits, target, limit=3):
    return activity("questions", title, prompt, candidates,
                    Puzzle=dict(Questions=features, Traits=traits, Target=target, Limit=limit))


def conflict(title, prompt, cards, cases):
    return activity("contradiction", title, prompt, cards, Multiple=True,
                    Puzzle=dict(Cases=[dict(Input=i, Removals=r, Reason=why) for i, r, why in cases]))


def argument(title, prompt, cards, combinations, rows=("ادعا", "شاهد", "دلیل")):
    return activity("argument", title, prompt, cards, Rows=list(rows),
                    Puzzle=dict(Cases=[dict(Input=c, Success=True) for c in combinations]))


def analogy(title, prompt, cards, combinations):
    result = argument(title, prompt, cards, combinations, ("رابطه", "عضو اول جفت دوم", "عضو دوم جفت دوم"))
    result.update(Id="analogy", Kind="analogy")
    return result


def domino(title, prompt, cards, edges, target, required, limit=6):
    return activity("domino", title, prompt, cards,
                    Puzzle=dict(Edges=edges, Start=0, Target=target, Required=required, Limit=limit))


def spectrum(title, prompt, criterion, minimum, maximum, low, high, anchors, step=1, limit=4):
    return activity("spectrum", title, prompt, ["جایگاه"],
                    Puzzle=dict(Criterion=criterion, Min=minimum, Max=maximum, Low=low, High=high,
                                Anchors=anchors, Step=step, Limit=limit))


def pipeline(title, prompt, operations, states, transitions, targets, limit=4, repeat=False):
    result = simulation(title, prompt, operations, states, transitions, targets, limit)
    result.update(Id="pipeline", Kind="pipeline")
    result["Puzzle"]["Repeat"] = repeat
    return result


def slots(title, prompt, rows, options, mapping):
    return activity("slots", title, prompt, options, [f"{r}:{c}" for r, c in enumerate(mapping)], Rows=rows)


def experiment(title, prompt, results, rules, answer, limit=3):
    return activity("experiment", title, prompt, rules, [str(answer)], Game=dict(Results=results, Limit=min(limit, len(results))))


def concept(title, prompt, candidates, features, traits, target, limit=4):
    return activity("concept", title, prompt, candidates, [str(target)], Game=dict(Features=features, Traits=traits, Limit=limit))


def assessment(title, prompt, claims, mapping):
    return activity("assessment", title, prompt, ["درست است", "باید اصلاح شود"], [f"{i}:{0 if valid else 1}" for i, valid in enumerate(mapping)], Rows=claims, Game={})


def budget(title, prompt, cards, costs, covers, goals, limit, reference):
    return activity("budget", title, prompt, cards, list(map(str, reference)), Multiple=True,
                    Game=dict(Costs=costs, Covers=covers, Goals=goals, Limit=limit))


def mission(title, prompt, steps):
    nodes = []
    for i, (text, good, bad) in enumerate(steps):
        choices=[good,bad];destinations=[i+1,len(steps)+1]
        if i%2==1:choices.reverse();destinations.reverse()
        nodes.append(dict(Text=text, Choices=choices, Next=destinations))
    nodes.extend([dict(Text="مسیر با قرارداد مسئله سازگار است.", Success=True), dict(Text="این تصمیم قرارداد را نقض کرد؛ علت را بررسی کن و دوباره شروع کن.")])
    return activity("mission", title, prompt, [], Game=dict(Nodes=nodes, Limit=len(steps)))


def unit(number, summary, example, output, quiz, game, workshop, reference, rubric, *, extra=()):
    assert number not in UNITS
    UNITS[number] = dict(summary=summary, example=example, output=output, quiz=quiz, game=game,
                         workshop=workshop, reference=reference, rubric=rubric, extra=extra)


def finish():
    roadmap = (ROOT / "csharp-mastery-roadmap.md").read_text(encoding="utf-8")
    lessons = []
    phase = 0
    for line in roadmap.splitlines():
        if line.startswith("## فاز "):
            phase += 1
            phase_title = line.split(". ", 1)[1]
        match = re.match(r"\| R(\d{3}) \| (.*?) \| (.*?) \| (.*?) \| (.*?) \|$", line)
        if not match or phase > 11:
            continue
        number = int(match[1])
        if number in [int(l["Id"][1:]) for l in lessons]:
            continue
        data = UNITS[number]
        question, correct, wrong1, wrong2, reason = data["quiz"]
        options = [correct, wrong1, wrong2]
        shift = number % 3
        options = options[shift:] + options[:shift]
        decision = activity("choice", "تصمیم در موقعیت تازه", question, options, [str(options.index(correct))],
                            Explanation=reason, Hint="به قرارداد مثال و تفاوت گزینه‌ها توجه کن.", Solution=reason)
        decision["Id"] = "decide"
        output = data["output"]
        if re.fullmatch(r"-?\d+", output):
            distractor = str(int(output) + 1)
        elif "True" in output or "False" in output:
            distractor = output.replace("True", "TEMP").replace("False", "True").replace("TEMP", "False")
        elif "\n" in output and len(set(output.splitlines())) > 1:
            distractor = "\n".join(reversed(output.splitlines()))
        else:
            distractor = "هیچ خروجی‌ای چاپ نمی‌شود."
        observe_options = [output, distractor, "خطای زمان اجرا پیش از چاپ نتیجه رخ می‌دهد."]
        shift = (number + 1) % 3
        observe_options = observe_options[shift:] + observe_options[:shift]
        observe = activity("choice", "پیش‌بینی مثال", "پیش از بازکردن پاسخ، نتیجهٔ مثال زیر را پیش‌بینی کن. سپس علت را با مفهوم این واحد توضیح بده.", observe_options,
                           [str(observe_options.index(data["output"]))], Code=data["example"], Explanation=data["summary"],
                           Hint=reason, Solution=data["output"] + "\n" + reason)
        observe["Id"] = "observe"
        game = data["game"]
        for challenge in [game, *data["extra"]]:
            if "Solution" not in challenge:
                challenge["Solution"] = solution(challenge)
            challenge.setdefault("Explanation", challenge["Solution"])
            challenge.setdefault("Hint", GAME_HINTS.get(challenge["Kind"], reason))
        workshop = activity("workshop", "کارگاه مستقل: " + match[2], data["workshop"] +
                            "\nکد را در پروژهٔ محلی .NET 10 اجرا کن. ورودی، خروجی واقعی و نتیجهٔ بررسی حالت مرزی را همراه کد ثبت کن. " +
                            ("برای منطق تست خودکار بنویس؛ تست را با یک تغییر عمدی خراب کن و سپس اصلاح کن." if number >= 31 else "با خروجی مورد انتظار یا assert درستی را بررسی کن.") +
                            "\nبا یک مثال از خودت، دلیل انتخاب راه‌حل را توضیح بده. ثبت شخصی فقط ثبت انجام تمرین است و صحت کد را تأیید نمی‌کند.",
                            Explanation=data["rubric"], Hint=reason, Solution=data["rubric"] + "\nاین نمونه یک پاسخ مرجع است؛ راه‌حل هم‌رفتار معتبر است.", SolutionCode=data["reference"], ReferenceFiles=data.get("files",{}))
        activities = [observe, game, decision, *data["extra"]]
        if number in (20,32,43,55,67,79,94,105,114,123):
            activities.append(activity("explain", "دفاع از تصمیم", "با مثال تازه از پروژهٔ خودت توضیح بده: " + question + "\nیک حالت مرزی و یک راه‌حل نامناسب را هم بررسی کن.", Explanation=data["rubric"], Hint=reason, Solution=reason + "\n" + data["summary"]))
        activities.append(workshop)
        lessons.append(dict(Id=f"R{number:03}", Title=match[2], Phase=phase, PhaseTitle=phase_title,
                            Prerequisites=re.findall(r"R\d{3}", match[3]), Topics=match[4].replace("`", ""), Objective=match[5],
                            Summary=data["summary"], Example=data["example"], ExampleOutput=data["output"],
                            Activities=activities))
    assert [l["Id"] for l in lessons] == [f"R{i:03}" for i in range(1, 124)]
    (ROOT / "ILearn/Content/csharp-v2.json").write_text(json.dumps(lessons, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    coverage=["# پوشش اجرایی مسیر C#", "", "این جدول از محتوای اجرایی تولید می‌شود؛ آزمون CSharpV2Tests ترتیب،هدف،پیش‌نیاز و ریزمباحث را با رودمپ تطبیق می‌دهد.", "", "| واحد | عنوان | فاز | بازی محوری | چالش | XP |", "|---|---|---|---|---|---|"]
    for lesson in lessons:
        coverage.append(f"| {lesson['Id']} | {lesson['Title']} | {lesson['Phase']} | {lesson['Activities'][1]['Kind']} | {len(lesson['Activities'])} | {sum(30 if a['Kind']=='workshop' else 10 for a in lesson['Activities'])} |")
    (ROOT/"verification/csharp-v2-coverage.md").write_text("\n".join(coverage)+"\n",encoding="utf-8")
    print(f"Authored {len(lessons)} units, {sum(len(l['Activities']) for l in lessons)} activities")


def solution(game):
    p = game.get("Puzzle")
    if not p:
        if game["Kind"] == "mission":
            nodes=game["Game"]["Nodes"];queue=[(0,[])]
            while queue:
                index,path=queue.pop(0);node=nodes[index]
                if node.get("Success"):return "\n".join(path)
                if len(path)<game["Game"]["Limit"]:
                    queue.extend((target,path+[node["Choices"][i]]) for i,target in enumerate(node.get("Next",[])))
            raise AssertionError("Unsolvable mission")
        if game["Kind"] in ("slots", "assessment", "trace", "deduction"):
            return "\n".join(game["Rows"][int(key.split(":")[0])] + ": " + game["Options"][int(key.split(":")[1])] for key in game["Answers"])
        return "\n".join(game["Options"][int(key)] for key in game["Answers"])
    kind = game["Kind"]
    names = game["Options"]
    if kind == "counterexample":
        return "\n".join(names[c["Input"][0]] + ": " + c["Reason"] for c in p["Cases"] if c["Success"])
    if kind == "repair":
        return "\n".join("، ".join(names[i] for i in c["Input"]) for c in p["Cases"] if all(c["Passed"]) and sum(x != y for x, y in zip(c["Input"], p["Initial"])) <= p["Limit"])
    if kind == "groups":
        return "\n".join(label + ": " + "، ".join(names[i] for i in group) for label, group in zip(p["Labels"], p["Groups"]))
    if kind == "questions":
        return "هدف: " + names[p["Target"]] + "\n" + "\n".join(q + ": " + ("بله" if b else "خیر") for q, b in zip(p["Questions"], p["Traits"][p["Target"]]))
    if kind == "contradiction":
        return "\n".join("، ".join(names[i] for i in c["Input"]) + "؛ " + c["Reason"] + " حذف مجاز: " + " / ".join(names[i] for i in c["Removals"]) for c in p["Cases"])
    if kind in ("argument", "analogy"):
        return "\n".join("؛ ".join(row + ": " + names[i] for row, i in zip(game["Rows"], c["Input"])) for c in p["Cases"] if c["Success"])
    if kind == "spectrum":
        return f"بازهٔ معتبر: {p['Low']} تا {p['High']}. " + p["Criterion"]
    if kind == "constraints":
        order = []
        while len(order) < len(names):
            ready = next(i for i in range(len(names)) if i not in order and all(a in order for a, b in p["Edges"] if b == i))
            order.append(ready)
        return "یک ترتیب معتبر:\n" + "\n".join(names[i] for i in order) + "\nهر ترتیب رعایت‌کنندهٔ همهٔ قیدها پذیرفته است."
    queue = [(p.get("Start", 0), [])]
    while queue:
        state, path = queue.pop(0)
        if path and ((kind == "domino" and state == p["Target"] and all(i == p.get("Start", 0) or i in path for i in p["Required"])) or (kind != "domino" and state in p["Targets"])):
            return "یک مسیر معتبر:\n" + "\n".join(names[i] for i in path)
        if len(path) >= p["Limit"]:
            continue
        if kind == "domino":
            queue.extend((b, path + [b]) for a, b in p["Edges"] if a == state and b not in [p.get("Start", 0)] + path)
        else:
            queue.extend((target, path + [i]) for i, target in enumerate(p["Transitions"][state]) if target >= 0 and (kind != "pipeline" or p.get("Repeat") or i not in path))
    raise AssertionError("Unsolvable: " + game["Title"])


if __name__ == "__main__":
    import sys
    sys.modules["author_csharp_v2"] = sys.modules[__name__]
    for phase in range(1, 12):
        importlib.import_module(f"csharp_content.phase{phase:02}")
    importlib.import_module("csharp_content.enrichment")
    importlib.import_module("csharp_content.workshops")
    importlib.import_module("csharp_content.feature_examples")
    finish()
