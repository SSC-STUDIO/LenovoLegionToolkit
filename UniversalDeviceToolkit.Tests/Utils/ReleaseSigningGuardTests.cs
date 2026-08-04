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
        var workflow = GitHubWorkflowContract.Parse(
            File.ReadAllText(Path.Combine(root, ".github", "workflows", "Release.yml")));
        var script = File.ReadAllText(Path.Combine(root, "Scripts", "Assert-AuthenticodeSignatures.ps1"));

        var job = workflow.Job("build");
        var payloadSigning = job.Step("Sign release payload");
        var payloadVerification = job.Step("Verify release payload signatures");
        var installerSigning = job.Step("Sign installers");
        var installerVerification = job.Step("Verify installer signatures");

        payloadSigning.Uses.Should().Be("azure/trusted-signing-action@v0.5");
        installerSigning.Uses.Should().Be("azure/trusted-signing-action@v0.5");
        payloadSigning.WithValue("files-folder").Should().Be("${{ env.BUILD_OUTPUT }}");
        payloadSigning.WithValue("files-folder-filter").Should().Be("exe,dll");
        installerSigning.WithValue("files-folder").Should().Be("${{ env.INSTALLER_OUTPUT }}");
        installerSigning.WithValue("files-folder-filter").Should().Be("exe");
        payloadVerification.Run.Should().Contain("Assert-AuthenticodeSignatures.ps1");
        installerVerification.Run.Should().Contain("Assert-AuthenticodeSignatures.ps1");
        script.Should().Contain("Get-AuthenticodeSignature");
        script.Should().Contain("Status -ne 'Valid'");
    }
}
