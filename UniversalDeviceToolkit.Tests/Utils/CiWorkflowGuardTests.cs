using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class CiWorkflowGuardTests
{
    [Fact]
    public void CiTestsWorkflow_ShouldGateSecurityAndGuardFailFast()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");

        workflow.Should().Contain("Test (Security + Guard — fail fast)");
        workflow.Should().Contain("Category=Security|Category=Guard");
        workflow.Should().Contain("UniversalDeviceToolkit.Tests.SecurityGuard.trx");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldGatePluginFailFast()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");

        workflow.Should().Contain("Test (Plugin category — fail fast)");
        workflow.Should().Contain("Category=Plugin");
        workflow.Should().Contain("UniversalDeviceToolkit.Tests.Plugin.trx");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldGateUnitFailFastExcludingCoveragePadding()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");

        workflow.Should().Contain("Test (Unit category — fail fast)");
        workflow.Should().Contain("Category=Unit&Category!=Coverage");
        workflow.Should().Contain("UniversalDeviceToolkit.Tests.Unit.trx");
    }

    [Fact]
    public void TestRunner_ShouldStaySerialUntilSharedStateIsFullyIsolated()
    {
        // Plugin/path tests share Folders.AppDataOverride and temp plugin roots.
        // Keep the suite serial until those fixtures are process-isolated.
        var runner = RepositoryPaths.ReadFile("UniversalDeviceToolkit.Tests", "xunit.runner.json");
        var collections = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Tests", "Infrastructure", "TestCollections.cs");

        runner.Should().Contain("\"parallelizeTestCollections\": false");
        runner.Should().Contain("\"maxParallelThreads\": 1");
        collections.Should().Contain("DisableParallelization = true");
        collections.Should().Contain("Localization");
        collections.Should().Contain("Settings");
        collections.Should().Contain("FlaUI");
    }

    [Fact]
    public void FailFastScript_ShouldMirrorCiLayers()
    {
        var script = RepositoryPaths.ReadFile("Scripts", "Run-TestFailFast.ps1");
        script.Should().Contain("Category=Security|Category=Guard");
        script.Should().Contain("Category=Plugin");
        script.Should().Contain("Category=Unit&Category!=Coverage");
        script.Should().Contain("Category=Smoke");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldGateMainAppPluginUiSmokeContract()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");

        workflow.Should().Contain("Test (Main app plugin UI smoke contract");
        workflow.Should().Contain("--filter \"Category=Smoke\"");
        workflow.Should().Contain("UniversalDeviceToolkit.Tests.Smoke.trx");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldGateWpfL10nCoverageScript()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");
        workflow.Should().Contain("Assert-WpfL10nCoverage.ps1");
        workflow.Should().Contain("Assert WPF l10n coverage");
    }

    [Fact]
    public void CrossPlatformCliWorkflow_ShouldRunOnPullRequests()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "CrossPlatformCli.yml");

        workflow.Should().Contain("pull_request:");
        workflow.Should().Contain("UniversalDeviceToolkit.CrossPlatform/**");
        workflow.Should().Contain("UniversalDeviceToolkit.CrossPlatform.Tests/**");
        workflow.Should().Contain("workflow_dispatch:");
    }

    [Fact]
    public void LinuxCiWorkflow_ShouldRunCrossPlatformTestsOnPullRequests()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "linux.yml");

        workflow.Should().Contain("pull_request:");
        workflow.Should().Contain("UniversalDeviceToolkit.CrossPlatform.Tests");
        workflow.Should().Contain("dotnet test");
        workflow.Should().Contain("Smoke diagnostics CLI");
        workflow.Should().Contain("-- status");
        workflow.Should().Contain("-- doctor");
        workflow.Should().Contain("-- json");
    }

    [Fact]
    public void FlaUiNightlyWorkflow_ShouldRunOnSelfHostedDesktopRunner()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "flaui-tests.yml");

        workflow.Should().Contain("schedule:");
        workflow.Should().Contain("cron: '0 3 * * *'");
        workflow.Should().Contain("workflow_dispatch:");
        workflow.Should().Contain("runs-on: [self-hosted, Windows, udt]");
        workflow.Should().Contain("UDT_ALLOW_FLAUI_TESTS: true");
        workflow.Should().Contain("--filter \"FullyQualifiedName~FlaUI\"");
        workflow.Should().NotContain("if: false");
    }

    [Fact]
    public void FlaUiTestBase_ShouldResolveX64BuildOutputPaths()
    {
        var source = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Tests",
            "FlaUI",
            "FlaUiTestBase.cs");

        source.Should().Contain("UniversalDeviceToolkit.WPF\", \"bin\", \"x64\", \"Release\", \"net10.0-windows10.0.26100.0\", \"win-x64\"");
        source.Should().Contain("RUNNER_ENVIRONMENT");
        source.Should().Contain("UDT_ALLOW_FLAUI_TESTS");
    }
}
