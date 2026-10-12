# ADR-0062: Copy and move WSL trees into Windows without changing names

Status: accepted

Date: 2026-10-12

Issue #216. Accepted by the design owner while implementing hide's 2026-10-12
request to continue the remaining features.

## Context

ADR-0059 implements Windows-to-WSL transfers; the reverse direction remains
closed. ADR-0060 preserves WSL names in Win32 transport, but Windows rejects
some names Linux accepts and compares names without case. Validating only the
top-level name, or scanning once and later using string child resolution, can
rename a descendant or merge two different source directories into one target.

## Decision

- Add the WSL-to-Windows pair to the existing `TransferRoute` and
  `ProviderFileOperationPort`. One batch must have WSL sources from the same
  distribution. Every other unavailable pair remains closed. No Application
  port member, UI command or move algorithm is added.
- One Infrastructure transfer reuses WSL snapshot revalidation and the existing
  Windows I/O boundary. Its atomic-move capability is `Unsupported`, so the
  gateway alone copies, verifies and permanently deletes the WSL source through
  its existing provider. The transfer itself cannot delete or perform a move.
- Preflight the complete batch and every source descendant before effects.
  Derive destination names through `FileSystemPath.Child`, additionally enforcing
  the Windows component limit of 255 UTF-16 code units. The existing canonical
  path limit is unchanged. Reserved names, trailing dots/spaces, invalid names
  and length failures report `ProviderUnavailable`, without translation.
  Existing targets and case-insensitive reservations, including nested
  file/directory and directory/directory collisions, report `Conflict`.
- Migrate every caller of `WindowsLocalTreeCopy.Copy` and `Matches` atomically
  to typed destination paths. The same child derivation runs in preflight,
  copy and verification; the old string `ResolveDirectChild` is removed. Tree
  copy rejects observed source or target links and does not merge directories.
  A Win32 `CreateDirectoryW` call creates exactly one missing target directory
  and reports an existing entry as failure, including one appearing after the
  initial existence check. File creation keeps `overwrite: false`.
- The tree engine receives the target transport resolver and source inspection
  from its owning adapter. WSL transport still uses ADR-0060 exclusively. Every
  WSL source descendant is checked with the existing no-follow 9P handle query
  during preflight, copy and verification. A denied nested mount or non-9P
  entry fails closed; there is no shell, enumeration-identity or local fallback.
- The new direction accepts only an existing Windows destination directory on
  an OS-reported fixed or removable drive. It checks that directory and its
  ancestors for links before each transfer step. Network and unknown drive
  kinds remain closed for this direction; Issue #181 owns their wider policy.
- Preserve the exact Windows-API-visible name, including private-use Unicode
  characters. Do not decode the 9P substitute U+F03A into a colon or reject all
  private-use characters. Live evidence must show the observed colon-substitute
  case copied to Windows with the same visible name and bytes. This does not
  promise a bijection with every possible Linux spelling.
- A source name or collision introduced after preflight is rejected by the same
  per-child admission during copying. Any created top-level target is reported
  as `CopyTargetCreated`, and the source remains. Verification applies the same
  name rules and checks kind, complete entry set and byte lengths. It does not
  promise hashes, Unix permissions, ACLs, timestamps or sparse-file fidelity.
- Existing cancellation boundaries and partial-effect reporting remain. Source
  identity is checked before copy, verification and source deletion, but does
  not freeze all descendants. Path-based reopen and ancestor-replacement races
  remain explicit limits; this change is not handle-relative tree traversal.

## Migration and proof

Update all tree-engine callers, the WSL filesystem seam and its fakes together.
Existing local and WSL transfers exercise the new shared child admission with
their own provider rules. No compatibility string path remains. Update FS-005,
FS-012 and ADV-022, then add router, owned-NTFS and gateway tests for successful
copy/move, whole-batch rejection, invalid nested names and lengths, case
collisions, existing directories, links, source changes, late child admission,
partial copy, verification failure and cancellation before source deletion.
Live Ubuntu proof covers a real case collision, invalid Windows name, the
observed private-use substitute and successful copy/move. Keep all fixtures in
the owned local and dedicated WSL roots; never use the working Ubuntu-22.04.

The live launcher accepts an explicit list of distinct declared `-Case` names,
records declared and selected counts, and requires each selected cell to pass.
The default still runs every declared cell. For this change, run the six affected
existing transfer cells and four new reverse-transfer cells; reuse the unchanged
identity-query evidence from Issue #190. The owned live root may retire a moved-out
source only after the gateway reports copy, verification and source deletion.

Under ADR-0061, use the affected Infrastructure and gateway tests during
implementation. Request one deep review when the candidate is ready because the
change opens a destructive cross-provider boundary and replaces shared recursive
copy admission. Its full gate is not repeated through a second quality run.

## References

- [CreateDirectoryW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createdirectoryw)
  creates only the final directory and fails if it already exists.
- ADR-0059, ADR-0060 and ADR-0061 retain their other decisions.
