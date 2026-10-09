# Design handoff — pane row metadata columns (size, modified) — 2026-10-09

Status: proposed (design owner's spec under Direction C; hide's visual approval is requested at the implementation PR)

## Overview

Issue #178 carries provider metadata (size, last-write time) into `DirectoryEntry` and sorts by it.
The row still shows only marker, kind icon, name, and the `DIR` label, so a user sorting by size or
time cannot see the values that decide the order. This handoff adds two right-aligned metadata
columns to the 28-DIP row under the approved Direction C layout (sharp flat, dark first, terminal
palette, monospace facts, 3-px gaps, 2–3-px radii). It changes no color, no row height, no key, no
semantics; it adds two texts and the resources that format them.

## Layout

Row grid today: `marker (2) | kind icon (16) | name (*) | kind label (Auto)` with
`ColumnSpacing = SpacingRowGap (10)`, `Padding = SpacingRowContent (0,0,10,0)`, `Height =
DensityRowHeight (28)`.

Row grid after this change:

| Column | Width | Content | Alignment |
|---|---|---|---|
| 0 | `DensityRowMarkerWidth` (2) | focus/selection marker | fill |
| 1 | `DensityKindIconSize` (16) | kind icon | center |
| 2 | `*` | name | left, `CharacterEllipsis` |
| 3 | `DensityRowSizeWidth` (new, 64) | size text | right |
| 4 | `DensityRowModifiedWidth` (new, 112) | modified text | right |
| 5 | Auto | kind label (`DIR` / empty) | right |

Order rationale: name grows, facts stay fixed-width on the right where the eye scans a column; the
kind label remains last because it is the existing anchor at the row's right edge and `DIR` rows
have no size, so the empty size cell sits next to the label rather than in the middle.

Both metadata texts use `TypographyMonospaceFamily` at `TypographyMonospaceSize` (12) so digits
align vertically, colored `TextSecondaryBrush`; the name keeps `TypographyBodySize` (13) and its
visibility-driven brush. Hidden entries keep their name brush rule; their metadata uses
`TextSecondaryBrush` like every other row, because the hidden signal is already carried by the name.

Fixed widths are derived from the longest formatted strings below at 12-px Cascadia (about 7.2 DIP
per glyph): size `1023.9 GB` is 9 glyphs ≈ 65 DIP → 64 with the gap absorbing rounding; modified
`2026-10-09 23:59` is 16 glyphs ≈ 115 DIP → 112. If measurement on the real window shows clipping
at 100 percent, raise the token values; never shrink the font.

## Design tokens

| Token | Value | Usage | New? |
|---|---|---|---|
| `DensityRowSizeWidth` | `64` | size column width | yes |
| `DensityRowModifiedWidth` | `112` | modified column width | yes |
| `TypographyMonospaceFamily` | existing | both metadata texts | no |
| `TypographyMonospaceSize` | `12` | both metadata texts | no |
| `TextSecondaryBrush` | per scheme | both metadata texts | no |
| `SpacingRowGap` | `10` | column spacing (unchanged) | no |
| `DensityRowHeight` | `28` | row height (unchanged) | no |

No new color. The two new density tokens live in the shared non-color dictionary next to the other
`Density*` keys and are referenced only from the row template (ARC-012, CS-023).

## Text formats (Presentation decides; the view binds strings)

Size (`EntrySize`):

| Bytes | Text | Rule |
|---|---|---|
| 0 … 1023 | `812 B` | integer, unit `B` |
| 1024 … 1048524 | `3.4 KB` | one decimal, round half away from zero |
| 1048525 … | `1.0 MB`, `12.0 MB`, `1.5 GB`, `2.0 TB` | one decimal, binary thresholds (1024), decimal-looking labels as commanders do; a value that rounds to `1024.0` of a unit is shown as `1.0` of the next unit, so `1024.0 KB` never appears |
| ≥ 1023.95 TB | `1023.9 TB` | the top unit saturates; never widen beyond 9 glyphs |
| `Unknown` for a file | `—` (U+2014) | one glyph, right-aligned |
| directory | empty | the `DIR` label already says what it is |

Modified (`EntryTimestamp`):

| Case | Text |
|---|---|
| `Known` | `yyyy-MM-dd HH:mm` in the user's local time zone, 24-hour, invariant digits |
| `Unknown` | `—` |

Rationale: ISO-like fixed-width date beats locale short dates for column alignment and is the
hacker-tool idiom hide approved. Seconds are omitted (28-DIP row, scan speed). The format strings
are resources so ja-JP can reorder if it ever wants to; both locales ship the same strings
initially: `PaneRowSizeUnitBytes` … `PaneRowSizeUnitTerabytes` (`B`, `KB`, `MB`, `GB`, `TB`),
`PaneRowSizeFormatInteger` (`{0} {1}`), `PaneRowSizeFormatDecimal` (`{0:0.0} {1}`),
`PaneRowModifiedFormat` (`yyyy-MM-dd HH:mm`), and `PaneRowMetadataUnknown` (`—`).

## Narrow window (900 × 600 DIP)

Each pane is about 440 DIP wide. Fixed columns consume 2 + 16 + 64 + 112 + label + 5 gaps (50) +
padding (10) ≈ 280 DIP, leaving about 160 DIP for the name, which still fits roughly 22 body-size
glyphs before ellipsis. That is acceptable for the approved narrow state and needs no responsive
step. Truncation priority: the name truncates first and alone; metadata never truncates or wraps.
If a future narrower state is approved, hide the modified column first, then the size column, by
binding their `Visibility` to a presenter-decided `PaneRowColumns` value; this is out of scope now.

## States

| Element | State | Behavior |
|---|---|---|
| size/modified text | focus row (active pane) | same brush; the marker and row surface carry focus as today |
| size/modified text | selected row | same brush; selection surface carries the state |
| size/modified text | hidden entry | `TextSecondaryBrush` (unchanged); only the name dims |
| size/modified text | `Unknown` | `—`, right-aligned, same brush |
| size text | directory | empty cell |
| row | listing bounded/omitted | unchanged status semantics; columns show per row |

No hover, no animation, no mouse interaction is added.

## High contrast

The two texts use the same semantic brush as the kind label, so whatever ADR-0022's scheme
dictionaries map `TextSecondaryBrush` to under a high-contrast theme applies unchanged. No
color-only meaning is introduced: `Unknown` is a glyph, directory size is absence, and sort state is
in the status line text.

## Automation

- Row `AutomationProperties.Name` stays `Entry.Name` (unchanged, screen readers hear the name).
- Each metadata `TextBlock` gets `AutomationProperties.Name` = its text and an `AutomationId`
  suffix on the row's template parts is not needed (rows are virtualized); the `PaneRow` record
  exposes `SizeText` and `ModifiedText` so UIA reads them through the TextBlock values.
- Narrator ordering inside a row is marker-less: icon (no name), name, size, modified, kind label.

## Implementation notes (for the implementation seat)

- `PaneRow` gains `SizeText` and `ModifiedText` strings computed by a pure `EntryMetadataFormatter`
  in Presentation (no `DateTime.Now`). The existing time boundary (`IClock`) carries only monotonic
  time, so the zone is not added to it: the composition root reads `TimeZoneInfo.Local` once and
  passes it to `CommanderWindow`, which resolves the metadata resources once into one
  `EntryMetadataFormat` (zone plus localized formats) and supplies that instance to every
  `DualPanePresenter` projection, following the `Func<string, string>` localization precedent of
  `PaneStatusLine` and `LocationsPresenter`.
- `PaneListingPresenter` reuses rows (ADR-0028) only while it receives the same listing and the
  same `EntryMetadataFormat` instance; the row source records the format it was projected with. A
  different format re-projects every row, and a mark-only replacement keeps the texts, because the
  same entry under the same format cannot produce other texts.
- The template keeps the existing pattern of `Auto` columns whose element carries the token width:
  each metadata `TextBlock` has `Width` = its density token and `TextAlignment="Right"`.
- Tests: formatter boundaries (1023/1024, rounding, top unit, Unknown, directory), local time
  conversion with an injected zone, row projection text, template bindings through the existing
  presentation tests. No snapshot-only proof.

## Out of scope

Column headers, click-to-sort, user-resizable columns, created/accessed times, attributes,
responsive hiding, locale-specific date formats.
