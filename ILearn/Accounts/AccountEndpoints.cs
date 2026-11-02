using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;

namespace ILearn.Accounts;

public static class AccountEndpoints
{
    public static string? Pending(HttpContext context, IDataProtectionProvider protection) { try { return context.Request.Cookies["ILearn.Pending"] is string value ? protection.CreateProtector("ILearn.PendingEmail").Unprotect(value) : null; } catch (CryptographicException) { return null; } }
    static void SetPending(HttpContext context, IDataProtectionProvider protection, string email) => context.Response.Cookies.Append("ILearn.Pending", protection.CreateProtector("ILearn.PendingEmail").Protect(email), new CookieOptions { HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Strict, Path = "/", MaxAge = TimeSpan.FromMinutes(15) });
    static async Task SignIn(HttpContext context, IAccounts database, Player player, bool remember = false) { var session = await database.CreateSession(player.Id); await context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, player.Id.ToString()), new Claim(ClaimTypes.Name, player.Name), new Claim("stamp", player.Stamp), new Claim("session", session.ToString()) }, "Cookies")), new AuthenticationProperties { IsPersistent = remember, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) }); }
    public static void MapAccounts(this WebApplication app)
    {
        app.MapPost("/auth/{action}", async (string action, HttpContext context, IAccounts database, IEmailVerification verification, IDataProtectionProvider protection) =>
        {
            if (!await context.RequestServices.GetRequiredService<IAntiforgery>().IsRequestValidAsync(context)) return Results.BadRequest();
            if (action == "logout") { await database.RevokeSession(context.User.Session()); await context.SignOutAsync(); return Results.LocalRedirect("/login"); }
            var form = await context.Request.ReadFormAsync(); var destination = action switch { "register" => "/register", "verify" or "resend" => "/verify", "forgot" => "/forgot-password", "password" => "/set-password", "profile" => "/profile", _ => "/login" };
            try
            {
                if (action is "register" or "forgot" or "resend")
                {
                    var email = action == "resend" ? Pending(context, protection) ?? throw new ArgumentException("اول ایمیلت رو وارد کن.") : EmailAddress.Normalize(form["email"].ToString()); SetPending(context, protection, email);
                    var result = await verification.Start(email, action == "register" ? form["name"].ToString() : null, action == "register" ? (form["avatar"].ToString() is { Length: > 0 } avatar ? avatar : "avatar-01") : null, context.Connection.RemoteIpAddress?.ToString() ?? "local"); return Results.LocalRedirect(result.Delivered ? "/verify" : "/verify?error=delivery");
                }
                if (action == "verify")
                {
                    var email = Pending(context, protection); if (email is null) return Results.LocalRedirect("/register"); var token = await verification.Verify(email, form["code"].ToString()); if (token is null) return Results.LocalRedirect("/verify?error=code");
                    context.Response.Cookies.Delete("ILearn.Pending"); context.Response.Cookies.Append("ILearn.PasswordGrant", token, new CookieOptions { HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Strict, Path = "/", MaxAge = TimeSpan.FromMinutes(10) }); return Results.LocalRedirect("/set-password");
                }
                if (action == "password")
                {
                    if (form["password"] != form["confirmation"]) throw new ArgumentException("تکرار رمز یکسان نیست."); var token = context.Request.Cookies["ILearn.PasswordGrant"] ?? ""; var result = await verification.SetPassword(token, form["password"].ToString()); if (result is null) return Results.LocalRedirect("/set-password?error=expired");
                    context.Response.Cookies.Delete("ILearn.PasswordGrant"); await context.SignOutAsync(); if (result.Value.Initial) { await SignIn(context, database, result.Value.Player); return Results.LocalRedirect("/lobby"); }
                    return Results.LocalRedirect("/login?reset=true");
                }
                if (action == "profile")
                {
                    if (context.User.Identity?.IsAuthenticated != true) return Results.Unauthorized(); if (form["newPassword"] != form["confirmation"]) throw new ArgumentException("تکرار رمز تازه یکسان نیست."); var remember = (await context.AuthenticateAsync()).Properties?.IsPersistent == true;
                    var player = await database.UpdateProfile(context.User.Session(), form["name"].ToString(), form["currentPassword"].ToString(), form["newPassword"].ToString()); await database.RevokeSession(context.User.Session()); await SignIn(context, database, player, remember); return Results.LocalRedirect("/profile?saved=true");
                }
                if (action == "login")
                {
                    var player = await database.Login(form["email"].ToString(), form["password"].ToString(), context.Connection.RemoteIpAddress?.ToString() ?? "local"); if (player is null) return Results.LocalRedirect(destination + "?error=credentials"); await SignIn(context, database, player, form["remember"] == "true"); return Results.LocalRedirect("/lobby");
                }
                return Results.NotFound();
            }
            catch (VerificationLimit limit) { context.Response.Headers.RetryAfter = limit.Seconds.ToString(); return Results.LocalRedirect((action == "login" ? destination : "/verify") + "?wait=" + limit.Seconds); }
            catch (ArgumentException ex) { return Results.LocalRedirect(destination + "?message=" + Uri.EscapeDataString(ex.Message)); }
            catch (DuplicateAccountException) { return Results.LocalRedirect(destination + "?message=" + Uri.EscapeDataString("این مشخصات قبلاً ثبت شده؛ اطلاعات رو بررسی کن.")); }
            catch (AccountStorageException ex) { context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Accounts").LogError(ex, "Account operation {Action} failed", action); return Results.LocalRedirect(destination + "?error=database"); }
        }).RequireRateLimiting("login");
    }
}
