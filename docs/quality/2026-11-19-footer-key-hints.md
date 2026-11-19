# Footer wrapping and saved key hints — Issue #454

## Invariant and implementation

At main `189bb61`, the footer's fixed height and horizontal layout could hide its
rightmost information and hints. The real NeNe Commanderリナ reviewed the proposed
layout, and [ADR-0082](../adr/0082-footer-key-hint-visibility.md) records the accepted
behavior. This work is independent of the unaccepted Bookmark feature in PR402.

Operation information and key hints now receive the available width in two bands.
The existing `KeyHintWrapPanel` wraps every complete item and skips Collapsed children
without gaps. Status text wraps, result labels stay with their counts, empty idle
information collapses, and the existing footer-height token becomes a minimum.
The normal hints retain their canonical order and right-aligned block, without a row cap.

`DisplayPreferences` contains color, launch-hidden and key-hint choices. The single
`UserSettings.Create` factory and all its callers move together. The new typed intent
uses CommanderSession → SettingsSession → the existing ordered complete-document
write queue. Hidden filters only normal footer hints to canonical F1; confirmation
and name-entry Enter/Esc remain. The checkbox explains this exception visibly and
through its accessible description. No input binding is disabled by the preference.

Schema 4 appends the required boolean `showKeyHints`. Valid versions 1–3 read as Shown
without rewriting the file. Strict shape/type/case/duplicate rejection, the 65,536-byte
limit, atomic writer and existing path/identity boundaries remain. Older binaries
reject version 4 and preserve it; there is no downgrade writer or second settings path.

`SettingsEditorView` owns settings rendering and native control forwarding previously
in the window. The host still owns the session and async work. A late native
RadioButton.Checked event on opening initially caused an unwanted save; comparing
with the rendered selected scheme prevents it. The initial Close focus waits for
the actual Loaded event, once only, and pending focus is cancelled by input, pointer
activity or closing. Closing uses the host's existing file-list focus route before
collapsing. That route prefers the realized selected row, falling back to the
focusable list for empty panes. Name input is focused after its ancestor becomes visible.

## Selected proof

ADR-0061 selects settings construction/selection/admission/ordered writes and failure
behavior; all affected settings persistence and ADV-012 cases; hint, pane and settings
projection; App compilation and view ownership. Shared provider/queue/identity
implementations, dependencies and gate thresholds are unchanged. A separate upcoming
Preview requires fresh applicable release/deep-review evidence; this focused proof
does not claim to satisfy that release requirement by itself.

Permanent logs, raw native observations and inputs are in
`D:/NeNeCommander/evidence/footer-20261119/`.

| Command or selected invocation | Result |
| --- | --- |
| Locked Release restore of affected projects, `dotnet restore <project> -p:Configuration=Release --locked-mode` | Passed |
| `dotnet build <affected-project> -c Release --no-restore -m:1 -nr:false` and App build with already-built references | Final builds passed with zero warnings/errors |
| Infrastructure `dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~Settings` | Initial 188: 186 passed, 2 stale fixtures failed; final evidence is 185 unchanged successes plus the 3 corrected boundary/version cases below |
| Same Infrastructure command with filter `FullyQualifiedName~VersionFourSerialization\|FullyQualifiedName~ReadAsyncWhenSchemaVersionIsUnsupportedRejectsUnknownVersionWithoutRepairAsync` | 3 passed, zero failed/skipped |
| Application selected Settings, PaneSortPersistence, Bookmark and NullGuardTests | 139 passed, zero failed/skipped |
| Application filter `FullyQualifiedName~CreateDisplayWhenAnyChoiceIsNullRejectsIncompleteValue` after extracting the null-choice cases | 3 passed, zero failed/skipped |
| Presentation selected SettingsPresenterTests, KeyHint, DualPane and LocationsKeyboardTests | 63 passed, zero failed/skipped |
| Architecture filter `FullyQualifiedName~ViewCompositionOwnershipTests` | 18 passed, zero failed/skipped |
| `pwsh -NoProfile -File eng/verify-record-closure.ps1 -Mode Product` | 135 abstract records, 124 closed visible roots, zero failures |
| Changed-file `dotnet format whitespace NeNeCommander.slnx --verify-no-changes --no-restore --include <changed-cs-files>` | Final exit 0; existing workspace-load warning remains, initial line-ending-only findings were corrected |
| `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` | Exit 1 for 26 existing findings; baseline 28, removed 2, added 0 |

The valid schema-4 boolean cases first failed against the old reader. The two later
Infrastructure failures were an unsupported-version fixture still using 4 and a
serialization fixture exceeding the boundary after the new field was added. Those
fixtures now use unknown version 5 and assert exact 65,536/65,537-byte boundaries.
The unchanged production reader/writer did not require another correction. The other
185 successful cases are reused, without rerunning the entire selection.

Other recorded trials: the first new test build needed a public parameter null guard;
an Application build exposed an unused using; App compilation rejected a projected
CheckBox array-to-interface conversion, fixed with an internal array contract and a
defensive copy; Architecture was first attempted before the App reference existed.
All were corrected and the affected builds/checks passed. Initial formatting findings
were CRLF mismatches only; a successful format still reports a design-time workspace
warning, distinct from the successful Release compilation. Two baseline Inspect
attempts used an unnormalized root, and one concurrent tool compilation hit a DLL
lock; normalized roots and sequential execution produced usable measurements.
The removed findings are CommanderWindow's type length and the existing
HandleAsyncWhenPaneIsLaunchingFreezesBookmarkManagerAndSlotsAsync method length.
The comparison uses source/member/type identity rather than shifted line numbers.

## Native evidence and limits

The final App from `build-app-loaded-focus.log` was exercised on November 19 at
10:29–10:43 JST. The owned D process was kept separate from the running C normal app.
Japanese/Monokai was observed at the current desktop scale; captures measured
2,291×1,234, 1,715×658 and 954×658 physical image pixels. No exact DIP or scale claim
is derived from those dimensions.

- Settings opening visibly focused Close. Up moved to the last scheme; eight more
  Up reached key hints. Opening/moving did not change the settings hash. Space and
  Enter each toggled the focused checkbox once, updated the footer and saved the choice.
- Hidden survived restart and left only F1. Repeated settings closes returned to a
  populated left list and an empty right list; F7 then focused native name input without
  clicking. Draft text was cancelled. No user directory was created or deleted.
- F1 opened Help and Escape → F7 worked without a click. Some immediate AX reads
  preceded the visible update and the UIA Focused summary stayed stale. An initially
  suspected Help-return failure was therefore withdrawn; no speculative Help fix was added.
- At 954×658, all 12 normal hints appeared in two rows. Hidden retained F1; name input
  retained its full instruction, input and Enter/Esc. The shared window-adjustment helper
  also rendered its declared controls during resize. An intentionally over-short
  intermediate window clipped the whole UI; no general minimum-size guarantee is claimed.
- F5 copied one owned 45-byte fixture. Running progress 0/1 and completed status with
  all five result label/count pairs were captured at narrow width. Both files' SHA-256
  was `ACA9444819942D309E1DD0FE26E468A10B7D473F619CAEA44314281B56B1F9F7`.
  This was a rendering test, not a latency benchmark or resolution of #453.
- The owned app exited. The exact 218-byte original schema-3 settings were restored
  only after checking the expected owned test hash and backup hash. Restored SHA-256:
  `ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.
  `native-final-restoration.json` records restoration and the retained normal process.

Earlier native trials established that opening attempted Close.Focus while IsLoaded
was false. A temporary caption-only diagnostic then exited with a CoreMessaging
0xc000027b event on Escape. Its sources/event observation were preserved and the
instrumentation removed before the final build; this is not attributed to the ordinary
product without proof. The final walk above did not reproduce that exit.

Computer Use's `typeText` inserted the existing clipboard text instead of the requested
literal in one draft; it was cancelled. Some chord aliases and edge drags had no effect.
Actual screenshots and control values, not those attempts or stale focus summaries,
define the native evidence. No display/security setting or other application's content
was changed. Native English, explicit 100%/200%, high contrast, Narrator, long failure
text and save-failure rendering remain unobserved in this walk. Unit tests are not
substituted for those observations. #94's hide-reported one-time check and PR402's
separate acceptance remain distinct.

## Review and integration

The read-only keyboard review found the ancestor-visibility ordering issue and reviewed
the final one-time Loaded focus guard. The ordering was corrected and the final native
name-input/empty-pane cases above passed. The real Claude consultation and its corrections
are in the [design handoff](../design/2026-11-19-footer-key-hints.md).

Commit hooks, PR, final Inspect/format results and integration are recorded in
`D:/NeNeCommander/evidence/footer-20261119/completion.json` as they occur. At preparation,
the candidate is unmerged; no public asset or running normal binary has been replaced.
Rules: ARC-001/004/012, CMD-001/009, KBD-005, CS-002/013/023, ADV-012,
QLT-003/015, GIT-001–004, ADR-0061/0077/0082. No waiver or new dependency.
