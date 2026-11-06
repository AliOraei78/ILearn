using ILearn;
using ILearn.Offline;
using ILearn.Offline.Accounts;
using ILearn.Offline.Reviewing;
using ILearn.Infrastructure.Learning;
using ILearn.Presentation;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddSingleton<ICurriculum>(_ => new CurriculumData(name =>
{
    using var stream = typeof(App).Assembly.GetManifestResourceStream("Content." + name)
        ?? throw new InvalidDataException("Missing bundled content: " + name);
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}));
builder.Services.AddScoped<OfflinePlayerRepository>();
builder.Services.AddScoped<IPlayerRepository>(sp => sp.GetRequiredService<OfflinePlayerRepository>());
builder.Services.AddScoped<ICurrentPlayer, OfflineCurrentPlayer>();
builder.Services.AddScoped<PlayerStore>();
builder.Services.AddSingleton(new LearningUiOptions { LocalProfile = true });
builder.Services.AddScoped(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(90) });
builder.Services.AddScoped<IReviewer, MobileReviewer>();
await builder.Build().RunAsync();
