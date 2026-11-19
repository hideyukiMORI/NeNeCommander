# Bookmark keyboard usability proof

Status: Issue #457 integrated into the feature; PR402 remains Draft and outside main/Preview

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

## Integration with main, 11:38–12:03 JST

PR458 was squash-merged into PR402's feature branch as `9ba489a`. Synchronizing
main `c25dc30` retains #454's canonical file-list focus restoration and schema-4
display preferences. Eight conflicts were resolved explicitly: six documentation
or resource files retain both changes, and the two Application fixtures retain
the feature's responsibility-specific layout plus main's new settings contract.
Seven factory calls in six moved/new bookmark fixtures now pass the same default
DisplayPreferences as main. No second focus or settings execution path was added.
`sync-conflict-resolution.json` and `sync-fixture-migration.json` record the edits.

| Command in the issue-398 worktree | Result |
|---|---|
| `dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore -m:1 -nr:false` | Zero warnings/errors; `sync-build-application.log` |
| `dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~Bookmark\|FullyQualifiedName~CommanderSession\|FullyQualifiedName~NullGuardTests'` | 206 passed; no failures/skips; `sync-test-application.log` |
| `dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore -m:1 -nr:false` | Zero warnings/errors; `sync-build-presentation.log` |
| `dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~Bookmark\|FullyQualifiedName~FooterKeyHint\|FullyQualifiedName~SettingsPresenter'` | 74 passed; no failures/skips; `sync-test-presentation.log` |
| `dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore -m:1 -nr:false` | Zero warnings/errors; `sync-build-app.log` |
| `dotnet build tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-restore -m:1 -nr:false -p:BuildProjectReferences=false` | Zero warnings/errors; `sync-build-architecture.log` |
| `dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~ViewCompositionOwnershipTests'` | 24 passed; no failures/skips; `sync-test-architecture.log` |
| `dotnet format whitespace NeNeCommander.slnx --no-restore --verify-no-changes --include $files --verbosity minimal` | Eight conflict/migrated C# files; exit 0; workspace-load warning retained in `sync-format.log` |
| `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` | Exit 1 for the same seven existing findings; `sync-complexity.json` |

Markdown escapes the filter pipes above; the executed filters contain plain `|`.
Domain and Infrastructure sources are identical to main; bookmark state/presenter
and dedicated views are identical to the accepted feature commit. Their applicable
successful evidence is reused. The independent keyboard review found no required
correction in the combined pending-focus/context ownership; this was source proof.

The combined App DLL SHA-256 was
`BA0090A543AF38493D61F23BC513F34A79CD2868B2D8060C92830B9CB582A56C`.
In owned process 12396/window 15666210, Ctrl+B → A opened the empty manager's form,
typing replaced the selected default name, and Ctrl+Enter saved one owned bookmark.
Enter navigated to its existing empty target. Without a pointer action, Ctrl+B
reopened the manager with the saved row selected. This establishes the previously
missing empty-pane return for this combined build. `sync-saved.txt`,
`sync-navigated.txt` and `sync-empty-reopen.txt/.jpg` retain the observation.
The tool's initial no-targetable-window result was followed by successful window
discovery; delayed immediate AX reads are not counted as application failures.

Escape and Alt+F4 closed only the owned process. The saved settings differed only
by that bookmark and the expected schema-3 to schema-4 migration/default key hints.
After process exit, the original 218 bytes were restored with SHA-256
`ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.
`native-sync-baseline.json` and `native-sync-restoration.json` record ownership and
hashes; normal process 38360 remained running. No latency, DPI or Narrator claim
is inferred. hide's separate PR402 screen acceptance is still required.

The subsequent sync of main `555704e` changes only the App version and release
documents; its sole conflict is the leading PROJECT_STATE records, retained from
both branches. Production/test C# and XAML inputs are unchanged from the combined
proof above. Under ADR-0061 that proof is reused without another build or test run.

## Narrow-window observation, 12:11–12:16 JST

The unchanged combined App DLL above ran in owned process 39680/window 10030906.
At an observed 807×658 pixels, the empty browse view showed the category list,
search, empty state and complete key band inside the modal. A opened the draft;
all four fields, Save (Ctrl+Enter), Cancel and settings status were visible.
Four Tabs from the initial name field reached Save with visible focus, the next
Tab reached Cancel, and Enter returned to the zero-row browse view without saving.
`narrow-browse`, `narrow-draft`, `narrow-save-focus` and `narrow-cancel-focus` retain
the available AX/image evidence; `native-narrow-result.json` records the outcome.

Setup used only the owned window: repeated Ctrl+W shrink steps initially made its
height about 50 pixels, so the height was recovered before assessing the modal.
The tool's `plus` and Shift+semicolon forms did not enlarge it; Shift+equal did.
One drag was refused because its endpoint was outside the window; two right-edge
drags did not resize it. The standard caption menu's Size operation then adjusted
width separately. Immediate AX/image results sometimes lagged the visible change;
an apparent failure to open the manager was not sustained by later observations.
A pointer click reestablished list focus during setup, so this is not proof that
native sizing preserves keyboard focus. No product change or latency conclusion
is inferred from these tool/setup attempts.

Escape and Alt+F4 closed the owned window; settings remained byte-identical to the
original 218-byte baseline and normal process 38360 remained open. The evidence
covers this narrow empty/draft view only. Populated row columns, deletion and
category forms at narrow width, explicit DPI/high contrast, Narrator and the other
previously listed environmental cases remain unobserved. hide acceptance remains
separate.

## Resumed native walk, 11:07–11:20 JST

Candidate `95549f5` used App DLL SHA-256
`A90C60C728D42182851AFBB8CF206EBF580F8044D3A556F293280397B8BB21DA`.
The test-owned D process was PID 4952, window 331598. The normal C process 38360
and the unrelated Chrome window were preserved. `native-final-baseline.json`,
`final-*.txt` and `final-*.jpg` retain the raw observations.

| Native observation | Result and boundary |
|---|---|
| Empty manager → A → name input; Escape → A again | Draft opened twice; an X key replaced the selected default name without a click |
| Ctrl+Enter save; E after save | One owned bookmark persisted and the selected row opened for editing |
| Empty-name validation | Error displayed; name/path draft remained; Escape cancelled |
| Search A → Enter → E | Search stayed in the manager, Enter selected a result, E edited that row |
| Tab through name/path/category/slot to Save; ordinary Enter | Save button had visible focus; changed name persisted, search cleared, row selected |
| Category and slot native dropdowns | Category Escape closed only the popup; category/slot Enter closed the popup and retained the draft; slot 1 was saved |
| Actual Japanese composition in name, path and category-name fields | K/A produced an underlined Japanese character and native candidates; Escape/Enter retained the form. Unicode value setting was not used as IME proof |
| Category-name Tab cycle; category-delete Tab cycle | Focus returned within the form/confirmation; confirmation initially focused Cancel |
| Category and bookmark deletion confirmation | Target name/path or category/count/destination visible; ordinary Enter on focused Cancel retained the entries. No deletion was submitted |
| Empty category → A | Add draft opened from a filtered zero-row list and retained that category on save |
| Missing owned destination and later retry | Failure preserved the original pane and showed retry/back. After creating only the owned destination, a later readback confirmed navigation there. Return and pointer attempts overlapped delayed observations, so keyboard-only retry success is not isolated |

The tool initially reported no targetable window while the app remained running.
The complete inventory subsequently returned the owned window, so this is not a
confirmed application startup failure. Some immediate AX results and screenshots
preceded later UI updates; the reported focused-element field also remained the
initial address. A CUA timeout reset the session during path composition; the next
inventory and screenshot confirmed the same owned draft and actual composition.
The apparent retry failure was withdrawn after the successful destination readback.
No retry code change is justified by that ambiguous interval alone.

After navigation to the empty destination, reopening from the keyboard was not
established; clicking the populated pane allowed the remaining confirmation check.
This candidate does not yet contain #454's accepted main-side file-list focus fix.
Reuse that canonical fix when synchronizing the feature instead of adding a second
focus path. The saved-selection notice also returned to the ordinary browse text
before another deliberate action. The bounded correction below covers redundant
Select, Search and Filter notifications; the precise native event remains unknown.

The app exited normally through Alt+F4. Settings contained exactly two owned
bookmarks and one owned category, with all other JSON values unchanged. After the
process exited, the original 218 bytes were restored and SHA-256 matched
`ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.
`native-final-restoration.json` records the checks. No ordinary app was closed.

Remaining limits: physical held keys, fast Tab/pointer races, search-field IME in
this walk, independently isolated retry/back, narrow-window/DPI/high-contrast and
Narrator checks. The observed window was 2291×1234 pixels; no DPI value is inferred.
These observations improve technical evidence but do not replace hide's PR402 acceptance.

## Saved-selection notice lifetime correction

Independent source review confirmed that all three browse actions discarded the
receipt even when their effective input was unchanged. `BookmarkEditorSession`
now retains the context and saved selection for an unchanged search, resolved
category or selected entry, while clearing an earlier problem as before. Category
resolution and complete selection metadata validation precede the equality check.
Actual changes, explicit selection clearing and rejected stale inputs end the notice.
The existing action path is unchanged; no timer or native-event suppression was added.

Twelve new cases cover repeated inputs after adding/editing (six), actual changes
(four), replaced metadata and a removed category (two). Against the previous
Application assembly, the six repeated-input cases failed and the other six passed.
After the correction, all 28 selected notice/editor/return/persistence cases passed.
The independent final review found no required correction. This establishes the
state contract; it does not identify the original native event or alone prove the
final on-screen notice lifetime.

Commands in the same worktree:

```powershell
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore -m:1 -nr:false -p:BuildProjectReferences=false
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~BookmarkSavedNoticeTests'
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore -m:1 -nr:false
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~BookmarkSavedNoticeTests|FullyQualifiedName~BookmarkEditorSessionTests|FullyQualifiedName~BookmarkEditorReturnTests|FullyQualifiedName~BookmarkBrowsePersistenceTests'
```

The first build intentionally reused the old product assembly for red proof. Both
builds passed with zero warnings/errors; the red test exit was 2 and the green exit
was 0. Separate `build-saved-notice-{red,green}.log` and
`test-saved-notice-{red,green}.log` preserve the results. Previously successful
Presentation and view-ownership evidence remains applicable to their unchanged inputs.

The final App build (`dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj
-c Release --no-restore -m:1 -nr:false`) passed with zero warnings/errors in 59.67s.
`pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` still reports the
same seven existing findings (exit 1), with none added. The two changed C# files
pass the changed-file whitespace command above (exit 0, no edits); its design-time
workspace warnings remain separate from the successful Release build.

At 11:31–11:33 JST, the rebuilt App DLL SHA-256 was
`FADBF8A61FA4B2BF453E93C93871E0257893C39DBE9587AD349EDAB97A05183F`.
An owned window added only `__NeNe457_Notice` at `C:\` using Ctrl+Enter. The immediate
AX observation and the subsequent screenshot/AX observation about 69 seconds
later both retained the updated-bookmark/search-cleared notice and selected row,
with no intervening app input. This is a bounded native confirmation of the fix;
it is not an event trace identifying the old trigger. `notice-after-save.txt`,
`notice-idle.txt` and `notice-idle.jpg` preserve the observations.
The app exited through Alt+F4, only the owned bookmark was found in the settings
diff, and the original 218 bytes were restored with the same baseline SHA-256.
`native-notice-baseline.json` and `native-notice-restoration.json` record the
assembly, settings hashes, 68.59-second observation interval and retained normal app.

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
