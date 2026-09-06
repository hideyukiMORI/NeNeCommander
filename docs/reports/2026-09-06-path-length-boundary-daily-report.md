# Daily report — path-length boundary proof — 2026-09-06

Status: informational

## Goal and invariant

Issue #89 protects the existing `FileSystemPath.Parse` length contract exposed by Issue #85's security deep review. A valid absolute path whose input is exactly 32767 characters is accepted; the first longer input is rejected as `TooLong`. Production parsing, the fixed limit, mutation thresholds, exclusions, and baselines remain unchanged.

## Failure-first evidence

The existing test used `C:\` plus 32768 characters and therefore covered only input well beyond the limit. A complete focused mutation run against `FileSystemPath.cs` left equality mutant 30 (`input.Length >= MaximumPathLength`) alive with no killing test. The command intentionally used a 100% break threshold for diagnosis and exited 1 at 95.57%; this did not change repository policy.

## Change and proof

`FileSystemPathTests` now constructs an otherwise valid Windows local path at exactly 32767 characters and proves successful canonical parsing. The existing rejection proof now uses exactly 32768 characters, the first value beyond the boundary.

- Domain Release build: zero warnings and errors.
- Domain tests: 66/66 passed.
- Domain coverage: 100.00% line and branch coverage (173/173 lines, 136/136 branches).
- Final focused `FileSystemPath.cs` complete mutation: 97.47%; mutant 30 changed from `Survived` with no killer to `Killed` by one test.
- Whole Domain complete mutation: 96.48% in this run, above the protected 95% threshold.

The runner has previously varied when classifying unrelated hash-code mutants. This change does not treat those incidental kills as contract evidence: applying the one deterministic boundary kill to the prior repeatable 189/199 result yields 190/199, or 95.48%, independently above the threshold.

## Remaining integration proof

Draft dependency review, exact-head security deep review, and the final Ready canonical CI gate remain pending. Issue #85 stays Draft until this focused proof is integrated and its branch is rebased for fresh evidence.
