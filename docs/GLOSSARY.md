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
| selection anchor | The absent or exact owned visible path in PaneState from which pointer and keyboard range selection is measured; only PaneReducer establishes, preserves, rebinds or clears it (ADR-0090, ADR-0093). |
| pane interaction | One closed row, background or header-control UserIntent qualified by explicit pane side and the exact rendered PaneSnapshot, with owned visible entry membership for a row (ADR-0090). |
| command entry | The one application-level Bookmarks/Settings/Help button group and its Presentation-only native-focus context; it invokes existing intents and owns no Application modal (ADR-0090). |
| filesystem path | A validated `FileSystemPath` value with a known provider boundary. |
| provider boundary | `WindowsLocal`, `WindowsUnc`, or `Wsl`, including capabilities and semantics. |
| intent | A UI-independent request for behavior. |
| command | Application-layer orchestration of one intent. |
| operation | A filesystem mutation executed only by `FileOperationGateway`. |
| inspection purpose | The closed `FileInspectionPurpose` requested by the gateway: `IdentityOnly` for transfer, rename, directory creation and empty text-file creation, or `Deletion` for identity plus ordinary-deletion capability. The single provider inspection route owns both (ADR-0083). |
| unrequested deletion capability | `DeletionCapability.NotRequested`: no ordinary-deletion capability was requested or granted. It is distinct from inspected `Recycle` and `PermanentOnly`; ordinary deletion rejects a batch containing it before any effect. Verified-copy deletion retains its separate receipt authority (ADR-0083). |
| outcome | A closed typed success, cancellation, conflict, or failure result. |
| closed record cases | Known sealed cases whose public abstract root prevents ordinary non-friend external C# code from adding another concrete case through an externally inaccessible abstract member (ADR-0073). External abstract derivation and trusted friend assemblies remain possible; this is not an in-process security sandbox. |
| entry | One direct child of a read location with its validated path, provider-reported name, closed kind, and entry metadata. |
| entry visibility | The closed `EntryVisibility` (`Normal` / `Hidden`) reported by the Infrastructure reader for one entry. Windows local and UNC readers report `Hidden` for the Windows Hidden or System attributes; the WSL reader reports `Hidden` for a Linux name beginning with a dot, ignoring projected Windows attributes. Otherwise it is `Normal`. Application and Presentation consume this classification without deriving it again from names; `PaneReducer` alone decides the visible set (ADR-0024, ADR-0035, ADR-0057). |
| entry metadata | The `EntryMetadata` a provider reports for one entry: its entry visibility, entry size, and entry timestamp (ADR-0056). Application reads it and never infers it. |
| entry size | The closed `EntrySize` of one entry: a known non-negative byte count, or `Unknown` for a directory or a size the adapter could not read. Zero never stands for absence. |
| entry timestamp | The closed `EntryTimestamp` of one entry: its last-modification time carried in UTC, or `Unknown` when the provider reports none. |
| visible set | The ordered entries of a pane that its `HiddenItemVisibility` admits, in the order its sort order projects, held by `PaneState` as `VisibleEntries`. `PaneReducer` alone decides it; movement, paging, focus, and selection address it and nothing else. |
| sort order | The closed `PaneSortOrder` one pane holds: a `SortKey` (`Name`, `Extension`, `Size`, or `Modified`) and a `SortDirection` (`Ascending` or `Descending`), name ascending by default. `EntryOrdering` alone projects entries through it, directories first; it is carried through reads of the same pane. Each accepted sort change is saved independently for the left or right pane through the settings document and restored at startup (ADR-0067). |
| natural name order | The ADR-0074 display comparison owned by `EntryOrdering`: ASCII digit runs compare by integer magnitude and then shorter zero padding; other text compares ordinally ignoring case, with a whole-name ordinal tie-break. Name sorting uses the selected direction; other keys use ascending natural names only for ties. Provider path identity and the canonical listing snapshot are unchanged. |
| pane sort preferences | The immutable `PaneSortPreferences` holding the left and right pane's saved `PaneSortOrder` in one complete `UserSettings`; both default to name ascending. It supplies startup values, while each listed `PaneState` owns its current order (ADR-0067). |
| listing | An immutable `DirectoryListing`: the deterministically ordered entries of one location plus its completeness and unrepresentable-entry count. |
| pane counts | Presentation-only direct file/folder counts of the visible set and the loaded listing, plus explicit selection. Loaded counts include hidden admitted entries and are not an unqualified directory total for bounded or incomplete reads. No listing has no counts; a successful empty listing has zero counts. |
| entry boundary | The positive number of provider entries after which a read stops and reports a bounded listing. |
| pane snapshot | An immutable `PaneSnapshot`: the pane's closed content (absent or listed) and closed external activity (idle, reading, launching, failed, cancelled, or abandoned). |
| abandoned read | A pane read the user left with `Escape` while it was in flight: `PaneSession` supersedes it, cancels its own token, and shows `PaneReadAbandoned` over the unchanged content, and its waiting caller returns at once; the provider step runs to completion, its result is discarded, and a fault it raises is rethrown at the pane session's next entry point (ADR-0058). It is neither a provider failure nor a cancellation outcome. |
| pane session | The sole `PaneSession` coordinator that owns one pane snapshot and advances it through intents, reads, and focused-file handoffs. |
| pane history | The immutable sequence of at most 100 successful locations, including current, held independently by each `PaneState`; `PaneReducer` alone appends locations or moves its Back/Forward cursor after a successful read. |
| file handoff | One user-requested transfer of an admitted Windows local, UNC or WSL path to its current Windows Shell association through `IFileLauncher` (ADR-0065); acceptance does not claim process creation, successful opening, or external application completion. |
| pane side | `PaneSide.Left` or `PaneSide.Right`; the closed identity of one pane surface. |
| transfer route | The closed `TransferRoute` the Infrastructure.Windows router derives from the frozen sources' provider and the destination's provider: same Windows local, WSL to WSL (same or different distribution), Windows local to WSL, WSL to Windows local, or unavailable. It alone selects the adapter for preflight, atomic-move capability, move, copy, and verification (ADR-0059, ADR-0062, ADR-0066). |
| verified copy receipt | Immutable workflow evidence from the shared copy verifier, binding one frozen source root to its exact target and recording descendant identities, kinds, membership and content evidence. Composite-move deletion revalidates it at the source provider; it is not a tree lock or a persisted authorization token (ADR-0069). |
| source partially deleted | An explicit effect stating that some recorded descendants of a requested move source were removed before deletion stopped. Remaining source entries and the copy target are retained; it does not claim complete `SourceDeleted` or rollback (ADR-0069). |
| operation activity | The closed `OperationActivity` of the dual-pane session: idle, running with progress, awaiting confirmation, awaiting a name, completed with a gateway outcome, or request rejected. |
| empty text-file creation | One zero-byte direct child created exclusively through `CreateTextFileRequest` and `FileOperationGateway` (ADR-0085). The editable localized suggestion ends in `.txt`; the submitted name is used unchanged, no encoding is asserted and no editor is launched. |
| file created | The `FileCreated(target)` effect published only after the provider's exclusive empty-file step succeeds, including success after late cancellation. It does not mean a later refresh succeeded. |
| operation progress | The closed `FileOperationProgress` the gateway reports once per source whose every step completed: completed and total source counts. |
| design token | A semantic resource such as surface, spacing, typography, or state color. |
| key binding | One declared entry of the canonical key map: a keyboard context, a layout-translated key, its explicit modifier state, and the single intent it emits. |
| key hint | One displayed shortcut: the localization resource naming a key cap and the one naming what the key does. Hints are generated from key bindings, never written into a view (KBD-005). |
| operation bar | The single full-width surface at the bottom of the shell. It shows the operation status with its closed `OperationBarTone`, the closed `OperationDetail`, the name entry, and the key hints of the current keyboard context. |
| color scheme | One of the 129 approved `ColorScheme` values in the sole privately owned fixed catalog (ADR-0087). Each has a kebab-case identifier, a closed `Dark` or `Light` appearance, and exactly one resource dictionary `Themes/Schemes/<identifier>.xaml` that defines every color key. |
| settings document | The sole persisted preferences file `%LOCALAPPDATA%\NeNeCommander\settings.json`, written as schema version 6 with color scheme, next-launch hidden-item and immediate interface-language choices, complete bookmark catalog, independent pane sort orders, footer-hint visibility and nullable normal window placement (ADR-0041, ADR-0067, ADR-0082, ADR-0086, ADR-0089). Exact schemas 1-5 remain readable without migration writes and gain absent placement; schemas 1-4 gain System language; schemas 1-3 also gain visible hints. `ISettingsStore` reads and atomically replaces one complete value. Absent or rejected startup input keeps `UserSettings.Default`; malformed content is never repaired automatically. |
| display preferences | The constructor-complete `DisplayPreferences` within `UserSettings`: `ColorScheme`, next-launch `HiddenItemVisibility`, immediate `KeyHintVisibility` and immediate `InterfaceLanguage`. It owns no mutable state or independent persistence (ADR-0082, ADR-0086). |
| interface language | The closed `InterfaceLanguage` choice System, Japanese, English or SimplifiedChinese, persisted exactly as `system`, `ja-JP`, `en-US` or `zh-Hans`. A selection immediately updates application-owned text through the one window-scoped MRT resource context and persists through the existing queue. System restores the platform language qualifier captured before selection. Regional formatting, keyboard layout and IME are unchanged; framework-owned strings and speech require native evidence (ADR-0086, ADR-0088). |
| key-hint visibility | The persisted closed `KeyHintVisibility` preference for normal footer hints. Shown is the default; Hidden retains canonical F1 help and required confirmation/name-entry hints. It never changes the key map or hides operation information (ADR-0082). |
| settings editor | The session-owned modal opened by `Ctrl+,` that saves one complete settings value whenever a display preference changes, including an explicit choice from the 129 color schemes. Its transient name/identifier search filters candidates without applying or saving them (ADR-0087). `Escape` closes it without rollback. |
| settings warning | Persistent localized presentation of a startup rejection or failed save, owned independently of the file-operation bar. |
| transient scope | One short-lived interaction the application session coordinates but does not own: the address editor, command palette, window adjustment mode, Locations picker, keyboard help and name-filter editor. Each is one closed state, one admission rule, and one expected-state validation, and each appears in `TransientScopeSnapshot`. |
| scope owner | The sole owner of one transient scope, in the `SettingsSession` shape: its own closed state under one lock, no reference to another owner, no freeze predicate of its own, no pane effect, and no dispatch. `AddressEditorSession`, `CommandPaletteSession`, `WindowAdjustmentSession`, `LocationsSession`, `KeyboardHelpSession` and `PaneFilterSession` are carried by `TransientScopeOwners`; pane-specific owners use its single `PaneScopeOwners` group (ADR-0051, ADR-0050, ADR-0055, ADR-0094). |
| drive location | One listed Windows volume: a validated `WindowsLocalPath` drive root, its closed `DriveKind` (`Fixed`, `Removable`, `Network`, `Optical`, or `Unknown`, which also covers a volume that is not ready), and the volume label it reported, if any. `IDriveCatalog` lists them fresh on every read; nothing about them is persisted. |
| location item | One selectable entry of the Locations picker: a listed drive location or WSL distribution root, carrying the root a selection navigates to. |
| observed drive kind | The optional OS-reported `DriveKind` attached to a successful drive-rooted read and retained with the pane's listing. It describes that observation, not future mapping identity or mutation permission; UNC and WSL have no drive-kind report (ADR-0072). |
| Locations picker | The session-owned transient scope opened by `Ctrl+G` that lists the drive locations and the WSL distribution roots as two independent sections, each listed or failed, and sends the pane active at open to the selected root through the single pane navigation route (ADR-0055). |
| window host dependencies | The immutable `CommanderWindowDependencies` supplied by the App composition root: the existing keyboard mapper, application session, defect callback and already-read local time zone. It carries references without acquiring state or taking ownership; the two initial pane paths remain separate window inputs. |
| saved window placement | The nullable `SavedWindowPlacement` stores one validated normal physical-pixel rectangle and only Restored or Maximized state. The single tracker retains it while minimized/unavailable; the pure restore planner chooses and clamps one attached work area. A changed final value joins the existing settings queue only at normal close; rejected startup text is not automatically repaired (ADR-0089). |
| window placement | The closed `WindowPlacement` the App host reads fresh for each window adjustment: the window bounds, the closed `WindowPresenterState` (`Restored`, `Maximized`, `Minimized`, `NotOverlapped`, or `Unavailable`), the rasterization scale with the presenter's declared preferred minimum width and height (zero when none is declared) as `WindowSizeConstraint`, and the work areas as `WindowWorkAreas`. Application never reads or writes the window itself. |
| window bounds | A `WindowBounds` rectangle in whole physical pixels: left, top, and a width and height of at least one pixel. |
| adjustment step | The distance one window adjustment moves an edge: 32 device-independent pixels multiplied by the placement's rasterization scale and rounded half away from zero, read fresh on every action. |
| work area | The physical rectangle of one attached display that windows may occupy, excluding the taskbar and other reserved bands; a window placement carries the work area of its own display and those of every attached display. |
| caption rule | The one boundary rule of a window move: some attached work area holds a run of the window's top edge row at least `min(step, width)` long, and either that work area continues `min(step, height)` below the row or one work area whose top edge is its bottom edge covers a segment of that run at least as long and reaches at least as far below the row. Two work areas with a gap between them are not continuous, so the rule refuses the band above the gap. A move that fails it is refused with `CaptionWouldLeaveDesktop`; enlarge and shrink keep the top-left corner and are not tested against it. |
| gate | An executable check called by `eng/check.ps1` locally and in CI. |
| CS-013 measurement | The versioned `cs013-v1` logical-line, cognitive, nesting, explicit-parameter and combined-type metrics defined by ADR-0075 and produced by `eng/measure-complexity.ps1`. A standalone measurement is not an active gate or a compiler-success claim. |
| palette caption | The immutable localized command title and canonical shortcut with their label-derived automation identity, captured by `CommandPaletteCaption` for one palette row. |
| palette context | The immutable localized target and opposite-pane summaries captured once by `CommandPaletteContext` for an open palette; it owns no application or selection state. |
| palette feedback | The immutable reason, unavailable marker, selected detail and complete automation name produced by `CommandPaletteFeedback` from the captured availability, caption and context. |
| waiver | A narrow, owned, expiring exception that does not weaken a protected invariant. |
| pane projection source | The immutable snapshot/frame pair captured by `PaneProjectionSource` to qualify reuse of a pane's rows. |
| pane header | The immutable status, address, sort and observed-location indications projected from one snapshot by `PaneHeaderPresentation`. |
| pane pair | The two projected panes and Application-owned active side captured by `PanePairPresentation`; each frame comes from its pane's projection source. |
| operation bar presentation | The immutable status, numeric detail, semantic tone and canonical key hints captured by `OperationBarPresentation`. |
| operation interaction presentation | The immutable name editor, conflict modal and input context captured by `OperationInteractionPresentation`; it owns no operation or input state. |
| command palette view | The App-owned `CommandPaletteView` that binds the existing palette controls and forwards captured typed intents through the window; `CommandPaletteViewState` still owns query and selection. |
| operation bar view | The App-owned `OperationBarView` that binds status, counts, name entry and conflict controls without owning an operation or an async task. |
| commander pane view | The App-owned `CommanderPaneView` that binds one pane's rows, address, status and semantic activation resources to its existing controls. |
| bookmark browse view | The App-owned `BookmarkBrowseView` that binds category/entry lists, selection and browsing controls, sharing the overlay's rendering guard. |
| bookmark details view | The App-owned `BookmarkDetailsView` that binds bookmark/category drafts, confirmation controls and detail focus, without owning editor state. |
| bookmark browse presentation | The immutable filtered result owned by `BookmarkBrowsePresentation`; its selected row is derived from the frozen rows and retains the same row object identity. |
| bookmark manager region | One of the two focus regions of the bookmark manager's browse state, the category column or the bookmark list, named by `BookmarkManagerRegion` (ADR-0080). Movement and the `a`/`e`/`d` commands act on the region that holds focus. |
| bookmark key band | The bottom band of the bookmark manager that shows the browse state's key hints for the focused region, projected by `BookmarkManagerKeyHintPresenter` from the canonical bookmark table; each hint except movement is also a pointer button that runs the same action (ADR-0080). |
| bookmark delete confirmation | The session-owned `BookmarkDeleteConfirmation` state that holds one unchanged bookmark until the user confirms its deletion; it starts on Cancel and changes or saves nothing before confirmation (ADR-0080). |
| bookmark keyboard view | The App-owned `BookmarkKeyboardView` that remembers the focused region and applies each resolved `BookmarkManagerCommand` without deciding it. |
| location row | A localized `LocationRow` retaining the exact Application location item, its display/detail text and UIA name; its stable UIA ID is derived only from the validated display name. |
| frozen transfer | The internal immutable `FrozenTransfer` holding the original validated request, owned ordered source snapshots and destination across gateway-issued continuations; resume never reinspects or replaces those facts. |
| transfer conflict response | The immutable `TransferConflictResponse` pairing an explicit decision and its selected current/all scope; it grants no ownership of a continuation. |
| conflict decision submission | The UI intent `ConflictDecisionSubmission` binding a decision and scope to the exact displayed `OperationAwaitingConflict`; it cannot authorize a later or value-equal foreign owner. |
| conflict modal view | The App-owned `ConflictModalView` that creates native controls for one conflict owner and forwards only current, ready callbacks; it owns no session or provider effect. |
| transfer execution | One internal `TransferExecution` attempt invoked only under FileOperationGateway's existing lease, owning its effects and skipped-source accounting without another lease or public mutation entry point. |
| transfer preflight validation | The existing gateway admission predicates, owned by internal `TransferPreflightValidation`, that reject substituted/reordered sources, changed choices and out-of-destination targets. |
| file entry transfer | The immutable `FileEntryTransfer` annotations for an entry's selected target and conflict choice; provider path, identity and deletion capability remain frozen. |
| file operation effect summary | The immutable `FileOperationEffectSummary` owning ordered completed effects, explicitly untransferred sources and source-qualified provider receipts in one outcome. |
| transfer conflict pause | The internal `TransferConflictPause` pairing a returned conflict set with its exact gateway-issued continuation in an awaiting outcome. |
| `WslRewriteFacts` | Immutable internal group of length, link count and write/change timestamps captured from the same opened WSL handle; WslHandleFacts preserves their existing projections and token order. |
| `WslEntryMetadata` | Immutable internal group of a WSL entry identity, observed kind and raw attributes; its path and independently observed name remain owned by WslFileSystemEntry. |
| `WindowsLocalOperationSteps` | Internal owner of the existing local mutation/capability/verification bodies, invoked only inside the Windows local adapter execution boundary. |
| `WindowsLocalTransferPreflight` | One-call owner of ordered Windows local transfer reservations, choices and conflicts; retains no plan across calls and performs no mutation. |
| command surface | One closed Palette, ItemContext or FolderContext case over the existing command-selection owner; it controls captured catalog membership and the sole visible adapter (ADR-0091). |
| command context request | The immutable qualified PaneInteraction plus CommandSurface derived from captured pane state without effects; CommanderSession alone applies and captures synchronously (ADR-0091). |
| command menu section | One localized explicit target heading with rows derived from the sole command catalog: focus, selection/focus fallback, New destination or current folder (ADR-0091). |
| menu invocation qualifier | The adapter-only CommandMenuInvocation that orders one native flyout's same-turn Click/Closed callbacks against its existing exact open state; it is not a scope owner or dispatcher (ADR-0091). |
| Explorer reveal | A user-requested path handoff through IFileLauncher.RevealAsync that asks Shell to select the exact path in its parent namespace; acceptance does not prove a selected window or stable target identity (ADR-0091). |
| file launch purpose | The closed activation/reveal purpose carried by the one PaneLaunching, PaneLaunchFailed and PaneLaunchCancelled lifecycle; no parallel busy flag exists (ADR-0091). |

| pane name filter | The immutable PaneNameFilterQuery stored only in PaneState; exact text is parsed into literal OrdinalIgnoreCase whitespace-separated AND terms over loaded entry names, without path normalization, wildcard semantics or provider reads (ADR-0094). |
| filter editor | The exact PaneFilterEditing capture of side and source pane owned by PaneFilterSession; it controls temporary native editing but owns no duplicate applied query (ADR-0094). |
| pane scope group | The immutable PaneScopeOwners and PaneScopeSnapshot composition carriers for address, Locations and name-filter scopes; their existing public snapshots remain the only access path (ADR-0094). |
| bookmark command coordination | CommanderBookmarkCommands owns the existing bookmark navigation latch and direct/manager navigation bodies; CommanderSession remains the sole dispatcher (ADR-0094). |

- **OverwriteContents**: ADR-0095's explicitly confirmed update of the same target file
  object's contents, with observed capability and exact target evidence. It is not an
  atomic pathname replacement; a newly created alias may see the same changed content.
  The design is accepted while production implementation remains pending.
- **Recovery mapping**: the bounded, append-only artifact record that relates a verified
  original-content backup and prepared content to the original target before mutation.
  It is evidence for manual inspection, not permission for automatic restoration.

- **Provider step receipt**: the mandatory immutable `ProviderStepReceipt` returned
  alongside explicit success, expected failure or cancellation. It retains ordered legacy effects, admitted target observations and preparation
  inventory; runtime overwrite sequencing remains a separate implementation obligation.
- **Operation provider step**: `OperationProviderStep` binds that exact receipt to the
  original source, or to the requested target for entry creation, without inferring
  path stability or a completed effect from the mere existence of a provider return.
- **File operation effect journal**: the execution-owned `FileOperationEffectJournal`
  accumulates gateway effects and provider receipts and publishes one immutable summary;
  it is not another executor, operation state or public mutation entry point.

- **Observed transfer entry**: an exact handle-derived full/stable identity snapshot with
  CurrentPathVerified or LastKnownPath; neither means a perpetual pathname guarantee.
- **Provider target observation**: the closed untouched, possibly changed, original-content
  verified or desired-content verified state of an admitted existing target. Absence in
  an unrelated provider receipt does not assert an untouched overwrite target.
- **Transfer artifact receipt**: a role-qualified preparation observation, either an exact
  identified object with content/link state or an unidentified attempted candidate. A
  later failed query preserves a previously known identity with qualified path evidence.
- **Recovery mapping content**: the initial validated frame and independent append state.
  RecoveryFrameEvidence pins complete framed bytes; RecoveryFrameObservation separately
  retains the native flush observation. Full readback is not a flush or power-loss claim.

- **Provider step completion**: `ProviderStepCompletion` explicitly distinguishes a
  succeeded, failed-with-reason or cancelled provider mutation. A cancelled step retains
  its complete receipt; absence of failure alone does not mean success. The gateway
  records that receipt before stopping without inventing copy, verification, source
  removal or progress completion for an interrupted step.

- **Windows local handle facts**: internal native observations of one held object's ID,
  basic timestamps/attributes, standard length/link/delete/directory fields and filesystem
  name. They do not certify path stability, local drive eligibility or a metadata freeze.
- **Windows unnamed stream observation**: the bounded native membership check that accepts
  only one complete default data-stream entry of the expected length. Extra, incomplete,
  malformed or failed observations cannot authorize omission of other streams.

- **Windows transfer path lease**: the internal owner of one existing transfer file and
  every directory handle from its admitted drive root. Each component is opened relative
  to its held parent; original identity, non-link shape and exact current path are
  re-observed without resetting the baseline. It grants no overwrite capability alone.
- **Windows transfer path observation**: an instantaneous held-handle normalized DOS
  pathname and local volume-GUID-form check. It remains separate from object identity
  and cannot promise permanent drive mapping, metadata or alias exclusion.

- **Verified artifact content**: the last successful complete byte observation of an
  owned preparation, retained as historical length/hash evidence across later read-only
  verification failure. It does not establish current equality or mutation/cleanup
  authority. The one-shot preparation refuses rewriting that verified baseline; fresh
  current admission must compare exact bytes against it (ADR-0095).
