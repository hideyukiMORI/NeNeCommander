# ADR-0029: Report a copy target created before provider failure

Status: accepted

Date: 2026-09-05

## Context

`WindowsLocalTreeCopy` creates a directory target before copying its children. An expected `IOException` or `UnauthorizedAccessException` at a later child leaves that target and any completed children in place. The adapter normalized the exception to a failed `ProviderStepOutcome`, but `FileOperationGateway` added `Copied` only after complete provider success. A one-source request could therefore return `Rejected` with no effects even though the destination changed, violating CMD-005 and TST-007.

The operation model records effects per requested source, not every descendant. It needs an exact statement that the top-level target exists and may be incomplete; it must not claim the whole copy completed. Removing the target automatically is not safe because cleanup can fail, race with external changes, or erase useful evidence of the partial operation.

## Decision

Extend `ProviderStepOutcome` with an optional closed `ProviderStepEffectKind`, currently `CopyTargetCreated`, and the factory `FailedAfterEffect`. `CopyTargetCreated` means the requested source's top-level target entry exists after a failed copy and its contents may be incomplete.

`WindowsLocalFileOperationAdapter.CopyEntry` catches only the expected copy exceptions at the point where the exact target text is known. It normalizes the failure and tests whether that target exists. If it does, it returns `FailedAfterEffect(failure, CopyTargetCreated)`; if it does not, it returns the ordinary failed outcome. Revalidation and reparse-point failures before copy retain no effect.

`FileOperationGateway.CopyOneAsync` consumes the closed provider effect before handling the failure and adds `FileOperationEffectKind.CopyTargetCreated` for the frozen source. The overall failed outcome therefore becomes `PartiallyCompleted`. It does not verify the incomplete target, does not continue the batch, and a move never deletes the source. The provider-effect vocabulary contains only this copy-specific variant; adding another variant requires updating this mapping and its exhaustive proof.

## Rejected alternatives

- Delete the target after failure: rollback has its own failure and race surface and can make the returned result less truthful.
- Report ordinary `Copied`: claims a complete provider step and would permit verification or source deletion logic to be misunderstood.
- Enumerate and report every created descendant: leaks provider tree mechanics into the request-level effect contract and still cannot atomically observe all bytes written before failure.
- Let the gateway inspect the destination after an effect-free failure: makes orchestration infer provider state without the adapter's exact target and failure boundary.
- Return an exception containing path details: expected platform failures are typed outcomes, and exception text risks leaking filesystem information.

## Consequences

- A leftover target is visible as one exact per-source effect; its descendants are deliberately summarized as potentially incomplete.
- Retrying the same request encounters the existing-target conflict until the person explicitly resolves the partial target. No hidden overwrite or cleanup path is added.
- File-copy failures that leave a target are covered by the same check as directory trees.
- Cancellation remains between atomic provider steps. This decision does not introduce mid-step cancellation or claim that a cancelled step produced a partial target.

## Migration and removal

The former assumption that a failed copy has no effect is removed from the gateway and ADR-0014. Existing `Succeeded` and effect-free `Failed` factories remain the only outcomes for provider steps that did not leave a partial copy target.

## Executable proof

An Infrastructure.Windows adversarial test locks the sole child of a test-owned source directory, forces failure after the destination root is created, and proves the real gateway returns `PartiallyCompleted`, `CopyTargetCreated`, and keeps the source. Application gateway tests prove copy and move mapping, no progress for the incomplete source, no verification, no next-source work, and no move deletion. Existing cancellation and source-delete tests remain active. Security-sensitive integration runs the deep review and final canonical CI gate.
