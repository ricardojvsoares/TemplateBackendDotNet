using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Keycloak;
using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Application.Settings;
using Infrastructure.Services.Identity;
using Infrastructure.Services.Identity.Keycloak;
using Infrastructure.Services.Messaging;
using Infrastructure.Services.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scrutor;
using System.Security.Claims;
using Wolverine;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IMessageService, MessageService>();
        services.AddScoped<IStorageService, StorageService>();

        services.AddStorage();
        services.AddMessaging();
        services.AddKeycloak();

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

    public static IHostBuilder UseInfrastructureMessaging(this IHostBuilder host)
    {
        return host.UseWolverine(options =>
            options.Discovery.IncludeAssembly(Application.AssemblyReference.Assembly));
    }

    private static IServiceCollection AddStorage(this IServiceCollection services)
    {
        services
            .AddOptions<StorageServiceSettings>()
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
            .AddOptions<MessageServiceSettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                MessageServiceSettings read = ReadMessageServiceSettings(config);
                settings.Host = read.Host;
                settings.Port = read.Port;
                settings.Username = read.Username;
                settings.Password = read.Password;
            })
            .Validate(
                s => !string.IsNullOrWhiteSpace(s.Host),
                "RABBITMQ_HOST must be set.")
            .ValidateOnStart();
        return services;
    }

    private static MessageServiceSettings ReadMessageServiceSettings(IConfiguration config)
        => new()
        {
            Host = config["RABBITMQ_HOST"] ?? "",
            Port = int.TryParse(config["RABBITMQ_PORT"], out int port) ? port : 5672,
            Username = config["RABBITMQ_USERNAME"] ?? "",
            Password = config["RABBITMQ_PASSWORD"] ?? ""
        };

    private static IServiceCollection AddKeycloak(this IServiceCollection services)
    {
        services
            .AddOptions<KeycloakSettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                settings.Authority = (config["KEYCLOAK_AUTHORITY"] ?? "").TrimEnd('/');
                settings.Issuer = config["KEYCLOAK_ISSUER"] is { Length: > 0 } issuer ? issuer : settings.Authority;
                settings.Audience = config["KEYCLOAK_AUDIENCE"] ?? "";
                settings.ClientId = config["KEYCLOAK_CLIENT_ID"] ?? "";
                settings.MetadataAddress = config["KEYCLOAK_METADATA_ADDRESS"] is { Length: > 0 } metadata
                    ? metadata
                    : $"{settings.Authority}/.well-known/openid-configuration";
                settings.RequireHttpsMetadata =
                    !bool.TryParse(config["KEYCLOAK_REQUIRE_HTTPS_METADATA"], out bool requireHttps) || requireHttps;
                settings.AdminClientId = config["KEYCLOAK_ADMIN_CLIENT_ID"] ?? "";
                settings.AdminClientSecret = config["KEYCLOAK_ADMIN_CLIENT_SECRET"] ?? "";
            })
            .Validate(
                s => !string.IsNullOrWhiteSpace(s.Authority)
                     && !string.IsNullOrWhiteSpace(s.Audience)
                     && !string.IsNullOrWhiteSpace(s.ClientId)
                     && !string.IsNullOrWhiteSpace(s.AdminClientId)
                     && !string.IsNullOrWhiteSpace(s.AdminClientSecret),
                "KEYCLOAK_AUTHORITY, KEYCLOAK_AUDIENCE, KEYCLOAK_CLIENT_ID, KEYCLOAK_ADMIN_CLIENT_ID and KEYCLOAK_ADMIN_CLIENT_SECRET must be set.")
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<KeycloakSettings>>((jwt, keycloak) =>
            {
                KeycloakSettings settings = keycloak.Value;

                jwt.Authority = settings.Authority;
                jwt.MetadataAddress = settings.MetadataAddress;
                jwt.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorization();
        services.AddSingleton<IClaimsTransformation, KeycloakClaimsTransformation>();

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<KeycloakAdminTokenProvider>();
        services.AddTransient<KeycloakAdminAuthHandler>();
        services.AddHttpClient(KeycloakAdminTokenProvider.HttpClientName);

        services.AddKeycloakAdminClient<IKeycloakAdminUsersService, KeycloakAdminUsersService>();
        services.AddKeycloakAdminClient<IKeycloakAdminSessionsService, KeycloakAdminSessionsService>();
        services.AddKeycloakAdminClient<IKeycloakAdminClientsService, KeycloakAdminClientsService>();

        return services;
    }

    private static void AddKeycloakAdminClient<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services
            .AddHttpClient<TService, TImplementation>((sp, client) =>
                client.BaseAddress = KeycloakEndpoints.GetAdminBaseUri(
                    sp.GetRequiredService<IOptions<KeycloakSettings>>().Value))
            .AddHttpMessageHandler<KeycloakAdminAuthHandler>();
    }

}