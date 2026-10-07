# Daily report — Window-adjustment CodeQL remediation — 2026-10-07

Status: informational

## Scope and delegation

hide assigned NeNe Commanderサナ the design and decision role, with explicitly delegated background implementation and investigation. Luna checked the repository and remote evidence; Sol inspected the release-matrix prerequisites and implemented the focused correction; the design owner reviewed the two-loop transformation and owns integration. No external design work was needed.

## Recovered checkpoint

The main checkout initially held `41a682c`, where Issue #100 was already integrated through PR #157. Its state file still described the older checkpoint because the current report and handoff were in the clean `docs/158-session-closure` worktree. PR #159 already had successful dependency run `37499870842` and canonical Ready run `37500002261` on its unchanged head and base. Those results were reused, and PR #159 was squash merged as `e395039`; main was synchronized. The empty, clean, integrated `D:/NeNeCommander/wt-158` worktree was removed after checking its absolute path, identical committed tree, ignored/untracked files, reparse points, and process references. Its branch and commit were retained.

## Deep-review read-back

Both main runs checked `41a682cc7f4c6213f7515cbb0ce6249e0975426f` and succeeded, including their canonical and mutation tiers:

| Run | Domain | Application | Infrastructure.Windows | Presentation.WinUI | CodeQL analysis |
|---|---:|---:|---:|---:|---|
| [37499633218](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/37499633218) | 95.52% | 96.24% | 90.86% | 94.47% | `1902667923`: 2 results, no error or warning |
| [37597191129](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/37597191129) | 95.52% | 96.24% | 90.96% | 94.57% | `1907175157`: 2 results, no error or warning |

The main alert API returned two open note-level `cs/linq/missed-select` findings: [#104](https://github.com/hideyukiMORI/NeNeCommander/security/code-scanning/104) in `KeyHintWrapPanel.Place`, and [#105](https://github.com/hideyukiMORI/NeNeCommander/security/code-scanning/105) in `WindowAdjustmentKeyboardTests`. Workflow success did not mean zero findings. Issue #160 records their correction; neither alert is dismissed or suppressed.

## Invariant and changes

- The existing `KeyHintWrapPanel` remains the only layout adapter. `Children.Select(static child => child.DesiredSize)` preserves child order, one size read per child, lazy single traversal, wrapping arithmetic, resource lookup, and arrangement indexing.
- The existing `KeyboardInputTranslator` proof still checks both `w` and U+0017 under Control, with both original assertions. Only the input projection moves into `Select`; a local character array avoids CA1861.
- The source change is exactly `src/NeNeCommander.App/Views/KeyHintWrapPanel.cs` and `tests/NeNeCommander.Presentation.WinUI.Tests/WindowAdjustmentKeyboardTests.cs`. The report, handoff, and state record the continuation and verification boundary.
- No behavior, visual token, key binding, package, gate, suppression, or architectural mechanism changed. No additional behavior test was needed for the equivalent projection.

## Focused verification

Commands ran from `D:/NeNeCommander/wt-160` and exited 0:

```powershell
dotnet restore D:/NeNeCommander/wt-160/NeNeCommander.slnx -p:Configuration=Release --locked-mode
dotnet build D:/NeNeCommander/wt-160/src/NeNeCommander.App/NeNeCommander.App.csproj --configuration Release --no-restore
dotnet test --project D:/NeNeCommander/wt-160/tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~WindowAdjustmentKeyboardTests|FullyQualifiedName~WindowAdjustmentPresenterTests|FullyQualifiedName~KeyHintPresenterTests' --results-directory D:/NeNeCommander/evidence-codeql-window/160-focused-test-results
$env:Configuration = 'Release'
dotnet format whitespace D:/NeNeCommander/wt-160/NeNeCommander.slnx --include src/NeNeCommander.App/Views/KeyHintWrapPanel.cs tests/NeNeCommander.Presentation.WinUI.Tests/WindowAdjustmentKeyboardTests.cs --verify-no-changes --no-restore --verbosity diagnostic
pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit
git diff --check
```

App build: zero warnings and errors. Focused MTP tests: 38 passed, zero failed or skipped. The first test build caught CA1861 on an inline constant array; the local-array correction passed. An initial formatter load warning came from unbuilt Debug references; the Release diagnostic run loaded the affected App and test project configurations without warnings or errors. The changed files retain UTF-8 without BOM and CRLF. No full local canonical run, all-layer mutation, or desktop interaction was performed.

## Integration evidence location

At this committed checkpoint, the focused implementation is `6e44e88`. The final Ready canonical CI, squash commit, main CodeQL run, analysis result count, and states of alerts #104/#105 must be read from the PR closing [Issue #160](https://github.com/hideyukiMORI/NeNeCommander/issues/160). Record those integration-only results there rather than creating a result-only commit and repeating the gate (the precedent is Issue #2). A clean CodeQL result is not claimed until that read-back exists.

## Issue #94 environment investigation

Read-only inspection confirmed only the shared `info` console session. Hyper-V services and a VM worker were present, but VM enumeration was denied and no dedicated interactive guest or test profile could be identified. This does not establish that no VM exists. No UI was launched and no input or machine setting was changed.

The production settings location uses `Environment.GetFolderPath(LocalApplicationData)`, so changing the `LOCALAPPDATA` environment variable is not proof of profile isolation. The old #70 driver writes the host settings file and must not be rerun on the shared desktop. The #100 driver checks foreground HWND but lacks the per-input PID/session and test-profile containment required by #94, and its default binary path refers to the removed worktree. Reuse individual observation routines only after those boundaries are redesigned. The dedicated environment remains a prerequisite for #94, including its DPI, high-contrast, Narrator, scheme, modal, cross-scale, and taskbar-seam cells.
