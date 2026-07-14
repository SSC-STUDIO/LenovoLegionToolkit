using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Dual-load naming for plugin assemblies after the UDT assembly cutover.
/// Primary prefix is <c>UniversalDeviceToolkit.Plugins.*</c>; legacy
/// <c>LenovoLegionToolkit.Plugins.*</c> remains accepted for one dual-load release.
/// </summary>
public static class PluginAssemblyNaming
{
    public const string PreferredPluginsPrefix = "UniversalDeviceToolkit.Plugins.";
    public const string LegacyPluginsPrefix = "LenovoLegionToolkit.Plugins.";

    public const string PreferredSdkAssemblySimpleName = "UniversalDeviceToolkit.Plugins.SDK";
    public const string PreferredSharedAssemblySimpleName = "UniversalDeviceToolkit.Plugins.Shared";
    public const string LegacySdkAssemblySimpleName = "LenovoLegionToolkit.Plugins.SDK";
    public const string LegacySharedAssemblySimpleName = "LenovoLegionToolkit.Plugins.Shared";

    public const string PreferredSdkDllFileName = PreferredSdkAssemblySimpleName + ".dll";
    public const string PreferredSharedDllFileName = PreferredSharedAssemblySimpleName + ".dll";
    public const string LegacySdkDllFileName = LegacySdkAssemblySimpleName + ".dll";
    public const string LegacySharedDllFileName = LegacySharedAssemblySimpleName + ".dll";

    public const string PreferredHostRoot = "UniversalDeviceToolkit";
    public const string LegacyHostRoot = "LenovoLegionToolkit";

    /// <summary>Plugin assembly simple-name prefixes in primary-then-legacy order.</summary>
    public static readonly string[] PluginPrefixes = [PreferredPluginsPrefix, LegacyPluginsPrefix];

    public static bool IsPluginPrefixedFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return fileName.StartsWith(PreferredPluginsPrefix, StringComparison.OrdinalIgnoreCase) ||
               fileName.StartsWith(LegacyPluginsPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPluginPrefixedAssemblySimpleName(string? assemblySimpleName)
    {
        if (string.IsNullOrWhiteSpace(assemblySimpleName))
            return false;

        return assemblySimpleName.StartsWith(PreferredPluginsPrefix, StringComparison.OrdinalIgnoreCase) ||
               assemblySimpleName.StartsWith(LegacyPluginsPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSdkOrSharedDllFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return fileName.Equals(PreferredSdkDllFileName, StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals(PreferredSharedDllFileName, StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals(LegacySdkDllFileName, StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals(LegacySharedDllFileName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSdkOrSharedAssemblySimpleName(string? assemblySimpleName)
    {
        if (string.IsNullOrWhiteSpace(assemblySimpleName))
            return false;

        return assemblySimpleName.Equals(PreferredSdkAssemblySimpleName, StringComparison.OrdinalIgnoreCase) ||
               assemblySimpleName.Equals(PreferredSharedAssemblySimpleName, StringComparison.OrdinalIgnoreCase) ||
               assemblySimpleName.Equals(LegacySdkAssemblySimpleName, StringComparison.OrdinalIgnoreCase) ||
               assemblySimpleName.Equals(LegacySharedAssemblySimpleName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSdkDirectoryName(string? directoryName)
    {
        if (string.IsNullOrWhiteSpace(directoryName))
            return false;

        return directoryName.Equals(PreferredSdkAssemblySimpleName, StringComparison.OrdinalIgnoreCase) ||
               directoryName.Equals(LegacySdkAssemblySimpleName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Host contract assemblies (Lib, WPF, etc.) that should resolve from the default ALC.
    /// Excludes plugin SDK/Shared packages (loaded from plugin or app-base dual names).
    /// </summary>
    public static bool ShouldShareHostContractAssembly(string? assemblySimpleName)
    {
        if (string.IsNullOrWhiteSpace(assemblySimpleName))
            return false;

        if (IsSdkOrSharedAssemblySimpleName(assemblySimpleName))
            return false;

        return assemblySimpleName.StartsWith(PreferredHostRoot, StringComparison.OrdinalIgnoreCase) ||
               assemblySimpleName.StartsWith(LegacyHostRoot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Non-plugin host assemblies that must not load into a collectible plugin ALC
    /// (return null from Load so the default context is used).
    /// </summary>
    public static bool IsNonPluginHostAssembly(string? assemblySimpleName)
    {
        if (string.IsNullOrWhiteSpace(assemblySimpleName))
            return false;

        var isHostRoot =
            assemblySimpleName.StartsWith(PreferredHostRoot, StringComparison.Ordinal) ||
            assemblySimpleName.StartsWith(LegacyHostRoot, StringComparison.Ordinal);

        return isHostRoot && !assemblySimpleName.Contains("Plugins", StringComparison.Ordinal);
    }

    /// <summary>
    /// Strips a known plugin assembly prefix (UDT or LLT). Returns null if none matched.
    /// </summary>
    public static string? TrimPluginPrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        foreach (var prefix in PluginPrefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return value[prefix.Length..];
        }

        return null;
    }

    /// <summary>
    /// Removes either plugin prefix for token normalization (always returns a string).
    /// </summary>
    public static string StripPluginPrefixForNormalization(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        foreach (var prefix in PluginPrefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return value.Replace(prefix, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }

    public static string? ExtractPluginIdFromAssemblyFileName(string dllNameWithoutExtension)
    {
        var trimmed = TrimPluginPrefix(dllNameWithoutExtension);
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static IEnumerable<string> EnumeratePrefixedPluginNames(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            yield break;

        yield return PreferredPluginsPrefix + pluginId;
        yield return LegacyPluginsPrefix + pluginId;
    }

    public static IEnumerable<string> EnumeratePrefixedPluginDllFileNames(string pluginId)
    {
        foreach (var name in EnumeratePrefixedPluginNames(pluginId))
            yield return name + ".dll";
    }

    public static IEnumerable<string> EnumerateSdkDllFileNames()
    {
        yield return PreferredSdkDllFileName;
        yield return LegacySdkDllFileName;
    }

    public static IEnumerable<string> EnumerateSharedDllFileNames()
    {
        yield return PreferredSharedDllFileName;
        yield return LegacySharedDllFileName;
    }

    /// <summary>
    /// App-base paths for Shared, preferred UDT name first then legacy.
    /// </summary>
    public static IEnumerable<string> EnumerateAppBaseSharedCandidates()
    {
        foreach (var fileName in EnumerateSharedDllFileNames())
        {
            yield return Path.Combine(AppContext.BaseDirectory, fileName);
            yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }
    }

    /// <summary>
    /// App-base paths for SDK, preferred UDT name first then legacy.
    /// </summary>
    public static IEnumerable<string> EnumerateAppBaseSdkCandidates()
    {
        foreach (var fileName in EnumerateSdkDllFileNames())
        {
            yield return Path.Combine(AppContext.BaseDirectory, fileName);
            yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }
    }

    public static bool HasPluginAssemblyPrefix(string fileName) =>
        IsPluginPrefixedFileName(fileName) && !IsSdkOrSharedDllFileName(fileName);
}
