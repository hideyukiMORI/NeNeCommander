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

## Durable admission and real-observer handshake preparation

A new diagnostic experiment under the same evidence root at
durable-admission-20261121 extends the historical TrialLedger's sendF6 admission.
The caller must supply an awaited persistence callback; the tested callback saves
the pending block/App PID/sample/actions and observer receipt with CreateNew and
flush before invoking input. This is a caller contract, not a security boundary:
an arbitrary no-op callback is not claimed to prove disk persistence. A recreated
controller using the same attempted sample path is refused; no resume API exists.
Acknowledgment is refused while persistence/input delivery remains in flight.
An explicit stop during persistence also prevents subsequent input.

Two old-helper probes demonstrate the missing persistence hook and acceptance of
an early acknowledgment during an unsettled synthetic input callback. They do not
show that a previous campaign actually issued such an early acknowledgment or
explain its runtime interruption. The final helper passes 16 synthetic ABBA inputs
and 11 additional refusal/failure groups (missing callback, six malformed receipt
cases, same-path recreation, filesystem open failure, unknown input, stop during
persistence). Saved preparation exists before each input and callback mutation
does not mutate the ledger. Decimal-text creation ticks preserve Int64 precision
through JavaScript; the earlier PowerShell-only proof's integer ticks remain valid.

The new start-observer.ps1 checks the supplied owner identity, saves an exclusive
launch intent, launches the unchanged historical observer hidden, and immediately
writes its independently observed PID/creation/executable/exact arguments. Its
second launch into the same attempt directory is refused without replacing bytes.
A dedicated PowerShell process owned synthetic event slots under its own PID.
The real observer5784's identity matched a fresh OS read, Node saved preparation
before requesting event0, exact DONE0 was acknowledged, both children exited0,
and the observer was then absent. No native App or real UI/key input was involved.

Commands executed successfully:
- C:/Program Files/nodejs/node.exe D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/durable-admission-20261121/run-proof.mjs
- pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/durable-admission-20261121/prove-handshake.ps1 -AttemptRoot D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/durable-admission-20261121/handshake-attempt1

Source preparation initially refused an exact replacement because of LF/CRLF
mismatch, before writing the candidate helper. The source was normalized in memory.
The first Node-tool call did not execute after idle reset; its retry was denied at
mkdir before creating the attempt. The existing same-user Node executable then ran
the prepared proof successfully. These environmental/preparation issues are retained.

Historical helper hashes and the reused observer binary are unchanged; no App runs
at the final observation. Raw attempts, old fixtures and both stopped series remain
untouched. This is local preparation; future native adoption still needs a fixed
protocol, source/binary/settings/fixture checks and appropriate review. It provides
no new timing sample, speed claim, release proof, PR402 acceptance or #453 closure.
