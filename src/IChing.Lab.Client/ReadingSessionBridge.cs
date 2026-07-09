using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Client;

/// <summary>
/// App SQLite session 与 Lab <c>/lab/chat</c> 内存 session 注册同步。
/// </summary>
public static class ReadingSessionBridge
{
    public static async Task<string?> RegisterWithLabAsync(
        string labApiUrl,
        bool useLabApi,
        string? authToken,
        string domain,
        int tier,
        ExchangeInput input,
        string? initialOutput,
        object? chart,
        CancellationToken cancellationToken = default)
    {
        if (!useLabApi || string.IsNullOrWhiteSpace(labApiUrl))
        {
            return null;
        }

        return await LabApiClient.RegisterChatSessionAsync(
            labApiUrl,
            domain,
            tier,
            input,
            initialOutput,
            chart,
            authToken,
            cancellationToken);
    }
}
