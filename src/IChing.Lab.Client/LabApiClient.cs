using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Client;

public sealed class LabApiClient
{
    private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromMinutes(3) };

    public static async Task<(bool Ok, int StatusCode, string? Error)> ConsumeCreditsAsync(
        string baseUrl,
        string exchangeId,
        string domain,
        string mode,
        string? bearerToken = null,
        string? sessionId = null,
        int tier = 1,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/lab/credits/consume";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new { exchangeId, sessionId, domain, mode, tier })
        };
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        using var response = await SharedHttp.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return (true, (int)response.StatusCode, null);
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return (false, (int)response.StatusCode, errorBody);
    }

    public static async Task<JsonDocument?> PostReadAsync(
        string baseUrl,
        string domain,
        int tier,
        object body,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/lab/{domain}/read?tier={tier}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        using var response = await SharedHttp.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    public static async Task<bool> HealthAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await SharedHttp.GetAsync($"{baseUrl.TrimEnd('/')}/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<string?> RegisterChatSessionAsync(
        string baseUrl,
        string domain,
        int tier,
        ExchangeInput input,
        string? initialOutput,
        object? chart,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/lab/chat";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                mode = "register",
                domain,
                tier,
                input,
                initialOutput,
                chart
            })
        };
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        using var response = await SharedHttp.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return doc.RootElement.TryGetProperty("sessionId", out var id) ? id.GetString() : null;
    }

    public static async Task<bool> AppendChatHistoryAsync(
        string baseUrl,
        string sessionId,
        string userQuestion,
        string assistantReply,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/lab/chat";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                mode = "append",
                sessionId,
                userQuestion,
                assistantReply
            })
        };
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        using var response = await SharedHttp.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>商业版追问：服务端持有 LLM Key，App 只传 sessionId + 用户问题。</summary>
    public static async Task<JsonDocument?> FollowUpAsync(
        string baseUrl,
        string sessionId,
        string userQuestion,
        int? maxTokens = null,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/lab/chat";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                mode = "followup",
                sessionId,
                userQuestion,
                maxTokens
            })
        };
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        using var response = await SharedHttp.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }
}
