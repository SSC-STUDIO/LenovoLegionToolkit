using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// Macro bridge: enable state, slot/sequence read/write and explicit playback.
///
/// Recording is intentionally NOT implemented here: the headless host has no
/// message-pumping UI thread, and MacroController's global input hooks
/// (WH_KEYBOARD_LL / WH_MOUSE_LL) only fire when the installing thread pumps
/// messages. Installing them from the bridge would silently capture nothing
/// (and degrade system input while Windows times the hook out), so
/// macro.startRecording / macro.stopRecording return -1005 — the recording UI
/// lives in the desktop (WPF) frontend.
/// </summary>
public static class MacroHandlers
{
    private const int RecordingNotAvailable = -1005;

    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("macro.getState", (request, _) => HandleGetStateAsync());
        rpc.RegisterHandler("macro.setEnabled", (request, _) => HandleSetEnabledAsync(request));
        rpc.RegisterHandler("macro.play", (request, _) => HandlePlayAsync(request));
        rpc.RegisterHandler("macro.startRecording", (request, _) => HandleStartRecordingAsync(request));
        rpc.RegisterHandler("macro.stopRecording", (request, _) => HandleStopRecordingAsync());
        rpc.RegisterHandler("macro.saveSequence", (request, _) => HandleSaveSequenceAsync(request));
        rpc.RegisterHandler("macro.clearSequence", (request, _) => HandleClearSequenceAsync(request));
    }

    private static MacroController Controller => IoCContainer.Resolve<MacroController>();

    private static async Task<BridgeResult> HandleGetStateAsync()
    {
        try
        {
            // JSON-friendly snapshot of the macro store. Slots are keyed by the
            // keyboard virtual-key code (0x60-0x69 numpad); Mouse-source sequences
            // can only exist in the settings file, so slots stay keyboard-oriented
            // like the WPF MacroPage number pad.
            var controller = Controller;
            var slots = controller.GetSequences()
                .OrderBy(kv => kv.Key.Key)
                .Where(kv => kv.Value.Events is { Length: > 0 })
                .Select(kv => new
                {
                    key = kv.Key.Key,
                    source = kv.Key.Source.ToString(),
                    repeatCount = kv.Value.RepeatCount,
                    ignoreDelays = kv.Value.IgnoreDelays,
                    interruptOnOtherKey = kv.Value.InterruptOnOtherKey,
                    events = (kv.Value.Events ?? []).Select(e => new
                    {
                        source = e.Source.ToString(),
                        direction = e.Direction.ToString(),
                        key = e.Key,
                        x = e.Point.X,
                        y = e.Point.Y,
                        delayMs = e.Delay.TotalMilliseconds,
                    }).ToArray(),
                })
                .ToArray();

            await Task.CompletedTask;
            return BridgeResult.Ok(new { isEnabled = controller.IsEnabled, slots });
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleSetEnabledAsync(BridgeRequest request)
    {
        try
        {
            if (!request.Parameters.TryGetProperty("enabled", out var enabledProp) ||
                enabledProp.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new BridgeErrorException(-32602, "Missing boolean parameter 'enabled'.");

            // Persists IsEnabled and starts/stops the global keyboard hook.
            // In the headless host the hook is installed without a message pump,
            // so automatic playback is inert — explicit macro.play still works.
            Controller.SetEnabled(enabledProp.GetBoolean());

            return BridgeResult.Ok(new { ok = true });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandlePlayAsync(BridgeRequest request)
    {
        try
        {
            var key = GetRequiredUInt64(request.Parameters, "key");
            var played = Controller.TryPlaySequence(key);

            await Task.CompletedTask;
            return BridgeResult.Ok(new { ok = played });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleStartRecordingAsync(BridgeRequest request)
    {
        try
        {
            _ = GetRequiredUInt64(request.Parameters, "key");
            _ = ParseRecordingMode(request.Parameters);

            await Task.CompletedTask;
            return RecordingUnavailable();
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleStopRecordingAsync()
    {
        try
        {
            await Task.CompletedTask;
            return RecordingUnavailable();
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static BridgeResult RecordingUnavailable() => BridgeResult.Error(
        RecordingNotAvailable,
        "Recording UI lives in frontend, not implemented: the headless host has no message-pumping UI thread, so global input hooks cannot capture events; use the desktop (WPF) recording UI.");

    private static async Task<BridgeResult> HandleSaveSequenceAsync(BridgeRequest request)
    {
        try
        {
            var key = GetRequiredUInt64(request.Parameters, "key");
            var repeatCount = Math.Clamp(GetOptionalInt(request.Parameters, "repeatCount", 1), 1, 10);
            var ignoreDelays = GetOptionalBool(request.Parameters, "ignoreDelays", false);
            var interruptOnOtherKey = GetOptionalBool(request.Parameters, "interruptOnOtherKey", false);
            var events = ParseEvents(request.Parameters);

            // Merge into the existing store instead of replacing it; SetSequences
            // applies the same cleanup (empty sequences, unpaired key-downs) as WPF.
            var sequences = new Dictionary<MacroIdentifier, MacroSequence>(Controller.GetSequences())
            {
                [new MacroIdentifier(MacroSource.Keyboard, key)] = new()
                {
                    RepeatCount = repeatCount,
                    IgnoreDelays = ignoreDelays,
                    InterruptOnOtherKey = interruptOnOtherKey,
                    Events = events,
                },
            };
            Controller.SetSequences(sequences);

            return BridgeResult.Ok(new { ok = true });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<BridgeResult> HandleClearSequenceAsync(BridgeRequest request)
    {
        try
        {
            var key = GetRequiredUInt64(request.Parameters, "key");
            var identifier = new MacroIdentifier(MacroSource.Keyboard, key);

            var sequences = new Dictionary<MacroIdentifier, MacroSequence>(Controller.GetSequences());
            if (sequences.Remove(identifier))
                Controller.SetSequences(sequences);

            return BridgeResult.Ok(new { ok = true });
        }
        catch (BridgeErrorException ex)
        {
            return BridgeResult.Error(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static MacroRecorderSettings ParseRecordingMode(JsonElement parameters)
    {
        var mode = parameters.TryGetProperty("mode", out var modeProp) && modeProp.ValueKind == JsonValueKind.String
            ? modeProp.GetString()!
            : "Keyboard";

        return mode switch
        {
            "Keyboard" => MacroRecorderSettings.Keyboard,
            "KeyboardMouse" => MacroRecorderSettings.Keyboard | MacroRecorderSettings.Mouse,
            "KeyboardMouseMovement" => MacroRecorderSettings.Keyboard | MacroRecorderSettings.Mouse | MacroRecorderSettings.Movement,
            _ => throw new BridgeErrorException(-32602, $"Unknown recording mode '{mode}'."),
        };
    }

    private static MacroEvent[] ParseEvents(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("events", out var eventsProp) || eventsProp.ValueKind != JsonValueKind.Array)
            return [];

        return eventsProp.EnumerateArray().Select(ParseEvent).ToArray();
    }

    private static MacroEvent ParseEvent(JsonElement element)
    {
        return new MacroEvent
        {
            Source = ParseEnum<MacroSource>(element, "source"),
            Direction = ParseEnum<MacroDirection>(element, "direction"),
            Key = element.TryGetProperty("key", out var keyProp) && keyProp.ValueKind == JsonValueKind.Number
                ? keyProp.GetUInt32()
                : 0,
            Point = new Point(
                element.TryGetProperty("x", out var xProp) && xProp.ValueKind == JsonValueKind.Number ? xProp.GetInt32() : 0,
                element.TryGetProperty("y", out var yProp) && yProp.ValueKind == JsonValueKind.Number ? yProp.GetInt32() : 0),
            Delay = TimeSpan.FromMilliseconds(
                element.TryGetProperty("delayMs", out var delayProp) && delayProp.ValueKind == JsonValueKind.Number
                    ? delayProp.GetDouble()
                    : 0),
        };
    }

    private static TEnum ParseEnum<TEnum>(JsonElement element, string name) where TEnum : struct, Enum
    {
        if (!element.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String)
            throw new BridgeErrorException(-32602, $"Missing string parameter '{name}'.");
        return Enum.Parse<TEnum>(prop.GetString()!, ignoreCase: true);
    }

    private static ulong GetRequiredUInt64(JsonElement parameters, string name)
    {
        if (!parameters.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Number)
            throw new BridgeErrorException(-32602, $"Missing number parameter '{name}'.");
        try
        {
            return prop.GetUInt64();
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new BridgeErrorException(-32602, $"Parameter '{name}' must be an unsigned integer.");
        }
    }

    private static int GetOptionalInt(JsonElement parameters, string name, int fallback)
    {
        return parameters.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetInt32()
            : fallback;
    }

    private static bool GetOptionalBool(JsonElement parameters, string name, bool fallback)
    {
        return parameters.TryGetProperty(name, out var prop) && prop.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? prop.GetBoolean()
            : fallback;
    }
}
