# Design brief — dual-pane shell design pass

Status: informational

This brief is the engineering side of [`docs/DESIGN_HANDOFF.md`](../DESIGN_HANDOFF.md) for the first design pass. It records what already exists, the interaction states the design must cover, the fixtures and constraints the design must respect, and where the design canvas lives. It changes no token value and no view; integration is a later Issue.

- Issue: [#40](https://github.com/hideyukiMORI/NeNeCommander/issues/40)
- Code baseline the brief describes: `efa2f326c19ed7c32d81c77102e995a4dd976312`
- Design canvas (Claude Design, editable by hide): <https://claude.ai/code/artifact/e2b0baae-b69f-4520-9e8f-886ae8ce8919>

## What engineering already provides

### Layout and automation identifiers

`src/NeNeCommander.App/Views/CommanderWindow.xaml` is a two-column grid with one bottom status row. Every region has a stable `AutomationId`.

| Region | AutomationId | Control |
|---|---|---|
| root input surface | `CommanderDualPane` | `Grid` |
| left / right pane frame | `LeftPaneBorder`, `RightPaneBorder` | `Border` (active: `FocusRingBrush` + `BorderActivePaneThickness`; passive: `BorderSubtleBrush` + `BorderPassivePaneThickness`) |
| pane region narrator name | (x:Uid `LeftPaneRegion`, `RightPaneRegion`) | `AutomationProperties.Name` on the pane `Border` |
| pane number badge | (x:Uid `LeftPaneNumber`, `RightPaneNumber`) | `TextBlock` inside a filled `Border` in the pane header |
| address | `LeftAddress`, `RightAddress` | `TextBox` in the pane header, minimal template |
| pane status | `LeftStatus`, `RightStatus` | `TextBlock` |
| file list | `LeftFileList`, `RightFileList` | `ListView`, `SelectionMode=Single`; the framework selection is the focus item; explicit selection is the `IsSelected` background `Border` in the row template |
| operation bar | `OperationBar` | `Border` whose surface, foreground, border, and icon come from the closed `OperationBarTone` |
| operation status | `OperationStatus` | `TextBlock` |
| running progress segments | `OperationProgressSegments` | `ItemsControl` over the closed twelve-segment `ProgressSegment` list |
| key hints | `OperationKeyHints` | `ItemsControl` over the `KeyHint` list generated from the canonical key map (KBD-005) |
| operation detail | `OperationDetail`, `OperationProgressSeparator`, `OperationTotal` | three `TextBlock`s: count, separator resource, total |
| name entry | `NameEntry` | `TextBox`, collapsed unless a name is awaited; opens with the current name selected for `F2` |

### Semantic token families and current placeholder values

`src/NeNeCommander.App/Themes/DesignTokens.xaml`, single theme, neutral placeholders. Keys are the contract; values are what the design pass replaces.

| Family | Key | Value |
|---|---|---|
| Surface | `SurfaceWindowColor`, `SurfacePaneColor`, `SurfaceFieldColor` | per scheme |
| Text | `TextPrimaryColor`, `TextSecondaryColor`, `TextHiddenColor`, `TextKeyHintColor` | per scheme |
| Border | `BorderSubtleColor` | per scheme |
| Border | `BorderNoneThickness`, `BorderActivePaneThickness`, `BorderPassivePaneThickness`, `BorderPaneHeaderThickness`, `BorderOperationBarThickness`, `BorderKeyCapThickness`, `BorderNameEntryThickness` | `0`, `1`, `1`, `0,0,0,1`, `1`, `1`, `1` |
| Focus | `FocusRingColor`, `FocusSurfaceColor` | per scheme |
| Selection | `SelectionSurfaceColor`, `SelectionMarkColor` | per scheme |
| Status | `StatusWarningColor`, `StatusWarningSurfaceColor`, `StatusDangerColor`, `StatusDangerSurfaceColor` | per scheme |
| Operation | `OperationProgressColor`, `OperationTrackColor` | per scheme |
| Spacing | `SpacingNone`, `SpacingWindowOuter`, `SpacingPaneHeader`, `SpacingPaneList`, `SpacingPaneNumber`, `SpacingRowContent`, `SpacingOperationBar`, `SpacingKeyCap`, `SpacingNameEntry` | `0`, `6`, `10,0`, `0,6`, `6,1`, `0,0,10,0`, `10,0`, `5,1`, `8,0` |
| Spacing | `SpacingPaneGap`, `SpacingRowGap`, `SpacingPaneHeaderGap`, `SpacingOperationBarGap`, `SpacingOperationDetailGap`, `SpacingKeyHintGap`, `SpacingProgressSegmentGap` | `3`, `10`, `10`, `10`, `14`, `5`, `2` |
| Typography | `TypographyBodySize`, `TypographyMonospaceSize`, `TypographyKindLabelSize`, `TypographyMonospaceFamily` | `13`, `12`, `11`, `Cascadia Code, Cascadia Mono, Consolas` |
| Radius | `RadiusPane`, `RadiusOperationBar`, `RadiusNameEntry`, `RadiusKeyCap` | `3`, `3`, `3`, `2` |
| Elevation | `ElevationPaneTranslation` | `2` |
| Density | `DensityNone`, `DensityPaneHeaderHeight`, `DensityRowHeight`, `DensityRowMarkerWidth`, `DensityKindIconSize`, `DensityOperationBarHeight`, `DensityOperationIconSize`, `DensityNameEntryWidth`, `DensityNameEntryHeight`, `DensityProgressSegmentSize`, `DensityIconStrokeThickness` | `0`, `34`, `28`, `2`, `16`, `34`, `18`, `320`, `26`, `8`, `1.5` |
| Motion | `MotionStandardDuration` | `0:0:0.160` |

Colours live in `Themes/Schemes/<identifier>.xaml`, one dictionary per scheme with an identical key set (ADR-0022); every colour is exposed as a `SolidColorBrush` with the `Brush` suffix. The non-colour values above are the approved Direction C values integrated by ADR-0023; the placeholder values this brief originally recorded (window padding 16, pane radius 8, body 14, row 36, and the `TypographyPaneTitleSize`, `SpacingPaneInner`, `SpacingAddressBottom`, `DensityRowMinimumHeight` keys) no longer exist. Views reference only these keys (ARC-012, CS-023). Adding a family remains out of scope.

### Text

All user-facing text is in `src/NeNeCommander.App/Resources/{en-US,ja-JP}/Resources.resw`: pane labels and address headers, nine pane statuses, and the operation statuses for move, copy, delete, create directory, and rename (running, awaiting, succeeded, cancelled, partially completed, rejected, request rejected). Numbers are never embedded in a sentence; the delete confirmation ends with `件数:` / `Item count:` and the count renders in its own control. The longest string is the delete confirmation; the design must survive it and its English expansion.

### Keyboard contract the design may not change

`docs/KEYBOARD_MODEL.md` is the sole key map. The modal states (`F8` confirmation, `F7` / `F2` name entry) own only `Enter` and `Escape`; everything else stays frozen or reaches the name editor. The design may restyle the confirmation and the name entry but may not turn them into a framework dialog, change focus order, or add a mouse-only affordance that bypasses confirmation (Integration law 3).

## Interaction states the design must cover

| State | Where it is visible | Source of truth |
|---|---|---|
| inactive pane / active pane | pane frame | `PaneFrame` |
| focus item | list row (framework selection) | `PanePresentation.FocusRow` |
| explicit selection (zero, one, many) | list row background | `PaneRow.IsSelected` |
| listing loading / complete / bounded / entries omitted / access denied / not a directory / unavailable / cancelled / no listing | pane status | `PaneStatus` |
| idle | operation status empty | `OperationStatus.Idle` |
| busy: moving / copying / deleting / creating directory / renaming with `completed / total` | operation status + detail | `OperationRunning` |
| destructive confirmation (`F8`) with item count | operation status + detail, both panes frozen | `OperationAwaitingConfirmation` |
| name entry (`F7` empty, `F2` prefilled and selected) | name entry TextBox | `ActiveNameEntry(initialText)` |
| completed: succeeded / cancelled / partially completed / rejected / request rejected | operation status | `OperationCompleted`, `OperationRequestRejected` |
| hidden and system entries | list row (reported, visibility toggle is a later transition) | `DirectoryEntry` |
| disabled / conflict resolver | not yet in the product; leave room, do not design behavior | FS-007 later Issue |

## Fixtures the design must be checked against

- Left `C:\` with hidden and system entries (`$Recycle.Bin`, `hiberfil.sys`, `pagefile.sys`), a directory name over 80 characters mixing Latin and Japanese, and more rows than fit.
- Right `C:\Users` with a short listing.
- A pane in `access denied` while the other pane is listed.
- Running copy with a multi-item selection and `3 / 12` progress; the same after a partial failure.
- `F8` confirmation with the full Japanese and English sentences.
- `F2` name entry prefilled with `Program Files (x86)`.
- UNC and WSL addresses (`\\server\share\...`, `\\wsl.localhost\Ubuntu\home\...`) in the address field; the product renders the canonical form only.

## Constraints

- Windows light and dark themes and high contrast: the current placeholders are light-only, and under Windows dark theme the address `TextBox` is invisible on the white pane. The pass must deliver light and dark values for every color key, and the high-contrast behavior must fall back to system brushes.
- 100–300 % scaling: sizes are device-independent pixels; nothing may depend on pixel-snapped hairlines.
- Narrow window: two panes stay side by side down to roughly 900 px; below that the design may stack but must not hide either pane.
- Touch targets: rows and the name entry are at least 32 dip tall; 44 dip is preferred where density allows.
- Fonts: Segoe UI Variable (Windows 11) with Segoe UI and Yu Gothic UI fallbacks; no web font.
- Icons: stroke-based inline vectors, no emoji.

## What the canvas currently shows

Page 1 (`Shell states`): the shipped shell reproduced from the tokens and WinUI defaults (`Current`), a proposed light idle (`Main`), the same in dark, and the `F8`, `F2` / `F7`, running-progress, and partial-failure states. The proposal keeps the structure and the key map: the pane label becomes a small muted caption merged with the address row, rows gain a kind icon and a distinct focus ring versus selection fill, hidden entries are muted, long names are truncated with an ellipsis, and the bottom becomes one full-width status bar whose modal states differ only by tint. The 4 px progress bar is a placeholder for later byte-level progress and is not part of this integration.

Page 2 (`Directions`): two low-fidelity alternates — dense monochrome and layered Fluent — for comparison only. hide's first reaction: acceptable but not sharp enough.

Page 3 (`Direction C · sharp`): the direction hide asked for after page 2 — a sharp, modern flat look leaning toward a hacker tool, dark first with a light variant: 2 px radii, 1 px lines, no shadows, monospace pane numbers and paths, 28 px rows with a 2 px left marker (cyan focus, green selection), a bottom bar carrying the key hints that switches to `Enter` / `Esc` while a modal is pending, segmented progress, amber warnings and red failures. After hide's review: pane gap 4 dip, outer padding 6 dip, radius 3 dip. Each Direction C artboard carries a `scheme` tweak that switches the whole palette between `nene-dark`, `ubuntu`, `monokai`, `solarized-dark`, `solarized-light`, `dracula`, `nene-black` (pure black background), and `nene-light`; the eight idle artboards show one scheme each, and `F8`, `F2`, and copying `3/12` are shown in `nene-dark`. Every value still maps onto the existing token keys; whether Selection needs a separate mark color is decided at integration.

### Requirement raised by hide during the pass

The color scheme must be user-selectable, in the spirit of terminal palettes (Ubuntu, Monokai, Solarized, Dracula). For the product this means one theme resource dictionary per scheme, all sharing the same semantic keys, chosen through the `ISettingsStore` boundary (Command model registry: settings persistence) rather than a view-level switch. The selection mechanism, its ADR, and the settings schema belong to the integration Issue; this pass only fixes the palettes.

## What the design handoff must return

Per `docs/DESIGN_HANDOFF.md`: the component inventory with annotated states above, token values for every key in the table (light and dark), the focus / selection / progress / error treatments, and accessibility notes. Values are then integrated in a separate Issue by editing theme dictionaries only, with presentation tests for every state in the table (QLT-011) and a runtime check of the exe.

## Out of scope for this pass

Collision resolver UI, byte-level progress, WSL and UNC adapters, new token families, and any change to command semantics, focus order, or the key map.
