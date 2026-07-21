using IChing.Lab.Core.Tarot;
using IChing.Tarot.App.Services;
using IChing.Tarot.App.Models;
using IChing.Tarot.App.Views;

namespace IChing.Tarot.App.Services;

public static class FunToolsService
{
    public static TarotCard PickSpiritCard(DateTime birthday)
    {
        var hash = HashCode.Combine(birthday.Year, birthday.Month, birthday.Day);
        var index = Math.Abs(hash) % TarotDeck.MajorOnly.Count;
        return TarotDeck.MajorOnly[index];
    }

    public sealed record QuizQuestion(string Text, string Fire, string Water, string Air, string Earth);

    public static IReadOnlyList<QuizQuestion> ElementQuestions { get; } =
    [
        new("周末最理想的状态是？", "户外冒险、动起来", "和亲近的人窝着", "读一本书或看展", "整理房间、做顿好饭"),
        new("遇到冲突时你更常？", "直接表达、快速解决", "先感受情绪再沟通", "理性分析利弊", "保持冷静、等时机"),
        new("你更被什么吸引？", "挑战与成就", "深度连接与共鸣", "新点子与可能性", "稳定与可预期"),
        new("压力来临时你会？", "加倍行动", "找信任的人倾诉", "列出计划分解问题", "规律作息、一步步来"),
        new("朋友形容你像？", "热情有行动力", "温柔敏感", "聪明善辩", "可靠务实"),
        new("做决定时更看重？", "直觉与热情", "感受与他人", "逻辑与信息", "现实与长期收益"),
        new("理想的工作环境？", "快节奏、多变化", "有温度、重协作", "开放讨论、创意自由", "流程清晰、资源充足"),
        new("你恢复能量的方式是？", "运动或旅行", "泡澡、音乐、独处", "学习新东西", "做饭、理财、整理")
    ];

    public static (string Element, string Title, string Summary, Color Color) ScoreElements(
        IReadOnlyDictionary<int, string> answers)
    {
        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["fire"] = 0, ["water"] = 0, ["air"] = 0, ["earth"] = 0
        };

        foreach (var (index, choice) in answers)
        {
            var q = ElementQuestions[index];
            if (choice == q.Fire) scores["fire"]++;
            else if (choice == q.Water) scores["water"]++;
            else if (choice == q.Air) scores["air"]++;
            else scores["earth"]++;
        }

        var top = scores.OrderByDescending(kv => kv.Value).First().Key;
        return top switch
        {
            "fire" => ("火", "行动派 · 火元素", "你以热情与行动力驱动生活，适合主动创造与快速迭代。", Color.FromArgb("#E07A3A")),
            "water" => ("水", "感受派 · 水元素", "你重视情感与直觉，在关系与内在世界中汲取力量。", Color.FromArgb("#4A9FD4")),
            "air" => ("风", "思维派 · 风元素", "你擅长分析与表达，在信息与创意流动中找到自由。", Color.FromArgb("#8B9CB3")),
            _ => ("土", "稳健派 · 土元素", "你脚踏实地、重视积累，在稳定与现实中构建安全感。", Color.FromArgb("#6FAF6A"))
        };
    }

    public static (string Name, string Hex, string Hint) DailyColor(DateTime date)
    {
        var palette = new (string Name, string Hex, string Hint)[]
        {
            ("琥珀金", "#D4AF37", "适合设定目标，把想法落地。"),
            ("深紫", "#7B5EA7", "适合内观与直觉写作。"),
            ("海蓝", "#4A9FD4", "适合沟通、修复关系。"),
            ("翡翠绿", "#6FAF6A", "适合整理财务与健康习惯。"),
            ("珊瑚橙", "#E07A3A", "适合启动新项目或社交。"),
            ("银灰", "#8B9CB3", "适合学习、阅读与规划。"),
            ("玫瑰粉", "#C97B9A", "适合自我关怀与创意表达。")
        };

        var index = Math.Abs(HashCode.Combine(date.Year, date.Month, date.Day)) % palette.Length;
        return palette[index];
    }
}

public static class ReadingExportBuilder
{
    public static View BuildSpread(TarotReading reading, IReadOnlyList<CardDisplayItem> cards)
    {
        var root = new VerticalStackLayout { Spacing = 0, BackgroundColor = Color.FromArgb("#0B0812") };
        var title = reading.Question is { Length: > 0 } q
            ? $"「{q}」· {reading.SpreadTitleZh}"
            : reading.SpreadTitleZh;
        root.Add(ExportService.BuildHeader("牌阵结果", title));

        var boardHost = new Grid();
        SpreadBoardLayout.Render(boardHost, reading.SpreadId, cards);
        root.Add(new Border
        {
            Padding = 12,
            Margin = new Thickness(12, 0),
            BackgroundColor = Color.FromArgb("#161022"),
            Stroke = Color.FromArgb("#9A7B2C"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Content = boardHost
        });

        foreach (var card in cards)
        {
            root.Add(ExportService.BuildTextBlock(
                card.PositionTitle,
                $"{card.CardLine}\n\n{card.Meaning}"));
        }

        root.Add(ExportService.BuildFooter());
        return root;
    }

    public static View BuildInterpretation(
        TarotReading reading,
        IReadOnlyList<InterpretationSectionItem> sections)
    {
        var root = new VerticalStackLayout { Spacing = 0, BackgroundColor = Color.FromArgb("#0B0812") };
        var title = reading.Question is { Length: > 0 } q
            ? $"「{q}」· {reading.SpreadTitleZh}"
            : reading.SpreadTitleZh;
        root.Add(ExportService.BuildHeader("解读结果", title));

        foreach (var section in sections)
        {
            root.Add(ExportService.BuildTextBlock(section.Title, section.Body, section.Band == InterpretationLayoutBand.Advice));
        }

        root.Add(ExportService.BuildFooter());
        return root;
    }
}
