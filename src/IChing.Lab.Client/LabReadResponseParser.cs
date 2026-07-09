using System.Text;
using System.Text.Json;
using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Client;

public static class LabReadResponseParser
{
    public static string? TryGetNarrativeText(JsonElement root)
    {
        if (IsEnvelopeV2(root))
        {
            return TryGetStructuredText(root);
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
        if (!IsEnvelopeV2(root))
        {
            return false;
        }

        return root.TryGetProperty("exchange", out var exchange)
               && exchange.TryGetProperty("output", out var output)
               && output.TryGetProperty("isFallback", out var fb)
               && fb.ValueKind == JsonValueKind.True;
    }

    private static bool IsEnvelopeV2(JsonElement root) =>
        root.TryGetProperty("schema", out var schema)
        && schema.ValueKind == JsonValueKind.String
        && string.Equals(schema.GetString(), ReadingSchemas.EnvelopeV2, StringComparison.Ordinal);

    private static string? TryGetStructuredText(JsonElement root)
    {
        if (!root.TryGetProperty("exchange", out var exchange)
            || !exchange.TryGetProperty("output", out var output))
        {
            return null;
        }

        if (output.TryGetProperty("structured", out var structured)
            && structured.ValueKind == JsonValueKind.Object)
        {
            if (structured.TryGetProperty("summary", out var summary)
                && summary.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(summary.GetString()))
            {
                return summary.GetString();
            }

            if (structured.TryGetProperty("sections", out var sections)
                && sections.ValueKind == JsonValueKind.Array)
            {
                var sb = new StringBuilder();
                foreach (var section in sections.EnumerateArray())
                {
                    if (section.TryGetProperty("body", out var body)
                        && body.ValueKind == JsonValueKind.String)
                    {
                        sb.AppendLine(body.GetString());
                    }
                }

                var text = sb.ToString().Trim();
                if (text.Length > 0)
                {
                    return text;
                }
            }
        }

        if (output.TryGetProperty("rawText", out var raw)
            && raw.ValueKind == JsonValueKind.String)
        {
            return raw.GetString();
        }

        return null;
    }
}
