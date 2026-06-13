using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;

namespace UniversalDeviceToolkit.WPF.Windows.Utils
{
public partial class UpdateWindow : IProgress<float>, INotifyPropertyChanged
{
    private readonly UpdateChecker _updateChecker = IoCContainer.Resolve<UpdateChecker>();

    private CancellationTokenSource? _downloadCancellationTokenSource;
    private bool _isDownloadAvailable;
    private bool _isDownloading;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsDownloadAvailable
    {
        get => _isDownloadAvailable;
        private set => SetField(ref _isDownloadAvailable, value);
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        private set => SetField(ref _isDownloading, value);
    }

    public UpdateWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private async void UpdateWindow_Loaded(object sender, RoutedEventArgs e)
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

        IsDownloadAvailable = true;
    }

    private void UpdateWindow_Closing(object? sender, CancelEventArgs e) => _downloadCancellationTokenSource?.Cancel();

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_downloadCancellationTokenSource is not null)
                await _downloadCancellationTokenSource.CancelAsync();

            _downloadCancellationTokenSource = new();

            SetDownloading(true);

            var path = await _updateChecker.DownloadLatestUpdateAsync(this, _downloadCancellationTokenSource.Token);

            _downloadCancellationTokenSource = null;

            Process.Start(path, $"/SILENT /RESTARTAPPLICATIONS /LANG={Resource.Culture.Name.Replace("-", string.Empty)}");
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
        if (isDownloading)
        {
            _downloadProgressBar.Visibility = Visibility.Visible;

            _downloadButton.Visibility = Visibility.Collapsed;
            IsDownloadAvailable = false;

            _cancelDownloadButton.Visibility = Visibility.Visible;
            IsDownloading = true;
        }
        else
        {
            _downloadProgressBar.Value = 0;
            _downloadProgressBar.IsIndeterminate = true;
            _downloadProgressBar.Visibility = Visibility.Hidden;

            _downloadButton.Visibility = Visibility.Visible;
            IsDownloadAvailable = true;

            _cancelDownloadButton.Visibility = Visibility.Collapsed;
            IsDownloading = false;
        }
    }

    public void Report(float value) => Dispatcher.Invoke(() =>
    {
        _downloadProgressBar.IsIndeterminate = !(value > 0);
        _downloadProgressBar.Value = value;
    });

    private bool SetField(ref bool field, bool value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value)
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
}
