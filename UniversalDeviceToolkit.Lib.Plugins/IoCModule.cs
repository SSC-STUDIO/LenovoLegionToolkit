using Autofac;
using UniversalDeviceToolkit.Lib.Extensions;
#if WINDOWS
using UniversalDeviceToolkit.Lib.Optimization;
#endif

namespace UniversalDeviceToolkit.Lib.Plugins;

public class IoCModule : Module
{
    /// <summary>
    /// Optional environment override for plugin signature validation
    /// (require | development | disable). Defaults to Production (require).
    /// Relaxed aliases are honored only in DEBUG; Release always uses Production.
    /// </summary>
    public const string SignatureModeEnvironmentVariable = "UDT_PLUGIN_SIGNATURE_MODE";

    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(_ =>
            {
                var mode = global::System.Environment.GetEnvironmentVariable(SignatureModeEnvironmentVariable);
                return new PluginSignatureValidator(PluginSignatureSettings.CreateForRuntime(mode));
            })
            .As<IPluginSignatureValidator>()
            .SingleInstance();

        builder.Register<PluginLoader>().As<IPluginLoader>().SingleInstance();
        builder.Register<PluginRegistry>().As<IPluginRegistry>().SingleInstance();
        builder.Register<PluginFileSystemManager>().As<IPluginFileSystemManager>().SingleInstance();

        builder.Register<PluginManager>().As<IPluginManager>().SingleInstance();

        builder.Register<PluginRepositoryService>().AsSelf().SingleInstance();

#if !WINDOWS
        // Portable substitute for the Windows ApplicationSettings-backed plugin
        // state (PluginManager ctor resolves this on non-Windows builds).
        builder.Register<PluginStateStore>().SingleInstance();
#endif

#if WINDOWS
        // Register optimization category extender so Lib can discover plugin categories
        // without a direct circular reference to Lib.Plugins.
        builder.RegisterType<OptimizationCategoryExtender>()
            .As<IOptimizationCategoryExtender>()
            .SingleInstance();
#endif
    }
}
