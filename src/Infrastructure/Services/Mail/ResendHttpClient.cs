namespace Infrastructure.Services.Mail;

public sealed class ResendHttpClient : HttpClient
{
    public ResendHttpClient()
        : this(new SocketsHttpHandler(), disposeHandler: true)
    {
    }

    public static ResendHttpClient Create(HttpMessageHandler handler) =>
        new(handler, disposeHandler: false);

    private ResendHttpClient(HttpMessageHandler handler, bool disposeHandler)
        : base(handler, disposeHandler)
    {
#pragma warning disable S1075 // Resend's API host is fixed. The key and from-address come from configuration.
        BaseAddress = new Uri("https://api.resend.com/");
#pragma warning restore S1075
    }
}
