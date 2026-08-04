<#
.SYNOPSIS
  Fail when a FlaUI TRX contains no results or any skipped test.

.DESCRIPTION
  Desktop preflight failures must fail the workflow before the test command.
  This check protects the next boundary: a green test command must contain
  real FlaUI results, and every selected test must execute.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TrxPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $TrxPath -PathType Leaf)) {
    throw "FlaUI TRX was not found: $TrxPath"
}

[xml]$trx = Get-Content -LiteralPath $TrxPath -Raw -Encoding UTF8
$results = @($trx.SelectNodes("//*[local-name()='UnitTestResult']"))
if ($results.Count -eq 0) {
    throw "FlaUI TRX contains no UnitTestResult entries: $TrxPath"
}

$blockedResults = @($results | Where-Object {
        $outcome = [string]$_.outcome
        $outcome -eq 'Skipped' -or $outcome -eq 'NotExecuted'
    })

if ($blockedResults.Count -gt 0) {
    $names = @($blockedResults |
        ForEach-Object { [string]$_.testName } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 10)
    $sample = if ($names.Count -gt 0) { $names -join ', ' } else { '<unnamed test>' }
    throw "FlaUI TRX contains $($blockedResults.Count) skipped or not-executed result(s): $sample"
}

Write-Host "FlaUI TRX contains $($results.Count) executed result(s) and no skipped tests."
