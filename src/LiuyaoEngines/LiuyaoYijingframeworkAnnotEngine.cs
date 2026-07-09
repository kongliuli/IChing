using System.Collections.ObjectModel;
using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using YiJingFramework.Annotating;

namespace IChing.Lab.Engines.Liuyao;

/// <summary>
/// C# 直接引用引擎：基于 <see cref="YiJingFramework.Annotating"/> 5.0.1 包，
/// 以《周易》《易传》原文（卦辞 + 爻辞）作为 liuyao 域排盘的爻辞注解数据载体。
/// <para>
/// <b>未退化</b>：YiJingFramework.Annotating 5.0.1 在沙箱内 <c>dotnet restore</c> 已成功还原，
/// 故直接使用 <see cref="AnnotationStore"/> / <see cref="AnnotationGroup"/> / <see cref="AnnotationEntry"/>
/// 作为注解数据载体（store.Title / group.Title=卦名 / group.Comment=卦辞 / entry.Target=爻位 / entry.Content=爻辞）。
/// </para>
/// <para>
/// 该包仅提供泛型注解容器，本身不内嵌《周易》原文。本引擎在构造时按 King Wen 序将 64 卦名 + 卦辞
/// 注入 store；其中乾（乾为天）、坤（坤为地）两卦补全六爻爻辞 + 用九/用六，其余 62 卦提供卦名 + 卦辞。
/// 所有文本均为《周易》原文，非占位假数据。
/// </para>
/// <para>
/// Calculate 输入 Args["hexagramName"]（如 "乾为天"）返回该卦完整爻辞注解；
/// 或 Args["yaos"]（如 "初九" 或字符串数组）返回对应爻位注解。
/// </para>
/// </summary>
public sealed class LiuyaoYijingframeworkAnnotEngine : IChartEngine
{
    private readonly AnnotationStore _store;

    /// <summary>构造引擎：构建内嵌 64 卦注解仓库。</summary>
    public LiuyaoYijingframeworkAnnotEngine()
    {
        _store = new AnnotationStore { Title = "周易爻辞注解" };

        foreach (var hex in ZhouyiHexagrams.All)
        {
            var group = _store.AddGroup(hex.Name, hex.GuaCi);
            if (hex.YaoCi is { Length: > 0 } yaoci)
            {
                foreach (var (position, text) in yaoci)
                {
                    group.AddEntry(position, text);
                }
            }
        }
    }

    /// <inheritdoc />
    public string Domain => "liuyao";

    /// <inheritdoc />
    public string EngineId => "liuyao-yijingframework-annot";

    /// <inheritdoc />
    public EngineMetadata Metadata { get; } = new(
        Source: "YiJingFramework.Annotating",
        Version: "5.0.1",
        AlgorithmBasis: "《周易》《易传》注解作爻辞数据载体",
        TemplateHint: "yijingframework",
        ModuleFocus: ["yaoci", "zhouyi"]);

    /// <summary>
    /// 根据请求参数返回爻辞注解对象。
    /// <list type="bullet">
    /// <item>Args["hexagramName"]：返回该卦的卦辞 + 全部爻辞（若已录入）。</item>
    /// <item>Args["yaos"]：可为单个爻位字符串（如 "初九"）或爻位数组；返回命中的爻辞集合。
    /// 当同时提供 hexagramName 与 yaos 时，yaos 限定在该卦内查询。</item>
    /// </list>
    /// 未命中或参数缺失时返回 <c>{ engine, error }</c> 对象，不抛异常。
    /// </summary>
    public object Calculate(ChartRequest request)
    {
        var args = request.Args ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var hexagramName = TryGetString(args, "hexagramName");
        var yaosRaw = args.TryGetValue("yaos", out var yaosObj) ? yaosObj : null;

        var engineInfo = new
        {
            paipan = EngineId,
            source = Metadata.Source,
            version = Metadata.Version
        };

        // 单爻位 / 多爻位查询。
        if (yaosRaw is not null)
        {
            var yaoPositions = ToStringList(yaosRaw);
            if (yaoPositions.Count == 0)
            {
                return new { engine = engineInfo, error = "yaos 为空或无法识别" };
            }

            var matches = LookupYaos(hexagramName, yaoPositions);
            if (matches.Count == 0)
            {
                return new { engine = engineInfo, error = "未找到对应爻位注解", hexagram = hexagramName, yaos = yaoPositions };
            }

            return new
            {
                engine = engineInfo,
                hexagram = hexagramName,
                yaoci = matches
            };
        }

        // 整卦查询。
        if (!string.IsNullOrWhiteSpace(hexagramName))
        {
            var group = _store.GetGroup(hexagramName);
            if (group is null)
            {
                return new { engine = engineInfo, error = "未找到卦名", hexagram = hexagramName };
            }

            var yaoci = group.Entries
                .Select(e => new { yao = e.Target, text = e.Content })
                .ToList();

            return new
            {
                engine = engineInfo,
                hexagram = group.Title,
                guaci = group.Comment,
                yaoci
            };
        }

        return new { engine = engineInfo, error = "缺少 hexagramName 或 yaos 参数" };
    }

    /// <summary>在指定卦（若提供）或全 store 范围内查询爻位注解。</summary>
    private List<object> LookupYaos(string? hexagramName, IReadOnlyList<string> yaoPositions)
    {
        var result = new List<object>();
        if (!string.IsNullOrWhiteSpace(hexagramName))
        {
            var group = _store.GetGroup(hexagramName!);
            if (group is null)
            {
                return result;
            }

            foreach (var pos in yaoPositions)
            {
                var entry = group.GetEntry(pos);
                if (entry is not null)
                {
                    result.Add(new { hexagram = group.Title, yao = entry.Target, text = entry.Content });
                }
            }
        }
        else
        {
            foreach (var group in _store.Groups)
            {
                foreach (var pos in yaoPositions)
                {
                    var entry = group.GetEntry(pos);
                    if (entry is not null)
                    {
                        result.Add(new { hexagram = group.Title, yao = entry.Target, text = entry.Content });
                    }
                }
            }
        }
        return result;
    }

    private static string? TryGetString(IDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }
        return value switch
        {
            string s => s,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
            _ => Convert.ToString(value)
        };
    }

    /// <summary>将 args 中可能为 string / IEnumerable / JsonElement 的值统一为字符串列表。</summary>
    private static ReadOnlyCollection<string> ToStringList(object? raw)
    {
        var list = new List<string>();
        switch (raw)
        {
            case null:
                break;
            case string s:
                list.Add(s);
                break;
            case JsonElement je:
                if (je.ValueKind == JsonValueKind.String)
                {
                    var s = je.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        list.Add(s);
                    }
                }
                else if (je.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in je.EnumerateArray())
                    {
                        var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            list.Add(s);
                        }
                    }
                }
                break;
            case System.Collections.IEnumerable enumerable:
                foreach (var item in enumerable)
                {
                    var s = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        list.Add(s);
                    }
                }
                break;
            default:
                list.Add(raw.ToString() ?? string.Empty);
                break;
        }
        return list.AsReadOnly();
    }
}

/// <summary>《周易》64 卦静态数据：卦名（含上下卦组合名） + 卦辞 + 爻辞（仅乾/坤两卦完整录入）。</summary>
internal static class ZhouyiHexagrams
{
    internal sealed record Hexagram(string Name, string GuaCi, (string Position, string Text)[]? YaoCi = null);

    /// <summary>64 卦按 King Wen 序。乾/坤两卦含完整六爻爻辞 + 用九/用六；其余 62 卦仅卦名 + 卦辞。</summary>
    internal static readonly IReadOnlyList<Hexagram> All = BuildAll();

    private static List<Hexagram> BuildAll()
    {
        var list = new List<Hexagram>(64)
        {
            new("乾为天", "元亨利贞", new[]
            {
                ("初九", "潜龙勿用"),
                ("九二", "见龙在田，利见大人"),
                ("九三", "君子终日乾乾，夕惕若厉，无咎"),
                ("九四", "或跃在渊，无咎"),
                ("九五", "飞龙在天，利见大人"),
                ("上九", "亢龙有悔"),
                ("用九", "见群龙无首，吉")
            }),
            new("坤为地", "元亨，利牝马之贞。君子有攸往，先迷后得主，利。西南得朋，东北丧朋。安贞，吉", new[]
            {
                ("初六", "履霜，坚冰至"),
                ("六二", "直方大，不习无不利"),
                ("六三", "含章可贞。或从王事，无成有终"),
                ("六四", "括囊，无咎无誉"),
                ("六五", "黄裳，元吉"),
                ("上六", "龙战于野，其血玄黄"),
                ("用六", "利永贞")
            }),
            new("水雷屯", "元亨利贞，勿用有攸往，利建侯"),
            new("山水蒙", "亨。匪我求童蒙，童蒙求我。初筮告，再三渎，渎则不告。利贞"),
            new("水天需", "有孚，光亨，贞吉。利涉大川"),
            new("天水讼", "有孚，窒。惕中吉，终凶。利见大人，不利涉大川"),
            new("地水师", "贞，丈人吉无咎"),
            new("水地比", "吉。原筮元永贞，无咎。不宁方来，后夫凶"),
            new("风天小畜", "亨。密云不雨，自我西郊"),
            new("天泽履", "履虎尾，不咥人，亨"),
            new("地天泰", "小往大来，吉亨"),
            new("天地否", "否之匪人，不利君子贞，大往小来"),
            new("天火同人", "同人于野，亨。利涉大川，利君子贞"),
            new("火天大有", "元亨"),
            new("地山谦", "亨，君子有终"),
            new("雷地豫", "利建侯行师"),
            new("泽雷随", "元亨利贞，无咎"),
            new("山风蛊", "元亨，利涉大川。先甲三日，后甲三日"),
            new("地泽临", "元亨利贞。至于八月有凶"),
            new("风地观", "盥而不荐，有孚颙若"),
            new("火雷噬嗑", "亨。利用狱"),
            new("山火贲", "亨。小利有攸往"),
            new("山地剥", "不利有攸往"),
            new("地雷复", "亨。出入无疾，朋来无咎。反复其道，七日来复，利有攸往"),
            new("天雷无妄", "元亨利贞。其匪正有眚，不利有攸往"),
            new("山天大畜", "利贞，不家食吉，利涉大川"),
            new("山雷颐", "贞吉。观颐，自求口实"),
            new("泽风大过", "栋桡，利有攸往，亨"),
            new("坎为水", "习坎，有孚，维心亨，行有尚"),
            new("离为火", "利贞，亨。畜牝牛，吉"),
            new("泽山咸", "亨，利贞，取女吉"),
            new("雷风恒", "亨，无咎，利贞，利有攸往"),
            new("天山遁", "亨，小利贞"),
            new("雷天大壮", "利贞"),
            new("火地晋", "康侯用锡马蕃庶，昼日三接"),
            new("地火明夷", "利艰贞"),
            new("风火家人", "利女贞"),
            new("火泽睽", "小事吉"),
            new("水山蹇", "利西南，不利东北；利见大人，贞吉"),
            new("雷水解", "利西南，无所往，其来复吉。有攸往，夙吉"),
            new("山泽损", "有孚，元吉，无咎，可贞，利有攸往"),
            new("风雷益", "利有攸往，利涉大川"),
            new("泽天夬", "扬于王庭，孚号，有厉，告自邑，不利即戎，利有攸往"),
            new("天风姤", "女壮，勿用取女"),
            new("泽地萃", "亨。王假有庙，利见大人，亨，利贞。用大牲吉，利有攸往"),
            new("地风升", "元亨，用见大人，勿恤，南征吉"),
            new("泽水困", "亨，贞，大人吉，无咎，有言不信"),
            new("水风井", "改邑不改井，无丧无得，往来井井。汔至，亦未繘井，羸其瓶，凶"),
            new("泽火革", "己日乃孚，元亨利贞，悔亡"),
            new("火风鼎", "元吉，亨"),
            new("震为雷", "亨。震来虩虩，笑言哑哑。震惊百里，不丧匕鬯"),
            new("艮为山", "艮其背，不获其身，行其庭，不见其人，无咎"),
            new("风山渐", "女归吉，利贞"),
            new("雷泽归妹", "征凶，无攸利"),
            new("雷火丰", "亨，王假之，勿忧，宜日中"),
            new("火山旅", "小亨，旅贞吉"),
            new("巽为风", "小亨，利有攸往，利见大人"),
            new("兑为泽", "亨，利贞"),
            new("风水涣", "亨。王假有庙，利涉大川，利贞"),
            new("水泽节", "亨。苦节不可贞"),
            new("风泽中孚", "豚鱼吉，利涉大川，利贞"),
            new("雷山小过", "亨，利贞，可小事，不可大事。飞鸟遗之音，不宜上宜下，大吉"),
            new("水火既济", "亨小，利贞，初吉终乱"),
            new("火水未济", "亨。小狐汔济，濡其尾，无攸利")
        };
        return list;
    }
}
