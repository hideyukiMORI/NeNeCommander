# Native capability-continuation comparison preparation

Status: prepared and built; native collection paused after Computer Use reported physical Escape.

[Issue491](https://github.com/hideyukiMORI/NeNeCommander/issues/491) follows accepted
[#488](2026-11-19-capability-continuation.md). No new timing result, speedup, cause or resolution
of #453 is claimed. Repository changes here are documentation only.

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
an absent output path before launch. A2/B2 must reuse the same binaries as A1/B1.

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

Fixture preparation, controller implementation/limit proof, native collection, independent raw
analysis, outcome/hash checks and final acceptance remain. Do not restart UI work before hide
resumes it. Revalidate frozen inputs and exact process/raw ownership before any future sample.
The normal app, settings and public beta.2 have not been modified by this task.

Retain `D:/NeNeCommander/worktrees/issue-491` and
`D:/NeNeCommander/outputs/f6-continuation-comparison-20261119` because the experiment is incomplete.
After final technical acceptance/integration/preservation, check absolute targets, clean/untracked/
ignored state, unique artifacts, links and live references before cleanup. Keep commits/branches/
permanent evidence. Four old rejected cleanup targets remain separate and are not retried; their
exact targets and command are in the [current handoff](../handoffs/2026-11-19-capability-continuation-handoff.md).
