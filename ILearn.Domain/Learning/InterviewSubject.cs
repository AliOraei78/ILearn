namespace ILearn.Domain.Learning;

public sealed class InterviewSubject
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string SelectionNote { get; set; } = "";
    public string ReviewedOn { get; set; } = "";
    public List<InterviewSource> Sources { get; set; } = [];
    public List<InterviewQuestion> Questions { get; set; } = [];
}

public sealed class InterviewQuestion
{
    public string Category { get; set; } = "";
    public string Level { get; set; } = "";
    public string[] SourceIds { get; set; } = [];
    public Activity Activity { get; set; } = new();
    public string Key(string subject) => $"interview/{subject}/{Activity.Id}";
}

public sealed class InterviewSource
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Role { get; set; } = "";
}
