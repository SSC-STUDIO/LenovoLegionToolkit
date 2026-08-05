using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public class NetworkAccelerationUiGuardTests
{
    [Fact]
    public void NetworkAccelerationControl_Xaml_HasCoreAutomationIds()
    {
        var xaml = File.ReadAllText(FindControlXaml());

        xaml.Should().Contain("NetworkAccelerationPageScrollViewer");
        xaml.Should().Contain("NetworkAccelerationControlCard");
        xaml.Should().Contain("NetworkAccelerationDomainsCard");
        xaml.Should().Contain("NetworkAccelerationNatSection");
        xaml.Should().Contain("NetworkAccelerationDnsSection");
        xaml.Should().Contain("NetworkAccelerationIpv6Section");
        xaml.Should().Contain("NetworkAccelerationNatDetectButton");
        xaml.Should().Contain("NetworkAccelerationDnsDetectButton");
        xaml.Should().Contain("NetworkAccelerationIpv6DetectButton");
        xaml.Should().Contain("NetworkAccelerationAdvancedExpander");
        xaml.Should().Contain("NetworkAccelerationModeSelector");
        xaml.Should().NotContain("NetworkAccelerationLatencyMetric");
        xaml.Should().NotContain("NetworkAccelerationUploadMetric");
        xaml.Should().NotContain("NetworkAccelerationDownloadMetric");
        xaml.Should().NotContain("NetworkAccelerationConnectionsMetric");
        xaml.Should().Contain("NetworkAccelerationRestoreButton");
        // Status pill and primary action button removed in Watt Toolkit redesign.
        xaml.Should().NotContain("NetworkAccelerationPrimaryActionButton");
        xaml.Should().NotContain("NetworkAccelerationStatusIndicator");
        xaml.Should().NotContain("NetworkAccelerationStatusPill");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_UsesExplicitRuntimeDashboard()
    {
        var xaml = File.ReadAllText(FindControlXaml());

        xaml.Should().Contain("NetworkAccelerationRunStatusBar");
        xaml.Should().NotContain("NetworkAccelerationRunButton");
        xaml.Should().Contain("x:Name=\"_trafficSection\" Margin=\"0,12,0,0\" Visibility=\"Collapsed\"");
        xaml.Should().Contain("NetworkAccelerationConnectionsList");
        xaml.Should().Contain("NetworkAccelerationDestinationsList");
        xaml.Should().Contain("NetworkAccelerationTargetSearchBox");
    }

    [Fact]
    public void NetworkAccelerationControl_Code_DoesNotStartFromSelectionChanges()
    {
        var code = File.ReadAllText(FindControlCode());

        code.Should().NotContain("AutoToggleAccelerationAsync");
        code.Should().Contain("ToggleAccelerationFromToolbarAsync");
        code.Should().Contain("GetRuntimeSnapshotAsync");
        code.Should().Contain("StartRuntimePolling");
        code.Should().Contain("ClearRuntimeLists");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_ServiceListAndSelectionHint()
    {
        var xaml = File.ReadAllText(FindControlXaml());
        xaml.Should().Contain("NetworkAccelerationDomainsCard");
        xaml.Should().Contain("NetworkAccelerationPage_SelectionHint");
        xaml.Should().NotContain("NetworkAccelerationSelectionBar");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_HasDiagnosticsSections()
    {
        var xaml = File.ReadAllText(FindControlXaml());
        // Watt Toolkit-style diagnostics: NAT, DNS, IPv6 sections with detect buttons.
        xaml.Should().Contain("NetworkAccelerationNatSection");
        xaml.Should().Contain("NetworkAccelerationDnsSection");
        xaml.Should().Contain("NetworkAccelerationIpv6Section");
        xaml.Should().Contain("NetworkAccelerationNatDetectButton");
        xaml.Should().Contain("NetworkAccelerationDnsDetectButton");
        xaml.Should().Contain("NetworkAccelerationIpv6DetectButton");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_HasNaDiagResourceStrings()
    {
        var xaml = File.ReadAllText(FindControlXaml());
        // Diagnostics panel uses NaDiag_ resource keys.
        xaml.Should().Contain("NaDiag_NatTitle");
        xaml.Should().Contain("NaDiag_DnsTitle");
        xaml.Should().Contain("NaDiag_Ipv6Title");
        xaml.Should().Contain("NaDiag_Detect");
    }

    [Fact]
    public void NetworkAccelerationControl_Code_HasNatDnsIpv6Handlers()
    {
        var code = File.ReadAllText(FindControlCode());
        code.Should().Contain("NatDetectButton_Click");
        code.Should().Contain("DnsDetectButton_Click");
        code.Should().Contain("Ipv6DetectButton_Click");
        code.Should().Contain("BuildServiceList");
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
        xaml.Should().NotContain("x:Static resources:Resource.NetworkAccelerationPage_Metric_Latency");
        xaml.Should().NotContain("x:Static resources:Resource.NetworkAccelerationPage_MetricsHeading");
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
        // The mode selector remains grouped with its label in the status-bar Grid;
        // the primary action lives in the page toolbar.
        var modeIdx = xaml.IndexOf("NetworkAccelerationModeSelector", StringComparison.Ordinal);
        modeIdx.Should().BeGreaterThan(0);
        var modeContainerStart = xaml.LastIndexOf("<StackPanel", modeIdx, StringComparison.Ordinal);
        modeContainerStart.Should().BeGreaterThan(0);
        var modeContainer = xaml.Substring(modeContainerStart, modeIdx - modeContainerStart);
        modeContainer.Should().Contain("Grid.Column=\"2\"");
        modeContainer.Should().NotContain("<WrapPanel");
        xaml.Should().NotContain("AutomationProperties.AutomationId=\"NetworkAccelerationRunButton\"");
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
        // Hosts is not selectable: the combo only contains SystemProxy + DiagnosticsOnly.
        // Only the BuildModeCombo method body — not ToSelectableMode / Start coerce helpers / their docs.
        var buildStart = code.IndexOf("private void BuildModeCombo()", StringComparison.Ordinal);
        buildStart.Should().BeGreaterThan(0);
        // End at the closing brace of BuildModeCombo (before ToSelectableMode docs or next method).
        var afterBuild = code.IndexOf("private static NetworkAccelerationMode ToSelectableMode", StringComparison.Ordinal);
        if (afterBuild < 0)
            afterBuild = code.IndexOf("private void BuildServiceList()", StringComparison.Ordinal);
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
    public void NetworkAccelerationControl_Code_HasServiceListBuilder()
    {
        var code = File.ReadAllText(FindControlCode());
        code.Should().Contain("BuildServiceList");
        code.Should().Contain("CreateServiceGroupRow");
        code.Should().Contain("CreateSubItemRow");
    }

    [Fact]
    public void NetworkAccelerationControl_Xaml_DiagnosticPanelsCollapsedByDefault()
    {
        var xaml = File.ReadAllText(FindControlXaml());
        // Diagnostics are opened on demand in dedicated Popup surfaces.
        Regex.Matches(xaml, "<Popup x:Name=\"_(nat|dns|ipv6)DetailsPopup\"").Count.Should().Be(3);
        Regex.Matches(xaml, "IsOpen=\"False\"").Count.Should().Be(3);
        Regex.Matches(xaml, "StaysOpen=\"False\"").Count.Should().Be(3);
        xaml.Should().NotContain("<custom:CardExpander");
        xaml.Should().NotContain("CollapseArrow");
        xaml.Should().NotContain("_natContentPanel");
        xaml.Should().NotContain("_dnsContentPanel");
        xaml.Should().NotContain("_ipv6ContentPanel");
        // Collapsing the surfaces must not remove diagnostics AutomationIds.
        xaml.Should().Contain("NetworkAccelerationNatSection");
        xaml.Should().Contain("NetworkAccelerationDnsSection");
        xaml.Should().Contain("NetworkAccelerationIpv6Section");
        xaml.Should().Contain("NetworkAccelerationNatDetectButton");
        xaml.Should().Contain("NetworkAccelerationDnsDetectButton");
        xaml.Should().Contain("NetworkAccelerationIpv6DetectButton");
    }

    [Fact]
    public void NetworkAccelerationControl_Code_WiresDiagnosticStatusButtonsToPopups()
    {
        var code = File.ReadAllText(FindControlCode());
        code.Should().Contain("NatStatus_Click");
        code.Should().Contain("DnsStatus_Click");
        code.Should().Contain("Ipv6Status_Click");
        code.Should().Contain("ShowDiagnosticPopup");
        code.Should().Contain("CloseDiagnosticPopups");
        code.Should().Contain("ToggleAccelerationFromToolbarAsync");
        code.Should().Contain("TargetSearchBox_TextChanged");
    }

    [Fact]
    public void NetworkAccelerationSelectedTargetInfo_ShouldBeRemoved()
    {
        var root = FindRepoRoot();
        var pageXaml = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Pages",
            "WindowsOptimizationPage.xaml"));
        var pageCode = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Pages",
            "WindowsOptimizationPage.xaml.cs"));
        var controlCode = File.ReadAllText(FindControlCode());

        pageXaml.Should().NotContain("NetworkAccelerationSelectionSummaryBar");
        pageXaml.Should().NotContain("NetworkAccelerationSelectionCount");
        pageCode.Should().NotContain("NetworkAccelerationSelectedTargetsWindow");
        pageCode.Should().NotContain("GetSelectedTargetSummaries");
        controlCode.Should().NotContain("GetSelectedTargetSummaries");
        File.Exists(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Windows",
            "Utils",
            "NetworkAccelerationSelectedTargetsWindow.xaml")).Should().BeFalse();
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
