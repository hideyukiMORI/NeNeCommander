# Frameless window engineering handoff

Status: approved direction A for Issue #379

Date: 2026-11-15

This handoff records the design consultation for the frameless window decided in
[ADR-0078](../adr/0078-frameless-window-caption-bar.md). hide asked on 2026-11-15 for the window to
be frameless "like NeNeNib and NeNeFolio" and agreed that a new top strip is added to hold the
caption buttons and the drag area. Engineering owns the behavior, accessibility and integration
constraints below. The visual direction is chosen by hide; final token values follow
[DESIGN_HANDOFF.md](../DESIGN_HANDOFF.md).

## What the family look means

Surveyed in the two sibling Win32 applications on 2026-11-15:

- No system caption; the client area covers the whole window. The system keeps the resize frame,
  the shadow, Snap and the minimize and maximize animations.
- A flat, opaque strip drawn by the application. No Mica or other backdrop.
- Minimize, maximize and close are line glyphs drawn in the scheme's text color. The close button
  does not turn red.
- The empty part of the strip drags the window and maximizes on a double click.

The two applications differ in strip height (40 and 44 DIP), strip content (tabs; a command header)
and button shape (full-height 46 DIP; inset 28 DIP squares), so the family look fixes the principles
above and not the measurements.

## Content inventory

1. Application name `NeNe Commander` at the left: the existing localized window title text.
2. Three caption buttons at the right, in the system order: minimize, maximize or restore, close.

Nothing else. No icon, menu, tab, path, status or new command.

## States and behavior

- Buttons: normal, pointer over, pressed. Window: active and inactive. Presenter: restored and
  maximized (the middle button shows the restore glyph while maximized).
- The strip's empty part and the application name move the window, maximize or restore on a double
  click and open the system menu on a right click.
- hide's explicit requirement (2026-11-15): the top-right corner and the edges next to the buttons
  must resize the window by dragging. NeNeNib once lost this because the buttons covered the resize
  frame. The top edge has no system resize frame outside the window, so the strip reserves its own:
  4 DIP along the whole top edge, including over the buttons, and an 8 by 8 DIP square in each top
  corner. A maximized window reserves none, so the top-right screen corner presses close.
- The buttons never take keyboard focus and are not tab stops. Keyboard operation is unchanged:
  the window adjustment mode (`Ctrl+W`), `Alt+F4`, `Alt+Space` and the Windows Snap keys.
- The caption buttons carry no tooltip. Hovering the maximize button does not open the Snap
  layouts flyout; `Win+Z` and the `Win`+arrow keys remain.

## Accessibility and environment constraints

- Automation identifiers `CaptionTitle`, `CaptionMinimize`, `CaptionMaximize` and `CaptionClose`.
  The buttons have localized names; the middle button's name follows the presenter state
  (maximize or restore).
- Eight color schemes, 100–300 % scaling, the narrow window (900×600 DIP), the maximized window, the
  inactive window and high contrast must keep the strip intact and the three buttons visible.
- Colors only through existing semantic brushes (`SurfaceWindow`, `SurfacePane`, `BorderSubtle`,
  `TextPrimary`, `TextSecondary`, `TextKeyHint`, `TextHidden`); no new color token.

## Selected direction

hide selected direction A on 2026-11-15, and "the same as the other buttons" for the close button's
pointer-over color.

| Part | Value |
|---|---|
| Strip height | 32 DIP, background `SurfaceWindow`, no separator line |
| Application name | Monospace 12, `TextSecondary`, 12 DIP from the left edge, vertically centered |
| Buttons | 46 DIP wide, full strip height, no gap, flush with the right edge |
| Glyphs | 10 DIP, 1 DIP stroke: line, square, two overlapping squares (restore), cross |
| Normal | No surface, glyph `TextKeyHint` |
| Pointer over | Surface `SurfacePane`, glyph `TextPrimary` — the close button included |
| Pressed | Surface `BorderSubtle`, glyph `TextPrimary` |
| Inactive window | Application name and glyphs `TextHidden` |
| Content below | Unchanged; the strip replaces the former 6 DIP top outer spacing |

## Directions considered

- **A. 32 DIP strip, full-height buttons (selected).** Closest to NeNeNib. The close button reaches
  the screen corner when maximized, and the strip is the lowest that still holds a 46 DIP button.
- **B. 34 DIP strip, inset 28 DIP square buttons.** Closest to NeNeFolio and to the 3 DIP tile gaps,
  but the buttons do not reach the screen corner.
- **C. 24 DIP strip without the application name.** Largest file area, but a thin drag target and
  small buttons.

B and C were not selected. The canvas with the three directions and the state sheet is private to
hide's account and is not a normative source; this document is.
