# Daily report — WSL distribution catalog — 2026-09-06

Status: informational

## Scope

Issue #79 implements the WSL distribution discovery port and Windows adapter required by ADR-0004. It does not add WSL directory enumeration, file mutation, root-picker UI, shell commands, or live mutation against a distribution.

## Invariant and canonical mechanism

Discovery uses one Infrastructure.Windows-owned fixed process invocation. No caller supplies process arguments. Output is bounded before publication, every reported name becomes an exact root through `FileSystemPath.Parse`, one invalid line rejects the entire snapshot, and cancellation terminates and awaits only the process owned by that call. `IWslDistributionCatalog` is the sole consumer boundary.

## Failure-first proof

Four catalog tests were introduced against a stub parser. Three failed as intended: valid and empty outputs returned failure, and unsafe/excessive output returned the wrong failure. Pre-cancellation already passed because the port never invoked its process. The implemented parser and process boundary make all cases pass.

## Changes

- Added closed Application catalog outcomes with immutable, unique WSL root snapshots.
- Added `WslDistributionCatalog` and its one dedicated `WslDistributionProcess`.
- Fixed `wsl.exe --list --quiet` as separate ArgumentList tokens with no shell or window.
- Bounded UTF-16 stdout and stderr at 64 KiB, bounded output at 256 reported lines, and validated every line with the canonical path parser.
- Owned active cancellation through kill, stream drain, and exit await before returning `Cancelled`.
- Normalized process start and stream I/O failures to the closed `ProviderUnavailable` outcome.
- Added an internal start seam and test-owned short-lived process proofs; production construction cannot supply another command.
- Recorded ADR-0034 and updated ADR-0004, FS-003, SEC-002, ADV-009, and the project checkpoint.

## Focused verification

- Release solution build: PASS, zero warnings and errors.
- Application tests: 186/186 PASS, skip 0.
- Infrastructure.Windows tests after process proofs: 100/100 PASS, skip 0.
- Architecture tests: 5/5 PASS, skip 0.
- Conformance and security without negative fixtures: PASS; all 18 adversarial mappings remain registered.
- Final branch coverage: Application 100.00%, Infrastructure.Windows 93.20% (minimums 100% and 90%).
- Focused Application WSL mutation: 100.00% (13/13 killed).
- Focused Infrastructure.Windows WSL mutation: 90.38%; the first process-boundary run failed at 43.75% and the first lifecycle run failed at 58.02%, prompting real test-owned process lifecycle proof and a simpler cancellation ownership design without lowering the threshold.

The exact-head deep review and Ready canonical CI remain pending integration evidence.

## Remaining environmental proof

A read-only local `wsl.exe --list --quiet` invocation exited 0 with nine output lines; redirected PowerShell representation contained UTF-16 null characters, supporting the explicit Unicode decoder. Distribution names are intentionally not recorded. No `NENE_COMMANDER_WSL_TEST_ROOT` mutation was performed; live directory and mutation proof belongs to later provider Issues.
