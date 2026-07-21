using System.Net;
using System.Text;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Presentation;

public static class ReadingHtmlFormatter
{
    public static string ToDocument(string title, string subject, string markdown, string theme = "tarot")
    {
        var sections = ParseSections(markdown);
        var body = BuildSummaryCard(sections) + BuildToc(sections) + RenderSections(sections);
        return Page(title, subject, body, theme);
    }

    public static string ToTarotDocument(string title, string subject, string markdown, TarotReading reading)
    {
        var sections = ParseSections(markdown);
        var body = BuildSummaryCard(sections) + BuildToc(sections) + BuildTarotSpreadTable(reading) + RenderSections(sections);
        return Page(title, subject, body, "tarot");
    }

    private static string Page(string title, string subject, string body, string theme) =>
        $$"""
        <!doctype html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <style>
            :root { --bg:#090711; --panel:#120d1c; --card:#171122; --card2:#231935; --ink:#f7f1e8; --muted:#b9ad9e; --soft:#82758e; --line:#49375f; --line2:#6d568b; --gold:#d8b757; --violet:#b794f6; --green:#7fd992; }
            body.light { --bg:#f7f3ea; --panel:#f3eadb; --card:#fffdf8; --card2:#fbf6ea; --ink:#211b16; --muted:#786f63; --soft:#8f8374; --line:#ded3c1; --line2:#c7b489; --gold:#a83a2d; --violet:#1f7668; --green:#1f7668; }
            * { box-sizing:border-box; }
            html { color-scheme:dark; }
            body { margin:0; padding:24px; background:var(--bg); color:var(--ink); font-family:"Microsoft YaHei","PingFang SC",system-ui,sans-serif; letter-spacing:0; }
            body::-webkit-scrollbar { width:10px; }
            body::-webkit-scrollbar-track { background:var(--panel); }
            body::-webkit-scrollbar-thumb { background:var(--line2); border-radius:8px; border:2px solid var(--panel); }
            main { max-width:860px; margin:0 auto; }
            .meta { margin:0 0 10px; color:var(--soft); font-size:12px; }
            .hero,.section { border:1px solid var(--line); border-radius:8px; background:var(--card); box-shadow:0 16px 38px rgba(0,0,0,.22); }
            .hero { position:relative; overflow:hidden; padding:20px 22px; margin-bottom:14px; background:linear-gradient(135deg,var(--card),var(--card2)); }
            .hero:before { content:""; position:absolute; inset:0 auto 0 0; width:4px; background:var(--gold); }
            .hero h1 { margin:0 0 6px; font-size:24px; line-height:1.2; color:var(--gold); }
            .hero p { margin:0; color:var(--muted); font-size:13px; }
            .summary-card { display:grid; grid-template-columns:auto 1fr; gap:14px; align-items:start; padding:16px; margin-bottom:12px; border:1px solid var(--line2); border-radius:8px; background:linear-gradient(135deg,rgba(216,183,87,.14),rgba(183,148,246,.1)); }
            .summary-mark { width:36px; height:36px; border-radius:8px; display:grid; place-items:center; color:var(--bg); background:var(--gold); font-weight:800; }
            .summary-card b { display:block; margin:0 0 5px; color:var(--gold); font-size:13px; }
            .summary-card p { color:var(--ink); }
            .toc { padding:14px 16px; margin-bottom:12px; border:1px solid var(--line); border-radius:8px; background:var(--panel); }
            .toc-title { margin:0 0 10px; color:var(--muted); font-size:12px; }
            .toc-grid { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:8px; }
            .toc a,.toc button.toc-link { display:block; width:100%; min-width:0; padding:8px 10px; border:1px solid var(--line); border-radius:8px; color:var(--ink); text-decoration:none; font:inherit; font-size:13px; text-align:left; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; background:rgba(255,255,255,.025); cursor:pointer; }
            .spread-table { margin-bottom:12px; overflow:hidden; border:1px solid var(--line); border-radius:8px; background:var(--card); }
            .spread-table h2 { margin:0; padding:14px 16px 10px; font-size:17px; }
            table { width:100%; border-collapse:collapse; }
            th,td { padding:11px 12px; border-top:1px solid var(--line); text-align:left; vertical-align:top; font-size:13px; line-height:1.5; }
            th { color:var(--muted); font-weight:600; background:rgba(255,255,255,.025); }
            td:first-child { color:var(--gold); font-weight:700; white-space:nowrap; }
            .badge { display:inline-block; padding:2px 7px; border:1px solid var(--line2); border-radius:8px; color:var(--violet); font-size:12px; }
            .section { position:relative; padding:18px 18px 18px 20px; margin:0 0 12px; background:linear-gradient(180deg,var(--card),rgba(23,17,34,.92)); }
            .section:before { content:""; position:absolute; left:0; top:16px; bottom:16px; width:3px; border-radius:0 8px 8px 0; background:var(--violet); }
            h2,h3 { margin:0 0 11px; line-height:1.25; font-weight:700; }
            h2 { font-size:18px; color:var(--gold); }
            h3 { font-size:15px; color:var(--violet); }
            p,li { font-size:14px; line-height:1.82; }
            p { margin:0 0 11px; white-space:pre-wrap; }
            p:last-child { margin-bottom:0; }
            ul { margin:0 0 10px 0; padding:0; color:var(--ink); list-style:none; }
            li { position:relative; margin:0 0 8px; padding-left:18px; }
            li:before { content:""; position:absolute; left:2px; top:.75em; width:6px; height:6px; border-radius:50%; background:var(--green); }
            @media (max-width:640px) {
              body { padding:14px; }
              .hero { padding:18px; }
              .summary-card { grid-template-columns:1fr; }
              .toc-grid { grid-template-columns:1fr; }
              th:nth-child(4),td:nth-child(4) { display:none; }
              .section { padding:16px 15px 16px 17px; }
              .hero h1 { font-size:22px; }
              h2 { font-size:17px; }
              p,li { font-size:13px; line-height:1.76; }
            }
          </style>
        </head>
        <body class="{{(theme == "light" ? "light" : string.Empty)}}">
          <main>
            <p class="meta">{{H(subject)}}</p>
            <section class="hero"><h1>{{H(title)}}</h1><p>{{H(subject)}}</p></section>
            {{body}}
          </main>
          <script>
            // WinUI WebView 会把 hash 锚点解析成 https://appdir/… 并 ERR_ACCESS_DENIED；改用页内滚动
            document.querySelectorAll('button.toc-link[data-sec]').forEach(function (btn) {
              btn.addEventListener('click', function () {
                var el = document.getElementById(btn.getAttribute('data-sec'));
                if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
              });
            });
          </script>
        </body>
        </html>
        """;

    public static string ToFragment(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        return RenderSections(ParseSections(markdown));
    }

    private static IReadOnlyList<ReportSection> ParseSections(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }

        var sections = new List<ReportSection>();
        var title = string.Empty;
        var body = new List<string>();
        var sub = false;

        void Flush()
        {
            var text = string.Join('\n', body).Trim();
            if (text.Length == 0)
            {
                return;
            }

            sections.Add(new ReportSection(
                ReadingPromptProtocol.LocalizeTitle(string.IsNullOrWhiteSpace(title) ? "解读" : title),
                text,
                sub));
            title = string.Empty;
            body.Clear();
            sub = false;
        }

        var text = ReadingPromptProtocol.NormalizeOutput(markdown);
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                body.Add(string.Empty);
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal) || line.StartsWith("### ", StringComparison.Ordinal))
            {
                var h3 = line.StartsWith("### ", StringComparison.Ordinal);
                Flush();
                title = line[(h3 ? 4 : 3)..].Trim();
                sub = h3;
                continue;
            }

            if (ReadingPromptProtocol.IsKnownEnglishTitle(line))
            {
                Flush();
                title = line;
                continue;
            }

            body.Add(line);
        }

        Flush();
        return sections;
    }

    private static string BuildSummaryCard(IReadOnlyList<ReportSection> sections)
    {
        var picked = sections.FirstOrDefault(s => s.Title.Contains("总结", StringComparison.Ordinal) || s.Title.Contains("整体", StringComparison.Ordinal))
                     ?? sections.FirstOrDefault();
        if (picked is null)
        {
            return string.Empty;
        }

        return $$"""
        <section class="summary-card">
          <div class="summary-mark">要</div>
          <div><b>重点摘要</b><p>{{H(FirstParagraph(picked.Body, 180))}}</p></div>
        </section>
        """;
    }

    private static string BuildToc(IReadOnlyList<ReportSection> sections)
    {
        if (sections.Count < 2)
        {
            return string.Empty;
        }

        var links = string.Join("", sections.Select((s, i) =>
            $"""<button type="button" class="toc-link" data-sec="sec-{i + 1}">{H(s.Title)}</button>"""));
        return $$"""
        <nav class="toc">
          <p class="toc-title">目录</p>
          <div class="toc-grid">{{links}}</div>
        </nav>
        """;
    }

    private static string BuildTarotSpreadTable(TarotReading reading)
    {
        var rows = string.Join("", reading.Positions.Select(p =>
            $"""
            <tr>
              <td>{H(p.PositionTitleZh)}</td>
              <td>{H(p.CardNameZh)}</td>
              <td><span class="badge">{(p.Reversed ? "逆位" : "正位")}</span></td>
              <td>{H(p.Meaning)}</td>
            </tr>
            """));

        return $$"""
        <section class="spread-table">
          <h2>牌位对照表</h2>
          <table>
            <thead><tr><th>牌位</th><th>牌</th><th>方向</th><th>本地牌义</th></tr></thead>
            <tbody>{{rows}}</tbody>
          </table>
        </section>
        """;
    }

    private static string RenderSections(IReadOnlyList<ReportSection> sections) =>
        string.Join("", sections.Select((s, i) =>
        {
            var tag = s.IsSubsection ? "h3" : "h2";
            return $"""<section class="section" id="sec-{i + 1}"><{tag}>{H(s.Title)}</{tag}>{RenderBody(s.Body)}</section>""";
        }));

    private static string RenderBody(string body)
    {
        var html = new StringBuilder();
        var paragraph = new List<string>();
        var listOpen = false;

        void FlushParagraph()
        {
            if (paragraph.Count == 0)
            {
                return;
            }

            html.Append("<p>");
            html.Append(H(string.Join("\n", paragraph).Trim()));
            html.AppendLine("</p>");
            paragraph.Clear();
        }

        void CloseList()
        {
            if (!listOpen)
            {
                return;
            }

            html.AppendLine("</ul>");
            listOpen = false;
        }

        foreach (var raw in body.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                FlushParagraph();
                CloseList();
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                FlushParagraph();
                if (!listOpen)
                {
                    html.AppendLine("<ul>");
                    listOpen = true;
                }

                html.Append("<li>");
                html.Append(H(line[2..].Trim()));
                html.AppendLine("</li>");
                continue;
            }

            paragraph.Add(line);
        }

        FlushParagraph();
        CloseList();
        return html.ToString();
    }

    private static string FirstParagraph(string body, int max)
    {
        var text = body.Replace("- ", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? body.Trim();
        return text.Length <= max ? text : text[..max].TrimEnd() + "...";
    }

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private sealed record ReportSection(string Title, string Body, bool IsSubsection);
}
