using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Plugin.Maui.NetworkDiagnostics;

sealed class IosGatewayResolver : IGatewayResolver
{
    const int CtlNet = 4;
    const int PfRoute = 17;
    const int AfInet = 2;
    const int NetRtFlags = 2;
    const int RtfGateway = 0x2;
    const int RtaGateway = 0x2;

    public Task<IReadOnlyList<IPAddress>> ResolveAsync(CancellationToken cancellationToken)
    {
        var list = new List<IPAddress>();

        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                foreach (var gateway in adapter.GetIPProperties().GatewayAddresses)
                {
                    if (gateway.Address is { } address
                        && !address.Equals(IPAddress.Any)
                        && !address.Equals(IPAddress.IPv6Any)
                        && !list.Contains(address))
                    {
                        list.Add(address);
                    }
                }
            }
        }
        catch
        {
            // Fall through to the routing table.
        }

        if (list.Count == 0 && TryReadIpv4DefaultGateway() is { } routed && !list.Contains(routed))
            list.Add(routed);

        return Task.FromResult<IReadOnlyList<IPAddress>>(list);
    }

    static IPAddress? TryReadIpv4DefaultGateway()
    {
        try
        {
            int[] mib = [CtlNet, PfRoute, 0, AfInet, NetRtFlags, RtfGateway];
            nuint length = 0;
            if (sysctl(mib, (uint)mib.Length, IntPtr.Zero, ref length, IntPtr.Zero, 0) != 0 || length == 0)
                return null;

            var buffer = Marshal.AllocHGlobal((int)length);
            try
            {
                if (sysctl(mib, (uint)mib.Length, buffer, ref length, IntPtr.Zero, 0) != 0)
                    return null;

                var offset = 0;
                while (offset + 4 < (int)length)
                {
                    var messageLength = Marshal.ReadInt16(buffer, offset);
                    if (messageLength <= 0)
                        break;

                    var flags = Marshal.ReadInt32(buffer, offset + 8);
                    var addrs = Marshal.ReadInt32(buffer, offset + 12);
                    if ((flags & RtfGateway) != 0 && (addrs & RtaGateway) != 0)
                    {
                        // rt_msghdr is 92 bytes on 64-bit Darwin; sockaddrs follow.
                        var sockaddr = buffer + offset + 92;
                        var family = Marshal.ReadByte(sockaddr, 1);
                        if (family == AfInet)
                        {
                            var b0 = Marshal.ReadByte(sockaddr, 4);
                            var b1 = Marshal.ReadByte(sockaddr, 5);
                            var b2 = Marshal.ReadByte(sockaddr, 6);
                            var b3 = Marshal.ReadByte(sockaddr, 7);
                            var address = new IPAddress(new[] { b0, b1, b2, b3 });
                            if (!address.Equals(IPAddress.Any))
                                return address;
                        }
                    }

                    offset += messageLength;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    [DllImport("libc", SetLastError = true)]
    static extern int sysctl(int[] name, uint namelen, IntPtr oldp, ref nuint oldlenp, IntPtr newp, nuint newlen);
}
