# Compact Locations picker — Issue #548

Status: local engineering handoff; native scrolling and screen acceptance pending
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
allows variable-height rows; narrow clients show fewer rows and native scroll
arrival remains an explicit acceptance item. It is not claimed to be the advisor's
one-line proposal or a final approved screen.

Hints use the already-existing KeyHintWrapPanel and canonical key-hint template
and source. Status/error text already wraps. No font, color, radius, motion,
locale, command path, package or persistence change is introduced.

[Quality evidence](../quality/2026-11-22-compact-locations.md) distinguishes24
row/hint and4 outer-width checks from12 limited composition measurements. An
unshown, unactivated helper Window exits normally, but does not acquire XamlRoot;
its ScrollIntoView calls do not establish real scroll arrival. Full-window,
short-height interaction, actual DPI/text scaling, input, Narrator and visual
acceptance remain pending. The user App and desktop input are untouched.
