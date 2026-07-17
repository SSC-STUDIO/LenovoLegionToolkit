using FluentAssertions;
using UniversalDeviceToolkit.WPF.Pages;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class AutomationPageTests
{
    [Fact]
    public void AutomationPage_ShouldUseCancelableLatestWinsLoading()
    {
        ReadAutomationPageSource()
            .Should()
            .Contain("CancellationTokenSource")
            .And.Contain("refreshVersion")
            .And.Contain("_hasLoadedContent");
    }

    [Fact]
    public void AutomationPage_ShouldNotUseFixedLoadingDelays()
    {
        ReadAutomationPageSource()
            .Should()
            .NotContain("GetAutomationFallbackLoadingDelay")
            .And.NotContain("Task.Delay(");
    }

    [Fact]
    public void AutomationPage_EnableToggle_ShouldExposeStableAutomationId()
    {
        var root = RepositoryPaths.FindRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Pages", "AutomationPage.xaml"));
        xaml.Should().Contain("AutomationProperties.AutomationId=\"AutomationEnableAutomaticPipelinesToggle\"");
    }

    private static string ReadAutomationPageSource()
    {
        var root = RepositoryPaths.FindRoot();
        return File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Pages", "AutomationPage.xaml.cs"));
    }

}
