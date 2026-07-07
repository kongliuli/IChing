using IChing.Liuyao.App.Services;

namespace IChing.Liuyao.App;

public partial class App : Application
{
    public static AppSettings Settings { get; } = new();
    public static LiuyaoCastService Casts { get; } = new();
    public static LiuyaoInterpretationService Interpretation { get; } = new();
    public static LiuyaoHistoryStore History { get; } = new();

    public App()
    {
        InitializeComponent();
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            System.Diagnostics.Debug.WriteLine($"[App] {e.ExceptionObject}");
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell());
}
