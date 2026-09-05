# ADR-0027: Schedule synchronous Windows local I/O through one execution boundary

Status: accepted

Date: 2026-09-05

## Context

`IDirectoryReadPort` and `IFileOperationPort` are asynchronous contracts, but their Windows local implementations performed synchronous `System.IO` work before returning `Task.FromResult`. Because `CommanderWindow` invokes pane and operation work on the UI thread, a slow enumeration, inspection, tree copy, verification, deletion, directory creation, or rename could prevent input and rendering until that step returned. ADR-0010 and ADR-0014 deliberately recorded this limitation and require a scheduling decision before it changes.

The filesystem operations themselves have no naturally asynchronous BCL API. Mutation serialization, provider identity revalidation, typed failures, cancellation observation between atomic provider steps, and UI-thread continuation ownership must remain unchanged.

## Decision

Add `WindowsLocalIoExecutionBoundary` in Infrastructure.Windows as the single scheduling mechanism for synchronous Windows-side filesystem work. It schedules a supplied synchronous operation with `Task.Factory.StartNew`, `TaskScheduler.Default`, `DenyChildAttach`, and no scheduler-level cancellation. `WindowsLocalDirectoryReader`, the WSL directory reader added by ADR-0035, and every method of `WindowsLocalFileOperationAdapter` delegate their provider work to this boundary.

The App composition root creates one boundary and gives the same instance to the reader and mutation adapter. Application ports remain unchanged. `PaneSession`, `DualPaneSession`, and `FileOperationGateway` continue to await the returned tasks; therefore the caller owns completion and fault observation, the captured UI context owns presentation callbacks, and the gateway semaphore remains the only mutation serialization mechanism.

The scheduler does not cancel a queued provider step. Cancellation remains a typed provider/application concern: a directory read observes its token before and during enumeration, and the gateway observes cancellation before starting each atomic mutation step and immediately after relevant steps. This preserves ADR-0014 and ADR-0018 rather than introducing a new partially executed provider outcome.

## Rejected alternatives

- Put `Task.Run` in each adapter method: duplicates scheduling policy across eight entry points and permits cancellation and options to drift.
- Add scheduling to `PaneSession` and `FileOperationGateway`: makes Application know that one provider is synchronous and risks a second execution path when UNC or WSL adapters arrive.
- Own a dedicated thread or serialize all reads and mutations in the execution boundary: adds lifetime and shutdown state and would make unrelated queries wait behind large copies. The existing gateway alone serializes mutations.
- Dispatch completion from Infrastructure.Windows: UI affinity belongs to the App owner and its captured context, not to a filesystem adapter.

## Consequences

- Calls return an incomplete task before queued synchronous filesystem work executes, so the UI owner can yield instead of running that I/O inline.
- The default thread pool may run independent directory reads concurrently. Existing pane supersession discards stale results; mutations remain serialized by `FileOperationGateway`.
- One large provider step still occupies one worker until it completes and remains atomic for cancellation reporting.
- Required argument validation remains synchronous before scheduling. Unexpected defects fault the returned task and must be observed by its existing owner; lifecycle cleanup is tracked separately by Issue #59.

## Migration and removal

This decision supersedes only the synchronous-caller portions of ADR-0010 and ADR-0014. Their port, provider, identity, failure, and cancellation decisions remain active. Future synchronous Windows local adapters use this boundary; naturally asynchronous providers await their native APIs and do not wrap them in it.

## Executable proof

`WindowsLocalIoExecutionBoundaryTests` uses a deterministic manual scheduler to prove both directory reads and mutation inspection return before provider work runs and use the same boundary. The complete Infrastructure.Windows, Application, and Architecture suites prove adapter outcomes, gateway serialization/cancellation, and dependency ownership. The final canonical CI gate remains the merge-readiness proof.
