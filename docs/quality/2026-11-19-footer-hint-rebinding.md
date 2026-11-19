# 同じフッターキー案内の再bind削減 — 2026-11-19

Status: implementation and selected proof complete; integration tracked in the handoff.

Issue [#467](https://github.com/hideyukiMORI/NeNeCommander/issues/467) follows the bounded
[#465 observation](2026-11-19-f6-xaml-observation.md). It does not close #453 or establish
the cause of hide's reported pause. Public beta.2 and the unaccepted PR402 are separate.

## Invariant and implementation

`DualPanePresenter` continues to obtain hints from `KeyHintPresenter` and its canonical keyboard
mapping. It returns the prior immutable hint list when values and order are equal.
`OperationBarView` assigns `ItemsSource` only when that reference changes. Existing pane-row
projection reuse is the model; no global/mutable cache, alternate key table or execution route
is added. The two product files and two directly related test files are the complete code change.

Hint text/order, automation identity, shown/hidden preference, F1, input/confirmation guidance,
status/progress/results, resource styling and provider/safety paths keep their existing contracts.
The hint list is still calculated for comparison; this change does not claim zero allocation.
Color changes update the existing brushes through `ColorSchemeResources.CopySchemeInPlace`,
so retained elements continue to follow the selected scheme.

## Selected proof

ADR-0061 ordinary Presentation/App change: affected presentation tests, view ownership, App build,
changed-file formatting, complexity comparison and bounded native observations. Application,
provider, safety, dependency, schema and XAML inputs are unchanged; no full/deep CI, coverage or
mutation run is required for this scope. Rules ARC-001/004/012, CMD-003/009, KBD-005 and
QLT-005/009/015 apply. No new ADR or waiver.

- Red: three selected identity assertions fail on the unchanged implementation, test exit2.
  They exercise pane navigation/selection/activation, name-entry transitions and progress updates.
- Green: 46 selected Presentation cases pass, including existing deletion/conflict/transfer and
  automation cases. Shown/hidden/restored content and same-content modal updates are covered.
- ViewCompositionOwnershipTests: 18 pass. App, Presentation test and Architecture test builds:
  zero warnings/errors, exit0. All three locked restores passed.
- Four changed C# files: `dotnet format ... --verify-no-changes` exit0, no changes. The format
  command emitted a workspace-load warning; this is distinct from the successful Release builds.
- Complexity Inspect exit1 contains the same26 findings as accepted #454, with identical
  path/metric/value/limit tuples and no added finding. This is not a clean complexity result.
- Successful evidence is retained with input hashes and is reused for review/merge while those
  inputs remain unchanged. Ordinary commit policy/security/whitespace hooks remain enabled.

Commands and full arguments/timestamps/exits are retained in
`D:/NeNeCommander/evidence/footer-rebind-20261119/{red,green}/commands.json`.
The executed driver is `pwsh -NoProfile -File D:/NeNeCommander/evidence/footer-rebind-20261119/verify.ps1 -Stage Red`
and then the same command with `-Stage Green`. The driver runs locked Release restores, then
`dotnet build <App|Presentation tests> -c Release --no-restore -m:1 -nr:false`, the Architecture
test build with `-p:BuildProjectReferences=false`, and `dotnet test --project <test project>
-c Release --no-build --no-restore -- --filter <selected names> --report-trx --results-directory <evidence>`.

The green Presentation filter is `FooterKeyHintTests|KeyHintPresenterTests|DualPanePresenterTests|
DualPaneProgressPresenterTests|DualPaneNameEntryPresenterTests|DualPaneDeletionPresenterTests|
DualPaneConflictPresenterTests|DualPaneTransferPresenterTests|PaneListingReuseTests|
DeletionConfirmationAutomationTests`, each prefixed with `FullyQualifiedName~` in the actual command.
The Architecture filter is `FullyQualifiedName~ViewCompositionOwnershipTests`.
The other commands, run from the task worktree, are:

```powershell
dotnet format NeNeCommander.slnx --include src/NeNeCommander.App/Views/OperationBarView.cs src/NeNeCommander.Presentation.WinUI/Panes/DualPanePresenter.cs tests/NeNeCommander.Presentation.WinUI.Tests/FooterKeyHintTests.cs tests/NeNeCommander.Presentation.WinUI.Tests/DualPaneProgressPresenterTests.cs --verify-no-changes --no-restore --verbosity normal
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

Their exits are0 and1 respectively, with the qualification above.

## Native behavior and bounded comparison

The predeclared comparison uses the same worktree and Release IL build configuration from main
`fbcb6b2`, before/after only this patch. It does not compare a published ReadyToRun build with IL.
Windows is10.0.26300.0; WinUI DLL SHA256 remains
`BA728EC8E18E86577581AC611B7379AD08F69D47898F0ADA0F1536F4313A9758`.
Each variant has one owned app, one unrecorded warmup, and two fresh SQL106B/TXT105B pairs.
The initial2291×1234-pixel window and Monokai/shown settings are the same. Navigation,
two-item selection and F6 use the keyboard. Normal PID38360 is preserved.

Each capture is target-PID-only XAML level4, QPC, 64×64KiB buffers, 64MiB cap and45-second limit.
Capture begins after selection and stops explicitly after the visible move completes. All four
start/enable/stop calls return0, post-stop query4201, and all event/buffer loss counters0.
The established raw-QPC reader and analyzer validate PID, descriptors and complete pairs.
No other key is sent during a capture. The event's KeyCode field is not independent F6 proof.

| Observation | PID | XAML events | All matched intervals | KeyDown ms | Frame count / max ms | ApplyTemplate count / inclusive sum ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| control-01 | 42792 | 4770 | 1359 | 3.3237 | 116 / 36.2998 | 689 / 28.6929 |
| control-02 | 42792 | 4779 | 1360 | 3.8693 | 117 / 35.7001 | 689 / 32.3749 |
| changed-01 | 33456 | 3519 | 875 | 1.2813 | 114 / 14.4743 | 357 / 4.4592 |
| changed-02 | 33456 | 3519 | 875 | 1.0908 | 114 / 14.4317 | 357 / 4.5949 |

Task statistics include intervals starting in the predeclared two seconds after the sole KeyDown
start, not the complete capture duration. Frames over16.67ms are3/4/0/0 respectively; that value
is a descriptive threshold, not a measured display-refresh contract. ApplyTemplate counts are
framework intervals, not proven new-object counts. Inclusive/nested phase times must not be added.
The complete per-task results are in `comparison-all.json`.

These observations support reduced framework work in this fixture. Two ordered runs per variant
do not establish a percentile, causal attribution of every interval, cold-cache behavior, CPU time,
total key-to-completion/display latency, provider/safety/refresh durations or capture overhead.
Background contention/JIT/cache differences remain. #453 stays open; no extra timing repetitions
were added after the predeclared four captures.

Both controls and changed-01 plus both warmups have source0/target2 and both expected hashes.
changed-02 visibly completed with source0/target2; a subsequent F8 check recycled its owned SQL
file before the final hash pass. Its remaining TXT hash matches, but a post-move SQL hash is not
claimed for that run. The later action occurred after trace stop and does not alter the trace.
No user file or path outside the six owned fixture roots was modified by these file operations.

Shown→hidden retains F1; hidden→name entry shows Enter/Esc; Escape returns to F1; enabling hints
restores their full order. Monokai→NeNe Light→Monokai visibly updates retained hint elements.
The status/result remains independent of the preference. This walk covers Japanese at the recorded
size, not additional scale, English, high contrast, Narrator or physical held-key conditions.
Permanent-deletion confirmation was covered by selected presentation tests, not this native walk.

## Setup attempts and limits

- Baseline warmup selection initially confused active-focus styling with selection. The extra Space
  was corrected before moving, with passive-pane selection marks inspected. No dropped input is claimed.
- Immediate AX/screenshot observations can lag input and often report an obsolete address focus.
  One settings AX read was unavailable; reselecting the observed window recovered it. Early files
  named `hints-hidden` still show the preceding state and are not accepted as transition proof;
  `hints-hidden-settled`, `name-entry-hidden`, `light-hints` and `restored-hints` own the settled proof.
  The initial Tab/Space attempt did not establish a toggle; later targeted clicks and Space did.
- F8 was mistakenly chosen to seek a confirmation on a Windows fixed-drive fixture. Existing
  ADR-0063 policy uses recycle there, so one disposable SQL file was recycled immediately. The
  deletion result is preserved in `f8-outcome.txt`; no permanent-confirmation claim or retry follows.
- Read-only source lookups with guessed paths failed and were corrected using actual paths.
  A recorder path substitution was caught and corrected before capture. No failed capture is hidden.
- GitHub connector writes had returned403; authenticated `gh` was the available Issue/PR write path.
  No credential value was read or recorded. This is distinct from automatic approval rejection.

## Evidence and environment restoration

All logs, command records, red/green TRX, input/binary hashes, comparison plan/analyzer, native state
and screenshots are under `D:/NeNeCommander/evidence/footer-rebind-20261119/`.
`runs/` preserves all73 output files /6,592,842 bytes with size/SHA256 equality in
`preservation-manifest.json`; raw traces are local only. The missing recycled file is explicit above.

Capture commands use `capture-xaml.ps1 -TargetId 42792|33456 -RunDirectory <run>/capture` in that
evidence root; the shared `f6-latency-20261119/read-raw-trace.ps1 -TraceFile <capture>/xaml.etl`
and `analyze-raw-intervals.py <capture>` return0 for all four. The final command is
`python D:/NeNeCommander/evidence/footer-rebind-20261119/summarize-comparison.py
D:/NeNeCommander/outputs/footer-rebind-20261119 control-01 control-02 changed-01 changed-02`, exit0.

Both owned apps exited normally; traces ended; normal PID38360 and Chrome remain. UI settings were
restored semantically, then the original218 bytes restored after owned-app exit, with SHA256
`ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.
No normal binary or public release was rebuilt/replaced. Integration and cleanup are recorded in the
[progress report](../reports/2026-11-19-footer-rebind-progress-report.md) and
[handoff](../handoffs/2026-11-19-footer-rebind-handoff.md).
