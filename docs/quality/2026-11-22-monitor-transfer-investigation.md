# Monitor transfer: investigation, implementation and verification

Issue #551 / 2026-11-22 / main input `7ef9b96`.
Current status November24: local implementation and selected pure/isolated native proof complete under accepted technical ADR0092. Physical/visible acceptance, source publication and main integration remain pending. Dated sections below preserve the earlier investigation and failed trials.

Evidence: `D:/NeNeCommander/evidence/monitor-transfer-20261122/`.
Diagnostic output: `D:/NeNeCommander/outputs/monitor-transfer-20261122/probe/`.

## Isolation and provenance

A dedicated WinUI process created exactly one unshown frameless Window, with a Grid
as content, and moved only its own AppWindow. It never called Show, Activate or an
input API, and never opened the product, settings or user data. GetDpiForWindow and
IsWindowVisible read only its own HWND; GetForegroundWindow observed the foreground
without changing it. No monitor configuration or user window was changed. Those
native imports exist only in the external diagnostic source, not in the repository.

Both runs exited0. Activation count and ever-visible flag were0/false. All sampled
visibility facts and Changed-event observations were false; all foreground handles
remained the initially observed one. This proves those observations, not native
keyboard routing or user focus behavior. A 25-second launcher bound could stop only
the owned diagnostic process; neither run hit it.

The fixture copied the pinned SDK, unchanged package lock and PerMonitorV2 manifest.
It used the same pinned Windows App SDK dependency as main. The baseline and final
source snapshots, builds, JSON, provenance and hashes are retained separately.
The diagnostic uses a 120 ms DispatcherQueueTimer to observe after-return behavior;
these are empirical measurements, not deterministic product tests or a product timer.

## Results and all trials

| Evidence | Result |
|---|---|
| `probe-restore.log` | Locked Release restore succeeded. |
| `probe-build.log`, `run.json`, `hidden-transfer.json` | Initial build0 warnings/errors; process39740 exited0. Four monitors, all12 directed pairs ×3 API variants =36 observations. Native geometry/DPI at return matched the later sample in every case. |
| `probe-fit-build-first.log` | Intermediate fit-fixture build succeeded0 warnings/errors. Not executed; cases were narrowed and an oversized case added before collection. |
| `probe-fit-build.log`, `run-fit.json`, `hidden-transfer-fit.json` | Final build0 warnings/errors; process19120 exited0. Existing absolute MoveAndResize path, all12 pairs ×2 sizes =24 observations. All24 final rectangles fit the requested work area; all24 matched the immediate post-correction geometry. |
| `verification-summary.json` | XAML scale was stale immediately after20/24 first moves and matched native DPI at the later sample in24/24. Both runs observed no activation/visibility/foreground change. |
| `baseline-source/`, `fit-source/`, `product-input-hashes.json`, `evidence-hashes.json` | Exact diagnostic variants and relevant unmodified product inputs retained. |

The four observed work areas were (-3840,0,3840,2160) at150%,
(3840,0,3840,2160) at175%, (3818,-1440,2560,1440) at150%,
and (0,0,3840,2100) at125%. These are observed environment facts, not constants.
A900×600 rectangle moved from125% to175% became1260×840 before API return.
Absolute and display-relative MoveAndResize and Move behaved alike for the baseline.

The oversized primary-to-upper transfer initially became5264×2828 at175%, with
DisplayArea identifying the adjacent right-hand monitor. The intended upper monitor
is150%. The fixture's second fit reached2560×1440 at(3818,-1440), native144DPI,
while XAML still reported1.25 until the dispatcher advanced. This counterexample
prevents claiming the first result always belongs to the requested monitor.

The baseline JSON's human-readable limitation string incorrectly said `No XamlRoot`.
Its actual before/immediate/settled fields all record `xamlRootExists=true` after
the dispatcher ran. The final fixture records actual RasterizationScale. The raw
baseline remains unchanged and the correction is explicit in verification-summary.
This does not retroactively turn either run into rendered-screen proof.

## Exact diagnostic commands

From the dedicated probe directory:

```powershell
dotnet restore MonitorProbe.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build MonitorProbe.csproj -c Release --no-restore -maxcpucount:4
```

The latter was run for the initial, intermediate fit and final fit source variants;
all returned0. Each runtime launch used `Start-Process -WindowStyle Hidden -PassThru`
for the exact probe executable, followed by `WaitForExit(25000)`; run.json and
run-fit.json record PID/exit/time. The final executable was
`probe/bin/Release/net10.0-windows10.0.26100.0/win-x64/MonitorProbe.exe` beneath the
output root. No product build/test, coverage, mutation, CodeQL or CI was run for
this documentation change. Normal commit-hook results belong to commit.log.

## Limits and next proof

This is an unshown diagnostic window, not CommanderWindow or the complete product
route. No rendered layout, native key/focus, visible maximize/restore, presenter
change, disconnect/reconnect, rapid-repeat or settings/operation preservation was
exercised. The observed immediate resize is not an API timing guarantee. The
fixture's arithmetic is not the accepted application algorithm. The design record
keeps two mechanisms unresolved; an accepted ADR and targeted product/native proof
are still required. Retain unfinished output and the local branch; do not clean it
as a completed feature or infer integration from this evidence.

## November24 continuation: display-scale API and compensated landing

hide's current milestone-continuation instruction supersedes the old stop. This
section remains investigation/design evidence only: no production change, accepted
monitor-transfer ADR, final visual acceptance, public source push or release.
The retained issue551 worktree starts clean at eabae9e; all11 old investigation
input hashes match. Current release-base f6c722c now also has ADR-0089 placement
tracking/startup fitting, which a future implementation must preserve. No base
integration or production edit has occurred in this continuation.

Background Claude consultation completes16:50:59/exit0. The2851-byte brief contains
only public Issue551 requirements and authored generic UX choices, not repository
source, private layout or diagnostic contents. It runs in the dedicated empty D
consult directory with safe mode, no tools, empty strict MCP config, no settings
sources, no session persistence and one turn. The installed CLI's /design command
was already observed unsupported in this session, so this is text advice rather
than a generated design artifact. The earlier held repository-source payload was
not retried. Exact brief/advice/invocation/hash are retained in the new evidence.

The advice recommends n/p within Ctrl+W for one-shot next/previous geometric cycling,
left-to-right then top-to-bottom order, logical-size preservation, existing helper
hints and no new mouse entry. It prefers maximized transfer and relative landing,
while explicitly allowing refusal when reliability cannot be proved. These are
recommendations, not accepted behavior. The existing proposal uses centered landing
and normal-only admission; engineering still must select and record that contract.

Official API inspection distinguishes Microsoft.Graphics.Display.DisplayInformation
(which exposes display/color information but no scale property in the inspected
public surface) from Windows.Graphics.Display.DisplayInformationInterop. The latter
has GetForMonitor/GetForWindow wrappers through the existing Windows SDK projection.
Microsoft documents GetForMonitor as Windows11 build22621 or newer, with scale based
on GetScaleFactorForMonitor. This is no proof of custom-scale precision or Windows11
build22000 availability. Sources: [display class](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.graphics.display.displayinformation?view=windows-app-sdk-1.8),
[C# wrappers](https://learn.microsoft.com/windows/apps/desktop/modernize/winrt-com-interop-csharp),
[monitor contract](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.display.interop/nf-windows-graphics-display-interop-idisplayinformationstaticsinterop-getformonitor).

New scale-probe copies the existing pinned package lock, SDK and PerMonitorV2 manifest
into D:/NeNeCommander/outputs/monitor-transfer-20261124/scale-probe. Locked restore
and Release build both succeed, zero warnings/errors. At16:53 owned process11320
exits0, no timeout, activations0, visible samples0 and unchanged foreground/settings.
GetForMonitor reports1.5/1.75/1.5/1.25 for the four attached displays and agrees with
the own-window native DPI after each move. GetForWindow fails all8 immediate/settled
observations with E_NOTIMPL (-2147467263); this is retained and excluded as an API
choice. Immediate XAML scale is stale in all4 tested transitions and settles later.
The diagnostic-only native imports never enter product source.

A separate transfer-probe-01 evaluates a new two-rectangle candidate against all12
ordered display pairs at normal and oversized starting dimensions,24conditions.
It precomputes a centered final rectangle by converting source physical dimensions
through observed source/target scale and fitting each dimension to the target work
area. The first request's dimensions compensate for that scale ratio; the second
request applies the already-decided final physical rectangle. This is candidate
arithmetic, not an accepted timing contract or an adapter-owned product decision.
Locked restore/build succeed with0warnings/errors. At16:59 process25288 exits0,
no timeout/activation/visibility/foreground/settings change. All24 final rectangles
exactly equal the plan, fit the requested work area and have the intended native
DPI. The first request alone is exact23/24; all24 already select the intended display.
The prior primary-to-upper oversized neighbouring-display counterexample is avoided
in this bounded candidate. Its earlier failure evidence remains unchanged.

The first console summary used Select-Object directly on an OrderedDictionary and
printed null fields. The saved JSON was correct; rereading it as a structured object
confirms24/24. No runtime repeat occurred for this reporting correction. Read-only
source discovery also records absent guessed route/ADR paths and truncated combined
reads; exact files were then located. Remaining normative reading must be completed
before any production edit; truncated text is not treated as read.

Exact diagnostic commands (inside each dedicated probe):

```powershell
dotnet restore MonitorProbe.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build MonitorProbe.csproj -c Release --no-restore
```

Each exact bin/Release/net10.0-windows10.0.26100.0/win-x64/MonitorProbe.exe is launched
with Start-Process -WindowStyle Hidden -PassThru and bounded WaitForExit(20000) for
scale-probe or25000 for transfer-probe-01. Only its returned process could be ended
on timeout; neither timed out. Commands/results/source/managed-DLL hashes and normal
settings hash are in scale-api-run-01 and transfer-compensated-run-01. Raw API errors,
plans, before/immediate/final/settled samples and independent arithmetic summary are
retained. Normal settings SHA256 is561932A6530A6317499BAB59AD0735C8469869A15C6E6DA9BE3507B37AF557CE.

Next decisions before implementation: minimum size versus work-area fit, first-stage
minimum and rounding, destination identity/topology revalidation, unsupported OS/API
failure, custom scaling, maximized/minimized/hidden admission, partial effect/failure
wording and exact ownership during reentrant/stopping events. A successful hidden
probe does not establish visible/native-key/pane preservation or general OS timing.
Use the existing WindowAdjustment planner/route/adapter, keep geometry policy in
Application, and do not introduce a timer, retry loop or new window-operation path.
Reserve the next truly free ADR number (0085 is now occupied;0091 is used by local531).
No product test, full gate, mutation or CI is run for this documentation/diagnostic
work. The new evidence/output and existing worktree are unfinished and retained.

## Current-base connection and minimum-size API proof - November24 17:27 JST

The existing docs-only branch e76a81c is connected to accepted release base f6c722c
as8f34e7c. Only PROJECT_STATE conflicts; both dated records are retained. All product,
test, dependency and gate inputs in the staged merge match f6c722c exactly. Normal
Commit conformance112/security24/message checks pass. No product build/test is run
for that input-identical connection, and main/release branches remain unchanged.

Proposed ADR0092 selects the existing adjustment route, n/p with Ctrl+Right/Left
aliases, deterministic geometric cycling, normal/visible admission, centered reported-
logical-size preservation and two precomputed rectangles. It explicitly includes
owner/topology/state revalidation and partial apply receipts. It remains proposed,
with an independent read-only mechanism review pending and no production edits.
GetForMonitor's documented22621 minimum refuses only transfer on earlier product-
supported OS; ordinary adjustment/startup does not read that API. The source/SDK
boundary and recorded custom-scale limits remain part of review.

A newly isolated minimum-probe-01 resolves an actual open contract: whether an SDK
preferred minimum changes with per-monitor DPI. It copies only the five pinned
project/XAML/manifest/SDK/lock inputs from scale-probe and adds its own App source.
Locked restore and Release build succeed with0warnings/errors. Four conditions are
125%->175% and175%->125%, each with undeclared and500x400declared minima. The window
is never shown/activated, and no keyboard/mouse event is generated.

At17:26:50 owned process44796 exits0 without timeout. All20samples remain nonvisible,
activations0, foreground unchanged, normal settings hash and write-time unchanged.
The declared minimum reads500x400before/after both transfers. A300x200request is
clamped to500x400at either source scale. Without a declared minimum, a600x450transfer
becomes840x630from125%to175% and429x321in reverse. With minimum500x400, the reverse
request is clamped to500x400rather than429x321. Thus the existing ADR0050 physical-
minimum interpretation is supported by these cases; only the undeclared32-DIP floor
uses the relevant reported scale. This is not a custom-DPI/visible/general timing
proof. Do not multiply a declared minimum by the display scale.

Exact build commands in D:/NeNeCommander/outputs/monitor-transfer-20261124/minimum-probe-01:

```powershell
dotnet restore MonitorProbe.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build MonitorProbe.csproj -c Release --no-restore
```

The resulting bin/Release/net10.0-windows10.0.26100.0/win-x64/MonitorProbe.exe was
launched with Start-Process -WindowStyle Hidden -PassThru, its returned process alone
bounded by WaitForExit(20000). No timeout termination was needed. minimum-api-run-01
contains exact source/binary hashes and settings evidence; minimum-api-observation-01
contains every requested-stage/result/minimum/DPI sample. Both raw and source remain.
Official preferred-minimum property pages supply no unit/transition guarantee beyond
the property, so the above specific observations are reported separately rather than
inferred from that documentation.

Read-only continuation retains absent guessed filenames, one invalid rg shell glob,
a sandbox-denied process query later performed through explicit read permission,
and an initial blank OrderedDictionary console projection whose saved JSON was
correct. No failed product trial is concealed or retried into success. All unfinished
worktrees/outputs remain; no publication, accepted ADR, product implementation or
completion cleanup is claimed.

Independent read-only mechanism review17:26:38 found three mandatory details:
1 qualify reentrant Escape from the still-rendered incoming owner and propagate
stop before FinishTracking, preventing outer Forward repaint/focus after close;
2 first-changing-call entry is the no-effect boundary; final observed receipt also
requires owner/lifetime/visibility/presenter/target facts; 3 exact physical/undeclared
minimum formulas and revalidation. Proposed revision02 now specifies all three.
The first replacement script failed its exact-paragraph guard because a PowerShell
nested-array expression combined the replacement pair; no file was written and the
original ADR hash remained D35899A4...FF0. Explicit paragraph variables corrected the
script, without changing a product input or executing another native trial.
No production edit, accepted design or new runtime trial is inferred.

## Technical mechanism acceptance - November24 17:35 JST

Independent re-review at17:34:27 verifies proposed revision02 hash00765DEDD9558A5E10C9BEDEE4F6843F2E91500349CD340BDF2E9882DE94CDCA and finds no mandatory correction. The delegated engineering owner accepts ADR0092 for scoped implementation. This is neither product/native/visual acceptance nor publication/release authority. The required owner/stop/uncertain-effect/minimum details are explicit. The handoff preserves the existing helper; each new canonical group displays its letter and control-arrow cap, which existing hint-derived help can also show. Production inputs remain equal to f6c722c. The acceptance script's first exact-alias replacement failed after writing only the ADR status; it did not alter product, run checks or commit. Exact handoff text was inspected and corrected before this atomic documentation commit. All raw trials and review revisions remain.

## Application implementation begins - November24 17:52 JST

Technical contract commit6a7315c passes normal Commit conformance112/security24 and
message checks. Production work then adds owned WindowDisplayIdentity/Display/
Snapshot, closed transfer input/effect, two-rectangle geometry/plan and the existing
window outcome's qualified transfer observation. WindowDisplaySnapshot privately
owns its list and exposes only IReadOnlyList/enumerators, without ICollection.SyncRoot.
The canonical planner alone admits PreviousDisplay/NextDisplay, checks consistent
work areas, groups duplicate areas using the snapshot's identity order, cycles in
left/top/width/height order and calls pure sizing. Both rectangles respect minimum
and finite/narrowing/end-coordinate boundaries. No OS transfer or new key is wired yet.

WindowAdjustmentRequest adds explicit transfer facts. Existing27calls are migrated
to NotRequested, including the host's current ordinary route and tests; no compatibility
overload is added. Planned decisions carry their exact new owner. ReportTransfer
updates only that current owner when its prior outcome is a planned display action;
ordinary/refused/stale/repeated reports cannot change it. CommanderSession forwards
to that same owner. This new logic still needs its dedicated behavioral proof.

Locked Release restore succeeds. application-build-01 fails5mandatory style findings
(IDE0048two, IDE0305one, IDE0046two); failed source4files are archived before scoped
style correction. application-build-02 compiles Application but fails one preexisting
constructor null-guard fixture now missing the new owner argument. That fixture uses
the existing reflection assertion for both missing plan/owner, and closed-case tests
include the new actions/refusals/plan/outcome. application-build-03 succeeds with
zero warnings/errors. The filtered existing WindowAdjustment Application suite then
passes75/75with0skip at17:51:37. This proves existing compatibility only, not completed
transfer behavior, adapter, keyboard, coverage or native acceptance.

Exact commands:

```powershell
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore
dotnet format style src/NeNeCommander.Application/NeNeCommander.Application.csproj --no-restore --include <four exact archived files> --diagnostics IDE0046 IDE0305 IDE0048
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~WindowAdjustment
```

The four formatter inputs are WindowTransferSizing.cs, WindowTransferPlanner.cs,
WindowAdjustmentPlanner.cs and WindowDisplay.cs under Application/Windowing. Logs,
ordinary-call-migration-01.json and failed-source archive preserve exact trials.
Current implementation is uncommitted atop6a7315c; no whole-gate/product/feature
acceptance is claimed. Next: dedicated model/order/sizing/receipt tests, pure apply
qualification, mapper/resources/hints, guarded host application/stop and isolated
actual-control proof. Do not rerun these75unchanged cases merely for a new phase.
No external write, cleanup or new user approval occurred.

## Implementation and focused proof - November24 18:20 JST

Application adds pure WindowTransferQualification: CanStart compares complete source
identity/topology/scale, exact bounds and declared minima; CanFinish requires the
intended current target and unchanged minima, and HasObservedResult also requires
exact final bounds. XAML scale is deliberately excluded. Shared area consistency
lives in the owned snapshot. Available/receipt constructors now reject absent parts;
receipt creation rejects ordinary actions. New model, permutation/duplicate cycle,
scale/round/minimum/overflow, qualification and owner/session tests pass with the
existing window cases:120passed/0failed/0skipped. application-window-coverage-01.xml
reports branch-rate1 for every WindowAdjustment/Display/Transfer class. This is
focused class proof, not whole-Application or a full-gate claim.

Application build04 fails CA1062 on final-result null checks; the source is archived
as qualification-build04.cs and explicit validation fixes it. Build05 fails redundant
nongeneric enumeration cast plus3unused using directives; all4test sources are saved
in application-build05-tests. The test now invokes the nongeneric enumerator directly.
Build06 succeeds0warnings/errors, followed by application-transfer-tests-01/coverage.
Complexity01 has exactly the existing5findings; no new Application finding. A later
change adds closed input/effect case enumeration to the old model test and still
needs that changed test compiled/executed.

Presentation declares produced n, previous/next actions and2hint groups with p/n and
Ctrl+Left/Right caps. The existing repeat guard retains accepted transfer presses
across input-owner changes; ordinary repeats resume on the next initial event.
Planned, four refusals, not-applied, uncertain and observed outcomes have distinct
labels/tones.16resources are added consistently to each of3catalogs. Not-applied
wording states cancellation before start without guessing a reason; handoff wording
is aligned. KEYBOARD_MODEL reflects the same table. The old310-line keyboard test's
exact binding declaration moves into WindowAdjustmentBindingTests before expansion.
No new mapper/dispatch mechanism, input injection or UI execution is inferred.

Presentation build01 succeeds; build02 fails CA1861 for2test arrays (source archived
keyboard-build02.cs); named readonly arrays fix it and build03 succeeds. First scoped
run71has69pass/2fail: old key-count48and oldcapindices are stale after n/fourbindings.
The original test is saved keyboard-test01.cs; expected49keys/18bindings/11actions/
8hintgroups plus transferred fixture are updated. Build04 succeeds0warnings/errors;
second scoped run72/72passes0skip (presentation-transfer-tests-02), with window,
translator/help/repeat filtering and coverage02. Wider shared keyboard boundaries
and final coverage interpretation remain to be checked; no whole-Presentation claim.

App adds guarded transfer-only SDK display reads (22621capability, no new import),
qualified two-call apply and effect classification setUnconfirmed BEFOREentering
first MoveAndResize, plus route receipt/reentrant geometry guard and narrow leave
bridge. Stop invalidates route/view before FinishTracking. Initialapprestore succeeds.
Appbuild01 fails IDE0046and missing SupportedOSPlatform declarations; adapter-build01
is saved. Appbuild02 fixes OS attribute but a remaining simplifiable if fails; saved
adapter-build02. Appbuild03 succeeds0warnings/errors. Changed-file whitespace and
scopedIDE0046formatter attempts are logged; no dependency, gate or threshold changes.
Conformance01passes112rules.

Independent read-only review begins on current uncommitted implementation. Its first
causal finding is that property-change callbacks can Stop during RenderOpen/Closed,
after which later visibility/focus setters still ran. The prior source is saved as
view-before-review01.cs. Rendering now checks stopping between text/automation updates,
both visibility changes and focus/announcement, and between the two collapse setters.
This correction still requires final build/complexity plus actual callback proof.
No native trial, feature acceptance, publication, main merge, release or cleanup has
occurred in this implementation segment. Allwork remains local/incomplete atop6a7315c.

Exact scoped test commands are in application-transfer-tests-01/presentation-transfer-
tests-01/02logs. The tests use dotnet test --project <layerTests> -c Release --no-build
--no-restore --filter <WindowTransfer|WindowDisplay|WindowAdjustment for Application;
WindowTransfer|WindowAdjustment|KeyboardInputTranslator|KeyboardHelp|Repeat for
Presentation> -- --coverage --coverage-settings eng/coverage.settings
--coverage-output-format cobertura --coverage-output <distinct evidence xml>.

Independent review02 also identifies reentrant Leave/reopen during property callbacks:
non-stopping alone cannot qualify an old render. The first corrected view is archived
view-before-review02.cs. Open render now retains exact current AND rendered owner at
each boundary; closed render requires the same closed current owner and no rendered
replacement, so a reopened scope cannot be collapsed by its caller. The final
review has this one required finding pending re-review/native proof; no other required
pure/keyboard correction. The malformed n Markdown row from escaped script text was
corrected using literal text; p/n/+ rows read back correctly. Additional shared-input
boundary tests68pass/0fail/0skip (keyboard-boundaries-01), distinct from prior72cases.

## Native product-route proof and causal rendering checks - November24 18:43 JST

Product App build04 succeeds with0warnings/errors after exact current/rendered-owner
render qualification. Independent read-only re-review03 finds no required correction;
view SHA25659CAE469A9506F9E497A6455D24A7E7EA002084CA31069C7FC3A8C635969FE21.
Complexity02 identified2new findings in the new presenter TEST's nested expectations.
The prior test is archived; six explicit data cases replace nested conditionals.
Complexity03 now has only4unchanged prior findings (2scope-owner parameter counts,
help nesting and virtual-key translator41lines). The old310-line keyboard test is
below its ceiling after moving the exact-table specification. No threshold changed.

D output product-controls-01 copies172App inputs with matching hashes at copy. Only
composition root/private settings/fake ports and project references differ;170remain
exact, plus the added diagnostic partial files. The4Domain/Application/Infrastructure/
Presentation DLLs match the product. All137XBF/PRI files match. The first displayed
summary's equal counter used a literal-property comparison and printed0incorrectly;
raw pairs correctly had0differences. view-baseline-restoration-01 corrects the count
to137/137without rerunning product behavior. This diagnostic reuses the previous
external fixture's compiler properties; product build04 remains the strict build.

The new launcher creates NeNeCommander551Controls_20261124 without SWITCHDESKTOP
rights and never sends global input. Its startup/runtime guard checks this is not
the input desktop. Each attempt uses a fresh private settings file, fake30-row
readers, a throwing file-operation port and a throwing Shell launcher. Native geometry
and property callbacks run on the real owned window; calls are API/mapper driven,
not physical keyboard or visible acceptance. Every run records normal-settings hash,
mtime and length unchanged, input desktop Default unchanged, no timeout, and normal exit.

Current successful en-US attempts:1standard56checks (8next/previous transfers across
attached125/150/175% monitors, exact immediate bounds/typedObserved, focus/rows/selection
and no transfer write, small500x400fit);2leave7;3nested8;4minimum6;5stop7;6view-stop-title3;
7view-stop-sink3;8view-leave-title2;9view-reopen-collapse3. All corresponding JSON raw
observations are retained. Native Changed callbacks really occurred inside transfer
in2-5; minimum change yieldsUnconfirmed, exact incoming Escape leaves, nested geometry
and value-equal staleLeave cannot replace owner, normal close stops further forward.
No SDK-call-count claim is inferred from Changed event counts.

Causal counterexamples: only diagnostic WindowAdjustmentView is replaced with saved
view-before-review01 (not the product tree). Baseline build02 succeeds. Attempts10
view-stop-title and11view-leave-title both exit2with the expected failed assertion:
later view/focus mutation afterStop and stale reopening afterLeave. Their failure
JSON/launcher receipts remain. Restore the current exact source, build03 succeeds;
the restored managed App DLL A94B3961...970E4B equals the original passing diagnostic
binary byte-for-byte. There is no reason to repeat unchanged passing scenarios.

No publication/merge/release/cleanup or normal-settings write is claimed. Remaining:
three-locale long/refusal text and minimum layout across attached scales; more actual
visibility/collapse callbacks as needed, native p/n delivery if warranted, final
source/resource/input reconciliation, changed formatting/conformance/record closure,
and complete quality/state/handoff records. Physical Ctrl/IME/hold, unplug/reconnect,
custom scale, speech and hide's screen acceptance remain environmental proof.

## Responsive helper verification — November24 19:46 checkpoint

The later focus fix moves the nonvisual ContentControl out of the scrolling body
into the fixed footer Grid; the actual first focus and each page stop retain that
sink. App scroll build06 and diagnostic build12 pass. English attempt18 at150%
passes352checks, but exported top/bottom images expose text under the expanded
native scrollbar. XAML reserves the SDK ScrollBarSize plus ScrollViewerScrollBarMargin
in a separate column; product build07 and diagnostic13 pass. No new visual constant.

Read-only review at19:27 identifies sole LayoutUpdated reset dependence and requests
proof of intermediate body readability, not just the last row. Diagnostic build14
adds identical-result owners and collection of full body TextBlocks observed at page
stops. Attempt19 fails because the diagnostic includes the scrollbar's own glyphs;
build15 narrows inspection to actual content/footer. English20passes373checks,
including identical-result reset, with incidental layout; no causal claim about
missing-notification independence is made. English21 at175% fails147checks into
unconfirmed outcome: the h/j/k/l Move row straddles full-viewport page stops.

WindowAdjustmentView now uses one qualified TryResetScroll from completed rendering,
LayoutUpdated and page-action layout, checking exact current/rendered owner and
nonstopping lifetime afterwards. Product08passes. Page step becomes half a viewport
without a new geometry action. Product09 and diagnostic16pass0warnings/errors.
English22 at175% passes683checks; images show clear text/bar separation. Independent
read-only review at19:38has no required correction for that View hash84F54E2E...2376C7D.
Final matrix/parity/physical/visual acceptance were not inferred by that review.

Matrix01 English23/150% passes393checks, then English24/125% fails4checks because
an unused zero-area native ScrollBar remains Visibility=Visible. That has no actual
occluding area. Diagnostic17 corrects the overlap predicate; product unchanged.
Matrix02 English26/125%passes373 and English25/150%passes393, then Japanese2/150%
fails16checks: the final Restore row remains below the scrollable extent. Diagnostic18
adds panel metrics; Japanese7reproduces. The panel is measured217.333328DIP wide and
89.333336high but lays out an extra row. Diagnostic19 temporarily instruments only
the copied KeyHintWrapPanel to record Measure/Arrange constraints and child desired
sizes, so this copy is explicitly not source-parity-ready. Product panel unchanged.

Changed-format04 fails only51ENDOFLINE diagnostics in WindowAdjustmentHelpScrollTests.
Normalize that one file toCRLF; exact equality modulo line endings is recorded,
format05passes. Reuse semantic test proof. Complexity05 exits1 with exactly the same
4findings as04, including every field; View158logical lines. No clean-complexity
claim or unrelated tests. Corrected stale keyboard fixture tests05passed2; the other
58selected cases from04remain successful without changes. Record-closure01was already
successful141abstractrecords/130closedroots and relevant model inputs are unchanged.

All named-desktop launches retain normal settings/hash/mtime and input desktop;
no SwitchDesktop or SendInput. Each raw launcher/result JSON/log/source trial stays
preserved. Matrix01/02remain failed; no final three-locale completion is claimed.

## Final responsive helper and source reconciliation — November24 20:00–20:57 JST

The instrumented diagnostic identifies the actual Japanese clipping cause: the
shared wrap panel measures the hint groups at one width and arranges them at a
narrower width, adding a row beyond its desired height. The measured89.6667DIP
versus arranged113.3333DIP example is retained. Only this helper's seven geometry
hint groups now use standard StackPanel; the shared KeyHintWrapPanel is unchanged.
Title/full outcome stay in the bounded body, generated Esc and paging hints plus
the existing nonvisual focus sink stay in the fixed footer, and the SDK scrollbar
gutter prevents text overlap. Half-viewport paging clamps within0..ScrollableHeight.
No domain operation, second input route, fixed visual constant, timer or product
retry is added. Owner reset/stop/leave qualification remains the single path in
ADR0092 and the accepted engineering handoff.

Independent read-only review at19:53 finds no required correction for the scoped
StackPanel change and clamped page offset. The diagnostic's temporarily instrumented
wrap panel is restored to exact product bytes before final parity. Product
app-scroll-build-10 and diagnostic controls-scroll-build-23 succeed with0warnings/
errors. controls-parity-summary-03 confirms170App source files,137XBF/PRI resources
and4layer DLLs match; only private composition/project and diagnostic partials differ.

Japanese9/10 retain failures where the observer assumed ChangeView completed after
100ms; attempt9 still observed offset20.3333 after PageUp. Clamping is retained as
an API-domain guard, but is not falsely credited with making asynchronous completion
synchronous. Diagnostic23 awaits the observed target within a fixed20-observation
bound, without reissuing the command. Japanese11 then passes623checks with at most
one extra observation tick. The wrong-directory diagnostic build21 failsMSB1009
and remains saved; corrected22 and final23 builds succeed. No product failure is
hidden by these diagnostic corrections.

Final matrix03 reuses Japanese11 and adds en27–30,ja12–14,zh2–5 on the four attached
displays:150%,125%,150%,175%, each500x400physical at current system text scale.
Each locale/display checks13outcomes plus an explicit same-text/new-owner update.
All body TextBlocks become fully readable at overlapping page stops, every hint
row is reachable, PageUp returns to0, Esc/pager remain inside the client, and focus,
owner, pane data, settings and input desktop remain qualified. This totals156distinct
locale/display/outcome cases plus12same-outcome updates and7556layout checks.
Japanese15 and Chinese6 add the real owned-window transfer route,56checks each;
final aggregate7668checks/0failures. The earlier English standard/stop/reentrant
cases remain separately recorded and are not counted again in this aggregate.

All owned launches exit normally and leave normal settings/hash/mtime and input
desktop unchanged. Top/bottom PNGs are saved; final Japanese14/Chinese5 images show
clear footer/last rows and no text under the scrollbar. These are isolated actual
controls driven by API/mapper, not physical keyboard, IME, hold, Narrator, human
screen acceptance, custom DPI, system text scaling, smaller client fit or display
unplug/reconnect proof. Those environmental cases remain explicit limits.

Exact final native commands (exit0):

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/monitor-transfer-20261124/run-product-controls-01.ps1 -Locale ja-JP -Attempt 11 -Scenario scroll-layout-0
pwsh -NoProfile -File D:/NeNeCommander/evidence/monitor-transfer-20261124/run-scroll-matrix-03.ps1
```

The matrix script records its13additional exact locale/attempt/scenario calls;
scroll-matrix-03-complete.json and all launcher/result receipts remain. The first
aggregate serialized correct raw rows but OrderedDictionary projection produced
null totals. The saved original is preserved as scroll-matrix-before-aggregate-
correction.json; totals were recomputed from deserialized rows without rerunning
any test. Final summary and every raw receipt hash reconcile.

Final format05 passes after recorded CRLF-only normalization of the new scroll test;
view-only format06 exits0 with the existing workspace-load warning preserved.
Complexity06 exits1 with exactly4unchanged prior findings (all fields), no new
finding, View159logical lines. Do not call this a clean complexity run. Conformance
scroll02 passes112rules. The selected scroll tests retain58passes from04 and2corrected
stale fixture passes from05; the failed04 remains evidence. Earlier Application120
and3model cases, Presentation72 and68shared-boundary cases retain their recorded
coverage/reuse scope; overlapping later selections are not summed as distinct tests.

The final Presentation assembly adds two private sealed help-page cases after the
first closure proof, so only the affected structural proof is refreshed:

```powershell
pwsh -NoProfile -File ./eng/verify-record-closure.ps1 -RepositoryRoot D:/NeNeCommander/worktrees/issue-551
```

record-closure-final-02 exits0 with141abstract records/130closed visible roots/0failures.
No full gate, mutation, CI or successful unrelated behavior test is repeated.
proof-inputs-02 freezes1268inputs; final-check02 and the20:55 pre-documentation check
both match every hash. The change map retains15changed inputs since the earlier
snapshot and4newly listed paths: one new test plus three unchanged tool/hook inputs
that the older selection omitted. This is not a claim that those hooks changed.

The release priority/pause interrupted documentation after the19:46 checkpoint;
this section now preserves the remaining trials and final results. beta.4 was
published separately from3909039 and contains none of this feature. Local save,
physical/visible acceptance and a separately authorized feature publication remain
the next steps; unfinished worktree/output and all raw evidence are retained.
