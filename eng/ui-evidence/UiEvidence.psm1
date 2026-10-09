Set-StrictMode -Version Latest

# Issue #169: functions for the Issue #94 UI release evidence harness. This module is an
# environment-tier recorder outside eng/check.ps1. The admission decisions are pure functions
# that take observed values only, so eng/ui-evidence/selftest.ps1 proves them without Win32.
# Every Win32 and UI Automation read lives in a separate function, and the sole synthetic-input
# path is Send-AdmittedKey, which re-observes and re-admits immediately before every send.

$script:InputStopReason = $null

# Keys the harness never sends: they start or confirm a file operation or change the selection.
$script:RefusedKeys = @('F5', 'F6', 'Delete', 'Enter', 'Space')

# Keys that open a modal or editor. A guarded key is admitted only when the step's expects block
# names the modal that the key opens; otherwise it is refused exactly like a refused key.
$script:GuardedKeys = @{
    F2 = 'NameEntry'
    F7 = 'NameEntry'
    F8 = $null
}

$script:VirtualKeys = @{
    Control = 0x11
    Shift = 0x10
    Alt = 0x12
    Escape = 0x1B
    F2 = 0x71
    F5 = 0x74
    F6 = 0x75
    F7 = 0x76
    F8 = 0x77
    Delete = 0x2E
    Enter = 0x0D
    Space = 0x20
    H = 0x48
    J = 0x4A
    K = 0x4B
    L = 0x4C
    W = 0x57
}

$script:FocusOwnerIds = @(
    'LeftFileList', 'RightFileList', 'LeftAddress', 'RightAddress', 'WindowAdjustmentHelper',
    'NameEntry', 'CommandPaletteSearch', 'SettingsModal', 'BookmarkManagerModal',
    'TransferConflictModal')

$script:NativeLoaded = $false

#region Pure decisions (no Win32, no UI Automation)

function Test-InputAdmission {
    <#
    .SYNOPSIS
      Decides whether one synthetic key may be sent. Pure: it reads only the two hashtables.
    .OUTPUTS
      [pscustomobject] with Admit (bool) and Reason (string).
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable] $Observed,

        [Parameter(Mandatory)]
        [hashtable] $Expected
    )

    foreach ($name in @('RootHwnd', 'ProcessId', 'SessionId', 'FocusedAutomationIds')) {
        if (-not $Expected.ContainsKey($name) -or $null -eq $Expected[$name]) {
            return [pscustomobject]@{ Admit = $false; Reason = "expectation-incomplete:$name" }
        }
    }
    if ([long] $Expected.RootHwnd -eq 0 -or [long] $Expected.ProcessId -le 0 -or
        @($Expected.FocusedAutomationIds).Count -eq 0) {
        return [pscustomobject]@{ Admit = $false; Reason = 'expectation-invalid' }
    }
    foreach ($name in @('ForegroundRootHwnd', 'ForegroundProcessId', 'ForegroundSessionId',
            'FocusedProcessId', 'FocusedAutomationId', 'PresentAutomationIds')) {
        if (-not $Observed.ContainsKey($name)) {
            return [pscustomobject]@{ Admit = $false; Reason = "observation-incomplete:$name" }
        }
    }

    if ($null -eq $Observed.ForegroundRootHwnd -or
        [long] $Observed.ForegroundRootHwnd -ne [long] $Expected.RootHwnd) {
        return [pscustomobject]@{ Admit = $false; Reason = 'foreground-window-mismatch' }
    }
    if ($null -eq $Observed.ForegroundProcessId -or
        [long] $Observed.ForegroundProcessId -ne [long] $Expected.ProcessId) {
        return [pscustomobject]@{ Admit = $false; Reason = 'foreground-process-mismatch' }
    }
    if ($null -eq $Observed.ForegroundSessionId -or
        [long] $Observed.ForegroundSessionId -ne [long] $Expected.SessionId) {
        return [pscustomobject]@{ Admit = $false; Reason = 'session-mismatch' }
    }
    if ($null -eq $Observed.FocusedProcessId -or
        [long] $Observed.FocusedProcessId -ne [long] $Expected.ProcessId) {
        return [pscustomobject]@{ Admit = $false; Reason = 'focus-process-mismatch' }
    }
    $focused = [string] $Observed.FocusedAutomationId
    if ([string]::IsNullOrEmpty($focused) -or
        @($Expected.FocusedAutomationIds | Where-Object { [string] $_ -ceq $focused }).Count -eq 0) {
        return [pscustomobject]@{ Admit = $false; Reason = 'focus-control-mismatch' }
    }

    $present = @($Observed.PresentAutomationIds | ForEach-Object { [string] $_ })
    if ($Expected.ContainsKey('Present')) {
        foreach ($id in @($Expected.Present)) {
            if ($present -cnotcontains [string] $id) {
                return [pscustomobject]@{ Admit = $false; Reason = "mode-element-missing:$id" }
            }
        }
    }
    if ($Expected.ContainsKey('Absent')) {
        foreach ($id in @($Expected.Absent)) {
            if ($present -ccontains [string] $id) {
                return [pscustomobject]@{ Admit = $false; Reason = "unexpected-element-present:$id" }
            }
        }
    }

    return [pscustomobject]@{ Admit = $true; Reason = 'admitted' }
}

function Test-KeyAdmission {
    <#
    .SYNOPSIS
      Decides whether a key is sendable at all for a step. Pure.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Key,

        [Parameter()]
        [string[]] $Modifiers = @(),

        [Parameter()]
        [AllowNull()]
        [string] $OpensModal
    )

    if (-not $script:VirtualKeys.ContainsKey($Key) -or $Key -in @('Control', 'Shift', 'Alt')) {
        return [pscustomobject]@{ Admit = $false; Reason = "key-unknown:$Key" }
    }
    foreach ($modifier in $Modifiers) {
        if ($modifier -cnotin @('Control', 'Shift', 'Alt')) {
            return [pscustomobject]@{ Admit = $false; Reason = "modifier-unknown:$modifier" }
        }
    }
    if ($script:RefusedKeys -ccontains $Key) {
        return [pscustomobject]@{ Admit = $false; Reason = "file-operation-key-refused:$Key" }
    }
    if ($script:GuardedKeys.ContainsKey($Key)) {
        $declared = $script:GuardedKeys[$Key]
        if ([string]::IsNullOrEmpty($OpensModal)) {
            return [pscustomobject]@{ Admit = $false; Reason = "guarded-key-without-expected-modal:$Key" }
        }
        if ($null -eq $declared) {
            return [pscustomobject]@{ Admit = $false; Reason = "guarded-key-modal-not-exposed:$Key" }
        }
        if ($OpensModal -cne $declared) {
            return [pscustomobject]@{ Admit = $false; Reason = "guarded-key-modal-mismatch:$Key" }
        }
    }

    return [pscustomobject]@{ Admit = $true; Reason = 'admitted' }
}

function Get-StepPixels {
    <# One window-adjustment step in physical pixels for a window DPI (ADR-0050). Pure. #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [int] $Dpi
    )

    return [int] [Math]::Round(32.0 * $Dpi / 96.0, [MidpointRounding]::AwayFromZero)
}

function ConvertTo-PhysicalPixels {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [double] $Dip,

        [Parameter(Mandatory)]
        [int] $Dpi
    )

    return [int] [Math]::Round($Dip * $Dpi / 96.0, [MidpointRounding]::AwayFromZero)
}

function Test-CommitMatch {
    <#
    .SYNOPSIS
      Compares the binary ProductVersion "+<sha>" suffix with an expected commit prefix. Pure.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [AllowNull()]
        [AllowEmptyString()]
        [string] $ProductVersion,

        [Parameter()]
        [AllowNull()]
        [AllowEmptyString()]
        [string] $ExpectedCommit
    )

    $commit = $null
    if (-not [string]::IsNullOrEmpty($ProductVersion)) {
        $match = [regex]::Match($ProductVersion, '\+(?<sha>[0-9a-fA-F]{7,40})(?:\.|$)')
        if ($match.Success) {
            $commit = $match.Groups['sha'].Value.ToLowerInvariant()
        }
    }
    if ([string]::IsNullOrEmpty($ExpectedCommit)) {
        return [pscustomobject]@{ Match = $true; Commit = $commit; Reason = 'not-requested' }
    }
    if ($ExpectedCommit -notmatch '^[0-9a-fA-F]{7,40}$') {
        return [pscustomobject]@{ Match = $false; Commit = $commit; Reason = 'expected-commit-invalid' }
    }
    if ($null -eq $commit) {
        return [pscustomobject]@{ Match = $false; Commit = $null; Reason = 'binary-commit-absent' }
    }
    $expected = $ExpectedCommit.ToLowerInvariant()
    if (-not $commit.StartsWith($expected, [StringComparison]::Ordinal) -and
        -not $expected.StartsWith($commit, [StringComparison]::Ordinal)) {
        return [pscustomobject]@{ Match = $false; Commit = $commit; Reason = 'binary-commit-mismatch' }
    }
    return [pscustomobject]@{ Match = $true; Commit = $commit; Reason = 'matched' }
}

function Test-RectsDisjoint {
    param($A, $B)
    return ($A.Right -le $B.Left -or $B.Right -le $A.Left -or $A.Bottom -le $B.Top -or $B.Bottom -le $A.Top)
}

function Test-RectInside {
    param($Inner, $Outer, [int] $Tolerance = 1)
    return ($Inner.Left -ge $Outer.Left - $Tolerance -and $Inner.Top -ge $Outer.Top - $Tolerance -and
        $Inner.Right -le $Outer.Right + $Tolerance -and $Inner.Bottom -le $Outer.Bottom + $Tolerance)
}

function Test-LayoutVerdict {
    <#
    .SYNOPSIS
      Judges visible, positive, non-overlapping, and non-clipped elements from measured rects. Pure.
    .PARAMETER Elements
      Hashtable of AutomationId -> $null (absent) or @{ Left; Top; Right; Bottom; Offscreen }.
    .PARAMETER Window
      The owned window rect @{ Left; Top; Right; Bottom } in the same physical coordinates.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable] $Elements,

        [Parameter(Mandatory)]
        [System.Collections.IDictionary] $Window,

        [Parameter()]
        [string[]] $Required = @(),

        [Parameter()]
        [object[]] $DisjointPairs = @(),

        [Parameter()]
        [object[]] $ContainedIn = @()
    )

    $failures = [System.Collections.Generic.List[string]]::new()
    foreach ($id in $Required) {
        $rect = if ($Elements.ContainsKey($id)) { $Elements[$id] } else { $null }
        if ($null -eq $rect) {
            $failures.Add("absent:$id")
            continue
        }
        if ($rect.Contains('Offscreen') -and [bool] $rect.Offscreen) {
            $failures.Add("offscreen:$id")
        }
        if ($rect.Right - $rect.Left -le 0 -or $rect.Bottom - $rect.Top -le 0) {
            $failures.Add("non-positive:$id")
            continue
        }
        if (-not (Test-RectInside -Inner $rect -Outer $Window)) {
            $failures.Add("clipped-by-window:$id")
        }
    }
    foreach ($pair in $DisjointPairs) {
        $first = $Elements[[string] $pair[0]]
        $second = $Elements[[string] $pair[1]]
        if ($null -ne $first -and $null -ne $second -and -not (Test-RectsDisjoint -A $first -B $second)) {
            $failures.Add("overlap:$($pair[0])+$($pair[1])")
        }
    }
    foreach ($pair in $ContainedIn) {
        $inner = $Elements[[string] $pair[0]]
        $outer = $Elements[[string] $pair[1]]
        if ($null -ne $inner -and $null -ne $outer -and -not (Test-RectInside -Inner $inner -Outer $outer)) {
            $failures.Add("clipped:$($pair[0])-in-$($pair[1])")
        }
    }

    return [pscustomobject]@{
        Pass = $failures.Count -eq 0
        Failures = @($failures)
    }
}

function Resolve-StepExpectation {
    <#
    .SYNOPSIS
      Turns a cells.json expects block into an admission expectation for the owned process. Pure.
      "@entryFileList" is replaced by the file list that owned focus when the cell started.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        $Block,

        [Parameter(Mandatory)]
        [hashtable] $Owner,

        [Parameter()]
        [AllowNull()]
        [string] $EntryFileList
    )

    $focus = @(@($Block.focus) | ForEach-Object {
            if ([string] $_ -ceq '@entryFileList') { $EntryFileList } else { [string] $_ }
        } | Where-Object { -not [string]::IsNullOrEmpty($_) })
    return @{
        RootHwnd = $Owner.RootHwnd
        ProcessId = $Owner.ProcessId
        SessionId = $Owner.SessionId
        FocusedAutomationIds = $focus
        Present = @(@($Block.present) | Where-Object { $null -ne $_ } | ForEach-Object { [string] $_ })
        Absent = @(@($Block.absent) | Where-Object { $null -ne $_ } | ForEach-Object { [string] $_ })
    }
}

#endregion

#region Guarded input

function Get-UiEvidenceInputStop {
    <# The latched stop reason, or $null while input is still permitted. #>
    return $script:InputStopReason
}

function Stop-UiEvidenceInput {
    param([Parameter(Mandatory)] [string] $Reason)

    if ($null -eq $script:InputStopReason) {
        $script:InputStopReason = $Reason
    }
    $exception = [System.InvalidOperationException]::new("UiEvidenceStop: $Reason")
    throw [System.Management.Automation.ErrorRecord]::new(
        $exception, 'UiEvidenceStop', [System.Management.Automation.ErrorCategory]::OperationStopped, $Reason)
}

function Send-AdmittedKey {
    <#
    .SYNOPSIS
      The sole synthetic-input path. Re-observes, re-admits, then sends one atomic chord.
    .DESCRIPTION
      The chord (modifiers down, key down, key up, modifiers up) goes out in one SendInput call,
      so no key is ever left held. There is no held-key function. When any admission fails, the
      stop is latched for the rest of the process and no further SendInput is made, including no
      key-up cleanup. Observer and Sender are injectable so selftest.ps1 can prove the stop path
      without Win32.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Key,

        [Parameter()]
        [string[]] $Modifiers = @(),

        [Parameter(Mandatory)]
        [hashtable] $Expected,

        [Parameter()]
        [AllowNull()]
        [string] $OpensModal,

        [Parameter()]
        [scriptblock] $Observer = { param($expectation) Get-InputObservation -Expected $expectation },

        [Parameter()]
        [scriptblock] $Sender = { param($modifierCodes, $keyCode) Send-NativeChord -ModifierCodes $modifierCodes -KeyCode $keyCode }
    )

    if ($null -ne $script:InputStopReason) {
        Stop-UiEvidenceInput -Reason $script:InputStopReason
    }

    $keyAdmission = Test-KeyAdmission -Key $Key -Modifiers $Modifiers -OpensModal $OpensModal
    if (-not $keyAdmission.Admit) {
        Stop-UiEvidenceInput -Reason $keyAdmission.Reason
    }

    $observed = $null
    try {
        $observed = & $Observer $Expected
    }
    catch {
        Stop-UiEvidenceInput -Reason 'observation-failed'
    }
    if ($observed -isnot [hashtable]) {
        Stop-UiEvidenceInput -Reason 'observation-failed'
    }
    $admission = Test-InputAdmission -Observed $observed -Expected $Expected
    if (-not $admission.Admit) {
        Stop-UiEvidenceInput -Reason $admission.Reason
    }

    $modifierCodes = [uint16[]] @($Modifiers | ForEach-Object { $script:VirtualKeys[$_] })
    $keyCode = [uint16] $script:VirtualKeys[$Key]
    $expectedEvents = 2 * ($modifierCodes.Count + 1)
    $sentAt = [DateTimeOffset]::Now
    $sent = & $Sender $modifierCodes $keyCode
    if ([int] $sent -ne $expectedEvents) {
        # A partial SendInput is not repaired; repairing would be blind input.
        Stop-UiEvidenceInput -Reason "send-incomplete:$sent/$expectedEvents"
    }

    return [pscustomobject]@{
        Key = $Key
        Modifiers = @($Modifiers)
        SentAt = $sentAt.ToString('o', [Globalization.CultureInfo]::InvariantCulture)
        Events = $expectedEvents
        Admission = $admission.Reason
        Observed = $observed
    }
}

#endregion

#region Win32 and UI Automation readers

function Initialize-UiEvidenceNative {
    if ($script:NativeLoaded) {
        return
    }
    Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
    if (-not ('NeNeUiEvidenceNative' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class NeNeUiEvidenceNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT { public uint type; public InputUnion u; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFOEX
    {
        public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIGHCONTRAST { public uint cbSize; public uint dwFlags; public IntPtr lpszDefaultScheme; }

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA { public uint cbSize; public IntPtr hWnd; public uint uCallbackMessage; public uint uEdge; public RECT rc; public IntPtr lParam; }

    public sealed class MonitorRecord
    {
        public string Device; public RECT Bounds; public RECT Work; public bool Primary; public uint DpiX; public uint DpiY;
    }

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);
    private delegate bool WindowEnumProc(IntPtr hwnd, IntPtr data);

    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("kernel32.dll")] public static extern bool ProcessIdToSessionId(uint processId, out uint sessionId);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint code, uint mapType);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowEnumProc callback, IntPtr data);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hwnd, uint command);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFOEX info);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint dpiX, out uint dpiY);
    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")] private static extern bool SystemParametersInfoHighContrast(uint action, uint size, ref HIGHCONTRAST value, uint flags);
    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")] private static extern bool SystemParametersInfoBool(uint action, uint size, out int value, uint flags);
    [DllImport("shell32.dll")] private static extern IntPtr SHAppBarMessage(uint message, ref APPBARDATA data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string title);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    public static uint SendChord(ushort[] modifiers, ushort key)
    {
        int count = 2 * (modifiers.Length + 1);
        INPUT[] inputs = new INPUT[count];
        int index = 0;
        foreach (ushort modifier in modifiers) { inputs[index++] = Key(modifier, false); }
        inputs[index++] = Key(key, false);
        inputs[index++] = Key(key, true);
        for (int i = modifiers.Length - 1; i >= 0; i--) { inputs[index++] = Key(modifiers[i], true); }
        return SendInput((uint)count, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    private static INPUT Key(ushort vk, bool up)
    {
        INPUT input = new INPUT();
        input.type = 1;
        input.u.ki.wVk = vk;
        input.u.ki.wScan = (ushort)MapVirtualKey(vk, 0);
        input.u.ki.dwFlags = up ? 0x2u : 0u;
        return input;
    }

    public static bool IsUniform(byte[] buffer)
    {
        for (int i = 4; i + 3 < buffer.Length; i += 4)
        {
            if (buffer[i] != buffer[0] || buffer[i + 1] != buffer[1] || buffer[i + 2] != buffer[2] || buffer[i + 3] != buffer[3]) { return false; }
        }
        return true;
    }

    public static IntPtr[] FindTopLevelWindows(uint processId)
    {
        List<IntPtr> found = new List<IntPtr>();
        EnumWindows((hwnd, data) =>
        {
            uint owner;
            GetWindowThreadProcessId(hwnd, out owner);
            if (owner == processId && IsWindowVisible(hwnd) && GetWindow(hwnd, 4) == IntPtr.Zero) { found.Add(hwnd); }
            return true;
        }, IntPtr.Zero);
        return found.ToArray();
    }

    public static MonitorRecord[] GetMonitors()
    {
        List<MonitorRecord> monitors = new List<MonitorRecord>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, hdc, rect, data) =>
        {
            MONITORINFOEX info = new MONITORINFOEX();
            info.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
            if (GetMonitorInfo(monitor, ref info))
            {
                uint dpiX = 0; uint dpiY = 0;
                GetDpiForMonitor(monitor, 0, out dpiX, out dpiY);
                monitors.Add(new MonitorRecord { Device = info.szDevice, Bounds = info.rcMonitor, Work = info.rcWork, Primary = (info.dwFlags & 1u) != 0, DpiX = dpiX, DpiY = dpiY });
            }
            return true;
        }, IntPtr.Zero);
        return monitors.ToArray();
    }

    public static bool IsHighContrastOn(out bool readSucceeded)
    {
        HIGHCONTRAST value = new HIGHCONTRAST();
        value.cbSize = (uint)Marshal.SizeOf(typeof(HIGHCONTRAST));
        readSucceeded = SystemParametersInfoHighContrast(0x0042, value.cbSize, ref value, 0);
        return readSucceeded && (value.dwFlags & 1u) != 0;
    }

    public static bool IsScreenReaderFlagOn(out bool readSucceeded)
    {
        int value;
        readSucceeded = SystemParametersInfoBool(0x0046, 0, out value, 0);
        return readSucceeded && value != 0;
    }

    public static bool GetTaskbar(out RECT rect, out uint edge)
    {
        APPBARDATA data = new APPBARDATA();
        data.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
        bool found = SHAppBarMessage(5, ref data) != IntPtr.Zero;
        rect = data.rc;
        edge = data.uEdge;
        return found;
    }
}
'@
    }
    # Physical pixels for every read in this harness process only (per-monitor aware v2).
    [void] [NeNeUiEvidenceNative]::SetProcessDpiAwarenessContext([IntPtr]::new(-4))
    $script:NativeLoaded = $true
}

function Send-NativeChord {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [uint16[]] $ModifierCodes,

        [Parameter(Mandatory)]
        [uint16] $KeyCode
    )

    Initialize-UiEvidenceNative
    return [int] [NeNeUiEvidenceNative]::SendChord($ModifierCodes, $KeyCode)
}

function ConvertTo-RectRecord {
    param($Left, $Top, $Right, $Bottom)
    return [ordered]@{
        Left = [int] $Left
        Top = [int] $Top
        Right = [int] $Right
        Bottom = [int] $Bottom
        Width = [int] ($Right - $Left)
        Height = [int] ($Bottom - $Top)
    }
}

function Get-WindowRectRecord {
    param([Parameter(Mandatory)] [IntPtr] $Hwnd)

    $rect = New-Object NeNeUiEvidenceNative+RECT
    if (-not [NeNeUiEvidenceNative]::GetWindowRect($Hwnd, [ref] $rect)) {
        return $null
    }
    return ConvertTo-RectRecord $rect.Left $rect.Top $rect.Right $rect.Bottom
}

function Get-OwnedWindowElement {
    param([Parameter(Mandatory)] [IntPtr] $Hwnd)

    return [System.Windows.Automation.AutomationElement]::FromHandle($Hwnd)
}

function Find-OwnedElement {
    param(
        [Parameter(Mandatory)]
        $Window,

        [Parameter(Mandatory)]
        [string] $AutomationId
    )

    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $AutomationId)
    return $Window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Get-ElementRect {
    param([Parameter(Mandatory)] $Element)

    $bounds = $Element.Current.BoundingRectangle
    if ($bounds.IsEmpty) {
        return $null
    }
    $record = ConvertTo-RectRecord ([Math]::Round($bounds.Left)) ([Math]::Round($bounds.Top)) `
        ([Math]::Round($bounds.Right)) ([Math]::Round($bounds.Bottom))
    $record.Offscreen = [bool] $Element.Current.IsOffscreen
    return $record
}

function Get-FocusOwner {
    <# The focused element and the nearest ancestor-or-self whose AutomationId is a known owner. #>
    param()

    $focused = $null
    try {
        $focused = [System.Windows.Automation.AutomationElement]::FocusedElement
    }
    catch {
        return @{ ProcessId = $null; AutomationId = $null; RawAutomationId = $null; Rect = $null }
    }
    if ($null -eq $focused) {
        return @{ ProcessId = $null; AutomationId = $null; RawAutomationId = $null; Rect = $null }
    }
    $processId = [int] $focused.Current.ProcessId
    $raw = [string] $focused.Current.AutomationId
    $rect = Get-ElementRect -Element $focused
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $element = $focused
    $owner = $null
    for ($depth = 0; $null -ne $element -and $depth -lt 32; $depth++) {
        $id = [string] $element.Current.AutomationId
        if ($script:FocusOwnerIds -ccontains $id) {
            $owner = $id
            break
        }
        $element = $walker.GetParent($element)
    }
    return @{ ProcessId = $processId; AutomationId = $owner; RawAutomationId = $raw; Rect = $rect }
}

function Get-InputObservation {
    <# Reads the live values that Test-InputAdmission judges. Read-only Win32 and UIA. #>
    param(
        [Parameter(Mandatory)]
        [hashtable] $Expected
    )

    Initialize-UiEvidenceNative
    $foreground = [NeNeUiEvidenceNative]::GetForegroundWindow()
    $root = if ($foreground -eq [IntPtr]::Zero) { [IntPtr]::Zero } else { [NeNeUiEvidenceNative]::GetAncestor($foreground, 2) }
    [uint32] $processId = 0
    [uint32] $sessionId = 0
    $sessionKnown = $false
    if ($root -ne [IntPtr]::Zero) {
        [void] [NeNeUiEvidenceNative]::GetWindowThreadProcessId($root, [ref] $processId)
        $sessionKnown = [NeNeUiEvidenceNative]::ProcessIdToSessionId($processId, [ref] $sessionId)
    }
    $focus = Get-FocusOwner
    $present = [System.Collections.Generic.List[string]]::new()
    $ids = @(@($Expected.Present) + @($Expected.Absent) | Where-Object { -not [string]::IsNullOrEmpty($_) } | Select-Object -Unique)
    if ($ids.Count -ne 0 -and [long] $Expected.RootHwnd -ne 0) {
        $window = Get-OwnedWindowElement -Hwnd ([IntPtr]::new([long] $Expected.RootHwnd))
        foreach ($id in $ids) {
            $element = Find-OwnedElement -Window $window -AutomationId $id
            if ($null -ne $element) {
                $rect = Get-ElementRect -Element $element
                if ($null -ne $rect -and -not $rect.Offscreen -and $rect.Width -gt 0 -and $rect.Height -gt 0) {
                    $present.Add($id)
                }
            }
        }
    }
    return @{
        ForegroundRootHwnd = $root.ToInt64()
        ForegroundProcessId = if ($root -eq [IntPtr]::Zero) { $null } else { [long] $processId }
        ForegroundSessionId = if ($sessionKnown) { [long] $sessionId } else { $null }
        FocusedProcessId = $focus.ProcessId
        FocusedAutomationId = $focus.AutomationId
        FocusedRawAutomationId = $focus.RawAutomationId
        FocusedRect = $focus.Rect
        PresentAutomationIds = @($present)
    }
}

function Get-FileSha256 {
    param([Parameter(Mandatory)] [string] $Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-BytesSha256 {
    param([Parameter(Mandatory)] [byte[]] $Bytes)

    $hash = [System.Security.Cryptography.SHA256]::HashData($Bytes)
    return [Convert]::ToHexString($hash).ToLowerInvariant()
}

function Get-SettingsDocumentPath {
    <# The same Known Folder as WindowsLocalSettingsLocation.Resolve (DoNotVerify). #>
    $localApplicationData = [Environment]::GetFolderPath(
        [Environment+SpecialFolder]::LocalApplicationData,
        [Environment+SpecialFolderOption]::DoNotVerify)
    return [System.IO.Path]::Join($localApplicationData, 'NeNeCommander', 'settings.json')
}

function Get-SettingsSnapshot {
    <# Bytes or absence of the production settings document, plus its SHA-256. Read-only. #>
    $path = Get-SettingsDocumentPath
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $bytes = [System.IO.File]::ReadAllBytes($path)
        return [pscustomobject]@{ Path = $path; Exists = $true; Length = $bytes.Length; Sha256 = Get-BytesSha256 -Bytes $bytes; Bytes = $bytes }
    }
    return [pscustomobject]@{ Path = $path; Exists = $false; Length = $null; Sha256 = $null; Bytes = $null }
}

function Get-SettingsScheme {
    param($Snapshot)

    if (-not $Snapshot.Exists) {
        return 'default:settings-absent'
    }
    try {
        $document = [System.Text.Encoding]::UTF8.GetString($Snapshot.Bytes) | ConvertFrom-Json
        if ($null -ne $document.PSObject.Properties['colorScheme']) {
            return [string] $document.colorScheme
        }
    }
    catch {
        return 'unknown:settings-unreadable'
    }
    return 'unknown:scheme-not-declared'
}

function Get-PreflightRecord {
    <# Read-only environment facts. Nothing here starts a process, sends input, or writes. #>
    param(
        [Parameter(Mandatory)]
        [string] $Binary,

        [Parameter()]
        [AllowNull()]
        [string] $ExpectedCommit,

        [Parameter()]
        [AllowNull()]
        [string] $EnvironmentId,

        [Parameter(Mandatory)]
        [string] $TestRoot,

        [Parameter(Mandatory)]
        [string] $HarnessRoot
    )

    Initialize-UiEvidenceNative

    $currentVersion = Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -ErrorAction SilentlyContinue
    $os = [ordered]@{
        Version = [Environment]::OSVersion.VersionString
        ProductName = if ($null -ne $currentVersion) { [string] $currentVersion.ProductName } else { $null }
        DisplayVersion = if ($null -ne $currentVersion -and $null -ne $currentVersion.PSObject.Properties['DisplayVersion']) { [string] $currentVersion.DisplayVersion } else { $null }
        CurrentBuild = if ($null -ne $currentVersion) { [string] $currentVersion.CurrentBuild } else { $null }
        Ubr = if ($null -ne $currentVersion -and $null -ne $currentVersion.PSObject.Properties['UBR']) { [int] $currentVersion.UBR } else { $null }
    }

    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $session = [ordered]@{
        SessionId = [System.Diagnostics.Process]::GetCurrentProcess().SessionId
        UserSid = $identity.User.Value
        Interactive = [Environment]::UserInteractive
    }
    $identity.Dispose()

    $monitors = @([NeNeUiEvidenceNative]::GetMonitors() | ForEach-Object {
            [ordered]@{
                Device = $_.Device
                Primary = $_.Primary
                Bounds = ConvertTo-RectRecord $_.Bounds.Left $_.Bounds.Top $_.Bounds.Right $_.Bounds.Bottom
                WorkArea = ConvertTo-RectRecord $_.Work.Left $_.Work.Top $_.Work.Right $_.Work.Bottom
                Dpi = [int] $_.DpiX
                ScalePercent = [int] [Math]::Round($_.DpiX * 100.0 / 96.0)
            }
        })

    $highContrastRead = $false
    $highContrast = [NeNeUiEvidenceNative]::IsHighContrastOn([ref] $highContrastRead)
    $screenReaderRead = $false
    $screenReader = [NeNeUiEvidenceNative]::IsScreenReaderFlagOn([ref] $screenReaderRead)

    $taskbarRect = New-Object NeNeUiEvidenceNative+RECT
    [uint32] $taskbarEdge = 0
    $taskbarFound = [NeNeUiEvidenceNative]::GetTaskbar([ref] $taskbarRect, [ref] $taskbarEdge)
    $edgeNames = @('left', 'top', 'right', 'bottom')
    $secondaryTaskbars = [System.Collections.Generic.List[object]]::new()
    $after = [IntPtr]::Zero
    for ($guard = 0; $guard -lt 16; $guard++) {
        $after = [NeNeUiEvidenceNative]::FindWindowEx([IntPtr]::Zero, $after, 'Shell_SecondaryTrayWnd', $null)
        if ($after -eq [IntPtr]::Zero) {
            break
        }
        $secondaryTaskbars.Add((Get-WindowRectRecord -Hwnd $after))
    }
    $taskbar = [ordered]@{
        Found = $taskbarFound
        Bounds = if ($taskbarFound) { ConvertTo-RectRecord $taskbarRect.Left $taskbarRect.Top $taskbarRect.Right $taskbarRect.Bottom } else { $null }
        Edge = if ($taskbarFound -and $taskbarEdge -lt 4) { $edgeNames[$taskbarEdge] } else { $null }
        SecondaryBounds = @($secondaryTaskbars)
    }

    $narrator = [ordered]@{
        ProcessRunning = @(Get-Process -Name 'Narrator' -ErrorAction SilentlyContinue).Count -ne 0
        ScreenReaderFlag = if ($screenReaderRead) { $screenReader } else { $null }
    }

    $binaryRecord = [ordered]@{
        Path = $Binary
        Exists = $false
        Length = $null
        Sha256 = $null
        ProductVersion = $null
        FileVersion = $null
        Commit = $null
        WindowsAppRuntime = $null
        WindowsAppSdkPackages = $null
    }
    $commitCheck = Test-CommitMatch -ProductVersion $null -ExpectedCommit $ExpectedCommit
    if (Test-Path -LiteralPath $Binary -PathType Leaf) {
        $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($Binary)
        $commitCheck = Test-CommitMatch -ProductVersion $version.ProductVersion -ExpectedCommit $ExpectedCommit
        $runtimePath = Join-Path (Split-Path -Parent $Binary) 'Microsoft.WindowsAppRuntime.dll'
        $runtime = $null
        if (Test-Path -LiteralPath $runtimePath -PathType Leaf) {
            $runtimeVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($runtimePath)
            $runtime = [ordered]@{
                Path = $runtimePath
                FileVersion = $runtimeVersion.FileVersion
                ProductVersion = $runtimeVersion.ProductVersion
                Sha256 = Get-FileSha256 -Path $runtimePath
            }
        }
        # The package graph in <app>.deps.json is the authoritative Windows App SDK version record.
        $packages = $null
        $depsPath = [System.IO.Path]::ChangeExtension($Binary, '.deps.json')
        if (Test-Path -LiteralPath $depsPath -PathType Leaf) {
            try {
                $deps = Get-Content -LiteralPath $depsPath -Raw | ConvertFrom-Json
                $packages = @($deps.libraries.PSObject.Properties.Name | Where-Object { $_ -like 'Microsoft.WindowsAppSDK*' } | Sort-Object)
            }
            catch {
                $packages = @('unreadable')
            }
        }
        $binaryRecord = [ordered]@{
            Path = $Binary
            Exists = $true
            Length = (Get-Item -LiteralPath $Binary).Length
            Sha256 = Get-FileSha256 -Path $Binary
            ProductVersion = $version.ProductVersion
            FileVersion = $version.FileVersion
            Commit = $commitCheck.Commit
            WindowsAppRuntime = $runtime
            WindowsAppSdkPackages = $packages
        }
    }

    $settings = Get-SettingsSnapshot
    $testRootExists = Test-Path -LiteralPath $TestRoot -PathType Container
    $harnessFiles = [ordered]@{}
    foreach ($name in @('Invoke-UiEvidence.ps1', 'UiEvidence.psm1', 'cells.json', 'selftest.ps1')) {
        $path = Join-Path $HarnessRoot $name
        $harnessFiles[$name] = if (Test-Path -LiteralPath $path -PathType Leaf) { Get-FileSha256 -Path $path } else { $null }
    }
    $harnessCommit = $null
    $harnessDirty = $null
    if ($null -ne (Get-Command git -ErrorAction SilentlyContinue)) {
        $harnessCommit = (& git -C $HarnessRoot rev-parse --verify HEAD 2>$null)
        if ($LASTEXITCODE -eq 0) {
            $harnessDirty = @(& git -C $HarnessRoot status --porcelain=v1 --untracked-files=all -- . 2>$null).Count -ne 0
        }
        else {
            $harnessCommit = $null
        }
    }

    return [ordered]@{
        RecordedAt = [DateTimeOffset]::Now.ToString('o', [Globalization.CultureInfo]::InvariantCulture)
        EnvironmentId = $EnvironmentId
        Os = $os
        Session = $session
        Monitors = $monitors
        HighContrast = if ($highContrastRead) { $highContrast } else { $null }
        Taskbar = $taskbar
        Narrator = $narrator
        Binary = $binaryRecord
        ExpectedCommit = $ExpectedCommit
        CommitCheck = [ordered]@{ Match = $commitCheck.Match; Reason = $commitCheck.Reason }
        Settings = [ordered]@{ Path = $settings.Path; Exists = $settings.Exists; Length = $settings.Length; Sha256 = $settings.Sha256 }
        TestRoot = [ordered]@{
            Path = $TestRoot
            Exists = $testRootExists
            Empty = if ($testRootExists) { @(Get-ChildItem -LiteralPath $TestRoot -Force | Select-Object -First 1).Count -eq 0 } else { $true }
        }
        Harness = [ordered]@{ Commit = $harnessCommit; Dirty = $harnessDirty; Sha256 = $harnessFiles }
    }
}

#endregion

#region Owned process, layout, screenshot

function Start-OwnedProcess {
    param(
        [Parameter(Mandatory)]
        [string] $Binary,

        [Parameter(Mandatory)]
        [string] $WorkingDirectory,

        [Parameter()]
        [int] $TimeoutSeconds = 30
    )

    Initialize-UiEvidenceNative
    $start = [System.Diagnostics.ProcessStartInfo]::new($Binary)
    $start.UseShellExecute = $false
    $start.WorkingDirectory = $WorkingDirectory
    $process = [System.Diagnostics.Process]::Start($start)
    $deadline = [DateTimeOffset]::Now.AddSeconds($TimeoutSeconds)
    $hwnd = [IntPtr]::Zero
    while ([DateTimeOffset]::Now -lt $deadline -and -not $process.HasExited) {
        $windows = [NeNeUiEvidenceNative]::FindTopLevelWindows([uint32] $process.Id)
        if ($windows.Count -eq 1) {
            $hwnd = $windows[0]
            break
        }
        Start-Sleep -Milliseconds 200
    }
    if ($hwnd -eq [IntPtr]::Zero) {
        return [pscustomobject]@{ Process = $process; Hwnd = [IntPtr]::Zero; Window = $null; Reason = 'owned-window-not-found' }
    }
    $window = Get-OwnedWindowElement -Hwnd $hwnd
    while ([DateTimeOffset]::Now -lt $deadline) {
        if ($null -ne (Find-OwnedElement -Window $window -AutomationId 'LeftFileList') -and
            $null -ne (Find-OwnedElement -Window $window -AutomationId 'RightFileList')) {
            Start-Sleep -Seconds 2
            return [pscustomobject]@{ Process = $process; Hwnd = $hwnd; Window = $window; Reason = 'ready' }
        }
        Start-Sleep -Milliseconds 200
    }
    return [pscustomobject]@{ Process = $process; Hwnd = $hwnd; Window = $window; Reason = 'file-lists-not-ready' }
}

function Stop-OwnedProcess {
    <# Closes the owned window through UIA WindowPattern.Close; never sends a key. #>
    param(
        [Parameter(Mandatory)]
        $Owned
    )

    $record = [ordered]@{ Method = 'WindowPattern.Close'; Exited = $false; ExitCode = $null; Milliseconds = $null; Killed = $false }
    $started = [DateTimeOffset]::Now
    try {
        if ($null -ne $Owned.Window -and -not $Owned.Process.HasExited) {
            $pattern = $Owned.Window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
            $pattern.Close()
        }
    }
    catch {
        $record.Method = 'WindowPattern.Close:failed'
    }
    $record.Exited = $Owned.Process.WaitForExit(30000)
    if (-not $record.Exited) {
        # Only the harness-owned process is ever terminated.
        $Owned.Process.Kill()
        $record.Killed = $true
        $record.Exited = $Owned.Process.WaitForExit(10000)
    }
    if ($record.Exited) {
        $record.ExitCode = $Owned.Process.ExitCode
    }
    $record.Milliseconds = [int] ([DateTimeOffset]::Now - $started).TotalMilliseconds
    return $record
}

function Get-WindowBoundsRecord {
    <# GetWindowRect and UIA BoundingRectangle for the owned window, with DIP conversion. #>
    param(
        [Parameter(Mandatory)]
        $Owned
    )

    $dpi = [int] [NeNeUiEvidenceNative]::GetDpiForWindow($Owned.Hwnd)
    $physical = Get-WindowRectRecord -Hwnd $Owned.Hwnd
    $uia = Get-ElementRect -Element $Owned.Window
    $scale = if ($dpi -gt 0) { $dpi / 96.0 } else { 1.0 }
    return [ordered]@{
        Dpi = $dpi
        Physical = $physical
        Uia = $uia
        Dip = [ordered]@{
            Width = [Math]::Round($physical.Width / $scale, 2)
            Height = [Math]::Round($physical.Height / $scale, 2)
        }
        ReadsAgree = ($null -ne $uia -and $physical.Left -eq $uia.Left -and $physical.Top -eq $uia.Top -and
            $physical.Width -eq $uia.Width -and $physical.Height -eq $uia.Height)
        Maximized = [NeNeUiEvidenceNative]::IsZoomed($Owned.Hwnd)
        Minimized = [NeNeUiEvidenceNative]::IsIconic($Owned.Hwnd)
    }
}

function Set-NarrowWindow {
    <# Sizes only the owned window to the declared DIP size through SetWindowPos. #>
    param(
        [Parameter(Mandatory)]
        $Owned,

        [Parameter(Mandatory)]
        [double] $WidthDip,

        [Parameter(Mandatory)]
        [double] $HeightDip
    )

    [uint32] $owner = 0
    [void] [NeNeUiEvidenceNative]::GetWindowThreadProcessId($Owned.Hwnd, [ref] $owner)
    if ([int] $owner -ne $Owned.Process.Id) {
        return $false
    }
    $dpi = [int] [NeNeUiEvidenceNative]::GetDpiForWindow($Owned.Hwnd)
    $width = ConvertTo-PhysicalPixels -Dip $WidthDip -Dpi $dpi
    $height = ConvertTo-PhysicalPixels -Dip $HeightDip -Dpi $dpi
    # SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE
    $result = [NeNeUiEvidenceNative]::SetWindowPos($Owned.Hwnd, [IntPtr]::Zero, 0, 0, $width, $height, 0x0002 -bor 0x0004 -bor 0x0010)
    Start-Sleep -Milliseconds 800
    return $result
}

function Get-LayoutMeasurement {
    param(
        [Parameter(Mandatory)]
        $Owned,

        [Parameter(Mandatory)]
        $Layout
    )

    $elements = @{}
    foreach ($id in @($Layout.elements)) {
        $element = Find-OwnedElement -Window $Owned.Window -AutomationId ([string] $id)
        $elements[[string] $id] = if ($null -ne $element) { Get-ElementRect -Element $element } else { $null }
    }
    $focus = Get-FocusOwner
    if ($focus.ProcessId -eq $Owned.Process.Id -and $null -ne $focus.Rect) {
        $elements['@focusedRow'] = $focus.Rect
    }
    else {
        $elements['@focusedRow'] = $null
    }
    $window = Get-ElementRect -Element $Owned.Window
    $required = @(@($Layout.elements) | ForEach-Object { [string] $_ }) + @('@focusedRow')
    $contained = @(@($Layout.containedIn) | ForEach-Object { , @([string] $_[0], [string] $_[1]) })
    if ($null -ne $focus.AutomationId -and $focus.AutomationId -in @('LeftFileList', 'RightFileList')) {
        $contained += , @('@focusedRow', $focus.AutomationId)
    }
    $verdict = Test-LayoutVerdict -Elements $elements -Window $window -Required $required `
        -DisjointPairs @(@($Layout.disjointPairs) | ForEach-Object { , @([string] $_[0], [string] $_[1]) }) `
        -ContainedIn $contained
    return [ordered]@{
        Window = $window
        Elements = $elements
        Focus = [ordered]@{ AutomationId = $focus.AutomationId; RawAutomationId = $focus.RawAutomationId; ProcessId = $focus.ProcessId; Rect = $focus.Rect }
        NotExposed = @($Layout.notExposed)
        Pass = $verdict.Pass
        Failures = $verdict.Failures
    }
}

function Test-BitmapUniform {
    param([Parameter(Mandatory)] [System.Drawing.Bitmap] $Bitmap)

    $rectangle = [System.Drawing.Rectangle]::new(0, 0, $Bitmap.Width, $Bitmap.Height)
    $data = $Bitmap.LockBits($rectangle, [System.Drawing.Imaging.ImageLockMode]::ReadOnly,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $length = [Math]::Abs($data.Stride) * $Bitmap.Height
        $buffer = [byte[]]::new($length)
        [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buffer, 0, $length)
    }
    finally {
        $Bitmap.UnlockBits($data)
    }
    return [NeNeUiEvidenceNative]::IsUniform($buffer)
}

function Save-OwnedScreenshot {
    <#
      PrintWindow with PW_RENDERFULLCONTENT first. A uniform frame falls back to a screen-region
      capture only while the owned window is verifiably foreground; otherwise nothing is captured.
    #>
    param(
        [Parameter(Mandatory)]
        $Owned,

        [Parameter(Mandatory)]
        [string] $Directory,

        [Parameter(Mandatory)]
        [string] $Name
    )

    $rect = Get-WindowRectRecord -Hwnd $Owned.Hwnd
    $path = Join-Path $Directory "$Name.png"
    $bitmap = [System.Drawing.Bitmap]::new($rect.Width, $rect.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $hdc = $graphics.GetHdc()
        $printed = [NeNeUiEvidenceNative]::PrintWindow($Owned.Hwnd, $hdc, 0x2)
        $graphics.ReleaseHdc($hdc)
        $graphics.Dispose()
        $method = 'PrintWindow:PW_RENDERFULLCONTENT'
        if (-not $printed -or (Test-BitmapUniform -Bitmap $bitmap)) {
            $foreground = [NeNeUiEvidenceNative]::GetForegroundWindow()
            $root = if ($foreground -eq [IntPtr]::Zero) { [IntPtr]::Zero } else { [NeNeUiEvidenceNative]::GetAncestor($foreground, 2) }
            if ($root -ne $Owned.Hwnd) {
                return [ordered]@{ Path = $null; Sha256 = $null; CaptureMethod = 'none:uniform-frame-and-not-foreground' }
            }
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
            $graphics.Dispose()
            $method = 'CopyFromScreen:foreground-verified'
        }
        $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
    return [ordered]@{ Path = $path; Sha256 = Get-FileSha256 -Path $path; CaptureMethod = $method }
}

#endregion

#region Records

function Write-Utf8NoBom {
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string] $Text
    )

    $normalized = ($Text -replace "`r?`n", "`r`n")
    if (-not $normalized.EndsWith("`r`n", [StringComparison]::Ordinal)) {
        $normalized += "`r`n"
    }
    [System.IO.File]::WriteAllText($Path, $normalized, [System.Text.UTF8Encoding]::new($false))
}

function New-CellRecord {
    param(
        [Parameter(Mandatory)]
        $Cell,

        [Parameter(Mandatory)]
        $Run
    )

    return [ordered]@{
        Id = [string] $Cell.id
        Mode = [string] $Cell.mode
        Status = 'SKIP'
        Reason = $null
        Commit = $Run.Commit
        BinarySha256 = $Run.BinarySha256
        Environment = $Run.EnvironmentId
        Dpi = $null
        HighContrast = $Run.HighContrast
        Scheme = $null
        Window = $null
        OwnedProcessId = $null
        OwnedHwnd = $null
        UiaFocus = $null
        UiaBounds = $null
        Screenshots = @()
        Steps = @()
        Layout = $null
        Close = $null
        StartedAt = [DateTimeOffset]::Now.ToString('o', [Globalization.CultureInfo]::InvariantCulture)
        EndedAt = $null
    }
}

function ConvertTo-SummaryMarkdown {
    param(
        [Parameter(Mandatory)]
        $Evidence
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('# UI evidence summary')
    $lines.Add('')
    $lines.Add("- Run: ``$($Evidence.RunId)``")
    $lines.Add("- Mode: ``$($Evidence.Mode)``")
    $lines.Add("- Environment: ``$($Evidence.Preflight.EnvironmentId)``")
    $lines.Add("- Binary commit: ``$($Evidence.Preflight.Binary.Commit)``; expected: ``$($Evidence.Preflight.ExpectedCommit)``; check: ``$($Evidence.Preflight.CommitCheck.Reason)``")
    $lines.Add("- Binary SHA-256: ``$($Evidence.Preflight.Binary.Sha256)``")
    if ($Evidence.Preflight.Binary.Exists) {
        $lines.Add("- Windows App SDK packages: ``$(@($Evidence.Preflight.Binary.WindowsAppSdkPackages) -join ', ')``")
    }
    $lines.Add("- OS: ``$($Evidence.Preflight.Os.Version)`` build ``$($Evidence.Preflight.Os.CurrentBuild).$($Evidence.Preflight.Os.Ubr)``; session ``$($Evidence.Preflight.Session.SessionId)``")
    $lines.Add("- Taskbar: edge ``$($Evidence.Preflight.Taskbar.Edge)``, secondary taskbars ``$(@($Evidence.Preflight.Taskbar.SecondaryBounds).Count)``")
    $lines.Add("- High contrast: ``$($Evidence.Preflight.HighContrast)``; Narrator running: ``$($Evidence.Preflight.Narrator.ProcessRunning)``")
    $lines.Add("- Settings before: exists ``$($Evidence.Preflight.Settings.Exists)``, SHA-256 ``$($Evidence.Preflight.Settings.Sha256)``")
    if ($null -ne $Evidence.SettingsAfter) {
        $lines.Add("- Settings after: exists ``$($Evidence.SettingsAfter.Exists)``, SHA-256 ``$($Evidence.SettingsAfter.Sha256)``, restored match ``$($Evidence.SettingsAfter.MatchesStart)``")
    }
    $lines.Add("- Input stop: ``$($Evidence.InputStop)``")
    $lines.Add('')
    $lines.Add('| Monitor | Primary | Bounds | DPI | Scale |')
    $lines.Add('|---|---|---|---|---|')
    foreach ($monitor in @($Evidence.Preflight.Monitors)) {
        $b = $monitor.Bounds
        $lines.Add("| $($monitor.Device) | $($monitor.Primary) | $($b.Left),$($b.Top) $($b.Width)x$($b.Height) | $($monitor.Dpi) | $($monitor.ScalePercent)% |")
    }
    $lines.Add('')
    $lines.Add('| Cell | Mode | Status | Reason | DPI | Scheme | Window px | Screenshot SHA-256 |')
    $lines.Add('|---|---|---|---|---|---|---|---|')
    foreach ($cell in @($Evidence.Cells)) {
        $size = if ($null -ne $cell.Window -and $null -ne $cell.Window.Physical) { "$($cell.Window.Physical.Width)x$($cell.Window.Physical.Height)" } else { '' }
        $shot = (@($cell.Screenshots) | ForEach-Object { $_.Sha256 } | Where-Object { $null -ne $_ }) -join ' '
        $lines.Add("| $($cell.Id) | $($cell.Mode) | $($cell.Status) | $($cell.Reason) | $($cell.Dpi) | $($cell.Scheme) | $size | $shot |")
    }
    $lines.Add('')
    $lines.Add('A PASS here is a recorded measurement, not a gate result. Visual review of each screenshot is still required (Issue #94).')
    return ($lines -join "`r`n")
}

#endregion

Export-ModuleMember -Function @(
    'Test-InputAdmission',
    'Test-KeyAdmission',
    'Get-StepPixels',
    'ConvertTo-PhysicalPixels',
    'Test-CommitMatch',
    'Test-LayoutVerdict',
    'Resolve-StepExpectation',
    'Get-UiEvidenceInputStop',
    'Send-AdmittedKey',
    'Initialize-UiEvidenceNative',
    'Get-InputObservation',
    'Get-FocusOwner',
    'Get-FileSha256',
    'Get-BytesSha256',
    'Get-SettingsDocumentPath',
    'Get-SettingsSnapshot',
    'Get-SettingsScheme',
    'Get-PreflightRecord',
    'Start-OwnedProcess',
    'Stop-OwnedProcess',
    'Get-WindowBoundsRecord',
    'Set-NarrowWindow',
    'Get-LayoutMeasurement',
    'Find-OwnedElement',
    'Get-ElementRect',
    'Save-OwnedScreenshot',
    'Write-Utf8NoBom',
    'New-CellRecord',
    'ConvertTo-SummaryMarkdown')
