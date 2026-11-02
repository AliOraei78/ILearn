namespace ILearn.Domain.Learning;

public sealed class Activity
{
    public string Solution { get; set; } = "";
    public string SolutionCode { get; set; } = "";
    public string Id { get; set; } = ""; public string Title { get; set; } = ""; public string Kind { get; set; } = "choice";
    public string Prompt { get; set; } = ""; public string Code { get; set; } = "";
    public string[] Options { get; set; } = []; public string[] Rows { get; set; } = []; public string[] Answers { get; set; } = []; public string[] Clues { get; set; } = [];
    public string Explanation { get; set; } = ""; public string Hint { get; set; } = ""; public bool Multiple { get; set; }
    public bool IsAi => Kind is "explain" or "workshop"; public int Xp => Kind == "workshop" ? 30 : 10;
    public bool Check(IEnumerable<string> values) => Answers.Order().SequenceEqual(values.Order());
}
