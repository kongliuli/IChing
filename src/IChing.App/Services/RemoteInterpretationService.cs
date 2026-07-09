using System.Text.Json;
using IChing.Lab.Core.Integrations;
using IChing.Lab.Core.Readings;

namespace IChing.App.Services;

public sealed record RemoteInterpretationResult(string Text, bool IsFallback, string? Error);

public sealed class RemoteInterpretationService
{
    public Task<RemoteInterpretationResult> InterpretAsync(
        AppSettings settings,
        string domain,
        ReadingPromptPacket packet,
        CancellationToken cancellationToken = default) =>
        InterpretAsync(
            settings,
            domain,
            ReadingPromptProtocol.BuildUserMessage(packet),
            ReadingPromptProtocol.BuildSystemPrompt(packet),
            packet.Mode,
            cancellationToken);

    public Task<RemoteInterpretationResult> InterpretAsync(
        AppSettings settings,
        string domain,
        string prompt,
        CancellationToken cancellationToken = default) =>
        InterpretAsync(settings, domain, prompt, null, null, cancellationToken);

    private async Task<RemoteInterpretationResult> InterpretAsync(
        AppSettings settings,
        string domain,
        string prompt,
        string? systemPrompt,
        string? mode,
        CancellationToken cancellationToken = default)
    {
        if (!settings.IsConfigured)
        {
            return new RemoteInterpretationResult(string.Empty, true, "请先在设置中填写 API Key");
        }

        var structured = prompt.Contains("reading-request.v1", StringComparison.Ordinal);
        var messages = structured
            ? [new ChatTurn("system", systemPrompt ?? ReadingPromptProtocol.SystemPrompt), new ChatTurn("user", prompt)]
            : new[]
            {
                new ChatTurn("system", $"你是谨慎的{domain}解读助手。盘面和规则摘要由系统计算，不能改动已计算事实。"),
                new ChatTurn("user", prompt)
            };

        try
        {
            var maxTokens = structured ? settings.MaxTokens : settings.MaxTokens;
            using var response = await OpenAiCompatibleChatClient.SendAsync(
                settings,
                new OpenAiChatRequest(messages, maxTokens, settings.Temperature, JsonOutput: structured),
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new RemoteInterpretationResult(string.Empty, true, $"{(int)response.StatusCode}: {OpenAiCompatibleChatClient.Trim(body, 180)}");
            }

            using var doc = JsonDocument.Parse(body);
            OpenAiCompatibleChatClient.AppendUsageLog(
                doc.RootElement,
                settings,
                new OpenAiChatUsageContext(maxTokens, structured ? domain : null, structured ? mode ?? "initial" : null),
                Path.Combine(FileSystem.AppDataDirectory, "deepseek-usage.log"));
            var text = OpenAiCompatibleChatClient.ExtractMessageContent(doc.RootElement) ?? string.Empty;
            return new RemoteInterpretationResult(
                structured ? ReadingPromptProtocol.NormalizeOutput(text) : text,
                false,
                null);
        }
        catch (Exception ex)
        {
            return new RemoteInterpretationResult(string.Empty, true, ex.Message);
        }
    }

    public IAsyncEnumerable<string> StreamAsync(
        AppSettings settings,
        IReadOnlyList<ChatTurn> messages,
        CancellationToken cancellationToken = default) =>
        OpenAiCompatibleChatClient.StreamContentAsync(
            settings,
            messages,
            Math.Min(settings.MaxTokens, 350),
            settings.Temperature,
            cancellationToken);

    public async Task<ConnectionTestResult> TestAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var result = await InterpretAsync(settings, "连通性", "请只回复 pong。", cancellationToken);
        return result.IsFallback ? new ConnectionTestResult(false, result.Error) : new ConnectionTestResult(true, null);
    }
}
