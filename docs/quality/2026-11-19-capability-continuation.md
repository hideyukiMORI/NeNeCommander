# Atomic capability continuation ownership — 2026-11-19

Status: PR489 integrated; this task worktree/output cleanup complete. #453 remains open.

[Issue #488](https://github.com/hideyukiMORI/NeNeCommander/issues/488) removes an unnecessary
captured-context dependency inside the existing capability collector. Base is accepted main
`4d95310f840f171abcae26c723c6202d148cedb2`. This follows the separately recorded
[#485 I/O-boundary observation](2026-11-19-f6-io-boundary.md), without attributing that
experiment's gaps to a cause or claiming a measured speedup. #453 remains open.

## Invariant and implementation

`TransferExecution.GetAtomicMoveCapabilitiesAsync` now applies `ConfigureAwait(false)` only to
its inner capability await. Its state is a local list plus an owned immutable plan/request/destination.
It neither updates effects/session state nor publishes progress. `ExecutePlanAsync` retains the
ordinary await of the complete collector; mutation, effects and progress therefore resume on the
existing owner context. Already-complete tasks need no thread/context switch.

The gateway/provider route, single execution lease, original I/O scheduler, invocation count/order,
frozen identities/targets, complete capability admission, cancellation precedence and exact fault
propagation remain. No provider parallelism, task, cache, dispatcher, port, dependency, telemetry
or alternate route is added. All required capability queries still precede every mutation, and
effect-time provider revalidation is unchanged. ARC-001/004/005/007/010, CMD-001/004/005/006/007/009,
CS-014/015/016/027, QLT-014/015, ADR-0019/0027/0032/0058/0061 and GIT-001..004 apply. No waiver.

The production diff is one await expression and its ownership comment in TransferExecution.cs.
Four Application test files own a controlled context, delayed capability port, progress recorder
and five behavioral cases. No other product file or public contract changes.

## Causal proof and selected scope

The initial delayed-capability case was built against the old production source and failed only
`Assert.IsTrue(advancedWithoutOwner)`: owner pumping was required before query2. The test drains
the operation before asserting, so this is a behavioral red, not a timeout or abandoned task.
After the one-await change the same case passes. Both source snapshots and complete logs remain.

Five final deterministic cases use pending TCS answers and a manually advanced SynchronizationContext:

- Query2 begins while owner pumping is withheld; no mutation/progress occurs before answer2 or
  the eventual owner turn. After that turn, ordered Move1/Move2 and progress1/2 then2/2 use the owner.
  Reentrant gateway entry remains refused while the first operation owns its lease.
- Cancellation during pending query1 prevents query2 and every effect/progress report.
- A delayed failure from query2 refuses the whole batch with zero effects/progress.
- A delayed defect preserves the exact exception instance; a following request reaches inspection,
  proving that the existing lease is released.
- Precompleted answers finish inline on the original context and do not post a forced continuation.

No sleep, wall-time threshold, fixed thread ID or synchronous blocking wait is used. The fake port
records contexts at calls, and the outer owner turn is admitted separately from capability completion.
The old-source drain path permits a future regression to fail its assertion rather than merely hang.

ADR-0061 scope is ordinary focused verification: one internal continuation policy changes, with
unchanged public port, scheduler, frozen admission and effect boundaries. The controlled async proof
directly covers its new scheduling edge; existing transfer/cancellation/provider/presentation cases
cover consumers. Independent read-only design and final-code review find no required correction and
agree that this scope is sufficient. No full/deep CI, full-layer coverage or mutation was requested.

| Selected check | Result |
| --- | --- |
| Original-source delayed-context regression | expected 1 failure at the causal assertion |
| Changed-source same case | 1 passed |
| Final new ownership/cancel/failure/fault/synchronous cases | 5 passed |
| Affected Application transfer/admission/continuation/lifetime cases | 67 passed, including the new5 |
| Windows local move and shared execution boundary | 8 passed |
| Transfer presentation | 6 passed |
| Application tests, App and consumer builds | zero warnings/errors |
| Changed-file whitespace | corrected exit0, with a workspace-loading warning |
| CS-013 Inspect | exit1 for the identical26 existing findings; no added path/metric/value/limit |

Counts overlap and are not summed as distinct coverage. The CS-013 result is not an all-green
repository complexity claim. Source interpretation and callback assertions establish ownership;
they do not measure real UI latency or prove the earlier observed gap's cause.

## Recorded attempts and evidence reuse

The first whitespace check returned2 because the four newly added test files used LF rather than
the prescribed CRLF. All diagnostics were ENDOFLINE. Their exact earlier bytes and1011-input manifest
are retained. Byte equality after LF normalization proves the correction changed only line endings;
the final manifest records the corrected hashes. Successful behavior tests are reused, with no
product or test semantic change and no additional build. The measured native App DLL remains identical.

The native preparation's first Ctrl+L/type/Return sequence did not establish the requested address;
the screenshot showed a different directory. It was retained, then the visible address editor and
the intended owned folders were confirmed before any F6. No cause is assigned to that input mismatch.
One F6 was sent after two selected fixture files and an empty destination were visible.

Final Release app PID25776/window8589096 completed the40B TXT/43B SQL move. AX/image evidence shows
completion and loaded panes; independent file checks find source0/target2 and matching two hashes.
All1011 relevant source/test/dependency/tool inputs and the App DLL match the final manifest.
This is one functional smoke, with no timer or comparison. It does not replace the deterministic
owner-context proof. Settings remain the original218B/SHA256
`ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.

Normal Alt+F4/drain closes only25776; the subsequent inventory and process record retain normal
38360/window200412. No settings restoration, public release or normal-app replacement occurred.

## Exact bounded commands

Complete output, source snapshots, manifests, native inputs/AX/images and exact dotnet argument
arrays/start/end/exit records live at `D:/NeNeCommander/evidence/capability-continuation-20261119/`.

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/run-step.ps1 -Step RestoreApplication -Attempt initial
pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/run-step.ps1 -Step BuildApplication -Attempt red
pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/run-step.ps1 -Step TestContinuation -Attempt red
pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/run-step.ps1 -Step BuildApplication -Attempt green
pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/run-step.ps1 -Step TestContinuation -Attempt green
pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/run-step.ps1 -Step BuildApplication -Attempt boundaries
pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/run-step.ps1 -Step TestContinuation -Attempt boundaries
pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/run-focused.ps1 -Part Application
pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/run-focused.ps1 -Part Consumers
pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/run-focused.ps1 -Part Format
python D:/NeNeCommander/evidence/capability-continuation-20261119/normalize-source-lines.py
pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/run-format-corrected.ps1 -Part Format
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
python D:/NeNeCommander/evidence/capability-continuation-20261119/prepare-native.py
python D:/NeNeCommander/evidence/capability-continuation-20261119/verify-native.py
python D:/NeNeCommander/evidence/capability-continuation-20261119/preserve.py
```

Restore explicitly uses Release and locked mode before Release no-restore builds. The preserved
native actions are CUA keyboard operations, not a custom input harness. The new direct assertions
and existing selected cases are the required evidence; commit/push/merge alone does not invalidate it.

## Preservation and remaining boundaries

The task worktree is `D:/NeNeCommander/worktrees/issue-488`; disposable output is
`D:/NeNeCommander/outputs/capability-continuation-20261119`. All1,568 artifacts/458,189,082 bytes (1,556 generated ignored worktree files and12 output files)
are preserved in task-artifacts.zip with complete entry size/SHA256 verification. The manifest
also retains each source size/mtime and archive hash. Tracked source remains in commits; red and
pre-format source snapshots remain in evidence. Cleanup verified accepted integration,
clean/untracked/ignored state, absolute targets, links, live references and unchanged preservation. Branches,
commits, permanent evidence and unfinished Bookmark work remain. Four earlier cleanup rejections
are separate and must not be retried.

PR402's hide screen acceptance, actual uninstrumented/display latency, broader providers/environment
proof and public release remain outside this change. See the
[progress report](../reports/2026-11-19-capability-continuation-progress-report.md) and
[handoff](../handoffs/2026-11-19-capability-continuation-handoff.md).


## Integration and cleanup completed

[PR489](https://github.com/hideyukiMORI/NeNeCommander/pull/489) merged by normal expected-head squash
at19:49:28 JST as e1ebdf4. Accepted0d658ad and the squash share tree
a4ddb9ba0aac60b9f3b4423da70eb046d2566e0c. Local main advanced by fast-forward; requested reviews,
unresolved threads and failed checks were absent. All1011 proof inputs and212 relative links
were checked, and ordinary112-rule/24-adversarial-registration/security/commit-message hooks passed.
No unchanged product proof was repeated or additional CI requested.

`pwsh -NoProfile -File D:/NeNeCommander/evidence/capability-continuation-20261119/cleanup-completed.ps1
-ExpectedHead 0d658ad944ab1be00b1d01d82f389f3496547e9c -PrNumber 489` exited0 at19:49:43 JST.
Both exact targets are absent. Preflight checked absolute/resolved targets, accepted/squash tree
equality, clean and untracked state, the preserved1556 generated ignored files and12 output files,
no links/live references, archive hash and unchanged file sets/sizes/mtimes. Complete prior content
hash verification was reused. Branches/commits/permanent evidence remain; four old rejections
were not retried.
