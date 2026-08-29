using System.Text;

namespace Plugin.Maui.NetworkDiagnostics;

/// <summary>
/// Structured result of <see cref="NetworkDiagnostics.RunAsync(CancellationToken)"/>.
/// </summary>
public sealed class NetworkDiagnosticReport
{
    /// <summary>
    /// Initializes a diagnostic report.
    /// </summary>
    public NetworkDiagnosticReport(
        IReadOnlyList<DiagnosticCheck> checks,
        TimeSpan? latency,
        PacketTiming? packetTiming,
        string summary,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt)
    {
        Checks = checks;
        Latency = latency;
        PacketTiming = packetTiming;
        Summary = summary;
        StartedAt = startedAt;
        FinishedAt = finishedAt;
    }

    /// <summary>Gets every layer that was evaluated or skipped.</summary>
    public IReadOnlyList<DiagnosticCheck> Checks { get; }

    /// <summary>Gets a value indicating whether no layer failed.</summary>
    public bool Succeeded => Checks.All(static check => !check.Failed);

    /// <summary>Gets the first failed layer, or <see langword="null"/> when the run succeeded.</summary>
    public DiagnosticLayer? FirstFailure =>
        Checks.FirstOrDefault(static check => check.Failed)?.Layer;

    /// <summary>Gets the Internet layer.</summary>
    public DiagnosticCheck Internet => Get(DiagnosticLayer.Internet);

    /// <summary>Gets the DNS layer.</summary>
    public DiagnosticCheck Dns => Get(DiagnosticLayer.Dns);

    /// <summary>Gets the Gateway layer.</summary>
    public DiagnosticCheck Gateway => Get(DiagnosticLayer.Gateway);

    /// <summary>Gets the TCP layer.</summary>
    public DiagnosticCheck Tcp => Get(DiagnosticLayer.Tcp);

    /// <summary>Gets the TLS layer.</summary>
    public DiagnosticCheck Tls => Get(DiagnosticLayer.Tls);

    /// <summary>Gets the HTTPS layer.</summary>
    public DiagnosticCheck Https => Get(DiagnosticLayer.Https);

    /// <summary>Gets the API layer.</summary>
    public DiagnosticCheck Api => Get(DiagnosticLayer.Api);

    /// <summary>Gets the Latency layer.</summary>
    public DiagnosticCheck LatencyCheck => Get(DiagnosticLayer.Latency);

    /// <summary>Gets the representative latency (API, HTTPS, or TCP), when measured.</summary>
    public TimeSpan? Latency { get; }

    /// <summary>Gets TCP connect sample statistics, when collected.</summary>
    public PacketTiming? PacketTiming { get; }

    /// <summary>
    /// Gets a support-ready sentence, for example
    /// "Internet is available, but API endpoint is unreachable."
    /// </summary>
    public string Summary { get; }

    /// <summary>Gets when the run started.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>Gets when the run finished.</summary>
    public DateTimeOffset FinishedAt { get; }

    /// <summary>Gets the total run duration.</summary>
    public TimeSpan Duration => FinishedAt - StartedAt;

    /// <summary>Returns the check for <paramref name="layer"/>, or a skipped placeholder.</summary>
    public DiagnosticCheck Get(DiagnosticLayer layer) =>
        Checks.FirstOrDefault(check => check.Layer == layer)
        ?? DiagnosticCheck.Skip(layer, layer.ToString(), "Not evaluated.");

    /// <summary>
    /// Formats the report as a support table.
    /// </summary>
    /// <example>
    /// <code>
    /// Internet       ✓
    /// DNS            ✓
    /// Gateway        ✓
    /// HTTPS          ✓
    /// TLS            ✓
    /// API            ✗
    /// Latency        423ms
    /// </code>
    /// </example>
    public override string ToString()
    {
        var builder = new StringBuilder();
        foreach (var check in Checks)
        {
            var mark = check.Status switch
            {
                CheckStatus.Passed when check.Layer == DiagnosticLayer.Latency && Latency is { } latency =>
                    $"{(int)Math.Round(latency.TotalMilliseconds)}ms",
                CheckStatus.Passed => "✓",
                CheckStatus.Failed => "✗",
                _ => "—"
            };

            builder.Append(check.Name.PadRight(14));
            builder.Append(mark);
            builder.AppendLine();
        }

        if (PacketTiming is { SampleCount: > 0 } timing)
        {
            builder.Append("Packet timing  ");
            builder.Append("min ");
            builder.Append((int)Math.Round(timing.Min.TotalMilliseconds));
            builder.Append("ms  p50 ");
            builder.Append((int)Math.Round(timing.Median.TotalMilliseconds));
            builder.Append("ms  p95 ");
            builder.Append((int)Math.Round(timing.P95.TotalMilliseconds));
            builder.Append("ms  n=");
            builder.Append(timing.SampleCount);
            builder.AppendLine();
        }

        builder.AppendLine();
        builder.Append(Summary);
        return builder.ToString().TrimEnd();
    }
}
