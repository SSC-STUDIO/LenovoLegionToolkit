using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Lib.Macro.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace UniversalDeviceToolkit.Lib.Macro;

public class MacroController : IDisposable
{
    public class RecorderReceivedEventArgs : EventArgs
    {
        public MacroEvent MacroEvent { get; init; }
    }

    public class RecorderStoppedEventArgs : EventArgs
    {
        public bool Interrupted { get; init; }
    }

    private static readonly uint[] AllowedKeys = [0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69];
    public static readonly int[] AllowedRepeatCounts = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    private readonly MacroRecorder _recorder = new();
    private readonly MacroPlayer _player = new();

    private readonly HOOKPROC _kbProc;
    private readonly MacroSettings _settings;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;

    private HHOOK _kbHook;
    private bool _disposed;

    public event EventHandler<RecorderReceivedEventArgs>? RecorderReceived;
    public event EventHandler<RecorderStoppedEventArgs>? RecorderStopped;

    public bool IsEnabled => _settings.Store.IsEnabled;

    public MacroController(MacroSettings settings, IMainThreadDispatcher mainThreadDispatcher)
    {
        _settings = settings;

        _mainThreadDispatcher = mainThreadDispatcher;

        _kbProc = LowLevelKeyboardProc;

        _recorder.Received += Recorder_Received;
        _recorder.Stopped += Recorder_Stopped;
    }

    private void Recorder_Received(object? sender, MacroRecorder.ReceivedEventArgs e) => RecorderReceived?.Invoke(this, new() { MacroEvent = e.MacroEvent });

    private void Recorder_Stopped(object? sender, MacroRecorder.StoppedEventArgs e) => RecorderStopped?.Invoke(this, new() { Interrupted = e.Interrupted });

    public void SetEnabled(bool enabled)
    {
        _settings.Store.IsEnabled = enabled;
        _settings.SynchronizeStore();

        // Dynamically start/stop the hook when the macro feature is toggled at
        // runtime so no global keyboard hook stays installed while disabled.
        if (enabled)
            Start();
        else
            Stop();
    }

    public Dictionary<MacroIdentifier, MacroSequence> GetSequences() => _settings.Store.Sequences;

    public void SetSequences(Dictionary<MacroIdentifier, MacroSequence> sequences)
    {
        CleanUp(ref sequences);

        _settings.Store.Sequences = sequences;
        _settings.SynchronizeStore();
    }

    public void Start()
    {
        // Don't install a global keyboard hook when macros are disabled -- the
        // callback would call CallNextHookEx for every keystroke for nothing.
        if (!IsEnabled)
            return;

        if (_kbHook != default)
            return;

        // WH_KEYBOARD_LL callbacks are dispatched via the installing thread's
        // message loop. Installing from a background thread (e.g. the Task.Run
        // inside App.InitMacroController) leaves the hook with no message pump,
        // so Windows times out on every keystroke system-wide. Marshal to the UI
        // thread which pumps messages for the hook's lifetime.
        _mainThreadDispatcher.Dispatch(() =>
        {
            if (_kbHook != default)
                return;

            _kbHook = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_KEYBOARD_LL, _kbProc, HINSTANCE.Null, 0);
        });
    }

    public void Stop()
    {
        if (_kbHook == default)
            return;

        // Capture the live hook handle and clear the field BEFORE teardown so a
        // concurrent Start() cannot re-enter and double-install the hook.
        var hook = _kbHook;
        _kbHook = default;

        // Unhook the system-wide WH_KEYBOARD_LL hook FIRST. A throw from the recorder
        // or player teardown must never leave the hook installed (orphan + duplicate
        // macro firing on next Start). Each teardown step is isolated and traced (KB #8).
        try
        {
            if (!PInvoke.UnhookWindowsHookEx(hook) && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("MacroController.UnhookWindowsHookEx returned false (hook may already be invalid).");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("MacroController.Stop() unhook failed.", ex);
        }

        try
        {
            _recorder.StopRecording();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("MacroController.Stop() recorder teardown failed.", ex);
        }

        try
        {
            _player.Stop();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("MacroController.Stop() player teardown failed.", ex);
        }
    }

    public void StartRecording(MacroRecorderSettings settings = MacroRecorderSettings.Keyboard) => _recorder.StartRecording(settings);

    public void StopRecording() => _recorder.StopRecording();

    private unsafe LRESULT LowLevelKeyboardProc(int nCode, WPARAM wParam, LPARAM lParam)
    {
        if (nCode != PInvoke.HC_ACTION)
            return PInvoke.CallNextHookEx(HHOOK.Null, nCode, wParam, lParam);

        if (!IsEnabled)
            return PInvoke.CallNextHookEx(HHOOK.Null, nCode, wParam, lParam);

        ref var kbStruct = ref Unsafe.AsRef<KBDLLHOOKSTRUCT>((void*)lParam.Value);

        _player.InterruptIfNeeded(kbStruct);

        var shouldRun = !_recorder.IsRecording;
        shouldRun &= kbStruct.flags == 0;
        shouldRun &= _settings.Store.Sequences.GetValueOrNull(new(MacroSource.Keyboard, kbStruct.vkCode))?.Events?.Length > 0;
        shouldRun &= AllowedKeys.Contains(kbStruct.vkCode);

        if (!shouldRun)
            return PInvoke.CallNextHookEx(HHOOK.Null, nCode, wParam, lParam);

        var sequence = _settings.Store.Sequences[new(MacroSource.Keyboard, kbStruct.vkCode)];
        Play(sequence);

        return new LRESULT(96);
    }

    private void Play(MacroSequence sequence) => _ = PlayAsync(sequence);

    private async Task PlayAsync(MacroSequence sequence)
    {
        try
        {
            await _player.StartPlayingAsync(sequence).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error playing macro sequence: {ex.Message}", ex);
        }
    }

    private static void CleanUp(ref Dictionary<MacroIdentifier, MacroSequence> sequences)
    {
        sequences = ClearDownsWithoutUps(sequences);
        sequences = ClearEmptySequences(sequences);
    }

    private static Dictionary<MacroIdentifier, MacroSequence> ClearEmptySequences(Dictionary<MacroIdentifier, MacroSequence> sequences)
    {
        return sequences.Where(kv => kv.Value.Events?.Length > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static Dictionary<MacroIdentifier, MacroSequence> ClearDownsWithoutUps(Dictionary<MacroIdentifier, MacroSequence> sequences)
    {
        var result = new Dictionary<MacroIdentifier, MacroSequence>();
        foreach (var kv in sequences)
            result[kv.Key] = ClearDownsWithoutUps(kv.Value);
        return result;
    }

    private static MacroSequence ClearDownsWithoutUps(MacroSequence sequence)
    {
        var macroEvents = new List<MacroEvent>(sequence.Events ?? []);
        for (var i = macroEvents.Count - 1; i >= 0; i--)
        {
            var macroEvent = macroEvents[i];

            switch (macroEvent.Direction)
            {
                case MacroDirection.Down:
                    {
                        var remove = true;

                        for (var j = i; j < macroEvents.Count; j++)
                        {
                            if (macroEvents[j].Direction != MacroDirection.Up || macroEvents[j].Key != macroEvent.Key || macroEvents[j].Source != macroEvent.Source)
                                continue;

                            remove = false;
                            break;
                        }

                        if (remove)
                            macroEvents.RemoveAt(i);
                        break;
                    }
                case MacroDirection.Up:
                case MacroDirection.Unknown:
                default:
                    continue;
            }
        }

        return sequence with { Events = [.. macroEvents] };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Stop();
            _recorder.Received -= Recorder_Received;
            _recorder.Stopped -= Recorder_Stopped;
        }

        _disposed = true;
    }
}
