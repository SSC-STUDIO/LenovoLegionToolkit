# FlaUI Automated Verification Pipeline

## Overview
FlaUI native UI Automation tests for UDT. These tests run on Windows with a desktop session and administrator privileges.

## Prerequisites
1. Windows 10/11 with desktop session (not SSH/headless)
2. Administrator privileges (for global hook tests and app launch)
3. .NET 10 SDK
4. Built UDT WPF app (Debug or Release)

## Quick Start

### Option 1: Auto-elevate and run (Recommended)
```powershell
# Right-click PowerShell → Run as Administrator, then:
cd "C:\path\to\UniversalDeviceToolkit"
dotnet test UniversalDeviceToolkit.UiAutomation.Tests/UniversalDeviceToolkit.UiAutomation.Tests.csproj --framework net10.0-windows10.0.26100.0 --configuration Release --filter "FullyQualifiedName~FlaUI"
```

### Option 2: Manual run (if already admin)
```powershell
cd "C:\path\to\UniversalDeviceToolkit"
dotnet test UniversalDeviceToolkit.UiAutomation.Tests/UniversalDeviceToolkit.UiAutomation.Tests.csproj --framework net10.0-windows10.0.26100.0 --configuration Release --filter "FullyQualifiedName~FlaUI" -v n
```

## Test Categories
- `FlaUI Tests` collection: All FlaUI tests (serialized, no parallel)
- `UI.MainWindow`: Main window structure and launch tests
- `UI.Infrastructure`: Test infrastructure verification (no app needed)

## CI/CD Integration
See `.github/workflows/flaui-tests.yml` for GitHub Actions setup.

The nightly job writes the TRX to `TestResults/FlaUI` and fails when the file is
missing or contains a `Skipped`/`NotExecuted` result. Desktop preflight failures
also fail the job; they are not converted into passing skips.

## Troubleshooting

### "FlaUI desktop preflight failed"
- Run on the self-hosted Windows desktop runner used by `.github/workflows/flaui-tests.yml`
- Verify the session is interactive, elevated, and has the Release WPF executable

### "UDT application requires administrator privileges"
- Re-run PowerShell as Administrator
- Or: right-click PowerShell → "Run as administrator"

### "SingleInstanceGuard: Only one instance allowed"
- Stop any existing `Universal Device Toolkit` process, then rerun the Release UI test command as Administrator
- Or manually: `Stop-Process -Name "Universal Device Toolkit" -Force`

### Tests time out waiting for main window
- Increase `DefaultTimeoutMs` in `FlaUiTestBase.cs`
- Check if app crashes on startup (see Windows Event Viewer)
- Verify app builds successfully

## Writing New FlaUI Tests

1. Create a new class in `UniversalDeviceToolkit.UiAutomation.Tests/FlaUI/`
2. Inherit from `FlaUiTestBase` (xUnit initializes and disposes the app automatically)
3. Add `[Collection("FlaUI Tests")]` attribute
4. Add `[Trait("Category", "UI.<YourCategory>")]` for filtering
5. Use `MainWindow`, `Automation`, `WaitForElement()`, `ExtractTextFromWindowAsync()` helpers

Example:
```csharp
[Fact]
public void MyNewTest()
{
    var button = MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("MyButtonAutomationId"));
    Assert.NotNull(button);
    button.Click();
}
```
