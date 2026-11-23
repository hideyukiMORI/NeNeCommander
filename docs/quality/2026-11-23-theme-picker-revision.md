# Theme picker revision proof — 2026-11-23

Publication addendum12:27 JST: hide approved the prepared payload at12:25. Four commits/43
changed files through6182b0d and the exact4,647-byte body/title are now in OPEN/Draft PR565,
verified against mainbaseb82aa1e with252 total PR files. The connector's metadata-write403
was handled by the existing authenticated CLI with the same approved content. `publication-approval.json`,
`push-approved.log`, `pr-edit-approved.log` and `pr-readback-approved.json` retain evidence.
The local-only statements below describe the pre-publication checkpoint. This receipt addendum
is saved locally; it is not silently added to the precisely approved6182b0d payload.

Issue: #527. Base for this revision: local `15f5244`; published Draft PR565 remains
`35776de` until an explicitly authorized update. Main is `b82aa1e`.

hide resumed at10:26 and selected the Nib interaction at about10:31: candidate movement
previews; Enter/click accepts and closes. The pause for hide's Notepad comparison at11:09
ended with the12:06 resume instruction. No test was launched during the pause.
The accepted contract is [the interaction revision](../design/2026-11-23-theme-picker-revision.md).

## Ownership and change

SettingsSession remains the sole accepted-value owner and complete-value write queue.
`ColorSchemePickerOpen` captures the parent settings state; submissions and cancellation
require its exact reference. Settings' compact theme button and the palette's searchable
`:colorscheme` command reach that same owner. Unqualified and stale requests cannot accept
a preview. Both panes remain frozen. Same-theme acceptance closes without a write; another
theme closes and uses the existing queue, retaining the latest other preferences and warnings.

SettingsSchemeViewState owns query/candidate/preview only. The host applies preview through
the existing ColorSchemeChanged/CopySchemeInPlace route, preserving brushes. Cancellation
and zero matches restore the latest accepted theme. Progress/save-completion renders retain
query, candidate and row storage. Settings no longer realizes129 radio rows: a separate
virtualized ListView is the sole theme scroll area, with fixed search/current-name/hints/Cancel.
The existing command-palette key map and native text/IME route are reused. Hint keys are
projected from that map. Its repeat guard now retains both Execute and Cancel across owner
changes, preventing held Escape from closing the returned parent or clearing pane selection.

## Selected scope and results

ADR-0061 focused proof covers the changed state constructor, command admission, save queue,
palette consumers, input repeat boundary, App resources and layout. No provider, filesystem
trust boundary, settings storage primitive, dependency or catalog-import behavior changes.
The older catalog/import/notice/contrast proof remains applicable. This revision does not
request full/deep CI, coverage or mutation merely to save or publish. Release proof remains
separate. Existing tests are reused after their final affected inputs; overlapping filters
and repeated locale runs are not added as distinct behavioral cases.

| Scope | Result / evidence under `D:/NeNeCommander/evidence/theme-picker-20261123/` |
|---|---|
| Application picker/session/palette/settings boundaries |46/46, `application-focused-first.log`; final affected build0 warnings/errors |
| Presentation search/preview/settings/palette |61/61, `presentation-focused-first.log` |
| Cancel repeat and adjacent keyboard contexts |22/22 after the new baseline failure, `keyboard-repeat-fixed.log`; overlaps prior61 |
| Canonical localized theme key hints |1/1, `theme-hints-final.log`; affected build0 warnings/errors |
| Unmodified product App |`app-final-build.log`, exit0, warnings0/errors0 |
| Normative scan |112 rules, `conformance-final.log`, exit0 |
| CS-013 |`complexity-final.json`, exit1 with exactly the same5 pre-existing findings as `complexity-second.json`; no new finding, not a clean pass |
| Changed C# whitespace |`format-final-corrected.log`, exit0; workspace-load warning retained |
| Actual Application DLL state closure |7 compiler probes, `product-closure-checks/closure.json`; two positive cases compile, five invalid cases fail with expected diagnostics |
| Hidden real controls |same91 assertions in each of en-US/ja-JP/zh-Hans; all pass, one explicit save each, no activation/visible window |
| Resource/binary parity |536 keys per locale,132 XBF and4 common DLLs equal the product; PRI byte-equal; `final-proof.json` |
| Fixture source boundary |169 App sources equal after declared substitutions and newline normalization; `fixture-source-parity.json` |
| Evidence inputs |1,227 source/test/tool/configuration files frozen in `final-input-hashes.json` |

Compiler probes use the actual Release DLL and pinned compiler: known copy and external
abstract copy remain legal; external concrete ordinary/copy/override, direct picker construction
and ReturnTo mutation fail. No all-root closure rerun was needed for this one changed root.

## Exact selected commands

From `D:/NeNeCommander/worktrees/issue-527` (each exit0 unless stated):

```powershell
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~ColorSchemePickerSessionTests|FullyQualifiedName~CommanderThemePickerTests|FullyQualifiedName~CommanderSessionPaletteCatalogTests|FullyQualifiedName~CommanderSessionPaletteDispatchTests|FullyQualifiedName~CommanderSessionPaletteOwnershipTests|FullyQualifiedName~SettingsSessionTests|FullyQualifiedName~ColorSchemeSettingsQueueTests'
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~SettingsSchemeViewStateTests|FullyQualifiedName~SettingsPresenterTests|FullyQualifiedName~SettingsKeyboardTests|FullyQualifiedName~CommandPalette|FullyQualifiedName~KeyboardPaletteTests'
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~KeyboardPaletteTests|FullyQualifiedName~KeyboardModalTests|FullyQualifiedName~SettingsKeyboardTests|FullyQualifiedName~BookmarkKeyboardMappingTests'
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~ThemePickerKeyHintPresenterTests'
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore
pwsh -NoProfile -File ./eng/conformance.ps1
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
$changed = @(git diff --name-only -- '*.cs') + @(git ls-files --others --exclude-standard -- '*.cs')
dotnet format whitespace NeNeCommander.slnx --no-restore --verify-no-changes --include $changed
pwsh -NoProfile -File D:/NeNeCommander/evidence/theme-picker-20261123/verify-product-closure.ps1
& 'C:/Program Files (x86)/Windows Kits/10/bin/10.0.26100.0/x64/makepri.exe' dump /if src/NeNeCommander.App/bin/Release/net10.0-windows10.0.26100.0/win-x64/NeNeCommander.App.pri /of D:/NeNeCommander/evidence/theme-picker-20261123/app-pri-dump.xml /dt detailed
python D:/NeNeCommander/evidence/theme-picker-20261123/verify-final-proof.py
python D:/NeNeCommander/evidence/theme-picker-20261123/freeze-inputs.py
```

CS-013 exits1 for the five unchanged findings. Whitespace's initial exit2 was corrected by
CRLF conversion in the two newly added hint files only; normalized content is recorded in
`newline-only-correction.json`. Successful behavior tests/builds were not rerun for newlines.
Fixture builds use `dotnet build App/NeNeCommander.App.csproj -c Release --no-restore` from
`D:/NeNeCommander/outputs/theme-picker-20261123/control-fixture`. Latest build is
`fixture-eighth-build.log`, exit0. Each locale is an argument to that copied App executable,
launched with `Start-Process -WindowStyle Hidden`; exact executable/PID/time is in launch JSON.
Successful processes exited0. Launch records and all earlier failures remain retained.

## Control proof and retained failed attempts

The copied App uses owned empty panes and per-locale settings under its D-drive fixture.
Activation and native focus calls on exercised paths are replaced; its real XAML, view/session,
resource and writer routes are preserved. Timer/reflection/property-driven diagnostics remain
outside the repository product. The source substitutions and one stale documentation comment
are explicitly enumerated. Both real foreground ownership and visibility abort the fixture.

At real125% scale the final fixture verifies320x300,640x360 and960x600 DIP viewports, fixed
title/search/current theme/count/instructions/Cancel, bounded virtualized rows, no product
ancestor theme ScrollViewer and scrollbar gutter. Candidate navigation reaches the full last
row; first-row API scrolling also reaches it. It tests preview/brush identity, no-result fallback,
both parent-return paths, no repeated/same-theme save and delayed-save completion retaining
the newer query/preview. Final dedicated settings differ only by the explicitly accepted Dracula.

- Initial App CA1822, Application IDE0058, undefined KeyHintBrush and three new cognitive
  complexity findings were corrected in their owning code; original logs remain.
- The new held-Escape baseline failed with MappedKeyboardIntent after returning to Settings.
  The existing repeat guard fix passes the22 directly affected cases.
- Fixture source initially failed its own formatting/analyzers; corrected diagnostic copies
  and all build logs remain. No failed-build DLL was presented as final proof.
- First en-US run accepted before its programmatic TextChanged was delivered; later steps
  await a dispatcher turn. Second run requested Settings while startup pane work still owned
  the host; the fixture now waits for that work and asserts the initial owner. These were
  fixture sequencing defects, not a product-settings failure claim.
- Third run inspected framework ancestors beyond the application content root and rejected
  a framework ScrollViewer. Inspection now verifies every ancestor through the actual product
  root and asserts that root is reached; no product outer scrollbar was added or excused.
- Fourth/fifth runs found a direct virtualized `ScrollIntoView(last, Leading)` jump leaving
  the last row4 DIP below the viewport (offset4100 vs then-maximum4104). This diagnostic-only
  direct jump still clips in the final English run and is explicitly recorded as false,
  independently of the required behavior assertions. The actual MoveNext route advances
  through candidates and ends with the complete row visible. This is not a claim that every
  arbitrary direct jump is fixed. [Microsoft's API remarks](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.listviewbase.scrollintoview)
  were consulted about layout/virtualization; no speculative product workaround was added.

## Limits and next work

Hidden controls do not establish physical keys/clicks/focus, IME, speech, rendered-pixel quality,
Windows contrast mode, other DPI, unmodified startup/restart or hide's visual acceptance.
Timing fields in the fixture measure property-driven hidden work through UpdateLayout only;
they are not the requested Notepad/activation/resize comparison. Those investigations and
instant language switching remain separate. The language dependency is still Draft PR559.

PR565 remains Draft; main merge, release and Issue completion require their own acceptance.
No new payload is sent under the old35776de-only publication permission. The current worktree,
fixture and evidence are incomplete and retained. No new cleanup rejection or hide cleanup
request exists. Independent review and local commit completion are recorded in the current
daily report/handoff rather than inferred from this implementation record.
