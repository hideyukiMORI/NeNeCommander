# Daily report — WSL same-distribution mutations — 2026-09-06

Status: informational

## Scope

Issue #83 connects WSL directory creation, same-parent rename, and confirmed permanent deletion to the existing `FileOperationGateway`. It does not enable copy, move, verification, atomic move, cross-provider transfer, recycle, overwrite, or a collision default.

## Invariant and canonical mechanism

All mutations remain `FileOperationGateway` operations. One App-composed `ProviderFileOperationPort` selects a provider-owned adapter from the already validated path variant. WSL side effects use `\\wsl.localhost` through `System.IO` and the shared `WindowsLocalIoExecutionBoundary`; no shell or second command path exists. The adapter revalidates identity and provider containment before each effect, rejects links, and reports existing targets as conflicts.

## Failure-first proof

The initial provider-router skeleton assigned both adapters but performed no routing. Release compilation failed on the unused WSL field, demonstrating that App still had no WSL mutation route. After routing was added, the first gateway create test failed because the adapter incorrectly applied rename's same-parent predicate to a location snapshot. The direct-child predicate was separated and the test then passed.

## Changes

- Added the mutation provider router and composed it as the only `IFileOperationPort`.
- Added a WSL adapter for direct-child directory create, same-parent rename, and permanent delete.
- Shared the low-level Windows namespace file identifier under `WindowsFileIdentifier`; Windows-local identity format remains unchanged.
- Added deterministic adapter and router tests plus actual `System.IO` tests redirected internally to `TestOwnedTemporaryRoot`.
- Added ADR-0036 and aligned command, filesystem, security, adversarial, project-state, report, and handoff records.

## Focused verification

- Release solution build after the shared-identifier rename: PASS, zero warnings and errors.
- Infrastructure.Windows tests: 124/124 PASS.
- Architecture tests: 5/5 PASS.
- Infrastructure.Windows branch coverage before the name-only identifier refactor: 93.70% (minimum 90%).
- Focused WSL adapter/filesystem mutation: 100.00%, 88/88 killed.
- Focused provider-router mutation: 100.00%, 23/23 killed.
- Focused shared `WindowsFileIdentifier` mutation after its rename: 100.00%, 7/7 killed.
- Conformance: 112 unique normative rules PASS. Security conformance: 18 adversarial mappings, secrets, and workflow supply chain PASS.

Conformance, security conformance, exact-head deep review, dependency review, and Ready canonical CI remain integration evidence and are not yet claimed here.

## Remaining environmental proof

No live WSL path was mutated. Live create, rename, and delete proof remains opt-in and requires `NENE_COMMANDER_WSL_TEST_ROOT` to identify a dedicated empty test-owned directory. Transfer remains intentionally unavailable and is the next independent Issue.
