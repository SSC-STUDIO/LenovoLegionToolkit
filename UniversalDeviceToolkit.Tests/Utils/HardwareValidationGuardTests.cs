using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

public sealed class HardwareValidationGuardTests
{
    [Fact]
    public void HardwareValidation_ShouldRequireGodModeReadbackForMeasuredPresetVerification()
    {
        var source = ReadRepositoryFile("Tools", "HardwareValidation", "Program.cs");

        source.Should().Contain("PowerModeVerificationPassed: {powerModeVerificationPassed}");
        source.Should().Contain("measuredVerificationPassed &&");
        source.Should().Contain("powerModeVerificationPassed;");
        source.Should().Contain("verificationPassed = passedCount == plans.Count && powerModeObservedGodMode;");
        source.Should().Contain("BatchPowerModeObservedGodMode: {powerModeObservedGodMode}");
    }

    [Fact]
    public void HardwareValidationWrapper_ShouldIncludePowerModeReadbackInOverallResult()
    {
        var script = ReadRepositoryFile("Tools", "HardwareValidation", "Run-HardwareValidationElevated.ps1");

        script.Should().Contain("'PowerModeVerificationPassed'");
        script.Should().Contain("$powerModePassed -eq 'True'");
        script.Should().Contain("'BatchPowerModeObservedGodMode'");
        script.Should().Contain("$batchPowerModePassed -eq 'True'");
    }

    [Fact]
    public void HardwareValidation_ShouldExposePowerModeSetVerifyWithReadbackAndRestore()
    {
        var source = ReadRepositoryFile("Tools", "HardwareValidation", "Program.cs");

        source.Should().Contain("\"set-verify\"");
        source.Should().Contain("BeforeSmartFanMode: {beforeMode}");
        source.Should().Contain("RequestedSmartFanMode: {requestedMode}");
        source.Should().Contain("PowerModeVerificationPassed: {verificationPassed}");
        source.Should().Contain("RestoreVerificationPassed: {restorePassed}");
        source.Should().Contain("OverallPassed: {overallPassed}");
    }

    [Fact]
    public void HardwareValidationWrapper_ShouldExposePowerModeVerifyScenario()
    {
        var script = ReadRepositoryFile("Tools", "HardwareValidation", "Run-HardwareValidationElevated.ps1");

        script.Should().Contain("'PowerModeVerify'");
        script.Should().Contain("CommandArguments = @('set-verify', '2')");
        script.Should().Contain("'MeasuredPowerModeChangeObserved'");
        script.Should().Contain("$powerModeOverallPassed -eq 'True'");
    }

    [Fact]
    public void MainAppPowerModeHardwareCheck_ShouldRunUiSmokeAndHardwareReadback()
    {
        var script = ReadRepositoryFile("Tools", "MainAppPluginUi.Smoke", "AdminPowerModeHardwareCheck.ps1");

        script.Should().Contain("'PowerModeUiAndHardwareVerify'");
        script.Should().Contain("'--scenario'");
        script.Should().Contain("'power-mode'");
        script.Should().Contain("'-Scenario', 'PowerModeVerify'");
        script.Should().Contain("'PowerModeVerificationPassed'");
        script.Should().Contain("'OverallPassed'");
    }

    [Fact]
    public void ElevatedValidationScripts_ShouldResolveRepositoryRootDynamically()
    {
        foreach (var pathParts in ElevatedValidationScriptPaths)
        {
            var script = ReadRepositoryFile(pathParts);

            script.Should().NotContain(@"D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit");
            script.Should().Contain("[string]$RepoRoot");
            script.Should().Contain("function Resolve-RepositoryRoot");
            script.Should().Contain("Write-Result 'RepositoryRoot' $repoRoot");
        }
    }

    [Fact]
    public void ValidationDelegates_ShouldPassResolvedRepositoryRootToChildProcesses()
    {
        var powerModeSmoke = ReadRepositoryFile("Tools", "MainAppPluginUi.Smoke", "AdminPowerModeHardwareCheck.ps1");
        powerModeSmoke.Should().Contain("$smokeProcessStartInfo.ArgumentList.Add('--repo-root')");
        powerModeSmoke.Should().Contain("$smokeProcessStartInfo.ArgumentList.Add($repoRoot)");
        powerModeSmoke.Should().Contain("'-RepoRoot', $repoRoot");
        powerModeSmoke.Should().Contain("-WorkingDirectory $repoRoot");

        var directHardwareSmoke = ReadRepositoryFile("Tools", "MainAppPluginUi.Smoke", "AdminDirectHardwareSmoke.ps1");
        directHardwareSmoke.Should().Contain("'-RepoRoot', $repoRoot");
        directHardwareSmoke.Should().Contain("-WorkingDirectory $repoRoot");

        var presetCrudSmoke = ReadRepositoryFile("Tools", "MainAppPluginUi.Smoke", "AdminPresetCrudSmoke.ps1");
        presetCrudSmoke.Should().Contain("'-RepoRoot', $repoRoot");
        presetCrudSmoke.Should().Contain("-WorkingDirectory $repoRoot");

        var hardwareValidation = ReadRepositoryFile("Tools", "HardwareValidation", "Run-HardwareValidationElevated.ps1");
        hardwareValidation.Should().Contain("'-RepoRoot', $repoRoot");
        hardwareValidation.Should().Contain("-WorkingDirectory $repoRoot");

        var presetValidation = ReadRepositoryFile("Tools", "PresetUiValidation", "Run-PresetUiValidationElevated.ps1");
        presetValidation.Should().Contain("'-RepoRoot', $repoRoot");
        presetValidation.Should().Contain("-WorkingDirectory $repoRoot");
    }

    private static string[][] ElevatedValidationScriptPaths =>
    [
        ["Tools", "MainAppPluginUi.Smoke", "AdminPowerModeHardwareCheck.ps1"],
        ["Tools", "MainAppPluginUi.Smoke", "AdminDirectHardwareSmoke.ps1"],
        ["Tools", "MainAppPluginUi.Smoke", "AdminPresetCrudSmoke.ps1"],
        ["Tools", "HardwareValidation", "Run-HardwareValidationElevated.ps1"],
        ["Tools", "PresetUiValidation", "Run-PresetUiValidationElevated.ps1"]
    ];

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var repositoryRoot = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine([repositoryRoot, .. pathParts]));
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
            var directory = new DirectoryInfo(Path.GetFullPath(candidate!));
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                    return directory.FullName;

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
