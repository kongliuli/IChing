using System.Text;
using System.Text.Json;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Prompts;
using Scriban;
using Scriban.Runtime;

namespace IChing.Lab.Inference.Prompts;

/// <summary>
/// 基于 Scriban 的 <see cref="IPromptBuilder"/> 实现：从 <see cref="PromptTemplateRegistry"/>
/// 取模板文本，按 (domain, tier, templateId) 从 <see cref="PromptContext"/> 提取变量并渲染。
/// 模板解析失败时回退到 <see cref="EmbeddedPromptDefaults"/> 内嵌默认；塔罗英文模板默认
/// <see cref="PromptBuildResult.NeedsTranslationPass"/>=true（需第二 pass 翻译为中文）。
/// </summary>
public sealed class TemplatePromptBuilder : IPromptBuilder
{
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    private readonly PromptTemplateRegistry _registry;
    private readonly bool _needsTranslationPass;

    public TemplatePromptBuilder(PromptTemplateRegistry registry, string domain, int tier, string templateId)
    {
        _registry = registry;
        Domain = domain;
        Tier = tier;
        TemplateId = templateId;
        // 塔罗英文模板需要额外的英译中 pass；其余模板（含翻译模板自身）不需要。
        _needsTranslationPass = domain == "tarot" && templateId == "tarot-tier1-en";
    }

    public string Domain { get; }
    public int Tier { get; }
    public string TemplateId { get; }

    public PromptBuildResult Build(PromptContext ctx)
    {
        // 多模块组合：ModuleFocuses 含 2 个及以上模块时，分模块生成片段再用 combined 模板拼装。
        if (ctx.ModuleFocuses is { Count: > 1 })
        {
            return BuildCombined(ctx);
        }
        var script = BuildScriptObject(ctx);
        var rendered = RenderWithFallback(script, ctx);
        return new PromptBuildResult(rendered, EngineHint: null, NeedsTranslationPass: _needsTranslationPass);
    }

    /// <summary>
    /// 当 ModuleFocuses.Count > 1 时，分别用各模块模板生成片段，再用 {domain}-tier{N}-combined.txt 拼装。
    /// 组合模板内 {{ module_snippets.{module} }} 占位被片段替换。
    /// 组合模板缺失时回退到单模块模板（取首个模块）。
    /// </summary>
    internal PromptBuildResult BuildCombined(PromptContext ctx)
    {
        var modules = ctx.ModuleFocuses!;
        var engineVariant = ctx.Engine?.TemplateHint;
        var snippets = new Dictionary<string, string>(StringComparer.Ordinal);

        // 逐模块渲染片段：用单模块上下文取对应模块模板并渲染。
        foreach (var m in modules)
        {
            var moduleCtx = ctx with { ModuleFocuses = new[] { m } };
            var moduleScript = BuildScriptObject(moduleCtx);
            moduleScript["module_focuses"] = new[] { m };
            var moduleText = _registry.GetTemplateWithFallback(Domain, Tier, engineVariant, m);
            var rendered = TryRender(moduleText, moduleScript, out _);
            snippets[m] = rendered?.TrimEnd() ?? string.Empty;
        }

        var combinedId = $"{Domain}-tier{Tier}-combined";
        var combinedText = _registry.GetTemplate(combinedId);
        if (string.IsNullOrEmpty(combinedText))
        {
            // 组合模板缺失：回退到首个模块的单模块模板。
            var firstModule = modules[0];
            var fallbackText = _registry.GetTemplateWithFallback(Domain, Tier, engineVariant, firstModule);
            var fallbackScript = BuildScriptObject(ctx with { ModuleFocuses = new[] { firstModule } });
            var fallbackRendered = TryRender(fallbackText, fallbackScript, out _);
            return new PromptBuildResult(
                (fallbackRendered ?? string.Empty).TrimEnd(),
                EngineHint: null,
                NeedsTranslationPass: _needsTranslationPass);
        }

        // 用组合模板拼装：将各模块片段注入 module_snippets.{module} 占位。
        var combinedScript = BuildScriptObject(ctx);
        var snippetScript = new ScriptObject();
        foreach (var kv in snippets)
        {
            snippetScript[kv.Key] = kv.Value;
        }
        combinedScript["module_snippets"] = snippetScript;
        var combinedRendered = TryRender(combinedText, combinedScript, out _);
        return new PromptBuildResult(
            (combinedRendered ?? string.Empty).TrimEnd(),
            EngineHint: null,
            NeedsTranslationPass: _needsTranslationPass);
    }

    /// <summary>
    /// 渲染模板：先用算法感知的三级回退选取模板（ctx.Engine 非空时），否则降级到原 GetTemplate(TemplateId)。
    /// 选取的模板解析失败时回退到内嵌默认，绝不抛异常。
    /// </summary>
    private string RenderWithFallback(ScriptObject script, PromptContext ctx)
    {
        // ctx.Engine 非空：走算法感知三级回退；module 仅在 ModuleFocuses 恰好 1 个时取首项。
        string text;
        if (ctx.Engine is not null)
        {
            var module = ctx.ModuleFocuses is { Count: 1 } ? ctx.ModuleFocuses[0] : null;
            text = _registry.GetTemplateWithFallback(Domain, Tier, ctx.Engine.TemplateHint, module);
        }
        else
        {
            // 旧调用方（未传 Engine）：保持改造前行为，按 TemplateId 直接取模板。
            text = _registry.GetTemplate(TemplateId);
        }

        var rendered = TryRender(text, script, out _);
        if (rendered is not null)
        {
            // 与原 QwenChatTemplate.Wrap 行为一致：去除尾部空白（文件尾部换行不影响输出）。
            return rendered.TrimEnd();
        }

        // 外部模板解析失败，回退到内嵌默认。
        var embedded = EmbeddedPromptDefaults.Get(TemplateId);
        if (embedded is not null)
        {
            rendered = TryRender(embedded, script, out _);
            if (rendered is not null)
            {
                return rendered.TrimEnd();
            }
        }

        // 兜底：返回原始内嵌文本（极端情况，不应发生）。
        return embedded ?? text ?? string.Empty;
    }

    private static string? TryRender(string text, ScriptObject script, out string? error)
    {
        error = null;
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var template = Template.Parse(text);
        if (template.HasErrors)
        {
            error = string.Join("; ", template.Messages);
            return null;
        }

        var context = new TemplateContext();
        context.PushGlobal(script);
        return template.Render(context);
    }

    /// <summary>按 (domain, templateId) 从 PromptContext 提取 Scriban 变量。</summary>
    private ScriptObject BuildScriptObject(PromptContext ctx)
    {
        var script = new ScriptObject();
        var focus = ctx.Focus ?? "综合";
        script["focus"] = focus;
        script["question"] = ctx.Question ?? string.Empty;

        // 算法感知变量：注入引擎元数据，供模板按算法来源组织回答；旧调用方（Engine=null）取空值。
        script["engine_variant"] = ctx.Engine?.TemplateHint ?? string.Empty;
        script["module_focuses"] = ctx.ModuleFocuses ?? Array.Empty<string>();
        script["engine_source"] = ctx.Engine?.Source ?? string.Empty;
        script["engine_algorithm_basis"] = ctx.Engine?.AlgorithmBasis ?? string.Empty;

        switch (Domain, TemplateId)
        {
            case ("bazi", "bazi-tier1-default"):
                script["chart_json"] = SerializeChart(ctx.Chart);
                script["rule_digest"] = FormatRuleDigestSimple(ctx.RuleDigest);
                script["word_max"] = 400;
                break;

            case ("liuyao", "liuyao-tier1-default"):
                script["chart_json"] = SerializeChart(ctx.Chart);
                script["rule_digest"] = FormatRuleDigestLiuyao(ctx.RuleDigest);
                script["word_min"] = 200;
                script["word_max"] = 400;
                break;

            case ("tarot", "tarot-tier1-en"):
                if (ctx.Chart is TarotPromptInput input)
                {
                    script["spread_title"] = input.SpreadTitle;
                    script["positions_block"] = FormatPositionsBlock(input.Positions);
                    script["word_limit"] = input.WordLimit;
                }
                else
                {
                    script["spread_title"] = string.Empty;
                    script["positions_block"] = string.Empty;
                    script["word_limit"] = 280;
                }
                script["rule_digest"] = FormatRuleDigestSimple(ctx.RuleDigest);
                break;

            case ("tarot", "tarot-translate-to-zh"):
                script["english_text"] = (ctx.Chart as string)?.Trim() ?? string.Empty;
                script["card_names_block"] = FormatCardGlossary(ctx.RuleDigest);
                break;
        }

        return script;
    }

    private static string SerializeChart(object? chart)
    {
        if (chart is null)
        {
            return string.Empty;
        }

        // 与原 BaziPromptBuilder/LiuyaoPromptBuilder 一致：按 object 重载序列化，
        // 对 JsonElement（fixture）与普通对象（controller）均产生缩进 JSON 且转义非 ASCII。
        return JsonSerializer.Serialize(chart, IndentedJson);
    }

    /// <summary>八字/塔罗规则摘要格式化：每属性一行 "- name: value"，数组以 JSON 值呈现。</summary>
    private static string FormatRuleDigestSimple(object? ruleDigest)
    {
        if (ruleDigest is null)
        {
            return string.Empty;
        }

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ruleDigest));
        var sb = new StringBuilder();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            sb.AppendLine($"- {prop.Name}: {prop.Value}");
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>六爻规则摘要格式化：数组属性展开为多行 "- name: item"。</summary>
    private static string FormatRuleDigestLiuyao(object? ruleDigest)
    {
        if (ruleDigest is null)
        {
            return string.Empty;
        }

        JsonElement el;
        IDisposable? disposable = null;
        if (ruleDigest is JsonElement je)
        {
            el = je;
        }
        else
        {
            var doc = JsonDocument.Parse(JsonSerializer.Serialize(ruleDigest));
            disposable = doc;
            el = doc.RootElement;
        }

        try
        {
            return FormatLiuyaoElement(el);
        }
        finally
        {
            disposable?.Dispose();
        }
    }

    private static string FormatLiuyaoElement(JsonElement el)
    {
        var sb = new StringBuilder();
        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.Value.EnumerateArray())
                {
                    sb.AppendLine($"- {prop.Name}: {item}");
                }
            }
            else
            {
                sb.AppendLine($"- {prop.Name}: {prop.Value}");
            }
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>塔罗牌位块：保留尾部换行以匹配原 AppendLine 行为。</summary>
    private static string FormatPositionsBlock(IReadOnlyList<TarotPositionPrompt> positions)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            var orient = p.Reversed ? "reversed" : "upright";
            sb.AppendLine(
                $"{i + 1}. [{p.PositionTitle} / {p.PositionContext}] {p.CardName} ({orient}) — {p.MeaningEn}");
        }
        return sb.ToString();
    }

    /// <summary>塔罗牌名词典：空时回退到默认牌名列表，不保留尾部换行。</summary>
    private static string FormatCardGlossary(object? ruleDigest)
    {
        var names = ExtractCardNames(ruleDigest);
        if (names is null || names.Count == 0)
        {
            return "- The Tower\n- The Star\n- Eight of Pentacles";
        }

        return string.Join('\n', names.Distinct().Select(n => $"- {n}"));
    }

    private static IReadOnlyList<string>? ExtractCardNames(object? ruleDigest)
    {
        if (ruleDigest is null)
        {
            return null;
        }

        if (ruleDigest is IEnumerable<string> seq)
        {
            return seq.ToList();
        }

        if (ruleDigest is string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : new[] { s };
        }

        return null;
    }
}
