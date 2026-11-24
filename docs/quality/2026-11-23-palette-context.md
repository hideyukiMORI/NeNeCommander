# Compact palette with one captured context — Issues525/526

Date: 2026-11-23
Scope: local implementation and focused evidence; no publication or integration yet.

hide reports that language selection/save/reopening/restart, translations and
preservation of other settings work, with Japanese kanji input in the English
palette. The individual language sequence was not itemized. Immediate switching
remains requested, not implemented by this change. The same feedback reiterates
Issues525/526 and adds window placement persistence/off-screen correction.

Worktree: D:/NeNeCommander/worktrees/issue-526.
Evidence: D:/NeNeCommander/evidence/palette-context-20261123.
Fixture: D:/NeNeCommander/outputs/palette-context-20261123/attached-probe.
The live candidate in issue527 and its current user settings are preserved.

## Ownership and implementation

Import the existing525 commits onto e48b250, retaining both additive PROJECT_STATE
histories. The ordinary hooks pass112 rules/security24/message checks. The prior
125% short-height synthetic-Chinese failure remains historical evidence.

CommandPalettePresenter/Context/Feedback project one captured common header.
Rows retain title, shortcut, unavailable reason/marker and full-title tooltip,
without their repeated target subtitle. Ordinary available pane commands do not
repeat the header in the detail bar, which collapses when empty. Transfer, opposite
activation, settings, themes, window adjustment, help and bookmarks have accurate
selected-detail/accessibility scope. Availability is never bypassed for global
rows. Filtering to global-only/zero rows cannot change the captured header.
CommandPaletteSession/dispatch/eligibility/key-map source remain unchanged.

## Executed commands and results

All commands run from the worktree unless noted. Logs named below retain complete output.

```powershell
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~CommandPaletteScopeTests'
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~CommandPalette|FullyQualifiedName~KeyboardPaletteTests'
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj -p:Configuration=Release --locked-mode
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore
pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect
```

- The six new cases compile and all fail against unchanged product for their
  intended visible/accessibility scope defects (baseline-tests.log, exit2).
- The first revised test build fails on ten old constructor calls in existing
  tests (focused-build-1.log). Migrate all callers, retaining required-input checks.
- Final selected34 tests pass0 failures/skips; build0 warnings/errors, exit0
  (focused-build-2.log/focused-tests-2.log). Six regressions are included, not added twice.
- Final App restore/build exit0 with0 warnings/errors (app-restore.log/app-build.log).
- Complexity exit1 reports exactly the same five findings, including location/value,
  as the previous theme candidate (complexity.json); no new finding or clean claim.
- `dotnet format whitespace <project> --no-restore --verify-no-changes --include <changed files>`
  selects only changed Presentation, test and App files; exact arguments/results are
  in the execution record and format logs. App reports its existing workspace-load
  warning while exiting0. Successful checks are reused.
- `git diff --check` succeeds. The three catalogs have identical546 keys, matching
  placeholder multisets and every dynamic palette projection key. A first
  order-sensitive comparator rejected an allowed translation argument reorder;
  the corrected multiset check passes (resource-parity.json).

## Hidden attached layout

Copy only the six existing standalone fixture input files, retaining the pinned
SDK/lock/dependency, and point its sole production panel source/XAML/resources at526.
From that fixture directory run:

```powershell
dotnet restore LayoutProbe.csproj -p:Configuration=Release --locked-mode
dotnet build LayoutProbe.csproj -c Release --no-restore
```

Both exit0; build0 warnings/errors. Start only its LayoutProbe.exe with
`Start-Process -WindowStyle Hidden`, retaining its PID/exit in fixture-launch.json
and fixture-exit.json. Owned PID43432 exits0. No Show/Activate/input/settings access
is used; observed activations0, everVisible=false and foreground unchanged.

The actual attached XamlRoot measures125%. The36 cases use real en-US/ja-JP/zh-Hans
strings,640/320 widths,800/300 heights, and ordinary/transfer-unavailable/global
detail. Long actual title/reason stress each row. In every case first/last/returned
rows fit entirely, modal/hints remain inside the client and scroll returns to0.
Full raw output and geometry-summary.json retain all108 position captures.

This is literal-row geometry with the real tokens/templates/panel. It does not
exercise product bindings, the Loaded tooltip handler, physical keys, IME, pointer,
focus, visible pixels or speech. Do not turn the old candidate's native language
acceptance into acceptance of this changed layout. Other DPI/contrast/native
acceptance remain named limits. No full/deep/product-wide regression or CI is run:
ADR-0061 selects this bounded projection/XAML consumer; unchanged execution and
settings proof is reused.

## Remaining work

Save the local candidate through the normal Commit hook, then implement requested
immediate language switching and window placement as their own focused changes.
#548 compact Locations remains a separate existing candidate. No new app launch,
public payload approval, merge, release or completed-task cleanup is claimed.
Keep incomplete worktree/fixture/output and the live527 reference.
