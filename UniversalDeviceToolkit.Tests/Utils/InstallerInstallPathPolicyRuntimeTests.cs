using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
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

    [SkippableFact]
    public void PrepareForInstall_ShouldProtectNestedPayloadAcl()
    {
        var isAdministrator = new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);
        Skip.If(!isAdministrator, "Program Files ACL audit requires an elevated administrator token");

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var auditRoot = Path.Combine(
            programFiles,
            $"UniversalDeviceToolkit-AclAudit-{Guid.NewGuid():N}");
        var nestedDirectory = Path.Combine(auditRoot, "payload");
        var payloadFile = Path.Combine(nestedDirectory, "payload.dll");

        try
        {
            Directory.CreateDirectory(nestedDirectory);
            File.WriteAllText(payloadFile, "acl-audit");

            var policyType = LoadPolicyType();
            var prepare = policyType.GetMethod("PrepareForInstall", BindingFlags.Public | BindingFlags.Static);
            prepare.Should().NotBeNull();
            prepare!.Invoke(null, [auditRoot]);

            AssertProtectedReadOnlyAcl(auditRoot, isDirectory: true);
            AssertProtectedReadOnlyAcl(nestedDirectory, isDirectory: true);
            AssertProtectedReadOnlyAcl(payloadFile, isDirectory: false);
        }
        finally
        {
            if (Directory.Exists(auditRoot))
                Directory.Delete(auditRoot, recursive: true);
        }
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

    private static void AssertProtectedReadOnlyAcl(string path, bool isDirectory)
    {
        AuthorizationRuleCollection accessRules;
        bool areAccessRulesProtected;
        if (isDirectory)
        {
            var security = new DirectoryInfo(path).GetAccessControl();
            accessRules = security.GetAccessRules(
                includeExplicit: true,
                includeInherited: true,
                targetType: typeof(SecurityIdentifier));
            areAccessRulesProtected = security.AreAccessRulesProtected;
        }
        else
        {
            var security = new FileInfo(path).GetAccessControl();
            accessRules = security.GetAccessRules(
                includeExplicit: true,
                includeInherited: true,
                targetType: typeof(SecurityIdentifier));
            areAccessRulesProtected = security.AreAccessRulesProtected;
        }

        areAccessRulesProtected.Should().BeTrue(path);

        var usersSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, domainSid: null);
        var usersRules = accessRules
            .OfType<FileSystemAccessRule>()
            .Where(rule => rule.AccessControlType == AccessControlType.Allow
                           && rule.IdentityReference is SecurityIdentifier sid
                           && sid.Equals(usersSid))
            .ToArray();

        usersRules.Should().ContainSingle(path);
        foreach (var rule in usersRules)
        {
            rule.FileSystemRights.Should().HaveFlag(FileSystemRights.ReadAndExecute);
            var writeRights = FileSystemRights.Write
                              | FileSystemRights.Delete
                              | FileSystemRights.ChangePermissions
                              | FileSystemRights.TakeOwnership;
            ((int)(rule.FileSystemRights & writeRights)).Should().Be(0, path);
        }
    }
}
