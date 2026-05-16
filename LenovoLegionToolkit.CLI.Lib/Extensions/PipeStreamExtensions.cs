using System;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace LenovoLegionToolkit.CLI.Lib.Extensions;

public static class PipeStreamExtensions
{
    private static readonly Encoding Encoding = Encoding.UTF8;

    private static readonly JsonSerializerOptions PipeJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static async Task WriteObjectAsync<T>(this PipeStream stream, T obj, CancellationToken token = default)
    {
        if (stream.ReadMode != PipeTransmissionMode.Message)
            throw new InvalidOperationException("ReadMode is not PipeTransmissionMode.Message");

        var str = JsonSerializer.Serialize(obj, PipeJsonOptions);
        var bytes = Encoding.GetBytes(str);
        await stream.WriteAsync(bytes, token).ConfigureAwait(false);
    }

    public static async Task<T?> ReadObjectAsync<T>(this PipeStream stream, CancellationToken token = default)
    {
        if (stream.ReadMode != PipeTransmissionMode.Message)
            throw new InvalidOperationException("ReadMode is not PipeTransmissionMode.Message");

        var buffer = new byte[1024];
        var builder = new StringBuilder();

        do
        {
            var bytesRead = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
            builder.Append(Encoding.GetString(buffer, 0, bytesRead));
        } while (!stream.IsMessageComplete);

        return JsonSerializer.Deserialize<T>(builder.ToString(), PipeJsonOptions);
    }
}
