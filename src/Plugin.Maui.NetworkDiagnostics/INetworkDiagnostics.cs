namespace Plugin.Maui.NetworkDiagnostics;

/// <summary>
/// On-demand production connectivity diagnostics. Not a network monitor.
/// </summary>
public interface INetworkDiagnostics
{
    /// <summary>
    /// Gets the live options. Mutate through <see cref="Configure"/>.
    /// </summary>
    NetworkDiagnosticsOptions Options { get; }

    /// <summary>
    /// Raised after each layer completes so a support screen can update live.
    /// </summary>
    event EventHandler<DiagnosticCheck>? CheckCompleted;

    /// <summary>
    /// Updates diagnostic options.
    /// </summary>
    void Configure(Action<NetworkDiagnosticsOptions> configure);

    /// <summary>
    /// Runs the full diagnostic stack using the current options.
    /// </summary>
    Task<NetworkDiagnosticReport> RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the full diagnostic stack using a one-shot options override.
    /// </summary>
    Task<NetworkDiagnosticReport> RunAsync(NetworkDiagnosticsOptions options, CancellationToken cancellationToken = default);
}
