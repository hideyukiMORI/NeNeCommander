# ADR-0089: Restore and remember normal window placement through the settings queue

Status: accepted

Date: 2026-11-23

Issue #567. hide requested remembered window position/size and initial/off-screen
correction at17:21 JST, and explicitly approved the recorded Issue body/destination.
This is the technical contract for that requested behavior, not native acceptance.

## Decision and single mechanism

- Extend the existing AppWindowPlacementAdapter, Application window geometry and
  SettingsSession/ISettingsStore. No second settings file, native import, SDK
  placement store, background timer or window-operation route is introduced.
- Schema6 adds required `windowPlacement`: null means no previously saved placement;
  otherwise it is an exact object with `left`, `top`, `width`, `height` integer
  physical desktop pixels and `state` equal to `restored` or `maximized`. Sizes must
  be positive; positions may be negative. Wrong types, duplicates, unknown fields,
  missing properties and unsupported state reject the whole document. Schemas1-5
  acquire absence in memory, without a migration write. No numeric coercion occurs.
- SavedWindowPlacement owns a validated normal rectangle and only restored/maximized
  state. WindowPlacementTracker owns the latest observed normal rectangle. A restored
  AppWindow.Changed observation replaces it; maximized changes only its remembered
  state; minimized/non-overlapped/unavailable observations retain it. Before the first
  normal observation there is no saveable placement. The initial corrected rectangle
  seeds that same tracker before any requested maximization. Final capture and event
  detachment happen before the normal-close drain. Minimized is never a startup mode.
- WindowRestorePlanner is pure. With no saved rectangle it centers the framework's
  initial size within its current display work area, reducing oversized dimensions.
  With a saved rectangle it chooses the attached work area with the largest overlap,
  then smallest rectangle-to-rectangle squared distance, then left/top/width/height
  order to break ties independently of display enumeration. It reduces each dimension
  to that work area and clamps both coordinates so the full normal rectangle fits.
  This also deliberately brings spanning windows into one work area at next launch.
  Arithmetic uses wide intermediates; extreme accepted Int32 values cannot wrap.
- Initialize before activation, through the same adapter that owns keyboard
  adjustment. Move the own window origin first so the target display DPI change
  settles, then use its existing Apply route for the decided physical bounds.
  A single MoveAndResize across 125% to175% was observed to enlarge840x600
  to1176x840; no scale guess, retry loop or delayed correction is introduced. Restore the corrected normal rectangle before requesting maximization.
  Subscribe only to the application's own AppWindow; capture geometry without waiting
  for XamlRoot scale. Placement is defined in physical pixels, matching existing
  AppWindow/WindowBounds contracts. A changed DPI does not scale saved dimensions a
  second time; logical DIP size may differ. WinUI handles content scale for the target
  display. No stale source XamlRoot scale determines restored target geometry.
- CommanderWindow stops accepting input and captures/detaches tracking, then stops
  pane work. CommanderSession's normal stop queues at most one changed placement in
  its existing complete-value settings queue and always drains SettingsSession in
  finally. Pending prior writes finish in order. All display/bookmark/sort values
  remain current. Once host stopping starts, RenderSession and the direct RenderPanes progress
  route refuse late callbacks; pending and direct file-list focus also stop,
  including the final settings notification already queued on the UI dispatcher.
  This avoids rebuilding closing content while still observing and draining writes.
  A failed placement write remains a typed failure; closing does not
  claim it succeeded. No same-value write occurs.
- Startup reading, placement correction and ordinary move/resize events do not write.
  A malformed/rejected startup document is not repaired by automatic placement-only
  saving on close. It remains untouched until an explicit settings change uses the
  existing write policy. The store's existing malformed-input and identity checks
  remain authoritative. Shutdown/crash/kill and OS session end keep ADR-0071 limits.
- Read/apply failures in startup translation reach the existing defect observer;
  they do not invent a display, guessed rectangle or successful save. An unavailable
  later observation retains the last valid placement. No shared desktop input,
  other window geometry or global display setting is readjusted.

## Reference investigation

Read-only local Nib main64d612e and integration732ce712 (int50) both contain
`EditorWindowLifecycle.cpp::place_at_screen_centre`: scale the design dimensions,
read the nearest monitor work area, and center. Those sources do not clamp oversized
initial dimensions in that method; a settings placement persistence mechanism was
not found in the inspected lifecycle/adapters. Do not claim Nib implements this new
Commander contract or copy an assumed implementation. No Nib files were changed.

Microsoft documents [AppWindow.Changed](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow.changed)
as a steady-state change notification and [MoveAndResize](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow.moveandresize)
as using screen coordinates. The pinned package exposes these stable APIs. Online
placement-persistence APIs are not present in the pinned public metadata and would
introduce another storage owner even if available.

## Selected verification and limits

ADR-0061 selects pure planner/tracker cases, strict schema and round-trip cases,
complete-value preservation, delayed/failing queue and close drain tests, changed
App/test builds, source conformance and architecture boundaries. Existing unrelated
file-operation/provider proof is reused; no dependency or user-file trust boundary
changes. New settings text uses the existing strict parser/ADV-012 cases. Verify
positive and negative/extreme values, old schema migration without writes, absent
and unchanged placement, negative/disconnected displays, ties, oversized work areas,
maximized/minimized ordering and root-scale independence.

An isolated unshown copy may verify real own-window move/resize/capture, startup,
settings path and drain, with all diagnostic substitutions recorded. Do not maximize
or activate a hidden diagnostic if it can expose a window. Native visible
maximize/minimize, physical multi-monitor/DPI transitions and screen acceptance remain
separate environmental evidence. The user's live issue527 App/settings are retained.
