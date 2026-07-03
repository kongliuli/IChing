namespace IChing.Lab.PluginLoader;

/// <summary>
/// SemVer 兼容性检查器：major 版本相同视为兼容。
/// </summary>
public static class VersionCompatibilityChecker
{
    /// <summary>
    /// 判断插件要求的 API 版本与当前抽象层版本是否兼容（major 相同）。
    /// </summary>
    /// <param name="required">插件声明的 <c>RequiredApiVersion</c>。</param>
    /// <param name="current">当前抽象层 <c>AbstractionsVersion.Current</c>。</param>
    /// <returns>major 版本相同时返回 <c>true</c>；无法解析或 major 不同返回 <c>false</c>。</returns>
    public static bool IsCompatible(string? required, string? current)
    {
        var reqMajor = TryParseMajor(required);
        var curMajor = TryParseMajor(current);
        if (reqMajor is null || curMajor is null)
        {
            return false;
        }

        return reqMajor == curMajor;
    }

    /// <summary>
    /// 解析版本号的 major 段（第一个点之前的部分）。
    /// </summary>
    private static int? TryParseMajor(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var span = version.AsSpan();
        var dot = span.IndexOf('.');
        var majorSpan = dot < 0 ? span : span[..dot];
        return int.TryParse(majorSpan, out var major) ? major : null;
    }
}
