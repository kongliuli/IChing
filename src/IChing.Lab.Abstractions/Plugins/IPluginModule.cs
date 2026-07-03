using Microsoft.Extensions.DependencyInjection;

namespace IChing.Lab.Abstractions.Plugins;

/// <summary>
/// 插件模块自注册入口接口，由各插件实现以向主程序 DI 容器注册自身服务。
/// </summary>
public interface IPluginModule
{
    /// <summary>
    /// 向主程序的 DI 容器注册本插件提供的服务。
    /// </summary>
    /// <param name="services">主程序的服务集合。</param>
    void Register(IServiceCollection services);
}
