namespace Plugin.Maui.NetworkDiagnostics;

/// <summary>
/// Layers of the on-demand connectivity diagnostic run.
/// </summary>
public enum DiagnosticLayer
{
    /// <summary>OS-reported internet path (not a live monitor).</summary>
    Internet,

    /// <summary>DNS lookup for the diagnostic host.</summary>
    Dns,

    /// <summary>Default gateway / first-hop reachability.</summary>
    Gateway,

    /// <summary>TCP connect to the diagnostic host and port.</summary>
    Tcp,

    /// <summary>TLS handshake with the diagnostic host.</summary>
    Tls,

    /// <summary>HTTPS request to the configured URI.</summary>
    Https,

    /// <summary>HTTP request to the app's API health endpoint.</summary>
    Api,

    /// <summary>Measured request or connect latency.</summary>
    Latency
}
