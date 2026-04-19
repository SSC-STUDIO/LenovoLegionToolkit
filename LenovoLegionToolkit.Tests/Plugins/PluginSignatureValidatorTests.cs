using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Xunit;

namespace LenovoLegionToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginSignatureValidatorTests : TemporaryFileTestBase
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultSettings_ShouldInitialize()
    {
        // Act
        var validator = new PluginSignatureValidator();

        // Assert
        validator.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomSettings_ShouldInitialize()
    {
        // Arrange
        var settings = PluginSignatureSettings.Development;

        // Act
        var validator = new PluginSignatureValidator(settings);

        // Assert
        validator.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullSettings_ShouldUseDefaultSettings()
    {
        // Act
        var validator = new PluginSignatureValidator(null!);

        // Assert
        validator.Should().NotBeNull();
    }

    #endregion

    #region ValidateAsync Tests

    [Fact]
    public async Task ValidateAsync_WhenValidationDisabled_ShouldReturnValid()
    {
        // Arrange
        var settings = PluginSignatureSettings.Disabled;
        var validator = new PluginSignatureValidator(settings);
        var tempFile = CreateTempFile("test dll content");

        // Act
        var result = await validator.ValidateAsync(tempFile);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(PluginSignatureStatus.Valid);
    }

    [Fact]
    public async Task ValidateAsync_WhenFileDoesNotExist_ShouldReturnValidationError()
    {
        // Arrange
        var settings = PluginSignatureSettings.Production;
        var validator = new PluginSignatureValidator(settings);
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.dll");

        // Act
        var result = await validator.ValidateAsync(nonExistentFile);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(PluginSignatureStatus.ValidationError);
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateAsync_WithUnsignedFileAndAllowUnsigned_ShouldReturnNotSignedButValid()
    {
        // Arrange
        var settings = PluginSignatureSettings.Development;
        var validator = new PluginSignatureValidator(settings);
        var tempFile = CreateTempFile("test dll content");

        // Act
        var result = await validator.ValidateAsync(tempFile);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PluginSignatureStatus.NotSigned);
        result.ErrorMessage.Should().Contain("not signed");
        result.IsAllowedByPolicy.Should().BeTrue();
        result.IsValid.Should().BeTrue(); // Should be valid when AllowUnsigned policy is in effect
    }

    [Fact]
    public async Task ValidateAsync_WithUnsignedFileAndRequireSignature_ShouldReturnNotSigned()
    {
        // Arrange
        var settings = PluginSignatureSettings.Production;
        var validator = new PluginSignatureValidator(settings);
        var tempFile = CreateTempFile("test dll content");

        // Act
        var result = await validator.ValidateAsync(tempFile);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(PluginSignatureStatus.NotSigned);
        result.ErrorMessage.Should().Contain("not signed");
    }

    [Fact]
    public async Task ValidateAsync_WhenValidationDisabled_ShouldNotCheckFileExistence()
    {
        // Arrange
        var settings = PluginSignatureSettings.Disabled;
        var validator = new PluginSignatureValidator(settings);
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = await validator.ValidateAsync(nonExistentFile);

        // Assert
        // When validation is disabled, it should return Valid immediately without checking file existence
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region PluginSignatureResult Tests

    [Fact]
    public void PluginSignatureResult_DefaultConstructor_ShouldInitialize()
    {
        // Act
        var result = new PluginSignatureResult(PluginSignatureStatus.Valid);

        // Assert
        result.Status.Should().Be(PluginSignatureStatus.Valid);
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Certificate.Should().BeNull();
        result.Issuer.Should().BeNull();
        result.ExpirationDate.Should().BeNull();
    }

    [Fact]
    public void PluginSignatureResult_WithErrorMessage_ShouldSetMessage()
    {
        // Arrange
        var errorMessage = "Test error message";

        // Act
        var result = new PluginSignatureResult(PluginSignatureStatus.Invalid, errorMessage);

        // Assert
        result.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void PluginSignatureResult_WithInvalidStatus_ShouldNotBeValid()
    {
        // Act & Assert
        new PluginSignatureResult(PluginSignatureStatus.Invalid).IsValid.Should().BeFalse();
        new PluginSignatureResult(PluginSignatureStatus.NotSigned).IsValid.Should().BeFalse();
        new PluginSignatureResult(PluginSignatureStatus.Expired).IsValid.Should().BeFalse();
        new PluginSignatureResult(PluginSignatureStatus.Untrusted).IsValid.Should().BeFalse();
        new PluginSignatureResult(PluginSignatureStatus.ValidationError).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PluginSignatureResult_WithValidStatus_ShouldBeValid()
    {
        // Act
        var result = new PluginSignatureResult(PluginSignatureStatus.Valid);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region PluginSignatureSettings Tests

    [Fact]
    public void PluginSignatureSettings_Production_ShouldHaveStrictSettings()
    {
        // Act
        var settings = PluginSignatureSettings.Production;

        // Assert
        settings.ValidationMode.Should().Be(PluginSignatureValidationMode.RequireSignature);
        settings.AllowTestCertificates.Should().BeFalse();
        settings.CheckRevocationStatus.Should().BeTrue();
    }

    [Fact]
    public void PluginSignatureSettings_Development_ShouldHaveRelaxedSettings()
    {
        // Act
        var settings = PluginSignatureSettings.Development;

        // Assert
        settings.ValidationMode.Should().Be(PluginSignatureValidationMode.AllowUnsigned);
        settings.AllowTestCertificates.Should().BeTrue();
        settings.CheckRevocationStatus.Should().BeFalse();
    }

    [Fact]
    public void PluginSignatureSettings_Disabled_ShouldHaveNoValidation()
    {
        // Act
        var settings = PluginSignatureSettings.Disabled;

        // Assert
        settings.ValidationMode.Should().Be(PluginSignatureValidationMode.DisableValidation);
        settings.AllowTestCertificates.Should().BeTrue();
        settings.CheckRevocationStatus.Should().BeFalse();
    }

    [Fact]
    public void PluginSignatureSettings_DefaultConstructor_ShouldUseProductionDefaults()
    {
        // Act
        var settings = new PluginSignatureSettings();

        // Assert
        settings.ValidationMode.Should().Be(PluginSignatureValidationMode.RequireSignature);
        settings.AllowTestCertificates.Should().BeFalse();
        settings.CheckRevocationStatus.Should().BeTrue();
        settings.TrustedIssuers.Should().BeEmpty();
        settings.AllowedUnsignedPlugins.Should().BeEmpty();
    }

    [Fact]
    public void PluginSignatureSettings_WithCustomProperties_ShouldSetProperties()
    {
        // Arrange & Act
        var settings = new PluginSignatureSettings
        {
            ValidationMode = PluginSignatureValidationMode.AllowUnsigned,
            AllowTestCertificates = true,
            TrustedIssuers = new[] { "thumbprint1", "thumbprint2" },
            AllowedUnsignedPlugins = new[] { "test-plugin" },
            CheckRevocationStatus = false
        };

        // Assert
        settings.ValidationMode.Should().Be(PluginSignatureValidationMode.AllowUnsigned);
        settings.AllowTestCertificates.Should().BeTrue();
        settings.TrustedIssuers.Should().HaveCount(2);
        settings.AllowedUnsignedPlugins.Should().HaveCount(1);
        settings.CheckRevocationStatus.Should().BeFalse();
    }

    #endregion

    #region PluginSignatureStatus Enum Tests

    [Fact]
    public void PluginSignatureStatus_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<PluginSignatureStatus>().Should().Contain(new[]
        {
            PluginSignatureStatus.Valid,
            PluginSignatureStatus.Invalid,
            PluginSignatureStatus.NotSigned,
            PluginSignatureStatus.Expired,
            PluginSignatureStatus.Untrusted,
            PluginSignatureStatus.ValidationError
        });
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ValidateAsync_WithEmptyPath_ShouldHandleGracefully()
    {
        // Arrange
        var settings = PluginSignatureSettings.Production;
        var validator = new PluginSignatureValidator(settings);

        // Act
        var result = await validator.ValidateAsync("");

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(PluginSignatureStatus.ValidationError);
    }

    [Fact]
    public async Task ValidateAsync_WithNullPath_ShouldReturnValidationError()
    {
        // Arrange
        var settings = PluginSignatureSettings.Production;
        var validator = new PluginSignatureValidator(settings);

        // Act
        var result = await validator.ValidateAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(PluginSignatureStatus.ValidationError);
    }

    [Fact]
    public async Task ValidateAsync_WithNonDllFile_ShouldAttemptValidation()
    {
        // Arrange
        var settings = PluginSignatureSettings.Development;
        var validator = new PluginSignatureValidator(settings);
        var tempFile = Path.GetTempFileName();
        TempFiles.Add(tempFile);
        File.WriteAllText(tempFile, "not a dll");

        // Act
        var result = await validator.ValidateAsync(tempFile);

        // Assert
        result.Should().NotBeNull();
        // It should attempt validation even for non-DLL files
    }

    #endregion
}