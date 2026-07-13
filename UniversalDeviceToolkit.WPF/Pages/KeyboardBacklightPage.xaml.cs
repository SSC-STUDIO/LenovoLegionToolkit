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
    private bool _isInitializing;

    public KeyboardBacklightPage()
    {
        InitializeComponent();
        _titleTextBlock.Visibility = Visibility.Collapsed;
        Loaded += KeyboardBacklightPage_Loaded;
        Unloaded += KeyboardBacklightPage_Unloaded;
    }

    private async void KeyboardBacklightPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || _content.Children.Count > 0)
            return;

        _isInitializing = true;
        _loader.IsLoading = true;
        _content.Visibility = Visibility.Visible;

        try
        {
            await InitializeKeyboardBacklightAsync();
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void KeyboardBacklightPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _content.Children.Clear();
    }

    private async Task InitializeKeyboardBacklightAsync()
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

            _loader.IsLoading = false;
        }
    }

    public static async Task<bool> IsSupportedAsync() => await KeyboardBacklightViewModel.IsSupportedAsync();
}
}
