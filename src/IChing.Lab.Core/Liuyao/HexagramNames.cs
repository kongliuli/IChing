namespace IChing.Lab.Core.Liuyao;

public static class HexagramNames
{
    public static string Display(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return Map.TryGetValue(name.Trim(), out var zh) ? zh : name;
    }

    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Qian"] = "乾为天", ["The Creative"] = "乾为天", ["䷀"] = "乾为天",
        ["Kun"] = "坤为地", ["The Receptive"] = "坤为地", ["䷁"] = "坤为地",
        ["Zhun"] = "水雷屯", ["Difficulty at the Beginning"] = "水雷屯",
        ["Meng"] = "山水蒙", ["Youthful Folly"] = "山水蒙",
        ["Xu"] = "水天需", ["Waiting"] = "水天需",
        ["Song"] = "天水讼", ["Conflict"] = "天水讼",
        ["Shi"] = "地水师", ["The Army"] = "地水师",
        ["Bi"] = "水地比", ["Holding Together"] = "水地比",
        ["Xiao Chu"] = "风天小畜", ["Small Taming"] = "风天小畜",
        ["Lu"] = "天泽履", ["Treading"] = "天泽履",
        ["Tai"] = "地天泰", ["Peace"] = "地天泰",
        ["Pi"] = "天地否", ["Standstill"] = "天地否",
        ["Tong Ren"] = "天火同人", ["Fellowship"] = "天火同人",
        ["Da You"] = "火天大有", ["Great Possession"] = "火天大有",
        ["Qian Humility"] = "地山谦", ["Modesty"] = "地山谦",
        ["Yu"] = "雷地豫", ["Enthusiasm"] = "雷地豫",
        ["Sui"] = "泽雷随", ["Following"] = "泽雷随",
        ["Gu"] = "山风蛊", ["Work on the Decayed"] = "山风蛊",
        ["Lin"] = "地泽临", ["Approach"] = "地泽临",
        ["Guan"] = "风地观", ["Contemplation"] = "风地观",
        ["Shi He"] = "火雷噬嗑", ["Biting Through"] = "火雷噬嗑",
        ["Bi Grace"] = "山火贲", ["Grace"] = "山火贲",
        ["Bo"] = "山地剥", ["Splitting Apart"] = "山地剥",
        ["Fu"] = "地雷复", ["Return"] = "地雷复",
        ["Wu Wang"] = "天雷无妄", ["Innocence"] = "天雷无妄",
        ["Da Chu"] = "山天大畜", ["Great Taming"] = "山天大畜",
        ["Yi"] = "山雷颐", ["Nourishment"] = "山雷颐",
        ["Da Guo"] = "泽风大过", ["Great Preponderance"] = "泽风大过",
        ["Kan"] = "坎为水", ["The Abysmal"] = "坎为水",
        ["Li"] = "离为火", ["The Clinging"] = "离为火",
        ["Xian"] = "泽山咸", ["Influence"] = "泽山咸",
        ["Heng"] = "雷风恒", ["Duration"] = "雷风恒",
        ["Dun"] = "天山遁", ["Retreat"] = "天山遁",
        ["Da Zhuang"] = "雷天大壮", ["Great Power"] = "雷天大壮",
        ["Jin"] = "火地晋", ["Progress"] = "火地晋",
        ["Ming Yi"] = "地火明夷", ["Darkening of the Light"] = "地火明夷",
        ["Jia Ren"] = "风火家人", ["The Family"] = "风火家人",
        ["Kui"] = "火泽睽", ["Opposition"] = "火泽睽",
        ["Jian"] = "水山蹇", ["Obstruction"] = "水山蹇",
        ["Jie"] = "雷水解", ["Deliverance"] = "雷水解",
        ["Sun"] = "山泽损", ["Decrease"] = "山泽损",
        ["Yi Increase"] = "风雷益", ["Increase"] = "风雷益",
        ["Guai"] = "泽天夬", ["Breakthrough"] = "泽天夬",
        ["Gou"] = "天风姤", ["Coming to Meet"] = "天风姤",
        ["Cui"] = "泽地萃", ["Gathering Together"] = "泽地萃",
        ["Sheng"] = "地风升", ["Pushing Upward"] = "地风升",
        ["Kun Oppression"] = "泽水困", ["Oppression"] = "泽水困",
        ["Jing"] = "水风井", ["The Well"] = "水风井",
        ["Ge"] = "泽火革", ["Revolution"] = "泽火革",
        ["Ding"] = "火风鼎", ["The Cauldron"] = "火风鼎",
        ["Zhen"] = "震为雷", ["The Arousing"] = "震为雷",
        ["Gen"] = "艮为山", ["Keeping Still"] = "艮为山",
        ["Jian Gradual"] = "风山渐", ["Development"] = "风山渐",
        ["Gui Mei"] = "雷泽归妹", ["The Marrying Maiden"] = "雷泽归妹",
        ["Feng"] = "雷火丰", ["Abundance"] = "雷火丰",
        ["Lu Wanderer"] = "火山旅", ["The Wanderer"] = "火山旅",
        ["Xun"] = "巽为风", ["The Gentle"] = "巽为风",
        ["Dui"] = "兑为泽", ["The Joyous"] = "兑为泽",
        ["Huan"] = "风水涣", ["Dispersion"] = "风水涣",
        ["Jie Limitation"] = "水泽节", ["Limitation"] = "水泽节",
        ["Zhong Fu"] = "风泽中孚", ["Inner Truth"] = "风泽中孚",
        ["Xiao Guo"] = "雷山小过", ["Small Preponderance"] = "雷山小过",
        ["Ji Ji"] = "水火既济", ["After Completion"] = "水火既济",
        ["Wei Ji"] = "火水未济", ["Before Completion"] = "火水未济"
    };
}
