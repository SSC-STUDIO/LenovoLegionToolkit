using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

public class NetworkAccelerationUiGuardTests
{
    [Fact]
    public void DomainGroupsSummary_ShouldSupportCurrentAndLegacyPlaceholderShapes()
    {
        NetworkAccelerationControl.FormatDomainGroupsSummary(
                "{0}/{1} groups enabled · {2} domains",
                "Domain groups",
                1,
                3,
                14)
            .Should().Be("1/3 groups enabled · 14 domains");

        NetworkAccelerationControl.FormatDomainGroupsSummary(
                "{0}: {1}/{2} enabled, {3} domains",
                "Domain groups",
                1,
                3,
                14)
            .Should().Be("Domain groups: 1/3 enabled, 14 domains");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_HasCoreAutomationIdsAndPrimaryAction()
    {
        var xaml = File.ReadAllText(FindControlXaml());

        xaml.Should().Contain("NetworkAccelerationPageScrollViewer");
        xaml.Should().Contain("NetworkAccelerationControlCard");
        xaml.Should().Contain("NetworkAccelerationDomainsCard");
        xaml.Should().Contain("NetworkAccelerationDiagnosticsCard");
        xaml.Should().Contain("NetworkAccelerationDiagnosticsButton");
        xaml.Should().Contain("NetworkAccelerationDiagnosticsText");
        xaml.Should().Contain("NetworkAccelerationAdvancedExpander");
        xaml.Should().Contain("NetworkAccelerationPrimaryActionButton");
        xaml.Should().Contain("NetworkAccelerationStatusIndicator");
        xaml.Should().Contain("NetworkAccelerationModeSelector");
        xaml.Should().Contain("NetworkAccelerationLatencyMetric");
        xaml.Should().Contain("NetworkAccelerationUploadMetric");
        xaml.Should().Contain("NetworkAccelerationDownloadMetric");
        xaml.Should().Contain("NetworkAccelerationConnectionsMetric");
        xaml.Should().Contain("NetworkAccelerationRulesMetric");
        xaml.Should().Contain("NetworkAccelerationRestoreButton");
        // Selection chrome lives on WindowsOptimizationPage tab toolbar (not floating in the control).
        xaml.Should().NotContain("NetworkAccelerationSelectionBar");
    }

    [Fact]
    public void WindowsOptimizationPage_Xaml_HostsNetworkAccelerationSelectionBarInTabToolbar()
    {
        var pageXaml = File.ReadAllText(FindPageXaml());
        pageXaml.Should().Contain("NetworkAccelerationSelectionBar");
        pageXaml.Should().Contain("NetworkAccelerationSelectionCount");
        pageXaml.Should().Contain("NetworkAccelerationSelectionFavoriteButton");
        pageXaml.Should().Contain("NetworkAccelerationSelectionStartButton");
        pageXaml.Should().Contain("_networkAccelerationControl");
        pageXaml.Should().Contain("IsNetworkAccelerationMode");
        // Visual parity with bulk SelectedActions chrome (Tertiary + Card + wpfui Secondary).
        pageXaml.Should().Contain("ControlFillColorTertiaryBrush");
        pageXaml.Should().Contain("CornerRadiusCard");
        pageXaml.Should().Contain("DocumentBulletList24");
        pageXaml.Should().Contain("Star24");
        pageXaml.Should().Contain("Play24");
    }

    [Fact]
    public void FormatSelectionCount_ShouldFormatZeroAndNonZero()
    {
        NetworkAccelerationControl.FormatSelectionCount("0 items selected", "{0} items selected", 0)
            .Should().Be("0 items selected");
        NetworkAccelerationControl.FormatSelectionCount("0 items selected", "{0} items selected", 2)
            .Should().Be("2 items selected");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_SelectionBarUsesResourceStrings()
    {
        var controlXaml = File.ReadAllText(FindControlXaml());
        var pageXaml = File.ReadAllText(FindPageXaml());
        var code = File.ReadAllText(FindControlCode());
        controlXaml.Should().Contain("NetworkAccelerationPage_SelectionHint");
        pageXaml.Should().Contain("NetworkAccelerationPage_SelectionBar");
        pageXaml.Should().Contain("NetworkAccelerationPage_SelectionCountZero");
        pageXaml.Should().Contain("NetworkAccelerationPage_SelectionFavorite");
        pageXaml.Should().Contain("NetworkAccelerationPage_SelectionStart");
        // Handlers stay on the control; chrome is hosted by the page tab toolbar.
        code.Should().Contain("SelectionFavoriteButton_Click");
        code.Should().Contain("SelectionStartButton_Click");
        code.Should().Contain("AttachSelectionChrome");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_AdvancedIsCollapsedByDefault()
    {
        var xaml = File.ReadAllText(FindControlXaml());
        xaml.Should().Contain("IsExpanded=\"False\"");
        xaml.Should().Contain("NetworkAccelerationAdvancedExpander");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_RestoreEntryStillExists()
    {
        var xaml = File.ReadAllText(FindControlXaml());
        xaml.Should().Contain("NetworkAccelerationRestoreButton");
        xaml.Should().Contain("NetworkAccelerationPage_RestoreNetwork");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_UsesResourceStringsNotHardcodedCopyBlocks()
    {
        var xaml = File.ReadAllText(FindControlXaml());
        // Status/metrics/sections come from resources. Page title is chrome-only (not a plugin).
        xaml.Should().Contain("x:Static resources:Resource.NetworkAccelerationPage_State_Idle");
        xaml.Should().Contain("x:Static resources:Resource.NetworkAccelerationPage_Metric_Latency");
        xaml.Should().Contain("x:Static resources:Resource.NetworkAccelerationPage_MetricsHeading");
        xaml.Should().Contain("x:Static resources:Resource.NetworkAccelerationPage_TargetsHeading");
        xaml.Should().Contain("x:Static resources:Resource.NetworkAccelerationPage_DangerZoneHeading");
        xaml.Should().NotContain("Text=\"Overview\"");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_DoesNotRepeatPageTitleOrIcon()
    {
        var xaml = File.ReadAllText(FindControlXaml());
        // Built-in feature: WindowsOptimization page chrome already shows title — no Rocket/title hero.
        xaml.Should().NotContain("Rocket24");
        xaml.Should().NotContain("NetworkAccelerationPage_Title");
        xaml.Should().NotContain("SymbolIcon");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_NoLiteralCornerRadiusDigits()
    {
        var lines = File.ReadAllLines(FindControlXaml());
        var offenders = lines
            .Select((line, index) => (line, index))
            .Where(t => Regex.IsMatch(t.line, @"CornerRadius\s*=\s*""\d") &&
                        !t.line.Contains("StaticResource", StringComparison.Ordinal) &&
                        !t.line.Contains("DynamicResource", StringComparison.Ordinal))
            .Select(t => $"{t.index + 1}:{t.line.Trim()}")
            .ToArray();

        offenders.Should().BeEmpty();
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_SingleMainSurfaceCard()
    {
        var xaml = File.ReadAllText(FindControlXaml());
        // One main surface AutomationId for the control card; no nested peer CardControl stack.
        Regex.Matches(xaml, "NetworkAccelerationControlCard").Count.Should().Be(1);
        xaml.Should().NotContain("custom:CardControl");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_PrimaryActionAndModeSelectorShareGridNotWrapPanel()
    {
        var xaml = File.ReadAllText(FindControlXaml());
        // Start button + mode combo must not share a WrapPanel (overlap with long mode labels).
        xaml.Should().Contain("NetworkAccelerationPrimaryActionButton");
        xaml.Should().Contain("NetworkAccelerationModeSelector");
        // Mode selector lives in a Grid column next to the primary action.
        var modeIdx = xaml.IndexOf("NetworkAccelerationModeSelector", StringComparison.Ordinal);
        modeIdx.Should().BeGreaterThan(0);
        var slice = xaml.Substring(Math.Max(0, modeIdx - 400), Math.Min(500, xaml.Length - Math.Max(0, modeIdx - 400)));
        slice.Should().Contain("Grid.Column=\"1\"");
    }

    [Fact]
    public void NetworkAccelerationControl_Code_UsesDataModeOptionsNotComboBoxItemItems()
    {
        var code = File.ReadAllText(FindControlCode());
        // Closed ComboBox SelectionBoxItem double-paints when Items are ComboBoxItem controls.
        code.Should().Contain("sealed class ModeOption");
        code.Should().Contain("new ModeOption(");
        code.Should().NotContain("new ComboBoxItem");
    }

    [Fact]
    public void NetworkAccelerationControl_Code_OmitsHostsFromSelectableModes()
    {
        var code = File.ReadAllText(FindControlCode());
        // Hosts is refused at the service layer; do not offer it in the mode combo.
        code.Should().Contain("ToSelectableMode");
        code.Should().Contain("HostsDisabledNote");
        // Only the BuildModeCombo method body — not ToSelectableMode / Start coerce helpers / their docs.
        var buildStart = code.IndexOf("private void BuildModeCombo()", StringComparison.Ordinal);
        buildStart.Should().BeGreaterThan(0);
        // End at the closing brace of BuildModeCombo (before ToSelectableMode docs or next method).
        var afterBuild = code.IndexOf("private static NetworkAccelerationMode ToSelectableMode", StringComparison.Ordinal);
        if (afterBuild < 0)
            afterBuild = code.IndexOf("private void BuildDomainGroupTiles()", StringComparison.Ordinal);
        afterBuild.Should().BeGreaterThan(buildStart);
        // Slice only the method: stop at the last '}' before ToSelectableMode so XML docs are excluded.
        var slice = code.Substring(buildStart, afterBuild - buildStart);
        var lastBrace = slice.LastIndexOf('}');
        lastBrace.Should().BeGreaterThan(0);
        var buildBody = slice.Substring(0, lastBrace + 1);
        buildBody.Should().NotContain("NetworkAccelerationMode.Hosts");
        buildBody.Should().Contain("NetworkAccelerationMode.SystemProxy");
        buildBody.Should().Contain("NetworkAccelerationMode.DiagnosticsOnly");
        // Items.Add calls for modes: SystemProxy + DiagnosticsOnly only (no Hosts ModeOption).
        Regex.Matches(buildBody, @"new ModeOption\(").Count.Should().Be(2);
    }

    [Fact]
    public void NetworkAccelerationControl_Code_SelectionBarHasFavoriteAndStartHandlers()
    {
        var code = File.ReadAllText(FindControlCode());
        code.Should().Contain("_selectedGroupIds");
        code.Should().Contain("SelectionFavoriteButton_Click");
        code.Should().Contain("SelectionStartButton_Click");
        code.Should().Contain("ToggleFavoriteForIdsAsync");
        code.Should().Contain("IsFavorite");
        // Start selected enables groups then starts acceleration.
        code.Should().Contain("group.Enabled = true");
    }

    private static string FindControlCode()
    {
        var root = FindRepoRoot();
        return Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Pages",
            "WindowsOptimization",
            "NetworkAccelerationControl.xaml.cs");
    }

    private static string FindControlXaml()
    {
        var root = FindRepoRoot();
        return Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Pages",
            "WindowsOptimization",
            "NetworkAccelerationControl.xaml");
    }

    private static string FindPageXaml()
    {
        var root = FindRepoRoot();
        return Path.Combine(root, "UniversalDeviceToolkit.WPF", "Pages", "WindowsOptimizationPage.xaml");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "UniversalDeviceToolkit.sln")) ||
                File.Exists(Path.Combine(dir.FullName, "UniversalDeviceToolkit.WPF", "UniversalDeviceToolkit.WPF.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate UniversalDeviceToolkit repo root.");
    }
}
