namespace IChing.Lab.Core.Readings;

public static class ReadingSectionKeyRegistry
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedKeys =
        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["bazi"] =
            [
                "overview", "yongshen", "geju", "flow", "advice", "warnings"
            ],
            ["liuyao"] =
            [
                "overview", "yongshen", "changing", "shi_ying", "advice", "warnings"
            ],
            ["tarot"] =
            [
                "overview", "spread", "advice", "warnings"
            ]
        };

    public static bool IsKnownKey(string domain, string key)
    {
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return AllowedKeys.TryGetValue(domain, out var set) && set.Contains(key);
    }

    public static IReadOnlyList<string> ValidateKeys(string domain, IEnumerable<string> keys)
    {
        var warnings = new List<string>();
        foreach (var key in keys)
        {
            if (!IsKnownKey(domain, key))
            {
                warnings.Add($"unknown_section_key:{key}");
            }
        }

        return warnings;
    }
}
