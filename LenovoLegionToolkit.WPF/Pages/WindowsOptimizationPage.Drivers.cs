using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.PackageDownloader;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Controls.Packages;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.WPF.Utils;
using System.IO;
using System.Net.Http;
using System.Windows.Media;
using Wpf.Ui.Common;
using MenuItem = Wpf.Ui.Controls.MenuItem;
using System.Windows.Forms;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using LenovoLegionToolkit.WPF.Pages.WindowsOptimization;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.Lib.Extensions;

namespace LenovoLegionToolkit.WPF.Pages;

public partial class WindowsOptimizationPage
{
    private IPackageDownloader? _driverPackageDownloader;
    private CancellationTokenSource? _driverGetPackagesTokenSource;
    private CancellationTokenSource? _driverFilterDebounceCancellationTokenSource;
    private List<Package>? _driverPackages;

    private async void InitializeDriverDownloadPage()
    {
        if (_driverOsComboBox != null && _driverOsComboBox.Items.Count == 0)
                _driverOsComboBox.SetItems(Enum.GetValues<OS>(), OSExtensions.GetCurrent(), os => os.GetDisplayName());

            if (_driverMachineTypeTextBox != null && string.IsNullOrWhiteSpace(_driverMachineTypeTextBox.Text))
            {
                try
                {
                    var machineInfo = await Compatibility.GetMachineInformationAsync();
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
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }

    private void DriverSourceRadio_Checked(object sender, RoutedEventArgs e)
    {
        // Source selection is handled when clicking Search
    }

    private async void DriverSearchButton_Click(object sender, RoutedEventArgs e)
    {
        await DriverDownloadPackagesButton_Click(sender, e);
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
                Dispatcher.Invoke(() => DriverReload());
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

    private async Task DriverDownloadPackagesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ShouldInterruptDriverDownloadsIfRunning())
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
                    Resource.PackagesPage_DownloadFailed_Message);
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
            Dispatcher.Invoke(() => DriverReload());

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
            Dispatcher.Invoke(() =>
            {
                if (!errorOccurred && _driverLoadingIndicator != null)
                    _driverLoadingIndicator.Visibility = Visibility.Collapsed;

                if (errorOccurred && _driverPackagesStackPanel != null)
                    _driverPackagesStackPanel.Children.Clear();
            });
        }
    }

    private void DriverReload()
    {
        if (_driverPackageDownloader is null || _driverPackagesStackPanel == null)
            return;

        // Clear existing children
        _driverPackagesStackPanel.Children.Clear();

        if (_driverPackages is null || _driverPackages.Count == 0)
            return;

        var packages = DriverSortAndFilter(_driverPackages);

        // Pre-allocate list to reduce allocations during iteration
        var controlsToAdd = new List<UIElement>(packages.Count);

        foreach (var package in packages)
        {
            var control = new PackageControl(_driverPackageDownloader, package, GetDriverDownloadLocation)
            {
                ContextMenu = GetDriverContextMenu(package, packages)
            };

            control.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PackageControl.IsSelected))
                {
                    UpdateSelectedDriverPackages();
                }
                else if (e.PropertyName == nameof(PackageControl.Status) ||
                         e.PropertyName == nameof(PackageControl.IsDownloading))
                {
                    if (control.Status == PackageControl.PackageStatus.Completed)
                        control.Visibility = Visibility.Collapsed;
                }
            };

            controlsToAdd.Add(control);
        }

        // Batch add controls to reduce UI updates
        foreach (var control in controlsToAdd)
        {
            _driverPackagesStackPanel.Children.Add(control);
        }

        UpdateSelectedDriverPackages();

        // Update visibility for selected packages
        foreach (var selectedPackage in ViewModel.SelectedDriverPackages)
        {
            if (selectedPackage?.IsCompleted == true && selectedPackage._sourcePackageControl != null)
                selectedPackage._sourcePackageControl.Visibility = Visibility.Collapsed;
        }

        if (packages.Count == 0)
        {
            var tb = new TextBlock
            {
                Text = Resource.PackagesPage_NoMatchingDownloads,
                Foreground = (SolidColorBrush)FindResource("TextFillColorSecondaryBrush"),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new(0, 32, 0, 32),
                Focusable = true
            };
            _driverPackagesStackPanel.Children.Add(tb);
        }
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
            result = result.Where(p => p.Index.Contains(filterText, StringComparison.InvariantCultureIgnoreCase));

        return result.ToList();
    }

    private void UpdateSelectedDriverPackages()
    {
        if (_driverPackagesStackPanel?.Children == null)
            return;

        var newSelectedPackages = new List<SelectedDriverPackageViewModel>();
        foreach (var child in _driverPackagesStackPanel.Children.OfType<PackageControl>())
        {
            if (child.IsSelected)
            {
                var existing = ViewModel.SelectedDriverPackages.FirstOrDefault(p => p.PackageId == child.Package.Id);
                if (existing != null)
                    newSelectedPackages.Add(existing);
                else
                    newSelectedPackages.Add(new SelectedDriverPackageViewModel(child.Package.Id, child.Package.Title, child.Package.Description, child.Package.Category, child));
            }
        }

        foreach (var existing in ViewModel.SelectedDriverPackages.ToList())
        {
            if (!newSelectedPackages.Any(p => p.PackageId == existing.PackageId))
            {
                existing.Dispose();
                ViewModel.SelectedDriverPackages.Remove(existing);
            }
        }

        foreach (var newPackage in newSelectedPackages)
        {
            if (!ViewModel.SelectedDriverPackages.Any(p => p.PackageId == newPackage.PackageId))
                ViewModel.SelectedDriverPackages.Add(newPackage);
        }

        ViewModel.NotifyDriverSelectionChanged();
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
            SymbolIcon = SymbolRegular.EyeOff24,
            Header = Resource.Hide,
        };
        hideMenuItem.Click += (_, _) =>
        {
            _packageDownloaderSettings.Store.HiddenPackages.Add(package.Id);
            _packageDownloaderSettings.SynchronizeStore();
            DriverReload();
        };

        var hideAllMenuItem = new MenuItem
        {
            SymbolIcon = SymbolRegular.EyeOff24,
            Header = Resource.HideAll,
        };
        hideAllMenuItem.Click += (_, _) =>
        {
            foreach (var id in packages.Select(p => p.Id))
                _packageDownloaderSettings.Store.HiddenPackages.Add(id);
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

        if (_driverPackagesStackPanel.Children.OfType<PackageControl>().Where(pc => pc.IsDownloading).Count() == 0)
            return true;

        return await MessageBoxHelper.ShowAsync(this, Resource.PackagesPage_DownloadInProgress_Title, Resource.PackagesPage_DownloadInProgress_Message);
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

    
}
