using Microsoft.Maui.Hosting;

namespace Plugin.Maui.NetworkDiagnostics;

sealed class NetworkDiagnosticsInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var diagnostics = services.GetService<INetworkDiagnostics>();
        if (diagnostics is null)
            return;

        NetworkDiagnostics.SetCurrent(diagnostics);
    }
}
