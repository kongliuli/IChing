using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IChing.Lab.Api.Services;

public sealed class AccountsCreditsGateway
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountsCreditsGateway> _logger;

    public AccountsCreditsGateway(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AccountsCreditsGateway> logger)
    {
        _http = httpClientFactory.CreateClient(nameof(AccountsCreditsGateway));
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsEnabled =>
        _configuration.GetValue("Accounts:Enabled", false)
        && !string.IsNullOrWhiteSpace(_configuration["Accounts:BaseUrl"]);

    public int MinTier => _configuration.GetValue("Accounts:RequireForTierGte", 1);

    public async Task<AccountsConsumeResult> TryConsumeAsync(string? authorizationHeader, int tier, string? readingId, CancellationToken cancellationToken)
    {
        if (!IsEnabled || tier < MinTier)
        {
            return AccountsConsumeResult.Skipped;
        }

        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AccountsConsumeResult.Unauthorized("Tier 1+ 解读需要登录并携带 Bearer Token");
        }

        var baseUrl = _configuration["Accounts:BaseUrl"]!.TrimEnd('/');
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/credits/consume")
        {
            Content = JsonContent.Create(new { amount = 1, readingId })
        };
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return AccountsConsumeResult.Unauthorized("Accounts Token 无效或已过期");
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Accounts consume failed: {Status} {Body}", (int)response.StatusCode, body);
                return AccountsConsumeResult.Insufficient("解读额度不足");
            }

            return AccountsConsumeResult.Consumed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Accounts consume request failed");
            return AccountsConsumeResult.Unavailable("Accounts 服务暂不可用");
        }
    }
}

public readonly record struct AccountsConsumeResult(bool Allowed, string? Error, bool SkippedBecauseDisabled)
{
    public static AccountsConsumeResult Skipped => new(true, null, true);
    public static AccountsConsumeResult Consumed => new(true, null, false);
    public static AccountsConsumeResult Unauthorized(string error) => new(false, error, false);
    public static AccountsConsumeResult Insufficient(string error) => new(false, error, false);
    public static AccountsConsumeResult Unavailable(string error) => new(false, error, false);
}
