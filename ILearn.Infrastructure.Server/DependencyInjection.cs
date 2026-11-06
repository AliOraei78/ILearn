using ILearn.Application.Learning;
using ILearn.Infrastructure.Accounts;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ILearn.Infrastructure.Server;

public static class DependencyInjection
{
    public static IServiceCollection AddServerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var codeKey = configuration["Auth:CodeKey"]
            ?? throw new InvalidOperationException("Auth:CodeKey is required.");
        if (Convert.FromBase64String(codeKey).Length < 32)
            throw new InvalidOperationException("Auth:CodeKey requires at least 32 random bytes.");

        services.AddSingleton(_ => NpgsqlDataSource.Create(configuration.GetConnectionString("ILearn")
            ?? "Host=localhost;Port=5441;Database=ilearn;Username=ilearn_app"));
        services.AddSingleton<SchemaInitializer>();
        services.AddHostedService<CredentialCleanup>();
        services.AddSingleton<PostgresAccounts>();
        services.AddSingleton<IAccounts>(provider => provider.GetRequiredService<PostgresAccounts>());
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<IEmailVerification, EmailVerification>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddSingleton<Curriculum>();
        services.AddSingleton<ICurriculum>(provider => provider.GetRequiredService<Curriculum>());
        return services;
    }
}
