using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Utils;

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

    public static Control CreateElement(PluginIconDescriptor descriptor, double symbolFontSize = 30, double monogramFontSize = 24)
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
        return PluginPaths.GetPluginsDirectory();
    }

    private static Control CreateImage(string imagePath)
    {
        var bitmap = new Bitmap(imagePath);

        return new Image
        {
            Source = bitmap,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static Control CreateSymbolElement(SymbolRegular symbol, double fontSize)
    {
        // AVALONIA: removed SetResourceReference("SystemAccentColorBrush") — the Avalonia
        // app does not define that resource; the icon uses the default foreground instead.
        return new SymbolIcon
        {
            Symbol = symbol,
            FontSize = fontSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static Control CreateMonogramElement(string monogram, double fontSize)
    {
        return new TextBlock
        {
            Text = monogram,
            FontSize = fontSize,
            FontWeight = FontWeight.Medium,
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
            $"UniversalDeviceToolkit.Plugins.{pluginId}",
            $"LenovoLegionToolkit.Plugins.{pluginId}",
            $"UniversalDeviceToolkit.Plugins.{normalizedPluginId}",
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
