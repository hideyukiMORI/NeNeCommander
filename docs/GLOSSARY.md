# Glossary

Status: normative

Use these terms in code, documentation, tests, telemetry, and UI resources. Do not invent synonyms for the same concept.

| Term | Exact meaning |
|---|---|
| pane | One of the two file-list surfaces. |
| active pane | The sole pane that receives navigation and file-operation intents. |
| passive pane | The other pane; the default destination for cross-pane operations. |
| focus item | The single item addressed by movement and open commands. |
| selection | The explicit set of items marked for a batch operation. |
| filesystem path | A validated `FileSystemPath` value with a known provider boundary. |
| provider boundary | `WindowsLocal`, `WindowsUnc`, or `Wsl`, including capabilities and semantics. |
| intent | A UI-independent request for behavior. |
| command | Application-layer orchestration of one intent. |
| operation | A filesystem mutation executed only by `FileOperationGateway`. |
| outcome | A closed typed success, cancellation, conflict, or failure result. |
| entry | One direct child of a read location with its validated path, provider-reported name, closed kind, and entry metadata. |
| entry visibility | The closed `EntryVisibility` a provider reports for one entry: `Normal`, or `Hidden` when the provider marks it hidden or system. It is a reported attribute, never derived from the entry name. |
| entry metadata | The `EntryMetadata` a provider reports for one entry: its entry visibility, entry size, and entry timestamp (ADR-0056). Application reads it and never infers it. |
| entry size | The closed `EntrySize` of one entry: a known non-negative byte count, or `Unknown` for a directory or a size the adapter could not read. Zero never stands for absence. |
| entry timestamp | The closed `EntryTimestamp` of one entry: its last-modification time carried in UTC, or `Unknown` when the provider reports none. |
| visible set | The ordered entries of a pane that its `HiddenItemVisibility` admits, in the order its sort order projects, held by `PaneState` as `VisibleEntries`. `PaneReducer` alone decides it; movement, paging, focus, and selection address it and nothing else. |
| sort order | The closed `PaneSortOrder` one pane holds: a `SortKey` (`Name`, `Extension`, `Size`, or `Modified`) and a `SortDirection` (`Ascending` or `Descending`), name ascending by default. `EntryOrdering` alone projects entries through it, directories first; it is carried through reads of the same pane and not persisted. |
| listing | An immutable `DirectoryListing`: the deterministically ordered entries of one location plus its completeness and unrepresentable-entry count. |
| entry boundary | The positive number of provider entries after which a read stops and reports a bounded listing. |
| pane snapshot | An immutable `PaneSnapshot`: the pane's closed content (absent or listed) and closed external activity (idle, reading, launching, failed, cancelled, or abandoned). |
| abandoned read | A pane read the user left with `Escape` while it was in flight: `PaneSession` supersedes it, cancels its own token, and shows `PaneReadAbandoned` over the unchanged content; the provider step runs to completion and its result is discarded (ADR-0058). It is neither a provider failure nor a cancellation outcome. |
| pane session | The sole `PaneSession` coordinator that owns one pane snapshot and advances it through intents, reads, and focused-file handoffs. |
| pane history | The immutable sequence of at most 100 successful locations, including current, held independently by each `PaneState`; `PaneReducer` alone appends locations or moves its Back/Forward cursor after a successful read. |
| file handoff | One user-requested transfer of a validated Windows local path to its current Windows Shell association through `IFileLauncher`; acceptance does not claim process creation, successful opening, or external application completion. |
| pane side | `PaneSide.Left` or `PaneSide.Right`; the closed identity of one pane surface. |
| operation activity | The closed `OperationActivity` of the dual-pane session: idle, running with progress, awaiting confirmation, awaiting a name, completed with a gateway outcome, or request rejected. |
| operation progress | The closed `FileOperationProgress` the gateway reports once per source whose every step completed: completed and total source counts. |
| design token | A semantic resource such as surface, spacing, typography, or state color. |
| key binding | One declared entry of the canonical key map: a keyboard context, a layout-translated key, its explicit modifier state, and the single intent it emits. |
| key hint | One displayed shortcut: the localization resource naming a key cap and the one naming what the key does. Hints are generated from key bindings, never written into a view (KBD-005). |
| operation bar | The single full-width surface at the bottom of the shell. It shows the operation status with its closed `OperationBarTone`, the closed `OperationDetail`, the name entry, and the key hints of the current keyboard context. |
| color scheme | One of the eight approved `ColorScheme` members. Each has a kebab-case identifier, a closed `Dark` or `Light` appearance, and exactly one resource dictionary `Themes/Schemes/<identifier>.xaml` that defines every color key. |
| settings document | The sole persisted preferences file `%LOCALAPPDATA%\NeNeCommander\settings.json`, shaped `{ "schemaVersion": 1, "showHiddenItems": <boolean>, "colorScheme": "<identifier>" }`. It is read and atomically replaced as one complete value through `ISettingsStore`; an absent or rejected startup document keeps `UserSettings.Default`, and malformed content is never repaired automatically. |
| settings editor | The session-owned modal opened by `Ctrl+,` that saves one complete settings value whenever the launch-hidden default or one of the eight color schemes changes. `Escape` closes it without rollback. |
| settings warning | Persistent localized presentation of a startup rejection or failed save, owned independently of the file-operation bar. |
| transient scope | One short-lived interaction the application session coordinates but does not own: the address editor, the command palette, the window adjustment mode, and the Locations picker. Each is one closed state, one admission rule, and one expected-state validation, and each appears in `TransientScopeSnapshot`. |
| scope owner | The sole owner of one transient scope, in the `SettingsSession` shape: its own closed state under one lock, no reference to another owner, no freeze predicate of its own, no pane effect, and no dispatch. `AddressEditorSession`, `CommandPaletteSession`, `WindowAdjustmentSession`, and `LocationsSession` are the scope owners, carried together by `TransientScopeOwners` (ADR-0051, ADR-0050, ADR-0055). |
| drive location | One listed Windows volume: a validated `WindowsLocalPath` drive root, its closed `DriveKind` (`Fixed`, `Removable`, `Network`, `Optical`, or `Unknown`, which also covers a volume that is not ready), and the volume label it reported, if any. `IDriveCatalog` lists them fresh on every read; nothing about them is persisted. |
| location item | One selectable entry of the Locations picker: a listed drive location or WSL distribution root, carrying the root a selection navigates to. |
| Locations picker | The session-owned transient scope opened by `Ctrl+G` that lists the drive locations and the WSL distribution roots as two independent sections, each listed or failed, and sends the pane active at open to the selected root through the single pane navigation route (ADR-0055). |
| window placement | The closed `WindowPlacement` the App host reads fresh for each window adjustment: the window bounds, the closed `WindowPresenterState` (`Restored`, `Maximized`, `Minimized`, `NotOverlapped`, or `Unavailable`), the rasterization scale with the presenter's declared preferred minimum width and height (zero when none is declared) as `WindowSizeConstraint`, and the work areas as `WindowWorkAreas`. Application never reads or writes the window itself. |
| window bounds | A `WindowBounds` rectangle in whole physical pixels: left, top, and a width and height of at least one pixel. |
| adjustment step | The distance one window adjustment moves an edge: 32 device-independent pixels multiplied by the placement's rasterization scale and rounded half away from zero, read fresh on every action. |
| work area | The physical rectangle of one attached display that windows may occupy, excluding the taskbar and other reserved bands; a window placement carries the work area of its own display and those of every attached display. |
| caption rule | The one boundary rule of a window move: some attached work area holds a run of the window's top edge row at least `min(step, width)` long, and either that work area continues `min(step, height)` below the row or one work area whose top edge is its bottom edge covers a segment of that run at least as long and reaches at least as far below the row. Two work areas with a gap between them are not continuous, so the rule refuses the band above the gap. A move that fails it is refused with `CaptionWouldLeaveDesktop`; enlarge and shrink keep the top-left corner and are not tested against it. |
| gate | An executable check called by `eng/check.ps1` locally and in CI. |
| waiver | A narrow, owned, expiring exception that does not weaken a protected invariant. |
