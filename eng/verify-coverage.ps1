[CmdletBinding()]
param(
    [Parameter()]
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath($RepositoryRoot)
$settingsPath = Join-Path $root 'eng/coverage.settings'
$artifactRoot = Join-Path $root 'artifacts/coverage'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null

$coverageCases = @(
    [pscustomobject]@{
        Name = 'Domain'
        TestProject = 'tests/NeNeCommander.Domain.Tests/NeNeCommander.Domain.Tests.csproj'
        Package = 'NeNeCommander.Domain'
        MinimumBranchRate = 1.0
    },
    [pscustomobject]@{
        Name = 'Application'
        TestProject = 'tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj'
        Package = 'NeNeCommander.Application'
        MinimumBranchRate = 1.0
    },
    [pscustomobject]@{
        Name = 'Infrastructure.Windows'
        TestProject = 'tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj'
        Package = 'NeNeCommander.Infrastructure.Windows'
        MinimumBranchRate = 0.9
    },
    [pscustomobject]@{
        Name = 'Presentation.WinUI'
        TestProject = 'tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj'
        Package = 'NeNeCommander.Presentation.WinUI'
        MinimumBranchRate = 0.9
    }
)

foreach ($coverageCase in $coverageCases) {
    $projectPath = Join-Path $root $coverageCase.TestProject
    $reportName = $coverageCase.Name.ToLowerInvariant() + '.cobertura.xml'
    $reportPath = Join-Path $artifactRoot $reportName

    & dotnet test --project $projectPath --configuration Release --no-build --no-restore -- `
        --coverage `
        --coverage-settings $settingsPath `
        --coverage-output-format cobertura `
        --coverage-output $reportPath
    if ($LASTEXITCODE -ne 0) {
        throw "$($coverageCase.Name) coverage test run failed."
    }

    [xml] $report = Get-Content -LiteralPath $reportPath -Raw
    $packages = @($report.coverage.packages.package | Where-Object {
        $_.name -ceq $coverageCase.Package
    })
    if ($packages.Count -ne 1) {
        throw "$($coverageCase.Name) report must contain exactly one package named '$($coverageCase.Package)'."
    }

    $branchRate = [double]::Parse(
        [string] $packages[0].'branch-rate',
        [System.Globalization.CultureInfo]::InvariantCulture)
    if ($branchRate -lt [double] $coverageCase.MinimumBranchRate) {
        $actualPercent = $branchRate.ToString('P2', [System.Globalization.CultureInfo]::InvariantCulture)
        $requiredPercent = ([double] $coverageCase.MinimumBranchRate).ToString(
            'P2',
            [System.Globalization.CultureInfo]::InvariantCulture)
        throw "$($coverageCase.Name) branch coverage is $actualPercent; required minimum is $requiredPercent."
    }

    $displayRate = $branchRate.ToString('P2', [System.Globalization.CultureInfo]::InvariantCulture)
    Write-Host "PASS: $($coverageCase.Name) branch coverage $displayRate."
}

Write-Host 'PASS: all protected branch-coverage thresholds are satisfied.'
