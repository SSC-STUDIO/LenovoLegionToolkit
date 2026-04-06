# Plugin Trust Chain Hardening Design

Date: 2026-04-06
Status: Proposed
Primary repo: `Software/LenovoLegionToolkit`
Companion repo: `Software/LenovoLegionToolkit-Plugins`

## Summary

This design hardens the Lenovo Legion Toolkit plugin ecosystem end to end. The current code validates extracted certificates and certificate chains, but it does not reliably prove that a plugin DLL or native payload was Authenticode-signed correctly, that the signer is an approved publisher, or that the downloaded ZIP package matches trusted release metadata.

The fix introduces a strict trust chain with three gates:

1. Package-level integrity: remote plugin ZIPs must match a published SHA-256 hash.
2. File-level signature validation: every executable payload in a plugin package must pass Authenticode verification.
3. Publisher-level trust enforcement: accepted signatures must chain to an approved publisher identity in production mode.

This design also removes unsafe `ViVeTool.exe` discovery fallbacks and requires runtime validation before executing bundled native tools.

## Goals

- Prevent loading plugins whose files are unsigned, tampered, or signed by an unapproved publisher.
- Prevent installing remote plugin ZIPs whose contents do not match trusted release metadata.
- Prevent plugin packages from smuggling unverified native `.exe` or `.dll` payloads.
- Remove `PATH` and current-directory executable hijack risk from the `ViVeTool` plugin.
- Preserve a usable local development path behind explicit non-production settings.

## Non-Goals

- Re-architect the entire plugin marketplace UI.
- Introduce sandboxed execution for plugin code.
- Support legacy unsigned third-party plugins in production mode.
- Build a new signing pipeline for external vendors beyond documenting the required package/signature contract.

## Current Problems

### Main repo: `Software/LenovoLegionToolkit`

- `PluginSignatureValidator` uses `X509Certificate.CreateFromSignedFile(...)` and `X509Chain.Build(...)`, which proves certificate trust but does not prove the file itself has a valid Authenticode signature.
- `PluginRepositoryService` only hashes the extracted main DLL, not the downloaded ZIP or additional native payloads inside the package.
- Some plugin lifecycle paths can reflect over assemblies before a strict trust check has completed.

### Plugin repo: `Software/LenovoLegionToolkit-Plugins`

- `store.json` entries do not provide `fileHash`, so package integrity cannot be enforced.
- `ViveTool` can currently discover `ViVeTool.exe` from `PATH` or the current working directory.
- `ViveTool` accepts a user-selected executable based only on filename.
- Native payloads such as `shell.exe`, `shell.dll`, and `ViVeTool.exe` are not consistently re-validated at execution time.

## Security Invariants

Production mode must satisfy all of the following:

- Every remotely installed plugin package has a declared ZIP SHA-256 hash.
- The downloaded ZIP matches that hash exactly.
- Every executable payload copied into the plugins directory passes Authenticode validation.
- Every accepted signer matches an approved publisher allowlist.
- Runtime execution of bundled native tools re-checks the path and signature before launch.
- `PATH` and current-directory executable discovery are disabled for plugin-managed tools.

Development mode may relax selected rules, but only behind explicit settings and never by accident.

## Design

## 1. Signature Verification Abstraction

Add a dedicated verification abstraction in the main repo so tests can mock signature decisions without needing real signed files or Win32 state.

Proposed shape:

- `IAuthenticodeVerifier`
- `AuthenticodeVerificationResult`

Responsibilities:

- Verify a file with the Windows Authenticode policy, not just certificate parsing.
- Return a normalized result with:
  - `IsSigned`
  - `IsSignatureValid`
  - `SignerThumbprint`
  - `SignerSubject`
  - `Issuer`
  - `NotBefore`
  - `NotAfter`
  - `TimestampPresent`
  - `ErrorCode`
  - `ErrorMessage`

Implementation:

- Use Windows-native Authenticode verification via `WinVerifyTrust`.
- Extract signer certificate details only after file signature validation succeeds.
- Keep revocation behavior configurable through existing settings.

## 2. `PluginSignatureSettings` Trust Policy

Extend `PluginSignatureSettings` so production rules are explicit and testable.

Add or repurpose settings for:

- `AllowedPublisherThumbprints`
- `AllowedPublisherSubjects`
- `RequireAllowedPublisher`
- `AllowUnsignedPlugins`
- `AllowTestCertificates`
- `CheckRevocationStatus`
- `EnableLocalDevelopmentPackageFallback`

Rules:

- Production:
  - require valid Authenticode signature
  - require approved publisher match
  - require revocation checking
  - disable local package fallback
- Development:
  - may allow unsigned plugins only when explicitly configured
  - may allow test certificates
  - may enable local package fallback

`TrustedIssuers` should remain temporarily for backward compatibility but should no longer be the primary production trust primitive.

## 3. `PluginSignatureValidator` Flow

Replace the current validator flow with:

1. Fail if the file does not exist.
2. Run Authenticode verification on the file bytes.
3. If the file is unsigned:
   - reject in production
   - allow only under explicit development settings
4. If the signature is invalid or the file was tampered with:
   - reject unconditionally
5. Build certificate trust and revocation checks on the actual signer certificate.
6. Enforce allowed publisher thumbprint or subject policy in production.
7. Return a structured result that callers can log and test.

This validator becomes the single source of truth for:

- main plugin DLL loading
- dependency DLL resolution
- satellite resource assembly resolution
- package installation-time executable validation
- runtime native executable re-validation

## 4. `PluginManager` and `PluginLoader`

Keep the current "validate before load" pattern, but ensure every path uses the strengthened validator.

Required changes:

- `PluginManager.TryLoadTrustedPluginAssembly(...)` continues to gate dependency and satellite loads, now using real Authenticode results.
- `PluginLoader.LoadFromFileAsync(...)` uses the same validator contract without any bypass path.
- Any code path that currently does `Assembly.LoadFrom(...)` or equivalent before trust validation must be refactored so validation happens first.

Expected outcome:

- A plugin DLL with a real certificate but an invalid file signature no longer loads.
- A correctly signed DLL from an unapproved publisher no longer loads in production.
- Dependency and satellite DLLs cannot bypass the policy.

## 5. `PluginRepositoryService`

Strengthen package validation from "best effort" to "required for remote install".

### Package integrity

- Require `manifest.FileHash` for all remote downloads.
- Compute SHA-256 over the ZIP file itself immediately after download.
- Reject the package if the ZIP hash is missing or mismatched.

### Package contents

- After extraction, enumerate all executable payloads:
  - `*.dll`
  - `*.exe`
- Validate every executable payload with `PluginSignatureValidator` before copying into the live plugins directory.
- Reject the whole package if any executable payload fails validation.

### Local fallback

- Keep local package fallback for explicit development scenarios only.
- Guard it behind `EnableLocalDevelopmentPackageFallback`.
- Ensure fallback packages still pass content validation before installation.

## 6. Runtime Native Payload Validation

Any plugin that launches bundled executables or loads bundled native DLLs must re-check trust at runtime.

Main requirement:

- Before executing a native tool from a plugin-managed path, validate:
  - the resolved path is within the expected plugin-controlled directory
  - the file still exists
  - the file still passes the signature validator

This covers at least:

- `shell.exe`
- `shell.dll`
- `ViVeTool.exe`

## 7. `LenovoLegionToolkit-Plugins` Package Metadata

Update `store.json` so every plugin entry publishes:

- `downloadUrl`
- `fileHash` as ZIP SHA-256

Publishing contract:

- The ZIP hash must correspond to the exact published release asset.
- Asset naming must remain stable enough for the main repo to resolve and verify packages consistently.
- Plugin packages that contain native executables must ship trusted signed binaries.

## 8. `ViveTool` Hardening

Refactor `Plugins/ViveTool` so the plugin no longer trusts ambient executable discovery.

### Path resolution

Allowed sources:

- bundled path inside the plugin package
- managed built-in download location, but only after validation
- user-selected path, but only after validation

Disallowed sources:

- `PATH`
- current working directory

### Download validation

- Downloaded ZIP or EXE payloads must be hash-validated and signature-validated before use.
- If the plugin keeps a built-in download flow, the expected hash must come from controlled metadata, not from the download itself.

### User-selected path

- `SetViveToolPathAsync(...)` must reject files that fail signature validation, even if the filename is `ViVeTool.exe`.

### Execution

- `ViveToolProcessService` should execute only a path that already passed validation, and should re-validate immediately before launch.

## 9. Tests

### Main repo tests

Add or update tests for:

- `PluginSignatureValidator`
  - missing file
  - unsigned file in production
  - unsigned file in development
  - signed but invalid Authenticode result
  - valid signature with unapproved publisher
  - valid signature with approved publisher
- `PluginManagerSecurityTests`
  - dependency DLL rejected on failed signature validation
  - satellite DLL rejected on failed signature validation
- `PluginRepositoryService`
  - reject remote package without `fileHash`
  - reject ZIP hash mismatch
  - reject package when any extracted `.exe` or `.dll` fails validation
  - allow package only when all executable payloads validate

Tests should mock the Authenticode verifier instead of relying on real signed fixtures.

### Plugin repo tests

Add or update tests for:

- `ViveToolPathService`
  - reject `PATH` fallback
  - reject current-directory fallback
  - reject user-selected unsigned or invalid executable
  - accept validated bundled path
- `ViveToolDownloadService`
  - reject downloaded payload when validation fails
- runtime launch helpers for native tools
  - verify execution is blocked when signature validation fails after installation

## Rollout Plan

1. Implement the Authenticode verification abstraction in `LenovoLegionToolkit`.
2. Convert `PluginSignatureValidator` to use that abstraction and publisher allowlists.
3. Tighten `PluginManager`, `PluginLoader`, and `PluginRepositoryService`.
4. Update `LenovoLegionToolkit-Plugins` runtime code, especially `ViveTool`.
5. Backfill `store.json` with ZIP SHA-256 hashes.
6. Run targeted tests in both repositories before any broader integration run.

## Risks and Tradeoffs

- Existing unsigned or loosely packaged plugins will stop installing in production mode.
- Release engineering must keep `store.json` hashes synchronized with published assets.
- If publisher allowlists are too narrow, legitimate signed plugins may be blocked until metadata is updated.
- Development workflows need an explicit opt-in escape hatch or local contributors will be blocked.

## Acceptance Criteria

- Remote plugin installation fails when `fileHash` is absent or wrong.
- Main plugin DLLs, dependency DLLs, and satellite DLLs all require valid Authenticode verification.
- Package installation fails if any packaged `.exe` or `.dll` fails trust validation.
- `ViveTool` no longer resolves from `PATH` or the current working directory.
- User-selected `ViVeTool.exe` files require successful signature validation.
- Existing unit tests and new security regression tests pass in both repositories.
