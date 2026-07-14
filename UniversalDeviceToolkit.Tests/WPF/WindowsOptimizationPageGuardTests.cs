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

    [Fact]
    public void CategorySettingsButton_ShouldBindHasSettingsFromCardExpanderCategoryViewModel()
    {
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "WindowsOptimizationPage.xaml");
        // Gear only when OptimizationCategoryViewModel.HasSettings is true.
        // Self DataContext is required for WPF-UI PressedForeground; category fields must
        // bind via CardExpander.DataContext — NOT Grid (Grid has neither HasSettings nor PluginId).
        xaml.Should().Contain("DataContext.HasSettings");
        xaml.Should().Contain("AncestorType=custom:CardExpander");
        xaml.Should().Contain("DataContext.PluginId");
        xaml.Should().NotContain("HasSettings, Converter={StaticResource BoolToVisibilityConverter}, RelativeSource={RelativeSource AncestorType=Grid}");
        xaml.Should().NotContain("PluginId, StringFormat=WindowsOptimizationCategorySettings_{0}, RelativeSource={RelativeSource AncestorType=Grid}");
        xaml.Should().Contain("OpenStyleSettingsButton_Click");
    }

    [Fact]
    public void CleanupAndDriverControls_ShouldExposeStableAutomationIds()
    {
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "WindowsOptimizationPage.xaml");
        xaml.Should().Contain("WindowsOptimizationRunCleanupButton");
        xaml.Should().Contain("WindowsOptimizationReScanCleanupButton");
        xaml.Should().Contain("WindowsOptimizationDriverBrowseButton");
        xaml.Should().Contain("WindowsOptimizationDriverOpenFolderButton");
        xaml.Should().Contain("WindowsOptimizationDriverOsComboBox");
        xaml.Should().Contain("WindowsOptimizationDriverDownloadToTextBox");
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
