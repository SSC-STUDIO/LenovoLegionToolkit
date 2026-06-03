using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

public sealed class PresetUiValidationGuardTests
{
    [Fact]
    public void PresetUiValidation_ShouldRequireCreateAndRenamePersistence()
    {
        var source = ReadRepositoryFile("Tools", "PresetUiValidation", "Program.cs");

        source.Should().Contain("CreatePersistedVerificationPassed");
        source.Should().Contain("RenamePersistedVerificationPassed");
        source.Should().Contain("persistedAfterCreate.Presets.Count == originalCount + 1");
        source.Should().Contain("persistedAfterRename.Presets.Count == originalCount + 1");
        source.Should().Contain("persistedCreateVerificationPassed");
        source.Should().Contain("persistedRenameVerificationPassed");
    }

    [Fact]
    public void PresetUiValidationWrappers_ShouldRequirePersistenceInOverallResult()
    {
        var presetWrapper = ReadRepositoryFile("Tools", "PresetUiValidation", "Run-PresetUiValidationElevated.ps1");
        var smokeWrapper = ReadRepositoryFile("Tools", "MainAppPluginUi.Smoke", "AdminPresetCrudSmoke.ps1");

        presetWrapper.Should().Contain("'CreatePersistedVerificationPassed'");
        presetWrapper.Should().Contain("'RenamePersistedVerificationPassed'");
        presetWrapper.Should().Contain("(Get-ResultValue -FilePath $validatorResultPath -Key 'CreatePersistedVerificationPassed') -eq 'True'");
        presetWrapper.Should().Contain("(Get-ResultValue -FilePath $validatorResultPath -Key 'RenamePersistedVerificationPassed') -eq 'True'");

        smokeWrapper.Should().Contain("'CreatePersistedVerificationPassed'");
        smokeWrapper.Should().Contain("'RenamePersistedVerificationPassed'");
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
