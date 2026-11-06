using System.Text.Json;
using ILearn.Application.Learning;
using ILearn.Infrastructure.Learning;
using ILearn.Infrastructure.Reviewing;
using Microsoft.Extensions.Configuration;

if (!args.Contains("--live-free")) throw new ArgumentException("Pass --live-free to explicitly run four free-model checks.");
var content = new CurriculumData(name => File.ReadAllText(Path.Combine("ILearn/Content", name)));
var settings = File.ReadAllLines(".env").Where(line => line.Contains('=')).Select(line => line.Split('=', 2)).ToDictionary(line => line[0], line => line[1].Trim('"'));
var key = settings.GetValueOrDefault("OPENROUTER_API_KEY") ?? throw new InvalidOperationException("The configured OpenRouter key is missing.");
using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(90) };
var reviewer = new Reviewer(http, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["OpenRouter:ApiKey"] = key }).Build());
var syntax = content.Lessons.Single(l => l.Id == "R002").Activities.Single(a => a.Id == "workshop");
var positive = content.Lessons.Single(l => l.Id == "R012").Activities.Single(a => a.Id == "workshop");
var language = content.Interviews[0].Questions.Single(q => q.Activity.Id == "C001").Activity;
var cases = new[] {
    (Name:"equivalent syntax code", Activity:syntax, Draft:"Console.WriteLine(10);", Expected:true),
    (Name:"equivalent TryPositive", Activity:positive, Draft:"static bool TryPositive(string text, out int value) { if(int.TryParse(text,out int n) && n>0) { value=n; return true; } value=0; return false; }", Expected:true),
    (Name:"short equivalent concept", Activity:language, Draft:"C# یک زبان برنامه‌نویسی است. .NET پلتفرم اجرای برنامه و مجموعهٔ کتابخانه‌ها و ابزارهای ساخت آن است.", Expected:true),
    (Name:"wrong output is rejected", Activity:syntax, Draft:"Console.WriteLine(9);", Expected:false)
};
var results = new List<object>();
bool passed = true;
foreach (var item in cases)
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
    try
    {
        var result = await reviewer.Get(item.Activity, item.Draft, deadline.Token);
        var correct = result.sufficient == item.Expected;
        passed &= correct;
        results.Add(new { item.Name, result.score, result.summary, result.issues, passed = correct });
        Console.WriteLine($"{(correct ? "PASS" : "FAIL")} {item.Name}: {result.score}");
    }
    catch (Exception error) when (error is HttpRequestException or OperationCanceledException or InvalidDataException)
    {
        passed = false;
        results.Add(new { item.Name, error = error.GetType().Name, passed = false });
        Console.WriteLine($"UNAVAILABLE {item.Name}: {error.GetType().Name}");
    }
    await File.WriteAllTextAsync("verification/focused-live-review-report.json", JsonSerializer.Serialize(new { passed, results }, new JsonSerializerOptions { WriteIndented = true }));
}
return passed ? 0 : 1;
