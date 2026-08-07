# Avalonia Migration Matrix

## Purpose

WPF remains the functional and visual reference host. This matrix is the
implementation gate for the Avalonia migration: an entry is not considered
complete merely because its route exists or a page renders. It must use the
same shared service/state contract, preserve the supported user operation, and
provide an Avalonia-native surface for built-in plugin UI.

Visual regression, screenshots, or a WPF retirement decision are out of scope
until every item marked **In progress** is completed and its functional tests
are green.

## Current Baseline

The source-level migration manifest guard verifies the following against the
current WPF source:

| Area | WPF baseline | Avalonia implementation | Status |
| --- | --- | --- | --- |
| Main navigation | Dashboard, Keyboard, Actions, Macro, Windows optimization, Plugin Extensions, Settings, About | `MainNavigation` and `MainWindow` route all eight entries | Contracted |
| Settings navigation | Appearance, Application, Smart Keys, Display, Update, Power, Integrations | `SettingsPageViewModel` maps all seven entries without placeholder content | Contracted |
| Settings edits | Seven specialized WPF settings controls | Native appearance controls plus `SettingsCapabilityView` backed by `IAvaloniaSettingsService`; behavior-level persistence and rollback contracts cover all seven views | Complete |
| Dashboard layout | Sensors, power controls, GPU controls, layout editor and actions | `DashboardPage` and `DashboardPageViewModel` use the shared dashboard, sensor, GPU, power and layout contracts; strict platform-service contracts cover success, failure, refresh and rollback paths | Complete |
| Keyboard lighting | Keyboard backlight and Spectrum profiles | `KeyboardBacklightPage` uses the shared keyboard-lighting contract | In progress |
| Actions | Automation pipeline editor and manual actions | `AutomationPage` edits and saves the shared automation workspace | In progress |
| Macro | Macro editor and recorder state | `MacroPage` uses the shared macro workspace contract | In progress |
| Windows optimization | Optimization actions and custom cleanup rules | `WindowsOptimizationPage` and `FeaturePageView` preserve pending selection, batch application, per-action cleanup progress and partial-failure results through the shared feature-action contract | Complete |
| Official plugin UI | Custom Mouse, Shell Integration, ViVeTool | Each official plugin supplies an Avalonia page factory; `PluginHostedPage` embeds native controls, preserves plugin actions and failure states, and renders an explicit compatibility state for WPF-only plugins | Complete |
| Plugin lifecycle | Discovery, installation, updates and navigation refresh | Avalonia startup and `MainWindow` preserve install, update, cancellation, failure, retry, transactional rollback and navigation-refresh state through `IPluginManager` and the plugin catalog coordinator | Complete |
| Startup and desktop lifecycle | Single instance, crash recovery, compatibility gate, device setup, OSD, services and clean shutdown | Avalonia uses shared runtime coordinators and Windows service adapters | Contracted |
| Localization | WPF `Resource*.resx` catalogs | Avalonia `Resource*.resx` catalogs contain every WPF key across all 25 cultures and preserve fallback, RTL, runtime culture switching and per-plugin overrides | Complete |

`Contracted` means that a source and/or contract test proves the host is wired
to the required shared API. It does not mean visual equality. `In progress`
means the route and shared state are present, but the detailed control behavior,
dialogs, state transitions, and unsupported-hardware paths still require
feature-level comparison with WPF.

## Required Feature-Level Checks

Before changing a row to **Complete**, verify the relevant WPF implementation
and add or update an Avalonia contract/integration test for all of the following
applicable behaviors:

1. Load current shared state and display the same unavailable/error state.
2. Persist every editable operation through the same underlying service.
3. Roll back rejected toggle, selection, and text edits to the saved value.
4. Preserve add, edit, reorder, delete, import, export, and reset flows where
   WPF exposes them.
5. Provide a native dialog or in-page equivalent for WPF dialogs; a blank or
   generic placeholder is not an equivalent implementation.
6. Keep destructive or hardware operations explicitly disabled or confirmation
   gated in automated tests.

## Plugin Boundary

All bundled official plugins are required to expose an Avalonia-native page
factory. A third-party plugin compiled only for WPF cannot be embedded in an
Avalonia visual tree. The host must therefore show its compatibility state
without leaving navigation blank, while the plugin author migrates its UI using
the shared plugin SDK. A WPF-only compatibility message must never be counted
as a completed Avalonia plugin UI migration.

## Evidence and Next Order

The baseline is guarded by
`AvaloniaMigrationManifestTests` and
`OfficialPluginAvaloniaMigrationContractTests` in
`UniversalDeviceToolkit.Tests/Avalonia/`. The next implementation passes are:

1. Complete feature-level WPF/Avalonia comparison for Dashboard, Keyboard,
   Actions, Macro, Windows optimization, and all seven settings views.
2. Close any missing dialog and state-transition paths discovered by those
   comparisons.
3. Audit bundled plugin UI at runtime through the Avalonia page factories.
4. Only after those checks are complete, implement and execute the dual-host
   visual automation and video audit.
