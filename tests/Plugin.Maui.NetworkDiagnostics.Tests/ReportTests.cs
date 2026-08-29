namespace Plugin.Maui.NetworkDiagnostics.Tests;

public sealed class ReportTests
{
    [Fact]
    public async Task ToString_prints_the_support_table()
    {
        var http = new FakeHttpRequester
        {
            OnSend = uri => uri.AbsolutePath.Contains("health", StringComparison.Ordinal)
                ? new HttpProbeResult(false, null, TimeSpan.FromMilliseconds(423), "unreachable")
                : new HttpProbeResult(true, 200, TimeSpan.FromMilliseconds(40), null)
        };
        var diagnostics = Harness.Create(http: http);

        var text = (await diagnostics.RunAsync()).ToString();

        Assert.Contains("Internet", text);
        Assert.Contains("DNS", text);
        Assert.Contains("Gateway", text);
        Assert.Contains("HTTPS", text);
        Assert.Contains("TLS", text);
        Assert.Contains("API", text);
        Assert.Contains("Latency", text);
        Assert.Contains("✓", text);
        Assert.Contains("✗", text);
        Assert.Contains("Internet is available, but API endpoint is unreachable.", text);
    }

    [Fact]
    public void Packet_timing_computes_min_median_p95()
    {
        var samples = new[]
        {
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(20),
            TimeSpan.FromMilliseconds(30),
            TimeSpan.FromMilliseconds(40),
            TimeSpan.FromMilliseconds(100)
        };

        var timing = PacketTimingCalculator.From(samples);

        Assert.NotNull(timing);
        Assert.Equal(5, timing.SampleCount);
        Assert.Equal(TimeSpan.FromMilliseconds(10), timing.Min);
        Assert.Equal(TimeSpan.FromMilliseconds(30), timing.Median);
        Assert.Equal(TimeSpan.FromMilliseconds(100), timing.Max);
        Assert.True(timing.P95 >= timing.Median);
    }

    [Fact]
    public void ResolveHost_prefers_api_host_when_default_host_is_left()
    {
        var options = new NetworkDiagnosticsOptions
        {
            ApiEndpoint = new Uri("https://api.myapp.com/health")
        };

        Assert.Equal("api.myapp.com", options.ResolveHost());
    }

    [Fact]
    public async Task Configure_updates_api_endpoint()
    {
        var diagnostics = Harness.Create();
        diagnostics.Configure(options => options.ApiEndpoint = new Uri("https://other.example/health"));

        Assert.Equal("other.example", diagnostics.Options.ApiEndpoint?.Host);
        await diagnostics.RunAsync();
    }

    [Fact]
    public async Task Static_run_uses_shared_instance()
    {
        var instance = Harness.Create();
        NetworkDiagnostics.SetDefault(instance);

        var report = await NetworkDiagnostics.RunAsync();

        Assert.True(report.Internet.Passed);
        Assert.True(report.Dns.Passed);
    }
}
