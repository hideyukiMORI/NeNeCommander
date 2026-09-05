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
