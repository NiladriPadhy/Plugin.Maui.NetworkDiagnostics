using System.Net.Http.Headers;

namespace Plugin.Maui.NetworkDiagnostics;

sealed class SocketsHttpRequester : IHttpRequester
{
    public async Task<HttpProbeResult> SendAsync(
        Uri uri,
        string method,
        TimeSpan timeout,
        string userAgent,
        CancellationToken cancellationToken)
    {
        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            ConnectTimeout = timeout,
            PooledConnectionLifetime = TimeSpan.Zero,
            UseCookies = false
        };

        using var client = new HttpClient(handler)
        {
            Timeout = timeout
        };

        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true
        };

        using var request = new HttpRequestMessage(new HttpMethod(method), uri);
        var started = DateTime.UtcNow;

        try
        {
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            _ = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            return new HttpProbeResult(true, (int)response.StatusCode, DateTime.UtcNow - started, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new HttpProbeResult(false, null, DateTime.UtcNow - started, "timed out");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new HttpProbeResult(false, null, DateTime.UtcNow - started, ex.Message);
        }
    }
}
