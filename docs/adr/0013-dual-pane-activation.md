# ADR-0013: Coordinate both panes and the active side through one dual-pane session

Status: accepted

Date: 2026-09-03

## Context

`PaneSession` coordinates one pane. The product has two panes and exactly one active pane that receives navigation and file-operation intents (glossary: active pane, passive pane). `Tab` maps to `ActivateOtherPane`, KBD-004 requires activation not to move either pane's focus item, and ADV-016 requires a read in flight to land in the pane that started it even when activation changes meanwhile.

## Decision

Add one Application coordinator, `DualPaneSession`, over two distinct `PaneSession` instances and a closed `PaneSide` (`Left` or `Right`). Its snapshot, `DualPaneSnapshot`, is the product of both pane snapshots and the active side.

- Only `ActivateOtherPane` changes the active side; it touches neither pane session.
- Every other intent is handled by the active pane's `PaneSession` alone. Pane-local transitions remain owned by `PaneReducer` (CMD-002); the active side is owned by `DualPaneSession` and is the only state it holds.
- `NavigateAsync(PaneSide, FileSystemPath)` reads into a named side regardless of activation, so the composition root can load both initial locations.
- Because reads belong to a `PaneSession`, a read that completes after activation changed lands in its own pane and the active side is unaffected.

`DualPanePresenter.Present(DualPaneSnapshot)` projects both panes through `PaneListingPresenter` and assigns a closed `PaneFrame` (`Active` or `Passive`) that names the semantic border brush and thickness resources. The App host assigns the presentation to both panes' controls, applies the frames by resource key, and keeps keyboard focus on the active file list.

## Rejected alternatives

- Holding the active side in the window: a domain decision in code-behind (CMD-003, CS-022).
- Merging both panes into one session with a side parameter on every call: couples the pane's in-flight read to activation and makes ADV-016 harder to prove.
- Choosing border brushes in code-behind: hard-codes visual choice outside semantic resources (ARC-012).

## Consequences

- Initial locations for both panes are composed as constants until drive discovery and persisted locations exist.
- Cross-pane operations (copy and move to the passive pane) can now be expressed against `DualPaneSnapshot` but remain future work behind `FileOperationGateway`.
- The pane labels are static side names; the active pane is conveyed by its frame.

## Migration and removal

`CommanderWindow` takes the dual-pane coordinator instead of one pane session; the single-pane constructor is removed in the same change.

## Executable proof

`DualPaneSessionTests` with the ADV-016 mapping, `DualPanePresenterTests`, null-guard tests, runtime verification of both panes and their frames through UI Automation, and the canonical gate.
