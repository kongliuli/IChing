using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Rules;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Readings;

public sealed record ReadingPromptPacket(
    string Schema,
    string OutputSchema,
    string Domain,
    string Mode,
    string Language,
    int Tier,
    string? Question,
    string? Focus,
    IReadOnlyList<string> ComputedFacts,
    IReadOnlyList<string> RuleDigest,
    [property: JsonIgnore] IReadOnlyList<string> SystemDirectives,
    IReadOnlyList<PluginPromptContext> PluginContext,
    IReadOnlyList<OutputSectionSpec> OutputSections,
    string? Prior = null,
    string? UserQuestion = null);

public sealed record PluginPromptContext(
    string PluginId,
    IReadOnlyList<string> Facts,
    IReadOnlyList<string> Constraints,
    IReadOnlyList<string> OutputSections,
    [property: JsonIgnore] IReadOnlyList<string> SystemDirectives);

public sealed record OutputSectionSpec(string Key, string Title);

public sealed record ReadingPromptTemplate(
    string Domain,
    string Mode,
    IReadOnlyList<string> SystemDirectives,
    IReadOnlyList<OutputSectionSpec> OutputSections);

public sealed record ReadingOutput(
    string Summary,
    IReadOnlyList<ReadingOutputSection> Sections,
    IReadOnlyList<string>? Warnings = null);

public sealed record ReadingOutputSection(string Key, string Title, string Body);

public static class ReadingPromptProtocol
{
    public const string SystemPrompt =
        """
        You are a narrative renderer for computed divination data.
        The user message is a JSON envelope. Treat computedFacts, ruleDigest, and pluginContext as already-computed facts.
        Do not recalculate, replace, rename, or invent stems, branches, hexagrams, lines, cards, positions, dates, or plugin facts.
        Return only one valid JSON object. No markdown, no code fence, no extra prose.
        Output schema reading-output.v2:
        {
          "schema": "reading-output.v2",
          "summary": "one concise overall judgment",
          "sections": [
            { "key": "overview", "title": "section title", "body": "section body" }
          ],
          "warnings": []
        }
        Use the requested language and the requested outputSections. Keep answers concise and practical.
        """;

    private const int MaxSystemDirectives = 12;
    private const int MaxSystemDirectiveChars = 240;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string BuildSystemPrompt(ReadingPromptPacket packet)
    {
        var directives = packet.SystemDirectives
            .Concat(packet.PluginContext.SelectMany(p => p.SystemDirectives))
            .Select(d => Bound(d, MaxSystemDirectiveChars))
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSystemDirectives)
            .ToList();

        if (directives.Count == 0)
        {
            return SystemPrompt;
        }

        var sb = new StringBuilder(SystemPrompt.Trim());
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("Template and plugin system directives:");
        foreach (var directive in directives)
        {
            sb.AppendLine($"- {directive}");
        }

        return sb.ToString();
    }

    public static string BuildUserMessage(ReadingPromptPacket packet) =>
        JsonSerializer.Serialize(packet, JsonOptions);

    public static string NormalizeOutput(string raw)
    {
        var json = ExtractJson(raw);
        if (json is null)
        {
            return TryJsonLikeTextToMarkdown(raw) ?? LocalizeMarkdown(raw.Trim());
        }

        try
        {
            var output = JsonSerializer.Deserialize<ReadingOutput>(json, JsonOptions);
            return output is null ? raw.Trim() : ToMarkdown(output);
        }
        catch
        {
            return TryFlexibleJsonToMarkdown(json) ?? TryJsonLikeTextToMarkdown(json) ?? LocalizeMarkdown(raw.Trim());
        }
    }

    public static string ToMarkdownPublic(ReadingOutput output) => ToMarkdown(output);

    public static string? ExtractJsonPublic(string raw) => ExtractJson(raw);

    public static string LocalizeTitle(string title)
    {
        var text = title.Trim();
        return text.ToLowerInvariant() switch
        {
            "spread" => "牌阵",
            "spread basics" => "牌阵基础",
            "stats" => "统计",
            "statistics" => "统计",
            "element tendency" => "元素倾向",
            "element stats" => "元素统计",
            "meanings" => "牌义",
            "summary" => "总结",
            "overall" => "整体能量",
            "overall energy" => "整体能量",
            "advice" => "行动建议",
            "action advice" => "行动建议",
            _ => text
        };
    }

    public static bool IsKnownEnglishTitle(string title) =>
        !LocalizeTitle(title).Equals(title.Trim(), StringComparison.Ordinal);

    private static string LocalizeMarkdown(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                lines[i] = "## " + LocalizeTitle(line[3..]);
            }
            else if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                lines[i] = "### " + LocalizeTitle(line[4..]);
            }
            else if (IsKnownEnglishTitle(line))
            {
                lines[i] = "## " + LocalizeTitle(line);
            }
        }

        return string.Join('\n', lines).Trim();
    }

    private static string? TryJsonLikeTextToMarkdown(string text)
    {
        var sb = new StringBuilder();
        var title = string.Empty;
        var body = new StringBuilder();
        var warnings = new List<string>();

        void Flush()
        {
            var content = body.ToString().Trim();
            if (content.Length == 0)
            {
                return;
            }

            sb.AppendLine($"## {LocalizeTitle(Blank(title, "section"))}");
            sb.AppendLine(content);
            sb.AppendLine();
            title = string.Empty;
            body.Clear();
        }

        foreach (var rawLine in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (TryReadJsonishValue(line, "summary", out var summary))
            {
                sb.AppendLine("## 总结");
                sb.AppendLine(summary);
                sb.AppendLine();
                continue;
            }

            if (TryReadJsonishValue(line, "title", out var nextTitle))
            {
                Flush();
                title = nextTitle;
                continue;
            }

            if (TryReadJsonishValue(line, "body", out var nextBody))
            {
                if (body.Length > 0)
                {
                    body.AppendLine();
                }

                body.Append(nextBody);
                continue;
            }

            if (TryReadJsonishValue(line, "warnings", out var warning))
            {
                warnings.AddRange(warning.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }

        Flush();
        if (warnings.Count > 0)
        {
            sb.AppendLine("## 提醒");
            foreach (var warning in warnings)
            {
                sb.AppendLine($"- {warning}");
            }
        }

        var result = sb.ToString().Trim();
        return result.Length == 0 ? null : result;
    }

    private static bool TryReadJsonishValue(string line, string property, out string value)
    {
        value = string.Empty;
        var marker = $"\"{property}\"";
        var start = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return false;
        }

        var colon = line.IndexOf(':', start + marker.Length);
        if (colon < 0)
        {
            return false;
        }

        value = CleanJsonishValue(line[(colon + 1)..]);
        return value.Length > 0;
    }

    private static string CleanJsonishValue(string value)
    {
        var text = value.Trim().TrimEnd(',', '}', ']').Trim();
        if (text.StartsWith('['))
        {
            text = text.Trim('[', ']');
        }

        var parts = text.Split("\",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Trim().Trim('"', ',', '[', ']', '{', '}'))
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join("\n", parts);
    }

    private static string? TryFlexibleJsonToMarkdown(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var sb = new StringBuilder();
            if (ReadText(root, "summary") is { Length: > 0 } summary)
            {
                sb.AppendLine("## 总结");
                sb.AppendLine(summary);
                sb.AppendLine();
            }

            if (root.TryGetProperty("sections", out var sections) && sections.ValueKind == JsonValueKind.Array)
            {
                foreach (var section in sections.EnumerateArray())
                {
                    if (section.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var body = ReadText(section, "body");
                    if (string.IsNullOrWhiteSpace(body))
                    {
                        continue;
                    }

                    sb.AppendLine($"## {LocalizeTitle(Blank(ReadText(section, "title"), ReadText(section, "key") ?? "section"))}");
                    sb.AppendLine(body);
                    sb.AppendLine();
                }
            }

            if (ReadText(root, "warnings") is { Length: > 0 } warnings)
            {
                sb.AppendLine("## 提醒");
                foreach (var line in warnings.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    sb.AppendLine($"- {line}");
                }
            }

            var text = sb.ToString().Trim();
            return text.Length == 0 ? null : text;
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadText(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim(),
            JsonValueKind.Array => string.Join("\n", value.EnumerateArray().Select(TextValue).Where(x => !string.IsNullOrWhiteSpace(x))),
            JsonValueKind.Object => value.ToString(),
            _ => value.ToString()
        };
    }

    private static string TextValue(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() ?? string.Empty : value.ToString();

    private static string ToMarkdown(ReadingOutput output)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(output.Summary))
        {
            sb.AppendLine("## 总结");
            sb.AppendLine(output.Summary.Trim());
            sb.AppendLine();
        }

        foreach (var section in output.Sections ?? [])
        {
            if (string.IsNullOrWhiteSpace(section.Body))
            {
                continue;
            }

            sb.AppendLine($"## {LocalizeTitle(Blank(section.Title, section.Key))}");
            sb.AppendLine(section.Body.Trim());
            sb.AppendLine();
        }

        if (output.Warnings is { Count: > 0 })
        {
            sb.AppendLine("## 提醒");
            foreach (var warning in output.Warnings.Where(w => !string.IsNullOrWhiteSpace(w)))
            {
                sb.AppendLine($"- {warning.Trim()}");
            }
        }

        return sb.ToString().Trim();
    }

    private static string? ExtractJson(string raw)
    {
        var text = raw.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = text.IndexOf('\n');
            var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
            {
                text = text[(firstNewline + 1)..lastFence].Trim();
            }
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }

    private static string Blank(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string Bound(string? value, int max)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length <= max ? text : text[..max] + "...";
    }
}

public static class ReadingPromptTemplateManager
{
    private const int MaxOutputSections = 12;

    private static readonly IReadOnlyDictionary<string, ReadingPromptTemplate> Defaults =
        new Dictionary<string, ReadingPromptTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            ["bazi:initial"] = new(
                "bazi",
                "initial",
                ["Interpret only from the serialized bazi facts, rule digest, and plugin facts; never recompute pillars or yongshen."],
                [
                    new("overview", "整体判断"),
                    new("basis", "关键依据"),
                    new("advice", "行动建议")
                ]),
            ["liuyao:initial"] = new(
                "liuyao",
                "initial",
                ["Interpret only from the serialized liuyao facts, rule digest, and plugin facts; never change hexagrams, lines, six kin, shi, or ying."],
                [
                    new("overview", "整体判断"),
                    new("basis", "用神与动爻"),
                    new("advice", "行动建议")
                ]),
            ["tarot:initial"] = new(
                "tarot",
                "initial",
                ["Interpret only from the serialized tarot spread, positions, cards, and plugin facts; never add or replace cards."],
                [
                    new("overview", "整体能量")
                ])
        };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ReadingPromptTemplate Get(
        string domain,
        string mode,
        IEnumerable<OutputSectionSpec>? pluginSections = null)
    {
        var template = Defaults.TryGetValue(Key(domain, mode), out var found)
            ? found
            : new ReadingPromptTemplate(domain, mode, [], [new("overview", "整体判断"), new("advice", "行动建议")]);

        return template with
        {
            OutputSections = MergeSections(template.OutputSections, pluginSections)
        };
    }

    public static ReadingPromptTemplate LoadOrDefault(
        string? root,
        string domain,
        string mode,
        IEnumerable<OutputSectionSpec>? pluginSections = null)
    {
        var template = Get(domain, mode, pluginSections);
        if (string.IsNullOrWhiteSpace(root))
        {
            return template;
        }

        var path = Path.Combine(root, $"reading-template-{domain}-{mode}.json");
        if (!File.Exists(path))
        {
            return template;
        }

        try
        {
            var file = JsonSerializer.Deserialize<TemplateFile>(File.ReadAllText(path), JsonOptions);
            if (file is null)
            {
                return template;
            }

            return template with
            {
                SystemDirectives = BoundList(file.SystemDirectives, template.SystemDirectives, 240),
                OutputSections = MergeSections(
                    file.OutputSections is { Count: > 0 } ? file.OutputSections : template.OutputSections,
                    pluginSections)
            };
        }
        catch
        {
            return template;
        }
    }

    private static IReadOnlyList<OutputSectionSpec> MergeSections(
        IEnumerable<OutputSectionSpec> defaults,
        IEnumerable<OutputSectionSpec>? pluginSections)
    {
        var result = new List<OutputSectionSpec>();
        foreach (var section in defaults)
        {
            AddSection(result, section);
        }

        if (pluginSections is not null)
        {
            foreach (var section in pluginSections)
            {
                AddSection(result, section);
            }
        }

        return result.Take(MaxOutputSections).ToList();
    }

    private static void AddSection(List<OutputSectionSpec> result, OutputSectionSpec section)
    {
        if (result.Count >= MaxOutputSections)
        {
            return;
        }

        var title = Bound(section.Title, 48);
        if (string.IsNullOrWhiteSpace(title)
            || result.Any(s => string.Equals(s.Title, title, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var key = SectionKey(section.Key, result.Count + 1);
        if (result.Any(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            key = $"section{result.Count + 1}";
        }

        result.Add(new OutputSectionSpec(key, title));
    }

    private static IReadOnlyList<string> BoundList(
        IReadOnlyList<string>? values,
        IReadOnlyList<string> fallback,
        int maxChars)
    {
        var source = values is { Count: > 0 } ? values : fallback;
        return source
            .Select(v => Bound(v, maxChars))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static string SectionKey(string? value, int index)
    {
        var text = Bound(value, 32);
        var clean = new string(text.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return string.IsNullOrWhiteSpace(clean) ? $"section{index}" : clean;
    }

    private static string Key(string domain, string mode) =>
        $"{domain.Trim().ToLowerInvariant()}:{mode.Trim().ToLowerInvariant()}";

    private static string Bound(string? value, int max)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length <= max ? text : text[..max] + "...";
    }

    private sealed record TemplateFile(
        IReadOnlyList<string>? SystemDirectives,
        IReadOnlyList<OutputSectionSpec>? OutputSections);
}

public static class ReadingPromptPackets
{
    public static ReadingPromptPacket BaziInitial(BaziChart chart, BaziRuleDigest digest, string? focus)
    {
        var plugins = Plugins(digest.Items);
        var template = ReadingPromptTemplateManager.Get("bazi", "initial", PluginOutputSections(plugins));
        return new(
            Schema: "reading-request.v1",
            OutputSchema: ReadingSchemas.OutputV2,
            Domain: "bazi",
            Mode: "initial",
            Language: "zh-CN",
            Tier: 1,
            Question: null,
            Focus: focus,
            ComputedFacts:
            [
                $"engine: {chart.Engine}",
                $"time: {chart.WallClock}; lunar: {chart.Lunar}",
                $"pillars: year={chart.YearPillar.GanZhi}; month={chart.MonthPillar.GanZhi}; day={chart.DayPillar.GanZhi}; hour={chart.HourPillar.GanZhi}",
                $"dayMaster: {chart.DayMaster}",
                $"wuXingDominant: {chart.WuXingSummary.Dominant}",
                $"strength: {chart.YongShen.Strength}",
                $"pattern: {chart.YongShen.GeJu.Pattern}",
                $"primaryYongShen: {chart.YongShen.PrimaryYongShen}",
                $"secondaryYongShen: {chart.YongShen.SecondaryYongShen ?? ""}",
                $"daYun: {FormatDaYun(chart.DaYun)}"
            ],
            RuleDigest:
            [
                digest.PillarSummary,
                digest.YongShenSummary
            ],
            SystemDirectives: template.SystemDirectives,
            PluginContext: plugins,
            OutputSections: template.OutputSections);
    }

    public static ReadingPromptPacket LiuyaoInitial(
        LiuyaoNajiaResult chart,
        LiuyaoRuleDigest digest,
        string? question,
        string? focus)
    {
        var plugins = Plugins(digest.Items);
        var template = ReadingPromptTemplateManager.Get("liuyao", "initial", PluginOutputSections(plugins));
        return new(
            Schema: "reading-request.v1",
            OutputSchema: ReadingSchemas.OutputV2,
            Domain: "liuyao",
            Mode: "initial",
            Language: "zh-CN",
            Tier: 1,
            Question: question,
            Focus: focus,
            ComputedFacts:
            [
                $"engine: {chart.Engine}; method: {chart.Method}",
                $"hexagram: {chart.OriginalHexagram}; changed: {chart.ChangedHexagram ?? "none"}",
                $"time: {chart.CastingTime}",
                $"shi: {digest.ShiYaoSummary}",
                $"ying: {digest.YingYaoSummary}",
                $"changing: {string.Join("; ", digest.ChangingSummaries)}",
                $"lines: {FormatLiuyaoLines(chart.Lines)}"
            ],
            RuleDigest:
            [
                digest.QuestionType,
                digest.YongShenSummary,
                .. digest.Alerts
            ],
            SystemDirectives: template.SystemDirectives,
            PluginContext: plugins,
            OutputSections: template.OutputSections);
    }

    public static ReadingPromptPacket TarotInitial(TarotReading reading, TarotRuleDigest digest, string? question)
    {
        var plugins = Plugins(digest.Items);
        var template = ReadingPromptTemplateManager.Get(
            "tarot",
            "initial",
            reading.Positions
                .Select(p => new OutputSectionSpec(p.PositionKey, p.PositionTitleZh))
                .Concat(PluginOutputSections(plugins))
                .Append(new OutputSectionSpec("advice", "行动建议")));
        return new(
            Schema: "reading-request.v1",
            OutputSchema: ReadingSchemas.OutputV2,
            Domain: "tarot",
            Mode: "initial",
            Language: "zh-CN",
            Tier: 1,
            Question: question ?? reading.Question,
            Focus: null,
            ComputedFacts:
            [
                $"spread: {reading.SpreadTitleZh} / {reading.SpreadTitle}",
                $"cards: {FormatTarotCards(reading.Positions)}"
            ],
            RuleDigest:
            [
                $"major: {digest.MajorCount}/{digest.Total}",
                $"suits: wands={digest.Wands}; cups={digest.Cups}; swords={digest.Swords}; pentacles={digest.Pentacles}",
                $"reversed: {digest.ReversedCount}/{digest.Total}"
            ],
            SystemDirectives: template.SystemDirectives,
            PluginContext: plugins,
            OutputSections: template.OutputSections);
    }

    private static IReadOnlyList<PluginPromptContext> Plugins(IReadOnlyList<RuleDigestItem> items) =>
        items
            .GroupBy(i => i.PluginId)
            .Select(g => new PluginPromptContext(
                g.Key,
                g.Select(i => $"{i.Title}: {i.Text}").ToList(),
                [],
                g.Select(i => i.Title).Distinct().ToList(),
                g.Select(i => $"Plugin {g.Key} must be considered when relevant: {i.Title}: {Bound(i.Text, 160)}").ToList()))
            .ToList();

    private static IEnumerable<OutputSectionSpec> PluginOutputSections(IReadOnlyList<PluginPromptContext> plugins)
    {
        var index = 0;
        foreach (var title in plugins.SelectMany(p => p.OutputSections).Distinct())
        {
            yield return new OutputSectionSpec($"plugin{++index}", title);
        }
    }

    private static string FormatDaYun(IReadOnlyList<DaYunPeriod>? daYun) =>
        daYun is { Count: > 0 }
            ? string.Join("; ", daYun.Take(5).Select(x => $"{x.StartAge}-{x.EndAge}: {x.GanZhi}"))
            : "not calculated";

    private static string FormatLiuyaoLines(IReadOnlyList<LiuyaoLineDetail> lines) =>
        string.Join("; ", lines.OrderBy(l => l.Index).Select(l =>
            $"{l.Index}.{l.Position}:{l.YinYang},{l.SixKin},{l.StemBranch},{l.SixSpirit},{l.Role}{(l.IsChanging ? ",changing" : "")}"));

    private static string FormatTarotCards(IReadOnlyList<TarotPositionReading> positions) =>
        string.Join("; ", positions.Select(p =>
            $"[{p.PositionTitleZh}/{p.PositionTitle}] {p.CardNameZh}/{p.CardName} {(p.Reversed ? "reversed" : "upright")}: {p.Meaning}"));

    private static string Bound(string? value, int max)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length <= max ? text : text[..max] + "...";
    }
}
