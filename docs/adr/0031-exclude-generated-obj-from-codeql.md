# ADR-0031: Exclude only generated obj trees from CodeQL

Status: accepted

Date: 2026-09-05

## Context

Issue #2 began from CodeQL analysis `1712857281`, which reported 48 open alerts. Current-state read-back before this change found analysis `1723374634` at commit `2ef1310e`: 61 open alerts, of which 47 were WinUI-generated C# below `src/NeNeCommander.App/obj` and 14 were in owned source or tests. A successful deep-review workflow therefore did not mean that CodeQL had no open findings.

The generated files are SDK output, are already outside the repository source and coverage boundaries, and are regenerated rather than reviewed or committed. The owned findings must remain visible and be fixed. The repository must also retain the `security-and-quality` suite and must never replace current-tree analysis with a Git-only or source allowlist view that can omit newly added files.

## Decision

`.github/codeql/codeql-config.yml` is the single CodeQL path configuration. With the existing C# `build-mode: none` analysis, it declares exactly one `paths-ignore` entry, `**/obj/**`. The scheduled/manual deep-review workflow continues to select `security-and-quality` and references that configuration explicitly.

`eng/security-check.ps1` enforces the build mode, query suite, configuration reference, absence of a positive `paths` restriction, and the exact one-entry generated exclusion. `eng/prove-security.ps1` mutates the generated exclusion and query suite independently and proves both changes fail as SEC-008 violations.

Owned path-construction findings are fixed through `WindowsLocalTreeCopy.ResolveDirectChild`, the sole Windows-local target-child resolver used by recursive copy, recursive comparison, and transfer destination construction. It rejects empty, rooted, special, and multi-segment names before joining and verifies the resolved parent. Remaining owned quality findings are expressed with direct sequence predicates or projections without changing enumeration order or materialization ownership.

## Rejected alternatives

- Exclude all generated-looking files or all `*.g.cs`: an owned generator or future source could be hidden without a bounded output directory.
- Configure positive `paths`: an incomplete allowlist could silently omit new production or test roots.
- Analyze `git archive HEAD`: CodeQL would not inspect the final checked-out integration tree as supplied by CI and the model would conflict with current-tree gate proof.
- Dismiss or baseline alerts: that converts current defects into accepted debt and is prohibited by SEC-008.
- Replace `Path.Combine` with a different concatenation primitive only: that removes a query shape without proving the child remains beneath its parent.

## Consequences

- WinUI intermediate output no longer creates repository findings, while every owned path remains in scope.
- Any additional generated directory exclusion is a gate-contract change requiring the same ADR and proof path.
- Workflow completion and alert closure remain separate assertions. The final branch and merged default branch require successful analysis plus API read-back of zero open alerts.
- Target-path construction fails closed if a value that is not one direct filesystem name reaches the adapter boundary.

## Migration and removal

Add the canonical configuration and workflow reference, protect them through SEC-008, repair every currently owned finding, and run one security-sensitive integration deep review. No alternate CodeQL configuration, baseline, dismissal, or compatibility path remains.

## Executable proof

`eng/security-check.ps1` is the positive conformance check. `eng/prove-security.ps1` contains separate generated-filter and query-suite negative fixtures while retaining every prior fixture. Domain, Application, Infrastructure.Windows, and Presentation.WinUI tests cover the owned refactors; `ResolveDirectChildWhenNameIsNotOneSegmentThrowsArgumentException` maps the target-escape cases to ADV-009. A manually dispatched branch deep review must complete with a successful `security-and-quality` analysis and zero branch alerts before Ready. The final PR head/latest base must then pass the canonical CI gate; after merge, the default-branch deep review and API alert read-back establish zero open default-branch alerts.
