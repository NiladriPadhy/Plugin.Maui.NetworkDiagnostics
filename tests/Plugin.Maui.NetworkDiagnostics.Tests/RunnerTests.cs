namespace Plugin.Maui.NetworkDiagnostics.Tests;

public sealed class RunnerTests
{
    [Fact]
    public async Task All_layers_pass_and_summary_includes_latency()
    {
        var diagnostics = Harness.Create();

        var report = await diagnostics.RunAsync();

        Assert.True(report.Succeeded);
        Assert.Null(report.FirstFailure);
        Assert.True(report.Internet.Passed);
        Assert.True(report.Dns.Passed);
        Assert.True(report.Gateway.Passed);
        Assert.True(report.Tcp.Passed);
        Assert.True(report.Tls.Passed);
        Assert.True(report.Https.Passed);
        Assert.True(report.Api.Passed);
        Assert.NotNull(report.Latency);
        Assert.Contains("All connectivity checks passed.", report.Summary);
        Assert.Contains("Latency", report.Summary);
    }

    [Fact]
    public async Task Internet_up_api_down_is_the_support_sentence()
    {
        var http = new FakeHttpRequester
        {
            OnSend = uri => uri.AbsolutePath.Contains("health", StringComparison.Ordinal)
                ? new HttpProbeResult(false, null, TimeSpan.FromMilliseconds(30), "connection reset")
                : new HttpProbeResult(true, 200, TimeSpan.FromMilliseconds(40), null)
        };
        var diagnostics = Harness.Create(http: http);

        var report = await diagnostics.RunAsync();

        Assert.False(report.Succeeded);
        Assert.Equal(DiagnosticLayer.Api, report.FirstFailure);
        Assert.True(report.Internet.Passed);
        Assert.True(report.Https.Passed);
        Assert.True(report.Api.Failed);
        Assert.Equal("Internet is available, but API endpoint is unreachable.", report.Summary);
    }

    [Fact]
    public async Task Dns_failure_skips_dependent_layers()
    {
        var diagnostics = Harness.Create(
            dns: new FakeDnsLookup([], new System.Net.Sockets.SocketException()));

        var report = await diagnostics.RunAsync();

        Assert.True(report.Dns.Failed);
        Assert.True(report.Tcp.Skipped);
        Assert.True(report.Tls.Skipped);
        Assert.True(report.Https.Skipped);
        Assert.True(report.Api.Skipped);
        Assert.Equal("Internet is available, but DNS lookup failed for api.example.com.", report.Summary);
    }

    [Fact]
    public async Task Missing_api_endpoint_skips_api_and_can_still_succeed()
    {
        var diagnostics = Harness.Create(options => options.ApiEndpoint = null);

        var report = await diagnostics.RunAsync();

        Assert.True(report.Succeeded);
        Assert.True(report.Api.Skipped);
        Assert.Contains("No API endpoint configured.", report.Api.Detail);
        Assert.Contains("All connectivity checks passed.", report.Summary);
    }

    [Fact]
    public async Task Offline_device_uses_offline_summary()
    {
        var diagnostics = Harness.Create(
            internet: new FakeInternetAccess(new InternetSnapshot(false, false, "None", "")));

        var report = await diagnostics.RunAsync();

        Assert.True(report.Internet.Failed);
        Assert.Equal(DiagnosticLayer.Internet, report.FirstFailure);
        Assert.Equal("No internet. The device is offline.", report.Summary);
    }

    [Fact]
    public async Task Local_only_path_uses_local_network_summary()
    {
        var diagnostics = Harness.Create(
            internet: new FakeInternetAccess(new InternetSnapshot(true, false, "Local", "WiFi")));

        var report = await diagnostics.RunAsync();

        Assert.Equal("No internet. The device is on a local network only.", report.Summary);
    }

    [Fact]
    public async Task Tls_failure_keeps_tcp_and_explains_handshake()
    {
        var tls = new FakeTlsHandshaker
        {
            Result = new TlsHandshakeResult(false, TimeSpan.FromMilliseconds(20), null, "remote certificate invalid")
        };
        var diagnostics = Harness.Create(tls: tls);

        var report = await diagnostics.RunAsync();

        Assert.True(report.Tcp.Passed);
        Assert.True(report.Tls.Failed);
        Assert.True(report.Https.Skipped);
        Assert.Equal(
            "TCP works, but the TLS handshake failed (certificate, interception, or protocol).",
            report.Summary);
    }

    [Fact]
    public async Task Unhealthy_api_status_fails_api_layer()
    {
        var http = new FakeHttpRequester
        {
            OnSend = uri => uri.AbsolutePath.Contains("health", StringComparison.Ordinal)
                ? new HttpProbeResult(true, 503, TimeSpan.FromMilliseconds(25), null)
                : new HttpProbeResult(true, 200, TimeSpan.FromMilliseconds(20), null)
        };
        var diagnostics = Harness.Create(http: http);

        var report = await diagnostics.RunAsync();

        Assert.True(report.Api.Failed);
        Assert.Contains("503", report.Api.Error);
        Assert.Equal("Internet is available, but API endpoint is unreachable.", report.Summary);
    }

    [Fact]
    public async Task Gateway_refused_still_counts_as_reached()
    {
        var tcp = new FakeTcpConnector
        {
            OnConnect = (address, port) =>
                address.Equals(IPAddress.Parse("192.168.1.1"))
                    ? new TcpConnectResult(false, true, TimeSpan.FromMilliseconds(4), "connection refused")
                    : new TcpConnectResult(true, true, TimeSpan.FromMilliseconds(10), null)
        };
        var diagnostics = Harness.Create(tcp: tcp);

        var report = await diagnostics.RunAsync();

        Assert.True(report.Gateway.Passed);
        Assert.Contains("192.168.1.1", report.Gateway.Detail);
    }

    [Fact]
    public async Task CheckCompleted_fires_for_each_layer()
    {
        var diagnostics = Harness.Create();
        var seen = new List<DiagnosticLayer>();
        diagnostics.CheckCompleted += (_, check) => seen.Add(check.Layer);

        await diagnostics.RunAsync();

        Assert.Contains(DiagnosticLayer.Internet, seen);
        Assert.Contains(DiagnosticLayer.Dns, seen);
        Assert.Contains(DiagnosticLayer.Api, seen);
        Assert.Contains(DiagnosticLayer.Latency, seen);
    }
}
