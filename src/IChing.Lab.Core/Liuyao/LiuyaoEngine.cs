namespace IChing.Lab.Core.Liuyao;

/// <summary>
/// Six-yao coin method: sum of three coins (2=yin, 3=yang) yields 6/7/8/9 with 1/8,3/8,3/8,1/8.
/// </summary>
public static class LiuyaoEngine
{
    private static readonly string[] Trigrams = ["乾", "兑", "离", "震", "巽", "坎", "艮", "坤"];

    public static LiuyaoResult CoinToss(int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var lines = new List<YaoLine>(6);
        for (var i = 0; i < 6; i++)
        {
            var sum = 0;
            for (var c = 0; c < 3; c++)
            {
                sum += rng.Next(2) == 0 ? 2 : 3;
            }

            lines.Add(sum switch
            {
                6 => new YaoLine(i + 1, false, true, true),
                7 => new YaoLine(i + 1, true, false, false),
                8 => new YaoLine(i + 1, false, false, false),
                9 => new YaoLine(i + 1, true, true, true),
                _ => throw new InvalidOperationException("invalid coin sum")
            });
        }

        return BuildResult("coin", lines);
    }

    /// <summary>
    /// Mei Hua style time hexagram. Remainder 0 maps to 8 (坤) or 6 (上爻).
    /// </summary>
    public static LiuyaoResult TimeHexagram(DateTime when)
    {
        var lunarNums = ToLunarNums(when);
        var upper = Mod8(lunarNums.Year + lunarNums.Month + lunarNums.Day);
        var lower = Mod8(lunarNums.Year + lunarNums.Month + lunarNums.Day + lunarNums.Hour);
        var moving = Mod6(lunarNums.Year + lunarNums.Month + lunarNums.Day + lunarNums.Hour);

        var lines = new List<YaoLine>(6);
        for (var i = 0; i < 6; i++)
        {
            var isYang = i < 3 ? Bit(lower, i) : Bit(upper, i - 3);
            var movingLine = i + 1 == moving;
            lines.Add(new YaoLine(i + 1, isYang, movingLine, movingLine));
        }

        return BuildResult("time", lines, moving);
    }

    private static LiuyaoResult BuildResult(string method, List<YaoLine> lines, int? movingLine = null)
    {
        var moving = movingLine ?? lines.FirstOrDefault(l => l.Moving)?.Index;
        var upperIdx = TrigramIndex(lines, 3);
        var lowerIdx = TrigramIndex(lines, 0);
        var changed = lines.Select(l => l.Moving ? !l.Yang : l.Yang).ToList();

        return new LiuyaoResult(
            Method: method,
            Lines: lines,
            UpperTrigram: Trigrams[upperIdx],
            LowerTrigram: Trigrams[lowerIdx],
            HexagramName: $"{Trigrams[upperIdx]}{Trigrams[lowerIdx]}",
            MovingLine: moving,
            ChangedUpperTrigram: Trigrams[TrigramIndex(changed, 3)],
            ChangedLowerTrigram: Trigrams[TrigramIndex(changed, 0)]
        );
    }

    private static int TrigramIndex(List<YaoLine> lines, int start)
    {
        var bits = 0;
        for (var i = 0; i < 3; i++)
        {
            if (lines[start + i].Yang)
            {
                bits |= 1 << i;
            }
        }
        return bits;
    }

    private static int TrigramIndex(List<bool> yang, int start)
    {
        var bits = 0;
        for (var i = 0; i < 3; i++)
        {
            if (yang[start + i])
            {
                bits |= 1 << i;
            }
        }
        return bits;
    }

    private static bool Bit(int trigram, int pos) => (trigram & (1 << pos)) != 0;

    private static int Mod8(int n) => n % 8 == 0 ? 8 : n % 8;
    private static int Mod6(int n) => n % 6 == 0 ? 6 : n % 6;

    // ponytail: simplified stem-branch index for lab; production should use lunar-csharp GanZhi.
    private static (int Year, int Month, int Day, int Hour) ToLunarNums(DateTime when)
    {
        return (when.Year % 12 + 1, when.Month, when.Day, when.Hour % 12 + 1);
    }
}

public record YaoLine(int Index, bool Yang, bool Moving, bool Changes);

public record LiuyaoResult(
    string Method,
    IReadOnlyList<YaoLine> Lines,
    string UpperTrigram,
    string LowerTrigram,
    string HexagramName,
    int? MovingLine,
    string ChangedUpperTrigram,
    string ChangedLowerTrigram
);
