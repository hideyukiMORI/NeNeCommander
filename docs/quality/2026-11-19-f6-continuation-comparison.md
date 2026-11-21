# Native capability-continuation comparison preparation

Status: both native series stopped as inconclusive; Series 2 preserves ten complete moves but lacks the planned ABBA comparison. Issue #491 remains incomplete; PR #492 stays Draft.

[Issue491](https://github.com/hideyukiMORI/NeNeCommander/issues/491) follows accepted
[#488](2026-11-19-capability-continuation.md). No completed A/B comparison, speedup, cause or resolution
of #453 is claimed. Incomplete-series intervals below are descriptive only. Repository changes here are documentation only.

## Fixed plan and invariant

The complete plan was saved before collection at19:56 JST in
`D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/plan.md` and Issue491.
Four fresh-process blocks A1/B1/B2/A2 each contain one excluded warmup and three measured moves:
16 total, four excluded in advance, twelve measured. Every move uses fresh owned same-volume
two-file fixtures with identical content and size. Stop on invalid preparation/outcome/observation;
retain every failure and outlier, with no replacement or extra sample.

Both exports start at main009edf2. A restores TransferExecution.cs from parent4d95310; B keeps
the accepted ConfigureAwait(false). Their only source difference is that await and its comment.
Native F6 retains mapper/session/gateway/providers/refresh/render, serial full-batch admission,
identity/capability checks, cancellation, lease, effects/progress owner and original scheduler.
ARC-001/005/007/010, CMD-002/009, QLT-014/015 and ADR-0061 apply. No waiver or new product path.

The same diagnostic overlay records four host timestamps, three render spans and nine I/O
callbacks with submission/entry/exit ticks. Primary comparison: atomic1 exit to atomic2 submission.
All other intervals and totals are descriptive secondary results. ABBA limits order drift but
does not remove JIT/cache/time drift, preemption or interactions with the observer. No percentile,
pure queue/disk/CPU interval, physical display or general perceived-latency claim is permitted.

PID-scoped current-user/session events signal after the validated endpoint. The observer waits
exactly four signals; the recorder keeps its existing six-slot capacity but must finish at Count4.
Explicit controller logging/AX/images/file reads follow each matching DONE. Tool/injection/OS
costs remain unmeasured. CreateNew raw-PID JSON is written after normal close; PID reuse requires
an absent output path after PID identification and before the first F6. A2/B2 must reuse the same binaries as A1/B1.

## Completed preparation and exact checks

Each export contains1,366 frozen source files; the12 helper inputs are frozen. Source comparison
proves the only A/B difference is TransferExecution. The existing diagnostic overlay is recovered
from hash-verified #485 artifacts. Its change is process-scoped signal/output names and a four-event
observer; no task/scheduler/measurement-boundary change is introduced.

The following commands all exit0; exact argument arrays/times/logs are preserved in evidence:

```powershell
python D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/prepare.py
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/build.ps1 -Part SelfCheck
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/build.ps1 -Part Observer
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/build.ps1 -Part App -Variant A
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/build.ps1 -Part App -Variant B
```

App restores explicitly use locked Release configuration; both Release builds have zero
warnings/errors. A finishes19:59:40; B finishes20:00:17. Diagnostic selfchecks cover recorder bounds,
ordering/facts, exact callback forwarding, incomplete/throw/Shell/backward-clock handling and
event collision/pending/timeout/disposal. Independent read-only pre-collection review finds no
required correction and identifies Count4/fixture freeze/PID collision as collection preconditions.
After build, all source/helper hashes remain equal, and355 distribution files per variant are frozen
in build-complete-freeze.json. No product regression, hosted CI, coverage or mutation is repeated.

## Stop and resumption boundary

The first Computer Use window inventory reported that physical Escape stopped the tool.
No Issue491 app launch, fixture move or native sample occurred. No further UI calls followed.
The initial handoff recorded builds as pending; authoritative completion subsequently established
both successes. The read-only review was interrupted, then completed during non-UI continuation.

Sixteen fixture pairs are now prepared, all with identical41B/43B contents and empty destinations.
Their32 hashes and the unchanged218-byte settings are recorded. The memory-only trial ledger
passes16 synthetic admissions and rejects wrong ABBA order, a fifth sample, mismatched/duplicate
DONE, actions/log snapshots before DONE and retry after unknown input. No UI API is imported by
that check. `node D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/trial-ledger-check.mjs`
and `python D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/prepare-fixtures.py` exit0.
The observer launch wrapper is syntax-checked only; its real named-event handshake is unexecuted.

Native collection, independent raw analysis, outcome/hash checks and final acceptance remain. Do not restart UI work before hide
resumes it. Revalidate frozen inputs and exact process/raw ownership before any future sample.
The normal app, settings and public beta.2 have not been modified by this task.

Retain `D:/NeNeCommander/worktrees/issue-491` and
`D:/NeNeCommander/outputs/f6-continuation-comparison-20261119` because the experiment is incomplete.
After final technical acceptance/integration/preservation, check absolute targets, clean/untracked/
ignored state, unique artifacts, links and live references before cleanup. Keep commits/branches/
permanent evidence. Four old rejected cleanup targets remain separate and are not retried; their
exact targets and command are in the [current handoff](../handoffs/2026-11-19-capability-continuation-handoff.md).

## 2026-11-20 resumption amendment, before collection

hide's current ongoing milestone direction resumes the paused work. No sample
was collected on the previous day. At 09:33:03 JST, the resume preflight verifies
all 1,366 source inputs and 355 distribution files per variant, the 12 helper
inputs, two controller files and all 16 untouched fixture pairs/32 hashes.
No App process is present; the former normal PID38360 no longer exists.

The user's settings changed before collection from 218 to 428 bytes. Preserve
the current 428-byte document/hash as the common environment for every block;
never restore the old settings. The current settings hash is
7ea7ab2fb5b06a68d6015500a227ba7aea3b7f1da322430834b4b1361b2630ef.
The fixed A1/B1/B2/A2 order, warmup1/measured3 counts, two-file fixtures, frozen
binaries and diagnostic/observer boundaries remain. This compares the historical
009edf2 exports and one controlled collector difference, not the subsequently
changed main product. No build or already successful helper check is repeated.

The first resume preflight's broad observer-B*.log glob matched the pre-existing
observer-build-A.log on Windows. It stopped before any App launch or sample.
The check was corrected to four exact block observer paths and then passed;
resume-preflight-initial-failure.txt preserves that preparation error. Native
collection and real event handshake are still unexecuted at this amendment.

## Original series: stopped, not extended

Owned A1 App6036 ran two moves. Warmup received DONE0 and source0/target2/two
hashes passed. The observer began waiting for sample1 immediately after DONE0,
including controller preparation time. Its 120-second timeout ended at09:39:21.
The next F6 receipt was165.536559seconds after the preceding final assignment,
so its required DONE1 could not be obtained. The controller permanently aborted;
no further original sample or block ran and no replacement is allowed.

After normal App close, raw-6036-aborted.json contains Count2/Errors0/ExtraReceipts0,
Io Count2/Errors0,18 ordered callbacks and6 complete render spans. Both moves
succeeded and all32 fixture hashes validate across the two moved and14 untouched
pairs. Settings remain428bytes/hash-equal and App6036 exited. The second raw
sample is retained but is not an accepted comparison observation. Accepted A/B
comparison samples:0. No speedup, product hang or #453 resolution follows.
The startup accessibility-only click lacked geometry; a fresh screenshot restored
setup before the first F6. The next right-address click took about25seconds at
the controller tool boundary; no individual cause or product latency is inferred.

## Series 2 preparation: separate fixed protocol

The complete series2-plan.md was saved before new collection. It preserves the
original failed series and frozen A/B Apps, adds16 fresh fixture pairs, and fixes
A1/B1/B2/A2 with one excluded warmup and three measured moves per block. None of
the original data is pooled, replaced or reused as Series2 samples.

Only the observer procedure changes: after each sample's full navigation and
selection, start one observer for that sample. It checks all6 existing events
(previous signaled, current/future unsignaled), announces READY index, waits up
to120seconds for that sample, emits DONE index and exits before next preparation.
App binaries, endpoint signals, recorder and source remain unchanged. Observer
startup/platform/background effects are still unmeasured; no pure latency claim.

The package-free new observer's locked Release restore/build exits0 with zero
warnings/errors. Its self-check accepts all four readiness histories and rejects
missing previous, signaled current and every future slot. Invalid PID0 and index4
both exit nonzero. Exact commands/logs are in series2-20261120/checks.json.
Independent read-only review finds no mandatory correction: ManualReset events
are not consumed, a completion immediately after READY is retained, and one-sample
observer lifetime excludes next preparation from its wait. Existing successful
checks were read rather than rerun. New fixture/helper freezing, native READY/DONE
collection and post-collection arithmetic review remain; no Series2 sample yet.

## Series 2 collection and interruption — 2026-11-20

The new fixture and observer manifests were frozen at 09:49:32 JST. A1 PID36068
and B1 PID40536 completed four moves each; B2 PID37120 completed its warmup and
first measured move. Every completed move received the matching READY/DONE,
showed both loaded panes and successful status, and moved exactly two files with
matching hashes. Each selected mark was observed with focus on the other row.
The apps were closed normally before reading their raw output.

At B2 index2, after the observed READY2, the F6 tool call returned `aborted`.
The next observer poll returned `Unknown process id 13597`; the persistent
controller and UI bindings were also unavailable. No second F6 was sent. The
fixed unknown-input stop condition permanently ended Series2. A fresh binding
was used only to preserve the interrupted state and close the owned app normally.
The resulting raw Count2/Errors0/ExtraReceipts0 and untouched index2 source2/target0
establish that no third F6 receipt occurred. B2 index3 and all A2 samples were
never attempted. The observer log retains READY2 only; no exit record is present,
and no matching observer process remains. A successful exit or timeout is not inferred.

The last durable controller snapshot is `controller/B2-1.json`; the subsequent
index2 navigation and mark observations were not saved by the post-DONE route
before the runtime loss. The retained interruption screenshot and state are
separate evidence, not a reconstructed ledger. `interruption.json` records this
limit. The result is ten complete moves (three predeclared warmups and seven
predeclared measured observations), six untouched fixture pairs and **zero
samples accepted for a final ABBA comparison**. There is no replacement, pooling
with the first failed series, or continuation of this stopped series.

Two collection incidents remain explicit. The first post-DONE controller write
to D returned EPERM; the unchanged in-memory capture was staged in the existing
repository's ignored artifacts directory and copied to permanent D evidence with
hash equality. This was a filesystem error, not an automatic approval rejection.
During B1 index2 preparation a right-address Return/capture call took about 22.5s
at the tool boundary, and an extra read-only capture was needed to observe the
left rows. No product duration or cause follows from that controller timing.

## Descriptive rows from the incomplete Series 2

All values are milliseconds. `W` marks the warmups excluded by the original plan;
`M` marks the planned measurement positions, not acceptance for an A/B estimate.
The primary gap is atomic1 callback exit to atomic2 submission. Total runs from
host F6 receipt to final view assignment; it is not physical display latency.

| Block/index | Role | Total | Receipt to submission | Middle | Return to final assignment | Primary gap |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| A1/0 | W | 130.4784 | 0.4461 | 122.2261 | 7.8062 | 10.7561 |
| A1/1 | M | 31.4687 | 0.1007 | 28.2164 | 3.1516 | 10.4168 |
| A1/2 | M | 32.7066 | 0.0775 | 28.7718 | 3.8573 | 12.4611 |
| A1/3 | M | 29.5319 | 0.1086 | 26.5549 | 2.8684 | 0.3295 |
| B1/0 | W | 103.3579 | 0.6118 | 95.7478 | 6.9983 | 0.5606 |
| B1/1 | M | 45.4256 | 0.1172 | 40.4627 | 4.8457 | 0.0233 |
| B1/2 | M | 40.2908 | 0.0922 | 36.6771 | 3.5215 | 0.0221 |
| B1/3 | M | 36.0373 | 0.1230 | 32.8785 | 3.0358 | 0.0284 |
| B2/0 | W | 81.9824 | 0.4373 | 75.3949 | 6.1502 | 0.3682 |
| B2/1 | M | 26.2297 | 0.1049 | 23.5630 | 2.5618 | 0.0160 |

The middle interval partitions exactly, in integer ticks, into callback duration,
submission-to-entry intervals, between-callback gaps and the two edge intervals.
Render spans can overlap callback execution intervals and must not be added to this partition.
The overlap column is their intersection with callback entry-to-exit intervals.
None of these columns measures pure disk time, queue delay or CPU time.

| Block/index | Callbacks | Submission to entry | Between callbacks | Edges | Render | Render/callback overlap |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| A1/0 | 20.0172 | 1.6447 | 87.9798 | 12.5844 | 4.1925 | 0.0000 |
| A1/1 | 9.1226 | 0.7856 | 18.0738 | 0.2344 | 5.5863 | 0.1796 |
| A1/2 | 8.3933 | 0.7106 | 19.3775 | 0.2904 | 5.3806 | 0.1211 |
| A1/3 | 8.7417 | 0.6578 | 16.9643 | 0.1911 | 5.1293 | 0.1201 |
| B1/0 | 21.9790 | 2.3739 | 55.2261 | 16.1688 | 4.8764 | 0.0000 |
| B1/1 | 10.5575 | 1.0151 | 21.3809 | 7.5092 | 6.5523 | 0.1434 |
| B1/2 | 11.3275 | 1.3194 | 23.6287 | 0.4015 | 7.6472 | 0.4417 |
| B1/3 | 8.1895 | 0.6419 | 19.3111 | 4.7360 | 5.6035 | 0.1283 |
| B2/0 | 15.4107 | 2.2240 | 45.5503 | 12.2099 | 8.6281 | 0.0000 |
| B2/1 | 7.9680 | 0.6611 | 14.7102 | 0.2237 | 4.7736 | 0.1462 |

Within the completed rows, A1's three planned measurement positions have primary
gaps 0.3295–12.4611ms and totals29.5319–32.7066ms; B1's three positions have gaps
0.0221–0.0284ms and totals36.0373–45.4256ms. B2 has only one such position
(gap0.0160ms, total26.2297ms); A2 has none. These are incomplete-block descriptions,
not a pooled effect estimate. A smaller primary gap must not be described as
an overall speedup. In A1 the second capability submission uses the same recorded
submitter ID as the first; in B1 and the two retained B2 rows it uses the first
capability callback's worker ID. IDs are process-local and do not identify a
thread's type or establish scheduling cost. Fixed order, cache/JIT, preemption
and observer effects remain uncontrolled, and the missing final block prevents
the declared comparison.

## Verification and remaining work

`analyze-block.py A1` and `analyze-block.py B1` exited0 after their respective
blocks. `analyze-aborted-series2.py` exited0 after shutdown: ten ordered records,
90 expected callbacks,30 contained render spans, exact integer-tick interval
partitions, matching ten READY/DONE records and outcomes,32 valid fixture hashes
across ten moved/six untouched pairs, and unchanged428-byte settings. The final
`verify-freeze.py after-abort-series2` exited0: each variant's1,366 source and355
binary inputs,12 original helper inputs, two controllers, six new observer source
inputs, seven observer binaries and three new helpers all match the frozen hashes.
No successful product build/test, full gate, coverage, mutation or hosted CI was
repeated. Exact files and results are under the permanent `series2-20261120`
evidence directory alongside both failed-series plans and raw observations.

Independent read-only post-collection review confirms all ten samples/110 values,
90 callbacks,30 render spans, exact tick partitions/intersections,ten READY/DONE
records and saved ledger order,20 table rows,32 file hashes and settings. It found
one required wording correction: the overlap describes callback entry-to-exit,
not submission-to-exit; that text is corrected with no numeric change. Existing
freeze evidence was reused. The six untouched pairs include B2/index2, whose
preparation and F6 tool call were attempted without a recorded receipt. Observer
exit and interruption cause remain unknown. No required correction remains.

Issue #491's
requested completed ABBA result is still unproved; PR #492 remains Draft and
must not close the Issue on the strength of this partial evidence. No third
campaign is started by this record. Any future campaign requires a separately
fixed plan and fresh fixtures, preserving these failed attempts and the same
limits. The current worktree/outputs remain because comparison acceptance and
integration are incomplete. The four older rejected cleanup targets remain
excluded from this task. Normal app, settings and public beta.2 are unchanged.

## Observer recovery preparation — 2026-11-21

The [bounded recovery preparation](2026-11-21-observer-recovery-preparation.md)
identifies the missing durable observer identity and proves fresh local process
observation after discarding the original handle, including stale-identity refusal.
Its initial JSON-date mismatch, corrected integer-time proof and limits are retained.
No third campaign, new F6 sample, old-series continuation or accepted A/B result is
introduced. Historical observer exit and interruption cause remain unknown.

### 2026-11-21 Series3 pre-input stop

The [fixed Series3 preparation](2026-11-21-f6-series3-preflight.md) verified frozen
A/B inputs and new fixtures, but its initial ownership command incorrectly compared
the returned lowercase executable path case-sensitively. The guard stopped before
F6; normal-close raw records Count0/IoCount0. All inputs/settings/fixtures remain
unchanged and no App remains. Series3 was not resumed and adds no timing sample.
Its corrected future launch checker has focused proof; publication remains pending.


### 2026-11-21 stopped campaigns and background-only testing

The [Series8/9 stop and input-isolation record](2026-11-21-f6-series8-9-stops-and-input-isolation.md)
preserves the final incomplete campaign: 15 acknowledged moves, then a physical
Escape stop with no last-sample DONE. All inputs/settings and 32 fixture hashes
match; the owned App was normally closed without shared input and 428 transfer
files are archived. Accepted comparison samples remain zero. hide's restated
background-testing constraint supersedes old shared-desktop Computer Use plans;
none of Series1-9 may be resumed or pooled.
