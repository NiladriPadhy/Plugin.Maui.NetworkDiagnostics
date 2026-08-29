using Plugin.Maui.NetworkDiagnostics;

namespace Plugin.Maui.NetworkDiagnostics.Sample;

public partial class MainPage : ContentPage
{
    readonly INetworkDiagnostics diagnostics;

    public MainPage(INetworkDiagnostics diagnostics)
    {
        InitializeComponent();
        this.diagnostics = diagnostics;
        SummaryLabel.Text = "Tap Run diagnostics to see where connectivity breaks.";
    }

    async void OnRunClicked(object? sender, EventArgs e)
    {
        RunButton.IsEnabled = false;
        SummaryLabel.Text = "Running…";
        ReportLabel.Text = string.Empty;
        DetailLabel.Text = string.Empty;

        try
        {
            if (Uri.TryCreate(ApiEntry.Text?.Trim(), UriKind.Absolute, out var api))
            {
                diagnostics.Configure(options =>
                {
                    options.ApiEndpoint = api;
                    options.Host = api.Host;
                    options.HttpsUri = new Uri($"{api.Scheme}://{api.Host}/");
                });
            }

            var report = await diagnostics.RunAsync();
            SummaryLabel.Text = report.Summary;
            ReportLabel.Text = report.ToString();
            DetailLabel.Text = string.Join(
                Environment.NewLine,
                report.Checks.Select(check =>
                    $"{check.Name}: {check.Status} {(check.Error ?? check.Detail ?? string.Empty)}".Trim()));
        }
        catch (Exception ex)
        {
            SummaryLabel.Text = ex.Message;
        }
        finally
        {
            RunButton.IsEnabled = true;
        }
    }
}
