using IChing.App.Services;

namespace IChing.App;

public partial class App : Application
{
    public static AppSettings Settings { get; } = new();
    public static InterpretationService Interpretation { get; } = new();
    public static RemoteInterpretationService Remote { get; } = new();
    public static ReadingHistoryStore History { get; } = new();

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell());
}
