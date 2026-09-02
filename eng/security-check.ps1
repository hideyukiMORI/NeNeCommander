[CmdletBinding()]
param(
    [Parameter()]
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),

    [Parameter()]
    [switch] $SkipProof
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath($RepositoryRoot)
$violations = [System.Collections.Generic.List[string]]::new()

function Add-SecurityViolation {
    param(
        [Parameter(Mandatory)]
        [string] $Rule,

        [Parameter(Mandatory)]
        [string] $Message
    )

    $violations.Add("[$Rule] $Message")
}

function Get-SecurityRelativePath {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    return [System.IO.Path]::GetRelativePath($root, $Path).Replace('\', '/')
}

$requiredSecurityFiles = @(
    'docs/TEST_STRATEGY.md',
    'docs/SECURITY_MODEL.md',
    'eng/security-policy.json',
    'eng/adversarial-cases.json',
    'eng/security-check.ps1',
    'eng/prove-security.ps1',
    'eng/deep-review.ps1',
    '.config/dotnet-tools.json',
    'stryker-config.json',
    '.github/workflows/dependency-review.yml',
    '.github/workflows/security-deep-review.yml',
    '.github/dependabot.yml'
)

foreach ($relativePath in $requiredSecurityFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $relativePath) -PathType Leaf)) {
        Add-SecurityViolation -Rule 'SEC-001' -Message "Required security file is missing: $relativePath"
    }
}

$policy = $null
$cases = $null
if ($violations.Count -eq 0) {
    try {
        $policy = Get-Content -LiteralPath (Join-Path $root 'eng/security-policy.json') -Raw | ConvertFrom-Json
        $cases = Get-Content -LiteralPath (Join-Path $root 'eng/adversarial-cases.json') -Raw | ConvertFrom-Json
    }
    catch {
        Add-SecurityViolation -Rule 'SEC-001' -Message "Security policy JSON is invalid: $($_.Exception.Message)"
    }
}

if ($null -ne $policy) {
    if ($policy.schemaVersion -ne 1 -or
        $policy.deepReviewCron -cne '23 2 */3 * *' -or
        $policy.maximumReviewAgeHours -ne 96 -or
        $policy.nuGetAuditLevel -cne 'low' -or
        $policy.adversarialRepeatCount -ne 3 -or
        $policy.mutationLevel -cne 'Complete') {
        Add-SecurityViolation -Rule 'SEC-009' -Message 'eng/security-policy.json has weakened protected values.'
    }

    $expectedMutationProjects = [ordered]@{
        'src/NeNeCommander.Domain/NeNeCommander.Domain.csproj' = 95
        'src/NeNeCommander.Application/NeNeCommander.Application.csproj' = 95
        'src/NeNeCommander.Infrastructure.Windows/NeNeCommander.Infrastructure.Windows.csproj' = 90
        'src/NeNeCommander.Presentation.WinUI/NeNeCommander.Presentation.WinUI.csproj' = 90
    }
    if (@($policy.mutationProjects).Count -ne $expectedMutationProjects.Count) {
        Add-SecurityViolation -Rule 'TST-008' -Message 'Mutation project policy must contain exactly four governed production projects.'
    }
    foreach ($mutationProject in @($policy.mutationProjects)) {
        $mutationPath = ([string] $mutationProject.path).Replace('\', '/')
        if (-not $expectedMutationProjects.Contains($mutationPath) -or
            [int] $mutationProject.breakAt -ne [int] $expectedMutationProjects[$mutationPath]) {
            Add-SecurityViolation -Rule 'TST-008' -Message "Mutation threshold is missing or weakened for $mutationPath."
        }
    }

    $allowedActionRepositories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($repository in @($policy.allowedActionRepositories)) {
        [void] $allowedActionRepositories.Add([string] $repository)
    }

    $workflowRoot = Join-Path $root '.github/workflows'
    $workflowFiles = Get-ChildItem -LiteralPath $workflowRoot -File | Where-Object { $_.Extension -in @('.yml', '.yaml') }
    foreach ($workflowFile in $workflowFiles) {
        $relativePath = Get-SecurityRelativePath -Path $workflowFile.FullName
        $content = Get-Content -LiteralPath $workflowFile.FullName -Raw

        if ($content -match '(?m)^\s*pull_request_target\s*:') {
            Add-SecurityViolation -Rule 'SEC-004' -Message "$relativePath uses prohibited pull_request_target."
        }

        if ($content -match '(?im)^\s*permissions\s*:\s*write-all\s*$') {
            Add-SecurityViolation -Rule 'SEC-004' -Message "$relativePath grants write-all permissions."
        }

        if ($content -notmatch '(?m)^\s*timeout-minutes\s*:\s*\d+\s*$') {
            Add-SecurityViolation -Rule 'SEC-004' -Message "$relativePath has a job without an explicit timeout."
        }

        if ($content -match '\$\{\{\s*github\.(?:event|head_ref|base_ref)') {
            Add-SecurityViolation -Rule 'SEC-004' -Message "$relativePath interpolates untrusted event data."
        }

        $usesLines = [regex]::Matches($content, '(?m)^\s*uses:\s*(?<target>\S+)(?:\s+#\s*(?<version>\S+))?\s*$')
        foreach ($usesLine in $usesLines) {
            $target = $usesLine.Groups['target'].Value
            if ($target.StartsWith('./', [System.StringComparison]::Ordinal)) {
                continue
            }

            $actionMatch = [regex]::Match($target, '^(?<owner>[^/@]+)/(?<repository>[^/@]+)(?:/[^@]+)?@(?<sha>[0-9a-f]{40})$')
            if (-not $actionMatch.Success) {
                Add-SecurityViolation -Rule 'SEC-006' -Message "$relativePath contains an action that is not pinned to a full commit SHA: $target"
                continue
            }

            $repositoryName = "$($actionMatch.Groups['owner'].Value)/$($actionMatch.Groups['repository'].Value)"
            if (-not $allowedActionRepositories.Contains($repositoryName)) {
                Add-SecurityViolation -Rule 'SEC-006' -Message "$relativePath uses non-allowlisted action repository $repositoryName."
            }

            if (-not $usesLine.Groups['version'].Success -or $usesLine.Groups['version'].Value -notmatch '^v\d+\.\d+\.\d+$') {
                Add-SecurityViolation -Rule 'SEC-006' -Message "$relativePath must annotate $repositoryName with an exact release tag comment."
            }
        }

        if ($content -match 'actions/checkout@' -and $content -notmatch '(?m)^\s*persist-credentials:\s*false\s*$') {
            Add-SecurityViolation -Rule 'SEC-004' -Message "$relativePath must disable persisted checkout credentials."
        }
    }

    $scheduledWorkflowPath = Join-Path $root '.github/workflows/security-deep-review.yml'
    if (Test-Path -LiteralPath $scheduledWorkflowPath -PathType Leaf) {
        $scheduledWorkflow = Get-Content -LiteralPath $scheduledWorkflowPath -Raw
        if ($scheduledWorkflow -notmatch "(?m)^\s*- cron: '$([regex]::Escape([string] $policy.deepReviewCron))'\s*$") {
            Add-SecurityViolation -Rule 'SEC-009' -Message 'Scheduled workflow does not use the protected three-day cron expression.'
        }
    }
}

$buildPropsPath = Join-Path $root 'Directory.Build.props'
if (Test-Path -LiteralPath $buildPropsPath -PathType Leaf) {
    [xml] $buildProps = Get-Content -LiteralPath $buildPropsPath -Raw
    $auditSettings = @{
        'NuGetAudit' = 'true'
        'NuGetAuditMode' = 'all'
        'NuGetAuditLevel' = 'low'
    }
    foreach ($setting in $auditSettings.GetEnumerator()) {
        $nodes = $buildProps.SelectNodes("//PropertyGroup/$($setting.Key)")
        if ($nodes.Count -ne 1 -or $nodes[0].InnerText -cne $setting.Value) {
            Add-SecurityViolation -Rule 'SEC-007' -Message "Directory.Build.props must set $($setting.Key) to '$($setting.Value)'."
        }
    }

    $warningsAsErrors = $buildProps.SelectSingleNode('//PropertyGroup/WarningsAsErrors')
    foreach ($warningCode in @('NU1901', 'NU1902', 'NU1903', 'NU1904')) {
        if ($null -eq $warningsAsErrors -or $warningsAsErrors.InnerText -notmatch "(?:^|;)$warningCode(?:;|$)") {
            Add-SecurityViolation -Rule 'SEC-007' -Message "NuGet advisory $warningCode must be an error."
        }
    }
}

$configurationExtensions = @('.config', '.cs', '.csproj', '.json', '.md', '.props', '.ps1', '.targets', '.txt', '.xaml', '.yaml', '.yml')
$textFiles = Get-ChildItem -LiteralPath $root -File -Recurse -Force | Where-Object {
    $_.FullName -notmatch '[\\/](\.git|\.vs|bin|obj|artifacts|TestResults)[\\/]' -and
    ($configurationExtensions -contains $_.Extension.ToLowerInvariant() -or $_.Name -in @('AGENT.md', 'AGENTS.md', 'CLAUDE.md', 'NuGet.Config', 'pre-commit', 'commit-msg'))
}

$secretPatterns = [ordered]@{
    'private key' = '-----BEGIN (?:RSA |EC |DSA |OPENSSH )?PRIVATE KEY-----'
    'GitHub token' = '\b(?:gh[pousr]_[A-Za-z0-9]{36,}|github_pat_[A-Za-z0-9_]{40,})\b'
    'AWS access key' = '\b(?:AKIA|ASIA)[A-Z0-9]{16}\b'
    'Slack token' = '\bxox[baprs]-[A-Za-z0-9-]{20,}\b'
    'Azure storage account key' = '(?i)AccountKey=[A-Za-z0-9+/]{40,}={0,2}'
}

foreach ($file in $textFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($secretPattern in $secretPatterns.GetEnumerator()) {
        if ($content -match $secretPattern.Value) {
            Add-SecurityViolation -Rule 'SEC-005' -Message "$(Get-SecurityRelativePath -Path $file.FullName) contains a suspected $($secretPattern.Key)."
        }
    }
}

$secretFileExtensions = @('.key', '.pem', '.pfx', '.p12', '.snk')
$secretFiles = Get-ChildItem -LiteralPath $root -File -Recurse -Force | Where-Object {
    $_.FullName -notmatch '[\\/](\.git|bin|obj|artifacts)[\\/]' -and $secretFileExtensions -contains $_.Extension.ToLowerInvariant()
}
foreach ($secretFile in $secretFiles) {
    Add-SecurityViolation -Rule 'SEC-005' -Message "Prohibited secret-bearing file exists: $(Get-SecurityRelativePath -Path $secretFile.FullName)"
}

$scriptFiles = Get-ChildItem -LiteralPath (Join-Path $root 'eng') -Filter '*.ps1' -File -Recurse
foreach ($scriptFile in $scriptFiles) {
    $tokens = $null
    $parseErrors = $null
    $syntaxTree = [System.Management.Automation.Language.Parser]::ParseFile($scriptFile.FullName, [ref] $tokens, [ref] $parseErrors)
    foreach ($parseError in @($parseErrors)) {
        Add-SecurityViolation -Rule 'SEC-011' -Message "$(Get-SecurityRelativePath -Path $scriptFile.FullName) has a PowerShell parse error: $($parseError.Message)"
    }

    $commandNodes = $syntaxTree.FindAll({
        param($node)
        return $node -is [System.Management.Automation.Language.CommandAst]
    }, $true)
    foreach ($commandNode in $commandNodes) {
        $commandName = $commandNode.GetCommandName()
        if ($commandName -in @('iex', 'Invoke-Expression', 'Set-ExecutionPolicy', 'Add-MpPreference', 'Set-MpPreference')) {
            Add-SecurityViolation -Rule 'SEC-011' -Message "$(Get-SecurityRelativePath -Path $scriptFile.FullName) invokes prohibited command $commandName."
        }
        if ($commandName -eq 'Start-Process' -and $commandNode.Extent.Text -match '(?i)\b-Verb\s+RunAs\b') {
            Add-SecurityViolation -Rule 'SEC-011' -Message "$(Get-SecurityRelativePath -Path $scriptFile.FullName) requests an elevated child process."
        }
        if ($commandName -in @('cmd', 'cmd.exe') -and $commandNode.Extent.Text -match '(?i)(?:^|\s)/c(?:\s|$)') {
            Add-SecurityViolation -Rule 'SEC-011' -Message "$(Get-SecurityRelativePath -Path $scriptFile.FullName) invokes a cross-shell command string."
        }
    }

    $typeNodes = $syntaxTree.FindAll({
        param($node)
        return $node -is [System.Management.Automation.Language.TypeExpressionAst]
    }, $true)
    foreach ($typeNode in $typeNodes) {
        if ($typeNode.TypeName.FullName -in @('System.Net.WebClient', 'Net.WebClient')) {
            Add-SecurityViolation -Rule 'SEC-011' -Message "$(Get-SecurityRelativePath -Path $scriptFile.FullName) uses prohibited WebClient."
        }
    }

    $memberNodes = $syntaxTree.FindAll({
        param($node)
        return $node -is [System.Management.Automation.Language.MemberExpressionAst]
    }, $true)
    foreach ($memberNode in $memberNodes) {
        if ($memberNode.Member.Value -in @('DownloadString', 'DownloadFile')) {
            Add-SecurityViolation -Rule 'SEC-011' -Message "$(Get-SecurityRelativePath -Path $scriptFile.FullName) invokes a prohibited download member."
        }
    }
}

$suppressionPatterns = @(
    '(?i)<NuGetAuditSuppress\b',
    '(?i)<NuGetAudit>\s*false\s*</NuGetAudit>',
    '(?i)<NuGetAuditMode>\s*direct\s*</NuGetAuditMode>',
    '(?i)<NuGetAuditLevel>\s*(?:moderate|high|critical)\s*</NuGetAuditLevel>'
)
$nuGetConfigurationFiles = $textFiles | Where-Object {
    $_.Extension.ToLowerInvariant() -in @('.csproj', '.props', '.targets') -or $_.Name -eq 'NuGet.Config'
}
foreach ($file in $nuGetConfigurationFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($pattern in $suppressionPatterns) {
        if ($content -match $pattern) {
            Add-SecurityViolation -Rule 'SEC-007' -Message "$(Get-SecurityRelativePath -Path $file.FullName) weakens or suppresses NuGet auditing."
        }
    }
}

if ($null -ne $cases) {
    $caseIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($case in @($cases.cases)) {
        $caseId = [string] $case.id
        if ($caseId -notmatch '^ADV-\d{3}$' -or -not $caseIds.Add($caseId)) {
            Add-SecurityViolation -Rule 'SEC-001' -Message "Invalid or duplicate adversarial case ID: $caseId"
        }
        if ([string]::IsNullOrWhiteSpace([string] $case.owner) -or [string]::IsNullOrWhiteSpace([string] $case.defense)) {
            Add-SecurityViolation -Rule 'SEC-001' -Message "$caseId lacks an owner or defense."
        }
    }

    if ($caseIds.Count -lt 18) {
        Add-SecurityViolation -Rule 'SEC-001' -Message "Expected at least 18 adversarial cases; found $($caseIds.Count)."
    }

    $statePath = Join-Path $root 'docs/PROJECT_STATE.md'
    if (Test-Path -LiteralPath $statePath -PathType Leaf) {
        $state = Get-Content -LiteralPath $statePath -Raw
        if ($state -match '(?m)^- Stage: `implementation`\s*$') {
            $testContent = (Get-ChildItem -LiteralPath (Join-Path $root 'tests') -Filter '*.cs' -File -Recurse | ForEach-Object {
                Get-Content -LiteralPath $_.FullName -Raw
            }) -join "`n"

            if ($testContent -notmatch 'TestCategory\s*\(\s*"Adversarial"\s*\)') {
                Add-SecurityViolation -Rule 'TST-010' -Message 'Implementation stage requires tests categorized as Adversarial.'
            }

            foreach ($caseId in $caseIds) {
                $mappingPattern = "TestProperty\s*\(\s*`"ThreatId`"\s*,\s*`"$([regex]::Escape($caseId))`"\s*\)"
                if ($testContent -notmatch $mappingPattern) {
                    Add-SecurityViolation -Rule 'TST-010' -Message "$caseId has no exact TestProperty mapping."
                }
            }
        }
    }
}

$toolManifestPath = Join-Path $root '.config/dotnet-tools.json'
if (Test-Path -LiteralPath $toolManifestPath -PathType Leaf) {
    $toolManifest = Get-Content -LiteralPath $toolManifestPath -Raw | ConvertFrom-Json
    $strykerTool = $toolManifest.tools.'dotnet-stryker'
    if ($strykerTool.version -cne '4.16.0' -or $strykerTool.rollForward -ne $false) {
        Add-SecurityViolation -Rule 'TST-008' -Message 'dotnet-stryker must be pinned to 4.16.0 with roll-forward disabled.'
    }
}

$strykerConfigPath = Join-Path $root 'stryker-config.json'
if (Test-Path -LiteralPath $strykerConfigPath -PathType Leaf) {
    $strykerConfig = (Get-Content -LiteralPath $strykerConfigPath -Raw | ConvertFrom-Json).'stryker-config'
    if ($strykerConfig.'mutation-level' -cne 'Complete' -or
        $strykerConfig.'test-runner' -cne 'mtp' -or
        $strykerConfig.'break-on-initial-test-failure' -ne $true -or
        $strykerConfig.thresholds.high -ne 100 -or
        $strykerConfig.thresholds.low -ne 95 -or
        $strykerConfig.thresholds.break -ne 90) {
        Add-SecurityViolation -Rule 'TST-008' -Message 'Stryker complete-level execution and 100/95/90 thresholds are protected.'
    }

    $strykerRaw = Get-Content -LiteralPath $strykerConfigPath -Raw
    if ($strykerRaw -match '(?i)baseline|excluded-mutations|ignore-mutations') {
        Add-SecurityViolation -Rule 'TST-008' -Message 'Mutation baselines and mutation exclusions are prohibited.'
    }
}

if ($violations.Count -gt 0) {
    foreach ($violation in $violations) {
        Write-Error $violation -ErrorAction Continue
    }
    Write-Error "Security conformance failed with $($violations.Count) violation(s)."
    exit 1
}

if (-not $SkipProof) {
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'prove-security.ps1') -RepositoryRoot $root
    if ($LASTEXITCODE -ne 0) {
        throw 'Security negative proofs failed.'
    }
}

Write-Host "Security conformance passed: $($cases.cases.Count) adversarial cases registered; secrets and workflow supply chain clean."
