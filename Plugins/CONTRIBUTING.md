# Contributing

## Contribution Paths

This repository has two valid paths:

1. contributor path
2. official store path

Use the contributor path by default.

## Contributor Path

1. Run `doctor`
2. Scaffold with `new`
3. Build with `build`
4. Preview with `preview`
5. Validate with `validate --profile contributor`
6. Pack with `pack`

You do not need `store-entry.json` for this path.

## Official Store Path

Only use this path when the plugin is meant to ship from the official repository.

Additional requirements:

- `store-entry.json`
- plugin `CHANGELOG.md`
- root `CHANGELOG.md`
- successful `official-candidate` validation

## Pull Requests

PRs should include:

- plugin source
- test project
- `plugin.json`
- plugin `CHANGELOG.md`
- `store-entry.json` only for official candidates

## Do Not

- do not start new authoring by editing root `store.json`
- do not add source references back to the sibling main repo
- do not bypass `PluginWorkbench` when checking host-facing UI
