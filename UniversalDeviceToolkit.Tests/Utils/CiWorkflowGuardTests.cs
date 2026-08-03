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

        workflow.Should().Contain("Test (Security + Guard - fail fast)");
        workflow.Should().Contain("Category=Security|Category=Guard");
        workflow.Should().Contain("UniversalDeviceToolkit.Tests.SecurityGuard.trx");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldRunFastTests()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");

        workflow.Should().Contain("Test (Fast unit tests)");
        workflow.Should().Contain("UniversalDeviceToolkit.Fast.Tests/UniversalDeviceToolkit.Fast.Tests.csproj");
        workflow.Should().Contain("UniversalDeviceToolkit.Fast.Tests.trx");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldRunStatefulSuiteOnce()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");

        workflow.Should().Contain("Test (Windows stateful suite)");
        workflow.Should().Contain("Category!=Security&Category!=Guard");
        workflow.Should().Contain("UniversalDeviceToolkit.Tests.Stateful.trx");
    }

    [Fact]
    public void TestRunner_ShouldParallelizeTestsOutsideExplicitStateCollections()
    {
        var runner = RepositoryPaths.ReadFile("UniversalDeviceToolkit.Tests", "xunit.runner.json");
        var collections = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Tests", "Settings", "SettingsTestCollection.cs");

        runner.Should().Contain("\"parallelizeAssembly\": true");
        runner.Should().Contain("\"parallelizeTestCollections\": true");
        runner.Should().Contain("\"maxParallelThreads\": 0");
        collections.Should().Contain("DisableParallelization = true");
        collections.Should().Contain("Localization");
        collections.Should().Contain("Settings");
        collections.Should().Contain("FlaUI");
        collections.Should().Contain("ProcessState");
    }

    [Fact]
    public void FailFastScript_ShouldRunSecurityAndFastLayers()
    {
        var script = RepositoryPaths.ReadFile("Scripts", "Run-TestFailFast.ps1");

        script.Should().Contain("Category=Security|Category=Guard");
        script.Should().Contain("UniversalDeviceToolkit.Fast.Tests/UniversalDeviceToolkit.Fast.Tests.csproj");
        script.Should().NotContain("Category=Plugin");
        script.Should().NotContain("Category=Unit&Category!=Coverage");
        script.Should().NotContain("Category=Smoke");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldNotRepeatPluginUnitAndSmokeLayers()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");

        workflow.Should().NotContain("Test (Plugin category");
        workflow.Should().NotContain("Test (Unit category");
        workflow.Should().NotContain("Test (Main app plugin UI smoke contract");
        workflow.Should().NotContain("UniversalDeviceToolkit.Tests.Smoke.trx");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldGateWpfL10nCoverageScript()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");
        workflow.Should().Contain("Assert-WpfL10nCoverage.ps1");
        workflow.Should().Contain("Assert WPF l10n coverage");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldRunWindowsTestPreflight()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");
        var script = RepositoryPaths.ReadFile("Scripts", "Test-WindowsTestEnvironment.ps1");

        workflow.Should().Contain("Verify Windows test prerequisites");
        workflow.Should().Contain("Test-WindowsTestEnvironment.ps1");
        script.Should().Contain("UniversalDeviceToolkit.sln");
        script.Should().Contain("--list-sdks");
        script.Should().Contain("HKCU:\\Software\\UniversalDeviceToolkit");
        script.Should().Contain("cleanupFailures");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldPublishTestResultsAndDiagnosticsAsArtifacts()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");

        workflow.Should().Contain("Upload Windows test results and diagnostics");
        workflow.Should().Contain("windows-test-results-${{ github.run_id }}");
        workflow.Should().Contain("**/UniversalDeviceToolkit.Tests.SecurityGuard.trx");
        workflow.Should().Contain("**/UniversalDeviceToolkit.Fast.Tests.trx");
        workflow.Should().Contain("**/UniversalDeviceToolkit.Tests.Stateful.trx");
        workflow.Should().Contain("**/TestResults/**/*.dmp");
        workflow.Should().Contain("**/TestResults/**/*.log");
        workflow.Should().Contain("**/*coverage.opencover.xml");
    }

    [Fact]
    public void CrossPlatformWorkflows_ShouldPublishTestResultsAndOpenCoverCoverage()
    {
        var workflows = new[]
        {
            RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml"),
            RepositoryPaths.ReadFile(".github", "workflows", "CrossPlatformCli.yml"),
            RepositoryPaths.ReadFile(".github", "workflows", "linux.yml")
        };

        foreach (var workflow in workflows)
        {
            workflow.Should().Contain("coverage.opencover.xml");
            workflow.Should().Contain(".trx");
        }
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
    public void CrossPlatformWorkflows_ShouldCollectCoverageAndPublishArtifacts()
    {
        var ciWorkflow = RepositoryPaths.ReadFile(".github", "workflows", "Ci-tests.yml");
        var matrixWorkflow = RepositoryPaths.ReadFile(".github", "workflows", "CrossPlatformCli.yml");
        var linuxWorkflow = RepositoryPaths.ReadFile(".github", "workflows", "linux.yml");

        foreach (var workflow in new[] { ciWorkflow, matrixWorkflow, linuxWorkflow })
        {
            workflow.Should().Contain("--collect:\"XPlat Code Coverage\"");
            workflow.Should().Contain("UniversalDeviceToolkit.Tests/coverlet.runsettings");
            workflow.Should().Contain("coverage.cobertura.xml");
        }
    }

    [Fact]
    public void PerformanceWorkflow_ShouldRunBenchmarkAndUploadReport()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "performance.yml");

        workflow.Should().Contain("schedule:");
        workflow.Should().Contain("workflow_dispatch:");
        workflow.Should().Contain("UniversalDeviceToolkit.PerformanceTest/UniversalDeviceToolkit.PerformanceTest.csproj");
        workflow.Should().Contain("--locked-mode");
        workflow.Should().Contain("--output");
        workflow.Should().Contain("Upload benchmark report");
        workflow.Should().Contain("PerformanceBenchmark.txt");
    }

    [Fact]
    public void WslVerificationScript_ShouldRunLockedLinuxBuildAndSmokeChecks()
    {
        var script = RepositoryPaths.ReadFile("Scripts", "Test-CrossPlatformInWsl.ps1");

        script.Should().Contain("wsl --install -d Ubuntu");
        script.Should().Contain("--locked-mode");
        script.Should().Contain("UniversalDeviceToolkit.Platform.Linux");
        script.Should().Contain("UniversalDeviceToolkit.CrossPlatform.Tests");
        script.Should().Contain("cross-platform diagnostics");
        script.Should().Contain("Hardware identity");
        script.Should().Contain("Universal Device Toolkit");
        script.Should().Contain("No WSL distribution is installed");
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
    public void FlaUiNightlyWorkflow_ShouldFailOnMissingDesktopPreconditions()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "flaui-tests.yml");

        workflow.Should().Contain("Verify interactive desktop preconditions");
        workflow.Should().Contain("SESSIONNAME -eq 'Services'");
        workflow.Should().Contain("[Environment]::UserInteractive");
        workflow.Should().Contain("WindowsBuiltInRole]::Administrator");
        workflow.Should().Contain("Built UDT executable not found");
    }

    [Fact]
    public void FlaUiTestBase_ShouldResolveX64BuildOutputPaths()
    {
        var source = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.UiAutomation.Tests",
            "FlaUI",
            "FlaUiTestBase.cs");

        source.Should().Contain("UniversalDeviceToolkit.WPF\", \"bin\", \"x64\", \"Release\", \"net10.0-windows10.0.26100.0\", \"win-x64\"");
        source.Should().Contain("RUNNER_ENVIRONMENT");
        source.Should().Contain("UDT_ALLOW_FLAUI_TESTS");
    }
}
