[CmdletBinding()]
param(
    [Parameter()]
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath($RepositoryRoot)
$tempParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$proofRoot = [System.IO.Path]::GetFullPath((Join-Path $tempParent ("NeNeCommander-SecurityProof-" + [Guid]::NewGuid().ToString('N'))))

if (-not $proofRoot.StartsWith($tempParent, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Resolved security proof root escaped the operating-system temporary directory.'
}

function Copy-Foundation {
    param(
        [Parameter(Mandatory)]
        [string] $Destination
    )

    New-Item -ItemType Directory -Path $Destination | Out-Null
    foreach ($item in Get-ChildItem -LiteralPath $root -Force) {
        if ($item.Name -in @('.git', '.vs', 'artifacts', 'bin', 'obj', 'TestResults')) {
            continue
        }
        Copy-Item -LiteralPath $item.FullName -Destination $Destination -Recurse
    }
}

function Assert-SecurityFailure {
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string] $ExpectedRule,

        [Parameter(Mandatory)]
        [scriptblock] $Mutate
    )

    $caseRoot = Join-Path $proofRoot $Name
    Copy-Foundation -Destination $caseRoot
    & $Mutate $caseRoot

    $output = (& pwsh -NoProfile -File (Join-Path $caseRoot 'eng/security-check.ps1') -RepositoryRoot $caseRoot -SkipProof 2>&1) -join "`n"
    if ($LASTEXITCODE -eq 0) {
        throw "Security negative proof '$Name' unexpectedly passed."
    }
    if ($output -notmatch "\[$([regex]::Escape($ExpectedRule))\]") {
        throw "Security negative proof '$Name' failed for the wrong reason. Expected $ExpectedRule. Output: $output"
    }
}

try {
    New-Item -ItemType Directory -Path $proofRoot | Out-Null

    Assert-SecurityFailure -Name 'mutable-action-tag' -ExpectedRule 'SEC-006' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot '.github/workflows/quality.yml'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace('actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1', 'actions/checkout@v7')
        Set-Content -LiteralPath $path -Value $content -NoNewline
    }

    Assert-SecurityFailure -Name 'secret-token' -ExpectedRule 'SEC-005' -Mutate {
        param($caseRoot)
        $syntheticToken = 'github' + '_pat_' + ('A' * 82)
        Set-Content -LiteralPath (Join-Path $caseRoot 'synthetic-secret.txt') -Value $syntheticToken
    }

    Assert-SecurityFailure -Name 'unsafe-script' -ExpectedRule 'SEC-011' -Mutate {
        param($caseRoot)
        $unsafeCommand = 'Invoke' + '-Expression'
        Set-Content -LiteralPath (Join-Path $caseRoot 'eng/Unsafe.ps1') -Value "$unsafeCommand `$args[0]"
    }

    Assert-SecurityFailure -Name 'audit-disabled' -ExpectedRule 'SEC-007' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'Directory.Build.props'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace('<NuGetAudit>true</NuGetAudit>', '<NuGetAudit>false</NuGetAudit>')
        Set-Content -LiteralPath $path -Value $content -NoNewline
    }

    Assert-SecurityFailure -Name 'privileged-pull-request' -ExpectedRule 'SEC-004' -Mutate {
        param($caseRoot)
        Add-Content -LiteralPath (Join-Path $caseRoot '.github/workflows/quality.yml') -Value "`npull_request_target:`n"
    }

    Assert-SecurityFailure -Name 'mutation-threshold-weakened' -ExpectedRule 'TST-008' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'stryker-config.json'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace('"break": 90', '"break": 0')
        Set-Content -LiteralPath $path -Value $content -NoNewline
    }

    Write-Host 'Security proofs passed: mutable actions, secrets, unsafe scripts, audit weakening, privileged PR execution, and mutation weakening are rejected.'
}
finally {
    if (Test-Path -LiteralPath $proofRoot) {
        $resolvedProofRoot = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $proofRoot).Path)
        if (-not $resolvedProofRoot.StartsWith($tempParent, [System.StringComparison]::OrdinalIgnoreCase) -or
            -not ([System.IO.Path]::GetFileName($resolvedProofRoot).StartsWith('NeNeCommander-SecurityProof-', [System.StringComparison]::Ordinal))) {
            throw 'Refusing to clean an unverified security proof root.'
        }
        Remove-Item -LiteralPath $resolvedProofRoot -Recurse -Force
    }
}
