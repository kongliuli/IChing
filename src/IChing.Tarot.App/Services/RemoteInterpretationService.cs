using System.Text.Json;
using IChing.Lab.Core.Integrations;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

public sealed record InterpretationResult(string Text, bool IsFallback, string? Error);

public sealed class RemoteInterpretationService
{
    public async Task<InterpretationResult> InterpretAsync(
        AppSettings settings,
        TarotReading reading,
        string? question,
        CancellationToken cancellationToken = default)
    {
        if (!settings.IsConfigured)
        {
            var preview = ReadingSummaries.BuildTarotTier0Preview(reading, question);
            return new InterpretationResult(preview.OneLiner, true, "请先在设置中填写 API Key，或启用 Lab API");
        }

        var packet = ReadingPromptPackets.TarotInitial(
            reading,
            ReadingSummaries.BuildTarotRuleDigest(reading),
            question);
        var messages = new[]
        {
            new ChatTurn("system", ReadingPromptProtocol.BuildSystemPrompt(packet)),
            new ChatTurn("user", ReadingPromptProtocol.BuildUserMessage(packet))
        };
        var maxTokens = reading.Positions.Count >= 10 ? 700 : 500;

        try
        {
            using var response = await OpenAiCompatibleChatClient.SendAsync(
                settings,
                new OpenAiChatRequest(messages, maxTokens, Temperature: 0.7, JsonOutput: true),
                cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new InterpretationResult(
                    ReadingSummaries.BuildTarotTier0Preview(reading, question).OneLiner,
                    true,
                    UserFacingZh.Error($"{(int)response.StatusCode}: {OpenAiCompatibleChatClient.Trim(json, 200)}"));
            }

            using var doc = JsonDocument.Parse(json);
            OpenAiCompatibleChatClient.AppendUsageLog(
                doc.RootElement,
                settings,
                new OpenAiChatUsageContext(maxTokens, packet.Domain, packet.Mode),
                Path.Combine(FileSystem.AppDataDirectory, "deepseek-usage.log"));
            var text = OpenAiCompatibleChatClient.ExtractMessageContent(doc.RootElement);
            return new InterpretationResult(
                ReadingPromptProtocol.NormalizeOutput(text ?? ReadingSummaries.BuildTarotTier0Preview(reading, question).OneLiner),
                false,
                null);
        }
        catch (Exception ex)
        {
            return new InterpretationResult(
                ReadingSummaries.BuildTarotTier0Preview(reading, question).OneLiner,
                true,
                UserFacingZh.Error(ex.Message));
        }
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (!settings.IsConfigured)
        {
            return new ConnectionTestResult(false, "API Key 为空");
        }

        try
        {
            using var response = await OpenAiCompatibleChatClient.SendAsync(
                settings,
                new OpenAiChatRequest([new ChatTurn("user", "ping")], MaxTokens: 5),
                cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new ConnectionTestResult(true, null);
            }

            var body = OpenAiCompatibleChatClient.Trim(await response.Content.ReadAsStringAsync(cancellationToken), 120);
            return new ConnectionTestResult(false, UserFacingZh.Error($"{(int)response.StatusCode} {body}"));
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, UserFacingZh.Error(ex.Message));
        }
    }

    public IAsyncEnumerable<string> StreamAsync(
        AppSettings settings,
        IReadOnlyList<ChatTurn> messages,
        CancellationToken cancellationToken = default) =>
        OpenAiCompatibleChatClient.StreamContentAsync(settings, messages, maxTokens: 350, cancellationToken: cancellationToken);
}
