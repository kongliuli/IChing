using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Core.Engines;
using IChing.Lab.Core.Services;
using IChing.Lab.Engines.Bazi;
using IChing.Lab.Engines.Calendar;
using IChing.Lab.Engines.Liuyao;
using IChing.Lab.Engines.Tarot;
using IChing.Lab.Inference;
using IChing.Lab.Inference.Engines;
using IChing.Lab.Inference.Prompts;
using IChing.Lab.PluginLoader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IChing.Lab.Composition;

public static class LabServiceCollectionExtensions
{
    public static IServiceCollection AddLabChartEngines(this IServiceCollection services)
    {
        services.AddSingleton<IChartEngine, BaziChartEngine>();
        services.AddSingleton<IChartEngine, LiuyaoChartEngine>();
        services.AddSingleton<IChartEngine, TarotChartEngine>();
        services.AddSingleton<IChartEngine, CalendarEngine>();

        new BaziEnginesModule().Register(services);
        new LiuyaoEnginesModule().Register(services);
        new TarotEnginesModule().Register(services);
        new CalendarEnginesModule().Register(services);

        services.AddSingleton(sp => new ChartEngineRouter(sp.GetServices<IChartEngine>()));
        return services;
    }

    public static IServiceCollection AddLabInference(
        this IServiceCollection services,
        string modelPath,
        string promptsRoot)
    {
        services.AddSingleton<IInferenceEngine>(sp =>
            new OnnxGenAiEngine(modelPath, sp.GetRequiredService<ILogger<OnnxGenAiEngine>>()));
        services.AddSingleton<IInferenceEngine, TemplateFallbackEngine>();

        services.AddSingleton(sp => new PromptTemplateRegistry(
            promptsRoot,
            sp.GetRequiredService<ILogger<PromptTemplateRegistry>>()));
        services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
            sp.GetRequiredService<PromptTemplateRegistry>(), "bazi", 1, "bazi-tier1-default"));
        services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
            sp.GetRequiredService<PromptTemplateRegistry>(), "liuyao", 1, "liuyao-tier1-default"));
        services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
            sp.GetRequiredService<PromptTemplateRegistry>(), "tarot", 1, "tarot-tier1-en"));
        services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
            sp.GetRequiredService<PromptTemplateRegistry>(), "tarot", 1, "tarot-translate-to-zh"));
        services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
            sp.GetRequiredService<PromptTemplateRegistry>(), "tarot", 1, "tarot-tier1-deckaura-default"));
        services.AddSingleton<IPromptBuilder>(sp => new TemplatePromptBuilder(
            sp.GetRequiredService<PromptTemplateRegistry>(), "tarot", 2, "tarot-tier2-celtic-cross"));

        services.AddSingleton(sp => new ChartInterpretationOrchestrator(
            sp.GetServices<IInferenceEngine>(),
            sp.GetServices<IPromptBuilder>(),
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<ILogger<ChartInterpretationOrchestrator>>(),
            sp.GetServices<IChartEngine>()));

        return services;
    }

    public static IChing.Lab.PluginLoader.PluginLoader AddLabExternalPlugins(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath,
        ILoggerFactory loggerFactory,
        bool skipDiscovery = false)
    {
        var pluginLoader = new IChing.Lab.PluginLoader.PluginLoader(
            configuration,
            loggerFactory.CreateLogger<IChing.Lab.PluginLoader.PluginLoader>(),
            contentRootPath);

        if (!skipDiscovery)
        {
            pluginLoader.DiscoverAndRegister(services);
        }

        services.AddSingleton(pluginLoader);
        return pluginLoader;
    }
}
