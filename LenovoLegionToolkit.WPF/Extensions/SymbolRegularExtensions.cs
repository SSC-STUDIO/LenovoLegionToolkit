using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Extensions;

/// <summary>
/// WPF-UI 4 uses <see cref="IconElement"/> on buttons and chrome; wrap glyph enums for call sites.
/// </summary>
public static class SymbolRegularExtensions
{
    public static SymbolIcon ToSymbolIcon(this SymbolRegular symbol) => new() { Symbol = symbol };
}
