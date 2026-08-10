using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Avalonia.Controls.KeyboardBacklight.RGB;
using UniversalDeviceToolkit.Avalonia.Controls.KeyboardBacklight.Spectrum;
using UniversalDeviceToolkit.ViewModels;

namespace UniversalDeviceToolkit.Avalonia.Pages
{
public partial class KeyboardBacklightPage : global::Avalonia.Controls.UserControl
{
    private readonly KeyboardBacklightViewModel _viewModel = IoCContainer.Resolve<KeyboardBacklightViewModel>();
    private bool _isInitializing;
    private CancellationTokenSource? _initializationCancellationTokenSource;
    private int _initializationVersion;

    public KeyboardBacklightPage()
    {
        InitializeComponent();
        _titleTextBlock.IsVisible = false;
        Loaded += KeyboardBacklightPage_Loaded;
        Unloaded += KeyboardBacklightPage_Unloaded;
    }

    private async void KeyboardBacklightPage_Loaded(object? sender, RoutedEventArgs e)
    {
        if (_isInitializing || _content.Children.Count > 0)
            return;

        _isInitializing = true;
        _loader.IsLoading = true;
        _content.IsVisible = true;

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

    private void KeyboardBacklightPage_Unloaded(object? sender, RoutedEventArgs e)
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
            _titleTextBlock.IsVisible = false;

            await _viewModel.DetectKeyboardTypeCommand.ExecuteAsync(null);
            cancellationToken.ThrowIfCancellationRequested();
            if (initializationVersion != Volatile.Read(ref _initializationVersion))
                return;

            if (_viewModel.IsSpectrumSupported)
            {
                _titleTextBlock.IsVisible = true;
                var control = new SpectrumKeyboardBacklightControl();
                _content.Children.Add(control);
            }
            else if (_viewModel.IsRGBSupported)
            {
                _titleTextBlock.IsVisible = true;
                var control = new RGBKeyboardBacklightControl();
                _content.Children.Add(control);
            }
            else
            {
                _titleTextBlock.IsVisible = false;
                _content.IsVisible = false;
            }

            _loader.IsLoading = false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error initializing keyboard backlight page.", ex);

            _loader.IsLoading = false;
        }
    }

    public static async Task<bool> IsSupportedAsync()
    {
        var viewModel = IoCContainer.Resolve<KeyboardBacklightViewModel>();
        return await viewModel.IsSupportedAsync();
    }
}
}
