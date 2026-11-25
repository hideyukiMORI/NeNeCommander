# Selection controls investigation — 2026-11-25

Current status: selected technical proof complete; local commit/publication remain.
The chronological proposal and initial failures below remain historical records.

Issue #562 follows the active beta.5 release task in a separate D worktree. Base is
0e0829caaa284f3f3478f00ce782cc1b2df2d33d. No beta.5 input is changed. The existing
Issue is open and its requested controls are confirmed through the GitHub connector.

Read-only inspection finds pointer Shift/Control+Shift range selection and the
single anchor already implemented in PaneSelection/PaneReducer under ADR-0090.
The missing scope is keyboard ranges, select-all, invert and explicit clear entry.
The proposed technical decision is ADR-0093; implementation has not started.

A restricted background Claude invocation actually invokes Skill(design), then
ToolSearch for Canvas. It returns ordinary advice only: the installed /design is
access management and no dedicated Canvas tool is exposed. Neither access settings
nor existing design projects are changed. No repository file is sent. The authored
brief, raw stream, invocation receipt and local design-advice.md remain under
D:/NeNeCommander/evidence/selection-20261125 and outputs/selection-20261125.
Engineering adaptations and rejected advice are explained in the ADR. No visual
handoff or native acceptance is claimed.

The worktree creation succeeds with 1,850 files at09:19JST. The surrounding shell
ends nonzero only because its following CodeQL log read precedes that log's creation;
this is not a failed checkout. Initial exploratory missing-path/glob reads are
read-only setup misses; corrected file discovery finds the actual input owners.
All work remains local. No source behavior, product test or remote write is performed.

New worktree/output cleanup follows acceptance, main integration and permanent
preservation with exact-path, uniqueness and live-reference checks. Unfinished
beta.5, feature copies and live trial03 remain retained.

Independent read-only review requires two clarifications, both incorporated in
proposal02: navigation Shift classification applies only to FileList (F10 keeps
its existing all-context behavior), and produced a/A/U+0001 in FileList receives
Shift information so Ctrl+Shift+A is rejected instead of becoming select-all.
Other contexts and letter modifiers retain their existing rules. Selection/anchor,
chord and inversion policies have no other mandatory design finding. Source and
tests remain unchanged; no behavior proof is claimed.

The concrete Issue-body update is rejected by automatic approval before the remote
write, citing absent explicit destination/payload consent for project information.
The exact body is retained as issue-body-02.md with SHA256
E16998F287D0997B8B4C35A4C1E1DAC801091653C8DD237566856547C2D5BC19.
An asynchronous question is pending. No alternate write/retry is performed. Local
investigation and implementation remain within hide's active milestone instruction;
the unpublished local plan supplies the issue's detailed acceptance/verification
supplement while the public metadata update is held.

## Initial implementation and causal proof

Nine closed UserIntent cases now use the same PaneReducer and PaneSelection value
calculations. The sole mapper adds eight bindings, the existing native translator
reads Shift only for their declared FileList inputs, and the shared repeat guard
owns all/invert presses. The palette adds three entries; F1 remains generated.
Nineteen resource keys are added to each of the three existing locales.

API-only red proof adds the intent declarations and eleven behavior tests before
changing either reducer/calculator. The initial build rejects two test-style issues
(IDE0305 and unused using); both are corrected without production behavior changes.
The next build has zero warnings/errors and all11tests fail on missing selection
behavior, exit2. red-inputs-02.json freezes the exact declarations/tests and unchanged
reducer/calculator. Initial selected builds01/02 reject IDE0046 in the new dispatch;
the final dispatch is one switch expression. All original logs remain unchanged.

Selected Application03passes227/227, including new range/bulk/provider/active-pane/
palette proof, existing pointer selection/ownership and extended modal/busy admission.
The initial Presentation03has221pass/3fail: the complete catalog expectation lacks
three appended rows, last-row selection still expects the old last command, and
window-mode key enumeration still expects51rather than54keys. The expectations are
updated explicitly, preserving complete row/order and undeclared-input assertions.
Presentation04and the App resource build are in progress; no final acceptance is claimed.

Exact commands/timestamps/exits are retained in red-proof-01/02.json and
selected-progress-01/02/03/04.json under the task evidence root. The entry scripts
run-selected-01 through04.ps1 keep the precise filters. Principal selected commands:

- `dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore`
- `dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~Selection|FullyQualifiedName~PanePointer|FullyQualifiedName~CommanderPointer|FullyQualifiedName~PaneReducer|FullyQualifiedName~CommandPalette|FullyQualifiedName~CommanderSessionBookmarkOwnershipTests"`
- `dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~Keyboard|FullyQualifiedName~Selection|FullyQualifiedName~CommandPalette|FullyQualifiedName~CommandLabel|FullyQualifiedName~CommandEntry|FullyQualifiedName~ContextMenu"`
- `dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore`

ADR-0061 selects these changed consumers and explicit input-owner boundaries rather
than a provider/dependency/full-release rerun. Conformance/record closure/changed
format/complexity and final independent implementation review remain. There is no
new filesystem/provider/dependency contract and no full CI request. Physical-key,
native focus and visible acceptance remain distinct from deterministic input proof.
## Final selected verification and review corrections

Application selected05 passes227/227 and Presentation06 passes224/224 after the
same-owner dispatch/translation cleanup. App06 builds with0warnings/errors.
Record closure01 proves152abstract records/140closed visible roots/0failures.
Conformance01 originally rejects a Boolean test mode; the final test data names the
expected anchor instead. Complexity01 finds three new issues in movement offsets
and character translation. Switch expressions and a control-character helper in
the same translator remove them without changing input behavior. Structural03 has
format/conformance exit0 and exactly the original three complexity finding objects:
two existing parameter counts and KeyboardHelpPresenter nesting. Exit1 is retained;
there is no clean-complexity claim or threshold change. Initial file-copy and
nonexistent-path/glob reads are retained as operator setup errors, not product defects.

Independent implementation review identifies two proof gaps. The complete Application
palette catalog expectation still has28commands and was omitted by the old filter;
its explicit ordered expectation now includes SelectAll/InvertSelection/ClearSelection.
The old native-editor test manually supplied Shift outside FileList, which the host
does not produce. Its range assertion is replaced with classification, modifier
translation, key translation and mapper in the actual host order. Plain text editors
pass through, while BookmarkSearch Shift+Down retains the existing LeaveSearch route.
No product behavior changes for either correction. Review has no additional required
finding in selection identity, anchor, clamping, admission, repeat or chord handling.

Selected07build fails only on a diagnostic test's unused namespace import, preserved
in its original log. Removing that import gives selected08: Presentation225/225 and
explicit CommanderSessionPaletteCatalogTests4/4, both0fail/0skip. Application227from05
remains applicable to unchanged behavior/tests; only the corrected four catalog tests
need the new execution. The normal App06and record closure01are reused because these
final corrections change only tests. Final changed-test formatting is recorded in
review-format-01.json. Commit uses the normal repository-owned lightweight hooks.

Exact additional commands:

- `pwsh -NoProfile -File D:/NeNeCommander/evidence/selection-20261125/run-selected-05.ps1`
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/selection-20261125/run-selected-06.ps1`
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/selection-20261125/run-structural-03.ps1`
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/selection-20261125/run-selected-08.ps1` — the script preserves the exact225-case Presentation filter and explicit4-case Application filter.

The same existing WinUI keyboard adapter reads Control/Alt/Shift through
InputKeyboardSource.GetKeyStateForCurrentThread; only the pure classifier selects
when its existing Shift reader applies. No new event registration, native focus
behavior or visual composition is introduced. Deterministic mapper/translation
proof and the normal WinUI build establish selected technical acceptance. Physical
keyboard layouts, IME, screen-reader and hide's visible acceptance are unperformed
and are not inferred from these tests. This feature remains excluded from beta.5.
The Issue-body publication question is still pending; no public change, PR, main
integration or cleanup is claimed. No shared-desktop input/settings change occurs.

## Final-test complexity correction at 10:59 JST

The local feature was committed as2cfc3b2. The next-task inspection then discovers
that final review's host-order text test was added after complexity03: its nested
24-case loop has cognitive11/limit10. The earlier three-finding receipt is valid
for its measured inputs, not the final test file. That final evidence gap is explicit.
No production input or behavioral expectation changes. The exact same four contexts
and six keys are now independent DynamicData cases; modifier classification, mapped
BookmarkSearch Down exit and native pass-through assertions are unchanged.

An initial edit attempt reads UTF-8 source with Python's default cp932 and fails before
writing. Its following build/test/complexity09 therefore measures unchanged2cfc3b2;
translation-setup-failure-09.json corrects the intended-scope wording in that original
receipt. The original output stays. Explicit UTF-8 reading applies the real correction.
Build10, SelectionTranslationTests57/57 and changed-file format10pass. Complexity05
has exactly the same three complete pre-existing findings and no new finding; its
exit1 remains explicit. All other code/tests/dependencies are unchanged and prior
proof is reused. final-inputs02 records1366unchanged hashes and this one test change.

Commands: `dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore`;
`dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~SelectionTranslationTests"`;
`dotnet format tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --verify-no-changes --no-restore --include tests/NeNeCommander.Presentation.WinUI.Tests/SelectionTranslationTests.cs`;
`pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect`.
No new source-publication permission, native acceptance, integration or cleanup is claimed.

## Current publication checkpoint — November 25, 21:31 JST

The earlier Issue-body permission hold is resolved. hide explicitly approved the
exact saved issue-body-02.md at 19:59 JST, and the publication completed at 20:07 JST.
issue-readback-03.json retains the exact matching body. A fresh read-only GitHub
connector observation at 21:29 JST confirms the same open Issue #562 and published
scope. That historical proposal body is not a claim that feature source was pushed.

The local implementation and final test-only correction are saved at fa5422b.
Source publication, PR/main integration and physical/visible acceptance remain
pending. beta.5 is separately published from 5727258 and excludes this feature.
Its completed-copy cleanup explicitly excludes issue-562 and selection outputs.
This checkpoint changes documentation only: successful product/build evidence is
reused without additional product tests or CI. No new public write is performed.
The first documentation script failed before writing because universal-newline
reading altered one CRLF in the comparison; raw UTF-8 decoding confirms exact text.
That failed attempt and its no-change commit result remain recorded separately.
