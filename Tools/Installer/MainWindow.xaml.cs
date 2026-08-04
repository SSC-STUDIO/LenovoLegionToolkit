using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Installer;

public partial class MainWindow : Window
{
    private enum WizardPage
    {
        Welcome,
        Language,
        Device,
        Location,
        Progress,
        Done,
        UninstallConfirm,
    }

    private sealed record DeviceChoice(string? Id, string Display, bool IsHardware)
    {
        public bool AskLater => Id is null;
        public bool IsBasicMode => Id is not null && !IsHardware;
    }

    private readonly InstallerArguments _args;
    private WizardPage _currentPage;
    private CancellationTokenSource? _cts;
    private bool _operationRunning;
    private bool _forceClose;
    private bool _lastOperationFailed;
    private bool _languagePageInitialized;
    private bool _devicePageInitialized;
    private bool _updatingLanguage;

    public MainWindow(InstallerArguments args)
    {
        _args = args;
        InitializeComponent();
        ApplyTextDirection();
        LocalizeStaticText();

        Title = Strings.Get("WindowTitle");
        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(
            new Uri("pack://application:,,,/Icon.ico"));
        FooterVersion.Text = $"v{PayloadManifest.Version}";

        if (_args.Uninstall)
            ShowUninstallConfirm();
        else
            ShowWelcome();
    }

    private void LocalizeStaticText()
    {
        BackButton.Content = Strings.Get("Back");
        CancelButton.Content = Strings.Get("Cancel");
        BrowseButton.Content = Strings.Get("Browse");
        DesktopShortcutCheck.Content = Strings.Get("DesktopShortcut");
        DeleteDataCheck.Content = Strings.Get("DeleteData");
        LaunchCheck.Content = Strings.Get("LaunchApp");
        InstallRuntimeButton.Content = Strings.Get("InstallRuntime");
        LocationTitle.Text = Strings.Get("LocationTitle");
        LocationText.Text = Strings.Get("LocationText");
        ProgressDetail.Text = "";
    }

    // ---------- Page switching ----------

    private void SwitchTo(WizardPage page)
    {
        _currentPage = page;
        PageWelcome.Visibility = page == WizardPage.Welcome ? Visibility.Visible : Visibility.Collapsed;
        PageLanguage.Visibility = page == WizardPage.Language ? Visibility.Visible : Visibility.Collapsed;
        PageDevice.Visibility = page == WizardPage.Device ? Visibility.Visible : Visibility.Collapsed;
        PageLocation.Visibility = page == WizardPage.Location ? Visibility.Visible : Visibility.Collapsed;
        PageProgress.Visibility = page == WizardPage.Progress ? Visibility.Visible : Visibility.Collapsed;
        PageDone.Visibility = page == WizardPage.Done ? Visibility.Visible : Visibility.Collapsed;
        PageUninstallConfirm.Visibility = page == WizardPage.UninstallConfirm ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowWelcome()
    {
        SwitchTo(WizardPage.Welcome);
        TitleBarText.Text = $"Universal Device Toolkit - {Strings.Get("Install")}";
        WelcomeTitle.Text = Strings.Get("WelcomeTitle");
        WelcomeText.Text = Strings.Format("WelcomeText", PayloadManifest.Version);
        NextButton.Content = Strings.Get("Next");
        BackButton.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Visible;
        NextButton.Visibility = Visibility.Visible;

        if (InstallerEngine.FindLegacyInnoUninstallString() is not null || IsAlreadyInstalled())
        {
            UpgradeNotice.Visibility = Visibility.Visible;
            UpgradeNoticeText.Text = Strings.Get("UpgradeDetected");
        }

        CheckRuntimeAsync();
    }

    private void ShowLanguage()
    {
        SwitchTo(WizardPage.Language);
        if (!_languagePageInitialized)
        {
            _languagePageInitialized = true;
            LanguageTitle.Text = Strings.Get("LanguageTitle");
            LanguageText.Text = Strings.Get("LanguageText");
            LanguageCombo.ItemsSource = AppLanguages.All;
            LanguageCombo.SelectedItem = AppLanguages.All.FirstOrDefault(language =>
                language.Culture.Equals(LocalizationRuntime.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase))
                ?? AppLanguages.GetPreferred();
        }

        LanguageTitle.Text = Strings.Get("LanguageTitle");
        LanguageText.Text = Strings.Get("LanguageText");
        NextButton.Content = Strings.Get("Next");
        BackButton.Visibility = Visibility.Visible;
        CancelButton.Visibility = Visibility.Visible;
        NextButton.Visibility = Visibility.Visible;
    }

    private async void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingLanguage || LanguageCombo.SelectedItem is not AppLanguage language)
            return;

        try
        {
            _updatingLanguage = true;
            var culture = await LocalizationRuntime.SetCultureAsync(language.Culture, persist: false);
            Strings.ApplyCulture(culture);
            ApplyTextDirection();
            LocalizeStaticText();

            switch (_currentPage)
            {
                case WizardPage.Welcome:
                    ShowWelcome();
                    break;
                case WizardPage.Language:
                    ShowLanguage();
                    break;
                case WizardPage.Device:
                    _devicePageInitialized = false;
                    ShowDevice();
                    break;
                case WizardPage.Location:
                    ShowLocation();
                    break;
                case WizardPage.UninstallConfirm:
                    ShowUninstallConfirm();
                    break;
            }
        }
        finally
        {
            _updatingLanguage = false;
        }
    }

    private void ApplyTextDirection() =>
        FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;

    private void ShowDevice()
    {
        SwitchTo(WizardPage.Device);
        if (!_devicePageInitialized)
        {
            _devicePageInitialized = true;
            DeviceTitle.Text = Strings.Get("DeviceTitle");
            DeviceText.Text = Strings.Get("DeviceText");

            var machine = MachineDetector.Detect();
            DeviceDetectedText.Text = Strings.Format("DeviceDetected",
                string.IsNullOrWhiteSpace(machine.Vendor) && string.IsNullOrWhiteSpace(machine.ProductName)
                    ? "-"
                    : machine.ToString());

            var choices = DevicePackMatcher.BuildSelectable(machine)
                .Select(p => new DeviceChoice(p.Id, p.DisplayName, p.IsHardware))
                .ToList();
            choices.Add(new DeviceChoice(null, Strings.Get("DeviceAskLater"), IsHardware: false));
            DeviceCombo.ItemsSource = choices;

            var recommended = DevicePackMatcher.FindRecommended(machine);
            DeviceCombo.SelectedItem = choices.FirstOrDefault(c => c.Id == recommended.Id) ?? choices[0];
        }

        NextButton.Content = Strings.Get("Next");
        BackButton.Visibility = Visibility.Visible;
        CancelButton.Visibility = Visibility.Visible;
        NextButton.Visibility = Visibility.Visible;
    }

    private void DeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeviceCombo.SelectedItem is not DeviceChoice choice || choice.AskLater)
        {
            DeviceNote.Visibility = Visibility.Collapsed;
            return;
        }

        DeviceNoteText.Text = Strings.Get(choice.IsHardware ? "DeviceHardwareNote" : "DeviceBasicNote");
        DeviceNote.Visibility = Visibility.Visible;
    }

    private void ShowLocation()
    {
        SwitchTo(WizardPage.Location);
        var existingInstallDir = DetectExistingInstallDir();
        InstallDirBox.Text = _args.InstallDir
            ?? (existingInstallDir is not null && InstallerInstallPathPolicy.IsUnderProgramFiles(existingInstallDir)
                ? existingInstallDir
                : InstallerConstants.DefaultInstallDir);
        DesktopShortcutCheck.IsChecked = _args.DesktopShortcut;
        NextButton.Content = Strings.Get("Install");
        BackButton.Visibility = Visibility.Visible;
        CancelButton.Visibility = Visibility.Visible;
        NextButton.Visibility = Visibility.Visible;
    }

    private void ShowUninstallConfirm()
    {
        SwitchTo(WizardPage.UninstallConfirm);
        var installDir = _args.InstallDir ?? DetectExistingInstallDir() ?? InstallerConstants.DefaultInstallDir;
        InstallDirBox.Text = installDir; // reused later by the uninstall runner
        TitleBarText.Text = $"Universal Device Toolkit - {Strings.Get("Uninstall")}";
        UninstallConfirmTitle.Text = Strings.Get("UninstallConfirmTitle");
        UninstallConfirmText.Text = Strings.Format("UninstallConfirmText", installDir);
        NextButton.Content = Strings.Get("Uninstall");
        BackButton.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Visible;
        NextButton.Visibility = Visibility.Visible;
    }

    private void ShowProgress(bool uninstall)
    {
        SwitchTo(WizardPage.Progress);
        ProgressTitle.Text = Strings.Get(uninstall ? "UninstallProgressTitle" : "ProgressTitle");
        ProgressBar.Value = 0;
        ProgressBar.IsIndeterminate = false;
        ProgressStatus.Text = "";
        BackButton.Visibility = Visibility.Collapsed;
        NextButton.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Visible;
    }

    private void ShowDone(bool uninstall, string? error)
    {
        SwitchTo(WizardPage.Done);
        _lastOperationFailed = error is not null;
        BackButton.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Collapsed;
        NextButton.Visibility = Visibility.Visible;

        if (error is not null)
        {
            DoneTitle.Text = Strings.Get("ErrorTitle");
            DoneText.Text = Strings.Format("OperationFailedDetail", error);
            LaunchCheck.Visibility = Visibility.Collapsed;
            NextButton.Content = Strings.Get("Retry");
            CancelButton.Visibility = Visibility.Visible;
            CancelButton.Content = Strings.Get("Exit");
        }
        else if (uninstall)
        {
            DoneTitle.Text = Strings.Get("UninstallDoneTitle");
            DoneText.Text = Strings.Get("UninstallDoneText");
            LaunchCheck.Visibility = Visibility.Collapsed;
            NextButton.Content = Strings.Get("Finish");
        }
        else
        {
            DoneTitle.Text = Strings.Get("DoneTitle");
            DoneText.Text = Strings.Format("DoneText", PayloadManifest.Version);
            LaunchCheck.Visibility = Visibility.Visible;
            NextButton.Content = Strings.Get("Finish");
        }
    }

    // ---------- Runtime check ----------

    private async void CheckRuntimeAsync()
    {
        var ready = await Task.Run(() => InstallerEngine.TryGetDesktopRuntime(out _));
        UpdateRuntimeState(ready);
    }

    private void UpdateRuntimeState(bool ready)
    {
        RuntimePanel.Visibility = ready ? Visibility.Collapsed : Visibility.Visible;
        NextButton.IsEnabled = ready;
        if (!ready)
            RuntimeWarningText.Text = Strings.Format("RuntimeMissing", InstallerConstants.DotNetRuntimeMinimum);
    }

    private async void InstallRuntimeButton_Click(object sender, RoutedEventArgs e)
    {
        InstallRuntimeButton.IsEnabled = false;
        RuntimeStatusText.Text = Strings.Get("RuntimeInstalling");
        try
        {
            var progress = new Progress<EngineProgress>(p => RuntimeStatusText.Text = p.Status);
            await Task.Run(() => InstallerEngine.InstallDesktopRuntimeAsync(progress, CancellationToken.None));
            var ready = await Task.Run(() => InstallerEngine.TryGetDesktopRuntime(out _));
            UpdateRuntimeState(ready);
            RuntimeStatusText.Text = ready ? "" : Strings.Get("RuntimeFailed");
            if (ready)
                RuntimePanel.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            RuntimeStatusText.Text = Strings.Format("OperationFailedDetail", ex.Message);
        }
        finally
        {
            InstallRuntimeButton.IsEnabled = true;
        }
    }

    // ---------- Button handlers ----------

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        switch (_currentPage)
        {
            case WizardPage.Language:
                ShowWelcome();
                break;
            case WizardPage.Device:
                ShowLanguage();
                break;
            case WizardPage.Location:
                ShowDevice();
                break;
        }
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        switch (_currentPage)
        {
            case WizardPage.Welcome:
                ShowLanguage();
                break;
            case WizardPage.Language:
                ShowDevice();
                break;
            case WizardPage.Device:
                ShowLocation();
                break;
            case WizardPage.Location:
                await RunInstallAsync();
                break;
            case WizardPage.UninstallConfirm:
                await RunUninstallAsync();
                break;
            case WizardPage.Done:
                if (_lastOperationFailed)
                {
                    CancelButton.Content = Strings.Get("Cancel");
                    if (_args.Uninstall)
                        await RunUninstallAsync();
                    else
                        await RunInstallAsync();
                }
                else
                {
                    _forceClose = true;
                    Close();
                }
                break;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_operationRunning)
        {
            if (MessageBox.Show(this, Strings.Get("CancelConfirm"), Strings.Get("WindowTitle"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _cts?.Cancel();
            CancelButton.IsEnabled = false;
            return;
        }

        _forceClose = true;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    private void MinButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = Strings.Get("LocationTitle"),
            InitialDirectory = InstallDirBox.Text,
        };
        if (dialog.ShowDialog(this) == true)
            InstallDirBox.Text = dialog.FolderName;
    }

    // ---------- Operations ----------

    private async Task RunInstallAsync()
    {
        ShowProgress(uninstall: false);
        _operationRunning = true;
        _cts = new CancellationTokenSource();

        var progress = new Progress<EngineProgress>(p =>
        {
            if (p.Percent.HasValue)
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = Math.Clamp(p.Percent.Value, 0, 100);
            }
            else
            {
                ProgressBar.IsIndeterminate = true;
            }

            ProgressStatus.Text = p.Status;
        });

        try
        {
            var deviceChoice = DeviceCombo.SelectedItem as DeviceChoice;
            var options = new InstallOptions
            {
                InstallDir = InstallDirBox.Text.Trim(),
                CreateDesktopShortcut = DesktopShortcutCheck.IsChecked == true,
                LaunchAfterInstall = LaunchCheck.IsChecked == true,
                LanguageCulture = (LanguageCombo.SelectedItem as AppLanguage)?.Culture,
                DevicePackId = deviceChoice is { AskLater: false } ? deviceChoice.Id : null,
                DeviceBasicMode = deviceChoice?.IsBasicMode ?? false,
            };
            await Task.Run(() => InstallerEngine.InstallAsync(options, progress, _cts.Token));
            ShowDone(uninstall: false, error: null);
        }
        catch (OperationCanceledException)
        {
            _forceClose = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowDone(uninstall: false, error: ex.Message);
        }
        finally
        {
            _operationRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task RunUninstallAsync()
    {
        ShowProgress(uninstall: true);
        _operationRunning = true;
        _cts = new CancellationTokenSource();

        var progress = new Progress<EngineProgress>(p =>
        {
            if (p.Percent.HasValue)
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = Math.Clamp(p.Percent.Value, 0, 100);
            }
            else
            {
                ProgressBar.IsIndeterminate = true;
            }

            ProgressStatus.Text = p.Status;
        });

        try
        {
            var options = new UninstallOptions
            {
                InstallDir = InstallDirBox.Text.Trim(),
                DeleteAppData = DeleteDataCheck.IsChecked == true || _args.DeleteAppData,
            };
            await Task.Run(() => InstallerEngine.UninstallAsync(options, progress, _cts.Token));
            ShowDone(uninstall: true, error: null);
        }
        catch (OperationCanceledException)
        {
            _forceClose = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowDone(uninstall: true, error: ex.Message);
        }
        finally
        {
            _operationRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private static string? DetectExistingInstallDir()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{InstallerConstants.UninstallKeyName}");
            var location = key?.GetValue("InstallLocation") as string;
            if (!string.IsNullOrWhiteSpace(location) && Directory.Exists(location))
                return location;
        }
        catch
        {
            // Fall through.
        }

        return Directory.Exists(InstallerConstants.DefaultInstallDir)
            ? InstallerConstants.DefaultInstallDir
            : null;
    }

    private static bool IsAlreadyInstalled() =>
        File.Exists(Path.Combine(InstallerConstants.DefaultInstallDir, InstallerConstants.MainExeName)) ||
        DetectExistingInstallDir() is not null;

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_operationRunning && !_forceClose)
        {
            if (MessageBox.Show(this, Strings.Get("CancelConfirm"), Strings.Get("WindowTitle"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _cts?.Cancel();
            }

            e.Cancel = true; // the operation handler closes the window once unwound
            return;
        }

        base.OnClosing(e);
    }
}
