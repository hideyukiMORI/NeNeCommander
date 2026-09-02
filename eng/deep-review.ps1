[CmdletBinding()]
param(
    [Parameter()]
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),

    [Parameter()]
    [string] $ReportPath = 'artifacts/security/deep-review.json'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath($RepositoryRoot)
$artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $root 'artifacts/security'))
$resolvedReportPath = if ([System.IO.Path]::IsPathRooted($ReportPath)) {
    [System.IO.Path]::GetFullPath($ReportPath)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $root $ReportPath))
}

if (-not $resolvedReportPath.StartsWith($artifactRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Deep-review reports may be written only under artifacts/security.'
}

New-Item -ItemType Directory -Path (Split-Path -Parent $resolvedReportPath) -Force | Out-Null

$startedAt = [DateTimeOffset]::UtcNow
$conclusion = 'failed'
$stage = 'unknown'
$commit = 'uncommitted'
$checks = [System.Collections.Generic.List[object]]::new()

function Add-ReviewCheck {
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string] $Status,

        [Parameter(Mandatory)]
        [string] $Evidence
    )

    $checks.Add([ordered]@{
        name = $Name
        status = $Status
        evidence = $Evidence
    })
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [scriptblock] $Command
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
    Add-ReviewCheck -Name $Name -Status 'passed' -Evidence 'Process exited with code 0.'
}

Push-Location $root
try {
    & git rev-parse HEAD *> $null
    if ($LASTEXITCODE -eq 0) {
        $commit = (& git rev-parse HEAD).Trim()
    }

    Invoke-CheckedCommand -Name 'canonical-gate' -Command {
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'check.ps1')
    }

    $state = Get-Content -LiteralPath (Join-Path $root 'docs/PROJECT_STATE.md') -Raw
    $stageMatch = [regex]::Match($state, '(?m)^- Stage: `(?<value>[^`]+)`\s*$')
    if (-not $stageMatch.Success) {
        throw 'Project stage is missing after the canonical gate passed.'
    }
    $stage = $stageMatch.Groups['value'].Value

    if ($stage -eq 'policy-foundation') {
        Add-ReviewCheck -Name 'dependency-audit' -Status 'not-applicable' -Evidence 'Production packages and projects are prohibited by the verified stage interlock.'
        Add-ReviewCheck -Name 'adversarial-tests' -Status 'not-applicable' -Evidence 'Production tests cannot exist before the atomic implementation transition.'
        Add-ReviewCheck -Name 'mutation-testing' -Status 'not-applicable' -Evidence 'No production source is permitted; gate mutation proofs passed instead.'
        Add-ReviewCheck -Name 'codeql' -Status 'not-applicable' -Evidence 'The workflow activates CodeQL when PROJECT_STATE enters implementation.'
    }
    elseif ($stage -eq 'implementation') {
        $policy = Get-Content -LiteralPath (Join-Path $root 'eng/security-policy.json') -Raw | ConvertFrom-Json
        $architecture = Get-Content -LiteralPath (Join-Path $root 'eng/architecture.json') -Raw | ConvertFrom-Json
        $solution = Join-Path $root ([string] $architecture.solution)

        Invoke-CheckedCommand -Name 'tool-restore' -Command {
            & dotnet tool restore
        }

        $packageOutput = (& dotnet package list --project $solution --vulnerable --include-transitive --format json --output-version 1 --no-restore 2>&1) -join "`n"
        if ($LASTEXITCODE -ne 0) {
            throw "Explicit dependency audit failed: $packageOutput"
        }
        if ($packageOutput -match '"vulnerabilities"\s*:\s*\[\s*\{') {
            throw 'Explicit dependency audit found one or more vulnerable packages.'
        }
        Add-ReviewCheck -Name 'dependency-audit' -Status 'passed' -Evidence 'Direct and transitive package audit reported no vulnerability entries.'

        $adversarialTestProjects = @($policy.mutationProjects | ForEach-Object {
            $productionProjectName = [System.IO.Path]::GetFileNameWithoutExtension([string] $_.path)
            Join-Path $root "tests/$productionProjectName.Tests/$productionProjectName.Tests.csproj"
        })
        for ($iteration = 1; $iteration -le [int] $policy.adversarialRepeatCount; $iteration++) {
            foreach ($adversarialTestProject in $adversarialTestProjects) {
                $adversarialProjectName = [System.IO.Path]::GetFileNameWithoutExtension($adversarialTestProject)
                Invoke-CheckedCommand -Name "adversarial-tests-$iteration-$adversarialProjectName" -Command {
                    & dotnet test --project $adversarialTestProject --configuration Release --no-build --no-restore --filter 'TestCategory=Adversarial'
                }
            }
        }

        foreach ($mutationProject in @($policy.mutationProjects)) {
            $projectPath = Join-Path $root ([string] $mutationProject.path)
            $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
            $projectFileName = [System.IO.Path]::GetFileName($projectPath)
            $testProjectPath = Join-Path $root ("tests/$projectName.Tests/$projectName.Tests.csproj")
            $testProjectRoot = Split-Path -Parent $testProjectPath
            $relativeConfigPath = [System.IO.Path]::GetRelativePath(
                $testProjectRoot,
                (Join-Path $root 'stryker-config.json'))
            $outputPath = Join-Path $artifactRoot ("mutation/" + $projectName)
            Push-Location $testProjectRoot
            try {
                Invoke-CheckedCommand -Name "mutation-$projectName" -Command {
                    & dotnet stryker --config-file $relativeConfigPath --project $projectFileName --break-at ([int] $mutationProject.breakAt) --output $outputPath --skip-version-check
                }
            }
            finally {
                Pop-Location
            }
        }

        Add-ReviewCheck -Name 'codeql' -Status 'external-step' -Evidence 'The scheduled workflow runs CodeQL init/analyze around this script.'
    }
    else {
        throw "Unsupported project stage '$stage'."
    }

    $conclusion = 'passed'
    Write-Host "Deep review passed for stage '$stage'."
}
catch {
    Add-ReviewCheck -Name 'failure' -Status 'failed' -Evidence $_.Exception.Message
    throw
}
finally {
    $report = [ordered]@{
        schemaVersion = 1
        project = 'NeNe Commander'
        commit = $commit
        stage = $stage
        startedAtUtc = $startedAt.ToString('O')
        completedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        conclusion = $conclusion
        checks = $checks
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resolvedReportPath -Encoding utf8
    Pop-Location
}
