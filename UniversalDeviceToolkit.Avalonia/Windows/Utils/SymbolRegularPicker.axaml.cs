using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Utils;
using Button = UniversalDeviceToolkit.Avalonia.Controls.Button;

namespace UniversalDeviceToolkit.Avalonia.Windows.Utils
{
public partial class SymbolRegularPicker : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private readonly DebounceDispatcher _debouncer = new();

    private readonly TaskCompletionSource<SymbolRegular?> _tcs = new();

    public Task<SymbolRegular?> SymbolRegularTask => _tcs.Task;

    public SymbolRegularPicker()
    {
        InitializeComponent();
    }

    private void SymbolRegularPicker_Loaded(object sender, RoutedEventArgs e) => Refresh();

    private void SymbolRegularPicker_Closing(object? sender, WindowClosingEventArgs e) => _tcs.TrySetCanceled();

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _debouncer.Debounce(300, Refresh);
    }

    private void ItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        _tcs.TrySetResult(button.Icon is SymbolIcon symbolIcon ? symbolIcon.Symbol : SymbolRegular.Empty);
        Close();
    }

    private void DefaultButton_Click(object sender, RoutedEventArgs e)
    {
        _tcs.TrySetResult(null);
        Close();
    }

    private void Refresh()
    {
        _itemsControl.Items.Clear();

        var items = Enum.GetNames<SymbolRegular>()
                .Where(s => s.EndsWith("24", StringComparison.CurrentCultureIgnoreCase))
                .Where(s => s.Contains(_filterTextBox.Text, StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(s => s)
                .ToArray();

        foreach (var item in items)
        {
            var button = new Button()
            {
                Icon = new SymbolIcon { Symbol = Enum.Parse<SymbolRegular>(item) },
                FontSize = 32,
                Width = 80,
                Height = 80,
                Margin = new(0, 0, 4, 4)
            };
            button.Click += ItemButton_Click;
            _itemsControl.Items.Add(button);
        }
    }
}
}
