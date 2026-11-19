# Bookmark keyboard usability proof

Status: Issue #457 follow-up integrated through PR464; PR402 remains Draft and outside main/Preview

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

## Accepted main synchronization, 17:25 JST

PR477 passed final deep review44479037554 and merged as7bcf6de; completion record PR478
merged as1cc5d75. Draft PR402 now imports that accepted main history. The four conflicts are
documentation only: PROJECT_STATE retains both histories, while the three inspection-purpose
records take main's later acceptance/integration/cleanup evidence. There is no source/test/tool
change against the already-built7a10707 combination. All1,061 verification input hashes and
the combined App DLL hash remain identical to inspection-sync's successful proof.

The existing App/consumer builds and four focused boundary cases therefore remain applicable;
no build, product test, hosted CI, complexity scan or native walk is repeated for this history
synchronization. The accepted provider deep review is preserved in the inspection-purpose
record; this does not grant PR402 hide's separate screen acceptance. PR402 remains Draft.
The normal running app and public beta.2 remain unchanged. The issue-476 worktree/output are
now absent; unfinished Bookmark work and the four previously rejected cleanup targets remain.

Commands: `git merge --no-ff --no-commit 1cc5d75819b8044349c0353e36e28c21c57cba50`,
the bounded document conflict resolution and input checks in `python <evidence>/inspection-sync/
accept-main.py`, then normal commit hooks. The first merge returns1 only for the four stated
document conflicts. `accepted-main.json` records unchanged inputs and document-link checks.

## Inspection candidate combination, 16:40 JST

Feature26dcb52 was combined with PR477 candidate da42786, whose unchanged568371f verification
inputs are still under deep review44479037554. This is a preparatory combination inside Draft
PR402, not evidence that PR477 or PR402 has been accepted into main. The new purpose-aware
Inspect contract is imported unchanged; Bookmark input/edit/persistence/navigation ownership
does not change. The only merge conflict was PROJECT_STATE's added progress lines, resolved by
retaining both complete histories. All38 incoming source/test/tool files exactly match the
candidate, and all84 feature source/test/tool files exactly match the prior feature; the sets
do not overlap. No C# was manually edited. GLOSSARY and the ADR index merged automatically.

The changed public signature requires compilation of its combined consumers. Release builds
of Application tests, Presentation tests and App all pass with zero warnings/errors. Four
focused feature-split cases pass: the two NullGuardTests, palette Delete confirmation target,
and operation-modal ownership of Bookmark input. These checks exercise the local combination;
the complete provider/shared-contract deep review remains with PR477. Existing unchanged
Bookmark and provider evidence is reused rather than repeated. No additional hosted/full suite,
mutation, complexity scan or native launch was started for this merge.

Exact commands, argument arrays, times, exits and logs are in permanent `inspection-sync/`.
The driver is `pwsh -NoProfile -File <evidence>/inspection-sync/run.ps1`. Each build uses
`dotnet build <project> -c Release --no-restore -m:1 -nr:false`; the one Application test command
uses `dotnet test --project <Application.Tests.csproj> -c Release --no-build --no-restore --
--filter <four-case-filter>`, recorded exactly in commands.json. The source comparison and
all1,061 combined verification hashes pass `python <evidence>/inspection-sync/record.py`.
No dependency/project inputs changed, so the existing locked restore remains applicable.

The newly built combined App DLL SHA256 is `3C0DF6CEDF1ABBE9350538AE0F076CEA41BE8FA5397F3C2EC05F861E2C46A38F`. Earlier native screenshots and
timings retain their original candidates; this binary has not received a new native walk.
The normal running app/public beta.2 remain unchanged. PR402 stays Draft for hide's screen
acceptance; #476's final deep review and main integration remain independently pending.

## Native category and bookmark renaming, 16:30–16:34 JST

The unchanged featurebad2b03 and the same App DLL SHA256
`3C65F5877ABC06A8CF67887C930E18C0A2CA41578B207EC64F5678E7461FAC1A` were used for a
predeclared owned-data walk. All1,040 prior verification inputs remain byte-identical.
In dedicated process18252/window12193574, keyboard-only Tab/A and Ctrl+Enter create
NN Category Original, then one NN Category Item at the owned empty category-rename/target.
The native category selector assigns the new category. Its count is1 and Unclassified0.
Tab/Down/Down/E opens that category's rename form; Ctrl+A/type/Ctrl+Enter changes it to
NN Category Renamed. The item keeps its category membership and path. E then opens the
bookmark edit form, and the same save route renames it to NN Category Item Renamed.
The settled list and screenshot show the new names, one item, and the new category count1.
The saved JSON differs only in the two planned names and the corresponding category reference.

Several immediate AX reads still showed the preceding input/list state. Later screenshots
and subsequent state reads established the requested values before the relevant acceptance
claim; these observations establish no input latency, dropped-key cause or precise focus timing.
The first Alt+F4 inventory still listed the owned window; the subsequent inventory confirmed exit.
After restart, Ctrl+B's combined input/read call returned `foreground window did not report a
process id`. One rediscovery/rebind showed the manager already open with both renamed names,
the category count1 and matching membership. No second Ctrl+B was sent. A subsequent state
capture returned the same tool error, so UI attempts stopped. The final restart screenshot
was not obtained; the rebind readback is an observed tool result, not a saved raw screenshot.
After exact-path/PID selection, only owned process25852 was stopped. No lock-screen interaction,
security-setting change, UI deletion or file-operation command was submitted.

The saved430 bytes are identical before restart and after the last native attempt. An exact
comparison against the original permits only the owned category/bookmark and expected
schema4/default-hints migration. `python <evidence>/category-rename/restore.py` restored the
original218 bytes at16:34:06 and verified the original SHA256. Normal process38360 remains.
The baseline/plan,19 distinct state snapshots,20-action list, screenshots, before/after settings,
tool-error record and restoration result are retained in permanent `category-rename/` evidence.
The empty target directory remains with the unaccepted feature's other fixtures.

Redefining the capture helper after the first manager read reused state-01 for the next read;
the initial open action remains in the20-action list, but its separate raw snapshot was overwritten.
No acceptance claim depends on that missing initial snapshot. The first document-verification
attempt confused action ordinals16/20 with the actual snapshot numbers15/19 and stopped before
editing documents. Listing the retained files corrected those references; captured data was not edited.

`python <evidence>/category-rename/record.py` independently checks the saved-name/reference
change, restart byte equality, key native state lines and all1,040 unchanged verification inputs.
This is additional bounded native evidence and a documentation-only change, with no new build,
product test or hosted CI. It does not replace hide's separate PR402 screen acceptance.

## Native shortcut collision and restart, 15:47–15:54 JST

The unchanged combined source9006020/App DLL SHA256
`3C65F5877ABC06A8CF67887C930E18C0A2CA41578B207EC64F5678E7461FAC1A` was used with a
dedicated empty `slot-restart/target-one` and `target-two`. The planned keyboard walk assigned
Ctrl+1 to NN Slot One. Assigning the same slot to NN Slot Two displayed the occupied-slot
failure, retained its name/path draft and kept the saved count at1. Choosing slot2 and
Ctrl+Enter then saved both entries, whose row descriptions expose Ctrl+1 and Ctrl+2.

After closing that owned window and observing its absence, a fresh process read the517-byte
schema4 settings. Ctrl+1 changed the left active pane to target-one while the right remained
at its starting path. Tab followed by Ctrl+2 changed the right pane to target-two and retained
the left target. Both lists completed loading. The before-restart and after-navigation
settings bytes are equal and contain exactly those two names, paths and slots, with no categories.
This establishes persistence and routing for these two numbered shortcuts, not all nine slots,
physical held keys, repeated activation or every filesystem provider.

The final owned window exited. At15:54:19, comparison against the saved original established
only the two owned registrations and expected schema4/default-hints migration. The original218
bytes were restored with SHA256 `ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.
Normal process38360 remains. No deletion was submitted and no product/settings rule changed.
`slot-restart/baseline.json`, `actions.json`, state text/images, both saved settings snapshots
and `restoration.json` retain the plan and observations. The immediate first-close inventory
still showed the window; the later inventory confirmed exit before restarting or restoring.

This is a documentation-only record of unchanged product inputs; existing selected build/test
evidence is reused. No additional build, product test or CI is required. PR402 remains Draft
and hide's screen acceptance is still required before integration into main or a Preview.

## Native accessibility readback, 14:17–14:23 JST

The unchanged combined source `277a003` and App DLL below ran as owned process35744/window27856424.
The existing UI Automation surface exposes the four form labels: name, path, category and shortcut,
and category-name on its separate form. Two owned categories, 確認カテゴリA/B, each received a
bookmark named 同名確認 at the same owned empty `accessibility-target`. In the realized list,
the accessible names are `同名確認 · 確認カテゴリA` and `同名確認 · 確認カテゴリB`, and both
descriptions contain the complete target path. This verifies actual native peer information,
not Narrator speech, announcement timing, unique automation IDs or visual category columns.
The two rows intentionally share the documented `BookmarkRow_同名確認` automation identifier.

This bounded readback uses native keys and targeted field-value setup; it is not an additional
keyboard-only end-to-end claim. One incorrectly escaped setup string was corrected and read back
before saving. A ComboBox element click returned `coordinate input geometry is unavailable`;
a fresh screenshot and Tab/End/Alt+Down recovered selection through the keyboard. The immediate
Return/Ctrl+Enter observation still showed the draft; the later Tab/Ctrl+Enter and settled readback
showed two saved entries. No timing or dropped-input cause is inferred from that sequence.
`second-saved.txt` is the earlier draft observation, not proof of the final saved state;
`two-names-readback.txt` and `two-names.jpg` own that proof. No deletion or navigation was submitted.

After Escape/Alt+F4, a subsequent window inventory confirmed that only the normal app and the
unrelated Chrome window remained. The settings difference was exactly the two owned categories,
two bookmarks, and expected schema4/default-hints migration. At14:23:44 the original218 bytes
were restored with SHA256 `ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`;
normal process38360 remained. `accessibility-readback/baseline.json`, form-label/row text,
the observed settings bytes and `restoration.json` retain the evidence.

This documentation-only addition reuses the unchanged build and selected-test evidence below.
No extra build, product test, CI, release, physical key-hold, DPI, high-contrast or Narrator proof
is claimed. hide's separate PR402 screen acceptance remains pending.

## Shared footer integration, 14:09–14:11 JST

Main `3865060` brings the accepted #467 unchanged-hint reuse and #465/#467 records into the
feature. The only merge conflict is the leading PROJECT_STATE entries; both histories remain.
The only code/test changes from feature `ddc16f4` are main's exact two footer product files
and two related test files. No bookmark, input/focus, Application, provider, dependency or tooling
implementation changes. The independent Bookmark key-hint owner remains separate from the normal
operation footer. This synchronization does not make PR402 accepted or part of public beta.2.

The combined App and Presentation test Release builds pass with zero warnings/errors. Twenty
focused normal-footer/Bookmark-hint cases pass, zero failures/skips, through:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/bookmark-ux-20261119/footer-sync/verify.ps1
```

The script runs `dotnet build <App|Presentation tests> -c Release --no-restore -m:1 -nr:false`
and `dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj
-c Release --no-build --no-restore -- --filter 'FullyQualifiedName~FooterKeyHintTests|FullyQualifiedName~KeyHintPresenterTests|FullyQualifiedName~BookmarkManagerKeyHintTests|FullyQualifiedName~BookmarkPresentationBehaviorTests'`
with TRX output in `footer-sync/presentation`. `commands.json` records complete arguments and exits.
All1,040 inputs captured for this bounded run are unchanged; imported files exactly match main.
The combined App DLL SHA256 is `3C65F5877ABC06A8CF67887C930E18C0A2CA41578B207EC64F5678E7461FAC1A`.

ADR-0061 selects this combined compile and shared-hint boundary proof. Main's #467 render/progress,
format, complexity, ownership and native evidence and the feature's unchanged input/IME/focus
proof are reused. No repeat Application/provider/full suite, complexity run, new native walk,
hosted workflow, release or settings mutation occurs. Existing feature limitations remain.
Source comparison and proof are in `footer-sync/final-proof.json`; the worktree stays for PR402.

## Native input ownership follow-up, 12:44–12:54 JST

PR464 was squash-merged into the feature at 12:58:05 JST as `2e644bf`; its complete tree equals
the verified `40b4c88` tree. The normal commit hook passed 112-rule conformance, registered
24 adversarial cases and secret/workflow checks. Main `5b54b0a` was then synchronized solely
for the four release-publication/cleanup documents. The one PROJECT_STATE conflict retains
the new bookmark record and all release/feature history. Product, test, dependency and tooling
inputs are unchanged by this synchronization, so successful evidence is reused without a
build/test/CI rerun. The final actual-diff keyboard review found no required correction.

The prior fast-input observations prompted a read-only source review, which found a valid
ordering even though the observed native sequence was not isolated: an explicit list request
survives incidental Search focus, actual text input does not cancel it, and its later callback
can take focus back. The same missing ownership transfer can let a draft's delayed SelectAll
replace the user's newer selection. This is a source-confirmed gap; it is not proof of the exact
cause or timing of the earlier native observations.

The existing root mapper now reports input left unhandled to the bookmark ownership view.
Only actual focus in Search, Name, Path or Category Name cancels the existing browse/detail
queues. Composition start uses the same path. Initial KeyDown and identified editing keys can
transfer ownership; a repeated raw Other awaits the produced character's repeat-guard decision.
Thus holding an opening letter cannot prematurely discard the new form's initial focus, while
an actual unhandled character can. Mapped actions and pending-confirmation Enter retain their
existing handling. No key table, state, persistence, navigation or deferred-queue mechanism was
added. The independent keyboard reviewer supported these boundaries before implementation.

`fix/457-bookmark-native-input` reuses the issue-457 worktree and starts at feature `527ac43`.
The existing queue test now includes incidental layout focus before explicit cancellation.
This proves that later cancellation still wins after layout retained the request; it does not
test WinUI event delivery. The unchanged mapper/repeat tests provide their existing boundaries.

Verification was run with:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/bookmark-ux-20261119/verify-native-input.ps1
```

The script records every executable, argument, start/end time and exit in
`native-input/commands.json`. Each of App, Presentation tests and Architecture tests received
`dotnet restore <project> -p:Configuration=Release --locked-mode`. All builds used
`dotnet build <project> -c Release --no-restore -m:1 -nr:false`; the Architecture build additionally
used `-p:BuildProjectReferences=false` after the successful App/dependency build. All returned
zero warnings/errors. Both test commands used
`dotnet test --project <project> -c Release --no-build --no-restore -- --filter <filter>` with
TRX output under `native-input/`:

| Filter | Result |
|---|---|
| `FullyQualifiedName~BookmarkFocusRestorationTests\|FullyQualifiedName~BookmarkKeyboardRepeatTests\|FullyQualifiedName~BookmarkDraftKeyboardTests\|FullyQualifiedName~BookmarkKeyboardMappingTests` | 27 passed, zero failed/skipped |
| `FullyQualifiedName~ViewCompositionOwnershipTests` | 24 passed, zero failed/skipped |

The 1,034 tracked source/build/tooling inputs remained byte-identical throughout verification.
`dotnet format whitespace NeNeCommander.slnx --no-restore --verify-no-changes --include $changed --verbosity diagnostic`
returned 0 for the four changed C# files, with no edits; design-time workspace warnings remain
in its log. `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` returned 1 for the
same seven existing findings. The window type grows from 305 to 311 logical lines; no new
finding, suppression, baseline or waiver was introduced. Application206 and unrelated combined
Presentation evidence above are reused because those inputs did not change. No full gate,
coverage, mutation, hosted workflow or release revalidation was requested by this local hook fix.

The new App DLL SHA-256 is
`69D83CD2FEB7E559872B1D977F813FE02E0F98170CA9B7DD415AAEF2DA502CC4`.
Owned process10680/window7737148 ran from 12:51 to 12:54. Ctrl+B and A opened the zero-row
manager and draft; the default name was visibly selected, and discrete X/Y keys replaced it
with `xy`. A fast Escape/slash/X/Y batch subsequently left `/xy` in Search, retained after a
settled observation. Slash was literal because Search owned it at that point; this does not
claim that the list transition completed before slash or identify a queued callback's timing.
The entire later text stayed in Search rather than changing into a browse command.

Actual Search IME showed underlined Japanese `か` and a candidate popup. Enter committed it
without leaving Search; another K/A composition followed by Escape retained only the committed
`か` and the manager. IME was switched off afterward. Clearing the query and a fresh Enter
returned to browse hints; the next A opened a draft. Name and Path both retained `xy` after
Tab and native Ctrl+A input, and Category Name retained `xy` after category Tab/A. All drafts
were cancelled; no bookmark/category was saved or deleted. The owned process exited normally
and the original 218-byte settings hash was unchanged; no restoration was needed. Normal
process38360 and the unrelated Chrome window remained running.

The first element click failed before input with `coordinate input geometry is unavailable`;
a new snapshot and keyboard input recovered the session. One screenshot request with
`emit:false` was refused and was repeated with supported arguments. Immediate AX reads often
preceded the rendered change, so the saved later AX/screenshot determines the outcome. These
tool/setup events are not product failures. Windows Computer Use does not support held-key
duration, so physical hold and deterministic delayed-dispatch interleaving remain unobserved.
No destructive confirmation, DPI, high contrast or Narrator proof is added by this follow-up.

## Additional native walk, 12:33–12:40 JST

Unchanged feature `527ac43`/combined DLL below ran as owned process10792/window29561286.
One owned bookmark pointed to an absent test directory. Ctrl+Enter saved it; Enter showed the
navigation failure while preserving C:\. Escape returned to browsing, and Enter failed again.
After creating only that owned empty directory externally, Enter alone navigated successfully.
No pointer input occurred between initial entry/save and this back/retry sequence. This resolves
the earlier inability to separate keyboard retry from pointer trials for this one owned case.

At an observed 819×658 pixels, populated rows, bookmark-delete confirmation, category draft and
the actual category-delete confirmation fit inside the modal. Cancel had visible initial focus;
Enter cancelled both confirmations. No deletion was executed. Search IME was also observed
with a candidate popup, Enter commit and Escape cancelling only the next composition.
`native-walk-1233.md` owns the full attempt order and image provenance. In particular,
`populated-narrow-category-delete.*` actually shows bookmark deletion and is not category proof;
`narrow-category-confirm-actual.*` is the successful category capture.

Setup included ineffective initial key attempts, a window-mode attempt that inserted minuses
into the address (restored without submission), native caption sizing, delayed AX data and a
rejected stale element index. Fast Return/Tab/Down/D and cancel/slash sequences had uncertain
focus timing; the first IME attempt opened an unsaved draft and was cancelled. Only the later
explicitly focused Search composition is counted as successful IME evidence. These attempts
prompted the source review above; no blanket fast-input success is claimed.

The process closed normally. Settings differed only by the owned bookmark/category plus the
expected schema migration and default hints. After verifying those differences, the exact
original218 bytes were restored, SHA-256
`ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.
`native-retry-restoration.json` records the comparison. Normal process38360 remained running.
hide's separate PR402 acceptance is still pending.

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
