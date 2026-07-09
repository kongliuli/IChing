using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Core.Readings;

public static class FollowUpPromptBuilder
{
    private const string SystemPrompt =
        """
        你是占卜追问助手。只能基于已给出的 computedFacts、ruleDigest、pluginContext 与 initialStructured 回答；
        不能改动盘面、卦象、牌阵或规则结论。回答要短，直接回应用户追问。
        返回一个合法 JSON 对象（reading-output.v2），不要 markdown 或代码围栏。
        """;

    public static (string SystemPrompt, string Context) Build(
        string domain,
        ExchangeInput input,
        ReadingStructuredOutput? initialStructured,
        string? rawInitialText,
        IReadOnlyList<DialogueTurn>? history = null)
    {
        var structured = initialStructured ?? ReadingOutputParser.TryParseStructured(rawInitialText, domain);
        var context = ExchangeContextCompactor.BuildFollowUpContext(
            input,
            structured,
            history ?? Array.Empty<DialogueTurn>());
        return (SystemPrompt, context);
    }
}
