using ILearn.Infrastructure.Accounts;
using ILearn.Infrastructure.Learning;
using ILearn.Infrastructure.Reviewing;
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
        var protection = builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
        if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
        builder.Services.AddSingleton<Curriculum>();
        builder.Services.AddAuthentication("Cookies").AddCookie(o =>
        {
            o.LoginPath = "/login";
            o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict;
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
        builder.Services.AddSingleton(Npgsql.NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("ILearn") ?? "Host=localhost;Port=5441;Database=ilearn;Username=ilearn"));
        builder.Services.AddSingleton<SchemaInitializer>();
        builder.Services.AddSingleton<PostgresAccounts>();
        builder.Services.AddSingleton<IAccounts>(sp => sp.GetRequiredService<PostgresAccounts>());
        builder.Services.AddSingleton<IEmailSender, EmailSender>();
        builder.Services.AddSingleton<IEmailVerification, EmailVerification>();
        builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
        builder.Services.AddScoped<ICurrentPlayer, CurrentPlayer>();
        builder.Services.AddScoped<PlayerStore>();
        builder.Services.AddSingleton<ICurriculum>(sp => sp.GetRequiredService<Curriculum>());
        builder.Services.AddTransient<IReviewer>(sp => sp.GetRequiredService<Reviewer>());
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
        builder.Services.AddHttpClient<Reviewer>(client => client.Timeout = TimeSpan.FromSeconds(90));
        if (Convert.FromBase64String(builder.Configuration["Auth:CodeKey"] ?? throw new InvalidOperationException("Auth:CodeKey is required.")).Length < 32) throw new InvalidOperationException("Auth:CodeKey requires at least 32 random bytes.");

    }
}