using System;
using System.Collections.Generic;
using System.Windows.Input;
using Humanizer;
using LenovoLegionToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Lib.Macro.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Macro
{
public abstract partial class AbstractMacroEventControl
{
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
            (MacroSource.Keyboard, _, _) => KeyInterop.KeyFromVirtualKey((int)macroEvent.Key).ToString(),
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

