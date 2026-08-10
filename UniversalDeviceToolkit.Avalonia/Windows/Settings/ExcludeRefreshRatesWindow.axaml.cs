using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows.Settings
{
public partial class ExcludeRefreshRatesWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private readonly RefreshRateFeature _feature = IoCContainer.Resolve<RefreshRateFeature>();
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();

    public ExcludeRefreshRatesWindow()
    {
        InitializeComponent();

        PropertyChanged += ExcludeRefreshRatesWindow_PropertyChanged;
    }

    private async void ExcludeRefreshRatesWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Visual.IsVisibleProperty)
            return;

        try
        {
            if (IsVisible)
                await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(ExcludeRefreshRatesWindow_PropertyChanged)}.", ex);
        }
    }

    private async Task RefreshAsync()
    {
        _loader.IsLoading = true;

        var loadingTask = Task.Delay(500);

        var refreshRates = await _feature.GetAllStatesAsync();
        var excluded = _settings.Store.ExcludedRefreshRates;

        if (refreshRates.IsEmpty())
        {
            await Task.Delay(500);

            var result = await MessageBoxHelper.ShowAsync(this,
                Resource.ExcludeRefreshRatesWindow_NoRefreshRatesFound_Title,
                Resource.ExcludeRefreshRatesWindow_NoRefreshRatesFound_Message,
                Resource.TryAgain,
                Resource.Cancel);

            if (result)
                await RefreshAsync();
            else
                Close();

            return;
        }

        _list.Items.Clear();

        var items = refreshRates
            .Union(excluded)
            .Distinct()
            .OrderBy(rr => rr.Frequency);

        foreach (var refreshRate in items)
        {
            var item = new ListItem(refreshRate)
            {
                IsChecked = !excluded.Contains(refreshRate)
            };
            _list.Items.Add(item);
        }

        await loadingTask;

        _loader.IsLoading = false;
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var excludedRefreshRates = _list.Items.OfType<ListItem>()
            .Where(li => !li.IsChecked)
            .Select(li => li.RefreshRate);

        _settings.Store.ExcludedRefreshRates.Clear();
        _settings.Store.ExcludedRefreshRates.AddRange(excludedRefreshRates);
        _settings.SynchronizeStore();

        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private class ListItem : UserControl
    {
        private readonly Grid _grid = new()
        {
            Margin = new(8, 4, 0, 16),
            ColumnDefinitions =
            {
                new() { Width = new(32, GridUnitType.Pixel) },
                new() { Width = new(1, GridUnitType.Star) },
            },
        };

        private readonly CheckBox _checkBox = new();

        private readonly TextBlock _nameTextBox = new()
        {
            FlowDirection = FlowDirection.LeftToRight,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };

        public RefreshRate RefreshRate { get; }

        public bool IsChecked
        {
            get => _checkBox.IsChecked ?? false;
            set => _checkBox.IsChecked = value;
        }

        public ListItem(RefreshRate refreshRate)
        {
            RefreshRate = refreshRate;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _nameTextBox.Text = RefreshRate.DisplayName;

            Grid.SetColumn(_checkBox, 0);
            Grid.SetColumn(_nameTextBox, 1);

            _grid.Children.Add(_checkBox);
            _grid.Children.Add(_nameTextBox);

            AutomationProperties.SetLabeledBy(_checkBox, _nameTextBox);

            Content = _grid;
        }
    }
}
}
