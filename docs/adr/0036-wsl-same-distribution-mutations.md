# ADR-0036: Route WSL same-distribution mutations through the canonical gateway

Status: accepted

Date: 2026-09-06

## Context

ADR-0004 models each WSL distribution as a provider and prohibits shell commands as a second operation engine. ADR-0035 can read validated WSL locations through the Windows `\\wsl.localhost` namespace, but App composition still gives `FileOperationGateway` a Windows-local-only adapter. Consequently directory creation, rename, and confirmed deletion of WSL entries cannot use the existing command path. Copy, move, cross-provider transfer, replacement, and recycle require additional capability and product-policy work and are outside this decision.

## Decision

Add `ProviderFileOperationPort` as the sole App-composed `IFileOperationPort`. It routes a validated `WindowsLocalPath` to `WindowsLocalFileOperationAdapter` and a validated `WslPath` to `WslFileOperationAdapter`; unsupported UNC, mixed providers, and mixed WSL distributions fail closed. `FileOperationGateway` remains the only mutation owner and keeps request validation, confirmation, conflict, progress, cancellation, effect, and outcome policy.

The WSL adapter uses the canonical `\\wsl.localhost` path through `System.IO` and the shared `WindowsLocalIoExecutionBoundary`; it never starts `wsl.exe` or a shell. It supports only:

- creating one direct child directory of the revalidated WSL location;
- renaming an entry within its existing parent and distribution;
- permanent deletion after the gateway has accepted exact capability-bound confirmation.

Before each side effect the adapter resolves the current entry, compares a `wsl-v1` identity containing the shared `WindowsFileIdentifier` plus kind, length, and timestamps, and refuses changed or missing entries. Reparse points are rejected instead of followed. Existing targets return `Conflict`; the adapter never replaces or chooses a collision result.

ADR-0037 subsequently enables same-distribution WSL preflight, copy, verification, and composite move. Atomic-move capability, recycle, cross-distribution transfer, and cross-provider transfer remain unavailable until a later ADR defines their complete capability, metadata, partial-result, and collision behavior.

## Rejected alternatives

- Invoke `mkdir`, `mv`, or `rm` through `wsl.exe`: creates a second mutation path and a command-injection surface.
- Treat WSL as Windows local or generic UNC: erases case-sensitive distribution identity and provider-specific capability policy.
- Add WSL decisions to `FileOperationGateway`: makes the provider-neutral application owner interpret platform details.
- Enable copy/move by reusing Windows-local tree-copy behavior immediately: would claim unproved metadata, link, partial-result, and cross-provider semantics.
- Roll back a failed mutation by deleting an observed target: risks deleting a target the operation does not own.

## Consequences

- Existing F7, F2, and confirmed F8 commands can operate on a WSL pane without a second UI or application path. ADR-0037 later adds F5/F6 within one distribution through the same gateway.
- WSL deletion is always reported as permanent and remains confirmation-bound.
- Same-name targets remain conflicts. ADR-0036 does not decide Issue #73's product collision policy.
- A residual path race remains between identifier lookup and the `System.IO` side effect, as recorded for Windows local in ADR-0033; this decision does not claim handle-relative Linux mutation.
- Live WSL mutation proof remains opt-in and must use a validated test-owned root. Deterministic production-I/O tests redirect only the internal path resolver to `TestOwnedTemporaryRoot`.

## Migration and removal

App composition replaces direct construction of `WindowsLocalFileOperationAdapter` with `ProviderFileOperationPort`; no compatibility path remains. The native identifier implementation is renamed from `WindowsLocalFileIdentifier` to `WindowsFileIdentifier` because Windows local and Windows-side WSL adapters now share that low-level mechanism. ADR-0033's Windows-local token format and safety contract remain unchanged.

## Executable proof

`ProviderFileOperationPortTests` proves exact provider routing and fail-closed unsupported or mixed inputs. `WslFileOperationAdapterTests` proves gateway-level create, rename, confirmed permanent delete, identity-race rejection, link refusal, distribution and parent containment, collision, failure normalization, and unavailable transfer operations. `WindowsWslFileSystemTests` exercises actual `System.IO` effects and shared identifiers only under `TestOwnedTemporaryRoot`. Infrastructure branch coverage and focused mutation prove the new branches; Architecture, conformance, security, deep review, and the final canonical CI gate prove the integration candidate.
