# 2026-11-20 Bookmark navigation stability — Issue #495

Status: focused source verification complete; native acceptance pending. Source baseline 76a281a.

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
