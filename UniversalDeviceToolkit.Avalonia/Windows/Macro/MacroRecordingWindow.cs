using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows;

namespace UniversalDeviceToolkit.Avalonia.Windows.Macro;

public class MacroRecordingWindow : BaseWindow
{
    private readonly Grid _mainGrid = new()
    {
        RowDefinitions =
        {
            new() { Height = GridLength.Auto },
            new() { Height = GridLength.Auto },
        },
        ColumnDefinitions =
        {
            new() { Width = GridLength.Auto, },
            new() { Width = new(1, GridUnitType.Star) },
        },
        Margin = new(16, 16, 32, 16),
    };

    private readonly SymbolIcon _symbolIcon = new()
    {
        FontSize = 32,
        Margin = new(0, 0, 16, 0),
    };

    private readonly TextBlock _titleLabel = new()
    {
        FontSize = 16,
        FontWeight = FontWeight.Medium,
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
    };

    private readonly TextBlock _subtitleLabel = new()
    {
        FontSize = 14,
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
        Margin = new(0, 4, 0, 0),
    };

    public static MacroRecordingWindow CreatePreparing() => new(SymbolRegular.HourglassThreeQuarter24, Resource.MacroRecordingWindow_Preparing_Title, null);

    public static MacroRecordingWindow CreateRecording() => new(SymbolRegular.Record24, Resource.MacroRecordingWindow_Recording_Title, Resource.MacroRecordingWindow_Recording_Message);

    private MacroRecordingWindow(SymbolRegular symbol, string title, string? subtitle)
    {
        InitializeStyle();
        InitializeContent(symbol, title, subtitle);

        _mainGrid.Measure(new Size(double.PositiveInfinity, 80));

        Width = MaxWidth = MinWidth = Math.Max(_mainGrid.DesiredSize.Width, 300);
        Height = MaxHeight = MinHeight = _mainGrid.DesiredSize.Height;
    }

    private void InitializeStyle()
    {
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;

        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Focusable = false;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;

        _mainGrid.FlowDirection = LocalizationHelper.Direction;
        _titleLabel.Foreground = (IBrush)this.FindResource("TextFillColorPrimaryBrush")!;
    }

    private void InitializeContent(SymbolRegular symbol, string title, string? subtitle)
    {
        _symbolIcon.Symbol = symbol;
        _titleLabel.Text = title;
        _subtitleLabel.Text = subtitle;


        Grid.SetRow(_symbolIcon, 0);
        Grid.SetRow(_titleLabel, 0);
        Grid.SetRow(_subtitleLabel, 1);

        Grid.SetColumn(_symbolIcon, 0);
        Grid.SetColumn(_titleLabel, 1);
        Grid.SetColumn(_subtitleLabel, 1);

        Grid.SetRowSpan(_symbolIcon, 2);

        if (subtitle is null)
            Grid.SetRowSpan(_titleLabel, 2);

        _mainGrid.Children.Add(_symbolIcon);
        _mainGrid.Children.Add(_titleLabel);
        _mainGrid.Children.Add(_subtitleLabel);

        Content = _mainGrid;
    }
}
