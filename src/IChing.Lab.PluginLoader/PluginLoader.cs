using System.Reflection;
using IChing.Lab.Abstractions.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IChing.Lab.PluginLoader;

/// <summary>
/// 插件加载器：从 <c>plugins:externalAssemblies</c> 配置发现外部插件程序集，
/// 使用独立的可回收 <see cref="PluginLoadContext"/> 加载，校验 API 版本兼容性，
/// 调用 <see cref="IPluginModule.Register"/> 注册服务，并支持按插件名卸载。
/// </summary>
public sealed class PluginLoader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PluginLoader> _logger;
    private readonly string _contentRoot;

    /// <summary>已加载插件：键为插件名，值为加载上下文与其弱引用。</summary>
    private readonly Dictionary<string, (PluginLoadContext Context, WeakReference WeakRef)> _loaded = new();

    /// <summary>
    /// 初始化插件加载器。
    /// </summary>
    /// <param name="configuration">应用配置，读取 <c>plugins:externalAssemblies</c> 段。</param>
    /// <param name="logger">日志记录器。</param>
    /// <param name="contentRoot">内容根目录，用于解析相对插件路径；默认为当前工作目录。</param>
    public PluginLoader(IConfiguration configuration, ILogger<PluginLoader> logger, string? contentRoot = null)
    {
        _configuration = configuration;
        _logger = logger;
        _contentRoot = contentRoot ?? Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// 便捷方法：发现 + 加载 + 注册所有外部插件到 <paramref name="services"/>。
    /// 任一插件加载失败仅记录日志并跳过，不影响主程序启动。
    /// </summary>
    public void DiscoverAndRegister(IServiceCollection services)
    {
        var manifests = Discover();
        if (manifests.Count == 0)
        {
            _logger.LogInformation("未发现外部插件（plugins:externalAssemblies 为空或 plugins/ 目录不存在）");
            return;
        }

        foreach (var manifest in manifests)
        {
            try
            {
                var assembly = LoadAssembly(manifest);
                RegisterModules(manifest.Name, assembly, services);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning("插件 {Name} 的程序集未找到：{Path}，已跳过。{Error}",
                    manifest.Name, manifest.Path, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载插件 {Name} 失败，已跳过。", manifest.Name);
            }
        }
    }

    /// <summary>
    /// 读取 <c>plugins:externalAssemblies</c> 配置，返回已解析路径的插件清单列表。
    /// </summary>
    public IReadOnlyList<PluginManifest> Discover()
    {
        var result = new List<PluginManifest>();
        var section = _configuration.GetSection("plugins:externalAssemblies");
        foreach (var child in section.GetChildren())
        {
            var name = child["name"];
            var path = child["path"];
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
            {
                _logger.LogWarning("plugins:externalAssemblies 存在 name 或 path 为空的项，已跳过。");
                continue;
            }

            var resolvedPath = ResolvePluginPath(path);
            result.Add(new PluginManifest(name!, resolvedPath, RequiredApiVersion: string.Empty));
        }

        return result;
    }

    /// <summary>
    /// 使用独立的 <see cref="PluginLoadContext"/> 加载插件主程序集，并在内部维护卸载句柄。
    /// </summary>
    public Assembly LoadAssembly(PluginManifest manifest)
    {
        var context = new PluginLoadContext(manifest.Path);
        var assembly = context.LoadFromAssemblyPath(manifest.Path);
        _loaded[manifest.Name] = (context, new WeakReference(context));
        _logger.LogInformation("已加载插件程序集 {Name} 来自 {Path}。", manifest.Name, manifest.Path);
        return assembly;
    }

    /// <summary>
    /// 扫描程序集中实现 <see cref="IPluginModule"/> 的非抽象类，校验 API 版本后调用 <c>Register</c>。
    /// </summary>
    public void RegisterModules(string pluginName, Assembly assembly, IServiceCollection services)
    {
        var moduleTypes = assembly.GetTypes()
            .Where(t => typeof(IPluginModule).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass)
            .ToList();

        if (moduleTypes.Count == 0)
        {
            _logger.LogWarning("插件 {Name} 未实现 IPluginModule，已跳过。", pluginName);
            return;
        }

        foreach (var moduleType in moduleTypes)
        {
            IPluginModule module;
            try
            {
                module = (IPluginModule)Activator.CreateInstance(moduleType)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "插件 {Name} 的模块 {Type} 实例化失败，已跳过。",
                    pluginName, moduleType.FullName);
                continue;
            }

            // 校验版本：从模块实例上的 IPluginManifest 属性读取 RequiredApiVersion。
            var pluginManifest = GetModuleManifest(module);
            if (pluginManifest is not null)
            {
                if (!VersionCompatibilityChecker.IsCompatible(
                        pluginManifest.RequiredApiVersion, AbstractionsVersion.Current))
                {
                    _logger.LogWarning(
                        "插件 {Name} 要求 API 版本 {Required}，与当前 {Current} 不兼容，已跳过。",
                        pluginName, pluginManifest.RequiredApiVersion, AbstractionsVersion.Current);
                    continue;
                }

                _logger.LogInformation(
                    "插件 {Name}（版本 {Version}，要求 API {Required}）版本兼容。",
                    pluginName, pluginManifest.Version, pluginManifest.RequiredApiVersion);
            }
            else
            {
                _logger.LogInformation("插件 {Name} 未声明 IPluginManifest，跳过版本校验。", pluginName);
            }

            try
            {
                module.Register(services);
                _logger.LogInformation("插件 {Name} 已注册服务（模块 {Type}）。",
                    pluginName, moduleType.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "插件 {Name} 注册服务失败。", pluginName);
            }
        }
    }

    /// <summary>
    /// 卸载指定插件：调用 <see cref="AssemblyLoadContext.Unload"/> 并触发 GC，
    /// 记录 <see cref="WeakReference"/> 存活状态（仅日志，不强制断言）。
    /// </summary>
    public void Unload(string pluginName)
    {
        if (!_loaded.TryGetValue(pluginName, out var entry))
        {
            _logger.LogWarning("插件 {Name} 未加载，无法卸载。", pluginName);
            return;
        }

        _loaded.Remove(pluginName);
        entry.Context.Unload();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var alive = entry.WeakRef.IsAlive;
        _logger.LogInformation("插件 {Name} 已卸载，WeakReference.IsAlive={Alive}。", pluginName, alive);
    }

    /// <summary>
    /// 获取指定已加载插件的 <see cref="AssemblyLoadContext"/> 弱引用，供观测卸载结果。
    /// </summary>
    public WeakReference? GetLoadContextWeakReference(string pluginName)
        => _loaded.TryGetValue(pluginName, out var entry) ? entry.WeakRef : null;

    /// <summary>
    /// 解析插件路径：相对路径依次尝试当前工作目录、内容根、仓库根（内容根上两级）、输出目录。
    /// </summary>
    private string ResolvePluginPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var candidates = new[]
        {
            Path.GetFullPath(configuredPath),
            Path.GetFullPath(Path.Combine(_contentRoot, configuredPath)),
            Path.GetFullPath(Path.Combine(_contentRoot, "..", "..", configuredPath)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath))
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    /// <summary>
    /// 反射查找模块实例上类型为 <see cref="IPluginManifest"/> 的公开属性并取其值。
    /// </summary>
    private static IPluginManifest? GetModuleManifest(object module)
    {
        var prop = module.GetType()
            .GetProperties()
            .FirstOrDefault(p => typeof(IPluginManifest).IsAssignableFrom(p.PropertyType));
        return prop?.GetValue(module) as IPluginManifest;
    }
}
