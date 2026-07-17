using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
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
        script.Should().Contain("dotnet build $hardwareValidationProject -c Release /m:1");
        script.Should().Contain("HardwareValidation.dll was not found after build.");
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
        var source = ReadRepositoryFile("Tools", "MainAppPluginUi.Smoke", "Program.cs");

        script.Should().Contain("'PowerModeUiAndHardwareVerify'");
        script.Should().Contain("'--scenario'");
        script.Should().Contain("'power-mode'");
        script.Should().Contain("'--power-mode-hardware-verify'");
        script.Should().Contain("'UiSmokeHardwareVerificationRequested' 'True'");
        script.Should().Contain("Get-LogValue -FilePath $uiSmokeLogPath -Key 'UiPowerModeHardwareVerificationPassed'");
        script.Should().Contain("$uiPowerModeChanged -eq 'True'");
        script.Should().Contain("$uiPowerModePassed -eq 'True'");
        script.Should().Contain("$uiPowerModeRestored -eq 'True'");
        script.Should().Contain("$uiHardwareOverallPassed -eq 'True'");
        script.Should().Contain("'-Scenario', 'PowerModeVerify'");
        script.Should().Contain("'PowerModeVerificationPassed'");
        script.Should().Contain("'OverallPassed'");
        script.Should().Contain("[switch]$SkipElevationCheck");
        script.Should().Contain("-Verb RunAs");
        script.Should().Contain("-SkipElevationCheck");
        script.Should().Contain("Write-Result 'IsAdmin'");
        script.Should().Contain("function Stop-DescendantProcesses");
        script.Should().Contain("Stop-DescendantProcesses -ParentProcessId $smokeProcess.Id");

        source.Should().Contain("RunPowerModeUiHardwareReadbackVerification(mainWindow, comboBox)");
        source.Should().Contain("SelectPowerModeComboBoxItem(comboBox, targetMode)");
        source.Should().Contain("TryResolveExpectedSmartFanModeRawValue(targetMode)");
        source.Should().Contain("TryResolvePowerModeStateFromSmartFanMode(beforeMode)");
        source.Should().Contain("afterMode == expectedAfterMode");
        source.Should().Contain("TryResolveLocalizedPowerModeState(text)");
        source.Should().Contain("Resource.ResourceManager.GetString(resourceKey, culture)");
        source.Should().Contain("UiPowerModeHardwareVerificationPassed: {hardwarePassed}");
        source.Should().Contain("UiPowerModeHardwareChanged: {hardwareChanged}");
        source.Should().NotContain("RunPowerModeHardwareVerification()");
    }

    [Fact]
    public void MainAppUiSmokeRunner_ShouldExposePowerModeHardwareVerificationScenario()
    {
        var runner = ReadRepositoryFile("Tools", "MainAppPluginUi.Smoke", "Run-MainAppPluginUi.Smoke.ps1");
        var workflow = ReadRepositoryFile(".github", "workflows", "MainAppPluginUi.Smoke.yml");

        runner.Should().Contain("'power-mode'");
        runner.Should().Contain("[switch]$PowerModeHardwareVerify");
        runner.Should().Contain("$commandArguments += '--power-mode-hardware-verify'");

        workflow.Should().Contain("- power-mode");
        workflow.Should().Contain("power_mode_hardware_verify");
        workflow.Should().Contain("[string[]]@('custom', 'shell-local', 'combo-local')");
        workflow.Should().Contain("Scenario '$env:SCENARIO' does not install plugins; skipping local package validation.");
        workflow.Should().Contain("POWER_MODE_HARDWARE_VERIFY: ${{ inputs.power_mode_hardware_verify }}");
        workflow.Should().Contain("PowerModeHardwareVerify = [System.Convert]::ToBoolean($env:POWER_MODE_HARDWARE_VERIFY)");
    }

    [Fact]
    public void PerformanceEffectVerificationRunner_ShouldComposeUiAndHardwareReadbackChecks()
    {
        var script = ReadRepositoryFile("Tools", "HardwareValidation", "Run-PerformanceEffectVerification.ps1");
        var readme = ReadRepositoryFile("Tools", "HardwareValidation", "README.md");

        script.Should().Contain("Scenario' 'PerformanceEffectVerification'");
        script.Should().Contain("-Verb RunAs");
        script.Should().Contain("-SkipElevationCheck");
        script.Should().Contain("dotnet build $mainAppProject -c Release /p:EnableUdtTestHooks=true /m:1");
        script.Should().Contain("dotnet build $smokeProject -c Release /m:1");
        script.Should().Contain("'UiPowerModeHardware'");
        script.Should().Contain("AdminPowerModeHardwareCheck.ps1");
        script.Should().Contain("'-Scenario', 'BatchDefault'");
        script.Should().Contain("'-Scenario', 'PowerModeVerify'");
        script.Should().Contain("'BatchMeasuredDeltas'");
        script.Should().Contain("'MeasuredPowerModeChangeObserved'");
        script.Should().Contain("Write-Result 'OverallPassed' ([string]$overallPassed)");

        readme.Should().Contain("Run-PerformanceEffectVerification.ps1");
        readme.Should().Contain("UI power mode verification");
        readme.Should().Contain("God Mode batch verification");
        readme.Should().Contain("Direct power mode verification");
        readme.Should().Contain("OverallPassed: True");
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
        powerModeSmoke.Should().Contain("$smokeProcessArguments = @(");
        powerModeSmoke.Should().Contain("'--repo-root',");
        powerModeSmoke.Should().Contain("$repoRoot,");
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
        ["Tools", "HardwareValidation", "Run-PerformanceEffectVerification.ps1"],
        ["Tools", "PresetUiValidation", "Run-PresetUiValidationElevated.ps1"]
    ];

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
        return File.ReadAllText(Path.Combine([repositoryRoot, .. pathParts]));
    }

}
