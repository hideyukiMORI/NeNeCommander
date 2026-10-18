# Handoff 2026-10-18 record concrete-case closure

Status: informational

Issue #306 implements accepted [ADR-0073](../adr/0073-close-record-cases-with-inaccessible-abstract-members.md)
at baseline `30529cdf9d99dd1e41b86cac2f03d2706237fabb`. See the
[daily report](../reports/2026-10-18-record-case-closure-daily-report.md) for exact commands,
failed development checks, proof scope and environmental limits. #305's proposed/unimplemented
status is historical and is superseded by this implementation.

## Contract and change

The single mechanism is an externally inaccessible abstract instance member in existing
public abstract records, with sealed concrete cases. 118 roots, 499 product cases and
five friend-test cases migrate; FileSystemPath's meaningful identity contract and 11
nonpublic roots remain. A token audit of 266 product files found no other code change
and zero new executable statements. Existing dispatch, factories, immutable payload,
equality/hash/with remain. This intentionally breaks arbitrary external concrete subclassing.
External abstract derivation and friend access remain possible; this is not a hostile-code sandbox.

The pinned compiler inventories actual metadata from four Release assemblies and proves
ordinary/copy/override concrete external rejection, abstract controls and known-case copies.
The same checker has positive, three reopened-hierarchy, wrong-diagnostic, runtime-copy,
friend, stale-binary and disconnected-gate proofs. It runs from the existing canonical gate
and therefore the manual CI path. No new package/project, runtime registry or SDK change.

## Accepted evidence

- Canonical gate exit 0: 112 rules, gate/security negative proofs, locked restore,
  format, zero-warning/error Release build, all 119 visible roots closed, tests and coverage.
- Tests: 1,840 passed, zero failed, 16 explicitly unexecuted live-WSL cases (root absent).
- Branch coverage: Domain/Application 100%, Infrastructure 92.32%, Presentation 94.25%.
- A checker-only stale-binary finding was reproduced RED and corrected. Final CS-002
  self/disconnected proof and product proof passed. Reuse unchanged product build/tests/coverage;
  a docs change, push or merge is not a reason to run them again.
- Format's workspace warning is diagnosed as missing Debug references during App design-time
  loading. All 729 files needed zero format changes; actual Release build passes. Diagnostic log retained.
- No Stryker all-layer/deep/CodeQL, live desktop/provider or release #94 proof was run.
  The marker has no executable statements/branches; three structural reopening mutations
  test the changed invariant. Thresholds/exclusions/suppressions remain unchanged.

## Integration and cleanup

The Issue PR and `D:/NeNeCommander/evidence/issue-306/completion.json` own actual Commit
checks, squash integration, synchronized clean main and cleanup completion. Do not infer
those later steps from test success. Permanent evidence includes source/diagnostic/result
artifacts, RED/GREEN, coverage, structural audit, input hashes, commands, diff and PR metadata.

Additional worktree: `D:/NeNeCommander/worktrees/issue-306`.
Disposable output: `D:/NeNeCommander/outputs/issue-306`.
After integration inspect saved/untracked/ignored files, unique evidence, live references and
links, then remove only those task paths. Keep branch/commit, permanent evidence and the shared SDK.
No additional product or environmental work is implied by this handoff.
