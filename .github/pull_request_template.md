## Summary
- 

## Verification
- [ ] `dotnet build UniversalDeviceToolkit.sln --configuration Release`
- [ ] `dotnet test UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj --framework net10.0-windows`
- [ ] CHANGELOG.md updated for user-visible changes

## Plugin Changes (if applicable)
- [ ] Plugin tests and validation pass (`Plugins\udt-plugin.cmd test` / `validate`)
- [ ] Official plugin metadata is in `Plugins/Official/*/plugin.manifest.json`
- [ ] Generated `Plugins/.build/catalog/store.json` was regenerated intentionally for a release
- [ ] New plugin packages do not bundle host DLLs

## Notes
- 
