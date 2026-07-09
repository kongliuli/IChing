using IChing.Lab.Core.Liuyao;

namespace IChing.Lab.Tests;

public class LiuyaoHexagramNamesTests
{
    [Theory]
    [MemberData(nameof(AllCanonicalNames))]
    public void Display_AllSixtyFourCanonicalNames_ReturnsChinese(string name, string expected)
    {
        AssertChineseName(name, expected);
    }

    [Theory]
    [MemberData(nameof(AllLibraryFieldNames))]
    public void Display_AllSixtyFourLibraryFieldNames_ReturnsChinese(string name, string expected)
    {
        AssertChineseName(name, expected);
    }

    private static void AssertChineseName(string name, string expected)
    {
        var actual = HexagramNames.Display(name);
        Assert.Equal(expected, actual);
        Assert.DoesNotContain("卦名待补", actual);
        Assert.DoesNotMatch("[A-Za-z]", actual);
    }

    public static TheoryData<string, string> AllCanonicalNames() => new()
    {
        { "Qian", "乾为天" },
        { "Kun", "坤为地" },
        { "Zhun", "水雷屯" },
        { "Meng", "山水蒙" },
        { "Xu", "水天需" },
        { "Song", "天水讼" },
        { "Shi", "地水师" },
        { "Bi", "水地比" },
        { "Xiao Chu", "风天小畜" },
        { "Lu", "天泽履" },
        { "Tai", "地天泰" },
        { "Pi", "天地否" },
        { "Tong Ren", "天火同人" },
        { "Da You", "火天大有" },
        { "Qian Humility", "地山谦" },
        { "Yu", "雷地豫" },
        { "Sui", "泽雷随" },
        { "Gu", "山风蛊" },
        { "Lin", "地泽临" },
        { "Guan", "风地观" },
        { "Shi He", "火雷噬嗑" },
        { "Bi Grace", "山火贲" },
        { "Bo", "山地剥" },
        { "Fu", "地雷复" },
        { "Wu Wang", "天雷无妄" },
        { "Da Chu", "山天大畜" },
        { "Yi", "山雷颐" },
        { "Da Guo", "泽风大过" },
        { "Kan", "坎为水" },
        { "Li", "离为火" },
        { "Xian", "泽山咸" },
        { "Heng", "雷风恒" },
        { "Dun", "天山遁" },
        { "Da Zhuang", "雷天大壮" },
        { "Jin", "火地晋" },
        { "Ming Yi", "地火明夷" },
        { "Jia Ren", "风火家人" },
        { "Kui", "火泽睽" },
        { "Jian", "水山蹇" },
        { "Jie", "雷水解" },
        { "Sun", "山泽损" },
        { "Yi Increase", "风雷益" },
        { "Guai", "泽天夬" },
        { "Gou", "天风姤" },
        { "Cui", "泽地萃" },
        { "Sheng", "地风升" },
        { "Kun Oppression", "泽水困" },
        { "Jing", "水风井" },
        { "Ge", "泽火革" },
        { "Ding", "火风鼎" },
        { "Zhen", "震为雷" },
        { "Gen", "艮为山" },
        { "Jian Gradual", "风山渐" },
        { "Gui Mei", "雷泽归妹" },
        { "Feng", "雷火丰" },
        { "Lu Wanderer", "火山旅" },
        { "Xun", "巽为风" },
        { "Dui", "兑为泽" },
        { "Huan", "风水涣" },
        { "Jie Limitation", "水泽节" },
        { "Zhong Fu", "风泽中孚" },
        { "Xiao Guo", "雷山小过" },
        { "Ji Ji", "水火既济" },
        { "Wei Ji", "火水未济" }
    };

    public static TheoryData<string, string> AllLibraryFieldNames() => new()
    {
        { "TheCreative", "乾为天" },
        { "ComingToMeet", "天风姤" },
        { "Retreat", "天山遁" },
        { "Standstill", "天地否" },
        { "Contemplation", "风地观" },
        { "SplittingApart", "山地剥" },
        { "Progress", "火地晋" },
        { "PossessionInGreatMeasure", "火天大有" },
        { "TheJoyous", "兑为泽" },
        { "Oppression", "泽水困" },
        { "GatheringTogether", "泽地萃" },
        { "Influence", "泽山咸" },
        { "Obstruction", "水山蹇" },
        { "Modesty", "地山谦" },
        { "PreponderanceOfTheSmall", "雷山小过" },
        { "TheMarryingMaiden", "雷泽归妹" },
        { "TheClinging", "离为火" },
        { "TheWanderer", "火山旅" },
        { "TheCauldron", "火风鼎" },
        { "BeforeCompletion", "火水未济" },
        { "YouthfulFolly", "山水蒙" },
        { "Dispersion", "风水涣" },
        { "Conflict", "天水讼" },
        { "FellowshipWithMen", "天火同人" },
        { "TheArousing", "震为雷" },
        { "Enthusiasm", "雷地豫" },
        { "Deliverance", "雷水解" },
        { "Duration", "雷风恒" },
        { "PushingUpward", "地风升" },
        { "TheWell", "水风井" },
        { "PreponderanceOfTheGreat", "泽风大过" },
        { "Following", "泽雷随" },
        { "TheGentle", "巽为风" },
        { "TheTamingPowerOfTheSmall", "风天小畜" },
        { "TheFamily", "风火家人" },
        { "Increase", "风雷益" },
        { "Innocence", "天雷无妄" },
        { "BitingThrough", "火雷噬嗑" },
        { "TheCornersOfTheMouth", "山雷颐" },
        { "WorkOnTheDecayed", "山风蛊" },
        { "TheAbysmal", "坎为水" },
        { "Limitation", "水泽节" },
        { "DifficultyAtTheBeginning", "水雷屯" },
        { "AfterCompletion", "水火既济" },
        { "Revolution", "泽火革" },
        { "Abundance", "雷火丰" },
        { "DarkeningOfTheLight", "地火明夷" },
        { "TheArmy", "地水师" },
        { "KeepingStill", "艮为山" },
        { "Grace", "山火贲" },
        { "TheTamingPowerOfTheGreat", "山天大畜" },
        { "Decrease", "山泽损" },
        { "Opposition", "火泽睽" },
        { "Treading", "天泽履" },
        { "InnerTruth", "风泽中孚" },
        { "Development", "风山渐" },
        { "TheReceptive", "坤为地" },
        { "Return", "地雷复" },
        { "Approach", "地泽临" },
        { "Peace", "地天泰" },
        { "ThePowerOfTheGreat", "雷天大壮" },
        { "BreakThrough", "泽天夬" },
        { "Waiting", "水天需" },
        { "HoldingTogether", "水地比" }
    };
}
