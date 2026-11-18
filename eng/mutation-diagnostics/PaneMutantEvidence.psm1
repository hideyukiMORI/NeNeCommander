Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-PaneTextHash {
    param([string] $Text)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Text))).ToLowerInvariant()
}

function Read-PaneMutantManifest {
    param([string] $Path)
    $hash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($hash -cne '14b54b07ace7bb25ad41f9b7fa4a9f1cefff78e6e8ca06fa2492e28250668033') { throw 'Fixed manifest hash differs.' }
    $manifest = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -AsHashtable
    if ($manifest.schemaVersion -ne 1 -or $manifest.inputFiles.Count -ne 424 -or $manifest.testIds.Count -ne 31) { throw 'Fixed manifest dimensions differ.' }
    Assert-PaneIds -Actual $manifest.testIds -Expected $manifest.testIds
    return $manifest
}

function Assert-PaneIds {
    param([AllowEmptyCollection()][string[]] $Actual, [string[]] $Expected)
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($id in $Actual) {
        if (-not $ids.Add($id)) { throw 'Duplicate test ID.' }
    }
    if ($Actual.Count -ne $Expected.Count -or -not $ids.SetEquals($Expected)) { throw 'Exact test ID set differs.' }
}

function Assert-PaneInputHashes {
    param([string] $Root, [Collections.IDictionary] $Expected)
    $prefix = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    foreach ($relative in $Expected.Keys) {
        $path = [IO.Path]::GetFullPath((Join-Path $Root $relative))
        if (-not $path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Input path escaped the source root.' }
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Input is absent: $relative" }
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $Expected[$relative]) { throw "Input hash differs: $relative" }
    }
}

function Assert-PaneRestoration {
    param([string] $Root, [Collections.IDictionary] $Inputs, [Collections.IDictionary] $Assemblies)
    Assert-PaneInputHashes -Root $Root -Expected $Inputs
    Assert-PaneInputHashes -Root $Root -Expected $Assemblies
}

function Invoke-PaneCommand {
    param([string] $Name, [string] $Executable, [string[]] $Arguments, [string] $Directory, [string] $Evidence)
    $record = [ordered]@{ name = $Name; executable = $Executable; arguments = $Arguments; cwd = $Directory; startedUtc = [DateTimeOffset]::UtcNow.ToString('o'); status = 'running'; exitCode = $null }
    $log = Join-Path $Evidence ($Name + '.log')
    $record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $Evidence ($Name + '.json')) -Encoding utf8
    $PSNativeCommandUseErrorActionPreference = $false
    Push-Location -LiteralPath $Directory
    try {
        & $Executable @Arguments *> $log
        $record.exitCode = $LASTEXITCODE
    }
    catch {
        $record.exitCode = 1
        $record.failure = $_.ToString()
        $_ | Out-String | Add-Content -LiteralPath $log -Encoding utf8
    }
    finally {
        Pop-Location
        $record.status = 'completed'
        $record.completedUtc = [DateTimeOffset]::UtcNow.ToString('o')
        $record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $Evidence ($Name + '.json')) -Encoding utf8
    }
    return $record
}

function Get-PaneDiagnosticExit {
    param([AllowNull()] $RawExit, [bool] $EvidenceValid)
    if ($null -ne $RawExit -and [int] $RawExit -ne 0) { return [int] $RawExit }
    if (-not $EvidenceValid) { return 1 }
    return 0
}

function Get-PaneHostPhase {
    param([string] $Path, [string[]] $ExpectedIds, [string] $TargetId)
    $start = $null
    $completion = $null
    $results = [Collections.Generic.List[object]]::new()
    $warnings = [Collections.Generic.List[string]]::new()
    foreach ($line in [IO.File]::ReadLines($Path)) {
        if ($line.Contains('TpTrace Warning:') -or $line.Contains('TpTrace Error:')) { $warnings.Add($line) }
        $offset = $line.IndexOf('{"Version":', [StringComparison]::Ordinal)
        if ($offset -lt 0) { continue }
        $event = $line.Substring($offset) | ConvertFrom-Json -AsHashtable
        $payload = $event.Payload
        if ($event.MessageType -cin @('TestExecution.StartWithSources', 'TestExecution.StartWithTests')) {
            if ($null -ne $start) { throw 'Multiple execution requests in one host log.' }
            $start = $payload
        }
        if ($event.MessageType -ceq 'TestExecution.StatsChange') {
            foreach ($result in $payload.NewTestResults) { $results.Add($result) }
        }
        if ($event.MessageType -ceq 'TestExecution.Completed') {
            if ($null -ne $completion) { throw 'Multiple completions in one host log.' }
            $completion = $payload
            if ($null -ne $payload.LastRunTests) {
                foreach ($result in $payload.LastRunTests.NewTestResults) { $results.Add($result) }
            }
        }
    }
    if ($null -eq $start) { return $null }
    if ($null -eq $completion) { throw 'Execution completion is absent.' }
    [xml] $settings = $start.RunSettings
    $mutants = @($settings.SelectNodes('//Mutant'))
    $controls = @($settings.SelectNodes('//MutantControl'))
    $phase = if ($mutants.Count -gt 0) { 'mutant' } elseif ($controls.Count -gt 0) { 'coverage' } else { 'initial' }
    if ($mutants.Count -gt 0) {
        if ($mutants.Count -ne 1 -or $mutants[0].GetAttribute('id') -cne $TargetId) { throw 'Active mutant differs or is not singular.' }
        Assert-PaneIds -Actual $mutants[0].GetAttribute('tests').Split(',') -Expected $ExpectedIds
    }
    if ($phase -ne 'initial' -and $controls.Count -ne 1) { throw 'Mutant control is not singular.' }
    $filter = ($ExpectedIds | ForEach-Object { 'Id=' + $_ }) -join '|'
    if ($settings.SelectSingleNode('//TestCaseFilter').InnerText -cne $filter) { throw 'RunSettings filter differs.' }
    if ($start.ContainsKey('Tests') -and $null -ne $start.Tests) {
        Assert-PaneIds -Actual @($start.Tests | ForEach-Object { $_.Id }) -Expected $ExpectedIds
    }
    Assert-PaneIds -Actual @($results | ForEach-Object { $_.TestCase.Id }) -Expected $ExpectedIds
    $complete = $completion.TestRunCompleteArgs
    if ($complete.IsCanceled -ne $false -or $complete.IsAborted -ne $false -or $null -ne $complete.Error) { throw 'Runner cancelled, aborted or reported an error.' }
    if ($complete.TestRunStatistics.ExecutedTests -ne $ExpectedIds.Count) { throw 'Executed test count differs.' }
    $failed = @($results | Where-Object { $_.Outcome -eq 2 })
    $passed = @($results | Where-Object { $_.Outcome -eq 1 })
    if ($failed.Count + $passed.Count -ne $ExpectedIds.Count) { throw 'A test is skipped, absent or has an unknown outcome.' }
    if ($phase -ne 'mutant' -and $failed.Count -ne 0) { throw 'Original or coverage tests failed.' }
    $statistics = $complete.TestRunStatistics.Stats
    if ([int] $statistics['Passed'] -ne $passed.Count -or [int] $statistics['Failed'] -ne $failed.Count -or
        @($statistics.Keys | Where-Object { $_ -cnotin @('Passed', 'Failed') -and [int] $statistics[$_] -ne 0 }).Count -ne 0) { throw 'Outcome statistics differ from individual results.' }
    return [ordered]@{ phase = $phase; hostLog = $Path; runSettings = $start.RunSettings; request = $start; completion = $completion; results = $results.ToArray(); warnings = $warnings.ToArray(); failedIds = @($failed | ForEach-Object { $_.TestCase.Id }) }
}

function Get-PaneInjectionHash {
    param([string] $Code, [string] $TargetId)
    $text = [regex]::Replace($Code, 'Stryker\w+\.MutantControl', 'CONTROL.MutantControl')
    $otherIds = [ordered]@{}
    $activeCalls = [regex]::Matches($text, 'IsActive\((\d+)\)')
    if (@($activeCalls | Where-Object { $_.Groups[1].Value -ceq $TargetId }).Count -ne 1) { throw 'Target activation is not singular in injected method.' }
    foreach ($call in $activeCalls) {
        $id = $call.Groups[1].Value
        if ($id -ceq $TargetId) { $replacement = 'TARGET' }
        else {
            if (-not $otherIds.Contains($id)) { $otherIds[$id] = 'OTHER' + $otherIds.Count }
            $replacement = $otherIds[$id]
        }
        $text = $text.Replace($call.Value, 'IsActive(' + $replacement + ')')
    }
    return Get-PaneTextHash -Text ([regex]::Replace($text, '\s+', ''))
}

function Get-PaneMutantEvidence {
    param([string] $Output, [Collections.IDictionary] $Manifest, [int] $RawExit)
    $report = Get-Content -LiteralPath (Join-Path $Output 'reports/mutation-report.json') -Raw | ConvertFrom-Json -AsHashtable
    $tested = @()
    foreach ($file in $report.files.Keys) {
        foreach ($mutant in $report.files[$file].mutants) {
            if ($mutant.status -cnotin @('Ignored', 'CompileError')) { $tested += @{ file = $file; mutant = $mutant } }
        }
    }
    if ($tested.Count -ne 1) { throw 'Tested mutant count is not one.' }
    $target = $tested[0].mutant
    $targetFile = $tested[0].file
    if (-not $targetFile.Replace('\', '/').EndsWith('/' + $Manifest.target.path, [StringComparison]::Ordinal) -or
        $target.mutatorName -cne $Manifest.target.mutatorName -or $target.replacement -cne $Manifest.target.replacement -or $target.static -ne $false) { throw 'Target mutation identity differs.' }
    foreach ($end in @('start', 'end')) {
        foreach ($part in @('line', 'column')) {
            if ($target.location[$end][$part] -ne $Manifest.target.location[$end][$part]) { throw 'Target location differs.' }
        }
    }
    if ((Get-PaneTextHash -Text $report.files[$targetFile].source) -cne $Manifest.target.sourceSha256) { throw 'Report source differs.' }
    $discovered = @($report.testFiles.Values | ForEach-Object { $_.tests } | ForEach-Object { $_.id })
    Assert-PaneIds -Actual $discovered -Expected $Manifest.testIds
    Assert-PaneIds -Actual $target.coveredBy -Expected $Manifest.testIds
    $phases = @()
    foreach ($log in Get-ChildItem -LiteralPath (Join-Path $Output 'logs') -Filter 'Runner *-log.host.*.txt' -File) {
        $phase = Get-PaneHostPhase -Path $log.FullName -ExpectedIds $Manifest.testIds -TargetId $target.id
        if ($null -ne $phase) { $phases += $phase }
    }
    Assert-PaneIds -Actual @($phases | ForEach-Object { $_.phase }) -Expected @('initial', 'coverage', 'mutant')
    $mutantPhase = @($phases | Where-Object { $_.phase -ceq 'mutant' })[0]
    if ($target.status -ceq 'Survived') {
        if ($RawExit -ne 2 -or $mutantPhase.failedIds.Count -ne 0 -or $target.killedBy.Count -ne 0) { throw 'Survived verdict, exit or individual outcomes disagree.' }
    } elseif ($target.status -ceq 'Killed') {
        if ($RawExit -ne 0 -or $mutantPhase.failedIds.Count -eq 0 -or $target.killedBy.Count -eq 0 -or
            @($target.killedBy | Where-Object { $_ -cnotin $mutantPhase.failedIds }).Count -ne 0) { throw 'Killed verdict, exit or individual outcomes disagree.' }
    } else { throw 'Target verdict is not a completed killed/survived comparison.' }
    $injections = @()
    foreach ($log in Get-ChildItem -LiteralPath (Join-Path $Output 'logs') -Filter 'log-*.txt' -File) {
        $contents = Get-Content -LiteralPath $log.FullName -Raw
        foreach ($match in [regex]::Matches($contents, '(?ms)^[ \t]*private static int GetFocusIndex\(PaneState state\).*?(?=^[ \t]*private static PaneState ToggleSelection)')) {
            $code = $match.Value
            $names = @([regex]::Matches($code, 'Stryker\w+\.MutantControl(?=\.IsActive)') | ForEach-Object { $_.Value } | Select-Object -Unique)
            if ($names.Count -ne 1) { throw 'Injected control name is not singular.' }
            foreach ($phase in $phases | Where-Object { $_.phase -cne 'initial' }) {
                [xml] $settings = $phase.runSettings
                if ($settings.SelectSingleNode('//MutantControl').GetAttribute('name') -cne $names[0]) { throw 'RunSettings control does not name the injected control.' }
            }
            if ((Get-PaneInjectionHash -Code $code -TargetId $target.id) -cne $Manifest.target.normalizedInjectionSha256) { throw 'Injected method differs from the local reference.' }
            $injections += @{ log = $log.FullName; offset = $match.Index; code = $code }
        }
    }
    if ($injections.Count -eq 0) { throw 'Injected method evidence is absent.' }
    return [ordered]@{ target = $target; targetFile = $targetFile; rawExit = $RawExit; phases = $phases; injections = $injections; sameFixedScope = $true }
}

Export-ModuleMember -Function Read-PaneMutantManifest, Assert-PaneIds, Assert-PaneInputHashes, Assert-PaneRestoration, Invoke-PaneCommand, Get-PaneDiagnosticExit, Get-PaneMutantEvidence, Get-PaneInjectionHash
