using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class PresetUiValidationGuardTests
{
    [Fact]
    public void PresetUiValidation_ShouldRequireCreateAndRenamePersistence()
    {
        var source = ReadRepositoryFile("Tools", "PresetUiValidation", "Program.cs");

        source.Should().Contain("CreatePersistedVerificationPassed");
        source.Should().Contain("RenamePersistedVerificationPassed");
        source.Should().Contain("CreateUiRefreshVerificationPassed");
        source.Should().Contain("RenameUiRefreshVerificationPassed");
        source.Should().Contain("DeleteUiRefreshVerificationPassed");
        source.Should().Contain("persistedAfterCreate.Presets.Count == originalCount + 1");
        source.Should().Contain("persistedAfterRename.Presets.Count == originalCount + 1");
        source.Should().Contain("persistedCreateVerificationPassed");
        source.Should().Contain("persistedRenameVerificationPassed");
        source.Should().Contain("createUiRefreshPassed");
        source.Should().Contain("renameUiRefreshPassed");
        source.Should().Contain("deleteUiRefreshPassed");
    }

    [Fact]
    public void PresetUiValidationWrappers_ShouldRequirePersistenceInOverallResult()
    {
        var presetWrapper = ReadRepositoryFile("Tools", "PresetUiValidation", "Run-PresetUiValidationElevated.ps1");
        var smokeWrapper = ReadRepositoryFile("Tools", "MainAppPluginUi.Smoke", "AdminPresetCrudSmoke.ps1");

        presetWrapper.Should().Contain("'CreatePersistedVerificationPassed'");
        presetWrapper.Should().Contain("'RenamePersistedVerificationPassed'");
        presetWrapper.Should().Contain("'CreateUiRefreshVerificationPassed'");
        presetWrapper.Should().Contain("'RenameUiRefreshVerificationPassed'");
        presetWrapper.Should().Contain("'DeleteUiRefreshVerificationPassed'");
        presetWrapper.Should().Contain("(Get-ResultValue -FilePath $validatorResultPath -Key 'CreateUiRefreshVerificationPassed') -eq 'True'");
        presetWrapper.Should().Contain("(Get-ResultValue -FilePath $validatorResultPath -Key 'RenameUiRefreshVerificationPassed') -eq 'True'");
        presetWrapper.Should().Contain("(Get-ResultValue -FilePath $validatorResultPath -Key 'DeleteUiRefreshVerificationPassed') -eq 'True'");
        presetWrapper.Should().Contain("(Get-ResultValue -FilePath $validatorResultPath -Key 'CreatePersistedVerificationPassed') -eq 'True'");
        presetWrapper.Should().Contain("(Get-ResultValue -FilePath $validatorResultPath -Key 'RenamePersistedVerificationPassed') -eq 'True'");

        smokeWrapper.Should().Contain("'CreatePersistedVerificationPassed'");
        smokeWrapper.Should().Contain("'RenamePersistedVerificationPassed'");
        smokeWrapper.Should().Contain("'CreateUiRefreshVerificationPassed'");
        smokeWrapper.Should().Contain("'RenameUiRefreshVerificationPassed'");
        smokeWrapper.Should().Contain("'DeleteUiRefreshVerificationPassed'");
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
        return File.ReadAllText(Path.Combine([repositoryRoot, .. pathParts]));
    }

}
