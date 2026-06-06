# Hardware Validation

These tools are development-only hardware verification utilities. They are not referenced by the shipping app and are blocked from release payloads by `Scripts\Assert-ShippingPayload.ps1`.

## Performance Effect Verification

Run this from an interactive Windows desktop on supported hardware:

```powershell
.\Tools\HardwareValidation\Run-PerformanceEffectVerification.ps1 -RepoRoot . -TimeoutSeconds 240
```

The script requests elevation with UAC unless `-SkipElevationCheck` is passed. It runs:

- UI power mode verification: clicks the main app power mode control and reads back `SmartFanMode`.
- God Mode batch verification: writes measurable CPU/GPU preset values, reads hardware values back, then restores the original state.
- Direct power mode verification: writes `SmartFanMode`, waits for readback, then restores the original mode.

Results are written to `Tools\HardwareValidation\PerformanceEffectVerification-*.result.txt`. The run passes only when every selected check reports `OverallPassed: True`.
