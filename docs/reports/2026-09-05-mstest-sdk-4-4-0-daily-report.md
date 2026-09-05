# Daily report — MSTest.Sdk 4.4.0 — 2026-09-05

Status: informational

## Scope

Issue #68 replaces stale Dependabot PR #27 through the normal Issue and branch path. It adopts the stable MSTest.Sdk 4.4.0 release and keeps the repository's Microsoft.Testing.Platform runner, Default extension profile, warning policy, and gate thresholds unchanged.

## Invariant and canonical mechanism

All five test projects use one centrally pinned MSTest SDK and remain discoverable through the Microsoft.Testing.Platform runner. `global.json` is the single version pin; CFG-002 and its negative fixture reject drift from it. Per-project lock files are the reproducible dependency record.

## Release review

The official 4.4.0 release was published on 2026-09-02 as a stable release. Its relevant changes include new MSTest analyzers, Microsoft.Testing.Platform 2.4.0, updated coverage/TRX extensions, and a native MTP adapter path that no longer depends on VSTestBridge. No new repository dependency, runner mode, suppression, or policy exception is introduced.

- Release: <https://github.com/microsoft/testfx/releases/tag/v4.4.0>
- Changelog: <https://github.com/microsoft/testfx/blob/main/docs/Changelog.md#4.4.0>
- Upstream comparison: <https://github.com/microsoft/testfx/compare/v4.3.3...v4.4.0>

## Focused verification

- Updating restore and subsequent locked Release restore: PASS for all ten projects.
- Release build with 4.4.0 analyzers: PASS with zero warnings and errors.
- Full discovery/execution: 400 passed, zero failed, zero skipped.
- Adversarial filtering: Domain 38, Application 48, Infrastructure.Windows 32, Presentation.WinUI 5; all passed.
- Branch coverage: Domain 100%, Application 100%, Infrastructure.Windows 96.27%, Presentation.WinUI 96.84%; all protected thresholds passed.
- Negative gate proofs: PASS, including the new CFG-002 4.4.0-to-4.3.3 pin-drift fixture.
- Every test lock resolves MSTest.TestAdapter and MSTest.TestFramework 4.4.0 plus Microsoft.Testing.Platform 2.4.0; the former VSTestBridge dependency is absent.

## Pending integration evidence

The exact final head and latest base require the canonical Ready CI gate. That result belongs in the PR body so a result-only documentation commit does not invalidate it.
