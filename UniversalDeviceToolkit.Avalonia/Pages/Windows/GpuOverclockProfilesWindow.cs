#if WINDOWS
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

/// <summary>Native Avalonia counterpart of WPF's discrete-GPU overclock profile editor.</summary>
internal sealed class GpuOverclockProfilesWindow : Window
{
    private readonly GPUOverclockController _controller = IoCContainer.Resolve<GPUOverclockController>();
    private readonly ComboBox _profiles = new() { MinWidth = 180 };
    private readonly TextBox _profileName = new() { MinWidth = 180 };
    private readonly NumericUpDown _core = new() { MinWidth = 120, Increment = 1 };
    private readonly NumericUpDown _memory = new() { MinWidth = 120, Increment = 1 };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private Guid _activeProfileId;
    private bool _isRefreshing;

    public GpuOverclockProfilesWindow()
    {
        Title = Get("OverclockDiscreteGPUSettingsWindow_Title", "GPU overclock settings");
        Width = 560;
        MinWidth = 480;
        MaxWidth = 720;
        MinHeight = 420;
        MaxHeight = 760;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        AutomationProperties.SetAutomationId(this, "AvaloniaGpuOverclockProfilesWindow");
        AutomationProperties.SetName(this, Title);

        var coreLimit = GPUOverclockController.GetMaxCoreDeltaMhz();
        var memoryLimit = GPUOverclockController.GetMaxMemoryDeltaMhz();
        _core.Minimum = -coreLimit;
        _core.Maximum = coreLimit;
        _memory.Minimum = -memoryLimit;
        _memory.Maximum = memoryLimit;
        AutomationProperties.SetAutomationId(_profiles, "AvaloniaGpuOverclockProfiles");
        AutomationProperties.SetAutomationId(_profileName, "AvaloniaGpuOverclockProfileName");
        AutomationProperties.SetAutomationId(_core, "AvaloniaGpuOverclockCoreOffset");
        AutomationProperties.SetAutomationId(_memory, "AvaloniaGpuOverclockMemoryOffset");
        _profiles.SelectionChanged += OnProfileChanged;

        var root = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 12,
            FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
        };
        root.Children.Add(new LocalizedTextBlock
        {
            Text = Title,
            FontSize = 20,
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        });
        root.Children.Add(CreateProfileEditor());
        root.Children.Add(CreateOffsetEditor(
            Get("OverclockDiscreteGPUSettingsWindow_CoreFrequencyOffset_Title", "Core frequency offset"),
            _core));
        root.Children.Add(CreateOffsetEditor(
            Get("OverclockDiscreteGPUSettingsWindow_MemoryFrequencyOffset_Title", "Memory frequency offset"),
            _memory));
        _status.Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush");
        AutomationProperties.SetAutomationId(_status, "AvaloniaGpuOverclockProfileStatus");
        root.Children.Add(_status);
        root.Children.Add(CreateActions());
        Content = new ScrollViewer { Content = root };
        Loaded += (_, _) => RefreshProfiles();
    }

    private Control CreateProfileEditor()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto"), ColumnSpacing = 8 };
        grid.Children.Add(_profiles);
        var rename = Button(Get("Rename", "Rename"), "AvaloniaGpuOverclockRename", RenameProfile);
        Grid.SetColumn(rename, 1);
        grid.Children.Add(rename);
        var delete = Button(Get("Delete", "Delete"), "AvaloniaGpuOverclockDelete", DeleteProfile);
        Grid.SetColumn(delete, 2);
        grid.Children.Add(delete);
        var add = Button(Get("Add", "Add"), "AvaloniaGpuOverclockAdd", AddProfile);
        Grid.SetColumn(add, 3);
        grid.Children.Add(add);

        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new LocalizedTextBlock
        {
            Text = Get("StatusTrayPopup_Preset", "Preset"),
            FontWeight = FontWeight.Medium,
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        });
        panel.Children.Add(grid);
        panel.Children.Add(_profileName);
        return panel;
    }

    private Control CreateOffsetEditor(string title, NumericUpDown editor)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        grid.Children.Add(new LocalizedTextBlock
        {
            Text = title,
            VerticalAlignment = VerticalAlignment.Center,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
            MinWidth = 0,
        });
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);
        return grid;
    }

    private Control CreateActions()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };
        panel.Children.Add(Button(Get("Save", "Save"), "AvaloniaGpuOverclockSave", Save));
        panel.Children.Add(Button(Get("Apply", "Apply"), "AvaloniaGpuOverclockApply", ApplyAsync));
        panel.Children.Add(Button(Get("ApplyAndClose", "Apply and close"), "AvaloniaGpuOverclockApplyClose", ApplyAndCloseAsync));
        return panel;
    }

    private void RefreshProfiles()
    {
        _isRefreshing = true;
        try
        {
            var profiles = _controller.GetProfiles()
                .OrderBy(pair => pair.Value.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(pair => new ProfileChoice(pair.Key, pair.Value.Name, pair.Value.Info))
                .ToArray();
            _activeProfileId = _controller.GetActiveProfileId();
            _profiles.ItemsSource = profiles;
            _profiles.SelectedItem = profiles.FirstOrDefault(profile => profile.Id == _activeProfileId);
            if (_profiles.SelectedItem is ProfileChoice current)
                RenderProfile(current);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void OnProfileChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing || _profiles.SelectedItem is not ProfileChoice selected || selected.Id == _activeProfileId)
            return;

        SaveProfile();
        _controller.SetActiveProfile(selected.Id);
        _activeProfileId = selected.Id;
        RenderProfile(selected);
        RefreshProfiles();
    }

    private void RenderProfile(ProfileChoice profile)
    {
        _profileName.Text = profile.Name;
        _core.Value = profile.Info.CoreDeltaMhz;
        _memory.Value = profile.Info.MemoryDeltaMhz;
    }

    private void AddProfile()
    {
        SaveProfile();
        var name = string.IsNullOrWhiteSpace(_profileName.Text)
            ? Get("GPUOverclockSettings_DefaultProfileName", "Custom")
            : _profileName.Text.Trim();
        _activeProfileId = _controller.AddProfile(name, CurrentInfo());
        RefreshProfiles();
    }

    private void RenameProfile()
    {
        if (string.IsNullOrWhiteSpace(_profileName.Text))
            return;
        _controller.RenameProfile(_activeProfileId, _profileName.Text.Trim());
        RefreshProfiles();
    }

    private void DeleteProfile()
    {
        _controller.DeleteProfile(_activeProfileId);
        RefreshProfiles();
    }

    private void Save()
    {
        var (enabled, _) = _controller.GetState();
        _controller.SaveState(enabled, _activeProfileId, CurrentInfo());
        _status.Text = Get("Dashboard_GpuOverclockSaved", "GPU overclock settings saved.");
    }

    private async Task ApplyAsync()
    {
        Save();
        await _controller.ApplyStateAsync().ConfigureAwait(true);
        _status.Text = Get("Dashboard_GpuOverclockSaved", "GPU overclock settings applied.");
    }

    private async Task ApplyAndCloseAsync()
    {
        await ApplyAsync().ConfigureAwait(true);
        Close();
    }

    private void SaveProfile() => _controller.SaveProfile(_activeProfileId, CurrentInfo());

    private GPUOverclockInfo CurrentInfo() => new((int)(_core.Value ?? 0), (int)(_memory.Value ?? 0));

    private static Button Button(string text, string automationId, Action action)
    {
        var button = new Button { Content = text, MinWidth = 76 };
        AutomationProperties.SetAutomationId(button, automationId);
        AutomationProperties.SetName(button, text);
        button.Click += (_, _) => action();
        return button;
    }

    private static Button Button(string text, string automationId, Func<Task> action)
    {
        var button = new Button { Content = text, MinWidth = 76 };
        AutomationProperties.SetAutomationId(button, automationId);
        AutomationProperties.SetName(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);

    private static T GetResource<T>(string key) where T : class =>
        Application.Current?.TryGetResource(key, out var value) == true && value is T resource
            ? resource
            : (T)(object)new SolidColorBrush(Colors.Gray);

    private sealed record ProfileChoice(Guid Id, string Name, GPUOverclockInfo Info)
    {
        public override string ToString() => Name;
    }
}
#endif
