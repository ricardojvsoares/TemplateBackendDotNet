using System.Globalization;
using System.Reflection;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DbUp;
using DbUp.Engine;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace WebApi.Tests;

public sealed class ApiFixture : IAsyncLifetime, IDisposable
{
    public const string AdminEmail = "admin@example.com";

    public const string AdminPassword = "AdminPass123";

    public const string AdminFirstName = "Ada";

    public const string AdminLastName = "Admin";

    public const string RabbitUser = "guest";

    public const string RabbitPassword = "guest";

    public const string StorageAccessKey = "testaccess";

    public const string StorageSecretKey = "testsecret";

    public const string StorageBucket = "files";

    public const string StorageRegion = "us-east-1";

    private PostgreSqlContainer? _postgres;

    private RedisContainer? _redis;

    private RabbitMqContainer? _rabbit;

    private IContainer? _minio;

    private ApiFactory? _factory;

    private int _disposed;

    public HttpClient Client { get; private set; } = null!;

    public IServiceProvider Services => _factory!.Services;

    public string RabbitHost { get; private set; } = string.Empty;

    public int RabbitPort { get; private set; }

    public string RedisHost { get; private set; } = string.Empty;

    public int RedisPort { get; private set; }

    public string StorageEndpoint { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("backlog_tests")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        _redis = new RedisBuilder("redis:8").Build();

        _rabbit = new RabbitMqBuilder("rabbitmq:4.4.0-management")
            .WithUsername(RabbitUser)
            .WithPassword(RabbitPassword)
            .Build();

        _minio = new ContainerBuilder("adobe/s3mock:latest")
            .WithPortBinding(9090, true)
            .WithEnvironment("initialBuckets", StorageBucket)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Started S3MockApplication"))
            .Build();

        await _postgres.StartAsync();
        await _redis.StartAsync();
        await _rabbit.StartAsync();
        await _minio.StartAsync();

        RabbitHost = _rabbit.Hostname;
        RabbitPort = _rabbit.GetMappedPublicPort(5672);
        RedisHost = _redis.Hostname;
        RedisPort = _redis.GetMappedPublicPort(6379);
        StorageEndpoint = new UriBuilder(
            "http",
            _minio.Hostname,
            _minio.GetMappedPublicPort(9090)).Uri.ToString().TrimEnd('/');

        var database = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString());
        ApplyMigrations(database.ConnectionString);
        await CreateBucketAsync();
        SetEnvironment(database, _redis);

        _factory = new ApiFactory();
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public async Task DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        Client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_minio is not null)
        {
            await _minio.DisposeAsync();
        }

        if (_rabbit is not null)
        {
            await _rabbit.DisposeAsync();
        }

        if (_redis is not null)
        {
            await _redis.DisposeAsync();
        }

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    public AmazonS3Client CreateStorageClient()
    {
        var config = new AmazonS3Config
        {
            ServiceURL = StorageEndpoint,
            ForcePathStyle = true,
            AuthenticationRegion = StorageRegion,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        };

        return new AmazonS3Client(
            new BasicAWSCredentials(StorageAccessKey, StorageSecretKey),
            config);
    }

    private async Task CreateBucketAsync()
    {
        using AmazonS3Client client = CreateStorageClient();
        await client.PutBucketAsync(new PutBucketRequest { BucketName = StorageBucket });
    }

    private static void ApplyMigrations(string connectionString)
    {
        Assembly migrations = Assembly.Load("Migrations");

        UpgradeEngine upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(migrations)
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        DatabaseUpgradeResult result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException("Database migration failed.", result.Error);
        }
    }

    private void SetEnvironment(NpgsqlConnectionStringBuilder database, RedisContainer redis)
    {
        var values = new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["ASPNETCORE_HTTPS_PORT"] = null,
            ["HTTPS_PORT"] = null,
            ["POSTGRES_HOST"] = database.Host,
            ["POSTGRES_PORT"] = database.Port.ToString(CultureInfo.InvariantCulture),
            ["POSTGRES_DATABASE"] = database.Database,
            ["POSTGRES_USERNAME"] = database.Username,
            ["POSTGRES_PASSWORD"] = database.Password,
            ["REDIS_HOST"] = redis.Hostname,
            ["REDIS_PORT"] = redis.GetMappedPublicPort(6379).ToString(CultureInfo.InvariantCulture),
            ["REDIS_PASSWORD"] = string.Empty,
            ["RABBITMQ_HOST"] = RabbitHost,
            ["RABBITMQ_PORT"] = RabbitPort.ToString(CultureInfo.InvariantCulture),
            ["RABBITMQ_USERNAME"] = RabbitUser,
            ["RABBITMQ_PASSWORD"] = RabbitPassword,
            ["STORAGE_ENDPOINT"] = StorageEndpoint,
            ["STORAGE_ACCESS_KEY"] = StorageAccessKey,
            ["STORAGE_SECRET_KEY"] = StorageSecretKey,
            ["STORAGE_BUCKET"] = StorageBucket,
            ["STORAGE_REGION"] = StorageRegion,
            ["AUTH_ISSUER"] = "https://tests.local",
            ["AUTH_AUDIENCE"] = "tests",
            ["AUTH_SIGNING_KEY"] = "test-signing-key-at-least-32-chars",
            ["AUTH_ACCESS_TOKEN_MINUTES"] = "15",
            ["AUTH_REFRESH_TOKEN_DAYS"] = "7",
            ["AUTH_ADMIN_EMAIL"] = AdminEmail,
            ["AUTH_ADMIN_PASSWORD"] = AdminPassword,
            ["AUTH_ADMIN_FIRST_NAME"] = AdminFirstName,
            ["AUTH_ADMIN_LAST_NAME"] = AdminLastName,
            ["RESEND_API_KEY"] = "re_test_key",
            ["RESEND_FROM"] = "test@example.com",
            ["RATE_LIMIT_PREFIX"] = "suite",
            ["RATE_LIMIT_GLOBAL_PERMIT"] = "10000",
            ["RATE_LIMIT_GLOBAL_WINDOW_SECONDS"] = "60",
            ["RATE_LIMIT_AUTH_PERMIT"] = "10000",
            ["RATE_LIMIT_AUTH_WINDOW_SECONDS"] = "60",
            ["RATE_LIMIT_UPLOAD_PERMIT"] = "10000",
            ["RATE_LIMIT_UPLOAD_WINDOW_SECONDS"] = "60"
        };

        foreach ((string key, string? value) in values)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
