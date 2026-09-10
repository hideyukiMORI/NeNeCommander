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

- **`CommanderSession` owns one address-editor state.** The state is closed with an optional
  captured side for a one-time file-list focus effect, editing one captured pane side and its
  original canonical location, or rejected with that same capture plus the submitted raw text and
  existing `PathParseFailureKind`. The native `TextBox` owns ordinary character composition while
  editing; Application does not mirror each keystroke. Rejected input is a rejection snapshot: its
  first presentation restores the exact submitted text without lossy repair, while later renders of
  that same state do not overwrite subsequent native edits.
- **Every entry route uses the same start command.** `Ctrl+L` targets the current active side.
  Native focus on either existing address control submits that exact side. `CommanderSession`
  admits the request only when no settings modal, file-operation modal or running operation, or
  pane read owns interaction. When the requested side is passive, the session first uses the
  existing `DualPaneSession` activation intent, then captures that side and its listed location.
  Re-entering the already active address through `Ctrl+L` keeps the current native text and requests
  Select All without creating a new editing state. Programmatic focus raised while rendering is
  recognized as the same state and does not submit a second start.
- **Address editing has its own canonical keyboard context.** The context owns `Enter` as confirm
  and `Escape` as cancel, and keeps `Ctrl+L` as the canonical focus-address command. Every printable
  key, IME event, and other editing chord passes through to the native editor. Generic `TextEntry`,
  modal, file-list, and navigation-surface mappings are not widened. A view-local key table is not
  introduced.
- **Submission parses exactly once in Application.** The host attaches the focused address text to
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
- **Leaving an address closes the same editor without reclaiming focus.** Native focus departure
  to a file list or another control discards the edit and restores the original canonical text,
  but it preserves the focus destination the user chose. Departure submits the exact editor-state
  instance that owned the old control; the session closes only when that instance is still current.
  If focus moves directly between the two addresses, the new address start may occur before or
  after the old address departure without the stale departure closing the new editor. Enter and
  Escape use the same internal close transition with an explicit file-list focus disposition;
  ordinary focus departure uses the no-focus disposition. No general focus engine is introduced.
- **Focus effects occur once per state transition.** Presentation carries the exact source editor
  state. The host compares it with the last rendered state: a new editing state focuses and selects
  the address, a new rejection focuses while preserving the raw text, and an Enter/Escape dismissal
  focuses the captured file list. A focus-departure close does not issue another focus request.
  Re-rendering the same state does not repeat those effects. This prevents read completion or an
  unrelated settings notification from erasing text or reclaiming focus from a newer modal.
- **The approved address controls remain the only visual surface.** The existing left and right
  `TextBox`, AutomationIds, `PlainTextEditorStyle`, and semantic resources remain in place. The
  change adds localized status text only. It adds no visual structure, style, token, history,
  completion, file launch, path parser, provider adapter, or I/O mechanism.

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
- Closing an editor from an unqualified `LostFocus` callback: event ordering between two address
  controls could let the old control close the newly opened editor.
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
accepted submission, and a stale focus-departure request cannot close a newer address editor.

Presentation tests prove the dedicated context owns `Enter`, `Escape`, and `Ctrl+L`, IME/printable
input passes through, `Ctrl+L` remains the canonical focus binding, and address projection retains raw
rejected text and the localized status key. Host rendering keeps an unchanged edit state from
overwriting text or repeating focus effects, and distinguishes Enter/Escape list-focus dismissal
from ordinary focus-departure closure. Actual WinUI mouse, tab, IME, focus, high-contrast, DPI, and
narrow-width behavior remains explicit release-environment evidence under Issue #94.

The change connects new untrusted input to the existing SEC-002 parser and read route without
changing either safety contract. Independent review confirmed that the parser, provider, mutation,
containment, and operation-identity contracts remain unchanged, so this Issue adds no separate deep
review or mutation run. Focused Application and Presentation tests, affected coverage, and
conformance run during implementation. The final candidate requires the normal Draft-to-Ready
canonical CI gate. Scheduled and release deep-review obligations remain unchanged.
