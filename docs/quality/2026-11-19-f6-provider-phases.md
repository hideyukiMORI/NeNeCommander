# F6 non-UI provider phase observation — 2026-11-19

Status: bounded diagnostic complete; #453 remains open, native end-to-end latency unmeasured.

Issue [#470](https://github.com/hideyukiMORI/NeNeCommander/issues/470) separates the existing provider
and directory-read boundaries that the [native XAML observation](2026-11-19-f6-xaml-observation.md)
could not expose. It neither changes the product nor establishes the cause of hide's reported pause.
The [footer optimization](2026-11-19-footer-hint-rebinding.md) and unaccepted PR402 are separate.

## Invariant, inputs and method

The standalone diagnostic calls `DualPaneSession.HandleAsync(UserIntent.Move)` through the unchanged
`FileOperationGateway → ProviderFileOperationPort → WindowsLocalIoExecutionBoundary`. Both refreshes
use the unchanged `ProviderDirectoryReadPort`. A forwarding wrapper times each existing port call
with the product's `IClock/StopwatchClock`, returns the same result and propagates the same exception.
All identity, capability, preflight, revalidation, no-overwrite and outcome logic stays in production
assemblies. The wrapper refuses paths outside the exact owned fixture and refuses an unexpected
copy/delete/rename/create operation; no fallback or alternate mutation implementation is provided.

The existing verified beta.2 payload from source `555704e` supplies these unchanged binaries:

| Assembly | SHA-256 |
| --- | --- |
| Domain | `8DC17791482069347CBAED9771601DD12C61BDC622CC5D195DEB84C051650BF3` |
| Application | `7EFCEAE55221F1D3D80E111D938A7F912A58C61ADAB40BD264565D6C7D973458` |
| Infrastructure.Windows | `B23985AE3ADAD359A94EBD501B53E14042A4ADCC32F32E31956D2FBF2168E0ED` |

Their complete source directories are unchanged between beta.2 and current main `3865060`; the
three DLL copies used by the probe match the payload byte-for-byte. No production assembly is rebuilt.
Windows is10.0.26300.0, SDK10.0.404, PowerShell7.6.10; exact runtime is in each raw result.
The external diagnostic project has no package dependency in its generated lock file, changes no
repository project/graph, and is built outside the repository under the dedicated D output root.

The predeclared `plan.md` selects one process, one warmup and exactly five measured moves. Each uses
fresh source/target directories with the same SQL106B/TXT105B content as #465. Source hashes are
`25DACE8D753FA546A9F8D1904BC6FEA5D3F21F8D040B2B897C9BB4231AD4A5EA` and
`2BE0A3799A573997076A39371BA756B0019DE3C23F2BE01AD71B81E63F009E09` respectively.
Before timing, both panes are listed and both sources selected through ToggleSelection/MoveNext/
ToggleSelection. Exact selected paths, empty destination, non-link ancestors and contents are checked.
Fresh panes/gateway are composed per run outside timing; runtime code remains warm in the same process.
The preparatory hash reads mean this is not a cold-cache observation.

The complete timed interval starts immediately before the typed Move call and ends when it returns
after both pane refreshes. Port timestamps and progress points stay in memory until that return;
raw records are then saved before postcondition validation. Final records require success, two
atomic-move effects, no not-transferred item, left0/right2 and source0/target2 with both content hashes.
All six runs use the same nine port calls in order: Inspect2, Preflight1, AtomicCapability2, Move2,
ReadLeft1 and ReadRight1. Two progress notifications follow their respective moves. No retry occurred.

## Results and independent arithmetic

All six owned moves succeeded at14:33:47 JST in process42996. The warmup took138.9552ms and is
excluded from the following five-sample summary. The five totals, in order, are57.9852,58.8015,
64.5815,67.3116 and64.0019ms. These are elapsed intervals, not CPU time or physical F6 response.

| Timed boundary | Minimum ms | Median ms | Maximum ms |
| --- | ---: | ---: | ---: |
| Complete non-UI Move and refresh | 57.9852 | 64.0019 | 67.3116 |
| Inspect, both calls combined | 51.1665 | 54.4297 | 57.1311 |
| Transfer preflight | 0.3892 | 0.4423 | 0.5022 |
| Atomic capability, both calls | 2.5677 | 3.7473 | 4.2806 |
| Atomic Move, both calls | 3.1739 | 3.9775 | 4.8552 |
| Left directory read | 0.1234 | 0.1266 | 0.1888 |
| Right directory read | 0.1232 | 0.1633 | 0.2015 |
| Remaining time outside measured ports | 0.2470 | 0.2695 | 0.3363 |

The disjoint port spans lie within each whole interval. Remaining time includes orchestration,
diagnostic guards/recording, continuations and unclassified work; it is not pure application CPU.
Do not add medians/ranges across rows. The two Inspect calls occupy84.8756–88.2406% of these five
non-UI totals. This identifies the largest observed boundary for this fixture, not its inner cause.

An independent Python pass checks all original integer start/end ticks, the exact nine phase/outcome
sequence, containment/non-overlap, the two progress locations, final/raw equality, all12 target hashes
and one process ID. All54 port intervals and12 progress points pass. Arithmetic uses the product
clock's TimeSpan ticks (100ns units); this unit is not a claimed physical timer resolution.
`analysis.json` retains every sample, each boundary total and the independent checks.

## Source finding and limits

`WindowsLocalFileOperationAdapter.InspectAsync` schedules each inspection through the existing Shell
STA scheduler. `Inspect` obtains drive admission, entry identity and `_recycleBin.GetCapability`.
That capability path performs recycle admission, Shell entry creation/attributes and a recycle-bin
query, including for a Move request. The per-call scheduler also creates/initializes/releases its STA.
These are source facts; the measured Inspect interval includes all of them plus scheduling. This
experiment does not isolate the recycle query, apartment startup, metadata/identity I/O or AV cost.

A later investigation can separate those inner contributions and examine whether operation-specific
capability capture can avoid unrelated work while preserving deletion and transfer safety. No cache,
lazy capability, weaker identity check, shared Shell worker, new port contract or optimization is
implemented or accepted here. #453 remains open.

The diagnostic has no UI synchronization context and excludes input delivery/mapping,
CommanderSession, dispatcher, presentation, XAML/layout and physical display. It cannot be added to
or subtracted from earlier native measurements. Provider spans include worker scheduling and
mandatory safety checks, so Move does not isolate the underlying OS rename. Tiny same-volume files
and controlled directory sizes do not establish large-tree, cross-volume, UNC/WSL/removable, cold,
contention or percentile behavior. Capture/forwarding overhead is not separately subtracted. The
original user binary was `6b0d231`, not this accepted beta.2 source.

## Commands, proof and retained evidence

Evidence root `E` is `D:/NeNeCommander/evidence/f6-phases-20261119`; output root `O` is
`D:/NeNeCommander/outputs/f6-phases-20261119`.

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-phases-20261119/prepare-and-build.ps1
dotnet D:/NeNeCommander/outputs/f6-phases-20261119/bin/Release/net10.0-windows10.0.26100.0/PhaseProbe.dll measure
python D:/NeNeCommander/evidence/f6-phases-20261119/analyze.py
```

All three commands return0. The first records `dotnet restore PhaseProbe.csproj`,
`dotnet build PhaseProbe.csproj -c Release --no-restore -m:1 -nr:false` and the probe's `selftest` mode.
Build has zero warnings/errors. Deterministic fake-clock proof covers immediate/pending completion,
same-result identity, exactly one forward, synchronous/asynchronous exact-exception propagation and
100→200 tick endpoints. The same self-proof runs at measure startup, before file operations.
`build-commands.json`, `measure-command.json` and logs own full arguments/times/exits.

Before compilation, source review corrected the effect-kind identifier and ensured raw data is
persisted before postcondition checks. No failed build or measurement is hidden. Several read-only
source/manifest lookups used nonexistent filenames and were corrected against the actual file list;
they changed no product/fixture. The planned run count was not expanded after observing the results.

At14:35:38 the probe process was absent, normal process38360 remained and the original218-byte
settings SHA256 still matched `ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.
No settings restoration or UI interaction was needed. All product DLLs and five diagnostic inputs
remain unchanged. All66 output files/30,257,615 bytes are preserved in `E/runs/` with complete size/hash
equality in `preservation-manifest.json`. This includes code, lock/configuration, binaries, fixtures,
raw and validated results. No trace or fixture is uploaded/committed.

Repository scope is documentation only under ADR-0061: text/numerical/link/diff review and ordinary
commit hooks. No product build/test, full/deep CI, coverage, mutation, dependency/schema, ADR/waiver or
release change. The [progress report](../reports/2026-11-19-f6-phases-progress-report.md) and
[handoff](../handoffs/2026-11-19-f6-phases-handoff.md) own integration and cleanup status.
