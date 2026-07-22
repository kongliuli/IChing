using System.Text.Json;

namespace IChing.App.Services;

public sealed class ReadingHistoryStore
{
    private const string PrefKey = "iching_reading_history_v1";
    private const int MaxEntries = 30;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Add(string domain, string title, string? question, string summary, string? interpretation)
    {
        var entries = Load();
        entries.Insert(0, new HistoryEntry(
            DateTimeOffset.Now,
            domain,
            title,
            question,
            summary,
            interpretation));
        if (entries.Count > MaxEntries)
        {
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
        }

        Preferences.Default.Set(PrefKey, JsonSerializer.Serialize(entries, JsonOptions));
    }

    public IReadOnlyList<HistoryEntry> GetRecent() => Load();

    public void Clear() => Preferences.Default.Remove(PrefKey);

    private static List<HistoryEntry> Load()
    {
        var json = Preferences.Default.Get(PrefKey, string.Empty);
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<HistoryEntry>>(json, JsonOptions) ?? [];
    }
}

public sealed record HistoryEntry(
    DateTimeOffset At,
    string Domain,
    string Title,
    string? Question,
    string Summary,
    string? Interpretation)
{
    public string DisplayLine =>
        $"{At.LocalDateTime:MM-dd HH:mm} · {Domain} · {Title}" +
        (string.IsNullOrWhiteSpace(Question) ? string.Empty : $" · {Question}");
}
