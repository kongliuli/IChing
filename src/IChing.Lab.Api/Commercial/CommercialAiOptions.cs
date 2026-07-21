namespace IChing.Lab.Api.Commercial;

/// <summary>
/// 商业版服务端配置：LLM Key 仅存服务端（User Secrets / 环境变量），App 不可见。
/// </summary>
public sealed class CommercialAiOptions
{
    public const string SectionName = "CommercialAi";

    /// <summary>是否启用商业远端推理（隐藏 Key）。</summary>
    public bool Enabled { get; set; }

    /// <summary>OpenAI 兼容 BaseUrl。</summary>
    public string BaseUrl { get; set; } = "https://api.deepseek.com/v1";

    public string Model { get; set; } = "deepseek-chat";

    /// <summary>环境变量名，例如 CommercialAi__ApiKey。</summary>
    public string ApiKeyEnvironmentVariable { get; set; } = "CommercialAi__ApiKey";
}
