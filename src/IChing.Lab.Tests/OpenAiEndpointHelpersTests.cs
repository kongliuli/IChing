using IChing.Client.Shared.Settings;
using IChing.Lab.Core.Integrations;

namespace IChing.Lab.Tests;

public class OpenAiEndpointHelpersTests
{
    [Theory]
    [InlineData("http://localhost:11434/v1", true)]
    [InlineData("http://127.0.0.1:11434/v1", true)]
    [InlineData("https://api.deepseek.com/v1", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLocalBaseUrl_DetectsLoopback(string? url, bool expected) =>
        Assert.Equal(expected, OpenAiEndpointHelpers.IsLocalBaseUrl(url));

    [Fact]
    public void IsConfigured_AllowsEmptyKeyOnLocalhost()
    {
        Assert.True(OpenAiEndpointHelpers.IsConfigured(null, "http://localhost:11434/v1"));
        Assert.True(OpenAiEndpointHelpers.IsConfigured("", "http://127.0.0.1:11434/v1"));
        Assert.False(OpenAiEndpointHelpers.IsConfigured("", "https://api.deepseek.com/v1"));
        Assert.True(OpenAiEndpointHelpers.IsConfigured("sk-x", "https://api.deepseek.com/v1"));
    }

    [Fact]
    public void MutableSettings_IsConfigured_UsesLocalHelper()
    {
        var settings = new MutableClientRuntimeSettings
        {
            BaseUrl = "http://localhost:11434/v1",
            ApiKey = string.Empty,
            Model = "qwen3.5:9b"
        };
        Assert.True(settings.IsConfigured);
    }

    [Fact]
    public void ProviderPresets_Ollama_PointsToQwen35()
    {
        Assert.Equal("qwen3.5:9b", ProviderPresets.Ollama.Model);
        Assert.Contains("11434", ProviderPresets.Ollama.BaseUrl);
    }
}
