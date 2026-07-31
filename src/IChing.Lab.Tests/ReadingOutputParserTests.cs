using IChing.Lab.Core.Readings;

namespace IChing.Lab.Tests;

public class ReadingOutputParserTests
{
    [Fact]
    public void TryParseStructured_ParsesV2Json()
    {
        const string json = """
            {
              "schema": "reading-output.v2",
              "summary": "总论一句",
              "sections": [
                { "key": "overview", "title": "总论", "body": "正文" }
              ],
              "warnings": []
            }
            """;

        var output = ReadingOutputParser.TryParseStructured(json, "bazi");
        Assert.NotNull(output);
        Assert.Equal("总论一句", output!.Summary);
        Assert.Single(output.Sections);
        Assert.Equal("overview", output.Sections[0].Key);
    }

    [Fact]
    public void TryParseStructured_UnknownKey_AddsWarning()
    {
        const string json = """
            {
              "schema": "reading-output.v2",
              "summary": "x",
              "sections": [
                { "key": "not_a_real_key", "title": "?", "body": "?" }
              ],
              "warnings": []
            }
            """;

        var output = ReadingOutputParser.TryParseStructured(json, "bazi");
        Assert.NotNull(output);
        Assert.Contains(output!.Warnings, w => w.StartsWith("unknown_section_key:", StringComparison.Ordinal));
    }

    [Fact]
    public void TryParseStructured_Tarot_AllowsDynamicPositionKeys()
    {
        const string json = """
            {
              "schema": "reading-output.v2",
              "summary": "三牌阵总览",
              "sections": [
                { "key": "past", "title": "过去", "body": "过去牌解读" },
                { "key": "present", "title": "现在", "body": "现在牌解读" },
                { "key": "future", "title": "未来", "body": "未来牌解读" },
                { "key": "overview", "title": "总论", "body": "总论正文" }
              ],
              "warnings": []
            }
            """;

        var output = ReadingOutputParser.TryParseStructured(json, "tarot");
        Assert.NotNull(output);
        Assert.DoesNotContain(output!.Warnings, w => w.StartsWith("unknown_section_key:", StringComparison.Ordinal));
        Assert.DoesNotContain(output.Warnings, w => w == "unknown_section_key:past");
        Assert.Equal(4, output.Sections.Count);
    }

    [Fact]
    public void TryParseStructured_Bazi_UnknownKey_StillWarns()
    {
        const string json = """
            {
              "schema": "reading-output.v2",
              "summary": "x",
              "sections": [
                { "key": "foo", "title": "?", "body": "?" }
              ],
              "warnings": []
            }
            """;

        var output = ReadingOutputParser.TryParseStructured(json, "bazi");
        Assert.NotNull(output);
        Assert.Contains(output!.Warnings, w => w == "unknown_section_key:foo");
    }

    [Fact]
    public void BuildExchangeOutput_FallbackPlainText()
    {
        var ex = ReadingOutputParser.BuildExchangeOutput("liuyao", "纯文本解读", null, "test-engine", isFallback: true);
        Assert.NotNull(ex.Structured);
        Assert.Equal("纯文本解读", ex.Structured!.Summary);
        Assert.True(ex.IsFallback);
    }
}
