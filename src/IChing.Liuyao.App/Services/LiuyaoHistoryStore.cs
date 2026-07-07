using System.Text.Json;
using IChing.Lab.Core.Liuyao;

namespace IChing.Liuyao.App.Services;

/// <summary>本地起卦历史（Preferences，最多 10 条）。</summary>
public sealed class LiuyaoHistoryStore
{
    private const string PrefKey = "liuyao_cast_history_v1";
    private const int MaxEntries = 10;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Add(LiuyaoHistoryEntry entry)
    {
        var list = Load();
        list.Insert(0, entry);
        if (list.Count > MaxEntries)
        {
            list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        }

        Preferences.Default.Set(PrefKey, JsonSerializer.Serialize(list, JsonOptions));
    }

    public IReadOnlyList<LiuyaoHistoryEntry> GetRecent() => Load();

    public void Clear() => Preferences.Default.Remove(PrefKey);

    private static List<LiuyaoHistoryEntry> Load()
    {
        var json = Preferences.Default.Get(PrefKey, string.Empty);
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<LiuyaoHistoryEntry>>(json, JsonOptions) ?? [];
    }
}

public sealed record LiuyaoHistoryEntry(
    DateTimeOffset At,
    string Method,
    DateTimeOffset CastAt,
    int? Seed,
    string? Question,
    string HexagramLine,
    string ChartJson)
{
    public string DisplayLine =>
        $"{At.LocalDateTime:MM-dd HH:mm} · {HexagramLine}" +
        (Question is { Length: > 0 } q ? $" · {Truncate(q, 16)}" : string.Empty);

    public LiuyaoNajiaResult? TryGetChart() =>
        string.IsNullOrWhiteSpace(ChartJson)
            ? null
            : JsonSerializer.Deserialize<LiuyaoNajiaResult>(ChartJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
