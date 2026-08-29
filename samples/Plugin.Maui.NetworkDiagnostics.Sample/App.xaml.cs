namespace Plugin.Maui.NetworkDiagnostics.Sample;

public partial class App : Application
{
    readonly MainPage mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        this.mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new Window(new NavigationPage(mainPage));
}
