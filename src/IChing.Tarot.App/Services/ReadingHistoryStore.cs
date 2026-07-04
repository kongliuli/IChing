using System.Text.Json;
using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

/// <summary>本地抽牌历史（Preferences，最多 10 条）。</summary>
public sealed class ReadingHistoryStore
{
    private const string PrefKey = "tarot_reading_history_v2";
    private const int MaxEntries = 10;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Add(TarotReading reading, string engineId)
    {
        var entries = Load();
        entries.Insert(0, new HistoryEntry(
            DateTimeOffset.Now,
            reading.SpreadId,
            reading.SpreadTitleZh,
            reading.Question,
            reading.Seed,
            engineId,
            reading.Positions.Count));

        if (entries.Count > MaxEntries)
        {
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
        }

        Preferences.Default.Set(PrefKey, JsonSerializer.Serialize(entries, JsonOptions));
    }

    public IReadOnlyList<HistoryEntry> GetRecent() => Load();

    private static List<HistoryEntry> Load()
    {
        var json = Preferences.Default.Get(PrefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<HistoryEntry>>(json, JsonOptions) ?? [];
    }
}

public sealed record HistoryEntry(
    DateTimeOffset At,
    string SpreadId,
    string SpreadTitle,
    string? Question,
    int? Seed,
    string EngineId,
    int CardCount)
{
    public string DisplayLine =>
        $"{At.LocalDateTime:MM-dd HH:mm} · {SpreadTitle} · {CardCount}张" +
        (Question is { Length: > 0 } q ? $" · {Truncate(q, 20)}" : string.Empty);

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
