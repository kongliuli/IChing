namespace IChing.Tarot.App.Services;

/// <summary>RWS 牌面 slug → mixvlad/TarotCards 720px 源文件名。</summary>
internal static class TarotRwsImageCatalog
{
    private static readonly Dictionary<string, string> SlugToSrc = new(StringComparer.OrdinalIgnoreCase);

    static TarotRwsImageCatalog()
    {
        var major = new (string num, string slug, string file)[]
        {
            ("00", "the-fool", "00_Fool.jpg"), ("01", "the-magician", "01_Magician.jpg"),
            ("02", "the-high-priestess", "02_High_Priestess.jpg"), ("03", "the-empress", "03_Empress.jpg"),
            ("04", "the-emperor", "04_Emperor.jpg"), ("05", "the-hierophant", "05_Hierophant.jpg"),
            ("06", "the-lovers", "06_Lovers.jpg"), ("07", "the-chariot", "07_Chariot.jpg"),
            ("08", "strength", "08_Strength.jpg"), ("09", "the-hermit", "09_Hermit.jpg"),
            ("10", "wheel-of-fortune", "10_Wheel_of_Fortune.jpg"), ("11", "justice", "11_Justice.jpg"),
            ("12", "the-hanged-man", "12_Hanged_Man.jpg"), ("13", "death", "13_Death.jpg"),
            ("14", "temperance", "14_Temperance.jpg"), ("15", "the-devil", "15_Devil.jpg"),
            ("16", "the-tower", "16_Tower.jpg"), ("17", "the-star", "17_Star.jpg"),
            ("18", "the-moon", "18_Moon.jpg"), ("19", "the-sun", "19_Sun.jpg"),
            ("20", "judgement", "20_Judgement.jpg"), ("21", "the-world", "21_World.jpg")
        };
        foreach (var (_, slug, file) in major)
        {
            SlugToSrc[slug] = file;
        }

        var rank = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ace"] = "ace", ["Two"] = "two", ["Three"] = "three", ["Four"] = "four",
            ["Five"] = "five", ["Six"] = "six", ["Seven"] = "seven", ["Eight"] = "eight",
            ["Nine"] = "nine", ["Ten"] = "ten", ["Page"] = "page", ["Knight"] = "knight",
            ["Queen"] = "queen", ["King"] = "king"
        };
        var suitSrc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Wands"] = "Wands", ["Cups"] = "Cups", ["Swords"] = "Swords", ["Pentacles"] = "Pents"
        };

        foreach (var (rankEn, rankSlug) in rank)
        {
            foreach (var (suitEn, suitFile) in suitSrc)
            {
                var slug = $"{rankSlug}-of-{suitEn.ToLowerInvariant()}";
                var num = rankEn switch
                {
                    "Ace" => "01", "Two" => "02", "Three" => "03", "Four" => "04",
                    "Five" => "05", "Six" => "06", "Seven" => "07", "Eight" => "08",
                    "Nine" => "09", "Ten" => "10", "Page" => "11", "Knight" => "12",
                    "Queen" => "13", "King" => "14",
                    _ => "01"
                };
                SlugToSrc[slug] = $"{suitFile}{num}.jpg";
            }
        }
    }

    public static string Slug(string cardName) =>
        cardName.ToLowerInvariant().Replace(" ", "-");

    public static bool TryGetSrcFile(string cardName, out string srcFile) =>
        SlugToSrc.TryGetValue(Slug(cardName), out srcFile!);

    public static IEnumerable<string> AllSlugs() => SlugToSrc.Keys;
}
