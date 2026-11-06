namespace ILearn.Domain.Learning;

public sealed class Activity
{
    public Puzzle? Puzzle { get; set; }
    public MiniGame? Game { get; set; }
    public string Solution { get; set; } = "";
    public string SolutionCode { get; set; } = "";
    public Dictionary<string, string> ReferenceFiles { get; set; } = [];
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Kind { get; set; } = "choice";
    public string Prompt { get; set; } = "";
    public string Code { get; set; } = "";
    public string AnswerFormat { get; set; } = "";
    public string[] Options { get; set; } = [];
    public string[] Rows { get; set; } = [];
    public string[] Answers { get; set; } = [];
    public string[] Clues { get; set; } = [];
    public string Explanation { get; set; } = "";
    public string Hint { get; set; } = "";
    public bool Multiple { get; set; }
    public bool IsAi => Kind is "explain" or "workshop";
    public int Xp => Kind == "workshop" ? 30 : 10;

    public bool Check(IEnumerable<string> values) => Answers.Order().SequenceEqual(values.Order());

    // Exact, complete references are authored answers; a model must not downgrade them.
    public bool MatchesReference(string draft)
    {
        static string Normalize(string value) => value.Replace("\r\n", "\n").Trim();
        return AnswerFormat switch
        {
            "code" => ReferenceFiles.Count == 0 && !string.IsNullOrWhiteSpace(SolutionCode) && Normalize(draft) == Normalize(SolutionCode),
            "text" => !string.IsNullOrWhiteSpace(Solution) && Normalize(draft) == Normalize(Solution),
            _ => false
        };
    }
}
