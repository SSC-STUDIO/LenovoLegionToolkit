using System;
using System.Threading;
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
    private CancellationTokenSource? _initializationCancellationTokenSource;
    private int _initializationVersion;

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

        var initializationVersion = Interlocked.Increment(ref _initializationVersion);
        var cancellationTokenSource = new CancellationTokenSource();
        var previousCancellationTokenSource = Interlocked.Exchange(ref _initializationCancellationTokenSource, cancellationTokenSource);
        previousCancellationTokenSource?.Cancel();
        previousCancellationTokenSource?.Dispose();

        try
        {
            await InitializeKeyboardBacklightAsync(initializationVersion, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (initializationVersion == Volatile.Read(ref _initializationVersion))
                _isInitializing = false;
        }
    }

    private void KeyboardBacklightPage_Unloaded(object sender, RoutedEventArgs e)
    {
        Interlocked.Increment(ref _initializationVersion);
        var cancellationTokenSource = Interlocked.Exchange(ref _initializationCancellationTokenSource, null);
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        _isInitializing = false;
        _content.Children.Clear();
    }

    private async Task InitializeKeyboardBacklightAsync(int initializationVersion, CancellationToken cancellationToken)
    {
        try
        {
            _titleTextBlock.Visibility = Visibility.Collapsed;

            await _viewModel.DetectKeyboardTypeCommand.ExecuteAsync(null);
            cancellationToken.ThrowIfCancellationRequested();
            if (initializationVersion != Volatile.Read(ref _initializationVersion))
                return;

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
        catch (OperationCanceledException)
        {
            throw;
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
