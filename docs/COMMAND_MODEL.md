# Command Model

Status: normative

All behavior follows one pipeline:

```text
WinUI input
  -> input-specific mapper
  -> typed intent
  -> application command/query
  -> validation and capability decision
  -> gateway or reducer
  -> typed outcome and immutable state
  -> presentation rendering
```

There is no direct UI-to-filesystem route.

### CMD-001 — Mutations use `FileOperationGateway`

- Status: **active**
- Enforcement: dependency boundary and forbidden-API scan.

Every filesystem mutation is represented by one typed operation request and executed by `FileOperationGateway`. The gateway owns preflight validation, conflict policy, progress, cancellation, provider dispatch, error normalization, and outcome creation.

### CMD-002 — Pane transitions use `PaneReducer`

- Status: **active**
- Enforcement: state-surface scan and reducer tests.

Navigation, focus, selection, sorting, hidden-item visibility, history, and refresh state are transitions applied only by `PaneReducer` to an immutable `PaneState`. The active side is held only by `DualPaneSession` and changes only on `ActivateOtherPane`.

### CMD-003 — UI emits intents, not decisions

- Status: **active**
- Enforcement: project boundary and presentation tests.

Code-behind may translate a framework event into an intent and forward it. It may not choose operation semantics, destination policy, overwrite behavior, provider behavior, or domain state transitions.

### CMD-004 — Preflight is atomic and complete

- Status: **active**
- Enforcement: command tests.

A mutating command validates sources, destination, collisions, recursion, provider capabilities, and destructive policy before beginning work. A batch is not partially started because later inputs failed validation.

### CMD-005 — Outcomes are exhaustive

- Status: **active**
- Enforcement: closed result type and exhaustive tests.

Every command returns the canonical outcome model. Boolean success flags, magic strings, null-as-failure, provider exceptions leaking to presentation, and unrelated result types are prohibited.

### CMD-006 — Cancellation has one meaning

- Status: **active**
- Enforcement: cancellation tests.

Cancellation means no new unit of work starts after observation. Already completed filesystem effects are reported explicitly. Cancellation is an outcome, not an error dialog and not an unobserved exception.

### CMD-007 — Queries do not mutate

- Status: **active**
- Enforcement: interface separation and tests.

Directory enumeration, capability discovery, metadata reads, and settings reads are queries. A query may populate an owned cache only behind its declared boundary; it may not alter pane or operation state as a hidden side effect.

### CMD-008 — Adapters translate; they do not reinterpret

- Status: **active**
- Enforcement: adapter contract tests.

Provider adapters translate canonical requests and platform responses. Product policy belongs in application commands; path invariants belong in domain types; UI wording belongs in presentation resources.

### CMD-009 — Keyboard input uses `KeyboardIntentMapper`

- Status: **active**
- Enforcement: presentation scan and keyboard contract tests.

All non-text keyboard shortcuts are mapped by one `KeyboardIntentMapper`. Pages, controls, and view models do not maintain private key maps. Context such as text-entry focus is an explicit mapper input.

## Canonical mechanism registry

| Concern | Only approved mechanism |
|---|---|
| filesystem path parsing | `FileSystemPath.Parse` |
| filesystem mutations | `FileOperationGateway` |
| Windows local file-operation provider | `WindowsLocalFileOperationAdapter` |
| directory reads | `IDirectoryReadPort` boundary with `DirectoryListing` ordering |
| pane navigation and intent routing | `PaneSession` |
| active side, intent routing between panes, and starting file operations | `DualPaneSession` |
| pane projection | `PaneListingPresenter` over `PaneSnapshot` |
| pane state | `PaneReducer` |
| keyboard mapping | `KeyboardIntentMapper` |
| view-model notifications and commands | CommunityToolkit.Mvvm source generators |
| expected operation results | canonical closed `OperationOutcome` model |
| settings persistence | `ISettingsStore` boundary |
| file launching | `IFileLauncher` boundary |
| time | `IClock` boundary |
| identifiers | `IIdentifierSource` boundary |
| object construction | `NeNeCommander.App` composition root |

A new row requires an accepted ADR. Replacing a row is one migration: the old mechanism and compatibility path are removed in the same change.
