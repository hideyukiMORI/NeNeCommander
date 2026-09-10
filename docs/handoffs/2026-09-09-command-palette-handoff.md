# Handoff — command palette — 2026-09-09

Status: informational — stopped checkpoint; not merge-ready

## Scope and repository state

Issue #101 extends the existing command path so `Ctrl+P` can search and submit declared commands. The invariant is that the palette never owns command semantics or execution: `CommanderSession` with `CommandCatalog` owns the catalog, captured scope, availability, and qualified routing; `CommandPalettePresenter` with `CommandPaletteViewState` owns localized projection, filtering, and selection; and `KeyboardIntentMapper` remains the only non-text key map. ADR-0047 is the accepted decision.

Draft [PR #113](https://github.com/hideyukiMORI/NeNeCommander/pull/113) remains open and unmerged on `feat/101-command-palette`. The relevant commits have distinct meanings:

- main/base: `6c9abd8998ce55f06d768ea9471aeb6adcd4d84c`;
- exact head evaluated by failed deep-review [run `34256292455`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/34256292455): `eee23c4e7b8381b9cffb7863b03623c994665593`;
- immediately preceding passing Presentation mutation comparison: `43b60a9` in deep run `34239166946`.

This checkpoint records post-deep changes after evaluated head `eee23c4`. `src/NeNeCommander.App/Views/CommanderWindow.xaml.cs` contains the CodeQL alert #102 correction. `tests/NeNeCommander.Presentation.WinUI.Tests/CommandPalettePresenterTests.cs` contains four catalog/projection/availability/selection tests and completed key-hint assertions. These later changes have focused coverage and build evidence but lack new deep-review and CodeQL evidence. Resolve the checkpoint revision from the branch tip or PR #113; no self-referential hash is stored here.

## Implemented behavior

The pushed feature enumerates 15 existing commands in fixed order, captures both pane snapshots and the active side when the palette opens, calculates availability without I/O, projects localized labels, shortcuts, target context, unavailable reasons, and stable automation identities, and submits only a candidate qualified by its captured palette state. The WinUI host provides the accepted near-top overlay and restores focus on close. Keyboard handling preserves busy, modal, text-entry, IME, destructive-confirmation, and Enter-repeat precedence.

Cancellation retains the prior pane state. Executing an existing destructive intent still reaches the existing confirmation route. Bookmark management from Issue #99 and window management remain outside this branch.

## Verification ledger

| Evidence | Scope and result |
|---|---|
| `pwsh -NoProfile -File ./eng/verify-coverage.ps1` | PASS on the post-deep source and tests recorded here: Domain 71/71, Application 294/294, Infrastructure.Windows 222/222, Presentation.WinUI 106/106; branch coverage 100.00% / 100.00% / 92.81% / 94.17% |
| `dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj --configuration Release --no-restore` | PASS after the candidate CodeQL fix; 0 warnings, 0 errors |
| `pwsh -NoProfile -File ./eng/conformance.ps1` | Latest scoped evidence PASS; 112 unique normative rules |
| pushed-head commit hook | PASS; security/adversarial registry 19 cases and commit convention checks |
| dependency-review `34256050939` | PASS for pushed head `eee23c4` |
| deep review `34256292455` | FAIL for exact head `eee23c4`; Presentation.WinUI mutation 80.98% and CodeQL analysis `1742890023` produced [alert #102](https://github.com/hideyukiMORI/NeNeCommander/security/code-scanning/102) |
| authorized Presentation-only local recheck | FAIL on the post-deep Presentation tests at 83.99%; 614 successful and 117 failed outcomes among 731 mutants |

The failed CI deep run used the canonical command:

```powershell
pwsh -NoProfile -File ./eng/deep-review.ps1 -ReportPath ./artifacts/security/deep-review.json
```

Its downloaded artifact is retained at `C:\Users\info\Temp\NeNeCommander-101-deep-artifact-34256292455`; the summary is `C:\Users\info\Temp\NeNeCommander-101-deep-artifact-34256292455\deep-review.json`.

The local mutation report is:

```text
C:\Users\info\WORKS\NeNeCommander-101\artifacts\security\mutation\NeNeCommander.Presentation.WinUI-issue101-recheck\reports\mutation-report.json
```

It was produced by:

Working directory: `tests/NeNeCommander.Presentation.WinUI.Tests`

```powershell
dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Presentation.WinUI.csproj --break-at 90 --output C:\Users\info\WORKS\NeNeCommander-101\artifacts\security\mutation\NeNeCommander.Presentation.WinUI-issue101-recheck --skip-version-check
```

The command used the existing configuration and did not change the runner, exclusions, or threshold. It exited below the required 90% break point. The App fix has not been evaluated by a new CodeQL analysis; the prior analysis ID is `1742890023`.

## Mutation diagnosis boundary

The older passing comparator `34239166946` at `43b60a9` scored 90.81% across 566 Presentation mutants. Issue #101's deep run expanded that set to 731 and scored 80.98%; the local test additions raised the result to 83.99%.

Independent review confirmed that the current report includes survivors whose exact expected values are asserted by the reported covering test set, including entries with all 106 tests in `CoveredBy` and none in `KilledBy`. This makes the numeric survivor total an unreliable proxy for missing contracts. A Stryker 4.16 / Microsoft.Testing.Platform activation issue is suspected, especially for the large increase in survivors in previously passing static Presentation code, but has not been proved. Do not describe every survivor as equivalent, false, or explained. ADR-0048 later proved that suspicion and settled it.

Three small behavioral tests remain reasonable candidates if work resumes: the palette hint intent-label blank guard, `CommandPaletteViewState`'s null rows guard, and clearing a pending `g` chord when raw Address input changes context. Closed-switch refactors in `CommandLabelCatalog` and key-binding projection are also review candidates. These are unapproved and unimplemented; they cannot by themselves account for the 44 additional successful mutant outcomes needed to reach 90% from the local recheck.

## Required next decision and remaining proof

Stop with PR #113 in Draft. Before implementation resumes, decide whether to diagnose and correct the Presentation mutation execution mechanism or make a small independently justified production/test correction. Do not alter thresholds, suppressions, exclusions, or the canonical runner to obtain a pass.

After an approved correction, the next integration candidate still requires:

1. focused tests and coverage for any source or test changes made after this checkpoint; the recorded 693-test pass need not be repeated unless the changed impact justifies it;
2. CodeQL analysis that actually evaluates the `_addressPresentation` fix;
3. a successful exact-head deep review, including Presentation mutation at or above the existing threshold;
4. a Draft-to-Ready transition and successful canonical CI gate for the final merge candidate; and
5. squash merge through the protected PR path.

No deep retry, Ready transition, canonical gate, or merge was run after the failed local diagnostic. Issue #93 still owns live WSL proof. Issue #94 still owns native WinUI IME, Narrator/UIA, high contrast, DPI, narrow-window, eight-scheme, and keyboard-modal proof. Issue #99 remains the separate bookmark slice and must consume the command model only through an independently accepted change.

## 2026-09-10 checkpoint

The decision required above was taken by ADR-0048, which replaced the mutation runner rather than the thresholds. The sections above are retained unchanged; this section records what resumed work established. Everything above the heading describes the stopped checkpoint and no longer describes the branch.

### Rebase base and conflicts

The branch was rebased onto main `f25cfb2b`. `docs/adr/README.md` conflicted because main added the ADR-0048 entry where the branch added ADR-0047; both entries are kept in descending order. `docs/PROJECT_STATE.md` conflicted because main's docs-closure commit rewrote the same checkpoint block; main's version is taken whole, the branch's stopped-checkpoint edit is dropped, and the branch no longer modifies that file. No source or test file conflicted.

`dotnet restore NeNeCommander.slnx -p:Configuration=Release --locked-mode` passed with the committed lock files after the rebase. No lock file changed.

### Verification ledger for this checkpoint

| Evidence | Scope and result |
|---|---|
| `dotnet restore NeNeCommander.slnx -p:Configuration=Release --locked-mode` | PASS after rebase; no lock regeneration |
| `dotnet test tests/NeNeCommander.Presentation.WinUI.Tests -c Release` | PASS; 115/115 |
| `dotnet test tests/NeNeCommander.Application.Tests -c Release` | PASS; 296/296 |
| Presentation.WinUI mutation, isolating VSTest host, rebased head before this checkpoint's tests | 92.23%; 660 killed, 17 timed out, 50 survived, 7 without coverage |
| Presentation.WinUI mutation, isolating VSTest host, rebased head after this checkpoint's tests | PASS at 94.14%; 673 killed, 18 timed out, 36 survived, 7 without coverage |
| Application mutation, isolating VSTest host, rebased head before this checkpoint's tests | 95.62%; 911 killed, 5 timed out, 41 survived, 1 without coverage |
| Application mutation, isolating VSTest host, rebased head after this checkpoint's tests | PASS at 95.72%; 912 killed, 5 timed out, 40 survived, 1 without coverage |

Both mutation runs used the repository `stryker-config.json` unchanged. Thresholds, mutation level, runner, and exclusions were not altered. The truthful stopped-checkpoint comparison for Presentation is 89.78% at `358c294`, measured by the ADR-0048 diagnostics with the same isolating host; the MTP-era 80.98% and 83.99% figures recorded above are not comparable.

### Behavior added

Seven Presentation test methods and one Application assertion were added; no production code changed. They kill `KeyboardIntentMapper` lines 91, 127, 157, 205, and 222; `CommandPaletteKeyBinding` line 10; `MappedKeyboardIntent` line 11; `CommandPaletteKeyHint` lines 10 and 11; `CommandPaletteRow` line 22; `CommandPaletteViewState` lines 19, 20, and 49; `CommandPalettePresenter` line 33; and `CommandCatalog` line 71. The daily report lists each test with the contract it proves.

Four Presentation survivors in Issue #101 code are retained as equivalent: the unreachable `throw` and its message in `CommandPaletteKeyHintPresenter` (lines 33 and 36) and in `CommandPalettePresenter` (lines 85 and 88), because `CommandPaletteKeyAction` and `CommandUnavailableReason` are closed record hierarchies with private constructors whose members are all handled. `KeyHintPresenter` lines 33 and 46 are retained for the same closed-projection reason. All other survivors are pre-existing on main.

### Remaining proof

The three earlier candidate corrections listed above are now resolved: the chord-clearing contract and the view-state guards are tested, and the palette-hint blank guard is covered by the key-hint construction test. The exact-head dependency review, security deep review including CodeQL reanalysis of alert #102, and the canonical Ready gate for this candidate are recorded in PR #113. Issue #93, Issue #94, and Issue #99 remain as stated above.
