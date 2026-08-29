using System.Net;

namespace Plugin.Maui.NetworkDiagnostics;

internal interface IInternetAccess
{
    InternetSnapshot GetSnapshot();
}

internal sealed record InternetSnapshot(bool HasLink, bool HasInternet, string Access, string Profiles);

internal interface IDnsLookup
{
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken);
}

internal interface ITcpConnector
{
    Task<TcpConnectResult> ConnectAsync(IPAddress address, int port, TimeSpan timeout, CancellationToken cancellationToken);
}

internal sealed record TcpConnectResult(bool Connected, bool Reached, TimeSpan Duration, string? Error);

internal interface ITlsHandshaker
{
    Task<TlsHandshakeResult> HandshakeAsync(string host, IPAddress address, int port, TimeSpan timeout, CancellationToken cancellationToken);
}

internal sealed record TlsHandshakeResult(bool Succeeded, TimeSpan Duration, string? Protocol, string? Error);

internal interface IHttpRequester
{
    Task<HttpProbeResult> SendAsync(Uri uri, string method, TimeSpan timeout, string userAgent, CancellationToken cancellationToken);
}

internal sealed record HttpProbeResult(bool Succeeded, int? StatusCode, TimeSpan Duration, string? Error);

internal interface IGatewayResolver
{
    Task<IReadOnlyList<IPAddress>> ResolveAsync(CancellationToken cancellationToken);
}
