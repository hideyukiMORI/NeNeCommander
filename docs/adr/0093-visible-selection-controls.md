# ADR-0093: Extend visible selection through the existing pane reducer

Status: accepted

Date: 2026-11-25

Issue: #562, following #530/ADR-0090. This change is excluded from the frozen beta.5 candidate.

## Context

The existing pointer route already supports single, toggled and inclusive range
selection. PaneState owns one selection anchor; PaneReducer alone changes focus,
selection and anchor. The requested Ctrl+A, keyboard ranges, inversion and explicit
clear must use those same visible entries and immutable state. Framework native
selection remains a projection and cannot choose file-operation targets.

## Chosen mechanism

Add closed UserIntent cases for SelectAll, InvertSelection, ClearSelection and six
range movements. CommanderSession and DualPaneSession retain input admission;
PaneSession dispatches to PaneReducer. PaneSelection only calculates values and
never owns or commits state. FileOperationGateway and provider contracts do not
change. Selection always contains the exact owned paths of current visible entries.

| Input or command | Result in the active file list |
|---|---|
| Ctrl+A | Select every currently visible entry; preserve focus; clear the anchor. |
| Produced `*` | Invert marks over currently visible entries; preserve focus; clear the anchor. |
| ClearSelection in the palette | Use the same idle selection/anchor clearing transition as Escape. |
| Shift+Up / Shift+Down | Move one row and replace selection with the inclusive anchor-to-target range. |
| Shift+PageUp / Shift+PageDown | Move the existing half-page distance and replace that range. |
| Shift+Home / Shift+End | Target the first/last visible entry and replace that range. |

All range movements clamp, including at the ends and when the half-page size is
one. They never cycle. Repeated range steps can expand, shrink and cross the anchor;
reaching an endpoint produces the same interval. One visible entry selects itself;
an empty visible set stays unchanged. Unmodified j/k/arrows still cycle, and existing
gg/G/page movement keeps its current meaning.

The exact anchor lifetime of ADR-0090 is retained for both pointer and keyboard:
plain/Ctrl click and Space establish it; a range keeps it; an absent anchor falls
back to the focus immediately before the first range and is then committed.
Ordinary movement and sorting preserve it. Hiding drops invisible marks and rebinds
or removes the anchor. Successful refresh clears marks and rebinds a surviving
anchor to the new listing's owned path; successful navigation clears both. No second
keyboard anchor, live flag or implicit input-modality state is introduced. All,
invert and explicit clear start a new selection operation and remove the old anchor.
The passive pane is unchanged.

The canonical KeyboardIntentMapper declares Ctrl+A, produced `*` and six explicit
Shift navigation bindings in FileList only. Letter A uses its produced `a`,
Control-qualified `A` or U+0001; raw virtual A remains a precursor. The existing
App translator reads Shift for F10 in every context, as before. It reads Shift for
the six navigation keys only in FileList, so Bookmark, WindowAdjustment, Help and
native text navigation retain their existing modifier behavior. The pure input
translator owns this context/key classification and is tested across every context.

A produced a/A/U+0001 in FileList also receives the explicit Shift distinction:
Ctrl+A selects all; Ctrl+Shift+A is Other and does not select or clear anything.
This prevents an undeclared clear-like chord from unexpectedly selecting every file.
Other contexts retain their previous produced-character modifiers and native text
ownership. Other letters/symbols, Control/Alt, uppercase G, IME and dead-key behavior
is preserved. Unmodified Home/End and Control+Shift navigation are not new bindings.
This explicitly extends ADR-0090's F10-only Shift distinction only for these new
FileList bindings. No new global modifier interpretation is introduced.

Invert is one action per physical press: repeated input is consumed even without
an earlier accepted press, and the existing repeat guard retains an accepted press
across context changes. Ctrl+A may also use the existing single-shot policy. The
existing text, native menu, modal, busy and shutdown precedence is unchanged. A new
declared key cancels a pending gg chord and is processed normally under the current
chord contract; it is not silently discarded. Text fields retain native Ctrl+A and
Shift editing. No selection command is queued behind other work.

SelectAll, InvertSelection and ClearSelection appear in the existing palette and
localized command labels. F1 reads the canonical binding table; the normal footer
is unchanged. Existing semantic selection/focus resources and pane counts remain
the visual representation. No new announcement timer, UI Automation selection
provider, platform boundary or resource family is added by this scope.

## Alternatives and design advice

Claude was invoked with a skill-capable restricted session and actually called
Skill(design), then searched for Canvas tools as hide requested. The available
/design only manages project access; discovery returned no dedicated Canvas tool.
No access grant/revoke, public artifact or repository-source export occurred.
Ordinary interaction advice and its limitations are retained in the task evidence.

Adopt clamped inclusive ranges, visible-only all/invert, native text ownership,
one-shot inversion, no extra footer items, and the existing refresh policy. Reject
a separate anchor-live flag: it would make the same shared anchor mean different
things for pointer and keyboard and add state without a current need. Reject the
advice to discard declared keys during gg because it contradicts the canonical
chord rule. Ctrl+I's produced control character overlaps Tab, so use the ordinary
produced `*` command without adding a raw-letter exception. Ctrl+Shift+A and extra
aliases are unnecessary for this initial scope; clear remains accessible through
the existing idle Escape and the palette. New selection announcements or native
provider changes require their own measured accessibility scope.

## Migration and proof

Update the keyboard/command/glossary contracts, closed intents, reducer calculation,
mapper/translator, repeat guard, palette labels and three resource catalogs together.
There is no settings migration, dependency or provider change. Existing pointer
range behavior is a consumer boundary and must remain proven.

Selected proof covers: forward/reverse/shrinking/cross-anchor and endpoint ranges;
empty/single/hidden/sorted/refreshed Windows and case-sensitive WSL identities;
immutable predecessor state and exact owned paths; all/invert/clear algebra;
active/passive pane independence; busy/read/launch/modal admission; selected batch
operation targets; text/IME/foreign modifiers/pending chord and held-key ownership;
palette candidates/localized hints and generated F1 labels. Build changed consumers,
run selected conformance/record closure and changed-file formatting, inspect changed
complexity, and use owned non-input-desktop native observations where necessary.
Physical input, screen-reader and broad screen acceptance remain distinct.

Selected behavior, closure and build proof are recorded in [the quality record](../quality/2026-11-25-selection-controls.md).
Physical input/visible acceptance, public PR, main integration and cleanup remain pending.
Preserve all failed attempts and exact commands/results.
