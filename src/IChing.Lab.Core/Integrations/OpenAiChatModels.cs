namespace IChing.Lab.Core.Integrations;

public interface IOpenAiChatCredentials
{
    string BaseUrl { get; }
    string ApiKey { get; }
    string Model { get; }
    bool IsConfigured { get; }
}

public sealed record ChatTurn(string Role, string Content);

public sealed record OpenAiChatRequest(
    IReadOnlyList<ChatTurn> Messages,
    int MaxTokens,
    double Temperature = 0.7,
    bool Stream = false,
    bool JsonOutput = false);

public sealed record OpenAiChatUsageContext(int MaxTokens, string? Domain = null, string? Mode = null);

public sealed record ConnectionTestResult(bool Ok, string? Error);
