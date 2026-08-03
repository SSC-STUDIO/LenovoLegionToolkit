using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class ReleaseSigningGuardTests
{
    [Fact]
    public void ReleaseWorkflow_ShouldSignAndVerifyPayloadAndInstallers()
    {
        var root = RepositoryPaths.FindRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "Release.yml"));
        var script = File.ReadAllText(Path.Combine(root, "Scripts", "Assert-AuthenticodeSignatures.ps1"));

        workflow.Should().Contain("azure/trusted-signing-action@");
        workflow.Should().Contain("Sign release payload");
        workflow.Should().Contain("Sign installers");
        workflow.Should().Contain("Assert-AuthenticodeSignatures.ps1");
        script.Should().Contain("Get-AuthenticodeSignature");
        script.Should().Contain("Status -ne 'Valid'");
    }
}
