using System;
using System.Linq;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.WPF.ViewModels;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Pages
{
public partial class MacroPage
{
    private readonly MacroViewModel _viewModel = new(IoCContainer.Resolve<MacroController>());

    public MacroPage()
    {
        Initialized += MacroPage_Initialized;

        InitializeComponent();
        DataContext = _viewModel;
    }

    private void MacroPage_Initialized(object? sender, EventArgs e)
    {
        _viewModel.LoadState();

        var zeroNumberButton = _numberPad.Children.OfType<Button>().Last();
        Reload(zeroNumberButton);
    }

    private void EnableMacroToggle_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SetEnabled(_enableMacroToggle.IsChecked ?? false);
    }

    private void NumberPadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        _numberPad.Children
            .OfType<Button>()
            .ForEach(b => b.Appearance = ControlAppearance.Secondary);

        Reload(button);
    }

    private void Reload(Button button)
    {
        button.Appearance = ControlAppearance.Primary;

        var key = Convert.ToUInt64((string)button.Tag, 16);
        _viewModel.SelectKey(key);
        _sequenceControl.Set(new(MacroSource.Keyboard, key));
    }
}
}
