using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Security)]
[Collection(TestCollections.ProcessState)]
public class PluginSignatureValidatorTests : TemporaryFileTestBase
{
    [Fact]
    public void ProductionSignaturePolicy_ShouldRequireExactCommittedPackageTrust()
    {
        var source = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Lib.Plugins", "PluginSignatureValidator.cs");

        source.Should().Contain("TrustedPluginPackageStore.IsTrustedFile");
    }

    [Fact]
    public void IoCModule_ShouldResolveSignaturePolicyThroughCreateForRuntime()
    {
        var source = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Lib.Plugins", "IoCModule.cs");

        source.Should().Contain("PluginSignatureSettings.CreateForRuntime");
        source.Should().NotContain("TryCreateFromEnvironmentValue");
        source.Should().NotContain("string.Equals(mode, \"disable\"");
    }

    [Fact]
    public void PluginSignatureValidator_ShouldForceProductionWhenRelaxedModesDisallowed()
    {
        var source = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Lib.Plugins", "PluginSignatureValidator.cs");

        source.Should().Contain("PluginSignatureSettings.RelaxedModesAllowed");
        source.Should().Contain("PluginSignatureSettings.Production");
    }

    [Fact]
    public void PluginSignatureSettings_CreateForRuntime_ShouldGateRelaxedModesOnDebug()
    {
        var source = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Lib.Plugins", "PluginSignatureSettings.cs");

        source.Should().Contain("RelaxedModesAllowed");
        source.Should().Contain("CreateForRuntime");
        source.Should().Contain("#if DEBUG");
        source.Should().Contain("if (!RelaxedModesAllowed)");
        source.Should().Contain("return Production;");
    }

    [Fact]
    public void PluginSignatureValidator_ShouldNotTreatInvalidAuthenticodeAsUnsigned()
    {
        var source = RepositoryPaths.ReadFile(
            "UniversalDeviceToolkit.Lib.Plugins", "PluginSignatureValidator.cs");
        var start = source.IndexOf("if (!authenticodeOk)", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);
        var end = source.IndexOf("ValidateCertificateAsync", start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start);
        var block = source[start..end];

        block.Should().Contain("PluginSignatureStatus.Invalid");
        block.Should().NotContain("PluginSignatureValidationMode.AllowUnsigned");
        block.Should().NotContain("IsAllowedByPolicy");
        block.Should().NotContain("PluginSignatureStatus.NotSigned");
    }

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
    public async Task ValidateAsync_WhenValidationDisabled_ShouldHonorDebugOnly()
    {
        // Arrange
        var settings = PluginSignatureSettings.Disabled;
        var validator = new PluginSignatureValidator(settings);
        var tempFile = CreateTempFile("test dll content");

        // Act
        var result = await validator.ValidateAsync(tempFile);

        // Assert
        result.Should().NotBeNull();
        if (PluginSignatureSettings.RelaxedModesAllowed)
        {
            result.IsValid.Should().BeTrue();
            result.Status.Should().Be(PluginSignatureStatus.Valid);
        }
        else
        {
            result.IsValid.Should().BeFalse();
            result.Status.Should().Be(PluginSignatureStatus.NotSigned);
            result.IsAllowedByPolicy.Should().BeFalse();
        }
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
    public async Task ValidateAsync_WithUnsignedFileAndAllowUnsigned_ShouldHonorDebugOnly()
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
        if (PluginSignatureSettings.RelaxedModesAllowed)
        {
            result.IsAllowedByPolicy.Should().BeTrue();
            result.IsValid.Should().BeTrue();
        }
        else
        {
            result.IsAllowedByPolicy.Should().BeFalse();
            result.IsValid.Should().BeFalse();
        }
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
    public async Task ValidateAsync_WithTrustedOnlinePackageHash_ShouldAcceptExactUnsignedFile()
    {
        // Arrange
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());

        var pluginDirectory = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginDirectory, "trusted-plugin.dll");
        File.WriteAllText(pluginPath, "trusted package content");

        try
        {
            TrustedPluginPackageStore.TrustPluginDirectory("trusted-plugin", pluginDirectory);

            var validator = new PluginSignatureValidator(PluginSignatureSettings.Production);

            // Act
            var result = await validator.ValidateAsync(pluginPath);

            // Assert
            result.Status.Should().Be(PluginSignatureStatus.Valid);
            result.IsValid.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public async Task ScopedRepositoryAuthorization_ShouldSatisfyProductionPolicyWithoutGlobalTrust()
    {
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
        var pluginDirectory = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginDirectory, "scoped-plugin.dll");
        File.WriteAllText(pluginPath, "verified repository package content");
        PluginPackageAuthorization? authorization = null;

        try
        {
            authorization = TrustedPluginPackageStore.CreateAuthorization(
                "scoped-plugin",
                pluginDirectory);
            var productionValidator = new PluginSignatureValidator(
                PluginSignatureSettings.Production);

            var globalResult = await productionValidator.ValidateAsync(pluginPath);
            var scopedResult = await authorization
                .Scope(productionValidator)
                .ValidateAsync(pluginPath);

            globalResult.IsValid.Should().BeFalse();
            scopedResult.IsValid.Should().BeTrue();
            TrustedPluginPackageStore.IsTrustedFile(pluginPath).Should().BeFalse(
                "transaction authorization must not publish global trust");
        }
        finally
        {
            authorization?.Close();
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public async Task ScopedRepositoryAuthorization_AfterClose_ShouldRejectReplay()
    {
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
        var pluginDirectory = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginDirectory, "closed-authorization.dll");
        File.WriteAllText(pluginPath, "closed transaction bytes");
        try
        {
            var authorization = TrustedPluginPackageStore.CreateAuthorization(
                "closed-authorization",
                pluginDirectory);
            var validator = new PluginSignatureValidator(PluginSignatureSettings.Production);
            var scopedValidator = authorization.Scope(validator);
            (await scopedValidator.ValidateAsync(pluginPath)).IsValid.Should().BeTrue();

            authorization.Close();

            (await scopedValidator.ValidateAsync(pluginPath)).IsValid.Should().BeFalse();
            Action replay = () => authorization.Scope(validator);
            replay.Should().Throw<InvalidOperationException>();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public void ScopedRepositoryAuthorization_ShouldRejectDifferentSignaturePolicyInstance()
    {
        var pluginDirectory = CreateTempDirectory();
        File.WriteAllText(
            Path.Combine(pluginDirectory, "policy-bound.dll"),
            "policy-bound bytes");
        var authorization = TrustedPluginPackageStore.CreateAuthorization(
            "policy-bound",
            pluginDirectory);
        try
        {
            authorization.Scope(new PluginSignatureValidator(PluginSignatureSettings.Production));

            Action crossPolicy = () => authorization.Scope(
                new PluginSignatureValidator(PluginSignatureSettings.Production));

            crossPolicy.Should().Throw<InvalidOperationException>()
                .WithMessage("*different signature policy*");
        }
        finally
        {
            authorization.Close();
        }
    }

    [Fact]
    public async Task ScopedRepositoryAuthorization_TwoPublishers_ShouldConsumeExactlyOnce()
    {
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
        var pluginDirectory = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginDirectory, "single-publish.dll");
        File.WriteAllText(pluginPath, "single publication bytes");
        var authorization = TrustedPluginPackageStore.CreateAuthorization(
            "single-publish",
            pluginDirectory);
        using var start = new ManualResetEventSlim();
        try
        {
            var publishers = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
            {
                start.Wait();
                TrustedPluginPackageStore.PublishAuthorizationStrict(authorization);
            })).ToArray();
            start.Set();

            var results = await Task.WhenAll(publishers.Select(async publisher =>
            {
                try
                {
                    await publisher;
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }));

            results.Count(result => result).Should().Be(1);
            TrustedPluginPackageStore.IsTrustedFile(pluginPath).Should().BeTrue();
            authorization.IsActive.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public async Task ScopedRepositoryAuthorization_CloseRacingPublish_ShouldHaveOneWinner()
    {
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
        var pluginDirectory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(pluginDirectory, "close-race.dll"), "close race bytes");
        var authorization = TrustedPluginPackageStore.CreateAuthorization(
            "close-race",
            pluginDirectory);
        using var start = new ManualResetEventSlim();
        try
        {
            var close = Task.Run(() =>
            {
                start.Wait();
                authorization.Close();
            });
            var publish = Task.Run(() =>
            {
                start.Wait();
                TrustedPluginPackageStore.PublishAuthorizationStrict(authorization);
            });
            start.Set();

            var results = await Task.WhenAll(new[] { close, publish }.Select(async operation =>
            {
                try
                {
                    await operation;
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }));

            results.Count(result => result).Should().Be(1);
            authorization.IsActive.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public void ScopedRepositoryAuthorization_FailedPublication_ShouldRejectReplay()
    {
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
        var pluginDirectory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(pluginDirectory, "failed-publish.dll"), "failed publish bytes");
        var authorization = TrustedPluginPackageStore.CreateAuthorization(
            "failed-publish",
            pluginDirectory);
        TrustedPluginPackageStore.PersistenceBoundaryOverride = () =>
            throw new IOException("injected publication failure");
        try
        {
            Action publish = () =>
                TrustedPluginPackageStore.PublishAuthorizationStrict(authorization);
            publish.Should().Throw<IOException>();

            publish.Should().Throw<InvalidOperationException>();
            authorization.IsActive.Should().BeFalse();
        }
        finally
        {
            TrustedPluginPackageStore.PersistenceBoundaryOverride = null;
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public void ScopedRepositoryAuthorization_SuccessfulPublication_ShouldRejectReplay()
    {
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
        var pluginDirectory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(pluginDirectory, "consumed.dll"), "consumed bytes");
        var authorization = TrustedPluginPackageStore.CreateAuthorization(
            "consumed",
            pluginDirectory);
        try
        {
            TrustedPluginPackageStore.PublishAuthorizationStrict(authorization);

            Action replay = () =>
                TrustedPluginPackageStore.PublishAuthorizationStrict(authorization);
            replay.Should().Throw<InvalidOperationException>();
            authorization.IsActive.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public async Task ValidateAsync_WhenTrustedPackageMovesDirectory_ShouldRejectWithoutTrustWidening()
    {
        // Arrange
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());

        var originalPluginDirectory = CreateTempDirectory();
        var movedPluginDirectory = CreateTempDirectory();
        var originalPluginPath = Path.Combine(originalPluginDirectory, "trusted-plugin.dll");
        var movedPluginPath = Path.Combine(movedPluginDirectory, "trusted-plugin.dll");
        File.WriteAllText(originalPluginPath, "trusted package content");
        File.Copy(originalPluginPath, movedPluginPath);

        try
        {
            TrustedPluginPackageStore.TrustPluginDirectory("trusted-plugin", originalPluginDirectory);

            var validator = new PluginSignatureValidator(PluginSignatureSettings.Production);

            // Act
            var result = await validator.ValidateAsync(movedPluginPath);

            // Assert
            result.Status.Should().Be(PluginSignatureStatus.NotSigned);
            result.IsAllowedByPolicy.Should().BeFalse();
            result.IsValid.Should().BeFalse();
            TrustedPluginPackageStore.IsTrustedFile(movedPluginPath).Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public async Task ValidateAsync_WhenTrustedOnlinePackageFileChanges_ShouldRejectUnsignedFile()
    {
        // Arrange
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());

        var pluginDirectory = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginDirectory, "trusted-plugin.dll");
        File.WriteAllText(pluginPath, "trusted package content");

        try
        {
            TrustedPluginPackageStore.TrustPluginDirectory("trusted-plugin", pluginDirectory);
            File.WriteAllText(pluginPath, "tampered package content");

            var validator = new PluginSignatureValidator(PluginSignatureSettings.Production);

            // Act
            var result = await validator.ValidateAsync(pluginPath);

            // Assert
            result.Status.Should().Be(PluginSignatureStatus.NotSigned);
            result.IsAllowedByPolicy.Should().BeFalse();
            result.IsValid.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public async Task ValidateAsync_AfterRemovingTrustedPackageWithDifferentCasing_ShouldRejectUnsignedFile()
    {
        // Arrange
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());

        var pluginDirectory = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginDirectory, "trusted-plugin.dll");
        File.WriteAllText(pluginPath, "trusted package content");

        try
        {
            TrustedPluginPackageStore.TrustPluginDirectory("Trusted-Plugin", pluginDirectory);
            TrustedPluginPackageStore.RemoveStrict("trusted-plugin");

            var validator = new PluginSignatureValidator(PluginSignatureSettings.Production);

            // Act
            var result = await validator.ValidateAsync(pluginPath);

            // Assert
            result.Status.Should().Be(PluginSignatureStatus.NotSigned);
            result.IsAllowedByPolicy.Should().BeFalse();
            result.IsValid.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
    }

    [Fact]
    public void RemoveStrict_WhenPersistenceFails_ShouldThrowAndKeepTrustRecord()
    {
        var originalAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
        var pluginDirectory = CreateTempDirectory();
        var pluginPath = Path.Combine(pluginDirectory, "strict-remove-plugin.dll");
        File.WriteAllText(pluginPath, "trusted package content");

        try
        {
            TrustedPluginPackageStore.TrustPluginDirectory(
                "strict-remove-plugin",
                pluginDirectory);
            TrustedPluginPackageStore.PersistenceBoundaryOverride = () =>
                throw new IOException("injected trust persistence failure");

            Action action = () =>
                TrustedPluginPackageStore.RemoveStrict("strict-remove-plugin");

            action.Should().Throw<IOException>()
                .WithMessage("*injected trust persistence failure*");
            TrustedPluginPackageStore.PersistenceBoundaryOverride = null;
            TrustedPluginPackageStore.IsTrustedFile(pluginPath).Should().BeTrue();
        }
        finally
        {
            TrustedPluginPackageStore.PersistenceBoundaryOverride = null;
            Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, originalAppDataOverride);
        }
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
        if (PluginSignatureSettings.RelaxedModesAllowed)
        {
            result.IsValid.Should().BeTrue();
            result.Status.Should().Be(PluginSignatureStatus.Valid);
        }
        else
        {
            result.IsValid.Should().BeFalse();
            result.Status.Should().Be(PluginSignatureStatus.ValidationError);
        }
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

    [Theory]
    [InlineData("require", PluginSignatureValidationMode.RequireSignature)]
    [InlineData("require-signature", PluginSignatureValidationMode.RequireSignature)]
    [InlineData("production", PluginSignatureValidationMode.RequireSignature)]
    [InlineData("allowunsigned", PluginSignatureValidationMode.AllowUnsigned)]
    [InlineData("allow-unsigned", PluginSignatureValidationMode.AllowUnsigned)]
    [InlineData("development", PluginSignatureValidationMode.AllowUnsigned)]
    [InlineData("disable", PluginSignatureValidationMode.DisableValidation)]
    [InlineData("disable-validation", PluginSignatureValidationMode.DisableValidation)]
    [InlineData("disabled", PluginSignatureValidationMode.DisableValidation)]
    public void PluginSignatureSettings_TryCreateFromEnvironmentValue_ShouldParseKnownModes(
        string value,
        PluginSignatureValidationMode expectedMode)
    {
        // Act
        var parsed = PluginSignatureSettings.TryCreateFromEnvironmentValue(value, out var settings);

        // Assert
        parsed.Should().BeTrue();
        settings.ValidationMode.Should().Be(expectedMode);
    }

    [Fact]
    public void PluginSignatureSettings_TryCreateFromEnvironmentValue_ShouldRejectUnknownModes()
    {
        // Act
        var parsed = PluginSignatureSettings.TryCreateFromEnvironmentValue("unexpected", out var settings);

        // Assert
        parsed.Should().BeFalse();
        settings.ValidationMode.Should().Be(PluginSignatureValidationMode.RequireSignature);
    }

    [Theory]
    [InlineData("disable", PluginSignatureValidationMode.DisableValidation)]
    [InlineData("disabled", PluginSignatureValidationMode.DisableValidation)]
    [InlineData("disablevalidation", PluginSignatureValidationMode.DisableValidation)]
    [InlineData("disable-validation", PluginSignatureValidationMode.DisableValidation)]
    [InlineData("development", PluginSignatureValidationMode.AllowUnsigned)]
    [InlineData("allowunsigned", PluginSignatureValidationMode.AllowUnsigned)]
    [InlineData("allow-unsigned", PluginSignatureValidationMode.AllowUnsigned)]
    public void PluginSignatureSettings_CreateForRuntime_ShouldNotLetRelaxedAliasesBypassProduction(
        string value,
        PluginSignatureValidationMode debugMode)
    {
        var settings = PluginSignatureSettings.CreateForRuntime(value);

        if (PluginSignatureSettings.RelaxedModesAllowed)
            settings.ValidationMode.Should().Be(debugMode);
        else
        {
            settings.ValidationMode.Should().Be(PluginSignatureValidationMode.RequireSignature);
            settings.AllowTestCertificates.Should().BeFalse();
            settings.CheckRevocationStatus.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("require")]
    [InlineData("production")]
    [InlineData("unexpected")]
    public void PluginSignatureSettings_CreateForRuntime_ShouldDefaultToProduction(string? value)
    {
        var settings = PluginSignatureSettings.CreateForRuntime(value);

        settings.ValidationMode.Should().Be(PluginSignatureValidationMode.RequireSignature);
        settings.AllowTestCertificates.Should().BeFalse();
        settings.CheckRevocationStatus.Should().BeTrue();
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

    [Fact]
    public async Task ValidateAsync_WithInvalidSignatureAndAllowUnsigned_ShouldReject()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var signedPath = TryFindEmbeddedSignedBinary();
        if (signedPath is null)
            return;

        var tamperedPath = Path.Combine(CreateTempDirectory(), "tampered-signed.dll");
        File.Copy(signedPath, tamperedPath, overwrite: true);
        TamperPeImageBytes(tamperedPath);
        if (!HasEmbeddedAuthenticode(tamperedPath))
            return;

        var validator = new PluginSignatureValidator(PluginSignatureSettings.Development);
        var result = await validator.ValidateAsync(tamperedPath);

        result.IsValid.Should().BeFalse();
        result.IsAllowedByPolicy.Should().BeFalse();
        result.Status.Should().Be(PluginSignatureStatus.Invalid);
    }

    private static string? TryFindEmbeddedSignedBinary()
    {
        foreach (var name in new[] { "kernel32.dll", "user32.dll", "advapi32.dll", "ntdll.dll" })
        {
            var path = Path.Combine(Environment.SystemDirectory, name);
            if (File.Exists(path) && HasEmbeddedAuthenticode(path))
                return path;
        }

        return null;
    }

    private static bool HasEmbeddedAuthenticode(string path)
    {
        try
        {
#pragma warning disable SYSLIB0057
            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
#pragma warning restore SYSLIB0057
            return certificate.Handle != IntPtr.Zero;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void TamperPeImageBytes(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 1024)
            throw new InvalidOperationException("Signed system binary is unexpectedly small.");

        // Authenticode excludes the certificate table (typically at EOF). Flip a
        // mid-file image byte so WinVerifyTrust fails while the cert blob remains.
        var index = Math.Min(bytes.Length / 2, bytes.Length - 512);
        bytes[index] ^= 0x01;
        File.WriteAllBytes(path, bytes);
    }

    #endregion
}
