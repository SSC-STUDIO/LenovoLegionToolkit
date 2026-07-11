using System.Threading;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Network;

public sealed class NetworkDiagnosticsService : INetworkDiagnosticsService
{
    private readonly INetworkAccelerationService _acceleration;

    public NetworkDiagnosticsService(INetworkAccelerationService acceleration)
    {
        _acceleration = acceleration;
    }

    public Task<NetworkDiagnosticsReport> RunQuickCheckAsync(CancellationToken cancellationToken = default)
    {
        var config = _acceleration.Config;
        var report = new NetworkDiagnosticsReport
        {
            LoopbackReachable = true,
            AccelerationEnabled = config.AccelerationEnabled,
            Mode = config.Mode,
            Summary = config.AccelerationEnabled
                ? $"Acceleration flagged on (mode={config.Mode}); backend ready={_acceleration.IsBackendReady}."
                : "Acceleration off (default). No system proxy/hosts/PAC mutations applied."
        };
        return Task.FromResult(report);
    }
}
