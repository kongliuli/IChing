namespace IChing.Lab.Engines.Tarot;

/// <summary>
/// Deckaura 78 牌 12 维牌义数据条目，对应 tarot-card-meanings(Deckaura) 数据集
/// （DOI 10.5281/zenodo.19152918）的单牌记录结构。
/// </summary>
/// <param name="Number">牌号：大阿卡那 0~21；小阿卡那 Ace/2..10/Page/Knight/Queen/King。</param>
/// <param name="Name">英文牌名，如 "The Fool" / "Ace of Wands"。</param>
/// <param name="Arcana">所属阿卡那：Major / Minor。</param>
/// <param name="Suit">花色：Major 为 "Major"；Minor 为 Wands/Cups/Swords/Pentacles。</param>
/// <param name="Element">元素：Fire/Water/Air/Earth。</param>
/// <param name="Planet">行星/占星对应（Golden Dame 体系）：如 "Uranus" / "Mars in Aries" / "Fire of Fire"。</param>
/// <param name="Upright">正位牌义（短语，逗号分隔）。</param>
/// <param name="Reversed">逆位牌义（短语，逗号分隔）。</param>
/// <param name="Love">感情维度牌义。</param>
/// <param name="Career">事业维度牌义。</param>
/// <param name="YesNo">是/否占断：yes / no / unknown。</param>
/// <param name="Keywords">关键词列表（逗号分隔）。</param>
public sealed record TarotCardMeaning(
    string Number,
    string Name,
    string Arcana,
    string Suit,
    string Element,
    string Planet,
    string Upright,
    string Reversed,
    string Love,
    string Career,
    string YesNo,
    string Keywords);

/// <summary>
/// Deckaura 78 牌 12 维牌义静态数据集。
/// <para>22 大阿卡那：完整 RWS 牌义 + Golden Dame 元素/行星对应。</para>
/// <para>56 小阿卡那：四花色（权杖/圣杯/宝剑/星币）各 14 牌（Ace + 2~10 + Page/Knight/Queen/King），
/// 行星字段采用 Golden Dame Book T 的十度分野（decan）对应，宫廷牌采用元素子元素对应。</para>
/// <para>所有牌义取自 RWS（Rider-Waite-Smith）传统，未编造。</para>
/// </summary>
public static class TarotDeckData
{
    /// <summary>78 张牌的完整牌义数据（22 大阿卡那 + 56 小阿卡那）。</summary>
    public static readonly IReadOnlyList<TarotCardMeaning> Cards = BuildCards();

    /// <summary>按牌名（英文，区分大小写）查找牌义，未命中返回 null。</summary>
    public static TarotCardMeaning? FindByName(string name)
        => Cards.FirstOrDefault(c => c.Name == name);

    /// <summary>按牌名查找（不区分大小写），未命中返回 null。</summary>
    public static TarotCardMeaning? FindByNameIgnoreCase(string name)
        => Cards.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    private static List<TarotCardMeaning> BuildCards()
    {
        var list = new List<TarotCardMeaning>(78);
        AddMajorArcana(list);
        AddMinorArcana(list);
        return list;
    }

    private static void AddMajorArcana(List<TarotCardMeaning> list)
    {
        // 22 大阿卡那：元素/行星采用 Golden Dame 体系；牌义取 RWS 传统。
        list.Add(new("0", "The Fool", "Major", "Major", "Air", "Uranus",
            "New beginnings, innocence, spontaneity, free spirit",
            "Recklessness, risk-taking, foolishness",
            "New romantic beginnings, spontaneity, playful energy",
            "New opportunity, fresh start, taking a leap",
            "yes", "beginnings,innocence,spontaneity,free-spirit"));
        list.Add(new("1", "The Magician", "Major", "Major", "Air", "Mercury",
            "Manifestation, willpower, resourcefulness, skill",
            "Manipulation, untapped talents, deception",
            "Attraction, new romance, chemistry, manifestation",
            "Success, skill, achievement, resourcefulness",
            "yes", "manifestation,willpower,resourcefulness,skill"));
        list.Add(new("2", "The High Priestess", "Major", "Major", "Water", "Moon",
            "Intuition, mystery, subconscious, inner voice",
            "Secrets, disconnected from intuition, withdrawal",
            "Mystery, deep connection, hidden feelings",
            "Intuition needed, hidden factors, research",
            "unknown", "intuition,mystery,subconscious,inner-voice"));
        list.Add(new("3", "The Empress", "Major", "Major", "Earth", "Venus",
            "Abundance, nurturing, fertility, femininity",
            "Dependence, smothering, creative block",
            "Romance, commitment, fertility, nurturing",
            "Growth, creativity, success, abundance",
            "yes", "abundance,nurturing,fertility,femininity"));
        list.Add(new("4", "The Emperor", "Major", "Major", "Fire", "Aries",
            "Authority, structure, control, father figure",
            "Domination, rigidity, inflexibility",
            "Stability, commitment, structured relationship",
            "Authority, leadership, structure, stability",
            "yes", "authority,structure,control,leadership"));
        list.Add(new("5", "The Hierophant", "Major", "Major", "Earth", "Taurus",
            "Tradition, spiritual wisdom, conformity, education",
            "Rebellion, new approaches, unconventional",
            "Traditional relationship, marriage, guidance",
            "Mentorship, conformity, institution, tradition",
            "yes", "tradition,wisdom,conformity,education"));
        list.Add(new("6", "The Lovers", "Major", "Major", "Air", "Gemini",
            "Love, harmony, choices, alignment of values",
            "Disharmony, imbalance, misalignment of values",
            "Deep love, soulmate, harmony, alignment",
            "Partnership, alignment, meaningful choices",
            "yes", "love,harmony,choices,alignment"));
        list.Add(new("7", "The Chariot", "Major", "Major", "Water", "Cancer",
            "Determination, willpower, victory, control",
            "Lack of direction, aggression, scattered energy",
            "Driven relationship, overcoming obstacles together",
            "Victory, ambition, success, determination",
            "yes", "determination,willpower,victory,control"));
        list.Add(new("8", "Strength", "Major", "Major", "Fire", "Leo",
            "Courage, inner strength, patience, compassion",
            "Self-doubt, weakness, insecurity, lack of courage",
            "Confidence, patience, gentle strength in love",
            "Perseverance, inner strength, steady progress",
            "yes", "courage,inner-strength,patience,compassion"));
        list.Add(new("9", "The Hermit", "Major", "Major", "Earth", "Virgo",
            "Introspection, solitude, inner guidance, wisdom",
            "Isolation, loneliness, withdrawal",
            "Time alone, soul-searching, distance in relationship",
            "Independent work, mentorship, deep expertise",
            "unknown", "introspection,solitude,inner-guidance,wisdom"));
        list.Add(new("10", "Wheel of Fortune", "Major", "Major", "Fire", "Jupiter",
            "Cycles, fate, destiny, turning point",
            "Bad luck, resistance to change, breaking cycle",
            "Destiny, karmic connection, change in relationship",
            "Turning point, luck, cyclical change",
            "yes", "cycles,fate,destiny,turning-point"));
        list.Add(new("11", "Justice", "Major", "Major", "Air", "Libra",
            "Fairness, truth, cause and effect, law",
            "Unfairness, dishonesty, lack of accountability",
            "Honesty, fairness, balanced relationship",
            "Fairness, legal matters, truth, accountability",
            "yes", "fairness,truth,cause-and-effect,law"));
        list.Add(new("12", "The Hanged Man", "Major", "Major", "Water", "Neptune",
            "Surrender, new perspective, pause, sacrifice",
            "Stalling, indecision, resistance to sacrifice",
            "Pause, new perspective, sacrifice for love",
            "Pause, reassessment, new angle on work",
            "unknown", "surrender,new-perspective,pause,sacrifice"));
        list.Add(new("13", "Death", "Major", "Major", "Water", "Scorpio",
            "Endings, transformation, transition, change",
            "Resistance to change, stagnation, decay",
            "End of cycle, transformation, profound change",
            "End of phase, transformation, career change",
            "no", "endings,transformation,transition,change"));
        list.Add(new("14", "Temperance", "Major", "Major", "Fire", "Sagittarius",
            "Balance, moderation, patience, harmony",
            "Imbalance, excess, lack of harmony",
            "Balance, harmony, patience in relationship",
            "Balance, moderation, harmonious work",
            "yes", "balance,moderation,patience,harmony"));
        list.Add(new("15", "The Devil", "Major", "Major", "Earth", "Capricorn",
            "Attachment, bondage, addiction, materialism",
            "Release, freedom, reclaiming power",
            "Toxic bond, attachment, lust, codependency",
            "Bondage to work, materialism, unhealthy ambition",
            "no", "attachment,bondage,addiction,materialism"));
        list.Add(new("16", "The Tower", "Major", "Major", "Fire", "Mars",
            "Sudden change, upheaval, revelation, awakening",
            "Avoided disaster, resistance to change, fear",
            "Sudden breakup, revelation, crisis in relationship",
            "Sudden change, upheaval, collapse of structures",
            "no", "sudden-change,upheaval,revelation,awakening"));
        list.Add(new("17", "The Star", "Major", "Major", "Air", "Aquarius",
            "Hope, faith, renewal, guidance",
            "Despair, hopelessness, lack of faith",
            "Renewed hope, healing, faith in love",
            "Hope, inspiration, renewal, guidance",
            "yes", "hope,faith,renewal,guidance"));
        list.Add(new("18", "The Moon", "Major", "Major", "Water", "Pisces",
            "Illusion, intuition, subconscious, fear",
            "Confusion cleared, truth revealed, release of fear",
            "Uncertainty, hidden emotions, illusion in love",
            "Confusion, deception, uncertainty at work",
            "unknown", "illusion,intuition,subconscious,fear"));
        list.Add(new("19", "The Sun", "Major", "Major", "Fire", "Sun",
            "Joy, success, positivity, vitality",
            "Temporary sadness, lack of success, delayed joy",
            "Happiness, warmth, success in relationship",
            "Success, positivity, achievement, vitality",
            "yes", "joy,success,positivity,vitality"));
        list.Add(new("20", "Judgement", "Major", "Major", "Fire", "Pluto",
            "Rebirth, reckoning, awakening, renewal",
            "Self-doubt, refusal of call, stagnation",
            "Rebirth, forgiveness, reckoning in relationship",
            "Renewal, reckoning, calling, career rebirth",
            "yes", "rebirth,reckoning,awakening,renewal"));
        list.Add(new("21", "The World", "Major", "Major", "Earth", "Saturn",
            "Completion, achievement, fulfillment, wholeness",
            "Incompletion, shortcuts, lack of closure",
            "Fulfillment, completion, committed relationship",
            "Achievement, completion, success, fulfillment",
            "yes", "completion,achievement,fulfillment,wholeness"));
    }

    private static void AddMinorArcana(List<TarotCardMeaning> list)
    {
        AddWands(list);
        AddCups(list);
        AddSwords(list);
        AddPentacles(list);
    }

    private static void AddWands(List<TarotCardMeaning> list)
    {
        // 权杖（Fire），十度分野对应 Aries / Leo / Sagittarius 三星座。
        list.Add(new("Ace", "Ace of Wands", "Minor", "Wands", "Fire", "Fire (root)",
            "Inspiration, new opportunities, growth, potential",
            "Delays, lack of motivation, missed opportunity",
            "Passion, new romance, attraction",
            "New venture, inspiration, growth",
            "yes", "inspiration,new-opportunities,growth,potential"));
        list.Add(new("2", "Two of Wands", "Minor", "Wands", "Fire", "Mars in Aries",
            "Planning, decisions, discovery, future planning",
            "Fear of unknown, lack of planning, indecision",
            "Future planning, partnership decisions",
            "Planning, decisions, vision for future",
            "unknown", "planning,decisions,discovery,future-planning"));
        list.Add(new("3", "Three of Wands", "Minor", "Wands", "Fire", "Sun in Aries",
            "Expansion, foresight, progress, opportunities",
            "Delays, obstacles, limited vision",
            "Looking ahead, expansion, long-distance",
            "Expansion, foresight, business growth",
            "yes", "expansion,foresight,progress,opportunities"));
        list.Add(new("4", "Four of Wands", "Minor", "Wands", "Fire", "Venus in Aries",
            "Celebration, harmony, home, stability",
            "Lack of support, transition, home conflict",
            "Commitment, celebration, family harmony",
            "Stability, celebration, milestone reached",
            "yes", "celebration,harmony,home,stability"));
        list.Add(new("5", "Five of Wands", "Minor", "Wands", "Fire", "Saturn in Leo",
            "Conflict, competition, tension, rivalry",
            "Conflict resolution, cooperation, avoiding conflict",
            "Conflict, tension, disagreement",
            "Competition, conflict, tension at work",
            "unknown", "conflict,competition,tension,rivalry"));
        list.Add(new("6", "Six of Wands", "Minor", "Wands", "Fire", "Jupiter in Leo",
            "Victory, recognition, success, public praise",
            "Ego, fall from grace, lack of recognition",
            "Recognition, pride, successful partnership",
            "Victory, recognition, public success",
            "yes", "victory,recognition,success,public-praise"));
        list.Add(new("7", "Seven of Wands", "Minor", "Wands", "Fire", "Mars in Leo",
            "Perseverance, defense, standing your ground, resilience",
            "Giving up, overwhelm, inability to defend",
            "Defending position, boundaries in love",
            "Defending position, challenge, resilience",
            "yes", "perseverance,defense,standing-ground,resilience"));
        list.Add(new("8", "Eight of Wands", "Minor", "Wands", "Fire", "Mercury in Sagittarius",
            "Speed, movement, swift action, communication",
            "Delays, frustration, miscommunication",
            "Swift movement, news, fast communication",
            "Speed, progress, rapid communication",
            "yes", "speed,movement,swift-action,communication"));
        list.Add(new("9", "Nine of Wands", "Minor", "Wands", "Fire", "Moon in Sagittarius",
            "Resilience, persistence, last stand, boundaries",
            "Fatigue, burnout, defensiveness",
            "Defensiveness, boundaries, persistence",
            "Persistence, exhaustion, last push needed",
            "yes", "resilience,persistence,last-stand,boundaries"));
        list.Add(new("10", "Ten of Wands", "Minor", "Wands", "Fire", "Saturn in Sagittarius",
            "Burden, responsibility, hard work, stress",
            "Release, delegation, letting go of burden",
            "Burden, stress, responsibility in relationship",
            "Overwork, burden, stress, heavy load",
            "no", "burden,responsibility,hard-work,stress"));
        list.Add(new("Page", "Page of Wands", "Minor", "Wands", "Fire", "Earth of Fire",
            "Exploration, enthusiasm, free spirit, new ideas",
            "Lack of direction, scattered energy, impulsiveness",
            "Playful romance, new crush, enthusiasm",
            "New ideas, enthusiasm, exploration",
            "yes", "exploration,enthusiasm,free-spirit,new-ideas"));
        list.Add(new("Knight", "Knight of Wands", "Minor", "Wands", "Fire", "Fire of Fire",
            "Energy, passion, adventure, impulsiveness",
            "Recklessness, delays, frustration, scattered",
            "Passionate, adventurous, fast-moving romance",
            "Energy, ambition, adventurous pursuit",
            "yes", "energy,passion,adventure,impulsiveness"));
        list.Add(new("Queen", "Queen of Wands", "Minor", "Wands", "Fire", "Water of Fire",
            "Confidence, charisma, vibrancy, determination",
            "Insecurity, jealousy, demanding, self-doubt",
            "Confidence, charisma, vibrant presence",
            "Confidence, leadership, charismatic authority",
            "yes", "confidence,charisma,vibrancy,determination"));
        list.Add(new("King", "King of Wands", "Minor", "Wands", "Fire", "Air of Fire",
            "Leadership, vision, charisma, boldness",
            "Ruthlessness, domineering, impulsive, tyrant",
            "Bold, charismatic, natural leader in love",
            "Leadership, vision, bold authority",
            "yes", "leadership,vision,charisma,boldness"));
    }

    private static void AddCups(List<TarotCardMeaning> list)
    {
        // 圣杯（Water），十度分野对应 Cancer / Scorpio / Pisces 三星座。
        list.Add(new("Ace", "Ace of Cups", "Minor", "Cups", "Water", "Water (root)",
            "Compassion, love, new feelings, emotional awakening",
            "Emotional block, emptiness, repressed feelings",
            "New love, deep feelings, romance blossoming",
            "Fulfillment, new opportunity, inspiration",
            "yes", "compassion,love,new-feelings,emotional-awakening"));
        list.Add(new("2", "Two of Cups", "Minor", "Cups", "Water", "Venus in Cancer",
            "Partnership, unity, love, connection",
            "Imbalance, broken trust, separation",
            "Soulmate, partnership, mutual love",
            "Partnership, collaboration, unity",
            "yes", "partnership,unity,love,connection"));
        list.Add(new("3", "Three of Cups", "Minor", "Cups", "Water", "Mercury in Cancer",
            "Celebration, friendship, joy, community",
            "Overindulgence, gossip, isolation, third party",
            "Friendship, joy, celebration together",
            "Teamwork, celebration, social success",
            "yes", "celebration,friendship,joy,community"));
        list.Add(new("4", "Four of Cups", "Minor", "Cups", "Water", "Moon in Cancer",
            "Apathy, contemplation, reevaluation, boredom",
            "New interest, motivation, acceptance",
            "Boredom, apathy, contemplation",
            "Boredom, apathy, missed opportunity",
            "no", "apathy,contemplation,reevaluation,boredom"));
        list.Add(new("5", "Five of Cups", "Minor", "Cups", "Water", "Mars in Scorpio",
            "Loss, grief, regret, disappointment",
            "Acceptance, moving on, forgiveness",
            "Loss, regret, disappointment in love",
            "Loss, regret, disappointment at work",
            "no", "loss,grief,regret,disappointment"));
        list.Add(new("6", "Six of Cups", "Minor", "Cups", "Water", "Sun in Scorpio",
            "Nostalgia, memories, childhood, innocence",
            "Stuck in past, moving forward, naive",
            "Nostalgia, old flame, innocent love",
            "Nostalgia, comfort, past patterns",
            "yes", "nostalgia,memories,childhood,innocence"));
        list.Add(new("7", "Seven of Cups", "Minor", "Cups", "Water", "Venus in Scorpio",
            "Choices, illusion, wishful thinking, confusion",
            "Clarity, decision, reality check",
            "Illusion, choices, confusion in love",
            "Choices, illusion, confusion at work",
            "unknown", "choices,illusion,wishful-thinking,confusion"));
        list.Add(new("8", "Eight of Cups", "Minor", "Cups", "Water", "Saturn in Pisces",
            "Walking away, withdrawal, seeking deeper meaning, discontent",
            "Fear of change, return, avoidance",
            "Walking away, discontent, seeking more",
            "Quitting, seeking more, walking away",
            "no", "walking-away,withdrawal,seeking-meaning,discontent"));
        list.Add(new("9", "Nine of Cups", "Minor", "Cups", "Water", "Jupiter in Pisces",
            "Contentment, satisfaction, wishes fulfilled, happiness",
            "Dissatisfaction, greed, wish unfulfilled",
            "Satisfaction, contentment, happiness",
            "Satisfaction, success, wish fulfilled",
            "yes", "contentment,satisfaction,wishes-fulfilled,happiness"));
        list.Add(new("10", "Ten of Cups", "Minor", "Cups", "Water", "Mars in Pisces",
            "Harmony, joy, family, lasting happiness",
            "Disconnection, misalignment, broken family",
            "Harmony, family, lasting happiness",
            "Harmony, fulfillment, joy at work",
            "yes", "harmony,joy,family,lasting-happiness"));
        list.Add(new("Page", "Page of Cups", "Minor", "Cups", "Water", "Earth of Water",
            "Creativity, intuition, new messages, sensitivity",
            "Emotional immaturity, escapism, moodiness",
            "New romance, sweet gesture, intuitive crush",
            "Creativity, intuition, new idea",
            "yes", "creativity,intuition,new-messages,sensitivity"));
        list.Add(new("Knight", "Knight of Cups", "Minor", "Cups", "Water", "Fire of Water",
            "Romance, idealism, charm, following the heart",
            "Moodiness, disappointment, unrealistic, jealousy",
            "Romance, charm, idealism, proposal",
            "Following heart, creativity, charming approach",
            "yes", "romance,idealism,charm,following-heart"));
        list.Add(new("Queen", "Queen of Cups", "Minor", "Cups", "Water", "Water of Water",
            "Compassion, calm, emotional security, intuitive",
            "Emotional insecurity, dependence, overwhelm",
            "Compassion, emotional security, caring",
            "Compassion, intuition, supportive role",
            "yes", "compassion,calm,emotional-security,intuitive"));
        list.Add(new("King", "King of Cups", "Minor", "Cups", "Water", "Air of Water",
            "Emotional maturity, calm, diplomacy, balance",
            "Moodiness, manipulation, volatility, coldness",
            "Emotional maturity, balance, calm partner",
            "Diplomacy, balance, emotional maturity",
            "yes", "emotional-maturity,calm,diplomacy,balance"));
    }

    private static void AddSwords(List<TarotCardMeaning> list)
    {
        // 宝剑（Air），十度分野对应 Gemini / Libra / Aquarius 三星座。
        list.Add(new("Ace", "Ace of Swords", "Minor", "Swords", "Air", "Air (root)",
            "Clarity, breakthrough, truth, new ideas",
            "Confusion, misinformation, poor judgment",
            "Clear communication, truth, new understanding",
            "Clarity, breakthrough, truth revealed",
            "yes", "clarity,breakthrough,truth,new-ideas"));
        list.Add(new("2", "Two of Swords", "Minor", "Swords", "Air", "Moon in Libra",
            "Indecision, stalemate, blocked emotions, avoidance",
            "Decision made, clarity, release of block",
            "Indecision, stalemate, avoidance",
            "Indecision, stalemate, avoidance at work",
            "unknown", "indecision,stalemate,blocked-emotions,avoidance"));
        list.Add(new("3", "Three of Swords", "Minor", "Swords", "Air", "Saturn in Libra",
            "Heartbreak, sorrow, grief, pain",
            "Healing, forgiveness, moving on",
            "Heartbreak, sorrow, painful love",
            "Sorrow, conflict, painful situation",
            "no", "heartbreak,sorrow,grief,pain"));
        list.Add(new("4", "Four of Swords", "Minor", "Swords", "Air", "Jupiter in Libra",
            "Rest, recovery, contemplation, meditation",
            "Restlessness, burnout, forced rest, stagnation",
            "Rest, space, contemplation in love",
            "Rest, recovery, needed pause",
            "unknown", "rest,recovery,contemplation,meditation"));
        list.Add(new("5", "Five of Swords", "Minor", "Swords", "Air", "Venus in Aquarius",
            "Conflict, defeat, winning at all costs, betrayal",
            "Reconciliation, forgiveness, regret",
            "Conflict, betrayal, tension in love",
            "Conflict, defeat, betrayal at work",
            "no", "conflict,defeat,winning-at-all-costs,betrayal"));
        list.Add(new("6", "Six of Swords", "Minor", "Swords", "Air", "Mercury in Aquarius",
            "Transition, moving on, leaving behind, travel",
            "Resistance to change, unfinished business, delay",
            "Moving on, transition, distance",
            "Transition, moving on, travel for work",
            "yes", "transition,moving-on,leaving-behind,travel"));
        list.Add(new("7", "Seven of Swords", "Minor", "Swords", "Air", "Moon in Aquarius",
            "Deception, stealth, strategy, trickery",
            "Confession, coming clean, conscience",
            "Deception, secrecy, dishonesty",
            "Deception, strategy, secrecy at work",
            "no", "deception,stealth,strategy,trickery"));
        list.Add(new("8", "Eight of Swords", "Minor", "Swords", "Air", "Jupiter in Gemini",
            "Restriction, feeling trapped, powerlessness, self-limiting",
            "Freedom, release, new perspective, empowerment",
            "Feeling trapped, restriction, powerlessness",
            "Restriction, trapped, limited at work",
            "no", "restriction,feeling-trapped,powerlessness,self-limiting"));
        list.Add(new("9", "Nine of Swords", "Minor", "Swords", "Air", "Mars in Gemini",
            "Anxiety, nightmares, fear, worry",
            "Release of fear, hope, facing fears",
            "Anxiety, worry, fear in love",
            "Anxiety, stress, worry at work",
            "no", "anxiety,nightmares,fear,worry"));
        list.Add(new("10", "Ten of Swords", "Minor", "Swords", "Air", "Sun in Gemini",
            "Painful endings, betrayal, rock bottom, collapse",
            "Recovery, regeneration, survival, worst passed",
            "Painful ending, betrayal, collapse",
            "Painful ending, collapse, rock bottom",
            "no", "painful-endings,betrayal,rock-bottom,collapse"));
        list.Add(new("Page", "Page of Swords", "Minor", "Swords", "Air", "Earth of Air",
            "Curiosity, new ideas, mental energy, vigilance",
            "Deception, haste, scattered thoughts, gossip",
            "Curiosity, new communication, vigilance",
            "Curiosity, new ideas, vigilance at work",
            "unknown", "curiosity,new-ideas,mental-energy,vigilance"));
        list.Add(new("Knight", "Knight of Swords", "Minor", "Swords", "Air", "Fire of Air",
            "Ambition, drive, fast action, impulsiveness",
            "Recklessness, scattered energy, impulsive, no direction",
            "Fast-moving, impulsive, driven romance",
            "Ambition, drive, fast action at work",
            "yes", "ambition,drive,fast-action,impulsiveness"));
        list.Add(new("Queen", "Queen of Swords", "Minor", "Swords", "Air", "Water of Air",
            "Clarity, independence, sharp mind, directness",
            "Coldness, bitterness, harshness, cruelty",
            "Independence, clarity, directness in love",
            "Clarity, independence, sharp mind at work",
            "yes", "clarity,independence,sharp-mind,directness"));
        list.Add(new("King", "King of Swords", "Minor", "Swords", "Air", "Air of Air",
            "Authority, intellect, truth, clarity, leadership",
            "Manipulation, tyranny, cruelty, harshness",
            "Logical, direct, intellectual partner",
            "Authority, intellect, leadership at work",
            "yes", "authority,intellect,truth,clarity,leadership"));
    }

    private static void AddPentacles(List<TarotCardMeaning> list)
    {
        // 星币（Earth），十度分野对应 Taurus / Virgo / Capricorn 三星座。
        list.Add(new("Ace", "Ace of Pentacles", "Minor", "Pentacles", "Earth", "Earth (root)",
            "Opportunity, prosperity, new venture, security",
            "Missed opportunity, scarcity, bad investment",
            "Solid foundation, security, new beginning",
            "New opportunity, prosperity, security",
            "yes", "opportunity,prosperity,new-venture,security"));
        list.Add(new("2", "Two of Pentacles", "Minor", "Pentacles", "Earth", "Jupiter in Capricorn",
            "Balance, adaptation, prioritization, juggling",
            "Imbalance, disorganization, overwhelm",
            "Juggling priorities, balance, adaptation",
            "Juggling, balance, adaptability at work",
            "unknown", "balance,adaptation,prioritization,juggling"));
        list.Add(new("3", "Three of Pentacles", "Minor", "Pentacles", "Earth", "Mars in Capricorn",
            "Teamwork, collaboration, building, skill",
            "Lack of teamwork, disorganization, conflict",
            "Teamwork, building together, collaboration",
            "Teamwork, collaboration, skill recognized",
            "yes", "teamwork,collaboration,building,skill"));
        list.Add(new("4", "Four of Pentacles", "Minor", "Pentacles", "Earth", "Sun in Capricorn",
            "Security, holding on, control, stability",
            "Greed, materialism, letting go, control issues",
            "Holding on, security, possessiveness",
            "Security, stability, holding on at work",
            "unknown", "security,holding-on,control,stability"));
        list.Add(new("5", "Five of Pentacles", "Minor", "Pentacles", "Earth", "Mercury in Taurus",
            "Hardship, financial loss, isolation, insecurity",
            "Recovery, improvement, finding help, hope",
            "Isolation, hardship, insecurity in love",
            "Hardship, financial loss, struggle",
            "no", "hardship,financial-loss,isolation,insecurity"));
        list.Add(new("6", "Six of Pentacles", "Minor", "Pentacles", "Earth", "Moon in Taurus",
            "Generosity, charity, giving, fairness",
            "Unequal relationship, debt, strings attached",
            "Generosity, giving, fairness in love",
            "Generosity, fair exchange, charity",
            "yes", "generosity,charity,giving,fairness"));
        list.Add(new("7", "Seven of Pentacles", "Minor", "Pentacles", "Earth", "Saturn in Taurus",
            "Patience, investment, hard work paying off, waiting",
            "Impatience, wasted effort, no return",
            "Patience, investment, waiting in love",
            "Patience, investment, hard work paying off",
            "unknown", "patience,investment,hard-work-paying-off,waiting"));
        list.Add(new("8", "Eight of Pentacles", "Minor", "Pentacles", "Earth", "Sun in Virgo",
            "Skill, mastery, diligence, hard work, craft",
            "Perfectionism, lack of focus, no ambition",
            "Dedication, effort, craft in love",
            "Skill, mastery, hard work, craft",
            "yes", "skill,mastery,diligence,hard-work,craft"));
        list.Add(new("9", "Nine of Pentacles", "Minor", "Pentacles", "Earth", "Venus in Virgo",
            "Abundance, independence, self-sufficiency, luxury",
            "Over-investment in work, false success, dependence",
            "Independence, self-sufficiency, abundance",
            "Abundance, success, independence at work",
            "yes", "abundance,independence,self-sufficiency,luxury"));
        list.Add(new("10", "Ten of Pentacles", "Minor", "Pentacles", "Earth", "Mercury in Virgo",
            "Wealth, legacy, family, long-term security",
            "Loss of family wealth, financial failure, instability",
            "Family, legacy, long-term commitment",
            "Wealth, legacy, security at work",
            "yes", "wealth,legacy,family,long-term-security"));
        list.Add(new("Page", "Page of Pentacles", "Minor", "Pentacles", "Earth", "Earth of Earth",
            "Ambition, study, new opportunities, diligence",
            "Lack of progress, procrastination, missed opportunity",
            "New opportunity, ambition, study in love",
            "Study, ambition, new opportunity at work",
            "yes", "ambition,study,new-opportunities,diligence"));
        list.Add(new("Knight", "Knight of Pentacles", "Minor", "Pentacles", "Earth", "Fire of Earth",
            "Hard work, diligence, routine, patience, reliability",
            "Boredom, stagnation, laziness, perfectionism",
            "Reliable, slow-moving, steady partner",
            "Hard work, diligence, routine at work",
            "yes", "hard-work,diligence,routine,patience,reliability"));
        list.Add(new("Queen", "Queen of Pentacles", "Minor", "Pentacles", "Earth", "Water of Earth",
            "Nurturing, practical, security, abundance, grounded",
            "Work-home imbalance, smothering, financial dependence",
            "Nurturing, security, grounded love",
            "Nurturing, security, practical at work",
            "yes", "nurturing,practical,security,abundance,grounded"));
        list.Add(new("King", "King of Pentacles", "Minor", "Pentacles", "Earth", "Air of Earth",
            "Wealth, success, stability, abundance, leadership",
            "Greed, materialism, domineering, stubbornness",
            "Stability, wealth, security in love",
            "Wealth, success, leadership at work",
            "yes", "wealth,success,stability,abundance,leadership"));
    }
}
