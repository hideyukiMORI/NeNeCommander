# ADR-0028: Reuse pane rows through incremental projection

Status: accepted

Date: 2026-09-05

## Context

`CommanderWindow.RenderPanes` called the one-shot `DualPanePresenter` for every focus movement and operation-progress notification. That call projected every entry in both panes into new `PaneRow` instances and then assigned both `ListView.ItemsSource` properties again. A sequence of M focus or progress updates over N entries therefore performed approximately N×M row projections and allocations even when no pane content changed.

`PaneListingPresenter` must remain the sole owner of row, focus, selection, frame, status, and address projection. The App host may apply render-ready values but may not compare application state or decide which rows changed.

## Decision

Keep `PaneListingPresenter` and `DualPanePresenter` as the canonical projections and add an internal previous-presentation path used only by the App host and executable tests. A public one-shot call delegates to the same projection with no previous value.

`PaneRows` owns one `ObservableCollection<PaneRow>`, exposes it as a read-only observable row source, and indexes rows by `FileSystemPathIdentityComparer`. A fresh listing creates all rows once. When the same immutable `DirectoryListing` is projected again, `PaneListingPresenter` computes the affected path set from the old and new focus, the symmetric selection difference, and the pane frame. It replaces only rows whose closed `PaneRowMark` changed. Activity-only and operation-progress updates reuse every row; an unchanged snapshot and frame reuse the entire pane presentation.

`CommanderWindow` retains only the latest `DualPanePresentation`, supplies it to the next canonical projection, and reassigns a list's `ItemsSource` only when the projected row-source identity changes. Observable replacement notifications update affected realized rows. The view still makes no domain or presentation decision.

## Rejected alternatives

- Keep recreating rows and rely on ListView virtualization: virtualization limits realized controls but does not remove N row allocations, mark calculations, or ItemsSource resets.
- Make `PaneRow` mutable and raise property notifications itself: spreads projection transitions across row objects and duplicates the presenter's mark-precedence rules.
- Compare `PaneSnapshot` values in `CommanderWindow`: places application-state decisions in the framework boundary and violates the sole projection mechanism.
- Cache only a complete pane and rebuild on every focus or selection change: fixes progress updates but retains the N×M focus-navigation cost.

## Consequences

- Progress and activity changes are O(1) for pane rows; focus movement replaces at most the old and new focus rows. Selection work is proportional to selected and changed paths, not every listed entry.
- A new `DirectoryListing` rebuilds the row source even if its values equal an earlier listing. Refresh therefore cannot retain stale provider entry objects.
- The internal incremental call consumes the previous presentation as cache state; its read-only observable `Rows` view is intentionally shared and may receive replacement notifications. Public one-shot callers receive an independent projection.
- Initial creation remains O(N) and owns one path index alongside the rows.

## Migration and removal

The App's unconditional ItemsSource assignment is removed in the same change. No second presenter or view-side comparison remains. Future hidden-entry projection must build and index only the presenter's visible row set while retaining this same incremental path.

## Executable proof

`PaneListingPresenterTests` proves activity-only source reuse and exact old/new-focus replacement with an unaffected row retained. `DualPanePresenterTests` proves operation progress reuses both pane row sources. The existing presentation suite proves every mark, frame, status, detail, name entry, and key hint. Release measurement covers 0, 100, and 10,000 entries over 100 updates. The final canonical CI gate is the merge-readiness proof.
