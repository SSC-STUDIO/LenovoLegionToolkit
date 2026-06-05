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
