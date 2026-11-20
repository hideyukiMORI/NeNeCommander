# 2026-11-20 Truncated command-title tooltip — Issue #397

Status: implementation and selected verification accepted; PR integration pending.

## Decision, invariant and mechanism

hide chose on 2026-11-20 at 08:10 JST to show the title tooltip only when the
native title is actually truncated. The accepted palette design handoff already
specifies that boundary. Base main 0796619 had an unconditional row Grid tooltip,
introduced in 7b8abe0. The removed Style Setter mentioned by Issue #397 was a
separate ineffective assignment. This change replaces the live assignment.

The existing row ToolTip retains the full localized title. The title TextBlock's
native IsTextTrimmed dependency property is the sole enablement condition. When
the row loads, CommandPaletteView assigns a one-way Binding whose Source is the
realized title TextBlock. The small code-behind event only forwards to that view.
Each load replaces the same binding; there is no retained row state, manual
property-change subscription, width calculation, timer, command or focus path.
Native ToolTip behavior owns showing and dismissal. Full accessible row names
remain unchanged. No package, resource, settings or domain contract changes.

Microsoft's [IsTextTrimmed API](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.textblock.istexttrimmed)
provides the actual layout result. Its [ToolTip implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/main/dxaml/xcp/dxaml/lib/ToolTip_Partial.cpp)
checks IsEnabled before placement and hides an already-open popup when disabled.
This inspection supports the design; the native candidate observations below
provide evidence for the shipped package. Two guessed upstream raw paths returned
404 before the repository tree supplied the correct source path.

## Selected verification

ADR-0061 ordinary view behavior correction: locked Release App restore/build,
changed-file whitespace, source/design review, ordinary Commit checks and owned
native positive/negative cases. Native layout is the relevant decision boundary;
a unit test restating the XAML or framework Binding would not establish it.
No unrelated Application/Presentation suite, coverage, mutation, full CI, deep
review or release is selected. ARC-001, ARC-012, CS-013, CS-022, CS-023 and the
existing command-palette handoff govern this change.

## Execution journal and corrections

Worktree: D:/NeNeCommander/worktrees/issue-397. Branch:
feat/397-truncated-command-title-tooltip. Permanent evidence:
D:/NeNeCommander/evidence/command-tooltip-20261120.

1. Initial compiled x:Bind experiment failed with XAML compiler WMC9999/resource
   lookup error. app-build.log and CommanderWindow-xbind-failed.xaml retain the
   failed input/result. It is not counted as passing proof.
2. ElementName Binding compiled with zero warnings/errors but owned App 10112
   still showed a tooltip on a visibly untrimmed title. The native failure JSON
   and image are retained. That assignment was replaced, not layered over.
3. Direct Binding.Source to the realized title compiled and passed exploratory
   full-title suppression and narrow-title full-tooltip observations in App
   10428. An initial input sequence did not visibly open the palette; explicit
   target activation and observation resolved the setup. No timing claim follows.
4. The added forwarding handler initially raised CommanderWindow's CS-013 type
   size to 301 logical lines. Its unused event argument is now a discard argument,
   bringing the type to 300. Final inspection has the same 26 other findings as
   the prior inspection; path/metric/value/limit comparison confirms equality.
   This is existing debt, not a clean repository-wide complexity result.
5. Saving an exploratory capture directly to the D evidence folder from the UI
   runtime returned EPERM. Captures were then staged in the main repository's
   ignored artifacts/issue397-capture folder and copied with authorized filesystem
   access. The aborted save also prevented that cell's planned App close. The
   first final build exited 0 with seven MSB3026 App-host lock retries. After the
   owned process exited, the explicit corrected build passed with zero warnings
   and zero errors; both logs remain. No warning was suppressed.
6. An accidental unqualified dotnet format invocation ran extra analyzer checks,
   reporting IDE0060 on untouched handlers and CA1812 for CommandPaletteView in
   the design-time workspace. That result is retained in final-format.log and is
   not a formatting pass. The selected canonical whitespace mode below succeeds;
   the real Release compiler/analyzer build has zero warnings/errors.
7. During final native resizing, one edge drag did not resize, a requested drag
   endpoint outside the window was rejected, and immediate capture after native
   maximization returned 'no monitor found for window'. Re-enumerating the same
   owned App restored capture and confirmed the maximized layout. These are
   recorded setup/tool outcomes, not product passes or measured responsiveness.

## Final commands and results

Commands run in the issue worktree; exact build timing/output is in the evidence.

- `dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj --locked-mode -p:Configuration=Release --disable-parallel`: exit 0.
- `dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore -m:1 -nr:false`: final corrected exit 0, zero warnings/errors, 09:15:23–09:15:53 JST. final-closed-build-command.json and final-closed-build.log are authoritative.
- `dotnet format whitespace NeNeCommander.slnx --verify-no-changes --no-restore --include src/NeNeCommander.App/Views/CommandPaletteView.cs src/NeNeCommander.App/Views/CommanderWindow.xaml.cs`: exit 0, with the existing workspace-load warning retained in final-whitespace.log.
- `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect`: exit 1 for 26 remaining findings, with no introduced finding. final-complexity.json retains the complete measurement; source-binding-complexity.json retains the corrected 301-line failure.
- `git diff --check`: exit 0.

Normal Commit-hook and PR results follow below after observation. Successful
native/build proof is reused for documentation-only changes and PR transitions.

## Final native observations

Owned App 11128 started at 09:17:23 JST from the final Release output. App DLL
SHA256 is B9EEFC9635A408BDDA0DAABBA976DD55C73DB70E3AE3E9DA002E93BDF9510942.
The source/tool/configuration manifest contains 1,016 inputs. Observations and
screenshots are retained in native-final-observations.json and final-*.png.

| Case | Observed result |
|---|---|
| Full refresh title, wide layout, pointer dwell | No tooltip in screenshot or accessibility tree |
| Same refresh title, narrow layout and visible ellipsis | Full localized title appears in native tooltip and accessibility tree |
| Filter to Ctrl+F6 while prior tooltip is open | Old tooltip disappears and only the matching sort row remains |
| Filtered sort title, narrow layout and visible ellipsis | Native tooltip exposes its full localized title |
| Maximize the same filtered layout, then dwell on its full title | No tooltip after the title fits |
| Two Tab inputs | Palette and filtered result remain; no command executes |
| Escape | Palette and tooltip disappear |

The UI helper's focused_element repeatedly reported the old LeftAddress element
while visible palette/search state changed. Therefore the Tab observations do
not claim verified native focus ownership, screen-reader speech or a complete
keyboard/IME matrix. Existing focus/command source is unchanged. No filesystem
command was selected or executed. Narrator, DPI, high contrast, all schemes and
performance distributions remain separate environmental evidence.

At 09:21:53 JST, native-final-postflight.json confirms all 1,016 inputs, the App
DLL and the original 428-byte settings hash are unchanged, and no App process
remains. The settings SHA256 remains
7EA7AB2FB5B06A68D6015500A227BA7AEA3B7F1DA322430834B4B1361B2630EF.

Independent read-only review found no mandatory code/design correction. It
confirmed native trimming ownership, same-row binding lifetime and replacement
on Loaded, and requested the earlier compiled-binding wording be corrected to
the actual Source Binding mechanism. This final record applies that correction.
The reviewer did not rerun already successful proof.

## Integration and retention

Main integration and removal of this completed worktree remain pending. Preserve
branches/commits and all evidence before bounded cleanup. The four older
rejected cleanup targets remain separate and must not be retried. This change
does not accept PR402's bookmark screen, close #453 or publish a release.
