namespace Plugin.Maui.NetworkDiagnostics;

/// <summary>
/// MAUI host registration for NetworkDiagnostics.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="INetworkDiagnostics"/> for on-demand production troubleshooting.
    /// This does not start a connectivity watcher — use Plugin.Maui.NetworkMonitor for that.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseNetworkDiagnostics(options =>
    /// {
    ///     options.ApiEndpoint = new Uri("https://api.myapp.com/health");
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseNetworkDiagnostics(
        this MauiAppBuilder builder,
        Action<NetworkDiagnosticsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddNetworkDiagnostics(configure);
        builder.Services.AddTransient<IMauiInitializeService, NetworkDiagnosticsInitializer>();
        return builder;
    }
}
