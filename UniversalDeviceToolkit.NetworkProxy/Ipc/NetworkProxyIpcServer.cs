using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using LenovoLegionToolkit.Lib.Network;
using UniversalDeviceToolkit.NetworkProxy.Host;

namespace UniversalDeviceToolkit.NetworkProxy.Ipc;

/// <summary>
/// Named-pipe IPC for start/stop/status/rules. Current-user (+ Administrators) ACL only.
/// Session token must match on every request.
/// </summary>
public sealed class NetworkProxyIpcServer : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly string _pipeName;
    private readonly string _sessionToken;
    private readonly INetworkProxyHost _host;
    private string _rulesJson = "[]";

    public NetworkProxyIpcServer(string pipeName, string sessionToken, INetworkProxyHost host)
    {
        _pipeName = string.IsNullOrWhiteSpace(pipeName)
            ? NetworkAccelerationDefaults.DefaultPipeName
            : pipeName.Trim();
        _sessionToken = sessionToken;
        _host = host ?? throw new ArgumentNullException(nameof(host));

        if (!NetworkProxySessionToken.IsValidFormat(_sessionToken))
            throw new ArgumentException("Session token must be a non-empty random token.", nameof(sessionToken));
    }

    /// <summary>
    /// Resolves the session token: env <see cref="NetworkProxySessionToken.WorkerTokenEnvironmentVariable"/>
    /// first (preferred; not on command line), then <c>--token</c> for backward compatibility.
    /// Clears the env var after a successful env read so it does not linger in the process.
    /// </summary>
    public static string ResolveSessionToken(string[] args)
    {
        var fromEnv = Environment.GetEnvironmentVariable(NetworkProxySessionToken.WorkerTokenEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            // Best-effort clear so the secret is not left in the worker environment table.
            try { Environment.SetEnvironmentVariable(NetworkProxySessionToken.WorkerTokenEnvironmentVariable, null); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"NetworkProxy: failed to clear {NetworkProxySessionToken.WorkerTokenEnvironmentVariable}: {ex.GetType().Name}");
            }

            return fromEnv.Trim();
        }

        // Backward compat: older launchers passed --token on argv.
        var fromArgs = ReadArg(args, "--token");
        return string.IsNullOrWhiteSpace(fromArgs)
            ? NetworkProxySessionToken.Create()
            : fromArgs.Trim();
    }

    public static string ResolvePipeName(string[] args)
    {
        var fromArgs = ReadArg(args, "--pipe");
        return string.IsNullOrWhiteSpace(fromArgs)
            ? NetworkAccelerationDefaults.DefaultPipeName
            : fromArgs.Trim();
    }

    public static int ResolveListenPort(string[] args)
    {
        var raw = ReadArg(args, "--port");
        if (int.TryParse(raw, out var port) && port is > 0 and <= 65535)
            return port;
        return NetworkAccelerationDefaults.DefaultListenPort;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var pipe = CreatePipeServerStream();
            try
            {
                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                await HandleClientAsync(pipe, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException)
            {
                // Client disconnected mid-request — keep serving.
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"IPC client error: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(PipeStream pipe, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        await using var writer = new StreamWriter(pipe, Encoding.UTF8, bufferSize: 4096, leaveOpen: true) { AutoFlush = true };

        var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(line))
        {
            await WriteResponseAsync(writer, NetworkProxyIpcResponse.Fail("empty request")).ConfigureAwait(false);
            return;
        }

        NetworkProxyIpcRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<NetworkProxyIpcRequest>(line, JsonOptions);
        }
        catch (JsonException)
        {
            await WriteResponseAsync(writer, NetworkProxyIpcResponse.Fail("invalid json")).ConfigureAwait(false);
            return;
        }

        if (request is null)
        {
            await WriteResponseAsync(writer, NetworkProxyIpcResponse.Fail("null request")).ConfigureAwait(false);
            return;
        }

        if (!NetworkProxySessionToken.Matches(request.Token, _sessionToken))
        {
            await WriteResponseAsync(writer, NetworkProxyIpcResponse.Fail("unauthorized")).ConfigureAwait(false);
            return;
        }

        var response = await DispatchAsync(request, cancellationToken).ConfigureAwait(false);
        await WriteResponseAsync(writer, response).ConfigureAwait(false);
    }

    private async Task<NetworkProxyIpcResponse> DispatchAsync(NetworkProxyIpcRequest request, CancellationToken cancellationToken)
    {
        switch (request.Operation?.Trim().ToLowerInvariant())
        {
            case "status":
                return NetworkProxyIpcResponse.Ok(_host.StatusSummary, new Dictionary<string, string>
                {
                    ["running"] = _host.IsRunning ? "true" : "false",
                    ["port"] = _host.ListenPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["bind"] = "127.0.0.1/::1"
                });

            case "start":
                await _host.StartAsync(cancellationToken).ConfigureAwait(false);
                return NetworkProxyIpcResponse.Ok(_host.StatusSummary);

            case "stop":
                await _host.StopAsync().ConfigureAwait(false);
                return NetworkProxyIpcResponse.Ok(_host.StatusSummary);

            case "rules":
                if (!string.IsNullOrWhiteSpace(request.Payload))
                {
                    string[] domains;
                    try
                    {
                        domains = JsonSerializer.Deserialize<string[]>(request.Payload, JsonOptions) ?? [];
                    }
                    catch (JsonException)
                    {
                        return NetworkProxyIpcResponse.Fail("invalid rules json (expected string array)");
                    }

                    _rulesJson = request.Payload!;
                    // Apply allowlist to the live host (empty = allow all).
                    _host.SetDomainAllowlist(domains);
                }

                return NetworkProxyIpcResponse.Ok("rules", new Dictionary<string, string>
                {
                    ["rules"] = _rulesJson
                });

            case "shutdown":
                _ = Task.Run(async () =>
                {
                    await Task.Delay(50, CancellationToken.None).ConfigureAwait(false);
                    Environment.Exit(0);
                });
                return NetworkProxyIpcResponse.Ok("shutting down");

            default:
                return NetworkProxyIpcResponse.Fail($"unknown operation: {request.Operation}");
        }
    }

    private static Task WriteResponseAsync(StreamWriter writer, NetworkProxyIpcResponse response)
    {
        var json = JsonSerializer.Serialize(response, JsonOptions);
        return writer.WriteLineAsync(json);
    }

    private NamedPipeServerStream CreatePipeServerStream()
    {
        var security = CreatePipeSecurity();
        return NamedPipeServerStreamAcl.Create(
            _pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous,
            0,
            0,
            security);
    }

    private static PipeSecurity CreatePipeSecurity()
    {
        var security = new PipeSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        var adminIdentity = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        security.AddAccessRule(new PipeAccessRule(adminIdentity, PipeAccessRights.ReadWrite, AccessControlType.Allow));

        if (WindowsIdentity.GetCurrent().User is { } currentUser)
            security.AddAccessRule(new PipeAccessRule(currentUser, PipeAccessRights.ReadWrite, AccessControlType.Allow));

        return security;
    }

    private static string? ReadArg(string[] args, string key)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var value = args[i];
            if (value.Equals(key, StringComparison.OrdinalIgnoreCase))
                return i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                    ? args[i + 1]
                    : null;

            if (value.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                return value[(key.Length + 1)..];
        }

        return null;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class NetworkProxyIpcRequest
{
    public string? Operation { get; set; }
    public string? Token { get; set; }
    public string? Payload { get; set; }
}

public sealed class NetworkProxyIpcResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, string>? Data { get; set; }

    public static NetworkProxyIpcResponse Ok(string message, Dictionary<string, string>? data = null) =>
        new() { Success = true, Message = message, Data = data };

    public static NetworkProxyIpcResponse Fail(string message) =>
        new() { Success = false, Message = message };
}
