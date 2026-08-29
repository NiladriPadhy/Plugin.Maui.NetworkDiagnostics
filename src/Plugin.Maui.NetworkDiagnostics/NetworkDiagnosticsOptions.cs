namespace Plugin.Maui.NetworkDiagnostics;

/// <summary>
/// Configuration for an on-demand diagnostic run.
/// </summary>
public sealed class NetworkDiagnosticsOptions
{
    /// <summary>
    /// Host used for DNS, TCP, TLS, and packet-timing samples.
    /// When unset and <see cref="ApiEndpoint"/> is set, the API host is used.
    /// Default: <c>one.one.one.one</c>.
    /// </summary>
    public string Host { get; set; } = "one.one.one.one";

    /// <summary>
    /// TCP / TLS port. Default: 443.
    /// </summary>
    public int Port { get; set; } = 443;

    /// <summary>
    /// HTTPS URI used for the HTTPS layer and as the latency source when no API is configured.
    /// Default: <c>https://one.one.one.one/</c>.
    /// </summary>
    public Uri HttpsUri { get; set; } = new("https://one.one.one.one/");

    /// <summary>
    /// Optional application health or API URL. When set, the API layer runs against it.
    /// </summary>
    public Uri? ApiEndpoint { get; set; }

    /// <summary>
    /// HTTP method for the API health check. Default: GET.
    /// </summary>
    public string ApiHttpMethod { get; set; } = "GET";

    /// <summary>
    /// Status codes treated as a healthy API. Default: 200 and 204.
    /// </summary>
    public IReadOnlyList<int> ApiSuccessStatusCodes { get; set; } = [200, 204];

    /// <summary>
    /// Per-check timeout. Default: 8 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Number of TCP connect samples for packet timing. Default: 5. Use 0 to skip.
    /// </summary>
    public int LatencySamples { get; set; } = 5;

    /// <summary>
    /// When true, later layers still run after an earlier failure.
    /// When false (default), dependent layers are skipped so the first break is obvious.
    /// </summary>
    public bool ContinueAfterFailure { get; set; }

    /// <summary>
    /// User-Agent sent with HTTPS and API requests.
    /// </summary>
    public string UserAgent { get; set; } = "Plugin.Maui.NetworkDiagnostics/1.0";

    internal NetworkDiagnosticsOptions Clone() =>
        new()
        {
            Host = Host,
            Port = Port,
            HttpsUri = HttpsUri,
            ApiEndpoint = ApiEndpoint,
            ApiHttpMethod = ApiHttpMethod,
            ApiSuccessStatusCodes = [.. ApiSuccessStatusCodes],
            Timeout = Timeout,
            LatencySamples = LatencySamples,
            ContinueAfterFailure = ContinueAfterFailure,
            UserAgent = UserAgent
        };

    internal string ResolveHost()
    {
        if (!string.IsNullOrWhiteSpace(Host) && !string.Equals(Host, "one.one.one.one", StringComparison.OrdinalIgnoreCase))
            return Host;

        if (ApiEndpoint is not null && !string.IsNullOrWhiteSpace(ApiEndpoint.Host))
            return ApiEndpoint.Host;

        if (HttpsUri is not null && !string.IsNullOrWhiteSpace(HttpsUri.Host))
            return HttpsUri.Host;

        return string.IsNullOrWhiteSpace(Host) ? "one.one.one.one" : Host;
    }
}
