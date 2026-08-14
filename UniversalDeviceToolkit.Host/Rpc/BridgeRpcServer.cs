using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.Host.Rpc;

/// <summary>
/// Newline-delimited JSON-RPC-ish protocol over the host process's stdio.
///
/// Requests (client -> host, one JSON object per line):
///   {"id":1,"method":"ping","params":{}}
/// Responses (host -> client):
///   {"id":1,"result":{...}}  |  {"id":1,"error":{"code":-32601,"message":"..."}}
/// Events (host -> client, no id):
///   {"event":"host.ready","data":{...}}
/// </summary>
public static class BridgeProtocol
{
    public const string EventProperty = "event";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Parses a single line into either a request or an event envelope.
    /// Returns null when the line is not a valid protocol message.
    /// </summary>
    public static bool TryParseRequest(string line, out BridgeRequest? request)
    {
        request = null;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return false;

        if (root.TryGetProperty(EventProperty, out _))
            return false;

        if (!root.TryGetProperty("method", out var method) || method.ValueKind != JsonValueKind.String)
            return false;

        var id = root.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number
            ? idProp.GetInt64()
            : (long?)null;

        // Clone the params element: it must outlive the parsing document.
        var parameters = root.TryGetProperty("params", out var paramsProp)
            ? paramsProp.Clone()
            : default;

        request = new BridgeRequest(id, method.GetString()!, parameters);
        return true;
    }

    public static byte[] WriteResponse(long? id, JsonElement? result, int? errorCode = null, string? errorMessage = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            if (id is not null)
                writer.WriteNumber("id", id.Value);
            else
                writer.WriteNull("id");

            if (errorCode is not null)
            {
                writer.WritePropertyName("error");
                writer.WriteStartObject();
                writer.WriteNumber("code", errorCode.Value);
                writer.WriteString("message", errorMessage ?? "Unknown error");
                writer.WriteEndObject();
            }
            else if (result is not null)
            {
                writer.WritePropertyName("result");
                result.Value.WriteTo(writer);
            }
            else
            {
                writer.WritePropertyName("result");
                writer.WriteNullValue();
            }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static byte[] WriteEvent(string name, object? data)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString(EventProperty, name);
            writer.WritePropertyName("data");
            if (data is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, data, data.GetType(), JsonOptions);
            }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static byte[] WriteResult(object? data)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            if (data is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, data, data.GetType(), JsonOptions);
            }
        }
        return stream.ToArray();
    }
}

public sealed class BridgeRequest
{
    public BridgeRequest(long? id, string method, JsonElement parameters)
    {
        Id = id;
        Method = method;
        Parameters = parameters;
    }

    public long? Id { get; }
    public string Method { get; }
    public JsonElement Parameters { get; }
}

/// <summary>
/// Result of dispatching a bridge request. Carries either a serializable result
/// or an error tuple.
/// </summary>
public sealed class BridgeResult
{
    private BridgeResult(object? value, int? errorCode, string? errorMessage)
    {
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public object? Value { get; }
    public int? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public bool IsError => ErrorCode is not null;

    public static BridgeResult Ok(object? value = null) => new(value, null, null);
    public static BridgeResult Error(int code, string message) => new(null, code, message);
}

/// <summary>
/// Reads requests from stdin, dispatches them to registered handlers and writes
/// responses/events to stdout. Single writer lock keeps each JSON line atomic.
/// </summary>
public sealed class BridgeRpcServer : IDisposable
{
    private readonly Dictionary<string, Func<BridgeRequest, CancellationToken, Task<BridgeResult>>> _handlers = new(StringComparer.Ordinal);
    private readonly object _writeLock = new();
    private readonly StreamReader _input;
    private readonly StreamWriter _output;
    private readonly CancellationTokenSource _cts = new();
    private int _disposed;

    /// <summary>Raised when the client closes the pipe (Electron exited).</summary>
    public event Action? ClientDisconnected;

    public BridgeRpcServer()
    {
        var stdin = Console.OpenStandardInput();
        var stdout = Console.OpenStandardOutput();
        _input = new StreamReader(stdin, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: false);
        _output = new StreamWriter(stdout, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true,
        };
    }

    public void RegisterHandler(string method, Func<BridgeRequest, CancellationToken, Task<BridgeResult>> handler)
        => _handlers[method] = handler;

    public void RegisterHandler(string method, Func<BridgeRequest, Task<BridgeResult>> handler)
        => RegisterHandler(method, (request, _) => handler(request));

    /// <summary>True when a handler is registered for the method.</summary>
    public bool HasHandler(string method) => _handlers.ContainsKey(method);

    /// <summary>Registered method names (startup surface verification).</summary>
    public IReadOnlyCollection<string> RegisteredMethods => _handlers.Keys;

    /// <summary>
    /// Blocks reading stdin until EOF or <see cref="RequestShutdown"/>.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        try
        {
            while (!linked.IsCancellationRequested)
            {
                var line = await _input.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                    break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!BridgeProtocol.TryParseRequest(line, out var request) || request is null)
                    continue;

                _ = DispatchAsync(request, linked.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when shutdown is requested.
        }

        ClientDisconnected?.Invoke();
    }

    private async Task DispatchAsync(BridgeRequest request, CancellationToken cancellationToken)
    {
        BridgeResult result;
        try
        {
            if (!_handlers.TryGetValue(request.Method, out var handler))
            {
                result = BridgeResult.Error(BridgeErrorCodes.UnknownMethod, $"Unknown method: {request.Method}");
            }
            else
            {
                result = await handler(request, cancellationToken).ConfigureAwait(false) ?? BridgeResult.Ok(null);
            }
        }
        catch (OperationCanceledException)
        {
            result = BridgeResult.Error(BridgeErrorCodes.RequestCancelled, "Request cancelled");
        }
        catch (Exception ex)
        {
            result = BridgeResult.Error(BridgeErrorCodes.InternalError, $"{ex.GetType().Name}: {ex.Message}");
        }

        if (request.Id is not null)
        {
            var payload = result.IsError
                ? BridgeProtocol.WriteResponse(request.Id, null, result.ErrorCode, result.ErrorMessage)
                : BridgeProtocol.WriteResponse(request.Id, ToElement(result.Value));
            WriteLine(payload);
        }
    }

    private static JsonElement? ToElement(object? value)
    {
        if (value is null)
            return null;

        var bytes = BridgeProtocol.WriteResult(value);
        using var doc = JsonDocument.Parse(bytes);
        return doc.RootElement.Clone();
    }

    public void Publish(string name, object? data)
    {
        var payload = BridgeProtocol.WriteEvent(name, data);
        WriteLine(payload);
    }

    private void WriteLine(byte[] payload)
    {
        lock (_writeLock)
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;
            try
            {
                _output.BaseStream.Write(payload, 0, payload.Length);
                _output.Write('\n');
            }
            catch (IOException)
            {
                // Pipe closed by the client; ignore.
            }
            catch (ObjectDisposedException)
            {
                // Already disposed.
            }
        }
    }

    public void RequestShutdown() => _cts.Cancel();

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _cts.Cancel();
        _cts.Dispose();
        try
        {
            _output.Dispose();
            _input.Dispose();
        }
        catch (Exception)
        {
            // Best-effort disposal.
        }
    }
}
