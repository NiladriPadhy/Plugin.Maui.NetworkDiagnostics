using System.Net;
using System.Net.Sockets;

namespace Plugin.Maui.NetworkDiagnostics;

sealed class SocketTcpConnector : ITcpConnector
{
    public async Task<TcpConnectResult> ConnectAsync(
        IPAddress address,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(timeout);

        var started = DateTime.UtcNow;
        try
        {
            await socket.ConnectAsync(address, port, linked.Token).ConfigureAwait(false);
            return new TcpConnectResult(true, true, DateTime.UtcNow - started, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new TcpConnectResult(false, false, DateTime.UtcNow - started, "timed out");
        }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.ConnectionRefused)
        {
            return new TcpConnectResult(false, true, DateTime.UtcNow - started, "connection refused");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new TcpConnectResult(false, false, DateTime.UtcNow - started, ex.Message);
        }
    }
}
