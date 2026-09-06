# Shutdown handoff — WSL same-distribution transfer — 2026-09-06

Status: informational

## Stop and resume contract

hide requested a shutdown checkpoint. Implementation is paused on Issue #85, branch `feat/85-wsl-same-distribution-transfer`, based on verified main `4b01d286cea2a823dddddd87aca0c611bbb2d0ff`. This is an interrupted-work handoff, not merge readiness or completion of all product work. Save this change as a coherent commit and Draft PR; the PR records the resulting head and checks.

The background worker was stopped after its final focused filesystem mutation command completed successfully. Its supervisor and notification loop are stopped; local stop markers prevent automatic restart. Resume repository work only after a new instruction from hide. Do not blindly restart the old unattended queue.

## Completed integration checkpoints

- Issue #81 / PR #82: WSL directory reading, merged as `1b514282`; dependency `33985129013`, deep retry `33985643645`, canonical `33986546237` succeeded.
- Issue #83 / PR #84: WSL directory creation, same-parent rename and confirmed permanent deletion, merged as `4b01d286`; dependency `33988561920`, deep `33988574140`, canonical `33989351752` succeeded.
- Earlier completed work and remaining release proof are indexed in `docs/PROJECT_STATE.md`.

## Invariant and canonical mechanism

ADR-0037 extends `FileOperationGateway` through `ProviderFileOperationPort`, `WslFileOperationAdapter`, `WindowsWslFileSystem`, existing `WindowsLocalTreeCopy`, and `WindowsLocalIoExecutionBoundary`. There is one transfer orchestrator. Complete-batch preflight precedes effects; each provider step revalidates source identity; links are not followed; partial targets are reported without rollback; move deletes the source only after copy and verification succeed.

## Files changed

- Infrastructure FileOperations: `IWslFileSystem.cs`, `WindowsWslFileSystem.cs`, `WslFileOperationAdapter.cs`, `WslFileSystemEntry.cs`.
- Infrastructure tests: `WindowsWslFileSystemTests.cs`, `WslFileOperationAdapterTests.cs`.
- Policy: filesystem/security/project state, adversarial registry, ADR-0037 and ADR index; supersession notes in ADR-0017, ADR-0029 and ADR-0036.
- Reports: this handoff, matching transfer daily report, and completion notes in the prior mutation report/handoff.

## Proof at shutdown

The matching [daily report](../reports/2026-09-06-wsl-same-distribution-transfer-daily-report.md) records exact commands and results.

- Final Release build: exit 0, zero warnings/errors. Infrastructure tests: 132/132; Architecture: 5/5, both exit 0 after restoring normal binaries following mutation.
- Gateway copy first failed with `Rejected` before implementation. Negative cases cover full-batch preflight, collisions, containment, foreign distribution, links, identity races, copy failure, verification failure, and preserved source/partial target effects.
- Earlier coverage checkpoint: 94.30% Infrastructure branch coverage. Later target-link and filesystem proof changes require final integration coverage; the earlier number is not claimed for the final commit.
- Earlier three-file mutation: 130/130 killed. After target-link changes, the next three-file run was 136/138 (98.55%), with two filesystem findings. Subsequent filesystem-focused proof reached 38/38 (100%) on the final source. These are separate runs, not a claim of a new whole-layer 100% result.
- `eng/check.ps1 -Mode Commit` is the save-time hook, not the full integration gate. Its actual result is recorded with the commit/PR.

## Resume steps

1. Read AGENTS and PROJECT_STATE completely, this handoff, ADR-0037, and the PR. Inspect `git status`, local/remote head and latest main; preserve any later work.
2. Review the complete Draft diff, including target-link and directory-attribute proof added late in the session. Check PR dependency review.
3. Obtain security-sensitive deep review on the final head using the existing workflow (`eng/deep-review.ps1`), including all-layer mutation, dependency audit and CodeQL. Review alerts and unresolved threads separately. No successful Issue #85 deep result exists at this checkpoint.
4. Resolve any findings while Draft. Once ready, verify the final head/base and mark Ready to request the full canonical CI command `pwsh -NoProfile -File ./eng/check.ps1`. It was not run for this Issue before shutdown. Do not merge without fresh successful evidence or duplicate a successful CI gate locally.
5. Only after successful integration, squash merge and synchronize main. Cross-provider transfer is future separate work; do not start it as part of shutdown recovery without resumed authorization.

## Resume review delta

hide resumed Issue #85 on 2026-09-06. Complete-diff review added failure-first proof for four missing boundary checks and then repaired them within the existing WSL provider path:

- copy repeats recursive source/destination containment immediately before its side effect;
- verification rescans both the source tree and copied target tree for late reparse points before comparison;
- copied-target link inspection is recursive instead of top-level only;
- permanent source deletion rescans the complete source tree for a late reparse point.

Against the saved implementation, the four new cases were the only failures in the 135-test Infrastructure run. After the repair, the Release test-project build passed with zero warnings/errors, Infrastructure passed 135/135, branch coverage was 94.48%, and complete focused mutation killed 144/144 tested mutants. The Windows namespace still reopens paths between checks and effects as already documented; this change does not claim handle-relative race elimination.

Exact-head security deep review, canonical Ready CI, and live WSL proof remain pending at this saved checkpoint.

## Resumed integration review

Draft head `d10f7619a146491f8747cfcfcdf9c05b8deb45b2` passed dependency review `34014276416`. Exact-head deep run `34014279382` passed its canonical, restore, dependency-audit, repeated-adversarial, test, coverage, and CodeQL execution stages, then stopped at unchanged Domain mutation score 94.97% before the other three mutation layers. The ten survivors exactly match Issue #81's first failed deep artifact. Investigation confirmed that maximum-length input acceptance lacked direct behavioral proof. Independent Issue #89 added the exact 32767/32768 boundary proof, passed deep run `34015752322` and canonical run `34016537381`, and merged as `b207c856` without changing production parsing or thresholds. No blind retry is accepted as evidence.

That CodeQL analysis opened two alerts in the #85 WSL adapter. The loops now express first-failure projection and all-source distribution validation directly. Test setup now proves linked-destination rejection independently of a previously linked source and adds a mixed-distribution source batch. Infrastructure.Windows passes 135/135, and the three-file complete focused mutation run kills 143/143 tested mutants (100.00%). The branch is rebased onto Issue #89's merge and still requires a new exact-head CodeQL/deep result.

Rebase proof on `b207c856dbff0bb4803d00fe1cfbd397b8214e89` passed locked restore, a zero-warning Release solution build, Domain 66/66, Infrastructure.Windows 135/135, Architecture 5/5, and 94.23% Infrastructure.Windows branch coverage. Final commit validation and exact-head deep review remain pending.

## Environmental proof and decisions

No live WSL tree was copied or deleted. Deterministic real I/O tests used a test-owned Windows temporary root. `NENE_COMMANDER_WSL_TEST_ROOT` is unset, and the repository currently contains no executable test or script that reads it. Live proof therefore requires both a configured dedicated empty root and an explicit harness that enforces FS-012's forbidden-root and cleanup checks; unit success does not establish it. UNC/removable media, high contrast, DPI and the remaining runtime UI matrix remain release proof.

Cross-distribution and Windows/WSL transfer, WSL atomic move, overwrite and recycle remain unavailable. Issues #72 (hidden-item key), #73 (collision policy), and #74 (settings persistence policy) are tracked separately and must not expand #85's scope.
