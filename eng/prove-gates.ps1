[CmdletBinding()]
param(
    [Parameter()]
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath($RepositoryRoot)
$tempParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$proofRoot = Join-Path $tempParent ("NeNeCommander-GateProof-" + [Guid]::NewGuid().ToString('N'))
$proofRoot = [System.IO.Path]::GetFullPath($proofRoot)

if (-not $proofRoot.StartsWith($tempParent, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Resolved proof root escaped the operating-system temporary directory.'
}

. (Join-Path $PSScriptRoot 'repository-tree.ps1')

function Assert-FoundationMaterialization {
    $source = Join-Path $proofRoot 'materialization-source'
    $destination = Join-Path $proofRoot 'materialization-destination'
    $sourceFile = Join-Path $source 'src/UntrackedInspectionInput.cs'
    $generatedFile = Join-Path $source 'src/Feature/bin/Generated.dll'
    $testOutput = Join-Path $source 'tests/Feature/obj/project.assets.json'
    $reparseTarget = Join-Path $proofRoot 'materialization-reparse-target'
    $reparseLink = Join-Path $source 'linked-output'
    New-Item -ItemType Directory -Path (Split-Path -Parent $sourceFile) -Force | Out-Null
    New-Item -ItemType Directory -Path (Split-Path -Parent $generatedFile) -Force | Out-Null
    New-Item -ItemType Directory -Path (Split-Path -Parent $testOutput) -Force | Out-Null
    Set-Content -LiteralPath $sourceFile -Value 'internal sealed class UntrackedInspectionInput { }'
    Set-Content -LiteralPath $generatedFile -Value 'generated'
    Set-Content -LiteralPath $testOutput -Value '{}'
    New-Item -ItemType Directory -Path $reparseTarget | Out-Null
    Set-Content -LiteralPath (Join-Path $reparseTarget 'External.cs') -Value 'internal sealed class External { }'
    New-Item -ItemType Junction -Path $reparseLink -Target $reparseTarget | Out-Null

    Copy-ProofFoundation -RepositoryRoot $source -Destination $destination

    if (-not (Test-Path -LiteralPath (Join-Path $destination 'src/UntrackedInspectionInput.cs') -PathType Leaf)) {
        throw 'Foundation materialization dropped an untracked inspection input.'
    }
    if (Test-Path -LiteralPath (Join-Path $destination 'src/Feature/bin/Generated.dll') -PathType Leaf) {
        throw 'Foundation materialization copied nested build output.'
    }
    if (Test-Path -LiteralPath (Join-Path $destination 'tests/Feature/obj/project.assets.json') -PathType Leaf) {
        throw 'Foundation materialization copied nested restore output.'
    }
    if (Test-Path -LiteralPath (Join-Path $destination 'linked-output/External.cs') -PathType Leaf) {
        throw 'Foundation materialization traversed a reparse-point directory.'
    }
}

function Assert-ConformanceFailure {
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string] $ExpectedRule,

        [Parameter(Mandatory)]
        [scriptblock] $Mutate
    )

    $caseRoot = Join-Path $proofRoot $Name
    Copy-ProofFoundation -RepositoryRoot $root -Destination $caseRoot
    & $Mutate $caseRoot

    $output = (& pwsh -NoProfile -File (Join-Path $caseRoot 'eng/conformance.ps1') -RepositoryRoot $caseRoot -Quiet 2>&1) -join "`n"
    if ($LASTEXITCODE -eq 0) {
        throw "Negative proof '$Name' unexpectedly passed."
    }

    if ($output -notmatch "\[$([regex]::Escape($ExpectedRule))\]") {
        throw "Negative proof '$Name' failed for the wrong reason. Expected $ExpectedRule. Output: $output"
    }
}

try {
    New-Item -ItemType Directory -Path $proofRoot | Out-Null
    Assert-FoundationMaterialization

    Assert-ConformanceFailure -Name 'missing-document' -ExpectedRule 'DOC-001' -Mutate {
        param($caseRoot)
        Remove-Item -LiteralPath (Join-Path $caseRoot 'docs/GLOSSARY.md')
    }

    Assert-ConformanceFailure -Name 'duplicate-rule' -ExpectedRule 'RULE-001' -Mutate {
        param($caseRoot)
        Add-Content -LiteralPath (Join-Path $caseRoot 'docs/GLOSSARY.md') -Value "`r`n### ARC-001 — Invalid duplicate`r`n`r`n- Status: **active**`r`n"
    }

    Assert-ConformanceFailure -Name 'weakened-build' -ExpectedRule 'CFG-003' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'Directory.Build.props'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace('<TreatWarningsAsErrors>true</TreatWarningsAsErrors>', '<TreatWarningsAsErrors>false</TreatWarningsAsErrors>')
        Set-Content -LiteralPath $path -Value $content -NoNewline
    }

    Assert-ConformanceFailure -Name 'mstest-sdk-pin-drift' -ExpectedRule 'CFG-002' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'global.json'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace('"MSTest.Sdk": "4.4.0"', '"MSTest.Sdk": "4.3.3"')
        Set-Content -LiteralPath $path -Value $content -NoNewline
    }

    Assert-ConformanceFailure -Name 'configuration-mismatched-restore' -ExpectedRule 'QLT-014' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'eng/check.ps1'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace('dotnet restore $solution -p:Configuration=Release --locked-mode', 'dotnet restore $solution --locked-mode')
        Set-Content -LiteralPath $path -Value $content -NoNewline
    }

    Assert-ConformanceFailure -Name 'suppression' -ExpectedRule 'CS-020' -Mutate {
        param($caseRoot)
        $testRoot = Join-Path $caseRoot 'tests/PolicyProof'
        New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $testRoot 'Violation.cs') -Value "#pragma warning disable CS0168`r`ninternal sealed class Violation { }`r`n"
    }

    Assert-ConformanceFailure -Name 'production-interlock' -ExpectedRule 'STATE-003' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'docs/PROJECT_STATE.md'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace('- Stage: `implementation`', '- Stage: `policy-foundation`')
        $content = $content.Replace('- Production code: `permitted`', '- Production code: `prohibited`')
        Set-Content -LiteralPath $path -Value $content -NoNewline
    }

    Assert-ConformanceFailure -Name 'platform-api-outside-infrastructure' -ExpectedRule 'CS-018' -Mutate {
        param($caseRoot)
        $testRoot = Join-Path $caseRoot 'tests/PolicyProof'
        New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $testRoot 'IoViolation.cs') -Value "using System.IO;`r`ninternal sealed class IoViolation { }`r`n"
    }

    Assert-ConformanceFailure -Name 'environment-outside-settings-location' -ExpectedRule 'CS-010' -Mutate {
        param($caseRoot)
        $testRoot = Join-Path $caseRoot 'tests/PolicyProof'
        New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $testRoot 'EnvironmentViolation.cs') -Value "internal sealed class EnvironmentViolation { private static string Read() { return Environment.CurrentDirectory; } }`r`n"
    }

    Assert-ConformanceFailure -Name 'color-scheme-dictionary-drift' -ExpectedRule 'ARC-012' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'src/NeNeCommander.App/Themes/Schemes/dracula.xaml'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace('<Color x:Key="SelectionMarkColor">', '<Color x:Key="SelectionMarkerColor">')
        Set-Content -LiteralPath $path -Value $content -NoNewline
    }

    Assert-ConformanceFailure -Name 'color-outside-scheme-dictionary' -ExpectedRule 'ARC-012' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'src/NeNeCommander.App/Themes/DesignTokens.xaml'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace(
            '<Thickness x:Key="SpacingWindowOuter">6</Thickness>',
            "<Color x:Key=`"SurfaceWindowColor`">#FF000000</Color>`r`n    <Thickness x:Key=`"SpacingWindowOuter`">6</Thickness>")
        Set-Content -LiteralPath $path -Value $content -NoNewline
    }

    Assert-ConformanceFailure -Name 'presentation-names-unknown-scheme-resource' -ExpectedRule 'ARC-012' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'src/NeNeCommander.Presentation.WinUI/Panes/PaneFrame.cs'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace('"BorderSubtleBrush"', '"BorderInvisibleBrush"')
        Set-Content -LiteralPath $path -Value $content -NoNewline
    }

    Assert-ConformanceFailure -Name 'full-gate-on-push' -ExpectedRule 'QLT-015' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot '.github/workflows/quality.yml'
        $content = Get-Content -LiteralPath $path -Raw
        Set-Content -LiteralPath $path -Value $content.Replace('  pull_request:', "  push:`n  pull_request:") -NoNewline
    }

    Assert-ConformanceFailure -Name 'full-gate-on-commit' -ExpectedRule 'QLT-015' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot '.githooks/pre-commit'
        $content = Get-Content -LiteralPath $path -Raw
        Set-Content -LiteralPath $path -Value $content.Replace(' -Mode Commit', '') -NoNewline
    }

    Assert-ConformanceFailure -Name 'skipped-full-gate-job' -ExpectedRule 'QLT-015' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot '.github/workflows/quality.yml'
        $content = Get-Content -LiteralPath $path -Raw
        Set-Content -LiteralPath $path -Value $content.Replace('  canonical-gate:', "  canonical-gate:`n    if: false") -NoNewline
    }

    Assert-ConformanceFailure -Name 'lightweight-default-gate' -ExpectedRule 'QLT-015' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'eng/check.ps1'
        $content = Get-Content -LiteralPath $path -Raw
        Set-Content -LiteralPath $path -Value $content.Replace('[string] $Mode = ''Merge''', '[string] $Mode = ''Commit''') -NoNewline
    }

    $invalidMessage = Join-Path $proofRoot 'invalid-commit-message.txt'
    Set-Content -LiteralPath $invalidMessage -Value 'Implement filesystem safety'
    & pwsh -NoProfile -File (Join-Path $root 'eng/validate-commit-message.ps1') -MessageFile $invalidMessage *> $null
    if ($LASTEXITCODE -eq 0) {
        throw 'Invalid commit message unexpectedly passed.'
    }

    $validMessage = Join-Path $proofRoot 'valid-commit-message.txt'
    Set-Content -LiteralPath $validMessage -Value 'feat(core): ファイル操作の安全境界を実装する (#1)'
    & pwsh -NoProfile -File (Join-Path $root 'eng/validate-commit-message.ps1') -MessageFile $validMessage *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'Valid commit message unexpectedly failed.'
    }

    Write-Host 'Gate proofs passed: required files, rule uniqueness, protected build and restore settings, suppressions, production interlock, platform API boundary, environment boundary, color scheme dictionary parity, presentation resource keys, and commit messages.'
}
finally {
    if (Test-Path -LiteralPath $proofRoot) {
        $resolvedProofRoot = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $proofRoot).Path)
        if (-not $resolvedProofRoot.StartsWith($tempParent, [System.StringComparison]::OrdinalIgnoreCase) -or
            -not ([System.IO.Path]::GetFileName($resolvedProofRoot).StartsWith('NeNeCommander-GateProof-', [System.StringComparison]::Ordinal))) {
            throw 'Refusing to clean an unverified proof root.'
        }

        Remove-Item -LiteralPath $resolvedProofRoot -Recurse -Force
    }
}
