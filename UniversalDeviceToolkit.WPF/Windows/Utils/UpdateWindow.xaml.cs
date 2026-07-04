using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;

namespace UniversalDeviceToolkit.WPF.Windows.Utils
{
public partial class UpdateWindow : IProgress<float>
{
    private readonly UpdateChecker _updateChecker = IoCContainer.Resolve<UpdateChecker>();

    private CancellationTokenSource? _downloadCancellationTokenSource;

    public static readonly DependencyProperty HasUpdatesProperty =
        DependencyProperty.Register(nameof(HasUpdates), typeof(bool), typeof(UpdateWindow), new PropertyMetadata(false));

    public static readonly DependencyProperty IsDownloadingProperty =
        DependencyProperty.Register(nameof(IsDownloading), typeof(bool), typeof(UpdateWindow), new PropertyMetadata(false));

    public bool HasUpdates
    {
        get => (bool)GetValue(HasUpdatesProperty);
        set => SetValue(HasUpdatesProperty, value);
    }

    public bool IsDownloading
    {
        get => (bool)GetValue(IsDownloadingProperty);
        set => SetValue(IsDownloadingProperty, value);
    }

    public UpdateWindow() => InitializeComponent();

    private async void UpdateWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var updates = await _updateChecker.GetUpdatesAsync();

            var stringBuilder = new StringBuilder();
            foreach (var update in updates)
            {
                stringBuilder.AppendLine("**" + update.Title + "**   _(" + update.Date.ToString("D") + ")_")
                    .AppendLine()
                    .AppendLine(update.Description)
                    .AppendLine();
            }

            _markdownViewer.Markdown = stringBuilder.ToString();

            HasUpdates = true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(UpdateWindow_Loaded)}.", ex);
        }
    }

    private void UpdateWindow_Closing(object? sender, CancelEventArgs e)
    {
        _downloadCancellationTokenSource?.Cancel();
        _downloadCancellationTokenSource?.Dispose();
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_downloadCancellationTokenSource is not null)
            {
                await _downloadCancellationTokenSource.CancelAsync();
                _downloadCancellationTokenSource.Dispose();
            }

            _downloadCancellationTokenSource = new();

            SetDownloading(true);

            var path = await _updateChecker.DownloadLatestUpdateAsync(this, _downloadCancellationTokenSource.Token);

            _downloadCancellationTokenSource = null;

            using var process = Process.Start(path, $"/SILENT /RESTARTAPPLICATIONS /LANG={Resource.Culture.Name.Replace("-", string.Empty)}");
            await App.Current.ShutdownAsync(true);
        }
        catch (OperationCanceledException)
        {
            SetDownloading(false);
        }
        catch
        {
            SetDownloading(false);

            Constants.LatestReleaseUri.Open();
            Close();
        }
    }

    private void CancelDownloadButton_Click(object sender, RoutedEventArgs e) => _downloadCancellationTokenSource?.Cancel();

    private void SetDownloading(bool isDownloading)
    {
        IsDownloading = isDownloading;

        if (!isDownloading)
        {
            _downloadProgressBar.Value = 0;
            _downloadProgressBar.IsIndeterminate = true;
        }
    }

    public void Report(float value) => Dispatcher.Invoke(() =>
    {
        _downloadProgressBar.IsIndeterminate = !(value > 0);
        _downloadProgressBar.Value = value;
    });
}
}

