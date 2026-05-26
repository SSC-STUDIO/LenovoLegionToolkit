# Local language-pack catalog (no WAN)

Use a **separate mock HTTP server** plus **catalog URL override** — do not embed the server inside `LanguagePackUi.Smoke`.

## 1. Start mock backend (terminal A)

```powershell
.\Tools\LanguagePackMockBackend\Start-MockCatalogServer.ps1
```

Serves `http://127.0.0.1:18765/catalog.json` and `de.zip` (built from real `Universal Device Toolkit.resources.dll`).

## 2. Point the app at it

The app already reads `UDT_RESOURCE_CATALOG_URL` / `LLT_RESOURCE_CATALOG_URL`:

```powershell
$env:UDT_RESOURCE_CATALOG_URL = "http://127.0.0.1:18765/catalog.json"
$env:LLT_RESOURCE_CATALOG_URL  = $env:UDT_RESOURCE_CATALOG_URL
# launch Universal Device Toolkit or smoke test
```

## 3. Run smoke (terminal B)

```powershell
dotnet run --project Tools/LanguagePackUi.Smoke -- --local
# or explicit:
dotnet run --project Tools/LanguagePackUi.Smoke -- --catalog-url http://127.0.0.1:18765/catalog.json
```

Backend-only (no UI):

```powershell
dotnet run --project Tools/LanguagePackUi.Smoke -- --backend-only --local
```

One-shot orchestration:

```powershell
.\Tools\LanguagePackMockBackend\Run-OfflineLanguagePackSmoke.ps1
```

## Port forwarding (optional)

`Setup-PortForward.ps1` (admin) can add `netsh interface portproxy` (e.g. 443 → 18765).

**Limitation:** production catalog is **HTTPS** on `ssc-studio.github.io`. Forwarding ports does not fix TLS/certificate validation. For offline dev, **catalog URL env override to `http://127.0.0.1:18765/...`** is the supported path.

## Real online test

```powershell
dotnet run --project Tools/LanguagePackUi.Smoke -- --online
```
