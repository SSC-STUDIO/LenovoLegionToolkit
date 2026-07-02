## Summary

`VantagePackageDownloader.GetPackage` parses required Lenovo package XML fields with null-forgiving `SelectSingleNode(...)!` and unguarded `int.Parse` / `DateTime.Parse`. Any malformed or incomplete package metadata throws and aborts `GetPackagesAsync` for the entire machine catalog — unlike update detection, which is already wrapped in per-package try/catch.

## Evidence

`UniversalDeviceToolkit.Lib/PackageDownloader/VantagePackageDownloader.cs` lines 49-52 (no per-package try/catch in loop) and 114-122:

```csharp
var id = document.SelectSingleNode("/Package/@id")!.InnerText;
var title = document.SelectSingleNode("/Package/Title/Desc")!.InnerText;
var fileSizeBytes = int.Parse(document.SelectSingleNode("/Package/Files/Installer/File/Size")!.InnerText);
var releaseDate = DateTime.Parse(releaseDateString);
```

Compare with lines 129-140 where `updateDetector.DetectAsync` failures are caught and logged per package.

## Impact

One bad package XML entry from Lenovo can prevent listing **all** Vantage catalog packages for a machine type, even though remaining packages could still be shown. Related pattern: #56 (PCSupport JSON parsing).

## Expected behavior

Skip or degrade individual malformed package entries and continue processing the rest of the catalog.

## Suggested fix

Wrap `GetPackage` body (or the foreach entry in `GetPackagesAsync`) in try/catch; return `null` for invalid entries and filter them out. Replace `int.Parse`/`DateTime.Parse` with `TryParse` and treat missing XML nodes as skip.

Reported by DevOps & QA audit (verified 2026-06-13).
