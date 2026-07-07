using IChing.Lab.Core.Liuyao;

namespace IChing.Lab.Tests;

public class LiuyaoHexagramNamesTests
{
    [Theory]
    [MemberData(nameof(AllCanonicalNames))]
    public void Display_AllSixtyFourCanonicalNames_ReturnsChinese(string name, string expected)
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
}
