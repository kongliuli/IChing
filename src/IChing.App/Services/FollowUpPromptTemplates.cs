using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;

namespace IChing.App.Services;

public static class FollowUpPromptTemplates
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static (string SystemPrompt, string Context) Bazi(BaziChart chart, BaziRuleDigest digest, string? focus, string interpretation) =>
        (
            "你是八字追问助手。只能基于已给出的四柱、用神、规则摘要和初始解读回答；不能改动干支、日主、用神或大运。回答要短，直接回应用户追问。",
            $"""
            当前八字：{chart.YearPillar.GanZhi} {chart.MonthPillar.GanZhi} {chart.DayPillar.GanZhi} {chart.HourPillar.GanZhi}
            最初关注：{focus ?? "综合"}
            日主：{chart.DayMaster}
            规则摘要：{JsonSerializer.Serialize(digest, JsonOptions)}
            初始解读：{interpretation}
            """);

    public static (string SystemPrompt, string Context) Liuyao(LiuyaoNajiaResult chart, LiuyaoRuleDigest digest, string? question, string? focus, string interpretation) =>
        (
            "你是六爻追问助手。只能基于已给出的本卦、变卦、六亲、世应、用神和初始解读回答；不能改动卦名、爻位、六亲、世应。回答要短，先判断再建议。",
            $"""
            当前六爻：{chart.OriginalHexagram}{(chart.ChangedHexagram is null ? "" : $"，变卦 {chart.ChangedHexagram}")}
            最初问题：{question ?? "未填写"}
            关注点：{focus ?? "综合"}
            规则摘要：{JsonSerializer.Serialize(digest, JsonOptions)}
            初始解读：{interpretation}
            """);
}
