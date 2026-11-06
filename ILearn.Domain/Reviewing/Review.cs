namespace ILearn.Domain.Reviewing;

public sealed record Review(int score, string summary, string[] issues, string nextHint)
{
    [System.Text.Json.Serialization.JsonIgnore] public bool ReferenceMatch { get; init; }
    [System.Text.Json.Serialization.JsonIgnore] public string Source => ReferenceMatch ? "تطبیق پاسخ مرجع" : "بررسی مدل";
    public bool sufficient => score == 100;
    public string Verdict => score == 100 ? "درست" : score >= 70 ? "تقریباً درست" : "غلط";
    public string Feedback => $"{Verdict}؛ برآورد درستی پاسخ: {score}٪\n{summary}\n{string.Join("\n", issues)}\nقدم بعد: {nextHint}";
}
