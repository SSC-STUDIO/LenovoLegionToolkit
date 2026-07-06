# Task Tracker / 任务追踪

## In Progress / 进行中

- [ ] Pillar C: Build FlaUI + WinRT OCR automated verification pipeline
      - Status: Test infrastructure builds (0 errors, 0 warnings). Need to run tests with admin privileges.
      - Next: Execute FlaUI tests, verify OCR text extraction accuracy.

- [ ] Pillar D: Execute open-source promotion campaign for 100+ GitHub stars
      - Status: README and promotion docs updated with taglines, UDT-vs-Vantage comparison, audience positioning.
      - Next: Publish to communities (GitHub, Reddit, V2EX, 52Poje, Bilibili).

## Completed / 已完成

- [x] Fix language selector and main window popup simultaneously (Show → ShowDialog in LocalizationHelper.cs)
- [x] Fix overly long Chinese translation strings bursting card space (shortened 25+ keys in Resource.zh-hans.resx)
- [x] Add MaxHeight=60 to CardHeaderControl._subtitleTextBlock to prevent card bloat
- [x] Build WPF project successfully (0 errors, 0 warnings)
- [x] Build Test project successfully (0 errors, 0 warnings)
- [x] Create KNOWLEDGE_BASE.md with lessons learned
- [x] Create TASK.md (this file)
- [x] Read AUTONOMOUS_MAINTENANCE_AND_EVOLUTION_WORKFLOW.md and begin execution
- [x] Update CHANGELOG.md with CardHeaderControl and Chinese translation fixes
- [x] Dual-Track Verification Track 1: Full dotnet test suite passes (2353 tests: 2326 passed, 27 skipped, 0 failed)

## Backlog / 待办

- [ ] Dual-Track Verification Track 2: Run FlaUI + WinRT OCR tests with admin privileges
- [ ] Cross-repository synchronization check with UniversalDeviceToolkit-Plugins
- [ ] OCR 5-Dimension verification on 78+ locale screenshots
- [ ] Create GitHub release tag for next version (4.2.2?)
- [ ] Review and shorten any remaining long Chinese strings (>50 chars) in Resource.zh-hans.resx
