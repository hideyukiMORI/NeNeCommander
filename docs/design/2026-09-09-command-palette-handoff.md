# Command palette engineering handoff

Status: approved direction for Issue #101

Date: 2026-09-09

hide accepted direction B on 2026-09-08: a compact command palette near the top of the window,
with the file panes still visible under ordinary layouts and a summary of the selected command's
target, opposite pane, shortcut, and unavailable reason. This handoff converts the completed design
consultation into the engineering constraints selected for the first native WinUI implementation.
It does not claim native runtime, UIA, high-contrast, DPI, or scheme proof.

## Component inventory

The overlay contains one raised palette surface over the frozen panes. The surface is ordered as:

1. a static context strip naming the captured target and opposite panes;
2. a native `SearchField`;
3. one compact, scrolling `CandidateList`;
4. a `DetailBar` below the list; and
5. non-focusable canonical key hints.

Each candidate row contains a non-color selection carrier, localized full command name, canonical
shortcut, target summary, and availability state. The selected row's complete target/opposite
summary and unavailable reason appear in the detail bar. There is no close button, clear button,
destructive-confirmation badge, or palette-owned confirmation. Existing destructive command paths
remain authoritative.

The palette has closed, open with an empty query, open with filtered results, open with zero
results, and native IME-composition states. It has no opening/closing animation state, debounce,
timer, busy-open mode, refusal flash, match-run styling, or transient error state in this Issue.
The palette does not open while either pane is reading or launching, or while an operation is
running or awaiting a modal decision.

## Focus, keyboard, and selection

Initial focus is the native search field. The canonical palette key map emits a Presentation-only
focus action for Tab, and the host applies native focus to the other stop. Tab and Shift+Tab form a
two-stop loop between the search field and the composite candidate list. Context, details, hints,
rows, badges, and the scrim do not add tab stops, and focus cannot reach the frozen panes while the
palette is open.

Up and Down move the selected candidate from either tab stop. The search field retains UIA focus
when it owns focus; changing the candidate is selection, not synthetic focus. Enter executes only an
available selected candidate, and Escape cancels. During native IME composition, Up, Down, Enter,
and Escape belong to the IME and do not change or execute the palette selection or close it. Printable input,
editing chords, dead keys, and composition remain native.

Clicking or tapping a candidate selects that row and uses the same qualified execution path as
Enter. An unavailable row only changes selection and exposes its reason. Clicking or tapping the
scrim uses the same qualified cancellation as Escape; input inside the palette surface does not
propagate to the scrim. There is no row button, confirmation column, or additional pointer-only
command path.

Every query change selects the first filtered row and scrolls the list to its start. Zero results
have no selection. Unavailable rows remain selectable and reachable so their reason can be read;
they are not implemented as `IsEnabled=false`. Enter on zero results or an unavailable row leaves
the palette unchanged. The first implementation updates filtering immediately and adds no timing
boundary.

Escape restores the captured active pane. Execution defers focus to the existing command result:
settings and address entry focus their editor, modal-producing commands focus their existing modal,
pane activation focuses the new active pane, and ordinary commands retain the current pane/result
behavior. The overlay adds no toast or banner that can steal focus after close.

## Localization and accessibility

Search covers the localized command title and canonical shortcut only. An empty query preserves
the Application catalog order. Fuzzy matching, aliases, recency, persistence, and query history are
absent.

The accessible name of each row is ordered as localized command name, canonical shortcut, target
pane, opposite pane, and, when applicable, localized unavailable status and reason. The full name
also appears in a tooltip when visual space truncates it. The unavailable reason is carried in the
accessible name rather than HelpText alone. Selection and availability each use a non-color carrier
in addition to semantic color. The detail reason wraps and is not ellipsized.

The native search field has a localized accessible name. Candidate count and selected-row changes
use existing native UIA behavior in the first implementation; no custom delayed live-region
announcement is added. Actual Narrator behavior remains an environmental proof item under Issue
#94.

## Semantic resource mapping

Reuse existing resources from every current top-level family. Do not introduce literal colors,
spacing, typography, radii, row height, elevation, or motion values in the view.

| Element | Existing semantic role |
|---|---|
| Overlay/scrim | the current modal overlay surface |
| Palette surface | the current modal or flyout raised surface and available elevation |
| Surface outline and separators | existing subtle border resources |
| Context, shortcut, and secondary metadata | existing secondary text resources |
| Command title | existing primary text resources |
| Unavailable title and metadata | existing disabled/secondary text plus a state glyph and reason |
| Selected row | existing selection surface/foreground resources plus a persistent non-color bar or border |
| Search/list focus | current system/semantic focus resources on the element with actual focus |
| Unavailable reason | existing warning/caution status resources, never danger/error wording |
| Zero results | existing neutral or informational text/surface resources |
| Gaps, padding, type, radius, density | existing spacing, typography, radius, and compact-row resources |

When the search field has focus, reuse the existing selection brush for the pending selected row
and retain the non-color selection carrier. Do not add a new pending-selection color solely for
this feature. Use an existing modal/flyout elevation value if the current XAML surface supports it;
do not create a new numeric elevation. All eight schemes resolve the same semantic keys.

## Responsive behavior

The palette remains within the existing shell margins and uses the available content width. At
narrow width, row content may wrap below the command title; the canonical shortcut remains visible.
Candidate count reduces before command text size. The list scrolls when height is constrained, and
the design does not claim that the palette, panes, detail, and status are simultaneously visible at
every size.

Localized command names expose their full value through UIA and tooltip. The detail reason always
wraps. This first slice does not implement special match-position ellipsis switching or a separate
internal detail scrollbar. Existing framework text trimming and wrapping are used where the final
handoff allows them.

High contrast uses the existing system-aware semantic resources and preserves the surface outline,
focus indicator, selection carrier, state glyph, and reason text. Effective-pixel scaling and
existing framework/vector assets are used from 100% through 300% DPI. These are implementation
constraints; the actual high-contrast, 100/150/200/300% DPI, narrow-window, eight-scheme, keyboard,
IME, and Narrator matrix remains unexecuted release-environment evidence tracked by Issue #94.

## Stable automation surface

Use stable automation identities following the current view convention for the palette, search
field, candidate list, detail bar, and zero-result state. Candidate rows use the canonical intent
identity through a deterministic name rather than a filtered index. Framework-generated row
containers do not become a second command identity or keyboard map.

## Explicitly excluded consultation suggestions

The first slice excludes opening during background work because engineering state ownership
requires all pane reads, file launches, and file-operation/modal activity to be idle before capture.
It also excludes new animations, reduced-motion branches introduced only for those animations,
debounce timers, delayed live-region work, refusal flashes, match-run decoration, destructive
badges, new token families, numeric visual constants, and special ellipsis switching. Those items
would add timing or styling contracts beyond the accepted minimal direction and require separate
evidence if proposed later.
