namespace Plugin.Maui.NetworkDiagnostics.Tests;

sealed class FakeInternetAccess(InternetSnapshot snapshot) : IInternetAccess
{
    public InternetSnapshot GetSnapshot() => snapshot;
}

sealed class FakeDnsLookup(IPAddress[] addresses, Exception? error = null) : IDnsLookup
{
    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        if (error is not null)
            throw error;

        return Task.FromResult(addresses);
    }
}

sealed class FakeTcpConnector : ITcpConnector
{
    public Func<IPAddress, int, TcpConnectResult> OnConnect { get; set; } =
        (address, port) => new TcpConnectResult(true, true, TimeSpan.FromMilliseconds(12), null);

    public Task<TcpConnectResult> ConnectAsync(
        IPAddress address,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        Task.FromResult(OnConnect(address, port));
}

sealed class FakeTlsHandshaker : ITlsHandshaker
{
    public TlsHandshakeResult Result { get; set; } =
        new(true, TimeSpan.FromMilliseconds(40), "Tls13", null);

    public Task<TlsHandshakeResult> HandshakeAsync(
        string host,
        IPAddress address,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result);
}

sealed class FakeHttpRequester : IHttpRequester
{
    public Func<Uri, HttpProbeResult> OnSend { get; set; } =
        uri => new HttpProbeResult(true, 200, TimeSpan.FromMilliseconds(80), null);

    public Task<HttpProbeResult> SendAsync(
        Uri uri,
        string method,
        TimeSpan timeout,
        string userAgent,
        CancellationToken cancellationToken) =>
        Task.FromResult(OnSend(uri));
}

sealed class FakeGatewayResolver(params IPAddress[] addresses) : IGatewayResolver
{
    public Task<IReadOnlyList<IPAddress>> ResolveAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<IPAddress>>(addresses);
}

static class Harness
{
    public static NetworkDiagnosticsImplementation Create(
        Action<NetworkDiagnosticsOptions>? configure = null,
        IInternetAccess? internet = null,
        IDnsLookup? dns = null,
        FakeTcpConnector? tcp = null,
        FakeTlsHandshaker? tls = null,
        FakeHttpRequester? http = null,
        IGatewayResolver? gateway = null)
    {
        var options = new NetworkDiagnosticsOptions
        {
            Host = "api.example.com",
            HttpsUri = new Uri("https://api.example.com/"),
            ApiEndpoint = new Uri("https://api.example.com/health"),
            LatencySamples = 2
        };
        configure?.Invoke(options);

        return NetworkDiagnostics.Create(
            options,
            internet ?? new FakeInternetAccess(new InternetSnapshot(true, true, "Internet", "WiFi")),
            dns ?? new FakeDnsLookup([IPAddress.Parse("1.2.3.4")]),
            tcp ?? new FakeTcpConnector(),
            tls ?? new FakeTlsHandshaker(),
            http ?? new FakeHttpRequester(),
            gateway ?? new FakeGatewayResolver(IPAddress.Parse("192.168.1.1")));
    }
}
