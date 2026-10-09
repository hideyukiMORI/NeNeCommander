<#
.SYNOPSIS
  Issue #94 UI release evidence harness (Issue #169). An environment-tier recorder, not a gate.

.DESCRIPTION
  Like eng/run-live-wsl-tests.ps1, this launcher runs outside eng/check.ps1 and CI. It records
  what it measured; a PASS row is a recorded measurement on a named environment, and release
  readiness is decided from those records plus visual review (QLT-009, QLT-011).

  -Preflight      Records the environment only. It starts no process, sends no input, and
                  writes nothing except the evidence directory under -OutputRoot.
  -Mode Observe   Starts the owned NeNe Commander process, reads window and UIA bounds, sizes the
                  owned window to the narrow DIP size, judges layout, captures screenshots, and
                  closes the window through UIA WindowPattern.Close. No synthetic input.
  -Mode Input     Requires -AllowInput and -EnvironmentId. Sends keys only through
                  Send-AdmittedKey, which re-checks the owned foreground root HWND, PID, session,
                  focused control, and mode/modal elements before every chord and latches a stop
                  on the first mismatch. No held key and no cleanup key is ever sent.

  Production settings resolve through the LocalApplicationData Known Folder, so no environment
  variable isolates them. Settings are written only with -OwnedProfile, which records the start
  bytes or absence and restores them at the end; without it, scheme cells are skipped.

.EXAMPLE
  pwsh -NoProfile -File ./eng/ui-evidence/Invoke-UiEvidence.ps1 -Preflight -Binary D:\b\NeNeCommander.App.exe `
      -OutputRoot D:\evidence -TestRoot D:\nene-ui-root
#>
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Observe', 'Input')]
    [string] $Mode = 'Observe',

    [Parameter()]
    [switch] $AllowInput,

    [Parameter()]
    [string] $EnvironmentId,

    [Parameter(Mandatory)]
    [string] $Binary,

    [Parameter()]
    [string] $ExpectedCommit,

    [Parameter(Mandatory)]
    [string] $OutputRoot,

    [Parameter(Mandatory)]
    [string] $TestRoot,

    [Parameter()]
    [switch] $OwnedProfile,

    [Parameter()]
    [string[]] $Cells,

    [Parameter()]
    [switch] $Preflight
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'UiEvidence.psm1') -Force

function Test-PathWithin {
    param([string] $Path, [string] $Root)

    $normalizedPath = [System.IO.Path]::TrimEndingDirectorySeparator([System.IO.Path]::GetFullPath($Path))
    $normalizedRoot = [System.IO.Path]::TrimEndingDirectorySeparator([System.IO.Path]::GetFullPath($Root))
    return $normalizedPath.Equals($normalizedRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $normalizedPath.StartsWith($normalizedRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

# ---- argument admission (nothing has been read or written yet) --------------------------------
foreach ($pathArgument in @(@('Binary', $Binary), @('OutputRoot', $OutputRoot), @('TestRoot', $TestRoot))) {
    if (-not [System.IO.Path]::IsPathFullyQualified($pathArgument[1])) {
        throw "-$($pathArgument[0]) must be an absolute path."
    }
}
$Binary = [System.IO.Path]::GetFullPath($Binary)
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$TestRoot = [System.IO.Path]::TrimEndingDirectorySeparator([System.IO.Path]::GetFullPath($TestRoot))

if (-not [string]::IsNullOrEmpty($EnvironmentId) -and $EnvironmentId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$') {
    throw '-EnvironmentId must be 1-64 characters of letters, digits, dot, underscore, or hyphen.'
}
if ($Mode -eq 'Input' -and -not $Preflight) {
    if (-not $AllowInput) {
        throw '-Mode Input requires -AllowInput.'
    }
    if ([string]::IsNullOrEmpty($EnvironmentId)) {
        throw '-Mode Input requires -EnvironmentId naming the dedicated environment.'
    }
}
if ($Mode -eq 'Observe' -and $AllowInput) {
    throw '-AllowInput is only meaningful with -Mode Input.'
}
if (-not [string]::IsNullOrEmpty($ExpectedCommit) -and $ExpectedCommit -notmatch '^[0-9a-fA-F]{7,40}$') {
    throw '-ExpectedCommit must be 7-40 hexadecimal characters.'
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$settingsDirectory = Split-Path -Parent (Get-SettingsDocumentPath)
if ([System.IO.Path]::GetPathRoot($TestRoot).TrimEnd('\', '/') -ieq $TestRoot.TrimEnd('\', '/')) {
    throw '-TestRoot may not be a drive or share root.'
}
foreach ($forbidden in @($OutputRoot, $repositoryRoot, $settingsDirectory, (Split-Path -Parent $Binary))) {
    if ((Test-PathWithin -Path $TestRoot -Root $forbidden) -or (Test-PathWithin -Path $forbidden -Root $TestRoot)) {
        throw '-TestRoot may not overlap the output root, the repository, the settings folder, or the binary folder.'
    }
}
if (Test-Path -LiteralPath $TestRoot) {
    if (-not (Test-Path -LiteralPath $TestRoot -PathType Container)) {
        throw '-TestRoot exists and is not a directory.'
    }
    if (@(Get-ChildItem -LiteralPath $TestRoot -Force | Select-Object -First 1).Count -ne 0) {
        throw '-TestRoot must be empty or absent.'
    }
}

$matrix = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'cells.json') -Raw | ConvertFrom-Json
$allCells = @($matrix.cells)
$selectedCells = $allCells
if ($null -ne $Cells -and $Cells.Count -ne 0) {
    $known = @($allCells | ForEach-Object { [string] $_.id })
    foreach ($id in $Cells) {
        if ($known -cnotcontains $id) {
            throw "Unknown cell id: $id"
        }
    }
    $selectedCells = @($allCells | Where-Object { $Cells -ccontains [string] $_.id })
}

# ---- run directory and preflight --------------------------------------------------------------
$stamp = [DateTimeOffset]::Now.ToString('yyyyMMdd-HHmmss', [Globalization.CultureInfo]::InvariantCulture)
$runId = if ($Preflight) { "$stamp-preflight" } else { "$stamp-$($Mode.ToLowerInvariant())" }
$runDirectory = Join-Path $OutputRoot $runId
if (Test-Path -LiteralPath $runDirectory) {
    throw "Run directory already exists: $runDirectory"
}
[void] [System.IO.Directory]::CreateDirectory($runDirectory)

$preflightRecord = Get-PreflightRecord -Binary $Binary -ExpectedCommit $ExpectedCommit -EnvironmentId $EnvironmentId `
    -TestRoot $TestRoot -HarnessRoot $PSScriptRoot
$evidence = [ordered]@{
    SchemaVersion = 1
    RunId = $runId
    Mode = if ($Preflight) { 'Preflight' } else { $Mode }
    OwnedProfile = [bool] $OwnedProfile
    StartedAt = [DateTimeOffset]::Now.ToString('o', [Globalization.CultureInfo]::InvariantCulture)
    EndedAt = $null
    Stopped = $null
    InputStop = $null
    Preflight = $preflightRecord
    PlannedCells = @($selectedCells | ForEach-Object { [string] $_.id })
    Cells = [System.Collections.Generic.List[object]]::new()
    SettingsAfter = $null
    TestRootAfter = $null
}

function Save-RunEvidence {
    $evidence.EndedAt = [DateTimeOffset]::Now.ToString('o', [Globalization.CultureInfo]::InvariantCulture)
    $evidence.InputStop = Get-UiEvidenceInputStop
    Write-Utf8NoBom -Path (Join-Path $runDirectory 'evidence.json') -Text ($evidence | ConvertTo-Json -Depth 16)
    Write-Utf8NoBom -Path (Join-Path $runDirectory 'summary.md') -Text (ConvertTo-SummaryMarkdown -Evidence $evidence)
}

if (-not $preflightRecord.Binary.Exists) {
    $evidence.Stopped = 'binary-missing'
}
elseif (-not $preflightRecord.CommitCheck.Match) {
    $evidence.Stopped = "expected-commit:$($preflightRecord.CommitCheck.Reason)"
}
if ($null -ne $evidence.Stopped -or $Preflight) {
    Save-RunEvidence
    Write-Host "UiEvidence preflight: $runDirectory"
    if ($null -ne $evidence.Stopped) {
        Write-Host "UiEvidence stopped before start: $($evidence.Stopped)"
        exit 1
    }
    exit 0
}

# ---- run --------------------------------------------------------------------------------------
Initialize-UiEvidenceNative
if (-not (Test-Path -LiteralPath $TestRoot)) {
    [void] [System.IO.Directory]::CreateDirectory($TestRoot)
}
$settingsStart = Get-SettingsSnapshot
$settingsDirectoryCreated = $false
if ($OwnedProfile -and $settingsStart.Exists) {
    # A recovery copy for the operator in case the restoring finally block cannot run.
    [System.IO.File]::WriteAllBytes((Join-Path $runDirectory 'settings-start.bin'), $settingsStart.Bytes)
}
$run = [pscustomobject]@{
    Commit = $preflightRecord.Binary.Commit
    BinarySha256 = $preflightRecord.Binary.Sha256
    EnvironmentId = $EnvironmentId
    HighContrast = $preflightRecord.HighContrast
}
$narrow = $matrix.narrowWindowDip

function Set-OwnedScheme {
    param([string] $Scheme)

    $document = $null
    if ($settingsStart.Exists) {
        try {
            $document = [System.Text.Encoding]::UTF8.GetString($settingsStart.Bytes) | ConvertFrom-Json
        }
        catch {
            $document = $null
        }
    }
    if ($null -eq $document) {
        $document = [pscustomobject]@{ schemaVersion = 2; showHiddenItems = $false; colorScheme = $Scheme; bookmarkCategories = @(); bookmarks = @() }
    }
    else {
        $document.colorScheme = $Scheme
    }
    $settingsPath = Get-SettingsDocumentPath
    $directory = Split-Path -Parent $settingsPath
    if (-not (Test-Path -LiteralPath $directory)) {
        [void] [System.IO.Directory]::CreateDirectory($directory)
        $script:settingsDirectoryCreated = $true
    }
    $json = $document | ConvertTo-Json -Depth 8 -Compress
    [System.IO.File]::WriteAllBytes($settingsPath, [System.Text.UTF8Encoding]::new($false).GetBytes($json))
}

function Restore-SettingsStart {
    <# Returns the owned profile's settings document to its recorded start bytes or absence. #>
    $settingsPath = Get-SettingsDocumentPath
    if ($settingsStart.Exists) {
        [System.IO.File]::WriteAllBytes($settingsPath, $settingsStart.Bytes)
    }
    elseif (Test-Path -LiteralPath $settingsPath -PathType Leaf) {
        Remove-Item -LiteralPath $settingsPath -Force
        $settingsParent = Split-Path -Parent $settingsPath
        if ($script:settingsDirectoryCreated -and @(Get-ChildItem -LiteralPath $settingsParent -Force).Count -eq 0) {
            Remove-Item -LiteralPath $settingsParent -Force
            $script:settingsDirectoryCreated = $false
        }
    }
}

function Complete-Cell {
    param($Record, [string] $Status, [string] $Reason)

    $Record.Status = $Status
    $Record.Reason = $Reason
    $Record.EndedAt = [DateTimeOffset]::Now.ToString('o', [Globalization.CultureInfo]::InvariantCulture)
    $evidence.Cells.Add($Record)
    Write-Host ("UiEvidence cell={0} status={1} reason={2}" -f $Record.Id, $Status, $Reason)
}

function Set-OwnedFacts {
    param($Record, $Owned)

    $Record.OwnedProcessId = $Owned.Process.Id
    $Record.OwnedHwnd = ('0x{0:X}' -f $Owned.Hwnd.ToInt64())
}

function Invoke-ObserveCell {
    param($Cell, $Record)

    $failures = [System.Collections.Generic.List[string]]::new()
    if ([string] $Cell.scheme -ceq '@current') {
        if ($OwnedProfile) {
            Restore-SettingsStart
        }
        $Record.Scheme = Get-SettingsScheme -Snapshot $settingsStart
    }
    else {
        Set-OwnedScheme -Scheme ([string] $Cell.scheme)
        $Record.Scheme = [string] $Cell.scheme
    }

    $owned = Start-OwnedProcess -Binary $Binary -WorkingDirectory $TestRoot
    try {
        if ($owned.Reason -cne 'ready') {
            if ($owned.Hwnd -ne [IntPtr]::Zero) { Set-OwnedFacts -Record $Record -Owned $owned }
            return $owned.Reason
        }
        Set-OwnedFacts -Record $Record -Owned $owned
        if ([string] $Cell.layout -ceq 'narrow') {
            if (-not (Set-NarrowWindow -Owned $owned -WidthDip ([double] $narrow.width) -HeightDip ([double] $narrow.height))) {
                $failures.Add('narrow-resize-refused')
            }
        }
        $bounds = Get-WindowBoundsRecord -Owned $owned
        $Record.Window = $bounds
        $Record.Dpi = $bounds.Dpi
        $Record.UiaBounds = $bounds.Uia
        if (-not $bounds.ReadsAgree) { $failures.Add('window-reads-disagree') }
        if ([string] $Cell.layout -ceq 'narrow' -and
            ([Math]::Abs($bounds.Dip.Width - [double] $narrow.width) -gt 1 -or [Math]::Abs($bounds.Dip.Height - [double] $narrow.height) -gt 1)) {
            $failures.Add('narrow-size-not-reached')
        }
        $layout = Get-LayoutMeasurement -Owned $owned -Layout $matrix.layout
        $Record.Layout = $layout
        $Record.UiaFocus = $layout.Focus
        foreach ($failure in $layout.Failures) { $failures.Add("layout:$failure") }
        $shot = Save-OwnedScreenshot -Owned $owned -Directory $runDirectory -Name ([string] $Cell.id)
        $Record.Screenshots = @($shot)
        if ($null -eq $shot.Sha256) { $failures.Add("screenshot:$($shot.CaptureMethod)") }
    }
    finally {
        $Record.Close = Stop-OwnedProcess -Owned $owned
    }
    if (-not $Record.Close.Exited -or $Record.Close.Killed) { $failures.Add('close-not-clean') }
    elseif ($Record.Close.ExitCode -ne 0) { $failures.Add("exit-code:$($Record.Close.ExitCode)") }
    return ($failures -join ';')
}

function Wait-AfterExpectation {
    param([hashtable] $Expected, [int] $TimeoutMilliseconds = 3000)

    $deadline = [DateTimeOffset]::Now.AddMilliseconds($TimeoutMilliseconds)
    do {
        Start-Sleep -Milliseconds 150
        $observed = Get-InputObservation -Expected $Expected
        $verdict = Test-InputAdmission -Observed $observed -Expected $Expected
        if ($verdict.Admit) {
            return [pscustomobject]@{ Met = $true; Reason = 'met'; Observed = $observed }
        }
    }
    while ([DateTimeOffset]::Now -lt $deadline)
    return [pscustomobject]@{ Met = $false; Reason = $verdict.Reason; Observed = $observed }
}

function Invoke-InputCell {
    param($Cell, $Record)

    $failures = [System.Collections.Generic.List[string]]::new()
    $Record.Scheme = Get-SettingsScheme -Snapshot $settingsStart
    $owned = Start-OwnedProcess -Binary $Binary -WorkingDirectory $TestRoot
    $steps = [System.Collections.Generic.List[object]]::new()
    try {
        if ($owned.Reason -cne 'ready') {
            if ($owned.Hwnd -ne [IntPtr]::Zero) { Set-OwnedFacts -Record $Record -Owned $owned }
            return $owned.Reason
        }
        Set-OwnedFacts -Record $Record -Owned $owned
        $ownerFacts = @{ RootHwnd = $owned.Hwnd.ToInt64(); ProcessId = $owned.Process.Id; SessionId = $owned.Process.SessionId }

        # The harness never activates a window; it only waits for the owned window to be foreground.
        $deadline = [DateTimeOffset]::Now.AddSeconds(30)
        $isForeground = $false
        while ([DateTimeOffset]::Now -lt $deadline) {
            $probe = Get-InputObservation -Expected @{ RootHwnd = 0; Present = @(); Absent = @() }
            if ($probe.ForegroundRootHwnd -eq $ownerFacts.RootHwnd) { $isForeground = $true; break }
            Start-Sleep -Milliseconds 250
        }
        if (-not $isForeground) {
            $Record.Status = 'SKIP'
            return 'skip:owned-window-not-foreground'
        }
        $entry = Get-FocusOwner
        $entryFileList = if ($entry.ProcessId -eq $owned.Process.Id -and $entry.AutomationId -in @('LeftFileList', 'RightFileList')) { $entry.AutomationId } else { $null }
        if ($null -eq $entryFileList) {
            return 'entry-focus-not-file-list'
        }

        foreach ($step in @($Cell.steps)) {
            $expects = $step.expects
            $before = Resolve-StepExpectation -Block $expects.before -Owner $ownerFacts -EntryFileList $entryFileList
            $after = Resolve-StepExpectation -Block $expects.after -Owner $ownerFacts -EntryFileList $entryFileList
            $opensModal = if ($null -ne $expects.PSObject.Properties['opensModal']) { [string] $expects.opensModal } else { $null }
            $stepRecord = [ordered]@{ Id = [string] $step.id; Key = [string] $step.key; Modifiers = @($step.modifiers); Sent = $null; After = $null; Measure = $null; Screenshot = $null; Result = $null }
            $boundsBefore = Get-WindowBoundsRecord -Owned $owned
            try {
                $stepRecord.Sent = Send-AdmittedKey -Key ([string] $step.key) -Modifiers @($step.modifiers | ForEach-Object { [string] $_ }) `
                    -Expected $before -OpensModal $opensModal
            }
            catch {
                if ($_.FullyQualifiedErrorId -like 'UiEvidenceStop*') {
                    $stepRecord.Result = "stop:$(Get-UiEvidenceInputStop)"
                    $steps.Add($stepRecord)
                    $failures.Add("stop:$(Get-UiEvidenceInputStop)")
                    break
                }
                throw
            }
            $afterResult = Wait-AfterExpectation -Expected $after
            $stepRecord.After = [ordered]@{ Met = $afterResult.Met; Reason = $afterResult.Reason; Focus = $afterResult.Observed.FocusedAutomationId; FocusRaw = $afterResult.Observed.FocusedRawAutomationId; FocusRect = $afterResult.Observed.FocusedRect; Present = $afterResult.Observed.PresentAutomationIds }
            if (-not $afterResult.Met) {
                $stepRecord.Result = "after:$($afterResult.Reason)"
                $steps.Add($stepRecord)
                $failures.Add("$($step.id):after:$($afterResult.Reason)")
                break
            }
            $measure = if ($null -ne $step.PSObject.Properties['measure']) { $step.measure } else { $null }
            if ($null -ne $measure -and [string] $measure.kind -ceq 'move') {
                Start-Sleep -Milliseconds 250
                $boundsAfter = Get-WindowBoundsRecord -Owned $owned
                $stepPixels = Get-StepPixels -Dpi $boundsBefore.Dpi
                $delta = [ordered]@{
                    Left = $boundsAfter.Physical.Left - $boundsBefore.Physical.Left
                    Top = $boundsAfter.Physical.Top - $boundsBefore.Physical.Top
                    Width = $boundsAfter.Physical.Width - $boundsBefore.Physical.Width
                    Height = $boundsAfter.Physical.Height - $boundsBefore.Physical.Height
                }
                $moved = ($delta.Left -eq [int] $measure.dx * $stepPixels -and $delta.Top -eq [int] $measure.dy * $stepPixels -and
                    $delta.Width -eq 0 -and $delta.Height -eq 0 -and $boundsBefore.ReadsAgree -and $boundsAfter.ReadsAgree)
                $stepRecord.Measure = [ordered]@{ Kind = 'move'; Dpi = $boundsBefore.Dpi; StepPixels = $stepPixels; Before = $boundsBefore; After = $boundsAfter; Delta = $delta; Pass = $moved }
                if (-not $moved) { $failures.Add("$($step.id):move-delta-mismatch") }
            }
            elseif ($null -ne $measure -and [string] $measure.kind -ceq 'addressUnfocused') {
                $focusedAddress = @('LeftAddress', 'RightAddress' | Where-Object {
                        $element = Find-OwnedElement -Window $owned.Window -AutomationId $_
                        $null -ne $element -and [bool] $element.Current.HasKeyboardFocus
                    })
                $stepRecord.Measure = [ordered]@{ Kind = 'addressUnfocused'; FocusedAddresses = $focusedAddress; Pass = $focusedAddress.Count -eq 0 }
                if ($focusedAddress.Count -ne 0) { $failures.Add("$($step.id):address-focused") }
            }
            if ($null -ne $step.PSObject.Properties['screenshot']) {
                $shot = Save-OwnedScreenshot -Owned $owned -Directory $runDirectory -Name "$($Cell.id)-$($step.screenshot)"
                $stepRecord.Screenshot = $shot
                $Record.Screenshots = @($Record.Screenshots) + @($shot)
                if ($null -eq $shot.Sha256) { $failures.Add("$($step.id):screenshot:$($shot.CaptureMethod)") }
            }
            $stepRecord.Result = 'done'
            $steps.Add($stepRecord)
        }
        $bounds = Get-WindowBoundsRecord -Owned $owned
        $Record.Window = $bounds
        $Record.Dpi = $bounds.Dpi
        $Record.UiaBounds = $bounds.Uia
        $finalFocus = Get-FocusOwner
        $Record.UiaFocus = [ordered]@{ AutomationId = $finalFocus.AutomationId; RawAutomationId = $finalFocus.RawAutomationId; ProcessId = $finalFocus.ProcessId; Rect = $finalFocus.Rect }
    }
    finally {
        $Record.Steps = @($steps)
        $Record.Close = Stop-OwnedProcess -Owned $owned
    }
    if (-not $Record.Close.Exited -or $Record.Close.Killed) { $failures.Add('close-not-clean') }
    elseif ($Record.Close.ExitCode -ne 0) { $failures.Add("exit-code:$($Record.Close.ExitCode)") }
    return ($failures -join ';')
}

$exitCode = 0
try {
    foreach ($cell in $selectedCells) {
        $record = New-CellRecord -Cell $cell -Run $run
        if ([string] $cell.status -cne 'active') {
            Complete-Cell -Record $record -Status 'SKIP' -Reason ([string] $cell.reason)
            continue
        }
        if ([string] $cell.mode -cne $Mode) {
            Complete-Cell -Record $record -Status 'SKIP' -Reason 'mode-not-selected'
            continue
        }
        if ($null -ne $cell.PSObject.Properties['requiresOwnedProfile'] -and [bool] $cell.requiresOwnedProfile -and -not $OwnedProfile) {
            Complete-Cell -Record $record -Status 'SKIP' -Reason 'profile-not-owned'
            continue
        }
        if ($null -ne (Get-UiEvidenceInputStop)) {
            Complete-Cell -Record $record -Status 'SKIP' -Reason 'input-stopped'
            continue
        }
        $result = if ($Mode -eq 'Observe') { Invoke-ObserveCell -Cell $cell -Record $record } else { Invoke-InputCell -Cell $cell -Record $record }
        if ([string]::IsNullOrEmpty($result)) {
            Complete-Cell -Record $record -Status 'PASS' -Reason $null
        }
        elseif ($result.StartsWith('skip:', [StringComparison]::Ordinal)) {
            Complete-Cell -Record $record -Status 'SKIP' -Reason $result.Substring(5)
        }
        else {
            Complete-Cell -Record $record -Status 'FAIL' -Reason $result
            $exitCode = 1
        }
    }
}
catch {
    $evidence.Stopped = "harness-error:$($_.Exception.Message)"
    $exitCode = 1
}
finally {
    if ($OwnedProfile) {
        Restore-SettingsStart
    }
    $settingsEnd = Get-SettingsSnapshot
    $evidence.SettingsAfter = [ordered]@{
        Path = $settingsEnd.Path
        Exists = $settingsEnd.Exists
        Length = $settingsEnd.Length
        Sha256 = $settingsEnd.Sha256
        Restored = [bool] $OwnedProfile
        MatchesStart = ($settingsEnd.Exists -eq $settingsStart.Exists -and $settingsEnd.Sha256 -eq $settingsStart.Sha256)
    }
    if (-not $evidence.SettingsAfter.MatchesStart) {
        $exitCode = 1
        if ($null -eq $evidence.Stopped) { $evidence.Stopped = 'settings-not-at-start-state' }
    }
    $entries = @(Get-ChildItem -LiteralPath $TestRoot -Force -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
            [System.IO.Path]::GetRelativePath($TestRoot, $_.FullName)
        })
    $evidence.TestRootAfter = [ordered]@{ Path = $TestRoot; Entries = $entries }
    if ($null -ne (Get-UiEvidenceInputStop)) { $exitCode = 1 }
    Save-RunEvidence
    Write-Host "UiEvidence record: $runDirectory"
}
exit $exitCode
