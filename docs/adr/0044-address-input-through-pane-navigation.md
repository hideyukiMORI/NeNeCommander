# ADR-0044: Route address input through the existing pane navigation path

Status: accepted

Date: 2026-09-08

Accepted for Issue #107 on 2026-09-08 by the NeNe Commander design owner under hide's delegated authority.

## Context

The shell already renders one address `TextBox` for each pane, and `Ctrl+L` already maps to
`UserIntent.FocusAddress`. The host currently forwards that intent into pane reduction, where it
is irrelevant, and an address editor has no submit path. Users therefore cannot navigate by
typing an absolute location even though `FileSystemPath.Parse`, `PaneSession.NavigateAsync`, and
`IDirectoryReadPort` already provide the sole validation and read mechanisms.

Address input crosses an untrusted-text boundary and temporarily owns keyboard focus. The feature
must preserve the current pane listing and selection when parsing, reading, or cancellation does
not produce a successful location change. It must also keep modal, operation, and read precedence,
and it must not let an asynchronous render erase native editor text or steal focus after another
surface has legitimately taken ownership.

Issue #99 changes some of the same session, input, and host files on a separate Draft branch. It is
blocked on an upstream Stryker MTP correction and is not an input to this decision. Issue #107
starts from main `0dab3f67c07fceafc77827e78f068a36c36e7003`; the two branches are reconciled only
after their respective canonical changes are ready.

## Decision

Revision accepted by hide on 2026-11-20 for Issue #495: the two pane paths are
permanent display-only `TextBlock` controls. `Ctrl+L` opens one explicit path-input
modal for the active pane. This replaces both native-focus entry and the interim
read-only inline `TextBox` direction. The existing state and navigation owners stay.

- **`AddressEditorSession` owns address-editor state, admission, and submission validation, and
  `CommanderSession` remains the sole owner of address navigation.** This ownership sentence is
  superseded by ADR-0051; every other sentence of this ADR stays in force.
  The state is closed with an optional
  captured side for a one-time file-list focus effect, editing one captured pane side and its
  original canonical location, or rejected with that same capture plus the submitted raw text and
  existing `PathParseFailureKind`. The native `TextBox` owns ordinary character composition while
  editing; Application does not mirror each keystroke. Rejected input is a rejection snapshot: its
  first presentation restores the exact submitted text without lossy repair, while later renders of
  that same state do not overwrite subsequent native edits.
- **Every entry route uses the same start command.** `Ctrl+L` targets the current active side.
  Native focus or a path-display click does not start an editor; only the explicit command does. `CommanderSession`
  admits the request only when no settings modal, file-operation modal or running operation, or
  pane read owns interaction. When the requested side is passive, the session first uses the
  existing `DualPaneSession` activation intent, then captures that side and its listed location.
  Re-entering the already active address through `Ctrl+L` keeps the current native text and requests
  Select All without creating a new editing state. The modal traps native Tab navigation and
  blocks pointer interaction with the panes while open.
- **Address editing has its own canonical keyboard context.** The context owns `Enter` as confirm
  and `Escape` as cancel, and keeps `Ctrl+L` as the canonical focus-address command. Every printable
  key, IME event, and other editing chord passes through to the native editor. Generic `TextEntry`,
  modal, file-list, and navigation-surface mappings are not widened. A view-local key table is not
  introduced.
- **Submission parses exactly once in Application.** The host attaches the modal editor text and exact rendered state to
  one `AddressSubmission` intent, as it already attaches name-entry text to a typed submission.
  `CommanderSession` passes the raw value once to `FileSystemPath.Parse`. A `PathParseSuccess`
  closes editing before starting the captured side's existing `DualPaneSession.NavigateAsync`
  route. No provider is selected from text in the host, and no filesystem API or second read path
  is added.
- **Invalid input remains editable and performs no I/O.** A `PathParseFailure` moves the editor to
  its rejected state, retaining the exact raw text and pane side. Presentation maps the existing
  closed reason to localized invalid-address wording, keeps editor focus without repeatedly
  selecting the text, and leaves both pane snapshots unchanged. The directory-read port receives
  no request.
- **Read outcomes keep the existing pane contract.** Accepted submission closes the editor and
  starts the existing read. Success alone replaces the listing and clears selection through
  `PaneReducer.Navigate`. Failure and cancellation retain the old listing and selection while
  `PaneReadFailed` or `PaneReadCancelled` supplies the existing status. The origin side captured
  when editing began cannot be retargeted by a later active-side change.
- **Escape cancels only address editing.** It closes the editor, restores the original canonical
  address text, and requests focus for the captured side's file list. It is consumed before the
  pane reducer, so it does not clear selection. Start and submit are rejected while modal,
  operation, or read work owns interaction.
- **Explicit dismissal closes the captured modal and restores list focus.** Escape and a scrim
  tap use the same expected-state-qualified cancellation. A stale cancellation cannot close a
  newer editor. A focus change inside the modal or temporary window deactivation does not discard
  text. There is no native `LostFocus` cancellation, per-pane text-control owner, or no-focus
  departure route. Cancellation, like accepted submission, requests the captured file list once.
- **Focus effects occur once per state transition.** Presentation carries the exact source editor
  state. A new editing state opens, focuses and selects the one input; a rejection restores the
  exact raw value and localized error inside the modal without selecting all. An unchanged state
  neither overwrites native text nor repeats focus effects. Closing collapses the modal and focuses
  the captured file list. IME composition remains native and cannot submit navigation prematurely.
- **One modal replaces inline editing.** `LeftAddress` and `RightAddress` remain the stable
  automation identifiers on display-only text. `AddressEditorOverlay`, `AddressEditorModal` and
  `AddressEditorInput` identify the sole editor. It reuses existing semantic surface, border,
  spacing, typography and key-hint resources. The title and input name are localized; Enter/Escape
  hints come from the existing canonical `AddressEntry` bindings. No history, completion,
  provider adapter, parser or second navigation mechanism is added.

## Rejected alternatives

- Parsing or branching on path validity in code-behind: validation and provider meaning would
  escape the Application and Domain boundaries.
- Calling `IDirectoryReadPort` from the view: it would create a second navigation path and bypass
  pane state ownership.
- Mapping `Enter` in generic `TextEntry`: it would change settings, bookmark, search, and future
  text controls that do not submit navigation.
- Continuously sending `TextChanged` values into Application: the native editor already owns IME
  composition and ordinary editing, and only rejected text must survive a snapshot render.
- Reusing pane selection `Escape`: it would clear selected files when the user only cancels address
  editing.
- Re-focusing controls after every render: late read or settings completion could steal focus from
  the surface that currently owns it.
- Keeping inline editors beside the explicit modal: that would duplicate entry and native focus
  ownership. Incidental focus changes must not decide whether a path edit exists.
- Adding a new navigation service or address-specific reader: the existing pane session and
  provider-neutral read port already express the complete operation.

## Consequences

Address editing becomes a small application-owned interaction state beside the settings modal and
dual-pane session. The view continues to own only framework text and focus mechanics. Invalid text
is visible and correctable without provider work; accepted input has the same provider behavior,
failure normalization, cancellation, stale-read handling, hidden-item visibility, and selection
rules as every existing pane navigation.

The new editor state must be reconciled with Issue #99's expanded `CommanderSession` modal owner
when that Draft resumes. That later integration must preserve one modal-precedence decision and one
address route; neither branch is rebased onto the other merely to avoid the current upstream tool
blocker.

## Migration and removal

Extend the current session snapshot, intent set, keyboard context, pane/address presentation, host
forwarding, localized resources, and their focused tests atomically. Remove the existing no-op
`FocusAddress` flow by routing it to the new state owner. No persisted data or dependency migration
exists.

## Executable proof

Application tests prove active and passive address start, existing activation routing, capture of
the origin side, rejection under settings/modal/operation/read ownership, exact successful routing,
invalid-input read count zero, and listing/selection preservation for parse failure, provider
failure, cancellation, and Escape. They also prove a later active-side change cannot retarget an
accepted submission, and a stale modal cancellation cannot close a newer address editor.

Presentation tests prove the dedicated context owns `Enter`, `Escape`, and `Ctrl+L`, IME/printable
input passes through, `Ctrl+L` remains the canonical focus binding, and address projection retains raw
rejected text and the localized status key. Host rendering keeps an unchanged edit state from
overwriting text or repeating focus effects, and restores captured list focus after explicit dismissal. Display-only paths cannot become editors;
Tab stays within the modal and native IME confirmation must not navigate. Actual WinUI mouse, tab, IME, focus, high-contrast, DPI, and
narrow-width behavior remains explicit release-environment evidence under Issue #94.

The change connects new untrusted input to the existing SEC-002 parser and read route without
changing either safety contract. Independent review confirmed that the parser, provider, mutation,
containment, and operation-identity contracts remain unchanged, so this Issue adds no separate deep
review or mutation run. Focused Application and Presentation tests, affected coverage, and
conformance run during implementation. The 2026-11-20 revision follows ADR-0061: selected address/cancellation/hint/ownership tests, App
build and native input proof; Ready starts no full CI. Release proof remains separately required.
