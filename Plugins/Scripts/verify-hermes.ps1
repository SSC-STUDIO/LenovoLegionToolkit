$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
dotnet test UniversalDeviceToolkit.Plugins.sln -c Release --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build UniversalDeviceToolkit.Plugins.sln -c Release --nologo
exit $LASTEXITCODE
