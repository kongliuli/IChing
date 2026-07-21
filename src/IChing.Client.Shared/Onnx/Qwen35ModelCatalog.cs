namespace IChing.Client.Shared.Onnx;

/// <summary>
/// Qwen 端侧选型备注。Qwen3.5 的 ORT GenAI 包需自建，见 docs/active/qwen35-genai.md。
/// </summary>
public static class Qwen35ModelCatalog
{
    public const string RecommendedId = "qwen3.5-2b-genai";
    public const string LiteId = "qwen3.5-0.8b-genai";
    public const string LegacyId = "qwen2.5-1.5b-genai";

    /// <summary>有现成 GenAI 包前默认用已验证的 2.5-1.5B。</summary>
    public static string DefaultDownloadId => LegacyId;

    public static string DefaultRelativePath => Path.Combine("models", DefaultDownloadId);

    public static IReadOnlyList<string> CandidateDirectoryNames { get; } =
        [LegacyId, LiteId, RecommendedId];
}
