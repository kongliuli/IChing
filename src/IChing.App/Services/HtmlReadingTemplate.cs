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
            {Block("AI 解读", interpretation)}
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
            {Block("AI 解读", interpretation)}
            """);
    }

    public static string HexagramName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim();
        if (Hexagrams.TryGetValue(trimmed, out var zh))
        {
            return zh;
        }

        return trimmed.Any(c => c is >= 'A' and <= 'z') ? "未识别卦" : trimmed;
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

    private static readonly Dictionary<string, string> Hexagrams = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Qian"] = "乾为天", ["The Creative"] = "乾为天", ["乾"] = "乾为天",
        ["Kun"] = "坤为地", ["The Receptive"] = "坤为地", ["坤"] = "坤为地",
        ["Zhun"] = "水雷屯", ["Difficulty at the Beginning"] = "水雷屯",
        ["Meng"] = "山水蒙", ["Youthful Folly"] = "山水蒙",
        ["Xu"] = "水天需", ["Waiting"] = "水天需",
        ["Song"] = "天水讼", ["Conflict"] = "天水讼",
        ["Shi"] = "地水师", ["The Army"] = "地水师",
        ["Bi"] = "水地比", ["Holding Together"] = "水地比",
        ["Xiao Chu"] = "风天小畜", ["Small Taming"] = "风天小畜",
        ["Lu"] = "天泽履", ["Treading"] = "天泽履",
        ["Tai"] = "地天泰", ["Peace"] = "地天泰",
        ["Pi"] = "天地否", ["Standstill"] = "天地否",
        ["Tong Ren"] = "天火同人", ["Fellowship"] = "天火同人",
        ["Da You"] = "火天大有", ["Great Possession"] = "火天大有",
        ["Qian Humility"] = "地山谦", ["Modesty"] = "地山谦",
        ["Yu"] = "雷地豫", ["Enthusiasm"] = "雷地豫",
        ["Sui"] = "泽雷随", ["Following"] = "泽雷随",
        ["Gu"] = "山风蛊", ["Work on the Decayed"] = "山风蛊",
        ["Lin"] = "地泽临", ["Approach"] = "地泽临",
        ["Guan"] = "风地观", ["Contemplation"] = "风地观",
        ["Shi He"] = "火雷噬嗑", ["Biting Through"] = "火雷噬嗑",
        ["Bi Grace"] = "山火贲", ["Grace"] = "山火贲",
        ["Bo"] = "山地剥", ["Splitting Apart"] = "山地剥",
        ["Fu"] = "地雷复", ["Return"] = "地雷复",
        ["Wu Wang"] = "天雷无妄", ["Innocence"] = "天雷无妄",
        ["Da Chu"] = "山天大畜", ["Great Taming"] = "山天大畜",
        ["Yi"] = "山雷颐", ["Nourishment"] = "山雷颐",
        ["Da Guo"] = "泽风大过", ["Great Preponderance"] = "泽风大过",
        ["Kan"] = "坎为水", ["The Abysmal"] = "坎为水",
        ["Li"] = "离为火", ["The Clinging"] = "离为火",
        ["Xian"] = "泽山咸", ["Influence"] = "泽山咸",
        ["Heng"] = "雷风恒", ["Duration"] = "雷风恒",
        ["Dun"] = "天山遁", ["Retreat"] = "天山遁",
        ["Da Zhuang"] = "雷天大壮", ["Great Power"] = "雷天大壮",
        ["Jin"] = "火地晋", ["Progress"] = "火地晋",
        ["Ming Yi"] = "地火明夷", ["Darkening of the Light"] = "地火明夷",
        ["Jia Ren"] = "风火家人", ["The Family"] = "风火家人",
        ["Kui"] = "火泽睽", ["Opposition"] = "火泽睽",
        ["Jian"] = "水山蹇", ["Obstruction"] = "水山蹇",
        ["Jie"] = "雷水解", ["Deliverance"] = "雷水解",
        ["Sun"] = "山泽损", ["Decrease"] = "山泽损",
        ["Yi Increase"] = "风雷益", ["Increase"] = "风雷益",
        ["Guai"] = "泽天夬", ["Breakthrough"] = "泽天夬",
        ["Gou"] = "天风姤", ["Coming to Meet"] = "天风姤",
        ["Cui"] = "泽地萃", ["Gathering Together"] = "泽地萃",
        ["Sheng"] = "地风升", ["Pushing Upward"] = "地风升",
        ["Kun Oppression"] = "泽水困", ["Oppression"] = "泽水困",
        ["Jing"] = "水风井", ["The Well"] = "水风井",
        ["Ge"] = "泽火革", ["Revolution"] = "泽火革",
        ["Ding"] = "火风鼎", ["The Cauldron"] = "火风鼎",
        ["Zhen"] = "震为雷", ["The Arousing"] = "震为雷",
        ["Gen"] = "艮为山", ["Keeping Still"] = "艮为山",
        ["Jian Gradual"] = "风山渐", ["Development"] = "风山渐",
        ["Gui Mei"] = "雷泽归妹", ["The Marrying Maiden"] = "雷泽归妹",
        ["Feng"] = "雷火丰", ["Abundance"] = "雷火丰",
        ["Lu Wanderer"] = "火山旅", ["The Wanderer"] = "火山旅",
        ["Xun"] = "巽为风", ["The Gentle"] = "巽为风",
        ["Dui"] = "兑为泽", ["The Joyous"] = "兑为泽",
        ["Huan"] = "风水涣", ["Dispersion"] = "风水涣",
        ["Jie Limitation"] = "水泽节", ["Limitation"] = "水泽节",
        ["Zhong Fu"] = "风泽中孚", ["Inner Truth"] = "风泽中孚",
        ["Xiao Guo"] = "雷山小过", ["Small Preponderance"] = "雷山小过",
        ["Ji Ji"] = "水火既济", ["After Completion"] = "水火既济",
        ["Wei Ji"] = "火水未济", ["Before Completion"] = "火水未济"
    };
}
