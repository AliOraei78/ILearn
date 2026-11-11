namespace ILearn.Domain.Learning;

public sealed class LessonSession
{
    public int? ResumeIndex { get; init; }
    public string Title { get; init; } = "";
    public string Goal { get; init; } = "";
    public string Text { get; init; } = "";
    public string CommonMistake { get; init; } = "";
    public string Question { get; init; } = "";
    public string[] Options { get; init; } = [];
    public int CorrectOption { get; init; } = -1;
    public string Feedback { get; init; } = "";
    public LessonPractice? Practice { get; init; }
    public string[] ActivityIds { get; init; } = [];
}

public sealed record LessonPractice(string Prompt, string Criteria);
