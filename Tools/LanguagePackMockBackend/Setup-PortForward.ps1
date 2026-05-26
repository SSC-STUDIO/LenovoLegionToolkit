#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Optional: forward local TCP port 443 to the mock catalog server (default 18765).

.NOTES
  GitHub Pages uses HTTPS. Port-forwarding 443 -> HTTP mock does NOT satisfy TLS handshakes.
  Prefer setting UDT_RESOURCE_CATALOG_URL=http://127.0.0.1:18765/catalog.json (app already supports this).

  If you still want hosts + portproxy for experiments:
  1. Start-MockCatalogServer.ps1 on port 18765
  2. Run this script (admin)
  3. App must use HTTP catalog URL via env var — do not expect https://ssc-studio.github.io to work without a local TLS terminator.
#>
param(
    [int] $ListenPort = 443,
    [int] $TargetPort = 18765,
    [string] $TargetHost = "127.0.0.1",
    [switch] $Remove
)

$ErrorActionPreference = "Stop"

if ($Remove) {
    netsh interface portproxy delete v4tov4 listenaddress=127.0.0.1 listenport=$ListenPort | Out-Null
    Write-Host "Removed portproxy 127.0.0.1:${ListenPort} -> ${TargetHost}:${TargetPort}"
    exit 0
}

netsh interface portproxy add v4tov4 listenaddress=127.0.0.1 listenport=$ListenPort connectaddress=$TargetHost connectport=$TargetPort
Write-Host "Added portproxy 127.0.0.1:${ListenPort} -> ${TargetHost}:${TargetPort}"
Write-Host "Show rules: netsh interface portproxy show all"
Write-Host "Remove: .\Setup-PortForward.ps1 -Remove"
