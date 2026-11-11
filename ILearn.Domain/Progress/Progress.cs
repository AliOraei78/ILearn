namespace ILearn.Domain.Progress;

public sealed class Progress
{
    public long Revision { get; set; }
    public Dictionary<string, int> Positions { get; set; } = [];
    public int Version { get; set; } = 1;
    public string Lesson { get; set; } = "R001";
    public int Position { get; set; }
    public string CodeRoomExercise { get; set; } = "";
    public string InterviewQuestion { get; set; } = "";
    public Dictionary<string, Entry> Entries { get; set; } = [];
    public HashSet<string> IntroductionsRead { get; set; } = [];
    public Dictionary<string, int> IntroductionSessions { get; set; } = [];
    public bool HasStarted(string lesson) => IntroductionsRead.Contains("csharp/" + lesson) || (Lesson == lesson && Position > 0) ||
        Entries.Any(x => x.Key.StartsWith("csharp/" + lesson + "/") &&
            (x.Value.Complete || x.Value.Draft.Length > 0 || x.Value.Answers.Count > 0 || x.Value.Feedback.Length > 0 || x.Value.GameState.History.Count > 0 || x.Value.GameState.Marks.Count > 0 || x.Value.GameState.Trials.Count > 0 || x.Value.GameState.Found >= 0));
}
