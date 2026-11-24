# Monitor transfer: bounded hidden-window investigation

Issue #551 / 2026-11-22 / main input `7ef9b96`.
Scope: diagnostic fixture and documentation only; no product implementation,
accepted ADR, native-key proof, source publication, merge or release claim.

Evidence: `D:/NeNeCommander/evidence/monitor-transfer-20261122/`.
Diagnostic output: `D:/NeNeCommander/outputs/monitor-transfer-20261122/probe/`.

## Isolation and provenance

A dedicated WinUI process created exactly one unshown frameless Window, with a Grid
as content, and moved only its own AppWindow. It never called Show, Activate or an
input API, and never opened the product, settings or user data. GetDpiForWindow and
IsWindowVisible read only its own HWND; GetForegroundWindow observed the foreground
without changing it. No monitor configuration or user window was changed. Those
native imports exist only in the external diagnostic source, not in the repository.

Both runs exited0. Activation count and ever-visible flag were0/false. All sampled
visibility facts and Changed-event observations were false; all foreground handles
remained the initially observed one. This proves those observations, not native
keyboard routing or user focus behavior. A 25-second launcher bound could stop only
the owned diagnostic process; neither run hit it.

The fixture copied the pinned SDK, unchanged package lock and PerMonitorV2 manifest.
It used the same pinned Windows App SDK dependency as main. The baseline and final
source snapshots, builds, JSON, provenance and hashes are retained separately.
The diagnostic uses a 120 ms DispatcherQueueTimer to observe after-return behavior;
these are empirical measurements, not deterministic product tests or a product timer.

## Results and all trials

| Evidence | Result |
|---|---|
| `probe-restore.log` | Locked Release restore succeeded. |
| `probe-build.log`, `run.json`, `hidden-transfer.json` | Initial build0 warnings/errors; process39740 exited0. Four monitors, all12 directed pairs ×3 API variants =36 observations. Native geometry/DPI at return matched the later sample in every case. |
| `probe-fit-build-first.log` | Intermediate fit-fixture build succeeded0 warnings/errors. Not executed; cases were narrowed and an oversized case added before collection. |
| `probe-fit-build.log`, `run-fit.json`, `hidden-transfer-fit.json` | Final build0 warnings/errors; process19120 exited0. Existing absolute MoveAndResize path, all12 pairs ×2 sizes =24 observations. All24 final rectangles fit the requested work area; all24 matched the immediate post-correction geometry. |
| `verification-summary.json` | XAML scale was stale immediately after20/24 first moves and matched native DPI at the later sample in24/24. Both runs observed no activation/visibility/foreground change. |
| `baseline-source/`, `fit-source/`, `product-input-hashes.json`, `evidence-hashes.json` | Exact diagnostic variants and relevant unmodified product inputs retained. |

The four observed work areas were (-3840,0,3840,2160) at150%,
(3840,0,3840,2160) at175%, (3818,-1440,2560,1440) at150%,
and (0,0,3840,2100) at125%. These are observed environment facts, not constants.
A900×600 rectangle moved from125% to175% became1260×840 before API return.
Absolute and display-relative MoveAndResize and Move behaved alike for the baseline.

The oversized primary-to-upper transfer initially became5264×2828 at175%, with
DisplayArea identifying the adjacent right-hand monitor. The intended upper monitor
is150%. The fixture's second fit reached2560×1440 at(3818,-1440), native144DPI,
while XAML still reported1.25 until the dispatcher advanced. This counterexample
prevents claiming the first result always belongs to the requested monitor.

The baseline JSON's human-readable limitation string incorrectly said `No XamlRoot`.
Its actual before/immediate/settled fields all record `xamlRootExists=true` after
the dispatcher ran. The final fixture records actual RasterizationScale. The raw
baseline remains unchanged and the correction is explicit in verification-summary.
This does not retroactively turn either run into rendered-screen proof.

## Exact diagnostic commands

From the dedicated probe directory:

```powershell
dotnet restore MonitorProbe.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build MonitorProbe.csproj -c Release --no-restore -maxcpucount:4
```

The latter was run for the initial, intermediate fit and final fit source variants;
all returned0. Each runtime launch used `Start-Process -WindowStyle Hidden -PassThru`
for the exact probe executable, followed by `WaitForExit(25000)`; run.json and
run-fit.json record PID/exit/time. The final executable was
`probe/bin/Release/net10.0-windows10.0.26100.0/win-x64/MonitorProbe.exe` beneath the
output root. No product build/test, coverage, mutation, CodeQL or CI was run for
this documentation change. Normal commit-hook results belong to commit.log.

## Limits and next proof

This is an unshown diagnostic window, not CommanderWindow or the complete product
route. No rendered layout, native key/focus, visible maximize/restore, presenter
change, disconnect/reconnect, rapid-repeat or settings/operation preservation was
exercised. The observed immediate resize is not an API timing guarantee. The
fixture's arithmetic is not the accepted application algorithm. The design record
keeps two mechanisms unresolved; an accepted ADR and targeted product/native proof
are still required. Retain unfinished output and the local branch; do not clean it
as a completed feature or infer integration from this evidence.

## November24 continuation: display-scale API and compensated landing

hide's current milestone-continuation instruction supersedes the old stop. This
section remains investigation/design evidence only: no production change, accepted
monitor-transfer ADR, final visual acceptance, public source push or release.
The retained issue551 worktree starts clean at eabae9e; all11 old investigation
input hashes match. Current release-base f6c722c now also has ADR-0089 placement
tracking/startup fitting, which a future implementation must preserve. No base
integration or production edit has occurred in this continuation.

Background Claude consultation completes16:50:59/exit0. The2851-byte brief contains
only public Issue551 requirements and authored generic UX choices, not repository
source, private layout or diagnostic contents. It runs in the dedicated empty D
consult directory with safe mode, no tools, empty strict MCP config, no settings
sources, no session persistence and one turn. The installed CLI's /design command
was already observed unsupported in this session, so this is text advice rather
than a generated design artifact. The earlier held repository-source payload was
not retried. Exact brief/advice/invocation/hash are retained in the new evidence.

The advice recommends n/p within Ctrl+W for one-shot next/previous geometric cycling,
left-to-right then top-to-bottom order, logical-size preservation, existing helper
hints and no new mouse entry. It prefers maximized transfer and relative landing,
while explicitly allowing refusal when reliability cannot be proved. These are
recommendations, not accepted behavior. The existing proposal uses centered landing
and normal-only admission; engineering still must select and record that contract.

Official API inspection distinguishes Microsoft.Graphics.Display.DisplayInformation
(which exposes display/color information but no scale property in the inspected
public surface) from Windows.Graphics.Display.DisplayInformationInterop. The latter
has GetForMonitor/GetForWindow wrappers through the existing Windows SDK projection.
Microsoft documents GetForMonitor as Windows11 build22621 or newer, with scale based
on GetScaleFactorForMonitor. This is no proof of custom-scale precision or Windows11
build22000 availability. Sources: [display class](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.graphics.display.displayinformation?view=windows-app-sdk-1.8),
[C# wrappers](https://learn.microsoft.com/windows/apps/desktop/modernize/winrt-com-interop-csharp),
[monitor contract](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.display.interop/nf-windows-graphics-display-interop-idisplayinformationstaticsinterop-getformonitor).

New scale-probe copies the existing pinned package lock, SDK and PerMonitorV2 manifest
into D:/NeNeCommander/outputs/monitor-transfer-20261124/scale-probe. Locked restore
and Release build both succeed, zero warnings/errors. At16:53 owned process11320
exits0, no timeout, activations0, visible samples0 and unchanged foreground/settings.
GetForMonitor reports1.5/1.75/1.5/1.25 for the four attached displays and agrees with
the own-window native DPI after each move. GetForWindow fails all8 immediate/settled
observations with E_NOTIMPL (-2147467263); this is retained and excluded as an API
choice. Immediate XAML scale is stale in all4 tested transitions and settles later.
The diagnostic-only native imports never enter product source.

A separate transfer-probe-01 evaluates a new two-rectangle candidate against all12
ordered display pairs at normal and oversized starting dimensions,24conditions.
It precomputes a centered final rectangle by converting source physical dimensions
through observed source/target scale and fitting each dimension to the target work
area. The first request's dimensions compensate for that scale ratio; the second
request applies the already-decided final physical rectangle. This is candidate
arithmetic, not an accepted timing contract or an adapter-owned product decision.
Locked restore/build succeed with0warnings/errors. At16:59 process25288 exits0,
no timeout/activation/visibility/foreground/settings change. All24 final rectangles
exactly equal the plan, fit the requested work area and have the intended native
DPI. The first request alone is exact23/24; all24 already select the intended display.
The prior primary-to-upper oversized neighbouring-display counterexample is avoided
in this bounded candidate. Its earlier failure evidence remains unchanged.

The first console summary used Select-Object directly on an OrderedDictionary and
printed null fields. The saved JSON was correct; rereading it as a structured object
confirms24/24. No runtime repeat occurred for this reporting correction. Read-only
source discovery also records absent guessed route/ADR paths and truncated combined
reads; exact files were then located. Remaining normative reading must be completed
before any production edit; truncated text is not treated as read.

Exact diagnostic commands (inside each dedicated probe):

```powershell
dotnet restore MonitorProbe.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build MonitorProbe.csproj -c Release --no-restore
```

Each exact bin/Release/net10.0-windows10.0.26100.0/win-x64/MonitorProbe.exe is launched
with Start-Process -WindowStyle Hidden -PassThru and bounded WaitForExit(20000) for
scale-probe or25000 for transfer-probe-01. Only its returned process could be ended
on timeout; neither timed out. Commands/results/source/managed-DLL hashes and normal
settings hash are in scale-api-run-01 and transfer-compensated-run-01. Raw API errors,
plans, before/immediate/final/settled samples and independent arithmetic summary are
retained. Normal settings SHA256 is561932A6530A6317499BAB59AD0735C8469869A15C6E6DA9BE3507B37AF557CE.

Next decisions before implementation: minimum size versus work-area fit, first-stage
minimum and rounding, destination identity/topology revalidation, unsupported OS/API
failure, custom scaling, maximized/minimized/hidden admission, partial effect/failure
wording and exact ownership during reentrant/stopping events. A successful hidden
probe does not establish visible/native-key/pane preservation or general OS timing.
Use the existing WindowAdjustment planner/route/adapter, keep geometry policy in
Application, and do not introduce a timer, retry loop or new window-operation path.
Reserve the next truly free ADR number (0085 is now occupied;0091 is used by local531).
No product test, full gate, mutation or CI is run for this documentation/diagnostic
work. The new evidence/output and existing worktree are unfinished and retained.
