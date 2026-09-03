# ADR-0020: Create a directory through a session-owned name entry

Status: accepted

Date: 2026-09-04

## Context

`F7` maps to `UserIntent.CreateDirectory` but nothing handled it. Creating a directory needs untrusted name text from the user, a request the gateway can validate and execute, and a provider step. The keyboard model gives a modal only its documented keys (KBD-002), CS-022 keeps decisions out of the window, and ADV-015 requires the name to face the same segment rules as any parsed path.

## Decision

- `FileSystemPath.Child(name)` derives the direct child under the parsing rules and rejects separators, `.`, and `..` before parsing, so the result is never the location or anything outside it. It touches no filesystem.
- `CreateDirectoryRequest.Create(location, name)` freezes the location as the sole source and the child as `Target`; a name the domain rejects is `FileOperationRequestFailureKind.InvalidName`.
- `IFileOperationPort.CreateDirectoryAsync(location, target)` is the one new provider step. `FileOperationGateway` inspects the location, observes cancellation, calls the step once, and reports `FileOperationEffectKind.DirectoryCreated` with progress `1 / 1`.
- `DualPaneSession` enters `OperationAwaitingName(location)` on `CreateDirectory` when the active pane is listed. While pending, `Escape` returns to `Idle`, a `NameSubmission` starts the creation through the same `StartAsync` as every operation, and every other intent and navigation is frozen (ADV-014, ADV-016). A successful creation refreshes the active pane focusing the new directory through `PaneSession.RefreshFocusingAsync` and the passive pane normally.
- `UserIntent.SubmitName(text)` is the only intent that carries data. The window builds it from the name editor's text when the mapper reports `Confirm` while the editor is visible; the window never validates the text.
- The presentation exposes `NameEntryPresentation` (`Hidden` or `Active`) and reports `KeyboardContext.Modal` for both modal states. The window prefers that context over the focused text control so `Enter` and `Escape` reach the mapper while every other key reaches the editor.
- The Windows adapter revalidates the location identity, requires a directory that is not a reparse point, requires the target to be contained in the location (FS-008), rejects an existing target as `Conflict`, and then creates the directory.

## Rejected alternatives

- Validating the name in the window or the presentation: the domain owns segment rules; a second validator would drift (ADV-015).
- A text-entry keyboard context for the editor: `Enter` passes through in that context, so the confirmation would have to be handled by the window's own key logic (KBD-005).
- Deriving the target in the adapter from the name: the gateway would execute a request without a frozen target, and the containment check would depend on provider text handling.

## Consequences

- The name editor is a placeholder control beside the status line until the design handoff.
- Rename can reuse the name-entry state and the submission intent.
- The editor text is framework state; the session only sees it at submission.

## Migration and removal

`DualPanePresentation` gains a required `NameEntryPresentation`; `IFileOperationPort` gains a required member every implementation adds in the same change.

## Executable proof

`FileSystemPathChildTests`, `CreateDirectoryRequestTests`, `FileOperationGatewayTests` (creation, and the three ways it does not proceed), `DualPaneSessionTests` (freeze, escape, creation with focus, invalid name and provider rejection, no listing), `DualPanePresenterTests`, `WindowsLocalFileOperationAdapterTests`, and the canonical gate.
