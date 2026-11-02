namespace ILearn.Application.Learning;

public static class LearningService
{
    public static int Xp(IEnumerable<Lesson> lessons, Progress progress) => lessons.Sum(l => l.Activities.Where(a => progress.Entries.GetValueOrDefault($"csharp/{l.Id}/{a.Id}")?.Complete == true).Sum(a => a.Xp));
    public static void Start(Progress progress, string lesson) => progress.IntroductionsRead.Add("csharp/" + lesson);
    public static (Lesson Lesson, int Position)? NextReview(IEnumerable<Lesson> lessons, Progress progress)
    {
        foreach (var lesson in lessons) { var index = lesson.Activities.FindIndex(a => progress.Entries.GetValueOrDefault($"csharp/{lesson.Id}/{a.Id}")?.NeedsReview == true); if (index >= 0) return (lesson, index); }
        return null;
    }
    public static void Complete(Entry entry) { if (string.IsNullOrWhiteSpace(entry.Draft)) return; entry.Complete = true; entry.Source = "ثبت شخصی"; entry.NeedsReview = false; }
    public static void Check(Activity activity, Entry entry, IEnumerable<string> values, int clues)
    {
        var input = values.ToArray();
        var game = activity.Game;
        var node = game is not null && activity.Kind == "mission" ? MiniGames.Node(activity, entry.GameState) : -1;
        var missing = activity.Kind switch
        {
            "explain" or "workshop" => "پاسخت را در کادر بنویس و بررسی مدل یا ثبت شخصی تکمیل را انتخاب کن.",
            "detective" when clues < 2 => "اول دو سرنخ را بررسی کن.",
            "slots" or "assessment" or "trace" when input.Length < activity.Rows.Length => "پاسخ همهٔ ردیف‌ها یا قدم‌ها را انتخاب کن، سپس جواب را ثبت کن.",
            "concept" when entry.GameState.History.Count == 0 => "ابتدا یک حدس انتخاب کن، سپس جواب را ثبت کن.",
            "experiment" when entry.GameState.History.Count == 0 || input.Length == 0 => "ابتدا یک آزمایش را باز کن و قانون پیشنهادی را انتخاب کن.",
            "mission" when game is not null && node >= 0 && node < game.Nodes.Length && game.Nodes[node].Choices.Length > 0 => "تصمیم‌های مسیر را تا پایان بگیر، سپس جواب را ثبت کن.",
            "deduction" when entry.GameState.Marks.Count(x => x.Value == 1) < activity.Rows.Length => "برای هر مرجع یک نتیجه را تأیید کن، سپس جواب را ثبت کن.",
            _ when input.Length == 0 && activity.Kind is not ("order" or "concept" or "mission" or "deduction") => "ابتدا گزینهٔ پاسخ را انتخاب کن.",
            _ => null
        };
        if (missing is not null) { entry.FeedbackError = true; entry.Feedback = missing; return; }
        var correct = game is not null ? MiniGames.Check(activity, entry.GameState, input) : activity.Kind == "order" ? activity.Answers.SequenceEqual(input) : activity.Check(input);
        entry.FeedbackError = entry.NeedsReview = !correct; entry.Feedback = correct ? "درست بود. " + activity.Explanation : "هنوز درست نیست. " + activity.Hint;
        if (correct) { entry.Complete = true; entry.Source = "پاسخ‌نامه"; }
    }
}
