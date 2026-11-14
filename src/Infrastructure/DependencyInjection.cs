using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Application.Abstractions.Cache;
using Application.Abstractions.Mail;
using Application.Abstractions.Identity;
using Application.Abstractions.Jobs;
using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Application.Features.Auth.Jobs;
using Application.Settings;
using Domain.Features.Users;
using Infrastructure.Jobs;
using Infrastructure.Policies;
using Infrastructure.Services.Cache;
using Infrastructure.Services.Identity;
using Infrastructure.Services.Mail;
using Infrastructure.Services.Messaging;
using Infrastructure.Services.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Scrutor;
using StackExchange.Redis;
using Wolverine;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddStorage();
        services.AddMessaging();
        services.AddAuth();
        services.AddCache();
        services.AddResend();

        services.Scan(
            selector => selector
                .FromAssemblies(AssemblyReference.Assembly)
                .AddClasses(false)
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsMatchingInterface()
                .WithScopedLifetime());

        return services;
    }

    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<JobScheduler>();
        services.AddScoped<IRecurringJob, PurgeExpiredRefreshTokensJob>();
        return services;
    }

    public static IHostBuilder UseInfrastructureMessaging(this IHostBuilder host)
    {
        return host.UseWolverine(options =>
        {
            options.Discovery.IncludeAssembly(Application.AssemblyReference.Assembly);
        });
    }

    public static IServiceCollection AddStorage(this IServiceCollection services)
    {
        services.AddScoped<IStorageService, StorageService>();

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

        services.AddSingleton<IAmazonS3>(sp =>
        {
            StorageServiceSettings settings = sp.GetRequiredService<IOptions<StorageServiceSettings>>().Value;

            var config = new AmazonS3Config
            {
                ServiceURL = settings.Endpoint,
                ForcePathStyle = settings.ForcePathStyle,
                AuthenticationRegion = settings.Region,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            };

            return new AmazonS3Client(
                new BasicAWSCredentials(settings.AccessKey, settings.SecretKey),
                config);
        });

        return services;
    }

    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IMessageService, MessageService>();

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
                settings => !string.IsNullOrWhiteSpace(settings.Host),
                "RABBITMQ_HOST must be set.")
            .ValidateOnStart();
        return services;
    }

    public static IServiceCollection AddCache(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ICacheService, CacheService>();

        services
            .AddOptions<RedisServiceSettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                settings.Host = config["REDIS_HOST"] ?? "";
                settings.Port = int.TryParse(config["REDIS_PORT"], out int port) ? port : 6379;
                settings.Password = config["REDIS_PASSWORD"] ?? "";
            })
            .ValidateDataAnnotations()
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.Host),
                "REDIS_HOST must be set.")
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            RedisServiceSettings settings = sp.GetRequiredService<IOptions<RedisServiceSettings>>().Value;
            var configuration = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                ConnectTimeout = 5000
            };
            configuration.EndPoints.Add(settings.Host, settings.Port);
            if (!string.IsNullOrEmpty(settings.Password))
            {
                configuration.Password = settings.Password;
            }

            return ConnectionMultiplexer.Connect(configuration);
        });

        services
            .AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddRedisInstrumentation());

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

    private static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services
            .AddOptions<AuthServiceSettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                settings.Issuer = config["AUTH_ISSUER"] ?? "";
                settings.Audience = config["AUTH_AUDIENCE"] ?? "";
                settings.SigningKey = config["AUTH_SIGNING_KEY"] ?? "";
                settings.AccessTokenMinutes = int.TryParse(config["AUTH_ACCESS_TOKEN_MINUTES"], out int minutes)
                    ? minutes
                    : 15;
                settings.RefreshTokenDays = int.TryParse(config["AUTH_REFRESH_TOKEN_DAYS"], out int days)
                    ? days
                    : 7;
                settings.AdminEmail = config["AUTH_ADMIN_EMAIL"] ?? "";
                settings.AdminPassword = config["AUTH_ADMIN_PASSWORD"] ?? "";
                settings.AdminFirstName = config["AUTH_ADMIN_FIRST_NAME"] ?? "";
                settings.AdminLastName = config["AUTH_ADMIN_LAST_NAME"] ?? "";
            })
            .ValidateDataAnnotations()
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.Issuer),
                "AUTH_ISSUER must be set.")
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.Audience),
                "AUTH_AUDIENCE must be set.")
            .Validate(
                settings => settings.SigningKey.Length >= AuthServiceSettings.MinimumSigningKeyLength,
                "AUTH_SIGNING_KEY must be at least 32 characters.")
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.AdminEmail),
                "AUTH_ADMIN_EMAIL must be set.")
            .Validate(
                settings => settings.AdminPassword.Length >= UserRules.MinimumPasswordLength
                    && settings.AdminPassword.Length <= UserRules.MaximumPasswordLength,
                "AUTH_ADMIN_PASSWORD must be between 8 and 128 characters.")
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.AdminFirstName),
                "AUTH_ADMIN_FIRST_NAME must be set.")
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.AdminLastName),
                "AUTH_ADMIN_LAST_NAME must be set.")
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthServiceSettings>>((jwt, auth) =>
            {
                AuthServiceSettings settings = auth.Value;
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                    ValidateLifetime = true,
                    NameClaimType = "sub",
                    RoleClaimType = AuthClaims.Role,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
                };
                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = OnTokenValidatedAsync
                };
            });

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();

        return services;
    }

    private static async Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        string? jti = context.Principal?.FindFirst("jti")?.Value;
        if (string.IsNullOrWhiteSpace(jti))
        {
            context.Fail("The access token is missing jti.");
            return;
        }

        try
        {
            ICacheService cache = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
            string? denied = await cache.GetAsync(
                AuthCacheKeys.Deny(jti),
                context.HttpContext.RequestAborted);

            if (denied is not null)
            {
                context.Fail("The access token has been revoked.");
                return;
            }
        }
        catch (Exception exception)
        {
            context.Fail(exception);
            return;
        }

        if (context.SecurityToken is not null)
        {
            context.HttpContext.Items[AuthTokenItems.ExpiresAt] = context.SecurityToken.ValidTo;
        }
    }

    private static IServiceCollection AddResend(this IServiceCollection services)
    {
        services
            .AddOptions<MailServiceSettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                settings.ApiKey = config["RESEND_API_KEY"] ?? "";
                settings.From = config["RESEND_FROM"] ?? "";
            })
            .ValidateDataAnnotations()
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.ApiKey),
                "RESEND_API_KEY must be set.")
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.From),
                "RESEND_FROM must be set.")
            .ValidateOnStart();

        services.AddSingleton<ResendHttpClient>();
        services.AddSingleton<IMailService, MailService>();

        return services;
    }
}
