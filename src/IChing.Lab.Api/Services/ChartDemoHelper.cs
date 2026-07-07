using System.Text.Json;

namespace IChing.Lab.Api.Services;

internal static class ChartDemoHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Dictionary<string, object?> ToArgs<T>(T input)
    {
        var json = JsonSerializer.Serialize(input, JsonOptions);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json)
            ?? new Dictionary<string, object?>();
    }
}
