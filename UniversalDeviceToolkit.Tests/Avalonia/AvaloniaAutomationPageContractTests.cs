using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AvaloniaAutomationPageContractTests
{
    [Fact]
    public void AutomationPage_ShouldExposeWpfPipelineOrderingAndManualIconEditing()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "AutomationPage.axaml.cs"));

        Assert.Contains("MovePipeline(PipelineRow row, int delta)", source, StringComparison.Ordinal);
        Assert.Contains("candidate.IsAutomatic == row.IsAutomatic", source, StringComparison.Ordinal);
        Assert.Contains("ManualPipelineIcons", source, StringComparison.Ordinal);
        Assert.Contains("AutomationPage_ChangeIcon", source, StringComparison.Ordinal);
        Assert.Contains("NormalizeIconName(row.IconEditor?.Text)", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
