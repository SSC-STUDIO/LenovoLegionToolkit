# Local language-pack catalog

Run the mock catalog server when developing or manually checking language-pack
downloads without WAN access:

```powershell
.\Tools\LanguagePackMockBackend\Start-MockCatalogServer.ps1
$env:UDT_RESOURCE_CATALOG_URL = "http://127.0.0.1:18765/catalog.json"
```

The server exposes `catalog.json` and a package generated from the current WPF
satellite resource. The retired desktop UI automation projects are not required.
