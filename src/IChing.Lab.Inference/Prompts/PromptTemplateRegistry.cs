using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;

namespace IChing.Lab.Inference.Prompts;

/// <summary>
/// Prompt 模板注册表：启动时扫描 <c>prompts/*.txt</c>，按文件名（去掉扩展名）建索引；
/// 用 <see cref="FileSystemWatcher"/> 监听变更并热重载；文件缺失或读取失败时回退到
/// <see cref="EmbeddedPromptDefaults"/> 内嵌默认模板，绝不抛异常。
/// </summary>
public sealed class PromptTemplateRegistry : IDisposable
{
    private readonly string _templateRoot;
    private readonly ILogger<PromptTemplateRegistry> _logger;
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private readonly FileSystemWatcher? _watcher;
    private bool _disposed;

    public PromptTemplateRegistry(string templateRoot, ILogger<PromptTemplateRegistry> logger)
    {
        _templateRoot = templateRoot;
        _logger = logger;

        ScanAll();

        // FileSystemWatcher 在 Linux 上正常工作；目录不存在时跳过监听（仅依赖内嵌默认）。
        if (Directory.Exists(_templateRoot))
        {
            _watcher = new FileSystemWatcher(_templateRoot, "*.txt")
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };
            _watcher.Created += (_, e) => OnChanged(e.Name, e.FullPath);
            _watcher.Changed += (_, e) => OnChanged(e.Name, e.FullPath);
            _watcher.Deleted += (_, e) => OnDeleted(e.Name);
            _watcher.Renamed += (_, e) => { OnChanged(e.Name, e.FullPath); OnDeleted(e.OldName); };
            _watcher.Error += (_, e) => _logger.LogWarning("FileSystemWatcher 出错: {Message}", e.GetException().Message);
            _watcher.EnableRaisingEvents = true;
        }
        else
        {
            _logger.LogWarning("模板目录不存在，使用内嵌默认模板: {Root}", _templateRoot);
        }
    }

    /// <summary>启动时扫描目录下所有 .txt 模板并加载到缓存。</summary>
    private void ScanAll()
    {
        if (!Directory.Exists(_templateRoot))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(_templateRoot, "*.txt"))
        {
            LoadFile(Path.GetFileNameWithoutExtension(file), file);
        }
    }

    /// <summary>加载单个模板文件到缓存；失败时记录日志并保留内嵌默认兜底。</summary>
    private void LoadFile(string templateId, string fullPath)
    {
        try
        {
            if (!File.Exists(fullPath))
            {
                // 文件被删除：移除缓存，GetTemplate 时回退到内嵌默认。
                _cache.TryRemove(templateId, out _);
                _logger.LogWarning("模板文件缺失，将回退到内嵌默认: {TemplateId}", templateId);
                return;
            }

            var text = File.ReadAllText(fullPath);
            _cache[templateId] = text;
            _logger.LogInformation("已加载模板: {TemplateId}", templateId);
        }
        catch (Exception ex)
        {
            // 读取失败不抛异常，GetTemplate 会回退到内嵌默认。
            _logger.LogWarning(ex, "加载模板失败，将回退到内嵌默认: {TemplateId}", templateId);
            _cache.TryRemove(templateId, out _);
        }
    }

    private void OnChanged(string? fileName, string fullPath)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        var templateId = Path.GetFileNameWithoutExtension(fileName);
        LoadFile(templateId, fullPath);
        _logger.LogInformation("模板已热重载: {TemplateId}", templateId);
    }

    private void OnDeleted(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        var templateId = Path.GetFileNameWithoutExtension(fileName);
        _cache.TryRemove(templateId, out _);
        _logger.LogWarning("模板文件被删除，回退到内嵌默认: {TemplateId}", templateId);
    }

    /// <summary>
    /// 按 templateId 取模板文本：优先外部文件缓存，缺失时回退到内嵌默认。
    /// 永不返回 null（未知 templateId 返回空字符串，由调用方决定如何处理）。
    /// </summary>
    public string GetTemplate(string templateId)
    {
        if (_cache.TryGetValue(templateId, out var text))
        {
            return text;
        }

        var embedded = EmbeddedPromptDefaults.Get(templateId);
        if (embedded is not null)
        {
            _logger.LogWarning("使用内嵌默认模板（外部文件未加载）: {TemplateId}", templateId);
            return embedded;
        }

        _logger.LogError("未找到模板，且无内嵌默认: {TemplateId}", templateId);
        return string.Empty;
    }

    /// <summary>
    /// 按 (domain, tier, engineVariant, module) 三级回退查找模板：
    /// 1. {domain}-tier{N}-{engineVariant}-{module}.txt
    /// 2. {domain}-tier{N}-{engineVariant}.txt（同时尝试 {domain}-tier{N}-{engineVariant}-default.txt，
    ///    兼容 spec 场景与 Task 6 的 -default 命名两种写法）
    /// 3. {domain}-tier{N}-default.txt
    /// 任一级命中即返回；engineVariant/module 为 null 或空时跳过对应层级。
    /// 永不返回 null（未命中返回空字符串，由调用方决定如何处理）。
    /// </summary>
    public string GetTemplateWithFallback(string domain, int tier, string? engineVariant, string? module)
    {
        // 1. module + engineVariant：最具体的模块模板
        if (!string.IsNullOrEmpty(engineVariant) && !string.IsNullOrEmpty(module))
        {
            var id = $"{domain}-tier{tier}-{engineVariant}-{module}";
            if (TryGetTemplate(id, out var t1)) return t1;
        }
        // 2. engineVariant only：引擎默认模板，兼容 bare 与 -default 两种命名
        if (!string.IsNullOrEmpty(engineVariant))
        {
            var bareId = $"{domain}-tier{tier}-{engineVariant}";
            if (TryGetTemplate(bareId, out var t2a)) return t2a;

            var defaultId = $"{domain}-tier{tier}-{engineVariant}-default";
            if (TryGetTemplate(defaultId, out var t2b)) return t2b;
        }
        // 3. 全局 default：向下兼容，行为与改造前一致
        var defId = $"{domain}-tier{tier}-default";
        if (TryGetTemplate(defId, out var t3)) return t3;

        _logger.LogWarning("三级回退未命中任何模板：domain={Domain}, tier={Tier}, engineVariant={Variant}, module={Module}",
            domain, tier, engineVariant, module);
        return string.Empty;
    }

    /// <summary>按 templateId 取模板文本（优先缓存，次内嵌默认），未命中返回 false。</summary>
    private bool TryGetTemplate(string templateId, out string text)
    {
        if (_cache.TryGetValue(templateId, out text!))
        {
            return true;
        }

        var embedded = EmbeddedPromptDefaults.Get(templateId);
        if (embedded is not null)
        {
            _logger.LogWarning("使用内嵌默认模板（外部文件未加载）: {TemplateId}", templateId);
            text = embedded;
            return true;
        }

        text = string.Empty;
        return false;
    }

    /// <summary>手动触发某模板重载（供测试或运维调用）。</summary>
    public void Reload(string templateId)
    {
        var path = Path.Combine(_templateRoot, templateId + ".txt");
        LoadFile(templateId, path);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
    }
}
