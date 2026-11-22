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
