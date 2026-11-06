using ILearn.Configuration;
using ILearn.Components;
using ILearn.Components.Pages;

var builder = WebApplication.CreateBuilder(args);

builder.AddILearn();
builder.AddWebSecurity();
var app = builder.Build();
app.UseWebSecurity();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
// Re-rendering a rejected POST would read its invalid form again while generating Razor antiforgery tokens.
app.UseWhen(context => HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();
app.MapAccounts();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Home).Assembly);

app.Run();
