using System.Net;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;
using IChing.Lab.Presentation;

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
                <p class="eyebrow">天干地支 · 五行旺衰 · 用神取向</p>
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
            {HtmlBlock("AI 解读", interpretation)}
            """);

    public static string BuildLiuyao(LiuyaoNajiaResult chart, LiuyaoRuleDigest digest, string? question, string? interpretation)
    {
        var original = HexagramName(chart.OriginalHexagram);
        var changed = chart.ChangedHexagram is null ? "无变卦" : $"变卦 {HexagramName(chart.ChangedHexagram)}";
        var method = chart.Method == "coin" ? "铜钱卦" : chart.Method == "time" ? "时间卦" : chart.Method;
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
                <p class="eyebrow">铜钱起卦 · 纳甲六亲 · 世应用神</p>
                <h1>{H(original)}</h1>
                <p>{H(changed)} · {H(method)}</p>
              </div>
            </section>
            {Block("世应与用神", $"{digest.ShiYaoSummary}\n{digest.YingYaoSummary}\n{digest.YongShenSummary}")}
            <section class="card"><h2>六爻</h2><div class="lines">{lines}</div></section>
            {HtmlBlock("AI 解读", interpretation)}
            """);
    }

    public static string HexagramName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim();
        var display = HexagramNames.Display(trimmed);
        return display.Any(c => c is >= 'A' and <= 'z') ? "卦名待补" : display;
    }

    private static string Page(string title, string subject, string body) =>
        $$"""
        <!doctype html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <style>
            :root { --paper:#f7f3ea; --card:#fffdf8; --ink:#211b16; --muted:#786f63; --line:#ded3c1; --red:#a83a2d; --jade:#1f7668; --gold:#b8944d; }
            * { box-sizing:border-box; }
            body { margin:0; padding:24px; background:var(--paper); color:var(--ink); font-family:"Microsoft YaHei","PingFang SC",system-ui,sans-serif; }
            main { max-width:760px; margin:0 auto; }
            .meta { margin:0 0 14px; color:var(--muted); font-size:13px; }
            .hero,.card,.pillar { background:var(--card); border:1px solid var(--line); border-radius:8px; box-shadow:0 10px 28px rgba(33,27,22,.06); }
            .hero { display:flex; gap:18px; align-items:center; padding:22px; margin-bottom:14px; }
            .seal { width:64px; height:64px; border:3px solid var(--red); color:var(--red); display:grid; place-items:center; font-size:32px; font-weight:800; border-radius:8px; }
            .eyebrow { margin:0 0 6px; color:var(--red); font-size:12px; }
            h1 { margin:0 0 6px; font-size:34px; line-height:1.15; }
            h2 { margin:0 0 10px; font-size:17px; color:var(--jade); }
            p { margin:0; line-height:1.75; white-space:normal; }
            .pillars { display:grid; grid-template-columns:repeat(4,1fr); gap:10px; margin-bottom:14px; }
            .pillar { padding:14px 10px; text-align:center; }
            .pillar span { display:block; color:var(--muted); font-size:12px; }
            .pillar b { display:block; margin-top:4px; font-size:24px; }
            .card { padding:16px; margin-bottom:14px; }
            .card .section { padding:12px 0 0; margin:12px 0 0; border-top:1px solid var(--line); }
            .card .section:first-of-type { margin-top:0; }
            .card .section h2 { color:var(--red); }
            .card .section h3 { margin:0 0 8px; color:var(--jade); font-size:15px; }
            .card .section ul { margin:0; padding:0; list-style:none; }
            .card .section li { position:relative; margin:0 0 6px; padding-left:16px; line-height:1.7; }
            .card .section li:before { content:""; position:absolute; left:2px; top:.75em; width:5px; height:5px; border-radius:50%; background:var(--jade); }
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

    private static string HtmlBlock(string title, string? markdown) =>
        string.IsNullOrWhiteSpace(markdown)
            ? string.Empty
            : $"""<section class="card"><h2>{H(title)}</h2>{ReadingHtmlFormatter.ToFragment(markdown)}</section>""";

    private static string Pillar(string label, string value) =>
        $"""<div class="pillar"><span>{H(label)}</span><b>{H(value)}</b></div>""";

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

}
