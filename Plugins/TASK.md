# Current tasks

Tracking notes for maintainers. Prefer GitHub Issues for public work.

## Documentation / ecosystem (2026-07-18)

- [x] Align README / zh-CN catalog with manifest versions (1.0.17 / 1.2.3 / 1.0.13)
- [x] Document host baseline v5.0.0 and minHostVersion sync
- [x] Prefer `udt-plugin.cmd` as canonical tooling entry
- [x] Add `Docs/README.md` index; mark sprint/promo docs as historical
- [ ] Regenerate release ZIP assets + `store.json` hashes on next plugin release (Windows CI)
- [ ] Optional: rename historical promo copies or move under `Docs/archive/`

## Engineering backlog (open)

- When host ships a new minor, update `HostBaseline/host-release.json`; the build downloads the matching DLLs into ignored `.host/<version>/`.
- Watch KNOWLEDGE_BASE / BUGS for ABI and migration rules before “cleanup renames”
