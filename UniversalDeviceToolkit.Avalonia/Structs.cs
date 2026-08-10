using System;
using Avalonia;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia;

public readonly struct DashboardGroup(DashboardGroupType type, string? customName, params DashboardItem[] items)
{
    public static readonly DashboardGroup[] DefaultGroups =
    [
        new(DashboardGroupType.Power, null,
            DashboardItem.PowerMode,
            DashboardItem.ItsMode,
            DashboardItem.BatteryMode,
            DashboardItem.BatteryNightChargeMode,
            DashboardItem.AlwaysOnUsb,
            DashboardItem.InstantBoot,
            DashboardItem.FlipToStart),
        new(DashboardGroupType.Graphics, null,
            DashboardItem.HybridMode,
            DashboardItem.DiscreteGpu,
            DashboardItem.OverclockDiscreteGpu),
        new(DashboardGroupType.Display, null,
            DashboardItem.Resolution,
            DashboardItem.RefreshRate,
            DashboardItem.DpiScale,
            DashboardItem.Hdr,
            DashboardItem.OverDrive,
            DashboardItem.TurnOffMonitors),
        new(DashboardGroupType.Other, null,
            DashboardItem.Microphone,
            DashboardItem.WhiteKeyboardBacklight,
            DashboardItem.PanelLogoBacklight,
            DashboardItem.PortsBacklight,
            DashboardItem.TouchpadLock,
            DashboardItem.FnLock,
            DashboardItem.WinKeyLock)
    ];

    public DashboardGroupType Type { get; } = type;

    public string? CustomName { get; } = type == DashboardGroupType.Custom ? customName : null;

    public DashboardItem[] Items { get; } = items;

    public string GetName() => Type switch
    {
        DashboardGroupType.Power => Resource.DashboardPage_Power_Title,
        DashboardGroupType.Graphics => Resource.DashboardPage_Graphics_Title,
        DashboardGroupType.Display => Resource.DashboardPage_Display_Title,
        DashboardGroupType.Other => Resource.DashboardPage_Other_Title,
        DashboardGroupType.Custom => CustomName ?? string.Empty,
        _ => throw new InvalidOperationException($"Invalid type {Type}"),
    };

    public override string ToString() =>
        $"{nameof(Type)}: {Type}," +
        $" {nameof(CustomName)}: {CustomName}," +
        $" {nameof(Items)}: {string.Join(",", Items)}";
}

public readonly struct ScreenInfo(Rect workArea, uint dpiX, uint dpiY, bool isPrimary)
{
    public Rect WorkArea { get; } = workArea;
    public uint DpiX { get; } = dpiX;
    public uint DpiY { get; } = dpiY;
    public bool IsPrimary { get; } = isPrimary;
}

public readonly struct FpsDisplayData
{
    public string? FpsText { get; init; }
    public Brush? FpsBrush { get; init; }
    public string? LowFpsText { get; init; }
    public Brush? LowFpsBrush { get; init; }
    public string? FrameTimeText { get; init; }
    public Brush? FrameTimeBrush { get; init; }
}

public readonly struct SensorSnapshot
{
    public double CpuUsage { get; init; }
    public double CpuFrequency { get; init; }
    public double CpuPClock { get; init; }
    public double CpuEClock { get; init; }
    public double CpuTemp { get; init; }
    public double CpuPower { get; init; }
    public int CpuFanSpeed { get; init; }
    public double GpuUsage { get; init; }
    public double GpuFrequency { get; init; }
    public double GpuTemp { get; init; }
    public double GpuVramUsage { get; init; }
    public double GpuVramUsed { get; init; }
    public double GpuVramTotal { get; init; }
    public double GpuVramTemp { get; init; }
    public double GpuPower { get; init; }
    public int GpuFanSpeed { get; init; }
    public double MemUsage { get; init; }
    public double MemUsed { get; init; }
    public double MemTotal { get; init; }
    public double MemTemp { get; init; }
    public double PchTemp { get; init; }
    public int PchFanSpeed { get; init; }
    public double Disk1Temp { get; init; }
    public double Disk2Temp { get; init; }
}
