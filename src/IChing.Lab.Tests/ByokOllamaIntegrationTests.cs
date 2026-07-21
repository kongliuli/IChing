using IChing.Client.Shared.Editions;
using IChing.Client.Shared.Providers;
using IChing.Client.Shared.Settings;
using IChing.Lab.Core.Integrations;

namespace IChing.Lab.Tests;

/// <summary>
/// 需要本机 ollama serve + qwen3.5:9b。默认单元测试集通过 Category!=Integration 跳过。
/// </summary>
public class ByokOllamaIntegrationTests
{
    private static MutableClientRuntimeSettings OllamaSettings(int maxTokens = 256) => new()
    {
        Provider = "ollama",
        BaseUrl = "http://localhost:11434/v1",
        Model = "qwen3.5:9b",
        ApiKey = string.Empty,
        MaxTokens = maxTokens,
        Temperature = 0.2,
        InterpretTier = 1
    };

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ByokProvider_TestAsync_AgainstLocalOllama()
    {
        var settings = OllamaSettings();
        Assert.True(settings.IsConfigured);
        var provider = new ByokRemoteProvider(settings);
        var result = await provider.TestAsync();
        Assert.True(result.Ok, result.Error);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Composite_StreamFollowUp_AgainstLocalOllama()
    {
        var settings = OllamaSettings(maxTokens: 1024);
        var composite = new CompositeInterpretationProvider(EditionCapabilities.Byok, settings);
        var chunks = new List<string>();
        await foreach (var chunk in composite.StreamFollowUpAsync(
                           [
                               new ChatTurn("system", "Reply with one short Chinese sentence. No markdown."),
                               new ChatTurn("user", "今天适合做什么？")
                           ]))
        {
            chunks.Add(chunk);
        }

        var text = string.Concat(chunks).Trim();
        Assert.False(string.IsNullOrWhiteSpace(text), $"empty stream; chunks={chunks.Count}");
        Assert.DoesNotContain("请先在设置中填写 API Key", text);
    }
}
