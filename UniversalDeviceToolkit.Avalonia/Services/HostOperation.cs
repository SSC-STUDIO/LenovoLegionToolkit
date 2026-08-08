namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Normalizes host-operation failures at the Avalonia boundary. Platform hosts
/// can reject a command with <c>false</c> or throw while their hardware/service
/// state is changing; pages must handle both forms as an actionable failure.
/// </summary>
internal static class HostOperation
{
    internal static async Task<bool> TryExecuteAsync(Func<Task<bool>> operation)
    {
        try
        {
            return await operation().ConfigureAwait(true);
        }
        catch
        {
            return false;
        }
    }
}
