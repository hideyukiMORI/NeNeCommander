# ADR-0026: Prune generated directories before proof-tree traversal

Status: accepted

Date: 2026-09-05

## Context

Issue #54. `eng/prove-gates.ps1` and `eng/prove-security.ps1` copied each negative fixture by enumerating only the repository root, excluding generated directories there, and calling `Copy-Item -Recurse` for every remaining child. Nested `bin`, `obj`, and `TestResults` trees therefore entered every fixture. On the same working tree the current rule selected 2,552 files and 594,718,320 bytes, while excluding generated directories at any depth selected 354 files and 1,311,760 bytes. The 593,406,560-byte difference was generated output. A deep review also removed 8.1 GiB after the gate negative fixtures.

The conformance and security scanners had the related traversal shape: recursively enumerate the whole tree and then reject paths under generated directories with `Where-Object`. This avoided scanning the files but still paid to enter every generated subtree. The proof source cannot become `git archive HEAD`, because negative proofs must exercise the current working tree and must not lose uncommitted or untracked source and configuration.

## Decision

- **One repository-tree enumerator owns pruning.** `eng/repository-tree.ps1` exposes `Get-RepositoryTreeFile`. It walks one or more validated roots directory by directory, skips the closed generated-directory set before descending, and never follows reparse-point directories. Callers filter the returned files by the exact extensions or names they own.
- **One fixture materializer copies the current tree.** The same file exposes `Copy-ProofFoundation`. It obtains every input through `Get-RepositoryTreeFile`, derives a relative path from the source root, creates only the required destination parent, and copies that file. It does not consult Git, so tracked, modified, untracked, and newly created inspection inputs have identical treatment.
- **The excluded directory names are closed and concern-specific.** `.git`, `.vs`, `artifacts`, `bin`, `obj`, `TestResults`, and `Generated Files` are skipped at any depth. They are repository metadata, IDE state, declared artifacts, build/test output, or generated framework source. A new excluded root is a gate-contract change and requires the same proof path.
- **Proof scripts share the materializer.** Both negative-proof orchestrators dot-source `repository-tree.ps1` and remove their local `Copy-Foundation` copies. Their case list, mutations, expected rule IDs, and cleanup containment checks remain unchanged.
- **Scanners prune before filtering.** `conformance.ps1` and `security-check.ps1` use the shared enumerator for recursive source, project, text, secret-file, script, and adversarial-test discovery. Their existing extension/name filters remain at the owning scanner, so traversal optimization does not centralize rule policy.
- **Materialization has executable positive and negative proof.** The gate proof builds a synthetic source tree under its already validated OS temporary root, including an ordinary untracked-style source file and nested generated markers. It asserts that the source is copied, the generated markers are absent, and a reparse point is not traversed when the platform can create one. The production repository is not mutated for this proof.

## Rejected alternatives

- **`git archive HEAD`.** It is fast but silently drops modified and untracked inspection inputs, so a local gate could validate a different tree from the one being committed.
- **A larger exclude list only at repository root.** Nested project output remains copied and the defect returns for every new project.
- **Enumerate recursively and filter afterward.** It avoids copying generated files but still traverses and allocates information for the expensive subtree.
- **Parallel fixture copies.** It increases I/O contention and keeps the unnecessary work; this change removes work instead.
- **Change each scanner independently.** Four subtly different exclusion sets would create competing repository-tree semantics.

## Consequences

- Negative fixtures contain only files the scanners can legitimately inspect plus other non-generated repository inputs; empty directories are not materialized because no gate contract depends on them.
- The materializer performs one file copy per selected input and creates destination parents lazily. The source tree is bounded by the scanner-relevant working tree rather than accumulated build output.
- Reparse-point directories are fail-closed as traversal boundaries. Repository conformance does not need linked external content, and following it could escape the intended source tree or create cycles.
- Scanner rule filters remain readable at their owning sites, while directory pruning has one implementation.

## Migration and removal

Add `eng/repository-tree.ps1`, replace both local `Copy-Foundation` functions, replace recursive-then-filter enumeration in conformance and security, and add the helper to required policy files. No compatibility path remains.

## Executable proof

`eng/prove-gates.ps1` proves ordinary current-tree inputs are copied and nested generated/reparse inputs are omitted before running all existing negative cases. `eng/prove-security.ps1` retains all six security cases. Focused runs of both scripts prove the case counts and rule IDs. Same-tree measurement records selected file count, bytes, and elapsed materialization before and after the change. Commit mode and the final Draft-to-Ready canonical CI gate remain required.
