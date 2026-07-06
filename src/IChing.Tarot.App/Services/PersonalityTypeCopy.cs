namespace IChing.Tarot.App.Services;

using IChing.Tarot.App.Models;

public sealed record PersonalityTypeReport(
    string Title,
    string Summary,
    IReadOnlyList<PersonalityReportSection> Sections);

public static class PersonalityTypeCopy
{
    public sealed record TypeCopy(string Title, string Summary, string Detail);

    public static TypeCopy Mbti16(string type) =>
        Mbti16Map.TryGetValue(type, out var copy)
            ? copy
            : new TypeCopy(type, "你的四维组合较为独特。", "建议结合具体情境理解，本结果仅供娱乐参考。");

    public static PersonalityTypeReport Mbti16Report(string type) =>
        Mbti16FullMap.TryGetValue(type, out var report)
            ? report
            : Fallback(type, "你的四维组合较为独特。");

    public static TypeCopy Enneagram(int type) =>
        EnneagramMap.TryGetValue(type, out var copy)
            ? copy
            : new TypeCopy($"类型 {type}", "动机倾向测试结果。", "仅供娱乐参考。");

    public static PersonalityTypeReport EnneagramReport(
        int primary,
        IReadOnlyList<(int Type, int Score)> ranked)
    {
        var baseReport = EnneagramFullMap.TryGetValue(primary, out var report)
            ? report
            : Fallback(primary.ToString(), "动机倾向测试结果。");

        var sections = new List<PersonalityReportSection>
        {
            new("得分分布", FormatEnneagramDistribution(ranked)),
            new("侧翼与次型", FormatEnneagramWing(primary, ranked))
        };
        sections.AddRange(baseReport.Sections);
        return new PersonalityTypeReport(baseReport.Title, baseReport.Summary, sections);
    }

    public static TypeCopy Holland(string code, IReadOnlyList<(string Key, int Score)> ranked)
    {
        var primary = ranked.FirstOrDefault().Key;
        var names = string.Join(" · ", ranked.Take(3).Select(x => $"{HollandName(x.Key)}({x.Key})"));
        return new TypeCopy(
            $"霍兰德 {code}",
            $"前三兴趣：{names}。{HollandName(primary)}维度最为突出。",
            HollandPrimaryDetail(primary));
    }

    public static PersonalityTypeReport HollandReport(
        string code,
        IReadOnlyList<(string Key, int Score)> ranked)
    {
        var primary = ranked.FirstOrDefault().Key;
        var names = string.Join(" · ", ranked.Take(3).Select(x => $"{HollandName(x.Key)}({x.Key})"));
        var summary = $"前三兴趣码 {code}：{names}。";
        var sections = new List<PersonalityReportSection>
        {
            new("兴趣概览", $"{HollandName(primary)}（{primary}）在你的回答中最为突出。{HollandPrimaryDetail(primary)}"),
            new("组合解读", HollandComboDetail(code)),
            new("六维排序", string.Join("\n", ranked.Select(x => $"· {HollandName(x.Key)}（{x.Key}）：{x.Score} 分"))),
            new("探索方向", HollandCareerCombo(code)),
            new("发展建议", HollandGrowth(primary, ranked))
        };
        return new PersonalityTypeReport($"霍兰德 {code}", summary, sections);
    }

    public static string HollandDimensionName(string key) => HollandName(key);

    private static string FormatEnneagramDistribution(IReadOnlyList<(int Type, int Score)> ranked) =>
        string.Join("\n", ranked.Select(x => $"· {x.Type} 号：{x.Score} 分"));

    private static string FormatEnneagramWing(int primary, IReadOnlyList<(int Type, int Score)> ranked)
    {
        var left = primary == 1 ? 9 : primary - 1;
        var right = primary == 9 ? 1 : primary + 1;
        var leftScore = ranked.FirstOrDefault(x => x.Type == left).Score;
        var rightScore = ranked.FirstOrDefault(x => x.Type == right).Score;
        var wing = leftScore >= rightScore ? left : right;
        var secondary = ranked.Count > 1 ? ranked[1].Type : primary;
        return $"主型 {primary} 号；侧翼倾向 {wing} 号（相邻类型中得分更高的一方）；次型参考 {secondary} 号。侧翼会让主型在表达上更柔和或更锐利，可结合两者理解自己。";
    }

    private static PersonalityTypeReport Fallback(string code, string summary) =>
        new(code, summary,
        [
            new("说明", "本结果基于简化量表，仅供娱乐与自我觉察参考。")
        ]);

    private static PersonalityTypeReport M(
        string title,
        string summary,
        string overview,
        string strengths,
        string weaknesses,
        string relations,
        string career,
        string growth) =>
        new(title, summary,
        [
            new("类型概览", overview),
            new("核心优势", strengths),
            new("潜在盲点", weaknesses),
            new("人际与亲密", relations),
            new("职业与发展", career),
            new("成长建议", growth)
        ]);

    private static string HollandComboDetail(string code)
    {
        if (code.Length < 2)
        {
            return HollandPrimaryDetail(code);
        }

        return code switch
        {
            var c when c.Contains("RI") => "研究 + 实操型：既爱动手验证，也享受独立分析，适合工程研发、实验技术、数据现场等交叉岗位。",
            var c when c.Contains("IA") => "研究 + 艺术型：理性与审美并存，适合 UX 研究、科学可视化、交互设计、内容策略等。",
            var c when c.Contains("AS") => "艺术 + 社会型：表达欲与助人意结合，适合教育内容、品牌传播、社区运营、心理咨询辅助等。",
            var c when c.Contains("SE") => "社会 + 企业型：善于连接人与目标，适合培训、人力、客户成功、项目协调等。",
            var c when c.Contains("EC") => "企业 + 常规型：推进力与流程感兼备，适合商务运营、项目管理、销售管理、创业早期执行。",
            var c when c.Contains("RC") => "现实 + 常规型：重稳定与可执行，适合制造、运维、质检、设备管理、现场督导等。",
            _ => $"以 {HollandName(code[0].ToString())} 为主导，辅以 {string.Concat(code.Skip(1).Select(c => HollandName(c.ToString())))} 兴趣，适合在复合型岗位中发挥。"
        };
    }

    private static string HollandCareerCombo(string code) => code switch
    {
        var c when c.StartsWith("R") => "技术实施、工程维护、智能制造、农业技术、应急保障、硬件测试。",
        var c when c.StartsWith("I") => "数据分析、科研助理、策略研究、医学检验、情报整理、算法应用。",
        var c when c.StartsWith("A") => "视觉设计、内容创作、产品体验、音乐影视、品牌创意、自由职业创作。",
        var c when c.StartsWith("S") => "教育培训、心理咨询、医护辅助、社工、客户陪伴、社区服务。",
        var c when c.StartsWith("E") => "销售商务、创业、市场拓展、团队管理、公关活动、投资拓展。",
        var c when c.StartsWith("C") => "财务审计、行政运营、法务合规、档案管理、供应链、政务办事。",
        _ => "结合前三码，优先选择能同时满足主导兴趣与辅助兴趣的岗位。"
    };

    private static string HollandGrowth(string primary, IReadOnlyList<(string Key, int Score)> ranked)
    {
        var gap = ranked.LastOrDefault();
        return $"发挥 {HollandName(primary)} 优势的同时，适度补强 {HollandName(gap.Key)}（{gap.Key}）相关能力，可避免职业路径过于单一。定期复盘：当前工作有多少比例在用它？" +
               "\n本结果基于简化 RIASEC 量表，非正式职业测评。";
    }

    private static string HollandName(string key) => key switch
    {
        "R" => "现实型",
        "I" => "研究型",
        "A" => "艺术型",
        "S" => "社会型",
        "E" => "企业型",
        "C" => "常规型",
        _ => key
    };

    private static string HollandPrimaryDetail(string key) => key switch
    {
        "R" => "你偏好动手、实操与可见成果，享受工具、设备与现场反馈。",
        "I" => "你偏好分析、研究与独立探索，在追问「为什么」中获得满足。",
        "A" => "你偏好表达、审美与创造，重视作品独特性与自我风格。",
        "S" => "你偏好助人与连接，在支持他人、营造关系中感到有价值。",
        "E" => "你偏好影响、组织与推进，享受目标、竞争与资源整合。",
        "C" => "你偏好秩序、流程与可靠执行，在规范与精确中获得安心。",
        _ => "结合前三码理解你的兴趣组合。"
    };

    private static readonly Dictionary<string, TypeCopy> Mbti16Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["INTJ"] = new("INTJ · 建筑师", "独立、系统思维、长期规划。", "你擅长从复杂信息中抽象模型，重视效率与自主。"),
        ["INTP"] = new("INTP · 逻辑学家", "好奇、分析、概念探索。", "你喜欢追问原理与可能性，思维灵活。"),
        ["ENTJ"] = new("ENTJ · 指挥官", "果断、组织、目标导向。", "你天然推动事情发生，善于统筹资源。"),
        ["ENTP"] = new("ENTP · 辩论家", "机敏、创新、挑战常规。", "你享受头脑碰撞与新点子，适应变化快。"),
        ["INFJ"] = new("INFJ · 提倡者", "洞察、理想、深度共情。", "你关注意义与人心，有清晰内在价值。"),
        ["INFP"] = new("INFP · 调停者", "真诚、价值驱动、富想象力。", "你重视自我表达与和谐，对美与义敏感。"),
        ["ENFJ"] = new("ENFJ · 主人公", "鼓舞、协调、看见他人潜能。", "你擅长连接人与愿景，天然导师气质。"),
        ["ENFP"] = new("ENFP · 竞选者", "热情、联想、人际感染力。", "你能量外放，对新体验开放。"),
        ["ISTJ"] = new("ISTJ · 物流师", "可靠、务实、重规则。", "你重视责任与事实，执行稳定。"),
        ["ISFJ"] = new("ISFJ · 守卫者", "细致、守护、默默付出。", "你关心身边人的需要，记忆与细节强。"),
        ["ESTJ"] = new("ESTJ · 总经理", "高效、标准、结果导向。", "你擅长建立流程与秩序，推动落地。"),
        ["ESFJ"] = new("ESFJ · 执政官", "温暖、组织、重和谐。", "你关注群体氛围与礼仪，乐于服务。"),
        ["ISTP"] = new("ISTP · 鉴赏家", "冷静、动手、现场解决问题。", "你偏好直接体验与工具感，临场反应快。"),
        ["ISFP"] = new("ISFP · 探险家", "感受、审美、当下体验。", "你温和而有个性，用作品或行动表达。"),
        ["ESTP"] = new("ESTP · 企业家", "行动、冒险、读局快。", "你享受挑战与即时反馈，社交能量足。"),
        ["ESFP"] = new("ESFP · 表演者", "活力、感官、带来欢乐。", "你让环境变轻，重视体验与互动。")
    };

    private static readonly Dictionary<string, PersonalityTypeReport> Mbti16FullMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["INTJ"] = M("INTJ · 建筑师", "独立、系统思维、长期规划。",
                "INTJ 倾向于用框架理解世界，重视逻辑一致与长期目标。你常在别人还在讨论细节时，已在脑中完成整体推演。",
                "战略眼光、深度专注、自主高效、标准清晰、善于优化系统。",
                "可能显得疏离或挑剔；对低效与重复耐心不足；情感需求表达偏少，易被误解为冷漠。",
                "重视精神契合与独立空间。你需要被理解「为什么」，而非只被安慰。亲密关系中宜主动分享感受，避免用理性代替陪伴。",
                "战略、产品架构、科研、投资分析、技术管理、咨询等需长期深度思考的岗位。适合有自主权、目标清晰的环境。",
                "练习「足够好」而非完美；定期与信任的人交流情绪；把宏大计划拆成可交付的小步，减少孤立感。"),
            ["INTP"] = M("INTP · 逻辑学家", "好奇、分析、概念探索。",
                "INTP 热爱追问原理，在概念、模型与可能性中如鱼得水。你更关心「是否成立」，而非「是否流行」。",
                "分析力强、思维开放、创新联想、客观冷静、学习曲线陡峭。",
                "易拖延落地；对日常琐事缺乏耐心；社交能量有限时可能突然「下线」。",
                "需要智力上的尊重与自由。你更擅长深度一对一，而非表面寒暄。表达关心时可多说具体行动，而不只停留在分析。",
                "研发、算法、哲学/写作、系统架构、游戏设计、学术与技术交叉领域。",
                "设定最小可行截止日期；用外部结构（清单、伙伴）辅助执行；允许自己偶尔从纯逻辑中抽离，体验身体与感官。"),
            ["ENTJ"] = M("ENTJ · 指挥官", "果断、组织、目标导向。",
                "ENTJ 天生看见目标与路径，擅长动员资源、推动结果。你对模糊与拖延容忍度低，习惯做那个「把事定下来」的人。",
                "领导力、决断力、系统规划、抗压、目标感强。",
                "可能过于直接；忽视他人情绪；把自我价值过度绑定在成就上。",
                "欣赏有主见、能成长的伴侣。你需要学会在冲突中先倾听，再推进；亲密不是项目，不必事事最优解。",
                "管理、创业、战略、销售总监、运营负责人、政治与公共事务等高压高产出角色。",
                "刻意练习共情表达；区分「控制局面」与「控制人」；留时间给无 KPI 的关系与休息。"),
            ["ENTP"] = M("ENTP · 辩论家", "机敏、创新、挑战常规。",
                "ENTP 在头脑风暴与破局中最兴奋，讨厌陈规与无聊。你像永动机一样连接点子，但完成闭环需要刻意训练。",
                "创意、应变、口才、跨界联想、挑战权威的勇气。",
                "易分散、开坑过多；对细节与收尾厌倦；争论时可能伤人而不自知。",
                "需要新鲜感和智力火花。长期关系里要有共同探索的项目；学会在重要话题上「少赢一点，多懂一点」。",
                "创业、产品、市场、咨询、媒体、投资、任何变化快、需创意的领域。",
                "一次只追一个主项目；用番茄钟或搭档制完成落地；争论前先问：这是在解决问题，还是在享受辩论？"),
            ["INFJ"] = M("INFJ · 提倡者", "洞察、理想、深度共情。",
                "INFJ 对人心与意义高度敏感，常扮演「看透却不说破」的角色。你有清晰的内在价值，并希望世界更接近它。",
                "洞察、共情、写作表达、长期主义、道德感、引导他人成长。",
                "易情感耗竭；过度理想化他人；难说「不」，边界模糊。",
                "追求灵魂层面的连接。你需要安全空间表达脆弱；避免拯救者角色，关系是双向滋养。",
                "咨询、心理、教育、写作、用户研究、公益、品牌叙事、人力资源发展等。",
                "设定情感边界；把洞察写下来的习惯；接受「世界不会完全符合理想」仍可行动。"),
            ["INFP"] = M("INFP · 调停者", "真诚、价值驱动、富想象力。",
                "INFP 以内在价值导航，对美、义与真实高度敏感。你可能安静，但内心世界极其丰富。",
                "真诚、创意、共情、适应多元价值、文字与审美表达。",
                "易逃避冲突；对批评过度内化；现实节奏与结构感偏弱。",
                "需要被完整接纳，而非被「改造」。表达需求宜具体，别只期待对方读懂沉默。",
                "创作、设计、心理咨询、内容、非营利、教育、自由职业与远程协作。",
                "建立轻量日常结构；把感受转化为创作；练习小步冲突，而非累积后爆发。"),
            ["ENFJ"] = M("ENFJ · 主人公", "鼓舞、协调、看见他人潜能。",
                "ENFJ 像天然导师，善于点燃群体愿景并照顾氛围。你容易成为他人的情感支柱。",
                "鼓舞、组织、沟通、洞察他人需求、责任感、团队凝聚。",
                "过度承担他人情绪；忽视自身需要；怕让人失望而难拒绝。",
                "重视成长与深度交流。你需要伴侣也看见你的需要；别把「被需要」当成唯一价值来源。",
                "培训、人力、教育、公关、社区、管理、客户成功、活动统筹。",
                "每周留「只为自己」的时间；拒绝练习；允许别人对你失望而不崩塌。"),
            ["ENFP"] = M("ENFP · 竞选者", "热情、联想、人际感染力。",
                "ENFP 能量外放，对新人与新体验开放。你能迅速点亮场域，但也易被下一个可能性吸引。",
                "热情、创意、人际连接、适应变化、感染力强。",
                "专注分散；逃避繁琐；情绪起伏；承诺过多后压力山大。",
                "需要自由与真诚。关系里要有玩乐也有深度；学会在兴奋期过后仍愿意维护日常。",
                "市场、媒体、创业、演艺、公关、产品、社会创新、教育。",
                "用日历锁定深度工作时间；完成比完美更重要；定期清理承诺清单。"),
            ["ISTJ"] = M("ISTJ · 物流师", "可靠、务实、重规则。",
                "ISTJ 以责任与事实为锚，做事有始有终。你信任经验与流程，是团队里「一定能交付」的人。",
                "可靠、细致、执行力、诚信、长期稳定、规则意识。",
                "对变化适应慢；表达情感含蓄；可能显得固执或保守。",
                "用行动表达爱更自然。伴侣需要理解你的「沉默负责」；你也需要练习说出 appreciation。",
                "财务、法务、运营、工程管理、政务、医疗行政、供应链、审计。",
                "为变化预留缓冲；尝试小步试错；定期表达感谢与柔软，而不只解决问题。"),
            ["ISFJ"] = M("ISFJ · 守卫者", "细致、守护、默默付出。",
                "ISFJ 记得细节与生日，在照顾他人中体现价值。你温和可靠，常是幕后稳定力量。",
                "体贴、记忆细节、服务精神、忠诚、实务能力。",
                "难拒绝；压抑自己需求；怕冲突；变化来临时焦虑。",
                "需要安全感与肯定。宜主动说出「我需要…」；别把付出变成隐性契约。",
                "护理、行政、客户成功、教务、人事、零售服务、家庭相关支持岗位。",
                "练习边界；小步表达不同意见；你的需要与他人的需要同等重要。"),
            ["ESTJ"] = M("ESTJ · 总经理", "高效、标准、结果导向。",
                "ESTJ 擅长建秩序、定标准、推执行。你相信清晰规则带来公平与效率。",
                "组织、执行、决策、负责、流程优化、公开表达立场。",
                "可能显得强势；情感细腻度不足；对非传统做法耐心有限。",
                "重视承诺与家庭责任。学会软化措辞；听完后先反映感受，再给方案。",
                "管理、制造、军警政务、商务、项目总监、连锁运营。",
                "区分「事不对」与「人不对」；留时间给无效率的陪伴；允许例外存在。"),
            ["ESFJ"] = M("ESFJ · 执政官", "温暖、组织、重和谐。",
                "ESFJ 关注群体感受与传统，乐于张罗与照顾。你是聚会里的粘合剂。",
                "社交、组织活动、关怀、合作、实务跟进、氛围营造。",
                "过度在意评价；难处理批评；可能牺牲自我换和谐。",
                "需要被看见与感谢。选择能回应你付出的关系；冲突不等于关系破裂。",
                "活动、人力、客户关系、医疗护理、教育、社区、零售管理。",
                "练习接受不同意见；独处充电；你的价值不只来自「被所有人喜欢」。"),
            ["ISTP"] = M("ISTP · 鉴赏家", "冷静、动手、现场解决问题。",
                "ISTP 在实操与危机中最佳状态，相信亲手验证胜过空谈。你简洁、独立、观察敏锐。",
                "临场反应、动手、逻辑、冷静、工具感、独立。",
                "长期规划弱；情感表达少；易 bored 于重复文书与会议。",
                "需要空间与低 drama。用共同活动建立连接；重要话宜说出口，别默认对方懂。",
                "工程、维修、应急、体育、技术现场、侦查、手工艺、开发运维。",
                "为长期目标设提醒；练习提前沟通；偶尔分享内心，而不只分享解决方案。"),
            ["ISFP"] = M("ISFP · 探险家", "感受、审美、当下体验。",
                "ISFP 温和而独特，用审美与行动表达自我。你活在当下，对美与和谐高度敏感。",
                "审美、共情、灵活、动手创作、价值观清晰、温和包容。",
                "难长期规划；怕冲突；易拖延；在 harsh 环境中退缩。",
                "需要温柔与尊重边界。宜主动争取想要的，而不只等待被邀请。",
                "设计、摄影、护理、厨艺、音乐、时尚、动物相关、自由创作。",
                "把大目标拆小；练习说不；你的温柔不等于没有立场。"),
            ["ESTP"] = M("ESTP · 企业家", "行动、冒险、读局快。",
                "ESTP 在动态环境中如鱼得水，喜欢即时反馈与真实体验。你能迅速读局并出手。",
                "行动、社交、应变、说服、现实感、享受挑战。",
                "冲动；耐心不足；忽视长远风险； boredom 于抽象理论。",
                "需要活力与真实。长期关系要练习「慢下来」；承诺前想三步后果。",
                "销售、交易、创业、现场、体育、应急、演艺、商务拓展。",
                "重大决定前强制 24 小时冷静；建立储蓄与风险缓冲；倾听伴侣对稳定的需求。"),
            ["ESFP"] = M("ESFP · 表演者", "活力、感官、带来欢乐。",
                "ESFP 让场域变轻，重视当下体验与人群连接。你是天生的气氛担当。",
                "活力、审美、人际、即兴、乐观、实践学习。",
                "难做长期规划；逃避负面话题；财务与细节易疏忽。",
                "需要被欣赏与一起玩乐。也要练习讨论严肃话题；深度不一定会杀死轻松。",
                "演艺、活动、零售、美妆、旅游、新媒体、客户服务、培训。",
                "自动储蓄/记账；设定年度主题目标；在快乐与责任间找平衡。")
        };

    private static readonly Dictionary<int, TypeCopy> EnneagramMap = new()
    {
        [1] = new("1 号 · 改革者", "追求正确与改进，内在有清晰标准。", "你重视原则与质量，常看见可优化之处。"),
        [2] = new("2 号 · 助人者", "渴望被需要，擅长感知他人需求。", "你温暖、慷慨，关系中的支持者。"),
        [3] = new("3 号 · 成就者", "目标感强，重视形象与效率。", "你适应力强，擅长把事做成。"),
        [4] = new("4 号 · 浪漫主义者", "追求独特与深度感受。", "你敏感、有审美，情绪层次丰富。"),
        [5] = new("5 号 · 观察者", "重思考、边界与知识储备。", "你独立、分析，需要独处充电。"),
        [6] = new("6 号 · 忠诚者", "重视安全、责任与可靠联盟。", "你警觉、负责，团队中的守护者。"),
        [7] = new("7 号 · 快乐主义者", "追求自由、新奇与可能性。", "你乐观、点子多，害怕错过。"),
        [8] = new("8 号 · 挑战者", "直接、力量、保护弱者。", "你有掌控欲与正义感，行动果断。"),
        [9] = new("9 号 · 和平者", "追求和谐、包容与稳定。", "你温和、调解，看见多方观点。")
    };

    private static readonly Dictionary<int, PersonalityTypeReport> EnneagramFullMap = new()
    {
        [1] = E(1, "追求正确与改进，内在有清晰标准。",
            "1 号的核心动机是「把事情做对」。你内在有法官般的声音，推动你改进自我与世界。",
            "原则感、责任心、质量意识、可靠、道德勇气。",
            "完美主义、自我批评、对错误难宽容、身体紧张与压抑愤怒。",
            "你表达爱常通过「帮你变好」。学会接受伴侣本来的样子；冲突时先放松身体，再讨论对错。",
            "质检、法务、编辑、管理、医疗、教育、任何重标准与改进的岗位。",
            "练习自我慈悲；区分「意见」与「价值」；允许灰色地带存在。"),
        [2] = E(2, "渴望被需要，擅长感知他人需求。",
            "2 号通过「被需要」感受价值。你敏锐感知他人情绪，常主动补位。",
            "温暖、慷慨、人际直觉、支持力、感染力。",
            "忽视自身需要、边界模糊、用付出换爱、难直接要帮助。",
            "你需听到具体感谢。练习说「我也需要…」；不要把拒绝等同于不被爱。",
            "护理、咨询、人力、客户成功、活动、销售服务、社群运营。",
            "每天问：我现在需要什么？；接受帮助不是软弱；区分真正需要与想要被需要。"),
        [3] = E(3, "目标感强，重视形象与效率。",
            "3 号核心恐惧是无价值，故不断追求成就与认可。你像变色龙般适应环境以达成目标。",
            "效率、目标感、形象管理、驱动团队、适应力强。",
            "过度在意外在评价、忽视真实感受、工作狂、难示弱。",
            "你需要伴侣看见「任务背后的你」。练习分享失败与脆弱；关系不是绩效展示。",
            "管理、销售、创业、媒体、公关、培训、竞技与结果导向行业。",
            "定期断联 KPI；写「无观众日记」；成功之外定义你是谁。"),
        [4] = E(4, "追求独特与深度感受。",
            "4 号渴望被理解其独特，常体验「别人拥有的我缺失」。情绪与审美是你认识世界的通道。",
            "深度、创意、审美、真诚、情感表达、艺术敏感。",
            "情绪放大、比较、自我专注、波动、难满足平凡日常。",
            "你需要被看见真实情绪，而非被快速安慰。练习稳定日常；爱也存在于普通 Tuesday。",
            "艺术、写作、设计、心理、品牌、音乐、独立创作。",
            "感恩练习；把感受转化为作品；建立规律作息锚点。"),
        [5] = E(5, "重思考、边界与知识储备。",
            "5 号在知识与独处中获得安全。你珍惜能量，倾向观察后再参与。",
            "分析、专注、客观、独立、知识深度、冷静。",
            "情感隔离、过度退缩、难求助、对侵入敏感、行动延迟。",
            "你需要预告与边界。伴侣宜尊重独处；你也需练习在场，而不只提供观点。",
            "科研、开发、分析、档案、战略、技术写作、图书馆与情报类工作。",
            "设定「参与配额」；身体活动接地；知识服务于连接，而非替代连接。"),
        [6] = E(6, "重视安全、责任与可靠联盟。",
            "6 号在忠诚与怀疑间摇摆，渴望可信依托。你对风险敏感，常预演最坏情况。",
            "负责、忠诚、团队守护、风险意识、务实、勇气（在信任后）。",
            "焦虑循环、决策拖延、投射怀疑、难信自己直觉。",
            "你需要一致与承诺。练习区分真实危险与想象；把信任分等级，而非全有或全无。",
            "安全、法务、项目管理、政务、军警辅助、合规、运维。",
            "身体放松训练；小步决策；记录「担心未发生」的证据。"),
        [7] = E(7, "追求自由、新奇与可能性。",
            "7 号用乐观与选项对抗痛苦与限制。你思维跳跃，害怕被困在负面体验中。",
            "乐观、创意、联想、社交、适应、战略头脑。",
            "逃避痛苦、分散、承诺恐惧、肤浅化深度话题、冲动。",
            "你需要一起冒险，也需要有人陪你坐下面对难情绪。练习停留，而非立刻换频道。",
            "创业、媒体、旅游、产品、咨询、活动策划、创新岗位。",
            "正念与单任务；允许悲伤存在；选少而深的承诺。"),
        [8] = E(8, "直接、力量、保护弱者。",
            "8 号厌恶被控制，以强度与正义感保护自我与他人。你直截了当，能量外放。",
            "领导力、保护欲、决断、诚实、抗压力、资源动员。",
            "控制欲、过度对抗、难示弱、把脆弱当威胁、语速音量压迫他人。",
            "你需要绝对忠诚与真实。练习温和启动；保护也可以轻声；脆弱是信任的信号。",
            "创业、管理、销售、政治、法律、应急、竞技、变革推动。",
            "暂停三秒再回应；倾听不打断；力量用于守护，而非压制。"),
        [9] = E(9, "追求和谐、包容与稳定。",
            "9 号通过融合与回避冲突维持内在和平。你能看见多方观点，却难坚持自己的优先级。",
            "包容、调解、稳定、耐心、倾听、看见全局。",
            "拖延、自我麻木、难说「不」、被动攻击、目标模糊。",
            "你需要低冲突但真实的沟通。练习表达偏好；你的意见会改善关系，而非破坏关系。",
            "调解、人力、行政、教育、社工、客服、支持型管理。",
            "每日三件优先事；身体唤醒（运动）；小步说「不」。")
    };

    private static PersonalityTypeReport E(
        int type,
        string summary,
        string overview,
        string strengths,
        string weaknesses,
        string relations,
        string career,
        string growth) =>
        M($"{type} 号 · {EnneagramMap[type].Title.Split('·')[1].Trim()}", summary,
            overview, strengths, weaknesses, relations, career, growth);
}
