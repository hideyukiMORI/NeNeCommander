# Daily report — command palette — 2026-09-09

Status: informational — stopped checkpoint; not merge-ready

## Goal and invariant

Issue #101 adds `Ctrl+P` command search without adding a second command implementation. The palette searches the 15 commands declared by the canonical command and keyboard models, captures the active and passive pane scope when it opens, derives availability from that captured state, and submits the selected existing `UserIntent` through `CommanderSession`. Busy, modal, text-entry, IME, destructive-confirmation, selection, focus, and active-pane precedence remain owned by the existing session and keyboard routes.

The single canonical mechanisms affected are `CommanderSession` with `CommandCatalog` for catalog, captured scope, availability, and qualified routing; `CommandPalettePresenter` with `CommandPaletteViewState` for localized filtering and selection; and `KeyboardIntentMapper` for all non-text key mapping. ADR-0047 records this extension.

## Implemented branch state

Draft [PR #113](https://github.com/hideyukiMORI/NeNeCommander/pull/113) on `feat/101-command-palette` remains open and unmerged. It is based on main `6c9abd8998ce55f06d768ea9471aeb6adcd4d84c`. Deep-review [run `34256292455`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/34256292455) evaluated exact head `eee23c4e7b8381b9cffb7863b03623c994665593`.

The pushed head adds the closed 15-command Application catalog, captured palette scope and state-only availability, Presentation-owned localized labels and filtering, deterministic selection and key hints, the WinUI overlay, qualified keyboard and pointer submission, cancellation, IME deferral, accessibility identities, and the Enter repeat guard. It does not add bookmark persistence, window commands, recent-use ranking, dynamic shortcuts, a new file operation, or a parallel execution route.

This checkpoint records later post-deep changes in addition to the two feature commits:

- a CodeQL correction in `CommanderWindow.xaml.cs` that captures `_addressPresentation` once before deciding address-edit and status rendering; and
- four broader Presentation contract tests in `CommandPalettePresenterTests.cs`, plus completion of the key-hint label assertions. These cover the complete label correspondence, all 15 projected catalog rows, all six unavailable reasons, and zero/one/many-row selection boundaries.

These post-deep changes lack new deep-review and CodeQL evidence. Resolve the checkpoint revision from the branch tip or PR #113; this document does not embed its own revision.

## Focused evidence

The latest focused coverage command against the post-deep source and tests recorded by this checkpoint passed:

```powershell
pwsh -NoProfile -File ./eng/verify-coverage.ps1
```

It protected 693 tests: Domain 71/71, Application 294/294, Infrastructure.Windows 222/222, and Presentation.WinUI 106/106. Branch coverage was 100.00%, 100.00%, 92.81%, and 94.17%, respectively.

The candidate CodeQL correction also passed a Release App build with zero warnings and zero errors:

```powershell
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj --configuration Release --no-restore
```

The latest conformance evidence passed 112 unique normative rules, and the pushed-head commit hook passed the 19-case security/adversarial registry and commit convention checks. Dependency-review run `34256050939` passed for the pushed head. These scoped successes do not establish merge readiness.

## Failed deep evidence

Deep-review run `34256292455` evaluated exact head `eee23c4e7b8381b9cffb7863b03623c994665593` and failed. Domain, Application, and Infrastructure.Windows mutation passed, as did the other recorded deep stages, but Presentation.WinUI mutation scored 80.98%: 592 successful and 139 failed outcomes among 731 mutants. The run therefore did not satisfy the 90% break threshold.

The run's CodeQL analysis `1742890023` completed but produced open [alert #102](https://github.com/hideyukiMORI/NeNeCommander/security/code-scanning/102), `cs/dereferenced-value-may-be-null`, for `_addressPresentation` in `CommanderWindow.xaml.cs`. The post-deep correction builds successfully, but no CodeQL analysis has evaluated that correction. Alert closure is therefore unproved.

After the four Presentation tests were added, one explicitly authorized Presentation-only mutation diagnostic ran with the existing repository configuration and no runner or threshold change:

Working directory: `tests/NeNeCommander.Presentation.WinUI.Tests`

```powershell
dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Presentation.WinUI.csproj --break-at 90 --output C:\Users\info\WORKS\NeNeCommander-101\artifacts\security\mutation\NeNeCommander.Presentation.WinUI-issue101-recheck --skip-version-check
```

It still failed at 83.99%: 601 killed plus 13 timed out, and 110 survived plus 7 had no coverage, for 614 successful and 117 failed outcomes among 731 mutants. The report is retained locally at `C:\Users\info\WORKS\NeNeCommander-101\artifacts\security\mutation\NeNeCommander.Presentation.WinUI-issue101-recheck\reports\mutation-report.json` and excluded from the repository.

Independent read-only review found that some surviving mutants are reported as covered by exact assertions while remaining unkilled, including entries with `CoveredBy` 106 and `KilledBy` 0. Survivor count therefore cannot be treated as a direct count of missing behavioral contracts. A Stryker 4.16 / Microsoft.Testing.Platform test-activation problem is a plausible explanation and is consistent with the sharp increase in survivors in previously passing static Presentation code, but it remains an inference. The review did not establish the internal cause and did not classify all 110 survivors as equivalent or false positives. ADR-0048 later established that cause and closed the inference.

For comparison only, the immediately preceding passing deep run `34239166946` evaluated head `43b60a9` and reported Presentation.WinUI at 90.81%: 501 killed plus 13 timed out, and 47 survived plus 5 had no coverage, among 566 mutants. That older result is neither evidence for Issue #101 nor evidence for this post-deep checkpoint.

## Stop decision

PR #113 remains Draft and unmerged because the Presentation mutation tier is below threshold and the CodeQL correction has not been reanalyzed. No further mutation run, deep-review retry, Ready transition, canonical gate, or merge is authorized at this checkpoint.

Issue #99 bookmark work remains separate and must not be mixed into this branch. Live WSL proof remains open in Issue #93, and the native WinUI IME, Narrator/UIA, high-contrast, DPI, narrow-window, eight-scheme, and keyboard-modal matrix remains open in Issue #94. None of those environmental tiers is represented as passing here.

## 2026-09-10 checkpoint

Work resumed after ADR-0048 replaced the mutation-tier runner. The sections above are retained unchanged as the record of the stopped checkpoint; this section records what changed after it.

### Rebase

`feat/101-command-palette` was rebased onto main `f25cfb2b`. Two files conflicted:

- `docs/adr/README.md`: main added the ADR-0048 entry and the branch added the ADR-0047 entry at the same list position. Both entries are kept in descending number order.
- `docs/PROJECT_STATE.md`: main's `f25cfb2b` version is taken whole and the branch's stopped-checkpoint edit is dropped, because main now records the Issue #101 pause, the ADR-0048 re-baseline, and the #103/#107/#109/#111/#114/#116 closures canonically. The branch no longer modifies that file.

`dotnet restore NeNeCommander.slnx -p:Configuration=Release --locked-mode` passed after the rebase with the committed lock files; no lock file needed regeneration.

### Truthful mutation results

All three runs below used the repository `stryker-config.json` with `test-runner: vstest` and no threshold, runner, or exclusion change, from the layer's own test directory:

```powershell
dotnet stryker --config-file ../../stryker-config.json --project <layer>.csproj --break-at <threshold> --output ../../artifacts/security/mutation/<layer> --skip-version-check
```

| Measurement | Presentation.WinUI | Application |
|---|---|---|
| Stopped checkpoint `358c294`, isolating runner (ADR-0048 diagnostics) | 89.78% | not measured |
| Rebased head, before this checkpoint's tests | 92.23%; 660 killed, 17 timed out, 50 survived, 7 without coverage | 95.62%; 911 killed, 5 timed out, 41 survived, 1 without coverage |
| Rebased head, after this checkpoint's tests | 94.14%; 673 killed, 18 timed out, 36 survived, 7 without coverage | 95.72%; 912 killed, 5 timed out, 40 survived, 1 without coverage |

The MTP-era 80.98% and 83.99% results recorded above are not comparable to these numbers and are not evidence, as ADR-0048 states.

### Tests added and mutants killed

`tests/NeNeCommander.Presentation.WinUI.Tests/KeyboardIntentMapperTests.cs`:

- `MapWhenPaletteReceivesAnUnstartedEnterRepeatConsumesItWithoutExecuting` — a held Enter the palette did not start is consumed instead of executing the selected command. Kills `Input/KeyboardIntentMapper.cs:222`.
- `MapWhenOwnedContextKeyRepeatsOnlyModalEnterIsConsumed` — only a repeated modal Enter is consumed; a held Enter still reaches a text editor and a held Escape still cancels. Kills `Input/KeyboardIntentMapper.cs:205`.
- `MapWhenAddressEntryKeyFollowsGCancelsTheChord` — an address-entry key cancels a pending `g` chord even when the key itself is passed through. Kills `Input/KeyboardIntentMapper.cs:157`.
- `ConstructKeyboardMappingWhenAnyPartIsNullThrowsArgumentNullException` — the mapper, its clock, its input, its palette binding key, and the mapped intent reject absence. Kills `Input/KeyboardIntentMapper.cs:91`, `Input/KeyboardIntentMapper.cs:127`, `Input/CommandPaletteKeyBinding.cs:10`, and `Input/MappedKeyboardIntent.cs:11`.

`tests/NeNeCommander.Presentation.WinUI.Tests/CommandPalettePresenterTests.cs`:

- `UpdateQueryWhenProjectionIsEmptyStillRejectsAnAbsentQueryAsync` — an absent query is rejected by the view state itself, not incidentally by the row predicate. Kills `Commands/CommandPaletteViewState.cs:49`.
- `PresentWhenCandidateHasNoCanonicalFileListShortcutThrowsAsync` — a candidate the canonical file-list key map does not declare fails loudly instead of projecting an absent shortcut. Kills `Commands/CommandPalettePresenter.cs:33`.
- `ConstructPaletteProjectionWhenAnyPartIsNullThrowsAsync` — key hints, rows, and view states reject each absent part. Kills `Commands/CommandPaletteKeyHint.cs:10`, `Commands/CommandPaletteKeyHint.cs:11`, `Commands/CommandPaletteRow.cs:22`, `Commands/CommandPaletteViewState.cs:19`, and `Commands/CommandPaletteViewState.cs:20`.

`tests/NeNeCommander.Application.Tests/CommanderSessionTests.cs`: the existing palette-capture test now also asserts that `NavigateParent` is available when the captured active location has a parent. Kills `Commands/CommandCatalog.cs:71`.

No production code changed in this checkpoint.

### Survivors retained as equivalent

- `Commands/CommandPaletteKeyHintPresenter.cs:33` and its message string at `:36`: `CommandPaletteKeyAction` is a closed record hierarchy with a private constructor and five instances, and all five are handled, so the trailing `throw` is unreachable and no test can distinguish the mutant.
- `Commands/CommandPalettePresenter.cs:85` and its message string at `:88`: `CommandUnavailableReason` is closed at six instances with a private constructor, and all six are handled, so the trailing `throw` is unreachable for the same reason.
- `Panes/KeyHintPresenter.cs:33` and `:46`: every intent this presenter projects is declared by the context it projects, so `FirstOrDefault` never returns absence and the empty projection of an unlisted context produces the same empty result under the mutant.

The remaining Presentation and Application survivors are pre-existing on main, unchanged by Issue #101, and above their thresholds.
