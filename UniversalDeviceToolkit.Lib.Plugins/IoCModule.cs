using Autofac;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Optimization;

namespace UniversalDeviceToolkit.Lib.Plugins;

public class IoCModule : Module
{
    /// <summary>
    /// Optional environment override for plugin signature validation
    /// (require | development | disable). Defaults to Production (require).
    /// Intended for local plugin development with unsigned builds.
    /// </summary>
    public const string SignatureModeEnvironmentVariable = "UDT_PLUGIN_SIGNATURE_MODE";

    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(_ =>
            {
                var mode = global::System.Environment.GetEnvironmentVariable(SignatureModeEnvironmentVariable);
                return new PluginSignatureValidator(
                    PluginSignatureSettings.TryCreateFromEnvironmentValue(mode, out var settings)
                        ? settings
                        : PluginSignatureSettings.Production);
            })
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
