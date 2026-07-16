using FluentAssertions;
using UniversalDeviceToolkit.Lib.Network;
using UniversalDeviceToolkit.Lib.Utils;
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

    [Fact]
    public void CreateDefault_IncludesBuiltinDomainGroupsDisabled()
    {
        var config = NetworkAccelerationConfig.CreateDefault();
        config.DomainGroups.Should().NotBeEmpty();
        config.DomainGroups.Should().Contain(g => g.Id == "steam");
        config.DomainGroups.Should().Contain(g => g.Id == "github");
        config.DomainGroups.Should().OnlyContain(g => !g.Enabled);
        config.DomainGroups.Should().OnlyContain(g => !g.IsFavorite);
        config.DomainGroups.First(g => g.Id == "steam").Domains.Should().Contain("steamcommunity.com");
        config.DomainGroups.First(g => g.Id == "github").Domains.Should().Contain("github.com");
    }

    [Fact]
    public void NetworkDomainGroup_IsFavorite_DefaultsFalseAndIsSettable()
    {
        var group = new NetworkDomainGroup { Id = "steam", DisplayName = "Steam" };
        group.IsFavorite.Should().BeFalse();
        group.IsFavorite = true;
        group.IsFavorite.Should().BeTrue();
    }
}

public class DomainMatcherTests
{
    [Theory]
    [InlineData("github.com", "github.com", true)]
    [InlineData("api.github.com", "github.com", true)]
    [InlineData("notgithub.com", "github.com", false)]
    [InlineData("steamcommunity.com", "steampowered.com", false)]
    [InlineData("cdn.steamstatic.com", "steamstatic.com", true)]
    public void Matches_HostAgainstRule(string host, string rule, bool expected)
    {
        DomainMatcher.Matches(host, rule).Should().Be(expected);
    }

    [Fact]
    public void MatchesAny_WhenOneRuleMatches()
    {
        DomainMatcher.MatchesAny("api.github.com", ["steamcommunity.com", "github.com"]).Should().BeTrue();
        DomainMatcher.MatchesAny("example.com", ["github.com", "steamcommunity.com"]).Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_EmptyOrNullAllowlist_DeniesAll()
    {
        // Fail closed: empty/null allowlist must not open the loopback proxy to all hosts.
        DomainMatcher.IsAllowed("evil.example", null).Should().BeFalse();
        DomainMatcher.IsAllowed("evil.example", Array.Empty<string>()).Should().BeFalse();
        DomainMatcher.IsAllowed("evil.example", []).Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_NonEmptyAllowlist_EnforcesSuffixMatch()
    {
        string[] rules = ["github.com", "steamcommunity.com"];
        DomainMatcher.IsAllowed("api.github.com", rules).Should().BeTrue();
        DomainMatcher.IsAllowed("github.com", rules).Should().BeTrue();
        DomainMatcher.IsAllowed("notgithub.com", rules).Should().BeFalse();
        DomainMatcher.IsAllowed("example.com", rules).Should().BeFalse();
        DomainMatcher.IsAllowed(null, rules).Should().BeFalse();
        DomainMatcher.IsAllowed("  ", rules).Should().BeFalse();
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

    [Fact]
    public async Task CaptureAndRestore_SystemProxy_RoundTrips()
    {
        var dir = Path.Combine(Path.GetTempPath(), "udt-net-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        try
        {
            var hosts = "127.0.0.1 localhost\n# other\n";
            SystemProxySnapshot? proxy = new SystemProxySnapshot
            {
                Enabled = true,
                Server = "proxy.example:8080",
                Override = "localhost",
                AutoConfigUrl = null
            };

            var service = new NetworkStateRecoveryService(
                dir,
                () => hosts,
                content => hosts = content,
                () => proxy,
                value => proxy = value);

            var snapshot = await service.CaptureSnapshotAsync();
            snapshot.SystemProxy.Should().NotBeNull();
            snapshot.SystemProxy!.Server.Should().Be("proxy.example:8080");

            // Simulate UDT applying loopback proxy
            proxy = new SystemProxySnapshot
            {
                Enabled = true,
                Server = "127.0.0.1:34123",
                Override = "localhost",
                AutoConfigUrl = null
            };

            // Also inject a UDT hosts block as if acceleration was running
            hosts = HostsMarkedBlock.Upsert(hosts, ["127.0.0.1 steamcommunity.com"]);

            service.TryRestoreFromSnapshot(out var report).Should().BeTrue();
            report.Should().Contain("Result: OK");
            proxy!.Server.Should().Be("proxy.example:8080");
            proxy.Enabled.Should().BeTrue();
            hosts.Should().NotContain(HostsMarkedBlock.BeginMarker);
            hosts.Should().Contain("127.0.0.1 localhost");
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }
}

public class PacDomainMatchingIntegrationTests
{
    [Fact]
    public void Generate_WithSteamDomains_MatchesExpectedHostsInPac()
    {
        var steam = BuiltinDomainGroups.CreateDefaults().First(g => g.Id == "steam");
        var pac = PacFileGenerator.Generate(34123, steam.Domains);
        pac.Should().Contain("steamcommunity.com");
        DomainMatcher.Matches("store.steampowered.com", "steampowered.com").Should().BeTrue();
    }
}

public class NetworkAccelerationStartSafetyTests
{
    [Fact]
    public void CanApplySystemProxy_WhenNullOrEmpty_IsFalse()
    {
        NetworkAccelerationService.CanApplySystemProxy(null).Should().BeFalse();
        NetworkAccelerationService.CanApplySystemProxy([]).Should().BeFalse();
        NetworkAccelerationService.CanApplySystemProxy(["", "  "]).Should().BeFalse();
    }

    [Fact]
    public void CanApplySystemProxy_WhenAnyNonEmptyDomain_IsTrue()
    {
        NetworkAccelerationService.CanApplySystemProxy(["steamcommunity.com"]).Should().BeTrue();
        NetworkAccelerationService.CanApplySystemProxy(["", "github.com"]).Should().BeTrue();
    }

    [Fact]
    public void CanStartMode_Hosts_IsAlwaysRefused()
    {
        NetworkAccelerationService.CanStartMode(NetworkAccelerationMode.Hosts, null).Should().BeFalse();
        NetworkAccelerationService.CanStartMode(NetworkAccelerationMode.Hosts, []).Should().BeFalse();
        NetworkAccelerationService.CanStartMode(NetworkAccelerationMode.Hosts, ["example.com"]).Should().BeFalse();
    }

    [Fact]
    public void CanStartMode_SystemProxy_RequiresEnabledDomains()
    {
        NetworkAccelerationService.CanStartMode(NetworkAccelerationMode.SystemProxy, null).Should().BeFalse();
        NetworkAccelerationService.CanStartMode(NetworkAccelerationMode.SystemProxy, []).Should().BeFalse();
        NetworkAccelerationService.CanStartMode(NetworkAccelerationMode.SystemProxy, ["steamcommunity.com"]).Should().BeTrue();
    }

    [Fact]
    public void CanStartMode_DiagnosticsOnly_IsAlwaysAllowed()
    {
        NetworkAccelerationService.CanStartMode(NetworkAccelerationMode.DiagnosticsOnly, null).Should().BeTrue();
        NetworkAccelerationService.CanStartMode(NetworkAccelerationMode.DiagnosticsOnly, []).Should().BeTrue();
    }

    [Fact]
    public void CanStartMode_Off_IsRefused()
    {
        NetworkAccelerationService.CanStartMode(NetworkAccelerationMode.Off, ["x.com"]).Should().BeFalse();
    }

    [Fact]
    public void CreateDefault_StillDisabledAndNeverImpliesRunning()
    {
        var config = NetworkAccelerationConfig.CreateDefault();
        config.AccelerationEnabled.Should().BeFalse();
        config.Mode.Should().Be(NetworkAccelerationMode.Off);
        NetworkAccelerationService.CanStartMode(config.Mode, config.DomainGroups.SelectMany(g => g.Domains)).Should().BeFalse();
    }
}
