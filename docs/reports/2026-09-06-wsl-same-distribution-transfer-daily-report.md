# Daily report — WSL same-distribution transfer — 2026-09-06

Status: informational

## Scope

Issue #85 adds copy and composite move within one WSL distribution. It does not add an atomic WSL move, cross-distribution or Windows-local/WSL transfer, overwrite, recycle, a new dependency, or a collision default.

## Invariant and canonical mechanism

`FileOperationGateway` remains the only transfer owner. `WslFileOperationAdapter` implements complete-batch preflight, copy, verification, and permanent delete provider steps through `WindowsWslFileSystem` and the shared `WindowsLocalIoExecutionBoundary`. Source identity is revalidated at each step, links are not followed, and a move deletes its source only after copy and verification succeed. A partial target is reported and never rolled back automatically.

## Failure-first proof

The first gateway test requested a valid same-distribution WSL copy and required `Copied` plus `Verified` effects. Release build passed, but the test failed because the existing adapter returned `Rejected`, proving the transfer members were still unavailable before implementation.

## Changes

- Added full-batch WSL preflight for destination existence/type/link, source identity/tree links, containment, derived target, and collision.
- Added same-distribution copy and verification through the existing recursive tree-copy contract.
- Reported WSL atomic capability as unsupported so the existing gateway composes move safely.
- Preserved `CopyTargetCreated` on expected failure and stopped source deletion after copy, verification, or identity failure.
- Added deterministic gateway, adapter, race, collision, link, partial-result, and test-owned real file/directory copy proof.
- Added ADR-0037 and aligned filesystem, security, adversarial, project-state, report, and handoff records.

## Focused verification

- Release solution build: PASS, zero warnings and errors.
- Infrastructure.Windows tests: 132/132 PASS.
- Intermediate Infrastructure.Windows branch coverage: 94.30% (minimum 90%); this precedes the final target-link and filesystem proof changes.
- Intermediate focused mutation improved from 90.91% to 100.00% (130/130 killed). Later changes and their separate results are recorded in the shutdown checkpoint below.
- Architecture tests: 5/5 PASS. Earlier conformance/security results are development evidence; the save-time Commit hook and pending full CI establish their respective final scopes.

Exact-head deep review, dependency review, and Ready canonical CI remain pending integration evidence.

## Remaining environmental proof

No live WSL tree was copied or deleted. Live proof remains opt-in and requires a validated `NENE_COMMANDER_WSL_TEST_ROOT`. Cross-distribution and Windows local/WSL transfer remain unavailable for the next independent Issue.
## Shutdown checkpoint

hide requested a resumable stopping point. Issues #81/PR #82 and #83/PR #84 are merged; their successful integration runs and commits are recorded in PROJECT_STATE and the matching handoff. Issue #85 remains unfinished on `feat/85-wsl-same-distribution-transfer`, based on `4b01d286cea2a823dddddd87aca0c611bbb2d0ff`, for preservation in a Draft PR. No Issue #85 merge readiness is claimed.

The implementation worker was stopped after the final filesystem mutation completed. Its supervisor and notification loop have stopped and automatic restart is disabled by local stop markers. The next session should follow the handoff instead of automatically starting the old queue.

Windows Terminal crashed at 04:06:28 JST (`Windows.UI.Xaml.dll`, exception `0xc000027b`), but the background worker continued. The repository changes were retained. The crash's root cause was not diagnosed.

### Exact verification commands

Commands below ran from the repository root unless specified. All listed successful results were checked against command output or saved reports.

| Command | Result |
|---|---|
| `dotnet build NeNeCommander.slnx --configuration Release --no-restore` | Final post-mutation restore of normal binaries: exit 0, zero warnings/errors. |
| `dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-build --no-restore` | Final checkpoint: exit 0, 132/132 passed. |
| `dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --configuration Release --no-build --no-restore` | Final checkpoint: exit 0, 5/5 passed. |
| `dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-build --no-restore -- --coverage --coverage-settings eng/coverage.settings --coverage-output-format cobertura --coverage-output issue85-final.cobertura.xml` | Earlier checkpoint: exit 0, 94.30% branch coverage, report in `TestResults/issue85-final.cobertura.xml`. Later changes still require integration coverage. |
| `git diff --check` | Exit 0 before save. |

From `tests/NeNeCommander.Infrastructure.Windows.Tests`:

- `dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Infrastructure.Windows.csproj --mutate "**/WslFileOperationAdapter.cs" --mutate "**/WindowsWslFileSystem.cs" --mutate "**/WslFileSystemEntry.cs" --break-at 95 --output ../../artifacts/mutation/issue85-wsl-transfer-target-link --skip-version-check`: exit 0, 136/138 killed (98.55%), two findings in WindowsWslFileSystem.
- `dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Infrastructure.Windows.csproj --mutate "**/WindowsWslFileSystem.cs" --break-at 95 --output ../../artifacts/mutation/issue85-wsl-filesystem-attributes --skip-version-check`: exit 0, 38/38 killed (100%) after added link/attribute proof. This does not replace all-layer deep review.

The broad diagnostic `dotnet format NeNeCommander.slnx --verify-no-changes --no-restore` returned exit 1 for mixed line endings and existing XAML event-handler IDE0051/IDE0060 findings. The canonical gate uses `dotnet format whitespace`, not that broad diagnostic. Normalize only the changed files to the prescribed CRLF form, verify with the canonical whitespace command, and retain the broader diagnostic as a review note without changing unrelated event handlers or suppressing rules. Save-time formatting and hook results are recorded below and in the PR.

Pending: final-head security deep review, dependency review, complete canonical CI and live WSL/release environmental proof. The full command `pwsh -NoProfile -File ./eng/check.ps1` was not executed for Issue #85 at shutdown. No waiver or threshold reduction was introduced.

- Save-time formatting: changed text files normalized to CRLF; dotnet format whitespace NeNeCommander.slnx --verify-no-changes --no-restore and git diff --check both exited 0.

## Resume review

hide resumed Issue #85 on 2026-09-06. Complete-diff review found four missing effect-boundary checks: copy did not repeat recursive source/destination containment, verification did not rescan the source tree for a late reparse point, copied-target inspection checked only the top-level entry, and permanent source deletion did not rescan the source tree. These gaps conflicted with ADR-0037, FS-012, and SEC-003.

Failure-first proof added four cases to the Infrastructure project. Against the saved implementation, 131/135 tests passed and exactly the four new cases failed. The adapter now repeats containment before copy, verification refuses a reparse point anywhere in the current source or copied target tree, and permanent deletion refuses a reparse point anywhere in the current source tree. `WindowsWslFileSystem` uses the existing non-following recursive tree inspection for both source and target trees. This closes the identified checks without claiming that Windows path reopening is handle-relative or race-free.

Focused resume verification:

- `dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-restore`: exit 0, zero warnings/errors.
- `dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-build --no-restore`: exit 0, 135/135 passed.
- The same test project with MTP coverage arguments produced 94.48% Infrastructure branch coverage (minimum 90%).
- Complete focused mutation of `WslFileOperationAdapter.cs`, `WindowsWslFileSystem.cs`, and `WslFileSystemEntry.cs`: exit 0, 144/144 tested mutants killed, 100.00%.

Exact-head deep review and canonical Ready CI remain integration evidence. Live WSL tree copy and deletion remain unexecuted environmental proof.

## Integration review findings

Draft head `d10f7619a146491f8747cfcfcdf9c05b8deb45b2` passed dependency review `34014276416`. Exact-head deep run `34014279382` then passed its canonical gate, locked restore, dependency audit, three adversarial repetitions across all four test projects, 467 tests, coverage thresholds, and CodeQL execution. The run stopped at the first all-layer mutation project because unchanged Domain code scored 94.97% (189 killed / 10 survived), so later Application, Infrastructure.Windows, and Presentation.WinUI mutation did not run.

The Domain report exactly matches the ten survivors from Issue #81's first deep run `33985153722`. Its same-head retry `33985643645` classified two otherwise identical mutants as killed and passed at 95.98%. Review found a real unprotected contract in one of those two: tests covered only input beyond `FileSystemPath`'s maximum length, not a valid path at the exact maximum. Independent Issue #89 added that boundary proof without a threshold, exclusion, baseline, or production parser change. Its exact-head deep run `34015752322` and canonical Ready run `34016537381` passed before squash merge `b207c856`.

The completed CodeQL analysis opened two query-shape alerts in `WslFileOperationAdapter`. Preflight now projects provider outcomes explicitly and selects the first failure, while distribution validation uses an all-source predicate. Focused proof independently preserves linked-destination rejection and rejects a batch containing one foreign-distribution source. The Infrastructure.Windows suite passes 135/135 and complete focused mutation kills 143/143 tested mutants (100.00%). The branch is now rebased onto Issue #89's merge; a fresh #85 exact-head deep review remains required.

## Rebased candidate

Issue #89 merged as `b207c856dbff0bb4803d00fe1cfbd397b8214e89`. After rebasing #85 onto that verified main, locked restore and the Release solution build passed with zero warnings or errors. Domain passed 66/66, Infrastructure.Windows 135/135, and Architecture 5/5. The final Infrastructure.Windows coverage run reports 94.23% branch coverage (minimum 90%). Commit validation and a fresh exact-head deep review remain required after saving the rebase evidence.

`NENE_COMMANDER_WSL_TEST_ROOT` is not set. Repository inspection found no executable test or script that reads it; current live-proof material consists of the normative opt-in contract and deterministic Windows-temporary-root tests. Therefore no live WSL copy or deletion can be run from the current tree. A live tier still requires an explicit harness that validates a dedicated empty root, rejects the distribution root, `/home`, any user home, `/mnt`, the repository, and their ancestors, and revalidates the root before cleanup.
