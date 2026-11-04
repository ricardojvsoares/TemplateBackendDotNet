using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Infrastructure.Services.Messaging;
using Infrastructure.Services.Storage;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IMessageBroker, MessageBroker>();
        services.AddScoped<IStorage, Storage>();

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