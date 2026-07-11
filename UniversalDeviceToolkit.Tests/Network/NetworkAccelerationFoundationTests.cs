using FluentAssertions;
using LenovoLegionToolkit.Lib.Network;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Network;

public class HostsMarkedBlockTests
{
    [Fact]
    public void TryExtract_WhenMarkersPresent_ReturnsBody()
    {
        var hosts = """
            127.0.0.1 localhost

            # BEGIN UDT-NETWORK-ACCELERATION
            127.0.0.1 example.test
            # END UDT-NETWORK-ACCELERATION

            # other
            """;

        HostsMarkedBlock.TryExtract(hosts, out var body).Should().BeTrue();
        body.Should().Be("127.0.0.1 example.test");
    }

    [Fact]
    public void Upsert_WhenMissing_AppendsBlock()
    {
        var result = HostsMarkedBlock.Upsert("127.0.0.1 localhost", ["127.0.0.1 a.test"]);
        result.Should().Contain(HostsMarkedBlock.BeginMarker);
        result.Should().Contain("127.0.0.1 a.test");
        result.Should().Contain(HostsMarkedBlock.EndMarker);
        result.Should().Contain("127.0.0.1 localhost");
    }

    [Fact]
    public void Upsert_WhenPresent_ReplacesOnlyMarkedBlock()
    {
        var original = """
            keep-me
            # BEGIN UDT-NETWORK-ACCELERATION
            127.0.0.1 old.test
            # END UDT-NETWORK-ACCELERATION
            tail
            """;

        var result = HostsMarkedBlock.Upsert(original, ["127.0.0.1 new.test"]);
        result.Should().Contain("keep-me");
        result.Should().Contain("tail");
        result.Should().Contain("127.0.0.1 new.test");
        result.Should().NotContain("old.test");
    }

    [Fact]
    public void Remove_WhenPresent_RemovesOnlyMarkedBlock()
    {
        var original = """
            keep
            # BEGIN UDT-NETWORK-ACCELERATION
            127.0.0.1 x
            # END UDT-NETWORK-ACCELERATION
            """;

        var result = HostsMarkedBlock.Remove(original);
        result.Should().Contain("keep");
        result.Should().NotContain(HostsMarkedBlock.BeginMarker);
        result.Should().NotContain("127.0.0.1 x");
    }
}

public class PacFileGeneratorTests
{
    [Fact]
    public void Generate_WithoutDomains_ReturnsDirectOnly()
    {
        var pac = PacFileGenerator.Generate(34123);
        pac.Should().Contain("FindProxyForURL");
        pac.Should().Contain("return \"DIRECT\"");
        pac.Should().NotContain("PROXY 127.0.0.1");
    }

    [Fact]
    public void Generate_WithDomains_IncludesLoopbackProxy()
    {
        var pac = PacFileGenerator.Generate(34123, ["steamcommunity.com"]);
        pac.Should().Contain("steamcommunity.com");
        pac.Should().Contain("PROXY 127.0.0.1:34123");
        pac.Should().Contain("PROXY [::1]:34123");
    }
}

public class NetworkAccelerationConfigDefaultsTests
{
    [Fact]
    public void CreateDefault_AccelerationIsOff()
    {
        var config = NetworkAccelerationConfig.CreateDefault();
        config.AccelerationEnabled.Should().BeFalse();
        config.Mode.Should().Be(NetworkAccelerationMode.Off);
        config.ListenPort.Should().Be(NetworkAccelerationDefaults.DefaultListenPort);
        config.ShowInNavigation.Should().BeTrue();
    }
}

public class NetworkProxySessionTokenTests
{
    [Fact]
    public void Create_ProducesValidToken()
    {
        var token = NetworkProxySessionToken.Create();
        NetworkProxySessionToken.IsValidFormat(token).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("has space in token!!")]
    public void IsValidFormat_RejectsInvalid(string? token)
    {
        NetworkProxySessionToken.IsValidFormat(token).Should().BeFalse();
    }

    [Fact]
    public void Matches_RequiresExactToken()
    {
        var token = NetworkProxySessionToken.Create();
        NetworkProxySessionToken.Matches(token, token).Should().BeTrue();
        NetworkProxySessionToken.Matches(token, NetworkProxySessionToken.Create()).Should().BeFalse();
        NetworkProxySessionToken.Matches("not-a-valid-token", token).Should().BeFalse();
    }
}

public class NetworkStateRecoveryServiceTests
{
    [Fact]
    public void TryRestoreFromSnapshot_WhenMissing_IsIdempotentSuccess()
    {
        var dir = Path.Combine(Path.GetTempPath(), "udt-net-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        try
        {
            var hosts = "127.0.0.1 localhost\n";
            SystemProxySnapshot? proxy = null;

            var service = new NetworkStateRecoveryService(
                dir,
                () => hosts,
                content => hosts = content,
                () => proxy,
                value => proxy = value);

            var ok = service.TryRestoreFromSnapshot(out var report);
            ok.Should().BeTrue();
            report.Should().Contain("idempotent");
            report.Should().Contain("Result: OK");
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void TryRestoreFromSnapshot_WhenEmptyFile_IsIdempotentSuccess()
    {
        var dir = Path.Combine(Path.GetTempPath(), "udt-net-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, NetworkAccelerationDefaults.SnapshotFileName), "   ");

        try
        {
            var service = new NetworkStateRecoveryService(
                dir,
                () => string.Empty,
                _ => { },
                () => null,
                _ => { });

            service.TryRestoreFromSnapshot(out var report).Should().BeTrue();
            report.Should().Contain("empty");
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void HardwareStateRecovery_ResetNetwork_WithEmptySnapshot_Succeeds()
    {
        // Uses real AppData path semantics via Folders — exercise TryResetNetwork path that
        // delegates to NetworkStateRecoveryService when no snapshot exists.
        var service = new HardwareStateRecoveryService(new HardwareStateRecoveryImplementation(
            _ => null,
            _ => { }));

        var ok = service.TryResetNetwork(out var report);
        ok.Should().BeTrue();
        report.Should().Contain("Network state");
    }
}
