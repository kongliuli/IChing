using IChing.Lab.Abstractions.Models;

namespace IChing.Lab.Abstractions.Prompts;

/// <summary>
/// Prompt 模板构建器抽象接口，将排盘上下文转换为最终提示文本。
/// </summary>
public interface IPromptBuilder
{
    /// <summary>领域标识，例如 bazi / liuyao / tarot。</summary>
    string Domain { get; }

    /// <summary>模板层级（Tier），用于区分不同精度/复杂度的提示模板。</summary>
    int Tier { get; }

    /// <summary>模板标识，唯一标识本构建器使用的模板。</summary>
    string TemplateId { get; }

    /// <summary>
    /// 根据排盘上下文构建最终提示文本及元信息。
    /// </summary>
    /// <param name="ctx">Prompt 构建上下文。</param>
    /// <returns>Prompt 构建结果。</returns>
    PromptBuildResult Build(PromptContext ctx);
}
