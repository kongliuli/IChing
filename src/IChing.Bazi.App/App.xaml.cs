using IChing.Bazi.App.Services;

namespace IChing.Bazi.App;

public partial class App : Application
{
    public static AppSettings Settings { get; } = new();
    public static BaziChartService Charts { get; } = new();
    public static BaziInterpretationService Interpretation { get; } = new();
    public static BaziHistoryStore History { get; } = new();

    public App()
    {
        InitializeComponent();
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            System.Diagnostics.Debug.WriteLine($"[App] {e.ExceptionObject}");
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell());
}
