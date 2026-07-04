using System;
using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.WPF.Controls.KeyboardBacklight.RGB;
using UniversalDeviceToolkit.WPF.Controls.KeyboardBacklight.Spectrum;
using UniversalDeviceToolkit.WPF.ViewModels;

namespace UniversalDeviceToolkit.WPF.Pages
{
public partial class KeyboardBacklightPage
{
    private readonly KeyboardBacklightViewModel _viewModel = new();

    public KeyboardBacklightPage()
    {
        InitializeComponent();
        _titleTextBlock.Visibility = Visibility.Collapsed;
        Unloaded += KeyboardBacklightPage_Unloaded;
    }

    private void KeyboardBacklightPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _content.Children.Clear();
    }

    private async void KeyboardBacklightPage_Initialized(object? sender, EventArgs e)
    {
        try
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
                _titleTextBlock.Visibility = Visibility.Collapsed;
                _content.Visibility = Visibility.Collapsed;
            }

            _loader.IsLoading = false;
        }
        catch (Exception ex)
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error initializing keyboard backlight page.", ex);
        }
    }

    public static async Task<bool> IsSupportedAsync() => await KeyboardBacklightViewModel.IsSupportedAsync();
}
}

