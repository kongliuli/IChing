using System.Text.RegularExpressions;
using IChing.Tarot.App.Models;

namespace IChing.Tarot.App.Services;

/// <summary>将 AI 解读文本拆成卡片段落（支持 ## / ### / 编号标题）。</summary>
public static partial class InterpretationSectionParser
{
    private static readonly Color Purple = Color.FromArgb("#B794F6");
    private static readonly Color Gold = Color.FromArgb("#D4AF37");
    private static readonly Color Teal = Color.FromArgb("#7FD992");

    public static IReadOnlyList<InterpretationSectionItem> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var sections = new List<InterpretationSectionItem>();
        string? currentTitle = null;
        var body = new List<string>();
        var isSub = false;

        void Flush()
        {
            if (currentTitle is null && body.Count == 0)
            {
                return;
            }

            var content = string.Join('\n', body).Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                body.Clear();
                currentTitle = null;
                isSub = false;
                return;
            }

            var title = UserFacingZh.SectionTitle(currentTitle ?? "解读");
            sections.Add(new InterpretationSectionItem
            {
                Title = title,
                Body = content,
                IsSubsection = isSub,
                Band = ClassifyBand(title, isSub),
                Accent = PickAccent(title)
            });
            body.Clear();
            currentTitle = null;
            isSub = false;
        }

        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (Heading3().Match(line) is { Success: true } h3)
            {
                Flush();
                currentTitle = h3.Groups[1].Value.Trim();
                isSub = true;
                continue;
            }

            if (Heading2().Match(line) is { Success: true } h2)
            {
                Flush();
                currentTitle = h2.Groups[1].Value.Trim();
                isSub = false;
                continue;
            }

            if (Numbered().Match(line) is { Success: true } num)
            {
                Flush();
                currentTitle = num.Groups[1].Value.Trim();
                isSub = false;
                continue;
            }

            if (BracketHeading().Match(line) is { Success: true } br)
            {
                Flush();
                currentTitle = br.Groups[1].Value.Trim();
                isSub = true;
                continue;
            }

            body.Add(line);
        }

        Flush();
        var filtered = sections.Where(s => !string.IsNullOrWhiteSpace(s.Body)).ToList();
        return filtered.Count > 0
            ? filtered
            : [];
    }

    private static InterpretationLayoutBand ClassifyBand(string title, bool isSub)
    {
        if (title.Contains("建议", StringComparison.Ordinal) ||
            title.Contains("行动", StringComparison.Ordinal) ||
            title.Contains("提醒", StringComparison.Ordinal))
        {
            return InterpretationLayoutBand.Advice;
        }

        if (title.Contains("整体", StringComparison.Ordinal) ||
            title.Contains("概览", StringComparison.Ordinal) ||
            title.Contains("能量", StringComparison.Ordinal) ||
            title.Contains("总结", StringComparison.Ordinal))
        {
            return InterpretationLayoutBand.Overview;
        }

        if (isSub)
        {
            return InterpretationLayoutBand.Card;
        }

        return InterpretationLayoutBand.General;
    }

    private static Color PickAccent(string title)
    {
        if (title.Contains("建议", StringComparison.Ordinal) ||
            title.Contains("行动", StringComparison.Ordinal))
        {
            return Teal;
        }

        if (title.Contains("整体", StringComparison.Ordinal) ||
            title.Contains("概览", StringComparison.Ordinal) ||
            title.Contains("能量", StringComparison.Ordinal))
        {
            return Gold;
        }

        return Purple;
    }

    [GeneratedRegex(@"^###\s+(.+)$")]
    private static partial Regex Heading3();

    [GeneratedRegex(@"^##\s+(.+)$")]
    private static partial Regex Heading2();

    [GeneratedRegex(@"^\d+[\.、)\]]\s*(.+)$")]
    private static partial Regex Numbered();

    [GeneratedRegex(@"^【(.+)】$")]
    private static partial Regex BracketHeading();
}
