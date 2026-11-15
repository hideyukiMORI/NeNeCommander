# ADR-0078: Remove the system title bar and draw one caption bar

Status: accepted

Date: 2026-11-15

Accepted for Issue #379. hide asked on 2026-11-15 for a frameless window "like NeNeNib and
NeNeFolio", agreed that a new top strip is added, selected direction A and a close button that does
not turn red in the design handoff
[`docs/design/2026-11-15-frameless-window-handoff.md`](../design/2026-11-15-frameless-window-handoff.md),
and required that the top-right corner and the edges next to the buttons still resize the window.

## Context

The window used the system title bar and had no strip of its own. The two sibling applications are
frameless: the client area covers the whole window, an opaque strip drawn by the application holds
line-glyph caption buttons that never turn red, and the system keeps the resize frame, shadow, Snap
and the minimize and maximize animations.

WinUI offers a documented way to extend content into the title bar while the system keeps drawing
the caption buttons. Microsoft's
[title bar customization guidance](https://learn.microsoft.com/en-us/windows/apps/develop/title-bar)
states that in that mode "the close button always uses the system-defined color" for its
pointer-over and pressed states, so it cannot satisfy hide's decision. The `TitleBar` control only
allocates space for the same system buttons. The same guidance and the API reference do not
describe how to remove the system buttons, what `TitleBarHeightOption.Collapsed` means, which
`NonClientRegionKind` wins where declarations overlap, or how these interact with Snap. The
decisions below therefore rest on measurement on the pinned Windows App SDK 2.6.5, not on
documentation.

NeNeNib's lesson, which hide named explicitly: its caption buttons covered the resize frame, and
the top-right corner and the adjacent edges could no longer be dragged.

## Measured facts

Measured on 2026-11-15 with Windows App SDK 2.6.5 at 125 % scaling by asking the running window for
its `WM_NCHITTEST` answer at screen points (a query; no input was sent), and confirmed by hand by
hide on the same build:

- `AppWindow.TitleBar.ExtendsContentIntoTitleBar = true` together with
  `OverlappedPresenter.SetBorderAndTitleBar(true, false)` removes the system title bar and its
  caption buttons. The client area starts at the top of the window, and the resize frame, shadow,
  minimize and maximize animations, `Alt+Space`, the `Win`+arrow Snap keys and `Win+Z` remain. The
  window style still reports `WS_CAPTION`; no system title bar is drawn.
- `InputNonClientPointerSource.SetRegionRects` takes client-relative physical pixels. A `Caption`
  rectangle answers `HTCAPTION`: it drags the window, maximizes and restores on a double click and
  opens the system menu on a right click.
- **The top edge has no system resize frame outside the window.** The left, right and bottom edges
  keep an invisible frame about 8 pixels outside the visible frame; the pixel above the top edge
  belongs to whatever window is behind. The only top resize targets are `TopBorder` rectangles
  declared inside the client area.
- Where a `TopBorder` rectangle overlaps a `LeftBorder` or `RightBorder` rectangle the system
  answers the corner (`HTTOPLEFT`, `HTTOPRIGHT`). Declaring the same square as both kinds makes the
  whole square a corner target.
- With content extended into the title bar the SDK declares defaults of its own: a top strip 4
  pixels deep across the window and 4-pixel side strips for the title-bar height only. Declaring a
  kind replaces its default.
- XAML content under a `TopBorder`, `LeftBorder` or `RightBorder` rectangle receives no pointer
  input; content below it does.
- `InputNonClientPointerSource.GetRegionRects` throws `E_POINTER` for a kind with no declaration,
  which terminates the process as an unhandled stowed exception. Product code does not call it.
- When the window is deactivated the system draws no band on the outer edge, and a maximized
  window stays inside the work area with its caption bar fully visible.

## Decision

- **The system title bar and its caption buttons are removed** with the two calls above, once,
  when the window is composed. No backdrop material is introduced. The resize frame, shadow, Snap
  and animations stay the system's.
- **One caption bar is the first row of the window.** It shows the existing localized window title
  at the left and three buttons at the right in the system order: minimize, maximize or restore,
  close. It carries no icon, menu, tab, path, status or command. The dual-pane content keeps its
  layout below it; the caption bar replaces the former top outer spacing.
- **The buttons are ordinary XAML buttons in the client area.** Their pointer-over and pressed
  surfaces are drawn from existing semantic brushes, the close button like the other two. They are
  not tab stops and never take keyboard focus, so focus order and every key binding are unchanged.
  They have no tooltip.
- **The presenter is the only record of the window state.** Minimize and maximize or restore call
  the window's `OverlappedPresenter`; the middle button's glyph and accessible name are read from
  `OverlappedPresenter.State` each time `AppWindow.Changed` reports a presenter or size change. No
  second maximized flag exists. Window adjustment mode (ADR-0050) already uses the same presenter,
  so its `m` and `r` keys and the middle button always agree.
- **The close button enters the system close path.** It posts `WM_SYSCOMMAND` / `SC_CLOSE` to the
  window, the same command as `Alt+F4` and the system menu's Close. `AppWindow.Closing` and
  ADR-0071's drain then run unchanged; the button does not call the session, the drain or
  `Window.Close` itself. ADR-0071's "ordinary system-affordance close" therefore includes this
  button. The native call lives in `NeNeCommander.Infrastructure.Windows` under `Windowing`, not in
  the App project.
- **Declared regions.** The empty part of the caption bar, including the title text, is declared
  `Caption`. While the window is restored the caption bar also declares its own resize targets,
  because the top edge has none outside the window:
  - a `TopBorder` strip `DensityCaptionResizeBorder` (4 DIP) deep across the whole bar, including
    over the three buttons;
  - a `DensityCaptionResizeCorner` (8 DIP) square in each top corner, declared as both `TopBorder`
    and the adjacent side kind, so the whole square is a corner target;
  - a side strip `DensityCaptionResizeBorder` wide down each end of the bar, replacing the SDK's
    default side strips.

  The buttons are pressable below the top strip. A maximized window declares no resize region, so
  the top-right screen corner presses close. Regions are declared again when the caption bar
  changes size, when the rasterization scale changes and when the presenter or window size changes.
- **Hovering the maximize button does not open the Snap layouts flyout.** That requires declaring
  the button as the system's `Maximize` region, which would hand its pointer handling and
  pointer-over drawing back to the system. `Win+Z` and the `Win`+arrow keys remain. hide was told of
  this, used the build without the flyout on 2026-11-15 and did not ask for it.
- **Active and inactive.** The view reads the window's activation state and draws the title and
  glyphs with `TextHidden` while inactive. It makes no domain or application decision: the
  framework-free `CaptionBarPresenter` in Presentation maps the presenter state, the activation
  and each button's pointer state to brush and resource keys, and the App view only applies them.
- **Tokens.** Sizes are new `Density` and `Spacing` tokens (`DensityCaptionBarHeight`,
  `DensityCaptionButtonWidth`, `DensityCaptionGlyphSize`, `DensityCaptionGlyphStrokeThickness`,
  `DensityCaptionResizeBorder`, `DensityCaptionResizeCorner`, `SpacingCaptionTitle`,
  `SpacingWindowContent`). No color token and no token family is added; views reference brushes
  only (Issue #366).
- **Accessibility and localization.** `CaptionTitle`, `CaptionMinimize`, `CaptionMaximize` and
  `CaptionClose` are stable automation identifiers on elements that have automation peers. Button
  names are resource keys; the middle button's name follows the presenter state. No literal string
  is added to a view.

## Rejected alternatives

- Keeping the system caption buttons and matching their colors: the close button's pointer-over
  and pressed colors stay the system's red, against hide's decision.
- The `TitleBar` control: it reserves space for the same system buttons.
- `TitleBarHeightOption.Collapsed`: undocumented, and unnecessary once the measured mechanism
  worked.
- Subclassing the window procedure to answer `WM_NCCALCSIZE` and `WM_NCHITTEST` as the Win32
  siblings do: a second, lower-level owner of the frame beside the `AppWindow` presenter, and the
  siblings' recorded pitfalls (system-painted frame bands, maximized overhang) would have to be
  solved again by hand.
- Halving the top resize strip over the buttons, as NeNeNib does: at 125 % it left two pixels over
  each button and a two-pixel corner, which is the defect hide asked to avoid.
- Declaring the buttons as the system's `Minimize`, `Maximize` and `Close` regions: gives the Snap
  flyout but moves pointer handling and pointer-over drawing to the system, and its interaction with
  XAML buttons is undocumented.
- Closing through `Window.Close` or the session: a second close route beside the one ADR-0071
  drains.
- A backdrop material: outside the family look, and the siblings recorded corner artifacts with it.

## Consequences

The window has one more row and the file area is 26 DIP lower than before (a 32 DIP bar replaces
the 6 DIP top spacing). Resizing from the top edge and top corners depends on declarations this
application owns, so a change to the caption bar layout must keep them and re-measure. The 4 DIP
top strip takes the top eighth of each caption button. The UI evidence harness sees one more
region; its layout expectations are updated in the same change where they assume the old window
rectangle. A `Grid` that carries only an automation identifier and no accessible name had no
automation peer in the measurement (`CaptionBar` and `CommanderDualPane` were absent from UI
Automation), so such an identifier never reaches clients; identifiers that clients must find belong
on elements with peers, and the caption bar's own grid carries none.

## Executable proof

- Architecture tests keep the caption view under the same view-composition ownership rules as the
  other named visual parts, and the XAML conformance scan (CS-023) keeps the caption sizes in tokens.
- Presentation tests prove the appearance table: the middle button's form and name for every
  presenter state, the title brush for each activation, and the surface and glyph brushes for every
  pointer state and activation, none of them a danger brush.
- An Infrastructure test proves the close request rejects a zero window handle before any native
  call; the native post itself is exercised only by the running application.
- The UI evidence harness lists the four caption identifiers among its layout elements and checks
  that the caption parts do not overlap each other or the panes.
- The hit-test measurement of 2026-11-15 (restored window, 125 %): every point of the 4 DIP top
  strip over the three buttons answers `HTTOP`, the points below it answer `HTCLIENT`, each 8 DIP
  top corner square answers its corner, the inner 4 DIP right of the close button answers
  `HTRIGHT`, and the title answers `HTCAPTION`. The measurement script and its output stay outside
  the repository because the captures show local content.
- hide's hands-on confirmation of 2026-11-15 on the same build: dragging the top-right corner, the
  edge over the buttons and the edge right of close resizes the window; the bar drags the window and
  a double click maximizes and restores; the three buttons work and close exits normally; the
  maximized window does not overhang the screen and the screen corner presses close; `Alt+Space`,
  the right-click system menu, the `Win`+arrow keys and `Win+Z` work; no band appears on
  deactivation.
- Not yet proven by measurement and left to the release confirmation in Issue #94: scaling other
  than 125 %, high contrast, and a move between monitors of different scales.
