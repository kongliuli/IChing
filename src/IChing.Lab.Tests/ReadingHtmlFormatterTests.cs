using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Tests;

public class ReadingHtmlFormatterTests
{
    [Fact]
    public void ToFragment_FormatsAndEscapesTemplateMarkdown()
    {
        var html = ReadingHtmlFormatter.ToFragment("""
        ## 总结
        <script>alert(1)</script>

        - 第一条
        """);

        Assert.Contains("<h2>总结</h2>", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.Contains("<li>第一条</li>", html);
    }

    [Fact]
    public void ToFragment_ConsumesReadingOutputJson()
    {
        var html = ReadingHtmlFormatter.ToFragment("""
        {"summary":"总览","sections":[{"key":"overview","title":"整体能量","body":"正文"}],"warnings":[]}
        """);

        Assert.Contains("<h2>总结</h2>", html);
        Assert.Contains("总览", html);
        Assert.Contains("<h2>整体能量</h2>", html);
        Assert.DoesNotContain("\"sections\"", html);
    }

    [Fact]
    public void ToFragment_ConsumesLooseJsonBodies()
    {
        var html = ReadingHtmlFormatter.ToFragment("""
        {"summary":"ok","sections":[{"key":"actions","title":"Actions","body":["line one","line two"]}],"warnings":["watch"]}
        """);

        Assert.Contains("<h2>Actions</h2>", html);
        Assert.Contains("line one", html);
        Assert.Contains("line two", html);
        Assert.Contains("<li>watch</li>", html);
        Assert.DoesNotContain("\"body\"", html);
    }

    [Fact]
    public void ToFragment_HidesJsonLikeFieldNames()
    {
        var html = ReadingHtmlFormatter.ToFragment("""
        {
        "summary": "from past to future",
        "sections": [
        {
        "key": "overview",
        "title": "Overall",
        "body": "real content"
        },
        """);

        Assert.Contains("<h2>总结</h2>", html);
        Assert.Contains("<h2>整体能量</h2>", html);
        Assert.Contains("real content", html);
        Assert.DoesNotContain("\"key\"", html);
        Assert.DoesNotContain("\"title\"", html);
        Assert.DoesNotContain("\"body\"", html);
    }

    [Fact]
    public void ToFragment_LocalizesEnglishTarotHeadings()
    {
        var html = ReadingHtmlFormatter.ToFragment("""
        Spread
        three cards

        Stats
        major two

        Element tendency
        cups
        """);

        Assert.Contains("<h2>牌阵</h2>", html);
        Assert.Contains("<h2>统计</h2>", html);
        Assert.Contains("<h2>元素倾向</h2>", html);
        Assert.DoesNotContain("<h2>Spread</h2>", html);
        Assert.DoesNotContain("<h2>Stats</h2>", html);
    }

    [Fact]
    public void ToDocument_AddsSummaryAndToc()
    {
        var html = ReadingHtmlFormatter.ToDocument("报告", "三牌", """
        ## 总结
        第一段摘要。

        ## 行动建议
        - 先观察
        """);

        Assert.Contains("重点摘要", html);
        Assert.Contains("目录", html);
        Assert.Contains("href=\"#sec-1\"", html);
    }

    [Fact]
    public void ToTarotDocument_AddsSpreadTable()
    {
        var reading = TarotEngine.Draw("past-present-future", "career", 42);
        var html = ReadingHtmlFormatter.ToTarotDocument("报告", reading.SpreadTitleZh, "## 总结\n可以行动。", reading);

        Assert.Contains("牌位对照表", html);
        Assert.Contains(reading.Positions[0].PositionTitleZh, html);
        Assert.Contains(reading.Positions[0].CardNameZh, html);
    }
}
