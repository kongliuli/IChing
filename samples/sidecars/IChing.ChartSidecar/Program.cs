using System.Text.Json;
using IChing.Lab.Core.Engines;
using IChing.Lab.Core.Services;
using IChing.Lab.Engines.Tarot;

var preset = ParsePreset(args);
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    foreach (var port in preset.Ports)
    {
        options.ListenLocalhost(port);
    }
});

var app = builder.Build();
var chartRouter = new ChartEngineRouter([
    new BaziChartEngine(),
    new LiuyaoChartEngine(),
    new TarotChartEngine(),
    new CalendarEngine()
]);
var tarotChain = new[] { "iching-tarot-built-in" };

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    sidecar = "iching-chart-sidecar",
    preset = preset.Name,
    ports = preset.Ports,
    routes = new[] { "POST /bazi", "POST /liuyao", "POST /tarot", "POST /calendar" },
    tarotPipeline = "TarotDrawPipeline",
    engines = chartRouter.All.Select(e => new { e.Domain, e.EngineId }).ToArray()
}));

app.MapPost("/bazi", async (HttpRequest request) =>
{
    var body = await request.ReadFromJsonAsync<SidecarBody>();
    if (body?.Args is null)
    {
        return Results.BadRequest(new { error = "missing args" });
    }

    var routed = chartRouter.Calculate("bazi", body.Args, ["lunar-csharp-1.6.8"]);
    return Results.Json(routed.Result, SidecarJson.Web);
});

app.MapPost("/liuyao", async (HttpRequest request) =>
{
    var body = await request.ReadFromJsonAsync<SidecarBody>();
    if (body?.Args is null)
    {
        return Results.BadRequest(new { error = "missing args" });
    }

    var routed = chartRouter.Calculate("liuyao", body.Args, ["iching-sixlines-2.0.3"]);
    return Results.Json(routed.Result, SidecarJson.Web);
});

app.MapPost("/tarot", async (HttpRequest request) =>
{
    var body = await request.ReadFromJsonAsync<SidecarBody>();
    if (body?.Args is null)
    {
        return Results.BadRequest(new { error = "missing args" });
    }

    var spreadId = SidecarArgs.GetString(body.Args, "spreadId");
    var question = SidecarArgs.GetString(body.Args, "question");
    var seed = SidecarArgs.GetInt(body.Args, "seed");
    var (reading, engineId) = TarotDrawPipeline.Draw(chartRouter, tarotChain, spreadId, question, seed);
    return Results.Json(new { engine = new { paipan = engineId }, reading }, SidecarJson.Web);
});

app.MapPost("/calendar", async (HttpRequest request) =>
{
    var body = await request.ReadFromJsonAsync<SidecarBody>();
    if (body?.Args is null)
    {
        return Results.BadRequest(new { error = "missing args" });
    }

    var routed = chartRouter.Calculate("calendar", body.Args, ["lunar-csharp-1.6.8"]);
    return Results.Json(new { engine = new { paipan = routed.EngineId }, day = routed.Result }, SidecarJson.Web);
});

Console.WriteLine($"IChing.ChartSidecar preset={preset.Name} ports=[{string.Join(',', preset.Ports)}]");
await app.RunAsync();

static SidecarPreset ParsePreset(string[] args)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--preset" && i + 1 < args.Length)
        {
            return args[i + 1] switch
            {
                "minimal" => new SidecarPreset("minimal", [5001, 5004]),
                "dev" => new SidecarPreset("dev", [5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 5009, 5010, 5011, 5012]),
                "all" => new SidecarPreset("all", [5080]),
                _ => new SidecarPreset("minimal", [5001, 5004])
            };
        }
    }

    return new SidecarPreset("minimal", [5001, 5004]);
}

internal sealed record SidecarBody(Dictionary<string, object?>? Args);

internal sealed record SidecarPreset(string Name, int[] Ports);

internal static class SidecarJson
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}

internal static class SidecarArgs
{
    public static string? GetString(IDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var raw) || raw is null)
        {
            return null;
        }

        return raw switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } el => el.GetString(),
            _ => Convert.ToString(raw)
        };
    }

    public static int? GetInt(IDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var raw) || raw is null)
        {
            return null;
        }

        return raw switch
        {
            int i => i,
            long l => (int)l,
            JsonElement { ValueKind: JsonValueKind.Number } el when el.TryGetInt32(out var n) => n,
            string s when int.TryParse(s, out var n) => n,
            _ => null
        };
    }
}
