# Compact command palette — Issue #525

Status: engineering handoff for hide's requested narrower palette
Date: 2026-11-22

This refines the existing approved Direction B palette geometry. The original
session, search/list two-stop loop, native IME ownership, qualified execution and
scrim cancellation remain authoritative. Context, selected-row detail, native
truncation-dependent tooltip and automation names keep their current content.

Use DensityCommandPaletteMaxWidth640 and SpacingCommandPaletteOuter24. The existing
surface stretches within the available slot up to the cap and is horizontally
centered; it retains Top alignment so filtering cannot move the search field
vertically. A fixed24 effective-pixel inset replaces the advice's dynamic top
percentage and narrow breakpoint, avoiding an extra sizing callback. There is no
minimum/fixed width or height. The original star-sized scrolling candidate list
continues to receive remaining height; short-window/full-window/DPI constraints
are not claimed as newly proved.

The candidate grid keeps the marker and shortcut in Auto columns. The title and
target share a2-star column; the existing reason uses a1-star column instead of
an unbounded Auto column. The full reason stays in the detail and accessible name.
The existing full-title tooltip binding still follows actual IsTextTrimmed.
Canonical key hints now use the existing KeyHintWrapPanel already used by the
footer, preserving the same ItemsControl, template and key-map source. No new
panel, converter, timing behavior, dependency or command owner is introduced.

Background ordinary Claude consultation recommended a640 palette cap and24 inset,
plus a separate520 suggestion for Locations. /design had already reported only
consent-management support in this CLI; no claim is made that it generated a
mockup. The full brief/advice is retained under compact-modal-design-20261122.
The brief's text-entry420 reference was corrected to the actual320 token; the
selected palette cap does not depend on that reference. New row templates,
dynamic top offsets, breakpoints, animations and custom layout code were declined
in favor of the existing adaptive mechanisms. #548 remains a separate change.

Microsoft's [alignment guidance](https://learn.microsoft.com/en-us/windows/apps/develop/ui/alignment-margin-padding)
and [maximum-width contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.frameworkelement.maxwidth?view=windows-app-sdk-1.8)
explain the framework constraints; actual composition was checked separately.
Detached native WinUI fixtures prove17 distinct row/hint cases and4 surface-width
cases using the changed XAML and existing resources/panel source. No Window was
created or activated and no input was injected. This is geometry evidence, not a
complete product screenshot, native keyboard/IME, DPI or Narrator result.
The [quality record](../quality/2026-11-22-compact-command-palette.md) records trials
and exact limits, including an unavailable built-in panel experiment.
