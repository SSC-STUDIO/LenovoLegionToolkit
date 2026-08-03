[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Container })]
    [string]$Path
)

$files = Get-ChildItem -LiteralPath $Path -Recurse -File |
    Where-Object { $_.Extension -in @('.exe', '.dll') }

if (-not $files) {
    throw "No PE files were found under '$Path'."
}

$invalid = @(
    foreach ($file in $files) {
        $signature = Get-AuthenticodeSignature -LiteralPath $file.FullName
        if ($signature.Status -ne 'Valid' -or $null -eq $signature.SignerCertificate) {
            [pscustomobject]@{
                Path = $file.FullName
                Status = $signature.Status
                StatusMessage = $signature.StatusMessage
            }
        }
    }
)

if ($invalid.Count -gt 0) {
    $details = $invalid | ForEach-Object { "$($_.Path): $($_.Status) ($($_.StatusMessage))" }
    throw "Authenticode verification failed for $($invalid.Count) of $($files.Count) file(s):`n$($details -join [Environment]::NewLine)"
}

Write-Host "Verified Authenticode signatures for $($files.Count) PE file(s) under '$Path'."
