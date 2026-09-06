[CmdletBinding()]
param(
    [Parameter()]
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),

    [Parameter()]
    [switch] $Quiet
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath($RepositoryRoot)
$violations = [System.Collections.Generic.List[string]]::new()
. (Join-Path $PSScriptRoot 'repository-tree.ps1')

function Add-Violation {
    param(
        [Parameter(Mandatory)]
        [string] $Rule,

        [Parameter(Mandatory)]
        [string] $Message
    )

    $violations.Add("[$Rule] $Message")
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    return [System.IO.Path]::GetRelativePath($root, $Path).Replace('\', '/')
}

function Get-RepositoryFiles {
    param(
        [Parameter(Mandatory)]
        [string[]] $Extensions,

        [Parameter(Mandatory)]
        [string[]] $Roots
    )

    $found = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    foreach ($relativeRoot in $Roots) {
        $searchRoot = Join-Path $root $relativeRoot
        if (-not (Test-Path -LiteralPath $searchRoot -PathType Container)) {
            continue
        }

        $files = Get-RepositoryTreeFile -RepositoryRoot $root -Roots @($relativeRoot) | Where-Object {
            $Extensions -contains $_.Extension.ToLowerInvariant()
        }
        foreach ($file in $files) {
            $found.Add($file)
        }
    }

    return $found
}

function Assert-TextContains {
    param(
        [Parameter(Mandatory)]
        [string] $Rule,

        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $Pattern,

        [Parameter(Mandatory)]
        [string] $Description
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch $Pattern) {
        Add-Violation -Rule $Rule -Message "$(Get-RelativePath -Path $Path): $Description"
    }
}

$requiredFiles = @(
    'AGENT.md',
    'AGENTS.md',
    'CLAUDE.md',
    'README.md',
    '.editorconfig',
    '.gitattributes',
    '.gitignore',
    'Directory.Build.props',
    'Directory.Packages.props',
    'NuGet.Config',
    'global.json',
    'docs/PROJECT_CHARTER.md',
    'docs/PROJECT_STATE.md',
    'docs/ARCHITECTURE_CONSTITUTION.md',
    'docs/COMMAND_MODEL.md',
    'docs/PROJECT_LAYOUT.md',
    'docs/CODING_RULES.md',
    'docs/QUALITY_GATES.md',
    'docs/TEST_STRATEGY.md',
    'docs/SECURITY_MODEL.md',
    'docs/DEVELOPMENT_WORKFLOW.md',
    'docs/COMMIT_CONVENTIONS.md',
    'docs/KEYBOARD_MODEL.md',
    'docs/FILESYSTEM_BOUNDARIES.md',
    'docs/DESIGN_HANDOFF.md',
    'docs/GLOSSARY.md',
    'docs/adr/README.md',
    'docs/waivers/README.md',
    'docs/quality/GATE_PROOFS.md',
    'eng/architecture.json',
    'eng/adversarial-cases.json',
    'eng/bootstrap.ps1',
    'eng/repository-tree.ps1',
    'eng/validate-commit-message.ps1',
    'eng/check.ps1',
    'eng/verify-coverage.ps1',
    'eng/coverage.settings',
    'eng/prove-gates.ps1',
    'eng/security-policy.json',
    'eng/security-check.ps1',
    'eng/prove-security.ps1',
    'eng/deep-review.ps1',
    '.config/dotnet-tools.json',
    'stryker-config.json',
    '.github/dependabot.yml',
    '.github/workflows/dependency-review.yml',
    '.github/workflows/security-deep-review.yml',
    '.github/workflows/quality.yml',
    '.github/copilot-instructions.md',
    '.githooks/pre-commit',
    '.githooks/commit-msg'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        Add-Violation -Rule 'DOC-001' -Message "Required file is missing: $relativePath"
    }
}

if ($violations.Count -eq 0) {
    foreach ($compatibilityPath in @('AGENT.md', 'CLAUDE.md')) {
        $compatibilityContent = Get-Content -LiteralPath (Join-Path $root $compatibilityPath) -Raw
        if ($compatibilityContent -notmatch '\[AGENTS\.md\]\(AGENTS\.md\)') {
            Add-Violation -Rule 'DOC-002' -Message "$compatibilityPath must point to the sole AGENTS.md constitution."
        }
    }

    $copilotCompatibility = Get-Content -LiteralPath (Join-Path $root '.github/copilot-instructions.md') -Raw
    if ($copilotCompatibility -notmatch '\[\.\./AGENTS\.md\]\(\.\./AGENTS\.md\)') {
        Add-Violation -Rule 'DOC-002' -Message '.github/copilot-instructions.md must point to the sole AGENTS.md constitution.'
    }

    $agentConstitution = Join-Path $root 'AGENTS.md'
    foreach ($requiredLink in @(
        'docs/PROJECT_STATE.md',
        'docs/ARCHITECTURE_CONSTITUTION.md',
        'docs/CODING_RULES.md',
        'docs/QUALITY_GATES.md',
        'eng/check.ps1'
    )) {
        Assert-TextContains -Rule 'DOC-003' -Path $agentConstitution -Pattern ([regex]::Escape($requiredLink)) -Description "missing mandatory link to $requiredLink"
    }
}

$canonicalGatePath = Join-Path $root 'eng/check.ps1'
if (Test-Path -LiteralPath $canonicalGatePath -PathType Leaf) {
    $canonicalGate = Get-Content -LiteralPath $canonicalGatePath -Raw
    $releaseRestorePattern = '(?m)^\s*& dotnet restore \$solution -p:Configuration=Release --locked-mode\s*$'
    if ([regex]::Matches($canonicalGate, $releaseRestorePattern).Count -ne 1) {
        Add-Violation -Rule 'QLT-014' -Message 'The canonical gate must perform exactly one locked restore evaluated with Configuration=Release.'
    }
}

$coverageSettingsPath = Join-Path $root 'eng/coverage.settings'
$qualityWorkflowPath = Join-Path $root '.github/workflows/quality.yml'
if (Test-Path -LiteralPath $qualityWorkflowPath -PathType Leaf) {
    $expectedQualityWorkflow = @'
name: quality

on:
  pull_request:
    types: [ready_for_review]

concurrency:
  group: quality-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read

jobs:
  canonical-gate:
    runs-on: windows-latest
    timeout-minutes: 30
    steps:
      - name: Check out repository
        uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
        with:
          persist-credentials: false

      - name: Install pinned .NET SDK
        uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
        with:
          dotnet-version: 10.0.400

      - name: Run canonical gate
        shell: pwsh
        run: pwsh -NoProfile -File ./eng/check.ps1
'@
    $actualQualityWorkflow = Get-Content -LiteralPath $qualityWorkflowPath -Raw
    if ($actualQualityWorkflow.Replace("`r`n", "`n").Trim() -cne $expectedQualityWorkflow.Replace("`r`n", "`n").Trim()) {
        Add-Violation -Rule 'QLT-015' -Message 'quality.yml must run the full required gate only on readiness, without conditional success or skip paths.'
    }
}
$commitHookPath = Join-Path $root '.githooks/pre-commit'
if (Test-Path -LiteralPath $commitHookPath -PathType Leaf) {
    $commitHook = Get-Content -LiteralPath $commitHookPath -Raw
    if ($commitHook.Replace("`r`n", "`n").Trim() -cne "#!/usr/bin/env sh`nexec pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit") {
        Add-Violation -Rule 'QLT-015' -Message 'The commit hook must invoke only canonical Commit mode.'
    }
}
Assert-TextContains -Rule 'QLT-015' -Path $canonicalGatePath -Pattern "\[ValidateSet\('Commit', 'Merge'\)\]" -Description 'missing closed validation modes'
Assert-TextContains -Rule 'QLT-015' -Path $canonicalGatePath -Pattern '\[string\] \$Mode = ''Merge''' -Description 'the default must remain the full Merge gate'
Assert-TextContains -Rule 'QLT-015' -Path (Join-Path $root 'eng/bootstrap.ps1') -Pattern "'check.ps1'\) -Mode Commit" -Description 'bootstrap must use lightweight Commit checks'

if (Test-Path -LiteralPath $coverageSettingsPath -PathType Leaf) {
    [xml] $coverageSettings = Get-Content -LiteralPath $coverageSettingsPath -Raw
    $excludedAttributes = @($coverageSettings.SelectNodes('/Configuration/CodeCoverage/Attributes/Exclude/Attribute'))
    $excludedSources = @($coverageSettings.SelectNodes('/Configuration/CodeCoverage/Sources/Exclude/Source'))
    if ($excludedAttributes.Count -ne 1 -or
        $excludedAttributes[0].InnerText -cne '^System\.CodeDom\.Compiler\.GeneratedCodeAttribute$') {
        Add-Violation -Rule 'QLT-008' -Message 'Coverage may exclude only GeneratedCodeAttribute members.'
    }
    $expectedExcludedSources = @(
        '.*[\\/]obj[\\/].*',
        '.*[\\/]Views[\\/]CommanderWindow\.xaml\.cs$'
    )
    if ($excludedSources.Count -ne $expectedExcludedSources.Count) {
        Add-Violation -Rule 'QLT-008' -Message 'Coverage source exclusions do not match ADR-0008.'
    }
    else {
        for ($index = 0; $index -lt $expectedExcludedSources.Count; $index++) {
            if ($excludedSources[$index].InnerText -cne $expectedExcludedSources[$index]) {
                Add-Violation -Rule 'QLT-008' -Message 'Coverage source exclusions do not match ADR-0008.'
                break
            }
        }
    }
}

$normativeDocuments = @(
    'AGENTS.md',
    'docs/PROJECT_CHARTER.md',
    'docs/PROJECT_STATE.md',
    'docs/ARCHITECTURE_CONSTITUTION.md',
    'docs/COMMAND_MODEL.md',
    'docs/PROJECT_LAYOUT.md',
    'docs/CODING_RULES.md',
    'docs/QUALITY_GATES.md',
    'docs/TEST_STRATEGY.md',
    'docs/SECURITY_MODEL.md',
    'docs/DEVELOPMENT_WORKFLOW.md',
    'docs/COMMIT_CONVENTIONS.md',
    'docs/KEYBOARD_MODEL.md',
    'docs/FILESYSTEM_BOUNDARIES.md',
    'docs/DESIGN_HANDOFF.md',
    'docs/GLOSSARY.md',
    'docs/adr/README.md',
    'docs/waivers/README.md',
    'docs/quality/GATE_PROOFS.md'
)

$ruleDeclarations = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)
foreach ($relativePath in $normativeDocuments) {
    $fullPath = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        continue
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    if ($content -notmatch '(?m)^Status: normative\s*$') {
        Add-Violation -Rule 'DOC-004' -Message "$relativePath must declare exactly 'Status: normative'."
    }

    $matches = [regex]::Matches($content, '(?m)^### (?<id>(?:ARC|CMD|CS|KBD|FS|QLT|TST|SEC|GIT)-\d{3}) —[^\r\n]+')
    foreach ($match in $matches) {
        $ruleId = $match.Groups['id'].Value
        if ($ruleDeclarations.ContainsKey($ruleId)) {
            Add-Violation -Rule 'RULE-001' -Message "Duplicate declaration $ruleId in $relativePath and $($ruleDeclarations[$ruleId])."
        }
        else {
            $ruleDeclarations.Add($ruleId, $relativePath)
        }

        $sectionStart = $match.Index + $match.Length
        $nextHeading = $content.IndexOf("`n### ", $sectionStart, [System.StringComparison]::Ordinal)
        if ($nextHeading -lt 0) {
            $nextHeading = $content.Length
        }

        $section = $content.Substring($sectionStart, $nextHeading - $sectionStart)
        if ($section -notmatch '(?m)^- Status: \*\*(active|planned|impossible|rejected)\*\*\s*$') {
            Add-Violation -Rule 'RULE-002' -Message "$ruleId in $relativePath lacks one recognized status."
        }
    }
}

if ($ruleDeclarations.Count -lt 40) {
    Add-Violation -Rule 'RULE-003' -Message "Expected at least 40 declared rules; found $($ruleDeclarations.Count)."
}

$globalJsonPath = Join-Path $root 'global.json'
if (Test-Path -LiteralPath $globalJsonPath -PathType Leaf) {
    $globalSettings = Get-Content -LiteralPath $globalJsonPath -Raw | ConvertFrom-Json
    if ($globalSettings.sdk.version -ne '10.0.400' -or $globalSettings.sdk.rollForward -ne 'disable' -or $globalSettings.sdk.allowPrerelease -ne $false) {
        Add-Violation -Rule 'CFG-001' -Message 'global.json must pin stable SDK 10.0.400 with rollForward disabled.'
    }

    if ($globalSettings.'msbuild-sdks'.'MSTest.Sdk' -ne '4.4.0') {
        Add-Violation -Rule 'CFG-002' -Message 'global.json must pin MSTest.Sdk 4.4.0.'
    }
}

$buildPropsPath = Join-Path $root 'Directory.Build.props'
if (Test-Path -LiteralPath $buildPropsPath -PathType Leaf) {
    $requiredBuildSettings = @{
        'LangVersion' = '14.0'
        'Nullable' = 'enable'
        'ImplicitUsings' = 'disable'
        'TreatWarningsAsErrors' = 'true'
        'CodeAnalysisTreatWarningsAsErrors' = 'true'
        'EnforceCodeStyleInBuild' = 'true'
        'EnableNETAnalyzers' = 'true'
        'AnalysisLevel' = '10.0-recommended'
        'Deterministic' = 'true'
        'GenerateDocumentationFile' = 'true'
        'RestorePackagesWithLockFile' = 'true'
        'NuGetAudit' = 'true'
        'NuGetAuditMode' = 'all'
        'NuGetAuditLevel' = 'low'
    }

    [xml] $buildProps = Get-Content -LiteralPath $buildPropsPath -Raw
    foreach ($setting in $requiredBuildSettings.GetEnumerator()) {
        $nodes = $buildProps.SelectNodes("//PropertyGroup/$($setting.Key)")
        if ($nodes.Count -ne 1 -or $nodes[0].InnerText -cne $setting.Value) {
            Add-Violation -Rule 'CFG-003' -Message "Directory.Build.props must set $($setting.Key) exactly once to '$($setting.Value)'."
        }
    }
}

$editorConfigPath = Join-Path $root '.editorconfig'
if (Test-Path -LiteralPath $editorConfigPath -PathType Leaf) {
    $editorConfig = Get-Content -LiteralPath $editorConfigPath -Raw
    foreach ($requiredEditorSetting in @(
        'root = true',
        'csharp_style_var_elsewhere = false:error',
        'csharp_style_namespace_declarations = file_scoped:error',
        'dotnet_diagnostic.CS1591.severity = error',
        'dotnet_diagnostic.IDE0055.severity = error'
    )) {
        if ($editorConfig -notmatch "(?m)^$([regex]::Escape($requiredEditorSetting))\s*$") {
            Add-Violation -Rule 'CFG-004' -Message ".editorconfig is missing protected setting: $requiredEditorSetting"
        }
    }
}

$configurationFiles = Get-RepositoryFiles -Extensions @('.cs', '.csproj', '.props', '.targets') -Roots @('.')
$configurationPatterns = [ordered]@{
    'CS-020:#pragma warning disable' = '(?im)#pragma\s+warning\s+disable'
    'CS-020:SuppressMessage' = '(?i)SuppressMessage'
    'CS-020:NoWarn' = '(?i)<NoWarn(?:\s|>)'
    'CS-020:WarningsNotAsErrors' = '(?i)<WarningsNotAsErrors(?:\s|>)'
    'CS-020:warnings-as-errors disabled' = '(?i)<(?:TreatWarningsAsErrors|CodeAnalysisTreatWarningsAsErrors)>\s*false\s*</'
    'CS-005:nullable disabled' = '(?im)#nullable\s+disable|<Nullable>\s*disable\s*</Nullable>'
}

foreach ($file in $configurationFiles) {
    $relativePath = Get-RelativePath -Path $file.FullName
    if ($relativePath -match '^eng/fixtures/') {
        continue
    }

    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($entry in $configurationPatterns.GetEnumerator()) {
        $parts = $entry.Key.Split(':', 2)
        if ($content -match $entry.Value) {
            Add-Violation -Rule $parts[0] -Message "$relativePath contains prohibited $($parts[1])."
        }
    }
}

$unsafeInteropProject = 'src/NeNeCommander.Infrastructure.Windows/NeNeCommander.Infrastructure.Windows.csproj'
foreach ($projectFile in ($configurationFiles | Where-Object { $_.Extension -ceq '.csproj' })) {
    $relativePath = Get-RelativePath -Path $projectFile.FullName
    $content = Get-Content -LiteralPath $projectFile.FullName -Raw
    $settings = [regex]::Matches($content, '<AllowUnsafeBlocks>\s*(?<value>[^<]+)\s*</AllowUnsafeBlocks>')
    if ($relativePath -ceq $unsafeInteropProject) {
        if ($settings.Count -ne 1 -or $settings[0].Groups['value'].Value.Trim() -cne 'true') {
            Add-Violation -Rule 'SEC-014' -Message "$relativePath must enable generated interop support exactly once."
        }
    }
    elseif ($settings.Count -ne 0) {
        Add-Violation -Rule 'SEC-014' -Message "$relativePath may not enable unsafe blocks."
    }
}

foreach ($sourceFile in (Get-RepositoryFiles -Extensions @('.cs') -Roots @('src', 'tests'))) {
    $relativePath = Get-RelativePath -Path $sourceFile.FullName
    $content = Get-Content -LiteralPath $sourceFile.FullName -Raw
    if ($content -match '(?m)^\s*(?:(?:public|private|protected|internal|static|sealed|partial|readonly|ref|abstract|virtual|override|extern|new)\s+)*unsafe\s+') {
        Add-Violation -Rule 'SEC-014' -Message "$relativePath contains handwritten unsafe code."
    }
}

$statePath = Join-Path $root 'docs/PROJECT_STATE.md'
$stage = $null
$productionPermission = $null
if (Test-Path -LiteralPath $statePath -PathType Leaf) {
    $stateContent = Get-Content -LiteralPath $statePath -Raw
    $stageMatch = [regex]::Match($stateContent, '(?m)^- Stage: `(?<value>[^`]+)`\s*$')
    $permissionMatch = [regex]::Match($stateContent, '(?m)^- Production code: `(?<value>permitted|prohibited)`\s*$')
    if (-not $stageMatch.Success -or -not $permissionMatch.Success) {
        Add-Violation -Rule 'STATE-001' -Message 'PROJECT_STATE.md must declare exact stage and production-code markers.'
    }
    else {
        $stage = $stageMatch.Groups['value'].Value
        $productionPermission = $permissionMatch.Groups['value'].Value
    }
}

$implementationFiles = Get-RepositoryFiles -Extensions @('.cs', '.xaml', '.csproj') -Roots @('src', 'tests')
if ($stage -eq 'policy-foundation') {
    if ($productionPermission -ne 'prohibited') {
        Add-Violation -Rule 'STATE-002' -Message 'policy-foundation must prohibit production code.'
    }

    foreach ($file in $implementationFiles) {
        Add-Violation -Rule 'STATE-003' -Message "Implementation or test file is prohibited during policy-foundation: $(Get-RelativePath -Path $file.FullName)"
    }
}
elseif ($stage -eq 'implementation') {
    if ($productionPermission -ne 'permitted') {
        Add-Violation -Rule 'STATE-004' -Message 'implementation stage must explicitly permit production code.'
    }
}
elseif ($null -ne $stage) {
    Add-Violation -Rule 'STATE-005' -Message "Unknown project stage: $stage"
}

$manifestPath = Join-Path $root 'eng/architecture.json'
$manifest = $null
if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
    try {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    }
    catch {
        Add-Violation -Rule 'ARC-002' -Message "eng/architecture.json is invalid JSON: $($_.Exception.Message)"
    }
}

if ($null -ne $manifest) {
    $projectNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $projectPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($project in @($manifest.projects)) {
        if (-not $projectNames.Add([string] $project.name)) {
            Add-Violation -Rule 'ARC-002' -Message "Duplicate project name in architecture manifest: $($project.name)"
        }

        $normalizedPath = ([string] $project.path).Replace('\', '/')
        if (-not $projectPaths.Add($normalizedPath)) {
            Add-Violation -Rule 'ARC-002' -Message "Duplicate project path in architecture manifest: $normalizedPath"
        }
    }

    foreach ($project in @($manifest.projects)) {
        foreach ($reference in @($project.projectReferences)) {
            if (-not $projectNames.Contains([string] $reference)) {
                Add-Violation -Rule 'ARC-002' -Message "$($project.name) references undeclared project $reference."
            }
        }
    }

    $incomingCounts = @{}
    $outgoingReferences = @{}
    foreach ($project in @($manifest.projects)) {
        $projectName = [string] $project.name
        $incomingCounts[$projectName] = 0
        $outgoingReferences[$projectName] = @($project.projectReferences | ForEach-Object { [string] $_ })
    }

    foreach ($references in $outgoingReferences.Values) {
        foreach ($reference in $references) {
            if ($incomingCounts.ContainsKey($reference)) {
                $incomingCounts[$reference] = [int] $incomingCounts[$reference] + 1
            }
        }
    }

    $ready = [System.Collections.Generic.Queue[string]]::new()
    foreach ($entry in $incomingCounts.GetEnumerator()) {
        if ([int] $entry.Value -eq 0) {
            $ready.Enqueue([string] $entry.Key)
        }
    }

    $visitedProjectCount = 0
    while ($ready.Count -gt 0) {
        $projectName = $ready.Dequeue()
        $visitedProjectCount++
        foreach ($reference in @($outgoingReferences[$projectName])) {
            $incomingCounts[$reference] = [int] $incomingCounts[$reference] - 1
            if ([int] $incomingCounts[$reference] -eq 0) {
                $ready.Enqueue($reference)
            }
        }
    }

    if ($visitedProjectCount -ne $projectNames.Count) {
        Add-Violation -Rule 'ARC-002' -Message 'The architecture manifest contains a project-reference cycle.'
    }

    if ($stage -eq 'implementation') {
        $solutionPath = Join-Path $root ([string] $manifest.solution)
        if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
            Add-Violation -Rule 'ARC-002' -Message "Declared solution is missing: $($manifest.solution)"
        }

        $actualProjects = Get-RepositoryTreeFile -RepositoryRoot $root | Where-Object {
            $_.Extension -ceq '.csproj'
        }
        $actualPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($actualProject in $actualProjects) {
            [void] $actualPaths.Add((Get-RelativePath -Path $actualProject.FullName))
        }

        foreach ($declaredPath in $projectPaths) {
            if (-not $actualPaths.Contains($declaredPath)) {
                Add-Violation -Rule 'ARC-002' -Message "Declared project is missing: $declaredPath"
            }
        }

        foreach ($actualPath in $actualPaths) {
            if (-not $projectPaths.Contains($actualPath)) {
                Add-Violation -Rule 'ARC-002' -Message "Project is not declared in architecture manifest: $actualPath"
            }
        }

        $pathByName = @{}
        foreach ($project in @($manifest.projects)) {
            $pathByName[[string] $project.name] = ([string] $project.path).Replace('\', '/')
        }

        [xml] $centralPackages = Get-Content -LiteralPath (Join-Path $root 'Directory.Packages.props') -Raw
        $centralPackageNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        foreach ($itemGroup in @($centralPackages.Project.SelectNodes('ItemGroup'))) {
            foreach ($packageVersion in @($itemGroup.SelectNodes('PackageVersion'))) {
                if ($null -ne $packageVersion) {
                    [void] $centralPackageNames.Add([string] $packageVersion.Include)
                }
            }
        }

        foreach ($project in @($manifest.projects)) {
            $projectPath = Join-Path $root ([string] $project.path)
            if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
                continue
            }

            [xml] $projectXml = Get-Content -LiteralPath $projectPath -Raw
            $declaredReferences = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            foreach ($referenceName in @($project.projectReferences)) {
                [void] $declaredReferences.Add([string] $referenceName)
            }

            $actualReferenceNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            foreach ($itemGroup in @($projectXml.Project.SelectNodes('ItemGroup'))) {
                foreach ($referenceNode in @($itemGroup.SelectNodes('ProjectReference'))) {
                    if ($null -eq $referenceNode) {
                        continue
                    }

                    $referenceFullPath = [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $projectPath) ([string] $referenceNode.Include)))
                    $referenceRelativePath = [System.IO.Path]::GetRelativePath($root, $referenceFullPath).Replace('\', '/')
                    $matchedName = $null
                    foreach ($candidate in $pathByName.GetEnumerator()) {
                        if ($candidate.Value -ieq $referenceRelativePath) {
                            $matchedName = $candidate.Key
                            break
                        }
                    }

                    if ($null -eq $matchedName) {
                        Add-Violation -Rule 'ARC-002' -Message "$($project.name) has a reference to undeclared path $referenceRelativePath."
                    }
                    else {
                        [void] $actualReferenceNames.Add([string] $matchedName)
                    }
                }
            }

            foreach ($referenceName in $declaredReferences) {
                if (-not $actualReferenceNames.Contains($referenceName)) {
                    Add-Violation -Rule 'ARC-002' -Message "$($project.name) is missing declared reference $referenceName."
                }
            }

            foreach ($referenceName in $actualReferenceNames) {
                if (-not $declaredReferences.Contains($referenceName)) {
                    Add-Violation -Rule 'ARC-002' -Message "$($project.name) has forbidden reference $referenceName."
                }
            }

            $allowedPackages = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            foreach ($packageName in @($project.packageReferences)) {
                [void] $allowedPackages.Add([string] $packageName)
                if (-not $centralPackageNames.Contains([string] $packageName)) {
                    Add-Violation -Rule 'CS-024' -Message "$($project.name) allows $packageName but no central version is declared."
                }
            }

            $actualPackages = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            foreach ($itemGroup in @($projectXml.Project.SelectNodes('ItemGroup'))) {
                foreach ($packageNode in @($itemGroup.SelectNodes('PackageReference'))) {
                    if ($null -eq $packageNode) {
                        continue
                    }

                    $packageName = [string] $packageNode.Include
                    [void] $actualPackages.Add($packageName)
                    if ($packageNode.HasAttribute('Version') -or $packageNode.HasAttribute('VersionOverride')) {
                        Add-Violation -Rule 'CS-024' -Message "$($project.name) sets a project-local version for $packageName."
                    }
                }
            }

            foreach ($packageName in $actualPackages) {
                if (-not $allowedPackages.Contains($packageName)) {
                    Add-Violation -Rule 'CS-024' -Message "$($project.name) uses non-allowlisted package $packageName."
                }
            }
        }
    }
}

$sourceFiles = Get-RepositoryFiles -Extensions @('.cs') -Roots @('src', 'tests')
$sourcePatterns = [ordered]@{
    'CS-003:project-owned value type' = '(?m)^\s*(?:public|internal|private|protected|file)?\s*(?:readonly\s+)?(?:record\s+struct|struct)\s+[A-Za-z_]'
    'CS-004:project-owned enum' = '(?m)^\s*(?:public|internal|private|protected|file)?\s*enum\s+[A-Za-z_]'
    # This remains a text scan by design. Keep direct-expression coverage (including
    # interpolated expressions) and close the known type-import escape routes without
    # introducing a second parser or pretending to resolve symbols.
    'CS-010:direct wall clock' = '(?i)(?<![\w@])@?(?:DateTime|DateTimeOffset)\s*\.\s*@?(?:Now|UtcNow)\b'
    'CS-010:ambient clock alias' = '(?im)(?:^|[;{}])\s*(?:global\s+)?using\s+@?[A-Za-z_]\w*\s*=\s*(?:global\s*::\s*)?@?System(?:\s*\.\s*@?(?:DateTime|DateTimeOffset|TimeProvider|Environment|Diagnostics(?:\s*\.\s*@?Stopwatch)?))?\s*;'
    'CS-010:ambient clock static import' = '(?im)(?:^|[;{}])\s*(?:global\s+)?using\s+static\s+(?:global\s*::\s*)?@?System\s*\.\s*@?(?:DateTime|DateTimeOffset|TimeProvider|Environment|Diagnostics\s*\.\s*@?Stopwatch)\s*;'
    'CS-010:TimeProvider.System' = '(?i)(?<![\w@])@?TimeProvider\s*\.\s*@?System\b'
    'CS-010:ambient Stopwatch type reference' = '(?i)(?<![\w@])@?Stopwatch\b'
    'CS-010:ambient Environment clock' = '(?i)(?<![\w@])@?Environment\s*\.\s*@?(?:TickCount|TickCount64)\b'
    'CS-010:direct environment access' = '(?i)(?<![\w@])@?Environment\s*\.\s*@?[A-Za-z_]\w*\b'
    'CS-010:direct identifier generation' = '\bGuid\.NewGuid\s*\('
    'CS-014:global using' = '(?m)^\s*global\s+using\s+'
    'CS-014:primary constructor' = '(?m)^\s*(?:public|internal|private|protected|file)?\s*(?:sealed\s+|abstract\s+|partial\s+)*(?:class|record(?:\s+class)?)\s+[A-Za-z_]\w*\s*\('
    'CS-016:blocking task result' = '\.(?:Result|Wait)\b'
    'CS-016:blocking awaiter' = '\.GetAwaiter\s*\(\s*\)\s*\.GetResult\s*\('
    'CS-016:Task.Run' = '\bTask\.Run\s*\('
    'CS-016:async void' = '\basync\s+void\s+'
    'CS-017:broad exception catch' = '\bcatch\s*\(\s*(?:System\.)?Exception\b'
    'CS-021:manual property notification' = '\bINotifyPropertyChanged\b'
    'CS-021:manual command implementation' = '\bSystem\.Windows\.Input\.ICommand\b|\bnew\s+(?:Async)?RelayCommand\b'
}

$bannedNamePattern = '(?i)\b(?:Manager|Helper|Helpers|Util|Utils|Utility|Utilities|Common|Misc|Base|General)\b'
foreach ($file in $sourceFiles) {
    $relativePath = Get-RelativePath -Path $file.FullName
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($entry in $sourcePatterns.GetEnumerator()) {
        $parts = $entry.Key.Split(':', 2)
        # Each ambient exception is exact by concern and repository-relative path.
        if ($parts[1] -eq 'ambient Stopwatch type reference' -and $relativePath -ceq 'src/NeNeCommander.Infrastructure.Windows/Time/StopwatchClock.cs') {
            continue
        }
        if ($parts[1] -eq 'direct environment access' -and $relativePath -ceq 'src/NeNeCommander.Infrastructure.Windows/Settings/WindowsLocalSettingsLocation.cs') {
            continue
        }
        if ($content -match $entry.Value) {
            Add-Violation -Rule $parts[0] -Message "$relativePath contains prohibited $($parts[1])."
        }
    }

    $platformApiOwners = '^(?:src/NeNeCommander\.Infrastructure\.Windows|tests/NeNeCommander\.Infrastructure\.Windows\.Tests)/'
    if ($relativePath -notmatch $platformApiOwners -and $content -match '\bSystem\.IO\b') {
        Add-Violation -Rule 'CS-018' -Message "$relativePath accesses System.IO outside Windows infrastructure and its integration tests."
    }

    $pathSegments = $relativePath.Split('/')
    foreach ($segment in $pathSegments) {
        $nameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($segment)
        if ($nameWithoutExtension -match $bannedNamePattern) {
            Add-Violation -Rule 'CS-011' -Message "$relativePath contains prohibited vague name '$nameWithoutExtension'."
        }
    }

    $topLevelTypes = [regex]::Matches($content, '(?m)^(?:public|internal|file)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(?:class|record(?:\s+class)?|interface)\s+(?<name>[A-Za-z_]\w*)')
    if ($topLevelTypes.Count -gt 1) {
        Add-Violation -Rule 'CS-012' -Message "$relativePath declares more than one top-level type."
    }
    $expectedTypeName = if ($file.Name.EndsWith('.xaml.cs', [System.StringComparison]::Ordinal)) {
        $file.Name.Substring(0, $file.Name.Length - '.xaml.cs'.Length)
    }
    else {
        $file.BaseName
    }
    if ($topLevelTypes.Count -eq 1 -and $expectedTypeName -cne $topLevelTypes[0].Groups['name'].Value) {
        Add-Violation -Rule 'CS-012' -Message "$relativePath must match type $($topLevelTypes[0].Groups['name'].Value)."
    }

    if ($content -match '(?s)\([^)]*\bbool\s+[A-Za-z_]\w*[^)]*\)') {
        Add-Violation -Rule 'CS-002' -Message "$relativePath contains a boolean mode parameter."
    }
}

$xamlFiles = Get-RepositoryFiles -Extensions @('.xaml') -Roots @('src')
foreach ($file in $xamlFiles) {
    $relativePath = Get-RelativePath -Path $file.FullName
    if ($relativePath -match '/Themes/') {
        continue
    }

    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -match '#[0-9A-Fa-f]{6}(?:[0-9A-Fa-f]{2})?') {
        Add-Violation -Rule 'CS-023' -Message "$relativePath contains a hard-coded color outside Themes."
    }

    if ($content -match '\b(?:Margin|Padding|CornerRadius|FontSize|MinWidth|MinHeight|RowSpacing|ColumnSpacing)="[0-9]') {
        Add-Violation -Rule 'CS-023' -Message "$relativePath contains a hard-coded semantic layout value."
    }
}

function Get-ResourceKeys {
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $ElementName
    )

    [xml] $document = Get-Content -LiteralPath $Path -Raw
    $keys = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($node in @($document.DocumentElement.ChildNodes)) {
        if ($node.NodeType -eq [System.Xml.XmlNodeType]::Element -and $node.LocalName -ceq $ElementName) {
            [void] $keys.Add($node.GetAttribute('Key', 'http://schemas.microsoft.com/winfx/2006/xaml'))
        }
    }

    return , $keys
}

$designTokensPath = Join-Path $root 'src/NeNeCommander.App/Themes/DesignTokens.xaml'
$schemeRoot = Join-Path $root 'src/NeNeCommander.App/Themes/Schemes'
$colorSchemeSourcePath = Join-Path $root 'src/NeNeCommander.Application/Settings/ColorScheme.cs'
if ((Test-Path -LiteralPath $colorSchemeSourcePath -PathType Leaf) -and
    -not ((Test-Path -LiteralPath $designTokensPath -PathType Leaf) -and
        (Test-Path -LiteralPath $schemeRoot -PathType Container))) {
    Add-Violation -Rule 'ARC-012' -Message 'ColorScheme requires Themes/DesignTokens.xaml and a Themes/Schemes dictionary directory.'
}

if ((Test-Path -LiteralPath $designTokensPath -PathType Leaf) -and
    (Test-Path -LiteralPath $schemeRoot -PathType Container) -and
    (Test-Path -LiteralPath $colorSchemeSourcePath -PathType Leaf)) {
    foreach ($colorElement in @('Color', 'SolidColorBrush')) {
        if ((Get-ResourceKeys -Path $designTokensPath -ElementName $colorElement).Count -ne 0) {
            Add-Violation -Rule 'ARC-012' -Message "Themes/DesignTokens.xaml must define no $colorElement; colors belong to one scheme dictionary."
        }
    }

    $declaredSchemes = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($identifierMatch in [regex]::Matches(
            (Get-Content -LiteralPath $colorSchemeSourcePath -Raw),
            '(?m)^\s*public override string Identifier => "(?<id>[a-z0-9-]+)";\s*$')) {
        if (-not $declaredSchemes.Add($identifierMatch.Groups['id'].Value)) {
            Add-Violation -Rule 'ARC-012' -Message "ColorScheme declares the duplicate identifier $($identifierMatch.Groups['id'].Value)."
        }
    }

    $schemeFiles = @(Get-ChildItem -LiteralPath $schemeRoot -Filter '*.xaml' -File | Sort-Object -Property Name)
    $schemeNames = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($schemeFile in $schemeFiles) {
        [void] $schemeNames.Add($schemeFile.BaseName)
    }

    if (-not $declaredSchemes.SetEquals($schemeNames)) {
        Add-Violation -Rule 'ARC-012' -Message "Themes/Schemes must contain exactly one dictionary per ColorScheme member. Declared: $($declaredSchemes -join ','). Present: $($schemeNames -join ',')."
    }

    $expectedColorKeys = $null
    foreach ($schemeFile in $schemeFiles) {
        $relativeScheme = Get-RelativePath -Path $schemeFile.FullName
        $colorKeys = Get-ResourceKeys -Path $schemeFile.FullName -ElementName 'Color'
        $brushKeys = Get-ResourceKeys -Path $schemeFile.FullName -ElementName 'SolidColorBrush'
        $expectedBrushKeys = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
        foreach ($colorKey in $colorKeys) {
            if (-not $colorKey.EndsWith('Color', [System.StringComparison]::Ordinal)) {
                Add-Violation -Rule 'ARC-012' -Message "$relativeScheme declares the color key $colorKey without the Color suffix."
                continue
            }

            [void] $expectedBrushKeys.Add($colorKey.Substring(0, $colorKey.Length - 'Color'.Length) + 'Brush')
        }

        if (-not $brushKeys.SetEquals($expectedBrushKeys)) {
            Add-Violation -Rule 'ARC-012' -Message "$relativeScheme does not pair every color with exactly one brush of the same name."
        }

        if ($null -eq $expectedColorKeys) {
            $expectedColorKeys = $colorKeys
            if ($colorKeys.Count -eq 0) {
                Add-Violation -Rule 'ARC-012' -Message "$relativeScheme defines no color."
            }
        }
        elseif (-not $colorKeys.SetEquals($expectedColorKeys)) {
            Add-Violation -Rule 'ARC-012' -Message "$relativeScheme does not define the same color keys as the other scheme dictionaries."
        }
    }

    if ($null -ne $expectedColorKeys) {
        $schemeKeys = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
        foreach ($colorKey in $expectedColorKeys) {
            [void] $schemeKeys.Add($colorKey)
            [void] $schemeKeys.Add($colorKey.Substring(0, $colorKey.Length - 'Color'.Length) + 'Brush')
        }

        foreach ($viewFile in (Get-RepositoryFiles -Extensions @('.xaml') -Roots @('src/NeNeCommander.App/Views'))) {
            $viewContent = Get-Content -LiteralPath $viewFile.FullName -Raw
            foreach ($reference in [regex]::Matches($viewContent, '\{StaticResource (?<key>\w+(?:Color|Brush))\}')) {
                $referencedKey = $reference.Groups['key'].Value
                if (-not $schemeKeys.Contains($referencedKey)) {
                    Add-Violation -Rule 'ARC-012' -Message "$(Get-RelativePath -Path $viewFile.FullName) references the undefined scheme resource $referencedKey."
                }
            }
        }

        foreach ($presentationFile in (Get-RepositoryFiles -Extensions @('.cs') -Roots @('src/NeNeCommander.Presentation.WinUI'))) {
            $presentationContent = Get-Content -LiteralPath $presentationFile.FullName -Raw
            foreach ($reference in [regex]::Matches($presentationContent, '"(?<key>\w+(?:Color|Brush))"')) {
                $referencedKey = $reference.Groups['key'].Value
                if (-not $schemeKeys.Contains($referencedKey)) {
                    Add-Violation -Rule 'ARC-012' -Message "$(Get-RelativePath -Path $presentationFile.FullName) names the undefined scheme resource $referencedKey."
                }
            }
        }
    }
}

$activeWaivers = Get-ChildItem -LiteralPath (Join-Path $root 'docs/waivers') -Filter '*.md' -File -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -notin @('README.md', '0000-template.md')
}
foreach ($waiver in $activeWaivers) {
    $content = Get-Content -LiteralPath $waiver.FullName -Raw
    if ($content -notmatch '(?m)^Status: active\s*$') {
        Add-Violation -Rule 'WAIVER-001' -Message "$(Get-RelativePath -Path $waiver.FullName) must be active or removed from the active waiver directory."
        continue
    }

    $createdMatch = [regex]::Match($content, '(?m)^- Created: `(?<date>\d{4}-\d{2}-\d{2})`\s*$')
    $expiresMatch = [regex]::Match($content, '(?m)^- Expires: `(?<date>\d{4}-\d{2}-\d{2})`\s*$')
    if (-not $createdMatch.Success -or -not $expiresMatch.Success) {
        Add-Violation -Rule 'WAIVER-002' -Message "$(Get-RelativePath -Path $waiver.FullName) lacks exact created and expiry dates."
        continue
    }

    $created = [DateTime]::ParseExact($createdMatch.Groups['date'].Value, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
    $expires = [DateTime]::ParseExact($expiresMatch.Groups['date'].Value, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
    if (($expires - $created).TotalDays -gt 30 -or $expires -lt [DateTime]::UtcNow.Date) {
        Add-Violation -Rule 'WAIVER-003' -Message "$(Get-RelativePath -Path $waiver.FullName) exceeds 30 days or is expired."
    }
}

if ($violations.Count -gt 0) {
    foreach ($violation in $violations) {
        Write-Error $violation -ErrorAction Continue
    }

    Write-Error "Conformance failed with $($violations.Count) violation(s)."
    exit 1
}

if (-not $Quiet) {
    Write-Host "Conformance passed: $($ruleDeclarations.Count) unique normative rules; stage '$stage'."
}
