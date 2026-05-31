using Autofac;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Optimization;

namespace LenovoLegionToolkit.Lib.Plugins;

public class IoCModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(_ => new PluginSignatureValidator(PluginSignatureSettings.Production))
            .As<IPluginSignatureValidator>()
            .SingleInstance();

        builder.Register<PluginLoader>().As<IPluginLoader>().SingleInstance();
        builder.Register<PluginRegistry>().As<IPluginRegistry>().SingleInstance();
        builder.Register<PluginFileSystemManager>().As<IPluginFileSystemManager>().SingleInstance();

        builder.Register<PluginManager>().As<IPluginManager>().SingleInstance();

        builder.Register<PluginRepositoryService>().AsSelf().SingleInstance();

        // Register optimization category extender so Lib can discover plugin categories
        // without a direct circular reference to Lib.Plugins.
        builder.RegisterType<OptimizationCategoryExtender>()
            .As<IOptimizationCategoryExtender>()
            .SingleInstance();
    }
}
