using System.IO;
using IChing.Lab.Inference.Prompts;
using Microsoft.Extensions.Logging.Abstractions;

namespace IChing.Lab.Tests;

public class PromptTemplateRegistryTests
{
    private static readonly string TemplateId = "bazi-tier1-default";

    // 热重载：运行时修改模板文件，注册表应在 FileSystemWatcher 回调后返回新内容。
    [Fact]
    public void HotReload_FileChanged_ReturnsNewContent()
    {
        using var dir = new TempDir();
        var path = Path.Combine(dir.Path, TemplateId + ".txt");
        File.WriteAllText(path, "FIRST-{{ focus }}");

        using var registry = new PromptTemplateRegistry(dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        Assert.Equal("FIRST-{{ focus }}", registry.GetTemplate(TemplateId));

        // 覆盖写入新内容，等待 FileSystemWatcher 触发热重载。
        File.WriteAllText(path, "SECOND-{{ focus }}");
        Assert.True(WaitFor(() => registry.GetTemplate(TemplateId) == "SECOND-{{ focus }}"),
            "热重载未在超时内生效");
    }

    // 降级：模板文件被删除后，GetTemplate 应回退到内嵌默认，不抛异常。
    [Fact]
    public void Fallback_FileDeleted_ReturnsEmbeddedDefault()
    {
        using var dir = new TempDir();
        var path = Path.Combine(dir.Path, TemplateId + ".txt");
        File.WriteAllText(path, "EXTERNAL-{{ focus }}");

        using var registry = new PromptTemplateRegistry(dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        Assert.Equal("EXTERNAL-{{ focus }}", registry.GetTemplate(TemplateId));

        File.Delete(path);
        // 删除后应回退到内嵌默认（非空、且不再是 EXTERNAL 内容）。
        Assert.True(WaitFor(() => registry.GetTemplate(TemplateId) != "EXTERNAL-{{ focus }}"),
            "删除后未回退到内嵌默认");
        var afterDelete = registry.GetTemplate(TemplateId);
        Assert.NotEmpty(afterDelete);
        Assert.DoesNotContain("EXTERNAL", afterDelete);
    }

    // 降级：目录不存在时，GetTemplate 直接回退到内嵌默认。
    [Fact]
    public void Fallback_MissingDir_ReturnsEmbeddedDefault()
    {
        var missing = Path.Combine(Path.GetTempPath(), "iching-missing-prompts-" + Guid.NewGuid().ToString("N"));
        using var registry = new PromptTemplateRegistry(missing, NullLogger<PromptTemplateRegistry>.Instance);

        var embedded = registry.GetTemplate(TemplateId);
        Assert.NotEmpty(embedded);
        Assert.Contains("八字解读助手", embedded);
    }

    // 手动 Reload：显式调用 Reload 后应返回文件最新内容。
    [Fact]
    public void Reload_RefreshesCache()
    {
        using var dir = new TempDir();
        var path = Path.Combine(dir.Path, TemplateId + ".txt");
        File.WriteAllText(path, "V1");

        using var registry = new PromptTemplateRegistry(dir.Path, NullLogger<PromptTemplateRegistry>.Instance);
        Assert.Equal("V1", registry.GetTemplate(TemplateId));

        File.WriteAllText(path, "V2");
        registry.Reload(TemplateId);
        Assert.Equal("V2", registry.GetTemplate(TemplateId));
    }

    private static bool WaitFor(Func<bool> predicate, int timeoutMs = 3000, int intervalMs = 100)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (predicate())
            {
                return true;
            }
            Thread.Sleep(intervalMs);
        }
        return predicate();
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iching-prompts-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch { /* 忽略清理失败 */ }
        }
    }
}
