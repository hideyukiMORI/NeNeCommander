# Read-only Shell query observation — 2026-11-19

Status: measurements complete; documentation integration and task cleanup pending; #453 stays open.

Issue [#473](https://github.com/hideyukiMORI/NeNeCommander/issues/473) follows the
[non-UI provider observation](2026-11-19-f6-provider-phases.md). That experiment found a large
Inspect boundary but did not separate its internals. This independent read-only microprobe observes
the native calls used by recycle-capability inspection. It does not measure production Inspect,
change product code, or establish the cause of hide's original pause.

## Inputs, invariant and method

Main is `5a8230b`. The unchanged verified beta.2 Domain/Application/Infrastructure DLLs supply only
the existing `IClock/StopwatchClock`; hashes are identical to those listed in the preceding quality
record. SDK10.0.404 and Windows10.0.26300.0 are recorded in `environment-before.json`. No production
assembly is rebuilt. The external diagnostic has an empty package lock and uses the existing
generated `IShellItem` ABI for its read-only calls; no repository project or dependency is changed.

The existing production mutation route remains `FileOperationGateway → provider → Windows local
boundary`. This diagnostic contains no mutation route: it reads only two owned SQL106B/TXT105B
fixtures, using one fresh STA per file, in the existing capability check's order:
explicit `CoInitializeEx`, `SHCreateItemFromParsingName`, `IShellItem.GetAttributes(0x40000020)`,
`SHQueryRecycleBinW(D:\)`, same-STA `FinalRelease`, and `CoUninitialize` in finally.
The query retrieves drive-wide aggregate count/size, which the diagnostic clears without printing
or serializing; only HRESULT and elapsed ticks are kept. No recycle/delete/empty-bin, move, file
launch, settings, UI or network action is included.

The same two fixture hashes from #470 are verified before and after each pair. Exact inventory,
file type and all ancestors must have no reparse point. Plan: one process, one warmup pair, then
five measured pairs, SQL followed by TXT. An initial diagnostic lifecycle failure required the
explicitly recorded correction below; it is excluded and not replaced silently. The corrected
series ran once and was not expanded after observing its values.

Six disjoint native spans use integer TimeSpan ticks (100ns units, not a physical resolution claim).
Caller/worker endpoints and managed thread IDs are also recorded. Results/exception identity and
exactly one call are checked with a deterministic fake clock. Independent Python arithmetic checks
all 72 accepted spans, order, containment, thread identity, successful HRESULTs, 12 joined STAs,
raw/final equality, one PID and both unchanged fixture hashes.

## Diagnostic failure and correction

The first attempt at14:48:39–14:48:45 stopped on the first warmup file: all six native calls returned
success, but the diagnostic's five-second thread join returned false. No measured pair started.
Original source, `warmup.json`, log and exit `-532462766` are retained. This is a failed diagnostic
attempt, not an observed product failure.

The wrapper awaited a task that could run its continuation inline on the same STA, then attempted
to join that worker. A native-free controlled proof with a registered synchronous continuation
reproduces this self-join (`same thread=true`, `joined=false`) and verifies the correction
(`same thread=false`, `joined=true`). `RunContinuationsAsynchronously` is added only to the diagnostic
task. The actual production scheduler has no join and remains unchanged. The original attempt did
not record its continuation's thread ID, so the controlled reproduction supports this explanation
without retrospectively observing that missing ID.

The corrected probe records/asserts a different join-caller thread, retains the original failed
file, and writes separate `corrected/` results. Native operations, fixtures and warmup/five-pair
plan are unchanged. The diagnostic scheduling difference prevents treating its caller interval as
product scheduler performance. Both diagnostic builds have zero warnings/errors; recorder and
lifecycle self-proofs pass. No further failed attempt or retry is omitted.

## Results

The corrected process40872 completed at14:53:20 JST. All12 accepted native call sequences succeeded,
all STAs exited, and file contents were unchanged. Warmup query-pair time33.0352ms is excluded below.
The five measured query-pair values are31.6343,31.2457,30.0840,30.6134 and30.6415ms.

Each row is the sum of its two file calls in one pair; summaries use the five accepted pairs.

| Boundary | Minimum ms | Median ms | Maximum ms |
| --- | ---: | ---: | ---: |
| Caller interval, excluding final join | 34.7004 | 35.2399 | 36.2354 |
| Worker body | 33.1825 | 33.7288 | 34.6450 |
| Explicit COM init | 0.0009 | 0.0010 | 0.0010 |
| Shell entry creation | 2.8299 | 2.8856 | 2.9873 |
| Requested attributes | 0.0520 | 0.0526 | 0.0549 |
| Recycle-bin query | 30.0840 | 30.6415 | 31.6343 |
| Shell entry release | 0.0257 | 0.0266 | 0.0444 |
| Explicit COM uninit | 0.0044 | 0.0046 | 0.0048 |
| Worker time outside native spans | 0.0226 | 0.0243 | 0.0445 |
| Caller time outside worker body | 1.4644 | 1.5904 | 1.6683 |

These elapsed intervals include interop/wrapper/OS work and are not CPU cost. Explicit COM init
returns `S_FALSE` because the runtime's STA is already initialized; its small interval does not
measure total apartment startup. Caller intervals exclude the later join; outside-worker time
contains thread startup, task scheduling/continuation and unclassified overhead. No separate
overhead subtraction is made. Do not add row medians or ranges.

The query itself shows a material interval under this controlled setup. It is a candidate for
later operation-specific capability design, with identity, revalidation, deletion eligibility
and all existing safety outcomes preserved. There is no cache, lazy capability, shared worker,
port-contract change or product optimization here. Independent values cannot be subtracted from
#470's production Inspect intervals or added to native XAML timings. Cold/cache, AV, percentiles,
other drives/providers, different recycle-bin contents and end-to-end key/display latency remain
unmeasured. #453 stays open; PR402's hide screen acceptance is separate.

## Commands and preservation

Evidence root `E`: `D:/NeNeCommander/evidence/f6-shell-query-20261119`.
Temporary root `O`: `D:/NeNeCommander/outputs/f6-shell-query-20261119`.

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-shell-query-20261119/prepare-and-build.ps1
dotnet D:/NeNeCommander/outputs/f6-shell-query-20261119/bin/Release/net10.0-windows10.0.26100.0/ShellProbe.dll measure
# After the preserved diagnostic-only lifecycle correction, from O:
dotnet build ShellProbe.csproj -c Release --no-restore -m:1 -nr:false
dotnet bin/Release/net10.0-windows10.0.26100.0/ShellProbe.dll selftest
dotnet bin/Release/net10.0-windows10.0.26100.0/ShellProbe.dll measure-corrected
python D:/NeNeCommander/evidence/f6-shell-query-20261119/analyze.py
```

Preparation (restore/build/selftest) exits0. Original `measure` exits `-532462766` as explained;
corrected build/selftest/measure and independent analysis exit0. `build-commands.json`, both
measure command records and logs preserve exact arguments/times/results. A source lookup used
the nonexistent `src/NeNeCommander.Infrastructure` directory and was corrected to `.Windows`.
The first preservation summary used a PowerShell dictionary sum that emitted null; recomputing
the byte sum from the saved manifest corrected it to30,267,365 without recopying or losing files.

At14:54:24, all60 output files/30,267,365 bytes were preserved under `E/runs` with complete size/hash
equality, including original failure, corrected sources/binaries/raw/final files and both fixtures.
Original sources remain separately under `E`. Probe40872 ended, normal process38360 remained, and
the original218-byte settings retained SHA256
`ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`. No UI or settings restoration.

Primary API references: [query](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shqueryrecyclebinw),
[attributes](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ishellitem-getattributes),
[task continuation option](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcreationoptions?view=net-10.0)
and [thread join](https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.join?view=net-10.0).

Repository change is documentation only under ADR-0061: ordinary commit hooks, text/link/diff and
numerical review. No product build/tests, CI, coverage/mutation, dependency/schema, ADR/waiver or
release change. [Progress](../reports/2026-11-19-f6-shell-query-progress-report.md) and
[handoff](../handoffs/2026-11-19-f6-shell-query-handoff.md) own integration/cleanup status.
