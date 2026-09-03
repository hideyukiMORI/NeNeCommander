# ADR-0017: Copy through the shared transfer path

Status: accepted

Date: 2026-09-04

## Context

`F5` maps to `UserIntent.Copy` (KEYBOARD_MODEL), but no application request existed for it. `FileOperationGateway` already owned the composite move algorithm (preflight, copy, verify, delete source) and `IFileOperationPort` already exposed the copy and verify steps. A copy is that algorithm without the final delete; adding a second gateway, a second preflight, or a provider-level copy entry point would create two owners of one mutation path (ARC-009, FS-005).

## Decision

- `CopyRequest.Create(sources, destination)` is a validated immutable request with the same validation as `MoveRequest` (empty, bounded, null, duplicate, destination equal to a source). Both share one `FileOperationRequest.ValidateTransfer` so a rule cannot drift between them.
- `FileOperationGateway` executes move and copy through one `ExecuteTransferAsync`: inspect every source, preflight the whole batch, then run one per-source step. The copy step is copy then verify; the move step is the copy step followed by the source delete. Cancellation and failure are observed at the same points for both, so effects are reported identically (ADV-005, ADV-018).
- `IFileOperationPort.PreflightMoveAsync` is renamed `PreflightTransferAsync`; its contract (destination containment, recursion, capability, collisions) is unchanged and the Windows local adapter is untouched apart from the name.
- `OperationKind.Copy` is added. `DualPaneSession` starts move and copy through one `TransferAsync` that takes the request factory, so source selection (selection, else focus item) and destination (passive pane location) are decided once.
- `DualPanePresenter` projects copy through the same status shape as move: `Copying`, `CopySucceeded`, `CopyCancelled`, `CopyPartiallyCompleted`, `CopyRejected`, `CopyRequestRejected`; the App only gains resources.

## Rejected alternatives

- A copy flag on `MoveRequest`: a boolean mode on one request is prohibited (CS-023) and would let a move be mistaken for a copy at the gateway.
- A provider `CopyTreeAsync` that bypasses inspect and preflight: identity revalidation and collision detection would be skipped (ADV-004, ADV-006).
- Resolving destination collisions during copy: the conflict resolver (FS-007) is a separate slice; a collision is a typed `Conflict` rejection before any side effect.

## Consequences

- A collision at the destination rejects the whole batch before mutation; there is no replace, skip, or keep-both yet.
- Copy verification compares the declared metadata and byte count only, as for move; hash verification remains a later capability.
- Progress and cancellation UI remain absent; a running copy freezes both panes until it completes.

## Migration and removal

`PreflightMoveAsync` is removed in the same change; every port implementation renames the member.

## Executable proof

`FileOperationGatewayTests` (copy reports copy and verify per source and never deletes, verification failure stops before the next source, preflight conflict starts no mutation), `FileOperationRequestTests` for `CopyRequest`, `DualPaneSessionTests` (copy of the focus item, request rejection), `DualPanePresenterTests` (copy statuses and the running kinds), and the canonical gate.
