# Design Handoff Contract

Status: normative

Claude Design or ChatGPT design tooling will supply the final visual direction. Engineering owns behavior, information hierarchy, accessibility, semantic resources, and integration constraints; the design tool owns the approved visual specification within those constraints.

## What engineering prepares

- A functional dual-pane layout with semantic regions and stable automation identifiers.
- Complete interaction states: inactive, active, focused, selected, busy, disabled, warning, error, conflict, and destructive confirmation.
- A semantic resource dictionary whose keys describe purpose rather than a literal value.
- Localization-ready text, keyboard hints from the canonical key map, and representative long strings.
- Windows light, dark, high-contrast, 100–300% scaling, narrow-window, and touch-target constraints.
- A fixture showing local, UNC, and WSL paths, long names, hidden items, permissions, progress, and partial failures.

## What the design handoff must return

- Component inventory and annotated states.
- Token values for color, typography, spacing, radii, elevation, density, and motion.
- Pane hierarchy, focus treatment, selection treatment, progress and error treatment.
- Assets with source, license, export dimensions, and high-contrast behavior.
- Responsive and DPI behavior plus accessibility notes.

## Integration law

1. Token values are changed in theme resource dictionaries, not copied into views.
2. Views bind to semantic keys such as `PaneActiveSurfaceBrush`; keys such as `Blue500` are prohibited at the feature layer.
3. A design change may not change command semantics, focus order, keyboard mappings, safety confirmation, or provider policy without a separate product ADR.
4. Generated raster mockups are references, not implementation assets, unless explicitly exported and licensed for use.
5. The initial implementation uses neutral placeholder token values. It must not invent a competing brand system.

## Required semantic token families

`Surface`, `Text`, `Border`, `Focus`, `Selection`, `Status`, `Operation`, `Spacing`, `Typography`, `Radius`, `Elevation`, `Density`, and `Motion` are the only top-level token families. Adding a family requires updating the design fixture and token conformance test.

## Automation identification

Issue #240 extends the existing presentation projections and `AutomationProperties` bindings.
These identifiers describe the rendered surface; they are not filesystem identities, invocation
targets, or permission to send input. Existing region identifiers remain unchanged.

| Surface | Identifier and scope | Lifetime |
|---|---|---|
| File-list row container | `FileListRow_` followed by the exact `DirectoryEntry.Name`, within `LeftFileList` or `RightFileList` | The same entry name keeps its identifier through focus, selection, activation, sorting and refresh. A rename changes it. |
| Settings shortcut hint label | `SettingsEntryHint`, on the existing intent-label `TextBlock` within `OperationKeyHints` | Only the existing `OpenSettings` hint carries it. It disappears when that hint is absent. |
| Address modal (Issue #495, ADR-0044) | `LeftAddress` and `RightAddress` stay on permanent display-only path text; `AddressEditorModal` names the single panel, `AddressEditorInput` the localized editable field, `AddressEditorStatus` its validation message, and `AddressEditorKeyHints` its noninteractive hint band | The pane path texts exist for the window lifetime; the editor and its children are visible only while address editing owns the session. |
| Bookmark manager (Issue #398, ADR-0080) | `BookmarkManagerModal` on the named panel; `BookmarkRow_` or `BookmarkCategory_` followed by the visible name on each row or category container within `BookmarkRows` or `BookmarkCategories`; `BookmarkKeyHints` on the band and `BookmarkHint*` on each hint button | While the manager is open. Row identifiers name a visible row, not a bookmark identity; identical names in different categories may share one. |
| Opposite-pane header action (Issue #547, ADR-0084) | `LeftOpenOtherPane` and `RightOpenOtherPane` on the two permanent header buttons | Only the active header exposes its button; both keep a fixed-size reserved slot. The complete localized Name and tooltip name the current folder, opposite direction and canonical shortcut. Readiness controls enabled state; the command revalidates at dispatch. |
| Permanent-deletion confirmation | `DeletionConfirmation`, on the existing named content container inside `OperationBar` | Only while the projected status is `DeleteAwaitingConfirmation`; every other render clears it. |
| Caption bar (Issue #379, ADR-0078) | `CaptionTitle` on the application-name text; `CaptionMinimize`, `CaptionMaximize` and `CaptionClose` on the three caption buttons | For the window's lifetime. `CaptionMaximize` keeps its identifier while its localized name switches between maximize and restore with the presenter state. |

A file-row lookup must first select its owning file list. Identical names in the two panes may
have identical row identifiers. Match the entire identifier exactly: preserve case, Unicode,
punctuation and trailing characters without normalization or escaping. The identifier adds no
parent path or provider identity token. It names a visible child, not a persistent file identity;
refresh may show a replacement at the same name, and a virtualized row need not be realized.
The identifier belongs to the `ListViewItem`, not a second copy in its template. Its accessible
name remains the entry name.

Deletion confirmation is the existing operation-bar state, not a separate framework dialog.
The identifier covers its status, detail and key hints without moving focus or changing keyboard
ownership. The container's `LabeledBy` points to the existing localized `OperationStatus` text, using
WinUI's named-container peer instead of relying on an identifier alone on a layout panel.
That peer retains its children. The attached identifier becomes empty outside confirmation;
WinUI may then fall back to the element's `OperationContent` name. Clients test for an exact
`DeletionConfirmation` match, not absence of the entire element or of every possible identifier.
That match is absent for transfer conflicts, name entry, running/completed operations and
recycling that does not request permanent-deletion confirmation. Existing `OperationBar`,
`OperationStatus` and child identifier declarations are preserved.

The settings hint remains informational. The shared key-hint template binds the identifier on
its label `TextBlock`, whose framework peer already exists; palette hints implement the same
projection property with an empty value. No new button, command or input route is introduced.
The UI evidence harness records these declarations in `cells.json`; its F8 admission and
unimplemented Input cells remain unchanged. Presentation/build proof does not claim live UIA,
screen-reader or Issue #94 release confirmation.

Framework basis: [WinUI automation-peer guidance](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/custom-automation-peers)
and [the Name/LabeledBy named-container implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/main/dxaml/xcp/core/core/elements/framework.cpp).

## Pane count strip — Issue #542

The existing pane projection supplies direct file/folder/selection counts in a
noninteractive TextBlock below each list. LeftPaneCounts and RightPaneCounts are
stable automation identifiers. Existing monospace, secondary text and pane-spacing
tokens apply. Full text wraps without fixed dimensions, trimming or a line limit;
the tooltip holds the same complete text and the normal TextBlock peer owns its
accessible content. It adds no tab stop or automatic per-keystroke announcement.

The authored brief and ordinary Claude design advice are retained in the Issue542
quality evidence. The requested /design command reported that it only manages
project access in the installed CLI; that invocation produced no design. The
advice's bottom placement is adopted with engineering adaptations: wrap complete
labels instead of hiding narrow-width details, explicitly show selection zero,
and qualify loaded-only or retained counts in text. Whole templates are localizable.
Native narrow/DPI/high-contrast and screen-reader results remain unclaimed.
