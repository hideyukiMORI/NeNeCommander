# Presentation model and input refactor — 2026-10-18

Issue: #332. Scope: ADR-0061 focused behavior-preserving refactor, following the
priority order authorized by hide on 2026-10-18.

## Invariant and canonical path

The existing presenters remain the sole projection path and KeyboardIntentMapper
owns the same three binding tables, clock, pending chord and palette Enter guard.
Public binding properties and their signatures are unchanged. No App/XAML,
Application/Domain/Infrastructure behavior, dependency, threshold or CI change.

BookmarkBrowsePresentation derives its selected row from the owned row snapshot,
LocationRow derives its UIA ID from its validated name, AddressEditorPresentation
derives status from SourceState just as it already derives SelectAll, and
SettingsPresentation consumes the existing complete SettingsSnapshot. Their
internal constructors drop from five inputs to four without new argument bags or
compatibility overloads. Presenter callers migrate together. Settings/source and
row/name guards now protect the derived facts; the obsolete independent visibility
and automation-ID inputs no longer exist.

Map separates context routing, palette repeat admission and pane/chord mapping;
its initial Enter guard still precedes every context. Chord clearing, raw-event
pass-through, expiry, mapped-key cancellation and repeat suppression retain order.
Map falls 38→12 logical lines, cognitive 17→6 and nesting 2→1. The new methods also
meet all five limits.

## Proof

Using SDK `D:/NeNeCommander/tools/dotnet-10.0.401`, with TEMP/TMP under the task's
D-drive output directory, from `D:/NeNeCommander/worktrees/issue-332`:

```powershell
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --locked-mode -p:Configuration=Release
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~Keyboard|FullyQualifiedName~BookmarkManagerPresenterTests|FullyQualifiedName~BookmarkDraftPresenterTests|FullyQualifiedName~BookmarkNavigationPresenterTests|FullyQualifiedName~BookmarkPresentationContractTests|FullyQualifiedName~BookmarkPresentationBehaviorTests|FullyQualifiedName~SettingsPresenterTests|FullyQualifiedName~LocationsPresenterTests|FullyQualifiedName~LocationsInputTests|FullyQualifiedName~AddressEditorPresenterTests|FullyQualifiedName~KeyHint'
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~LocationsPresenterTests'
python -X utf8 D:/NeNeCommander/evidence/presentation-input-refactor-20261018/Verify-Migration.py
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

Both final Release builds pass with zero warnings/errors. The initial 114-case run
passed 113 and failed one because Python's Windows default decoding accidentally
changed two expected middle-dot strings in LocationsPresenterTests. UTF-8 was made
explicit and the original expectations restored. The affected test project was
rebuilt; all nine Locations cases then passed. The other 105 cases have unchanged
inputs and are reused: 114 distinct focused cases are now successful, with no
remaining failures or skips. The App build's product inputs were unchanged by this
test-only correction and it is reused.

All 52 old methods moved exactly once with names, attributes and assertions
preserved. Long cases extract named assertion responsibilities; the owned-context
case moves read-only assertions earlier but keeps its mapped-event order. The
bookmark guard adapts the removed selected-row argument. The retained migration
script checks these transformations, public declaration lines and exact binding
tables. Two new cases prove defensive collection copies and selected-row identity.

Changed-file `dotnet format style NeNeCommander.slnx --no-restore --include
<changed C#> --diagnostics IDE0005 --verbosity minimal` removed unused imports.
`dotnet format whitespace NeNeCommander.slnx --no-restore --include <changed C#>
--verify-no-changes --verbosity minimal` exits 0 with the recorded workspace-load
warning. Normal commit policy/security/message checks remain enabled.

The final cs013-v1 inspection returns 1 as expected for 141 remaining findings:
55 product, 86 tests, zero engineering. All changed/new declarations have zero
findings. There are 765 sources, 1,205 types, 7,917 callables and 7,298 unresolved
invocations. Method/cognitive/nesting/parameter/type findings are 48/22/10/33/28.
The unchanged #324 tool's 24 fixture groups and seven launcher cases are reused.
No full/deep CI, broad regression, coverage/mutation, live UI/provider tier or
release #94 proof was selected or claimed.

## Integration and next work

Permanent proof: `D:/NeNeCommander/evidence/presentation-input-refactor-20261018/`.
The Issue PR and completion record retain squash integration and clean main
synchronization, followed by checked removal of the completed task worktree/output.
Branches, commits and permanent evidence are retained. Global enforcement remains
disconnected until all in-scope findings are resolved.

The remaining authorized order is shared file-operation contracts/execution,
Windows/WSL providers, settings attempt ownership, remaining test/flow findings,
then the single global checker integration. Each is a focused Issue/PR sequence.
Release #94 remains separate environmental work.
