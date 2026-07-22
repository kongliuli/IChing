using IChing.Client.Shared.Editions;
using Microsoft.Extensions.Logging;

namespace IChing.App.Biz;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        IChing.App.EditionHost.Capabilities = EditionCapabilities.Commercial;
        IChing.App.EditionHost.DefaultLabApiUrl =
            Environment.GetEnvironmentVariable("ICHING_LAB_URL")
            ?? "https://lab.iching.example.com";

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<IChing.App.App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        IChing.App.EditionHost.DefaultLabApiUrl =
            Environment.GetEnvironmentVariable("ICHING_LAB_URL")
            ?? "http://localhost:5000";
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
