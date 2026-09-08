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

Independent review confirmed that the current report includes survivors whose exact expected values are asserted by the reported covering test set, including entries with all 106 tests in `CoveredBy` and none in `KilledBy`. This makes the numeric survivor total an unreliable proxy for missing contracts. A Stryker 4.16 / Microsoft.Testing.Platform activation issue is suspected, especially for the large increase in survivors in previously passing static Presentation code, but has not been proved. Do not describe every survivor as equivalent, false, or explained.

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
