using IChing.Tarot.App.Models;

namespace IChing.Tarot.App.Services;

/// <summary>内置人格测评题库（IPIP 风格 / 九型 / 霍兰德简化版）。</summary>
public static class PersonalityQuizBank
{
    private static PersonalityQuizDefinition? _mbti16;
    private static PersonalityQuizDefinition? _enneagram9;
    private static PersonalityQuizDefinition? _hollandRiasec;

    public static bool TryGet(string id, out PersonalityQuizDefinition quiz)
    {
        quiz = id.ToLowerInvariant() switch
        {
            "mbti-16" => _mbti16 ??= CreateMbti16(),
            "enneagram-9" => _enneagram9 ??= CreateEnneagram9(),
            "holland-riasec" => _hollandRiasec ??= CreateHollandRiasec(),
            _ => null!
        };
        return quiz is not null;
    }

    public static PersonalityQuizDefinition Get(string id) =>
        TryGet(id, out var quiz) ? quiz : throw new KeyNotFoundException($"未知测评：{id}");

    private static PersonalityQuizDefinition CreateMbti16() => new()
    {
        Id = "mbti-16",
        Title = "十六型人格",
        Subtitle = "28 题 · IPIP 风格四维计分",
        Scoring = "mbti16",
        Disclaimer = "基于 IPIP 风格开源计分，非官方 MBTI®，与 16personalities 等网站算法不同，仅供娱乐参考。",
        Questions = BuildMbtiQuestions()
    };

    private static PersonalityQuizDefinition CreateEnneagram9() => new()
    {
        Id = "enneagram-9",
        Title = "九型人格",
        Subtitle = "27 题 · 核心动机倾向",
        Scoring = "enneagram9",
        Disclaimer = "简化版九型倾向测试，非官方 Riso-Hudson 量表，仅供娱乐参考。",
        Questions = BuildEnneagramQuestions()
    };

    private static PersonalityQuizDefinition CreateHollandRiasec() => new()
    {
        Id = "holland-riasec",
        Title = "霍兰德职业兴趣",
        Subtitle = "30 题 · RIASEC 六维",
        Scoring = "holland",
        Disclaimer = "简化版霍兰德 RIASEC 兴趣测试，非正式职业测评，仅供娱乐与方向参考。",
        Questions = BuildHollandQuestions()
    };

    private static List<PersonalityQuestion> BuildMbtiQuestions() =>
    [
        Pair("社交场合结束后，你通常？", "更有精神", "E", "需要独处恢复", "I"),
        Pair("你更常？", "先开口破冰", "E", "等别人来找你", "I"),
        Pair("周末理想安排？", "见朋友、参加活动", "E", "在家看书或做项目", "I"),
        Pair("在团队讨论中你？", "边说边想", "E", "先想清再说", "I"),
        Pair("你获取能量的方式？", "与人互动", "E", "安静思考", "I"),
        Pair("电话/消息回复？", "很快，甚至享受聊天", "E", "攒一攒再回", "I"),
        Pair("新环境你会？", "迅速认识一圈人", "E", "观察后再深交", "I"),
        Pair("你更关注？", "具体事实与细节", "S", "整体模式与可能", "N"),
        Pair("学习新东西时？", "按步骤实操", "S", "先理解概念框架", "N"),
        Pair("描述一件事你更常？", "列举实际例子", "S", "讲隐喻和联想", "N"),
        Pair("你更相信？", "亲眼所见、数据", "S", "直觉与趋势", "N"),
        Pair("旅行规划？", "详细行程表", "S", "大方向随意探索", "N"),
        Pair("读说明书？", "会认真看完", "S", "直接上手试", "N"),
        Pair("你更擅长记住？", "人脸、场景、细节", "S", "意义、关联、灵感", "N"),
        Pair("做决定时优先？", "逻辑是否自洽", "T", "对人的影响", "F"),
        Pair("批评别人时你？", "直指问题本身", "T", "先照顾感受", "F"),
        Pair("冲突中你更在意？", "谁对谁错", "T", "关系是否受伤", "F"),
        Pair("被求助时你？", "给方案和分析", "T", "给陪伴和共情", "F"),
        Pair("评价工作你更看重？", "结果与效率", "T", "团队氛围", "F"),
        Pair("看电影更容易被？", "精巧结构说服", "T", "人物命运打动", "F"),
        Pair("你更常？", "列清单、按计划", "J", "灵活应变、即兴", "P"),
        Pair("截止日期前？", "提前完成", "J", "临近才有状态", "P"),
        Pair("桌面/文件习惯？", "分类整齐", "J", "乱中有序", "P"),
        Pair("旅行行李？", "清单核对", "J", "临出门再塞", "P"),
        Pair("对变化的态度？", "计划内调整可接受", "J", "享受开放选项", "P"),
        Pair("项目推进你？", "先定里程碑", "J", "边做边改方向", "P"),
        Pair("空闲时间？", "安排满满也安心", "J", "留白才舒服", "P"),
        Pair("长期目标？", "路径清晰", "J", "保留多种可能", "P")
    ];

    private static List<PersonalityQuestion> BuildEnneagramQuestions() =>
    [
        TypePair("我很难容忍马虎的工作", "1", "我容易看见别人需要什么", "2"),
        TypePair("规则应该被遵守", "1", "被感谢时我最有动力", "2"),
        TypePair("对错对我来说很重要", "1", "我习惯把别人放第一位", "2"),
        TypePair("我会主动纠正错误", "1", "拒绝别人让我内疚", "2"),
        TypePair("标准低让我焦虑", "1", "我渴望被需要", "2"),
        TypePair("形象与成就对我很重要", "3", "我常感到和别人不同", "4"),
        TypePair("我擅长展示优势", "3", "情绪深刻且私密", "4"),
        TypePair("效率低让我烦躁", "3", "美与意义不可缺", "4"),
        TypePair("目标感驱动我", "3", "缺失感时常出现", "4"),
        TypePair("我适应不同场合", "3", "真实自我对我很重要", "4"),
        TypePair("我需要大量独处", "5", "我会预想最坏情况", "6"),
        TypePair("知识让我安全", "5", "忠诚与承诺重要", "6"),
        TypePair("情感消耗我", "5", "我依赖可信团队", "6"),
        TypePair("边界清晰才舒适", "5", "权威可信则安心", "6"),
        TypePair("先观察再参与", "5", "我会反复确认", "6"),
        TypePair("多选项让我兴奋", "7", "我不怕正面冲突", "8"),
        TypePair("重复无聊难以忍受", "7", "弱者需要被保护", "8"),
        TypePair("快乐是重要资源", "7", "控制局面让我安心", "8"),
        TypePair("计划太满会窒息", "7", "直接比迂回好", "8"),
        TypePair("新体验优先", "7", "强度与真实感吸引我", "8"),
        TypePair("和谐比赢更重要", "9", "我很难对错误视而不见", "1"),
        TypePair("我容易合并他人观点", "9", "成就让我有价值", "3"),
        TypePair("慢节奏适合我", "9", "独处比社交恢复我", "5"),
        TypePair("冲突让我不适", "9", "有趣比沉重好", "7"),
        TypePair("被忽视也能忍", "9", "被需要让我安心", "2"),
        TypePair("重要事也常放最后", "9", "安全比冒险重要", "6"),
        TypePair("主动竞争不自然", "9", "独特比合群重要", "4")
    ];

    private static List<PersonalityQuestion> BuildHollandQuestions() =>
    [
        HollandPair("喜欢动手修理或组装", "R", "喜欢查资料找原理", "I"),
        HollandPair("户外实操比写报告有趣", "R", "抽象问题比重复劳动有趣", "I"),
        HollandPair("看得见摸得着的结果安心", "R", "理论模型让我兴奋", "I"),
        HollandPair("工具设备让我有掌控感", "R", "独自研究很享受", "I"),
        HollandPair("身体活动的工作适合我", "R", "数据分析有成就感", "I"),
        HollandPair("表达创意比执行流程重要", "A", "帮助别人让我满足", "S"),
        HollandPair("审美与风格不可妥协", "A", "倾听他人很有意义", "S"),
        HollandPair("自由创作比 KPI 重要", "A", "团队合作让我有劲", "S"),
        HollandPair("艺术/设计/内容吸引我", "A", "教学辅导有成就感", "S"),
        HollandPair("常规重复容易扼杀我", "A", "人际和谐很重要", "S"),
        HollandPair("说服他人很有成就感", "E", "清晰流程让我高效", "C"),
        HollandPair("竞争环境让我兴奋", "E", "细节准确不可出错", "C"),
        HollandPair("带团队推进项目适合我", "E", "按规章办事我放心", "C"),
        HollandPair("商业机会嗅觉灵敏", "E", "表格数据让我安心", "C"),
        HollandPair("影响力比专业深度重要", "E", "归档整理有成就感", "C"),
        HollandPair("机器/代码/硬件比 PPT 有趣", "R", "谈判博弈有乐趣", "E"),
        HollandPair("实验验证假设最吸引", "I", "故事与视觉表达吸引", "A"),
        HollandPair("志愿服务/社区有温度", "S", "预算与合规我擅长", "C"),
        HollandPair("即兴创作比模板好", "A", "公开演讲不怵", "E"),
        HollandPair("故障排查像解谜", "R", "写研究报告不抗拒", "I"),
        HollandPair("他人进步让我开心", "S", "稳定可预期最重要", "C"),
        HollandPair("创业/销售有吸引力", "E", "品牌与体验设计有趣", "A"),
        HollandPair("统计/编程/科研方向", "I", "现场执行比远程协调好", "R"),
        HollandPair("心理咨询/教育有召唤", "S", "资源整合是强项", "E"),
        HollandPair("审计/行政/运营适合", "C", "音乐/写作/绘画有冲动", "A"),
        HollandPair("运动/工程/农业不排斥", "R", "读论文比应酬更自在", "I"),
        HollandPair("做决策比等指令好", "E", "陪伴比独处更有价值", "S"),
        HollandPair("清单与 SOP 是好朋友", "C", "打破常规才有灵感", "A"),
        HollandPair("独立课题比轮岗好", "I", "三维/实体成果最实在", "R"),
        HollandPair("冲突调解我能胜任", "S", "目标导向文化适合我", "E")
    ];

    private static PersonalityQuestion Pair(string text, string aText, string aKey, string bText, string bKey) =>
        new()
        {
            Text = text,
            Options =
            [
                new PersonalityOption { Text = aText, Scores = Score(aKey, 2) },
                new PersonalityOption { Text = bText, Scores = Score(bKey, 2) }
            ]
        };

    private static PersonalityQuestion TypePair(string aText, string aType, string bText, string bType) =>
        new()
        {
            Text = "下列哪项更像你？",
            Options =
            [
                new PersonalityOption { Text = aText, Scores = Score(aType, 2) },
                new PersonalityOption { Text = bText, Scores = Score(bType, 2) }
            ]
        };

    private static PersonalityQuestion HollandPair(string aText, string aKey, string bText, string bKey) =>
        new()
        {
            Text = "你更同意哪边？",
            Options =
            [
                new PersonalityOption { Text = aText, Scores = Score(aKey, 2) },
                new PersonalityOption { Text = bText, Scores = Score(bKey, 2) }
            ]
        };

    private static Dictionary<string, int> Score(string key, int value) =>
        new(StringComparer.OrdinalIgnoreCase) { [key] = value };
}
