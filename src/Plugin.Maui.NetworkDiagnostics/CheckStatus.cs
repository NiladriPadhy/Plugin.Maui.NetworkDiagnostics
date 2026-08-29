namespace Plugin.Maui.NetworkDiagnostics;

/// <summary>
/// Outcome of one diagnostic layer.
/// </summary>
public enum CheckStatus
{
    /// <summary>The layer succeeded.</summary>
    Passed,

    /// <summary>The layer failed.</summary>
    Failed,

    /// <summary>The layer was not run (missing config or an earlier required layer failed).</summary>
    Skipped
}
