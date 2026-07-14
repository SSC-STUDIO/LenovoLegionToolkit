# Upstream Capability Matrix

Source reviewed: `UniversalDeviceToolkit-Team/UniversalDeviceToolkit` (including releases through ~v2.34.x) on 2026-07-12.

**Master plan:** `Docs/OnlineLanguageAndUpstreamAbsorptionPlan.md`  
**Language protocol:** `Docs/LanguagePacks.md`

Legend: **Keep** = UDT already has equivalent · **Adopted** = independently implemented · **Gated** = only with device capability · **Rejected** = will not take · **Pending** = planned next phase.

| Capability | UDT status | Decision | Notes / evidence |
|---|---|---|---|
| Driver/software updates, warranty, boot logo | Present | Keep | UDT resources + safety handling |
| Night charge, DPI scale, Flip-to-start, Instant Boot, monitor off | Present | Keep | No duplicate |
| OR composite automation trigger | Present | Adopted | `OrAutomationPipelineTrigger` |
| Hardware sensor automation conditions | Present | Adopted | Reuse `SensorsGroupController`; no extra WMI/HWiNFO timer |
| Battery percentage automation trigger | Present | Adopted | Threshold, duration, cooldown, charge/discharge filter; no-data → false |
| Sensor dashboard grouping / order / hide | Present | Adopted | Settings hardware sensors card when enabled |
| Notification type customization | Present | Adopted | `NotificationTypePolicy`; main host bottom-right stack; OSD separate |
| Settings export/import | Present | Adopted | Versioned AppData JSON archive, backup + rollback |
| Language pack lifecycle / startup gate | Present | Adopted | Catalog + install/repair/update/uninstall; gate before MainWindow |
| Automation display notification step | Present | Keep | UI control wired |
| Automation show/hide/close main window | Present | Adopted | MessagingCenter → MainWindow |
| Automation volume mute / Wi‑Fi on-off | Present | Keep | Level-set volume not in scope |
| Special-key discovery / action mapping | Present | **Gated / adopted** | `SpecialKeyDiscovery` catalog + `FilterForDevice`; empty on non-Legion |
| Special-key LED sync isolation | Present | **Gated / adopted** | `SpecialKeyLedIsolation` wraps spectrum/white LED feedback |
| 24-zone Spectrum keyboard | Not confirmed | **Gated** | `LightingCapabilityGate` / `Keyboard24ZoneLightingCapability` always false until evidence |
| Front/rear ambient lighting | Not confirmed | **Gated** | Listed unsupported in `LightingCapabilityGate` |
| Extension framework replacement | N/A | **Rejected** | Keep UDT plugin host |
| Background services / telemetry | N/A | **Rejected** | Privacy / lightweight |
| Dangerous hardware auto-replay | N/A | **Rejected** | Safe-start first |
| Network acceleration (Watt-like) | Phase foundation | Independent | Default OFF; see `Docs/NetworkAcceleration.md` |
| Plugin consolidation | In progress | Merge / deprecate | `Docs/PluginConsolidation.md` |

## Review process

1. On each upstream release of interest, add rows or update **Notes** only.  
2. Prefer behavioral parity tests over code copy.  
3. CI may **report** newly discovered upstream trigger/step/key names; it must **not** enable features automatically.  
4. Any **Gated** item needs a device capability probe test before UI registration.

## Next review focus

- Device-lab confirmation for any future 24-zone / ambient protocol enablement  
- Sensor threshold UI shared with automation editor (Phase B remainder)  
- Optional UI FlaUI language-window exclusive smoke on self-hosted runner  

