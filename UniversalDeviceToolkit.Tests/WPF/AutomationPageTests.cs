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

    private static string ReadAutomationPageSource()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Pages", "AutomationPage.xaml.cs"));
    }

    private static string FindRepositoryRoot()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidate in candidates.Where(static candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            var current = Path.GetFullPath(candidate!);
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "UniversalDeviceToolkit.sln")))
                    return current;

                current = Directory.GetParent(current)?.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate UniversalDeviceToolkit.sln.");
    }
}
