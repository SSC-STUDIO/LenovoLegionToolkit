using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Utils;

internal enum PluginIconKind
{
    Symbol,
    Image,
    Monogram
}

internal sealed record PluginIconDescriptor(
    PluginIconKind Kind,
    SymbolRegular Symbol,
    string? ImagePath,
    string Monogram);

internal static class PluginIconResolver
{
    private static readonly string[] IconExtensions = [".png", ".jpg", ".jpeg", ".ico"];
    private static readonly string[] IconFileNames = ["icon", "plugin", "logo"];

    public static PluginIconDescriptor Resolve(
        string pluginId,
        string? pluginName,
        string? iconValue,
        string? metadataFilePath,
        string? pluginsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        var safePluginId = NormalizePluginId(pluginId);
        var monogram = CreateMonogram(pluginName, safePluginId);

        if (TryResolveImagePath(safePluginId, iconValue, metadataFilePath, pluginsDirectory, out var imagePath))
            return new PluginIconDescriptor(PluginIconKind.Image, SymbolRegular.Apps24, imagePath, monogram);

        if (TryResolveSymbol(iconValue, out var symbol))
            return new PluginIconDescriptor(PluginIconKind.Symbol, symbol, null, monogram);

        if (TryResolveImagePath(safePluginId, null, metadataFilePath, pluginsDirectory, out imagePath))
            return new PluginIconDescriptor(PluginIconKind.Image, SymbolRegular.Apps24, imagePath, monogram);

        return !string.IsNullOrWhiteSpace(monogram)
            ? new PluginIconDescriptor(PluginIconKind.Monogram, SymbolRegular.Apps24, null, monogram)
            : new PluginIconDescriptor(PluginIconKind.Symbol, SymbolRegular.Apps24, null, "P");
    }

    public static FrameworkElement CreateElement(PluginIconDescriptor descriptor, double symbolFontSize = 30, double monogramFontSize = 24)
    {
        if (descriptor.Kind == PluginIconKind.Image && !string.IsNullOrWhiteSpace(descriptor.ImagePath))
        {
            try
            {
                return CreateImage(descriptor.ImagePath);
            }
            catch (Exception ex) when (IsRecoverableImageException(ex))
            {
                return !string.IsNullOrWhiteSpace(descriptor.Monogram)
                    ? CreateMonogramElement(descriptor.Monogram, monogramFontSize)
                    : CreateSymbolElement(SymbolRegular.Apps24, symbolFontSize);
            }
        }

        return descriptor.Kind switch
        {
            PluginIconKind.Monogram => CreateMonogramElement(descriptor.Monogram, monogramFontSize),
            _ => CreateSymbolElement(descriptor.Symbol, symbolFontSize)
        };
    }

    public static string ResolvePluginsDirectory()
    {
        var overridePath = PluginPaths.GetPluginsDirectoryOverride();
        if (!string.IsNullOrWhiteSpace(overridePath))
            return overridePath;

        var appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        var possiblePaths = new[]
        {
            Path.Combine(appBaseDir, "plugins"),
            Path.Combine(appBaseDir, "Plugins"),
            Path.Combine(appBaseDir, "build", "plugins"),
            Path.Combine(appBaseDir, "Build", "plugins"),
            Path.Combine(appBaseDir, "..", "..", "..", "build", "plugins"),
            Path.Combine(appBaseDir, "..", "..", "..", "Build", "plugins"),
            Path.Combine(appBaseDir, "..", "build", "plugins"),
            Path.Combine(appBaseDir, "..", "Build", "plugins"),
        };

        foreach (var possiblePath in possiblePaths)
        {
            var fullPath = Path.GetFullPath(possiblePath);
            if (Directory.Exists(fullPath))
                return fullPath;
        }

        return Path.Combine(appBaseDir, "build", "plugins");
    }

    private static FrameworkElement CreateImage(string imagePath)
    {
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.UriSource = new Uri(imagePath, UriKind.Absolute);
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return new Image
        {
            Source = bitmapImage,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static FrameworkElement CreateSymbolElement(SymbolRegular symbol, double fontSize)
    {
        var icon = new SymbolIcon
        {
            Symbol = symbol,
            FontSize = fontSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        icon.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "SystemAccentColorBrush");
        return icon;
    }

    private static FrameworkElement CreateMonogramElement(string monogram, double fontSize)
    {
        return new TextBlock
        {
            Text = monogram,
            FontSize = fontSize,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static bool TryResolveSymbol(string? iconValue, out SymbolRegular symbol)
    {
        symbol = SymbolRegular.Apps24;
        return !string.IsNullOrWhiteSpace(iconValue)
               && Enum.TryParse(iconValue.Trim(), ignoreCase: true, out symbol);
    }

    private static bool TryResolveImagePath(
        string pluginId,
        string? iconValue,
        string? metadataFilePath,
        string? pluginsDirectory,
        out string? imagePath)
    {
        imagePath = null;

        foreach (var candidate in EnumerateIconCandidates(pluginId, iconValue, metadataFilePath, pluginsDirectory)
                     .Where(path => IsSupportedIconExtension(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (!File.Exists(candidate))
                    continue;

                imagePath = Path.GetFullPath(candidate);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // Ignore invalid icon paths and continue to safe fallbacks.
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateIconCandidates(
        string pluginId,
        string? iconValue,
        string? metadataFilePath,
        string? pluginsDirectory)
    {
        var searchDirectories = ResolvePluginDirectories(pluginId, metadataFilePath, pluginsDirectory).ToArray();

        if (!string.IsNullOrWhiteSpace(iconValue))
        {
            var trimmed = iconValue.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || uri.IsFile)
            {
                if (Path.IsPathRooted(trimmed))
                {
                    var fullPath = Path.GetFullPath(trimmed);
                    if (searchDirectories.Any(directory => PathSecurity.IsPathWithinAllowedDirectory(fullPath, directory, allowNonExistent: false)))
                        yield return fullPath;
                }
                else if (!trimmed.Contains("..", StringComparison.Ordinal))
                {
                    foreach (var directory in searchDirectories)
                    {
                        var resolvedPath = Path.GetFullPath(Path.Combine(directory, trimmed));
                        if (PathSecurity.IsPathWithinAllowedDirectory(resolvedPath, directory))
                            yield return resolvedPath;
                    }
                }
            }
        }

        foreach (var directory in searchDirectories)
        {
            foreach (var iconName in IconFileNames.Concat([pluginId]))
            {
                foreach (var extension in IconExtensions)
                    yield return Path.Combine(directory, iconName + extension);
            }
        }
    }

    private static IEnumerable<string> ResolvePluginDirectories(string pluginId, string? metadataFilePath, string? pluginsDirectory)
    {
        if (!string.IsNullOrWhiteSpace(metadataFilePath))
        {
            var metadataDirectory = Path.GetDirectoryName(metadataFilePath);
            if (!string.IsNullOrWhiteSpace(metadataDirectory))
                yield return Path.GetFullPath(metadataDirectory);
        }

        if (string.IsNullOrWhiteSpace(pluginsDirectory))
            yield break;

        var normalizedPluginId = pluginId.Replace("-", string.Empty);
        var candidates = new[]
        {
            pluginId,
            Path.Combine("local", pluginId),
            $"LenovoLegionToolkit.Plugins.{pluginId}",
            $"LenovoLegionToolkit.Plugins.{normalizedPluginId}"
        };

        foreach (var candidate in candidates)
            yield return Path.GetFullPath(Path.Combine(pluginsDirectory, candidate));
    }

    private static bool IsSupportedIconExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return IconExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizePluginId(string pluginId)
    {
        var trimmed = pluginId.Trim();
        return PathSecurity.IsValidFileName(trimmed) ? trimmed : "plugin";
    }

    private static bool IsRecoverableImageException(Exception exception)
    {
        return exception is ArgumentException
            or IOException
            or InvalidOperationException
            or NotSupportedException
            or PathTooLongException
            or UnauthorizedAccessException
            or UriFormatException;
    }

    private static string CreateMonogram(string? pluginName, string pluginId)
    {
        var source = string.IsNullOrWhiteSpace(pluginName) ? pluginId : pluginName;
        var separators = source.Where(ch => !char.IsLetterOrDigit(ch)).Distinct().ToArray();
        var tokens = source.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return tokens.Length == 0
            ? "P"
            : tokens.Length > 1 && (tokens[0].Length <= 2 || char.IsDigit(tokens[0][0]))
                ? string.Concat(tokens[0][0], tokens[1][0]).ToUpperInvariant()
                : new string(tokens[0].Take(2).ToArray()).ToUpperInvariant();
    }
}
