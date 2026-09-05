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

1. Review the complete candidate diff and commit through the existing Commit-mode hook.
2. Push and create a Draft PR closing #81.
3. Confirm remote head/latest base and obtain exact-head deep review because provider/path input handling changed.
4. Resolve CodeQL/review findings while Draft; after final head proof, mark Ready and require fresh canonical CI.
5. Squash merge, synchronize clean main, then create the separate WSL file-operation Issue.

## Remaining environmental proof

No live WSL path was enumerated. A future opt-in integration proof must use a configured, validated `NENE_COMMANDER_WSL_TEST_ROOT`; file mutation remains outside this Issue.
