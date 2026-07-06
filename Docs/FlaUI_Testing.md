# FlaUI Automated Verification Pipeline

## Overview
FlaUI + WinRT OCR automated UI tests for UDT. These tests run on Windows with a desktop session and administrator privileges.

## Prerequisites
1. Windows 10/11 with desktop session (not SSH/headless)
2. Administrator privileges (for global hook tests and app launch)
3. .NET 10 SDK
4. Built UDT WPF app (Debug or Release)

## Quick Start

### Option 1: Auto-elevate and run (Recommended)
```powershell
# Right-click PowerShell → Run as Administrator, then:
cd "D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit"
.\run_flaui_tests_admin.ps1
```

### Option 2: Manual run (if already admin)
```powershell
cd "D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit"
dotnet test UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj --filter "FullyQualifiedName~FlaUI" -c Debug -v n
```

## Test Categories
- `FlaUI Tests` collection: All FlaUI tests (serialized, no parallel)
- `UI.MainWindow`: Main window structure and launch tests
- `UI.Infrastructure`: Test infrastructure verification (no app needed)

## CI/CD Integration
See `.github/workflows/flaui-tests.yml` for GitHub Actions setup.

## Troubleshooting

### "FlaUI tests are skipped in CI environments"
- Run in a Windows session with desktop (not GitHub Actions hosted runners)
- For local headless servers: use PSRemoting with `-Interactive` switch

### "UDT application requires administrator privileges"
- Re-run PowerShell as Administrator
- Or: right-click PowerShell → "Run as administrator"

### "SingleInstanceGuard: Only one instance allowed"
- Run `run_flaui_tests_admin.ps1` (kills existing instances automatically)
- Or manually: `Stop-Process -Name "Universal Device Toolkit" -Force`

### Tests time out waiting for main window
- Increase `DefaultTimeoutMs` in `FlaUiTestBase.cs`
- Check if app crashes on startup (see Windows Event Viewer)
- Verify app builds successfully

## Writing New FlaUI Tests

1. Create a new class in `UniversalDeviceToolkit.Tests/FlaUI/`
2. Inherit from `FlaUiTestBase`
3. Add `[Collection("FlaUI Tests")]` attribute
4. Add `[Trait("Category", "UI.<YourCategory>")]` for filtering
5. Use `MainWindow`, `Automation`, `WaitForElement()`, `ExtractTextFromWindowAsync()` helpers

Example:
```csharp
[Fact]
public async Task MyNewTest()
{
    await InitializeAsync();
    var button = WaitForElement("MyButtonAutomationId");
    button.Click();
    await AssertWindowContainsTextAsync("Expected Text");
}
```
