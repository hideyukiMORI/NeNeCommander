<#
.SYNOPSIS
  Self-check of the UI evidence harness decisions (Issue #169). Calls no Win32 and no UIA.

.DESCRIPTION
  Proves that the pure admission functions reject every mismatch, and that Send-AdmittedKey
  sends nothing and latches a stop when the injected observation does not match. The sender and
  observer are injected script blocks, so no SendInput, process start, or settings access occurs.
  Pester is not required.

.EXAMPLE
  pwsh -NoProfile -File ./eng/ui-evidence/selftest.ps1
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'UiEvidence.psm1'
$script:Cases = 0
$script:Failures = [System.Collections.Generic.List[string]]::new()

function Assert-Case {
    param([string] $Name, [bool] $Condition, [string] $Detail = '')

    $script:Cases++
    if ($Condition) {
        Write-Host "PASS $Name"
    }
    else {
        $script:Failures.Add("$Name $Detail")
        Write-Host "FAIL $Name $Detail"
    }
}

function New-Expected {
    return @{
        RootHwnd = 0x1A2B
        ProcessId = 4242
        SessionId = 3
        FocusedAutomationIds = @('LeftFileList', 'RightFileList')
        Present = @()
        Absent = @('WindowAdjustmentHelper', 'NameEntry')
    }
}

function New-Observed {
    return @{
        ForegroundRootHwnd = 0x1A2B
        ForegroundProcessId = 4242
        ForegroundSessionId = 3
        FocusedProcessId = 4242
        FocusedAutomationId = 'LeftFileList'
        PresentAutomationIds = @()
    }
}

Import-Module $modulePath -Force

# ---- Test-InputAdmission ----------------------------------------------------------------------
$verdict = Test-InputAdmission -Observed (New-Observed) -Expected (New-Expected)
Assert-Case 'admission: all facts match -> admit' ($verdict.Admit -and $verdict.Reason -ceq 'admitted') $verdict.Reason

$mismatches = [ordered]@{
    'foreground window differs' = @{ Key = 'ForegroundRootHwnd'; Value = 0x9999; Reason = 'foreground-window-mismatch' }
    'no foreground window' = @{ Key = 'ForegroundRootHwnd'; Value = 0; Reason = 'foreground-window-mismatch' }
    'foreground process differs' = @{ Key = 'ForegroundProcessId'; Value = 1; Reason = 'foreground-process-mismatch' }
    'session differs' = @{ Key = 'ForegroundSessionId'; Value = 1; Reason = 'session-mismatch' }
    'focused element in another process' = @{ Key = 'FocusedProcessId'; Value = 1; Reason = 'focus-process-mismatch' }
    'focused control unexpected' = @{ Key = 'FocusedAutomationId'; Value = 'LeftAddress'; Reason = 'focus-control-mismatch' }
    'no focused control' = @{ Key = 'FocusedAutomationId'; Value = $null; Reason = 'focus-control-mismatch' }
    'forbidden element present' = @{ Key = 'PresentAutomationIds'; Value = @('NameEntry'); Reason = 'unexpected-element-present:NameEntry' }
}
foreach ($case in $mismatches.GetEnumerator()) {
    $observed = New-Observed
    $observed[$case.Value.Key] = $case.Value.Value
    $verdict = Test-InputAdmission -Observed $observed -Expected (New-Expected)
    Assert-Case "admission: $($case.Key) -> refuse" (-not $verdict.Admit -and $verdict.Reason -ceq $case.Value.Reason) $verdict.Reason
}

$expected = New-Expected
$expected.Present = @('WindowAdjustmentHelper')
$expected.Absent = @()
$expected.FocusedAutomationIds = @('WindowAdjustmentHelper')
$observed = New-Observed
$observed.FocusedAutomationId = 'WindowAdjustmentHelper'
$verdict = Test-InputAdmission -Observed $observed -Expected $expected
Assert-Case 'admission: mode element missing -> refuse' (-not $verdict.Admit -and $verdict.Reason -ceq 'mode-element-missing:WindowAdjustmentHelper') $verdict.Reason
$observed.PresentAutomationIds = @('WindowAdjustmentHelper')
$verdict = Test-InputAdmission -Observed $observed -Expected $expected
Assert-Case 'admission: mode element present -> admit' $verdict.Admit $verdict.Reason

$observed = New-Observed
$observed.Remove('FocusedProcessId')
$verdict = Test-InputAdmission -Observed $observed -Expected (New-Expected)
Assert-Case 'admission: incomplete observation -> refuse' (-not $verdict.Admit -and $verdict.Reason -ceq 'observation-incomplete:FocusedProcessId') $verdict.Reason
$expected = New-Expected
$expected.Remove('SessionId')
$verdict = Test-InputAdmission -Observed (New-Observed) -Expected $expected
Assert-Case 'admission: incomplete expectation -> refuse' (-not $verdict.Admit -and $verdict.Reason -ceq 'expectation-incomplete:SessionId') $verdict.Reason
$expected = New-Expected
$expected.RootHwnd = 0
$verdict = Test-InputAdmission -Observed (New-Observed) -Expected $expected
Assert-Case 'admission: zero expected window -> refuse' (-not $verdict.Admit -and $verdict.Reason -ceq 'expectation-invalid') $verdict.Reason

# ---- Test-KeyAdmission ------------------------------------------------------------------------
foreach ($key in @('F5', 'F6', 'Delete', 'Enter', 'Space')) {
    $verdict = Test-KeyAdmission -Key $key -OpensModal 'NameEntry'
    Assert-Case "key: $key refused even with a declared modal" (-not $verdict.Admit -and $verdict.Reason -ceq "file-operation-key-refused:$key") $verdict.Reason
}
$verdict = Test-KeyAdmission -Key 'F8'
Assert-Case 'key: F8 without expected modal refused' (-not $verdict.Admit -and $verdict.Reason -ceq 'guarded-key-without-expected-modal:F8') $verdict.Reason
$verdict = Test-KeyAdmission -Key 'F8' -OpensModal 'DeletionConfirmation'
Assert-Case 'key: F8 refused while its modal is not exposed' (-not $verdict.Admit -and $verdict.Reason -ceq 'guarded-key-modal-not-exposed:F8') $verdict.Reason
$verdict = Test-KeyAdmission -Key 'F7'
Assert-Case 'key: F7 without expected modal refused' (-not $verdict.Admit) $verdict.Reason
$verdict = Test-KeyAdmission -Key 'F7' -OpensModal 'SettingsModal'
Assert-Case 'key: F7 with a different modal refused' (-not $verdict.Admit -and $verdict.Reason -ceq 'guarded-key-modal-mismatch:F7') $verdict.Reason
$verdict = Test-KeyAdmission -Key 'F7' -OpensModal 'NameEntry'
Assert-Case 'key: F7 with NameEntry admitted' $verdict.Admit $verdict.Reason
$verdict = Test-KeyAdmission -Key 'W' -Modifiers @('Control')
Assert-Case 'key: Ctrl+W admitted' $verdict.Admit $verdict.Reason
$verdict = Test-KeyAdmission -Key 'Q'
Assert-Case 'key: undeclared key refused' (-not $verdict.Admit -and $verdict.Reason -ceq 'key-unknown:Q') $verdict.Reason
$verdict = Test-KeyAdmission -Key 'Control'
Assert-Case 'key: a modifier alone (held key) refused' (-not $verdict.Admit) $verdict.Reason
$verdict = Test-KeyAdmission -Key 'L' -Modifiers @('Win')
Assert-Case 'key: undeclared modifier refused' (-not $verdict.Admit -and $verdict.Reason -ceq 'modifier-unknown:Win') $verdict.Reason

# ---- Send-AdmittedKey with injected observer and sender ----------------------------------------
function Invoke-SendCase {
    param([string] $Key, [string[]] $Modifiers = @(), [hashtable] $Observed, [string] $OpensModal, [int] $SenderReturns = -1, [switch] $ObserverThrows)

    # A fresh module instance per case so each latch starts clear.
    Import-Module $modulePath -Force
    $state = @{ Sent = 0; Observed = 0; Codes = $null }
    $observer = {
        param($expectation)
        $state.Observed++
        if ($ObserverThrows) { throw 'observation unavailable' }
        return $Observed
    }.GetNewClosure()
    $sender = {
        param($modifierCodes, $keyCode)
        $state.Sent++
        $state.Codes = @($modifierCodes) + @($keyCode)
        if ($SenderReturns -ge 0) { return $SenderReturns }
        return 2 * (@($modifierCodes).Count + 1)
    }.GetNewClosure()
    $stopped = $null
    $result = $null
    try {
        $result = Send-AdmittedKey -Key $Key -Modifiers $Modifiers -Expected (New-Expected) -OpensModal $OpensModal -Observer $observer -Sender $sender
    }
    catch {
        if ($_.FullyQualifiedErrorId -like 'UiEvidenceStop*') {
            $stopped = Get-UiEvidenceInputStop
        }
        else {
            throw
        }
    }
    return [pscustomobject]@{ Stopped = $stopped; Sent = $state.Sent; ObserverCalls = $state.Observed; Codes = $state.Codes; Result = $result }
}

$sendMismatches = [ordered]@{
    'foreground window mismatch' = @{ Key = 'ForegroundRootHwnd'; Value = 0x7777; Reason = 'foreground-window-mismatch' }
    'PID mismatch' = @{ Key = 'ForegroundProcessId'; Value = 99; Reason = 'foreground-process-mismatch' }
    'session mismatch' = @{ Key = 'ForegroundSessionId'; Value = 9; Reason = 'session-mismatch' }
    'focus process mismatch' = @{ Key = 'FocusedProcessId'; Value = 99; Reason = 'focus-process-mismatch' }
    'focus control mismatch' = @{ Key = 'FocusedAutomationId'; Value = 'RightAddress'; Reason = 'focus-control-mismatch' }
    'unexpected modal present' = @{ Key = 'PresentAutomationIds'; Value = @('WindowAdjustmentHelper'); Reason = 'unexpected-element-present:WindowAdjustmentHelper' }
}
foreach ($case in $sendMismatches.GetEnumerator()) {
    $observed = New-Observed
    $observed[$case.Value.Key] = $case.Value.Value
    $outcome = Invoke-SendCase -Key 'W' -Modifiers @('Control') -Observed $observed
    Assert-Case "send: $($case.Key) -> nothing sent, stop latched" ($outcome.Sent -eq 0 -and $outcome.Stopped -ceq $case.Value.Reason) "sent=$($outcome.Sent) stop=$($outcome.Stopped)"
}

# Mode element missing: the helper must be present for a mode key.
Import-Module $modulePath -Force
$state = @{ Sent = 0 }
$modeExpected = New-Expected
$modeExpected.FocusedAutomationIds = @('WindowAdjustmentHelper')
$modeExpected.Present = @('WindowAdjustmentHelper')
$modeExpected.Absent = @()
$modeObserved = New-Observed
$modeObserved.FocusedAutomationId = 'WindowAdjustmentHelper'
$stopReason = $null
try {
    [void] (Send-AdmittedKey -Key 'L' -Expected $modeExpected -Observer { param($e) $modeObserved }.GetNewClosure() -Sender { param($m, $k) $state.Sent++; 2 }.GetNewClosure())
}
catch {
    if ($_.FullyQualifiedErrorId -like 'UiEvidenceStop*') { $stopReason = Get-UiEvidenceInputStop } else { throw }
}
Assert-Case 'send: mode element missing -> nothing sent, stop latched' ($state.Sent -eq 0 -and $stopReason -ceq 'mode-element-missing:WindowAdjustmentHelper') "sent=$($state.Sent) stop=$stopReason"

# After a stop, even a fully matching observation sends nothing (no cleanup or key-up).
$latched = $null
try {
    [void] (Send-AdmittedKey -Key 'Escape' -Expected (New-Expected) -Observer { param($e) New-Observed } -Sender { param($m, $k) $state.Sent++; 2 }.GetNewClosure())
}
catch {
    if ($_.FullyQualifiedErrorId -like 'UiEvidenceStop*') { $latched = Get-UiEvidenceInputStop } else { throw }
}
Assert-Case 'send: latched stop blocks later matching sends' ($state.Sent -eq 0 -and $latched -ceq 'mode-element-missing:WindowAdjustmentHelper') "sent=$($state.Sent) stop=$latched"

$outcome = Invoke-SendCase -Key 'F5' -Observed (New-Observed)
Assert-Case 'send: refused key -> no observation, nothing sent' ($outcome.Sent -eq 0 -and $outcome.ObserverCalls -eq 0 -and $outcome.Stopped -ceq 'file-operation-key-refused:F5') "stop=$($outcome.Stopped)"
$outcome = Invoke-SendCase -Key 'F8' -Observed (New-Observed)
Assert-Case 'send: F8 without expected modal -> nothing sent' ($outcome.Sent -eq 0 -and $outcome.Stopped -ceq 'guarded-key-without-expected-modal:F8') "stop=$($outcome.Stopped)"
$outcome = Invoke-SendCase -Key 'W' -Modifiers @('Control') -Observed (New-Observed) -ObserverThrows
Assert-Case 'send: observation failure -> nothing sent' ($outcome.Sent -eq 0 -and $outcome.Stopped -ceq 'observation-failed') "stop=$($outcome.Stopped)"
$outcome = Invoke-SendCase -Key 'W' -Modifiers @('Control') -Observed (New-Observed) -SenderReturns 2
Assert-Case 'send: partial SendInput -> stop latched, no repair' ($outcome.Sent -eq 1 -and $outcome.Stopped -ceq 'send-incomplete:2/4') "sent=$($outcome.Sent) stop=$($outcome.Stopped)"
$outcome = Invoke-SendCase -Key 'W' -Modifiers @('Control') -Observed (New-Observed)
Assert-Case 'send: all facts match -> one atomic chord' ($outcome.Sent -eq 1 -and $null -eq $outcome.Stopped -and
    $outcome.Result.Events -eq 4 -and ($outcome.Codes -join ',') -ceq '17,87') "sent=$($outcome.Sent) codes=$($outcome.Codes -join ',')"
$outcome = Invoke-SendCase -Key 'F7' -Observed (New-Observed) -OpensModal 'NameEntry'
Assert-Case 'send: F7 with declared NameEntry -> sent once' ($outcome.Sent -eq 1 -and $null -eq $outcome.Stopped) "stop=$($outcome.Stopped)"

# ---- other pure decisions -----------------------------------------------------------------------
Import-Module $modulePath -Force
$steps = @{ 96 = 32; 120 = 40; 144 = 48; 168 = 56; 192 = 64; 288 = 96 }
foreach ($entry in $steps.GetEnumerator()) {
    Assert-Case "step: dpi $($entry.Key) -> $($entry.Value) px" ((Get-StepPixels -Dpi $entry.Key) -eq $entry.Value)
}

$commit = Test-CommitMatch -ProductVersion '1.0.0+55d911b0123456789abcdef0123456789abcdef' -ExpectedCommit '55D911B'
Assert-Case 'commit: prefix match' ($commit.Match -and $commit.Commit -ceq '55d911b0123456789abcdef0123456789abcdef')
$commit = Test-CommitMatch -ProductVersion '1.0.0+2b5e2ee42264b438d25e495bfd885c732908989c' -ExpectedCommit '55d911b'
Assert-Case 'commit: mismatch stops' (-not $commit.Match -and $commit.Reason -ceq 'binary-commit-mismatch')
$commit = Test-CommitMatch -ProductVersion '1.0.0' -ExpectedCommit '55d911b'
Assert-Case 'commit: absent suffix stops' (-not $commit.Match -and $commit.Reason -ceq 'binary-commit-absent')
$commit = Test-CommitMatch -ProductVersion '1.0.0+2b5e2ee' -ExpectedCommit $null
Assert-Case 'commit: not requested passes and still records' ($commit.Match -and $commit.Commit -ceq '2b5e2ee')

$window = @{ Left = 0; Top = 0; Right = 1000; Bottom = 700 }
$elements = @{
    A = @{ Left = 10; Top = 10; Right = 490; Bottom = 600; Offscreen = $false }
    B = @{ Left = 510; Top = 10; Right = 990; Bottom = 600; Offscreen = $false }
    C = @{ Left = 20; Top = 20; Right = 200; Bottom = 40; Offscreen = $false }
}
$layout = Test-LayoutVerdict -Elements $elements -Window $window -Required @('A', 'B', 'C') -DisjointPairs @(, @('A', 'B')) -ContainedIn @(, @('C', 'A'))
Assert-Case 'layout: disjoint, contained, inside window -> pass' $layout.Pass ($layout.Failures -join ',')
$elements.B = @{ Left = 480; Top = 10; Right = 1010; Bottom = 600; Offscreen = $false }
$elements.C = @{ Left = 20; Top = 20; Right = 20; Bottom = 40; Offscreen = $false }
$elements.D = $null
$layout = Test-LayoutVerdict -Elements $elements -Window $window -Required @('A', 'B', 'C', 'D') -DisjointPairs @(, @('A', 'B')) -ContainedIn @(, @('C', 'A'))
$expectedFailures = @('non-positive:C', 'absent:D', 'clipped-by-window:B', 'overlap:A+B')
Assert-Case 'layout: overlap, clipping, zero size, absence -> fail' (-not $layout.Pass -and
    @($expectedFailures | Where-Object { $layout.Failures -cnotcontains $_ }).Count -eq 0) ($layout.Failures -join ',')

$block = [pscustomobject]@{ focus = @('@entryFileList'); present = @(); absent = @('NameEntry') }
$resolved = Resolve-StepExpectation -Block $block -Owner @{ RootHwnd = 5; ProcessId = 6; SessionId = 7 } -EntryFileList 'RightFileList'
Assert-Case 'expectation: entry file list resolved' ((@($resolved.FocusedAutomationIds) -join ',') -ceq 'RightFileList' -and $resolved.RootHwnd -eq 5 -and (@($resolved.Absent) -join ',') -ceq 'NameEntry')

# ---- cells.json consistency -------------------------------------------------------------------
$matrix = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'cells.json') -Raw | ConvertFrom-Json
$ids = @($matrix.cells | ForEach-Object { [string] $_.id })
Assert-Case 'cells: ids unique' (@($ids | Select-Object -Unique).Count -eq $ids.Count)
foreach ($required in @('ctrl-w-helper', 'f7-name-entry', 'f2-rename-entry', 'f8-delete-confirmation', 'transfer-conflict',
        'settings-editor', 'narrator-speech', 'cross-scale-move', 'taskbar-seam', 'dpi-100', 'dpi-150', 'dpi-200', 'dpi-300', 'high-contrast')) {
    Assert-Case "cells: $required registered" ($ids -ccontains $required)
}
foreach ($scheme in @($matrix.schemes)) {
    Assert-Case "cells: scheme $scheme has normal and narrow" (($ids -ccontains "observe-$scheme-normal") -and ($ids -ccontains "observe-$scheme-narrow"))
}
foreach ($cell in @($matrix.cells)) {
    if ([string] $cell.status -ceq 'skip') {
        Assert-Case "cells: $($cell.id) skip reason declared" ([string] $cell.reason -cin @('not-implemented', 'environment-required'))
        continue
    }
    if ([string] $cell.mode -ceq 'Input') {
        foreach ($step in @($cell.steps)) {
            $opens = if ($null -ne $step.expects.PSObject.Properties['opensModal']) { [string] $step.expects.opensModal } else { $null }
            $verdict = Test-KeyAdmission -Key ([string] $step.key) -Modifiers @($step.modifiers | ForEach-Object { [string] $_ }) -OpensModal $opens
            Assert-Case "cells: $($cell.id)/$($step.id) key admitted by policy" $verdict.Admit $verdict.Reason
            Assert-Case "cells: $($cell.id)/$($step.id) declares before/after focus" (@($step.expects.before.focus).Count -ne 0 -and @($step.expects.after.focus).Count -ne 0)
        }
    }
}

Assert-Case 'no Win32 type was loaded by this self-check' (-not ('NeNeUiEvidenceNative' -as [type]))

Write-Host ''
Write-Host "UiEvidence selftest: $($script:Cases) cases, $($script:Cases - $script:Failures.Count) passed, $($script:Failures.Count) failed."
if ($script:Failures.Count -ne 0) {
    exit 1
}
exit 0
