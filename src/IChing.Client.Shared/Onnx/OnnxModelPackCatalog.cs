namespace IChing.Client.Shared.Onnx;

/// <summary>
/// 可下载的 ORT GenAI 模型清单（与 scripts/download-qwen-15b-model.ps1 对齐）。
/// </summary>
public sealed record OnnxModelPack(
    string Id,
    string DisplayName,
    string HuggingFaceRepo,
    IReadOnlyList<string> Files,
    string Notes);

public static class OnnxModelPackCatalog
{
    /// <summary>当前仓库已验证可跑的 GenAI 包（约 6GB FP32）。</summary>
    public static OnnxModelPack Qwen25_15b { get; } = new(
        Qwen35ModelCatalog.LegacyId,
        "Qwen2.5-1.5B Instruct (ORT GenAI)",
        "tonythethompson/Qwen2.5-1.5B-Instruct-ONNX",
        [
            "added_tokens.json",
            "chat_template.jinja",
            "config.json",
            "genai_config.json",
            "generation_config.json",
            "merges.txt",
            "model.onnx",
            "model.onnx.data",
            "quantize_config.json",
            "special_tokens_map.json",
            "tokenizer.json",
            "tokenizer_config.json",
            "vocab.json"
        ],
        "默认端侧包；开发机可直接从仓库 models/ 导入。");

    public static IReadOnlyList<OnnxModelPack> Downloadable { get; } = [Qwen25_15b];

    public static OnnxModelPack? Find(string id) =>
        Downloadable.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static string CombineModelDirectory(string root, string modelId) =>
        Path.Combine(root, "models", modelId);

    /// <summary>开发时相对输出目录的 models/ 路径候选。</summary>
    public static IEnumerable<string> DevRepoModelCandidates(string modelId)
    {
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "models", modelId));
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "models", modelId));
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "models", modelId));
    }
}
