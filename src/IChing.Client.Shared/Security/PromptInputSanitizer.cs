using System.Text.RegularExpressions;

namespace IChing.Client.Shared.Security;

/// <summary>
/// 剥离 ChatML / 指令注入标记，并限制用户输入长度。
/// </summary>
public static partial class PromptInputSanitizer
{
    public const int DefaultMaxLength = 500;

    [GeneratedRegex(@"<\|[^|>]+\|>", RegexOptions.CultureInvariant)]
    private static partial Regex SpecialTokenRegex();

    [GeneratedRegex(@"(?i)(system\s*:|assistant\s*:|###\s*(system|instruction))", RegexOptions.CultureInvariant)]
    private static partial Regex RolePrefixRegex();

    public static string SanitizeUserText(string? input, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var text = input.Trim();
        text = SpecialTokenRegex().Replace(text, " ");
        text = RolePrefixRegex().Replace(text, " ");
        text = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (text.Length > maxLength)
        {
            text = text[..maxLength];
        }

        return text;
    }
}
