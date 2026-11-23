# Reactivation input investigation — Issue523

Date: 2026-11-23
Status: correction implemented; selected proof and hide's left/right reproduction confirmation pass. Publication/main integration remain separate.

hide accepted the immediate-language, compact-palette/context and saved-window-placement
candidate with「どれもいい感じだと思う」after the normal20:34 launch from issue-526.
This is overall user acceptance, not a measured environment matrix. hide still observes
that Ctrl+P sometimes does nothing after returning from another application by clicking
the file-list blank area; clicking the active pane's file text then permits Ctrl+P.
The blank-area clarification narrows the older, broader Issue523 report.

Worktree: `D:/NeNeCommander/worktrees/issue-523`, branch `fix/523-reactivation-input`,
base5712d03. All three accepted features are in this base. Evidence and experimental
copies are respectively `D:/NeNeCommander/evidence/reactivation-input-20261123` and
`D:/NeNeCommander/outputs/reactivation-input-20261123`. The accepted issue-526 output,
normal settings and existing Draft PR568–570 are separate and unchanged.

## Invariant and ownership

The existing CommanderWindow focus helper and WinUiKeyboardInputTranslator /
KeyboardIntentMapper remain the sole keyboard route. Modal, text/IME, active-pane and
stopping ownership must remain intact. Do not relocate the key handler to encompass
the caption or add a shortcut dispatcher without proving the observed cause.
There is no product source change at this checkpoint.

Inspection confirms that OnActivated already queues FocusActiveFileListWhenIdle,
which retains the text/modal and stopping guards. FocusFileList focuses the selected
row or its ListView. PreviewKeyDown belongs to InputSurface; the caption is its sibling.
Both file lists have no app-owned blank-area pointer handler. These facts alone do
not establish which layer loses the reported key.

The [official WinUI focus design note](https://github.com/microsoft/microsoft-ui-xaml/blob/main/docs/design-notes/focus.md)
distinguishes native island focus from the XAML focused element. The public
[InputFocusController](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.input.inputfocuscontroller?view=windows-app-sdk-1.8)
and [InputKeyboardSource](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.input.inputkeyboardsource?view=windows-app-sdk-1.8)
provide separate observation points. This motivates measurement; it is not evidence
that a framework defect or TrySetFocus correction is the cause.

## Baseline and isolated experiment

```powershell
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj -p:Configuration=Release --locked-mode
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore
```

Both succeed; App build has0 warnings/errors. The initial restore incorrectly passed
`-r win-x64` to all project references and failed NU1004. The corrected command does not
change a lock file. Both attempts remain recorded. No whole-suite, coverage, mutation,
CI or unrelated operation test is selected for diagnostic-only work (ADR-0061).

`prepare-native-fixture.py` copies App source under the owned output root, redirects
settings and initial pane locations, and adds event snapshots and scripted interactions.
Only this copied diagnostic project disables analyzers/documentation generation.
Production activation/focus and settings/session/normal-close mechanisms are retained.

`run-isolated-native.py baseline` starts only the owned process on the named
NeNeCommander523 desktop, never the input desktop. Guards check both desktop names and
every native target's owning PID. No SwitchDesktop or SendInput call exists. The owned
watchdog may terminate only its exact child if it fails to exit. The20:45 run/PID40012
exits0 and keeps the input desktop Default.

The native experiment restores the island's native focus after scripted deactivation
and activation. It does **not** reproduce hide's click/Ctrl+P sequence: SendMessage
mouse messages produce no routed PointerPressed, and its P key messages omit the
CharacterReceived event used by the canonical Ctrl+P translation. Modifier-state failure
was initially suspected but was not established by that experiment. Both blank and row
positive-control attempts leave the palette closed. Therefore the latter observations
prove neither the reported failure nor a successful correction. They are retained as
an invalid input experiment, not counted as passing native keyboard proof.

## Human-operated diagnostic

`prepare-interactive-trace.py` creates a separate copied App with three owned files
per pane and a private copy of current settings. The normal settings document is never
restored or overwritten. The unmodified product input route remains; listeners record
activation, native/XAML/island focus, pointer ancestry, sanitized key families, modifier,
keyboard context, composition and mapped-outcome kind. No text, file names or command
query is logged. It sends no input and uses no CUA/desktop automation.

```powershell
dotnet restore App/NeNeCommander.App.csproj -p:Configuration=Release --locked-mode
dotnet build App/NeNeCommander.App.csproj -c Release --no-restore --no-dependencies
```

These commands run from the interactive-trace output directory. Three diagnostic builds
have0 warnings/errors. The first two launches fail in diagnostic formatting: PaneSide's
generated ToString follows its opposite-side property recursively. The exception trace
identifies InsufficientExecutionStackException; no product code is changed for this
diagnostic mistake. Replacing that diagnostic expression with the concrete type name
allows the20:56:48 launch/PID29164 to reach its responding window and normal file-list
focus. The original error/event/build records remain retained. An early post-launch
log read before creation also fails; it is not a product test.

At20:56:52 hide is asked to repeat the actual blank-area/row comparison. The diagnostic
remains running for that interaction. No synthetic click, key, focus change or screen
capture is used. Pending results must be added before drawing a causal conclusion.

## Retained work and remaining proof

The local diagnostic source, logs, private settings, outputs and worktree are retained
because Issue523 remains open. Live PID/path must be rechecked before any rebuild or
cleanup; a historical PID is not ongoing authority to stop a process. Normal user
settings and any user-running app stay untouched. No merge, release, new PR publication
or cleanup completion is claimed. Successful unchanged build evidence can be reused;
the corrected source and directly affected keyboard/focus boundaries will determine
the next selected verification after reproduction.

## Human reproduction and correction

At21:08 hide reproduces the reported condition while the left pane is active and the
right blank area is clicked after returning from another application. The101-event trace
has SHA256 `1702DADB3DC90E05031C6C09D4DD93DDB9684B497D57F07BD6530FC768F1E8B6`.
After the pointer release, focus changes from ListViewItem to an unnamed ScrollViewer.
At21:08:18.145,19.063 and27.344, the native island and WindowFrame receive P while
Control is down, FileList context is current and composition is false. InputSurface's
handler receives none of those keys. Clicking the active left row restores its route;
the21:08:22.753 P reaches the existing handler and the palette is visible at22.854.
The diagnostic closes through the caption at21:09:05.661. Normal settings stay identical.

Only two product files change. The existing PreviewKeyDown and CharacterReceived
registrations move from InputSurface to WindowFrame, each exactly once. In OnKeyDown,
a focused ScrollViewer uses the existing FocusActiveFileListWhenIdle helper before
translation. The helper already admits only FileList context and refuses stopping.
This restores the character target after preview input without a second mapper, focus
timer, pointer dispatcher, native product API, pane-activation rule or text/IME policy.
The active side still changes only through its existing intent. The root-only first
attempt admits arrows but fails character delivery; it is not the final correction.

The final guard reuses the helper instead of repeating its FileList check inside
OnKeyDown. This removes the intermediate new cognitive-complexity11/10 finding.
Final complexity returns exactly the five pre-existing findings, including their
locations/values; no threshold, suppression or unrelated existing code changes.

## Final impact-scoped verification

The copied native fixture locates and focuses the real framework ancestor ScrollViewer
on the identified non-input desktop. It sends process-owned window key/character
messages, not physical input; positive row controls prove the chosen delivery works.
This measures real WinUI routing and focus for the isolated state, not a synthetic
shared-desktop mouse click or the complete physical reactivation sequence.

| Comparison | Observed result |
|---|---|
| Baseline first fixture | Row input works; root arrow/character delivery fails. Two later modal checks incorrectly expect index0 after those failures; those dependent expectations are corrected to compare the captured prior index. |
| Corrected baseline2,24 checks | Exactly four failures: root arrow and character delivery with the left or right pane active. The other20 checks pass. |
| First root-only correction | Root arrows work; the two standalone root-character cases still fail. |
| Focus-restoring correction,27 checks | All pass, including paired preview/character delivery, Ctrl+P from the root, one dispatch, left/right ownership, unchanged passive panes, palette text/Escape and address-editor isolation. Exit0. |
| Final helper-reuse source,27 checks | All pass again after the relevant source refinement. Exit0; input desktop remains Default. |

The first unsuccessful P-only experiment is not combined with these results. The final
Control+P probe includes its canonical control-character event and its ordinary-row
positive control. The duplicate27-check run verifies changed guard source and is not
counted as54 distinct tests. Fixture source/scripts and every attempt remain under the
evidence root (`RoutingFixture.cs`, `prepare-routing-fixture.py`, `run-isolated-routing.py`).

```powershell
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore
dotnet run --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build -- --filter 'FullyQualifiedName~KeyboardTranslationTests|FullyQualifiedName~KeyboardPaletteTests|FullyQualifiedName~KeyboardModalTests|FullyQualifiedName~BookmarkDraftKeyboardTests|FullyQualifiedName~BookmarkKeyboardRepeatTests|FullyQualifiedName~SettingsKeyboardTests|FullyQualifiedName~LocationsKeyboardTests|FullyQualifiedName~WindowAdjustmentKeyboardTests'
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore
dotnet format whitespace src/NeNeCommander.App/NeNeCommander.App.csproj --no-restore --include D:/NeNeCommander/worktrees/issue-523/src/NeNeCommander.App/Views/CommanderWindow.xaml.cs --verify-no-changes
pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect
python D:/NeNeCommander/evidence/reactivation-input-20261123/run-isolated-routing.py routing-final
git diff --check
```

Presentation56 pass with no failure/skip. Final product and fixture builds have0 warnings
and0 errors. The unchanged mapper tests are reused after the host-only guard refinement.
The whitespace command retains the known workspace-load warning. Complexity exits1
for only the same five unrelated findings. Normal commit checks remain enabled; no full
gate, coverage, mutation or CI is selected for this bounded event-routing correction.
Release proof has separate requirements and is not inferred from these focused results.

The intermediate ordinary product build launches at about21:20 as PID45912 with no
diagnostic substitutions/logging and with the real settings retained. hide confirms
「左右とも開くようになった」for returning through the inactive pane and pressing Ctrl+P,
including both active sides. This accepts the reported defect's physical reproduction;
it is not an all-environment, latency, IME/speech or release assertion. Its exact binary
and source hashes are in `manual-fixed-launch/launch.json`. The equivalent guard reuse
occurs after that build and is covered by the final27 checks above; it is not claimed
to be byte-identical to the accepted intermediate binary.

The live process is subsequently absent before rebuilding the ordinary product from
the final source. A separate exact-source product copy was built while the earlier
window was in use; only its two project-reference paths differ, and analyzers remain
enabled. Final source and binary-resource comparisons identify the deliverable. Normal
settings are never restored from an old backup. New copies stay on D and are retained
until integration and required artifact preservation/cleanup are complete.

hide asks whether a release checkpoint exists. The proposed next cut is0.1.0-beta.4,
including the already confirmed themes, immediate language, compact palette/context,
window placement and this correction, plus accepted main changes since beta.3. This is
a scope recommendation: candidate PR integration, release verification and package
verification remain before tagging/publication. The question itself is not recorded
as a command to publish a new release.
