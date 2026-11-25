# Compact Locations picker — Issue #548

Status: local engineering handoff; attached API scrolling proved, native input/screen acceptance pending
Date: 2026-11-22

The existing Locations overlay uses a520 effective-pixel maximum width and24
outer inset through DensityLocationsMaxWidth and SpacingLocationsOuter. It is
horizontally centered by the existing Stretch/MaxWidth behavior and remains
Top-aligned. It shrinks inside narrower clients without a minimum/fixed width.
The full-window scrim, named modal/list/status/hint surfaces and list scroll owner
remain. LocationsView and the catalog/session/keyboard/navigation contracts do
not change; the existing surface consumes its taps and the surrounding scrim
forwards the state-qualified cancellation.

The same background Claude consultation used for #525 suggested520 for Locations
and640 for the command palette. /design was unavailable as a design-generation
command in the installed CLI; the retained advice is ordinary text consultation,
not a generated mockup. The full authored brief and advice remain in
D:/NeNeCommander/evidence/compact-modal-design-20261122. The brief's420 text-entry
reference was corrected to the actual320 token. No extra consultation or consent
configuration change was made for this task.

Engineering adopts fixed24 inset and existing Top placement instead of dynamic
percentage positioning or new breakpoint code. The advisor proposed one-line
ellipsis/tooltip rows. Actual baseline measurement showed an Auto name column
expanding a long WSL row to496 inside a272 or128 slot, while detail was trimmed.
This handoff instead bounds name/detail to2-star/1-star columns and wraps both:
#548 asks that identifiers and information remain available, and the old row has
no keyboard-visible full-name detail surface. Full text and the existing accessible
name are preserved without introducing tooltip/input code. This deliberately
allows variable-height rows; narrow clients show fewer rows and native input-driven scroll
arrival remains an explicit acceptance item. It is not claimed to be the advisor's
one-line proposal or a final approved screen.

Hints use the already-existing KeyHintWrapPanel and canonical key-hint template
and source. Status/error text already wraps. No font, color, radius, motion,
locale, command path, package or persistence change is introduced.

[Quality evidence](../quality/2026-11-22-compact-locations.md) retains24 row/hint,
4 outer-width and the original12 limited composition measurements. A new fixture
lets the dispatcher attach the unshown Window's XamlRoot. At actual125% scale and
640/320 by800/300 client sizes, the same12 composition conditions now prove complete
last-row arrival and first-row return through ScrollIntoView, with modal/hints inside
the24 inset. The earlier unconnected/faulted attempts remain recorded separately.
This API proof uses literal rows, forced status text and the actual templates; it
is not product key routing, pointer, focus, scrim hit testing or screen acceptance.
Other DPI/text scales, Narrator and visible interaction remain pending. No user App,
settings, visible window or desktop input was touched.

## November24 native-arrival correction

Full product-control testing reproduces a41.83DIP hidden part of the selected last
wrapped row when every key move resets ItemsSource. The original no-C# scope above
is therefore superseded by one LocationsView display correction: preserve the actual
row source only when exact item identities, order/count and every localized row value
match; select from those retained rows and keep the single existing ScrollIntoView.
No new focus, keyboard, catalog or navigation mechanism is added. Native sequential
arrival/return, Enter/UIA selection and error/empty cases pass in all three shipped
languages at125%; normal build, conformance and independent review pass. See the
quality record for exact counts and preserved failures. Extremely short/narrow
clients can have a row taller than the viewport; top/bottom text access through
real UIA scrolling is proved separately, not asserted to be simultaneously visible.
The baseline initial-list-focus defect, direct-API jump limit, other scales and
physical/visible acceptance remain explicit. This is a local candidate, not final
screen approval or release integration.
