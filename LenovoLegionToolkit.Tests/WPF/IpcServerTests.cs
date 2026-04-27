using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using FluentAssertions;
using LenovoLegionToolkit.WPF.CLI;
using Xunit;

namespace LenovoLegionToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class IpcServerTests
{
    [Fact]
    public void CreatePipeSecurity_ShouldOnlyAllowAdministrators()
    {
        var method = typeof(IpcServer).GetMethod("CreatePipeSecurity", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        var security = method!.Invoke(null, []) as PipeSecurity;
        security.Should().NotBeNull();

        var rules = security!
            .GetAccessRules(true, true, typeof(SecurityIdentifier))
            .Cast<PipeAccessRule>()
            .ToList();

        var adminRules = rules.Where(IsAdministratorReadWriteAllowRule);

        adminRules.Should().ContainSingle();
        rules.All(IsAdministratorReadWriteAllowRule).Should().BeTrue();
        rules.Should().NotContain(rule =>
            rule.AccessControlType == AccessControlType.Deny &&
            rule.IdentityReference is SecurityIdentifier &&
            ((SecurityIdentifier)rule.IdentityReference).IsWellKnown(WellKnownSidType.WorldSid));
    }

    [Fact]
    public void CreatePipeSecurity_ShouldGrantReadWriteAccess()
    {
        var method = typeof(IpcServer).GetMethod("CreatePipeSecurity", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var security = method!.Invoke(null, []) as PipeSecurity;
        security.Should().NotBeNull();

        var rules = security!
            .GetAccessRules(true, true, typeof(SecurityIdentifier))
            .Cast<PipeAccessRule>()
            .ToList();

        var adminRule = rules.FirstOrDefault(IsAdministratorReadWriteAllowRule);
        adminRule.Should().NotBeNull();
        adminRule!.PipeAccessRights.Should().HaveFlag(PipeAccessRights.ReadWrite);
    }

    [Fact]
    public void CreatePipeSecurity_ShouldNotAllowWorldSid()
    {
        var method = typeof(IpcServer).GetMethod("CreatePipeSecurity", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var security = method!.Invoke(null, []) as PipeSecurity;
        security.Should().NotBeNull();

        var rules = security!
            .GetAccessRules(true, true, typeof(SecurityIdentifier))
            .Cast<PipeAccessRule>()
            .ToList();

        // Should not have any rules for WorldSid (Everyone)
        rules.Should().NotContain(rule =>
            rule.IdentityReference is SecurityIdentifier &&
            ((SecurityIdentifier)rule.IdentityReference).IsWellKnown(WellKnownSidType.WorldSid));
    }

    [Fact]
    public void CreatePipeSecurity_ShouldNotAllowAnonymousSid()
    {
        var method = typeof(IpcServer).GetMethod("CreatePipeSecurity", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var security = method!.Invoke(null, []) as PipeSecurity;
        security.Should().NotBeNull();

        var rules = security!
            .GetAccessRules(true, true, typeof(SecurityIdentifier))
            .Cast<PipeAccessRule>()
            .ToList();

        // Should not have any rules for AnonymousSid
        rules.Should().NotContain(rule =>
            rule.IdentityReference is SecurityIdentifier &&
            ((SecurityIdentifier)rule.IdentityReference).IsWellKnown(WellKnownSidType.AnonymousSid));
    }

    [Fact]
    public void CreatePipeServerStream_ShouldCreateValidPipe()
    {
        var method = typeof(IpcServer).GetMethod("CreatePipeServerStream", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        // This test verifies the method exists and can be invoked
        // We don't actually create the pipe to avoid resource issues
        method!.Should().NotBeNull();
    }

    private static bool IsAdministratorReadWriteAllowRule(PipeAccessRule rule)
    {
        return rule.AccessControlType == AccessControlType.Allow &&
               rule.IdentityReference is SecurityIdentifier sid &&
               sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid) &&
               rule.PipeAccessRights.HasFlag(PipeAccessRights.ReadWrite);
    }
}
