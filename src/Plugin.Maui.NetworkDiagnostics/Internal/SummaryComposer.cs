namespace Plugin.Maui.NetworkDiagnostics;

static class SummaryComposer
{
    public static string Compose(IReadOnlyList<DiagnosticCheck> checks, TimeSpan? latency, string host)
    {
        var firstFailure = checks.FirstOrDefault(static check => check.Failed);
        if (firstFailure is null)
        {
            if (latency is { } measured)
                return $"All connectivity checks passed. Latency {(int)Math.Round(measured.TotalMilliseconds)}ms.";

            return "All connectivity checks passed.";
        }

        return firstFailure.Layer switch
        {
            DiagnosticLayer.Internet =>
                firstFailure.Detail?.Contains("Local", StringComparison.OrdinalIgnoreCase) == true
                    ? "No internet. The device is on a local network only."
                    : "No internet. The device is offline.",
            DiagnosticLayer.Dns =>
                $"Internet is available, but DNS lookup failed for {host}.",
            DiagnosticLayer.Gateway =>
                "Internet is available, but the network gateway is unreachable.",
            DiagnosticLayer.Tcp =>
                $"DNS resolved, but TCP connectivity to {host} failed.",
            DiagnosticLayer.Tls =>
                "TCP works, but the TLS handshake failed (certificate, interception, or protocol).",
            DiagnosticLayer.Https =>
                "TLS works, but the HTTPS request failed.",
            DiagnosticLayer.Api =>
                "Internet is available, but API endpoint is unreachable.",
            DiagnosticLayer.Latency =>
                "Connectivity checks passed, but latency sampling failed.",
            _ => firstFailure.Error ?? "Something went wrong."
        };
    }
}
