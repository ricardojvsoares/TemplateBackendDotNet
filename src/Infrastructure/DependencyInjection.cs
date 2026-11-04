using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Application.Settings;
using Infrastructure.Services.Messaging;
using Infrastructure.Services.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IMessageBroker, MessageBroker>();
        services.AddScoped<IStorage, Storage>();

        services.AddStorage();
        services.AddMessaging();

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

    private static IServiceCollection AddStorage(this IServiceCollection services)
    {
        services
            .AddOptions<StorageSettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                settings.Endpoint = config["STORAGE_ENDPOINT"] ?? "";
                settings.AccessKey = config["STORAGE_ACCESS_KEY"] ?? "";
                settings.SecretKey = config["STORAGE_SECRET_KEY"] ?? "";
                settings.Bucket = config["STORAGE_BUCKET"] ?? "";
                settings.Region = config["STORAGE_REGION"] ?? "";
            })
            .ValidateOnStart();
        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services
            .AddOptions<RabbitMqSettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                settings.Host = config["RABBITMQ_HOST"] ?? "";
                settings.Port = int.TryParse(config["RABBITMQ_PORT"], out var port) ? port : 5672;
                settings.Username = config["RABBITMQ_USERNAME"] ?? "";
                settings.Password = config["RABBITMQ_PASSWORD"] ?? "";
            })
            .ValidateOnStart();
        return services;
    }
}