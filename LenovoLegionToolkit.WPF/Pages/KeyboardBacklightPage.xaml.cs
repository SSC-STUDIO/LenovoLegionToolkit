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

    public KeyboardBacklightPage()
    {
        InitializeComponent();
        _titleTextBlock.Visibility = Visibility.Collapsed;
    }

    private async void KeyboardBacklightPage_Initialized(object? sender, EventArgs e)
    {
        _titleTextBlock.Visibility = Visibility.Collapsed;

        await _viewModel.DetectKeyboardTypeCommand.ExecuteAsync(null);

        if (_viewModel.IsSpectrumSupported)
        {
            _titleTextBlock.Visibility = Visibility.Visible;
            var control = new SpectrumKeyboardBacklightControl();
            _content.Children.Add(control);
        }
        else if (_viewModel.IsRGBSupported)
        {
            _titleTextBlock.Visibility = Visibility.Visible;
            var control = new RGBKeyboardBacklightControl();
            _content.Children.Add(control);
        }
        else
        {
            if (KeyboardBacklightViewModel.ShouldKeepUnsupportedNavigationItems())
            {
                _titleTextBlock.Visibility = Visibility.Visible;
                _noKeyboardsText.Visibility = Visibility.Visible;
            }
            else
            {
                _titleTextBlock.Visibility = Visibility.Collapsed;
                _content.Visibility = Visibility.Collapsed;
            }
        }

        _loader.IsLoading = false;
    }

    public static async Task<bool> IsSupportedAsync() => await KeyboardBacklightViewModel.IsSupportedAsync();
}
}
