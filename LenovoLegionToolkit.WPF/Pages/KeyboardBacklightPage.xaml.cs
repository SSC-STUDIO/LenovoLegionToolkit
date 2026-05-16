using System;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.WPF.Controls.KeyboardBacklight.RGB;
using LenovoLegionToolkit.WPF.Controls.KeyboardBacklight.Spectrum;
using LenovoLegionToolkit.WPF.ViewModels;

namespace LenovoLegionToolkit.WPF.Pages
{
public partial class KeyboardBacklightPage
{
    private readonly KeyboardBacklightViewModel _viewModel = new();

    public KeyboardBacklightPage() => InitializeComponent();

    private async void KeyboardBacklightPage_Initialized(object? sender, EventArgs e)
    {
        _titleTextBlock.Visibility = Visibility.Collapsed;

        await Task.Delay(TimeSpan.FromSeconds(1));

        _titleTextBlock.Visibility = Visibility.Visible;

        await _viewModel.DetectKeyboardTypeCommand.ExecuteAsync(null);

        if (_viewModel.IsSpectrumSupported)
        {
            var control = new SpectrumKeyboardBacklightControl();
            _content.Children.Add(control);
        }
        else if (_viewModel.IsRGBSupported)
        {
            var control = new RGBKeyboardBacklightControl();
            _content.Children.Add(control);
        }
        else
        {
            _noKeyboardsText.Visibility = Visibility.Visible;
        }

        _loader.IsLoading = false;
    }

    public static async Task<bool> IsSupportedAsync() => await KeyboardBacklightViewModel.IsSupportedAsync();
}
}
