using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

public class NetworkAccelerationUiGuardTests
{
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
        xaml.Should().Contain("x:Static resources:Resource.NetworkAccelerationPage_TargetsHeading");
        xaml.Should().Contain("x:Static resources:Resource.NetworkAccelerationPage_DangerZoneHeading");
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
