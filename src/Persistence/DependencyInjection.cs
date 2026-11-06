using Application.Abstractions.Data;
using Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Persistence.Services.Data;
using Scrutor;

namespace Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {

        services.AddScoped<IDatabaseService, DatabaseService>();

        services.AddPostgres();

        services.Scan(
            s => s
            .FromAssemblies(
                AssemblyReference.Assembly)
            .AddClasses(false)
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsMatchingInterface()
            .WithScopedLifetime());

        return services;
    }

    private static IServiceCollection AddPostgres(this IServiceCollection services)
    {
        services
            .AddOptions<DatabaseServiceSettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                settings.Host = config["POSTGRES_HOST"] ?? "";
                settings.Port = int.TryParse(config["POSTGRES_PORT"], out var port) ? port : 5432;
                settings.Database = config["POSTGRES_DATABASE"] ?? "";
                settings.Username = config["POSTGRES_USERNAME"] ?? "";
                settings.Password = config["POSTGRES_PASSWORD"] ?? "";
            })
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<DatabaseServiceSettings>>().Value;
            var connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = settings.Host,
                Port = settings.Port,
                Database = settings.Database,
                Username = settings.Username,
                Password = settings.Password
            };

            return NpgsqlDataSource.Create(connectionString.ConnectionString);
        });

        return services;
    }
}