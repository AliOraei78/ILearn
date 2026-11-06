using System.Text.Json;
using Microsoft.JSInterop;

namespace ILearn.Offline.Accounts;

public sealed class OfflinePlayerRepository(IJSRuntime js) : IPlayerRepository
{
    readonly SemaphoreSlim saves = new(1, 1);

    public async Task<Player> Profile(Guid owner)
    {
        var json = await js.InvokeAsync<string?>("ilearnOffline.read", "profile");
        return json is null ? new Player(owner, "بازیکن", "avatar-01") :
            JsonSerializer.Deserialize<Player>(json) ?? throw new InvalidDataException("پروفایل خوانده نشد؛ از پشتیبان استفاده کن.");
    }

    public async Task Avatar(Guid owner, string avatar)
    {
        if (!int.TryParse(avatar.Replace("avatar-", ""), out var number) || number is < 1 or > 48)
            throw new ArgumentException("کاراکتر نامعتبر است.");
        await SaveProfile((await Profile(owner)) with { Avatar = $"avatar-{number:00}" });
    }

    public async Task SaveProfile(Player player)
    {
        var name = player.Name.Trim();
        if (name.Length is < 1 or > 50) throw new ArgumentException("نام باید بین ۱ تا ۵۰ نویسه باشد.");
        await js.InvokeVoidAsync("ilearnOffline.write", "profile", JsonSerializer.Serialize(player with { Name = name }));
    }

    public async Task<Progress> Load(Guid owner)
    {
        var json = await js.InvokeAsync<string?>("ilearnOffline.read", "progress");
        return json is null ? new Progress() : JsonSerializer.Deserialize<Progress>(json)
            ?? throw new InvalidDataException("سیو خوانده نشد؛ دادهٔ قبلی محفوظ است.");
    }

    public async Task Save(Guid owner, Progress progress)
    {
        // Commit and revision check share one IndexedDB transaction, including across tabs.
        await saves.WaitAsync();
        try { progress.Revision = await js.InvokeAsync<long>("ilearnOffline.saveProgress", JsonSerializer.Serialize(progress)); }
        finally { saves.Release(); }
    }
}
