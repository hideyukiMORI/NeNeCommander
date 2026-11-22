# Compact Locations picker — Issue #548

Date: 2026-11-22
Status: local XAML implementation, geometry and attached API-scroll proof; native input/screen acceptance and publication pending.
Base: main7ef9b9654bf26a74d2c673c6ff1d1d79195e65b2.

## Change and invariant

Only CommanderWindow.xaml and DesignTokens.xaml change product behavior.
The existing LocationsModal is capped at520, with24 outer inset and Top placement.
The row's unbounded Auto name column becomes2-star against the detail's1-star;
both texts wrap. Key hints reuse the existing KeyHintWrapPanel. The named list,
status/error surfaces, automation content and complete full-window scrim remain.
The [design handoff](../design/2026-11-22-compact-locations-handoff.md) records the
consultation and why wrapping differs from its suggested one-line ellipsis.

The canonical LocationsView/LocationsSession/catalog/navigation and qualified
keyboard/pointer-intent mechanism remain unchanged. No C#, resource string,
provider, settings, package, focus owner or keyboard mapping changes. Source
inspection confirms the same surface Tapped handler consumes surface taps, the
same surrounding overlay handler qualifies cancel, and RenderShown keeps its
SelectedItem/ScrollIntoView calls. This is not a native pointer/input success claim.

## Evidence and attempts

Task: D:/NeNeCommander/worktrees/issue-548.
Evidence: D:/NeNeCommander/evidence/compact-locations-20261122.
Fixture/output: D:/NeNeCommander/outputs/compact-locations-20261122/layout-probe.

The fixture uses the pinned App SDK, actual XAML/resources and unchanged production
KeyHintWrapPanel source. It substitutes literal names/details and canonical J/K/
Enter/Escape label resources into the actual templates. English/Japanese are
shipped resources; Chinese is synthetic stress text, not a claim that #528 ships.
Each result includes the exact view hash, process ID and environmental limits.
No user directory, settings, catalog, input injection or user App is used.

- Baseline24 cases: three language sets, drive/long WSL identifiers, widths1040,
  506,272,128. The accepted-main view hash matches the recorded baseline. Long WSL
  rows expand to496 at272/128 and their detail trims even at506. Drive detail also
  trims at272/128. Hints exceed narrow slots. Four baseline outer surfaces occupy
  the complete client width.
- Final24 row/hint cases fit each slot, preserve exact complete names/details and
  report no text trimming. Hints wrap in whole units. At128, long rows reach147–196
  effective pixels in height; this is a conscious tradeoff, not uniform-row proof.
- Four final outer cases: clients1280/640 yield centered520, and clients320/200
  yield272/152 with24 side/top inset. These isolated surface checks use an empty
  fixed-height child; they do not prove the full product.
- The first launch after editing used an assumed net8 executable path. It was
  rejected before launch; the enumerated net10 target was then used. The product
  was not re-edited for this invocation correction.
- Additional composition fixture: actual modal markup, loading text and12 literal
  rows, three language sets, widths640/320 and heights800/300. This intentionally
  pressures layout; it is not an actual LocationsLoading state transition.
  The windowless version serialized all12 cases then faulted at exit with
  Microsoft.UI.Xaml.dll/0xc0000005. Queuing Exit at low dispatcher priority produced
  the same fault. Both raw results, original fixture variants, process exits and
  scoped Windows fault records are retained; neither is accepted as a passing run.
- An unshown, unactivated owned Window allowed normal process exit0. All12
  composition measurements keep surface/hints within the24 inset, a positive
  ListView viewport and the existing ScrollViewer. This only establishes measured
  regions and estimated scroll extent. Despite assigning Window.Content, every
  parent.XamlRoot remained null and only the first row was realized. The earlier
  host-exit metadata's intended XamlRoot description is corrected explicitly in
  fixture-provenance.json; no actual attachment is claimed.
- A final targeted variant selected the last row and called the production-used
  ScrollIntoView plus UpdateLayout. It exits0, but last-row realization is false
  and VerticalOffset stays0 in all12 cases. This fixture cannot establish native
  scrolling. It is retained as an environmental limit, not counted as success.
  No visible window or input was introduced to work around that limit.
- App locked Release restore/build passes with0 warnings/errors. layout-comparison
  checks24 row/hint and4 outer cases plus the12 limited region cases. Final1079
  tracked non-Markdown inputs and fixture source hashes are frozen; all original
  fixture variants remain. Product inputs did not change during fixture trials.

## Attached-tree follow-up at19:31 JST

Issue #551's independent hidden-window measurement established that dispatcher
progress can attach XamlRoot without showing or activating a Window. This is a
specific improvement to the earlier unavailable-host fixture, not a retry of an
unchanged test or a workaround using desktop input. The old failure/limited records
above remain historical evidence and are not relabelled as successes.

A separate `attached-probe` directory preserves the original fixture unchanged.
It uses the same product XAML, resources and KeyHintWrapPanel, the pinned package
lock and PerMonitorV2 manifest. The dispatcher advances between attaching content,
resizing the owned client, selecting/ScrollIntoView of the last row and returning
by the same API to the first row. No Show, Activate, keyboard or pointer API runs.
The diagnostic sampling timer is not product code or a deterministic test clock.

`attached-probe-restore.log` and `attached-probe-build.log` record successful locked
Release restore and0-warning/error build. `full-attached-scroll-exit.json` records
process24152/exit0. The actual unshown native client sizes match640/320 by800/300
effective pixels at125%, across English/Japanese/synthetic Chinese: the same12
composition conditions as before, now attached. All12 acquire XamlRoot; the last
row is realized and entirely inside the list viewport, and return to the first row
realizes it at top0/offset0. All modal and hint regions remain within the24 inset.
Nine cases require nonzero scrolling. Narrow/short Japanese and synthetic-Chinese
cases retain91.2 viewport height with an81.6-high last row. Numeric float comparison
uses0.02 effective pixels, explicitly recorded rather than integer rounding.

`full-attached-scroll-layout.json` preserves all first/last/return observations.
`attached-scroll-comparison.json` proves12/12, plus all1079 original product-input
hashes unchanged. `attached-fixture-source/` and `attached-fixture-hashes.json`
preserve this new fixture. Both activation count and ever-visible flag are0/false;
all sampled foreground values match the initial handle. These observations do not
claim native key/focus behavior. Product App build and prior passing geometry are
reused because their inputs did not change; no product build/test or CI repeated.

Commands from `attached-probe` are the same locked restore and no-restore build
below. The exact executable under its net10/win-x64 output was launched with
`Start-Process -WindowStyle Hidden -PassThru` and `WaitForExit(20000)`, with no JSON
argument. The new fixture has a fixed task-owned output filename. No timeout fired.
The launcher and comparison results, not the diagnostic's timer alone, establish
this bounded run. No target process or setting outside this fixture was modified.
## Exact commands and scope

From the task worktree:

```powershell
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore -maxcpucount:4
```

The task-owned fixture starts with the reused locked package inputs:

```powershell
dotnet restore LayoutProbe.csproj --locked-mode -p:Configuration=Release
dotnet build LayoutProbe.csproj -c Release --no-restore -maxcpucount:4
```

Each changed fixture uses the same no-restore build. Its exact net10/win-x64
LayoutProbe.exe is launched with Start-Process -WindowStyle Hidden, a single JSON
filename and bounded20-second wait. Output arguments in order: baseline-layout.json,
changed-layout.json, full-layout.json, full-queued-exit-layout.json,
full-hidden-host-layout.json, full-scroll-layout.json. Repeated composition cases
are attempts at the same12 conditions, not additional distinct proof. The standard
Commit hook is pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit; source/XML and
diff checks are limited to this change. No full CI, coverage, mutation, CS-013 or
unrelated behavior suites run for two XAML files. Unchanged canonical behavior
proof is reused under ADR-0061, without claiming it verifies new layout behavior.

## Acceptance and retention

Real keyboard/pointer/focus return, scrim hit testing, visible full-window interaction,
other DPI/text scales and Narrator remain
unproved. A present, isolated native session is needed for those direct boundaries;
do not mark this issue complete or imply screen acceptance from detached geometry.
The row-height tradeoff is recorded for that review. No beta.3 change is made.

#542's push was rejected by automatic approval for lack of destination/payload-
specific approval; its question remains pending. #525 and #548 stay local and are
not sent as another route around that rejection. No PR, integration or cleanup is
claimed. Retain all three unintegrated task worktrees/outputs and evidence, along
with user App/issue495, unfinished491/514 and pr498-review. Branches/commits stay.
The eventual closing PR and completion record own integration and exact cleanup.
