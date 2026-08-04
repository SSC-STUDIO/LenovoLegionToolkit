using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using UniversalDeviceToolkit.Plugins.Shared;

namespace UniversalDeviceToolkit.Plugins.CustomMouse;

public partial class CustomMouseSettingsControl : UserControl
{
    private readonly CustomMousePlugin _plugin;
    private bool _isHydratingCursorThemeSelection;
    private int _cursorThemeSelectionChangeVersion;

    private static CultureInfo Culture => CustomMouseText.Culture;

    public CustomMouseSettingsControl(CustomMousePlugin plugin)
    {
        _plugin = plugin;
        WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);
        LoadCurrentValues();
    }

    private void BuildFallbackUi()
    {
        _pointerSpeedSlider = new Slider
        {
            Minimum = 1,
            Maximum = 20,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            Margin = new Thickness(0, 8, 0, 0)
        };
        AutomationProperties.SetAutomationId(_pointerSpeedSlider, "PointerSpeedSlider");
        _pointerSpeedSlider.ValueChanged += SettingsInputChanged;

        _swapButtonsCheckBox = new CheckBox
        {
            Content = CustomMouseText.SwapButtonsLabel,
            Margin = new Thickness(0, 14, 0, 0)
        };
        AutomationProperties.SetAutomationId(_swapButtonsCheckBox, "SwapButtonsCheckBox");
        _swapButtonsCheckBox.Checked += SettingsInputChanged;
        _swapButtonsCheckBox.Unchecked += SettingsInputChanged;

        _cursorThemeModeComboBox = new ComboBox
        {
            Margin = new Thickness(0, 8, 0, 0),
            MinHeight = 34
        };
        AutomationProperties.SetAutomationId(_cursorThemeModeComboBox, "CursorThemeModeComboBox");
        _cursorThemeModeComboBox.Items.Add(new ComboBoxItem { Content = CustomMouseText.CursorThemeModeAuto, Tag = "Auto" });
        _cursorThemeModeComboBox.Items.Add(new ComboBoxItem { Content = CustomMouseText.CursorThemeModeLight, Tag = "Light" });
        _cursorThemeModeComboBox.Items.Add(new ComboBoxItem { Content = CustomMouseText.CursorThemeModeDark, Tag = "Dark" });
        _cursorThemeModeComboBox.Items.Add(new ComboBoxItem { Content = CustomMouseText.CursorThemeModeWindowsDefault, Tag = "WindowsDefault" });
        _cursorThemeModeComboBox.SelectionChanged += CursorThemeModeComboBox_SelectionChanged;

        _statusTextBlock = new TextBlock
        {
            Text = CustomMouseText.ProfileReady,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = ResolveBrush("TextFillColorSecondaryBrush", SystemColors.ControlTextBrush)
        };
        AutomationProperties.SetAutomationId(_statusTextBlock, "CustomMouse_StatusText");

        _pointerPreviewValueTextBlock = CreateFallbackValueTextBlock();
        AutomationProperties.SetAutomationId(_pointerPreviewValueTextBlock, "CustomMousePointerPreviewText");
        _buttonLayoutValueTextBlock = CreateFallbackValueTextBlock();
        AutomationProperties.SetAutomationId(_buttonLayoutValueTextBlock, "CustomMouseButtonLayoutText");
        _cursorThemeValueTextBlock = CreateFallbackValueTextBlock();
        AutomationProperties.SetAutomationId(_cursorThemeValueTextBlock, "CustomMouseCursorThemeText");
        _pointerSpeedValueLabel = CreateFallbackValueTextBlock();

        var root = new StackPanel { Margin = new Thickness(20, 14, 20, 18) };
        AutomationProperties.SetAutomationId(root, "CustomMouseSettingsRoot");

        var overviewCard = CreateFallbackSurface(new Thickness(18, 16, 18, 16), new Thickness(0, 0, 0, 14));
        var overviewGrid = new Grid();
        overviewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        overviewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        overviewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var overviewIcon = CreateFallbackIconBadge(Wpf.Ui.Controls.SymbolRegular.Cursor24);
        Grid.SetColumn(overviewIcon, 0);
        overviewGrid.Children.Add(overviewIcon);

        var overviewTextPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var overviewTitle = new TextBlock
        {
            Text = CustomMouseText.SettingsOverviewTitle,
            FontSize = ResolveFontSize("PluginFontSizeSection", 15),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        overviewTitle.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        overviewTextPanel.Children.Add(overviewTitle);
        overviewTextPanel.Children.Add(CreateFallbackBodyText(CustomMouseText.WindowsSettingsDescription, new Thickness(0, 4, 0, 0)));
        Grid.SetColumn(overviewTextPanel, 1);
        overviewGrid.Children.Add(overviewTextPanel);

        var statusPill = new Border
        {
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(18, 0, 0, 0),
            CornerRadius = new CornerRadius(8),
            VerticalAlignment = VerticalAlignment.Center
        };
        statusPill.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");

        var statusPillPanel = new StackPanel { Orientation = Orientation.Horizontal };
        _statusIcon = new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24,
            FontSize = ResolveFontSize("PluginIconSizeMD", 18),
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = ResolveBrush("SystemFillColorSuccessBrush", ResolveBrush("SystemAccentColorPrimaryBrush", SystemColors.HighlightBrush))
        };
        statusPillPanel.Children.Add(_statusIcon);

        var readyText = new TextBlock
        {
            Text = CustomMouseText.ProfileReady,
            FontSize = ResolveFontSize("PluginFontSizeCaption", 12),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        readyText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        statusPillPanel.Children.Add(readyText);
        statusPill.Child = statusPillPanel;
        Grid.SetColumn(statusPill, 2);
        overviewGrid.Children.Add(statusPill);
        overviewCard.Child = overviewGrid;
        root.Children.Add(overviewCard);

        var metricsGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pointerMetric = CreateFallbackMetric(CustomMouseText.PointerPreviewLabel, _pointerPreviewValueTextBlock);
        Grid.SetColumn(pointerMetric, 0);
        metricsGrid.Children.Add(pointerMetric);

        var buttonMetric = CreateFallbackMetric(CustomMouseText.ButtonLayoutLabel, _buttonLayoutValueTextBlock);
        Grid.SetColumn(buttonMetric, 2);
        metricsGrid.Children.Add(buttonMetric);

        var cursorMetric = CreateFallbackMetric(CustomMouseText.CursorThemeStatusLabel, _cursorThemeValueTextBlock);
        Grid.SetColumn(cursorMetric, 4);
        metricsGrid.Children.Add(cursorMetric);
        root.Children.Add(metricsGrid);

        var settingsGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pointerCard = CreateFallbackSurface(new Thickness(18, 16, 18, 16), new Thickness(0));
        var pointerPanel = new StackPanel();
        var pointerHeader = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 14)
        };
        pointerHeader.Children.Add(CreateFallbackSymbol(Wpf.Ui.Controls.SymbolRegular.Cursor24, ResolveFontSize("PluginIconSizeMD", 18), new Thickness(0, 0, 10, 0)));
        pointerHeader.Children.Add(CreateFallbackSectionTitle(CustomMouseText.WindowsSettingsTitle));
        pointerPanel.Children.Add(pointerHeader);

        var speedHeader = new Grid();
        speedHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        speedHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        speedHeader.Children.Add(CreateFallbackLabel(CustomMouseText.PointerSpeedLabel));
        Grid.SetColumn(_pointerSpeedValueLabel, 1);
        speedHeader.Children.Add(_pointerSpeedValueLabel);
        pointerPanel.Children.Add(speedHeader);
        _pointerSpeedSlider.Margin = new Thickness(0, 10, 0, 0);
        pointerPanel.Children.Add(_pointerSpeedSlider);

        _swapButtonsCheckBox.Margin = new Thickness(0);
        var swapSurface = CreateFallbackSurface(new Thickness(12, 10, 12, 10), new Thickness(0, 18, 0, 0));
        swapSurface.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");
        swapSurface.Child = _swapButtonsCheckBox;
        pointerPanel.Children.Add(swapSurface);
        pointerCard.Child = pointerPanel;
        Grid.SetColumn(pointerCard, 0);
        settingsGrid.Children.Add(pointerCard);

        var cursorCard = CreateFallbackSurface(new Thickness(18, 16, 18, 16), new Thickness(0));
        var cursorPanel = new StackPanel();
        var cursorHeader = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10)
        };
        cursorHeader.Children.Add(CreateFallbackSymbol(Wpf.Ui.Controls.SymbolRegular.PaintBrush24, ResolveFontSize("PluginIconSizeMD", 18), new Thickness(0, 0, 10, 0)));
        cursorHeader.Children.Add(CreateFallbackSectionTitle(CustomMouseText.CursorThemeModeLabel));
        cursorPanel.Children.Add(cursorHeader);
        cursorPanel.Children.Add(CreateFallbackBodyText(CustomMouseText.CursorHint, new Thickness(0)));
        _cursorThemeModeComboBox.Margin = new Thickness(0, 14, 0, 0);
        cursorPanel.Children.Add(_cursorThemeModeComboBox);

        var cursorActionsPanel = new WrapPanel
        {
            Margin = new Thickness(0, 14, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var applyCursorButton = CreateFallbackButton(CustomMouseText.ApplyCursorThemeNowButton, "ApplyCursorThemeNowButton", ApplyCursorThemeNowButton_Click, false);
        applyCursorButton.Margin = new Thickness(0, 0, 8, 0);
        applyCursorButton.MinWidth = 196;
        cursorActionsPanel.Children.Add(applyCursorButton);
        var restoreCursorButton = CreateFallbackButton(CustomMouseText.RestoreWindowsDefaultButton, "RestoreWindowsDefaultButton", RestoreWindowsDefaultButton_Click, false);
        restoreCursorButton.Margin = new Thickness(0);
        restoreCursorButton.MinWidth = 196;
        cursorActionsPanel.Children.Add(restoreCursorButton);
        cursorPanel.Children.Add(cursorActionsPanel);
        cursorCard.Child = cursorPanel;
        Grid.SetColumn(cursorCard, 2);
        settingsGrid.Children.Add(cursorCard);
        root.Children.Add(settingsGrid);

        var actionBar = CreateFallbackSurface(new Thickness(16, 12, 16, 12), new Thickness(0));
        var actionGrid = new Grid();
        actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var infoIcon = CreateFallbackSymbol(Wpf.Ui.Controls.SymbolRegular.Info24, ResolveFontSize("PluginIconSizeMD", 18), new Thickness(0, 1, 10, 0));
        infoIcon.Foreground = ResolveBrush("TextFillColorTertiaryBrush", SystemColors.ControlTextBrush);
        Grid.SetColumn(infoIcon, 0);
        actionGrid.Children.Add(infoIcon);

        _applyProgressRing = new Wpf.Ui.Controls.ProgressRing
        {
            Width = 18,
            Height = 18,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            IsIndeterminate = true,
            Visibility = Visibility.Collapsed
        };
        var statusPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        statusPanel.Children.Add(_applyProgressRing);
        statusPanel.Children.Add(_statusTextBlock);
        Grid.SetColumn(statusPanel, 1);
        actionGrid.Children.Add(statusPanel);

        var mainActionsPanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(18, 0, 0, 0)
        };
        mainActionsPanel.Children.Add(CreateFallbackButton(CustomMouseText.SyncFromWindowsButton, "SyncFromWindowsButton", SyncFromWindowsButton_Click, false));
        mainActionsPanel.Children.Add(CreateFallbackButton(CustomMouseText.ReloadButton, "ReloadButton", ReloadButton_Click, false));
        mainActionsPanel.Children.Add(CreateFallbackButton(CustomMouseText.ApplyToWindowsButton, "ApplyToWindowsButton", ApplyButton_Click, true));
        Grid.SetColumn(mainActionsPanel, 2);
        actionGrid.Children.Add(mainActionsPanel);
        actionBar.Child = actionGrid;
        root.Children.Add(actionBar);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
    }

    private static Border CreateFallbackSurface(Thickness padding, Thickness margin)
    {
        var border = new Border
        {
            Padding = padding,
            Margin = margin,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1)
        };
        border.SetResourceReference(Border.BackgroundProperty, "ControlFillColorDefaultBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        return border;
    }

    private static TextBlock CreateFallbackSectionTitle(string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = ResolveFontSize("PluginFontSizeBody", 14),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        return textBlock;
    }

    private static TextBlock CreateFallbackBodyText(string text, Thickness margin)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            Margin = margin,
            FontSize = ResolveFontSize("PluginFontSizeCaption", 12),
            TextWrapping = TextWrapping.Wrap
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        return textBlock;
    }

    private static TextBlock CreateFallbackLabel(string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = ResolveFontSize("PluginFontSizeBody", 14),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        return textBlock;
    }

    private static TextBlock CreateFallbackValueTextBlock()
    {
        var textBlock = new TextBlock
        {
            MinWidth = 48,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = ResolveFontSize("PluginFontSizeBody", 14),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        return textBlock;
    }

    private static Border CreateFallbackIconBadge(Wpf.Ui.Controls.SymbolRegular symbol)
    {
        var icon = new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = symbol,
            FontSize = ResolveFontSize("PluginIconSizeLG", 24),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = ResolveBrush("TextOnAccentFillColorPrimaryBrush", Brushes.White)
        };

        return new Border
        {
            Width = 42,
            Height = 42,
            Margin = new Thickness(0, 0, 16, 0),
            CornerRadius = new CornerRadius(8),
            Background = ResolveBrush("SystemAccentColorPrimaryBrush", SystemColors.HighlightBrush),
            Child = icon
        };
    }

    private static Wpf.Ui.Controls.SymbolIcon CreateFallbackSymbol(
        Wpf.Ui.Controls.SymbolRegular symbol,
        double fontSize,
        Thickness margin)
    {
        return new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = symbol,
            FontSize = fontSize,
            Margin = margin,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = ResolveBrush("SystemAccentColorPrimaryBrush", SystemColors.HighlightBrush)
        };
    }

    private static Border CreateFallbackMetric(string label, TextBlock valueTextBlock)
    {
        valueTextBlock.Margin = new Thickness(0, 5, 0, 0);
        valueTextBlock.MinWidth = 0;
        valueTextBlock.TextAlignment = TextAlignment.Left;
        valueTextBlock.FontSize = ResolveFontSize("PluginFontSizeSection", 15);

        var labelText = CreateFallbackBodyText(label, new Thickness(0));
        labelText.FontSize = ResolveFontSize("PluginFontSizeCaption", 12);
        labelText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");

        var panel = new StackPanel();
        panel.Children.Add(labelText);
        panel.Children.Add(valueTextBlock);

        var card = CreateFallbackSurface(new Thickness(14, 12, 14, 12), new Thickness(0));
        card.Child = panel;
        return card;
    }

    private static Button CreateFallbackButton(string text, string automationId, RoutedEventHandler handler, bool primary)
    {
        var button = new Button
        {
            Content = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                MaxWidth = 170
            },
            MinWidth = 96,
            MinHeight = 34,
            Margin = new Thickness(8, 0, 0, 8),
            Padding = new Thickness(12, 6, 12, 6),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetAutomationId(button, automationId);
        AutomationProperties.SetName(button, text);
        if (primary)
        {
            button.SetResourceReference(Control.BackgroundProperty, "SystemAccentColorPrimaryBrush");
            button.SetResourceReference(Control.ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
        }
        button.Click += handler;
        return button;
    }

    private void LoadCurrentValues()
    {
        if (_pointerSpeedSlider is null || _swapButtonsCheckBox is null)
        {
            return;
        }

        Interlocked.Increment(ref _cursorThemeSelectionChangeVersion);
        _isHydratingCursorThemeSelection = true;
        try
        {
            _pointerSpeedSlider.Value = _plugin.Settings.WindowsPointerSpeed;
            _swapButtonsCheckBox.IsChecked = _plugin.Settings.SwapButtons;
            LoadCursorThemeMode();
            UpdateSummaryCards();
            UpdatePointerSpeedValueLabel();
        }
        finally
        {
            _isHydratingCursorThemeSelection = false;
        }
    }

    private void LoadCursorThemeMode()
    {
        if (_cursorThemeModeComboBox is null)
        {
            return;
        }

        var modeTag = _plugin.Settings.CursorThemeMode switch
        {
            CursorThemeMode.Light => "Light",
            CursorThemeMode.Dark => "Dark",
            CursorThemeMode.WindowsDefault => "WindowsDefault",
            _ => "Auto"
        };

        foreach (var item in _cursorThemeModeComboBox.Items)
        {
            if (item is ComboBoxItem comboItem && comboItem.Tag as string == modeTag)
            {
                _cursorThemeModeComboBox.SelectedItem = comboItem;
                break;
            }
        }
    }

    private void SettingsInputChanged(object sender, RoutedEventArgs e)
    {
        UpdateSummaryCards();
        UpdatePointerSpeedValueLabel();
    }

    private void UpdatePointerSpeedValueLabel()
    {
        if (_pointerSpeedValueLabel is null || _pointerSpeedSlider is null)
        {
            return;
        }

        _pointerSpeedValueLabel.Text = string.Format(Culture, "{0}/20", (int)Math.Round(_pointerSpeedSlider.Value));
    }

    private async void CursorThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_isHydratingCursorThemeSelection)
            {
                return;
            }

            if (_cursorThemeModeComboBox?.SelectedItem is not ComboBoxItem selected)
            {
                return;
            }

            var selectionVersion = Interlocked.Increment(ref _cursorThemeSelectionChangeVersion);
            var mode = (selected.Tag as string) switch
            {
                "Light" => CursorThemeMode.Light,
                "Dark" => CursorThemeMode.Dark,
                "WindowsDefault" => CursorThemeMode.WindowsDefault,
                _ => CursorThemeMode.Auto
            };

            var applied = await _plugin.SetCursorThemeModeAsync(mode).ConfigureAwait(true);
            if (selectionVersion != Volatile.Read(ref _cursorThemeSelectionChangeVersion))
            {
                return;
            }

            if (applied)
            {
                var modeText = mode switch
                {
                    CursorThemeMode.Light => CustomMouseText.CursorThemeModeLight,
                    CursorThemeMode.Dark => CustomMouseText.CursorThemeModeDark,
                    CursorThemeMode.WindowsDefault => CustomMouseText.CursorThemeModeWindowsDefault,
                    _ => CustomMouseText.CursorThemeModeAuto
                };
                SetStatus(string.Format(CustomMouseText.CursorThemeModeApplied, modeText), false);
            }
            else
            {
                SetStatus(CustomMouseText.StatusCursorApplyFailed, true);
                LoadCursorThemeMode();
            }

            UpdateSummaryCards();
        }
        catch (Exception ex)
        {
            SetStatus($"{CustomMouseText.StatusCursorApplyFailed}: {ex.Message}", true);
            LoadCursorThemeMode();
            PluginLog.Trace($"CursorThemeModeComboBox_SelectionChanged error: {ex.Message}", ex);
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var applyButton = sender as Button;
        SetApplyBusy(true, applyButton);
        try
        {
            if (_pointerSpeedSlider is null || _swapButtonsCheckBox is null || _statusTextBlock is null)
            {
                return;
            }

            var speed = (int)Math.Round(_pointerSpeedSlider.Value);
            var swapButtons = _swapButtonsCheckBox.IsChecked == true;
            var originalSpeed = _plugin.Settings.WindowsPointerSpeed;

            if (!_plugin.SetWindowsPointerSpeed(speed))
            {
                SetStatus(CustomMouseText.StatusApplyPointerFail, true);
                LoadCurrentValues();
                return;
            }

            if (!_plugin.SetSwapButtons(swapButtons))
            {
                if (_plugin.Settings.WindowsPointerSpeed != originalSpeed)
                {
                    _plugin.SetWindowsPointerSpeed(originalSpeed);
                }

                SetStatus(CustomMouseText.StatusApplySwapFail, true);
                LoadCurrentValues();
                return;
            }

            await _plugin.SaveSettingsAsync().ConfigureAwait(true);
            SetStatus(CustomMouseText.StatusWindowsApplied, false);
            UpdateSummaryCards();
        }
        catch (Exception ex)
        {
            SetStatus($"{CustomMouseText.ApplyFailedPrefix} {ex.Message}", true);
            PluginLog.Trace($"ApplyButton_Click error: {ex.Message}", ex);
        }
        finally
        {
            SetApplyBusy(false, applyButton);
        }
    }

    private async void ApplyCursorThemeNowButton_Click(object sender, RoutedEventArgs e)
    {
        var applyButton = sender as Button;
        SetApplyBusy(true, applyButton);
        try
        {
            if (_statusTextBlock is null)
            {
                return;
            }

            var applied = await _plugin.ApplyCursorStyleForCurrentThemeAsync().ConfigureAwait(true);
            SetStatus(
                applied ? CustomMouseText.FormatCursorApplied(_plugin.Settings.LastAppliedTheme) : CustomMouseText.StatusCursorApplyFailed,
                !applied);
            UpdateSummaryCards();
        }
        catch (Exception ex)
        {
            SetStatus($"{CustomMouseText.ApplyCursorThemeFailedPrefix} {ex.Message}", true);
            PluginLog.Trace($"ApplyCursorThemeNowButton_Click error: {ex.Message}", ex);
        }
        finally
        {
            SetApplyBusy(false, applyButton);
        }
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_statusTextBlock is null)
        {
            return;
        }

        LoadCurrentValues();
        SetStatus(CustomMouseText.StatusReloaded, false);
        UpdateSummaryCards();
    }

    private void SyncFromWindowsButton_Click(object sender, RoutedEventArgs e)
    {
        _plugin.ReloadSettingsFromSystem();
        LoadCurrentValues();
        SetStatus(CustomMouseText.StatusSyncedFromWindows, false);
        UpdateSummaryCards();
    }

    private async void RestoreWindowsDefaultButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var restored = await _plugin.RestoreWindowsDefaultCursorThemeAsync().ConfigureAwait(true);
            if (!restored)
            {
                SetStatus(CustomMouseText.StatusRestoreWindowsDefaultFailed, true);
                return;
            }

            LoadCurrentValues();
            SetStatus(CustomMouseText.StatusWindowsDefaultRestored, false);
            UpdateSummaryCards();
        }
        catch (Exception ex)
        {
            SetStatus($"{CustomMouseText.StatusRestoreWindowsDefaultFailed} {ex.Message}", true);
            PluginLog.Trace($"RestoreWindowsDefaultButton_Click error: {ex.Message}", ex);
        }
    }

    private void SetApplyBusy(bool isBusy, Button? button)
    {
        if (button is not null)
        {
            button.IsEnabled = !isBusy;
        }

        if (_applyProgressRing is not null)
        {
            _applyProgressRing.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        }

        if (isBusy && _statusTextBlock is not null)
        {
            _statusTextBlock.Text = CustomMouseText.StatusApplying;
            _statusTextBlock.Foreground = ResolveBrush("TextFillColorSecondaryBrush", SystemColors.ControlTextBrush);
        }
    }

    private void SetStatus(string text, bool isError)
    {
        if (_statusTextBlock is null)
        {
            return;
        }

        _statusTextBlock.Text = text;
        _statusTextBlock.Foreground = isError
            ? ResolveBrush("SystemFillColorCriticalBrush", SystemColors.ControlTextBrush)
            : ResolveBrush("SystemFillColorSuccessBrush", SystemColors.ControlTextBrush);

        if (_statusIcon is not null)
        {
            _statusIcon.Symbol = isError
                ? Wpf.Ui.Controls.SymbolRegular.ErrorCircle24
                : Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24;
            _statusIcon.Foreground = _statusTextBlock.Foreground;
        }
    }

    private static Brush ResolveBrush(string resourceKey, Brush fallback)
    {
        return Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;
    }

    private static double ResolveFontSize(string resourceKey, double fallback)
    {
        return Application.Current?.TryFindResource(resourceKey) is double size ? size : fallback;
    }

    private void UpdateSummaryCards()
    {
        if (_pointerPreviewValueTextBlock != null)
        {
            var speed = _pointerSpeedSlider is null ? _plugin.Settings.WindowsPointerSpeed : (int)Math.Round(_pointerSpeedSlider.Value);
            _pointerPreviewValueTextBlock.Text = string.Format(Culture, "{0}/20", speed);
        }

        _buttonLayoutValueTextBlock?.Text = _swapButtonsCheckBox?.IsChecked == true
                ? CustomMouseText.SwappedButtonsState
                : CustomMouseText.StandardButtonsState;

        if (_cursorThemeValueTextBlock != null)
        {
            var mode = _plugin.Settings.CursorThemeMode;
            _cursorThemeValueTextBlock.Text = mode switch
            {
                CursorThemeMode.Light => CustomMouseText.CursorThemeModeLight,
                CursorThemeMode.Dark => CustomMouseText.CursorThemeModeDark,
                CursorThemeMode.WindowsDefault => CustomMouseText.CursorThemeModeWindowsDefault,
                _ => CustomMouseText.CursorThemeModeAuto
            };
        }
    }

    private CursorThemeMode GetSelectedCursorThemeMode()
    {
        if (_cursorThemeModeComboBox?.SelectedItem is ComboBoxItem selected)
        {
            return (selected.Tag as string) switch
            {
                "Light" => CursorThemeMode.Light,
                "Dark" => CursorThemeMode.Dark,
                "WindowsDefault" => CursorThemeMode.WindowsDefault,
                _ => CursorThemeMode.Auto
            };
        }

        return _plugin.Settings.CursorThemeMode;
    }
}
