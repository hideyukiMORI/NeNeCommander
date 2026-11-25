# Pointer entry investigation — 2026-11-24

Status: local Issue530 implementation and selected technical proof are complete,
with two unchanged-base Application test failures explicitly separated. Physical
input/caption interaction, visible acceptance, publication and integration remain pending.

## 11:16 final stopping receipt and source correspondence

The bounded context-counter probe06 builds without warnings/errors and, on the
pre-guard host, fails at stage20 on the specific unwanted context query. Its owned
process exits2 with a managed failure receipt; the normal input desktop is unchanged.
Probe07 recopies all175 current App inputs, preserves the declared diagnostic
substitutions and adds only the same context-query counter. Its build has0warnings/
errors. en-US attempt6 passes96 checks/14 transitions, including actual normal-stop
completion followed by zero context queries and zero focus requests. It exits0 in
3294.4752ms at125% scale with Default unchanged. This proves the latest early guard;
the earlier93-check ja-JP/zh-Hans geometry results remain applicable to their
unchanged resources/layout. It does not convert either probe into physical input
or visible-acceptance evidence.

Final-source-checks01 records1267 proof inputs,175/175 copied App source hashes
unchanged and exact product/fixture DLL parity for Domain, Application,
Infrastructure.Windows and Presentation.WinUI. Latest source window hash is
b0f4d1a97a5b5bf1585b4ea5b4a710a79200858b23198ff690434fbc01c27512;
the declared instrumented copy hash is
ed2a7a67e2ba57f6bb6211a0bab4a4ce4f12d9150d0bd0e24233afc7ae72b10f.
Conformance06 passes112 rules and complexity04 retains exactly the same four
existing findings. Whitespace03 finds only the new PanePointerBoundaryTests LF
line endings; diff-check01 finds one trailing empty line in PROJECT_STATE. Those
format-only issues are corrected, with a saved before/after hash and exact
newline-normalized text equality for the test file. No product source changes.
The final formatting receipt is recorded separately from the previous failed pass.
Format-repair01 confirms whitespace04 and diff-check02 both exit0. The test file's
newline-normalized contents are exactly identical, so no behavior test/build is
repeated for that formatting-only change. Final input manifest02 records its new
raw hash while retaining the original manifest and normalization evidence.

Presentation proof covers436 distinct successful cases:435 passed in consumer02,
and the corrected seven-case focus class includes six of those plus the one former
failure. These numbers must not be added as442 distinct cases. Application has
980 successful cases and the two explicitly documented base failures. Coverage
thresholds are established for the two affected libraries, not a claim that the
entire canonical gate or every test is green. No full CI/deep review is started.
Read-only independent final review reports no required source correction.

## 11:10 consumer coverage and three-language controls

`run-owning-consumers-01.ps1` applies only canonical whitespace formatting, builds
the two owning test projects and measures their coverage, then builds the App and
runs conformance/complexity. Application builds cleanly and returns975/977 with two
unchanged-base failures: CommanderSessionBookmarkOwnershipTests expects23 commands
where main already has24; NullGuardTests invokes UserSettings.Create with three
arguments after its signature gained the placement argument. The failing test files,
UserSettings and catalog have no worktree diff from mainaf17755. Neither is changed
to make this feature green. The earlier checkpoint's reference to InterfaceLanguageQueueTests
as the second base failure was inaccurate; the actual observed second failure is
NullGuardTests.

Application branch coverage initially99.6757458% identifies five uncovered branches
in the new pointer boundary/range paths. Five focused boundary cases now prove
direct PaneSession rejection during a read/launch, absent-content row rejection,
the reducer's absent intermediate focus fallback and rejection of foreign range
endpoint references. Consumer02 builds cleanly and returns980/982 with exactly the
same two unrelated failures and0skip; Application branch coverage reaches100%.
Its aggregate coverage is rerun because these previously uncovered branches required
new test inputs, not because of a new commit or reviewer.

Presentation consumer01 stops before execution on two void Assert.IsNotNull return
uses and two unassigned new ObserveResult results. These fixture errors are corrected
without production suppression. Conformance04 separately detects a boolean mode
test parameter; explicit listed/unlisted test methods replace it. Consumer02 builds
cleanly and runs435/436,0skip, with96.827495% Presentation branch coverage. The one
failure is the new Open receipt fixture's missing directory-read response. Providing
a pending successful read and proving it actually waits corrects the fixture;
`focus-tests-03` rebuilds with0warnings/errors and passes its7cases. The other435
successful cases and the unchanged production coverage evidence are reused.
Conformance05 passes112 normative rules.

Normal App build01 succeeds with0warnings/errors. Complexity03 finds only four
existing violations: scope-owner/snapshot constructors each5parameters, Help
presenter nesting4 and WindowAdjustmentKeyboardTests310logical lines. The previous
raw-key translator overflow is removed; no new violation is present. A final host
inspection moves the stopping/absent-side guard before GetKeyboardContext, so late
completion cannot query native focus while stopping. App build02 confirms this
latest source with0warnings/errors. Only that App change requires another build.

Control fixture04 builds cleanly. On the guarded non-input desktop, en-US attempt3,
ja-JP attempt1 and zh-Hans attempt1 each pass93 checks across14 transitions at125%
scale, finishing in2549.3972/2894.0439/2839.5736ms respectively. These are complete
fixture elapsed times, not input latency benchmarks. Each owned process exits0;
input desktop remains Default. Proof includes native F10 focus, forward/reverse
focus traversal, accepted header focus return, actual SelectionChanged reconciliation,
existing command ButtonAutomationPeer invocation, modal/busy rejection, qualified
directory opening, localized labels/automation metadata and geometry after800x600
and1200x800 pixel resize requests. Caption separation is geometric, not native
drag/minimize/maximize hit-test proof. No physical mouse/keyboard injection, speech,
hover/high-contrast visual acceptance or hide's screen acceptance is inferred.

The three-locale fixture copied the source before the final early stopping guard.
The independent read-only review accepts the latest mechanism with no required
source correction, and explicitly requires this distinction in the evidence.
A subsequent closing probe in fixture05 detaches the content root and ends in native
exit3221226107 without a managed failure receipt. That attempt is unestablished,
not successful stopping proof and not an attributed product defect. Fixture06
replaces root detachment with a bounded diagnostic context-query counter and an
explicit failure receipt; latest-stop proof and final source parity remain pending.
Both failed and successful diagnostic inputs/results are preserved.

## 10:57 actual controls and accepted-pointer focus

Routing09 adds five positive file/directory/transfer cases and passes all five with
zero-warning/error builds. Record closure02 passes145 abstract roots/134 visible
roots without failures. The initial full `dotnet format` command returned0 but
performed code fixes that removed ten XAML callbacks and dependent methods; that
result is rejected. The removed version is retained, the callbacks are restored
from the inspected original with intentional pointer/F10 changes preserved, and
the reviewer verifies the repaired hash83cbe365fc6b2108c94efbf4a8274ad052931c198faa99b05d064d4002a47158.
The canonical command is `dotnet format whitespace`; the current pass uses only
that command and changed C# files. No successful earlier App build is claimed for
the later callback repair.

The owned control fixture copies175 App source inputs and uses dedicated panes
and three-locale schema6 settings. Unshown mode omits native activation/focus.
Isolated mode checks its named desktop against OpenInputDesktop before native
activation/focus; launcher rights exclude SWITCHDESKTOP and no SendInput is used.
The main input desktop remains Default before and after both isolated attempts.
Diagnostics are excluded from production and their analyzer relaxation applies
only to this copied probe. Production builds retain every analyzer.

Fixture build01 fails on a diagnostic using directive after the namespace, causing
CS1529 and subsequent XAML cascades. Correcting the directive produces build02
with0warnings/errors. Unshown attempt1 passes23 label/geometry/hit-target checks,
then fails its assumed F10 context. Native-focus assertions are now reserved for
isolated mode, where a real pane focus is established. Isolated attempt1 proves
F10 reaches the first enabled native button, then exposes a diagnostic API error:
WinUI desktop TryMoveFocus requires loaded SearchRoot options. Fixture build03
fixes that probe call with0warnings/errors. Isolated attempt2 then proves forward
and reverse group cycling and the actual CommandEntry keyboard context, before
failing at stage2: direct accepted header handling changes the active pane but
leaves native focus in the command group. This direct API sequence does not prove
physical PointerPressed/Tapped ordering; that remains separate.

Following read-only technical review, admitted synchronous row/surface input now
returns an exact before/after snapshot receipt through PanePointerMapper. Header
and unlisted surfaces replace only their immutable wrapper while preserving
content/activity/model identities. App renders, rechecks stopping/current input
ownership and then calls the existing synchronous FocusFileList helper. Stale,
modal, unadmitted and asynchronous Open results cannot request that focus return.
Only plain accepted selection seeds double tap. ADR0090 and owning proof are
updated together; no second execution or focus queue is introduced.

The current consumer run selects all Application and Presentation tests with their
coverage because the shared PaneSession transition and input map affect every
consumer in those two layers. It also selects the normal App build, conformance
and complexity measurement. Domain/Infrastructure tests and all-layer mutation
are not added. Baseaf17755's two already-recorded Application fixture failures are
kept separate from this work and will not be silently repaired from the held
release payload. Results are pending, not yet claimed successful.

All exact scripts, attempts and logs are in the permanent task evidence directory.
Additional read-only command errors (guessed script names, reads before receipts
exist) are retained in the session record; corrected bounded reads identify the
actual names. No source publication, release action, normal settings mutation,
shared-desktop input, physical interaction or visible acceptance is claimed.


## 10:22 routing and command-entry continuation

Routing04 passes3operation-freeze cases and14pointer mapping/receipt cases. App
pointer build01 completes with0warnings/errors. The previous closure01 result
fails on the public intermediate PaneInteraction root; inherited closure alone is
not recognized by the required compiler proof. The root now explicitly retains
its inaccessible abstract override, with no runtime method call or suppression.
Closure02 is running against the built assemblies.

Routing05 stops at CA1062 on a typed dynamic-data argument. A null guard allows06
to build, but MSTest's generated display name recursively formats PaneSide.Other:
30pass/3failed display generation. Replacing side data with explicit left/right
methods gives07 34pass/1remaining display-generation failure in the read-owner
fixture. That final class is corrected the same way; routing08 passes8/8. The
selection and launch side cases already pass in07. No product ToString change or
retry of unchanged tests is used to hide this harness failure.

The command-entry implementation adds three existing intents and their canonical
labels/shortcuts, a presentation focus-only F10 binding, CommandEntry context,
Shift-only F10 translation and the existing cross-context repeat guard. File-list
Tab still switches panes. Native Tab/Enter/Space remain native within the group.
The group sits outside CaptionDragArea using existing tokens, with Cycle traversal.
This is a proposed placement, not accepted visual styling. Pointer receipts and
command availability now share one Presentation input-ownership projection.
Closing a host-owned help/window scope refreshes button availability when its
existing focus return runs; no new Application close mechanism is introduced.

Command-entry01 builds0warnings/errors;141/144 cases pass. Its failures expose the
fixture's wrong use of generic Escape to close host-owned help, duplicate Escape
help rows across contexts, and the added F10/modifier changing fixed key counts.
The fixture uses the existing qualified CloseKeyboardHelp API. Help now separates
command-entry return bindings from file-list bindings. Attempt02 passes143/145,
with two old fixed group indexes affected by insertion. The new group moves after
existing groups, preserving their declared order. Only the affected help/focus
classes rerun in03:13/13pass,0skip,0warning/error. Other successful input assertions
are reused because their inputs did not change.

Command-entry App build01 passes0warnings/errors at10:18:58. Subsequent bounded
refactoring remains to rebuild: constructor composition moves to CreateCommandEntry,
and the enlarged raw virtual-key switch splits Control-only letters/digits from
unmodified navigation/function keys. No binding changes in that split. Structure02
passes112conformance rules and finds6complexity findings: new constructor41lines,
the pre-existing translator41 now42, the old window test310 now311, and3unchanged
findings. The constructor split removes its new overflow; the Control split removes
the translator overflow; the redundant count assertion is removed so the old test
size does not increase. Remeasurement and final focused proof are pending.

Changed-file formatting and record closure are running. Actual unshown control
proof is being prepared; no global desktop input, focus, user settings, physical
key/double-click sequence, visible acceptance or public publication is claimed.
All failed attempts/logs remain. Read-only command errors include guessed paths,
Windows rg wildcard paths and a sandbox process-inventory access denial; corrected
bounded reads succeed. No denied external publication or cleanup is retried.

## 09:48 routing progress

PanePointerMapper now maps exact rendered row/surface owners and retains only an
Application-accepted plain first tap for double-tap opening. It compares the actual
before-call snapshot with the returned snapshot, and expires on other input,
changed ownership/listing or deactivation. Four closed modifiers map to the four
selection actions. Passive header/blank targets remain distinct. Public immutable
focus-path identity is used without widening Application internal visibility.

CommanderPointerAdmissionTests passes12 cases, including every declared modal or
editor entry and either pane launching. PanePointerMappingTests and
PanePointerReceiptTests pass14 cases, including exact-owner mapping, a foreign row
projection, stale same-focus rejection, unadmitted first tap, candidate consumption,
wrong target, modified double tap, changed listing and settings ownership. Both
final routing03 builds have zero warnings/errors and no test is skipped. Prior
core73 and boundary14 success remain applicable to unchanged Application inputs.

Routing01 had six successful cases before waiting indefinitely on the newly used
Locations fake catalog. The exact test process was identified and stopped; that
attempt is incomplete, not a product hang. Routing02 used completed catalog replies,
but one cancelled reply correctly closed Locations, so the positive ownership
precondition failed (11pass/1fail). Routing03 supplies successful empty catalogs
and all12 pass. No production behavior was changed to satisfy those fixture errors.
The first Presentation build rejected an inaccessible internal focus lookup and
IDE0046; the next rejected three MSTEST0017 expected-value identifiers. Public
immutable focus comparison, required conditional syntax and captured expected
snapshots resolve these with no suppression. All failed logs remain preserved.

App adapters now forward framework Tapped/DoubleTapped and modifier facts through
this mapper and the existing ForwardIntent/AsyncWorkOwner route. Passive hit
classification excludes controls such as scrollbars and buttons. A native selection
change is reconciled even when the Application projection is unchanged. The old
independent path PointerPressed behavior is removed. Header background uses the
existing SurfacePaneBrush so passive background has a hit target. This integration
is still being built; no real control/native event ordering is yet proved.

Initial complexity inspection finds the same five prior findings plus one new
four-level conditional nesting in PanePointerMapper.SelectionAction. Correction
and remeasurement remain. App build01 remains in progress. No source publication,
native desktop input, user-settings change or completed feature is claimed.

Framework references consulted for the adapter are the official
[Tapped event](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.uielement.tapped),
[event device type](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.input.tappedroutedeventargs.pointerdevicetype)
and [row presenter inheritance](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.primitives.listviewitempresenter).
These describe API contracts; they are not runtime evidence.

Base: accepted mainaf1775513cd4d7d21768e0d3de1ee6e4272696bd.
Branch: feat/530-pointer-command-entry.
Worktree: D:/NeNeCommander/worktrees/issue-530.
Evidence: D:/NeNeCommander/evidence/pointer-entry-20261124.
Output: D:/NeNeCommander/outputs/pointer-entry-20261124/design-consult.

Read the applicable constitution, command/keyboard model, coding/test strategy and
design handoff. Live Issues530/531/547/552 are open; no GitHub milestones are
assigned. Retained544/548/551 records remain incomplete and are not relabeled as
accepted. The separate release candidate and its public-push hold remain unchanged.

Source inspection: CommanderPaneView projects focus to native SelectedItem, but
no file-list selection/double-click event updates Application. Only existing typed
keyboard intents change focus/selection and active side. This is a source-level
finding; no native click was injected or observed. The proposed contract and
selection/double-click risks are in ADR-0090; design reconciliation is linked there.

The background /design invocation succeeds as a command but explicitly produces
no design; it only describes access management. No permission is changed. An
ordinary tools-disabled consultation then returns a text proposal from public
requirements only. Both raw results and exact brief/invocation hashes are retained.
No source, normal settings, user files or desktop input are sent or changed.

Attempts include missing guessed CommanderInputView/PaneSelection paths and Windows
rg wildcard syntax errors; existing files were then located with rg --files.
A status command initially ran in main with only the task safe.directory allowance
and failed ownership; exact working-directory git reads are used thereafter.
Truncated multi-file outputs are not treated as full review; relevant declarations
are read in bounded sections. No build, tests, coverage, mutation, CI or remote
write is justified for these proposed documents.

Remaining: independent technical design review, accepted ADR, implementation with
causal focused tests, shared-consumer proof as justified, actual host/native and
visible acceptance, PR/main integration and task cleanup. Keep this unfinished
worktree/output; archive permanent evidence and inspect saved/untracked/ignored,
links and running references before eventual cleanup. There is no cleanup hold.

At08:52 independent read-only review requests four corrections before technical
acceptance: the toolbar context/Shift/repeat/focus boundaries; accepted first-tap
and double-tap expiry; persisted fallback range anchor; and exact blank/header
hit targets with terminal-state preservation. The proposed ADR now specifies each,
including Application-result evidence of first-tap acceptance and framework-native
Tapped/DoubleTapped sequencing. Microsoft documentation confirms the declared
sequence and Cycle tab behavior; actual host/native proof remains unrun. The ADR
stays proposed pending re-review. No production/test/build/CI/public change.
A guessed KeyInput.cs lookup fails and is corrected to KeyboardInput.cs from rg.

08:55 review correction: a stale first-tap request can return a newer current snapshot with the same listing/focus. First-tap acceptance now compares the actual Application snapshot captured inside admitted work immediately before HandleAsync against its returned snapshot, never only the request snapshot. This exact stale-but-same-focus case is required proof. All other four review areas were accepted; final confirmation is pending.

08:58 update: technical design is accepted with no remaining required correction. CMD-002/CMD-009, keyboard model and glossary now specify the chosen pointer/anchor/focus contract. New closed payloads and10causal selection cases are compiling; existing handlers still ignore them for baseline proof. Build/native/feature completion is not yet claimed.

## Application implementation and selected proof

The causal baseline compiles with the new closed intent payloads and absent anchor
API, while existing handlers ignore pointer requests. Its ten selection tests all
fail for that behavior; frozen DLL/source hashes and logs are retained. The first
baseline build failed IDE0305 before any test; the required collection expression
resolves it without suppression.

PaneReducer now owns pointer focus, explicit selection and persistent range anchor;
its internal PaneSelection calculation is shared with keyboard selection. Refresh
rebinds owned visible paths and navigation clears the anchor. PaneSession and
DualPaneSession qualify exact snapshots/entries before activation or existing
OpenFocused dispatch, freeze on either pane's Loading/Launching and retain terminal
activity. No view or native selection becomes an Application state owner.

Core attempt01 stopped on IDE0046/IDE0048 syntax requirements; attempt02 stopped on
three directly affected fixtures missing the new explicit anchor argument. The
fixtures now preserve their current anchor without an overload/default fallback.
Core03 build has zero warnings/errors and all73 selected selection/focus/navigation
consumer cases pass. Boundary01 build also has zero warnings/errors and all14
ownership/anchor cases pass. No case is skipped. Exact commands and filters are
recorded by run-application-core-03.ps1 and run-application-boundary-01.ps1, together
with their JSON receipts and logs. Both use dotnet build -c Release --no-restore
then dotnet run -c Release --no-build with the recorded MTP filter.

The boundary cases include stale same-focus requests, equal but foreign entries,
wrong-pane owners, hidden entries, either pane loading, empty-pane activation,
terminal outcome retention and anchor rebinding. Broader modal/operation/launch
admission, gesture acceptance receipts and native reconciliation remain required.
No App build/native input/feature completion is implied by these tests.

## Viewable proposal follow-up

hide additionally requested a viewable Canvas proposal from Lina. The background
consultation supplied HTML with two command-entry placements. A later Docs attempt
reached the caller's eight-turn limit without a completion result; creation remains
uncertain. Reconciliation found no listing tool, and reading the prior transcript
was denied by that consultation's restricted directory boundary. No alternative
access or duplicate creation was attempted. No confirmed Claude artifact URL,
publication or visible acceptance is claimed. Exact responses are retained.

A conversation preview adapts the two placements without adopting the original's
unaccepted selection/keyboard policy. It shows command entry only; its local close
behavior is not a product focus-return contract. Original proposal discrepancies
(blank selection retention, noncyclic movement and modal focus return) remain
excluded from ADR0090. The preview is task output, not production styling.
Script syntax passes; Playwright and playwright-core imports are unavailable, so
no rendered/native visual verification is claimed. A duplicate-target patch failed
before changes; its corrected update succeeded. The original HTML is unchanged.
