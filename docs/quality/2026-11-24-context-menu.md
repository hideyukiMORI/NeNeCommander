# Context-menu design and implementation proof - 2026-11-24

Status: local implementation and assembled canonical/mutation component evidence complete; canonical CodeQL extraction, candidate processing and product acceptance remain pending.

Latest: see the final November24 17:15 section. Earlier sections are dated investigation and execution history; the exact beta.4 source/PR publication completed at12:55 and is no longer awaiting approval. The separate candidate SARIF question remains unanswered.

## Initial design and investigation history

Issue531 is open. The dedicated worktree is D:/NeNeCommander/worktrees/issue-531,
feat/531-context-menu, based on local technical pointer candidate76bff48. New-file
Issue544 remains a required dependency for its mouse entry; its local coreb593ef4
and existing provider evidence are retained separately. The beta.4 release candidate
f6c722c and pending exact publication approval are unchanged.

The invariant is exact displayed/executed command targeting through the existing
CommandPaletteSession/CommandCatalog/CommanderSession owners and existing operation
paths. Proposed ADR0091 adds closed menu surfaces and qualified context acquisition,
not a parallel dispatcher. Explorer reveal extends the existing Shell launcher and
STA scheduler with explicit path-handoff limits.

Live GitHub issue bodies, relevant owner/mapper/launcher source, the accepted pointer
ADR and the new-file ADR/quality/handoff were inspected. Background /design returned
access-management guidance only; no consent was changed. Tools-disabled ordinary
Claude advice was collected from an authored public brief with SHA-256
EDBB62D2FCF3995D0B17E93AEAB0DFE4694ACB371FE92A486261CD42B9FAC953. No repository source
or local file contents were sent. Advice, two invocations, raw JSON/stderr and all
investigation attempts are retained in D:/NeNeCommander/evidence/context-menu-20261124.
The journal records failed guessed-path/wildcard reads as well as the slow completed
worktree/file discovery. No render or native design acceptance is claimed.

Technical decisions and selected differences from the advice are explicit in
[ADR0091](../adr/0091-context-menu-through-command-selection.md). Stock native menu
heading/accessibility behavior and root/popup key ordering remain questions for a
bounded design/control prototype. Production implementation awaits independent
technical review. Real Explorer display is not run on hide's shared desktop.

Documentation-only scope: no product build, tests, mutation, full gate or CI starts.
The existing release Infrastructure02 process remains independently live. Preserve
this unfinished worktree/output; integration and unique-evidence checks precede any
completion cleanup. Every subsequent attempt and changed input is appended here.

12:16 independent review: three required clarifications are recorded and addressed
in the proposed ADR: surface-specific rendering/focus and shortcut absence; explicit
positionless ContextRequested rejection plus per-owner deferred Closed finalization;
and inheritance of existing non-interruptible Shell/normal-stop cancellation. The
proposal remains proposed, with no product source/build/test/CI claim. A re-review
will decide whether those mechanism clarifications are sufficient; native keyboard/
control ordering and visual acceptance remain separately unproved.

12:18 re-review: the clarified mechanism has no remaining mandatory design correction
and is technically accepted at that level. ADR proposed status is retained while the
bounded host prototype remains unproved: same-turn Click/Closed orders, submenu
Escape/repeat and root/popup input routing. If the queued finalizer runs before the
actual valid Invoke, redesign that boundary. UI presentation/accessibility, physical
input and implementation acceptance are not implied. No source change or product
build/test/CI has occurred. Full reviewed ADR hash and scope are in
independent-mechanism-review-02.json in the task evidence directory.

## Minimal native menu prototype — 12:26 to12:30 JST

A minimal standalone App copies only the pinned App SDK/project/build configuration
and desktop-isolation helper into outputs/context-menu-20261124/menu-prototype-01.
It does not reference product assemblies, use product settings, enumerate user paths,
or call Shell. Analyzers are disabled only for this diagnostic copy. The first
restore/build exits0 with0warnings/errors; exact commands are dotnet restore
D:/NeNeCommander/outputs/context-menu-20261124/menu-prototype-01/App/NeNeCommander.App.csproj -p:Configuration=Release
and dotnet build the same project -c Release --no-restore. Both logs and the receipt
are retained. This is bounded framework investigation, not a product build/test gate.

Attempt1 runs via pwsh -NoProfile -File
D:/NeNeCommander/evidence/context-menu-20261124/run-isolated-menu-01.ps1 -Locale en-US -Attempt 1.
It passes/exit0 on NeNeCommander531_20261124, with input desktop Default unchanged.
Both root and New-submenu UIA Invoke yield Click, Closing, later Closed, then queued
finalizer. Each submits once before cancellation; API Hide yields no submission and
does finalize cancellation. An explicit full automation name/help survives a long
target label; the root item reports MenuItem,398.4x32DIP and the submenu item96x32.
No screenshot/visual fit, actual product wording, physical input, or complete UIA
accessibility acceptance follows from those properties.

Attempt2 adds explicit native WM_KEYDOWN/UP messages to GetFocus's HWND only after
same-process and non-input-desktop guards. No SendInput or SwitchDesktop is used.
Diagnostic repeat handlers on BOTH window root and submenu presenter receive the
same initial Escape. Root sets held; the submenu mistakes that same routed event
for a repeat and suppresses it. Stage10 times out, exit1 from the launcher/child2.
This is a causal adapter-design failure, not an environment skip. All failed
events/source/build/launch receipts remain. The next attempt removes the second
handler: the observed root is already in the submenu's routed preview path.

Several read-only guessed-file/glob calls failed and were replaced by discovery;
the missing failure JSON for successful attempt1 is not a failed control run.
A release-ledger update script first failed JavaScript parsing before any shell
action, then the corrected invocation succeeded. No product source mutation or
public write occurred during any of these attempts.

Root-only attempt3 succeeds, child exit0/input desktop Default unchanged. Actual
Escape preview is observed once at window-root for each native key message.
First Escape collapses only the submenu; one held-repeat event is suppressed;
key-up clears the diagnostic held state; a fresh Escape closes/finalizes the root.
See menu-en-US-3-passed.json and isolated-launch-en-US-3.json. The ADR explicitly
replaces dual root/popup forwarding with the existing WindowFrame single route.

Build04 of the additional Enter/Space probe fails CS0649 because CRLF-based text
insertion did not match the LF diagnostic helper; XAML also emits WMC1509 after
the missing assembly. No App run follows that failed build. Source/log retained;
build05 corrects the insertion with an explicit match assertion. Product source
is still untouched; final product mapper/host tests remain required.

At12:34, attempt4 passes/exit0 after build05 succeeds with0warnings/errors.
The exact launcher command is pwsh -NoProfile -File
D:/NeNeCommander/evidence/context-menu-20261124/run-isolated-menu-01.ps1 -Locale en-US -Attempt 4.
The same root-only native Escape cases pass. Enter invokes once; a newly opened
menu receives and suppresses a repeated Enter without submitting/closing; Space
then invokes once. UIA root/submenu invocation and native Enter/Space all precede
Closed/finalization. Across six owners the submission counts are1,1,0,0,1,1.
Source hashes, sequence summary and hashed runtime/launch receipts are retained in
menu-prototype-04-summary.json. Normal input desktop remains Default. No product
mapper, physical modifier/IME, three-locale resources, visual fit or final host
acceptance is implied. Both same-turn callback orders and final state-qualified
cancellation/submission remain required deterministic implementation cases.

A necessary independent read-only review of the revised one-root-entry mechanism
and raw prototype evidence is requested from the existing reviewer. No new worker
implements or edits source. The accepted #544 core remains a required dependency;
its80-file change is being inspected before local integration, not sent publicly.

12:38 independent read-only review has no mandatory design correction and accepts
the single-root mechanism. The ADR becomes technically accepted before production
implementation. The reviewed ADR SHA-256 is
14BBD3900A8E2D8ADC1AE12B1703383598B2EA0748129E9EF0DBBD36652FA28A; subsequent differences
only record status/acceptance and this explicit proof limit. Attempt4's Enter KeyUp
precedes the repeat-marked event on the next owner. It does not prove uninterrupted
held-input transition. Final product mapper/repeat-owner integration, both callback
orders, stale/replaced/finalized/stopping owners, Shift+F10/Menu duplicates, IME,
modifier/focus, three-locale/long-target/disabled/accessibility/visible evidence remain.
No build/test/App run or edit was performed by the reviewer.

The read-only git merge-tree preview of accepted #544 b593ef4 from shared7ef9b96
finds four content conflicts: PROJECT_STATE, ADR index and en-US/ja-JP resources.
It changes no worktree/index. The new-file core has80changed files; integration must
preserve current pointer/language/theme/placement behavior and supply the third
shipped resource catalog. Product validation follows actual merged inputs; old
provider evidence is reused only if the relevant boundary inputs remain identical.

## #544 dependency integration — November24 13:05 JST

The exact accepted local coreb593ef498d299536c2091de0eec72cd8cdc78181 is merged into
the feature from61b27e4 without touching main or the release candidate. The four
expected conflicts are resolved: both additive PROJECT_STATE/ADR-index blocks stay,
current en-US/ja-JP catalogs retain every existing552key/value and import only the9
new-file keys. The incoming obsolete AddressEditorInput attached-key spelling is
not reintroduced. Simplified Chinese receives the same9keys, including editable
default 新建文本文档.txt. All3catalogs have561keys and unchanged prior values.

Selected integration01 restores/builds all3test projects with0warnings/errors.
Application164/165 and Presentation73/75 expose3integration fixture assumptions:
two24-command counts and a fixed23-step last-row navigation. Infrastructure53/53
passes, including owned Windows exclusive-create boundaries and WSL fake contracts.
The fixtures now expect25commands and advance through the actual row count to
prove clamping. Only those3changed cases rerun in integration02, allpass. The
imported CreateTextFile window-mode-freeze case separately passes1/1. Thus distinct
successful selected cases are Application166, Infrastructure53, Presentation75.
The two known old window-planner fixture failures from#530 are not claimed repaired.

Exact scripts/filters/commands/returns are run-integration-selected-01.ps1,
run-integration-selected-02.ps1, integration-selected-01/02.json and
integration-window-freeze-01.json/log in the task evidence directory. Default native
filesystem test temporary roots are under the dedicated D output; no live WSL tier
or shared desktop input is invoked. App locked restore and normal Release build
both exit0 with0warnings/errors. No full gate, mutation or CI is repeated here.

All FileOperations source in Application/Infrastructure, Domain Paths, the7existing
live-WSL source/ownership fixture files, live-WSL launcher and pinned build/package/
SDK declarations have an empty git diff againstb593ef4. Existing provider/live
evidence retains its original scope; this source comparison does not claim new
native UI acceptance. Changed session/catalog/keyboard/presentation consumers use
the selected combined-source tests above. Full shared-contract deep review remains
required after the actual context menu implementation is complete.

CS-013 Inspect exits1 with exactly the same4pointer-baseline findings (all location,
metric,value,limit fields equal),1599types/10087members. There is no new finding and
no clean-complexity claim. Several read-only guessed path/glob probes fail without
changing source; subsequent discovery uses actual paths. All failed build/test
attempts are retained. Context owner/menu/reveal product implementation is next;
the imported keyboard/palette new-file core alone does not complete either issue.

## Shared context/reveal implementation — November24 13:48 JST

After integrationbf3c877 the sole CommandCatalog now declares surface/group metadata,
CommandPaletteOpen captures its surface, and CommanderSession synchronously admits
an exact context request through the existing PaneReducer before capturing candidates.
Search/pointer/context candidates share one owner and exact candidate membership.
Selected-row context preserves the batch/anchor, unselected context performs single
selection, and background context preserves focus/selection while offering folder actions.
PaneLaunching/failed/cancelled share an explicit launch/reveal purpose. Windows reveal
uses the existing STA Shell scheduler and a narrow PIDL parse/select/free boundary;
no real Explorer display is invoked. All launcher fakes and purpose-aware presenters
are updated together. Initial fixture/build failures remain in the evidence folder.

Selected Application context/reveal/direct-consumer tests pass152 distinct cases;
Infrastructure81 pass including native owned-path PIDL parse/free without Explorer;
Presentation44 distinct cases pass across the recorded attempts. Exact filters and
build/test exits are context-application-selected-01/02/03.json,
reveal-infrastructure-selected-01.json, reveal-consumers-selected-01.json and
reveal-presentation-selected-02/03.json. Presentation's initial failures are stale
catalog count/unbound shortcut assumptions; only changed cases rerun. No exclusion,
threshold change, or unrelated window-planner repair is made. Complexity02 reports
CommanderSession324 logical lines and context admission cognitive11; scope capture
and existing owner wrappers are subsequently factored, with remeasurement pending.

The native MenuFlyout adapter is now implemented against captured application state.
Its four native target submenus name focused item, selected batch/focus fallback,
New destination and current folder; children reuse canonical localized rows, reasons,
shortcuts and full automation names. Three shipped locale catalogs receive the same
headings. Pointer ContextRequested is qualified by rendered snapshot/owned row;
positionless framework events are handled/ignored. Shift+F10 and Apps use the sole
KeyboardIntentMapper. BareF10/Tab retain their prior entry/pane behavior. Native menu
navigation passes through; Enter/Space/Escape repeat ownership persists across scopes.
Exactly one WindowFrame preview/character route remains; no popup handler is added.

CommandMenuInvocation is an adapter callback qualifier, not another command session.
Closed queues one dispatcher finalizer; a same-turn Click consumes the exact owner
first. Departing/stopping adapters detach pending callbacks. Both callback orders,
genuine late clicks, foreign/disabled candidates, replacement scopes and held-input
owner transitions are tested through the real Application owner. Initial build01
fails on the MSTest null-assert return and two test analyzer diagnostics; build02
passes0warnings/errors. Selected147 cases yield145pass/2fail solely from the newly
expanded binding/key counts. App menu-app-build-01 passes0warnings/errors in19s.
Presentation test additions03 have4 literal escaping errors;04 corrects those but
finds unused using/assertion-name analyzer diagnostics. These attempts are retained,
with the two expected counts and test-only diagnostics now corrected for attempt05.
Actual final-product controls, menu layout/locale/DPI/disabled access, physical keys,
IME/visible acceptance, focused coverage/conformance and completed-feature deep review
remain pending. No whole-feature success or public integration is claimed.

At13:45 hide asks whether push also creates the release. The answer records that
PR573 publication is complete but tag/release is not automatic; candidate-ref CodeQL,
main integration and final-package proof precede tagging/publication. This question
is a status clarification, not a new release action or a stop to ongoing work. The
public f6c722c release candidate and unfinished feature copies remain separate.

## Product host verification and engineering corrections — November24 14:16 JST

The callback/keyboard suite02 passes145/147. The two failures are explicit expanded
binding/key counts; corrected counts and four new target-projection cases pass6/6
in attempt05. Builds03/04 retain test literal/analyzer errors before that success.
Conformance01 rejects the native HRESULT property named Result and a boolean test
mode parameter. The property becomes HResult and the two event-order tests are
separate methods. No task blocking call or production boolean mode was introduced.
Conformance02 passes112rules. Complexity01 finds four newly exceeded budgets in
addition to the four existing findings. Existing responsibilities are isolated:
CommandContextRequest derives target/surface only, CommanderSession still applies
and captures synchronously; PaneSession separates navigation from external handoff;
KeyboardRepeatGuard owns native-repeat policy; window text initialization is named.
Two refactor builds retain IDE0046 failures, then switch-based routing passes selected
Application114 and Presentation34, with App build02 at0warnings/errors. Complexity03
returns exactly the existing4findings. No threshold/suppression is added.

The product-controls-01 diagnostic copies all177App inputs. Only composition startup
and project-reference paths differ: owned D folders/settings, guarded non-input
desktop activation, test callbacks, and a fake IFileLauncher that cannot display
Explorer/external applications. Original product view/focus/mapper code is unmodified.
Actual original provider creation remains active only on the owned fixture paths.
Probe extra source and runtime identities are recorded separately. Normal App build03
passes0warnings/errors; diagnostic builds retain their explicit analyzer-disabled scope.

Native attempt1 reaches stage10: row/UIA Open, native Apps opener, New > Text file,
existing localized name editor and actual Enter-driven exclusive zero-byte creation
all run. Enter-down without key-up does not complete native invocation in that case,
so the bounded fixture times out/child2. Attempt2 adds native event tracing and a
normal Enter press/release, passes110checks, and separately proves uninterrupted
Enter ownership by issuing UIA Invoke after the observed initial Enter-down and before
any key-up. A replacement scope consumes the native repeat without another handoff.
That transition is not misrepresented as physical Enter-down activation. Submenu
Escape preserves root, held Escape stays consumed, fresh Escape closes root, and its
held repeat cannot clear returned pane selection. Actual callback replacement and
stopping/late-owner behavior also pass. The failed attempt/source/binaries remain.

Attempt2 measurements reveal a1779.2DIP long-target menu in a529.6DIP viewport despite
finite-geometry assertions. The product now constrains native presenter width to the
actual window and overrides the framework's semantic MenuFlyoutItemTextTrimming from
Clip to CharacterEllipsis; it keeps full group/command names and tooltip/HelpText.
No final color, density or arbitrary new width token is introduced. The final probe
requires width within viewport and real TextBlock ellipsis/trim state. English3,
Japanese1 and SimplifiedChinese1 each pass144checks at125%; disabled-opposite-pane
English4 passes5checks including disabled native/UIA state and full canonical reason.
The same managed diagnostic binary runs all four. Every child exits0, input desktop
stays Default, and normal settings bytes/hash/write time remain unchanged.

product-controls-final-parity-01.json reconciles177App source inputs (only the2named
composition/project differences),4dependency DLLs and137compiled XBF/PRI resources;
no unexpected difference exists. review-production-inputs.json freezes795production
inputs. Exact launcher: pwsh -NoProfile -File
D:/NeNeCommander/evidence/context-menu-20261124/run-product-controls-01.ps1
-Locale <locale> -Attempt <recorded number> [-Scenario disabled]. No physical pointer,
physical Shift modifier, IME, visible/high-contrast/speech acceptance or real Explorer
handoff is claimed. Geometry is tested on reopening at recorded sizes, not arbitrary
resizing while an existing popup remains open.

Seven additional menu-to-effect tests pass after preserving their initial path-literal
build failure: copy/move retain the marked batch and captured opposite folder;
permanent deletion uses the existing exact confirmation and cancellation; both New
actions use the existing name owner; Refresh and Bookmarks use their existing routes.
See menu-dispatch-selected-02.json and its exact filter/log. Required command/keyboard/
filesystem/security/glossary records and existing threat definitions are updated.

The existing independent reviewer now reviews the frozen production diff read-only.
Affected final coverage, source formatting/conformance and complete shared-contract
deep proof are still pending. Known unchanged-base Application window-fixture failures
from#530 remain separate; do not suppress them or claim a green full gate. This local
feature is not added to beta.4, publicly pushed, integrated or accepted visually. All
unfinished worktrees/evidence remain retained; no cleanup request/rejection occurred.

## Independent review, causal correction and selected coverage — November24 14:37 JST

The independent read-only review identifies two mandatory host corrections and one
normative wording correction. A raw ContextCanceled previously hid whichever menu
was current; Render also requested current-pane focus for any closed state, including
stale endpoints without a cancellation receipt. Request now returns a callback bound
to the exact newly admitted invocation/menu, or null for a rejected request. The pane
keeps it only for its own request surface and discards it at pointer press/release;
old, foreign, replaced and stopped callbacks cannot close the current menu. Render
returns focus only for CommandPaletteClosed.FileListFocusSide. The command-model and
ADV-019 text now correctly distinguish unchanged activation ExecuteAsync from reveal
ExecuteShellAsync on the existing STA scheduler. No activation implementation changed.

The official ContextCanceled contract describes a request followed by manipulation
before PointerReleased. The adapter tracks that gesture boundary; the callback itself
retains the exact invocation. This is not a claim that the event supplies a gesture
identifier, nor physical touch/pointer event-order proof.

The new diagnostic review scenarios retain the existing non-input desktop, private
settings/folders and fake external launcher. Baseline build01 fails on inaccessible
Application constructors; test-only reflection fixes that in build02. Baseline cancel
attempt5 reaches the rejected-request guard, but its immediate replacement assertions
were too early to observe native closure. Build03 adds realized-menu/Closing observation;
attempt7 then causally fails on old same-pane cancellation. Baseline focus attempt6
fails on a closed state without a focus receipt. These logs, fixture sources and DLLs
are retained in product-controls-before-review and product-review-causal-baseline.

Fixed build01 accidentally reuses the baseline DLL because copy2 preserved older source
write times. Attempts8/9 therefore run the same CABF8485... DLL and are not corrected
product evidence. Updating only the copied input timestamps plus a runtime return-type
trace forces compilation. Attempt10 passes11 cancellation/gesture/stop checks and exits0.
Attempt11 passes5 assertions but exits0xC0000005 during native teardown after a probe's
immediate repeated Show/Hide; it is not an accepted complete run. The diagnostic alone
is staged to await each actual Closed event and retain its adapter. Fixed build03 and
attempt12 pass5 focus/stop checks with exit0; attempt13 passes the existing144 actual
control checks with exit0. No additional production fix is made for that diagnostic
lifetime correction. All children leave Default input desktop and normal settings
623 bytes/hash/write time unchanged. Commands:

- dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore
  → menu-review-app-build-01.log,0warnings/0errors.
- dotnet build D:/NeNeCommander/outputs/context-menu-20261124/product-controls-01/App/NeNeCommander.App.csproj -c Release --no-restore
  → product-review-baseline-build-01/02/03.log and product-review-fixed-build-01/02/03.log,
  with the failure/reuse limits above; final diagnostic build0warnings/0errors.
- pwsh -NoProfile -File D:/NeNeCommander/evidence/context-menu-20261124/run-product-controls-01.ps1
  -Locale en-US -Attempt 10 -Scenario review-cancel; the same launcher with
  -Locale en-US -Attempt 12 -Scenario review-focus and
  -Locale en-US -Attempt 13 -Scenario standard → accepted11/5/144 checks, each child exit0.

Affected layer coverage uses run-coverage-selected-01.ps1 with canonical MTP coverage
settings, Release/no-build/no-restore and D-owned process TEMP/TMP. Application1051:
1050pass/1fail, branch99.9389499389%. The sole failure is the unchanged-base NullGuardTests
reflection call to UserSettings.Create with three instead of four arguments. Its fix
already exists in the separate beta.4 branch; it is not suppressed or independently
rewritten here. Earlier records of two #530 baseline failures are not the results of
this current run. Infrastructure1046:1026pass/20explicit environment skips, branch
94.6120689655%; Presentation460/460pass, branch97.0056144729%. No live provider, real
Explorer, or broad repeated test was added to resolve the unrelated fixture failure.

The uncovered changed branch is the dual-pane coordinator's accepted row Open path,
not the already-tested direct PaneSession route. A new consumer test opens the exact
inactive-pane directory once, preserves the other pane and rejects the stale repeat.
Two current/stale endpoint cases also verify cancel and submit close without a focus
receipt. The exact focused command after its normal analyzer-enabled test build is:

    dotnet run --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build -- --filter "FullyQualifiedName~PanePointerOpenTests|FullyQualifiedName~CommanderContextOwnershipTests" --coverage --coverage-settings eng/coverage.settings --coverage-output-format cobertura --coverage-output D:/NeNeCommander/evidence/context-menu-20261124/menu-review-application.cobertura.xml

All16 selected cases pass. Both direct and dual HandleInteractionAsync methods have
6/6branches in this focused report. This supplements the changed-boundary proof; it
is not relabeled as a successful full Application coverage run or full canonical gate.

Conformance03 passes112rules. Complexity04 returns exactly the same4baseline findings
(path/metric/value/limit), with no new finding or changed threshold. Changed-file format
verification01 detects line endings/spacing; whitespace-only formatting of30 out of75
selected C#files is reconciled in menu-format-input-comparison.json. Verification02
passes, with a workspace-load warning retained for inspection. Non-whitespace content
is unchanged, so successful behavior/native results are reused rather than replayed.

review-production-inputs-02.json freezes795current product inputs. Updated parity02
reconciles177App sources (2existing diagnostic composition/project differences and
5whitespace-only differences),4dependency DLLs and137compiled XBF/PRI resources.
Accepted receipts carry their own exact binary hashes; attempt11 is excluded. The
three-locale/disabled wording and layout evidence remains from the preceding same
resources/unchanged presenter; the corrected adapter additionally runs the English
144-case scenario. Physical input/IME/visible/high-contrast/speech and real Shell
selection remain unclaimed. Focus probe snapshots isolate the actual adapter callback
contract; actual endpoint invalidation is proven by the separate Application cases.

The combined shared-command/Shell/#544 feature still requires its one justified deep
review before technical completion. Known baseline failure is recorded rather than
rerunning an inevitably failing whole gate or fixing an unrelated case. Independent
correction review, final engineering records and local commit remain next. No public
source push, CI dispatch, main integration, tag/release or cleanup occurred for #531.

At14:41 the independent correction review finds no remaining mandatory correction
or evidence misstatement. It rechecks all795current product hashes, the three accepted
receipt hashes and normal exits, and confirms that failed/stale executions and full
technical acceptance remain separate. This is read-only review, not another test run.
The formatter warning is traced to absent Debug reference binaries. Process-scoped
Configuration=Release plus the same75-file whitespace verification with diagnostic
verbosity passes0 with no load warning (menu-format-release-03.log/json). No product
code changes or repeated behavioral tests are needed for this tooling clarification.
The source/input and full/deep limitations above still apply.

## Accepted base integration and remaining deep scope — November24 14:48 JST

The reviewed feature is saved as3946dc4 (89files; normal Commit gate and commit-message
check pass). To prepare its required whole-feature proof, the already accepted beta.4
basef6c722c is merged into this feature branch only. This does not add the feature to
the public release candidate or main. The merge preview reports exactly three
conflicts: PROJECT_STATE preserves both chronological records, the palette catalog
fixture keeps27feature commands rather than24base commands, and the PaneSession null
fixture keeps the new purpose arguments while explicitly discarding the accepted
helper's new return value. Product files have no merge conflict. This is dependency
integration of an existing accepted fix, not suppression or a new unrelated repair.

The integration brings the known UserSettings.Create fixture correction, accepted
count formatter/one-pass count fixes, the placement/null-boundary tests and beta.4
version. The exact file list is retained by the merge receipt and staged Git inspection.
No dependency pin, exclusion, threshold, formatter or native host code is invented.

The shared command-selection/catalog contract affects existing commands and input
owners, while544 introduces a provider mutation and531 a new Shell handoff purpose.
Focused cases alone do not establish their combined full-consumer/closure/adversarial
impact. Therefore run the canonical eng/check.ps1 once on this combined candidate,
then the changed Application/Infrastructure/Presentation adversarial repetitions and
Complete mutation thresholds. The successful identical Domain mutation/adversarial
and unchanged dependency-audit evidence may be reused after exact relevant-input
mapping under ADR0061. Do not repeat those successful tiers merely for a merge.
Record this as assembled stage evidence; do not claim an unexecuted full deep-review
script or CI workflow passed. Candidate CodeQL and physical/environmental tiers retain
separate obligations. The existing source and earlier selected proofs remain retained.

## Whole-feature proof and CodeQL correction — November24 15:45 JST

The first canonical run on20336c8 fails deterministic theme-import parity after
conformance112, all negative gate fixtures and security24/proofs pass. No build,
test, coverage or mutation stage starts in that failed run. All owned processes
end before source edits; exact1317-input checks and the controlled-stop receipt
are retained. The three resource catalogs are normalized to the importer output
with exact XML-tree equivalence. No translated value, threshold or importer changes.

The supplemental compiler-traced CodeQL run completes164rules with134raw results:
122generatedobj and12owned. Compiler/extractor errors are absent;42hidden compiler
messages and the explicit unsupported-path-filtering notice remain in raw SARIF.
Eight owned expression/test recommendations are addressed by seven changed files:
format-key resolution before formatting, menu/repeat condition factoring, context
recognition in its session owner, and observable test assertions/sequence mapping.
Independent read-only review catches duplicate using/IDE0058 issues in unexecuted
plans; only corrected plan05 executes. Earlier plans remain as rejected attempts.
The guard introduced in that plan creates one unreachable FolderContext false edge
following the other two closed cases. Remove that redundant edge, preserving the
actual Folder/New membership and existing consumer tests; no artificial invalid
surface, suppression or excluded coverage is introduced.

Four note-level native-call advisories are retained as intentional boundaries:
CoInitializeEx, CoUninitialize, RtlNtStatusToDosError and SHOpenFolderAndSelectItems.
The first three predate this feature and are recorded by ADR0063/SEC014; the fourth
is explicitly adopted by ADR0091. A managed substitute preserving these contracts
is not established. The initial journal's GetVolumePathNameW label is corrected to
RtlNtStatusToDosError from the exact SARIF/source location. The raw notes are not
filtered, waived or described as fixed. A new compiler-traced analysis and a separate
canonical build-mode:none analysis run against exact frozen inputs03; results remain
pending. Neither local mode proves candidate-ref alert processing under ADR0031.

Commands and staged results:

- pwsh -NoProfile -File ./eng/check.ps1: failed theme parity as above; original log
  deep-canonical-01.log and deep-progress-01.json remain failed.
- pwsh -NoProfile -File run-canonical-remainder-02.ps1 (task evidence path): fresh
  conformance/security, locked restore, whitespace, Release solution build and record
  closure pass. Ordinary solution tests total2766:2746pass/0fail/20explicit environment
  skips. Coverage Domain100% passes; Application1080tests pass but99.9386503067% fails
  its100% threshold before the redundant-edge correction. The original log is retained.
- dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj
  -c Release --no-restore: corrected build03 passes0warnings/0errors.
- pwsh -NoProfile -File run-deep-remaining-03.ps1 (task evidence path): current
  conformance and incremental consumer solution build pass. Canonical coverage args
  run each affected Release/no-build/no-restore test project with eng/coverage.settings.
  Application1080/0fail reaches100%; Infrastructure1026pass/20explicit environment skips
  reaches94.6120689655%; Presentation461/0fail reaches96.9943644333%. All thresholds pass.
  Three adversarial repetitions and Complete mutation95/90/90 follow on this frozen
  candidate; they are not yet declared passed. Driver17172 starts15:43:47JST.
- pwsh -NoProfile -File run-codeql-manual-02.ps1 (task evidence path): driver39572
  starts15:44:26JST, copies exact inputs, traces a required compiler extraction build,
  analyzes the164-rule suite, then separately creates/analyzes the canonical buildless
  database. All outputs stay local. Original and corrected raw SARIF remain distinct.

Manifest deep-inputs-03.json freezes1317inputs; deep-input-differences-03.json retains
all10byte-level changes versus20336c8, including the three XML-equivalent resources.
Domain mutation/adversarial and dependency audit retain their exact mapped inputs.
Passed earlier stages with unchanged relevant inputs are reused under ADR0061; the
failed original gate/CI is never relabeled green. No source edits occur during these
runs. Documentation is outside this frozen product/tooling/test input set. Physical
input/IME/visible/speech/high-contrast and real Explorer selection remain unproved.
#531 source publication/integration and beta.4 analysis-upload approval remain separate.
All unfinished worktree/output paths are retained; no completion cleanup is eligible.

## Analysis readback and Application mutation — November24 16:19 JST

Current compiler-traced CodeQL completes164rules/126raw results:122generatedobj
and4intentional native-call notes. All8prior owned expression/test findings disappear.
There are no compiler/extractor error notifications. The raw generated results and
unsupported path-filtering notice remain; this supplemental mode is not the canonical
buildless workflow. Canonical-mode attempt02 has1result in unchanged RecursionGraph,
342compiler errors and31error-level extraction diagnostic notifications. Its missing
Roslyn/reference metadata makes it insufficient standalone evidence. Seven existing
managed references, including the pinned SDK's two Roslyn assemblies, are copied with
assembly identities and exact hashes into the analysis copy only. No source, package,
query, exclusion or threshold changes. Resolved attempt03 reports164rules/0results,
with62compiler errors and12error-level extraction diagnostic notifications still
explicitly retained. The unrelated RecursionGraph result disappears with references,
not a source workaround. codeql-comparison-review-03.json indexes all three raw results.

The resolved buildless result is not complete extraction, canonical acceptance or
API zero-open proof. Keep its remaining diagnostics alongside compiler-traced source
proof; candidate-ref processing/alert verification remains separate and unsent.
At16:14 independent read-only review verifies the seven-file implementation/test diff
and all1317 frozen hashes, with no mandatory correction. In particular, the sole
AppearsOn caller is preceded by CommandPaletteSession.Open's existing null rejection;
the final branch is the third closed surface, so removing its redundant guard does
not weaken the admission boundary. No review edit or test run occurs.

Application Complete mutation finishes16:13:39JST, exit0, score96.4196502914% against95%.
All3425mutants are retained: CompileError481, Ignored542, Killed2293, NoCoverage2,
Survived84, Timeout23. Time27m25s. All three affected adversarial repetitions passed
before this tier. Infrastructure mutation is now running under the original driver17172;
Presentation remains queued. Do not duplicate quiet long-running tests: read the exact
process chain and final receipt/report. No complete deep/CI result is yet claimed.

## Infrastructure mutation completion — November24 16:56 JST

The original remaining03 driver completes Infrastructure with exit0 at16:56:39,
elapsed42m59s. Independent raw-report counts:2960total,593CompileError,398Ignored,
1804Killed,63NoCoverage,89Survived,13Timeout;1817/1969detected gives92.28034535%
against90%. Original logs and all status classes remain, with report SHA256/counts
in infrastructure-mutation-summary-03.json. This does not relabel the earlier failed
canonical run. Presentation starts at16:56:40 through the same sequential driver and
is still running at17:04. No source input is changed or duplicate verification run.

## Final affected mutation and assembled stages - November24 17:15 JST

The original sequential driver finishes Presentation at17:11:54JST with exit0,
elapsed15m14s. Its2917raw mutants are239CompileError,341Ignored,2131Killed,
17NoCoverage,161Survived and28Timeout. Thus2159/2337detected is92.3833975182%,
above90%. Application96.4196502914%/95 and Infrastructure92.2803453530%/90 also
pass. Domain97.4747474747%/95 is reused only with the exact unchanged Domain and
runner input mapping in deep-reuse-inputs-01.json. All raw status classes and
reports remain; safe-mode compilation diagnostics are not suppressed.

The driver verifies all1317frozen inputs again after the last stage. An independent
parent read at17:14 confirms the same count/hashes and recomputes all three scores
from raw reports. final-stage-summary-03.json indexes report hashes, denominators,
thresholds and the remaining CodeQL limit. The first console table selected fields
from OrderedDictionary objects and displayed blanks; the stored JSON is correct,
and a structured read confirms each value without repeating any runtime test.

The assembled evidence now covers the canonical component stages and affected
adversarial/mutation stages: initial negative gate and security proofs; corrected
XML-equivalent theme parity; locked restore, format, build, record closure and
ordinary tests; final affected build/coverage and three adversarial repetitions;
current three mutation tiers; and exact-input Domain/audit reuse. The original
canonical01 and remainder02 failures remain failed. This is stage-by-stage reuse
underADR0061, not a successful invocation of the whole original gate or CI.
Ordinary tests02 preceded only the redundant closed-surface guard removal; the
current Application1080, Infrastructure1026/20environment skips and Presentation461
consumer cases all pass under coverage03 at100%,94.6120689655% and96.9943644333%.

The last changed guard receives a focused whitespace check after mutation ends:
`dotnet format whitespace NeNeCommander.slnx --no-restore --include src/NeNeCommander.Application/Commands/CommandMenuGroup.cs --verify-no-changes`
exits0. `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` exits1
with exactly the same4findings, including identical paths/locations/values/limits,
as menu-complexity-04.json. The raw current report is final-complexity-03.json.
No threshold, suppression or baseline is introduced; this is not a clean complexity
claim. CodeQL still has the documented incomplete canonical-mode extraction and no
candidate-ref processing/open-alert proof. Technical acceptance, physical/visible
acceptance, real Explorer display, publication and main integration remain pending.

Independent final stage-evidence review is requested read-only. No source changed
through the run, no duplicate mutation was started and no external write occurred.
All unfinished copies and evidence remain. Read-only continuation also recorded
absent guessed filenames and an initial sandbox-denied process query; the explicit
owned-process query succeeded and observed active owned test hosts before normal
completion. No approval rejection or cleanup hold was introduced.

Independent read-only review completed2026-11-24 17:17:01JST.
No mandatory correction, arithmetic error or input-mapping mismatch.
Raw mutation scores: Application96.4196502914%, Infrastructure92.2803453530%,
Presentation92.3833975182%; hashes/counts/thresholds match, all1317inputs unchanged.
Domain/audit reuse is limited to unchanged-input successful stages. Original
canonical01/remainder02 failures remain. CodeQL extraction limits, no candidate-ref
processing and remaining environmental proof are explicit; no complete deep or
technical acceptance is claimed. Reviewer performed no edit/build/test/send.
