namespace Plugin.Maui.NetworkDiagnostics;

/// <summary>
/// Result of a single diagnostic layer.
/// </summary>
public sealed class DiagnosticCheck
{
    /// <summary>
    /// Initializes a new diagnostic check.
    /// </summary>
    public DiagnosticCheck(
        DiagnosticLayer layer,
        string name,
        CheckStatus status,
        TimeSpan duration,
        string? detail = null,
        string? error = null)
    {
        Layer = layer;
        Name = name;
        Status = status;
        Duration = duration;
        Detail = detail;
        Error = error;
    }

    /// <summary>Gets the layer this check belongs to.</summary>
    public DiagnosticLayer Layer { get; }

    /// <summary>Gets the display name (Internet, DNS, Gateway, …).</summary>
    public string Name { get; }

    /// <summary>Gets whether the layer passed, failed, or was skipped.</summary>
    public CheckStatus Status { get; }

    /// <summary>Gets how long the check took.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Gets extra context (resolved addresses, status codes, gateway IP).</summary>
    public string? Detail { get; }

    /// <summary>Gets the failure reason when <see cref="Status"/> is <see cref="CheckStatus.Failed"/>.</summary>
    public string? Error { get; }

    /// <summary>Gets a value indicating whether the layer passed.</summary>
    public bool Passed => Status == CheckStatus.Passed;

    /// <summary>Gets a value indicating whether the layer failed.</summary>
    public bool Failed => Status == CheckStatus.Failed;

    /// <summary>Gets a value indicating whether the layer was skipped.</summary>
    public bool Skipped => Status == CheckStatus.Skipped;

    internal static DiagnosticCheck Pass(DiagnosticLayer layer, string name, TimeSpan duration, string? detail = null) =>
        new(layer, name, CheckStatus.Passed, duration, detail);

    internal static DiagnosticCheck Fail(DiagnosticLayer layer, string name, TimeSpan duration, string error, string? detail = null) =>
        new(layer, name, CheckStatus.Failed, duration, detail, error);

    internal static DiagnosticCheck Skip(DiagnosticLayer layer, string name, string reason) =>
        new(layer, name, CheckStatus.Skipped, TimeSpan.Zero, reason);
}
