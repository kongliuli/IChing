using IChing.Client.Shared.Onnx;
using IChing.Client.Shared.Settings;
using IChing.Lab.Api.Commercial;
using Microsoft.Extensions.Configuration;

namespace IChing.Lab.Tests;

public class EditionScaffoldingTests
{
    [Fact]
    public void Qwen35Catalog_DefaultIs2B()
    {
        Assert.Equal("qwen3.5-2b-genai", Qwen35ModelCatalog.RecommendedId);
        Assert.Contains(Qwen35ModelCatalog.LiteId, Qwen35ModelCatalog.CandidateDirectoryNames);
    }

    [Fact]
    public void ProviderPresets_ContainsDeepSeekAndOllama()
    {
        Assert.Contains(ProviderPresets.All, p => p.Id == "deepseek");
        Assert.Contains(ProviderPresets.All, p => p.Id == "ollama");
        var settings = new MutableClientRuntimeSettings();
        ProviderPresets.Apply(settings, "zhipu");
        Assert.Equal("zhipu", settings.Provider);
        Assert.Contains("bigmodel", settings.BaseUrl);
    }

    [Fact]
    public void CommercialAiBootstrap_InjectsServerKeyIntoEngineConfig()
    {
        var manager = new ConfigurationManager();
        manager.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CommercialAi:Enabled"] = "true",
            ["CommercialAi:BaseUrl"] = "https://api.deepseek.com/v1",
            ["CommercialAi:Model"] = "deepseek-chat",
            ["CommercialAi:ApiKey"] = "sk-server-only",
        });

        CommercialAiBootstrap.Apply(manager);

        Assert.Equal("sk-server-only", manager["DeepSeek:ApiKey"]);
        Assert.Equal("sk-server-only", manager["CommercialAi:ApiKey"]);
        Assert.Equal("true", manager["CommercialAi:Enabled"]);
    }
}
