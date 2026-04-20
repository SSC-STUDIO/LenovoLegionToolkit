using PluginTooling.Core;

namespace PluginWorkbench;

internal sealed record PluginWorkbenchUiState
{
    public PluginWorkbenchThemeMode ThemeMode { get; init; } = PluginWorkbenchThemeMode.System;
    public PluginWorkbenchView LastView { get; init; } = PluginWorkbenchView.Feature;
    public bool IsLogExpanded { get; init; } = true;
}
