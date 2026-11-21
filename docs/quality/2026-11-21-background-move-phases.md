# Current-main non-UI move phases — 2026-11-21

Issue #513 records this completed observation; parent #453 remains open. hide confirmed that no isolated Windows VM or separate
test PC is available and requested non-UI continuation. This record observes the
existing application/provider path without starting a native App or using shared
keyboard, mouse, focus, Computer Use or UI Automation. It does not complete the
separate native #491 comparison or establish UI latency.

## Invariant, inputs and fixed protocol

All mutations remain DualPaneSession.HandleAsync(UserIntent.Move) ->
FileOperationGateway -> ProviderFileOperationPort -> WindowsLocalIoExecutionBoundary.
The existing provider owns preflight, capability, identity revalidation and atomic
effects; the existing sessions own selection, progress and both pane refreshes.
No product source, dependency, setting or public release changed.

The three non-host Release DLLs are taken from the accepted #510 artifact archive:
candidate c7a1afddffddc9d33e3a5d00ef95492584927770, whose tree equals main
2d372dd4b8a308d4d3bfc553f83b01a93213f658. The original archive SHA256, each selected
DLL's recorded size/hash, copied build references and 450 related source/build
inputs were checked. Existing accepted product proof is reused; no product build
or test was repeated.

The standalone diagnostic reuses #470's public-port recorder. Its bounded changes
are the new owned output root, explicit FileInspectionPurpose parameter with
IdentityOnly admission and unchanged forwarding, a CreateNew no-retry marker, and
provenance fields. It references the accepted Domain/Application/Infrastructure
DLLs and adds no package. The pinned SDK's locked Release restore, helper build
and recorder selfproof completed before collection, with zero warnings/errors.
The selfproof covers immediate/pending success, result identity, one forwarding
call, synchronous/asynchronous faults and deterministic timestamps.

The plan was saved before measurement: one process, exactly six independent
same-volume two-file moves, one excluded warmup then five measurement positions.
Each pair contains the same 106-byte SQL and 105-byte text contents and an empty
destination. Only task-owned D fixtures are admitted; root ancestors and files
are checked for reparse points, and non-move operations/Shell launch are refused.
Every inspection asserts IdentityOnly and forwards the exact purpose object.
The global run has a 30-second cancellation bound. An existing attempt marker
refuses rerun; any failed attempt would stop without replacement or pooling.

The interval begins immediately before the canonical Move call and ends when it
returns after both pane refreshes. The recorder buffers nine public-port await
spans and two progress points in memory, then writes raw output before post-result
validation. No UI synchronization context is installed. Port spans include
scheduling, asynchronous continuation and recorder overhead, not pure OS I/O;
the residual includes application orchestration and instrumentation. Setup,
fixture validation and result-file writes are outside the recorded interval.

## Complete observations

All values below are milliseconds. The warmup is retained and excluded only by
the predeclared plan. There was no extra attempt or outlier removal.

| Position | Total | Inspect x2 | Preflight | Capability x2 | Move x2 | Left read | Right read | Outside ports |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Warmup | 38.7637 | 3.3417 | 4.4957 | 3.6181 | 5.7684 | 0.1516 | 0.3523 | 21.0359 |
| 01 | 6.2371 | 0.1504 | 0.2298 | 2.5411 | 2.8388 | 0.0948 | 0.0945 | 0.2877 |
| 02 | 7.5969 | 0.2146 | 0.2842 | 3.3080 | 3.1915 | 0.1663 | 0.1963 | 0.2360 |
| 03 | 6.0176 | 0.1649 | 0.3048 | 2.2145 | 2.9940 | 0.0949 | 0.0985 | 0.1460 |
| 04 | 5.4041 | 0.1131 | 0.1604 | 2.1795 | 2.5971 | 0.1318 | 0.0946 | 0.1276 |
| 05 | 5.3589 | 0.1335 | 0.1819 | 2.2415 | 2.5460 | 0.0836 | 0.0808 | 0.0916 |

The five planned measurement totals span 5.3589–7.5969ms, median6.0176ms.
Capability and Move are the two largest recorded port groups in each measurement
position. Their spans cannot identify individual syscalls or separate scheduling
from work. The larger warmup residual likewise does not establish a JIT cause.
No improvement percentage, percentile, pure I/O time, physical display result or
general performance guarantee follows from this one small-file observation.
It must not be subtracted from the earlier native measurements to estimate UI cost.

All six results report two atomic effects, no untransferred item, source0/target2
and the expected left0/right2 listings. The Python check independently reads the
raw integer TimeSpan ticks and confirms all raw/final pairs, 54 ordered disjoint
port spans within the total, 12 ordered progress points and 12 target hashes.
The process is6240; the recorded measurement command exited0. No native App was
started. Background host load, filesystem cache and JIT state are uncontrolled.

## Commands, preservation and disposition

Permanent evidence is
D:/NeNeCommander/evidence/f6-background-current-20261121. It includes the fixed
plan, helper source, accepted-DLL provenance, command arguments/logs/exit codes,
before/after hashes, every raw/final row, Python analysis and complete output zip.

The executed preparation/build script uses exactly:

    dotnet restore PhaseProbe.csproj --locked-mode -p:Configuration=Release
    dotnet build PhaseProbe.csproj -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
    dotnet bin/Release/net10.0-windows10.0.26100.0/PhaseProbe.dll selftest

Those commands exited0. After the separate successful freeze, collection and
analysis used:

    pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-background-current-20261121/measure-background-phases.ps1
    python D:/NeNeCommander/evidence/f6-background-current-20261121/analyze.py
    python C:/Users/info/WORKS/NeNeCommander/artifacts/issue491-prepared-bridge-20261121/preserve-background-phases.py

All exited0. The recorded inner collection command is
`dotnet bin/Release/net10.0-windows10.0.26100.0/PhaseProbe.dll measure`.
These are historical executions with single-attempt outputs, not rerun commands.

After collection, all 450 source/build inputs, 36 diagnostic inputs and the
original 428-byte settings/hash remain unchanged. All 64 output files totaling
29,267,945 bytes are archived with complete path, size and SHA256 verification.
Product proof is reused. Repository changes are documents only, so their scope is
text/link/numeric review and ordinary Commit checks, with no product/full CI,
coverage, mutation or native UI verification. No independent-agent review is claimed.

The observation fulfills only this non-UI phase question. #453 still lacks the
native key-to-visible-completion explanation; #491's Series1-9 remain stopped,
incomplete and unpooled. Shared-desktop input must not be used to fill that gap.
PR402 still requires hide's screen acceptance. AppSDK PR498 and release remain
separate. hide authorized publication on 2026-11-22; Issue #513 owns this documentation unit. Integration and cleanup are not yet claimed.

The D worktree issue-453-background and output f6-background-current-20261121 are
retained because publication/acceptance/integration are unfinished. No deletion
was attempted. Keep the branch, commit and permanent evidence after eventual
exact-target/no-link/no-reference/complete-archive cleanup.
