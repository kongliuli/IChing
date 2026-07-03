using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IChing.Desktop;

public sealed class OpenAiChatClient
{
    private static readonly HttpClient Http = new();

    public async Task<InterpretationResult> InterpretAsync(
        DesktopSettings settings,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return new InterpretationResult("", true, "Please fill API key in Settings first.");
        }

        var endpoint = $"{settings.BaseUrl.TrimEnd('/')}/chat/completions";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = settings.Model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7
        }), Encoding.UTF8, "application/json");

        try
        {
            using var response = await Http.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new InterpretationResult("", true, $"{(int)response.StatusCode}: {json}");
            }

            using var doc = JsonDocument.Parse(json);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return new InterpretationResult(text ?? "", false, null);
        }
        catch (Exception ex)
        {
            return new InterpretationResult("", true, ex.Message);
        }
    }
}

public sealed record InterpretationResult(string Text, bool IsFallback, string? Error);
