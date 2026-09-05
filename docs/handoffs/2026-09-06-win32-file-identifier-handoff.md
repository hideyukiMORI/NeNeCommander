# Win32 file-identifier identity handoff — 2026-09-06

Status: informational

## Work item

- Issue: #77
- Branch: `security/77-win32-file-id`
- Decision: ADR-0033
- Invariant: replacement cannot preserve identity by restoring metadata; reparse queries identify the link entry; query failure is closed.
- Canonical mechanism: `WindowsLocalEntryIdentity.Describe` and `Revalidate`, with one internal Win32 identifier query.

## Verification checkpoint

- The failure-first same-size/same-time replacement proof failed against the metadata-only token and now proves both distinct inspection identity and `IdentityChanged` transfer preflight.
- Release solution build passes with zero warnings and errors.
- Infrastructure.Windows tests pass 89/89 with zero skips, including fixed-width identity, missing/null query, and junction-not-target boundaries.
- Architecture tests pass 5/5; conformance and security pass with all 18 adversarial mappings.
- Infrastructure.Windows branch coverage is 93.29%, and focused identifier mutation is 100.00% (7/7 killed) after a failure-first 55.56% run exposed four weak assertions/expressions.
- Exact-head deep review and final canonical Ready CI remain to be completed.

## Integration steps

1. Review the native struct layout, reparse-safe open flags, closed failure behavior, and absence of metadata fallback.
2. Run affected Architecture, conformance, security, coverage, and focused Infrastructure.Windows mutation checks after a fresh Release build.
3. Commit through the existing Commit-mode hook, push, and open a Draft PR closing #77.
4. Confirm the remote PR head equals local HEAD, run the security deep review, then mark Ready only for the unchanged final candidate.
5. Require fresh canonical CI on the latest base, squash merge, synchronize clean `main`, and continue WSL provider work.

## Remaining environmental proof

The test-owned NTFS root provides replacement and junction proof. This change does not eliminate the residual path-reopen race between identifier query and a later mutation and must not be represented as handle-relative safety.
