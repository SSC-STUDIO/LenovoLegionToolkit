using System;
using System.Collections.Generic;
using System.IO;
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
public enum BridgeParseStatus
{
    Empty,
    Event,
    Ok,
    InvalidJson,
    InvalidRequest,
}

/// <summary>
/// Encode/decode helpers for the Host stdio bridge protocol.
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
        => TryParseLine(line, out request, out _) == BridgeParseStatus.Ok;

    /// <summary>
    /// Classifies one stdio line: valid request, event, empty, parse error, or
    /// invalid request (object without a method / malformed id).
    /// </summary>
    public static BridgeParseStatus TryParseLine(string line, out BridgeRequest? request, out long? id)
    {
        request = null;
        id = null;
        if (string.IsNullOrWhiteSpace(line))
            return BridgeParseStatus.Empty;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            return BridgeParseStatus.InvalidJson;
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return BridgeParseStatus.InvalidRequest;

            if (!TryReadId(root, out id, out var idMalformed) || idMalformed)
                return BridgeParseStatus.InvalidRequest;

            if (root.TryGetProperty(EventProperty, out _))
                return BridgeParseStatus.Event;

            if (!root.TryGetProperty("method", out var method) || method.ValueKind != JsonValueKind.String)
                return BridgeParseStatus.InvalidRequest;

            var methodName = method.GetString();
            if (string.IsNullOrWhiteSpace(methodName))
                return BridgeParseStatus.InvalidRequest;

            var parameters = root.TryGetProperty("params", out var paramsProp)
                ? paramsProp.Clone()
                : default;

            request = new BridgeRequest(id, methodName, parameters);
            return BridgeParseStatus.Ok;
        }
    }

    private static bool TryReadId(JsonElement root, out long? id, out bool malformed)
    {
        id = null;
        malformed = false;
        if (!root.TryGetProperty("id", out var idProp) || idProp.ValueKind == JsonValueKind.Null)
            return true;

        if (idProp.ValueKind == JsonValueKind.Number)
        {
            if (idProp.TryGetInt64(out var number))
            {
                id = number;
                return true;
            }

            malformed = true;
            return false;
        }

        if (idProp.ValueKind == JsonValueKind.String)
        {
            var text = idProp.GetString();
            if (long.TryParse(text, out var parsed))
            {
                id = parsed;
                return true;
            }

            malformed = true;
            return false;
        }

        malformed = true;
        return false;
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
/// Frame size, pending count and handler concurrency are bounded so a noisy
/// renderer or plugin cannot exhaust memory or thread-pool work.
/// </summary>
public sealed class BridgeRpcServer : IDisposable
{
    public const int MaxFrameBytes = 1_048_576;
    public const int MaxPendingRequests = 64;
    public const int MaxConcurrentHandlers = 16;
    private const int DrainTimeoutMs = 2000;

    private readonly Dictionary<string, Func<BridgeRequest, CancellationToken, Task<BridgeResult>>> _handlers = new(StringComparer.Ordinal);
    private readonly object _writeLock = new();
    private readonly object _inflightLock = new();
    private readonly HashSet<Task> _inflight = new();
    private readonly Stream _input;
    private readonly StreamWriter _output;
    private readonly BoundedLineReader _lineReader;
    private readonly SemaphoreSlim _concurrency = new(MaxConcurrentHandlers, MaxConcurrentHandlers);
    private readonly CancellationTokenSource _cts = new();
    private int _pending;
    private int _disposed;

    /// <summary>Raised when the client closes the pipe (Electron exited).</summary>
    public event Action? ClientDisconnected;

    public BridgeRpcServer()
    {
        _input = Console.OpenStandardInput();
        var stdout = Console.OpenStandardOutput();
        _lineReader = new BoundedLineReader(_input);
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
                BoundedLineStatus status;
                string? line;
                try
                {
                    (status, line) = await _lineReader.ReadLineAsync(MaxFrameBytes, linked.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (status == BoundedLineStatus.Eof)
                    break;

                if (status == BoundedLineStatus.TooLarge)
                {
                    WriteError(null, BridgeErrorCodes.RequestTooLarge,
                        $"Request frame exceeds {MaxFrameBytes} bytes.");
                    continue;
                }

                if (line is null || string.IsNullOrWhiteSpace(line))
                    continue;

                var parse = BridgeProtocol.TryParseLine(line, out var request, out var id);
                if (parse == BridgeParseStatus.Empty || parse == BridgeParseStatus.Event)
                    continue;

                if (parse == BridgeParseStatus.InvalidJson)
                {
                    WriteError(id, BridgeErrorCodes.ParseError, "Invalid JSON.");
                    continue;
                }

                if (parse != BridgeParseStatus.Ok || request is null)
                {
                    WriteError(id, BridgeErrorCodes.InvalidRequest, "Invalid request.");
                    continue;
                }

                var pending = Interlocked.Increment(ref _pending);
                if (pending > MaxPendingRequests)
                {
                    Interlocked.Decrement(ref _pending);
                    WriteError(request.Id, BridgeErrorCodes.TooManyRequests,
                        $"Too many pending requests (limit {MaxPendingRequests}).");
                    continue;
                }

                TrackInflight(DispatchAsync(request, linked.Token));
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when shutdown is requested.
        }

        await DrainInflightAsync().ConfigureAwait(false);
        ClientDisconnected?.Invoke();
    }

    private void TrackInflight(Task task)
    {
        lock (_inflightLock)
            _inflight.Add(task);

        _ = AwaitInflightAsync(task);
    }

    private async Task AwaitInflightAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // DispatchAsync already converts handler failures into RPC errors.
        }
        finally
        {
            lock (_inflightLock)
                _inflight.Remove(task);
        }
    }

    private async Task DrainInflightAsync()
    {
        Task[] snapshot;
        lock (_inflightLock)
            snapshot = _inflight.Count == 0 ? Array.Empty<Task>() : _inflight.ToArray();

        if (snapshot.Length == 0)
            return;

        try
        {
            var drain = Task.WhenAll(snapshot);
            await Task.WhenAny(drain, Task.Delay(DrainTimeoutMs)).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Best-effort drain.
        }
    }

    private async Task DispatchAsync(BridgeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            BridgeResult result;
            try
            {
                await _concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteError(request.Id, BridgeErrorCodes.RequestCancelled, "Request cancelled");
                return;
            }

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
            finally
            {
                _concurrency.Release();
            }

            if (request.Id is not null)
            {
                var payload = result.IsError
                    ? BridgeProtocol.WriteResponse(request.Id, null, result.ErrorCode, result.ErrorMessage)
                    : BridgeProtocol.WriteResponse(request.Id, ToElement(result.Value));
                WriteLine(payload);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _pending);
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

    private void WriteError(long? id, int code, string message)
    {
        WriteLine(BridgeProtocol.WriteResponse(id, null, code, message));
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
        _concurrency.Dispose();
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

    private enum BoundedLineStatus
    {
        Ok,
        Eof,
        TooLarge,
    }

    private sealed class BoundedLineReader
    {
        private readonly Stream _stream;
        private readonly byte[] _buffer = new byte[4096];
        private readonly List<byte> _line = new();
        private int _buffered;
        private int _offset;

        public BoundedLineReader(Stream stream)
        {
            _stream = stream;
        }

        public async Task<(BoundedLineStatus Status, string? Line)> ReadLineAsync(
            int maxBytes,
            CancellationToken cancellationToken)
        {
            _line.Clear();
            var overflow = false;

            while (true)
            {
                if (_offset >= _buffered)
                {
                    _buffered = await _stream.ReadAsync(_buffer.AsMemory(0, _buffer.Length), cancellationToken)
                        .ConfigureAwait(false);
                    _offset = 0;
                    if (_buffered == 0)
                    {
                        if (_line.Count == 0 && !overflow)
                            return (BoundedLineStatus.Eof, null);
                        return overflow
                            ? (BoundedLineStatus.TooLarge, null)
                            : (BoundedLineStatus.Ok, Encoding.UTF8.GetString(_line.ToArray()));
                    }
                }

                var value = _buffer[_offset++];
                if (value == (byte)'\n')
                {
                    if (overflow)
                        return (BoundedLineStatus.TooLarge, null);
                    if (_line.Count > 0 && _line[_line.Count - 1] == (byte)'\r')
                        _line.RemoveAt(_line.Count - 1);
                    return (BoundedLineStatus.Ok, Encoding.UTF8.GetString(_line.ToArray()));
                }

                if (overflow)
                    continue;

                if (_line.Count >= maxBytes)
                {
                    overflow = true;
                    _line.Clear();
                    continue;
                }

                _line.Add(value);
            }
        }
    }
}
