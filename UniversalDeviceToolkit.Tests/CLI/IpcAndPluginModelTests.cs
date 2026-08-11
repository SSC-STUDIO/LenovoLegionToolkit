using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.CLI.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests.CLI;

[Trait("Category", TestCategories.Unit)]
public class IpcAndPluginModelTests
{
    #region IpcRequest Tests

    [Fact]
    public void IpcRequest_Defaults_ShouldBeNull()
    {
        var req = new IpcRequest();
        req.Operation.Should().BeNull();
        req.Name.Should().BeNull();
        req.Value.Should().BeNull();
        req.AuthToken.Should().BeNull();
    }

    [Fact]
    public void IpcRequest_SetProperties_ShouldRetainValues()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.GetFeatureValue,
            Name = "ThermalMode",
            Value = "Performance",
            AuthToken = "secret-token"
        };
        req.Operation.Should().Be(IpcRequest.OperationType.GetFeatureValue);
        req.Name.Should().Be("ThermalMode");
        req.Value.Should().Be("Performance");
        req.AuthToken.Should().Be("secret-token");
    }

    [Theory]
    [InlineData(IpcRequest.OperationType.Unknown)]
    [InlineData(IpcRequest.OperationType.ListFeatures)]
    [InlineData(IpcRequest.OperationType.ListFeatureValues)]
    [InlineData(IpcRequest.OperationType.ListQuickActions)]
    [InlineData(IpcRequest.OperationType.GetFeatureValue)]
    [InlineData(IpcRequest.OperationType.SetFeatureValue)]
    [InlineData(IpcRequest.OperationType.GetSpectrumProfile)]
    [InlineData(IpcRequest.OperationType.SetSpectrumProfile)]
    [InlineData(IpcRequest.OperationType.GetSpectrumBrightness)]
    [InlineData(IpcRequest.OperationType.SetSpectrumBrightness)]
    [InlineData(IpcRequest.OperationType.GetRGBPreset)]
    [InlineData(IpcRequest.OperationType.SetRGBPreset)]
    [InlineData(IpcRequest.OperationType.QuickAction)]
    [InlineData(IpcRequest.OperationType.IsShellRegistered)]
    [InlineData(IpcRequest.OperationType.IsShellInstalled)]
    [InlineData(IpcRequest.OperationType.InstallShell)]
    [InlineData(IpcRequest.OperationType.UninstallShell)]
    [InlineData(IpcRequest.OperationType.GetAppStatus)]
    [InlineData(IpcRequest.OperationType.GetNetworkAccelerationStatus)]
    [InlineData(IpcRequest.OperationType.StartNetworkAcceleration)]
    [InlineData(IpcRequest.OperationType.StopNetworkAcceleration)]
    [InlineData(IpcRequest.OperationType.RunNetworkDiagnostics)]
    public void IpcRequest_OperationType_AllValues_ShouldBeDefined(IpcRequest.OperationType op)
    {
        Enum.IsDefined(op).Should().BeTrue();
    }

    [Fact]
    public void IpcRequest_OperationType_Has22Values()
    {
        Enum.GetValues<IpcRequest.OperationType>().Should().HaveCount(22);
    }

    #endregion

    #region IpcResponse Tests

    [Fact]
    public void IpcResponse_Defaults_ShouldBeNull()
    {
        var resp = new IpcResponse();
        resp.Success.Should().BeFalse();
        resp.Message.Should().BeNull();
    }

    [Fact]
    public void IpcResponse_SetSuccess_ShouldRetainValue()
    {
        var resp = new IpcResponse { Success = true, Message = "OK" };
        resp.Success.Should().BeTrue();
        resp.Message.Should().Be("OK");
    }

    [Fact]
    public void IpcResponse_SetFailure_ShouldRetainValue()
    {
        var resp = new IpcResponse { Success = false, Message = "Not found" };
        resp.Success.Should().BeFalse();
        resp.Message.Should().Be("Not found");
    }

    [Fact]
    public void IpcResponse_SerializeRoundtrip_ShouldPreserveData()
    {
        var resp = new IpcResponse { Success = true, Message = "test" };
        var json = JsonSerializer.Serialize(resp);
        var deserialized = JsonSerializer.Deserialize<IpcResponse>(json);
        deserialized.Should().NotBeNull();
        deserialized!.Success.Should().BeTrue();
        deserialized.Message.Should().Be("test");
    }

    #endregion

    #region IpcException Tests

    [Fact]
    public void IpcException_Message_ShouldRetainValue()
    {
        var ex = new IpcException("test error");
        ex.Message.Should().Be("test error");
    }

    [Fact]
    public void IpcException_NullMessage_ShouldNotThrow()
    {
        var act = () => new IpcException(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void IpcException_ShouldBeException()
    {
        new IpcException("x").Should().BeAssignableTo<Exception>();
    }

    #endregion

    #region IpcConnectException Tests

    [Fact]
    public void IpcConnectException_ShouldBeException()
    {
        new IpcConnectException().Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void IpcConnectException_DefaultMessage_ShouldNotBeNull()
    {
        var ex = new IpcConnectException();
        ex.Message.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region PluginManifest Defaults Tests

    [Fact]
    public void PluginManifest_Defaults_ShouldHaveExpectedValues()
    {
        var m = new PluginManifest();
        m.Id.Should().BeEmpty();
        m.Name.Should().BeEmpty();
        m.Description.Should().BeEmpty();
        m.Version.Should().Be("1.0.0");
        m.MinimumHostVersion.Should().Be("1.0.0");
        m.DownloadUrl.Should().BeEmpty();
        m.FileHash.Should().BeEmpty();
        m.FileSize.Should().Be(0);
        m.IsSystemPlugin.Should().BeFalse();
    }

    [Fact]
    public void PluginManifest_LegacyMinimumHostVersion_Set_ShouldOverrideMinimumHostVersion()
    {
        var m = new PluginManifest { LegacyMinimumHostVersion = "2.0.0" };
        m.MinimumHostVersion.Should().Be("2.0.0");
    }

    [Fact]
    public void PluginManifest_LegacyMinimumHostVersion_Null_ShouldNotOverride()
    {
        var m = new PluginManifest { MinimumHostVersion = "3.0.0" };
        m.LegacyMinimumHostVersion = null;
        m.MinimumHostVersion.Should().Be("3.0.0");
    }

    [Fact]
    public void PluginManifest_LegacyMinimumHostVersion_Empty_ShouldNotOverride()
    {
        var m = new PluginManifest { MinimumHostVersion = "3.0.0" };
        m.LegacyMinimumHostVersion = "";
        m.MinimumHostVersion.Should().Be("3.0.0");
    }

    [Fact]
    public void PluginManifest_LegacyMinimumHostVersion_Whitespace_ShouldNotOverride()
    {
        var m = new PluginManifest { MinimumHostVersion = "3.0.0" };
        m.LegacyMinimumHostVersion = "   ";
        m.MinimumHostVersion.Should().Be("3.0.0");
    }

    [Fact]
    public void PluginManifest_SetProperties_ShouldRetainValues()
    {
        var m = new PluginManifest
        {
            Id = "test-plugin",
            Name = "Test Plugin",
            Description = "A test",
            Author = "Tester",
            Icon = "icon.png",
            Version = "2.0.0",
            MinimumHostVersion = "1.5.0",
            DownloadUrl = "https://example.com/download",
            FileHash = "abc123",
            FileSize = 1024,
            ReleaseDate = "2024-01-01",
            IsSystemPlugin = true,
            Dependencies = new[] { "dep1" },
            Tags = new[] { "tag1", "tag2" },
            Changelog = "v2.0.0 - Initial"
        };
        m.Id.Should().Be("test-plugin");
        m.Name.Should().Be("Test Plugin");
        m.Dependencies.Should().HaveCount(1);
        m.Tags.Should().HaveCount(2);
    }

    #endregion

    #region PluginManifestStore Tests

    [Fact]
    public void PluginManifestStore_Defaults_ShouldHaveExpectedValues()
    {
        var s = new PluginManifestStore();
        s.Description.Should().BeEmpty();
        s.Details.Should().BeNull();
        s.UsageGuide.Should().BeNull();
        s.Localizations.Should().BeNull();
    }

    #endregion

    #region PluginManifestLocalization Tests

    [Fact]
    public void PluginManifestLocalization_Defaults_ShouldBeNull()
    {
        var l = new PluginManifestLocalization();
        l.Name.Should().BeNull();
        l.Description.Should().BeNull();
        l.Details.Should().BeNull();
        l.UsageGuide.Should().BeNull();
    }

    #endregion

    #region PluginManifestContributions Tests

    [Fact]
    public void PluginManifestContributions_Defaults_ShouldBeNull()
    {
        var c = new PluginManifestContributions();
        c.FeaturePage.Should().BeNull();
        c.SettingsPage.Should().BeNull();
        c.Runtime.Should().BeNull();
        c.OptimizationActions.Should().BeNull();
    }

    [Fact]
    public void PluginManifestPageContribution_SetProperties_ShouldRetainValues()
    {
        var p = new PluginManifestPageContribution { Class = "MyPage", Title = "My Title" };
        p.Class.Should().Be("MyPage");
        p.Title.Should().Be("My Title");
    }

    [Fact]
    public void PluginManifestRuntimeContribution_SetProperties_ShouldRetainValues()
    {
        var r = new PluginManifestRuntimeContribution { Class = "MyRuntime" };
        r.Class.Should().Be("MyRuntime");
    }

    [Fact]
    public void PluginManifestOptimizationContribution_SetProperties_ShouldRetainValues()
    {
        var a = new PluginManifestOptimizationContribution
        {
            Id = "opt-1",
            Key = "key-1",
            Title = "Optimization 1",
            Description = "Desc",
            Recommended = true
        };
        a.Id.Should().Be("opt-1");
        a.Recommended.Should().BeTrue();
    }

    [Fact]
    public void PluginManifestOptimizationContribution_RecommendedDefault_ShouldBeNull()
    {
        new PluginManifestOptimizationContribution().Recommended.Should().BeNull();
    }

    #endregion

    #region PluginStoreResponse Tests

    [Fact]
    public void PluginStoreResponse_Defaults_ShouldHaveExpectedValues()
    {
        var r = new PluginStoreResponse();
        r.Plugins.Should().BeEmpty();
        r.LastUpdated.Should().BeEmpty();
        r.StoreVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void PluginStoreResponse_LegacyStoreVersion_Set_ShouldOverrideStoreVersion()
    {
        var r = new PluginStoreResponse { LegacyStoreVersion = "3.0.0" };
        r.StoreVersion.Should().Be("3.0.0");
    }

    [Fact]
    public void PluginStoreResponse_LegacyStoreVersion_Null_ShouldNotOverride()
    {
        var r = new PluginStoreResponse { StoreVersion = "5.0.0" };
        r.LegacyStoreVersion = null;
        r.StoreVersion.Should().Be("5.0.0");
    }

    [Fact]
    public void PluginStoreResponse_LegacyStoreVersion_Empty_ShouldNotOverride()
    {
        var r = new PluginStoreResponse { StoreVersion = "5.0.0" };
        r.LegacyStoreVersion = "";
        r.StoreVersion.Should().Be("5.0.0");
    }

    [Fact]
    public void PluginStoreResponse_LegacyStoreVersion_Whitespace_ShouldNotOverride()
    {
        var r = new PluginStoreResponse { StoreVersion = "5.0.0" };
        r.LegacyStoreVersion = "  ";
        r.StoreVersion.Should().Be("5.0.0");
    }

    #endregion

    #region PluginDownloadProgress Tests

    [Fact]
    public void PluginDownloadProgress_Defaults_ShouldHaveExpectedValues()
    {
        var p = new PluginDownloadProgress();
        p.PluginId.Should().BeEmpty();
        p.BytesDownloaded.Should().Be(0);
        p.TotalBytes.Should().Be(0);
        p.ProgressPercentage.Should().Be(0);
        p.IsCompleted.Should().BeFalse();
        p.ErrorMessage.Should().BeNull();
        p.LocalFilePath.Should().BeNull();
    }

    [Fact]
    public void PluginDownloadProgress_SetProperties_ShouldRetainValues()
    {
        var p = new PluginDownloadProgress
        {
            PluginId = "my-plugin",
            BytesDownloaded = 512,
            TotalBytes = 1024,
            ProgressPercentage = 50.0,
            IsCompleted = false,
            ErrorMessage = "timeout",
            LocalFilePath = @"C:\temp\plugin.zip"
        };
        p.PluginId.Should().Be("my-plugin");
        p.ProgressPercentage.Should().Be(50.0);
        p.ErrorMessage.Should().Be("timeout");
    }

    #endregion

    #region GitHubFileResponse Tests

    [Fact]
    public void GitHubFileResponse_Defaults_ShouldHaveExpectedValues()
    {
        var g = new GitHubFileResponse();
        g.Name.Should().BeEmpty();
        g.Path.Should().BeEmpty();
        g.Sha.Should().BeEmpty();
        g.Size.Should().Be(0);
        g.Url.Should().BeEmpty();
        g.HtmlUrl.Should().BeEmpty();
        g.GitUrl.Should().BeEmpty();
        g.DownloadUrl.Should().BeNull();
        g.Type.Should().BeEmpty();
        g.Content.Should().BeEmpty();
        g.Encoding.Should().BeEmpty();
    }

    #endregion

    #region PluginMetadata Tests

    [Fact]
    public void PluginMetadata_Defaults_ShouldHaveExpectedValues()
    {
        var m = new PluginMetadata();
        m.Id.Should().BeEmpty();
        m.Name.Should().BeEmpty();
        m.Description.Should().BeEmpty();
        m.Icon.Should().BeEmpty();
        m.IsSystemPlugin.Should().BeFalse();
        m.Dependencies.Should().BeNull();
        m.Version.Should().Be("1.0.0");
        m.MinimumHostVersion.Should().Be("1.0.0");
        m.Author.Should().BeNull();
        m.FilePath.Should().BeNull();
    }

    [Fact]
    public void PluginMetadata_SetProperties_ShouldRetainValues()
    {
        var m = new PluginMetadata
        {
            Id = "plugin-id",
            Name = "Plugin Name",
            Description = "Desc",
            Icon = "icon.png",
            IsSystemPlugin = true,
            Dependencies = new[] { "dep1", "dep2" },
            Version = "2.0.0",
            MinimumHostVersion = "1.5.0",
            Author = "Author",
            FilePath = @"C:\plugins\test.dll"
        };
        m.Id.Should().Be("plugin-id");
        m.Dependencies.Should().HaveCount(2);
    }

    #endregion

    #region PluginState Enum Tests

    [Fact]
    public void PluginState_Has5Values()
    {
        Enum.GetValues<PluginState>().Should().HaveCount(5);
    }

    [Theory]
    [InlineData(PluginState.NotInstalled)]
    [InlineData(PluginState.Installed)]
    [InlineData(PluginState.Enabled)]
    [InlineData(PluginState.Disabled)]
    [InlineData(PluginState.Error)]
    public void PluginState_AllValues_ShouldBeDefined(PluginState state)
    {
        Enum.IsDefined(state).Should().BeTrue();
    }

    #endregion

    #region PluginHealthStatus Enum Tests

    [Fact]
    public void PluginHealthStatus_Has6Values()
    {
        Enum.GetValues<PluginHealthStatus>().Should().HaveCount(6);
    }

    [Theory]
    [InlineData(PluginHealthStatus.Healthy)]
    [InlineData(PluginHealthStatus.Warning)]
    [InlineData(PluginHealthStatus.Error)]
    [InlineData(PluginHealthStatus.NotFound)]
    [InlineData(PluginHealthStatus.MissingDependencies)]
    [InlineData(PluginHealthStatus.VersionIncompatible)]
    public void PluginHealthStatus_AllValues_ShouldBeDefined(PluginHealthStatus status)
    {
        Enum.IsDefined(status).Should().BeTrue();
    }

    #endregion

    #region PluginStateChangedEventArgs Tests

    [Fact]
    public void PluginStateChangedEventArgs_SetProperties_ShouldRetainValues()
    {
        var args = new PluginStateChangedEventArgs("test-plugin", PluginState.Disabled, PluginState.Enabled, "activated");
        args.PluginId.Should().Be("test-plugin");
        args.OldState.Should().Be(PluginState.Disabled);
        args.NewState.Should().Be(PluginState.Enabled);
        args.ErrorMessage.Should().Be("activated");
    }

    [Fact]
    public void PluginStateChangedEventArgs_NullError_ShouldBeNull()
    {
        var args = new PluginStateChangedEventArgs("p", PluginState.Installed, PluginState.Enabled);
        args.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void PluginStateChangedEventArgs_ShouldBeEventArgs()
    {
        new PluginStateChangedEventArgs("x", PluginState.Error, PluginState.Disabled)
            .Should().BeAssignableTo<EventArgs>();
    }

    #endregion

    #region PluginUpdateInfo Tests

    [Fact]
    public void PluginUpdateInfo_Defaults_ShouldHaveExpectedValues()
    {
        var u = new PluginUpdateInfo();
        u.PluginId.Should().BeEmpty();
        u.CurrentVersion.Should().BeEmpty();
        u.NewVersion.Should().BeEmpty();
        u.DownloadUrl.Should().BeEmpty();
        u.Changelog.Should().BeEmpty();
        u.ReleaseDate.Should().BeEmpty();
    }

    #endregion

    #region UpdateCheckResult Tests

    [Fact]
    public void UpdateCheckResult_Defaults_ShouldHaveExpectedValues()
    {
        var r = new UpdateCheckResult();
        r.AvailableUpdates.Should().BeEmpty();
        r.HasUpdates.Should().BeFalse();
        r.IsSuccess.Should().BeFalse();
        r.ErrorMessage.Should().BeNull();
        r.LastCheckTime.Should().BeNull();
    }

    [Fact]
    public void UpdateCheckResult_HasUpdates_WhenNotEmpty_ShouldBeTrue()
    {
        var r = new UpdateCheckResult
        {
            AvailableUpdates = { new PluginUpdateInfo { PluginId = "p1" } }
        };
        r.HasUpdates.Should().BeTrue();
    }

    #endregion

    #region CompatibilityUpdateCheckResult Tests

    [Fact]
    public void CompatibilityUpdateCheckResult_Defaults_ShouldHaveExpectedValues()
    {
        var r = new CompatibilityUpdateCheckResult();
        r.IncompatiblePlugins.Should().BeEmpty();
        r.HasUpdates.Should().BeFalse();
    }

    #endregion

    #region VersionChecker Tests

    [Fact]
    public void VersionChecker_NullVersion_ShouldThrow()
    {
        var act = () => new VersionChecker(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VersionChecker_IsCompatible_WhenHigherVersion_ShouldReturnTrue()
    {
        var checker = new VersionChecker("2.0.0");
        checker.IsCompatible("1.0.0").Should().BeTrue();
    }

    [Fact]
    public void VersionChecker_IsCompatible_WhenSameVersion_ShouldReturnTrue()
    {
        var checker = new VersionChecker("1.0.0");
        checker.IsCompatible("1.0.0").Should().BeTrue();
    }

    [Fact]
    public void VersionChecker_IsCompatible_WhenLowerVersion_ShouldReturnFalse()
    {
        var checker = new VersionChecker("1.0.0");
        checker.IsCompatible("2.0.0").Should().BeFalse();
    }

    [Fact]
    public void VersionChecker_IsCompatible_WhenNullOrEmpty_ShouldReturnTrue()
    {
        var checker = new VersionChecker("1.0.0");
        checker.IsCompatible(null!).Should().BeTrue();
        checker.IsCompatible("").Should().BeTrue();
        checker.IsCompatible("  ").Should().BeTrue();
    }

    [Fact]
    public void VersionChecker_IsCompatible_WhenInvalidVersion_ShouldReturnFalse()
    {
        var checker = new VersionChecker("1.0.0");
        checker.IsCompatible("not-a-version").Should().BeFalse();
    }

    [Fact]
    public void VersionChecker_IsUpdateAvailable_WhenNewer_ShouldReturnTrue()
    {
        var checker = new VersionChecker("1.0.0");
        checker.IsUpdateAvailable("1.0.0", "2.0.0").Should().BeTrue();
    }

    [Fact]
    public void VersionChecker_IsUpdateAvailable_WhenSame_ShouldReturnFalse()
    {
        var checker = new VersionChecker("1.0.0");
        checker.IsUpdateAvailable("1.0.0", "1.0.0").Should().BeFalse();
    }

    [Fact]
    public void VersionChecker_IsUpdateAvailable_WhenOlder_ShouldReturnFalse()
    {
        var checker = new VersionChecker("1.0.0");
        checker.IsUpdateAvailable("2.0.0", "1.0.0").Should().BeFalse();
    }

    [Fact]
    public void VersionChecker_IsUpdateAvailable_WhenNewVersionNull_ShouldReturnFalse()
    {
        var checker = new VersionChecker("1.0.0");
        checker.IsUpdateAvailable("1.0.0", null!).Should().BeFalse();
        checker.IsUpdateAvailable("1.0.0", "").Should().BeFalse();
    }

    [Fact]
    public void VersionChecker_IsUpdateAvailable_WhenCurrentVersionEmpty_ShouldTreatAsZero()
    {
        var checker = new VersionChecker("1.0.0");
        checker.IsUpdateAvailable("", "1.0.0").Should().BeTrue();
    }

    [Fact]
    public void VersionChecker_IsUpdateAvailable_WhenInvalidVersion_ShouldReturnFalse()
    {
        var checker = new VersionChecker("1.0.0");
        checker.IsUpdateAvailable("bad", "1.0.0").Should().BeFalse();
    }

    [Fact]
    public void VersionChecker_CompareVersions_Equal_ShouldReturnZero()
    {
        var checker = new VersionChecker("1.0.0");
        checker.CompareVersions("1.0.0", "1.0.0").Should().Be(0);
    }

    [Fact]
    public void VersionChecker_CompareVersions_Less_ShouldReturnNegative()
    {
        var checker = new VersionChecker("1.0.0");
        checker.CompareVersions("1.0.0", "2.0.0").Should().BeLessThan(0);
    }

    [Fact]
    public void VersionChecker_CompareVersions_Greater_ShouldReturnPositive()
    {
        var checker = new VersionChecker("1.0.0");
        checker.CompareVersions("2.0.0", "1.0.0").Should().BeGreaterThan(0);
    }

    [Fact]
    public void VersionChecker_CompareVersions_EmptyVersions_ShouldTreatAsZero()
    {
        var checker = new VersionChecker("1.0.0");
        checker.CompareVersions("", "").Should().Be(0);
    }

    [Fact]
    public void VersionChecker_CompareVersions_InvalidVersions_ShouldReturnZero()
    {
        var checker = new VersionChecker("1.0.0");
        checker.CompareVersions("bad", "worse").Should().Be(0);
    }

    [Fact]
    public void VersionChecker_CheckCompatibility_AllCompatible_ShouldReturnEmpty()
    {
        var checker = new VersionChecker("2.0.0");
        var plugins = new List<PluginManifest>
        {
            new() { MinimumHostVersion = "1.0.0" },
            new() { MinimumHostVersion = "1.5.0" }
        };
        checker.CheckCompatibility(plugins).Should().BeEmpty();
    }

    [Fact]
    public void VersionChecker_CheckCompatibility_SomeIncompatible_ShouldReturnThose()
    {
        var checker = new VersionChecker("1.0.0");
        var plugins = new List<PluginManifest>
        {
            new() { Id = "a", MinimumHostVersion = "1.0.0" },
            new() { Id = "b", MinimumHostVersion = "2.0.0" }
        };
        var incompatible = checker.CheckCompatibility(plugins);
        incompatible.Should().HaveCount(1);
        incompatible[0].Id.Should().Be("b");
    }

    [Fact]
    public void VersionChecker_GetAvailableUpdates_WithUpdates_ShouldReturnThem()
    {
        var checker = new VersionChecker("1.0.0");
        var installed = new Dictionary<string, string> { ["p1"] = "1.0.0" };
        var available = new List<PluginManifest>
        {
            new() { Id = "p1", Version = "2.0.0", DownloadUrl = "https://example.com" }
        };
        var updates = checker.GetAvailableUpdates(installed, available);
        updates.Should().HaveCount(1);
        updates[0].PluginId.Should().Be("p1");
        updates[0].NewVersion.Should().Be("2.0.0");
    }

    [Fact]
    public void VersionChecker_GetAvailableUpdates_NoUpdates_ShouldReturnEmpty()
    {
        var checker = new VersionChecker("1.0.0");
        var installed = new Dictionary<string, string> { ["p1"] = "2.0.0" };
        var available = new List<PluginManifest>
        {
            new() { Id = "p1", Version = "1.0.0" }
        };
        checker.GetAvailableUpdates(installed, available).Should().BeEmpty();
    }

    [Fact]
    public void VersionChecker_GetAvailableUpdates_NotInstalled_ShouldSkip()
    {
        var checker = new VersionChecker("1.0.0");
        var installed = new Dictionary<string, string>();
        var available = new List<PluginManifest>
        {
            new() { Id = "p1", Version = "2.0.0" }
        };
        checker.GetAvailableUpdates(installed, available).Should().BeEmpty();
    }

    [Fact]
    public void VersionChecker_DefaultConstructor_ShouldNotThrow()
    {
        var act = () => new VersionChecker();
        act.Should().NotThrow();
    }

    #endregion

    #region PluginConstants Tests

    [Fact]
    public void PluginConstants_ViveTool_ShouldBeExpectedValue()
    {
        PluginConstants.ViveTool.Should().Be("ViveTool");
    }

    #endregion

    #region PluginPaths Constants Tests

    [Fact]
    public void PluginPaths_PluginsDirectoryName_ShouldBePlugins()
    {
        PluginPaths.PluginsDirectoryName.Should().Be("plugins");
    }

    [Fact]
    public void PluginPaths_PluginMetadataFileName_ShouldBePluginJson()
    {
        PluginPaths.PluginMetadataFileName.Should().Be("Plugin.json");
    }

    [Fact]
    public void PluginPaths_GetPluginDirectory_WithId_ShouldCombine()
    {
        var path = PluginPaths.GetPluginDirectory("my-plugin");
        path.Should().Contain("plugins");
        path.Should().Contain("my-plugin");
    }

    [Fact]
    public void PluginPaths_GetPluginDirectory_EmptyId_ShouldThrow()
    {
        var act = () => PluginPaths.GetPluginDirectory("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PluginPaths_GetPluginDirectory_WhitespaceId_ShouldThrow()
    {
        var act = () => PluginPaths.GetPluginDirectory("  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PluginPaths_GetPluginAssemblyFiles_NonExistentDir_ShouldReturnEmpty()
    {
        var result = PluginPaths.GetPluginAssemblyFiles(@"C:\nonexistent_dir_12345");
        result.Should().BeEmpty();
    }

    [Fact]
    public void PluginPaths_GetPluginMetadataFilePath_NonExistentDir_ShouldReturnNull()
    {
        var result = PluginPaths.GetPluginMetadataFilePath(@"C:\nonexistent_dir_12345");
        result.Should().BeNull();
    }

    [Fact]
    public void PluginPaths_ContainsPlugin_NonExistentDir_ShouldReturnFalse()
    {
        PluginPaths.ContainsPlugin(@"C:\nonexistent_dir_12345").Should().BeFalse();
    }

    [Fact]
    public void PluginPaths_GetAllPossiblePluginsDirectories_ShouldNotBeEmpty()
    {
        var dirs = PluginPaths.GetAllPossiblePluginsDirectories();
        dirs.Should().NotBeEmpty();
        dirs.Should().Contain(d => d.Contains("plugins"));
    }

    [Fact]
    public void PluginPaths_GetDevelopmentPluginsDirectories_ShouldNotBeEmpty()
    {
        var dirs = PluginPaths.GetDevelopmentPluginsDirectories();
        dirs.Should().NotBeEmpty();
    }

    #endregion

    #region PluginManifest Serialization Roundtrip Tests

    [Fact]
    public void PluginManifest_SerializeRoundtrip_ShouldPreserveData()
    {
        var m = new PluginManifest
        {
            Id = "test-id",
            Name = "Test",
            Version = "2.0.0",
            MinimumHostVersion = "1.0.0",
            DownloadUrl = "https://example.com",
            FileSize = 2048,
            IsSystemPlugin = true,
            Tags = new[] { "a", "b" }
        };
        var json = JsonSerializer.Serialize(m);
        var deserialized = JsonSerializer.Deserialize<PluginManifest>(json);
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("test-id");
        deserialized.Tags.Should().HaveCount(2);
    }

    [Fact]
    public void PluginManifest_Deserialize_WithLegacyMinimumHostVersion_ShouldOverride()
    {
        var json = """{"id":"p","minLLTVersion":"3.0.0"}""";
        var m = JsonSerializer.Deserialize<PluginManifest>(json);
        m.Should().NotBeNull();
        m!.MinimumHostVersion.Should().Be("3.0.0");
    }

    [Fact]
    public void PluginStoreResponse_Deserialize_WithLegacyVersion_ShouldOverride()
    {
        var json = """{"version":"4.0.0"}""";
        var r = JsonSerializer.Deserialize<PluginStoreResponse>(json);
        r.Should().NotBeNull();
        r!.StoreVersion.Should().Be("4.0.0");
    }

    #endregion
}