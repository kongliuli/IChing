using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IChing.App.Services;

public sealed class RemoteInterpretationService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(2) };

    public async Task<RemoteInterpretationResult> InterpretAsync(
        AppSettings settings,
        string domain,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (!settings.IsConfigured)
        {
            return new RemoteInterpretationResult(string.Empty, true, "请先在设置中填写 API Key");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = settings.Model,
            messages = new[]
            {
                new { role = "system", content = $"你是谨慎的{domain}解读助手。盘面和规则摘要由系统计算，不能改动干支、卦名、爻位、六亲或世应。" },
                new { role = "user", content = prompt }
            },
            temperature = settings.Temperature,
            max_tokens = settings.MaxTokens
        }), Encoding.UTF8, "application/json");

        try
        {
            using var response = await Http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new RemoteInterpretationResult(string.Empty, true, $"{(int)response.StatusCode}: {Trim(body, 180)}");
            }

            using var doc = JsonDocument.Parse(body);
            var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return new RemoteInterpretationResult(text ?? string.Empty, false, null);
        }
        catch (Exception ex)
        {
            return new RemoteInterpretationResult(string.Empty, true, ex.Message);
        }
    }

    public async Task<ConnectionTestResult> TestAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var result = await InterpretAsync(settings, "连通性", "请只回复 pong。", cancellationToken);
        return result.IsFallback ? new ConnectionTestResult(false, result.Error) : new ConnectionTestResult(true, null);
    }

    private static string Trim(string text, int max) =>
        text.Length <= max ? text : text[..max] + "...";
}

public sealed record RemoteInterpretationResult(string Text, bool IsFallback, string? Error);

public sealed record ConnectionTestResult(bool Ok, string? Error);
