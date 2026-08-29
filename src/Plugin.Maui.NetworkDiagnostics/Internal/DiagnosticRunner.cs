using System.Diagnostics;
using System.Net;

namespace Plugin.Maui.NetworkDiagnostics;

sealed class DiagnosticRunner
{
    readonly IInternetAccess internet;
    readonly IDnsLookup dns;
    readonly ITcpConnector tcp;
    readonly ITlsHandshaker tls;
    readonly IHttpRequester http;
    readonly IGatewayResolver gateway;

    public DiagnosticRunner(
        IInternetAccess internet,
        IDnsLookup dns,
        ITcpConnector tcp,
        ITlsHandshaker tls,
        IHttpRequester http,
        IGatewayResolver gateway)
    {
        this.internet = internet;
        this.dns = dns;
        this.tcp = tcp;
        this.tls = tls;
        this.http = http;
        this.gateway = gateway;
    }

    public async Task<NetworkDiagnosticReport> RunAsync(
        NetworkDiagnosticsOptions options,
        Action<DiagnosticCheck> onCheck,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var checks = new List<DiagnosticCheck>(8);
        var host = options.ResolveHost();
        IPAddress[] addresses = [];

        var internetCheck = CheckInternet();
        Add(checks, internetCheck, onCheck);

        var dnsCheck = await CheckDnsAsync(host, options.Timeout, cancellationToken).ConfigureAwait(false);
        Add(checks, dnsCheck, onCheck);
        if (dnsCheck.Passed)
            addresses = ParseAddresses(dnsCheck.Detail);

        var gatewayCheck = await CheckGatewayAsync(options, cancellationToken).ConfigureAwait(false);
        Add(checks, gatewayCheck, onCheck);

        var canContinue = dnsCheck.Passed || options.ContinueAfterFailure;
        var tcpCheck = canContinue && addresses.Length > 0
            ? await CheckTcpAsync(addresses, options.Port, options.Timeout, cancellationToken).ConfigureAwait(false)
            : DiagnosticCheck.Skip(
                DiagnosticLayer.Tcp,
                "TCP",
                dnsCheck.Passed ? "No addresses to connect to." : "Skipped because DNS failed.");
        Add(checks, tcpCheck, onCheck);

        canContinue = tcpCheck.Passed || options.ContinueAfterFailure;
        var tlsCheck = canContinue && addresses.Length > 0
            ? await CheckTlsAsync(host, addresses, options.Port, options.Timeout, cancellationToken).ConfigureAwait(false)
            : DiagnosticCheck.Skip(
                DiagnosticLayer.Tls,
                "TLS",
                tcpCheck.Passed ? "No addresses for TLS." : "Skipped because TCP failed.");
        Add(checks, tlsCheck, onCheck);

        canContinue = tlsCheck.Passed || options.ContinueAfterFailure;
        var httpsCheck = canContinue
            ? await CheckHttpAsync(
                DiagnosticLayer.Https,
                "HTTPS",
                options.HttpsUri,
                "GET",
                options,
                cancellationToken).ConfigureAwait(false)
            : DiagnosticCheck.Skip(DiagnosticLayer.Https, "HTTPS", "Skipped because TLS failed.");
        Add(checks, httpsCheck, onCheck);

        DiagnosticCheck apiCheck;
        if (options.ApiEndpoint is null)
        {
            apiCheck = DiagnosticCheck.Skip(DiagnosticLayer.Api, "API", "No API endpoint configured.");
        }
        else if (httpsCheck.Passed || options.ContinueAfterFailure)
        {
            apiCheck = await CheckApiAsync(options, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            apiCheck = DiagnosticCheck.Skip(DiagnosticLayer.Api, "API", "Skipped because HTTPS failed.");
        }

        Add(checks, apiCheck, onCheck);

        TimeSpan? latency = apiCheck.Passed
            ? apiCheck.Duration
            : httpsCheck.Passed
                ? httpsCheck.Duration
                : tcpCheck.Passed ? tcpCheck.Duration : null;

        PacketTiming? timing = null;
        if (options.LatencySamples > 0 && addresses.Length > 0 && (dnsCheck.Passed || options.ContinueAfterFailure))
        {
            timing = await SamplePacketTimingAsync(
                addresses,
                options.Port,
                options.Timeout,
                options.LatencySamples,
                cancellationToken).ConfigureAwait(false);
        }

        var latencyCheck = latency is { } measured
            ? DiagnosticCheck.Pass(
                DiagnosticLayer.Latency,
                "Latency",
                measured,
                timing is null
                    ? null
                    : $"min {(int)Math.Round(timing.Min.TotalMilliseconds)}ms p95 {(int)Math.Round(timing.P95.TotalMilliseconds)}ms")
            : DiagnosticCheck.Skip(DiagnosticLayer.Latency, "Latency", "No successful request or connect to measure.");
        Add(checks, latencyCheck, onCheck);

        var summary = SummaryComposer.Compose(checks, latency, host);
        return new NetworkDiagnosticReport(checks, latency, timing, summary, startedAt, DateTimeOffset.UtcNow);
    }

    DiagnosticCheck CheckInternet()
    {
        var snapshot = internet.GetSnapshot();
        if (snapshot.Access is "Unavailable" or "Unknown")
        {
            return DiagnosticCheck.Pass(
                DiagnosticLayer.Internet,
                "Internet",
                TimeSpan.Zero,
                "OS connectivity is unknown; continuing.");
        }

        if (snapshot.HasInternet)
        {
            var detail = string.IsNullOrWhiteSpace(snapshot.Profiles)
                ? snapshot.Access
                : $"{snapshot.Access} ({snapshot.Profiles})";
            return DiagnosticCheck.Pass(DiagnosticLayer.Internet, "Internet", TimeSpan.Zero, detail);
        }

        if (snapshot.Access.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            return DiagnosticCheck.Fail(
                DiagnosticLayer.Internet,
                "Internet",
                TimeSpan.Zero,
                "Local network only.",
                snapshot.Access);
        }

        return DiagnosticCheck.Fail(
            DiagnosticLayer.Internet,
            "Internet",
            TimeSpan.Zero,
            "Device is offline.",
            snapshot.Access);
    }

    async Task<DiagnosticCheck> CheckDnsAsync(string host, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(timeout);
            var addresses = await dns.ResolveAsync(host, linked.Token).ConfigureAwait(false);
            watch.Stop();

            if (addresses.Length == 0)
            {
                return DiagnosticCheck.Fail(
                    DiagnosticLayer.Dns,
                    "DNS",
                    watch.Elapsed,
                    $"No addresses for {host}.");
            }

            return DiagnosticCheck.Pass(
                DiagnosticLayer.Dns,
                "DNS",
                watch.Elapsed,
                string.Join(", ", addresses.Select(static address => address.ToString())));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DiagnosticCheck.Fail(DiagnosticLayer.Dns, "DNS", watch.Elapsed, $"Lookup for {host} timed out.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return DiagnosticCheck.Fail(DiagnosticLayer.Dns, "DNS", watch.Elapsed, ex.Message);
        }
    }

    async Task<DiagnosticCheck> CheckGatewayAsync(NetworkDiagnosticsOptions options, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        IReadOnlyList<IPAddress> gateways;
        try
        {
            gateways = await gateway.ResolveAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return DiagnosticCheck.Fail(DiagnosticLayer.Gateway, "Gateway", watch.Elapsed, ex.Message);
        }

        if (gateways.Count == 0)
        {
            return DiagnosticCheck.Skip(
                DiagnosticLayer.Gateway,
                "Gateway",
                "Default gateway address is not available on this platform.");
        }

        var target = gateways[0];
        var probePort = options.Port == 443 ? 53 : options.Port;
        var result = await tcp.ConnectAsync(target, probePort, options.Timeout, cancellationToken).ConfigureAwait(false);
        watch.Stop();

        if (result.Connected || result.Reached)
        {
            var detail = result.Connected
                ? $"{target} reachable"
                : $"{target} reached ({result.Error})";
            return DiagnosticCheck.Pass(DiagnosticLayer.Gateway, "Gateway", watch.Elapsed, detail);
        }

        return DiagnosticCheck.Fail(
            DiagnosticLayer.Gateway,
            "Gateway",
            watch.Elapsed,
            result.Error ?? "unreachable",
            target.ToString());
    }

    async Task<DiagnosticCheck> CheckTcpAsync(
        IReadOnlyList<IPAddress> addresses,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        foreach (var address in addresses)
        {
            try
            {
                var result = await tcp.ConnectAsync(address, port, timeout, cancellationToken).ConfigureAwait(false);
                if (result.Connected)
                    return DiagnosticCheck.Pass(DiagnosticLayer.Tcp, "TCP", result.Duration, $"{address}:{port}");

                lastError = new InvalidOperationException(result.Error ?? "connect failed");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
            }
        }

        return DiagnosticCheck.Fail(
            DiagnosticLayer.Tcp,
            "TCP",
            TimeSpan.Zero,
            lastError?.Message ?? $"Could not connect to port {port}.");
    }

    async Task<DiagnosticCheck> CheckTlsAsync(
        string host,
        IReadOnlyList<IPAddress> addresses,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        foreach (var address in addresses)
        {
            try
            {
                var result = await tls.HandshakeAsync(host, address, port, timeout, cancellationToken).ConfigureAwait(false);
                if (result.Succeeded)
                    return DiagnosticCheck.Pass(DiagnosticLayer.Tls, "TLS", result.Duration, result.Protocol);

                lastError = new InvalidOperationException(result.Error ?? "handshake failed");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
            }
        }

        return DiagnosticCheck.Fail(
            DiagnosticLayer.Tls,
            "TLS",
            TimeSpan.Zero,
            lastError?.Message ?? "TLS handshake failed.");
    }

    async Task<DiagnosticCheck> CheckHttpAsync(
        DiagnosticLayer layer,
        string name,
        Uri uri,
        string method,
        NetworkDiagnosticsOptions options,
        CancellationToken cancellationToken)
    {
        var result = await http.SendAsync(uri, method, options.Timeout, options.UserAgent, cancellationToken)
            .ConfigureAwait(false);

        if (result.Succeeded)
            return DiagnosticCheck.Pass(layer, name, result.Duration, $"{(int?)result.StatusCode} {uri.Host}");

        return DiagnosticCheck.Fail(layer, name, result.Duration, result.Error ?? "request failed", uri.ToString());
    }

    async Task<DiagnosticCheck> CheckApiAsync(NetworkDiagnosticsOptions options, CancellationToken cancellationToken)
    {
        var result = await http.SendAsync(
            options.ApiEndpoint!,
            options.ApiHttpMethod,
            options.Timeout,
            options.UserAgent,
            cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return DiagnosticCheck.Fail(
                DiagnosticLayer.Api,
                "API",
                result.Duration,
                result.Error ?? "request failed",
                options.ApiEndpoint!.ToString());
        }

        if (result.StatusCode is { } code && !options.ApiSuccessStatusCodes.Contains(code))
        {
            return DiagnosticCheck.Fail(
                DiagnosticLayer.Api,
                "API",
                result.Duration,
                $"HTTP {code} is not a healthy status.",
                options.ApiEndpoint!.ToString());
        }

        return DiagnosticCheck.Pass(
            DiagnosticLayer.Api,
            "API",
            result.Duration,
            $"{result.StatusCode} {options.ApiEndpoint!.Host}");
    }

    async Task<PacketTiming?> SamplePacketTimingAsync(
        IReadOnlyList<IPAddress> addresses,
        int port,
        TimeSpan timeout,
        int samples,
        CancellationToken cancellationToken)
    {
        var collected = new List<TimeSpan>(samples);
        var address = addresses[0];
        for (var i = 0; i < samples; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await tcp.ConnectAsync(address, port, timeout, cancellationToken).ConfigureAwait(false);
            if (result.Connected || result.Reached)
                collected.Add(result.Duration);
        }

        return PacketTimingCalculator.From(collected);
    }

    static void Add(List<DiagnosticCheck> checks, DiagnosticCheck check, Action<DiagnosticCheck> onCheck)
    {
        checks.Add(check);
        onCheck(check);
    }

    static IPAddress[] ParseAddresses(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return [];

        var list = new List<IPAddress>();
        foreach (var part in detail.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (IPAddress.TryParse(part, out var address))
                list.Add(address);
        }

        return [.. list];
    }
}
