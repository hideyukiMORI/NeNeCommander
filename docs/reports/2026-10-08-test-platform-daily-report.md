# Daily report — Test-platform pin alignment — 2026-10-08

Status: informational

## Resumption and completed documentation integration

hide assigned NeNe Commanderサナ the design and decision role and explicitly delegated bounded investigation, implementation, and verification to Sol/Luna background agents. The latest report and handoff were in the Issue #162 worktree. Their read-back showed that Issue #160 and its main CodeQL analysis were already complete; none of those successful checks was repeated.

PR #163 kept head `cec1f5aaca1f448b56210cc65872faf891504be3` and base `2ef9cb0a2cd36408500e3742fd1eb860f351dd11`. Existing dependency run `37660618187` was reused. Ready transition requested [canonical run 37784392752](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/37784392752), which ran `pwsh -NoProfile -File ./eng/check.ps1` and succeeded. After checking the unchanged candidate and clean merge state, the design owner squash merged PR #163 as `ccf4423b2c883acb3ab82684d4e5f1022076cae3` at 2026-10-08 22:35:58 JST and synchronized main with `git pull --ff-only`.

The integrated `D:/NeNeCommander/wt-162` and its two body files in `D:/NeNeCommander/closure-2026-10-08/` were removed after checking absolute paths, identical committed trees, clean/untracked/ignored state, remote body equality, reparse points, and process references. Branch `docs/162-session-closure` and its commit remain. Earlier policy-rejected cleanup targets were not touched.

## Issue #165: invariant and decision

Dependabot PR #164 changed only `global.json`, omitted the final newline, and left CFG-002 and all five test lock files on 4.4.1. [Issue #165](https://github.com/hideyukiMORI/NeNeCommander/issues/165) replaces it through the normal lifecycle.

The canonical mechanism remains the centrally declared test SDK, CFG-002 pin enforcement and negative fixture, and locked package graph. Canonical tests stay on MTP with the Default extension profile; mutation stays on Stryker 5.0.0's isolating VSTest host. No production code, production package, runner choice, test assertion, threshold, suppression, or baseline changes.

The published MSTest.Sdk 4.5.1 package resolves `Microsoft.NET.Test.Sdk` 18.10.1, MTP 2.5.1, and Microsoft coverage extension 18.12.0. The adapter also requires ObjectModel at least 18.10.1. Keeping the old central test SDK 18.9.0 would violate ADR-0048's graph condition, so both pins move atomically. The design owner accepted the dedicated ADR-0006 pin review under hide's delegated authority and updated ADR-0048's current-version statement. The release and package sources are linked in ADR-0006.

The coverage package declares four additional implementation dependencies. Actual .NET 10 restore adds only `Mono.Cecil` 0.11.6 to each test lock file: `System.IO.Pipelines`, `System.Text.Encodings.Web`, and `System.Text.Json` are framework-pruned, as confirmed by `packagesToPrune` in the generated assets. Each mutation test project's lock changes 13 versions and adds Mono.Cecil; Architecture.Tests changes 10 versions and adds Mono.Cecil. There are no removed packages or production-lock changes. The accepted ADR records the test-only scope, alternatives, and next pin-review/removal conditions instead of leaving the addition implicit.

## Focused verification

Work runs from `D:/NeNeCommander/wt-165`; temporary files and logs are under `D:/NeNeCommander/outputs/resume-20261008/sdk-165/`, including the process-local `TEMP` and `TMP` directory. The only main update during verification was the three Markdown files from PR #163; runtime and test inputs were unchanged.

Locked Release restore and Release solution build succeeded with zero warnings and errors. No code changes were needed for the new MSTest analyzers. MTP Domain tests passed 72/72 with zero skips and emitted both TRX and Cobertura; Domain branch coverage was 100%. The existing CFG-002 pin-drift fixture was executed through the repository's proof functions and failed specifically with CFG-002, while the updated pin passed conformance. Commit-mode checks passed.

Domain mutation scored 95.52%, matching the previous baseline: 245 generated, 19 compile errors, 25 removed by the existing covered-block filter, and 201 tested (192 Killed, 9 Survived, zero timeouts). The Presentation `OperationStatus` diagnostic scored 97.14%: all 34 static string mutants were Killed, including `MoveAwaitingConflict` and `CopyAwaitingConflict`, while one nonstatic constructor-block mutant survived. Its trace contains 38 host logs with 38 distinct testhost PIDs. This is evidence for isolation of the changed adapter/host combination, not a claim that every Presentation mutant was tested. Stryker rolled back an unrelated compile-error mutation in `WindowAdjustmentKeyHintPresenter` using its existing safe mode; the ordinary Release build had no warning or error, and no production source was changed to accommodate the diagnostic. The unrelated survivor was not expanded into a second task.

All nine commands below exited 0. The first seven ran from the worktree root; each mutation command ran from its matching test-project directory. `TEMP` and `TMP` were set for those processes to the task's D-drive `tmp` directory. The temporary pin-proof driver extracted the existing `Assert-ConformanceSuccess`, `Assert-ConformanceFailure`, and `mstest-sdk-pin-drift` command from `eng/prove-gates.ps1`, and used the existing `Copy-ProofFoundation`; it did not introduce a second committed gate.

```powershell
dotnet restore NeNeCommander.slnx -p:Configuration=Release --force-evaluate
dotnet restore NeNeCommander.slnx -p:Configuration=Release --locked-mode
dotnet build NeNeCommander.slnx --configuration Release --no-restore
dotnet test --project tests/NeNeCommander.Domain.Tests/NeNeCommander.Domain.Tests.csproj --configuration Release --no-build --no-restore -- --coverage --coverage-settings eng/coverage.settings --coverage-output-format cobertura --coverage-output D:/NeNeCommander/outputs/resume-20261008/sdk-165/domain.cobertura.xml --report-trx --results-directory D:/NeNeCommander/outputs/resume-20261008/sdk-165/domain-trx
pwsh -NoProfile -File D:/NeNeCommander/outputs/resume-20261008/sdk-165/verify-pin-proof.ps1
pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit
dotnet tool restore
dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Domain.csproj --break-at 95 --output D:/NeNeCommander/outputs/resume-20261008/sdk-165/mutation-domain --skip-version-check --log-to-file
dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Presentation.WinUI.csproj --break-at 90 --mutate **/Panes/OperationStatus.cs --output D:/NeNeCommander/outputs/resume-20261008/sdk-165/mutation-operation-status --skip-version-check --log-to-file
```

The final required Ready CI and squash result belong in the PR closing Issue #165; a matching successful CI result is reused rather than rerun locally. No local full canonical gate or all-layer mutation was run for this update. The final CI covers all test projects, the shared test-platform/analyzer changes, coverage thresholds, package audit, and the complete existing gate proofs. The remaining mutation layers stay in the scheduled deep-review tier; no new deep-review success is claimed.

## Remaining environmental proof

hide confirmed in this session that a dedicated Windows UI test environment is **not prepared**. Issue #94 remains open and no required matrix cell is claimed passed. Read-only inspection found the shared `info` console, not an approved guest/account/session. No UI was launched and no input, settings, DPI, high contrast, or host account was changed.

The old Issue #100 driver still cannot be used as the #94 harness: its foreground guard checks HWND only, its default binary points to the removed worktree, and `Stop-Run` sends held-key releases even after losing foreground ownership. The new harness must verify process/session/root HWND, mode/modal, and focus before each input, including cleanup; focus loss stops further input. A dedicated profile must resolve the actual production settings location, preserve its bytes or absence, and restore only that owned profile. The existing 125-percent evidence is still valid for its unchanged implementation.

The next product work is the dedicated environment and safe preflight for #94, followed by its real DPI, high-contrast, scheme, keyboard-modal, Narrator, cross-scale, and taskbar-seam cells. This test-platform update does not substitute for those results.
