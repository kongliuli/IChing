using System.Text.Json;
using System.Text.RegularExpressions;
using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

public static partial class UserFacingZh
{
    private static readonly (string En, string Zh)[] HeadingMap =
    [
        ("Overall Energy", "整体能量"),
        ("Overall", "整体能量"),
        ("General Reading", "综合解读"),
        ("Summary", "总结"),
        ("Card Reading", "牌位解读"),
        ("Position Reading", "牌位解读"),
        ("Card Interpretation", "牌位解读"),
        ("Spread Interaction", "牌阵互动"),
        ("Interaction", "牌阵互动"),
        ("Action Advice", "行动建议"),
        ("Actionable Advice", "行动建议"),
        ("Recommendations", "行动建议"),
        ("Advice", "行动建议"),
        ("Conclusion", "结语"),
    ];

    public static string Error(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "发生未知错误";
        }

        var text = raw.Trim();
        if (int.TryParse(text, out var codeOnly) && codeOnly is >= 100 and <= 599)
        {
            return HttpStatus(codeOnly);
        }

        var statusMatch = StatusPrefix().Match(text);
        if (statusMatch.Success && int.TryParse(statusMatch.Groups[1].Value, out var code))
        {
            var tail = statusMatch.Groups[2].Value.Trim();
            var apiMsg = TryParseApiMessage(tail);
            return string.IsNullOrWhiteSpace(apiMsg)
                ? HttpStatus(code)
                : $"{HttpStatus(code)}：{apiMsg}";
        }

        return TranslateKnownPhrase(text);
    }

    public static string HttpStatus(int code) => code switch
    {
        400 => "请求无效（400）",
        401 => "未授权，请检查 API Key（401）",
        403 => "访问被拒绝（403）",
        404 => "接口不存在，请检查 API 地址（404）",
        408 => "请求超时（408）",
        429 => "请求过于频繁，请稍后再试（429）",
        500 => "服务端错误（500）",
        502 => "网关错误（502）",
        503 => "服务暂不可用（503）",
        _ => $"网络错误（{code}）"
    };

    public static string ProviderLabel(string provider) => provider switch
    {
        "deepseek" => "DeepSeek",
        "openai" => "OpenAI",
        "custom" => "自定义 API",
        _ => "远程 API"
    };

    public static string EngineLabel(string engineId) => engineId switch
    {
        "iching-tarot-built-in" => "内置",
        _ => engineId
    };

    public static string CardLine(TarotPositionReading position) =>
        $"{position.CardNameZh} · {(position.Reversed ? "逆位" : "正位")}";

    public static string CardSource(TarotPositionReading position) =>
        $"[{position.PositionTitleZh}]\n{CardLine(position)}\n\n{position.Meaning}";

    public static string SectionTitle(string title, TarotReading? reading = null)
    {
        var t = title.Trim();
        if (string.IsNullOrWhiteSpace(t))
        {
            return "解读";
        }

        foreach (var (en, zh) in HeadingMap)
        {
            if (t.Equals(en, StringComparison.OrdinalIgnoreCase))
            {
                return zh;
            }
        }

        t = t.Replace("Upright", "正位", StringComparison.OrdinalIgnoreCase)
            .Replace("Reversed", "逆位", StringComparison.OrdinalIgnoreCase);

        if (reading is not null)
        {
            foreach (var p in reading.Positions)
            {
                if (!string.IsNullOrWhiteSpace(p.PositionTitle) &&
                    t.Contains(p.PositionTitle, StringComparison.OrdinalIgnoreCase))
                {
                    t = Regex.Replace(t, Regex.Escape(p.PositionTitle), p.PositionTitleZh, RegexOptions.IgnoreCase);
                }

                if (t.Contains(p.CardName, StringComparison.OrdinalIgnoreCase))
                {
                    t = Regex.Replace(t, Regex.Escape(p.CardName), p.CardNameZh, RegexOptions.IgnoreCase);
                }
            }
        }

        foreach (var (en, zh) in HeadingMap)
        {
            if (t.Contains(en, StringComparison.OrdinalIgnoreCase))
            {
                t = Regex.Replace(t, Regex.Escape(en), zh, RegexOptions.IgnoreCase);
            }
        }

        return t;
    }

    private static string? TryParseApiMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                {
                    return TranslateKnownPhrase(error.GetString() ?? string.Empty);
                }

                if (error.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                {
                    return TranslateKnownPhrase(message.GetString() ?? string.Empty);
                }
            }

            if (root.TryGetProperty("message", out var topMessage) && topMessage.ValueKind == JsonValueKind.String)
            {
                return TranslateKnownPhrase(topMessage.GetString() ?? string.Empty);
            }
        }
        catch
        {
            // ponytail: non-json error bodies fall through to phrase mapping.
        }

        return TranslateKnownPhrase(body.Length > 120 ? body[..120] + "..." : body);
    }

    private static string TranslateKnownPhrase(string text)
    {
        var t = text.Trim();
        if (string.IsNullOrWhiteSpace(t))
        {
            return "发生未知错误";
        }

        var lower = t.ToLowerInvariant();
        if (lower.Contains("incorrect api key") || lower.Contains("invalid api key"))
        {
            return "API Key 不正确";
        }

        if (lower.Contains("exceeded") && lower.Contains("quota"))
        {
            return "API 额度已用尽";
        }

        if (lower.Contains("rate limit"))
        {
            return "请求过于频繁，请稍后再试";
        }

        if (lower.Contains("model") && lower.Contains("not found"))
        {
            return "模型不存在或不可用";
        }

        if (lower.Contains("no connection could be made") || lower.Contains("connection refused"))
        {
            return "无法连接到服务器";
        }

        if (lower.Contains("name or service not known") || lower.Contains("no such host"))
        {
            return "无法解析服务器地址";
        }

        if (lower.Contains("ssl connection could not be established") || lower.Contains("certificate"))
        {
            return "安全连接失败，请检查网络或 API 地址";
        }

        if (lower.Contains("operation canceled") || lower.Contains("task was canceled"))
        {
            return "请求已取消";
        }

        if (lower.Contains("unauthorized"))
        {
            return "未授权，请检查 API Key";
        }

        if (lower.Contains("timeout") || lower.Contains("timed out"))
        {
            return "连接超时，请检查网络";
        }

        if (lower.Contains("network is unreachable"))
        {
            return "网络不可用";
        }

        if (lower is "ping")
        {
            return "连接测试成功";
        }

        return ContainsCjk(t) ? t : $"请求失败：{t}";
    }

    private static bool ContainsCjk(string s) =>
        s.Any(ch => ch is >= '\u4e00' and <= '\u9fff');

    [GeneratedRegex(@"^(\d{3})\s*[:：]?\s*(.*)$", RegexOptions.Singleline)]
    private static partial Regex StatusPrefix();
}
