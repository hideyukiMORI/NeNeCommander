# Observer recovery preparation — 2026-11-21

Issue #491; PR #492 remains Draft. This is diagnostic preparation, not a third
native campaign or an accepted A/B result. Both stopped series remain unchanged.

## Confirmed gap

Series2's `observe.ps1` writes its command record only after dotnet exits. The ten
completed attempts have records; B2/index2 has only `READY 2` and no command record.
Those records identify the owned App PID, not the observer PID or creation time.
The memory-only TrialLedger also saved the last controller snapshot after DONE,
so B2/index2's preparation cannot be reconstructed from the retained ledger.

This explains why the retained artifacts cannot independently identify that lost
observer process. It does not explain what interrupted the tool/runtime or prove
that the observer timed out. Its historical exit status remains unknown.

## Bounded process proof

A dedicated hidden PowerShell child wrote its PID, creation identity and executable
before waiting for a task-owned release marker. The parent disposed its original
Process object, then used fresh OS process observations and the persisted receipt.
No App, UI input, F6, provider operation or old fixture was used.

The first proof failed: ConvertFrom-Json materialized the ISO creation string as a
DateTime, making the string comparison reject the actual worker. The retained
receipt reproduces that comparison mismatch. The worker then hit its predeclared
30-second release timeout; its stderr and failed proof remain. No exit code was
recovered after disposing the original handle.

A separate corrected proof stores creation time as integer UTC ticks. It passes:

- the independently observed worker is Alive after the original object is disposed;
- changing the creation tick refuses the identity;
- changing the executable path refuses the identity;
- after release, a fresh OS lookup finds the worker absent.

The original and corrected workers were subsequently confirmed absent. A failed
OS query, including access denial, is not an absent-process result. No process was
killed, and neither proof claims a recoverable exit code or a security boundary.
PID/creation/path identify the local diagnostic instance for observation; they do
not authorize new input or establish an operation's outcome.

## Consequence for a future campaign

Before the next F6, persist the prepared controller snapshot and an observer start
receipt including PID, numeric creation time, executable and exact arguments.
On a lost tool handle, inspect that specific identity instead of treating the tool
handle error as process termination. An unknown F6 result still stops the campaign;
no receipt or later process observation permits retrying or replacing the sample.
The old observer and ledger have not been modified by this preparation. Adoption
needs a separately fixed future protocol and its relevant helper proof first.

Evidence is under
`D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/recovery-20261121`:
original/corrected helper sources, receipts, stdout/stderr, initial failure,
date-conversion diagnosis, corrected result and final process observation.

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/recovery-20261121/prove-process-recovery.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/recovery-20261121/corrected/prove-process-recovery.ps1
```

These identify completed historical attempts (exit1 then exit0), not commands to
overwrite their evidence. Repository changes are documentation only: text/link/diff
review and ordinary Commit checks; no product build/test/full CI/mutation. The
preparation does not validate the current App's speed, solve #453, complete #491,
accept PR402 or change release status. No independent review is claimed.
