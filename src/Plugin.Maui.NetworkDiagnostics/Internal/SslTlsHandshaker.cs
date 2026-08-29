using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

namespace Plugin.Maui.NetworkDiagnostics;

sealed class SslTlsHandshaker : ITlsHandshaker
{
    public async Task<TlsHandshakeResult> HandshakeAsync(
        string host,
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
            await using var network = new NetworkStream(socket, ownsSocket: false);
            await using var ssl = new SslStream(network, leaveInnerStreamOpen: false);
            await ssl.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions
                {
                    TargetHost = host,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                },
                linked.Token).ConfigureAwait(false);

            return new TlsHandshakeResult(true, DateTime.UtcNow - started, ssl.SslProtocol.ToString(), null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new TlsHandshakeResult(false, DateTime.UtcNow - started, null, "timed out");
        }
        catch (AuthenticationException ex)
        {
            return new TlsHandshakeResult(false, DateTime.UtcNow - started, null, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new TlsHandshakeResult(false, DateTime.UtcNow - started, null, ex.Message);
        }
    }
}
