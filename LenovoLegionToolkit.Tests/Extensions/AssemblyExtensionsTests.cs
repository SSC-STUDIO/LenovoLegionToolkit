using System;
using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Extensions;
using Xunit;

namespace LenovoLegionToolkit.Tests.Extensions;

[Trait("Category", TestCategories.Unit)]
public class AssemblyExtensionsTests
{
    [Fact]
    public void GetBuildDateTime_WithCurrentAssembly_ShouldReturnNonNullForDebugBuild()
    {
        // The test assembly itself has an AssemblyInformationalVersionAttribute
        // (from the SDK-generated version). In debug builds it may or may not have +build metadata.
        // This test verifies the method doesn't throw regardless of the assembly.
        var assembly = typeof(AssemblyExtensionsTests).Assembly;

        // Should not throw; result depends on build metadata
        var result = assembly.GetBuildDateTime();

        // In a debug/test build the informational version likely doesn't have +build prefix
        // so result is expected to be null, but the method should complete without error
        result.Should().BeNull("test assemblies typically lack +build metadata");
    }

    [Fact]
    public void GetBuildDateTimeString_WithCurrentAssembly_ShouldReturnNullWhenNoBuildMetadata()
    {
        var assembly = typeof(AssemblyExtensionsTests).Assembly;

        var result = assembly.GetBuildDateTimeString();

        result.Should().BeNull("test assemblies typically lack +build metadata");
    }

    [Fact]
    public void GetBuildDateTime_WithSyntheticInformationalVersion_ShouldParseCorrectly()
    {
        // Create a dynamic assembly with a known informational version to test the parsing
        var assembly = CreateAssemblyWithInformationalVersion("1.2.3+build20260512143000");

        var result = assembly.GetBuildDateTime();

        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2026);
        result.Value.Month.Should().Be(5);
        result.Value.Day.Should().Be(12);
        result.Value.Hour.Should().Be(14);
        result.Value.Minute.Should().Be(30);
        result.Value.Second.Should().Be(0);
    }

    [Fact]
    public void GetBuildDateTimeString_WithSyntheticInformationalVersion_ShouldReturnFormattedString()
    {
        var assembly = CreateAssemblyWithInformationalVersion("2.0.0+build20250101120000");

        var result = assembly.GetBuildDateTimeString();

        result.Should().Be("20250101120000");
    }

    [Fact]
    public void GetBuildDateTime_WithoutBuildPrefix_ShouldReturnNull()
    {
        var assembly = CreateAssemblyWithInformationalVersion("1.0.0");

        var result = assembly.GetBuildDateTime();

        result.Should().BeNull();
    }

    [Fact]
    public void GetBuildDateTime_WithInvalidDateFormat_ShouldReturnNull()
    {
        var assembly = CreateAssemblyWithInformationalVersion("1.0.0+build-notadate");

        var result = assembly.GetBuildDateTime();

        result.Should().BeNull();
    }

    [Fact]
    public void GetBuildDateTime_WithEmptyBuildSuffix_ShouldReturnNull()
    {
        var assembly = CreateAssemblyWithInformationalVersion("1.0.0+build");

        var result = assembly.GetBuildDateTime();

        result.Should().BeNull();
    }

    [Fact]
    public void GetBuildDateTime_PrefixIsCaseInsensitive()
    {
        var assembly = CreateAssemblyWithInformationalVersion("1.0.0+BUILD20260512143000");

        var result = assembly.GetBuildDateTime();

        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2026);
    }

    private static Assembly CreateAssemblyWithInformationalVersion(string informationalVersion)
    {
        var assemblyName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("TestModule");

        // Apply the informational version attribute
        var attrCtor = typeof(AssemblyInformationalVersionAttribute)
            .GetConstructor(new[] { typeof(string) })!;
        var attrBuilder = new CustomAttributeBuilder(attrCtor, new object[] { informationalVersion });
        assemblyBuilder.SetCustomAttribute(attrBuilder);

        return assemblyBuilder;
    }
}
