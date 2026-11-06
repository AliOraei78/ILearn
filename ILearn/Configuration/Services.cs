using ILearn.Infrastructure.Server;
using ILearn.Infrastructure.Reviewing;
using ILearn.Presentation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
namespace ILearn.Configuration;

public static class Services
{
    public static void AddILearn(this WebApplicationBuilder builder)
    {
        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var keyDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
        Directory.CreateDirectory(keyDirectory);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(keyDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        // Pre-audit tickets must reauthenticate under the stronger password policy.
        var protection = builder.Services.AddDataProtection().SetApplicationName("ILearn.Security.v2").PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
        if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
        builder.Services.AddServerInfrastructure(builder.Configuration);
        builder.Services.AddAuthentication("Cookies").AddCookie(o =>
        {
            o.LoginPath = "/login";
            o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.SecurePolicy = builder.Configuration.GetValue("Security:AllowLocalHttp", builder.Environment.IsDevelopment()) ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            o.ExpireTimeSpan = TimeSpan.FromDays(7); o.SlidingExpiration = false;
            o.Events.OnValidatePrincipal = async context =>
            {
                if (!await context.HttpContext.RequestServices.GetRequiredService<IAccounts>().ValidSession(context.Principal!.Session()))
                {
                    context.RejectPrincipal(); await context.HttpContext.SignOutAsync();
                }
            };
        });
        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<ICurrentPlayer, CurrentPlayer>();
        builder.Services.AddScoped<PlayerStore>();
        builder.Services.AddSingleton(new LearningUiOptions());
        builder.Services.AddScoped<IReviewer, WebReviewer>();
        builder.Services.AddSingleton<System.Threading.RateLimiting.PartitionedRateLimiter<Guid>>(_ =>
            System.Threading.RateLimiting.PartitionedRateLimiter.CreateChained(
                System.Threading.RateLimiting.PartitionedRateLimiter.Create<Guid, Guid>(id =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(id, _ => new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })),
                System.Threading.RateLimiting.PartitionedRateLimiter.Create<Guid, Guid>(id =>
                    System.Threading.RateLimiting.RateLimitPartition.GetConcurrencyLimiter(id, _ => new() { PermitLimit = 2, QueueLimit = 0 }))));
        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = 429;
            o.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                await context.HttpContext.Response.WriteAsync("تعداد درخواست‌ها زیاد شده. یک دقیقه صبر کن و دوباره امتحان کن.", ct);
            };
            o.AddPolicy("login", context =>
            {
                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new() { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
            });
        });
        builder.Services.AddHttpClient<Reviewer>(client => client.Timeout = TimeSpan.FromSeconds(90))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
    }
}
