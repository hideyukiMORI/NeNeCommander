# Reactivation input investigation — Issue523

Date: 2026-11-23
Status: diagnostic observation in progress; no product correction or native acceptance claimed.

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
mouse messages produce no routed PointerPressed, and SetKeyboardState plus a P message
does not establish the modifier observed by InputKeyboardSource. Both blank and row
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
