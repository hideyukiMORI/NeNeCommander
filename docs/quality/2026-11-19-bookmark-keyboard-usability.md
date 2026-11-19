# Bookmark keyboard usability proof

Status: in progress for Issue #457; PR402 remains Draft and outside main/Preview

## Scope and invariant

The manager must support browsing, filtering, adding and editing through the keyboard while
native text composition and open selectors retain their input. Every shortcut still comes
from the canonical bookmark key table; every edit still uses BookmarkEditorAction and the
existing settings path; every activation still uses the existing navigation path.

The [design review](../design/2026-11-19-bookmark-keyboard-review.md) records the three
specialist personas and NeNe Commanderリナ consultation. The implementation adds draft-only
Ctrl+Enter, focus-specific hints, first-row selection on list entry, active/passive region
marks, form focus cycling, accessible labels and occupied shortcut names. Save clears the
search and selects the accepted item with a notice; Cancel preserves the previous context.

Under ADR-0061, verification covers Application bookmark state/persistence, Presentation
bookmark/keyboard/help behavior, the App Release build, composed-view ownership, formatting
and commit conformance. Filesystem providers, settings schema, dependencies, gates and
security boundaries are unchanged. Full regression, all-layer mutation and hosted CI were
not selected. Successful evidence is reused when its relevant inputs have not changed.

Raw logs, source-review inputs, consultation results and UI evidence are retained in
`D:/NeNeCommander/evidence/bookmark-ux-20261119/`.

## Behavioral evidence

| Case | Result |
|---|---|
| Save from a nonmatching search/category, before fix | 2 failed; Cancel return-context case passed |
| Application Bookmark cases after save-return fix | 95 passed, 0 failed |
| New browse/search/filter persistence proof | 1 passed; no settings write from browsing |
| Presentation Bookmark/Keyboard cases before final Enter correction | 131 passed |
| Native draft Enter against the previous assembly | 1 failed, proving repeated native submission was not consumed |
| Presentation after native Enter correction | 135 passed, 0 failed; includes new focus, hint, draft and repeat cases |
| Final hint labels and F1 projection | 8 passed, 0 failed |
| Composed-view ownership | 20 passed, 0 failed; field/constructor ownership unchanged by subsequent focus scheduling |
| Focus request, restoration and cancellation after the native fixes | 8 passed, 0 failed, including 2 new cases |

The independent keyboard review found the ordinary-Enter repeat regression in the draft
context. The correction passes the first Enter to the native control and retains subsequent
events from the same press across owner changes. The red assembly hash and red/green logs
are preserved. These mapper tests establish translated-event behavior, not physical key-hold
timing or real IME composition.

## Build and tooling attempts

- App compilation first rejected a reserved local identifier, then an instance member that
  should be static; both were corrected. The third App build passed with zero warnings/errors.
- The new persistence test initially had one unused import; removing it produced a successful
  test build and the passing case above.
- The final hint extraction initially failed IDE0046. The conditional was simplified without
  changing labels. A subsequent parallel MSBuild attempt produced no output or assembly change
  for more than three minutes and was stopped. A single-node, non-reusing build passed with
  zero warnings/errors in 2m59s.
- The first ownership-test build lacked its restore assets. Locked restore and the subsequent
  build passed with zero warnings/errors; this was an initial worktree setup omission.
- CS-013 Inspect first reported 8 findings. Splitting the new label projection removed its
  cognitive finding; final Inspect reports 7 existing findings and exit 1. The window remains
  above its existing type-line limit (326, base 324); its unchanged cognitive finding, two
  ADR-0051 parameter findings, help nesting, translator method length and one window-input
  test type length remain. No suppression, baseline or waiver was added.
- Deferred form focus temporarily added two new CS-013 findings (constructor 41 lines and
  Render cognitive 11). Separating the save-button label setup and visibility projection
  removed both; the subsequent Inspect returned to the same 7 existing findings. The final
  native-input observation adds one window statement (327 lines, base 324); its other metrics
  and remaining findings are recorded in the final inspection artifact.
- Changed-file whitespace verification first returned exit 2 for LF endings in new files.
  Normalizing the changed/new text files to the repository's CRLF rule preserved their text.
  The repeated verification returned exit 0 with no formatted files. Design-time workspace
  load warnings remain in the diagnostic log, separately from the successful Release builds.

## Native findings and corrections

1. The first launch terminated at BookmarkDetailsView line 79 with 0x80073B17. The code tried
   `GetString("BookmarkSave.Content")` for a XAML property resource. It now appends the canonical
   key label to the already localized button Content, keeping the XAML label as its single
   source. The new App build passed (zero warnings/errors, 57s) and the next launch displayed
   the shell and bookmark manager. The exact .NET Runtime event is preserved.
2. With no bookmarks, opening the manager left focus in Search. Enter displayed browse hints
   but the following `a` appeared in Search instead of opening the draft. This is an observed
   native defect, not just a source concern. Both ListViews now explicitly set IsTabStop, and
   hint rendering consults actual search focus even if focus was already there. The native
   rerun is recorded below after observation.
3. The Computer Use `Control_L+b` spelling did not open the manager; `CTRL+B` did. This tool
   input difference is not attributed to the application. Lowercase letter spelling also did
   not act like the uppercase virtual-key spelling; subsequent shortcut checks use `A`, `E`,
   and the effective modifier spelling. Its reported focused-element field remained the initial address
   control even when Search received text; visible control content and hints therefore provide
   the operative focus evidence, together with screenshots and the next key's result.
4. Explicit IsTabStop allowed the first empty-list `A` to open the draft. Cancellation still
   returned to Search, where a following `A` became text; draft creation also left its name
   field without the promised initial input focus. Coordinate/automation value setting and
   later paste could edit the field, separating data retention from the failed initial focus.
   The actual draft image is `native-draft.png`.
5. A native automatic focus event during layout was cancelling the pending explicit list
   request. BookmarkFocusRestoration now distinguishes an explicit request from passive row
   restoration: incidental focus cancels only the latter; explicit Search, pointer input or
   leaving the state still cancels all. Draft and confirmation initial focus now uses this
   same deferred mechanism and is cancelled if its state ends. Two focused cases protect the
   distinction; native return/draft checks remain required below.
6. Independent review then identified newer pointer/native Tab input arriving before the
   queued focus. The overlay now cancels both queues on pointer input, including handled
   events, and initial Tab. Pending form/confirmation focus consumes Enter before native
   confirmation can run. Input observation occurs before the IME/selector branch; that branch
   retains its native Enter ownership. Existing explicit-cancellation proof and the pending
   state assertions pass; the actual fast-input scenario is not claimed as observed.
7. The interaction reviewer found `Saved` beside the unsaved draft ambiguous. A separate
   localized `Settings:` label clarifies the persistence channel without hiding warnings.
   An attempted LabeledBy on the status TextBlock caused the automation name to expose only
   the label in readback; it was removed so the status retains its own text. The input-control
   labels remain explicitly associated as observed in the earlier UIA tree.

The next native walk stopped when Computer Use returned
`foreground window did not report a process id`. Window rediscovery/rebinding showed the
manager but a subsequent input and read failed again. No lock-screen interaction or security
setting change was attempted. After verifying the unchanged settings hash, only the owned
test process was stopped (PID and exact path are in `native-session-stop.json`). The normal
main app was left running. No synthetic bookmark was saved and no settings restoration was
needed. This leaves final form focus, cancellation, save, popup, IME and confirmation checks
unaccepted; the Issue PR stays Draft.

After closing this first test window, the original settings SHA-256 remained
`ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.
No bookmark had been saved, and the normal main app remained open.

Final command results, further native checks and integration are appended after observation.

## Commands and evidence reuse

The first normal commit hook rejected two boolean mode parameters under CS-002: the region
mark helper and pending-focus kind. They were replaced with the existing typed region values
and explicit-request identity, respectively; no rule or suppression was changed. The affected
focus/mark tests passed 9/9 and the App dependency build passed with zero warnings/errors, recorded in `test-focus-mode-free.log` and
`build-app-mode-free.log`. The failed hook is retained as `commit-first.log`.

Commands ran in `D:/NeNeCommander/worktrees/issue-457`. Release builds returned zero warnings
and errors except the explicitly recorded failed attempts. The named logs preserve each
attempt separately; no unsuccessful attempt is counted as successful evidence.

| Command | Recorded outcome |
|---|---|
| `dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore -m:1 -nr:false` | Success; latest `build-focus-guard-final.log` |
| `dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~BookmarkFocusRestorationTests'` | 8 passed; `test-focus-guard-final.log` |
| `dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build -- --filter 'FullyQualifiedName~BookmarkManagerKeyHintTests\|FullyQualifiedName~KeyboardHelpTests'` | 8 passed; the backslash before the table pipe is Markdown escaping only; `test-presentation-hints.log` |
| `dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore -m:1 -nr:false -p:BuildProjectReferences=false` | Latest native-priority result in `build-app-native-priority-final.log`; dependencies were built immediately beforehand |
| `dotnet restore tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -p:Configuration=Release --locked-mode` | Success; `restore-architecture.log` |
| `dotnet build tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-restore -m:1 -nr:false -p:BuildProjectReferences=false` | Success; `build-architecture-final.log` |
| `dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~ViewCompositionOwnershipTests'` | 20 passed; `test-architecture-final.log` |
| `dotnet format whitespace NeNeCommander.slnx --no-restore --verify-no-changes --include $changedSources --verbosity diagnostic` | Exit 0; changed/new C# paths from Git; `format-final.log` |
| `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` | Exit 1 for the seven existing findings; raw JSON and logs preserve intermediate and final measurements |

Application's 95 bookmark cases plus the new browse-persistence case, and Presentation's 135
bookmark/keyboard cases, are reused for unchanged relevant behavior. Later hint changes were
covered by the eight hint/help cases, and focus changes by the eight focus cases. The twenty
ownership cases were rebuilt after adding the form focus owner; subsequent method-body and
pointer/input hooks add no session/task field or constructor parameter. Formatting and comment
changes do not invalidate behavioral proof. No hosted workflow, full gate, coverage or mutation
run is inferred from these results.

## Environmental limits

Initial review images showed the closed shell only. Source inspection and mapper tests do not
prove native Popup routing, IME, Tab cycling, Narrator or scaling. An owned build and synthetic
bookmark targets are used for native checks; the existing normal app and original settings
are preserved. hide's separate PR402 screen acceptance remains required.
