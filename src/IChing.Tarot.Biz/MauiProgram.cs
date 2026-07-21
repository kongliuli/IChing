using IChing.Client.Shared.Editions;
using Microsoft.Extensions.Logging;

namespace IChing.Tarot.Biz;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		IChing.Tarot.App.EditionHost.Capabilities = EditionCapabilities.Commercial;
		// 正式环境改为自建 Lab 公网/内网地址；调试可覆写环境变量 ICHING_LAB_URL
		IChing.Tarot.App.EditionHost.DefaultLabApiUrl =
			Environment.GetEnvironmentVariable("ICHING_LAB_URL")
			?? "https://lab.iching.example.com";
		// 有自建牌面 CDN 时注入；空则沿用 jsdelivr 默认
		var cardCdn = Environment.GetEnvironmentVariable("ICHING_CARD_CDN");
		if (!string.IsNullOrWhiteSpace(cardCdn))
		{
			IChing.Tarot.App.EditionHost.DefaultCardCdnBase = cardCdn.Trim().TrimEnd('/');
		}

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<IChing.Tarot.App.App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		// 开发时默认打本地 Lab，避免商业头误连占位域名
		IChing.Tarot.App.EditionHost.DefaultLabApiUrl =
			Environment.GetEnvironmentVariable("ICHING_LAB_URL")
			?? "http://localhost:5000";
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
