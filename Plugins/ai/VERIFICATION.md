# Verification

## Canonical Command

```powershell
powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File scripts/verify-hermes.ps1
```

## Focused Verification
Before the canonical gate, run the narrowest relevant plugin test project or test filter. Record exact commands and exit codes in the active task plan.

## Acceptance
- Focused regression coverage passes.
- Canonical verification exits zero.
- No new warnings or failures are hidden.
