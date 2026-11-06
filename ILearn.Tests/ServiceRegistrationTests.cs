using ILearn.Application.Accounts;
using ILearn.Application.Learning;
using ILearn.Application.Reviewing;
using ILearn.Configuration;
using ILearn.Presentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ILearn.Tests;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public async Task WebCompositionResolvesServerAdaptersAndSharedPresentation()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ILearn")),
            ApplicationName = typeof(Services).Assembly.GetName().Name,
            EnvironmentName = "Testing"
        });
        builder.Configuration["Auth:CodeKey"] = Convert.ToBase64String(new byte[32]);
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });
        builder.AddILearn();

        await using var app = builder.Build();
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        Assert.IsType<PostgresAccounts>(services.GetRequiredService<IAccounts>());
        Assert.IsType<PlayerRepository>(services.GetRequiredService<IPlayerRepository>());
        Assert.IsType<WebReviewer>(services.GetRequiredService<IReviewer>());
        Assert.NotNull(services.GetRequiredService<PlayerStore>());
        Assert.False(services.GetRequiredService<LearningUiOptions>().LocalProfile);
        Assert.NotEmpty(services.GetRequiredService<ICurriculum>().Lessons);
    }
}
