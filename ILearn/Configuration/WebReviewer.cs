using System.Threading.RateLimiting;
using ILearn.Infrastructure.Reviewing;

namespace ILearn.Configuration;

public sealed class WebReviewer(ICurrentPlayer current, Reviewer reviewer, PartitionedRateLimiter<Guid> limits) : IReviewer
{
    async Task<T> Run<T>(Func<Task<T>> action, CancellationToken ct)
    {
        var session = await current.Get();
        using var lease = await limits.AcquireAsync(session.PlayerId, 1, ct);
        if (!lease.IsAcquired) throw new InvalidOperationException("سقف بررسی AI رسیده؛ یک دقیقه صبر کن و دوباره امتحان کن.");
        return await action();
    }

    public Task<string> Answer(Activity activity, CancellationToken ct) => Run(() => reviewer.Answer(activity, ct), ct);
    public Task<Review> Get(Activity activity, string draft, CancellationToken ct, Lesson? lesson = null, string technology = "C#", string? template = null)
        => Run(() => reviewer.Get(activity, draft, ct, lesson, technology, template), ct);
}
