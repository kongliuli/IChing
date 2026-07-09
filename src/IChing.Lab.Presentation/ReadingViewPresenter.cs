using System.Net;
using System.Text;
using System.Text.Json;
using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Presentation;

public static class ReadingViewPresenter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string ToDocument(ReadingViewModel vm, object? chartRef = null)
    {
        var markdown = SectionsToMarkdown(vm);
        if (string.Equals(vm.Domain, "tarot", StringComparison.OrdinalIgnoreCase) && chartRef is TarotReading tarot)
        {
            return ReadingHtmlFormatter.ToTarotDocument(vm.Title, vm.Subject, markdown, tarot);
        }

        var widgetHtml = string.Join("", vm.Widgets.Select(RenderWidget));
        var sections = ReadingHtmlFormatter.ToFragment(markdown);
        var body = widgetHtml + sections;
        return WrapDocument(vm.Title, vm.Subject, body, vm.Theme);
    }

    public static string SectionsToMarkdown(ReadingViewModel vm)
    {
        if (vm.Sections.Count == 0 && string.IsNullOrWhiteSpace(vm.Summary))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(vm.Summary))
        {
            sb.AppendLine("## 总结");
            sb.AppendLine(vm.Summary);
            sb.AppendLine();
        }

        foreach (var section in vm.Sections.Where(s => !string.Equals(s.Key, "overview", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(vm.Summary)))
        {
            sb.AppendLine($"## {section.Title}");
            sb.AppendLine(section.Body);
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    private static string RenderWidget(ReadingWidgetVm widget) =>
        widget.Kind switch
        {
            "pillarGrid" => RenderPillarGrid(widget.PayloadJson),
            "daYunTimeline" => RenderDaYun(widget.PayloadJson),
            "hexagramLines" => RenderHexagramLines(widget.PayloadJson),
            "spreadTable" => RenderSpreadFromPayload(widget.PayloadJson),
            "dimensionBars" => RenderDimensionBars(widget.PayloadJson),
            "typeCard" => RenderTypeCard(widget.PayloadJson),
            _ => string.Empty
        };

    private static string RenderPillarGrid(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var pillars = root.GetProperty("pillars").EnumerateArray()
            .Select(p => $"""<div class="pillar"><span>{H(p.GetProperty("label").GetString())}</span><b>{H(p.GetProperty("ganZhi").GetString())}</b></div>""")
            .ToList();
        var dayMaster = root.TryGetProperty("dayMaster", out var dm) ? dm.GetString() : "";
        return $"""<section class="widget pillars">{string.Join("", pillars)}<p>日主 {H(dayMaster)}</p></section>""";
    }

    private static string RenderDaYun(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items))
        {
            return string.Empty;
        }

        var text = string.Join(" · ", items.EnumerateArray().Select(i => H(i.GetString())));
        return $"""<section class="widget"><h2>大运概览</h2><p>{text}</p></section>""";
    }

    private static string RenderHexagramLines(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var title = root.GetProperty("original").GetString() ?? "";
        if (root.TryGetProperty("changed", out var changed) && changed.ValueKind == JsonValueKind.String)
        {
            var c = changed.GetString();
            if (!string.IsNullOrWhiteSpace(c))
            {
                title += $" · 变卦 {c}";
            }
        }

        var lines = root.GetProperty("lines").EnumerateArray()
            .Select(l =>
            {
                var yinYang = l.GetProperty("yinYang").GetString();
                var changing = l.TryGetProperty("isChanging", out var ch) && ch.GetBoolean() ? " 动" : "";
                return $"""<div class="line"><b>{H(l.GetProperty("position").GetString())} · {H(yinYang)}{changing}</b><span>{H($"{l.GetProperty("sixKin").GetString()} {l.GetProperty("stemBranch").GetString()}")}</span></div>""";
            });
        return $"""<section class="widget"><h2>{H(title)}</h2><div class="lines">{string.Join("", lines)}</div></section>""";
    }

    private static string RenderSpreadFromPayload(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var rows = doc.RootElement.GetProperty("positions").EnumerateArray()
            .Select(p =>
                $"""
                <tr>
                  <td>{H(p.GetProperty("PositionTitleZh").GetString())}</td>
                  <td>{H(p.GetProperty("CardNameZh").GetString())}</td>
                  <td>{(p.TryGetProperty("reversed", out var rev) && rev.GetBoolean() ? "逆位" : "正位")}</td>
                  <td>{H(p.GetProperty("Meaning").GetString())}</td>
                </tr>
                """);
        return $"""
        <section class="spread-table">
          <h2>牌位对照表</h2>
          <table><thead><tr><th>牌位</th><th>牌</th><th>方向</th><th>牌义</th></tr></thead><tbody>{string.Join("", rows)}</tbody></table>
        </section>
        """;
    }

    private static string RenderDimensionBars(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var rows = doc.RootElement.EnumerateArray()
            .Select(b =>
            {
                var left = b.GetProperty("LeftPercent").GetInt32();
                return $"""
                <div class="bar-row">
                  <span>{H(b.GetProperty("Title").GetString())}</span>
                  <div class="bar"><i style="width:{left}%"></i></div>
                  <small>{H(b.GetProperty("LeftLabel").GetString())} / {H(b.GetProperty("RightLabel").GetString())}</small>
                </div>
                """;
            });
        return $"""<section class="widget bars">{string.Join("", rows)}</section>""";
    }

    private static string RenderTypeCard(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return $"""
        <section class="widget type-card">
          <div class="code">{H(root.GetProperty("code").GetString())}</div>
          <h2>{H(root.GetProperty("title").GetString())}</h2>
          <p>{H(root.GetProperty("summary").GetString())}</p>
        </section>
        """;
    }

    private static string WrapDocument(string title, string subject, string body, string theme) =>
        $$"""
        <!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
        <style>
          body{margin:0;padding:20px;font-family:"Microsoft YaHei",sans-serif;background:#090711;color:#f7f1e8}
          body.light{background:#f7f3ea;color:#211b16}
          .widget{margin:0 0 14px;padding:14px;border:1px solid #49375f;border-radius:8px;background:#171122}
          body.light .widget{border-color:#ded3c1;background:#fffdf8}
          .pillars{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px}
          .pillar span{display:block;font-size:12px;color:#b9ad9e}
          .line{margin:0 0 8px}
          .bar-row{margin:0 0 10px}
          .bar{height:8px;background:#231935;border-radius:4px;overflow:hidden;margin:4px 0}
          .bar i{display:block;height:100%;background:#d8b757}
          .type-card .code{font-size:28px;font-weight:800;color:#d8b757}
        </style></head>
        <body class="{{(theme == "light" ? "light" : string.Empty)}}"><main><h1>{{H(title)}}</h1><p>{{H(subject)}}</p>{{body}}</main></body></html>
        """;

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
