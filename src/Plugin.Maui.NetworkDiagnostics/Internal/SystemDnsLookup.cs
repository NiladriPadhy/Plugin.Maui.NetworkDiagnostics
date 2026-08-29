using System.Net;

namespace Plugin.Maui.NetworkDiagnostics;

sealed class SystemDnsLookup : IDnsLookup
{
    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken) =>
        Dns.GetHostAddressesAsync(host, cancellationToken);
}
