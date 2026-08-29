using System.Net;
using Android.Content;
using Android.Net;

namespace Plugin.Maui.NetworkDiagnostics;

sealed class AndroidGatewayResolver : IGatewayResolver
{
    public Task<IReadOnlyList<IPAddress>> ResolveAsync(CancellationToken cancellationToken)
    {
        var list = new List<IPAddress>();
        try
        {
            var context = Android.App.Application.Context;
            if (context.GetSystemService(Context.ConnectivityService) is not ConnectivityManager manager)
                return Task.FromResult<IReadOnlyList<IPAddress>>(list);

            var network = manager.ActiveNetwork;
            if (network is null)
                return Task.FromResult<IReadOnlyList<IPAddress>>(list);

            var properties = manager.GetLinkProperties(network);
            if (properties?.Routes is null)
                return Task.FromResult<IReadOnlyList<IPAddress>>(list);

            foreach (var route in properties.Routes)
            {
                if (!route.IsDefaultRoute)
                    continue;

                var host = route.Gateway?.HostAddress;
                if (host is not null
                    && IPAddress.TryParse(host, out var address)
                    && !address.Equals(IPAddress.Any)
                    && !address.Equals(IPAddress.IPv6Any))
                {
                    list.Add(address);
                }
            }
        }
        catch
        {
            // Keep an empty list so the runner can report a skipped / failed gateway layer.
        }

        return Task.FromResult<IReadOnlyList<IPAddress>>(list);
    }
}
