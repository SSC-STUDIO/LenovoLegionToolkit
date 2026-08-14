using Autofac;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
public class PluginsIoCModuleTests
{
    [Fact]
    public void Load_ShouldRegisterPluginServices()
    {
        var builder = new ContainerBuilder();
        builder.Register(_ => new ApplicationSettings()).AsSelf().SingleInstance();
        builder.RegisterType<HttpClientFactory>().AsSelf().SingleInstance();
        builder.RegisterModule<UniversalDeviceToolkit.Lib.Plugins.IoCModule>();
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
