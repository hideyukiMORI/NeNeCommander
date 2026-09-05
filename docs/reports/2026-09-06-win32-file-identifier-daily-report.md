# Daily report — Win32 file-identifier identity — 2026-09-06

Status: informational

## Scope

Issue #77 strengthens Windows local operation snapshots against same-metadata entry replacement. It does not add handle-relative mutation, link traversal, persistence, a public token format, or a second gateway path.

## Invariant and canonical mechanism

A replacement cannot impersonate the inspected entry by restoring its kind, byte length, creation time, and last-write time. Reparse points are identified as entries without following their targets, and an ambiguous native query fails closed. `WindowsLocalEntryIdentity.Describe` and `Revalidate` remain the sole Windows local snapshot mechanism.

## Failure-first proof

`InspectAndPreflightWhenFileIsReplacedWithMatchingMetadataRejectReplacement` first parked the original file inside a test-owned temporary root, created a different same-length file at the same path, and restored the exact creation and last-write timestamps. The previous metadata-only implementation failed because both tokens were identical and transfer preflight accepted the replacement. The Win32 file identifier makes the replacement distinct and preflight returns `IdentityChanged`.

## Changes

- Added a source-generated `CreateFileW` and `GetFileInformationByHandleEx(FileIdInfo)` query inside Infrastructure.Windows.
- Opened directories with backup semantics and reparse points with `FILE_FLAG_OPEN_REPARSE_POINT`.
- Versioned the opaque identity token and combined volume/file identifier with the existing rewrite-sensitive metadata.
- Kept all inspection and effect-boundary checks on the existing adapter revalidation route; native failures have no metadata-only fallback.
- Recorded ADR-0033 and updated ADV-004, SEC-003, filesystem boundaries, and the current project checkpoint.

## Focused verification

- Failure-first legacy implementation: FAIL as expected because matching metadata produced equal identities.
- Release solution build: PASS, zero warnings and errors.
- Infrastructure.Windows tests: 89/89 PASS, skip 0, including replacement, junction, missing-path, null, file, and directory cases.
- Architecture tests: 5/5 PASS, skip 0.
- Conformance and security without negative fixtures: PASS; all 18 adversarial cases remain registered.
- Infrastructure.Windows branch coverage: 93.29% (minimum 90%).
- Focused `WindowsLocalFileIdentifier` mutation: 100.00% (7/7 killed). The first run exposed four survivors and correctly failed at 55.56%; observable HRESULT proof and a non-decomposed share-mode constant removed those gaps without lowering the threshold.

The exact-head security deep review and final Ready canonical CI remain pending integration evidence.

## Remaining environmental proof

No additional environment is required for the test-owned NTFS replacement and junction proofs. A residual path-reopen window between identity query and filesystem mutation remains explicit; handle-relative mutation would require a separate design and is not claimed here.
