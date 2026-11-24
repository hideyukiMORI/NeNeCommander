# Monitor transfer: engineering investigation

Status: proposed engineering design, not an accepted ADR or visual handoff

Issue #551 / 2026-11-22 / base `7ef9b96`

## Invariant and existing mechanism

Moving the application between monitors must preserve panes, active side, selection,
operation state and unsaved input. Only the existing `WindowAdjustmentSession` /
`WindowAdjustmentPlanner` and `WindowAdjustmentRoute` / `AppWindowPlacementAdapter`
may decide and apply window placement. Application may receive immutable geometric
facts; it must never call OS APIs. ADR-0050 already uses physical rectangles in
Application, so the Issue's prohibition on leaking screen coordinates means no raw
platform objects, OS access or host policy, not removal of that existing value model.

The existing Ctrl+W admission excludes editors, modals, running operations, reads
and launches. That preserves unsaved input by refusing mode entry; this work does
not propose cancelling or closing an editor to move the window. Existing OS window
shortcuts remain untouched. No global hotkey, automatic startup, tray or persisted
placement is inferred from this Issue; #552 owns the separate show/hide request.

## Current limitations

`WindowPlacement` carries current bounds, current XAML scale, current work area and
all attached work areas. It carries neither each destination's scale nor a stable
monitor identity. `WindowWorkAreas` owns an immutable copy, but the attached order is
OS enumeration order. Existing move plans apply `AppWindow.MoveAndResize` once;
there is no monitor-transfer action or completion phase. Maximized and minimized
geometry actions are explicitly refused. All facts are read afresh for each action.

The actual pinned runtime was measured with a separate unshown frameless WinUI
window, without Show/Activate or input injection. See the
[quality record](../quality/2026-11-22-monitor-transfer-investigation.md).

- All 36 baseline transitions changed native bounds/DPI before the API returned.
  The three APIs (absolute MoveAndResize, DisplayArea-relative MoveAndResize and
  absolute Move) all showed the same scaling for these inputs.
- A second physical-bounds fit kept all 24 measured cases inside the requested work
  area. It is a candidate technique, not a general guarantee or accepted policy.
- XAML scale remained the source scale immediately after 20 of those 24 transfers,
  even though native window DPI had already changed. It agreed after the dispatcher
  advanced. A fresh existing adapter read immediately after a move can therefore
  combine new bounds with an old XAML scale.
- A large transfer from the primary 125% display toward the upper 150% display was
  initially assigned to the neighbouring 175% display because of the resulting
  overlap. The subsequent fit reached the intended display. Using the first move's
  scale as if it necessarily belonged to the requested display is incorrect.

## Proposed interaction for review

Keep Ctrl+W as the entry. Add explicit previous/next-display commands within that
mode; proposed keys are Ctrl+Left / Ctrl+Right, with single activation per key press.
The ordinary h/j/k/l and arrow movement retains its 32-DIP behavior. Hints must be
generated from the same binding table, with localized labels and accessible outcome
text. No new pointer control or final layout is accepted by this document.

For a predictable cycle, sort distinct work-area rectangles by left edge, then top
edge, then width and height; cycle and wrap through that order, independent of OS
enumeration. Exact duplicate work areas are one geometric destination. This is a
proposal requiring examples for vertical and staggered layouts before acceptance;
it must not be described as the Windows display-number order. A single distinct
work area produces an explicit no-other-display refusal and no apply call.

For an ordinary window, the intended result is centred inside the destination work
area, preserving its effective size unless it must shrink to fit. The exact
rounding, minimum and failure semantics are unresolved until the mechanism below
is accepted. Maximized/minimized requests should initially retain ADR-0050's
explicit refusal: `r`, transfer, `m` remains an explicit sequence. Supporting a
maximized transfer without losing normal restore bounds is a separate alternative
that requires isolated runtime proof; implicit restore is not accepted.

Hidden-window actions must not show or activate the window. A future #552 route
must qualify its own visibility and mode admission; this proposal adds no hidden
background trigger. Re-enumerate on every request and refuse a removed destination;
never use a saved index to target a replacement display. Leaving the mode must
remain unconditional and retain the captured pane focus return.

## Mechanism decision still required

Two plausible mechanisms remain. Neither is accepted production code here.

1. **One synchronous transfer, then bounded qualified completion in the existing
   route.** Application decides the destination and the landing; after the first
   apply, the same route reads new physical facts and asks the same Application
   owner to decide the fit. No size/target decisions belong in the adapter. A
   completion must be bound to the exact transfer and expected open state, and be
   refused after stale input, removal or an intervening external move. It cannot
   loop until success or introduce a timer/polling lifecycle. Physical fitting
   avoids assuming that XAML scale is current, but effective-size preservation in
   the neighbouring-display case still needs proof. The 120 ms sampling timer in
   the diagnostic fixture is not a proposed product implementation.
2. **Read a destination scale at a declared native boundary before planning.**
   This would extend Infrastructure.Windows under SEC-014, with source-generated
   marshalling, closed failure translation and an accepted ADR. It cannot add a
   DllImport to App or use the probe's diagnostic imports as product precedent.
   `GetScaleFactorForMonitor` is an API candidate, but exact/custom scale semantics,
   topology races and the OS's subsequent resize must be verified. A failure's
   populated fallback scale must not be treated as a successful measurement.

A larger geometry snapshot alone does not settle this choice. The selected design
must define apply failure after a first effect, the freshness checks, whether a
follow-up result is planned or applied, integer overflow/rounding, preferred minimum
larger than the destination, and how a changed presenter stops the transfer.

## Rejected shortcuts

- Trust FindAll order as a stable cycle or infer a destination scale from its size.
- Multiply by the immediate XAML scale after moving; the local measurement disproves it.
- Assume the DisplayArea overload handles all fitting and DPI; the baseline does not.
- Cache the initial geometry or monitor indices across requests.
- Put arithmetic or destination selection in AppWindowPlacementAdapter/code-behind.
- Restore/maximize implicitly, show a hidden window, or send keyboard/mouse input to
  the shared desktop to obtain missing evidence.
- Treat the fixture's success as visible-product, native-key, focus or reconnect proof.

## Required acceptance and migration

Allocate ADR-0085 or the next unreserved number only when selecting the mechanism;
ADR-0084 is already used by local Issue #547. Refine ADR-0050, COMMAND_MODEL,
KEYBOARD_MODEL and GLOSSARY atomically with implementation and the exact design
handoff. No constitutional registry or production type changes in this investigation.

Focused proof must cover cycle order/permutations/wrap/deduplication, one display,
negative coordinates/gaps/staggered geometry, distinct DPI and rounding, size/minimum
boundaries, removed/replaced displays, every presenter/visibility state, stale
completion, reentrant OS changes, scope admission and pane/draft preservation.
Presentation proof must cover key translation, held-key suppression, aliases and
canonical hints. The actual route/adapter requires an isolated visible runtime
session for multi-monitor, focus, rapid commands and topology changes before feature
acceptance. No test changes or full/CI/mutation run is justified for this prose-only
investigation.

Framework references, checked 2026-11-22:
[AppWindow coordinate model](https://learn.microsoft.com/en-us/windows/apps/develop/ui/windowing-overview),
[MoveAndResize](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow.moveandresize),
[WM_DPICHANGED](https://learn.microsoft.com/en-us/windows/win32/hidpi/wm-dpichanged),
[GetDpiForWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforwindow),
[GetScaleFactorForMonitor](https://learn.microsoft.com/en-us/windows/win32/api/shellscalingapi/nf-shellscalingapi-getscalefactorformonitor).
The documentation describes the APIs; only the local evidence establishes the
observed pinned-runtime behavior. No timing guarantee is inferred from either.

## November24 evidence supplement — still proposed

The current public Issue551 remains open with no milestone. A tools-disabled
background Claude consultation based only on its public requirements recommends
one-shot n/p cycling inside Ctrl+W and logical-size preservation. It does not approve
a final screen or choose the engineering contract. Its maximized-transfer and
relative-position recommendations remain alternatives to the earlier centered,
normal-only proposal. Current package APIs were inspected, and an owned unshown
probe confirms GetForMonitor scale retrieval on the four available displays while
GetForWindow returns E_NOTIMPL. The wrapper requires Windows11 build22621; product
minimum22000 and custom scales remain explicit design concerns.

A new compensated-first-request plus precomputed-final-rectangle candidate lands
exactly in all24 normal/large ordered-pair cases, including the previous wrong-display
counterexample. The first request alone misses exact dimensions once, so no one-call
claim follows. No product source is changed and no asynchronous timing guarantee is
inferred. See the updated quality record for all raw results, failures, API references,
commands and remaining acceptance. ADR-0089 startup/settings tracking must now be
preserved; the old0085 ADR suggestion is obsolete and the next free number must be
checked across active worktrees. Technical design is still unresolved, not accepted.
