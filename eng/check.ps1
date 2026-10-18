[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Commit', 'Merge')]
    [string] $Mode = 'Merge'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
Push-Location $root

try {
    $gitAvailable = $null -ne (Get-Command git -ErrorAction SilentlyContinue)
    $isGitWorkTree = $false
    $beforeStatus = $null
    if ($gitAvailable) {
        & git rev-parse --is-inside-work-tree *> $null
        $isGitWorkTree = $LASTEXITCODE -eq 0
        if ($isGitWorkTree) {
            $beforeStatus = (& git status --porcelain=v1 --untracked-files=all) -join "`n"
        }
    }

    $sdkVersion = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0 -or $sdkVersion -cne '10.0.401') {
        throw "Required .NET SDK 10.0.401 is not active. Actual: '$sdkVersion'."
    }

    Write-Host '==> Conformance'
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'conformance.ps1') -RepositoryRoot $root
    if ($LASTEXITCODE -ne 0) {
        throw 'Conformance failed.'
    }

    if ($Mode -eq 'Commit') {
        Write-Host '==> Commit security checks (without negative proof fixtures)'
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'security-check.ps1') -RepositoryRoot $root -SkipProof
        if ($LASTEXITCODE -ne 0) { throw 'Commit security checks failed.' }
        if ($isGitWorkTree) {
            & git diff --check
            if ($LASTEXITCODE -ne 0) { throw 'Working-tree whitespace check failed.' }
            & git diff --cached --check
            if ($LASTEXITCODE -ne 0) { throw 'Staged whitespace check failed.' }
        }
        Write-Host 'PASS: commit checks only; select any additional verification from the actual change impact (ADR-0061).'
        return
    }

    Write-Host '==> Negative gate proofs'
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'prove-gates.ps1') -RepositoryRoot $root
    if ($LASTEXITCODE -ne 0) {
        throw 'Negative gate proofs failed.'
    }

    Write-Host '==> Security and supply-chain conformance'
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'security-check.ps1') -RepositoryRoot $root
    if ($LASTEXITCODE -ne 0) {
        throw 'Security and supply-chain conformance failed.'
    }

    $state = Get-Content -LiteralPath (Join-Path $root 'docs/PROJECT_STATE.md') -Raw
    $stageMatch = [regex]::Match($state, '(?m)^- Stage: `(?<value>[^`]+)`\s*$')
    if (-not $stageMatch.Success) {
        throw 'Project stage could not be read after conformance.'
    }

    $stage = $stageMatch.Groups['value'].Value
    if ($stage -eq 'implementation') {
        $manifest = Get-Content -LiteralPath (Join-Path $root 'eng/architecture.json') -Raw | ConvertFrom-Json
        $solution = Join-Path $root ([string] $manifest.solution)

        Write-Host '==> Locked restore'
        & dotnet restore $solution -p:Configuration=Release --locked-mode
        if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed.' }

        Write-Host '==> Formatting'
        & dotnet format whitespace $solution --verify-no-changes --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Formatting verification failed.' }

        Write-Host '==> Release build'
        & dotnet build $solution --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }

        Write-Host '==> External record-case closure'
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'verify-record-closure.ps1') -RepositoryRoot $root
        if ($LASTEXITCODE -ne 0) { throw 'External record-case closure failed.' }

        Write-Host '==> Tests'
        & dotnet test --solution $solution --configuration Release --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }

        Write-Host '==> Branch coverage thresholds'
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'verify-coverage.ps1') -RepositoryRoot $root
        if ($LASTEXITCODE -ne 0) { throw 'Coverage verification failed.' }
    }
    elseif ($stage -eq 'policy-foundation') {
        Write-Host '==> Implementation stages locked by policy-foundation interlock'
    }
    else {
        throw "Unsupported stage '$stage'."
    }

    if ($isGitWorkTree) {
        $afterStatus = (& git status --porcelain=v1 --untracked-files=all) -join "`n"
        if ($beforeStatus -cne $afterStatus) {
            throw 'The canonical gate changed the working tree.'
        }
    }
    else {
        Write-Host '==> Git clean-tree comparison unavailable until repository initialization'
    }

    Write-Host "PASS: NeNe Commander canonical gate ($stage)."
}
finally {
    Pop-Location
}
