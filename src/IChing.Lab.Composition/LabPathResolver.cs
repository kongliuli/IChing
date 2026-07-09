namespace IChing.Lab.Composition;

public static class LabPathResolver
{
    public static string ResolveModelPath(string configuredPath, string contentRoot)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var candidates = new[]
        {
            Path.GetFullPath(configuredPath),
            Path.GetFullPath(Path.Combine(contentRoot, configuredPath)),
            Path.GetFullPath(Path.Combine(contentRoot, "..", "..", configuredPath))
        };

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[1];
    }

    /// <summary>
    /// 解析 prompts 模板根目录：依次尝试 cwd、contentRoot、仓库根、输出目录。
    /// 找不到时返回首个候选路径，由 <see cref="Inference.Prompts.PromptTemplateRegistry"/> 回退到内嵌默认模板。
    /// </summary>
    public static string ResolvePromptsRoot(string configuredPath, string contentRoot)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var candidates = new[]
        {
            Path.GetFullPath(configuredPath),
            Path.GetFullPath(Path.Combine(contentRoot, configuredPath)),
            Path.GetFullPath(Path.Combine(contentRoot, "..", "..", configuredPath)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath))
        };

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }
}
