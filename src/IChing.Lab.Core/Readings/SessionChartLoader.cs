using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Readings;

public static class SessionChartLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static object? Deserialize(string domain, string chartJson) =>
        domain.ToLowerInvariant() switch
        {
            "bazi" => JsonSerializer.Deserialize<BaziChart>(chartJson, JsonOptions),
            "liuyao" => JsonSerializer.Deserialize<LiuyaoNajiaResult>(chartJson, JsonOptions),
            "tarot" => JsonSerializer.Deserialize<TarotReading>(chartJson, JsonOptions),
            _ => null
        };
}
