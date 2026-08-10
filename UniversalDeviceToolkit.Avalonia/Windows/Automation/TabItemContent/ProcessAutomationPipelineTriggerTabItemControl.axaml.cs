using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using Button = UniversalDeviceToolkit.Avalonia.Controls.Button;

namespace UniversalDeviceToolkit.Avalonia.Windows.Automation.TabItemContent
{
public partial class ProcessAutomationPipelineTriggerTabItemControl : global::Avalonia.Controls.UserControl, IAutomationPipelineTriggerTabItemContent<IProcessesAutomationPipelineTrigger>
{
    private static string T(string key, string fallback) => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    private readonly IProcessesAutomationPipelineTrigger _trigger;
    private readonly List<ProcessInfo> _processes;

    public ProcessAutomationPipelineTriggerTabItemControl(IProcessesAutomationPipelineTrigger trigger)
    {
        _trigger = trigger;
        _processes = [.. trigger.Processes];

        InitializeComponent();
    }

    public IProcessesAutomationPipelineTrigger GetTrigger() => _trigger.DeepCopy([.. _processes]);

    private void ProcessAutomationPipelineTriggerTabItemControl_Initialized(object? sender, EventArgs e)
    {
        // AVALONIA: removed WPF CommandBindings/KeyGesture (Ctrl+C / Ctrl+V handled by the
        // toolbar buttons only).
        Refresh();
    }

    private void Item_OnDelete(object? sender, EventArgs e)
    {
        if (sender is not ListItem listItem)
            return;

        _processes.Remove(listItem.Process);
        Refresh();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new System.Windows.Forms.OpenFileDialog
        {
            Title = Resource.Open,
            InitialDirectory = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}",
            Filter = T("Common_ExecutableFileDialogFilter", "Exe Files (.exe)|*.exe"),
            CheckFileExists = true,
        };

        if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        var processInfo = ProcessInfo.FromPath(ofd.FileName);
        if (_processes.Contains(processInfo))
            return;

        _processes.Add(processInfo);
        Refresh();
    }

    private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
    {
        _processes.Clear();
        Refresh();
    }

    private void CopyShortcut(object sender, RoutedEventArgs e)
    {
        try
        {
            ClipboardExtensions.SetProcesses(_processes);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't copy to clipboard", ex);
        }
    }

    private void PasteShortcut(object sender, RoutedEventArgs e)
    {
        try
        {
            var processes = ClipboardExtensions.GetProcesses()
                .Where(p => !_processes.Contains(p))
                .ToArray();
            if (processes.Length == 0)
                return;

            _processes.AddRange(processes);
            Refresh();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't paste from clipboard", ex);
        }
    }

    private void Refresh()
    {
        _list.Items.Clear();
        foreach (var process in _processes.OrderBy(p => p))
        {
            var item = new ListItem(process);
            item.OnDelete += Item_OnDelete;
            _list.Items.Add(item);
        }
    }

    private class ListItem : UserControl
    {
        private readonly Grid _grid = new()
        {
            Margin = new(8, 4, 0, 16),
            ColumnDefinitions =
            {
                new() { Width = GridLength.Auto },
                new() { Width = new(1, GridUnitType.Star) },
                new() { Width = GridLength.Auto },
            },
            RowDefinitions =
            {
                new() { Height = GridLength.Auto },
                new() { Height = GridLength.Auto },
            },
        };

        private readonly Image _icon = new()
        {
            Width = 24,
            Height = 24,
            Margin = new(0, 0, 8, 0),
        };

        private readonly TextBlock _nameTextBox = new()
        {
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        private readonly TextBlock _pathTextBox = new()
        {
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new(0, 4, 0, 0),
        };

        private readonly Button _deleteButton = new()
        {
            Icon = new SymbolIcon { Symbol = SymbolRegular.Delete24 },
            FontSize = 18,
            Margin = new(8, 0, 0, 0),
        };

        public ProcessInfo Process { get; }

        public event EventHandler? OnDelete;

        public ListItem(ProcessInfo process)
        {
            Process = process;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _icon.Source = ImageSourceExtensions.ApplicationIcon(Process.ExecutablePath) ?? ImageSourceExtensions.FromResource("Assets/Logo.png");
            _nameTextBox.Text = Process.Name;
            _pathTextBox.Text = Process.ExecutablePath;

            ToolTip.SetTip(_pathTextBox, Process.ExecutablePath);

            _pathTextBox.SetResourceReference(ForegroundProperty, "TextFillColorSecondaryBrush");

            _deleteButton.Click += (_, _) => OnDelete?.Invoke(this, EventArgs.Empty);

            Grid.SetColumn(_icon, 0);
            Grid.SetRow(_icon, 0);
            Grid.SetRowSpan(_icon, 2);

            Grid.SetColumn(_nameTextBox, 1);
            Grid.SetRow(_nameTextBox, 0);

            Grid.SetColumn(_pathTextBox, 1);
            Grid.SetRow(_pathTextBox, 1);

            Grid.SetColumn(_deleteButton, 2);
            Grid.SetRow(_deleteButton, 0);
            Grid.SetRowSpan(_deleteButton, 2);

            _grid.Children.Add(_icon);
            _grid.Children.Add(_nameTextBox);
            _grid.Children.Add(_pathTextBox);
            _grid.Children.Add(_deleteButton);

            Content = _grid;
        }
    }
}
}
