using FluentAssertions;
using UniversalDeviceToolkit.Lib.Network;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Network;

[Trait("Category", TestCategories.Unit)]
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
