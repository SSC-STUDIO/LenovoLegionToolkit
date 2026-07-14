// NOTE: Do not use LenovoLegionToolkit.Lib.Compatibility as a namespace —
// Compatibility is already a static class in LenovoLegionToolkit.Lib and a
// child namespace would shadow it (CS0234 on Compatibility.GetMachineInformationAsync).
namespace LenovoLegionToolkit.Lib.Branding;

/// <summary>
/// Non-breaking dual-surface brand and ABI constants for Phase 2 of the namespace migration.
/// </summary>
/// <remarks>
/// <para>
/// User-facing product names use Universal Device Toolkit (UDT) branding.
/// Plugin / reflection load paths continue to resolve against the retained
/// <c>LenovoLegionToolkit.Lib*</c> assembly simple names until a coordinated
/// TypeForwardedTo / dual-package cutover (Phase 2–3).
/// </para>
/// <para>
/// This type is a constants-only dual surface — it does not rename types or
/// assemblies. See <c>Docs/NamespaceMigration.md</c> for the phased plan,
/// dual IPC pipe names, and automation env-var <c>LLT_*</c> / <c>UDT_*</c> aliases.
/// </para>
/// <para>
/// Display-name values align with <see cref="LenovoLegionToolkit.Lib.Utils.AppIdentity"/>; assembly simple
/// names match the ABI-retained <c>AssemblyName</c> values in Lib / Lib.Plugins.
/// </para>
/// </remarks>
public static class BrandCompatibility
{
    /// <summary>User-facing product display name (WPF process / installer brand).</summary>
    public const string ProductDisplayName = "Universal Device Toolkit";

    /// <summary>Compact product identifier without spaces (AppData folder, compact brand).</summary>
    public const string ProductCompactName = "UniversalDeviceToolkit";

    /// <summary>Legacy product display name retained for migration messaging and docs.</summary>
    public const string LegacyProductDisplayName = "Lenovo Legion Toolkit";

    /// <summary>
    /// Legacy compact product identifier (legacy AppData folder / historical brand token).
    /// </summary>
    public const string LegacyProductCompactName = "LenovoLegionToolkit";

    /// <summary>
    /// ABI-retained core library assembly simple name (no extension).
    /// Use for reflection, plugin assembly resolution, and dependency checks —
    /// matches <c>UniversalDeviceToolkit.Lib</c> project <c>AssemblyName</c>.
    /// </summary>
    public const string LegacyAssemblyLib = "LenovoLegionToolkit.Lib";

    /// <summary>
    /// ABI-retained plugins host assembly simple name (no extension).
    /// Matches <c>UniversalDeviceToolkit.Lib.Plugins</c> project <c>AssemblyName</c>.
    /// </summary>
    public const string LegacyAssemblyLibPlugins = "LenovoLegionToolkit.Lib.Plugins";

    /// <summary>
    /// Future preferred core library assembly simple name (documentation / planning only).
    /// Not used for load paths until TypeForwardedTo / dual-package lands — see
    /// <c>Docs/NamespaceMigration.md</c> Phase 2–3.
    /// </summary>
    public const string PreferredAssemblyLib = "UniversalDeviceToolkit.Lib";

    /// <summary>
    /// Future preferred plugins host assembly simple name (documentation / planning only).
    /// Not used for load paths until TypeForwardedTo / dual-package lands.
    /// </summary>
    public const string PreferredAssemblyLibPlugins = "UniversalDeviceToolkit.Lib.Plugins";
}
