# Opposite-pane header action integration

Issue #547 / ADR-0084. Engineering adoption: 2026-11-22, after hide approved the
2,593-byte Claude brief and its tool-free consultation completed at 21:34 JST.
The original advice is retained in the permanent local evidence. This is an
engineering integration specification, not native screen acceptance or a /design artifact.

The source remains the active pane's currently listed folder. The button emits
only OpenCurrentLocationInOtherPane through the existing host forwarder. It never
chooses a pane, reads a path or changes activation. DualPaneSnapshot's existing
readiness remains the sole admission predicate; the Presentation action only projects it.
Existing modal overlays block pointer access, and CommanderSession revalidates every intent.

Adopt a 24 DIP square target, 16 DIP folder-plus-arrow vector and 4 DIP adjacent
gaps between path and status. The pane number keeps the existing 10 DIP gap.
Both headers reserve the same target slot, including when its inactive button is
collapsed. The active button points toward the opposite pane, preserving position
and path ellipsis when activation changes. Keep the existing 34 DIP header.
The glyph is authored vector geometry, not an external asset. Its coordinate
values (including the mirrored form) are framework drawing geometry, not styling.
All dimensions, spacing and typography remain semantic tokens; the standard WinUI
Button template retains hover, press, disabled and high-contrast theme states.
The normal foreground/background use existing semantic pane/text brushes.

Set AllowFocusOnInteraction=False and IsTabStop=False. Ctrl+E remains the keyboard
path. Tooltip and UIA Name are the same complete localized sentence, with the key
label obtained from the canonical key map. Each direction has a full English and
Japanese format resource. LeftOpenOtherPane and RightOpenOtherPane are stable IDs.
The passive button is absent from accessibility and pointer hit testing; its slot
remains. Loading, launching, missing source and pending operations disable the
active button without opacity. No new focus restoration or click policy is added.

Two consultation suggestions are deliberately bounded by the accepted contract:
same-location remains enabled and idempotent, as ADR-0084 requires; narrow widths
ellipsis the path while retaining the reserved action, instead of introducing an
unaccepted width threshold. Existing narrow-window/header limits remain explicit.
No new disabled-reason state or provider comparison is created in Presentation.

Selected proof: direction/activation, missing/retained listing, pending opposite
read, operation modal, same-location no-op, canonical localized shortcut, both
resource catalogs, merged count projection and affected App compilation. Existing
core/keyboard/host proof is reused because those inputs do not change. Native
click/focus, screen reader, narrow width, DPI and high contrast still need an
isolated session. Shared-desktop input and the normal App/settings remain untouched.
