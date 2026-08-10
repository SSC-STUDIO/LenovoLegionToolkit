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
        Assert.DoesNotContain("UniversalDeviceToolkit.Plugins.Shared", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void SharedCore_ContainsRuntimeServicesUsedBySdk()
    {
        Assert.NotNull(typeof(UniversalDeviceToolkit.Plugins.Core.HttpClientManager));
        Assert.NotNull(typeof(UniversalDeviceToolkit.Plugins.Core.ProcessRunner));
        Assert.NotNull(typeof(UniversalDeviceToolkit.Plugins.Core.PluginLog));
        Assert.NotNull(typeof(UniversalDeviceToolkit.Plugins.Core.SettingsManager<>));
    }

    [Fact]
    public void SharedCore_DoesNotContainWpfHelperTypes()
    {
        // WpfFallbackHelper / WpfHostNotifications live in the WPF-flavored
        // Plugins\Shared library only; the portable Shared.Core must stay free
        // of WPF code so the portable plugin flavor can reference it.
        var sharedCoreAssembly = typeof(UniversalDeviceToolkit.Plugins.Core.Constants).Assembly;

        Assert.Null(sharedCoreAssembly.GetType("UniversalDeviceToolkit.Plugins.Core.WpfFallbackHelper", throwOnError: false));
        Assert.Null(sharedCoreAssembly.GetType("UniversalDeviceToolkit.Plugins.Core.WpfHostNotifications", throwOnError: false));
        Assert.Null(sharedCoreAssembly.GetType("UniversalDeviceToolkit.Plugins.Shared.WpfFallbackHelper", throwOnError: false));
        Assert.Null(sharedCoreAssembly.GetType("UniversalDeviceToolkit.Plugins.Shared.WpfHostNotifications", throwOnError: false));
    }
}
