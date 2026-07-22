using IChing.Client.Shared.Editions;
using Microsoft.Extensions.Logging;

namespace IChing.App.Free;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        IChing.App.EditionHost.Capabilities = EditionCapabilities.Free;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<IChing.App.App>()
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
