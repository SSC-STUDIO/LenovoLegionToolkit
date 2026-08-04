using System;
using System.Collections.Generic;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
public class PluginSandboxModelTests
{
    #region SandboxPermission Enum Tests

    [Fact]
    public void SandboxPermission_Has11Values()
    {
        Enum.GetValues<SandboxPermission>().Should().HaveCount(11);
    }

    [Theory]
    [InlineData(SandboxPermission.None)]
    [InlineData(SandboxPermission.FileSystemRead)]
    [InlineData(SandboxPermission.FileSystemWrite)]
    [InlineData(SandboxPermission.NetworkAccess)]
    [InlineData(SandboxPermission.RegistryRead)]
    [InlineData(SandboxPermission.RegistryWrite)]
    [InlineData(SandboxPermission.SystemInformation)]
    [InlineData(SandboxPermission.HardwareAccess)]
    [InlineData(SandboxPermission.UICustomization)]
    [InlineData(SandboxPermission.InterPluginCommunication)]
    [InlineData(SandboxPermission.All)]
    public void SandboxPermission_AllValues_ShouldBeDefined(SandboxPermission perm)
    {
        Enum.IsDefined(perm).Should().BeTrue();
    }

    [Fact]
    public void SandboxPermission_None_ShouldBeZero()
    {
        ((int)SandboxPermission.None).Should().Be(0);
    }

    [Fact]
    public void SandboxPermission_All_ShouldBeNegativeOne()
    {
        ((int)SandboxPermission.All).Should().Be(-1);
    }

    [Fact]
    public void SandboxPermission_Combination_ShouldWork()
    {
        var combined = SandboxPermission.FileSystemRead | SandboxPermission.NetworkAccess;
        combined.Should().HaveFlag(SandboxPermission.FileSystemRead);
        combined.Should().HaveFlag(SandboxPermission.NetworkAccess);
        combined.Should().NotHaveFlag(SandboxPermission.HardwareAccess);
    }

    #endregion

    #region ResourceType Enum Tests

    [Fact]
    public void ResourceType_Has5Values()
    {
        Enum.GetValues<ResourceType>().Should().HaveCount(5);
    }

    [Theory]
    [InlineData(ResourceType.Memory)]
    [InlineData(ResourceType.Cpu)]
    [InlineData(ResourceType.FileSystem)]
    [InlineData(ResourceType.Network)]
    [InlineData(ResourceType.ExecutionTime)]
    public void ResourceType_AllValues_ShouldBeDefined(ResourceType type)
    {
        Enum.IsDefined(type).Should().BeTrue();
    }

    #endregion

    #region SandboxConfiguration Defaults Tests

    [Fact]
    public void SandboxConfiguration_Defaults_ShouldHaveExpectedValues()
    {
        var config = new SandboxConfiguration();
        config.Permissions.Should().Be(SandboxPermission.None);
        config.MaxMemoryMB.Should().Be(100);
        config.MaxCpuPercentage.Should().Be(10);
        config.AllowedPaths.Should().BeEmpty();
        config.BlockedPaths.Should().BeEmpty();
        config.AllowedHosts.Should().BeEmpty();
        config.AllowDynamicAssemblyLoading.Should().BeFalse();
        config.AllowReflection.Should().BeFalse();
        config.OperationTimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void SandboxConfiguration_SetProperties_ShouldRetainValues()
    {
        var config = new SandboxConfiguration
        {
            Permissions = SandboxPermission.FileSystemRead | SandboxPermission.NetworkAccess,
            MaxMemoryMB = 512,
            MaxCpuPercentage = 50,
            AllowedPaths = new List<string> { @"C:\Plugins" },
            BlockedPaths = new List<string> { @"C:\Windows" },
            AllowedHosts = new List<string> { "api.example.com" },
            AllowDynamicAssemblyLoading = true,
            AllowReflection = true,
            OperationTimeoutSeconds = 60
        };
        config.Permissions.Should().HaveFlag(SandboxPermission.FileSystemRead);
        config.MaxMemoryMB.Should().Be(512);
        config.AllowDynamicAssemblyLoading.Should().BeTrue();
    }

    #endregion

    #region SandboxedPluginInfo Defaults Tests

    [Fact]
    public void SandboxedPluginInfo_Defaults_ShouldHaveExpectedValues()
    {
        var info = new SandboxedPluginInfo();
        info.PluginId.Should().BeEmpty();
        info.PluginName.Should().BeEmpty();
        info.Version.Should().BeEmpty();
        info.Configuration.Should().NotBeNull();
        info.MemoryUsage.Should().Be(0);
        info.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SandboxedPluginInfo_SetProperties_ShouldRetainValues()
    {
        var info = new SandboxedPluginInfo
        {
            PluginId = "p1",
            PluginName = "Plugin 1",
            Version = "1.0.0",
            MemoryUsage = 1024 * 1024,
            IsActive = true,
            LoadedAt = new DateTime(2024, 1, 1)
        };
        info.PluginId.Should().Be("p1");
        info.MemoryUsage.Should().Be(1024 * 1024);
        info.IsActive.Should().BeTrue();
    }

    #endregion

    #region SandboxOperationResult Defaults Tests

    [Fact]
    public void SandboxOperationResult_Defaults_ShouldHaveExpectedValues()
    {
        var result = new SandboxOperationResult();
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.WasBlocked.Should().BeFalse();
    }

    [Fact]
    public void SandboxOperationResult_SetProperties_ShouldRetainValues()
    {
        var result = new SandboxOperationResult
        {
            Success = true,
            Data = "result data",
            ErrorMessage = null,
            WasBlocked = false
        };
        result.Success.Should().BeTrue();
        result.Data.Should().Be("result data");
    }

    #endregion

    #region SandboxViolationEventArgs Tests

    [Fact]
    public void SandboxViolationEventArgs_Defaults_ShouldHaveExpectedValues()
    {
        var args = new SandboxViolationEventArgs();
        args.PluginId.Should().BeEmpty();
        args.ViolatedPermission.Should().Be(default);
        args.Description.Should().BeEmpty();
        args.StackTrace.Should().BeNull();
    }

    [Fact]
    public void SandboxViolationEventArgs_SetProperties_ShouldRetainValues()
    {
        var args = new SandboxViolationEventArgs
        {
            PluginId = "p1",
            ViolatedPermission = SandboxPermission.HardwareAccess,
            Description = "Unauthorized hardware access",
            StackTrace = "at Method()",
            Timestamp = new DateTime(2024, 6, 1)
        };
        args.PluginId.Should().Be("p1");
        args.ViolatedPermission.Should().Be(SandboxPermission.HardwareAccess);
        args.StackTrace.Should().Be("at Method()");
    }

    [Fact]
    public void SandboxViolationEventArgs_ShouldBeEventArgs()
    {
        new SandboxViolationEventArgs().Should().BeAssignableTo<EventArgs>();
    }

    #endregion

    #region ResourceLimitExceededEventArgs Tests

    [Fact]
    public void ResourceLimitExceededEventArgs_Defaults_ShouldHaveExpectedValues()
    {
        var args = new ResourceLimitExceededEventArgs();
        args.PluginId.Should().BeEmpty();
        args.ResourceType.Should().Be(default);
        args.CurrentUsage.Should().Be(0);
        args.MaximumAllowed.Should().Be(0);
    }

    [Fact]
    public void ResourceLimitExceededEventArgs_SetProperties_ShouldRetainValues()
    {
        var args = new ResourceLimitExceededEventArgs
        {
            PluginId = "p1",
            ResourceType = ResourceType.Memory,
            CurrentUsage = 200,
            MaximumAllowed = 100
        };
        args.ResourceType.Should().Be(ResourceType.Memory);
        args.CurrentUsage.Should().Be(200);
    }

    [Fact]
    public void ResourceLimitExceededEventArgs_ShouldBeEventArgs()
    {
        new ResourceLimitExceededEventArgs().Should().BeAssignableTo<EventArgs>();
    }

    #endregion

    #region SandboxResourceUsage Defaults Tests

    [Fact]
    public void SandboxResourceUsage_Defaults_ShouldHaveExpectedValues()
    {
        var usage = new SandboxResourceUsage();
        usage.MemoryUsageBytes.Should().Be(0);
        usage.PeakMemoryUsageBytes.Should().Be(0);
        usage.CpuUsagePercentage.Should().Be(0);
        usage.FileSystemOperationCount.Should().Be(0);
        usage.NetworkOperationCount.Should().Be(0);
        usage.AverageOperationTimeMs.Should().Be(0);
        usage.ViolationCount.Should().Be(0);
        usage.TotalRunningTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void SandboxResourceUsage_SetProperties_ShouldRetainValues()
    {
        var usage = new SandboxResourceUsage
        {
            MemoryUsageBytes = 1024 * 1024,
            PeakMemoryUsageBytes = 2 * 1024 * 1024,
            CpuUsagePercentage = 25.5,
            FileSystemOperationCount = 42,
            NetworkOperationCount = 7,
            AverageOperationTimeMs = 15.3,
            ViolationCount = 1,
            TotalRunningTime = TimeSpan.FromMinutes(5)
        };
        usage.MemoryUsageBytes.Should().Be(1024 * 1024);
        usage.CpuUsagePercentage.Should().Be(25.5);
        usage.TotalRunningTime.Should().Be(TimeSpan.FromMinutes(5));
    }

    #endregion
}