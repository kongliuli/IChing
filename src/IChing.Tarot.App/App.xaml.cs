using IChing.Tarot.App.Services;

namespace IChing.Tarot.App;

public partial class App : Application
{
    public static AppSettings Settings { get; } = new();
    public static TarotDrawService Tarot { get; } = new();
    public static InterpretationService Interpretation { get; } = new();
    public static ReadingHistoryStore History { get; } = new();

    public App()
    {
        InitializeComponent();
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            System.Diagnostics.Debug.WriteLine($"[App] {e.ExceptionObject}");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
