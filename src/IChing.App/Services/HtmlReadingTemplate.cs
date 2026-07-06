using System.Net;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;

namespace IChing.App.Services;

public static class HtmlReadingTemplate
{
    public static string BuildBazi(BaziChart chart, BaziRuleDigest digest, string? focus, string? interpretation) =>
        Page(
            "八字命盘",
            focus ?? "综合",
            $"""
            <section class="hero">
              <div class="seal">命</div>
              <div>
                <p class="eyebrow">乾 坤 震 巽 坎 离 艮 兑</p>
                <h1>日主 {H(chart.DayMaster)}</h1>
                <p>{H(chart.WallClock)} · 农历 {H(chart.Lunar)}</p>
              </div>
            </section>
            <section class="pillars">
              {Pillar("年柱", chart.YearPillar.GanZhi)}
              {Pillar("月柱", chart.MonthPillar.GanZhi)}
              {Pillar("日柱", chart.DayPillar.GanZhi)}
              {Pillar("时柱", chart.HourPillar.GanZhi)}
            </section>
            {Block("命盘摘要", $"{chart.WuXingSummary.Dominant}；{digest.PillarSummary}")}
            {Block("用神判断", digest.YongShenSummary)}
            {Block("大运概览", chart.DaYun is { Count: > 0 } ? string.Join("　", chart.DaYun.Take(5).Select(x => $"{x.StartAge}-{x.EndAge}岁 {x.GanZhi}")) : "未计算大运")}
            {Block("AI 解读", interpretation)}
            """);

    public static string BuildLiuyao(LiuyaoNajiaResult chart, LiuyaoRuleDigest digest, string? question, string? interpretation)
    {
        var changed = chart.ChangedHexagram is null ? "无变卦" : $"变卦 {chart.ChangedHexagram}";
        var lines = string.Join("", chart.Lines.OrderByDescending(x => x.Index).Select(line =>
            $"""
            <div class="line">
              <b>{H(line.Position)} · {H(line.YinYang)}{(line.IsChanging ? " 动" : "")}</b>
              <span>{H($"{line.SixKin} {line.StemBranch} {line.SixSpirit} {line.Role}".Trim())}</span>
            </div>
            """));

        return Page(
            "六爻卦例",
            question ?? "未填写问题",
            $"""
            <section class="hero">
              <div class="seal">卦</div>
              <div>
                <p class="eyebrow">少阳 少阴 老阳 老阴</p>
                <h1>{H(chart.OriginalHexagram)}</h1>
                <p>{H(changed)} · {H(chart.Method)}</p>
              </div>
            </section>
            {Block("世应与用神", $"{digest.ShiYaoSummary}\n{digest.YingYaoSummary}\n{digest.YongShenSummary}")}
            <section class="card"><h2>六爻</h2><div class="lines">{lines}</div></section>
            {Block("AI 解读", interpretation)}
            """);
    }

    private static string Page(string title, string subject, string body) =>
        $$"""
        <!doctype html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <style>
            :root { --paper:#f6efe0; --card:#fffdf7; --ink:#241c17; --muted:#766957; --line:#d6c7a8; --red:#a33a2d; --jade:#2f6f5e; }
            * { box-sizing:border-box; }
            body { margin:0; padding:24px; background:var(--paper); color:var(--ink); font-family:"Microsoft YaHei","PingFang SC",system-ui,sans-serif; }
            main { max-width:760px; margin:0 auto; }
            .meta { margin:0 0 14px; color:var(--muted); font-size:13px; }
            .hero,.card { background:var(--card); border:1px solid var(--line); border-radius:14px; box-shadow:0 10px 30px rgba(36,28,23,.07); }
            .hero { display:flex; gap:18px; align-items:center; padding:22px; margin-bottom:14px; }
            .seal { width:64px; height:64px; border:3px solid var(--red); color:var(--red); display:grid; place-items:center; font-size:32px; font-weight:800; border-radius:16px; }
            .eyebrow { margin:0 0 6px; color:var(--red); font-size:12px; letter-spacing:.04em; }
            h1 { margin:0 0 6px; font-size:34px; line-height:1.15; }
            h2 { margin:0 0 10px; font-size:17px; color:var(--jade); }
            p { margin:0; line-height:1.75; }
            .pillars { display:grid; grid-template-columns:repeat(4,1fr); gap:10px; margin-bottom:14px; }
            .pillar { padding:14px 10px; text-align:center; background:var(--card); border:1px solid var(--line); border-radius:12px; }
            .pillar span { display:block; color:var(--muted); font-size:12px; }
            .pillar b { display:block; margin-top:4px; font-size:24px; }
            .card { padding:16px; margin-bottom:14px; }
            .lines { display:grid; gap:8px; }
            .line { display:flex; justify-content:space-between; gap:12px; padding:10px 0; border-bottom:1px solid var(--line); }
            .line:last-child { border-bottom:0; }
            .line span { color:var(--muted); text-align:right; }
          </style>
        </head>
        <body>
          <main>
            <p class="meta">{{H(title)}} · {{H(subject)}}</p>
            {{body}}
          </main>
        </body>
        </html>
        """;

    private static string Block(string title, string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : $"""<section class="card"><h2>{H(title)}</h2><p>{H(text).Replace("\n", "<br>")}</p></section>""";

    private static string Pillar(string label, string value) =>
        $"""<div class="pillar"><span>{H(label)}</span><b>{H(value)}</b></div>""";

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
