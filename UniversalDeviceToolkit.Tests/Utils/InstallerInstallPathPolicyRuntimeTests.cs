using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Security)]
[Trait("Category", TestCategories.Unit)]
public sealed class InstallerInstallPathPolicyRuntimeTests
{
    [Fact]
    public void IsUnderProgramFiles_ShouldEnforceDirectoryBoundary()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        programFiles.Should().NotBeNullOrWhiteSpace();

        InvokeIsUnderProgramFiles(programFiles).Should().BeFalse();
        InvokeIsUnderProgramFiles(Path.Combine(programFiles, "UniversalDeviceToolkit")).Should().BeTrue();

        var programFilesParent = Directory.GetParent(programFiles)?.FullName;
        programFilesParent.Should().NotBeNullOrWhiteSpace();
        var sibling = Path.Combine(programFilesParent!, Path.GetFileName(programFiles) + "-sibling");
        InvokeIsUnderProgramFiles(sibling).Should().BeFalse();
        InvokeIsUnderProgramFiles(Path.Combine(Path.GetTempPath(), "UniversalDeviceToolkit")).Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldRejectPathsOutsideProgramFiles()
    {
        var policyType = LoadPolicyType();
        var validate = policyType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
        validate.Should().NotBeNull();

        var outsidePath = Path.Combine(Path.GetTempPath(), "UniversalDeviceToolkit-policy-test");
        var invocation = Assert.Throws<TargetInvocationException>(() => validate!.Invoke(null, [outsidePath]));
        invocation.InnerException.Should().BeOfType<UnauthorizedAccessException>();
    }

    private static bool InvokeIsUnderProgramFiles(string path)
    {
        var policyType = LoadPolicyType();
        var method = policyType.GetMethod("IsUnderProgramFiles", BindingFlags.Public | BindingFlags.Static);
        method.Should().NotBeNull();
        return (bool)method!.Invoke(null, [path])!;
    }

    private static Type LoadPolicyType()
    {
        var installerPath = Path.Combine(AppContext.BaseDirectory, "UniversalDeviceToolkit.Installer.dll");
        File.Exists(installerPath).Should().BeTrue("the runtime policy test must load the built installer assembly");

        var assembly = Assembly.LoadFrom(installerPath);
        return assembly.GetType("UniversalDeviceToolkit.Installer.InstallerInstallPathPolicy", throwOnError: true)!;
    }
}
