using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using UniversalDeviceToolkit.Lib.PackageDownloader;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Packages
{
    public partial class PackageControl : IProgress<float>, IDisposable
    {
        private readonly PackageControlViewModel _viewModel;

        public enum PackageStatus
        {
            NotStarted,    // Not started
            Queued,        // Queued for later start
            Downloading,   // Downloading
            Installing,    // Installing
            Completed      // Completed
        }

        public bool AutoStartOnSelection
        {
            get => _viewModel.AutoStartOnSelection;
            set => _viewModel.AutoStartOnSelection = value;
        }

        public bool HideWhenCompleted
        {
            get => _viewModel.HideWhenCompleted;
            set => _viewModel.HideWhenCompleted = value;
        }

        public bool IsDownloading => _viewModel.IsDownloading;

        public PackageStatus Status => _viewModel.Status;

        public bool IsCompleted => _viewModel.IsCompleted;

        public bool IsSelected
        {
            get => _viewModel.IsSelected;
            set => _viewModel.IsSelected = value;
        }

        public bool IsRecommended => _viewModel.IsRecommended;

        public Package Package => _viewModel.Package;

        public event PropertyChangedEventHandler? PropertyChanged;

        public PackageControl(IPackageDownloader packageDownloader, Package package, Func<string> getDownloadPath)
        {
            _viewModel = new PackageControlViewModel(packageDownloader, package, getDownloadPath);
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.StatusChanged += ViewModel_StatusChanged;
            _viewModel.StatusRefreshRequested += ViewModel_StatusRefreshRequested;
            _viewModel.ProgressChanged += ViewModel_ProgressChanged;
            _viewModel.SnackbarRequested += ViewModel_SnackbarRequested;
            _viewModel.DownloadButtonStateChanged += ViewModel_DownloadButtonStateChanged;

            InitializeComponent();

            Unloaded += PackageControl_Unloaded;

            // Mark as recommended if it's an update; also evaluates the download/install
            // button state (raises DownloadButtonStateChanged once the button exists).
            _viewModel.Initialize(isRecommended: package.IsUpdate);

            _dateTextBlock.Text = package.ReleaseDate.ToString(LocalizationHelper.ShortDateFormat);
            _titleTextBlock.Text = package.Title;
            _descriptionTextBlock.Text = package.Description;
            _descriptionTextBlock.Visibility = string.IsNullOrWhiteSpace(package.Description) ? Visibility.Collapsed : Visibility.Visible;
            _categoryTextBlock.Text = package.Category;
            _detailTextBlock.Text = $"{Resource.PackageControl_Version} {package.Version}  |  {package.FileSize}  |  {package.FileName}";

            // Vantage packages often set Readme to "" when missing — hide unless a real URL exists.
            _readmeButton.Visibility = HasOpenableReadme(package.Readme) ? Visibility.Visible : Visibility.Collapsed;
            _updateRebootStackPanel.Visibility = _isUpdateStackPanel.Visibility = package.IsUpdate ? Visibility.Visible : Visibility.Collapsed;

            _rebootStackPanel.Visibility = package is { IsUpdate: true, Reboot: RebootType.Delayed or RebootType.Requested or RebootType.Forced or RebootType.ForcedPowerOff }
                ? Visibility.Visible
                : Visibility.Collapsed;
            _rebootTextBlock.Text = package.Reboot switch
            {
                RebootType.Delayed or RebootType.Requested => Resource.PackageControl_RebootRecommended,
                RebootType.Forced => Resource.PackageControl_RebootRequired,
                RebootType.ForcedPowerOff => Resource.PackageControl_ShutdownRequired,
                _ => string.Empty
            };

            var showWarning = package.ReleaseDate < DateTime.UtcNow.AddYears(-1);
            _warningTextBlock.Visibility = showWarning ? Visibility.Visible : Visibility.Collapsed;

            // Bind checkbox
            if (_selectCheckBox != null)
            {
                _selectCheckBox.IsChecked = IsSelected;
                _selectCheckBox.Checked += SelectCheckBox_Checked;
                _selectCheckBox.Unchecked += SelectCheckBox_Unchecked;
            }

            // Initialize recommended badge and status display
            if (_recommendedBadge != null)
            {
                // Ensure recommended badge has content
                if (string.IsNullOrEmpty(_recommendedBadge.Content?.ToString()))
                {
                    _recommendedBadge.Content = Resource.PackageControl_Recommended;
                }
                _recommendedBadge.Visibility = IsRecommended && Status != PackageStatus.Completed
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            // Initialize status display (must be after recommended badge, as UpdateStatusDisplay may modify badge visibility)
            UpdateStatusDisplay();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);

            if (e.PropertyName == nameof(PackageControlViewModel.IsDownloading))
            {
                // Panels and progress ring follow the download state (same timing as
                // the original code-behind: panels swapped around the download, ring reset after).
                if (_viewModel.IsDownloading)
                {
                    _idleStackPanel.Visibility = Visibility.Collapsed;
                    _downloadingStackPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    _idleStackPanel.Visibility = Visibility.Visible;
                    _downloadingStackPanel.Visibility = Visibility.Collapsed;
                    _downloadProgressRing.Progress = 0;
                    _downloadProgressLabel.Content = null;
                }
            }
            else if (e.PropertyName == nameof(PackageControlViewModel.IsSelected))
            {
                // Synchronize checkbox state in UI (avoid event loop)
                if (_selectCheckBox != null && _selectCheckBox.IsChecked != _viewModel.IsSelected)
                {
                    // Temporarily remove event handlers to avoid triggering SelectCheckBox_Checked/Unchecked
                    _selectCheckBox.Checked -= SelectCheckBox_Checked;
                    _selectCheckBox.Unchecked -= SelectCheckBox_Unchecked;

                    _selectCheckBox.IsChecked = _viewModel.IsSelected;

                    // Re-add event handlers
                    _selectCheckBox.Checked += SelectCheckBox_Checked;
                    _selectCheckBox.Unchecked += SelectCheckBox_Unchecked;
                }
            }
        }

        private void ViewModel_StatusChanged(object? sender, PackageStatus status)
        {
            if (status == PackageStatus.Completed && _viewModel.HideWhenCompleted)
            {
                Visibility = Visibility.Collapsed;
            }

            UpdateStatusDisplay();
        }

        private void ViewModel_StatusRefreshRequested(object? sender, EventArgs e) => UpdateStatusDisplay();

        private void ViewModel_ProgressChanged(object? sender, float value)
        {
            _downloadProgressRing.IsIndeterminate = !(value > 0);
            _downloadProgressRing.Progress = value * 100;
            _downloadProgressLabel.Content = $"{value * 100:0}%";
        }

        private void ViewModel_SnackbarRequested(object? sender, PackageControlViewModel.SnackbarRequestedEventArgs e)
        {
            var type = e.Kind switch
            {
                PackageControlViewModel.SnackbarKind.Warning => SnackbarType.Warning,
                PackageControlViewModel.SnackbarKind.Error => SnackbarType.Error,
                _ => SnackbarType.Success
            };

            _ = SnackbarHelper.ShowAsync(e.Title, e.Message, type);
        }

        private void ViewModel_DownloadButtonStateChanged(object? sender, EventArgs e) => CheckAndUpdateDownloadButtonState();

        private void CheckAndUpdateDownloadButtonState()
        {
            if (_downloadButton == null)
                return;

            if (_viewModel.DownloadedFileExists)
            {
                // File already exists, switch download button to install button
                // Check current icon to avoid duplicate switching
                if (_downloadButton.Icon is SymbolIcon { Symbol: SymbolRegular.Play24 })
                    return; // Already install button, no switch needed

                _downloadButton.Click -= DownloadButton_Click;
                _downloadButton.Click += InstallButton_Click;

                _downloadButton.Icon = CreateActionIcon(SymbolRegular.Play24);
                _downloadButton.ToolTip = Resource.PackageControl_Install;
            }
            else
            {
                // File is not present, keep the button in download mode.
                if (_downloadButton.Icon is SymbolIcon { Symbol: SymbolRegular.ArrowDownload24 })
                    return; // Already download button, no switch needed

                _downloadButton.Click -= InstallButton_Click;
                _downloadButton.Click -= DownloadButton_Click; // Remove first to avoid duplicate binding
                _downloadButton.Click += DownloadButton_Click;

                _downloadButton.Icon = CreateActionIcon(SymbolRegular.ArrowDownload24);
                _downloadButton.ToolTip = Resource.PackageControl_Download;
            }
        }

        private static SymbolIcon CreateActionIcon(SymbolRegular symbol) => new()
        {
            Symbol = symbol,
            Foreground = Application.Current.TryFindResource("TextFillColorPrimaryBrush") as Brush
                ?? Brushes.White,
        };

        private void PackageControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.CancelDownload();
            _viewModel.StopInstallation();

            _viewModel.Dispose();
        }

        public void Dispose() => _viewModel.Dispose();

        public Task StartAsync() => _viewModel.StartAsync();

        public void Pause() => _viewModel.Pause();

        public void Report(float value) => _viewModel.Report(value);

        private async void SelectCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.IsSelected = true;

                // If already completed, do nothing
                if (Status == PackageStatus.Completed)
                    return;

                // If already downloading or installing, don't repeat
                if (Status == PackageStatus.Downloading || Status == PackageStatus.Installing)
                    return;

                if (!AutoStartOnSelection)
                    return;

                await _viewModel.StartAsync();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Exception in {nameof(SelectCheckBox_Checked)}.", ex);
            }
        }

        private void SelectCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _viewModel.IsSelected = false;

            // If already completed, cancellation not allowed
            if (Status == PackageStatus.Completed)
            {
                // Re-select the checkbox
                if (_selectCheckBox != null)
                    _selectCheckBox.IsChecked = true;
                return;
            }

            // Stop download or installation
            if (Status == PackageStatus.Downloading)
            {
                _viewModel.CancelDownload();
                _viewModel.ResetPendingStatus();
            }
            else if (Status == PackageStatus.Installing)
            {
                _viewModel.StopInstallation();
                _viewModel.ResetPendingStatus();
            }
            else if (Status == PackageStatus.Queued)
            {
                _viewModel.ResetPendingStatus();
            }
        }

        private void UpdateStatusDisplay()
        {
            if (_statusBadge is not null && _statusTextBlock is not null && _statusIcon is not null)
            {
                var status = Status == PackageStatus.NotStarted && !AutoStartOnSelection && IsSelected
                    ? PackageStatus.Queued
                    : Status;

                var statusText = GetStatusText(status);
                _statusBadge.Visibility = string.IsNullOrEmpty(statusText) ? Visibility.Collapsed : Visibility.Visible;
                _statusTextBlock.Text = statusText;
                _statusIcon.Symbol = status switch
                {
                    PackageStatus.Queued => SymbolRegular.Clock24,
                    PackageStatus.Downloading => SymbolRegular.ArrowDownload24,
                    PackageStatus.Installing => SymbolRegular.Play24,
                    PackageStatus.Completed => SymbolRegular.CheckmarkCircle24,
                    _ => SymbolRegular.Empty
                };
            }

            if (_recommendedBadge is null)
                return;

            if (Status == PackageStatus.Completed)
                _recommendedBadge.Visibility = Visibility.Collapsed;
            else if (IsRecommended)
            {
                if (string.IsNullOrEmpty(_recommendedBadge.Content?.ToString()))
                    _recommendedBadge.Content = Resource.PackageControl_Recommended;

                _recommendedBadge.Visibility = Visibility.Visible;
            }
            else
            {
                _recommendedBadge.Visibility = Visibility.Collapsed;
            }
        }

        private static string GetStatusText(PackageStatus status) => status switch
        {
            PackageStatus.Queued => LocalizationHelper.GetStringOrEnglish(
                Resource.ResourceManager,
                "PackageControl_Queued",
                "Queued",
                Resource.Culture ?? CultureInfo.CurrentUICulture),
            PackageStatus.Downloading => Resource.PackageControl_Downloading,
            PackageStatus.Installing => Resource.PackageControl_Installing,
            PackageStatus.Completed => Resource.PackageControl_Completed,
            _ => string.Empty
        };

        private async void CopyToClipboard_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (sender is not TextBlock tb)
                return;

            var str = tb.Text;

            try
            {
                System.Windows.Clipboard.SetText(str);
                await SnackbarHelper.ShowAsync(Resource.CopiedToClipboard_Title, string.Format(Resource.CopiedToClipboard_Message_WithParam, str));
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't copy to clipboard", ex);
            }
        }

        private void ReadmeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryCreateHttpUri(_viewModel.Package.Readme, out var uri) || uri is null)
                return;

            try
            {
                uri.Open();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't open package readme. [readme={_viewModel.Package.Readme}]", ex);
            }
        }

        private static bool HasOpenableReadme(string? readme) => TryCreateHttpUri(readme, out _);

        private static bool TryCreateHttpUri(string? value, out Uri? uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var created) || created is null)
                return false;

            if (!string.Equals(created.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(created.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return false;

            uri = created;
            return true;
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Status == PackageStatus.Completed)
                    return;

                if (Status == PackageStatus.Downloading || Status == PackageStatus.Installing)
                    return;

                await _viewModel.StartAsync();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Exception in {nameof(DownloadButton_Click)}.", ex);
            }
        }

        private void CancelDownloadButton_Click(object sender, RoutedEventArgs e) => _viewModel.CancelDownload();

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Status == PackageStatus.Completed)
                    return;

                if (Status == PackageStatus.Downloading || Status == PackageStatus.Installing)
                    return;

                await _viewModel.StartAsync();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Exception in {nameof(InstallButton_Click)}.", ex);
            }
        }

        // Reflection-compat delegators (PackageControlTests exercises these via
        // reflection); implementations live in PackageControlViewModel.
        private static PackageStatus GetStatusForInstallerExitCode(int exitCode) =>
            PackageControlViewModel.GetStatusForInstallerExitCode(exitCode);

        private static string? GetInstallerExitFailureMessage(int exitCode) =>
            PackageControlViewModel.GetInstallerExitFailureMessage(exitCode);
    }
}
