# UDT Online 语言链路与上游能力吸收计划

**Status:** Active implementation plan  
**Last updated:** 2026-07-12  
**Related:** `Docs/LanguagePacks.md`, `Docs/UpstreamCapabilityMatrix.md`, `Docs/LocalizationGuidelines.md`

---

## 1. Summary

| Track | Goal |
|---|---|
| **Online language lifecycle** | Catalog → download → SHA-256 → validate satellites → atomic install → settings manage → repair/update/uninstall |
| **Startup window gate** | Language window only until language resolved; then exactly one `MainWindow` |
| **Upstream absorption** | Selectively adopt LLT automation / notifications / special keys / lighting; keep UDT plugin, localization, charts, network, safe-start |
| **Brand** | Keep confirmed symmetric Trace icons; audit installer, tray, README, OG, store references only |

Upstream v2.34+ expanded battery/sensor automation, notification customization, special-key discovery, 24-zone Spectrum and ambient lighting. UDT implements independently behind capability gates — never by copying GPL-sensitive modules wholesale.

---

## 2. Implementation status (gap matrix)

### 2.1 Language packs & startup (mostly **Done**)

| Item | Status | Evidence |
|---|---|---|
| Versioned catalog fields (`culture`, `parent`, `size`, `sha256`, `resourceVersion`, `minAppVersion`, `url`, `displayName`) | **Done** | `LanguagePackCatalogEntry`, `OnlineResourceCatalogClient`, `Docs/LanguagePacks.md` |
| `LanguagePackManager` install / repair / update / uninstall / pending uninstall | **Done** | `Utils/LanguagePackManager.cs` |
| Temp download + hash + satellite validation + atomic replace | **Done** | `InstallAsync` staging flow |
| Full portable ZIP culture fallback | **Done** | Manager fallback extract path |
| Exact culture → parent → English (never Chinese for non-ZH UI) | **Done** | `LocalizationHelper` + guidelines |
| Language gate **before** IoC / plugins / hardware / MainWindow | **Done** | `StartupOrchestrator.RunLanguageGateAsync` |
| Gate outcomes: Continue / ContinueEnglish / Exit | **Done** | `LanguageGateOutcome` |
| Retry / Continue English / Exit on download failure | **Done** | `LanguageSelectorWindow` |
| Settings-page install uses owner / in-page progress | **Done** | `LanguagePackInstallCoordinator` |
| Online prune non-English satellites in CI/packaging | **Done** | `Prune-ShippingFootprint.ps1`, `UdtSatelliteResourceLanguages` |
| Mock catalog + UI smoke | **Partial** | `Tools/LanguagePackMockBackend`, `LanguagePackUi.Smoke` |
| CI: build Online/Full + local HTTP catalog install | **Partial** | Exists packaging; expand pipeline job if missing |
| Dual MainWindow during gate | **Mitigated** | Gate before create; `RestartMainWindow` only post-language for culture switch |

### 2.2 Upstream software capabilities (mostly **Done**)

| Item | Status | Evidence |
|---|---|---|
| OR composite trigger | **Done** | `OrAutomationPipelineTrigger` |
| Hardware sensor trigger (reuse SensorsGroupController) | **Done** | `HardwareSensorAutomationPipelineTrigger` |
| Battery % trigger (above/below, duration, cooldown, charge filter) | **Done** | `BatteryPercentageAutomationPipelineTrigger` + tab UI |
| Show / hide / close main window automation steps | **Done** | MessagingCenter visibility |
| Notification / volume / Wi‑Fi steps | **Done** / keep | Existing step registration |
| Sensor section order / hide UI | **Done** | Settings hardware sensors card |
| Notification type policy (enable/persist/severity) | **Done** | Settings store `TypePolicies` |
| Main notifications bottom-right stack | **Done** | MainWindow banners |
| OSD independent of main toast host | **Done** | Separate OSD windows |
| Settings export/import with backup + rollback | **Done** | Settings application behavior control |

### 2.3 Special keys & lighting (**Gated / next**)

| Item | Status | Decision |
|---|---|---|
| Discoverable special-key model (id, protocol, single/double click, LED) | **Partial / next** | Only when firmware reports capability |
| Lenovo-only keys hidden on non-capable devices | **Partial** | Keep capability filters; no empty UI |
| LED failure must not block key action | **Pending verification** | Add isolation + diagnostics |
| 24-zone Spectrum UI | **Gated** | Capability-matched regions only |
| Front/rear ambient lights | **Gated** | Verified protocol only |
| Background services / telemetry / dangerous auto-replay | **Rejected** | Do not adopt |

### 2.4 Brand assets (**Checklist**)

| Asset | Action |
|---|---|
| App / installer / tray / taskbar icons | Verify same Trace asset |
| README / OG / release pages | Same asset path |
| Store / winget / scoop metadata | Same asset path |
| Do **not** redesign graphics this round | — |

---

## 3. Language protocol (normative)

### 3.1 Catalog layout

```text
/{version}/catalog.json
/{version}/languages/{culture}.zip
/{version}/… full portable zip (fallback extraction only)
```

Catalog language entry **must** include: culture, optional parent, size, sha256, resourceVersion, minAppVersion, url, displayName.

### 3.2 Lifecycle

1. Query catalog (optional on normal launches if packs current).  
2. Download to user temp directory.  
3. Verify SHA-256.  
4. Extract & validate resource assemblies match culture.  
5. Stage → atomic replace culture directories under app base.  
6. Record installed resourceVersion + hash for upgrade drift repair.  
7. On failure: leave previous culture intact; surface structured error.  
8. Uninstall: remove dirs or queue pending uninstall if culture is active.

### 3.3 Startup window lifecycle

```text
Bootstrap logs → Single instance → Language gate (modal LanguageSelector only)
  → IoC + plugins/hardware prep
  → Create MainWindow once
  → Show MainWindow
  → Compatibility / plugins / background services
```

Rules:

- No MainWindow during language gate.  
- Download failure: **Retry | Continue in English | Exit** — never silent English.  
- Cancel: abort network, clean temp, `LanguageGateOutcome.Exit`.  
- Safe-start / offline: English MainWindow allowed without pack.  
- Settings install: progress owned by MainWindow (coordinator / modal with Owner).

---

## 4. Upstream absorption rules

1. **Compare behavior first** for same-name features; keep UDT safe-start, plugins, localization, charts, network acceleration.  
2. **No new WMI/HWiNFO pollers** for sensors already covered by `SensorsController` / `SensorsGroupController`.  
3. **Capability-gate** all Lenovo-specific keys and lighting; hide when unsupported.  
4. **CI matrix only reports** new upstream triggers/steps/keys/lights — never auto-copy code.  
5. Record every decision in `Docs/UpstreamCapabilityMatrix.md` with version, risk, tests.

---

## 5. Interfaces (already / target)

| Type | Role |
|---|---|
| `LanguagePackManager` | Catalog query, install, repair, update, uninstall, progress, cancel |
| `LanguagePackCatalogEntry` | Catalog DTO |
| `LanguagePackException` + `LanguagePackFailureKind` | Structured failures |
| `LanguageGateOutcome` | Continue / ContinueEnglish / Exit |
| `BatteryPercentageAutomationPipelineTrigger` | Battery automation |
| `NotificationTypePolicy` | Per-type notification policy in settings |
| Lighting region capability DTO | *Next* — no raw model strings in UI conditions |

---

## 6. Phased PR plan

### Phase A — Language hardening (P0)

- [x] Gate before MainWindow  
- [x] Manager lifecycle APIs  
- [x] CI job: Online build + mock HTTP catalog install smoke (`Scripts/Run-LanguageOnlineCi.ps1`, `.github/workflows/Language-Online-Ci.yml`)  
- [x] Audit no second MainWindow path before gate (`PhaseALanguageAndBrandTests` source-order smoke)  
- [x] Document offline/proxy privacy in README EN/ZH  

### Phase B — Software automation/settings polish (P0/P1)

- [x] OR + battery % + hardware sensor triggers  
- [x] Notification policies + settings import/export  
- [x] Expand trigger/step registration tests (duration, cooldown, no-data, safe-start) — `PhaseBAutomationSafeStartTests`  
- [ ] Sensor threshold UI shared with automation editor if still partial  

### Phase C — Special keys & lighting (P1, gated)

- [x] Special-key discovery model + capability filter (`SpecialKeyDiscovery`)  
- [x] LED failure isolation + diagnostics (`SpecialKeyLedIsolation`)  
- [x] 24-zone / ambient only with verified region capability (`LightingCapabilityGate`)  
- [x] Matrix updates with device evidence  

### Phase D — Brand & packaging consistency (P1)

- [x] Icon path audit (installer SetupIconFile, tray AssetResources.icon, README Logo, OG preview)  
- [x] Unified brand binaries under repo-root `Assets/` (WPF links; no second copy under WPF/Assets)  
- [x] Full/Online language pack offline behavior documented in README + LanguagePacks  


---

## 7. Test & acceptance (checklist)

### Language

- Catalog: success, 404, timeout, proxy, bad hash, corrupt zip, culture mismatch, minAppVersion, portable fallback, cancel  
- Install atomicity, upgrade, repair, uninstall, parent/English fallback, failed install keeps old pack  
- UI: only language window during gate; one MainWindow after  
- Settings install: owned progress UI, no orphan top-level download window  
- Online package footprint: no unexpected non-English satellites  

### Upstream software

- OR / battery % / hardware sensor: serialize, duration, cooldown, no-data → false, safe-start no hardware fire  
- Steps: show/hide window, notification, mute/Wi‑Fi registration  
- Sensors: order/hide/normalize; no extra sampler  
- Notification policies + stack + dismiss  
- Settings import: version reject, corrupt reject, mid-fail rollback  

### Special keys & lighting

- Device filter, action discovery, double-click, LED partial fail  
- Unsupported devices hide UI completely  

### Final

- Release x64, full tests, Online/Full packages, plugins green  
- Offline first run English; online language install survives restart  
- Normal launch does not hit catalog unless pack update check needed  
- No new always-on process / dangerous hardware replay for unreleased features  
- One brand icon set across package and docs  

---

## 8. Default assumptions

1. Online language packs use full lifecycle (not one-shot download).  
2. First language download shows only the language window.  
3. Failure allows retry or English continue.  
4. Software automation/settings land first; keys/lighting only when capability-proven.  
5. No brand redesign this round.  
6. Preserve uncommitted user work and `ai/` unless explicitly staged.  

---

## 9. Working references

- Code: `StartupOrchestrator`, `LocalizationHelper`, `LanguageSelectorWindow`, `LanguagePackManager`, `LanguagePackInstallCoordinator`  
- Automation: `UniversalDeviceToolkit.Lib.Automation/Pipeline/Triggers/*`  
- Packaging: `Scripts/Prune-ShippingFootprint.ps1`, release workflows under `.github` / `Packaging`  
- Smoke: `Tools/LanguagePackUi.Smoke`, `Tools/LanguagePackMockBackend`  
- Upstream monitor: release notes LLT ≥ 2.34.0 (manual review only)  
