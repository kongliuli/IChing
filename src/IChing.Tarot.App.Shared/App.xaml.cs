using System.Diagnostics;
using IChing.Tarot.App.Services;

namespace IChing.Tarot.App;

public partial class App : Application
{
    // ponytail: 防连点/脚本重复启动叠两个窗口；天花板是「同 exe 单实例」，升到多实例再改
    private static Mutex? _singleInstanceMutex;

    public static AppSettings Settings { get; } = new();
    public static TarotCardImageCache CardImages { get; } = new();
    public static TarotDrawService Tarot { get; } = new();
    public static InterpretationService Interpretation { get; } = new();
    public static ReadingHistoryStore History { get; } = new();
    public static LocalSessionStore Sessions { get; } = new();

    public App()
    {
        if (!TryAcquireSingleInstance())
        {
            // 第二个进程直接退出，避免叠两个空窗口
            Environment.Exit(0);
        }

        InitializeComponent();
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            System.Diagnostics.Debug.WriteLine($"[App] {e.ExceptionObject}");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Created += (_, _) => _ = InstallLaunchPrompt.TryPromptAsync(window);
        return window;
    }

    private static bool TryAcquireSingleInstance()
    {
        var exeName = Path.GetFileNameWithoutExtension(Environment.ProcessPath)
                      ?? Process.GetCurrentProcess().ProcessName;
        var mutexName = @"Local\IChing.Tarot." + exeName;
        _singleInstanceMutex = new Mutex(true, mutexName, out var createdNew);
        if (!createdNew)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }

        return createdNew;
    }
}
