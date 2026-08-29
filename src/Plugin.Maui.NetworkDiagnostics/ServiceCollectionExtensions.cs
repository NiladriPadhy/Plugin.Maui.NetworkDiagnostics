using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Plugin.Maui.NetworkDiagnostics;

/// <summary>
/// Registers NetworkDiagnostics without MAUI host hooks.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="INetworkDiagnostics"/> using the supplied options instance.
    /// </summary>
    public static IServiceCollection AddNetworkDiagnostics(
        this IServiceCollection services,
        NetworkDiagnosticsOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.TryAddSingleton<IInternetAccess, MauiInternetAccess>();
        services.TryAddSingleton<IDnsLookup, SystemDnsLookup>();
        services.TryAddSingleton<ITcpConnector, SocketTcpConnector>();
        services.TryAddSingleton<ITlsHandshaker, SslTlsHandshaker>();
        services.TryAddSingleton<IHttpRequester, SocketsHttpRequester>();
        services.TryAddSingleton(_ => GatewayResolver.Create());
        services.TryAddSingleton<INetworkDiagnostics>(sp =>
        {
            var resolved = sp.GetService<NetworkDiagnosticsOptions>() ?? options;
            var instance = NetworkDiagnostics.Create(
                resolved,
                sp.GetRequiredService<IInternetAccess>(),
                sp.GetRequiredService<IDnsLookup>(),
                sp.GetRequiredService<ITcpConnector>(),
                sp.GetRequiredService<ITlsHandshaker>(),
                sp.GetRequiredService<IHttpRequester>(),
                sp.GetRequiredService<IGatewayResolver>());
            NetworkDiagnostics.SetCurrent(instance);
            return instance;
        });

        return services;
    }

    /// <summary>
    /// Adds <see cref="INetworkDiagnostics"/> and applies <paramref name="configure"/> to a new options instance.
    /// </summary>
    public static IServiceCollection AddNetworkDiagnostics(
        this IServiceCollection services,
        Action<NetworkDiagnosticsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new NetworkDiagnosticsOptions();
        configure?.Invoke(options);
        return services.AddNetworkDiagnostics(options);
    }
}
