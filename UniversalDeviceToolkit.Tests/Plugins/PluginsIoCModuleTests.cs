using Autofac;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Optimization;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginsIoCModuleTests
{
    [Fact]
    public void Load_ShouldRegisterPluginServices()
    {
        var builder = new ContainerBuilder();
        builder.Register(_ => new ApplicationSettings()).AsSelf().SingleInstance();
        builder.RegisterType<HttpClientFactory>().AsSelf().SingleInstance();
        builder.RegisterModule<LenovoLegionToolkit.Lib.Plugins.IoCModule>();
        using var container = builder.Build();

        container.Resolve<IPluginManager>().Should().NotBeNull();
        container.Resolve<IPluginLoader>().Should().NotBeNull();
        container.Resolve<IPluginRegistry>().Should().NotBeNull();
        container.Resolve<IPluginFileSystemManager>().Should().NotBeNull();
        container.Resolve<IPluginSignatureValidator>().Should().NotBeNull();
        container.Resolve<PluginRepositoryService>().Should().NotBeNull();
        container.Resolve<IOptimizationCategoryExtender>().Should().BeOfType<OptimizationCategoryExtender>();
    }
}
