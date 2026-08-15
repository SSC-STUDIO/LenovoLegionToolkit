using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;
using UniversalDeviceToolkit.Abstractions.Utils;

namespace UniversalDeviceToolkit.Lib.Macro.Utils;

internal class MacroPlayer
{
    private const int MAGIC_NUMBER = 1337;

    private readonly ThreadSafeBool _isPlayingInterruptableSequence = new();

    private Task _playTask = Task.CompletedTask;
    private CancellationTokenSource _cancellationTokenSource = new();
    private readonly IDelayProvider _delayProvider;

    public void InterruptIfNeeded(KBDLLHOOKSTRUCT kbStruct)
    {
        if (!_isPlayingInterruptableSequence.Value)
            return;
        if (kbStruct.flags != 0)
            return;
        if (kbStruct.dwExtraInfo == MAGIC_NUMBER)
            return;

        _cancellationTokenSource.Cancel();
    }

    public void Stop()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "macro-player-stop-cancel",
                "MacroPlayer.Stop cancel failed during cleanup.",
                ex);
        }
    }

    public MacroPlayer(IDelayProvider? delayProvider = null)
    {
        _delayProvider = delayProvider ?? new DefaultDelayProvider();
    }

    public async Task StartPlayingAsync(MacroSequence sequence)
    {
        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        try { await _playTask.ConfigureAwait(false); }
        catch (OperationCanceledException)
        {
            // Expected when playback is cancelled, no action needed
        }

        var oldCts = _cancellationTokenSource;
        _cancellationTokenSource = new();
        try { oldCts?.Dispose(); } catch (Exception ex) { if (Log.Instance.IsTraceEnabled) Log.Instance.Trace($"Failed to dispose old CancellationTokenSource in {nameof(StartPlayingAsync)}.", ex); }
        var token = _cancellationTokenSource.Token;

        _playTask = Task.Run(async () =>
        {
            _isPlayingInterruptableSequence.Value = sequence.InterruptOnOtherKey;

            for (var i = 0; i < sequence.RepeatCount; i++)
            {
                foreach (var macroEvent in sequence.Events ?? [])
                {
                    if (!sequence.IgnoreDelays)
                        await _delayProvider.Delay(macroEvent.Delay, token).ConfigureAwait(false);

                    token.ThrowIfCancellationRequested();

                    try
                    {
                        var input = ToInput(macroEvent, GetPrimaryWorkingArea());
                        var result = PInvoke.SendInput(MemoryMarshal.CreateSpan(ref input, 1), Marshal.SizeOf<INPUT>());
                        if (result == 0)
                            PInvokeExtensions.ThrowIfWin32Error($"Failed to send input. Return code was {result}.");
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Failed to send input for event {macroEvent}", ex);
                    }
                }
            }
        }, token);
    }

    private static INPUT ToInput(MacroEvent macroEvent, Rectangle screenArea) => macroEvent.Source switch
    {
        MacroSource.Keyboard => ToKeyboardInput(macroEvent),
        MacroSource.Mouse => ToMouseInput(macroEvent, screenArea),
        MacroSource.Unknown => throw new ArgumentException(null, nameof(macroEvent)),
        _ => throw new ArgumentOutOfRangeException(nameof(macroEvent))
    };

    /// <summary>
    /// Primary-monitor working area (taskbar excluded) without
    /// System.Windows.Forms.Screen; absolute mouse coordinates are normalized
    /// against it. Falls back to an empty rectangle when the query fails.
    /// </summary>
    private static unsafe Rectangle GetPrimaryWorkingArea()
    {
        try
        {
            var workArea = new RECT();
            if (PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETWORKAREA, 0, &workArea, 0))
                return Rectangle.FromLTRB(workArea.left, workArea.top, workArea.right, workArea.bottom);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("SPI_GETWORKAREA failed; macro mouse moves use an empty screen area.", ex);
        }

        return Rectangle.Empty;
    }

    private static INPUT ToKeyboardInput(MacroEvent macroEvent) => new()
    {
        type = INPUT_TYPE.INPUT_KEYBOARD,
        Anonymous = new INPUT._Anonymous_e__Union
        {
            ki = new KEYBDINPUT
            {
                wVk = (VIRTUAL_KEY)macroEvent.Key,
                dwFlags = macroEvent.Direction switch
                {
                    MacroDirection.Up => KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP,
                    _ => 0
                },
                dwExtraInfo = MAGIC_NUMBER
            }
        }
    };

    private static INPUT ToMouseInput(MacroEvent macroEvent, Rectangle screenArea) => new()
    {
        type = INPUT_TYPE.INPUT_MOUSE,
        Anonymous = new INPUT._Anonymous_e__Union
        {
            mi = new MOUSEINPUT
            {
                dwFlags = (macroEvent.Direction, macroEvent.Key) switch
                {
                    (MacroDirection.Up, 1) => MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP,
                    (MacroDirection.Down, 1) => MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN,
                    (MacroDirection.Up, 2) => MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTUP,
                    (MacroDirection.Down, 2) => MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTDOWN,
                    (MacroDirection.Up, 3) => MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEUP,
                    (MacroDirection.Down, 3) => MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEDOWN,
                    (MacroDirection.Up, > 0xFF) => MOUSE_EVENT_FLAGS.MOUSEEVENTF_XUP,
                    (MacroDirection.Down, > 0xFF) => MOUSE_EVENT_FLAGS.MOUSEEVENTF_XDOWN,
                    (MacroDirection.Wheel, _) => MOUSE_EVENT_FLAGS.MOUSEEVENTF_WHEEL,
                    (MacroDirection.Move, _) => MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE,
                    _ => 0
                },
                mouseData = (macroEvent.Direction, macroEvent.Key) switch
                {
                    (MacroDirection.Up, >= 0xFF) => macroEvent.Key >> 16,
                    (MacroDirection.Down, >= 0xFF) => macroEvent.Key >> 16,
                    (MacroDirection.Wheel, _) => macroEvent.Key,
                    _ => 0
                },
                dx = macroEvent.Direction switch
                {
                    MacroDirection.Move => (int)(65535.0f * (macroEvent.Point.X / (float)screenArea.Width) + 0.5f),
                    _ => 0
                },
                dy = macroEvent.Direction switch
                {
                    MacroDirection.Move => (int)(65535.0f * (macroEvent.Point.Y / (float)screenArea.Height) + 0.5f),
                    _ => 0
                },
                dwExtraInfo = MAGIC_NUMBER
            }
        }
    };
}
