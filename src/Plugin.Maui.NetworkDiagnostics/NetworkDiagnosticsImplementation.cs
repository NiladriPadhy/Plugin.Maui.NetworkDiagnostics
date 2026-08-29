namespace Plugin.Maui.NetworkDiagnostics;

sealed class NetworkDiagnosticsImplementation : INetworkDiagnostics
{
    readonly IInternetAccess internet;
    readonly IDnsLookup dns;
    readonly ITcpConnector tcp;
    readonly ITlsHandshaker tls;
    readonly IHttpRequester http;
    readonly IGatewayResolver gateway;
    readonly DiagnosticRunner runner;

    public NetworkDiagnosticsImplementation(
        NetworkDiagnosticsOptions options,
        IInternetAccess internet,
        IDnsLookup dns,
        ITcpConnector tcp,
        ITlsHandshaker tls,
        IHttpRequester http,
        IGatewayResolver gateway)
    {
        Options = options;
        this.internet = internet;
        this.dns = dns;
        this.tcp = tcp;
        this.tls = tls;
        this.http = http;
        this.gateway = gateway;
        runner = new DiagnosticRunner(internet, dns, tcp, tls, http, gateway);
    }

    public NetworkDiagnosticsOptions Options { get; }

    public event EventHandler<DiagnosticCheck>? CheckCompleted;

    public void Configure(Action<NetworkDiagnosticsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Options);
    }

    public Task<NetworkDiagnosticReport> RunAsync(CancellationToken cancellationToken = default) =>
        RunAsync(Options.Clone(), cancellationToken);

    public async Task<NetworkDiagnosticReport> RunAsync(
        NetworkDiagnosticsOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        return await runner.RunAsync(options, OnCheckCompleted, cancellationToken).ConfigureAwait(false);
    }

    void OnCheckCompleted(DiagnosticCheck check) =>
        CheckCompleted?.Invoke(this, check);
}
