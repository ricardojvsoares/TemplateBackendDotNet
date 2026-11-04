using Application.Abstractions.Data;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Services.Data;
using Scrutor;

namespace Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {

        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
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
}