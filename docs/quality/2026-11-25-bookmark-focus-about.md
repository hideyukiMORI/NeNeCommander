# Bookmark focus and About — Issue #581

Status: selected technical proof accepted; interactive trial pending

Request/design: November 24, 2026 JST. Implementation: November 25.
Branch `fix/581-bookmark-focus-about`, worktree
`D:/NeNeCommander/worktrees/issue-581`, base `8d1c247`.
The accepted B direction is in [the handoff](../design/2026-11-25-bookmark-focus-about.md).
All raw diagnostics live in `D:/NeNeCommander/evidence/bookmark-focus-20261124`;
owned copies live under the same task name in `outputs`. Preserve the running
interactive02 and all unfinished feature/release work.

## Scope and invariant

The existing BookmarkBrowseView/BookmarkKeyboardView focus route and
KeyboardHelpView entry/close/scroll route remain canonical. At unchanged width,
scale, language and data, changing category/list/search focus must not move the
browse body or key band. Active headings and selected rows must remain distinct
from retained inactive selection, including an empty list. About uses the
executable's generated metadata rather than another version constant.

ADR-0061 scope: affected App build, native control/layout/focus/localization and
metadata evidence, policy/format/complexity checks. No provider, dependency,
Application, Domain or Presentation source changes. No full gate, mutation,
release CI or unrelated suites are justified by this bounded adapter correction.
Release #580 keeps its separate integration/freshness requirements.

## Baseline and retained attempts

- Baseline product `cd3ecab` under source `8d1c247` is copied with 178 App
  inputs: 176 exact source copies and declared project/composition substitutions.
  The owned observer and private settings are separate from product sources.
- Japanese/Ubuntu/1.25 scale attempt 3 collects 315 geometry samples over seven
  focus phases and 28 RenderTargetBitmap images. The requested 16 ms interval is
  not a fixed frame cadence; image encoding/dispatcher scheduling affects timing.
  This records the owned non-input-desktop window, not hide's desktop.
- At the tested width, body height alternates between 450.4 and 442.4 DIP.
  The footer grid changes from 33.6 to 41.6 DIP as category hints wrap; the list
  BorderThickness remains 1 DIP. The modal's y=60 and height=540 do not change.
  The empty list has UseSystemFocusVisuals=true. The evidence establishes the
  8 DIP layout movement and distinct native focus outline; it does not establish
  a physical spring trajectory or rule out additional composition effects.
- Attempt 1 records metrics successfully. Attempt 2 requests OpenBookmarks while
  pane work is still owned and is refused; its failed receipt is preserved.
  The observer is corrected to await idle; attempt 3 succeeds with 11 checks.
- Diagnostic layout prototype 4 turns off the two list system outlines and tries
  detached template measurement. It measures only 8 DIP instead of the loaded
  41.6 DIP category hint height and retains the 8 DIP body movement. This method
  is rejected; its completed observer is not claimed as a successful layout fix.

## Build and native progression

`dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj -p:Configuration=Release --locked-mode`
passes. Product build attempt 1 fails CsWinRT1028/1030 and IDE0028 in the new
measurement peer. The nested WinRT peer is made partial and returns the documented
null child collection. No unsafe blocks, suppression, baseline or dependency
change is added. [The WinUI API contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.peers.automationpeer.getchildrencore)
documents null as the base no-child result.

`dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore`
attempt 2 passes with 0 warnings and 0 errors. The copied diagnostic App also
builds with 0 warnings/errors; its analyzers are disabled only in that explicit
diagnostic copy, so it does not replace the ordinary product build.

Native attempt 5 confirms normal-width geometry stability through the initial
bookmark phase and two category/list cycles (45 samples each), the inert
measurement tree and 39 checks, then fails the immediate heading assertion on
entering search. The original receipt is retained. The observer now records the
immediate heading state and yields one dispatcher interval before that visual
assertion to distinguish FocusManager's update from delivery of the native focus
event. Attempt 6 resolves the observer timing issue; the original failed attempt remains recorded.

## Accepted product proof

Native attempts 6 (ja-JP frames), 7 (en-US), 8 (zh-Hans) and 9 (populated ja-JP)
pass 107 checks each. Each records 399 geometry samples over two widths (820 and
580 DIP) and seven focus phases. Body and hint-band geometry remain stable when
focus changes. Inert measurement hosts expose no accessible children or hidden
buttons; the sole real hint band's 8/1 control counts and Invoke route pass.
About agrees with executable metadata, and Help close restores file-list focus.
The Japanese wide, narrow and About render images were visually reviewed.

The added draft probe, native 10, passes 64 checks: bookmark/category drafts
collapse the whole browse hint band, Escape returns to the existing browse/focus
route, and live en-US/zh-Hans/ja-JP changes update all loaded measurement content.
Native 11 passes 107 checks and 399 samples against the final product correction.
All completed launch receipts have exit 0, unchanged ordinary settings and the
same input desktop. These observers send no physical input and never switch the
desktop. They are native control/layout evidence, not hide's visible acceptance.

Commit-check attempt 1 rejects a boolean visual-mode parameter in the new heading
adapter. The final correction reuses closed PaneRowMark.FocusInActivePane and
Unmarked; brush mappings stay the same. Product build 3 and diagnostic build 4
both pass with 0 warnings and 0 errors. Native 10/11 validate this exact source.

Whitespace attempt 1 finds only missing final newlines in four new files. Those
are normalized without changing code; whitespace 2 passes. Whitespace 3 passes
for the final closed-mark edit. The tool's workspace-loading warning is retained.
Complexity attempt 2 exits 1 with exactly the same three findings as the accepted
UI-trial baseline: TransientScopeOwners and TransientScopeSnapshot have five
parameters, and KeyboardHelpPresenter has nesting four. Every finding field
matches; there is no new finding. Commit-check 2 passes all 112 normative rules
and the security registration of 24 adversarial cases. It is not a full gate.

`final-source-parity-01.json` freezes 182 App inputs: 180 byte-identical copies
plus the two declared diagnostic project/composition substitutions. Four product
dependency DLL hashes match; three observer fixture hashes are recorded. Final
independent read-only reconciliation requires no correction. It confirms the
closed-mark mapping, native 10/11, builds, formatting and unchanged complexity.
Earlier unchanged-language/About/template cases are reused; only the impacted
final mapping and added draft/language boundaries are rerun.

## Commands and results

Commands run from the issue-581 worktree unless an absolute script path is shown:

- `dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj -p:Configuration=Release --locked-mode`: success.
- `dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore`: final attempt 3 success, 0 warnings/errors.
- `dotnet format whitespace src/NeNeCommander.App/NeNeCommander.App.csproj --no-restore --verify-no-changes --include src/NeNeCommander.App/Views/BookmarkRegionFocusView.cs`: final attempt 3 exit 0; earlier attempt 2 covers the other changed C# files.
- `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect`: exit 1, the same three complete findings as baseline, not a clean-complexity claim.
- `pwsh -NoProfile -File eng/check.ps1 -Mode Commit`: attempt 2 success.
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/bookmark-focus-20261124/run-accepted-controls-01.ps1 -Locale ja-JP -Attempt 10 -Scenario focus-drafts`: 64 checks, exit 0.
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/bookmark-focus-20261124/run-accepted-controls-01.ps1 -Locale ja-JP -Attempt 11 -Scenario focus-metrics`: 107 checks, exit 0.

## Limits and pending work

A separately configured interactive trial follows. No feature push, PR, main
integration or beta.5 release is claimed. About correctly displays the current
executable's 0.1.0-beta.4 informational version; release Issue580 owns the later
version change. This is author/copyright text, not an Authenticode signing claim.
Actual OS high contrast, other scales and physical-input/human acceptance remain
unproved by this 1.25-scale observer. The mock canvas proves none of those cases.
