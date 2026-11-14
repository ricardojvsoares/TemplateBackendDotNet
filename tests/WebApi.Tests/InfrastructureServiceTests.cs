using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.Abstractions.Identity;
using Application.Abstractions.Messaging;
using Application.Features.Auth.Jobs;
using Application.Settings;
using Domain.Authorization;
using Domain.Features.Users;
using Infrastructure.Services.Cache;
using Infrastructure.Services.Identity;
using Infrastructure.Services.Messaging;
using Infrastructure.Services.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace WebApi.Tests;

[Collection(ApiSuite.Name)]
public sealed class InfrastructureServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApiFixture _fixture;

    public InfrastructureServiceTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StorageRoundTripsTheUploadedBytes()
    {
        byte[] payload = [1, 2, 3];
        using var client = _fixture.CreateStorageClient();
        var storage = new StorageService(
            NullLogger<StorageService>.Instance,
            client,
            Options.Create(StorageSettings()));

        await using var source = new MemoryStream(payload);
        await storage.StoreAsync(source, "probe", "payload.bin", "application/octet-stream", CancellationToken.None);

        using MemoryStream downloaded = await storage.DownloadAsync("probe/payload.bin", CancellationToken.None);
        Assert.Equal(payload, downloaded.ToArray());
    }

    [Fact]
    public async Task MessageServiceDeliversThePublishedPayload()
    {
        await using var messages = new MessageService(
            Options.Create(new MessageServiceSettings
            {
                Host = _fixture.RabbitHost,
                Port = _fixture.RabbitPort,
                Username = ApiFixture.RabbitUser,
                Password = ApiFixture.RabbitPassword
            }),
            NullLogger<MessageService>.Instance);

        var received = new TaskCompletionSource<ProbeMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<ProbeMessage, CancellationToken, Task> handler = (message, _) =>
        {
            received.TrySetResult(message);
            return Task.CompletedTask;
        };

        await messages.SubscribeAsync("service-probe", handler, CancellationToken.None);
        try
        {
            await messages.PublishAsync(
                "service-probe",
                new ProbeMessage("probe-value"),
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                CancellationToken.None);

            Task completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(received.Task, completed);
            ProbeMessage message = await received.Task;
            Assert.Equal("probe-value", message.Value);
        }
        finally
        {
            await messages.UnsubscribeAsync("service-probe", handler, CancellationToken.None);
        }
    }

    [Fact]
    public async Task CacheStoresAndRemovesAValue()
    {
        await using var connection = await ConnectionMultiplexer.ConnectAsync(
            $"{_fixture.RedisHost}:{_fixture.RedisPort}");
        CacheService cache = new CacheService(connection);

        await cache.SetAsync("probe-key", "probe-value", TimeSpan.FromMinutes(1));
        Assert.Equal("probe-value", await cache.GetAsync("probe-key"));

        await cache.RemoveAsync("probe-key");
        Assert.Null(await cache.GetAsync("probe-key"));
    }

    [Fact]
    public async Task PurgeDeletesRefreshTokensThatArePastExpiry()
    {
        string email = $"{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage signup = await _fixture.Client.PostAsJsonAsync(
            "/api/v1/auth/signup",
            new { firstName = "Purge", lastName = "User", email, password = "UserPass123" });
        string body = await signup.Content.ReadAsStringAsync();
        Assert.True(signup.StatusCode == HttpStatusCode.Created, body);
        TokenResponse? tokens = JsonSerializer.Deserialize<TokenResponse>(body, JsonOptions);
        Assert.NotNull(tokens);

        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        var job = new PurgeExpiredRefreshTokensJob(
            scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>(),
            new AheadTimeProvider(),
            NullLogger<PurgeExpiredRefreshTokensJob>.Instance);
        await job.ExecuteAsync(CancellationToken.None);

        using HttpResponseMessage refresh = await _fixture.Client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = tokens.RefreshToken });
        string refreshBody = await refresh.Content.ReadAsStringAsync();
        Assert.True(refresh.StatusCode == HttpStatusCode.Unauthorized, refreshBody);
        Assert.Contains("Auth.InvalidRefreshToken", refreshBody, StringComparison.Ordinal);
    }

    private StorageServiceSettings StorageSettings() =>
        new()
        {
            Endpoint = _fixture.StorageEndpoint,
            AccessKey = ApiFixture.StorageAccessKey,
            SecretKey = ApiFixture.StorageSecretKey,
            Bucket = ApiFixture.StorageBucket,
            Region = ApiFixture.StorageRegion
        };

    private sealed record ProbeMessage(string Value);

    private sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime AccessExpiresAt);

    private sealed class AheadTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddDays(8);
    }
}
