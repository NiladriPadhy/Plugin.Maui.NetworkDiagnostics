namespace Plugin.Maui.NetworkDiagnostics;

/// <summary>
/// Static entry point for on-demand connectivity diagnostics.
/// </summary>
public static class NetworkDiagnostics
{
    static INetworkDiagnostics? current;

    /// <summary>
    /// Gets the instance registered by <c>UseNetworkDiagnostics</c> or created by <see cref="Configure"/>.
    /// </summary>
    public static INetworkDiagnostics Current =>
        current ?? throw new InvalidOperationException(
            "NetworkDiagnostics is not initialized. Call builder.UseNetworkDiagnostics() or NetworkDiagnostics.Configure().");

    /// <summary>
    /// Updates options on the shared instance, creating one if needed.
    /// </summary>
    public static void Configure(Action<NetworkDiagnosticsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (current is not null)
        {
            current.Configure(configure);
            return;
        }

        var options = new NetworkDiagnosticsOptions();
        configure(options);
        SetDefault(Create(options));
    }

    /// <summary>
    /// Runs the diagnostic stack. Creates a default instance when none is registered.
    /// </summary>
    /// <example>
    /// <code>
    /// var result = await NetworkDiagnostics.RunAsync();
    /// // Internet is available, but API endpoint is unreachable.
    /// </code>
    /// </example>
    public static Task<NetworkDiagnosticReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var instance = current ?? Create();
        return instance.RunAsync(cancellationToken);
    }

    /// <summary>
    /// Runs the diagnostic stack with a one-shot options override.
    /// </summary>
    public static Task<NetworkDiagnosticReport> RunAsync(
        NetworkDiagnosticsOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var instance = current ?? Create(options);
        return instance.RunAsync(options, cancellationToken);
    }

    /// <summary>
    /// Creates a diagnostics instance. Does not replace <see cref="Current"/> unless none exists.
    /// </summary>
    public static INetworkDiagnostics Create(NetworkDiagnosticsOptions? options = null)
    {
        var instance = Create(
            options ?? new NetworkDiagnosticsOptions(),
            new MauiInternetAccess(),
            new SystemDnsLookup(),
            new SocketTcpConnector(),
            new SslTlsHandshaker(),
            new SocketsHttpRequester(),
            GatewayResolver.Create());

        current ??= instance;
        return instance;
    }

    /// <summary>
    /// Replaces the shared instance. Intended for tests and custom implementations.
    /// </summary>
    public static void SetDefault(INetworkDiagnostics implementation) =>
        current = implementation ?? throw new ArgumentNullException(nameof(implementation));

    internal static NetworkDiagnosticsImplementation Create(
        NetworkDiagnosticsOptions options,
        IInternetAccess internet,
        IDnsLookup dns,
        ITcpConnector tcp,
        ITlsHandshaker tls,
        IHttpRequester http,
        IGatewayResolver gateway) =>
        new(options, internet, dns, tcp, tls, http, gateway);

    internal static void SetCurrent(INetworkDiagnostics? instance) => current = instance;
}
