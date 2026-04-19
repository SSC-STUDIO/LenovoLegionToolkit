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

        var adminRules = rules.Where(rule =>
            rule.AccessControlType == AccessControlType.Allow &&
            rule.IdentityReference is SecurityIdentifier sid &&
            sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid) &&
            rule.PipeAccessRights.HasFlag(PipeAccessRights.ReadWrite));

        adminRules.Should().ContainSingle();
        rules.Should().NotContain(rule =>
            rule.AccessControlType == AccessControlType.Deny &&
            rule.IdentityReference is SecurityIdentifier &&
            ((SecurityIdentifier)rule.IdentityReference).IsWellKnown(WellKnownSidType.WorldSid));
    }
}
