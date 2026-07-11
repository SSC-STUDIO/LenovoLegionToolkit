using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class WindowsOptimizationPageGuardTests
{
    [Fact]
    public void LoadedHandler_ShouldRefreshCategoriesBeforeApplyingPendingPluginFocus()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "WindowsOptimizationPage.xaml.cs");
        var loadedHandlerStart = source.IndexOf("private void WindowsOptimizationPage_Loaded", System.StringComparison.Ordinal);
        var focusMethodStart = source.IndexOf("private void SyncNavButtonToCurrentMode", System.StringComparison.Ordinal);
        loadedHandlerStart.Should().BeGreaterThanOrEqualTo(0);
        focusMethodStart.Should().BeGreaterThan(loadedHandlerStart);

        var loadedHandler = source[loadedHandlerStart..focusMethodStart];
        loadedHandler.Should().Contain("ViewModel.Initialize();");
        loadedHandler.IndexOf("ViewModel.Initialize();", System.StringComparison.Ordinal)
            .Should()
            .BeLessThan(loadedHandler.IndexOf("TryApplyPendingPluginFocusRequest();", System.StringComparison.Ordinal));
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.WPF")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the test must run from inside the repository tree");
        return File.ReadAllText(Path.Combine([directory!.FullName, .. pathParts]));
    }
}
