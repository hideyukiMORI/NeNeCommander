# ADR-0021: Rename the focus item through the shared name entry

Status: accepted

Date: 2026-09-04

## Context

`F2` maps to `UserIntent.Rename` but nothing handled it. ADR-0020 already gave the session a modal state that collects untrusted name text, a submission intent that carries it, and a gateway path that turns one name into one provider step. Renaming needs the same three things for a different subject: the focus item instead of the listed location. KBD-002 gives a modal only its documented keys, CS-022 keeps the decision out of the window, and ADV-015 requires the new name to face the same segment rules as any parsed path. A second awaiting-name state would be a second mechanism for one concern (ARC-001).

## Decision

- `OperationAwaitingName` is generalized instead of duplicated. It carries the closed `OperationKind` (`CreateDirectory` or the new `OperationKind.Rename`), the frozen `Subject` the name applies to, and the `InitialName` the editor starts from: the listed location and empty text for a creation, the focus item's path and its provider-reported `DirectoryEntry.Name` for a rename. `NameSubmission` and `Escape` behave identically for both kinds.
- `RenameRequest.Create(source, name)` freezes the source as the sole source and `source.Parent.Child(name)` as `Target`. A source without a parent is a provider root and is rejected as the new `FileOperationRequestFailureKind.SourceIsRoot`; a name the domain rejects is `InvalidName`; a target whose canonical text is ordinally equal to the source is `DestinationIsSource`. A change of letter case alone is accepted, so `FileSystemPathIdentityComparer` must not decide this rejection: the two paths are one filesystem identity but a different name.
- `IFileOperationPort.RenameAsync(source, target)` is the one new provider step. `FileOperationGateway` inspects the source, observes cancellation, calls the step once, and reports `FileOperationEffectKind.Renamed` on the source path with progress `1 / 1`, exactly like the creation path.
- `DualPaneSession` enters the shared state on `Rename` only when the active pane is listed and the listing has a focused entry, so an empty pane cannot start a rename. A successful rename refreshes the active pane focusing `Target` through `PaneSession.RefreshFocusingAsync` and the passive pane normally; that single "focus after success" decision is now derived from the request instead of being written once per request kind.
- `PaneContentListed.FindFocusedEntry` becomes the sole way to reach the provider-reported name and kind of the focus item; `PaneSession` now uses it too instead of its own private copy.
- `NameEntryPresentation` becomes a closed hierarchy whose active variant carries the initial text, so the presentation owns that text and the window only assigns it. The presenter reports `KeyboardContext.Modal` and the six rename statuses beside the six creation statuses.
- The Windows adapter revalidates the source identity, requires the target's parent to be identity-equal to the source's parent, treats an existing target that is not the source itself as `Conflict`, and then calls `Directory.Move` or `File.Move`.

## Rejected alternatives

- A separate `OperationAwaitingRename(source)` state: two states for one modal concern, duplicated freeze, escape, submission, and presentation branches (ARC-001).
- Deriving the initial name in the window from the address or row text: the provider-reported name lives in the listing, and CS-022 forbids the window from constructing it.
- Rejecting a case-only rename because the source and target are one filesystem identity: on Windows local paths identity is case-insensitive, so the only meaningful rename of case would become impossible. Ordinal canonical-text equality is the correct "nothing to do" test.
- Checking containment with `ProviderPathContainment` inside the source's parent: containment also admits grandchildren, so the parent identity check would still be required and the containment call could never fail behind it. Parent identity alone is exact and is still provider-aware rather than a string prefix (FS-001, FS-008).
- Letting the adapter derive the target from a name: the gateway would execute a request without a frozen target.

## Consequences

- The name editor is still a placeholder control beside the status line until the design handoff; it now starts with the current name selected.
- `RenameRequest` cannot be produced for a drive, share, or distribution root, so the session never offers rename for a location it cannot rename.
- A rename is one source, so it has no partially completed outcome; the presentation maps only succeeded, cancelled, and rejected.

## Migration and removal

`OperationAwaitingName(location)` is replaced by `OperationAwaitingName(kind, subject, initialName)` and `NameEntryPresentation.Active` by `ActiveNameEntry(initialText)`; both old shapes are removed in the same change. `IFileOperationPort` gains a required member every implementation adds in the same change.

## Executable proof

`RenameRequestTests`, `FileOperationGatewayTests` (rename, and the three ways it does not proceed), `DualPaneSessionTests` (freeze with the current name, escape, rename with focus on the new path, case-only rename, invalid name and provider conflict, no listing and empty listing, focus after movement), `DualPanePresenterTests`, `NullGuardTests`, `WindowsLocalFileOperationAdapterTests`, and the canonical gate.
