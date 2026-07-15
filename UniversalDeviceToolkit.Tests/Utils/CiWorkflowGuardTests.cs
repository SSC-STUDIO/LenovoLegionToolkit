using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

public sealed class CiWorkflowGuardTests
{
    [Fact]
    public void CiTestsWorkflow_ShouldGateMainAppPluginUiSmokeContract()
    {
        var workflow = ReadRepositoryFile(".github", "workflows", "Ci-tests.yml");

        workflow.Should().Contain("Test (Main app plugin UI smoke contract");
        workflow.Should().Contain("--filter \"Category=Smoke\"");
        workflow.Should().Contain("UniversalDeviceToolkit.Tests.Smoke.trx");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldGateWpfL10nCoverageScript()
    {
        var workflow = ReadRepositoryFile(".github", "workflows", "Ci-tests.yml");
        workflow.Should().Contain("Assert-WpfL10nCoverage.ps1");
        workflow.Should().Contain("Assert WPF l10n coverage");
    }

    [Fact]
    public void FlaUiNightlyWorkflow_ShouldRunOnSelfHostedDesktopRunner()
    {
        var workflow = ReadRepositoryFile(".github", "workflows", "flaui-tests.yml");

        workflow.Should().Contain("schedule:");
        workflow.Should().Contain("cron: '0 3 * * *'");
        workflow.Should().Contain("workflow_dispatch:");
        workflow.Should().Contain("runs-on: [self-hosted, Windows, LLT-UI-SMOKE]");
        workflow.Should().Contain("UDT_ALLOW_FLAUI_TESTS: true");
        workflow.Should().Contain("--filter \"FullyQualifiedName~FlaUI\"");
        workflow.Should().NotContain("if: false");
    }

    [Fact]
    public void FlaUiTestBase_ShouldResolveX64BuildOutputPaths()
    {
        var source = ReadRepositoryFile(
            "UniversalDeviceToolkit.Tests",
            "FlaUI",
            "FlaUiTestBase.cs");

        source.Should().Contain("UniversalDeviceToolkit.WPF\", \"bin\", \"x64\", \"Release\", \"net10.0-windows10.0.26100.0\", \"win-x64\"");
        source.Should().Contain("RUNNER_ENVIRONMENT");
        source.Should().Contain("UDT_ALLOW_FLAUI_TESTS");
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var repositoryRoot = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine([repositoryRoot, .. pathParts]));
    }

    private static string FindRepositoryRoot()
    {
        var overrideRoot = Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(overrideRoot) &&
            File.Exists(Path.Combine(overrideRoot, "UniversalDeviceToolkit.sln")))
        {
            return Path.GetFullPath(overrideRoot);
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}