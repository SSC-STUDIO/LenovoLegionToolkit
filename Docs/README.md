# Documentation index

Shipping UI is the **Electron** shell (`UniversalDeviceToolkit.Electron`) talking to a headless **.NET Host** (`UniversalDeviceToolkit.Host`) over JSON-RPC. Business logic stays in .NET; Electron renders Host data and plugin `contributes.webPage` entries.

Start here, then follow the topic docs. Do not treat WPF/Avalonia audit notes as current product guidance.

## Start here

| Doc | Notes |
| --- | --- |
| [../README.md](../README.md) / [../README_zh-hans.md](../README_zh-hans.md) | Product overview |
| [PROMOTION_EN.md](./PROMOTION_EN.md) / [PROMOTION_CN.md](./PROMOTION_CN.md) | Ready-to-post social copy |
| [COMMUNITY_OUTREACH.md](./COMMUNITY_OUTREACH.md) | Where to post without looking like spam |
| [SUBMISSIONS.md](./SUBMISSIONS.md) | Directory and awesome-list tracker |
| [../CONTRIBUTING.md](../CONTRIBUTING.md) | How to build and contribute |
| [../CHANGELOG.md](../CHANGELOG.md) | Project changelog |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Process model: Electron UI + Host |
| [DEPLOYMENT.md](./DEPLOYMENT.md) | Build, package, release |
| [Plugins/README.md](./Plugins/README.md) | Official plugin authoring |

## Product and runtime

| Doc | Notes |
| --- | --- |
| [LanguagePacks.md](./LanguagePacks.md) | Electron i18n + Host `.resx` + online catalog |
| [NamespaceMigration.md](./NamespaceMigration.md) | Completed LLT → UDT ABI cutover |
| [NetworkAcceleration.md](./NetworkAcceleration.md) | Built-in network acceleration |
| [DEVICE_PROVIDERS.md](./DEVICE_PROVIDERS.md) | Brand EC / hardware providers |
| [UI_PERFORMANCE.md](./UI_PERFORMANCE.md) | Renderer hot-path rules |
| [../UniversalDeviceToolkit.Electron/resources/README.md](../UniversalDeviceToolkit.Electron/resources/README.md) | Runtime extras vs `Assets/` vs `buildResources/` |
| [SECURITY.md](./SECURITY.md) | Vulnerability reporting |
| [TEST_DIAGNOSTICS.md](./TEST_DIAGNOSTICS.md) | Test map: Host / Plugins / Electron, CI ladder, testhost locks |
| [SUBMISSIONS.md](./SUBMISSIONS.md) | Directory and awesome-list tracker |
| [AUTOMATION.md](./AUTOMATION.md) | Community growth campaign (not the in-app automation engine) |
| [CLI.md](./CLI.md) | `udt-cli` contract: `--json`, `doctor`, exit codes |
| [skills/udt-hardware-cli/SKILL.md](./skills/udt-hardware-cli/SKILL.md) | Copyable Agent skill for local `udt-cli` |
| [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md) | Community guidelines |

## Plugins

| Doc | Notes |
| --- | --- |
| [Plugins/PLUGIN_QUICKSTART.md](./Plugins/PLUGIN_QUICKSTART.md) | doctor → init → web page → package |
| [Plugins/PLUGIN_DEVELOPMENT.md](./Plugins/PLUGIN_DEVELOPMENT.md) | Manifests, versioning, validation |
| [Plugins/HOST_INTEGRATION.md](./Plugins/HOST_INTEGRATION.md) | Lifecycle, `webPage`, Host RPC |
| [Plugins/ARCHITECTURE.md](./Plugins/ARCHITECTURE.md) | SDK layers and layout |
| [Plugins/CODING_STANDARDS.md](./Plugins/CODING_STANDARDS.md) | C# + plugin web UI kit |
| [Plugins/SDK_CHANGELOG.md](./Plugins/SDK_CHANGELOG.md) | SDK compatibility |
| [Plugins/BUILD_SMOKE.md](./Plugins/BUILD_SMOKE.md) | Plugin CI smoke |
| [Plugins/RELEASE_AND_MIGRATION.md](./Plugins/RELEASE_AND_MIGRATION.md) | Catalog release |

## Historical (not current SoT)

These files record past WPF/Avalonia work. They are not the shipping UI contract.

| Doc | Notes |
| --- | --- |
| [archive/AvaloniaMigrationMatrix.md](./archive/AvaloniaMigrationMatrix.md) | Retired Avalonia port notes |
| [archive/MAINTENANCE_AND_UPGRADE_PLAN.md](./archive/MAINTENANCE_AND_UPGRADE_PLAN.md) | 2026 WPF-era maintenance log |
| [archive/StabilityAudit-20260711.md](./archive/StabilityAudit-20260711.md) | Point-in-time audit including retired WPF smoke |
| [archive/LocalizationGuidelines.md](./archive/LocalizationGuidelines.md) / [archive/LocalizationAndUiModernization.md](./archive/LocalizationAndUiModernization.md) | Retired WPF l10n notes; current: LanguagePacks.md |
| [archive/VisualAudit-ScreenAdaptation-20260718.md](./archive/VisualAudit-ScreenAdaptation-20260718.md) / [archive/VisualDesignRecommendations-ScreenAdaptation.md](./archive/VisualDesignRecommendations-ScreenAdaptation.md) | Retired WPF visual audits |
| [archive/Plugins/VISUAL_AUDIT_20260718.md](./archive/Plugins/VISUAL_AUDIT_20260718.md) / [archive/Plugins/VISUAL_DESIGN_RECOMMENDATIONS.md](./archive/Plugins/VISUAL_DESIGN_RECOMMENDATIONS.md) | Retired WPF plugin visual notes |
| [archive/OnlineLanguageAndUpstreamAbsorptionPlan.md](./archive/OnlineLanguageAndUpstreamAbsorptionPlan.md) | Language-pack program notes (startup gate originally WPF) |
| [archive/PluginConsolidation.md](./archive/PluginConsolidation.md) | Plugin monorepo move |
| [archive/UpstreamCapabilityMatrix.md](./archive/UpstreamCapabilityMatrix.md) | Upstream feature matrix |

Agent entry points: root [AGENTS.md](../AGENTS.md), [UniversalDeviceToolkit.Electron/AGENTS.md](../UniversalDeviceToolkit.Electron/AGENTS.md), [UniversalDeviceToolkit.Host](../UniversalDeviceToolkit.Host) RPC handlers, [Plugins/AGENTS.md](../Plugins/AGENTS.md).
