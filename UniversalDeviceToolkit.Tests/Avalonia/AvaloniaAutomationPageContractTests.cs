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

    [Fact]
    public void AutomationPage_ShouldExposeVisibleFeedbackForHostFailures()
    {
        var root = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "AutomationPage.axaml"));
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "AutomationPage.axaml.cs"));

        Assert.Contains("x:Name=\"FeedbackBar\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationFeedback", xaml, StringComparison.Ordinal);
        Assert.Contains("FeedbackBar.IsVisible = true", source, StringComparison.Ordinal);
        Assert.Contains("FeedbackBar.Classes.Add(variant)", source, StringComparison.Ordinal);
        Assert.Contains("SetFeedback(", source, StringComparison.Ordinal);
        Assert.Contains("\"success\"", source, StringComparison.Ordinal);
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
