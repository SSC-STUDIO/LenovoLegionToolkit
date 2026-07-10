$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
dotnet test UniversalDeviceToolkit.sln -c Release --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build UniversalDeviceToolkit.sln -c Release --nologo -m:1
exit $LASTEXITCODE
