using System.Text.Json;
using IChing.Tarot.App.Models;

namespace IChing.Tarot.App.Services;

/// <summary>探索页模块配置：优先 AppData 覆盖，其次内置 JSON。</summary>
public static class ExploreModuleCatalog
{
    private const string AssetPath = "explore-modules.json";
    private const string OverrideFileName = "explore-modules.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static ExploreModulesConfig? _cached;
    private static DateTime _overrideWriteUtc;

    public static async Task<ExploreModulesConfig> LoadAsync(CancellationToken ct = default)
    {
        var overridePath = Path.Combine(FileSystem.AppDataDirectory, OverrideFileName);
        if (File.Exists(overridePath))
        {
            var write = File.GetLastWriteTimeUtc(overridePath);
            if (_cached is null || write != _overrideWriteUtc)
            {
                await using var overrideStream = File.OpenRead(overridePath);
                _cached = await JsonSerializer.DeserializeAsync<ExploreModulesConfig>(overrideStream, JsonOptions, ct)
                          ?? CreateDefault();
                _overrideWriteUtc = write;
            }

            return _cached;
        }

        if (_cached is not null)
        {
            return _cached;
        }

        await using var assetStream = await FileSystem.OpenAppPackageFileAsync(AssetPath);
        _cached = await JsonSerializer.DeserializeAsync<ExploreModulesConfig>(assetStream, JsonOptions, ct)
                  ?? CreateDefault();
        return _cached;
    }

    public static void InvalidateCache() => _cached = null;

    public static ExploreModulesConfig CreateDefault() =>
        new()
        {
            HeaderTitle = "趣味探索",
            HeaderSubtitle = "轻量人格小工具，与正式占卜相互独立，仅供娱乐参考。",
            Sections =
            [
                new ExploreSectionConfig
                {
                    Id = "personality",
                    Title = "人格测评",
                    Items =
                    [
                        Item("mbti-16", "十六型人格", "personality-quiz", "mbti-16", "开始测试 →"),
                        Item("enneagram-9", "九型人格", "personality-quiz", "enneagram-9", "开始测试 →"),
                        Item("holland-riasec", "霍兰德职业兴趣", "personality-quiz", "holland-riasec", "开始测试 →")
                    ]
                },
                new ExploreSectionConfig
                {
                    Id = "tarot-fun",
                    Title = "塔罗趣味",
                    Items =
                    [
                        Item("spirit-card", "牌灵对应", "spirit-card", null, "开始探索 →"),
                        Item("element-quiz", "四元素倾向", "element-quiz", null, "开始测试 →"),
                        Item("daily-color", "今日能量色", "daily-color", null, "查看今日 →")
                    ]
                }
            ]
        };

    private static ExploreModuleItemConfig Item(
        string id, string title, string action, string? param, string button) =>
        new()
        {
            Id = id,
            Title = title,
            Action = action,
            ActionParam = param,
            ButtonText = button
        };
}
