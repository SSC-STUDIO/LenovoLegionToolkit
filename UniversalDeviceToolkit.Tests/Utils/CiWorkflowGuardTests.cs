using System;
using System.Collections.Generic;
using System.Linq;
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
        var workflow = ReadWorkflow("Ci-tests.yml");
        var step = workflow.Job("build-test-and-smoke").Step("Test (Security + Guard - fail fast)");

        step.OptionValue("--filter").Should().Be("Category=Security|Category=Guard");
        step.OptionValue("--logger").Should().Be("trx;LogFileName=UniversalDeviceToolkit.Tests.SecurityGuard.trx");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldRunFastTests()
    {
        var step = ReadWorkflow("Ci-tests.yml").Job("build-test-and-smoke").Step("Test (Fast unit tests)");

        step.Run.Should().Contain("UniversalDeviceToolkit.Fast.Tests/UniversalDeviceToolkit.Fast.Tests.csproj");
        step.OptionValue("--logger").Should().Be("trx;LogFileName=UniversalDeviceToolkit.Fast.Tests.trx");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldRunStatefulSuiteOnce()
    {
        var step = ReadWorkflow("Ci-tests.yml").Job("build-test-and-smoke").Step("Test (Windows stateful suite)");

        step.OptionValue("--filter").Should().Be("Category!=Security&Category!=Guard");
        step.OptionValue("--logger").Should().Be("trx;LogFileName=UniversalDeviceToolkit.Tests.Stateful.trx");
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
        var stepNames = ReadWorkflow("Ci-tests.yml")
            .Job("build-test-and-smoke")
            .Steps
            .Select(step => step.Name)
            .Where(name => name is not null)
            .Cast<string>();

        stepNames.Should().NotContain(name => name.StartsWith("Test (Plugin category", StringComparison.Ordinal));
        stepNames.Should().NotContain(name => name.StartsWith("Test (Unit category", StringComparison.Ordinal));
        stepNames.Should().NotContain(name => name.StartsWith("Test (Main app plugin UI smoke contract", StringComparison.Ordinal));
        AllStepText(ReadWorkflow("Ci-tests.yml")).Should().NotContain("UniversalDeviceToolkit.Tests.Smoke.trx");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldNotRunWpfL10nCoverageScript()
    {
        var job = ReadWorkflow("Ci-tests.yml").Job("build-test-and-smoke");

        job.Steps.Should().NotContain(s => s.Name.Contains("WPF l10n", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CiTestsWorkflow_ShouldRunWindowsTestPreflight()
    {
        var workflow = ReadWorkflow("Ci-tests.yml");
        var script = RepositoryPaths.ReadFile("Scripts", "Test-WindowsTestEnvironment.ps1");
        var step = workflow.Job("build-test-and-smoke").Step("Verify Windows test prerequisites");

        step.Run.Should().Contain("Test-WindowsTestEnvironment.ps1");
        script.Should().Contain("UniversalDeviceToolkit.sln");
        script.Should().Contain("--list-sdks");
        script.Should().Contain("HKCU:\\Software\\UniversalDeviceToolkit");
        script.Should().Contain("cleanupFailures");
    }

    [Fact]
    public void CiTestsWorkflow_ShouldPublishTestResultsAndDiagnosticsAsArtifacts()
    {
        var step = ReadWorkflow("Ci-tests.yml").Job("build-test-and-smoke")
            .Step("Upload Windows test results and diagnostics");
        var paths = step.WithValue("path");

        step.Uses.Should().Be("actions/upload-artifact@v7");
        step.Condition.Should().Be("always()");
        step.WithValue("name").Should().Be("windows-test-results-${{ github.run_id }}");
        paths.Should().Contain("**/UniversalDeviceToolkit.Tests.SecurityGuard.trx");
        paths.Should().Contain("**/UniversalDeviceToolkit.Fast.Tests.trx");
        paths.Should().Contain("**/UniversalDeviceToolkit.Tests.Stateful.trx");
        paths.Should().Contain("**/TestResults/**/*.dmp");
        paths.Should().Contain("**/TestResults/**/*.log");
    }

    [Fact]
    public void CrossPlatformWorkflows_ShouldPublishTestResultsAndOpenCoverCoverage()
    {
        foreach (var workflowName in new[] { "Ci-tests.yml", "CrossPlatformCli.yml", "linux.yml" })
        {
            var text = AllStepText(ReadWorkflow(workflowName));
            text.Should().Contain(".trx");
            text.Should().Contain("coverage.opencover.xml");
        }
    }

    [Fact]
    public void CrossPlatformCliWorkflow_ShouldRunOnPullRequests()
    {
        var workflow = ReadWorkflow("CrossPlatformCli.yml");
        var trigger = workflow.Triggers["pull_request"];

        trigger.Paths.Should().Contain("UniversalDeviceToolkit.CrossPlatform/**");
        trigger.Paths.Should().Contain("UniversalDeviceToolkit.CrossPlatform.Tests/**");
        workflow.Triggers.Should().ContainKey("workflow_dispatch");
    }

    [Fact]
    public void LinuxCiWorkflow_ShouldRunCrossPlatformTestsOnPullRequests()
    {
        var workflow = ReadWorkflow("linux.yml");
        var text = AllStepText(workflow);

        workflow.Triggers.Should().ContainKey("pull_request");
        text.Should().Contain("UniversalDeviceToolkit.CrossPlatform.Tests");
        text.Should().Contain("dotnet test");
        text.Should().Contain("Smoke diagnostics CLI");
        text.Should().Contain("-- status");
        text.Should().Contain("-- doctor");
        text.Should().Contain("-- json");
    }

    [Fact]
    public void CrossPlatformWorkflows_ShouldCollectCoverageAndPublishArtifacts()
    {
        foreach (var workflowName in new[] { "Ci-tests.yml", "CrossPlatformCli.yml", "linux.yml" })
        {
            var text = AllStepText(ReadWorkflow(workflowName));
            text.Should().Contain("--collect:\"XPlat Code Coverage\"");
            text.Should().Contain("UniversalDeviceToolkit.Tests/coverlet.runsettings");
            text.Should().Contain("coverage.cobertura.xml");
        }
    }

    [Fact]
    public void PerformanceWorkflow_ShouldRunBenchmarkAndUploadReport()
    {
        var workflow = ReadWorkflow("performance.yml");
        var job = workflow.Job("backend-benchmark");
        var text = AllStepText(workflow);

        workflow.Triggers.Should().ContainKey("schedule");
        workflow.Triggers.Should().ContainKey("workflow_dispatch");
        workflow.Triggers["schedule"].Values.Should().Contain("cron: '30 3 * * 1'");
        text.Should().Contain("UniversalDeviceToolkit.PerformanceTest/UniversalDeviceToolkit.PerformanceTest.csproj");
        text.Should().Contain("--locked-mode");
        text.Should().Contain("--output");
        job.Step("Upload benchmark report").Uses.Should().Be("actions/upload-artifact@v7");
        job.Step("Upload benchmark report").WithValue("path").Should().Contain("PerformanceBenchmark.txt");
    }

    [Fact]
    public void WslVerificationScript_ShouldRunLockedLinuxBuildAndSmokeChecks()
    {
        var script = RepositoryPaths.ReadFile("Scripts", "Test-CrossPlatformInWsl.ps1");

        script.Should().Contain("never installs a distribution automatically");
        script.Should().NotContain("$wslCommand.Source --install");
        script.Should().Contain("--locked-mode");
        script.Should().Contain("UniversalDeviceToolkit.Platform.Linux");
        script.Should().Contain("UniversalDeviceToolkit.CrossPlatform.Tests");
        script.Should().Contain("cross-platform diagnostics");
        script.Should().Contain("Hardware identity");
        script.Should().Contain("Universal Device Toolkit");
        script.Should().Contain("No WSL distribution is installed");
    }

    [Fact]
    public void PluginValidationWorkflow_ShouldPrimeToolingWithHelpCommand()
    {
        var step = ReadWorkflow("plugins-validate.yml")
            .Job("validate")
            .Step("Prime plugin tooling CLI");

        step.Run.Should().Contain("Invoke-PluginTooling.ps1 --help --repository-root .\\Plugins");
    }

    [Fact]
    public void PluginCatalogReleaseWorkflow_ShouldPublishCatalogAfterPackageValidation()
    {
        var workflow = RepositoryPaths.ReadFile(".github", "workflows", "plugins-release.yml");

        workflow.Should().Contain("group: plugin-catalog-release");
        workflow.Should().Contain("cancel-in-progress: false");
        workflow.Should().Contain("CATALOG_RELEASE_TITLE: 'Official Plugin Catalog (managed)'");
        workflow.Should().Contain("Validate staged release contents");
        workflow.Should().Contain("$previousCatalog");
        workflow.Should().Contain("throw $catalogError");

        var packageUploadIndex = workflow.IndexOf("- name: Upload new package assets", StringComparison.Ordinal);
        var catalogUploadIndex = workflow.IndexOf("- name: Publish catalog asset last and prune stale packages", StringComparison.Ordinal);
        var staleAssetDeleteIndex = workflow.IndexOf("gh release delete-asset", StringComparison.Ordinal);

        packageUploadIndex.Should().BeGreaterThanOrEqualTo(0);
        catalogUploadIndex.Should().BeGreaterThan(packageUploadIndex);
        staleAssetDeleteIndex.Should().BeGreaterThan(catalogUploadIndex);
    }

    private static GitHubWorkflowContract ReadWorkflow(string fileName) =>
        GitHubWorkflowContract.Parse(RepositoryPaths.ReadFile(".github", "workflows", fileName));

    private static string AllStepText(GitHubWorkflowContract workflow)
    {
        var values = new List<string>();
        foreach (var step in workflow.Steps)
        {
            if (step.Name is not null)
                values.Add(step.Name);
            if (step.Uses is not null)
                values.Add(step.Uses);
            if (step.Run is not null)
                values.Add(step.Run);
            values.AddRange(step.With.Values);
        }

        return string.Join(Environment.NewLine, values);
    }
}
