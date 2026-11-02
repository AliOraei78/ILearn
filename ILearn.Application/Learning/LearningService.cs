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
        if (activity.Kind == "detective" && clues < 2) { entry.Feedback = "اول دو سرنخ را بررسی کن."; return; }
        var correct = activity.Kind == "order" ? activity.Answers.SequenceEqual(values) : activity.Check(values);
        entry.NeedsReview = !correct; entry.Feedback = correct ? "درست بود. " + activity.Explanation : "هنوز درست نیست. " + activity.Hint;
        if (correct) { entry.Complete = true; entry.Source = "پاسخ‌نامه"; }
    }
}
