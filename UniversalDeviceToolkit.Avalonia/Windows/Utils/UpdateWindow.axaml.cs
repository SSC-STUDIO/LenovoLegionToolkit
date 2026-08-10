using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Markdig;
using Markdig.Syntax;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Windows.Utils
{
public partial class UpdateWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow, IProgress<float>
{
    private readonly UpdateChecker _updateChecker = IoCContainer.Resolve<UpdateChecker>();

    private CancellationTokenSource? _downloadCancellationTokenSource;

    private static readonly MarkdownPipeline MarkdownPipeline =
        new MarkdownPipelineBuilder().DisableHtml().Build();

    // AVALONIA: Markdig.Wpf is not available; Markdig core is used and inline markers
    // (bold/italic/code/strikethrough/links) are flattened for the plain TextBlock renderer.
    private static readonly Regex InlineMarkersRegex = new(@"(\*\*|__|~~|`|\*|_)", RegexOptions.Compiled);
    private static readonly Regex InlineLinkRegex = new(@"\[([^\]]*)\]\([^)]*\)", RegexOptions.Compiled);

    public static readonly StyledProperty<bool> HasUpdatesProperty =
        AvaloniaProperty.Register<UpdateWindow, bool>(nameof(HasUpdates), false);

    public static readonly StyledProperty<bool> IsDownloadingProperty =
        AvaloniaProperty.Register<UpdateWindow, bool>(nameof(IsDownloading), false);

    public bool HasUpdates
    {
        get => GetValue(HasUpdatesProperty);
        set => SetValue(HasUpdatesProperty, value);
    }

    public bool IsDownloading
    {
        get => GetValue(IsDownloadingProperty);
        set => SetValue(IsDownloadingProperty, value);
    }

    public UpdateWindow() => InitializeComponent();

    private async void UpdateWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var updates = await _updateChecker.GetUpdatesAsync();
            if (updates.Length == 0)
                return;

            var latest = updates[0];
            _versionBadgeText.Text = string.IsNullOrWhiteSpace(latest.TagName)
                ? $"v{latest.Version}"
                : latest.TagName;
            _versionBadge.IsVisible = true;

            _releaseDateText.Text = latest.Date.ToString("D");
            _releaseDateBadge.IsVisible = true;

            var stringBuilder = new StringBuilder();
            foreach (var update in updates)
            {
                stringBuilder.AppendLine("### " + update.Title)
                    .AppendLine()
                    .AppendLine(update.Description)
                    .AppendLine();
            }

            RenderMarkdown(stringBuilder.ToString());

            HasUpdates = true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(UpdateWindow_Loaded)}.", ex);
        }
    }

    private void UpdateWindow_Closing(object? sender, WindowClosingEventArgs e)
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

            if (!IsAllowedInstallerPath(path))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Refusing to launch installer with disallowed path or name: {path}");
                SetDownloading(false);
                Constants.LatestReleaseUri.Open();
                Close();
                return;
            }

            using var process = Process.Start(path, $"/SILENT /RESTARTAPPLICATIONS /LANG={Resource.Culture.Name.Replace("-", string.Empty)}");
            await App.Current.ShutdownAsync(true);
        }
        catch (OperationCanceledException)
        {
            SetDownloading(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(DownloadButton_Click)}.", ex);

            SetDownloading(false);

            Constants.LatestReleaseUri.Open();
            Close();
        }
    }

    private static readonly Regex InstallerNamePattern = new(@"^UniversalDeviceToolkitSetup.*\.exe$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool IsAllowedInstallerPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(path);
            var fileName = Path.GetFileName(fullPath);

            if (!InstallerNamePattern.IsMatch(fileName))
                return false;

            return string.Equals(Path.GetExtension(fileName), ".exe", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void CancelDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsDownloading)
        {
            _downloadCancellationTokenSource?.Cancel();
            return;
        }

        Close();
    }

    private void SetDownloading(bool isDownloading)
    {
        IsDownloading = isDownloading;
        _downloadButton.IsEnabled = !isDownloading && HasUpdates;

        if (!isDownloading)
        {
            _downloadProgressBar.Value = 0;
            _downloadProgressBar.IsIndeterminate = true;
        }
    }

    public void Report(float value) => Dispatcher.UIThread.Invoke(() =>
    {
        _downloadProgressBar.IsIndeterminate = !(value > 0);
        _downloadProgressBar.Value = value;
    });

    /// <summary>
    /// Renders release-notes markdown into the <see cref="_markdownContainer"/> panel:
    /// headings scale by level, lists are indented, fenced/code blocks use a monospace
    /// font and block quotes get a left accent border.
    /// </summary>
    private void RenderMarkdown(string markdown)
    {
        _markdownContainer.Children.Clear();

        if (string.IsNullOrWhiteSpace(markdown))
            return;

        var document = Markdown.Parse(markdown, MarkdownPipeline);
        foreach (var block in document)
            RenderMarkdownBlock(block);
    }

    private void RenderMarkdownBlock(Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                AddTextBlock(
                    CleanInline(heading.Inline),
                    fontSize: heading.Level <= 1 ? ResolveDouble("FontSizeSubsection", 17) : ResolveDouble("FontSizeBody", 15),
                    fontWeight: FontWeight.Medium,
                    margin: new Thickness(0, heading.Level <= 1 ? 4 : 2, 0, 8));
                break;

            case ParagraphBlock paragraph:
                AddTextBlock(CleanInline(paragraph.Inline), lineHeight: 22);
                break;

            case ListBlock list:
                foreach (var item in list)
                {
                    if (item is ListItemBlock listItem && listItem.LastChild is LeafBlock leaf)
                        AddTextBlock("•  " + CleanInline(leaf.Inline), margin: new Thickness(18, 0, 0, 4));
                }
                break;

            case FencedCodeBlock fenced:
                AddCodeBlock(fenced.Lines.ToString());
                break;

            case CodeBlock code:
                AddCodeBlock(code.Lines.ToString());
                break;

            case QuoteBlock quote:
                foreach (var child in quote)
                {
                    if (child is LeafBlock quoteLeaf)
                    {
                        var border = new Border
                        {
                            Margin = new Thickness(0, 0, 0, 10),
                            Padding = new Thickness(12, 0, 0, 0),
                            BorderThickness = new Thickness(3, 0, 0, 0),
                            Child = CreateTextBlock(CleanInline(quoteLeaf.Inline), lineHeight: 20)
                        };
                        border.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
                        _markdownContainer.Children.Add(border);
                    }
                }
                break;
        }
    }

    private void AddTextBlock(string text, double? fontSize = null, FontWeight? fontWeight = null,
        Thickness? margin = null, double? lineHeight = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var textBlock = CreateTextBlock(text, fontSize, fontWeight, margin, lineHeight);
        _markdownContainer.Children.Add(textBlock);
    }

    private TextBlock CreateTextBlock(string text, double? fontSize = null, FontWeight? fontWeight = null,
        Thickness? margin = null, double? lineHeight = null)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = fontSize ?? ResolveDouble("FontSizeBody", 15),
            FontWeight = fontWeight ?? FontWeight.Normal,
            Margin = margin ?? new Thickness(0, 0, 0, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (lineHeight.HasValue)
            textBlock.LineHeight = lineHeight.Value;

        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        return textBlock;
    }

    private void AddCodeBlock(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return;

        var border = new Border
        {
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(10, 8),
            BorderThickness = new Thickness(1),
            CornerRadius = ResolveCornerRadius("CornerRadiusCompact", 8),
            Child = new TextBlock
            {
                Text = code.TrimEnd(),
                FontFamily = new FontFamily("Consolas, Cascadia Mono, Courier New"),
                FontSize = ResolveDouble("FontSizeSmallBody", 13),
                TextWrapping = TextWrapping.Wrap
            }
        };
        border.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        _markdownContainer.Children.Add(border);
    }

    private static string CleanInline(Markdig.Syntax.Inlines.Inline? inline)
    {
        if (inline is null)
            return string.Empty;

        var text = inline.ToString();
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = InlineLinkRegex.Replace(text, "$1");
        text = InlineMarkersRegex.Replace(text, string.Empty);
        return text.Trim();
    }

    private static double ResolveDouble(string key, double fallback) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is double d ? d : fallback;

    private static CornerRadius ResolveCornerRadius(string key, double fallback) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is CornerRadius radius ? radius : new CornerRadius(fallback);
}
}
