# Upstream Capability Matrix

Source reviewed: `LenovoLegionToolkit-Team/LenovoLegionToolkit` `master` on 2026-07-11.

| Capability | UDT status | Decision | Notes |
|---|---|---|---|
| Driver/software updates, warranty, boot logo | Present | Keep UDT implementation | Already integrated with UDT resources and safety handling. |
| Night charge, DPI scale, Flip-to-start, Instant Boot, monitor off | Present | Keep | No duplicate implementation. |
| OR composite automation trigger | Added | Adopt | Independent implementation using existing trigger interfaces and serialization discovery. |
| Hardware sensor automation conditions | Added | Adopted | Must reuse `SensorsGroupController`; no duplicate WMI/HWiNFO polling. |
| Battery percentage automation trigger | Added | Adopted | Above/below threshold, duration, cooldown, charge/discharge filter; no-data returns false. |
| Sensor dashboard grouping/customization | Added | Adopted | `VisibleSections` / `SectionOrder` operable via Settings → Hardware sensors card (when enabled). |
| Notification type customization | Added | Adopted | `NotificationTypePolicy` (enable/persist/severity); main-window position remains bottom-right; OSD stays separate. |
| Settings export/import | Added | Adopted | Versioned archive of all AppData `*.json`, pre-import rollback and path validation. |
| Language pack lifecycle / startup gate | Added | Adopted | Catalog fields + install/repair/update/uninstall; gate before MainWindow; see `Docs/LanguagePacks.md`. |
| Special-key discovery/action mapping | Partial | Gated / pending | Keep current capability detection; Lenovo-only discovery and LED actions stay hidden when firmware support is not reported. No fake UI this phase. |
| 24-zone and ambient lighting devices | Not confirmed | Gated / pending | Never expose without device capability evidence and verified protocol support. Matrix-only / stubs this phase. |
| Extension framework replacement | Rejected | Keep plugin architecture | UDT plugin host is broader and already deployed. |
| Background services or telemetry | Rejected | Do not adopt | Conflicts with lightweight/privacy philosophy. |
| Network acceleration (Watt-like) | Phase 1 foundation | Independent (non-GPL) | Built-in page + NetworkProxy worker stub; default OFF; see `Docs/NetworkAcceleration.md`. |
| Plugin consolidation (NA / Battery / Mouse / Shell / ViVe) | In progress | Merge / deprecate | Matrix in `Docs/PluginConsolidation.md`; store stubs in `Packaging/plugins/`. |
| Automation display notification step | Present | Keep | Wired with UI control. |
| Automation show/hide main window steps | Added | Adopted | MessagingCenter → MainWindow visibility. |
| Automation volume / Wi-Fi steps | Present | Keep | Speaker mute/unmute + Wi-Fi on/off already wired; volume level not in scope. |
