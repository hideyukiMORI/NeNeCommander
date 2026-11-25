# Pane name-filter investigation — 2026-11-25

Status: PR588 integrated; evidence preserved and completed cleanup verified; physical acceptance remains separate.
Issue545 is confirmed open through the GitHub connector. The active milestone includes
basic current-folder filtering; recursive search536and frozen beta.5remain separate.

Worktree D:/NeNeCommander/worktrees/issue-545; branch feat/545-pane-name-filter.
Created from selection2cfc3b2, then combined reviewed release correctionb72bbcc only in
this task. One PROJECT_STATE conflict consists of adjacent entries; both are retained.
basis-parity-01.json proves all1367selection inputs either unchanged or exactly one of
the six reviewed release correction inputs. Normal merge commit hooks run; no product
behavior or unrelated tests are added by that source combination.

The actual restricted Claude invocation calls Skill(design). No ToolSearch call is recorded; available-tool discovery is therefore not independently established.
Installed design is access management only; no access/settings/project write occurs.
It authors ordinary advice and local pane-filter.html from the authored brief only,
without repository source. Raw invocation and brief are retained under
D:/NeNeCommander/evidence/pane-filter-20261125; local artifacts under the matching outputs.
No cloud Canvas/share URL or approved final visual handoff is claimed.

Adopted proposal: inline bar below list; native committed text only; / entry; Enter
keeps query and returns to list; editor Escape clears; list Escape clears marks first,
then filter. Engineering keeps whole localized/accessible count text and rejects an
unqualified zero-eligible-means-empty claim when hidden/incomplete rows may exist.
Matching is literal ordinal-ignore-case AND terms with no Unicode normalization or
path-identity change. No recursive/per-key provider read, timers or settings persistence.

Inspection finds296logical lines in CommanderSession and two existing5-parameter
scope-carrier findings. ProposedADR0094 records the required cohesive bookmark
coordination extraction, pane-scope composition grouping and single new edit owner,
while PaneReducer remains the sole filter/selection/focus transition. The exact
native blur/modal handoff is a review focus before implementation. Source reads with
misspelled inferred filenames are corrected read-only setup misses, not product failures.

No Issue/body publication is attempted. Prior release/selection approval questions
remain pending for their exact payloads. No cleanup is due for this unfinished task;
keep all worktrees/artifacts and native trials. Shared desktop/normal settings stay
untouched. Next: independent read-only mechanism review, then selected causal proof
and implementation while monitoring the separate beta.5mutation/CodeQL drivers.

## Application ownership and initial selected proof

The mechanism review accepts PaneReducer as the single visible/query/selection owner,
PaneFilterSession exact editing capture, and the cohesive CommanderBookmarkCommands
extraction plus PaneScopeOwners constructor grouping. The root retains precedence
and every existing file-operation route. No provider, settings schema, dependency,
release source or threshold changes. The native host sequence remains a proof focus.

The literal query, reducer and pane lifecycle are implemented with tests. The initial
red03 stage retains actual failures before query behavior. Foundation03 succeeds,
and scope03/04 succeed after the composition migration and exact editing tests.
Earlier missing namespace/constructor compile attempts remain in their raw logs.
Native committed text rebinds the editor to the accepted pane snapshot. Old text,
commit, cancel and blur callbacks cannot modify a replacement owner. Blur preserves
query and never emits a list-focus effect; Enter preserves query, and editor Escape
clears it through the same reducer. Header requests carry an exact pane snapshot.

A read-only review identifies that new ApplyNameFilter originally used IdleWith,
which removed a cancelled/failed/abandoned read qualifier without another provider
read. Three causal tests fail in retained-red08. The correction uses WithContent
for query changes, including list Escape clearing a query; ordinary pre-existing
non-query state transitions retain their former behavior. Retained09 passes all 86
selected filter/scope/navigation/abandonment cases, with a 0-warning/error build.
The query operations retain exact Activity, Listing and provider request counts.

## Keyboard and incremental projection proof

Five PaneFilterKeyboardTests fail before binding/guard implementation in keyboard-red06.
Initial keyboard-red05 fails setup because Presentation project.assets.json is absent;
locked restore06 fixes that setup without changing a dependency. The sole mapper now
maps produced slash only in FileList to OpenPaneFilter and maps filter Enter/Escape to
Confirm/Escape. The existing KeyboardRepeatGuard keeps opener and exit presses across
scope changes. Native text, Space, Ctrl+A, star, slash and navigation remain native.
Observed IME Enter/Escape use the existing native-input ownership hook. Keyboard07
passes 18 selected filter/selection/mapper tests, with 0 warnings/errors. This is
translator/mapper evidence, not actual IME or physical keyboard proof.

The new same-count projection test fails in projection-red10: filtering a to b still
shows the old a row because the existing reuse predicate assumes equal visible counts
mean equal members. CanReuseRows now also requires the same exact query text. The
existing listing/sort/count conditions still avoid rebuilding ordinary focus/selection
updates. Projection12 passes 45 filter/listing/count tests and a 0-warning/error build.
Projection11's two MSTEST0017 compile errors on a property named ExpectedState remain;
explicit ReferenceEquals assertions preserve the intended exact-owner proof.

The new immutable filter presentation derives text only from PaneState, requests edit
focus only on entry and list focus only once on explicit exit. It retains no second
query and emits an explicit no-match hint without calling the folder empty. The
existing count strip remains authoritative, including bounded/retained qualifiers.
Palette open/clear entries, F1 grouping and all three locales use canonical labels.
Inline Enter/Escape text resolves its key caps from the sole mapper.

## Native host connection and selected remaining scope

Two inline controls reuse existing semantic tokens. TextChanged forwards only committed
text with its exact editor; render-assigned text and composition intermediates do not
apply. Native blur carries exact ownership and no focus request. Before a qualified
pane/header or existing command-button handoff, RenderIntentAsync checks the captured
editor against current state, dispatches its qualified Blur, checks the returned closed
owner against current, then dispatches the original intent without intervening render
or native focus. Text/Commit/Cancel never take this handoff. Render-owner and stopping
guards prevent superseded focus callbacks. Native buttons use the existing CommandEntry
context so blur to a button does not schedule list-focus theft or map Enter to file open.

App restore13 is locked. Native build13 fails on one unused using; that exact diagnostic
is retained and removed without suppression. Later host-build results follow separately.
Actual native focus/blur/composition and narrow layout remain unproved at this checkpoint.
No shared-desktop input or normal-settings mutation occurs.

ADR-0061 selection for completion: all Application and Presentation tests with coverage
because the root constructor/precedence and shared visible projection change, plus their
App build, relevant Architecture consumers, record closure, conformance and changed-file
format/complexity inspection. Domain/Infrastructure/provider contracts and dependencies
are unchanged; unrelated provider environment suites and full-layer mutation are not
selected for this non-release feature. Native proof uses only owned non-input desktop
processes and private settings. Beta.5 remains frozen and excluded from every filter edit.

Exact new stage commands (each stage has separately retained build/test logs):

- keyboard-red05/06: `dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore`, then `dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~PaneFilterKeyboardTests`; 05 setup fail, 06 build0/test2 with five red cases. 06 first runs `dotnet restore ... --locked-mode`.
- keyboard07: same build, test filter `FullyQualifiedName~PaneFilterKeyboardTests|FullyQualifiedName~SelectionKeyboardTests|FullyQualifiedName~KeyboardIntentMapper`; exit0, 18pass.
- retained-red08: Application build, test filter `FullyQualifiedName~ApplyNameFilterWhenReadRetainsOldListing|FullyQualifiedName~ApplyNameFilterWhenReadIsAbandoned`; build0/test2, three red cases.
- retained09: Application build, filter `FullyQualifiedName~PaneNameFilter|FullyQualifiedName~PaneFilterScope|FullyQualifiedName~PaneSessionNavigationStateTests|FullyQualifiedName~PaneSessionAbandonmentTests`; exit0, 86pass.
- projection-red10: Presentation build, filter `FullyQualifiedName~PaneFilterProjectionTests`; build0/test2, one red case.
- projection11/12: Presentation build, filter `FullyQualifiedName~PaneFilter|FullyQualifiedName~PaneListing|FullyQualifiedName~PaneCounts`; 11 build1 and no tests, 12 exit0/45pass.
- native13: `dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj --locked-mode`, then `dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore`; restore0/build1 (unused using).

All commands run in the issue-545 worktree. Raw logs live in the declared evidence root;
TEMP/TMP point to the owned D output proof-temp directory. Failed attempts are never
relabelled successful. Publication, native acceptance, integration and cleanup are pending.

## Integration coverage, ownership and native focus

Stage15 builds the changed layers and App/Architecture successfully;33affected
architecture cases, conformance and record closure pass. Five failures are stale
catalog/key expectations (31to33commands,58to59FileList bindings, new final command
and slash mapping). Initial Application branch coverage99.578947% is below100%;
Presentation96.993761% is not accepted while tests fail. New boundary cases cover
mismatched composition owners, unlisted/closed editor validation, clear under another
owner and a current clear surface. Stage16 retains two test analyzer errors; stage17
reaches Application100% but a new assertion incorrectly compares the freshly allocated
DualPaneSnapshot container. Stage18 retains an incorrect test member and App IDE0046.
The final assertions compare owned pane/operation references. Stage19 passes1,267
Application cases with100%branches; Presentation17 passes567 with97.399661%branches.
The normal App19 build has0warnings/errors. No rule or threshold changes.

CS01315 identifies new Window/type, mapped-input and pane-transition complexity.
Native focus classification and actual row/ListView Focus move together into the
stateless CommanderFocusView, with no session/async owner. PanePointerMapper gets the
unchanged pure focus predicate. ForwardMappedIntent preserves precedence, and
PaneSession.ApplyReducerTransition preserves query-only terminal-activity retention.
Existing Help nesting and selection-test cognitive findings remain separate. Final
measurement follows the last correction; no zero-findings claim is made yet.

Independent read-only review identifies a real nonempty-query Enter-focus defect:
the generic pointer predicate rejects the still-focused native TextBox's PaneFilter
context after its editor closes. Native attempt1 reproduces the closed scope with
TextBox focus after9passed assertions. The correction carries the exact closed render
owner into PaneFilterPresenter.FocusTarget. Only its current active pane and available
native context may receive focus; blur/replacement/later same-side exit/other text or
modal owner cannot borrow it. Related26Presentation cases and normal App build20pass.
The unchanged native Enter reproduction then passes14checks in attempt2.

The diagnostic copies185App inputs and freezes four normal product DLLs. Explicit
composition substitutions use private pane paths/settings, bounded lifetime and a
recording IFileLauncher, preventing real external launch even after a failed assertion.
Its standalone lock retains19package versions/hashes; product locks do not change.
Only copied diagnostic analyzers are disabled. Every child checks the named non-input
desktop NeNeCommander545Filter_20261125; no SwitchDesktop/SendInput is used. Receipts
preserve the exact child identity and normal settings/input-desktop before/after.

Expanded native attempt3 fails its late hint assertion without preserving the actual
hint/query values. Diagnostic detail in attempt4 accompanies64passed checks and actual
Japanese wide/narrow images, including settings-button blur, both-pane header handoff,
stale callbacks, clearing and stopped behavior. The first failure is not erased or
claimed explained by a passing retry. Attempt5 reaches a listed snapshot before startup
owned work settles and does not open the filter. Waiting for AsyncWorkOwner idle fixes
that fixture assumption; copied build06 then fails on two diagnostic pattern-name
collisions, corrected in07. Attempt6 opens successfully but observes uncommitted text
at its next tick. Diagnostic08 records ordered native TextChanged, render assignments,
editor/depth and refused-intent events to distinguish event timing from lost text.
Native three-locale proof remains pending. Direct composition callback injection proves
only guard wiring, never actual IME composition or physical input.

Commands: run-completion15/16/17 and19 retain exact build/coverage/selected-architecture/
conformance/complexity receipts. Record closure15 is reused because later changes add
no record case. Focus20 builds Presentation, tests
FullyQualifiedName~PaneFilter|FullyQualifiedName~PanePointerFocus, and builds App.
Diagnostic restore/build01..08, frozen-input manifests and run-native-controls01/02
retain every copied-source substitution, failed setup and launch observation. No
publication, integration, cleanup, visible acceptance or complete native proof is claimed.

## Pending native text: causal failure and corrected single exit contract

Native trace attempt7 fails in the diagnostic observer because PaneSide.ToString
recurses through Other; this is corrected only in that copied observer to explicit
Left/Right labels. Trace attempt8 passes64checks. A deliberate minimal attempt9 then
proves a product defect independently of timer cadence: native Text is a.txt while
the applied query is still empty, and ordinary RenderSession resets native Text to
empty before TextChanged. The before/after values and Render.TextAssignment event
are saved. Earlier text/hint timing failures remain retained rather than relabelled.

Ongoing editing now preserves native text. PaneFilterPresentation.SynchronizeText
permits writing the native Text property only when not editing or entering a new
editor; composition remains protected. Enter/blur can also precede deferred text
notification, so the existing PaneFilterEditSubmission carries mandatory RawText
captured at exit. No second query owner or apply route is added. Exact current
Commit/Blur applies that query through the existing reducer before closing; Cancel
ignores RawText and clears. Exact stale-pane blur retires without applying text.
The control reads native text only when its rendered.Editor matches the expected
owner and composition is inactive; otherwise it uses that owner's accepted query.
The existing host handoff carries this qualified blur submission before the external
intent, checks its closed receipt and never retargets a now-stale pane interaction.

Independent read-only review accepts this mechanism with those precise guard
requirements. Application red23 has2failed pending Commit/Blur cases and1passing
Cancel/stale case. Green24 passes the63previous and3new filter cases; Presentation
build succeeds. Complete affected-layer proof25 then passes Application1,273 with
100%branch coverage and Presentation570 with97.42296918767507%. App/Architecture
build has0warnings/errors; selected composition cases, conformance and refreshed
record closure pass. Complexity25 retains only the two existing findings (Help
nesting4; selection translation test cognitive11), with exit1 explicitly retained.
The former trivial palette-forwarding wrapper is folded into its existing outcome
switch, preserving precedence and bringing CommanderWindow below its type ceiling.

Whitespace format22 applies only76changed C# files; it succeeds with a workspace-load
warning retained. Later edits still need final changed-file format verification.
Operation21 independently passes63filter-related cases including actual gateway
copy subjects, no-match deletion admission and frozen confirmation subjects.
ADV-013/014/016 mappings and selected adversarial attributes cover the new scope.
The latest contract needs fresh native pending Render/Enter/Blur/Cancel proof,
composition/stale callbacks, three locales and final independent review. Earlier
native success is not represented as proof of the later RawText change.

## Final native ordering, locale inspection and measurements

Fresh native-controls-03 copies the latest 185 App inputs after RawText/SynchronizeText.
The 182 unmodified inputs match exactly; only composition/private paths/recording
launcher, standalone project references and diagnostic lock differ. Four normal product
DLLs match original, frozen and runtime files. Lock comparison is keyed by target and
package: all 19 target/package records match, representing 16 distinct package names.
The normal product retains every analyzer; only the declared diagnostic copy disables
them. final-inputs-29.json freezes 1,398 relevant source/test/tool/config inputs.

Japanese attempt 10 passes 65 checks including the same pending-render causal scenario
that failed in attempt 9. Attempts 11–16 pass pending Enter, Blur, Cancel, composition
Blur, opposite-pane handoff and same-pane stale request refusal. Each starts with native
Text=a.txt while the applied query is still empty, and checks late old text/blur cannot
reopen a closed/replaced owner. Reading the wrong control and composition fallback are
also checked. Composition callbacks are injected guard proof, not an actual IME.

Attempt 17 retains a failure: Focus and Invoke were called in the same turn and the
fixture incorrectly expected LostFocus to have completed. Attempt 18 adds observations
and a separate later Click, without a product change: after Focus the editor is still
Editing/query empty; actual LostFocus closes and applies a.txt; the later Click observes
the updated capture and clears it. All 20 checks pass. The same-turn pre-flush stale
request remains deliberately refused, not retargeted. This does not claim physical
mouse timing or broaden accepted stale input.

English and Simplified Chinese attempt 1 each pass 64 checks. Japanese attempt 10,
English 1 and Chinese 1 produce real wide (1,200 DIP requested) and narrow (760 DIP
requested) RenderTargetBitmap images at scale 1.25. Root visual inspection of all six
images finds no overlapping/clipped filter labels, input/clear controls or complete
count/no-match guidance. Native labels and automation names use the selected locale.
All 10 successful final runs preserve the input desktop and normal settings hash;
no SwitchDesktop/SendInput occurs. Native processes exit normally and no shared input
is injected. Physical keys, real IME, Narrator and hide's visible acceptance are unrun.

A separate public-API fixture measures parse plus PaneReducer.ApplyNameFilter using
prebuilt bounded listings with every entry selected. It creates no files/provider
requests and includes no XAML projection/render or complete keystroke latency. For
100/1,000/10,000 entries, four literal query shapes each have eight warm-ups and 41
samples; exact visible/selected counts are asserted after every sample. At 10,000,
median is 0.20–5.31 ms, p95 is 0.22–7.67 ms, and the largest observed sample is 11.80 ms.
The all-match case allocates a median 1,587,272 bytes per transition. These are local
observations, not a latency gate or benchmark guarantee. Raw samples and fixture source
are retained as measurement-27.json and measurement-Program.cs.

## Final selected verification and explicit limits

Stage 26 runs only TestCategory=Adversarial&FullyQualifiedName~PaneFilter in the two
affected test projects three times: Application 8/8 each and Presentation 3/3 each.
Stage 25 full-layer successes remain valid because product/test inputs are unchanged.
A supplemental broad `dotnet format ... --verify-no-changes --include` attempt returns
2 with workspace-load warnings, unchanged event-handler parameter/simplification
findings, and a design-time PaneFilterView CA1812 finding despite its explicit normal
App construction. This failed supplemental command is retained. The canonical format
command in eng/check.ps1 is `dotnet format whitespace`; stage 28 runs that exact mode
with --no-restore --verify-no-changes and all 77 changed C# files, exit 0, retaining its
workspace-load warning. No diagnostic is suppressed and no failure is relabelled.

The initial explicit restore logs did not state Configuration=Release. Stage 30 closes
that evidence gap with `dotnet restore <Application/Presentation/Architecture test
project> -p:Configuration=Release --locked-mode` and a normal App Release --no-restore
build. Relevant source/locks and the four frozen runtime dependencies are rechecked
before reusing stage 25 and native proof. No broad product retest is caused by a
configuration-receipt correction alone.

Final independent read-only review at 12:42:55 JST requires no further correction and
confirms exact exit ownership, single query/selection projection, scope precedence,
native ordering and 185/182/4 input parity. ADR-0094 records technical acceptance and
command/keyboard/glossary contracts are updated. No dependency, schema, provider,
threshold, release source or automatic CI change occurs. Publication/main integration
are not claimed, and no task copy is eligible for cleanup while this work is pending.

Exact command receipts: run-completion-25.ps1 and completion-progress-25.json;
adversarial-26.json; format-verify-26.json (failed supplemental); format-whitespace-28.json;
restore-release-30.json; run-native-controls-03.ps1 with the locale/attempt/scenario
above; native-controls-build-11/12/13.log; native-summary-29.json; native-parity-29.json;
measurement-build-27.log and measurement-27.json. All reside in the declared permanent
evidence directory. The native fixture revisions and every failed attempt are retained.

Stage 30 completes all three Release locked restores and App build with exit 0 and
0 warnings/errors. final-reconciliation-32.json confirms all 1,398 relevant inputs
and four normal/frozen/runtime DLL hashes remain exact after those restores/build.
All new document links resolve. Successful layer25, adversarial26 and native proof
are reused on these unchanged inputs. No runtime or test source changed after proof.

The complete 91-file feature is saved locally as 08008241aecdafaeed6e3989c9c5be0edb04107f. Normal Commit-mode conformance/security/message hooks pass and the worktree is clean. local-commit-32.json/log preserve the exact result. No public update, PR, main integration or cleanup is performed. This documentation-only follow-up reuses all unchanged product proof.


## November 26 accepted-main reconciliation

Selection PR585 and its completion PR587 are integrated; main is f44f6f0. Issue545
remains open. The existing D worktree keeps original branch feat/545-pane-name-filter
at27b92cd and starts feat/545-name-filter-integration from accepted main. Only feature
0800824 and documentation27b92cd are cherry-picked, yielding16e6c3d/1eea87a.
One PROJECT_STATE insertion conflict retains all main and filter history in date order.
There is no product conflict. The feature continuation runs normal Commit conformance112,
security24 and message checks successfully. The resulting tree is clean.

input-correspondence-01.json maps all1,398 frozen inputs:1,397 match. The sole difference
is SelectionTranslationTests.cs, the already accepted selection correction that separates
identical native-owner/navigation cases into DynamicData. Every production/configuration/
tooling input matches the prior accepted filter proof. A complete src/tests/eng comparison
against27b92cd likewise identifies only that test. No new product fix is applied.

product-proof-reuse-01.json verifies all185 App sources and all four original normal,
frozen and native-runtime dependency DLLs against native-parity-29. The pre-existing
Application1273/100%branch, Presentation570/97.422969%, Architecture35, normal App,
closure/conformance/format, selected three-repeat adversarial, native ordering/locales/
six-image inspection and local reducer measurement remain evidence for matching inputs.
These are reused results, not new executions or a claim of a newly measured aggregate.

The changed test is compiled with all normal compiler/analyzer settings and existing
exact product references, then run against the filter's new keyboard context:
- dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore -p:BuildProjectReferences=false
- dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~SelectionTranslationTests

The test build has zero warnings/errors; all59 cases pass,0failed/0skipped. This avoids
rebuilding unchanged product dependencies, whose four normal hashes remain exact after
the test. The dynamic context data includes PaneFilter. The prior570-case coverage
result is not relabelled as a new full-suite result after test parameterization.

pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect returns1, retaining
exactly the prior KeyboardHelpPresenter nesting4/limit3 finding at line136. The old
test cognitive11 finding is gone with the already accepted parameterization. The full
remaining finding object matches the prior record. No threshold or suppression changes;
the result is explicitly not clean complexity.

run-verification-01.ps1, verification-01.json, verification-summary-01.json and their
logs retain precise commands, timings, exits and hashes under
D:/NeNeCommander/evidence/pane-filter-20261126. A sandbox-only Python PATH miss was a
read-only inspection setup error; PowerShell reads and the existing approved execution
environment complete the audit. Inferred missing-file reads and reading a receipt before
its producer finished caused no source change and no product failure.

Independent read-only integration/evidence review at 08:14:39 JST requires no further correction and accepts the declared reuse scope. No
new native invocation, product build/full suite, coverage, mutation, release or shared
desktop input occurs in this resumption. Physical input, actual IME, speech and hide's
visible acceptance retain their prior explicit limits. Preserve the unfinished worktree
and all output until integration and verified permanent preservation permit cleanup.


## November 26 publication, preservation and completed cleanup

Normal Commit-mode conformance112, security24 and message checks pass for5dd10db.
Draft PR588's body, head and base match the saved payload on mainf44f6f0. It is
mergeable/clean without applicable failing checks. After Ready, expected head
5dd10db45d9f0b3768083fb7c58ddea0347fed11 is squash-merged at08:19:43 as
ad85de1ce354c9e8ccef9cb05c5621b83eb53a5c. Issue545 is closed. The complete candidate
and accepted trees match; primary main fast-forwards cleanly. No release is created.
Authenticated gh writes use the fallback established after this session's earlier
GitHub connector HTTP403; no automatic-approval rejection occurs for this work.

Issue589 owns completion documentation and administrative cleanup. Exact targets:
D:/NeNeCommander/worktrees/issue-545 and D:/NeNeCommander/outputs/pane-filter-20261125.
Original/integration branches, commits and raw evidence remain. All9,187 ignored/output
files,1,146,169,557 bytes, are saved in
D:/NeNeCommander/evidence/pane-filter-20261126/completed-filter-artifacts-01.zip.
SHA256: cd36e9aa5c2fe1a03a20c4b84963a765de0cd3be20a03bc3f286fd55b559fb2a.
Every saved entry is individually size/hash-verified. The exact inventory and source
metadata are unchanged before removal; clean source, accepted main/tree equality,
absolute parents, zero links and zero active references are verified. Raw evidence
in pane-filter-20261125 and pane-filter-20261126 remains permanently.

Exact commands:
- python D:/NeNeCommander/evidence/pane-filter-20261126/archive-filter-01.py
- pwsh -NoProfile -File D:/NeNeCommander/evidence/pane-filter-20261126/cleanup-filter-01.ps1

Both exit0. cleanup-completion-01 confirms both targets absent at08:50:51JST; every
unfinished task, including560, remains. No cleanup hold or user action is pending.
A preceding inspection's terminal rg exit1 means no matching project references,
not archive failure. The first completion-writer template failed before any tool
execution or repository edit because its timestamp placeholder was interpreted as
a JavaScript variable; the corrected data-only payload preserves that failed attempt.

Receipts: publication-plan-02, pr-readback-02, pr-merged-readback-02, main-sync-02,
completed-task-scope-01, completed-archive-manifest-01, archive-filter-command-01,
completed-artifact-preflight-01, cleanup-preflight-01, cleanup-command-01 and
cleanup-completion-01, all JSON under evidence/pane-filter-20261126.
Completion changes documents only; matching successful product/native proof is reused.
No product build/test/CI is repeated. Physical keys, actual IME, Narrator and hide's
visible acceptance remain unrun, and no new release or human acceptance is claimed.
