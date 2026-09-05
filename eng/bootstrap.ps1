[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
Push-Location $root

try {
    $sdkVersion = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0 -or $sdkVersion -cne '10.0.400') {
        throw "Install .NET SDK 10.0.400 before bootstrapping. Actual active SDK: '$sdkVersion'."
    }

    & git rev-parse --is-inside-work-tree *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'Initialize or clone this repository before bootstrapping.'
    }

    & git config --local core.hooksPath .githooks
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not configure the repository-owned Git hooks.'
    }

    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'check.ps1') -Mode Commit
    if ($LASTEXITCODE -ne 0) {
        throw 'Bootstrap verification failed.'
    }

    Write-Host 'Bootstrap complete: pinned SDK verified, repository hooks enabled, commit checks passed; merge gate not run.'
}
finally {
    Pop-Location
}
