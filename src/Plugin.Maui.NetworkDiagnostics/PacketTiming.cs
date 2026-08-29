namespace Plugin.Maui.NetworkDiagnostics;

/// <summary>
/// Timing statistics from repeated TCP connect samples.
/// ICMP is not used — mobile platforms often block it.
/// </summary>
public sealed class PacketTiming
{
    /// <summary>
    /// Initializes packet-timing statistics.
    /// </summary>
    public PacketTiming(
        IReadOnlyList<TimeSpan> samples,
        TimeSpan min,
        TimeSpan median,
        TimeSpan p95,
        TimeSpan max)
    {
        Samples = samples;
        Min = min;
        Median = median;
        P95 = p95;
        Max = max;
    }

    /// <summary>Gets the successful sample count.</summary>
    public int SampleCount => Samples.Count;

    /// <summary>Gets the individual connect durations.</summary>
    public IReadOnlyList<TimeSpan> Samples { get; }

    /// <summary>Gets the fastest sample.</summary>
    public TimeSpan Min { get; }

    /// <summary>Gets the median (p50) sample.</summary>
    public TimeSpan Median { get; }

    /// <summary>Gets the 95th-percentile sample.</summary>
    public TimeSpan P95 { get; }

    /// <summary>Gets the slowest sample.</summary>
    public TimeSpan Max { get; }
}
