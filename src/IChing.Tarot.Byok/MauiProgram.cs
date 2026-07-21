using IChing.Client.Shared.Editions;
using Microsoft.Extensions.Logging;

namespace IChing.Tarot.Byok;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		IChing.Tarot.App.EditionHost.Capabilities = EditionCapabilities.Byok;

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<IChing.Tarot.App.App>()
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
