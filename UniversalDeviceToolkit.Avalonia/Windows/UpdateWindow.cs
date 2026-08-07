#if WINDOWS
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Startup;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows;

/// <summary>
/// Avalonia counterpart of the WPF <c>UpdateWindow</c>: shows the newest release
/// notes and downloads the installer through the shared <see cref="UpdateChecker"/>
/// before handing off to the silent installer flow.
/// </summary>
internal sealed class AvaloniaUpdateWindow : Window
{
    private static readonly Regex InstallerNamePattern = new(
        @"^UniversalDeviceToolkitSetup.*\.exe$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ImagePattern = new(@"!\[([^\]]*)\]\([^)]*\)", RegexOptions.Compiled);
    private static readonly Regex LinkPattern = new(@"\[([^\]]*)\]\([^)]*\)", RegexOptions.Compiled);
    private static readonly Regex BoldPattern = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
    private static readonly Regex BoldUnderscorePattern = new(@"__([^_]+)__", RegexOptions.Compiled);
    private static readonly Regex ItalicPattern = new(@"\*([^*]+)\*", RegexOptions.Compiled);
    private static readonly Regex ItalicUnderscorePattern = new(@"_([^_]+)_", RegexOptions.Compiled);
    private static readonly Regex InlineCodePattern = new(@"`([^`]+)`", RegexOptions.Compiled);
    private static readonly Regex HeadingPattern = new(@"^#{1,6}\s+", RegexOptions.Compiled);
    private static readonly Regex BlockquotePattern = new(@"^\s*>\s?", RegexOptions.Compiled);
    private static readonly Regex ListMarkerPattern = new(@"^\s*[-*+]\s+", RegexOptions.Compiled);
    private static readonly Regex HorizontalRulePattern = new(@"^\s*(-\s*){3,}$", RegexOptions.Compiled);
    private static readonly Regex SetextUnderlinePattern = new(@"^\s*(=+|-+)\s*$", RegexOptions.Compiled);

    private readonly UpdateChecker _updateChecker;
    private readonly TextBlock _versionBadgeText = new();
    private readonly TextBlock _releaseDateText = new();
    private readonly TextBlock _changelogText = new();
    private readonly TextBlock _statusText = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Button _cancelButton;
    private readonly Button _installButton;
    private CancellationTokenSource? _downloadCts;
    private bool _isDownloading;

    public AvaloniaUpdateWindow(UpdateReleaseInfo update)
    {
        _updateChecker = IoCContainer.TryResolve<UpdateChecker>()
            ?? throw new InvalidOperationException("The update checker is not initialized.");

        Title = Get("UpdateWindow_Title", "Update");
        Width = 680;
        Height = 540;
        MinWidth = 640;
        MinHeight = 480;
        MaxWidth = 720;
        MaxHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        AutomationProperties.SetAutomationId(this, "AvaloniaUpdateWindow");
        AutomationProperties.SetName(this, Title);

        _versionBadgeText.Text = string.IsNullOrWhiteSpace(update.TagName)
            ? $"v{update.Version}"
            : update.TagName;
        _versionBadgeText.Foreground = GetBrush("StatusInfoBrush", Colors.CornflowerBlue);
        _releaseDateText.Text = update.Date.ToString("D", LocalizationRuntime.CurrentCulture);
        _releaseDateText.Foreground = GetBrush("TextFillColorSecondaryBrush", Colors.Gray);

        var changelog = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(update.Title))
            changelog.AppendLine(update.Title).AppendLine();
        changelog.Append(update.Description);
        _changelogText.Text = StripMarkdown(changelog.ToString());
        _changelogText.TextWrapping = TextWrapping.Wrap;
        _changelogText.LineHeight = 20;

        _statusText.TextWrapping = TextWrapping.Wrap;
        _statusText.IsVisible = false;

        _progressBar.Minimum = 0;
        _progressBar.Maximum = 1;
        _progressBar.Height = 4;
        _progressBar.IsIndeterminate = true;
        _progressBar.IsVisible = false;

        _cancelButton = CreateButton(Get("Cancel", "Cancel"), CancelButton_Click, "AvaloniaUpdateWindowCancel");
        _installButton = CreateButton(Get("Update", "Update"), InstallButton_Click, "AvaloniaUpdateWindowInstall");
        _installButton.IsEnabled = true;

        Closed += (_, _) =>
        {
            _downloadCts?.Cancel();
            _downloadCts?.Dispose();
            _downloadCts = null;
        };

        Content = BuildContent();
    }

    private Control BuildContent()
    {
        var icon = new NavigationIcon
        {
            IconIdentifier = "ArrowSync24",
            FontSize = (double)TryResource("IconSizeLG", 24d)!,
            Foreground = GetBrush("StatusInfoBrush", Colors.CornflowerBlue),
        };
        var iconHost = new Border
        {
            Width = 48,
            Height = 48,
            Margin = new Thickness(0, 0, 16, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Background = GetBrush("CardBackgroundBrush", new SolidColorBrush(Color.FromArgb(255, 43, 43, 43))),
            BorderBrush = GetBrush("CardBorderBrush", new SolidColorBrush(Color.FromArgb(51, 255, 255, 255))),
            BorderThickness = new Thickness(1),
            CornerRadius = (CornerRadius)TryResource("CornerRadiusControl", new CornerRadius(12))!,
            Child = icon,
        };

        var whatsNew = new TextBlock
        {
            Text = Get("UpdateWindow_WhatsNew", "What's new"),
            FontSize = (double)TryResource("FontSizeSection", 19d)!,
            FontWeight = FontWeight.SemiBold,
            Foreground = GetBrush("TextFillColorPrimaryBrush", Colors.White),
        };
        var title = new TextBlock
        {
            Text = Get("UpdateWindow_Title", "Update"),
            Margin = new Thickness(0, 4, 0, 0),
            FontSize = (double)TryResource("FontSizeBody", 15d)!,
            Foreground = GetBrush("TextFillColorSecondaryBrush", Colors.Gray),
        };

        var versionBadge = new Border
        {
            Padding = new Thickness(10, 4),
            Margin = new Thickness(0, 10, 8, 0),
            Background = GetBrush("StatusInfoBackgroundBrush", new SolidColorBrush(Color.FromArgb(51, 79, 157, 247))),
            BorderBrush = GetBrush("CardBorderBrush", new SolidColorBrush(Color.FromArgb(51, 255, 255, 255))),
            BorderThickness = new Thickness(1),
            CornerRadius = (CornerRadius)TryResource("CornerRadiusControl", new CornerRadius(12))!,
            Child = _versionBadgeText,
        };
        var dateBadge = new Border
        {
            Padding = new Thickness(10, 4),
            Margin = new Thickness(0, 10, 0, 0),
            Background = GetBrush("CardBackgroundBrush", new SolidColorBrush(Color.FromArgb(255, 43, 43, 43))),
            BorderBrush = GetBrush("CardBorderBrush", new SolidColorBrush(Color.FromArgb(51, 255, 255, 255))),
            BorderThickness = new Thickness(1),
            CornerRadius = (CornerRadius)TryResource("CornerRadiusControl", new CornerRadius(12))!,
            Child = _releaseDateText,
        };
        var badges = new StackPanel { Orientation = Orientation.Horizontal };
        badges.Children.Add(versionBadge);
        badges.Children.Add(dateBadge);

        var headerCopy = new StackPanel { Spacing = 0, MinWidth = 0 };
        headerCopy.Children.Add(whatsNew);
        headerCopy.Children.Add(title);
        headerCopy.Children.Add(badges);

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        header.Children.Add(iconHost);
        Grid.SetColumn(headerCopy, 1);
        header.Children.Add(headerCopy);

        _changelogText.Foreground = GetBrush("TextFillColorPrimaryBrush", Colors.White);
        var changelogCard = new Border
        {
            Padding = new Thickness(16, 14),
            Background = GetBrush("CardBackgroundBrush", new SolidColorBrush(Color.FromArgb(255, 43, 43, 43))),
            BorderBrush = GetBrush("CardBorderBrush", new SolidColorBrush(Color.FromArgb(51, 255, 255, 255))),
            BorderThickness = new Thickness(1),
            CornerRadius = (CornerRadius)TryResource("CornerRadiusCard", new CornerRadius(18))!,
            Child = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _changelogText,
            },
        };

        var actions = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 10 };
        actions.Children.Add(new Panel());
        Grid.SetColumn(_cancelButton, 1);
        actions.Children.Add(_cancelButton);
        Grid.SetColumn(_installButton, 2);
        actions.Children.Add(_installButton);

        var content = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto,Auto"), RowSpacing = 14 };
        content.Children.Add(header);
        Grid.SetRow(changelogCard, 1);
        content.Children.Add(changelogCard);
        Grid.SetRow(_progressBar, 2);
        content.Children.Add(_progressBar);
        Grid.SetRow(_statusText, 3);
        content.Children.Add(_statusText);
        Grid.SetRow(actions, 4);
        content.Children.Add(actions);

        return new Border { Padding = new Thickness(24, 20), Child = content };
    }

    private Button CreateButton(string label, EventHandler<RoutedEventArgs> click, string automationId)
    {
        var button = new Button
        {
            Content = label,
            MinWidth = (double)TryResource("ButtonMinWidthStandard", 120d)!,
            Height = (double)TryResource("ButtonHeightStandard", 36d)!,
        };
        button.Click += click;
        AutomationProperties.SetAutomationId(button, automationId);
        AutomationProperties.SetName(button, label);
        return button;
    }

    private async void InstallButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_downloadCts is not null)
            {
                _downloadCts.Cancel();
                _downloadCts.Dispose();
            }

            _downloadCts = new CancellationTokenSource();
            var token = _downloadCts.Token;
            SetDownloading(true);

            var path = await _updateChecker.DownloadLatestUpdateAsync(
                new Progress<float>(value => Dispatcher.UIThread.Post(() => UpdateProgress(value))),
                token);

            SetDownloading(false);

            if (!IsAllowedInstallerPath(path))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Refusing to launch installer with disallowed path or name: {path}");
                ShowError(string.Format(
                    Get("UpdateWindow_InvalidInstaller", "The downloaded file could not be validated: {0}"),
                    path));
                OpenReleasesPage();
                return;
            }

            LaunchInstaller(path);
        }
        catch (OperationCanceledException)
        {
            SetDownloading(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Avalonia update download failed.", ex);
            SetDownloading(false);
            ShowError(ex.Message);
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isDownloading)
        {
            _downloadCts?.Cancel();
            return;
        }

        Close();
    }

    private void LaunchInstaller(string path)
    {
        var language = LocalizationRuntime.CurrentCulture.Name.Replace("-", string.Empty);
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = path,
                Arguments = $"/SILENT /RESTARTAPPLICATIONS /LANG={language}",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to launch the update installer.", ex);
            ShowError(ex.Message);
            return;
        }

        Close();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { } desktop)
            desktop.Shutdown(0);
    }

    private void OpenReleasesPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppIdentity.RepositoryUrl,
                UseShellExecute = true,
            });
        }
        catch
        {
            // Opening the release page is best effort.
        }
    }

    private static bool IsAllowedInstallerPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            var fileName = Path.GetFileName(fullPath);
            return InstallerNamePattern.IsMatch(fileName);
        }
        catch
        {
            return false;
        }
    }

    private void SetDownloading(bool downloading)
    {
        _isDownloading = downloading;
        _installButton.IsEnabled = !downloading;
        _progressBar.IsVisible = downloading;
        _progressBar.IsIndeterminate = downloading;
        _progressBar.Value = 0;

        _statusText.IsVisible = downloading;
        _statusText.Foreground = GetBrush("TextFillColorSecondaryBrush", Colors.Gray);
        _statusText.Text = downloading
            ? Get("UpdateWindow_Downloading", "Downloading update...")
            : Get("UpdateWindow_ReadyToInstall", "Ready to install");
    }

    private void UpdateProgress(float value)
    {
        if (!_isDownloading)
            return;

        _progressBar.IsIndeterminate = value <= 0;
        _progressBar.Value = Math.Clamp(value, 0, 1);
    }

    private void ShowError(string message)
    {
        _statusText.IsVisible = true;
        _statusText.Foreground = GetBrush("StatusCriticalBrush", Colors.OrangeRed);
        _statusText.Text = message;
        _progressBar.IsVisible = false;
    }

    internal static string StripMarkdown(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var text = markdown;
        text = ImagePattern.Replace(text, "$1");
        text = LinkPattern.Replace(text, "$1");
        text = BoldPattern.Replace(text, "$1");
        text = BoldUnderscorePattern.Replace(text, "$1");
        text = ItalicPattern.Replace(text, "$1");
        text = ItalicUnderscorePattern.Replace(text, "$1");
        text = InlineCodePattern.Replace(text, "$1");

        var lines = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line =>
            {
                var trimmed = line.TrimStart();
                if (HorizontalRulePattern.IsMatch(trimmed) || SetextUnderlinePattern.IsMatch(trimmed))
                    return string.Empty;
                if (HeadingPattern.IsMatch(trimmed))
                    return HeadingPattern.Replace(trimmed, string.Empty).TrimStart();
                if (BlockquotePattern.IsMatch(trimmed))
                    return BlockquotePattern.Replace(trimmed, string.Empty).TrimStart();
                if (ListMarkerPattern.IsMatch(trimmed))
                    return "\u2022 " + ListMarkerPattern.Replace(trimmed, string.Empty).TrimStart();
                return line;
            });

        return string.Join(Environment.NewLine, lines).Trim();
    }

    private IBrush GetBrush(string key, Color fallbackColor) =>
        TryResource(key, null) is IBrush brush ? brush : new SolidColorBrush(fallbackColor);

    private IBrush GetBrush(string key, IBrush fallback) =>
        TryResource(key, null) is IBrush brush ? brush : fallback;

    private object? TryResource(object key, object? fallback) =>
        this.TryFindResource(key, out var resource) ? resource : fallback;

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);
}
#endif
