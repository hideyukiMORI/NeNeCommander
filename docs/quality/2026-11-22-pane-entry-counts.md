# Pane entry counts — Issue #542

Date: 2026-11-22
Status: implemented and locally verified; closing PR/completion owns integration and cleanup.

## Invariant and canonical projection

Base: accepted main `7ef9b9654bf26a74d2c673c6ff1d1d79195e65b2` after PR556.
Each pane describes the immutable listing whose rows are displayed. The existing
`PaneListingPresenter` creates internal `PaneCounts`/`PaneEntryCounts` through
`PaneProjectionSource`; `PanePresentation` carries them to `PaneCountsLine`.
The App renders its localized text and identical tooltip beneath the existing list.
There is no new directory read, recursive enumeration, operation, input route,
settings field, package, public state contract or provider decision.

Visible files/folders count `VisibleEntries`; loaded counts include all admitted
entries. Explicit selection is independent of the focused row. Existing provider
`DirectoryEntryKind` has exactly File and Directory; links retain the provider's
classification, and there is no unknown-kind case to guess. Size/metadata absence
never changes a kind count. Loaded counts are not whole-directory totals when
`Bounded` or unrepresentable entries are reported. The localized loaded-only
qualifier makes that limit explicit. No listing is unavailable, distinct from a
successfully loaded empty listing with zero counts. Retained content during a
pending, failed, cancelled or abandoned read is labelled as the last listing;
the existing header owns the actual activity/error explanation. Successful new
content replaces the counts even when its entry count equals the old count.

Focus/activation and explicit selection changes reuse immutable kind counts.
A new listing or rebuilt visible projection is counted from its already-owned
entries. The selection count does not promote native ListView focus to selection.
No filesystem I/O occurs in Presentation (ARC-001/004/012, CS-007/009/018/023).

## Design and localization

hide requested background Claude /design consultation when design is needed.
The requested /design invocation returned that this CLI command only manages
project access, and produced no design. An ordinary read-only design consultation
then returned advice from the authored brief alone. Both invocations/results and
the complete advice are retained; no repository files were sent and no access
permission or design project was changed. No claim is made that /design supplied
a completed mockup.

The advice's per-pane bottom placement and semantic typography/text resources
are applied. Engineering uses full wrapping text instead of lossy single-line
narrow variants: the qualifier, both count bases and selection remain available,
with the same text in the tooltip/default TextBlock accessible content. Label-first
English avoids numeric plural branches; Japanese follows the existing vocabulary.
Selection zero is deliberately explicit. Existing number formatting is invariant,
with grouped decimal counts. There are five complete templates/state resources
per shipped en-US and ja-JP catalog. Synthetic Chinese placeholder reordering is
proved without claiming a Chinese catalog or locale selector is shipped (#528).
There is no interactive control, new tab stop or per-key live announcement.

The two `TextBlock`s have stable LeftPaneCounts/RightPaneCounts automation IDs,
Wrap and no fixed dimensions/trimming/line limit. Native pixel-width, DPI/high-
contrast screenshots and live screen-reader behavior remain unperformed; the
structural check does not claim those environmental results. See the corresponding
section of DESIGN_HANDOFF. No shared-desktop input or user App/settings changes.

## Trials and evidence

Permanent evidence: `D:/NeNeCommander/evidence/pane-counts-20261122`.
Task worktree: `D:/NeNeCommander/worktrees/issue-542`.

- Initial test code used incorrectly escaped Windows path literals: build failed
  with four CS1009 diagnostics. The original two files and log are retained.
- Correcting those literals exposed four stale two-argument test calls to the
  source factory. They were updated atomically; the count types/property were
  limited to internal visibility. The corrected test build has zero warnings/errors.
- Two new test classes provide 18 cases. The first selected run contains those
  and 28 existing cases, all 46 pass. Some guessed class names in that initial
  filter match no class; the remaining six actual impacted classes were selected
  explicitly in a disjoint 33-case run. All79 distinct cases pass, zero skips.
  No unchanged successful case was rerun merely to enlarge the total.
- Cases cover file/folder classification, absent/empty, hidden/displayed counts,
  focus/activation reuse, selection-only changes, equal-length kind replacement,
  independent panes, null guards, grouped 10,000-entry boundary, bounded/omitted
  reads, pending navigation, failure/cancellation, abandonment and late completion.
  Existing incremental rows, marks, status, visible entries, resources and dual-pane
  projection remain proved. These are deterministic presentation tests, not native
  keyboard, performance latency or directory mutation tests.
- Locked App restore and final App build succeed with zero warnings/errors.
  Changed Presentation/tests/App whitespace checks all exit0. The App format
  invocation reports the existing workspace-load warning; it is retained.
- CS-013 Inspect exits1 with exactly the five existing findings from accepted
  #550 evidence. The locations, metrics and values match exactly; no changed
  file introduces a finding. No threshold or baseline was added.
- `verify-resources-layout.ps1` validates the five real resources in both catalogs,
  formatting for zero/hidden/partial/retained counts, Chinese placeholder order,
  both XAML surfaces and identical full tooltip source. Its JSON states the native
  visual and screen-reader limits explicitly.

## Exact commands and selected scope

Run from the dedicated worktree:

```powershell
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore -maxcpucount:4
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~PaneCountsTests|FullyQualifiedName~PaneCountsLineTests|FullyQualifiedName~PanePresentationTests|FullyQualifiedName~PaneListingPresenterTests|FullyQualifiedName~PaneListingPresenterFailureTests|FullyQualifiedName~PaneListingPresenterIncrementalTests|FullyQualifiedName~PaneListingPresenterSortingTests|FullyQualifiedName~DualPanePresenterTests|FullyQualifiedName~PaneStatusLineTests'
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore -maxcpucount:4
pwsh -NoProfile -File D:/NeNeCommander/evidence/pane-counts-20261122/run-remaining.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/pane-counts-20261122/verify-resources-layout.ps1
```

The retained runner and commands.jsonl contain all remaining test filters and
three exact changed-file format commands plus complexity inspection. The normal
Commit hook runs `pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`.
ADR-0061 scope is Presentation behavior and its App consumer. No Application,
provider, settings, key map or trust-boundary input changed; full CI, all-layer
coverage/mutation and release proof would not add necessary scoped evidence.
Successful proof is reused only while its relevant inputs match final-inputs.json.

## Integration and environmental limits

Review the final diff/evidence, normal Draft/Ready PR and exact-head squash;
verify accepted/merged tree equivalence and synchronize main. Preserve every
unique ignored artifact with per-file verification before removing only the
completed dedicated worktree. Keep branches, commits and permanent evidence.
The closing PR and completion.json state actual integration and cleanup.

Published beta.3 is unchanged. #523 remains an unreproduced reactivation report;
its existing low-priority activation restore and possible routed-input boundary
are recorded separately in the permanent investigation evidence. #453/#491 native
latency and AppSDK PR517 remain separate. This change neither establishes native
input isolation nor repeats the prohibited shared-desktop experiments.
