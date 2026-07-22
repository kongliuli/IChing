using IChing.Client.Shared.Editions;
using Microsoft.Extensions.Logging;

namespace IChing.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        EditionHost.Capabilities = EditionCapabilities.DevShell;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
