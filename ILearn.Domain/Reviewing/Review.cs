namespace ILearn.Domain.Reviewing;

public sealed record Review(int score, string summary, string[] issues, string nextHint)
{
    public bool sufficient => score == 100;
    public string Verdict => score == 100 ? "درست" : score >= 70 ? "تقریباً درست" : "غلط";
    public string Feedback => $"{Verdict}؛ برآورد درستی پاسخ: {score}٪\n{summary}\n{string.Join("\n", issues)}\nقدم بعد: {nextHint}";
}
