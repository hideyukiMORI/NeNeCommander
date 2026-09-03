# ADR-0012: Coordinate pane navigation through one session

Status: accepted

Date: 2026-09-03

## Context

`PaneReducer` owns focus and selection transitions over an immutable `PaneState`, and `IDirectoryReadPort` owns directory reads, but nothing connected keyboard intents to either. A location change is asynchronous, can fail or be cancelled, and can be superseded by a newer intent while a read is in flight. CMD-002 requires navigation to remain a reducer transition, CMD-006 requires cancellation to be an outcome, and ADV-016 requires identities to be frozen while work is in progress.

## Decision

Add one Application coordinator, `PaneSession`, with two operations: `NavigateAsync(FileSystemPath)` and `HandleAsync(UserIntent)`. It owns the current immutable `PaneSnapshot`, which is the product of a closed `PaneContent` (`Absent` or `PaneContentListed` holding `PaneState` and `DirectoryListing`) and a closed `PaneActivity` (`Idle`, `PaneLoading`, `PaneReadFailed`, `PaneReadCancelled`).

- Focus and selection intents are applied only by `PaneReducer.Apply`.
- A successful read is applied only by `PaneReducer.Navigate`, which lists the location, clears selection, and focuses the preferred item when present, otherwise the first entry. Navigating to the parent prefers the origin directory.
- `OpenFocused` starts a read only for a directory entry; file launch remains a future `IFileLauncher` boundary.
- `NavigateParent` uses the new `FileSystemPath.Parent`, which derives the containing location per provider without re-parsing and is absent at a provider root.
- While a read is in flight every intent returns the current snapshot unchanged. Each navigation carries a generation number; a read that completes after a newer navigation started is discarded.
- A failed or cancelled read leaves the listed content in place and records the typed activity with its target.

`PaneListingPresenter.Present(PaneSnapshot)` is the sole projection to rows, focus entry, status resource key, and address text. The App host keeps keyboard focus inside the file list, intercepts key events on the tunneling `PreviewKeyDown` route so the framework list never performs its own navigation, forwards mapped intents to the session, and assigns the presentation to controls. The mapper passes an unmapped raw virtual-key event through without touching a pending chord, because every printable key arrives once as a virtual key and once as its produced character.

## Rejected alternatives

- Letting the window code-behind call the read port and reducer directly: places orchestration and race handling in untested framework code and violates CMD-003.
- A view model with CommunityToolkit.Mvvm at this step: no binding requirement exists yet because the host assigns an immutable presentation; ADR-0003 governs the mechanism when a view model is introduced.
- Cancelling the superseded read through a linked token: the adapter completes synchronously, so a generation check is the deterministic mechanism; token cancellation remains available to callers.
- Clearing content on a failed read: hides the working listing and contradicts FS-010, which requires a refreshable pane state.

## Consequences

- `PaneSession` is single-owner and not thread-safe; the host drives it from the UI thread and owns the returned task.
- Half-page movement uses a composed visible-row capacity until the pane measures its own height.
- Hidden-item visibility, sorting, history, and refresh remain future reducer transitions.
- `DirectoryReadOutcome` exposes an internal constructor so the defect branch for an unregistered variant is provable.

## Migration and removal

`PaneListingPresenter.Present(DirectoryReadOutcome)`, `PaneListingPresented`, `PaneListingUnavailable`, and the window's `IntentMapped` event with `UserIntentMappedEventArgs` are removed in the same change.

## Executable proof

`PaneSessionTests` with the ADV-016 mappings, `PaneReducerTests` navigate cases, `FileSystemPathParentTests`, `PaneListingPresenterTests`, runtime verification of `j` / `l` / `h` through UI Automation, and the canonical gate.
