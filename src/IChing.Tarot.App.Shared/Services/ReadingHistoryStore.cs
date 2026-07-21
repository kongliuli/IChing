using System.Text.Json;
using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

/// <summary>本地抽牌历史（Preferences，最多 10 条，含完整牌阵 JSON + 可选解读正文）。</summary>
public sealed class ReadingHistoryStore
{
    private const string PrefKey = "tarot_reading_history_v3";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxEntries = 10;

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
            reading.Positions.Count,
            JsonSerializer.Serialize(reading, JsonOptions),
            Interpretation: null));

        if (entries.Count > MaxEntries)
        {
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
        }

        Save(entries);
    }

    /// <summary>把最近一条（或匹配 seed 的条目）的解读正文回写。</summary>
    public void UpdateLatestInterpretation(string interpretation, int? seed = null)
    {
        if (string.IsNullOrWhiteSpace(interpretation))
        {
            return;
        }

        var entries = Load();
        if (entries.Count == 0)
        {
            return;
        }

        var index = 0;
        if (seed is not null)
        {
            var found = entries.FindIndex(e => e.Seed == seed);
            if (found >= 0)
            {
                index = found;
            }
        }

        var old = entries[index];
        entries[index] = old with { Interpretation = interpretation };
        Save(entries);
    }

    public IReadOnlyList<HistoryEntry> GetRecent() => Load();

    public HistoryEntry? GetAt(int index)
    {
        var entries = Load();
        return index >= 0 && index < entries.Count ? entries[index] : null;
    }

    public void Clear() => Preferences.Default.Remove(PrefKey);

    private static void Save(List<HistoryEntry> entries) =>
        Preferences.Default.Set(PrefKey, JsonSerializer.Serialize(entries, JsonOptions));

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
    int CardCount,
    string ReadingJson,
    string? Interpretation = null)
{
    public string DisplayLine =>
        $"{At.LocalDateTime:MM-dd HH:mm} · {SpreadTitle} · {CardCount}张" +
        (Question is { Length: > 0 } q ? $" · {Truncate(q, 20)}" : string.Empty) +
        (string.IsNullOrWhiteSpace(Interpretation) ? string.Empty : " · 有解读");

    public TarotReading? TryGetReading()
    {
        if (string.IsNullOrWhiteSpace(ReadingJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<TarotReading>(ReadingJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
