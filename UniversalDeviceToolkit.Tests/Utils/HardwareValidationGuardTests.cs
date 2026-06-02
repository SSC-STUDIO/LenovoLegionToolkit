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

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var repositoryRoot = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine([repositoryRoot, .. pathParts]));
    }

    private static string FindRepositoryRoot()
    {
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
