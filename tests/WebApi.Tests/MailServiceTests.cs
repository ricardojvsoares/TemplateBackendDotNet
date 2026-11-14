using System.Net;
using System.Text.Json;
using Application.Abstractions.Mail;
using Application.Settings;
using Infrastructure.Services.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace WebApi.Tests;

public sealed class MailServiceTests
{
    [Fact]
    public async Task SendPostsResendPayload()
    {
        using var handler = new CaptureHandler();
        using TestMail mail = CreateMail(handler, "re_test_key", "Test <test@example.com>");
        MailService service = mail.Service;
        byte[] content = [1, 2, 3];

        await service.SendAsync(
            ["one@example.com", "two@example.com"],
            ["cc@example.com"],
            "Hello",
            "plain text",
            [
                new EmailAttachment
                {
                    FileName = "note.txt",
                    ContentType = "text/plain",
                    Content = content
                }
            ],
            CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(new Uri("https://api.resend.com/emails"), handler.Uri);
        Assert.Equal("Bearer re_test_key", handler.Authorization);

        using JsonDocument document = JsonDocument.Parse(handler.Body);
        JsonElement root = document.RootElement;
        Assert.Equal("Test <test@example.com>", root.GetProperty("from").GetString());
        Assert.Equal("one@example.com", root.GetProperty("to")[0].GetString());
        Assert.Equal("two@example.com", root.GetProperty("to")[1].GetString());
        Assert.Equal("cc@example.com", root.GetProperty("cc")[0].GetString());
        Assert.Equal("Hello", root.GetProperty("subject").GetString());
        Assert.Equal("plain text", root.GetProperty("text").GetString());
        JsonElement attachment = root.GetProperty("attachments")[0];
        Assert.Equal("note.txt", attachment.GetProperty("filename").GetString());
        Assert.Equal("AQID", attachment.GetProperty("content").GetString());
        Assert.Equal("text/plain", attachment.GetProperty("content_type").GetString());
    }

    [Fact]
    public async Task SendOmitsCcAndAttachmentsWhenAbsent()
    {
        using var handler = new CaptureHandler();
        using TestMail mail = CreateMail(handler, "re_test_key", "from@example.com");
        MailService service = mail.Service;

        await service.SendAsync(
            ["one@example.com"],
            cc: null,
            "Hello",
            "plain text",
            attachments: null,
            CancellationToken.None);

        using JsonDocument document = JsonDocument.Parse(handler.Body);
        Assert.False(document.RootElement.TryGetProperty("cc", out _));
        Assert.False(document.RootElement.TryGetProperty("attachments", out _));
    }

    [Fact]
    public async Task SendWithoutRecipientsThrowsBeforeTheRequest()
    {
        using var handler = new CaptureHandler();
        using TestMail mail = CreateMail(handler, "re_test_key", "from@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() => mail.Service.SendAsync(
            [],
            cc: null,
            "Hello",
            "plain text",
            attachments: null,
            CancellationToken.None));

        Assert.False(handler.Called);
    }

    [Fact]
    public async Task RejectedSendThrowsAndDoesNotLogTheApiKey()
    {
        using var handler = new CaptureHandler { StatusCode = HttpStatusCode.UnprocessableEntity };
        var logger = new CaptureLogger();
        using TestMail mail = CreateMail(handler, logger, "re_secret", "from@example.com");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mail.Service.SendAsync(
                ["one@example.com"],
                cc: null,
                "Hello",
                "plain text",
                attachments: null,
                CancellationToken.None));

        Assert.Contains("422", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("re_secret", exception.Message, StringComparison.Ordinal);
        string logs = string.Join('\n', logger.Messages);
        Assert.Contains("422", logs, StringComparison.Ordinal);
        Assert.DoesNotContain("re_secret", logs, StringComparison.Ordinal);
    }

    private static TestMail CreateMail(CaptureHandler handler, string apiKey, string from) =>
        CreateMail(handler, new CaptureLogger(), apiKey, from);

    private static TestMail CreateMail(
        CaptureHandler handler,
        CaptureLogger logger,
        string apiKey,
        string from)
    {
        return new TestMail(handler, logger, apiKey, from);
    }

    private sealed class TestMail : IDisposable
    {
        private readonly ResendHttpClient _client;

        public TestMail(CaptureHandler handler, CaptureLogger logger, string apiKey, string from)
        {
            _client = ResendHttpClient.Create(handler);
            Service = new MailService(
                _client,
                logger,
                Options.Create(new MailServiceSettings
                {
                    ApiKey = apiKey,
                    From = from
                }));
        }

        public MailService Service { get; }

        public void Dispose()
        {
            _client.Dispose();
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

        public bool Called { get; private set; }

        public HttpMethod? Method { get; private set; }

        public Uri? Uri { get; private set; }

        public string? Authorization { get; private set; }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Called = true;
            Method = request.Method;
            Uri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(StatusCode);
        }
    }

    private sealed class CaptureLogger : ILogger<MailService>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
