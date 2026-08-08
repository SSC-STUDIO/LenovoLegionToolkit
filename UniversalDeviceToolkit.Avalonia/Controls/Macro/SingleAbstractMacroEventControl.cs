using System;
using System.Collections.Generic;
using UniversalDeviceToolkit.Lib.Macro;

namespace UniversalDeviceToolkit.WPF.Controls.Macro;

public class SingleAbstractMacroEventControl : AbstractMacroEventControl
{
    private MacroEvent _macroEvent;

    public override IEnumerable<MacroEvent> GetEvents() => [_macroEvent];

    protected override TimeSpan TotalDelay => _macroEvent.Delay;

    public override void Set(MacroEvent macroEvent)
    {
        _macroEvent = macroEvent;

        base.Set(macroEvent);
    }
}
