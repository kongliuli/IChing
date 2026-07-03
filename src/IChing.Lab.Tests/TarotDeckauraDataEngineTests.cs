using IChing.Lab.Abstractions.Models;
using IChing.Lab.Engines.Tarot;
using System.Text.Json;

namespace IChing.Lab.Tests;

/// <summary>
/// <see cref="TarotDeckauraDataEngine"/> 单元测试：验证 Deckaura 78 牌数据集完整性与单牌查询行为。
/// </summary>
public class TarotDeckauraDataEngineTests
{
    /// <summary>Deckaura 静态数据集应包含 78 张牌（22 大阿卡那 + 56 小阿卡那）。</summary>
    [Fact]
    public void Cards_Has78Entries()
    {
        Assert.Equal(78, TarotDeckData.Cards.Count);
    }

    /// <summary>22 大阿卡那应完整存在（愚者~世界，编号 0~21）。</summary>
    [Fact]
    public void Cards_MajorArcana_Has22Entries()
    {
        var major = TarotDeckData.Cards.Where(c => c.Arcana == "Major").ToList();
        Assert.Equal(22, major.Count);
        // 编号 0~21 应全部存在且唯一。
        var numbers = major.Select(c => c.Number).OrderBy(n => int.Parse(n)).ToList();
        Assert.Equal(Enumerable.Range(0, 22).Select(n => n.ToString()).ToList(), numbers);
    }

    /// <summary>56 小阿卡那应完整（四花色各 14 张）。</summary>
    [Fact]
    public void Cards_MinorArcana_Has56EntriesAcrossFourSuits()
    {
        var minor = TarotDeckData.Cards.Where(c => c.Arcana == "Minor").ToList();
        Assert.Equal(56, minor.Count);

        foreach (var suit in new[] { "Wands", "Cups", "Swords", "Pentacles" })
        {
            var suitCards = minor.Where(c => c.Suit == suit).ToList();
            Assert.Equal(14, suitCards.Count);
            // 每花色应包含 Ace + 2~10 + Page/Knight/Queen/King。
            var expectedNumbers = new[] { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Page", "Knight", "Queen", "King" };
            Assert.Equal(expectedNumbers, suitCards.Select(c => c.Number).ToArray());
        }
    }

    /// <summary>查询 "The Fool" 应返回 upright 含 "New beginnings"、yesNo="yes"。</summary>
    [Fact]
    public void Calculate_TheFool_ReturnsUprightWithNewBeginningsAndYes()
    {
        var engine = new TarotDeckauraDataEngine();
        var request = new ChartRequest("tarot", new Dictionary<string, object?>
        {
            ["cardName"] = "The Fool"
        });

        var result = engine.Calculate(request);

        // 反序列化匿名对象为 JSON 再校验字段（Calculate 返回匿名对象）。
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("card", out var card));
        Assert.True(card.TryGetProperty("upright", out var upright));
        Assert.Contains("New beginnings", upright.GetString());

        Assert.True(card.TryGetProperty("yesNo", out var yesNo));
        Assert.Equal("yes", yesNo.GetString());

        Assert.True(card.TryGetProperty("name", out var name));
        Assert.Equal("The Fool", name.GetString());

        Assert.True(card.TryGetProperty("number", out var number));
        Assert.Equal("0", number.GetString());
    }

    /// <summary>不区分大小写查询 "the fool" 也应命中。</summary>
    [Fact]
    public void Calculate_CardNameCaseInsensitive_ReturnsCard()
    {
        var engine = new TarotDeckauraDataEngine();
        var request = new ChartRequest("tarot", new Dictionary<string, object?>
        {
            ["cardName"] = "the fool"
        });

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("card", out var card));
        Assert.True(card.TryGetProperty("name", out var name));
        Assert.Equal("The Fool", name.GetString());
    }

    /// <summary>未提供 cardName 时返回 78 牌索引。</summary>
    [Fact]
    public void Calculate_NoCardName_ReturnsAllCardsIndex()
    {
        var engine = new TarotDeckauraDataEngine();
        var request = new ChartRequest("tarot", new Dictionary<string, object?>());

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("total", out var total));
        Assert.Equal(78, total.GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("cards", out var cards));
        Assert.Equal(78, cards.GetArrayLength());
    }

    /// <summary>查询不存在的牌应返回 error="card not found"。</summary>
    [Fact]
    public void Calculate_UnknownCard_ReturnsError()
    {
        var engine = new TarotDeckauraDataEngine();
        var request = new ChartRequest("tarot", new Dictionary<string, object?>
        {
            ["cardName"] = "The Nonexistent Card"
        });

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Equal("card not found", error.GetString());
    }

    /// <summary>引擎元数据应符合任务规格（Source/Version/TemplateHint/ModuleFocus）。</summary>
    [Fact]
    public void Metadata_MatchesSpec()
    {
        var engine = new TarotDeckauraDataEngine();

        Assert.Equal("tarot-deckaura-data", engine.EngineId);
        Assert.Equal("tarot", engine.Domain);
        Assert.Equal("tarot-card-meanings(Deckaura)", engine.Metadata.Source);
        Assert.Equal("1.0.2", engine.Metadata.Version);
        Assert.Equal("deckaura", engine.Metadata.TemplateHint);
        Assert.Equal(new[] { "paiyi", "12wei" }, engine.Metadata.ModuleFocus);
        Assert.Contains("DOI 10.5281/zenodo.19152918", engine.Metadata.AlgorithmBasis);
    }

    /// <summary>所有 78 张牌的 12 维字段应非空。</summary>
    [Theory]
    [MemberData(nameof(AllCardNames))]
    public void EveryCard_HasAllTwelveDimensionsPopulated(string cardName)
    {
        var card = TarotDeckData.FindByName(cardName);
        Assert.NotNull(card);
        Assert.False(string.IsNullOrWhiteSpace(card!.Number));
        Assert.False(string.IsNullOrWhiteSpace(card.Name));
        Assert.False(string.IsNullOrWhiteSpace(card.Arcana));
        Assert.False(string.IsNullOrWhiteSpace(card.Suit));
        Assert.False(string.IsNullOrWhiteSpace(card.Element));
        Assert.False(string.IsNullOrWhiteSpace(card.Planet));
        Assert.False(string.IsNullOrWhiteSpace(card.Upright));
        Assert.False(string.IsNullOrWhiteSpace(card.Reversed));
        Assert.False(string.IsNullOrWhiteSpace(card.Love));
        Assert.False(string.IsNullOrWhiteSpace(card.Career));
        Assert.False(string.IsNullOrWhiteSpace(card.YesNo));
        Assert.False(string.IsNullOrWhiteSpace(card.Keywords));
    }

    /// <summary>yesNo 字段应仅取 yes / no / unknown 三值之一。</summary>
    [Theory]
    [MemberData(nameof(AllCardNames))]
    public void EveryCard_YesNo_IsValidEnum(string cardName)
    {
        var card = TarotDeckData.FindByName(cardName);
        Assert.NotNull(card);
        Assert.Contains(card!.YesNo, new[] { "yes", "no", "unknown" });
    }

    /// <summary>78 张牌名应唯一（无重复）。</summary>
    [Fact]
    public void AllCardNames_AreUnique()
    {
        var names = TarotDeckData.Cards.Select(c => c.Name).ToList();
        Assert.Equal(78, names.Count);
        Assert.Equal(78, names.Distinct().Count());
    }

    public static IEnumerable<object[]> AllCardNames()
        => TarotDeckData.Cards.Select(c => new object[] { c.Name });
}
