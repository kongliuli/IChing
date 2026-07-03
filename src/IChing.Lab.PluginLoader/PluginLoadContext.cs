using System.Reflection;
using System.Runtime.Loader;

namespace IChing.Lab.PluginLoader;

/// <summary>
/// 插件专用的可回收程序集加载上下文。
/// 每个外部插件使用独立的 <see cref="PluginLoadContext"/> 实例以隔离依赖，
/// 并通过 <c>isCollectible: true</c> 支持 <see cref="AssemblyLoadContext.Unload"/> 卸载。
/// </summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// 初始化插件加载上下文。
    /// </summary>
    /// <param name="mainAssemblyPath">插件主程序集的绝对路径，用于解析其 .deps.json 依赖。</param>
    public PluginLoadContext(string mainAssemblyPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    /// <summary>
    /// 解析插件依赖程序集：若该程序集已加载到 default ALC（如共享接口
    /// <c>IChing.Lab.Abstractions</c>），返回 <c>null</c> 让基类回落到默认上下文，
    /// 保证主程序与插件类型同一；其余依赖交由 <see cref="AssemblyDependencyResolver"/> 解析。
    /// </summary>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 共享接口（IPluginModule / IPluginManifest）所在的 Abstractions 等程序集
        // 必须落到 default ALC：若已加载则返回 null，由基类回落到默认上下文。
        if (AssemblyLoadContext.Default.Assemblies.Any(a => a.GetName().Name == assemblyName.Name))
        {
            return null;
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is not null ? LoadFromAssemblyPath(path) : null;
    }

    /// <summary>
    /// 解析非托管（原生）DLL 依赖，交由 <see cref="AssemblyDependencyResolver"/> 解析；
    /// 找不到返回 <see cref="IntPtr.Zero"/> 由基类继续处理。
    /// </summary>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is not null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}
