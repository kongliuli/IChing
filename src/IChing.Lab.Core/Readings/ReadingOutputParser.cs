using System.Text.Json;
using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Core.Readings;

public static class ReadingOutputParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ReadingStructuredOutput? TryParseStructured(string? raw, string domain)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var json = ReadingPromptProtocol.ExtractJsonPublic(raw) ?? raw.Trim();
        if (!json.StartsWith('{'))
        {
            return FallbackFromPlainText(raw, domain);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return FromJsonElement(doc.RootElement, domain, raw);
        }
        catch
        {
            return FallbackFromPlainText(raw, domain);
        }
    }

    public static ReadingStructuredOutput FromJsonElement(JsonElement root, string domain, string? rawFallback = null)
    {
        var schema = root.TryGetProperty("schema", out var schemaEl) && schemaEl.ValueKind == JsonValueKind.String
            ? schemaEl.GetString() ?? ReadingSchemas.OutputV2
            : ReadingSchemas.OutputV2;

        var summary = root.TryGetProperty("summary", out var summaryEl) && summaryEl.ValueKind == JsonValueKind.String
            ? summaryEl.GetString() ?? string.Empty
            : string.Empty;

        var sections = new List<ReadingStructuredSection>();
        if (root.TryGetProperty("sections", out var sectionsEl) && sectionsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in sectionsEl.EnumerateArray())
            {
                var key = item.TryGetProperty("key", out var keyEl) ? keyEl.GetString() ?? "section" : "section";
                var title = item.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? key : key;
                var body = item.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? string.Empty : string.Empty;
                sections.Add(new ReadingStructuredSection(key, title, body));
            }
        }

        var warnings = new List<string>();
        if (root.TryGetProperty("warnings", out var warningsEl) && warningsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var w in warningsEl.EnumerateArray())
            {
                if (w.ValueKind == JsonValueKind.String)
                {
                    var text = w.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        warnings.Add(text);
                    }
                }
            }
        }

        warnings.AddRange(ReadingSectionKeyRegistry.ValidateKeys(domain, sections.Select(s => s.Key)));

        ReadingOutputMeta? meta = null;
        if (root.TryGetProperty("meta", out var metaEl) && metaEl.ValueKind == JsonValueKind.Object)
        {
            meta = new ReadingOutputMeta(
                metaEl.TryGetProperty("confidence", out var c) ? c.GetString() : null,
                metaEl.TryGetProperty("disclaimer", out var d) ? d.GetString() : null);
        }

        if (sections.Count == 0 && string.IsNullOrWhiteSpace(summary) && !string.IsNullOrWhiteSpace(rawFallback))
        {
            return FallbackFromPlainText(rawFallback, domain)!;
        }

        return new ReadingStructuredOutput(schema, summary, sections, warnings, meta);
    }

    public static string ToMarkdown(ReadingStructuredOutput output)
    {
        var legacy = new ReadingOutput(
            output.Summary,
            output.Sections.Select(s => new ReadingOutputSection(s.Key, s.Title, s.Body)).ToList(),
            output.Warnings.Count > 0 ? output.Warnings : null);
        return ReadingPromptProtocol.ToMarkdownPublic(legacy);
    }

    public static ExchangeOutput BuildExchangeOutput(
        string domain,
        string? text,
        string? textEn,
        string engineId,
        bool isFallback,
        string? fallbackReason = null,
        string? promptTemplateId = null)
    {
        var structured = TryParseStructured(text, domain);
        return new ExchangeOutput(structured, text, textEn, engineId, isFallback, fallbackReason, promptTemplateId);
    }

    private static ReadingStructuredOutput? FallbackFromPlainText(string raw, string domain)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return new ReadingStructuredOutput(
            ReadingSchemas.OutputV2,
            trimmed,
            [],
            ReadingSectionKeyRegistry.ValidateKeys(domain, Array.Empty<string>()));
    }
}
