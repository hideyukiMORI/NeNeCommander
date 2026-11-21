# 2026-11-20 Bookmark navigation stability — Issue #495

Status: selected source and native proof complete; PR402 hide screen acceptance remains. Source baseline 76a281a.

## Authorized work and invariant

hide resumed milestone work and broad speed/stability improvement on 2026-11-20.
The immediate feature follow-up is #495 on the unaccepted PR402 branch. Preserve
all current bookmarks/settings and the sole key map, session and focus owners.
Stable browse row sources must retain complete stale-action captures and update
only changed visible rows. Startup/Bookmark exit restore the active file list.
The inherited ADR0044 amendment records Ctrl+L-only entry; this session retains
that unfinished change and validates it. PR402 still requires hide's acceptance.

## Initial inspection

Main is clean at 1627d55; feature PR402 is Draft at 76a281a. Issue495 has five
tracked and two untracked changed files, copied byte-for-byte into
`D:/NeNeCommander/evidence/bookmark-navigation-20261120/inherited-0722/`, plus
`inherited-tracked.patch` and SHA256 inventory. No running App was returned by the
initial process read. Do not restore the earlier 218-byte settings snapshot:
hide used the feature after the previous daily closure and may have saved data.

Repository access initially hit Git dubious-ownership under the sandbox user;
command-scoped safe.directory permitted inspection without global configuration.
The first GitHub search used unsupported optional arguments and was corrected.
Some large document reads were truncated, so relevant ranges were reread. A
missing guessed focus-test filename and two literal wildcard rg paths produced
read-only errors; no successful tests are claimed from those commands.

## Selected verification

ADR0061 ordinary UI correction: Presentation row/source tests, bookmark/focus/key
mapping and address tests; Application direct address/bookmark consumers; App
Release build; visual composition ownership; changed-file format and Commit
checks. Native owned-window checks cover startup, category movement, close, j/k,
Tab, Ctrl+L and deliberate text/IME ownership. Settings hashes must stay equal.
No full CI, coverage, all-layer mutation or release is selected for this task.

An independent read-only review identified reentrant focus events during native
collection notifications, selection identity after retained row comparisons, and
the Ctrl+L fast path while a read-only address happens to hold focus. Corrections
and exact proof follow below; source hypotheses are not measured UI latency.

## Work journal

- Prepared seven row-source cases: six inherited drafts, with the invalid-null
  boundary injected through reflection instead of a nullable suppression, and a
  new focus-region case requiring unchanged unselected rows to retain identity.
- Next: compile/run the new case against the inherited draft, connect the stable
  source to the view, correct focus boundaries, and run the selected proof.

- Initial Release restore passed. The first test build stopped at inherited
  IDE0305 and CA1861 style findings. Both were corrected without suppression.
  `run-rows.ps1 -Phase row-red2` then built with zero warnings/errors and ran
  seven cases: six passed, the new region-focus case failed because an
  unselected category object was replaced. This red concerns the inherited
  incremental-source draft, not a claim of native flicker reproduction.
- Connected the view to one pair of stable observable sources. Both GotFocus
  paths are guarded during synchronous collection updates; native selection
  uses objects from the retained sources. Comparisons include complete action
  captures and displayed marks, excluding invisible marks of unselected rows.
- Ctrl+L's existing fast path now applies only to an editable address. An
  incidentally focused read-only address uses the file-list context.
- Added a current-source selection/capture case (eight row cases in total).
- Current settings are 428 bytes, SHA256
  `7EA7AB2FB5B06A68D6015500A227BA7AEA3B7F1DA322430834B4B1361B2630EF`.
  The original candidate App DLL matches the prior recorded hash. GitHub
  returned no milestones, including closed ones; priority follows the open
  issue/feature records. A baseline native launch returned no targetable
  window, and one refreshed inventory still found none; no UI input was sent.
  Process 6760 was returned with unreadable executable path. No success or
  latency inference is made from this environmental failure.

- `run-rows.ps1 -Phase row-green`: Release build zero warnings/errors; eight
  tests passed, zero failed/skipped. The unchanged Presentation input-boundary
  set passed 45/45 through `run-focused.ps1`. App build then stopped on IDE0046
  in the address-context conditional; the equivalent expression was simplified
  without changing its conditions. Successful restore and 45-case proof are reused.
- Independent final source review accepts the stable-source integration,
  native-input cancellation synchronizes Search marks, address ownership, and
  captured action identity, with no remaining required correction. Native
  behavior remains a separate unexecuted condition.
- Baseline process 6760 later acquired a native window handle. A cleanup guard
  retained it; the subsequent scoped termination request was rejected before
  execution by automatic approval because a window might contain live state.
  It is retained pending hide response, with no workaround/retry. The exact
  rejection is in `process-cleanup-rejection.txt`; unlocking and process
  disposition were asked asynchronously without stopping independent work.

## Focused source results and remaining native acceptance

The selected behavioral proof comprises eight row-source cases, 45 Presentation
bookmark/focus/address boundaries, 19 direct Application address/bookmark cases,
and 24 view ownership/constructor cases. All pass with zero failed or skipped.
The corrected App, Application test and ownership test builds have zero warnings
and errors. Changed-file whitespace checks return zero; the App check reports a
workspace-loading warning, so its output is retained rather than called warning-free.

The initial CS-013 inspection returns six findings, including cognitive complexity
14 in the changed native keyboard-context method. That method now expresses the
same modal precedence as an ordered switch, delegating only focused-control
classification to a named view method. The new focus-restoration boolean parameter
was also removed; the caller's captured-focus condition controls the same call.
The two intermediate builds stopped at IDE0046 conditional-expression style rules.
Both unsuccessful logs remain; no test success is inferred from them. Final build,
format, complexity and Commit-mode results are appended after completion.

These last edits do not change Presentation, Application, test code, dependencies,
constructors or session/work ownership. Their successful 96 cases remain relevant;
only the affected App build and static checks are repeated. Native behavior is
still unobserved. Neither source equality nor the pure collection cases establish
that hide's reported flicker or sub-second delay is gone.

Commands were executed from `D:/NeNeCommander/worktrees/issue-495`. Complete
argument arrays, exit codes and timestamps are retained beside the raw logs in
`D:/NeNeCommander/evidence/bookmark-navigation-20261120/`:

| Command record | Scope and result |
| --- | --- |
| `run-rows.ps1 -Phase row-red2` | Release build succeeds; one regression fails and six cases pass |
| `run-rows.ps1 -Phase row-green` | Release build succeeds; eight cases pass |
| `run-focused.ps1` / `focused-commands.json` | 45 Presentation cases pass; Release locked App restore succeeds; first App build fails IDE0046 |
| `run-focused-corrected.ps1` / `focused-corrected-commands.json` | App build, explicit Release locked restores/builds, 24 ownership and 19 Application cases all succeed |
| `run-static.ps1` / `static-check-results.json` | Changed App/row whitespace exit0; initial complexity exit1 for six findings |
| `run-final.ps1`, `run-final2.ps1` | Intermediate equivalent source refactors fail IDE0046; separate logs retained |
| `run-final3.ps1` | Final equivalent context/refocus structure; results appended below |

`run-rows.ps1` uses `dotnet build` and `dotnet test --project` with
`FullyQualifiedName~BookmarkBrowseRowsTests`. The focused command records contain
the exact class filters. All tests use Release and `--no-build --no-restore` after
the matching builds. No full gate, hosted CI, coverage, mutation or release ran.

Pending native checks use the final candidate after the desktop is unlocked:
startup j/k and Tab; Ctrl+B category movement without source resets; Escape and
navigation exit back to the active file list; Ctrl+L entry and deliberate text/IME;
clicking a passive address must not start editing under the accepted ADR0044
amendment. Do not modify the saved catalog or restore the older settings snapshot.
The retained baseline PID6760 is not the corrected candidate. Its disposition
requires hide's answer to the earlier request; the rejected termination is not
retried through another mechanism. Keep PR402 Draft until hide's separate screen
acceptance, and keep this follow-up Draft until its selected native proof completes.

## Final source candidate (07:53 JST)

`run-final3.ps1` completed: App Release build zero warnings/errors, changed-view
whitespace exit0 (workspace-loading warning retained), complexity exit1 for only
five existing unrelated findings, and `pwsh -NoProfile -File eng/check.ps1 -Mode
Commit` exit0. Conformance reports 112 unique rules; security conformance registers
24 adversarial cases and finds no secret/workflow-supply-chain violation. This is
Commit-mode policy proof, not a run of the 24 adversarial behavior cases.

The five residual CS-013 findings are the two transient-scope parameter lists,
KeyboardHelpPresenter nesting, KeyboardInputTranslator method size, and
WindowAdjustmentKeyboardTests type size. Their five files equal baseline HEAD;
no finding remains in this change's source. Raw JSON retains unresolved invocation
counts and all measurements; exit1 is not represented as a repository-wide pass.

`final-inputs.json` freezes 1,059 source/test/tool/dependency inputs. The final App
DLL SHA256 is `F2952117222AC403837450337481A476EA9AE0E93499AD528135DF9B153DAB64`.
At 07:53:31 current settings remain exactly 428 bytes with the recorded resume
hash. PID6760 remains the old issue-398 comparison App, not the final candidate.
No native input, settings write, process termination, main integration or release
is added by these final checks. The independent source review precedes only the
equivalent context-priority/boolean-parameter cleanup; that cleanup was inspected
locally and verified by the final build/conformance/complexity checks.

The next action is the bounded native matrix above after hide unlocks the desktop
and resolves the retained old process. Keep this worktree and its build output;
they contain an unfinished acceptance candidate. The separate older four rejected
cleanup targets are carried in the [handoff](../handoffs/2026-11-20-bookmark-navigation-stability-handoff.md).

## Native pass and design steering (08:15 JST)

hide explicitly authorized ending necessary test-launched processes at08:03.
With exact path/start-time checks, the old comparison6760 ended at08:04:43.
A first guard stopped harmlessly because string conversion lost timestamp precision;
typed comparison confirmed identity. No termination workaround preceded the new authority.

Owned candidate8336 then ran the bounded native pass. Startup j/Tab, category
movement, Search native text/clear/return, close followed by j/k, explicit Ctrl+L
editing, real Japanese IME composition/native Enter commit, Escape restoration and
readonly address click were observed. Some immediate captures showed a prior frame;
settled snapshots and actual text effects are distinct. The accessibility focused
field remained LeftAddress even during verified Search/RightAddress input, so that
field alone does not prove native focus. The unsupported Zenkaku_Hankaku attempt is
recorded; documented Alt+grave enabled actual composition. No latency claim is made.

At08:14 hide requested display-only path components and an explicit shortcut-opened
path input modal. This supersedes the inline-editor direction. Reuse the existing
AddressEditorState and navigation owner, with Enter navigation, Escape cancellation
and list-focus restoration; amend ADR0044 and validate the changed view boundary.
The current source proof remains evidence for unchanged row behavior, not proof of
the forthcoming modal. PR496 stays Draft.

The native journal and two images are preserved with hash-checked transfer into
permanent evidence. Candidate8336 ended at08:15:31 under hide's test-process authority;
settings remain428bytes with the identical resume SHA256. No settings restoration
was required. The four older filesystem cleanup rejections remain separate.

## Explicit address modal implementation and first native finding (08:29 JST)

The existing address owner and mapper now drive one `AddressEditorView`. Permanent
paths are TextBlocks. Per-pane editor owners, native LostFocus cancellation and
address-transition focus suppression are removed. Expected-state `CancelAddress`
replaces the old focus-departure intent, preserving stale-state refusal and
requesting the captured list on actual cancellation. Validation errors and native
IME stay in the modal; hints come from the existing address bindings.

Before implementation, the cancellation-focus and address-hint assertions each
failed once with successful Release builds (`run-modal-red.ps1`). Afterward,
`run-modal-green.ps1` built App and its affected consumers with zero warnings and
errors, then passed 13 Application, 34 Presentation and 26 ownership cases. The
initial Application filter's `NullGuardTests.UserIntent` term matched no test;
that updated guard case therefore remains to run by its actual method name.
The separate unchanged bookmark row proof is reused, not repeated.

The independent read-only modal review found no required source correction and
requested the KEYBOARD_MODEL wording update, now made. Its result does not cover
native focus behavior. Candidate16540 initially confirms display-only paths,
startup j, Ctrl+L initial selection and native text entry. Tab then suppresses
further text; the same sequence reproduces twice, while Shift+Tab or clicking the
input restores typing. The initial journal and image preserve this failed proof.
Inspection found the new hint ItemsControl lacked the IsTabStop=False declaration
already used by the other hint bands. That declaration is now added; the selected
App rebuild and native recheck follow. No performance or complete native pass is
claimed from this first attempt. Owned16540 ended under hide's 08:03 authority.

Framework references: Microsoft's [focus navigation guidance](https://learn.microsoft.com/en-us/windows/apps/develop/input/focus-navigation)
and [WinUI focus design note](https://github.com/microsoft/microsoft-ui-xaml/blob/main/docs/design-notes/focus.md)
distinguish the native focus mechanics from the displayed/accessibility focus.
The diagnosis above is based on the observed input effect and this source change,
not on the stale accessibility focused-element field alone.

## Final modal proof and pointer boundary (08:47 JST)

The hint Tab-stop correction passes native Tab/Shift+Tab followed by actual j/k
text entry. Invalid Enter keeps the exact input and localized error; Ctrl+L
selects that current text. Real Japanese IME Enter commits composition without
navigation; composition Escape cancels composition without closing the modal.
A fresh Escape closes it and j moves the original list. Tab to the right, Ctrl+L,
absolute C:\ submission and j then navigate/move only the captured right pane.
`modal-tabfix-journal.json` and `modal-ime-composition.png` preserve this pass.

That pass also exposed a display-path click that left keyboard events outside
the file list. The first Tapped handler built successfully but independent review
identified that unconditional list focus would steal F2/F7 name input. It was
not accepted. The subsequent `AllowFocusOnInteraction=False`-only candidate also
built but failed native j and Ctrl+L after a path click; the failed journal is
retained. This property describes element focus, but it did not establish the
required native routing in this host (Microsoft [API reference](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.frameworkelement.allowfocusoninteraction)).

The final two TextBlocks consume PointerPressed and call the existing
`FocusActiveFileListWhenIdle()`. This retains the sole context/focus decision and
does not add focus state. Owned candidate36428 verifies passive-path click then j,
Ctrl+L opening, scrim dismissal, F7 opening, active-path click while entering a
name, actual `focus-only` text, Escape cancellation without creation, and k back
in the list. One initial indexed click returned a geometry error; a fresh screenshot
preceded the successful coordinate click. The eleven observations and final image
are archived with hash verification. The native focused-element field remained
stale in several observations; actual text/list changes establish the outcomes.
Independent read-only re-review at08:46:55 closes the F2/F7 finding with no required
correction. F2 shares the checked name-entry owner; a separate F2 native run is not
claimed. No latency or subjective flicker acceptance is inferred.

The updated factory null guard ran by its actual method name and passed one case
(`modal-null-guard.log`), completing the modal's selected74 cases:13 Application,
34 Presentation,26 ownership and one null-guard case. Earlier unaffected bookmark
proof is reused; these counts are not added as disjoint tests. Final App Release
build (`modal-pointer-build.log`) has zero warnings/errors. Changed C# whitespace
verification exits0 with the existing workspace-loading warning; its complete
argument list is `modal-final-format-command.json`. CS-013 inspection retains the
same five unrelated findings. The final two XML-comment corrections only replace
obsolete native-focus wording and do not invalidate behavior/build proof.

All candidate processes were bounded by exact path/start-time checks under hide's
08:03 test-process authority. The last candidate36428 received termination at
08:46:02 and the subsequent read found no NeNeCommander.App process. Settings
remain428 bytes with SHA256
`7EA7AB2FB5B06A68D6015500A227BA7AEA3B7F1DA322430834B4B1361B2630EF`.
The checked App DLL SHA256 is
`A4301CDBEBD68AA29725F4BB69A5639AD984331D9832E3F584EBA1D415F2E58B`.
`modal-final-inputs.json` freezes the final1061 relevant inputs; differences from
the Tab-fix candidate are the two path-click files and XML comments only.
Commit-mode policy results and publication are recorded after execution below.
No main/Preview/release integration, settings restoration or full CI is claimed.

At08:49 the final `pwsh -NoProfile -File eng/check.ps1 -Mode Commit` exits0:
112 conformance rules, security conformance and Git whitespace pass. The24
registered adversarial mappings are not claimed as executed behavior tests.
The independent final review accepts the bounded pointer fix. All relevant
source/consumer/native proof above is retained for publication without repetition.
