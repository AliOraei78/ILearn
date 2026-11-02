namespace ILearn.Domain.Progress;

public sealed class Entry
{
    public int Step { get; set; }
    public Learning.MiniGameState GameState { get; set; } = new();
    public string FullAnswer { get; set; } = "";
    public string Draft { get; set; } = "";
    public List<string> Answers { get; set; } = [];
    public bool Complete { get; set; }
    public bool NeedsReview { get; set; }
    public string Feedback { get; set; } = "";
    public bool FeedbackError { get; set; }
    public string Source { get; set; } = "";
}
