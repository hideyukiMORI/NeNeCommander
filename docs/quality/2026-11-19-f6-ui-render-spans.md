# F6 host render spans and completion handshake — 2026-11-19

Status: native collection and arithmetic complete; documentation integration/cleanup pending.

[Issue #482](https://github.com/hideyukiMORI/NeNeCommander/issues/482) continues the open
[#453](https://github.com/hideyukiMORI/NeNeCommander/issues/453) investigation from accepted
main `d0023d6da2174ff6fcf5d2f2b4ecc3d91694f5b9`. It records a separate predeclared experiment;
it neither replaces nor compares against [#479's different observer protocol](2026-11-19-f6-ui-boundary.md).
Repository product sources, dependencies, gates, public beta.2 and the normal running app are unchanged.
Permanent evidence: `D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/`.

## Invariant, diagnostic scope and endpoints

Native F6 follows the existing mapper → session → gateway → providers → both pane reads →
host rendering route. Identity/capability admission, transfer facts, cancellation, ordering,
and the original task evaluation/await order are preserved. There are no provider/task wrappers,
added awaits or new product execution routes. ARC-001/005/007/010, CMD-002/003/009, SEC-010,
QLT-014/015 and ADR-0061 apply. No waiver or warning suppression is used.

The diagnostic export contains 1,349 tracked files. Exactly two differ: observational calls
and diagnostic-only IDisposable ownership in CommanderWindow, plus one `_window?.Dispose()`
line in the existing composition-root Dispose. The latter was declared before native trials
after ownership analysis rejected a disposable window without its owner's disposal call.
All other 1,347 tracked files, including all Application/provider/Presentation sources, match.
Three diagnostic-only sources make 1,352 frozen source inputs; nine helper inputs are also
frozen and match after collection. The full patch and final source hashes are preserved.

One Stopwatch clock (10,000,000 ticks/second) records the same four outer endpoints as #479:
raw F6 receipt; mapped Move before existing TryStartIntent; return from the existing await;
return from existing final RenderSession before result facts. Admission is established by
the correlated successful result, not by the mapped marker alone. The endpoint is view value
assignment, not physical input, XAML layout, a correlated frame, compositor or display.

Within mapped-to-return, six additional timestamps bracket the existing initial RenderSession
and the two existing OperationProgressed RenderPanes calls. Three complete, disjoint spans
must be contained in that middle interval. One initial span and progress1/2 then2/2 are required;
the validator permits progress before initial rendering because the work argument starts before
RenderAfterAsync enters. The experiment does not reorder that evaluation.

These are instrumented elapsed intervals. Span entry bookkeeping and clock overhead are present;
preemption can occur within a span. The final phase also includes recorder validation following
the await-return marker. None is a pure CPU or uninstrumented render-cost measurement. The
unbracketed middle includes operation/read work, scheduling/continuations and diagnostic overhead;
it can also include deferred UI work caused by earlier rendering. It is not pure provider time.

## Completion handshake and controller observations

At initialization the diagnostic sink creates six named manual-reset events with current-user
and current-session options, rejecting any pre-existing name without resetting it. A separate
package-free observer opens all six before any F6, checks that all are nonsignaled, and emits
READY. Each wait uses an owned one-shot ThreadPool registration, asynchronous TCS continuations,
a finite120-second timeout, unregister in finally and disposal on exit. It sends no UI input,
reads no UI, polls no filesystem and performs no product work.

After the final timestamp and facts, the App signals only when the recorder admits the current
complete record: zero errors/extra receipts, Stage4, eight facts and three closed valid spans.
This guard was the pre-collection independent review's required correction. The initial sink
had signaled even when recorder calls rejected a sequence; its source is retained. Positive
and negative signal-admission cases passed before native collection. The App buffers records
and writes them only after the normal close/drain; signals are outside its timed endpoints.

CUA sends F6 and buffers that action in memory. Root reads the matching DONE from the already
running observer before acknowledging it and flushing the explicit controller log, obtaining
AX/images or checking fixtures. All six READY/DONE/acknowledgment sequences and subsequent
fixture-check times are preserved. Observer stdout/log output occurs after the signal.
This establishes the order of these explicit observations, not the absence of all system I/O.
Tool/platform logging, input-injection implementation, background OS activity and instrumentation
cost remain unmeasured. Event signaling/callbacks can affect subsequent display outside the endpoint.

The API ownership choices follow Microsoft's documentation for
[named event options](https://learn.microsoft.com/en-us/dotnet/api/system.threading.namedwaithandleoptions?view=net-10.0),
[event construction and createdNew](https://learn.microsoft.com/en-us/dotnet/api/system.threading.eventwaithandle.-ctor),
and [one-shot registered waits](https://learn.microsoft.com/en-us/dotnet/api/system.threading.threadpool.registerwaitforsingleobject).

## Fixed collection and values

One owned app, PID32792/window30081280, performs one excluded warmup and exactly five measured
same-volume moves. Each fresh owned fixture has a41-byte TXT and43-byte SQL source and empty
destination. Ctrl+L/Tab navigate; Space/Down/Space/Up select both; native F6 performs the move.
There are no replacement/extra trials after seeing values. All six have successful Move,
two effects, zero untransferred items, source0/target2, both panes idle, active-left and both
content hashes. Twelve final file hashes and the exact original settings match.

| Sample | Receipt to mapped Move ms | Mapped Move to await return ms | Final phase ms | Receipt to final assignment ms |
| --- | ---: | ---: | ---: | ---: |
| warmup-00 (excluded) | 0.2913 | 954.3623 | 11.1709 | 965.8245 |
| sample-01 | 0.1365 | 38.4292 | 5.4395 | 44.0052 |
| sample-02 | 0.1572 | 238.1112 | 5.5301 | 243.7985 |
| sample-03 | 0.1218 | 38.6204 | 4.7476 | 43.4898 |
| sample-04 | 0.0624 | 25.0823 | 2.8065 | 27.9512 |
| sample-05 | 0.0513 | 41.2720 | 4.9088 | 46.2321 |

The middle interval decomposes only into the following observed brackets and remainder:

| Sample | Initial render span ms | Progress1/2 span ms | Progress2/2 span ms | Unbracketed middle ms |
| --- | ---: | ---: | ---: | ---: |
| warmup-00 (excluded) | 3.3265 | 1.2613 | 0.9003 | 948.8742 |
| sample-01 | 1.2326 | 1.0260 | 6.1937 | 29.9769 |
| sample-02 | 0.7916 | 2.3608 | 0.9463 | 234.0125 |
| sample-03 | 0.8165 | 0.9059 | 0.8452 | 36.0528 |
| sample-04 | 0.5255 | 0.9756 | 0.5582 | 23.0230 |
| sample-05 | 0.7913 | 0.8758 | 0.8676 | 38.7373 |

The long second measured case and965.8245ms warmup remain in the record; warmup exclusion was
declared before values. Sample02 has234.0125ms outside the three synchronous render brackets.
This narrows where direct bracket time was spent but identifies no cause. Do not attribute it
to filesystem, AV, JIT, GC or scheduling without evidence, subtract earlier non-UI observations,
derive percentiles, compare it as an improvement over #479, or close #453.

Independent integer/Decimal arithmetic verifies24 outer timestamps,18 contained/disjoint
render spans,18 outer component intervals, six totals and six chronological samples. Recorder
Errors/ExtraReceipts are0 and every sample has Stage4/three closed spans/eight expected facts.
All source/helper/binary hashes match the pre-native freeze. Each post-DONE readback shows the
two destination rows and completion. During fixture navigation, AX sometimes retained previous
rows/focus until later observation; selected-fixture screenshots and final facts/files provide
separate evidence. These reads do not measure display completion.

## Setup, checks and recorded deviations

The first preparation assertion found two matches for a short initial-render fragment and
stopped before writing source. It was corrected to match the full method prefix; resumption
first compared every exported file to git archive. Two initial App build attempts are retained:
the first found partial/format/null-validation/disposal-ownership diagnostics; the second found
the composition owner's missing disposal. Corrections retain analyzer settings and warnings
as errors. Diagnostic formatting exited0 with a workspace-loading warning. No native attempt
preceded the final correction/freeze and independent review.

The final package-free selfcheck proves six exact ordered samples, both initial/progress
orderings, ignored work, bounds, incomplete/overlapping/stale/nested/duplicate/wrong-progress
and backward-clock records. It also proves event collision ownership, already-signaled and
pending waits, finite timeout, subsequent signaling and handle release/recreation. Final
App build succeeds with zero warnings/errors. The observer exits0 after DONE0..5.

After the last fixture check, a separate REPL scalar readback returned initial0/0/-1 values
and its close guard stopped before sending Alt+F4. The closure-maintained action array contains
exactly F6/ack indices0..5, with each matching DONE. It was independently checked before normal
close. No extra F6, native retry, measurement rewrite or pre-ack observation followed this
readback discrepancy. The deviation is retained in controller-actions.json; the scalar readback
is not used as evidence of sample counts.

During final command audit, the inherited diagnostic restore driver was found to omit explicit
Release configuration before a Release build. QLT-014 requires matching configurations. The
measured output was preserved before correcting that driver. Explicit Release restore/build
then passed with zero warnings/errors. Of355 distribution files,354 match byte hashes exactly,
including every executable, DLL, PDB, resource and dependency file. The sole difference is
NeNeCommander.App.xml: seven generated XAML API documentation members disappeared, with every
remaining member and assembly declaration equal. Both XML versions and the parsed difference
are retained. An initial strict all-file comparison failed on this XML; it was not reported as
an all-file pass. The measured executable inputs are identical, so native evidence is reused
without rerunning or relabeling samples. release-rebuild-verification.json records this distinction.

Successful bounded commands and exact arguments/times are retained in build-*-commands.json:

```powershell
python D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/prepare.py
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/build.ps1 -Part SelfCheck -Attempt '-review'
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/build.ps1 -Part Observer -Attempt '-ownership'
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/build.ps1 -Part AppBuild -Attempt '-review'
python D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/prepare-fixtures.py
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/observe.ps1
python D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/verify-fixture.py 0
python D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/verify-fixture.py 1
python D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/verify-fixture.py 2
python D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/verify-fixture.py 3
python D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/verify-fixture.py 4
python D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/verify-fixture.py 5
python D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/analyze.py
python D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/preserve.py
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/build.ps1 -Part App -Attempt '-release-restore'
python D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/verify-release-rebuild.py
```

Read-only source lookups using absent guessed filenames caused no modification; subsequent
discovery used existing paths. This integrates documentation only: no product tests, full CI,
coverage or mutation are added. Display/physical-key, cold/large-tree/cross-volume/UNC/WSL,
AV, latency distribution and hide's PR402 screen acceptance remain outside this proof.

## Preservation and lifecycle

PID32792 exited by normal Alt+F4/drain at18:30 JST; observer already exited0. Normal38360/window200412
remains. The original218-byte settings SHA256 is unchanged:
`ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.
No settings restoration was needed. All2,029 measured-output files/188,598,393 bytes are
preserved in diagnostic-output.zip with complete size/SHA256 verification. The subsequent
matching-configuration restore/build changes eight generated output files, preserved and
hash-verified in release-restore-supplement.zip. The current2,029 files/188,600,262 bytes are
fully covered by the original archive plus supplement; no prior evidence is replaced.

Independent read-only post-collection review independently recomputed all six totals and
remainders, verified18 span relationships and post-ack fixture times, and found no required
correction to the numerical or observer-order claims. It explicitly separated the then-pending
QLT-014 correction; the completed restore/binary/documentation comparison above closes that
local proof item. No review re-executed the product or native trials.

Documentation lives in `D:/NeNeCommander/worktrees/issue-482`; disposable output is
`D:/NeNeCommander/outputs/f6-ui-render-spans-20261119`. Only after integration, clean/untracked/
ignored/link/live-reference checks and complete preservation may these exact two paths be
removed. Branches, commits, permanent evidence and unfinished Bookmark work remain. Four
older rejected cleanup targets are not retried. See the [progress report](../reports/2026-11-19-f6-ui-render-spans-progress-report.md)
and [handoff](../handoffs/2026-11-19-f6-ui-render-spans-handoff.md).
