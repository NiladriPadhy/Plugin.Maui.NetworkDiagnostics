using Microsoft.Extensions.Logging;
using Plugin.Maui.NetworkDiagnostics;

namespace Plugin.Maui.NetworkDiagnostics.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseNetworkDiagnostics(options =>
            {
                options.ApiEndpoint = new Uri("https://one.one.one.one/");
                options.HttpsUri = new Uri("https://one.one.one.one/");
                options.Host = "one.one.one.one";
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
