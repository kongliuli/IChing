using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using Lunar;

namespace IChing.Lab.Engines.Bazi;

/// <summary>
/// cnlunar 八字引擎的 C# 移植实现（非桥接、非 stub）。
/// <para>
/// 复用 lunar-csharp 1.6.8 的历法原语取月支/日支，在其上实现 cnlunar 0.2.4 风格的
/// 建除十二神（建/除/满/平/定/执/破/危/成/收/开/闭）计算与宜忌等第映射表。
/// 宜忌等第依据《钦定协纪辨方书》建除十二神宜忌条目编排。
/// </para>
/// <para>
/// 建除十二神算法：以月支起建，日支对月支顺数即为当日十二神。
/// 例：寅月寅日为建，寅月卯日为除，寅月辰日为满，依此类推。
/// </para>
/// </summary>
public sealed class BaziCnlunarPortEngine : IChartEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>十二地支顺序，与建除十二神顺位一一对应。</summary>
    private static readonly string[] ZhiOrder =
        ["子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥"];

    /// <summary>建除十二神名，按顺序对应月支起建的偏移位 0..11。</summary>
    private static readonly string[] JianChuNames =
        ["建", "除", "满", "平", "定", "执", "破", "危", "成", "收", "开", "闭"];

    /// <summary>
    /// 建除十二神宜忌等第表，依据《钦定协纪辨方书》编排。
    /// 每神至少 3-5 条宜/忌，并给出等第（吉/平/凶）。
    /// </summary>
    private static readonly Dictionary<string, YiJiEntry> YiJiTable = BuildYiJiTable();

    public string Domain => "bazi";

    public string EngineId => "bazi-cnlunar-port";

    public EngineMetadata Metadata { get; } = new(
        Source: "cnlunar",
        Version: "0.2.4-port",
        AlgorithmBasis: "《钦定协纪辨方书》宜忌等第 + 建除十二神",
        TemplateHint: "cnlunar",
        ModuleFocus: ["yiji", "dengdi"]);

    /// <summary>
    /// 计算排盘：用 lunar-csharp 取月支/日支，计算建除十二神，查宜忌等第表，
    /// 返回 { engine:{paipan,source}, monthZhi, dayZhi, jianchu, yiji:{宜:[...],忌:[...],等第:"..."} }
    /// </summary>
    public object Calculate(ChartRequest request)
    {
        var input = DeserializeArgs<CnlunarInput>(request.Args);

        var solar = Solar.FromYmdHms(
            input.Year, input.Month, input.Day,
            input.Hour, input.Minute, input.Second);
        var lunar = solar.Lunar;
        var eight = lunar.EightChar;

        var monthZhi = eight.MonthZhi;
        var dayZhi = eight.DayZhi;

        var jianchu = ResolveJianChu(monthZhi, dayZhi);
        var yiji = YiJiTable.TryGetValue(jianchu, out var entry)
            ? entry
            : new YiJiEntry(Array.Empty<string>(), Array.Empty<string>(), "平");

        return new
        {
            engine = new
            {
                paipan = EngineId,
                source = Metadata.Source,
                version = Metadata.Version,
                ready = true
            },
            solar = solar.ToString(),
            lunar = lunar.ToString(),
            monthZhi,
            dayZhi,
            jianchu,
            yiji = new
            {
                宜 = yiji.Yi,
                忌 = yiji.Ji,
                等第 = yiji.DengDi
            }
        };
    }

    /// <summary>
    /// 根据月支与日支推算建除十二神。
    /// 算法：以月支为建，日支相对月支顺数得偏移，对应 JianChuNames[偏移]。
    /// </summary>
    internal static string ResolveJianChu(string monthZhi, string dayZhi)
    {
        var mIdx = Array.IndexOf(ZhiOrder, monthZhi);
        var dIdx = Array.IndexOf(ZhiOrder, dayZhi);
        if (mIdx < 0 || dIdx < 0)
        {
            // lunar-csharp 返回的地支恒在 ZhiOrder 内，容错返回空串由调用方感知。
            return string.Empty;
        }

        var offset = (dIdx - mIdx + 12) % 12;
        return JianChuNames[offset];
    }

    private static T DeserializeArgs<T>(IDictionary<string, object?> args)
    {
        var json = JsonSerializer.Serialize(args);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException("无法从参数字典反序列化输入");
    }

    /// <summary>构造《钦定协纪辨方书》建除十二神宜忌等第表。</summary>
    private static Dictionary<string, YiJiEntry> BuildYiJiTable()
    {
        // 宜/忌条目取自《钦定协纪辨方书》建除十二神宜忌条目，等第按传统吉/平/凶标注。
        return new Dictionary<string, YiJiEntry>(StringComparer.Ordinal)
        {
            ["建"] = new YiJiEntry(
                Yi: ["祭祀", "入学", "谒贵", "上任", "祈福"],
                Ji: ["动土", "开仓", "远行", "栽种"],
                DengDi: "平"),
            ["除"] = new YiJiEntry(
                Yi: ["祭祀", "解除", "沐浴", "扫舍", "求医", "疗病", "出行"],
                Ji: ["嫁娶", "求名", "上任"],
                DengDi: "吉"),
            ["满"] = new YiJiEntry(
                Yi: ["祭祀", "祈福", "进人口", "捕捉", "畋猎"],
                Ji: ["安床", "伐木", "开市", "交易", "嫁娶"],
                DengDi: "平"),
            ["平"] = new YiJiEntry(
                Yi: ["修造", "动土", "平治道涂"],
                Ji: ["祭祀", "祈福", "求嗣", "开渠"],
                DengDi: "凶"),
            ["定"] = new YiJiEntry(
                Yi: ["祭祀", "祈福", "冠笄", "嫁娶", "纳采", "盖屋"],
                Ji: ["出行", "词讼", "争斗", "乘船"],
                DengDi: "吉"),
            ["执"] = new YiJiEntry(
                Yi: ["捕捉", "畋猎", "祭祀", "祈福", "求嗣", "订约"],
                Ji: ["开市", "立券", "开仓", "出货", "移徙"],
                DengDi: "平"),
            ["破"] = new YiJiEntry(
                Yi: ["求医", "疗病", "破屋", "坏垣"],
                Ji: ["嫁娶", "开市", "立券", "祈福", "出行", "安葬"],
                DengDi: "凶"),
            ["危"] = new YiJiEntry(
                Yi: ["祭祀", "祈福", "安床", "入殓", "行丧"],
                Ji: ["登山", "乘船", "出行"],
                DengDi: "凶"),
            ["成"] = new YiJiEntry(
                Yi: ["祭祀", "祈福", "入学", "结婚", "订盟", "纳采", "移徙", "入宅", "安机械"],
                Ji: ["词讼", "出行", "乘船", "安葬"],
                DengDi: "吉"),
            ["收"] = new YiJiEntry(
                Yi: ["祭祀", "纳财", "捕捉", "畋猎", "开市", "交易", "纳畜"],
                Ji: ["出行", "安葬", "开仓", "出货"],
                DengDi: "平"),
            ["开"] = new YiJiEntry(
                Yi: ["祭祀", "祈福", "求嗣", "赴任", "出行", "入学", "嫁娶", "移徙", "入宅", "修造"],
                Ji: ["伐木", "开仓库", "出货财"],
                DengDi: "吉"),
            ["闭"] = new YiJiEntry(
                Yi: ["筑堤", "塞穴", "安葬", "祭祀"],
                Ji: ["开市", "出行", "嫁娶", "移徙", "求医", "疗病", "动土"],
                DengDi: "凶")
        };
    }

    private sealed record CnlunarInput(
        int Year, int Month, int Day, int Hour,
        int Minute = 0, int Second = 0);

    private sealed record YiJiEntry(IReadOnlyList<string> Yi, IReadOnlyList<string> Ji, string DengDi);
}
