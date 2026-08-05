# Current tasks

Tracking notes for maintainers. Prefer GitHub Issues for public work.

## Documentation / ecosystem (2026-08-05)

- [x] Align README / zh-CN catalog with manifest versions (1.0.18 / 1.2.4 / 1.0.14)
- [x] Document host baseline v5.0.2 and minHostVersion sync
- [x] Prefer `udt-plugin.cmd` as canonical tooling entry
- [x] Add `Docs/README.md` index; mark sprint/promo docs as historical
- [x] Publish the managed `plugin-catalog` release with current ZIP and `store.json` hashes
- [x] Retire the sibling plugin repository after preserving its historical releases and tags
- [ ] Optional: rename historical promo copies or move under `Docs/archive/`

## Engineering backlog (open)

- When host ships a new minor, update `HostBaseline/host-release.json`; the build downloads the matching DLLs into ignored `.host/<version>/`.
- Watch KNOWLEDGE_BASE / BUGS for ABI and migration rules before “cleanup renames”
