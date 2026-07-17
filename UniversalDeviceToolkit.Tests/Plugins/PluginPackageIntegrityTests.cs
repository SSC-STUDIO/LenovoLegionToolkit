using System;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Security)]
[Trait("Category", TestCategories.Unit)]
public class PluginPackageIntegrityTests
{
    [Fact]
    public void TryVerifyExpectedHash_WhenExpectedMatchesActual_ShouldSucceed()
    {
        const string hash = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";

        var result = PluginPackageIntegrity.TryVerifyExpectedHash(hash, hash, requireWhenMissing: true, out var failureReason);

        result.Should().BeTrue();
        failureReason.Should().BeNull();
    }

    [Fact]
    public void TryVerifyExpectedHash_WhenExpectedMismatch_ShouldFail()
    {
        var result = PluginPackageIntegrity.TryVerifyExpectedHash(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            requireWhenMissing: false,
            out var failureReason);

        result.Should().BeFalse();
        failureReason.Should().Contain("mismatch");
    }

    [Fact]
    public void TryVerifyExpectedHash_WhenExpectedMissingAndRequired_ShouldFail()
    {
        var result = PluginPackageIntegrity.TryVerifyExpectedHash(
            expectedHash: null,
            actualHash: "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            requireWhenMissing: true,
            out var failureReason);

        result.Should().BeFalse();
        failureReason.Should().Be("integrity hash is missing");
    }

    [Fact]
    public void TryVerifyExpectedHash_WhenExpectedMissingAndNotRequired_ShouldSucceed()
    {
        var result = PluginPackageIntegrity.TryVerifyExpectedHash(
            expectedHash: string.Empty,
            actualHash: "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd",
            requireWhenMissing: false,
            out var failureReason);

        result.Should().BeTrue();
        failureReason.Should().BeNull();
    }

    [Fact]
    public void IsVerificationWaived_ShouldRespectEnvironmentVariable()
    {
        var original = Environment.GetEnvironmentVariable("LLT_PLUGIN_INTEGRITY_MODE");

        try
        {
            Environment.SetEnvironmentVariable("LLT_PLUGIN_INTEGRITY_MODE", "skip");
            PluginPackageIntegrity.IsVerificationWaived().Should().BeTrue();

            Environment.SetEnvironmentVariable("LLT_PLUGIN_INTEGRITY_MODE", "enforce");
            PluginPackageIntegrity.IsVerificationWaived().Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("LLT_PLUGIN_INTEGRITY_MODE", original);
        }
    }
}