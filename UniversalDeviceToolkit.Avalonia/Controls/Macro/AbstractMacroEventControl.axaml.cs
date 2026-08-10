using System;
using System.Collections.Generic;
using Avalonia.Input;
using Humanizer;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Lib.Macro.Resources;

namespace UniversalDeviceToolkit.Avalonia.Controls.Macro
{
public abstract partial class AbstractMacroEventControl : global::Avalonia.Controls.UserControl
{
    // AVALONIA: WPF KeyInterop does not exist; Avalonia's Key enum uses WinUI-style
    // values (not Win32 VK codes), so the standard VK range is mapped by name for
    // display (matching the WPF KeyInterop.KeyFromVirtualKey(...).ToString() output).
    private static readonly Dictionary<int, Key> VirtualKeyMap = new()
    {
        [0x08] = Key.Back,
        [0x09] = Key.Tab,
        [0x0D] = Key.Return,
        [0x13] = Key.Pause,
        [0x14] = Key.CapsLock,
        [0x1B] = Key.Escape,
        [0x20] = Key.Space,
        [0x21] = Key.PageUp,
        [0x22] = Key.PageDown,
        [0x23] = Key.End,
        [0x24] = Key.Home,
        [0x25] = Key.Left,
        [0x26] = Key.Up,
        [0x27] = Key.Right,
        [0x28] = Key.Down,
        [0x2C] = Key.PrintScreen,
        [0x2D] = Key.Insert,
        [0x2E] = Key.Delete,
        [0x30] = Key.D0,
        [0x31] = Key.D1,
        [0x32] = Key.D2,
        [0x33] = Key.D3,
        [0x34] = Key.D4,
        [0x35] = Key.D5,
        [0x36] = Key.D6,
        [0x37] = Key.D7,
        [0x38] = Key.D8,
        [0x39] = Key.D9,
        [0x41] = Key.A,
        [0x42] = Key.B,
        [0x43] = Key.C,
        [0x44] = Key.D,
        [0x45] = Key.E,
        [0x46] = Key.F,
        [0x47] = Key.G,
        [0x48] = Key.H,
        [0x49] = Key.I,
        [0x4A] = Key.J,
        [0x4B] = Key.K,
        [0x4C] = Key.L,
        [0x4D] = Key.M,
        [0x4E] = Key.N,
        [0x4F] = Key.O,
        [0x50] = Key.P,
        [0x51] = Key.Q,
        [0x52] = Key.R,
        [0x53] = Key.S,
        [0x54] = Key.T,
        [0x55] = Key.U,
        [0x56] = Key.V,
        [0x57] = Key.W,
        [0x58] = Key.X,
        [0x59] = Key.Y,
        [0x5A] = Key.Z,
        [0x5B] = Key.LWin,
        [0x5C] = Key.RWin,
        [0x5D] = Key.Apps,
        [0x60] = Key.NumPad0,
        [0x61] = Key.NumPad1,
        [0x62] = Key.NumPad2,
        [0x63] = Key.NumPad3,
        [0x64] = Key.NumPad4,
        [0x65] = Key.NumPad5,
        [0x66] = Key.NumPad6,
        [0x67] = Key.NumPad7,
        [0x68] = Key.NumPad8,
        [0x69] = Key.NumPad9,
        [0x6A] = Key.Multiply,
        [0x6B] = Key.Add,
        [0x6D] = Key.Subtract,
        [0x6E] = Key.Decimal,
        [0x6F] = Key.Divide,
        [0x70] = Key.F1,
        [0x71] = Key.F2,
        [0x72] = Key.F3,
        [0x73] = Key.F4,
        [0x74] = Key.F5,
        [0x75] = Key.F6,
        [0x76] = Key.F7,
        [0x77] = Key.F8,
        [0x78] = Key.F9,
        [0x79] = Key.F10,
        [0x7A] = Key.F11,
        [0x7B] = Key.F12,
        [0x7C] = Key.F13,
        [0x7D] = Key.F14,
        [0x7E] = Key.F15,
        [0x7F] = Key.F16,
        [0x80] = Key.F17,
        [0x81] = Key.F18,
        [0x82] = Key.F19,
        [0x83] = Key.F20,
        [0x84] = Key.F21,
        [0x85] = Key.F22,
        [0x86] = Key.F23,
        [0x87] = Key.F24,
        [0x90] = Key.NumLock,
        [0x91] = Key.Scroll,
        [0xA0] = Key.LeftShift,
        [0xA1] = Key.RightShift,
        [0xA2] = Key.LeftCtrl,
        [0xA3] = Key.RightCtrl,
        [0xA4] = Key.LeftAlt,
        [0xA5] = Key.RightAlt,
        [0xA6] = Key.BrowserBack,
        [0xA7] = Key.BrowserForward,
        [0xA8] = Key.BrowserRefresh,
        [0xA9] = Key.BrowserStop,
        [0xAA] = Key.BrowserSearch,
        [0xAB] = Key.BrowserFavorites,
        [0xAC] = Key.BrowserHome,
        [0xAD] = Key.VolumeMute,
        [0xAE] = Key.VolumeDown,
        [0xAF] = Key.VolumeUp,
        [0xB0] = Key.MediaNextTrack,
        [0xB1] = Key.MediaPreviousTrack,
        [0xB2] = Key.MediaStop,
        [0xB3] = Key.MediaPlayPause,
        [0xB4] = Key.LaunchMail,
        [0xB5] = Key.SelectMedia,
        [0xB6] = Key.LaunchApplication1,
        [0xB7] = Key.LaunchApplication2,
        [0xBA] = Key.OemSemicolon,
        [0xBB] = Key.OemPlus,
        [0xBC] = Key.OemComma,
        [0xBD] = Key.OemMinus,
        [0xBE] = Key.OemPeriod,
        [0xBF] = Key.OemQuestion,
        [0xC0] = Key.OemTilde,
        [0xDB] = Key.OemOpenBrackets,
        [0xDC] = Key.OemPipe,
        [0xDD] = Key.OemCloseBrackets,
        [0xDE] = Key.OemQuotes,
        [0xE2] = Key.Oem102,
    };

    protected AbstractMacroEventControl()
    {
        InitializeComponent();
    }

    public abstract IEnumerable<MacroEvent> GetEvents();

    protected abstract TimeSpan TotalDelay { get; }

    public virtual void Set(MacroEvent macroEvent)
    {
        _card.Icon = new SymbolIcon
        {
            Symbol = macroEvent.Direction switch
            {
                MacroDirection.Up => SymbolRegular.ArrowCircleUp24,
                MacroDirection.Down => SymbolRegular.ArrowCircleDown24,
                MacroDirection.Wheel => SymbolRegular.ArrowRotateClockwise24,
                MacroDirection.HorizontalWheel => SymbolRegular.ArrowRotateClockwise24,
                MacroDirection.Move => SymbolRegular.ArrowMove24,
                _ => SymbolRegular.Empty
            }
        };

        _header.Title = (macroEvent.Source, macroEvent.Direction, macroEvent.Key) switch
        {
            (MacroSource.Keyboard, _, _) => VirtualKeyMap.TryGetValue((int)macroEvent.Key, out var key) ? key.ToString() : Key.None.ToString(),
            (MacroSource.Mouse, MacroDirection.Move, _) => Resource.MacroEventControl_MouseMove_Title,
            (MacroSource.Mouse, MacroDirection.Wheel, >= 0x80000000) => Resource.MacroEventControl_MouseWheelDown_Title,
            (MacroSource.Mouse, MacroDirection.Wheel, _) => Resource.MacroEventControl_MouseWheelUp_Title,
            (MacroSource.Mouse, MacroDirection.HorizontalWheel, >= 0x80000000) => Resource.MacroEventControl_MouseWheelLeft_Title,
            (MacroSource.Mouse, MacroDirection.HorizontalWheel, _) => Resource.MacroEventControl_MouseWheelRight_Title,
            (MacroSource.Mouse, _, >= 0xFF) => string.Format(Resource.MacroEventControl_MouseXButton_Title, macroEvent.Key >> 16),
            (MacroSource.Mouse, _, 1) => Resource.MacroEventControl_MouseLButton_Title,
            (MacroSource.Mouse, _, 2) => Resource.MacroEventControl_MouseRButton_Title,
            (MacroSource.Mouse, _, 3) => Resource.MacroEventControl_MouseMButton_Title,
            (MacroSource.Mouse, _, _) => string.Format(Resource.MacroEventControl_MouseButton_Title, macroEvent.Key),
            _ => string.Empty
        };

        _header.Subtitle = macroEvent.Source.GetDisplayName() + $" → {TotalDelay.Humanize(maxUnit: TimeUnit.Millisecond, culture: Resource.Culture)}";
    }
}
}
