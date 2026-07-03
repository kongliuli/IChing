using System.Collections;
using System.Text.Json;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Engines.Liuyao;

namespace IChing.Lab.Tests;

/// <summary>
/// <see cref="LiuyaoYijingframeworkAnnotEngine"/> 单元测试。
/// 验证基于 YiJingFramework.Annotating 5.0.1 的《周易》爻辞注解引擎在常见输入下返回真实非空爻辞。
/// </summary>
public class LiuyaoYijingframeworkAnnotEngineTests
{
    /// <summary>引擎应正确声明 liuyao 域与 yijingframework-annot 标识。</summary>
    [Fact]
    public void Metadata_DeclaresLiuyaoDomainAndYijingframeworkSource()
    {
        var engine = new LiuyaoYijingframeworkAnnotEngine();

        Assert.Equal("liuyao", engine.Domain);
        Assert.Equal("liuyao-yijingframework-annot", engine.EngineId);
        Assert.Equal("YiJingFramework.Annotating", engine.Metadata.Source);
        Assert.Equal("5.0.1", engine.Metadata.Version);
        Assert.Equal("yijingframework", engine.Metadata.TemplateHint);
    }

    /// <summary>输入 hexagramName="乾为天" 应返回非空爻辞列表（含初九/九二/.../上九/用九）。</summary>
    [Fact]
    public void Calculate_HexagramNameQian_ReturnsNonEmptyYaoci()
    {
        var engine = new LiuyaoYijingframeworkAnnotEngine();

        var request = new ChartRequest("liuyao", new Dictionary<string, object?>
        {
            ["hexagramName"] = "乾为天"
        });

        var result = engine.Calculate(request);

        // 用 JsonElement 反射访问动态对象的字段，避免引入额外类型。
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("乾为天", doc.RootElement.GetProperty("hexagram").GetString());
        Assert.Equal("元亨利贞", doc.RootElement.GetProperty("guaci").GetString());

        var yaoci = doc.RootElement.GetProperty("yaoci");
        Assert.Equal(JsonValueKind.Array, yaoci.ValueKind);
        Assert.True(yaoci.GetArrayLength() >= 6, "乾为天应至少返回 6 条爻辞");

        // 抽取初九爻辞文本，应匹配《周易》原文 "潜龙勿用"
        var firstYao = yaoci[0];
        Assert.Equal("初九", firstYao.GetProperty("yao").GetString());
        Assert.Equal("潜龙勿用", firstYao.GetProperty("text").GetString());
    }

    /// <summary>输入 hexagramName="坤为地" 应返回非空爻辞列表，含 "履霜，坚冰至"。</summary>
    [Fact]
    public void Calculate_HexagramNameKun_ReturnsNonEmptyYaoci()
    {
        var engine = new LiuyaoYijingframeworkAnnotEngine();

        var request = new ChartRequest("liuyao", new Dictionary<string, object?>
        {
            ["hexagramName"] = "坤为地"
        });

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("坤为地", doc.RootElement.GetProperty("hexagram").GetString());
        var yaoci = doc.RootElement.GetProperty("yaoci");
        Assert.True(yaoci.GetArrayLength() >= 6);

        var firstYao = yaoci[0];
        Assert.Equal("初六", firstYao.GetProperty("yao").GetString());
        Assert.Equal("履霜，坚冰至", firstYao.GetProperty("text").GetString());
    }

    /// <summary>其余 62 卦（如 "水雷屯"）仅返回卦辞，yaoci 为空数组（任务约定）。</summary>
    [Fact]
    public void Calculate_HexagramNameZhun_ReturnsGuaCiOnly()
    {
        var engine = new LiuyaoYijingframeworkAnnotEngine();

        var request = new ChartRequest("liuyao", new Dictionary<string, object?>
        {
            ["hexagramName"] = "水雷屯"
        });

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("水雷屯", doc.RootElement.GetProperty("hexagram").GetString());
        Assert.Equal("元亨利贞，勿用有攸往，利建侯", doc.RootElement.GetProperty("guaci").GetString());
        var yaoci = doc.RootElement.GetProperty("yaoci");
        Assert.Equal(0, yaoci.GetArrayLength());
    }

    /// <summary>输入 yaos="初九"（无 hexagramName）应在全 store 范围内匹配到乾为天的初九爻。</summary>
    [Fact]
    public void Calculate_YaoOnly_ReturnsMatchingEntry()
    {
        var engine = new LiuyaoYijingframeworkAnnotEngine();

        var request = new ChartRequest("liuyao", new Dictionary<string, object?>
        {
            ["yaos"] = "初九"
        });

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        var yaoci = doc.RootElement.GetProperty("yaoci");
        Assert.Equal(JsonValueKind.Array, yaoci.ValueKind);
        Assert.True(yaoci.GetArrayLength() >= 1);

        var first = yaoci[0];
        Assert.Equal("乾为天", first.GetProperty("hexagram").GetString());
        Assert.Equal("初九", first.GetProperty("yao").GetString());
        Assert.Equal("潜龙勿用", first.GetProperty("text").GetString());
    }

    /// <summary>同时提供 hexagramName 与 yaos（数组），yaos 应限定在该卦内查询。</summary>
    [Fact]
    public void Calculate_HexagramAndYaosArray_ScopesToHexagram()
    {
        var engine = new LiuyaoYijingframeworkAnnotEngine();

        var yaosList = new List<object> { "九五", "上九" };
        var request = new ChartRequest("liuyao", new Dictionary<string, object?>
        {
            ["hexagramName"] = "乾为天",
            ["yaos"] = yaosList
        });

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        var yaoci = doc.RootElement.GetProperty("yaoci");
        Assert.Equal(2, yaoci.GetArrayLength());
        Assert.Equal("九五", yaoci[0].GetProperty("yao").GetString());
        Assert.Equal("飞龙在天，利见大人", yaoci[0].GetProperty("text").GetString());
        Assert.Equal("上九", yaoci[1].GetProperty("yao").GetString());
        Assert.Equal("亢龙有悔", yaoci[1].GetProperty("text").GetString());
    }

    /// <summary>未提供任何参数应返回 error 对象，不抛异常。</summary>
    [Fact]
    public void Calculate_NoArgs_ReturnsErrorObject()
    {
        var engine = new LiuyaoYijingframeworkAnnotEngine();

        var request = new ChartRequest("liuyao", new Dictionary<string, object?>());

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    /// <summary>提供不存在的卦名应返回 error 对象，不抛异常。</summary>
    [Fact]
    public void Calculate_UnknownHexagram_ReturnsErrorObject()
    {
        var engine = new LiuyaoYijingframeworkAnnotEngine();

        var request = new ChartRequest("liuyao", new Dictionary<string, object?>
        {
            ["hexagramName"] = "不存在的卦"
        });

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    /// <summary>IEnumerable（如 ArrayList）作为 yaos 应被正确展开为字符串列表。</summary>
    [Fact]
    public void Calculate_YaosAsIEnumerable_ExpandsToList()
    {
        var engine = new LiuyaoYijingframeworkAnnotEngine();

        var yaosEnumerable = (IEnumerable)new ArrayList { "用九", "用六" };
        var request = new ChartRequest("liuyao", new Dictionary<string, object?>
        {
            ["yaos"] = yaosEnumerable
        });

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        var yaoci = doc.RootElement.GetProperty("yaoci");
        Assert.Equal(2, yaoci.GetArrayLength());
        Assert.Equal("用九", yaoci[0].GetProperty("yao").GetString());
        Assert.Equal("见群龙无首，吉", yaoci[0].GetProperty("text").GetString());
    }
}
