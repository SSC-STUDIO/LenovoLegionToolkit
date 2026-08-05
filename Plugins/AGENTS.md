# Agent rules (auto-loaded)

## Repository facts

- Official plugins for **Universal Device Toolkit** (UDT)
- Default git branch: **`master`**
- Host baseline: **v5.0.2** (`Plugins/HostBaseline/host-release.json`)
- Canonical CLI: **`udt-plugin.cmd`** (`llt-plugin.cmd` = alias)
- Plugin version SoT: `Plugins/Official/<Name>/plugin.manifest.json`
- Root `store.json` is **generated release output**, not the normal authoring entry

## Windows shell

When running under Windows PowerShell 5.1:

- Do not chain with `&&`; use separate commands or `;`
- Prefer `udt-plugin.cmd` over raw multi-`dotnet run` tooling invocations

## Git workflow

- Prefer Conventional Commits: `fix|feat|docs|chore|test|refactor(...)`
- Default branch name is `master` (not `main`)
- Do not force-push unless the user explicitly asks

## Code quality

- Keep `TreatWarningsAsErrors` green
- Add/adjust tests with behavior changes
- Preserve intentional legacy migration path segments under `%LocalAppData%\LenovoLegionToolkit\` (read-only migration sources)
- Do not reintroduce user-visible “Lenovo Legion Toolkit” branding outside historical notes / migration comments

## Workspace cleanliness

- Do not leave temp `_*.py` / `*.tmp` files in the repo root
- Prefer editing docs under `Docs/` + root README rather than inventing parallel guides

## Docs map

See `Docs/README.md` for the current documentation set.
