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

function Assert-ConformanceSuccess {
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [scriptblock] $Validate
    )

    $caseRoot = Join-Path $proofRoot $Name
    Copy-ProofFoundation -RepositoryRoot $root -Destination $caseRoot
    & $Validate $caseRoot

    $output = (& pwsh -NoProfile -File (Join-Path $caseRoot 'eng/conformance.ps1') -RepositoryRoot $caseRoot -Quiet 2>&1) -join "`n"
    if ($LASTEXITCODE -ne 0) {
        throw "Positive proof '$Name' unexpectedly failed. Output: $output"
    }
}

function Assert-SourceConformanceFailure {
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string] $Source
    )

    $caseRoot = Join-Path $proofRoot 'cs010-source-cases'
    if (-not (Test-Path -LiteralPath $caseRoot -PathType Container)) {
        Copy-ProofFoundation -RepositoryRoot $root -Destination $caseRoot
    }

    $testRoot = Join-Path $caseRoot 'tests/PolicyProof'
    $sourcePath = Join-Path $testRoot 'Violation.cs'
    New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
    Set-Content -LiteralPath $sourcePath -Value $Source
    try {
        $output = (& pwsh -NoProfile -File (Join-Path $caseRoot 'eng/conformance.ps1') -RepositoryRoot $caseRoot -Quiet 2>&1) -join "`n"
        if ($LASTEXITCODE -eq 0) {
            throw "Negative proof '$Name' unexpectedly passed."
        }
        if ($output -notmatch '\[CS-010\]') {
            throw "Negative proof '$Name' failed for the wrong reason. Expected CS-010. Output: $output"
        }
    }
    finally {
        Remove-Item -LiteralPath $sourcePath -Force
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

    Assert-ConformanceFailure -Name 'unsafe-enabled-outside-interop-project' -ExpectedRule 'SEC-014' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'src/NeNeCommander.Application/NeNeCommander.Application.csproj'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace('</PropertyGroup>', "    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>`r`n  </PropertyGroup>")
        Set-Content -LiteralPath $path -Value $content -NoNewline
    }

    Assert-ConformanceFailure -Name 'handwritten-unsafe-interop' -ExpectedRule 'SEC-014' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'src/NeNeCommander.Infrastructure.Windows/UnsafeViolation.cs'
        Set-Content -LiteralPath $path -Value "internal unsafe sealed class UnsafeViolation { }`r`n"
    }

    Assert-ConformanceFailure -Name 'environment-outside-settings-location' -ExpectedRule 'CS-010' -Mutate {
        param($caseRoot)
        $testRoot = Join-Path $caseRoot 'tests/PolicyProof'
        New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $testRoot 'EnvironmentViolation.cs') -Value "internal sealed class EnvironmentViolation { private static string Read() { return Environment.CurrentDirectory; } }`r`n"
    }

    Assert-ConformanceSuccess -Name 'environment-location-adapter' -Validate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'src/NeNeCommander.Infrastructure.Windows/Settings/WindowsLocalSettingsLocation.cs'
        $content = Get-Content -LiteralPath $path -Raw
        if ($content -notmatch 'Environment\.GetFolderPath' -or
            $content -notmatch 'Environment\.SpecialFolder\.LocalApplicationData') {
            throw 'The positive environment-location adapter no longer exercises its approved API concern.'
        }
    }

    Assert-SourceConformanceFailure -Name 'ambient-alias-after-header' -Source "// Header`r`nusing DT = System.DateTime;`r`ninternal sealed class Violation { }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-alias-after-directive' -Source "#nullable enable`r`nusing DTO = System.DateTimeOffset;`r`ninternal sealed class Violation { }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-global-alias' -Source "global using TP = global::System.TimeProvider;`r`ninternal sealed class Violation { }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-escaped-alias' -Source "using @DT = global :: @System . @DateTime;`r`ninternal sealed class Violation { }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-inline-namespace-alias' -Source "namespace PolicyProof { using DTO = System.DateTimeOffset; internal sealed class Violation { } }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-system-namespace-alias' -Source "using S = System;`r`ninternal sealed class Violation { }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-diagnostics-namespace-alias' -Source "using D = System.Diagnostics;`r`ninternal sealed class Violation { }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-environment-type-alias' -Source "using E = System.Environment;`r`ninternal sealed class Violation { }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-stopwatch-type-alias' -Source "using SW = System.Diagnostics.Stopwatch;`r`ninternal sealed class Violation { }`r`n"

    Assert-SourceConformanceFailure -Name 'ambient-datetime-static-import' -Source "using static System.DateTime;`r`ninternal sealed class Violation { }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-datetime-offset-static-import' -Source "using static global::System.DateTimeOffset;`r`ninternal sealed class Violation { }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-time-provider-static-import' -Source "using static @System.@TimeProvider;`r`ninternal sealed class Violation { }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-stopwatch-static-import' -Source "using static System.Diagnostics.Stopwatch;`r`ninternal sealed class Violation { }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-environment-static-import' -Source "using static System.Environment;`r`ninternal sealed class Violation { }`r`n"

    Assert-SourceConformanceFailure -Name 'time-provider-system-access' -Source "internal sealed class Violation { private static object Read() { return TimeProvider . System; } }`r`n"
    Assert-SourceConformanceFailure -Name 'escaped-time-provider-system-access' -Source "internal sealed class Violation { private static object Read() { return @TimeProvider . @System; } }`r`n"

    Assert-SourceConformanceFailure -Name 'ambient-stopwatch-static-access' -Source "internal sealed class Violation { private static long Read() { return Stopwatch . GetTimestamp(); } }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-stopwatch-qualified-construction' -Source "internal sealed class Violation { private static object Read() { return new System.Diagnostics.Stopwatch(); } }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-stopwatch-global-construction' -Source "internal sealed class Violation { private static object Read() { return new global::System.Diagnostics.Stopwatch(); } }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-stopwatch-target-typed-construction' -Source "using System.Diagnostics;`r`ninternal sealed class Violation { private static Stopwatch Read() { Stopwatch timer = new(); return timer; } }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-stopwatch-initializer-construction' -Source "using System.Diagnostics;`r`ninternal sealed class Violation { private static object Read() { return new Stopwatch { }; } }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-stopwatch-escaped-access' -Source "using System.Diagnostics;`r`ninternal sealed class Violation { private static object Read() { return @Stopwatch . @StartNew(); } }`r`n"

    Assert-SourceConformanceFailure -Name 'interpolated-direct-wall-clock' -Source 'internal sealed class Violation { private static string Read() { return $"{DateTime . UtcNow:o}"; } }'
    Assert-SourceConformanceFailure -Name 'escaped-direct-wall-clock' -Source "internal sealed class Violation { private static object Read() { return @DateTime . @Now; } }`r`n"
    Assert-SourceConformanceFailure -Name 'ambient-environment-clock' -Source "internal sealed class Violation { private static long Read() { return Environment . TickCount64; } }`r`n"
    Assert-SourceConformanceFailure -Name 'escaped-environment-clock' -Source "internal sealed class Violation { private static long Read() { return @Environment . @TickCount64; } }`r`n"

    Assert-ConformanceFailure -Name 'environment-clock-inside-location-adapter' -ExpectedRule 'CS-010' -Mutate {
        param($caseRoot)
        $path = Join-Path $caseRoot 'src/NeNeCommander.Infrastructure.Windows/Settings/WindowsLocalSettingsLocation.cs'
        $content = Get-Content -LiteralPath $path -Raw
        $content = $content.Replace(
            'string localApplicationData = Environment.GetFolderPath(',
            "long tickCount = Environment.TickCount64;`r`n        string localApplicationData = Environment.GetFolderPath(")
        Set-Content -LiteralPath $path -Value $content -NoNewline
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

    Write-Host 'Gate proofs passed: required files, rule uniqueness, protected build and restore settings, suppressions, production interlock, platform API boundary, environment and ambient clock boundaries, color scheme dictionary parity, presentation resource keys, and commit messages.'
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
