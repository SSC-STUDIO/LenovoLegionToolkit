namespace UniversalDeviceToolkit.Abstractions.Ipc;

/// <summary>
/// Abstraction for bidirectional inter-process communication.
/// Implementations may use named pipes, Unix domain sockets, or other transports.
/// </summary>
public interface IIpcTransport : IDisposable
{
    /// <summary>
    /// Waits for and accepts an incoming connection from a remote endpoint.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A <see cref="Stream"/> representing the accepted connection.</returns>
    Task<Stream> AcceptConnectionAsync(CancellationToken token);

    /// <summary>
    /// Connects to a remote endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint identifier (pipe name, socket path, etc.).</param>
    /// <param name="timeout">Maximum time to wait for the connection.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A <see cref="Stream"/> representing the established connection.</returns>
    Task<Stream> ConnectAsync(string endpoint, TimeSpan timeout, CancellationToken token);
}
