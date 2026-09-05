# Daily report — Windows local I/O execution boundary — 2026-09-05

Status: informational

## Scope

Issue #56 removes synchronous Windows local filesystem work from the UI caller. It does not change ports, provider validation, gateway serialization, cancellation observation, or typed outcomes. ADR-0027 records the scheduling mechanism.

## Invariant and canonical mechanism

I/O work is awaited by its owner, UI affinity stays with the App caller, and mutations still pass through the sole `FileOperationGateway` lease. `WindowsLocalIoExecutionBoundary` is the single mechanism that schedules synchronous Windows local filesystem operations away from the caller.

## Failure-first proof

`ProviderCallsWhenExecutionIsQueuedDoNotCompleteOnCallingThread` composed one manual scheduler into both Windows local adapters. Before the mutation adapter was wired to the boundary, the read was queued but inspection completed inline; the test failed at `Assert.IsFalse(inspection.IsCompleted)` with actual `true`. After all methods were wired, both calls remained incomplete until the manual scheduler executed their work.

## Changes

- Added one Infrastructure.Windows execution boundary over the default task scheduler.
- Composed one shared boundary into `WindowsLocalDirectoryReader` and `WindowsLocalFileOperationAdapter`.
- Kept argument validation synchronous and preserved the gateway's cancellation points and serialization lease.
- Updated ADR-0010 and ADR-0014 only where their former caller-thread execution statements were superseded.

## Focused verification

- Release locked restore and `dotnet build NeNeCommander.slnx -c Release --no-restore`: PASS, zero warnings.
- Infrastructure.Windows tests: PASS, 66/66.
- Application tests: PASS, 155/155.
- Architecture tests: PASS, 4/4.
- `eng/conformance.ps1 -Quiet`: PASS.
- `eng/security-check.ps1 -SkipProof`: PASS; all 18 adversarial cases remained registered.

The final Draft-to-Ready canonical CI gate is pending. No local full-gate duplicate is planned after successful CI evidence.
