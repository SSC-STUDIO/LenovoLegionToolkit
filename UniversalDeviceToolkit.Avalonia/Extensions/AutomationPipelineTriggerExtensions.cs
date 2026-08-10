using System;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Extensions;

public static class AutomationPipelineTriggerExtensions
{
    public static SymbolRegular Icon(this IAutomationPipelineTrigger trigger) => trigger switch
    {
        IPowerStateAutomationPipelineTrigger => SymbolRegular.BatteryCharge24,
        IPowerModeAutomationPipelineTrigger => SymbolRegular.Gauge24,
        IGodModePresetChangedAutomationPipelineTrigger => SymbolRegular.Gauge24,
        IGameAutomationPipelineTrigger => SymbolRegular.XboxController24,
        IHDRPipelineTrigger => SymbolRegular.Hdr24,
        IProcessesAutomationPipelineTrigger => SymbolRegular.WindowConsole20,
        IUserInactivityPipelineTrigger => SymbolRegular.ClockAlarm24,
        ISessionLockPipelineTrigger => SymbolRegular.LockClosed24,
        ISessionUnlockPipelineTrigger => SymbolRegular.LockOpen24,
        ITimeAutomationPipelineTrigger => SymbolRegular.HourglassHalf24,
        IDeviceAutomationPipelineTrigger => SymbolRegular.UsbPlug24,
        INativeWindowsMessagePipelineTrigger => SymbolRegular.Desktop24,
        IOnStartupAutomationPipelineTrigger => SymbolRegular.Flash24,
        IOnResumeAutomationPipelineTrigger => SymbolRegular.Flash24,
        IWiFiConnectedPipelineTrigger => SymbolRegular.Wifi124,
        IWiFiDisconnectedPipelineTrigger => SymbolRegular.WifiOff24,
        IPeriodicAutomationPipelineTrigger => SymbolRegular.ArrowRepeatAll24,
        HardwareSensorAutomationPipelineTrigger => SymbolRegular.DataUsage24,
        BatteryPercentageAutomationPipelineTrigger => SymbolRegular.Battery024,
        ICompositeAutomationPipelineTrigger => SymbolRegular.Flow20,
        _ => throw new ArgumentException($"Unsupported trigger {trigger.GetType().Name}")
    };
}
