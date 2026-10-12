# ADR-0066: Reuse the WSL transfer for different distributions

Status: accepted

Date: 2026-10-12

Issue #228. Accepted by the design owner while implementing hide's request to
continue the remaining features on 2026-10-12.

## Context

Both endpoints already use the typed WSL namespace and its provider-scoped
identity. The adapter's same-distribution guard is the remaining admission
restriction; copying through Windows staging would add an unnecessary second path.

## Decision

Extend the existing WSL transfer admission to accept a destination in another
distribution. Replace `TransferRoute.SameWslDistribution` with `WslToWsl`;
both same- and different-distribution pairs use `WslFileOperationAdapter`.
A batch still requires every source to belong to one distribution. The router
and adapter share that admission through `TransferRoute.Derive`.

Every path keeps its own distribution. Distribution identity remains
case-insensitive, while Linux path segments remain case-sensitive. Preflight
reserves all typed targets and rejects duplicate or existing targets before any
effect. Containment compares provider identity, so equal Linux paths in distinct
distributions do not become recursive copies. No segment is translated.

The gateway alone performs copy, verification and then permanent source deletion
for a move. Atomic capability is `Unsupported` for every WSL pair; direct atomic
move remains unavailable. Existing source identity, destination, link, collision,
cancellation and partial-effect checks remain active at their existing boundaries.
Rename still requires the same parent and distribution. Copy/verification/delete
use the unchanged `WindowsWslFileSystem`, extended-length resolver, no-follow 9P
identity and shared tree engine. No new port, process, shell command or algorithm
is introduced.

Verification compares kind, entry set and byte length; it does not promise hashes,
Unix metadata or a frozen descendant tree. Path-based reopen and ancestor races
retain their existing limits. Copy or verification failure keeps the source and
reports any created target; source-deletion failures retain the existing partial
completion semantics. No rollback, overwrite, merge or retry is added. UNC mutations,
mixed-source distributions, Linux link deletion and recycle remain unavailable.

This supersedes the same-distribution-only transfer restriction in ADR-0037,
ADR-0059 and ADR-0062. Their other decisions remain active.

## Alternatives rejected

- A separate cross-distribution adapter duplicates the same WSL checks and I/O.
- Windows staging or Linux shell commands introduce another transfer mechanism.
- Atomic move across distributions has no provider capability evidence.

## Proof

Under ADR-0061, run affected router, WSL adapter, filesystem/transport/identity,
shared-tree and gateway transfer tests and the dependency/lifecycle boundary tests.
Exercise both directions, exact names, distribution separation, complete-batch
collision refusal, links, identity changes, partial copy, failed verification,
source-deletion failure and cancellation. Two independently owned NTFS roots
exercise real nested copy/verification/deletion with injected namespace mapping;
they do not establish live two-distribution behavior.

The change extends typed admission over an existing provider without replacing
the filesystem primitive, identity contract, recursive engine or Application/UI
contract. Focused behavioral and adversarial proof establishes its bounded impact;
no full/deep/coverage/mutation run is selected. Re-evaluate that scope if any of
those shared mechanisms changes. Actual two-distribution proof remains an explicit
environmental limitation. Do not use hide's working Ubuntu-22.04 distribution or
install another distribution for this change.
