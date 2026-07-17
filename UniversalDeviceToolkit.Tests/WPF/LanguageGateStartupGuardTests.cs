using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
[Trait("Category", TestCategories.Guard)]
public sealed class LanguageGateStartupGuardTests
{
    [Fact]
    public void Startup_ShouldRunLanguageGateBeforeMainWindowCreation()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Startup", "StartupOrchestrator.cs");
        var runStart = source.IndexOf("public async Task<int> RunAsync()", StringComparison.Ordinal);
        runStart.Should().BeGreaterThanOrEqualTo(0);

        var runEnd = source.IndexOf("private void RunStartupResetSwitchesIfRequested", StringComparison.Ordinal);
        runEnd.Should().BeGreaterThan(runStart);
        var runBody = source[runStart..runEnd];

        var gateIndex = runBody.IndexOf("RunLanguageGateAsync", StringComparison.Ordinal);
        var iocIndex = runBody.IndexOf("InitializeIoCAsync", StringComparison.Ordinal);
        var createIndex = runBody.IndexOf("CreateMainWindowAsync", StringComparison.Ordinal);
        var showIndex = runBody.IndexOf("ShowMainWindowAsync", StringComparison.Ordinal);

        gateIndex.Should().BeGreaterThanOrEqualTo(0);
        iocIndex.Should().BeGreaterThan(gateIndex);
        createIndex.Should().BeGreaterThan(iocIndex);
        showIndex.Should().BeGreaterThan(createIndex);
    }

    [Fact]
    public void LanguageSelector_ShouldExposeRetryContinueEnglishAndExitOnFailure()
    {
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Utils", "LanguageSelectorWindow.xaml");
        xaml.Should().Contain("_failureActionsPanel");
        xaml.Should().Contain("Retry_Click");
        xaml.Should().Contain("ContinueEnglish_Click");
        xaml.Should().Contain("Exit_Click");

        var code = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Utils", "LanguageSelectorWindow.xaml.cs");
        code.Should().Contain("LanguageGateOutcome");
        code.Should().NotContain("_taskCompletionSource.TrySetResult(_fallbackLanguage);\r\n            Close();");
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var expectedRelativePath = Path.Combine(pathParts);
        foreach (var candidateRoot in GetRepositoryRootCandidates())
        {
            var path = Path.Combine(candidateRoot, expectedRelativePath);
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        throw new DirectoryNotFoundException($"Could not locate repository file '{expectedRelativePath}'.");
    }

    private static IEnumerable<string> GetRepositoryRootCandidates()
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
        };

        foreach (var root in roots.Where(static root => !string.IsNullOrWhiteSpace(root)))
        {
            var directory = new DirectoryInfo(root!);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                    yield return directory.FullName;

                directory = directory.Parent;
            }
        }
    }
}
