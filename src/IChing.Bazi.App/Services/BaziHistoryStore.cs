using System.Text.Json;
using IChing.Lab.Core.Bazi;

namespace IChing.Bazi.App.Services;

/// <summary>本地排盘历史（Preferences，最多 10 条）。</summary>
public sealed class BaziHistoryStore
{
    private const string PrefKey = "bazi_chart_history_v1";
    private const int MaxEntries = 10;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Add(BaziHistoryEntry entry)
    {
        var list = Load();
        list.Insert(0, entry);
        if (list.Count > MaxEntries)
        {
            list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        }

        Preferences.Default.Set(PrefKey, JsonSerializer.Serialize(list, JsonOptions));
    }

    public IReadOnlyList<BaziHistoryEntry> GetRecent() => Load();

    public void Clear() => Preferences.Default.Remove(PrefKey);

    private static List<BaziHistoryEntry> Load()
    {
        var json = Preferences.Default.Get(PrefKey, string.Empty);
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<BaziHistoryEntry>>(json, JsonOptions) ?? [];
    }
}

public sealed record BaziHistoryEntry(
    DateTimeOffset At,
    int Year, int Month, int Day, int Hour, int Minute,
    int? Gender,
    int? FlowYear,
    string? Focus,
    string PillarsLine,
    string ChartJson)
{
    public string DisplayLine =>
        $"{At.LocalDateTime:MM-dd HH:mm} · {PillarsLine}" +
        (FlowYear is int y ? $" · {y}流年" : string.Empty);

    public BaziChart? TryGetChart() =>
        string.IsNullOrWhiteSpace(ChartJson)
            ? null
            : JsonSerializer.Deserialize<BaziChart>(ChartJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
}
