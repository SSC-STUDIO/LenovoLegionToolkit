using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.PackageDownloader;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.ViewModels;

namespace UniversalDeviceToolkit.WPF.Pages
{
    public partial class PackagesPage : Page
    {
        private readonly PackagesViewModel _viewModel;
        private readonly PackageDownloaderSettings _packageDownloaderSettings;
        private bool _initialized;

        public PackagesPage()
        {
            _viewModel = IoCContainer.Resolve<PackagesViewModel>();
            _packageDownloaderSettings = IoCContainer.Resolve<PackageDownloaderSettings>();

            DataContext = _viewModel;

            InitializeComponent();

            Loaded += PackagesPage_Loaded;
            Unloaded += PackagesPage_Unloaded;
        }

        private void PackagesPage_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;
            _initialized = true;
            _osComboBox.SetItems(Enum.GetValues<OS>(), OSExtensions.GetCurrent(), os => os.GetDisplayName());
            InitializeViewModel();
        }

        private async void InitializeViewModel()
        {
            try
            {
                _viewModel.MachineType = (await MachineCompatibility.GetMachineInformationAsync().ConfigureAwait(false)).MachineType;

                var downloadsFolder = KnownFolders.GetPath(KnownFolder.Downloads);
                _downloadToText.PlaceholderText = downloadsFolder;
                _downloadToText.Text = Directory.Exists(_packageDownloaderSettings.Store.DownloadPath)
                    ? _packageDownloaderSettings.Store.DownloadPath
                    : downloadsFolder;

                _viewModel.OnlyShowUpdates = _packageDownloaderSettings.Store.OnlyShowUpdates
                    && _viewModel.DownloaderType == PackageDownloaderFactory.Type.Vantage;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error initializing packages page.", ex);
            }
        }

        private void PackagesPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= PackagesPage_Unloaded;
            _viewModel.CancelLoadCommand.Execute(null);
        }

        private void OsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_osComboBox.TryGetSelectedItem(out OS os))
                _viewModel.SelectedOS = os;
        }

        private void SourceRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.RadioButton radio || radio.IsChecked != true)
                return;

            _viewModel.DownloaderType = radio == _sourcePrimaryRadio
                ? PackageDownloaderFactory.Type.Vantage
                : PackageDownloaderFactory.Type.PCSupport;
        }

        private void DownloadToText_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var location = _downloadToText.Text;

            if (!Directory.Exists(location))
                return;

            _packageDownloaderSettings.Store.DownloadPath = location;
            _packageDownloaderSettings.SynchronizeStore();
        }

        private void OpenDownloadToButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var location = GetDownloadLocation();

                if (!Directory.Exists(location))
                    return;

                using var process = Process.Start("explorer", location);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to open download location.", ex);
            }
        }

        private void DownloadToButton_Click(object sender, RoutedEventArgs e)
        {
            using var ofd = new FolderBrowserDialog();
            ofd.InitialDirectory = _downloadToText.Text;

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            var selectedPath = ofd.SelectedPath;
            _downloadToText.Text = selectedPath;
            _packageDownloaderSettings.Store.DownloadPath = selectedPath;
            _packageDownloaderSettings.SynchronizeStore();
        }

        private string GetDownloadLocation()
        {
            var location = _downloadToText.Text.Trim();

            if (!Directory.Exists(location))
            {
                var downloads = KnownFolders.GetPath(KnownFolder.Downloads);
                location = downloads;
                _downloadToText.Text = downloads;
                _packageDownloaderSettings.Store.DownloadPath = downloads;
                _packageDownloaderSettings.SynchronizeStore();
            }

            return location;
        }
    }
}
