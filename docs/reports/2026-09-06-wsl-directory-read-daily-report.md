# Daily report — WSL directory read — 2026-09-06

Status: informational

## Scope

Issue #81 connects validated WSL locations to the existing provider-neutral directory-read path. It does not add WSL mutations, shell commands, UNC support, distribution UI, or a second listing model.

## Invariant and canonical mechanism

One App-composed `ProviderDirectoryReadPort` branches only on validated path variants. Windows local and WSL adapters share `WindowsDirectoryReadOperation`, which owns direct non-recursive enumeration, entry bounds, cancellation, child derivation, failure normalization, and complete `DirectoryListing` publication. All synchronous namespace I/O crosses the existing `WindowsLocalIoExecutionBoundary`.

## Failure-first proof

The first router test required Windows local and WSL requests to reach only their matching reader. The skeletal router had no routing and the Release build failed because both assigned readers were unused, proving that no provider connection existed before implementation. Later review strengthened the test to assert counts immediately after each call so swapped routing cannot pass.

## Changes

- Added the sole provider router and composed it as `IDirectoryReadPort` for both panes.
- Added `WslDirectoryReader` without `wsl.exe`, shell, recursion, or mutation authority.
- Replaced the Windows-local-only enumeration loop with one shared snapshot enumerator and bounded read operation.
- Derived every provider name through `FileSystemPath.Child`; unrepresentable names are counted.
- Preserved Windows attribute visibility and added WSL dot-name visibility while reporting every entry.
- Added deterministic routing and WSL contract tests, ADR-0035, and related normative updates.

## Focused verification

- Release solution build: PASS, zero warnings and errors.
- Final Infrastructure.Windows tests: 109/109 PASS; targeted provider/local/WSL/execution tests: 28/28 PASS.
- Architecture tests: 5/5 PASS.
- Final Infrastructure.Windows branch coverage: 94.04% (minimum 90%).
- Focused Directories mutation first failed at 85.45%; after guard proof and closed-variant simplification it passed at 100.00% (55/55 killed) without threshold reduction.
- Conformance: 111 unique normative rules PASS. Security conformance: 18 adversarial mappings, secrets, and workflow supply chain PASS.

## Integration completion

PR #82 passed dependency run `33985129013`, exact-head deep retry `33985643645`, and Ready canonical run `33986546237`, then squash-merged as `1b5142825b877e47e675f099b38c33a914583b0f`. Issue #81 is closed. The first deep run `33985153722` failed only because the unchanged Domain mutation score was 94.97%; no Domain source changed and no threshold was lowered. The exact-head retry passed at Domain 95.98%, Application 96.46%, Infrastructure.Windows 93.43%, and Presentation.WinUI 89.06%, with zero CodeQL alerts and zero unresolved review threads.

## Remaining environmental proof

No live WSL directory was opened or mutated in this Issue's deterministic tests. Live provider proof remains opt-in and requires `NENE_COMMANDER_WSL_TEST_ROOT` to identify a safe test-owned location; it must not target a distribution root, home, `/mnt`, repository, or their ancestors.
