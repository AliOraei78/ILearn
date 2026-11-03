namespace ILearn.Domain.Learning;

public sealed class Lesson
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public int Phase { get; set; }
    public string PhaseTitle { get; set; } = "";
    public string[] Prerequisites { get; set; } = [];
    public string Topics { get; set; } = "";
    public string Objective { get; set; } = "";
    public string Example { get; set; } = "";
    public string ExampleOutput { get; set; } = "";
    public List<Activity> Activities { get; set; } = [];
    public bool Legacy => Id.StartsWith('S');
}
