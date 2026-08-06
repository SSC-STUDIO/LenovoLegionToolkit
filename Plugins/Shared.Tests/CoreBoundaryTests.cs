using System;
using System.Linq;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.Shared.Tests;

public sealed class CoreBoundaryTests
{
    [Fact]
    public void SharedCore_DoesNotReferenceWpfAssemblies()
    {
        var references = typeof(UniversalDeviceToolkit.Plugins.Core.Constants).Assembly
            .GetReferencedAssemblies()
            .Select(static assembly => assembly.Name)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        Assert.DoesNotContain("PresentationFramework", references, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("PresentationCore", references, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("WindowsBase", references, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.Xaml", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void SharedCore_ContainsRuntimeServicesUsedBySdk()
    {
        Assert.NotNull(typeof(UniversalDeviceToolkit.Plugins.Core.HttpClientManager));
        Assert.NotNull(typeof(UniversalDeviceToolkit.Plugins.Core.ProcessRunner));
        Assert.NotNull(typeof(UniversalDeviceToolkit.Plugins.Core.PluginLog));
        Assert.NotNull(typeof(UniversalDeviceToolkit.Plugins.Core.SettingsManager<>));
    }
}
