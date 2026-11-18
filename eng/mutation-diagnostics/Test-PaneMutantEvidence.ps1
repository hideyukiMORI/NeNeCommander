[CmdletBinding()]
param([Parameter(Mandatory)][string] $Output)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'PaneMutantEvidence.psm1') -Force
$root = [IO.Path]::GetFullPath($Output)
if (-not $root.StartsWith('D:\NeNeCommander\outputs\issue-439\', [StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $root)) { throw 'Proof output must be a new directory below D:/NeNeCommander/outputs/issue-439.' }
New-Item -ItemType Directory -Path $root | Out-Null
$checks = [Collections.Generic.List[object]]::new()
$failure = $null
$manifestPath = Join-Path $PSScriptRoot 'pane-mutant-429.json'
$fixed = Read-PaneMutantManifest -Path $manifestPath
$snippet = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'fixtures/GetFocusIndex.instrumented.txt'))

function Copy-FixtureValue {
    param($Value)
    return $Value | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
}

function Assert-Rejected {
    param([string] $Name, [scriptblock] $Action, [string] $Message)
    $observed = $null
    try { & $Action | Out-Null } catch { $observed = $_.Exception.Message }
    if ($null -eq $observed -or $observed -notlike ('*' + $Message + '*')) { throw "Wrong rejection for $Name`: $observed" }
    $checks.Add(@{ name = $Name; result = 'rejected'; message = $observed })
}

function New-EvidenceFixture {
    $manifest = Copy-FixtureValue -Value $fixed
    $source = 'synthetic fixture source'
    $manifest.target.sourceSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($source))).ToLowerInvariant()
    $target = @{ id = '1813'; mutatorName = $manifest.target.mutatorName; replacement = $manifest.target.replacement; location = (Copy-FixtureValue -Value $manifest.target.location); status = 'Survived'; static = $false; coveredBy = @($manifest.testIds); killedBy = @() }
    $file = 'D:/Fixture/' + $manifest.target.path
    $report = @{ files = @{ $file = @{ source = $source; mutants = @($target) } }; testFiles = @{ 'Fixture.cs' = @{ tests = @($manifest.testIds | ForEach-Object { @{ id = $_ } }) } } }
    $filter = ($manifest.testIds | ForEach-Object { 'Id=' + $_ }) -join '|'
    $phases = @()
    foreach ($name in @('initial', 'coverage', 'mutant')) {
        $control = if ($name -eq 'initial') { '' } else { '<MutantControl name="StrykerFixture.MutantControl" />' }
        $active = if ($name -eq 'mutant') { '<Mutant id="1813" tests="' + ($manifest.testIds -join ',') + '" />' } else { '' }
        $settings = '<RunSettings><RunConfiguration><TestCaseFilter>' + $filter + '</TestCaseFilter></RunConfiguration><Configuration>' + $control + $active + '</Configuration></RunSettings>'
        $start = @{ RunSettings = $settings }
        if ($name -ne 'initial') { $start.Tests = @($manifest.testIds | ForEach-Object { @{ Id = $_ } }) }
        $results = @($manifest.testIds | ForEach-Object { @{ TestCase = @{ Id = $_; FullyQualifiedName = 'Fixture.Case' }; Outcome = 1; ErrorMessage = $null; ErrorStackTrace = $null } })
        $complete = @{ TestRunCompleteArgs = @{ IsCanceled = $false; IsAborted = $false; Error = $null; TestRunStatistics = @{ ExecutedTests = 31; Stats = @{ Passed = 31 } } }; LastRunTests = @{ NewTestResults = $results } }
        $phases += @{ name = $name; start = $start; complete = $complete }
    }
    return @{ manifest = $manifest; report = $report; phases = $phases; injection = $snippet; rawExit = 2; file = $file }
}

function Write-EvidenceFixture {
    param([string] $Directory, [Collections.IDictionary] $Case)
    New-Item -ItemType Directory -Path (Join-Path $Directory 'reports'), (Join-Path $Directory 'logs') -Force | Out-Null
    $Case.report | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $Directory 'reports/mutation-report.json') -Encoding utf8
    $index = 0
    foreach ($phase in $Case.phases) {
        $kind = if ($phase.name -eq 'initial') { 'TestExecution.StartWithSources' } else { 'TestExecution.StartWithTests' }
        $lines = @(([ordered]@{ Version = 7; MessageType = $kind; Payload = $phase.start } | ConvertTo-Json -Depth 100 -Compress))
        if ($null -ne $phase.complete) { $lines += ([ordered]@{ Version = 7; MessageType = 'TestExecution.Completed'; Payload = $phase.complete } | ConvertTo-Json -Depth 100 -Compress) }
        $lines | Set-Content -LiteralPath (Join-Path $Directory ('logs/Runner 1-log.host.' + $index + '.txt')) -Encoding utf8
        $index++
    }
    ($Case.injection + "`n    private static PaneState ToggleSelection(PaneState state)") | Set-Content -LiteralPath (Join-Path $Directory 'logs/log-fixture.txt') -Encoding utf8
}

function Assert-EvidenceRejected {
    param([string] $Name, [scriptblock] $Mutate, [string] $Message)
    $case = New-EvidenceFixture
    & $Mutate $case
    $directory = Join-Path $root $Name
    Write-EvidenceFixture -Directory $directory -Case $case
    Assert-Rejected -Name $Name -Message $Message -Action { Get-PaneMutantEvidence -Output $directory -Manifest $case.manifest -RawExit $case.rawExit }
}

try {
    $checks.Add(@{ name = 'fixed-manifest'; result = 'passed'; inputs = $fixed.inputFiles.Count; ids = $fixed.testIds.Count })
    $changedManifest = Join-Path $root 'changed-manifest.json'
    [IO.File]::WriteAllText($changedManifest, [IO.File]::ReadAllText($manifestPath) + ' ')
    Assert-Rejected -Name 'changed-manifest' -Message 'Fixed manifest hash differs' -Action { Read-PaneMutantManifest -Path $changedManifest }

    $case = New-EvidenceFixture
    $directory = Join-Path $root 'survived'
    Write-EvidenceFixture -Directory $directory -Case $case
    $proof = Get-PaneMutantEvidence -Output $directory -Manifest $case.manifest -RawExit 2
    if ($proof.phases.Count -ne 3 -or $proof.target.status -cne 'Survived') { throw 'Positive survived fixture differs.' }
    $checks.Add(@{ name = 'survived'; result = 'passed' })
    $case = New-EvidenceFixture
    $case.report.files[$case.file].mutants[0].status = 'Killed'
    $case.report.files[$case.file].mutants[0].killedBy = @($fixed.testIds[0])
    $case.phases[2].complete.LastRunTests.NewTestResults[0].Outcome = 2
    $case.phases[2].complete.LastRunTests.NewTestResults[0].ErrorMessage = 'Controlled failure'
    $case.phases[2].complete.LastRunTests.NewTestResults[0].ErrorStackTrace = 'Fixture.Case'
    $case.phases[2].complete.TestRunCompleteArgs.TestRunStatistics.Stats = @{ Passed = 30; Failed = 1 }
    $directory = Join-Path $root 'killed'
    Write-EvidenceFixture -Directory $directory -Case $case
    $proof = Get-PaneMutantEvidence -Output $directory -Manifest $case.manifest -RawExit 0
    if ($proof.target.status -cne 'Killed' -or $proof.phases[2].failedIds.Count -ne 1) { throw 'Positive killed fixture differs.' }
    $checks.Add(@{ name = 'killed'; result = 'passed' })

    Assert-EvidenceRejected 'missing-id' { param($c) $c.phases[2].complete.LastRunTests.NewTestResults = @($c.phases[2].complete.LastRunTests.NewTestResults | Select-Object -First 30) } 'Exact test ID set differs'
    Assert-EvidenceRejected 'duplicate-id' { param($c) $c.phases[2].complete.LastRunTests.NewTestResults[30].TestCase.Id = $c.manifest.testIds[0] } 'Duplicate test ID'
    Assert-EvidenceRejected 'two-active' { param($c) $c.phases[2].start.RunSettings = $c.phases[2].start.RunSettings.Replace('</Configuration>', '<Mutant id="1812" tests="x" /></Configuration>') } 'Active mutant differs'
    Assert-EvidenceRejected 'wrong-active' { param($c) $c.phases[2].start.RunSettings = $c.phases[2].start.RunSettings.Replace('id="1813"', 'id="1812"') } 'Active mutant differs'
    Assert-EvidenceRejected 'missing-phase' { param($c) $c.phases = @($c.phases | Select-Object -First 2) } 'Exact test ID set differs'
    Assert-EvidenceRejected 'duplicate-phase' { param($c) $c.phases += (Copy-FixtureValue $c.phases[2]) } 'Duplicate test ID'
    Assert-EvidenceRejected 'missing-completion' { param($c) $c.phases[2].complete = $null } 'Execution completion is absent'
    Assert-EvidenceRejected 'aborted' { param($c) $c.phases[2].complete.TestRunCompleteArgs.IsAborted = $true } 'Runner cancelled, aborted'
    Assert-EvidenceRejected 'initial-failure' { param($c) $c.phases[0].complete.LastRunTests.NewTestResults[0].Outcome = 2 } 'Original or coverage tests failed'
    Assert-EvidenceRejected 'skipped' { param($c) $c.phases[2].complete.LastRunTests.NewTestResults[0].Outcome = 3 } 'A test is skipped'
    Assert-EvidenceRejected 'wrong-count' { param($c) $c.phases[2].complete.TestRunCompleteArgs.TestRunStatistics.ExecutedTests = 30 } 'Executed test count differs'
    Assert-EvidenceRejected 'wrong-statistics' { param($c) $c.phases[2].complete.TestRunCompleteArgs.TestRunStatistics.Stats.Passed = 30 } 'Outcome statistics differ'
    Assert-EvidenceRejected 'extra-tested-mutant' { param($c) $c.report.files[$c.file].mutants += (Copy-FixtureValue $c.report.files[$c.file].mutants[0]) } 'Tested mutant count is not one'
    Assert-EvidenceRejected 'wrong-replacement' { param($c) $c.report.files[$c.file].mutants[0].replacement = 'changed' } 'Target mutation identity differs'
    Assert-EvidenceRejected 'wrong-source' { param($c) $c.report.files[$c.file].source = 'changed' } 'Report source differs'
    Assert-EvidenceRejected 'coverage-id-missing' { param($c) $c.report.files[$c.file].mutants[0].coveredBy = @($c.manifest.testIds | Select-Object -First 30) } 'Exact test ID set differs'
    Assert-EvidenceRejected 'discovered-id-duplicate' { param($c) $c.report.testFiles['Fixture.cs'].tests[30].id = $c.manifest.testIds[0] } 'Duplicate test ID'
    Assert-EvidenceRejected 'changed-injection' { param($c) $c.injection = $c.injection.Replace('return (', 'return 1 + (') } 'Injected method differs'
    Assert-EvidenceRejected 'absent-injection' { param($c) $c.injection = '' } 'Injected method evidence is absent'
    Assert-EvidenceRejected 'wrong-control-name' { param($c) $c.phases[2].start.RunSettings = $c.phases[2].start.RunSettings.Replace('StrykerFixture', 'StrykerOther') } 'RunSettings control does not name'
    Assert-EvidenceRejected 'wrong-filter' { param($c) $c.phases[2].start.RunSettings = $c.phases[2].start.RunSettings.Replace('Id=', 'Name=') } 'RunSettings filter differs'
    Assert-EvidenceRejected 'wrong-exit' { param($c) $c.rawExit = 0 } 'Survived verdict, exit'

    $inputsRoot = Join-Path $root 'inputs'
    New-Item -ItemType Directory -Path $inputsRoot | Out-Null
    [IO.File]::WriteAllText((Join-Path $inputsRoot 'source.txt'), 'source')
    [IO.File]::WriteAllText((Join-Path $inputsRoot 'assembly.bin'), 'original assembly')
    $inputs = @{ 'source.txt' = (Get-FileHash -LiteralPath (Join-Path $inputsRoot 'source.txt')).Hash.ToLowerInvariant() }
    $assemblies = @{ 'assembly.bin' = (Get-FileHash -LiteralPath (Join-Path $inputsRoot 'assembly.bin')).Hash.ToLowerInvariant() }
    Assert-PaneRestoration -Root $inputsRoot -Inputs $inputs -Assemblies $assemblies
    $checks.Add(@{ name = 'unchanged-inputs-and-assembly'; result = 'passed' })
    Assert-Rejected 'escaped-input' { Assert-PaneInputHashes -Root $inputsRoot -Expected @{ '../outside.txt' = 'bad' } } 'Input path escaped'
    Assert-Rejected 'absent-input' { Assert-PaneInputHashes -Root $inputsRoot -Expected @{ 'absent.txt' = 'bad' } } 'Input is absent'
    [IO.File]::WriteAllText((Join-Path $inputsRoot 'assembly.bin'), 'mutated assembly')
    Assert-Rejected 'assembly-not-restored' { Assert-PaneRestoration -Root $inputsRoot -Inputs $inputs -Assemblies $assemblies } 'Input hash differs: assembly.bin'
    [IO.File]::WriteAllText((Join-Path $inputsRoot 'source.txt'), 'changed source')
    Assert-Rejected 'input-hash-mismatch' { Assert-PaneInputHashes -Root $inputsRoot -Expected $inputs } 'Input hash differs: source.txt'

    $child = Join-Path $root 'exit-two.ps1'
    'Write-Output "controlled child output"; exit 2' | Set-Content -LiteralPath $child -Encoding utf8
    $command = Invoke-PaneCommand -Name 'raw-exit-two' -Executable 'pwsh' -Arguments @('-NoProfile', '-File', $child) -Directory $root -Evidence $root
    if ($command.exitCode -ne 2 -or (Get-PaneDiagnosticExit -RawExit $command.exitCode -EvidenceValid $true) -ne 2 -or
        (Get-PaneDiagnosticExit -RawExit $command.exitCode -EvidenceValid $false) -ne 2 -or
        -not (Test-Path -LiteralPath (Join-Path $root 'raw-exit-two.json')) -or -not (Test-Path -LiteralPath (Join-Path $root 'raw-exit-two.log'))) { throw 'Raw exit 2 or evidence preservation differs.' }
    if ((Get-PaneDiagnosticExit -RawExit 0 -EvidenceValid $false) -ne 1 -or (Get-PaneDiagnosticExit -RawExit $null -EvidenceValid $false) -ne 1) { throw 'Incomplete evidence was accepted.' }
    $checks.Add(@{ name = 'raw-exit-and-failure-evidence'; result = 'passed' })
    $entry = Join-Path $PSScriptRoot 'Invoke-PaneMutantComparison.ps1'
    foreach ($rejection in @(
        @{ name = 'not-ci'; key = 'GITHUB_ACTIONS'; value = 'false' },
        @{ name = 'not-manual'; key = 'GITHUB_EVENT_NAME'; value = 'push' },
        @{ name = 'wrong-repository'; key = 'GITHUB_REPOSITORY'; value = 'other/repository' },
        @{ name = 'not-main'; key = 'GITHUB_REF'; value = 'refs/heads/topic' },
        @{ name = 'invalid-run'; key = 'GITHUB_RUN_ID'; value = 'not-a-run' },
        @{ name = 'rerun'; key = 'GITHUB_RUN_ATTEMPT'; value = '2' }
    )) {
        $environment = [ordered]@{ GITHUB_ACTIONS = 'true'; GITHUB_EVENT_NAME = 'workflow_dispatch'; GITHUB_REPOSITORY = 'hideyukiMORI/NeNeCommander'; GITHUB_REF = 'refs/heads/main'; GITHUB_RUN_ID = '1'; GITHUB_RUN_ATTEMPT = '1' }
        $environment[$rejection.key] = $rejection.value
        $lines = @('$ErrorActionPreference = ''Stop''')
        foreach ($key in $environment.Keys) { $lines += '$env:' + $key + ' = ''' + $environment[$key] + '''' }
        $lines += '& ''' + $entry.Replace("'", "''") + ''''
        $child = Join-Path $root ($rejection.name + '.ps1')
        $lines | Set-Content -LiteralPath $child -Encoding utf8
        $command = Invoke-PaneCommand -Name $rejection.name -Executable 'pwsh' -Arguments @('-NoProfile', '-File', $child) -Directory $root -Evidence $root
        if ($command.exitCode -eq 0 -or (Get-Content -LiteralPath (Join-Path $root ($rejection.name + '.log')) -Raw) -notlike '*requires one first-attempt manual run*') { throw 'Entry guard did not stop before task storage and commands.' }
        $checks.Add(@{ name = $rejection.name; result = 'rejected-before-execution' })
    }
}
catch { $failure = $_.ToString() }
finally {
    @{ status = $(if ($null -eq $failure) { 'passed' } else { 'failed' }); count = $checks.Count; checks = $checks.ToArray(); failure = $failure } | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $root 'proof.json') -Encoding utf8
}
if ($null -ne $failure) { throw $failure }
Write-Host ('Pane mutant evidence proofs passed: ' + $checks.Count)
