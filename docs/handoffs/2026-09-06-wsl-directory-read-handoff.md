# WSL directory read handoff — 2026-09-06

Status: informational

## Work item

- Issue: #81
- Branch: `feat/81-wsl-directory-read`
- Decision: ADR-0035
- Invariant: validated provider routing, one bounded direct-enumeration operation, no partial listing, and provider-accurate visibility/identity.
- Canonical mechanism: App-composed `ProviderDirectoryReadPort` implementing the existing `IDirectoryReadPort`; Windows local and WSL adapters share `WindowsDirectoryReadOperation` and `WindowsLocalIoExecutionBoundary`.

## Verification checkpoint

- Failure-first skeletal routing did not compile because both delegated readers were unused; routing tests now reject swapped or duplicate delegation.
- Release build passes with zero warnings and errors.
- Final Infrastructure.Windows tests pass 109/109; targeted provider/local/WSL/execution tests pass 28/28.
- Final Infrastructure.Windows branch coverage is 94.04%.
- Focused Directories mutation improved from failing 85.45% to 100.00% (55/55 killed).
- Architecture 5/5, conformance 111 rules, and security conformance 18 mappings pass.

## Integration steps

Completed: PR #82 passed dependency run `33985129013`, exact-head deep retry `33985643645`, and Ready canonical run `33986546237`, then squash-merged as `1b5142825b877e47e675f099b38c33a914583b0f`. Issue #81 is closed. Continue with Issue #83 for the separate WSL mutation provider; do not repeat #81's successful gates.

## Remaining environmental proof

No live WSL path was enumerated. A future opt-in integration proof must use a configured, validated `NENE_COMMANDER_WSL_TEST_ROOT`; file mutation remains outside this Issue.
