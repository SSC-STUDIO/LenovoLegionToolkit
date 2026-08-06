using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Guard)]
public sealed class AvaloniaSingleInstanceContractTests
{
    [Fact]
    public void AvaloniaHost_ShouldShareWpfSingleInstanceIdentityAndActivationContract()
    {
        var root = RepositoryPaths.FindRoot();
        var guard = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Startup",
            "AvaloniaSingleInstanceGuard.cs"));
        var app = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "App.axaml.cs"));

        guard.Should().Contain("AppIdentity.CompactName + \"_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98\"");
        guard.Should().Contain("AppIdentity.LegacyCompactName + \"_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98\"");
        guard.Should().Contain("SignalAndAwaitActivation");
        guard.Should().Contain("RecoverySuffix");
        guard.Should().Contain("UDT_TEST_HOOKS");
        guard.Should().Contain("Folders.AppDataOverrideEnvironmentVariable");
        guard.Should().Contain("StartListener");
        app.Should().Contain("AcquireSingleInstance()");
        app.Should().Contain("_singleInstanceGuard!.StartListener");
        app.Should().Contain("_singleInstanceGuard?.Dispose();");
    }
}
