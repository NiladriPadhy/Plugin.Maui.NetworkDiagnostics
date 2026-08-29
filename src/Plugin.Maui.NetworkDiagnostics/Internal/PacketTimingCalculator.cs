namespace Plugin.Maui.NetworkDiagnostics;

static class PacketTimingCalculator
{
    public static PacketTiming? From(IReadOnlyList<TimeSpan> samples)
    {
        if (samples.Count == 0)
            return null;

        var ordered = samples.OrderBy(static sample => sample).ToArray();
        return new PacketTiming(
            samples,
            ordered[0],
            Percentile(ordered, 0.50),
            Percentile(ordered, 0.95),
            ordered[^1]);
    }

    static TimeSpan Percentile(IReadOnlyList<TimeSpan> ordered, double percentile)
    {
        if (ordered.Count == 1)
            return ordered[0];

        var rank = percentile * (ordered.Count - 1);
        var low = (int)Math.Floor(rank);
        var high = (int)Math.Ceiling(rank);
        if (low == high)
            return ordered[low];

        var weight = rank - low;
        return TimeSpan.FromTicks(
            (long)(ordered[low].Ticks + ((ordered[high].Ticks - ordered[low].Ticks) * weight)));
    }
}
