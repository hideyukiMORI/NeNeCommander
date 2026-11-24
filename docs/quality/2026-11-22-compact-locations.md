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

## Resumption and native diagnostic — November24

16:08 JST before integration all1079 previous inputs match. Base f6c722c merges into548 as4a57079 after2additive conflicts; exact changed product lines remain the same2XAML files. Normal App locked restore/build passes0warnings/errors. Diagnostic171Appfiles copied with private schema6settings and injected catalogs/reader/no-launch. Build01 fails only int-to-char Ctrl+Gdata; XAML193-error cascade retained. Corrected build02 passes0warnings/errors. Native en-US attempt1 records loading/catalog success then fails assumed empty-loading-list focus; pending fake outcomes prevent teardown, launcher timesout90s and terminates only PID28300. Normalsettings/inputdesktop unchanged. Exact diagnostic source/DLL archived. Pending correction records loadingfocus/geometry and retains settled/input/focus assertions; fake outcomes are released and normal shutdown used on failure. An initial CRLF-dependent guard insertion fails beforebuild; originalpartialfile retained, LF-normalized anchored insertion verified. No product fix or native success claimed.
16:10 JST corrected diagnostic build03 passes0warnings/errors. Native en-US2 exits2 normally in2.47s with unchanged normalsettings/inputdesktop: loading list has0height/506.4width and keeps file-list focus, while actual Tab is consumed and pane snapshots remain unchanged. This explains why an empty-loading-list focus assertion was over-specific, but settled focus remains unproved. The next failing assertion is wide modal centering at a0.1DIP tolerance; exact dimensions were previously appended only after successful checks. The next diagnostic adds pre-assert dimensions/focus to identify cause, with no changed assertion or product source.
16:14 JST native en-US3 preserves the centering failure and adds exact dimensions: overlay1188DIP, modal520 atx334.4 instead of arithmetic334; the0.4DIP delta equals half a physical pixel at125%, not a product alignment defect. The settled14-row list still leaves actual focus inLeftFileList. A narrowly staged baseline probe now uses exactly the two pre-width f6XAML inputs (verified against release proof hashes), with all focus/owner code unchanged, to determine causality before any product correction. Current compact source files are archived, all prior results retained.
16:16 JST pre-width baseline en-US4 reproduces the initial settled-focus defect exactly: loading list0height, settled list560.8height/1188width, actual focusLeftFileList. Exit2 normal, normalsettings/inputdesktop unchanged. This is an existing boundary limitation; no unrelated product repair starts. Baseline source/DLL retained. Restore the exact compact XAML and keep native focus observation as an explicit unresolved baseline item. Continued checks assert canonical Locations input ownership and selected row, not falsely claim list focus. Centering tolerance now precisely allows half a physical pixel plus0.02DIP floating tolerance (0.42DIP at125%); all inset/width/height bounds stay unchanged. No product source has changed since4a57079.
16:19 JST compact diagnostic build06 passes0warnings/errors. en-US attempt5 advances54checks through actual loading Tab, nativeDown/Up/j/k/Tab/Escape, left focus return, right activation, wide/medium/narrow modal/hint/status/hit-test bounds and13native row moves. It fails stage13 on complete last-row visibility in the narrow viewport; native selected index is correct. Exit2 normal in9s, normalsettings/inputdesktop unchanged. No overall pass and no product change. Next add row bounds/viewport/scroll offsets BEFORE assertions (layout already does so), then determine rounding, pending realization or actual clipping; do not weaken bounds or manufacture focus. Japanese/Chinese/full selection/error sequence remain unrun.

16:31 JST attempt6 adds pre-assert geometry and confirms last selected row bottom162.6286 versus viewport120.8000, offset1000.5714 versus max1042.3999. Attempt7 samples unchanged offset/extent across4ticks (3stable intervals) and still fails: not transient animation. Attempt8's diagnostic-only UpdateLayout before existing ScrollIntoView still fails by the same41.8286DIP; original and spike sources preserved. This attempted official recommendation is not adopted. Microsoft documentation: https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.listviewbase.scrollintoview .
Attempt9's diagnostic-only retention of rows for identical drive/WSL sections passes native last-row/first-row return and Enter/navigation/right-focus, then fails a separate diagnostic direct ScrollIntoView(last) before UIA invocation. This direct-jump result is preserved as a framework/API limit; production arrow moves do not use that diagnostic jump. Attempt10 prepares the UIA target through the unchanged native Down path instead; all122checks pass, including actual row IInvokeProvider->ItemClick, exact WSL target read once, partial/both failure/empty states and final/stopped focus guards. All attempts2-10 end normally, normal settings/input desktop unchanged. All new diagnostic builds07-11 pass0warnings/errors. No physical keyboard/mouse, visible acceptance or baseline initial-focus success claimed.
The product correction now stays in the sole LocationsView.RenderRows path: reuse the existing ItemsSource only when length, exact LocationItem references and all LocationRow values agree; choose selection from those retained rows by exact item identity. Changed text/items/order reopen invalidates reuse. No new scroll call, callback, focus rule, catalog/session/intent path is introduced. The diagnostic section-identity spike is replaced with this value/identity guard and requires final proof; its success is not claimed as final-product proof. This expands the original two-XAML scope by one App adapter source due to the reproduced width-induced keyboard scroll failure. App build/native cases/conformance/changed format/complexity are selected; unaffected pure layers and full gate are not rerun for this App-only display correction.

## Final product correction and native results — November24 16:40 JST

The final product version uses `HasSameRows` over the actual ListView source, requiring
matching count/order, exact underlying item references and complete row value equality.
It retains containers only for unchanged rendered rows and selects the retained row
by exact item reference. It introduces no extra ScrollIntoView call, delay, focus
policy or session/catalog/navigation path. Normal App build02 and diagnostic build12
pass with zero warnings/errors. Conformance112 and changed-file whitespace pass.
CS-013 exits1 with the exact prior five findings (all locations/values/limits match);
LocationsView has no finding. Independent read-only review requires no correction.

English attempt11 passes122checks/exit0 on final product code. Japanese attempt12
passes69checks before a row-frame boundary measurement fails by0.11439DIP: its text
bottom120.662 is already within viewport120.800. Chinese attempt13 fails because a
130.4DIP drive row is taller than the120.8 viewport. These failures remain retained.
Final diagnostic tolerance is half a physical pixel plus0.02DIP (0.42DIP at125%),
consistent with the already-measured layout rounding. It cannot mask the original
41.83DIP failure. A row taller than the viewport is explicitly a separate condition,
not counted as entirely visible: actual IScrollProvider.Scroll(SmallIncrement)
moves16DIP, giving top-16/bottom114.4 and an overlapping view of all text. Selected
row and canonical input owner remain unchanged, then SetScrollPercent returns top0.
Attempt14 proves this lower-edge access and native last-row navigation, but the
return-to-first assertion incorrectly still requires the taller row to fit at once.
That diagnostic inconsistency is corrected, with its original source/result retained.
Builds13/14 succeed. Final Chinese attempt15 passes128checks/exit0 and Japanese16
passes122checks/exit0. English11 is reused because production and its checked
scenario did not change; diagnostic binary hashes are kept separately per run.

The selected cases cover loading Tab ownership, native Down/Up/j/k/Tab/Escape,
left/right focus return, maximum/medium/narrow actual modal and hint geometry,
hit testing, native end-to-end row traversal and return, Enter navigation exactly
once, actual UIA row Invoke through ItemClick to the exact WSL item, independent
section failures, both failures, empty success, no empty Enter navigation and the
stopped-host guard. Native messages target only the owned child HWND/PID on a named
non-input desktop. Ctrl+G is canonical character/mapper data, not physical Ctrl.
Physical mouse/keyboard, visible repaint/acceptance, speech and other DPI/text scale
remain unproved. Initial list focus remains in the underlying file list in both the
exact pre-width baseline and this candidate; no fix or acceptance is claimed for it.
The separate direct-API end-jump failure remains; sequential product key traversal
passes. Very tall rows require vertical scrolling and are not promised to fit at once.

All normal settings hashes/write-times and the Default input desktop remain unchanged;
all final owned children exit normally. Source parity covers171 App files, with only
the declared composition/project substitutions. All137 XBF/PRI resources and4 common
product DLLs match. The normal product retains analyzers; only diagnostic composition
uses fake catalogs/readers/private settings. `final-product-parity-01.json`,
`final-native-summary-01.json` and `final-fixture-source-01` retain the provenance.
The final manifest has1249 non-Markdown tracked inputs:1246 comparable release
inputs plus3 already-existing release-document inputs newly included in this broader
inventory. Only3 product inputs differ from f6c722c (two XAML and LocationsView.cs);
`git diff f6c722c --name-only -- src docs/release` confirms no release-document edit.
This inventory difference is not an additional source change.

Exact commands (worktree issue548 unless otherwise stated):

```powershell
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore
pwsh -NoProfile -File eng/conformance.ps1
dotnet format whitespace src/NeNeCommander.App/NeNeCommander.App.csproj --no-restore --include src/NeNeCommander.App/Views/LocationsView.cs --verify-no-changes
pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect
# Each diagnostic revision builds only the dedicated copied App project:
# cwd D:/NeNeCommander/outputs/compact-locations-20261124/product-controls-01/App
dotnet build NeNeCommander.App.csproj -c Release --no-restore
pwsh -NoProfile -File D:/NeNeCommander/evidence/compact-locations-20261124/run-product-controls-01.ps1 -Locale en-US -Attempt 11
pwsh -NoProfile -File D:/NeNeCommander/evidence/compact-locations-20261124/run-product-controls-01.ps1 -Locale ja-JP -Attempt 16
pwsh -NoProfile -File D:/NeNeCommander/evidence/compact-locations-20261124/run-product-controls-01.ps1 -Locale zh-Hans -Attempt 15
```

Earlier attempts use the same launcher with their recorded locale/attempt/scenario;
each command receipt and failed diagnostic source is preserved. Read-only discovery
also records several invalid PowerShell rg wildcard paths and absent guessed evidence
filenames; subsequent exact discovered paths succeed, without source/process changes.
No unrelated product tests, full gate, mutation or new CI are run for this App adapter
correction. The chosen proof is normal build plus the directly affected real-control
boundary and lightweight conformance; unchanged pure behavior proof is reused.
The source remains local, with visible acceptance/initial-focus follow-up and
publication/integration pending. No cleanup is eligible for this unfinished task.
