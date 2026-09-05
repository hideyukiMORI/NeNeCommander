# Daily report — CodeQL remediation — 2026-09-05

Status: informational

## Scope

Issue #2 separates generated WinUI intermediate output from owned findings without weakening CodeQL. ADR-0031 records the gate-contract change and the exact default-branch evidence required after merge.

## Invariant and canonical mechanisms

Every owned production and test path remains analyzed by `security-and-quality`; only directories named `obj` are excluded. `.github/codeql/codeql-config.yml` is the single CodeQL path configuration. `WindowsLocalTreeCopy.ResolveDirectChild` is the single target-child resolver for Windows local copy and comparison.

## Current-state measurement

- Initial Issue evidence: analysis `1712857281`, 48 open alerts.
- Current read-back before editing: analysis `1723374634` at `2ef1310e`, 61 open alerts.
- Classification: 47 generated `obj` findings and 14 owned production/test findings.
- The latest scheduled deep workflow was successful, demonstrating why workflow success and alert count must be checked independently.

## Changes

- Added the exact `**/obj/**` exclusion and retained C# `build-mode: none` plus `security-and-quality`.
- Protected the mode, suite, configuration reference, no-positive-path rule, and exact exclusion as SEC-008.
- Retained all six security negative cases and added two weakening cases.
- Replaced unsafe parent/child combination with a direct-child boundary shared by copy, match, and destination construction.
- Re-expressed the currently reported owned sequence findings without changing order, short-circuit behavior, or owned snapshots. Findings already removed by intervening merged work were not reimplemented.

## Focused verification

- `pwsh -NoProfile -File ./eng/security-check.ps1`: PASS; 18 adversarial cases and 8 security negative fixtures.
- Locked Release restore and solution build: PASS after analyzer-guided simplification; zero warnings and errors.
- Initial focused set: Domain 65, Application 176, Infrastructure.Windows 76, Presentation.WinUI 76; PASS, 393 total and zero skipped.
- After the first branch analysis exposed 16 findings from intervening merged work, Application 176, Infrastructure.Windows 78, Presentation.WinUI 76, and Architecture 5 passed. Focused Infrastructure mutation recovered from the branch run's 85.11% failure to 90.61%, then remained at 90.68% after the final one-alert fix, above its protected 90% break threshold.

## Pending integration evidence

Draft run `33968797800` proved canonical gate, 398 tests, coverage, and CodeQL analysis, but failed Infrastructure mutation at 85.11%. Its branch alert read-back proved generated findings were gone and exposed 16 owned findings introduced across the current integration base; all are addressed in the next Draft head. That final Draft branch still requires a successful manually dispatched security deep review and branch alert read-back of zero. The final head/latest base then requires the canonical Ready CI gate. After squash merge, a default-branch deep review and API read-back must show zero open default-branch alerts. These results may be recorded in the PR instead of creating a result-only documentation commit and another gate run.
