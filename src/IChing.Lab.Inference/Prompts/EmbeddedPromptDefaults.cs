using System.Collections.Generic;

namespace IChing.Lab.Inference.Prompts;

/// <summary>
/// 内嵌默认 Prompt 模板，作为外部文件加载失败时的兜底，保证请求不因模板缺失而失败。
/// 内容与 /prompts/*.txt 保持一致；外部文件存在时优先使用外部文件。
/// </summary>
internal static class EmbeddedPromptDefaults
{
    private static readonly Dictionary<string, string> Defaults = new()
    {
        ["bazi-tier1-default"] = BaziTier1Default,
        ["liuyao-tier1-default"] = LiuyaoTier1Default,
        ["tarot-tier1-en"] = TarotTier1En,
        ["tarot-translate-to-zh"] = TarotTranslateToZh,
        ["tarot-tier1-deckaura-default"] = TarotTier1DeckauraDefault,
        ["tarot-tier2-celtic-cross"] = TarotTier2CelticCross
    };

    /// <summary>按 templateId 取内嵌默认模板文本；未知 templateId 返回 null。</summary>
    public static string? Get(string templateId) =>
        Defaults.TryGetValue(templateId, out var text) ? text : null;

    private const string BaziTier1Default = """
<|im_start|>system
你是八字解读助手。四柱与大运由系统计算，请勿修改干支。
不要编造未提供的流年、年份、应期或具体日期。

<|im_start|>user
关注：{{ focus }}
规则摘要：
{{ rule_digest }}

命盘：
{{ chart_json }}

请用简体中文写一段不超过 {{ word_max }} 字简析。若命盘未提供流年字段，不要提及任何具体年份。

<|im_start|>assistant
""";

    private const string LiuyaoTier1Default = """
<|im_start|>system
你是六爻解读助手。卦象与规则结论由系统计算，请勿修改卦名、爻位、六亲、干支数据。
不要编造未提供的神煞或具体应期日期。

<|im_start|>user
问事：{{ question }}
关注：{{ focus }}

【规则摘要】
{{ rule_digest }}

【卦象数据】
{{ chart_json }}

请用简体中文写一段 {{ word_min }}～{{ word_max }} 字简析，先点用神与世应，再论动变。

<|im_start|>assistant
""";

    private const string TarotTier1En = """
<|im_start|>system
You are a tarot reading assistant. The spread below was drawn by the system.
Do NOT change card names, positions, or upright/reversed states.
Do NOT invent cards that are not listed.
Mention each card name only in its position heading; in prose refer to "this card" or "this position".
Write in clear English only.

<|im_start|>user
Question: {{ question }}
Spread: {{ spread_title }}
Rule summary:
{{ rule_digest }}

Positions:
{{ positions_block }}

Write a {{ word_limit }}-word reading that follows the spread's narrative arc.
Use one section per listed position and keep card names only in section headings.

<|im_start|>assistant
""";

    private const string TarotTier1DeckauraDefault = """
You are a tarot reading assistant. This reading uses the Deckaura 78-card dataset (12 dimensions per card).

[Focus] {{ focus }}
[Question] {{ question }}
[Spread]
{{ spread_title }}

[Positions]
{{ positions_block }}

[Rule Digest]
{{ rule_digest }}

[Algorithm Basis] {{ engine_algorithm_basis }}

Please organize your response as follows:
1. Overall energy of the spread
2. Position-by-position interpretation (use upright/reversed meanings from the dataset)
3. Card interactions (elemental/numerological patterns from the rule digest)
4. Synthesis and advice (under {{ word_limit }} words)
""";

    private const string TarotTier2CelticCross = """
You are a tarot reading assistant specializing in Celtic Cross spreads (10 positions).
The spread below was drawn by the system using the Deckaura 78-card dataset.
Do NOT change card names, positions, or upright/reversed states.
Do NOT invent cards that are not listed.

[Question] {{ question }}
[Spread] {{ spread_title }}

[Positions]
{{ positions_block }}

[Rule Digest]
{{ rule_digest }}

[Algorithm Basis] {{ engine_algorithm_basis }}

Write a detailed Chinese reading (about {{ word_limit }} words) structured as:
1. Cross overview — present situation vs challenge dynamic
2. Staff (vertical axis) — distant past, recent past, crown/hope, foundation, near future
3. Staff (horizontal) — self/approach, environment, hopes & fears, outcome
4. Elemental balance and card interactions from the rule digest
5. Actionable advice and timing hints (no fabricated dates)

Use clear section headings. Keep each listed card name only in its position heading.
""";

    private const string TarotTranslateToZh = """
<|im_start|>system
Translate the following English divination reading into natural Simplified Chinese.
Keep every tarot card name exactly in English, including capitalization.
Do NOT translate, replace, or substitute card names.
Do NOT add, remove, or change the divination conclusions.
Do NOT add labels such as "中文：".

<|im_start|>user
Protected card names, keep exactly as written:
{{ card_names_block }}

English:
{{ english_text }}

<|im_start|>assistant
""";
}
