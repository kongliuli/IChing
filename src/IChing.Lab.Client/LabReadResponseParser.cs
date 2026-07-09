using System.Text.Json;

namespace IChing.Lab.Client;

public static class LabReadResponseParser
{
    public static string? TryGetNarrativeText(JsonElement root)
    {
        if (root.TryGetProperty("narrative", out var narrative)
            && narrative.ValueKind == JsonValueKind.Object
            && narrative.TryGetProperty("text", out var textProp)
            && textProp.ValueKind == JsonValueKind.String)
        {
            return textProp.GetString();
        }

        if (root.TryGetProperty("tier0Preview", out var preview)
            && preview.TryGetProperty("oneLiner", out var oneLiner)
            && oneLiner.ValueKind == JsonValueKind.String)
        {
            return oneLiner.GetString();
        }

        return null;
    }

    public static bool TryGetIsFallback(JsonElement root)
    {
        return root.TryGetProperty("narrative", out var narrative)
               && narrative.TryGetProperty("isFallback", out var fb)
               && fb.ValueKind == JsonValueKind.True;
    }
}
