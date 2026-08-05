using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
[Trait("Category", TestCategories.Guard)]
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
        loadedHandler.Should().Contain("RunInitialCategoriesLoadAsync(scanVersion, scanCancellation);");
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
        xaml.Should().Contain("WindowsOptimizationDriverMachineTypeTextBox");
        xaml.Should().Contain("WindowsOptimizationDriverSearchButton");
    }

    [Fact]
    public void CleanupCompletion_ShouldRestoreRunButtonText()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "WindowsOptimizationPage.Cleanup.cs");
        source.Should().Contain("ViewModel.ResetRunCleanupButtonText();");
        source.Should().NotContain("ViewModel.RunCleanupButtonText = string.Empty;");
    }

    [Fact]
    public void DriverFilterForm_ShouldNotBeStuffedIntoCardControlHeader()
    {
        // Multi-field filter forms belong in card body/surface chrome, not CardControl.Header
        // (Header is for title/subtitle rows; stuffing forms there leaves empty body/icon chrome).
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "WindowsOptimizationPage.xaml");
        AssertAutomationIdNotInsideCardControlHeader(xaml, "WindowsOptimizationDriverMachineTypeTextBox");
        AssertAutomationIdNotInsideCardControlHeader(xaml, "WindowsOptimizationDriverSearchButton");
    }

    private static void AssertAutomationIdNotInsideCardControlHeader(string xaml, string automationId)
    {
        var idIndex = xaml.IndexOf(automationId, System.StringComparison.Ordinal);
        idIndex.Should().BeGreaterThanOrEqualTo(0, $"expected AutomationId {automationId}");

        var searchFrom = 0;
        while (true)
        {
            var headerOpen = xaml.IndexOf("CardControl.Header", searchFrom, System.StringComparison.Ordinal);
            if (headerOpen < 0 || headerOpen > idIndex)
                break;

            var headerClose = xaml.IndexOf("/custom:CardControl.Header", headerOpen, System.StringComparison.Ordinal);
            if (headerClose < 0)
                headerClose = xaml.IndexOf("/wpfui:CardControl.Header", headerOpen, System.StringComparison.Ordinal);
            if (headerClose < 0)
                break;

            (idIndex > headerOpen && idIndex < headerClose)
            .Should()
            .BeFalse($"{automationId} must not live inside CardControl.Header");

            searchFrom = headerClose + 1;
        }
    }

    [Fact]
    public void OptimizationToolbar_ShouldPlaceSelectionSummaryAboveActionsAndOmitCancel()
    {
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "WindowsOptimizationPage.xaml");
        var code = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "WindowsOptimizationPage.xaml.cs");

        xaml.Should().Contain("WindowsOptimizationApplyButton");
        xaml.Should().Contain("WindowsOptimizationSelectedActionsSummaryBar");
        xaml.Should().Contain("WindowsOptimizationActionBar");
        xaml.Should().NotContain("WindowsOptimizationCancelButton");
        xaml.Should().Contain("IsEnabled=\"{Binding CanApplyOptimizationChanges}\"");
        xaml.Should().Contain("IsEnabled=\"{Binding CanSelectRecommended}\"");
        xaml.Should().Contain("IsEnabled=\"{Binding CanEdit}\"");
        code.Should().Contain("ApplyOptimizationButton_Click");
        code.Should().NotContain("CancelOptimizationButton_Click");
        code.Should().Contain("_optimizationStateScanCancellationTokenSource");
        code.Should().Contain("BeginOptimizationStateScan");
        code.Should().Contain("EndOptimizationStateScan");

        var selectedSummaryIndex = xaml.IndexOf("WindowsOptimizationSelectedActionsButton", StringComparison.Ordinal);
        var selectedSummaryClose = xaml.IndexOf("</wpfui:Button>", selectedSummaryIndex, StringComparison.Ordinal);
        var actionRowIndex = xaml.IndexOf("<StackPanel Orientation=\"Horizontal\">", selectedSummaryClose, StringComparison.Ordinal);

        selectedSummaryIndex.Should().BeGreaterThanOrEqualTo(0);
        selectedSummaryClose.Should().BeGreaterThan(selectedSummaryIndex);
        actionRowIndex.Should().BeGreaterThan(selectedSummaryClose);

        var selectedSummaryFrameClose = xaml.IndexOf("</Border>", selectedSummaryClose, StringComparison.Ordinal);
        var actionFrameIndex = xaml.IndexOf("WindowsOptimizationActionBar", selectedSummaryFrameClose, StringComparison.Ordinal);
        selectedSummaryFrameClose.Should().BeGreaterThan(selectedSummaryClose);
        actionFrameIndex.Should().BeGreaterThan(selectedSummaryFrameClose);
    }

    [Fact]
    public void NetworkAccelerationToolbar_ShouldPlaceSelectionSummaryAboveActions()
    {
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "WindowsOptimizationPage.xaml");

        xaml.Should().Contain("NetworkAccelerationSelectionSummaryBar");
        var summaryFrameIndex = xaml.IndexOf("NetworkAccelerationSelectionSummaryBar", StringComparison.Ordinal);
        var selectionBarIndex = xaml.IndexOf("NetworkAccelerationSelectionBar", StringComparison.Ordinal);
        var selectionCountIndex = xaml.IndexOf("NetworkAccelerationSelectionCountButton_Click", summaryFrameIndex, StringComparison.Ordinal);
        var selectionCountClose = xaml.IndexOf("</wpfui:Button>", selectionCountIndex, StringComparison.Ordinal);
        var actionRowIndex = xaml.IndexOf("<StackPanel Orientation=\"Horizontal\">", selectionCountClose, StringComparison.Ordinal);

        summaryFrameIndex.Should().BeGreaterThanOrEqualTo(0);
        selectionBarIndex.Should().BeGreaterThan(summaryFrameIndex);
        selectionCountIndex.Should().BeGreaterThan(summaryFrameIndex);
        selectionCountClose.Should().BeGreaterThan(selectionCountIndex);
        actionRowIndex.Should().BeGreaterThan(selectionCountClose);

        var summaryFrameClose = xaml.IndexOf("</Border>", summaryFrameIndex, StringComparison.Ordinal);
        summaryFrameClose.Should().BeGreaterThan(summaryFrameIndex);
        selectionBarIndex.Should().BeGreaterThan(summaryFrameClose);
    }

    [Fact]
    public void RecommendedButton_ShouldOnlyChangePendingSelection()
    {
        var code = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "WindowsOptimizationPage.xaml.cs");
        var handler = ExtractMethod(code, "private void SelectRecommendedButton_Click(");

        handler.Should().Contain("ViewModel.SelectRecommended();");
        handler.Should().NotContain("ApplyActionAsync");
        handler.Should().NotContain("ApplyOptimizationChangesAsync");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, System.StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        var braceStart = source.IndexOf('{', start);
        braceStart.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{')
                depth++;
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return source[start..(i + 1)];
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Environment.CurrentDirectory,
            AppContext.BaseDirectory
        };

        foreach (var root in roots.Where(static root => !string.IsNullOrWhiteSpace(root)))
        {
            var directory = new DirectoryInfo(root!);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.WPF")))
                    return File.ReadAllText(Path.Combine([directory.FullName, .. pathParts]));

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
