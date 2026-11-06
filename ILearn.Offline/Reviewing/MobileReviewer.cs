using ILearn.Infrastructure.Reviewing;
using Microsoft.JSInterop;

namespace ILearn.Offline.Reviewing;

public sealed class MobileReviewer(IJSRuntime js, HttpClient http) : IReviewer
{
    async Task<Reviewer> Create(CancellationToken ct)
    {
        var key = await js.InvokeAsync<string>("ilearnOffline.apiKey", ct);
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("کلید OpenRouter را از تنظیمات پروفایل وارد کن.");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["OpenRouter:ApiKey"] = key }).Build();
        return new(http, config);
    }

    public async Task<string> Answer(Activity activity, CancellationToken ct) => await (await Create(ct)).Answer(activity, ct);

    public async Task<Review> Get(Activity activity, string draft, CancellationToken ct, Lesson? lesson = null, string technology = "C#", string? template = null)
        => await (await Create(ct)).Get(activity, draft, ct, lesson, technology, template);
}
