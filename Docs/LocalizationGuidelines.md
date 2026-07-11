# Localization Guidelines

This document explains how new UI strings should be added to the
Universal Device Toolkit resource system. It is the canonical reference
for both contributors and reviewers and is enforced by
[`ResourceHardcodingGuardTests`](../UniversalDeviceToolkit.Tests/WPF/ResourceHardcodingGuardTests.cs).

## Resource files

Strings live in
[`UniversalDeviceToolkit.WPF/Resources/Resource.resx`](../UniversalDeviceToolkit.WPF/Resources/Resource.resx)
(English source-of-truth). The Designer partial
`Resource.Designer.cs` is regenerated automatically by Visual Studio /
`dotnet` and exposes each entry as a `static string` property on
`UniversalDeviceToolkit.WPF.Resources.Resource`.

Simplified Chinese translations live in
[`Resource.zh-hans.resx`](../UniversalDeviceToolkit.WPF/Resources/Resource.zh-hans.resx)
(used by zh-Hans and zh-CN). Other locales are pulled from the
Crowdin pipeline; **never** edit them by hand — they will be
overwritten by the next sync.

## Adding a new string

1. Decide whether the string is localizable. Ask yourself:
   - **Brand names** (AsyncLock, Autofac, Markdig, WPF-UI…)? Add to the
     whitelist, do not translate.
   - **Technical abbreviations, units, enum values, single-character
     glyphs**? Add to the whitelist, do not translate.
   - **Placeholder overwritten by code-behind before the UI is shown**?
     Add to the whitelist, do not translate.
   - **Anything else?** Translate.

2. If translating, decide on a key name using **PascalCase with the
   form** `Feature_Subject_Classifier` (e.g.
   `DashboardITSModeControl_Title`,
   `TimeAutomationPipelineTriggerTabItemContent_HHMMHint`).

3. Add the key/value to `Resource.resx` (English first) **and** to
   `Resource.zh-hans.resx` (Simplified Chinese). Use one `<data>`
   element per key. Make sure the key is unique and stable — never
   rename an existing key, even when its meaning drifts; instead add a
   new key and remove the old one after a release cycle.

   ```xml
   <data name="DashboardITSModeControl_Title" xml:space="preserve">
     <value>ITS Mode</value>
   </data>
   ```

   ```xml
   <data name="DashboardITSModeControl_Title" xml:space="preserve">
     <value>ITS 模式</value>
   </data>
   ```

4. Reference the key from XAML with `{x:Static resources:Resource.<Key>}`
   (the `xmlns:resources` declaration that already lives on the root
   element of every XAML page maps to
   `clr-namespace:UniversalDeviceToolkit.WPF.Resources`):

   ```xaml
   <TextBlock Text="{x:Static resources:Resource.DashboardITSModeControl_Title}" />
   ```

   When the binding target needs an interpolated value (e.g. for a
   `ToolTip` set in code-behind) use `LocalizationHelper.GetStringOrEnglish`
   with the explicit English fallback:

   ```csharp
   var fallback = "ITS runtime is unavailable on this system.";
   var localized = LocalizationHelper.GetStringOrEnglish(
       Resource.ResourceManager,
       "DashboardITSModeControl_RuntimeUnavailable",
       fallback,
       Resource.Culture);
   ```

5. Run the tests before submitting:

   ```pwsh
   dotnet test UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~ResourceHardcodingGuardTests"
   ```

## Keeping the whitelist in sync

The whitelist at
[`UniversalDeviceToolkit.WPF/L10n/EnglishHardcodingWhitelist.txt`](../UniversalDeviceToolkit.WPF/L10n/EnglishHardcodingWhitelist.txt)
records every literal English string that the guard is **allowed** to
ignore. Each line is `<TAG> :: <repo-relative path> :: <literal>` where
`<TAG>` is one of:

| Tag            | Meaning                                                            |
|----------------|--------------------------------------------------------------------|
| `[BRAND]`      | Third-party product, library, or vendor name.                      |
| `[TECH_ABBREV]`| Technical abbreviation, measurement unit, or WPF enum literal.     |
| `[URL]`        | Full URL string (reserved for future use).                         |
| `[LICENSE]`    | License or contract identifier (reserved for future use).          |
| `[PLACEHOLDER]`| XAML default that is overwritten by code-behind before the UI is shown. |
| `[GLYPH]`      | Single Unicode glyph used as a visual mark (bullet, pipe, dash).   |

Entries that do not fit any of the existing tags MUST be reviewed by
the team before the whitelist is committed.

When you intentionally add a new hard-coded literal (for example, a
new enum value or a new third-party brand) also add a corresponding
line to the whitelist. The
[`ResourceHardcodingGuardTests.AllHardcodedAttributes_MustBeWhitelistedOrResourceBacked`](../UniversalDeviceToolkit.Tests/WPF/ResourceHardcodingGuardTests.cs)
test will fail if your XAML change is not paired with a whitelist entry
or a resource key.

## Code-behind strings

Strings concatenated in `.cs` files for dialogs, snackbars or
notification messages must also flow through resources. The codebase
already uses `Resource.<Key>` for the common case (because the
strongly-typed `Resource.Designer.cs` makes it cheap). For one-off
strings use:

```csharp
var localized = LocalizationHelper.GetStringOrEnglish(
    Resource.ResourceManager,
    "DashboardITSModeControl_RuntimeUnavailable",
    "ITS runtime is unavailable on this system.",
    Resource.Culture);
```

`Log.Instance.Trace(...)` and other diagnostic strings are out of scope
of the guard (they are not displayed to users through the UI).

## Reviewer checklist

When reviewing a PR that touches XAML or `.cs` UI code:

- [ ] No new literal English strings in XAML.
- [ ] Any whitelist additions carry a one-line justification in the PR
      description.
- [ ] Both English and Simplified-Chinese translations exist for new
      keys, even when the translation is the same as the English value.
- [ ] `ResourceHardcodingGuardTests` pass locally.
