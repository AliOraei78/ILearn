namespace ILearn.Domain.Learning;

public sealed class Puzzle
{
    public int Limit { get; set; } = 4;
    public int[] Initial { get; set; } = [];
    public PuzzleCase[] Cases { get; set; } = [];
    public string[] Tests { get; set; } = [];
    public string[] States { get; set; } = [];
    public int[][] Transitions { get; set; } = [];
    public int Start { get; set; }
    public int[] Targets { get; set; } = [];
    public bool Repeat { get; set; }
    public int[][] Groups { get; set; } = [];
    public string[] Labels { get; set; } = [];
    public string[] Questions { get; set; } = [];
    public bool[][] Traits { get; set; } = [];
    public int Target { get; set; }
    public int[][] Edges { get; set; } = [];
    public int[] Required { get; set; } = [];
    public int Min { get; set; }
    public int Max { get; set; } = 100;
    public int Step { get; set; } = 1;
    public int Low { get; set; }
    public int High { get; set; }
    public string Criterion { get; set; } = "";
    public string[] Anchors { get; set; } = [];
}

public sealed class PuzzleCase
{
    public int[] Input { get; set; } = [];
    public string Actual { get; set; } = "";
    public string Expected { get; set; } = "";
    public string Reason { get; set; } = "";
    public bool Success { get; set; }
    public bool[] Passed { get; set; } = [];
    public int[] Removals { get; set; } = [];
}

public static class Puzzles
{
    public static readonly string[] Kinds = ["counterexample", "repair", "simulation", "pipeline", "groups", "questions", "contradiction", "argument", "analogy", "constraints", "domino", "spectrum"];

    public static bool Numbers(IEnumerable<string> input, out int[] values)
    {
        var result = new List<int>();
        foreach (var value in input)
        {
            if (!int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var number)) { values = []; return false; }
            result.Add(number);
        }
        values = result.ToArray();
        return true;
    }

    static bool Index(int index, int length) => index >= 0 && index < length;
    static bool Set(IEnumerable<int> left, IEnumerable<int> right) => left.Order().SequenceEqual(right.Order());
    static bool Selection(Activity a, int[] values) => values.Length > 0 && values.Distinct().Count() == values.Length && values.All(i => Index(i, a.Options.Length));
    public static PuzzleCase? Configuration(Activity a, IEnumerable<string> input) => Numbers(input, out var values) ? a.Puzzle?.Cases.FirstOrDefault(c => c.Input.SequenceEqual(values)) : null;
    public static int RepairCost(Activity a, IEnumerable<string> input) => Numbers(input, out var values) && values.Length == a.Puzzle!.Initial.Length ? values.Where((v, i) => v != a.Puzzle.Initial[i]).Count() : int.MaxValue;

    public static int State(Activity a, IEnumerable<int> history)
    {
        var p = a.Puzzle!;
        var moves = history.ToArray();
        if (moves.Length > p.Limit || (a.Kind == "pipeline" && !p.Repeat && moves.Distinct().Count() != moves.Length)) return -1;
        var state = p.Start;
        foreach (var move in moves)
        {
            if (!Index(state, p.Transitions.Length) || !Index(move, p.Transitions[state].Length)) return -1;
            state = p.Transitions[state][move];
            if (!Index(state, p.States.Length)) return -1;
        }
        return state;
    }

    public static bool Path(Activity a, IEnumerable<int> history)
    {
        var p = a.Puzzle!;
        var cards = new[] { p.Start }.Concat(history).ToArray();
        return cards.Length <= p.Limit + 1 && cards.Distinct().Count() == cards.Length && cards.All(i => Index(i, a.Options.Length)) &&
            cards.Zip(cards.Skip(1)).All(pair => p.Edges.Any(e => e[0] == pair.First && e[1] == pair.Second));
    }

    public static bool Record(Activity a, MiniGameState state, int move)
    {
        if (a.Puzzle is not { } p) return false;
        if (a.Kind == "questions")
        {
            if (!Index(move, p.Questions.Length) || state.History.Count >= p.Limit || state.History.Contains(move) || state.History.Any(i => !Index(i, p.Questions.Length))) return false;
        }
        else if (a.Kind is "simulation" or "pipeline")
        {
            if (!Index(move, a.Options.Length) || State(a, state.History.Append(move)) < 0) return false;
        }
        else if (a.Kind == "domino")
        {
            if (!Path(a, state.History.Append(move))) return false;
        }
        else return false;
        state.History.Add(move);
        return true;
    }

    public static int[] SolvedGroups(Activity a, MiniGameState state) => a.Puzzle!.Groups.Select((g, i) => (g, i)).Where(x => state.Trials.Any(t => Set(t, x.g))).Select(x => x.i).ToArray();
    public static int GroupErrors(Activity a, MiniGameState state) => state.Trials.Count(t => !a.Puzzle!.Groups.Any(g => Set(t, g)));

    public static bool Try(Activity a, MiniGameState state, IEnumerable<string> input)
    {
        if (a.Puzzle is not { } p || !Numbers(input, out var values)) return false;
        if (a.Kind is "counterexample" or "repair")
        {
            if (!p.Cases.Any(c => c.Input.SequenceEqual(values)) || state.Trials.Any(t => t.SequenceEqual(values))) return false;
        }
        else if (a.Kind == "spectrum")
        {
            if (values.Length != 1 || !OnScale(p, values[0]) || state.Trials.Count >= p.Limit || state.Trials.Any(t => t.SequenceEqual(values))) return false;
        }
        else if (a.Kind == "groups")
        {
            var solved = SolvedGroups(a, state);
            if (!Selection(a, values) || values.Length != p.Groups[0].Length || GroupErrors(a, state) >= p.Limit ||
                values.Any(v => solved.Any(i => p.Groups[i].Contains(v))) || state.Trials.Any(t => Set(t, values))) return false;
        }
        else return false;
        state.Trials.Add(values);
        return true;
    }

    static bool OnScale(Puzzle p, int value) => value >= p.Min && value <= p.Max && ((long)value - p.Min) % p.Step == 0;

    public static bool FindConflict(Activity a, MiniGameState state, IEnumerable<string> input)
    {
        if (a.Kind != "contradiction" || a.Puzzle is not { } p || !Numbers(input, out var values) || !Selection(a, values)) return false;
        var found = Array.FindIndex(p.Cases, c => Set(c.Input, values));
        if (found < 0) return false;
        state.Found = found; state.Removed = -1;
        return true;
    }

    public static bool ValidState(Activity a, MiniGameState state)
    {
        if (a.Puzzle is not { } p) return false;
        if (a.Kind is "simulation" or "pipeline") return State(a, state.History) >= 0;
        if (a.Kind == "domino") return Path(a, state.History);
        if (a.Kind == "questions") return state.History.Count <= p.Limit && state.History.Distinct().Count() == state.History.Count && state.History.All(i => Index(i, p.Questions.Length));
        if (a.Kind == "counterexample") return state.Trials.Select(t=>string.Join(",",t)).Distinct().Count()==state.Trials.Count && state.Trials.All(t => p.Cases.Any(c => c.Input.SequenceEqual(t)));
        if (a.Kind == "repair") return state.History.Count == 0 && state.Trials.Count <= p.Cases.Length && state.Trials.Select(t=>string.Join(",",t)).Distinct().Count()==state.Trials.Count && state.Trials.All(t => p.Cases.Any(c => c.Input.SequenceEqual(t)));
        if (a.Kind == "spectrum") return state.Trials.Count <= p.Limit && state.Trials.Select(t=>string.Join(",",t)).Distinct().Count()==state.Trials.Count && state.Trials.All(t => t.Length == 1 && OnScale(p, t[0]));
        if (a.Kind == "groups")
        {
            if(state.Trials.Select(t=>string.Join(",",t.Order())).Distinct().Count()!=state.Trials.Count) return false;
            var used = new HashSet<int>(); var errors = 0;
            foreach (var trial in state.Trials)
            {
                if (errors >= p.Limit || !Selection(a, trial) || trial.Length != p.Groups[0].Length || trial.Any(used.Contains)) return false;
                if (p.Groups.Any(g => Set(g, trial))) used.UnionWith(trial); else errors++;
            }
            return true;
        }
        if (a.Kind == "contradiction") return state.Found == -1 ? state.Removed == -1 : Index(state.Found, p.Cases.Length) && (state.Removed == -1 || p.Cases[state.Found].Input.Contains(state.Removed));
        return state.History.Count == 0;
    }

    public static bool Check(Activity a, MiniGameState state, IEnumerable<string> input)
    {
        if (a.Puzzle is not { } p || !ValidState(a, state) || !Numbers(input, out var values)) return false;
        switch (a.Kind)
        {
            case "counterexample": return p.Cases.Any(c => c.Success && c.Input.SequenceEqual(values)) && state.Trials.Any(t => t.SequenceEqual(values));
            case "repair": var normalized=values.Select(i=>i.ToString()).ToArray(); return RepairCost(a, normalized) <= p.Limit && Configuration(a, normalized) is { Passed.Length: > 0 } c && c.Passed.All(x => x);
            case "simulation": case "pipeline": return state.History.Count > 0 && p.Targets.Contains(State(a, state.History));
            case "groups": return SolvedGroups(a, state).Length == p.Groups.Length;
            case "questions": return values.Length == 1 && values[0] == p.Target;
            case "contradiction": return Index(state.Found, p.Cases.Length) && p.Cases[state.Found].Removals.Contains(state.Removed);
            case "argument": case "analogy": return p.Cases.Any(c => c.Success && c.Input.SequenceEqual(values));
            case "constraints": return Set(values, Enumerable.Range(0, a.Options.Length)) && p.Edges.All(e => Array.IndexOf(values, e[0]) < Array.IndexOf(values, e[1]));
            case "domino": return state.History.Count > 0 && state.History[^1] == p.Target && p.Required.All(i => i == p.Start || state.History.Contains(i));
            case "spectrum": return values.Length == 1 && OnScale(p, values[0]) && values[0] >= p.Low && values[0] <= p.High && state.Trials.Any(t => t.SequenceEqual(values));
            default: return false;
        }
    }

    public static string? Missing(Activity a, MiniGameState state, IEnumerable<string> input)
    {
        var values = input.ToArray();
        return a.Kind switch
        {
            "repair" or "argument" or "analogy" when values.Length != a.Rows.Length || values.Any(v => v == "") => "برای تمام جایگاه‌ها یک کارت انتخاب کن.",
            "counterexample" or "spectrum" when values.Length == 0 || !state.Trials.Any(t => Numbers(values, out var n) && t.SequenceEqual(n)) => "ابتدا نمونه یا مقدار فعلی را آزمایش کن، سپس جواب را ثبت کن.",
            "questions" when values.Length != 1 => "پس از پرسش‌ها، نامزد نهایی را انتخاب کن.",
            "contradiction" when state.Found < 0 => "ابتدا مجموعهٔ حداقلی متناقض را پیدا کن.",
            "contradiction" when state.Removed < 0 => "حالا یک گزاره را برای بازگرداندن سازگاری حذف کن.",
            "simulation" or "pipeline" or "domino" when state.History.Count == 0 => "ابتدا مسیر را با اقدام یا کارت بساز.",
            "groups" when SolvedGroups(a, state).Length == 0 => "ابتدا یک گروه کامل را کشف کن.",
            _ => null
        };
    }

    public static void Validate(Activity a)
    {
        var p = a.Puzzle ?? throw new InvalidDataException("Missing puzzle: " + a.Id);
        bool good = Kinds.Contains(a.Kind) && p.Limit > 0 && a.Options.Length > 0 && a.Options.All(o => !string.IsNullOrWhiteSpace(o));
        bool InOptions(int i) => Index(i, a.Options.Length);
        bool Edges() => p.Edges.Length > 0 && p.Edges.All(e => e.Length == 2 && e.All(InOptions) && e[0] != e[1]);
        bool Acyclic()
        {
            var ordered=new HashSet<int>();
            while(ordered.Count<a.Options.Length)
            {
                var ready=Enumerable.Range(0,a.Options.Length).FirstOrDefault(i=>!ordered.Contains(i)&&p.Edges.All(e=>e[1]!=i||ordered.Contains(e[0])),-1);
                if(ready<0)return false;ordered.Add(ready);
            }
            return true;
        }
        good &= a.Kind switch
        {
            "counterexample" => p.Cases.Length == a.Options.Length && p.Cases.SelectMany(c=>c.Input).Order().SequenceEqual(Enumerable.Range(0,a.Options.Length)) && p.Cases.All(c => c.Input.Length == 1 && c.Input.All(InOptions) && c.Actual.Length > 0 && c.Expected.Length > 0 && c.Reason.Length > 0) && p.Cases.Any(c => c.Success),
            "repair" => p.Initial.Length == a.Rows.Length && p.Initial.All(InOptions) && p.Tests.Length > 0 && p.Cases.All(c => c.Input.Length == a.Rows.Length && c.Input.All(InOptions) && c.Passed.Length == p.Tests.Length) && p.Cases.Any(c => c.Passed.All(x => x) && RepairCost(a, c.Input.Select(i => i.ToString())) <= p.Limit),
            "simulation" or "pipeline" => Index(p.Start, p.States.Length) && p.Transitions.Length == p.States.Length && p.Transitions.All(row => row.Length == a.Options.Length && row.All(v => v == -1 || Index(v, p.States.Length))) && p.Targets.Length > 0 && p.Targets.All(i => Index(i, p.States.Length)),
            "groups" => p.Groups.Length > 1 && p.Groups[0].Length > 1 && p.Groups.All(g => g.Length == p.Groups[0].Length) && Set(p.Groups.SelectMany(g => g), Enumerable.Range(0, a.Options.Length)) && p.Labels.Length == p.Groups.Length,
            "questions" => InOptions(p.Target) && p.Questions.Length > 0 && p.Traits.Length == a.Options.Length && p.Traits.All(t => t.Length == p.Questions.Length) && Enumerable.Range(0, a.Options.Length).Where(i => i != p.Target).All(i => !p.Traits[i].SequenceEqual(p.Traits[p.Target])),
            "contradiction" => p.Cases.Length > 0 && p.Cases.All(c => c.Input.Length > 1 && c.Input.All(InOptions) && c.Input.Distinct().Count() == c.Input.Length && c.Removals.Length > 0 && c.Removals.All(c.Input.Contains) && c.Reason.Length > 0) && p.Cases.All(c => c.Input.Length == p.Cases[0].Input.Length),
            "argument" or "analogy" => a.Rows.Length > 1 && p.Cases.Any(c => c.Success) && p.Cases.All(c => c.Input.Length == a.Rows.Length && c.Input.All(InOptions)),
            "constraints" => Edges() && Acyclic(),
            "domino" => Edges() && InOptions(p.Start) && InOptions(p.Target) && p.Required.All(InOptions) && p.Start != p.Target,
            "spectrum" => p.Step > 0 && p.Min < p.Max && p.Min <= p.Low && p.Low <= p.High && p.High <= p.Max && p.Criterion.Length > 0 && p.Anchors.Length > 1 && (long)p.Min+(((long)p.Low-p.Min+p.Step-1)/p.Step)*p.Step<=p.High,
            _ => false
        };
        if (!good) throw new InvalidDataException("Invalid " + a.Kind + " puzzle: " + a.Id);
    }
}
