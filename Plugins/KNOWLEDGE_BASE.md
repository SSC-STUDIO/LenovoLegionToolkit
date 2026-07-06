# Knowledge Base — Lessons Learned & Rules (经验知识与规则累积)

This file is the **Living Knowledge Ledger** for the Universal Device Toolkit Plugins project. Every time an AI agent or human developer solves a complex bug, discovers a Windows OS quirk, or optimizes an architecture, they MUST append a structured entry here.

---

## 📚 Format Template

```markdown
### [YYYY-MM-DD] <Brief Description>
- **Symptom / Pitfall**: What failed?
- **Root Cause**: Why did it fail?
- **Enforced Rule**: What mandatory constraint prevents recursion?
- **.NET/OS Version**: Under what environment was this learned?
```

---

### [2026-07-06] Zero-Warnings Build Achievement
- **Symptom / Pitfall**: Multiple Roslyn analyzer warnings (CA1062, CA2024, CS1591) accumulating in build output
- **Root Cause**: 
  - CA1062: Missing null validation in public methods
  - CA2024: `EndOfStream` used in async context (should use `ReadLineAsync`)
  - CS1591: Missing XML documentation for public APIs
- **Enforced Rule**: 
  - Added `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to `Directory.Build.props`
  - Added `<WarningsAsErrors>CS1591;CA1062;CA2024</WarningsAsErrors>`
  - All public methods MUST have `ArgumentNullException.ThrowIfNull()` 
  - All async methods MUST NOT use `EndOfStream`
  - All public APIs MUST have XML documentation (`/// <summary>`)
- **.NET/OS Version**: .NET 10, Windows 11 24H2

---

### [2026-07-06] Version Mismatch Between plugin.json and plugin.manifest.json
- **Symptom / Pitfall**: CI validation failure: `plugin.json version does not match plugin.manifest.json version`
- **Root Cause**: `plugin.json` had version `1.1.9` while `plugin.manifest.json` had `1.2.0`. Manual version updates cause drift.
- **Enforced Rule**: 
  - `plugin.manifest.json` is the SINGLE SOURCE OF TRUTH for version
  - `plugin.json` MUST be auto-generated from `plugin.manifest.json` (or strictly synced)
  - `.csproj` `<Version>` MUST match `plugin.manifest.json` version
  - Use `.\llt-plugin.cmd promote` to auto-sync versions
- **.NET/OS Version**: .NET 10, Windows 11 24H2

---

### [2026-07-06] FeatureMerger.cs Missing using System
- **Symptom / Pitfall**: Compilation error: `CS0103: The name 'ArgumentNullException' does not exist in the current context`
- **Root Cause**: Added `ArgumentNullException.ThrowIfNull()` calls but forgot to add `using System;` to the file
- **Enforced Rule**: 
  - ALL `.cs` files MUST have explicit `using System;` if they use `ArgumentNullException`, `NullReferenceException`, etc.
  - When adding null validation to a file, ALWAYS check that `using System;` is present
  - Prefer full-qualified names (`System.ArgumentNullException.ThrowIfNull()`) if modifying legacy code without `using System;`
- **.NET/OS Version**: .NET 10, C# 13 (preview)

---

### [2026-07-05] WPF Fallback UI Hardcoded Brushes
- **Symptom / Pitfall**: Fallback UI (when WPF fails to load XAML) shows white background in Dark theme
- **Root Cause**: `WpfFallbackHelper.cs` used hardcoded `Brushes.White`, `Brushes.Black`, `Brushes.Gray`
- **Enforced Rule**: 
  - ALL fallback UI MUST use `DynamicResource` or theme-aware brush resolution
  - Created `ResolveFallbackBrush(string lightBrush, string darkBrush)` method
  - Never use `Brushes.*` directly in fallback UI code
  - Test fallback UI in BOTH Light and Dark themes
- **.NET/OS Version**: .NET 10, Windows 11 24H2, WPF UI 4.3.0

---

### [2026-07-05] ProcessRunner.PumpAsync CA2024 Warning
- **Symptom / Pitfall**: `CA2024: Use 'ReadLineAsync' instead of 'EndOfStream' in async method`
- **Root Cause**: `StreamReader.EndOfStream` is a synchronous property that blocks in async context
- **Enforced Rule**: 
  - In async methods, NEVER use `EndOfStream`
  - Use `ReadLineAsync()` in a loop until it returns `null`
  - Example fix:
    ```csharp
    string? line;
    while ((line = await streamReader.ReadLineAsync()) != null)
    {
        // Process line
    }
    ```
- **.NET/OS Version**: .NET 10, C# 13 (preview)

---

## 🔧 General Engineering Rules (工程规则)

### WPF Thread Safety
- NEVER use `ConfigureAwait(false)` in WPF projects — it strips the UI synchronization context
- Always use `Dispatcher.CheckAccess()` and `Dispatcher.InvokeAsync()` for background-triggered UI updates

### WMI & Remote Desktop Deadlock Protection
- ALL WMI queries MUST use async wrappers with 2,500ms–3,000ms timeouts
- ALL administrative process executions (`netsh`, `sc`) MUST use `CancellationToken`
- Test WMI queries under Remote Desktop sessions (they behave differently!)

### Zero-Spam Polling & I/O Efficiency
- High-frequency monitoring loops (500ms–2000ms) MUST NOT serialize JSON on every tick
- Use in-memory caching and only write to disk every 5–10 seconds
- NEVER use `Debug.WriteLine()` in production polling loops (it kills performance)

### Modular UI & Design Token Binding
- ALL UI elements MUST use `CornerRadius="8"` or `CornerRadius="10"` (rounded cards)
- ALL colors MUST use `DynamicResource` (e.g., `{DynamicResource ControlFillColorDefaultBrush}`)
- NEVER write monolithic 600-line StackPanels — use `Grid` star-sizing and `WrapPanel`
- ALL text MUST use `TextWrapping="Wrap"` and `TextTrimming="CharacterEllipsis"`

---

## 🌍 Localization Rules (本地化规则)

### Resource File Conventions
- ALL user-facing strings MUST be in `Resource.resx` (never hardcode English text)
- Use numbered placeholders (`{0}`, `{1}`) — NEVER string concatenation
- Generate satellite assemblies for all 34 languages in `<PluginSatelliteResourceLanguages>`

### OCR Verification (5-Dimension Check)
When evaluating UI via FlaUI + WinRT OCR:
1. **Untranslated Detection**: Flag English text in non-English locales
2. **Mojibake & Encoding Corruption**: Flag corrupted UTF-8/16 characters
3. **Broken Placeholders**: Flag unreplaced `{0}` or raw binding tags
4. **Layout Truncation & Box Overflow**: Compare OCR text width vs. container width
5. **Technical Domain Semantics**: Verify accurate hardware terminology

---

## 📊 CI/CD Rules (持续集成规则)

### Multi-Plugin Validation
- ALL plugins MUST pass `.\llt-plugin.cmd validate --profile contributor`
- Version consistency check: `plugin.json` = `plugin.manifest.json` = `.csproj <Version>`
- Build output MUST have 0 warnings, 0 errors (enforced via `TreatWarningsAsErrors`)

### Release Automation
- Use `.\llt-plugin.cmd promote` to generate `store-entry.json`
- Release ZIP naming convention: `<plugin-id>-v<version>.zip`
- Tag format: `v<version>-<plugin-id>` (e.g., `v1.2.0-network-acceleration`)

---

**This file is alive. Every bug fix or architecture optimization MUST be recorded here.** 📝
