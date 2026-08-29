using System.Net;

namespace Plugin.Maui.NetworkDiagnostics;

static class GatewayResolver
{
    public static IGatewayResolver Create()
    {
#if ANDROID
        return new AndroidGatewayResolver();
#elif IOS
        return new IosGatewayResolver();
#else
        return new UnavailableGatewayResolver();
#endif
    }
}

sealed class UnavailableGatewayResolver : IGatewayResolver
{
    public Task<IReadOnlyList<IPAddress>> ResolveAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<IPAddress>>([]);
}
