using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.PackageDownloader;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Controls.Packages;
using UniversalDeviceToolkit.WPF.Extensions;
using LenovoLegionToolkit.Lib.System;
using UniversalDeviceToolkit.WPF.Utils;
using System.IO;
using System.Net.Http;
using Wpf.Ui.Controls;
using System.Windows.Forms;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;
using UniversalDeviceToolkit.WPF.Resources;
using LenovoLegionToolkit.Lib.Extensions;
using HyperlinkButton = Wpf.Ui.Controls.HyperlinkButton;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace UniversalDeviceToolkit.WPF.Pages;

public partial class WindowsOptimizationPage
{
    private IPackageDownloader? _driverPackageDownloader;
    private CancellationTokenSource? _driverGetPackagesTokenSource;
    private CancellationTokenSource? _driverFilterDebounceCancellationTokenSource;
    private List<Package>? _driverPackages;
    private static CultureInfo ActiveDriverCulture => Resource.Culture ?? CultureInfo.CurrentUICulture;

    private static string DriverText(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, ActiveDriverCulture);

    private async void InitializeDriverDownloadPage()
    {
        if (_driverOsComboBox != null && _driverOsComboBox.Items.Count == 0)
                _driverOsComboBox.SetItems(Enum.GetValues<OS>(), OSExtensions.GetCurrent(), os => os.GetDisplayName());

            if (_driverMachineTypeTextBox != null && string.IsNullOrWhiteSpace(_driverMachineTypeTextBox.Text))
            {
                try
                {
                    var machineInfo = await MachineCompatibility.GetMachineInformationAsync().ConfigureAwait(false);
                    _driverMachineTypeTextBox.Text = machineInfo.MachineType;
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to get machine info: {ex.Message}", ex);
                }
            }

            if (_driverDownloadToText != null && string.IsNullOrWhiteSpace(_driverDownloadToText.Text))
            {
                var downloadsFolder = KnownFolders.GetPath(KnownFolder.Downloads);
                _driverDownloadToText.Text = Directory.Exists(_packageDownloaderSettings.Store.DownloadPath)
                    ? _packageDownloaderSettings.Store.DownloadPath
                    : downloadsFolder;
            }

            if (_driverSourcePrimaryRadio != null && _driverSourcePrimaryRadio.Tag == null)
                _driverSourcePrimaryRadio.Tag = PackageDownloaderFactory.Type.Vantage;
            if (_driverSourceSecondaryRadio != null && _driverSourceSecondaryRadio.Tag == null)
                _driverSourceSecondaryRadio.Tag = PackageDownloaderFactory.Type.PCSupport;

            if (_driverSearchControlsGrid != null)
                _driverSearchControlsGrid.Visibility = Visibility.Collapsed;
            if (_driverInfoBar != null)
                _driverInfoBar.Visibility = Visibility.Collapsed;

            if (_driverPackages is null || _driverPackages.Count == 0)
                ShowDriverEmptyState(
                    DriverText("WindowsOptimizationPage_DriverEmpty_NotScanned_Title", "Scan for driver packages"),
                    DriverText("WindowsOptimizationPage_DriverEmpty_NotScanned_Message", "Choose a source and scan to list compatible driver downloads."));
    }

    private void DriverSelectRecommendedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_driverPackagesStackPanel?.Children == null)
            return;

        foreach (var child in _driverPackagesStackPanel.Children.OfType<PackageControl>())
        {
            if (child.IsRecommended)
            {
                child.IsSelected = true;
            }
        }
    }

    

    private void DriverOpenDownloadToButton_Click(object sender, RoutedEventArgs e)
    {
        var path = GetDriverDownloadLocation();
        if (Directory.Exists(path))
        {
            var startInfo = new ProcessStartInfo("explorer.exe")
            {
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(path);
            using var process = Process.Start(startInfo);
        }
    }

    private void DriverSourceRadio_Checked(object sender, RoutedEventArgs e)
    {
        // Source selection is handled when clicking Search
    }

    private async void DriverSearchButton_Click(object sender, RoutedEventArgs e)
    {
        await DriverDownloadPackagesButton_Click(sender, e).ConfigureAwait(false);
    }

    private void DriverScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scv)
        {
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }

    private async void DriverFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Cancel and dispose previous token source
        if (_driverFilterDebounceCancellationTokenSource != null)
        {
            try
            {
                await _driverFilterDebounceCancellationTokenSource.CancelAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
            finally
            {
                _driverFilterDebounceCancellationTokenSource.Dispose();
                _driverFilterDebounceCancellationTokenSource = null;
            }
        }

        _driverFilterDebounceCancellationTokenSource = new CancellationTokenSource();
        var token = _driverFilterDebounceCancellationTokenSource.Token;

        try
        {
            await Task.Delay(300, token).ConfigureAwait(false);
            if (!token.IsCancellationRequested)
            {
                // Ensure UI update happens on UI thread
                Dispatcher.BeginInvoke(() => DriverReload());
            }
        }
        catch (TaskCanceledException)
        {
            // Ignore - expected when cancellation occurs
        }
        catch (ObjectDisposedException)
        {
            // Token source was disposed, ignore
        }
    }

    private void DriverOnlyShowUpdatesCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        DriverReload();
    }

    private void DriverSortingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DriverReload();
    }

    private async Task StartOrPauseSelectedDriversAsync()
    {
        if (ViewModel.IsAnyDriverRunning)
        {
            foreach (var selectedPackage in ViewModel.SelectedDriverPackages.ToList())
            {
                var control = selectedPackage._sourcePackageControl;
                if (control?.Status is PackageControl.PackageStatus.Downloading or PackageControl.PackageStatus.Installing)
                    control.Pause();
            }

            UpdateDriverRunningState();
            ViewModel.NotifyDriverSelectionChanged();
            return;
        }

        foreach (var selectedPackage in ViewModel.SelectedDriverPackages.ToList())
        {
            if (selectedPackage.IsCompleted)
                continue;

            var control = selectedPackage._sourcePackageControl;
            if (control is null)
                continue;

            await control.StartAsync().ConfigureAwait(false);
            UpdateDriverRunningState();
            ViewModel.NotifyDriverSelectionChanged();
        }
    }

    private class DriverDownloadProgressReporter : IProgress<float>
    {
        private readonly WindowsOptimizationPage _page;

        public DriverDownloadProgressReporter(WindowsOptimizationPage page)
        {
            _page = page;
        }

        public void Report(float value)
        {
            // Optional: update UI progress
        }
    }

    private void StopDriverRetryTimer()
    {
        // No-op or implementation if timer exists
    }

    private void ShowDriverEmptyState(string title, string message)
    {
        if (_driverEmptyStateBorder is null)
            return;

        if (_driverEmptyStateTitleTextBlock is not null)
            _driverEmptyStateTitleTextBlock.Text = title;

        if (_driverEmptyStateMessageTextBlock is not null)
            _driverEmptyStateMessageTextBlock.Text = message;

        _driverEmptyStateBorder.Visibility = Visibility.Visible;
    }

    private void HideDriverEmptyState()
    {
        if (_driverEmptyStateBorder is not null)
            _driverEmptyStateBorder.Visibility = Visibility.Collapsed;
    }

    private void ClearSelectedDriverPackages()
    {
        foreach (var selectedPackage in ViewModel.SelectedDriverPackages.ToList())
        {
            selectedPackage.Dispose();
            ViewModel.SelectedDriverPackages.Remove(selectedPackage);
        }

        ViewModel.NotifyDriverSelectionChanged();
    }

    private async Task DriverDownloadPackagesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ShouldInterruptDriverDownloadsIfRunning().ConfigureAwait(false))
            return;

        var errorOccurred = false;
        try
        {
            if (_driverLoader != null)
                _driverLoader.Visibility = Visibility.Visible;

            if (_driverInfoBar != null)
            {
                _driverInfoBar.IsOpen = true;
                _driverInfoBar.Visibility = Visibility.Visible;
            }

            _driverPackages = null;

            if (_driverPackagesStackPanel != null)
                _driverPackagesStackPanel.Children.Clear();
            ClearSelectedDriverPackages();
            HideDriverEmptyState();

            if (_driverScrollViewer != null)
                _driverScrollViewer.ScrollToHome();

            if (_driverFilterTextBox != null)
                _driverFilterTextBox.Text = string.Empty;
            if (_driverSortingComboBox != null)
                _driverSortingComboBox.SelectedIndex = 2;

            var machineType = _driverMachineTypeTextBox?.Text.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(machineType) || machineType.Length != 4 ||
                _driverOsComboBox == null || !_driverOsComboBox.TryGetSelectedItem(out OS os))
            {
                await SnackbarHelper.ShowAsync(Resource.PackagesPage_DownloadFailed_Title,
                    Resource.PackagesPage_DownloadFailed_Message).ConfigureAwait(false);
                return;
            }

            if (_driverLoadingIndicator != null)
                _driverLoadingIndicator.Visibility = Visibility.Visible;

            // Cancel and dispose previous token source
            if (_driverGetPackagesTokenSource is not null)
            {
                try
                {
                    await _driverGetPackagesTokenSource.CancelAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed, ignore
                }
                finally
                {
                    _driverGetPackagesTokenSource.Dispose();
                    _driverGetPackagesTokenSource = null;
                }
            }

            _driverGetPackagesTokenSource = new CancellationTokenSource();

            var token = _driverGetPackagesTokenSource.Token;

            var packageDownloaderType = new[] { _driverSourcePrimaryRadio, _driverSourceSecondaryRadio }
                .Where(r => r != null && r.IsChecked == true)
                .Select(r => (PackageDownloaderFactory.Type)r.Tag)
                .FirstOrDefault();

            if (_driverOnlyShowUpdatesCheckBox != null)
            {
                _driverOnlyShowUpdatesCheckBox.Visibility = Visibility.Visible;
                if (packageDownloaderType == PackageDownloaderFactory.Type.Vantage)
                    _driverOnlyShowUpdatesCheckBox.IsChecked = _packageDownloaderSettings.Store.OnlyShowUpdates;
                else
                    _driverOnlyShowUpdatesCheckBox.IsChecked = false;
            }

            _driverPackageDownloader = _packageDownloaderFactory.GetInstance(packageDownloaderType);
            var packages = await _driverPackageDownloader.GetPackagesAsync(machineType, os, new DriverDownloadProgressReporter(this), token).ConfigureAwait(false);

            _driverPackages = packages;

            // Ensure UI update happens on UI thread
            Dispatcher.BeginInvoke(() => DriverReload());

            StopDriverRetryTimer();

            if (_driverLoadingIndicator != null)
                _driverLoadingIndicator.Visibility = Visibility.Collapsed;

            if (_driverSearchControlsGrid != null)
                _driverSearchControlsGrid.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error occurred when downloading packages.", ex);

            await SnackbarHelper.ShowAsync(Resource.PackagesPage_Error_Title, ex.Message, SnackbarType.Error).ConfigureAwait(false);
            errorOccurred = true;
        }
        finally
        {
            // Clean up token source
            if (_driverGetPackagesTokenSource != null)
            {
                try
                {
                    if (!_driverGetPackagesTokenSource.Token.IsCancellationRequested)
                        await _driverGetPackagesTokenSource.CancelAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed, ignore
                }
                finally
                {
                    _driverGetPackagesTokenSource?.Dispose();
                    _driverGetPackagesTokenSource = null;
                }
            }

            // UI updates must be on UI thread
            Dispatcher.BeginInvoke(() =>
            {
                if (!errorOccurred && _driverLoadingIndicator != null)
                    _driverLoadingIndicator.Visibility = Visibility.Collapsed;

                if (errorOccurred && _driverPackagesStackPanel != null)
                    _driverPackagesStackPanel.Children.Clear();

                if (errorOccurred)
                {
                    ShowDriverEmptyState(
                        DriverText("WindowsOptimizationPage_DriverEmpty_Error_Title", "Driver scan did not complete"),
                        DriverText("WindowsOptimizationPage_DriverEmpty_Error_Message", "Check the selected source and network connection, then scan again."));
                }
            });
        }
    }

    private void DriverReload()
    {
        if (_driverPackageDownloader is null || _driverPackagesStackPanel == null)
            return;

        // Clear existing children
        _driverPackagesStackPanel.Children.Clear();
        HideDriverEmptyState();

        if (_driverPackages is null || _driverPackages.Count == 0)
        {
            ShowDriverEmptyState(
                DriverText("WindowsOptimizationPage_DriverEmpty_NoResults_Title", "No driver downloads found"),
                DriverText("WindowsOptimizationPage_DriverEmpty_NoResults_Message", "Try a different source, operating system, or machine type."));
            AddDriverShowHiddenDownloadsLinkIfNeeded();
            return;
        }

        var packages = DriverSortAndFilter(_driverPackages);

        if (packages.Count == 0)
        {
            ShowDriverEmptyState(
                DriverText("WindowsOptimizationPage_DriverEmpty_NoFilterResults_Title", "No matching downloads found"),
                DriverText("WindowsOptimizationPage_DriverEmpty_NoFilterResults_Message", "Adjust the filter, update-only option, or hidden-download list."));
            AddDriverShowHiddenDownloadsLinkIfNeeded();
            return;
        }

        // Pre-allocate list to reduce allocations during iteration
        var controlsToAdd = new List<UIElement>(packages.Count);

        foreach (var package in packages)
        {
            var control = new PackageControl(_driverPackageDownloader, package, GetDriverDownloadLocation)
            {
                ContextMenu = GetDriverContextMenu(package, packages),
                AutoStartOnSelection = false,
                HideWhenCompleted = false
            };

            var existingSelectedPackage = ViewModel.SelectedDriverPackages.FirstOrDefault(p => p.PackageId == package.Id);
            if (existingSelectedPackage is not null)
            {
                control.IsSelected = true;
                existingSelectedPackage.AttachSource(control);
            }

            control.PropertyChanged += OnPackageControlPropertyChanged;

            controlsToAdd.Add(control);
        }

        // Batch add controls to reduce UI updates
        foreach (var control in controlsToAdd)
        {
            _driverPackagesStackPanel.Children.Add(control);
        }

        ViewModel.NotifyDriverSelectionChanged();
        UpdateDriverRunningState();
        AddDriverShowHiddenDownloadsLinkIfNeeded();
    }

    private List<Package> DriverSortAndFilter(List<Package> packages)
    {
        var selectedIndex = _driverSortingComboBox?.SelectedIndex ?? 2;
        var result = selectedIndex switch
        {
            0 => packages.OrderBy(p => p.Title),
            1 => packages.OrderBy(p => p.Category),
            2 => packages.OrderByDescending(p => p.ReleaseDate),
            _ => packages.AsEnumerable(),
        };

        result = result.Where(p => !_packageDownloaderSettings.Store.HiddenPackages.Contains(p.Id));

        if (_driverOnlyShowUpdatesCheckBox?.IsChecked ?? false)
            result = result.Where(p => p.IsUpdate);

        var filterText = _driverFilterTextBox?.Text ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(filterText))
            result = result.Where(p => p.Index.Contains(filterText, StringComparison.OrdinalIgnoreCase));

        return result.ToList();
    }

    private void SyncSelectedDriverPackage(PackageControl control)
    {
        var existing = ViewModel.SelectedDriverPackages.FirstOrDefault(p => p.PackageId == control.Package.Id);

        if (control.IsSelected)
        {
            if (existing is not null)
            {
                existing.AttachSource(control);
            }
            else
            {
                ViewModel.SelectedDriverPackages.Add(new SelectedDriverPackageViewModel(
                    control.Package.Id,
                    control.Package.Title,
                    control.Package.Description,
                    control.Package.Category,
                    control));
            }
        }
        else if (existing is not null && !existing.IsCompleted)
        {
            existing.Dispose();
            ViewModel.SelectedDriverPackages.Remove(existing);
        }

        ViewModel.NotifyDriverSelectionChanged();
        UpdateDriverRunningState();
    }

    private void RemoveSelectedDriverPackage(string packageId)
    {
        var selectedPackage = ViewModel.SelectedDriverPackages.FirstOrDefault(p => p.PackageId == packageId);
        if (selectedPackage is null)
            return;

        if (selectedPackage._sourcePackageControl is not null)
            selectedPackage._sourcePackageControl.IsSelected = false;

        selectedPackage.Dispose();
        ViewModel.SelectedDriverPackages.Remove(selectedPackage);
        ViewModel.NotifyDriverSelectionChanged();
        UpdateDriverRunningState();
    }

    private void UpdateDriverRunningState()
    {
        ViewModel.IsAnyDriverRunning = ViewModel.SelectedDriverPackages.Any(IsDriverPackageRunning);
    }

    private static bool IsDriverPackageRunning(SelectedDriverPackageViewModel package)
    {
        var control = package._sourcePackageControl;
        return control?.Status is PackageControl.PackageStatus.Downloading or PackageControl.PackageStatus.Installing;
    }

    private void AddDriverShowHiddenDownloadsLinkIfNeeded()
    {
        if (_driverPackagesStackPanel is null || _packageDownloaderSettings.Store.HiddenPackages.Count == 0)
            return;

        var clearHidden = new HyperlinkButton
        {
            Icon = new SymbolIcon { Symbol = SymbolRegular.Eye24 },
            Content = Resource.WindowsOptimizationPage_ShowHiddenDownloads,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
        };
        clearHidden.Click += (_, _) =>
        {
            _packageDownloaderSettings.Store.HiddenPackages.Clear();
            _packageDownloaderSettings.SynchronizeStore();

            DriverReload();
        };

        _driverPackagesStackPanel.Children.Add(clearHidden);
    }

    private string GetDriverDownloadLocation()
    {
        if (_driverDownloadToText == null)
            return KnownFolders.GetPath(KnownFolder.Downloads);

        var location = _driverDownloadToText.Text.Trim();

        if (!Directory.Exists(location))
        {
            var downloads = KnownFolders.GetPath(KnownFolder.Downloads);
            location = downloads;
            _driverDownloadToText.Text = downloads;
            _packageDownloaderSettings.Store.DownloadPath = downloads;
            _packageDownloaderSettings.SynchronizeStore();
        }

        return location;
    }

    private System.Windows.Controls.ContextMenu? GetDriverContextMenu(Package package, IEnumerable<Package> packages)
    {
        if (_packageDownloaderSettings.Store.HiddenPackages.Contains(package.Id))
            return null;

        var hideMenuItem = new MenuItem
        {
            Icon = new SymbolIcon { Symbol = SymbolRegular.EyeOff24 },
            Header = Resource.Hide,
        };
        hideMenuItem.Click += (_, _) =>
        {
            RemoveSelectedDriverPackage(package.Id);
            _packageDownloaderSettings.Store.HiddenPackages.Add(package.Id);
            _packageDownloaderSettings.SynchronizeStore();
            DriverReload();
        };

        var hideAllMenuItem = new MenuItem
        {
            Icon = new SymbolIcon { Symbol = SymbolRegular.EyeOff24 },
            Header = Resource.HideAll,
        };
        hideAllMenuItem.Click += (_, _) =>
        {
            foreach (var id in packages.Select(p => p.Id))
            {
                RemoveSelectedDriverPackage(id);
                _packageDownloaderSettings.Store.HiddenPackages.Add(id);
            }
            _packageDownloaderSettings.SynchronizeStore();
            DriverReload();
        };

        var cm = new System.Windows.Controls.ContextMenu();
        cm.Items.Add(hideMenuItem);
        cm.Items.Add(hideAllMenuItem);
        return cm;
    }

    private async Task<bool> ShouldInterruptDriverDownloadsIfRunning()
    {
        if (_driverPackagesStackPanel?.Children is null)
            return true;

        if (!_driverPackagesStackPanel.Children.OfType<PackageControl>().Any(pc =>
                pc.Status is PackageControl.PackageStatus.Downloading or PackageControl.PackageStatus.Installing))
            return true;

        return await MessageBoxHelper.ShowAsync(this, Resource.PackagesPage_DownloadInProgress_Title, Resource.PackagesPage_DownloadInProgress_Message).ConfigureAwait(false);
    }

    private void DriverDownloadToText_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_driverDownloadToText != null && Directory.Exists(_driverDownloadToText.Text))
        {
            _packageDownloaderSettings.Store.DownloadPath = _driverDownloadToText.Text;
            _packageDownloaderSettings.SynchronizeStore();
        }
    }

    private void DriverDownloadToButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            if (_driverDownloadToText != null)
                _driverDownloadToText.Text = dialog.SelectedPath;
        }
    }

    private void OnPackageControlPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PackageControl control)
            return;

        if (e.PropertyName == nameof(PackageControl.IsSelected))
        {
            SyncSelectedDriverPackage(control);
        }
        else if (e.PropertyName == nameof(PackageControl.Status) ||
                 e.PropertyName == nameof(PackageControl.IsDownloading))
        {
            UpdateDriverRunningState();
            ViewModel.NotifyDriverSelectionChanged();
        }
    }

    public void UnsubscribeFromPackageControlHandlers()
    {
        if (_driverPackagesStackPanel?.Children is null)
            return;

        foreach (var control in _driverPackagesStackPanel.Children.OfType<PackageControl>())
            control.PropertyChanged -= OnPackageControlPropertyChanged;
    }


}

