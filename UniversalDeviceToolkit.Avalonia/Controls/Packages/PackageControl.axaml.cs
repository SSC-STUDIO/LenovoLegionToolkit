using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Lib.PackageDownloader;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Controls.Packages
{
    public partial class PackageControl : global::Avalonia.Controls.UserControl, IProgress<float>, IDisposable
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

        // AVALONIA: the named Badge/ProgressRing elements are looked up through the name
        // scope instead of XAML-generated fields (their Custom-namespace types are not
        // resolvable from the ui: prefix, so the XAML compiler cannot emit fields).
        private ContentControl? _recommendedBadgeCache;
        private ContentControl? RecommendedBadge =>
            _recommendedBadgeCache ??= this.FindNameScope()?.Find("_recommendedBadge") as ContentControl;

        private global::UniversalDeviceToolkit.Avalonia.Controls.Custom.ProgressRing? _downloadProgressRingCache;
        private global::UniversalDeviceToolkit.Avalonia.Controls.Custom.ProgressRing? DownloadProgressRing =>
            _downloadProgressRingCache ??= this.FindNameScope()?.Find("_downloadProgressRing")
                as global::UniversalDeviceToolkit.Avalonia.Controls.Custom.ProgressRing;

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
            _descriptionTextBlock.IsVisible = string.IsNullOrWhiteSpace(package.Description) ? false : true;
            _categoryTextBlock.Text = package.Category;
            _detailTextBlock.Text = $"{Resource.PackageControl_Version} {package.Version}  |  {package.FileSize}  |  {package.FileName}";

            // Vantage packages often set Readme to "" when missing — hide unless a real URL exists.
            _readmeButton.IsVisible = HasOpenableReadme(package.Readme) ? true : false;
            _updateRebootStackPanel.IsVisible = _isUpdateStackPanel.IsVisible = package.IsUpdate ? true : false;

            _rebootStackPanel.IsVisible = package is { IsUpdate: true, Reboot: RebootType.Delayed or RebootType.Requested or RebootType.Forced or RebootType.ForcedPowerOff }
                ? true
                : false;
            _rebootTextBlock.Text = package.Reboot switch
            {
                RebootType.Delayed or RebootType.Requested => Resource.PackageControl_RebootRecommended,
                RebootType.Forced => Resource.PackageControl_RebootRequired,
                RebootType.ForcedPowerOff => Resource.PackageControl_ShutdownRequired,
                _ => string.Empty
            };

            var showWarning = package.ReleaseDate < DateTime.UtcNow.AddYears(-1);
            _warningTextBlock.IsVisible = showWarning ? true : false;

            // Bind checkbox
            if (_selectCheckBox != null)
            {
                _selectCheckBox.IsChecked = IsSelected;
                _selectCheckBox.Checked += SelectCheckBox_Checked;
                _selectCheckBox.Unchecked += SelectCheckBox_Unchecked;
            }

            // Initialize recommended badge and status display
            if (RecommendedBadge is { } recommendedBadge)
            {
                // Ensure recommended badge has content
                if (string.IsNullOrEmpty(recommendedBadge.Content?.ToString()))
                {
                    recommendedBadge.Content = Resource.PackageControl_Recommended;
                }
                recommendedBadge.IsVisible = IsRecommended && Status != PackageStatus.Completed
                    ? true
                    : false;
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
                    _idleStackPanel.IsVisible = false;
                    _downloadingStackPanel.IsVisible = true;
                }
                else
                {
                    _idleStackPanel.IsVisible = true;
                    _downloadingStackPanel.IsVisible = false;
                    if (DownloadProgressRing is { } progressRing)
                        progressRing.Progress = 0;
                    _downloadProgressLabel.Text = null;
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
                IsVisible = false;
            }

            UpdateStatusDisplay();
        }

        private void ViewModel_StatusRefreshRequested(object? sender, EventArgs e) => UpdateStatusDisplay();

        private void ViewModel_ProgressChanged(object? sender, float value)
        {
            if (DownloadProgressRing is { } progressRing)
            {
                progressRing.IsIndeterminate = !(value > 0);
                progressRing.Progress = value * 100;
            }
            _downloadProgressLabel.Text = $"{value * 100:0}%";
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
                ToolTip.SetTip(_downloadButton, Resource.PackageControl_Install);
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
                ToolTip.SetTip(_downloadButton, Resource.PackageControl_Download);
            }
        }

        private static SymbolIcon CreateActionIcon(SymbolRegular symbol) => new()
        {
            Symbol = symbol,
            Foreground = TryGetResourceBrush("TextFillColorPrimaryBrush")
                ?? new SolidColorBrush(Colors.White),
        };

        // AVALONIA: Application is not a StyledElement, so the 1-arg resource helper
        // does not apply; use the IResourceHost overload instead.
        private static Brush? TryGetResourceBrush(string key)
        {
            var app = Application.Current;
            if (app is not null && app.TryFindResource(key, out var value))
                return value as Brush;
            return null;
        }

        private void PackageControl_Unloaded(object? sender, RoutedEventArgs e)
        {
            _viewModel.CancelDownload();
            _viewModel.StopInstallation();

            _viewModel.Dispose();
        }

        public void Dispose() => _viewModel.Dispose();

        public Task StartAsync() => _viewModel.StartAsync();

        public void Pause() => _viewModel.Pause();

        public void Report(float value) => _viewModel.Report(value);

        private async void SelectCheckBox_Checked(object? sender, RoutedEventArgs e)
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

        private void SelectCheckBox_Unchecked(object? sender, RoutedEventArgs e)
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
                _statusBadge.IsVisible = string.IsNullOrEmpty(statusText) ? false : true;
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

            if (RecommendedBadge is null)
                return;

            if (Status == PackageStatus.Completed)
                RecommendedBadge.IsVisible = false;
            else if (IsRecommended)
            {
                if (string.IsNullOrEmpty(RecommendedBadge.Content?.ToString()))
                    RecommendedBadge.Content = Resource.PackageControl_Recommended;

                RecommendedBadge.IsVisible = true;
            }
            else
            {
                RecommendedBadge.IsVisible = false;
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

        private async void CopyToClipboard_MouseRightButtonUp(object? sender, PointerReleasedEventArgs e)
        {
            if (sender is not TextBlock tb)
                return;

            if (!e.GetCurrentPoint(tb).Properties.IsRightButtonPressed)
                return;

            e.Handled = true;

            var str = tb.Text;

            try
            {
            var clipboard = UdtAppContext.Clipboard;
            if (clipboard is not null)
                await clipboard.SetTextAsync(str);
                await SnackbarHelper.ShowAsync(Resource.CopiedToClipboard_Title, string.Format(Resource.CopiedToClipboard_Message_WithParam, str));
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't copy to clipboard", ex);
            }
        }

        private void ReadmeButton_Click(object? sender, RoutedEventArgs e)
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

        private async void DownloadButton_Click(object? sender, RoutedEventArgs e)
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

        private void CancelDownloadButton_Click(object? sender, RoutedEventArgs e) => _viewModel.CancelDownload();

        private async void InstallButton_Click(object? sender, RoutedEventArgs e)
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
