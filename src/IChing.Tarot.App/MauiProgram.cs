using IChing.Client.Shared.Editions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace IChing.Tarot.App;

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

#if ANDROID
		builder.ConfigureLifecycleEvents(events =>
		{
			events.AddAndroid(android => android.OnCreate((_, _) =>
			{
				Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
					System.Diagnostics.Debug.WriteLine($"[Android] {args.Exception}");
			}));
		});
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
