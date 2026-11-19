# F6 I/O submission and callback boundaries — 2026-11-19

Status: bounded native observation complete; documentation integration and task cleanup pending. #453 remains open.

[Issue #485](https://github.com/hideyukiMORI/NeNeCommander/issues/485) continues
[#453](https://github.com/hideyukiMORI/NeNeCommander/issues/453) from accepted main
`8a1aee8859d72692e1efe7dca4db3d00d997d327`. Its fixed experiment was declared before collection.
The repository change is documentation only. Product sources, dependencies, gates, public beta.2
and the normal running app are unchanged. Permanent source/raw/helpers/build/AX/image evidence:
`D:/NeNeCommander/evidence/f6-io-boundary-20261119/`.

## Invariant and observation boundaries

Native F6 retains mapper → session → gateway → providers → pane reads → host rendering.
Identity, capability, containment, transfer facts, cancellation and invocation order are unchanged.
The sole I/O scheduling boundary retains its original Task.Factory.StartNew, scheduler/options,
task count, awaits and synchronization-context choices. The diagnostic wrapper invokes each original
synchronous operation exactly once and forwards its exact result or exception. ARC-001/005/007/010,
CMD-002/003/009, SEC-010, QLT-014/015 and ADR-0061 apply; no waiver or suppression.

The D-drive export contains1,352 tracked originals, of which only CommanderWindow,
CommanderApplication and WindowsLocalIoExecutionBoundary differ. All other1,349 originals match.
Four added diagnostic sources make1,356 frozen source inputs. Twelve helper inputs and the built
App DLL also match their pre-native hashes after collection. The diagnostic patch is preserved.

The host records raw F6 receipt, mapped Move, return from the existing work await and return from
the final RenderSession. It also brackets the existing initial render and two progress renders.
The endpoint is view value assignment, not physical input, XAML layout, compositor or display.
The final phase includes IoObservation.Close's lock/nine-call validation and host validation.
It must not be named pure final-render time.

One explicitly composed bounded IoObservation records submission, worker-entry and worker-exit
ticks, closed operation kind, completion/throw and managed thread IDs. It stores no path/content.
A lock protects cross-thread access; it adds no task or continuation. An injected clock supports
self-proof. Each sample must contain exactly nine serial callbacks: Inspect1, Inspect2, transfer
preflight, atomic capability1, atomic capability2, Move1, Move2, left read and right read. All
54 callbacks succeed, none is Shell, and all162 ticks are monotonic and within mapped-to-return.

Each middle interval is partitioned into28 disjoint segments: an initial gap, then nine sets of
submission-to-entry, callback and exit-to-next-submission (the last ends at await return).
Their exact integer sum equals the middle interval. Three host-render spans intersect these
segments; each overlap is calculated explicitly. A render can overlap a worker callback, so
render and callback totals must not be naively added. All168 segments and18 render spans validate.

Submission-to-entry includes observer/scheduler/queue costs and preemption. Callback brackets
include synchronous work plus diagnostic locking/timestamps and preemption. Inter-call gaps
include task completion, continuation scheduling, coordinator work and any contained rendering.
None isolates pure queue, disk, CPU or UI delay. No OS scheduling trace or causal evidence was collected.

## Completion and fixed collection

The post-endpoint current-user/session event protocol follows the separately documented
[#482 handshake](2026-11-19-f6-ui-render-spans.md), with a new unique six-event prefix.
The package-free observer opens all initially nonsignaled events before F6 and emits READY.
DONE requires both complete host facts/spans and valid I/O records. CUA buffers each F6 action
in memory; only after reading its matching DONE does the controller acknowledge, save its
explicit log, obtain AX/images or read fixtures. READY, DONE0..5 and all six acknowledgments
agree. Tool/platform logging, input-injection and background OS costs remain unmeasured.

One owned process41792/window12847928 performs one excluded warmup and exactly five measured
fresh same-volume moves, each with41B TXT and43B SQL sources and an empty destination. Ctrl+L/Tab
navigate and Space/Down/Space/Up select. No trial is added/replaced after values. Every sample
has completed Move/two effects/zero untransferred/source0/target2/idle panes/active-left;
all12 final hashes match. AX can briefly retain prior rows/focus during navigation; selected
screenshots and final raw facts/files separately establish the actual result.

## Measured intervals

Stopwatch frequency is10,000,000 ticks/second. All rows, including excluded warmup, are retained.

| Sample | Receipt to mapped ms | Mapped to return ms | Final phase ms | Total ms |
| --- | ---: | ---: | ---: | ---: |
| warmup-00 (excluded) | 0.253 | 96.0859 | 5.5739 | 101.9128 |
| sample-01 | 0.1371 | 24.8541 | 3.1126 | 28.1038 |
| sample-02 | 0.0958 | 25.9703 | 2.8958 | 28.9619 |
| sample-03 | 0.1381 | 26.1763 | 2.9895 | 29.3039 |
| sample-04 | 0.0433 | 22.1548 | 2.6731 | 24.8712 |
| sample-05 | 0.0341 | 25.4682 | 3.8103 | 29.3126 |

The middle partitions are:

| Sample | Submission to entry total ms | Callback total ms | Between/outer gaps total ms |
| --- | ---: | ---: | ---: |
| warmup-00 | 1.6101 | 14.6846 | 79.7912 |
| sample-01 | 0.2454 | 6.1521 | 18.4566 |
| sample-02 | 0.3367 | 5.9954 | 19.6382 |
| sample-03 | 0.2781 | 6.8247 | 19.0735 |
| sample-04 | 0.2445 | 5.875 | 16.0353 |
| sample-05 | 0.2063 | 5.8983 | 19.3636 |

Host-render brackets within those same middle intervals are:

| Sample | Initial render ms | Progress1/2 ms | Progress2/2 ms | Outside render brackets ms |
| --- | ---: | ---: | ---: | ---: |
| warmup-00 | 1.9479 | 0.5944 | 3.7335 | 89.8101 |
| sample-01 | 0.5713 | 0.5819 | 3.4558 | 20.2451 |
| sample-02 | 0.7139 | 0.5218 | 3.5446 | 21.19 |
| sample-03 | 0.5903 | 0.6339 | 3.5379 | 21.4142 |
| sample-04 | 0.6494 | 0.7602 | 3.4009 | 17.3443 |
| sample-05 | 0.5382 | 0.5241 | 3.338 | 21.0679 |

The measured totals are24.8712–29.3126ms, with callback totals5.8750–6.8247ms and
submission-to-entry totals0.2063–0.3367ms. The largest inter-call gap in each measured sample
is between the first atomic-capability callback exit and the second submission:

| Sample | First atomic callback exit to second atomic submission ms |
| --- | ---: |
| sample-01 | 7.8408 |
| sample-02 | 8.814 |
| sample-03 | 8.2232 |
| sample-04 | 9.129 |
| sample-05 | 8.1749 |

These five gaps have zero overlap with the three synchronous host-render brackets. That locates
elapsed time only; it does not identify what ran or waited there. No long case matching #482's
second sample appears in this fixed series, but its absence does not invalidate the earlier
record. This is a different instrumented protocol, so no improvement comparison, subtraction
of earlier non-UI timings, percentile, physical display latency or #453 resolution is claimed.

## Preparation, selected checks and limits

The initial selfcheck was started before preparation created its project and exited1 without
compiling/running code. The next build found IDE0305/IDE0048 in diagnostic code; explicit collection
expression/parentheses corrected them without suppression. Original source and both failures
are retained. A helper-copy match failed on CRLF before writing; LF normalization fixed the match.
All occurred before freeze/native collection; no measured sample followed a failed preparation.

Final package-free selfcheck passes host recorder bounds/order/facts/progress/admission,
I/O bounds/ordering/incomplete/throw/Shell/backward-clock handling, exact callback result/exception
forwarding through the original scheduler, event collision/timeout/registration ownership.
Explicit Release locked restore and Release no-restore App build pass with zero warnings/errors.
Pre-collection independent review required no code correction and identified the final-phase
validation cost and render/callback overlap as interpretation limits. The observer exits0.

The predeclared Python analyzer independently recomputes24 outer ticks,18 render spans,
54 callbacks/162 I/O ticks,168 disjoint segments, six totals,12 hashes,1,356 source inputs,
12 helper inputs and six DONE/ack sequences. Normal close writes the buffered raw trace.
No additional native trial or unrelated product/full CI/coverage/mutation is run.

Successful bounded commands (exact dotnet arguments/timestamps/exits are in build-*-commands.json):

```powershell
python D:/NeNeCommander/evidence/f6-io-boundary-20261119/prepare.py
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-io-boundary-20261119/build.ps1 -Part SelfCheck -Attempt '-style'
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-io-boundary-20261119/build.ps1 -Part Observer
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-io-boundary-20261119/build.ps1 -Part App
python D:/NeNeCommander/evidence/f6-io-boundary-20261119/freeze.py
python D:/NeNeCommander/evidence/f6-io-boundary-20261119/prepare-fixtures.py
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-io-boundary-20261119/observe.ps1
python D:/NeNeCommander/evidence/f6-io-boundary-20261119/verify-fixture.py 0
python D:/NeNeCommander/evidence/f6-io-boundary-20261119/verify-fixture.py 1
python D:/NeNeCommander/evidence/f6-io-boundary-20261119/verify-fixture.py 2
python D:/NeNeCommander/evidence/f6-io-boundary-20261119/verify-fixture.py 3
python D:/NeNeCommander/evidence/f6-io-boundary-20261119/verify-fixture.py 4
python D:/NeNeCommander/evidence/f6-io-boundary-20261119/verify-fixture.py 5
python D:/NeNeCommander/evidence/f6-io-boundary-20261119/analyze.py
python D:/NeNeCommander/evidence/f6-io-boundary-20261119/preserve.py
```

Physical key/display, cold/large-tree/cross-volume/UNC/WSL, AV and latency distributions remain
outside this proof. PR402's hide screen acceptance is separate.

## Lifecycle and preservation

Owned41792 exits by normal Alt+F4/drain at19:16 JST; observer already exited0. Normal38360/window200412
remains. Original218B settings SHA256 remains
`ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`; no restoration was needed.
All2,007 output files/243,657,084 bytes are archived in diagnostic-output.zip with complete
file-size/SHA256 verification; preservation-manifest.json lists every source file and archive hash.
Independent read-only post-collection review recomputed every interval/intersection, checked all
source/helper/file hashes and DONE/ack ordering, and found no required correction. No product
or native test was repeated. A read-only lookup of a guessed verifier filename found no file;
discovery then selected the existing helper, with no mutation from that failed read.

Task worktree: `D:/NeNeCommander/worktrees/issue-485`.
Task output: `D:/NeNeCommander/outputs/f6-io-boundary-20261119`.
Only these two targets may be removed after accepted/clean/untracked/ignored/no-link/no-reference/
complete-archive checks. Branches, commits, permanent evidence and unfinished Bookmark work remain.
Four earlier rejected cleanup targets are not retried. See the
[progress report](../reports/2026-11-19-f6-io-boundary-progress-report.md) and
[handoff](../handoffs/2026-11-19-f6-io-boundary-handoff.md).
