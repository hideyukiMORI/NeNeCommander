# 2026-11-24 UI trial integration — Issue #564

Status: local integration in progress; no publication or human acceptance

## Scope and invariant

The local trial combines the existing #530 pointer entry, #531 context menu,
#544 empty text file, #547 opposite-pane navigation, #548 compact Locations and
#551 display transfer candidates. beta.4/3909039 remains published unchanged.
Source copies remain separate and clean; this work owns only
D:/NeNeCommander/worktrees/issue-564-ui-trial and matching ui-trial-20261124
output/evidence directories. No shared-desktop input, CUA, ordinary App termination
or user-settings write is permitted. A visible trial is not launched by this work.

The existing CommanderSession/CommandCatalog, pane owners, FileOperationGateway,
keyboard mapper/repeat guard and window-adjustment route remain the sole mechanisms.
Only overlapping composition is reconciled; no new operation or dependency is added.

## Exact candidates and reconciliation

Start: #551 at47fc6ba19cd03a6443ab30bc6d12384967f07506, already connected to
accepted main1d0fa30342dff7d92153d33577ca13a83a886c80. #531 at
4b3e75bb2ef17171e482d11f44ad2b3726b923b3 includes #530 and #544 by ancestry.
Its normal local merge is c6b8ec7a76b75e8ee27e9615991fec6d58adcd63.
Both Stop callbacks are retained before draining; the one repeat guard retains
window transfer, focus/menu and native single-shot ownership. The combined key
model has51keys and5modifiers;20window bindings leave230identified undeclared
combinations consumed. Three catalogs contain595unique keys after this merge.

#547 at298cc56f2fdb80022b797ff2c50a5209b10d656e is merged next. Its older
per-row target/opposite projection is reconciled to hide's accepted November23
compact-palette handoff: the captured header stays stable and the existing
CommandPaletteContext/Feedback names the actual opposite destination in selected
detail and UIA text. Four direction/filter cases and ADR0084 reflect this contract.
The command is added once to the shared catalog as PaletteOnly, preserving the
existing context-menu grouping contract. There are28palette commands,50FileList
and29NavigationSurface bindings. All3locale catalogs have601unique keys.
The old address PointerPressed handler is not reintroduced; PanePointerView owns
passive header taps and excludes the nested command Button from passive hit targets.

The header's navigation projection refreshes on every Render, independently of
cached pane rows, because passive readiness and language can change without a new
active-row projection. The existing pointer/selection cache remains intact.

#548 at ee1859a40eaa3183baf5b08e3eb37be847685f08 adds only the accepted
Locations token/template changes and its exact-item/full-row identity cache in
LocationsView. Three historical document conflicts are additive; product files
merge without conflicts. The pre-Locations hash comparison below establishes the
reuse boundary; final App/XAML and control seams remain pending.

## Attempts and selected evidence

All raw commands, exact arguments/times/results and retained failures are under
D:/NeNeCommander/evidence/ui-trial-20261124. No old component failure is relabeled
as a pass, and no whole canonical/deep-review/CodeQL result is claimed.

- Initial conflict-helper regex was too broad. XML parsing rejected it before any
  resource write. The only changed state document was restored from original merge
  stages; the corrected helper validates all writes before saving.
- Automatic approval initially rejected the #547 conflict helper because a comment
  replacement might damage executable code. No command executed. Read-only inspection
  established that the single DualPaneSession conflict is entirely XML documentation
  before the class; the revised helper requires identical complete class/method body
  and valid summary XML. That checked local action then succeeded.
- Locked Release restore succeeds. App build01 fails with three missing navigation
  variable references from an automatic merge and dependent XAML type-resolution
  diagnostics (193errors/1warning total). The navigation rendering correction above
  resolves the cause; App build02 succeeds with0warnings/0errors.
- Application test build01 exposes one old two-argument palette Open call. It is
  updated to explicit CommandSurface.Palette; build02 succeeds with0warnings/errors.
- Presentation builds01/02 succeed with0warnings/errors. Application seam tests01
  pass162/162. Presentation seam tests01 pass232/233; the only failure expects28
  navigation-surface bindings before Ctrl+E was added. The corrected29-count case
  passes1/1 in count02. The other232 successes are reused without another full run.

Commands are saved in restore-01.json, app-build-01/02.json,
application-build-01/02.json, presentation-build-01/02.json,
application-seams-01.json, presentation-seams-01.json and presentation-count-02.json.
The selected Application filter covers opposite navigation, captured menu/palette
ownership, pointer transitions and the directly shared window/text-file boundaries.
Presentation covers the combined keyboard, palette/menu/entry, opposite navigation,
pointer and window-adjustment consumers. Unchanged provider operations, mutation and
release proof are not rerun merely to combine branches (ADR0061/QLT015).

## Remaining proof and preservation

Finish #548 composition, then verify the affected final XAML/App and real-control
seams, including header-button readiness with a reused pane projection, language
refresh and pointer ownership. Review the combined ownership/render changes before
calling the trial ready. Physical mouse/key/IME, visible/native screen acceptance,
source publication, feature main integration and release are separate and pending.
Retain this incomplete worktree/output and all original feature candidates. Its
cleanup follows acceptance/integration and permanent artifact preservation; it is
not included in the completed beta.4 cleanup.

The #547 merge is saved locally as15b1d1feb37ab652cf27d37d3796623851f76169
with normal112-rule/24-threat commit checks. The #548 merge is not yet committed.

## Native callback and integration-responsibility proof

The independent review identified a real adapter reentry seam: Detach clears its
managed invocation before calling native Hide, whose Closing callbacks can run
synchronously. The initial diagnostic expected one native Closing event; attempts
01/02 instead observed two synchronous events, so those fixture-condition failures
are retained. With the same product and only one adversarial action injected on
the first callback, attempts03/04 confirm the callback occurs before outer Render
returns, then fail respectively because Stop still permits a new menu and a newer
nested Render is overwritten by the older outer snapshot.

CommandMenuView now assigns one private object identity to each admitted Render
and rechecks that identity plus stopping immediately after Detach. It retains the
existing invocation-qualified command/cancellation route and native menu; there is
no second dispatch path. With only that product change, attempt05 passes all four
checks and exits0; attempt06 passes all four checks but exits3221225477/0xC0000005
after the before-close receipt. The latter is not a successful complete run. The
original source, fixtures, build records and both kinds of receipt are preserved.
A diagnostic-only follow-up observes old/new Closed events and waits for replacement
realization and closure before final window teardown; its result is still pending.
No physical input or visible acceptance is inferred from these controlled callbacks.
All six launcher receipts retain the unchanged normal-settings bytes/hash/time and
Default input desktop; processes are owned children on the declared non-input desktop.

Integration initially measures six CS-013 findings: CommanderWindow301,
CommanderSession302 and KeyboardIntentMapper310 logical lines are three new
production violations, alongside three pre-existing findings. The old window-key
test-size finding is absent in this integrated input. The new violations are fixed
by responsibility, without changing thresholds or hiding measurements:

- CommanderPaneView connects its header Button to the existing PanePointerBindings
  Forward action; duplicate host/XAML Click hookup is removed.
- DualPaneSnapshot exposes two internal immutable activity facts with exactly the
  previous loading/launching expressions. CommanderSession's bookmark/palette/address
  admission and the existing opposite-pane readiness consume those same facts.
- WindowAdjustmentKeyMap holds the unchanged window declaration table, mapping and
  repeat predicate. KeyboardIntentMapper remains the sole public mapper/facade and
  supplies the same read-only list to shortcut hints.

Normal App build04 and Application/Presentation builds03 succeed with0warnings and
0errors. Application admission02 passes224/225; its sole failure is the old27-command
catalog count in CommanderSessionBookmarkOwnershipTests, before the opposite-pane
command made the integrated catalog28. Only that expectation changes. Application
build04 and count03 pass1/1; the224 previous successes are reused. Presentation
keymap02 passes162/162. These are bounded owner/admission/input checks, not a whole
suite, full gate, mutation or release claim. Exact commands and times are in
run-refactor-proof-01/02.ps1 and the matching per-command JSON/log files.

Independent source review finds no mandatory correction in the render guard or
three responsibility changes; it explicitly retains attempt06's process-exit limit.
Final native integration, complexity reconciliation, format, commit and trial
acceptance remain pending. Production feature publication/main integration is not
performed by this local trial work.

## Final selected integration result at22:24 JST

The lifecycle-observed reentry attempts07/08 both pass the same four callback/
ownership checks and both owned processes exit0. The replacement trace proves
synchronous Closing reentry, old Closed outside Render, realized replacement,
Stop, and replacement Closed before final window teardown. Normal settings and
input desktop remain unchanged. Independent read-only review accepts this repaired
boundary. Attempt06 remains an early-teardown access-violation record; this result
does not claim that earlier teardown condition is solved or unrelated to product.

The final source copy before line-ending normalization has178 App inputs:176 are
exact, with only the declared project-reference and composition substitutions.
All four production dependency DLLs match. Fixture hashes, build04 and final
parity02 are retained. Integration attempts09(en-US),10(ja-JP),11(zh-Hans) each
pass60 checks across16 stages and each process exits0. These exercise cached
active-pane readiness while passive loading changes, three-language header UIA/
tooltip refresh, nested-button exclusion from passive hits, actual left/right
header UIA Invoke and same-location no-op, native context dismissal/right focus,
actual palette-row Invoke into the window helper and its return, and compact
Locations loading/layout/14-row cached traversal with complete final wrapped text.
Catalogs and external launching are fakes; no synthetic external path is navigated.
No physical mouse/keys/IME, shared-desktop input, visible acceptance, full provider
coverage or final distribution package is claimed. The receipt scope string is a
generic diagnostic-adapter label; each scenario/check list and launcher receipt
is authoritative for what actually ran.

Complexity02 has exactly the three unchanged findings (two5-parameter scope records
and one4-level help-presenter nesting), with no new finding. CommanderWindow299,
CommanderSession296 and KeyboardIntentMapper269 are below300; new WindowAdjustmentKeyMap
is42 and CommandMenuView109. The tool still exits1 for the retained findings.

The initial broad dotnet format command fails on7files' LF/CRLF differences and
on XAML-wired host members that its design-time analyzer treats as unused. No
analyzer suppression is added and no XAML handler is removed for this diagnostic.
The repository's actual check uses `dotnet format whitespace`. After preserving
1368 proof inputs and proving only line endings change in7files, that canonical
whitespace mode on the9 touched C# files exits0. The workspace-load warning remains
recorded. Normal builds above retain all analyzers and0warnings/errors. Relevant
code bodies, tests, dependencies and tooling are unchanged by normalization, so
successful behavior evidence is reused; raw file hashes are not claimed identical.
See final-inputs-before-eol-01.json, eol-normalization-01.json and format-whitespace-02.json.

This is local technical proof for the combined trial. The existing published
beta.4 remains unchanged; #564 is the broader TODO tracker and is not closed by
this integration. Physical/visible acceptance, feature publication/main integration
and eventual cleanup remain distinct. All unfinished original copies are retained.

## Configured interactive trial at22:34 JST

The integration commit is070f65952d349af07cd342eb545ee705316bd7e1. Documentation-only
main connection7701aa570b94a5e4fdc83e9fb656a3b92f7a881a imports acceptedccb303b;
one additive Project State conflict retains both release-cleanup and feature records.
All five resulting changed paths are documentation. Both local commits pass the normal
112-rule/24-adversarial-case commit checks; no CI or behavior rerun is started.

D:/NeNeCommander/outputs/ui-trial-20261124/interactive-trial-01 is the configured
interactive copy. It retains normal analyzers and changes only the App project reference
paths plus composition: two owned initial folders, a dedicated settings path and native
window title. No diagnostic Fixture source or fake provider is included. Locked restore
and Release build pass0warnings/errors.178source inputs and176unchanged copies match;
all476 output files match trial-manifest.json. This is a development trial, not the
published package or a canonical release build.

The exact built trial responds at two observations more than3seconds apart on a separate
non-input desktop, after full manifest verification. Its owned child is then terminated;
normal shutdown is not claimed. Normal settings and Default input desktop stay unchanged.
The startup observer receives no interactive input and calls no desktop switch. Source
7701aa5, manifest/binary/helper hashes, substitutions and result are in interactive-trial-
parity-01.json and interactive-startup-01.json. The initial private settings remain under
interactive-trial-01/settings; the normal settings resolver is absent from this copy's
composition. Trial instructions are in the local Japanese text guide beside the manifest.

At22:34 an explicit question asks hide whether this prepared private-settings trial may
be displayed on the ordinary desktop. No visible launch is performed before that answer.
Physical mouse/key/IME and visible acceptance remain pending; no ordinary App is stopped.

### Approved visible trial

At22:38 hide's explicit visible-launch answer is executed once. The exact configured
trial runs as PID37744 (created22:38:20JST), with responsive native title
NeNe Commander / UI trial #564 observed22:38:22. The prelaunch manifest/critical seven
hashes match, normal-settings hash is unchanged, no shared input is sent and no ordinary
App is terminated. A prior filename guard fails before launching (resources.pri versus
the actual NeNeCommander.App.pri); it is retained and corrected using manifest identity.
The live trial is left to hide. Visible launch is complete; human acceptance has not
been reported. Its active output/private settings cannot be cleaned or rebuilt in place.

## 23:09 — requested icon and parent-entry revision

hide reports the general trial appearance close to the intended image at22:41,
asks for vector Bookmarks/Settings/Help with hover text, then at22:43 asks for a
parent-folder icon in each pane. This is scoped feedback, not acceptance of every
feature or release approval. Drag-and-drop remains unimplemented in separate #532.

ADR0090's amendment extends the same qualified surface action with NavigateParent.
Exact snapshot ownership, provider parent availability and shared work/operation
admission run before side activation. Both sessions use one follow-up declaration
and the existing PaneSession navigation. Root/absent/stale input changes nothing.
The pointer mapper rejects asynchronous parent completion as a synchronous focus
receipt. The parent arrow precedes each path and reuses the existing navigation
button tokens. The three title-bar buttons keep PathIcon content across rendering,
localized tooltip/name/shortcut and existing native focus/key behavior. No dependency,
provider, mutation gateway, normal setting or published beta.4 source is changed.

Evidence root remains D:/NeNeCommander/evidence/ui-trial-20261124:

- `dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore`
  (`icons-parent-app-build-01`): success,0warnings/errors.
- `run-icons-parent-proof-01/02/03/04.ps1` retain every exact invocation and outcome.
  First Application test build fails CA1062 in the new test parameter; explicit
  validation fixes it. The first selected run has123passed/1failed: the dynamic
  PaneSide argument's generated ToString recurses while MSTest generates its name.
  Replacing test arguments with declared left/right text resolves the fixture issue;
  14/14parent cases pass. Across both runs129distinct selected cases pass, including
  expanded modal/gateway rejection, existing pointer row-open and navigation consumers.
  Successful unrelated cases are not repeated.
- First Presentation build fails MSTEST0017 on the new assertion's inferred argument
  naming. `expectedSnapshot` follows existing tests and build02 has0warnings/errors.
  The selected mapper/receipt/command-entry/navigation run passes47/47.
- `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` exits1 with exactly
  the same3prior findings. `icons-parent-complexity-comparison.json` compares full
  finding objects; no threshold/baseline is changed. Unresolved invocations remain
  visible in the raw report. `dotnet format whitespace NeNeCommander.slnx --no-restore
  --verify-no-changes --include <15changed C# paths>` succeeds; the ordinary
  workspace-load warning is retained. Exact path arguments are in whitespace01.json.
- The new isolated diagnostic copy verifies176exact App source files and4dependency
  DLLs; project/composition substitutions and7Fixture sources are explicit in
  `icons-parent-controls-parity-01/02.json`. Only dedicated D paths/settings and
  a non-input desktop are used, with no synthetic shared input or desktop switch.
- Native attempt1 passes52checks including3locale icon/name/tooltip retention, both
  actual UIA parent Invokes, origin focus, root projection and modal rejection, then
  fails because the diagnostic sends help Escape into the generic session route.
  That does not own help closing. The fixture now uses the existing KeyboardHelpView
  forwarder used by actual mapped help keys; no product correction is made. Original
  source/failure/exit2 remain retained. Diagnostic build02 succeeds0warnings/errors.
- `pwsh -NoProfile -File <evidence>/run-icons-parent-controls-01.ps1 -Locale en-US
  -Attempt 2 -Scenario icons-parent` passes56checks/7stages and exits0. The same
  command with ja-JP/3 and zh-Hans/4 also passes56each/exit0. All three before/after
  ordinary-settings fingerprints match and input desktop remainsDefault. Exact
  commands/arguments, raw outcomes and final close observations are retained.
  Rendering screenshots are exported from the actual control tree; English/Japanese
  are visually inspected. This is control/property/API proof, not physical input,
  actual hover event delivery, screen-reader speech or visible human acceptance.

Normal settings were already at hashC2ABA704FEC19E2FABA2FA87CD39C6851F034AE8B68A8F6D7B2B6FAF3EB2C3FA
before this native run (write time23:02:34), different from the earlier22:38 trial
baseline. No cause is attributed. Each native run's own before/after bytes/hash/time
match; no restoration or overwrite is attempted. Live trial01 output is retained.
Two source-comment clarifications after these tests name navigation/header controls
more accurately; their executable bodies are unchanged and successful proof is reused.
A first documentation append stops on the shell's default text decoder; source
comments/glossary had already been written, and the remaining append explicitly
uses UTF-8. No product edit or proof is hidden by that recording failure.

Independent read-only ownership review finds no required product correction; its
initial test-build naming finding is corrected as above. Final receipt reconciliation,
updated interactive02 and local save follow. No trial push/main merge/tag/release.

23:10 independent read-only final reconciliation: no required correction. The original
MSTEST0017 finding is resolved;129distinct Application/47Presentation successes,
identical3complexity findings,176App inputs/4DLLs/7fixture identities and all three
56-check/exit0 native receipts reconcile. Physical input and human visible acceptance
remain separate. Save this local revision and prepare interactive02 next.

23:15 interactive02: product revision is saved ascd3ecab83a4215dd4172047a39c122bd13bdbb12
with the normal conformance/security/commit-message hooks passing. The configured
copy builds0warnings/errors with ordinary analyzers. Its178App inputs include176
exact copies, with only declared project/composition substitutions;4dependency DLLs
and all476output files match. No Fixture source or fake provider enters this copy.
The old trial's dedicated preferences are copied with stable source/destination
hash143542E3EC8DB78FC152C2D2297FB798A1AC175209EC69027FE557D47E965905;
normal settings are never copied. The manifest hash is
80387725DA54DE816CD54ACF2EE4A9B5DE396D0B0EB7B96774CB23799B49244E.

`verify-interactive-startup-01.ps1` verifies interactive02's complete manifest and
observes the exact unmodified binary twice more than3s apart on a non-input desktop.
It terminates only that owned startup child; normal shutdown is not claimed. Its
settings/input fingerprints remain unchanged. Then the standing visible-trial
request and this explicit revision request are fulfilled by
`launch-interactive-trial-02.ps1`: one responsive window is observed23:14:48JST,
PID49036, HWND77925544, title「NeNe Commander / UI trial #564 - icons and parent」.
No input is sent, no ordinary App is stopped, and normal-settings hashes match.
Preserve the running binary, its owned folders and private settings at
D:/NeNeCommander/outputs/ui-trial-20261124/interactive-trial-02.
See interactive-trial-parity-02.json, interactive-startup-02.json and
interactive-visible-launch-02.json. All original source/fixture/trial01 outputs remain.

hide's visible acceptance of this revision and the next release's cutoff are pending.
At23:15 an optional scope question offers this trial's features as the next cutoff,
adding drag/drop, or deciding later. No answer, new release authorization, public
feature push, PR, main merge or new tag is inferred. beta.4 remains already published.
