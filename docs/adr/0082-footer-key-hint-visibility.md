# ADR-0082: Wrap the footer and persist its normal key-hint visibility

Status: accepted

Date: 2026-11-19

Accepted for Issue #454 under hide's delegated authority for the current continuation.
The real NeNe Commanderリナ reviewed the concrete layout and interaction proposal.
The [design handoff](../design/2026-11-19-footer-key-hints.md) records that consultation
and the remaining native evidence. This amends ADR-0023's fixed single-line footer,
ADR-0067's version-3 writer and ADR-0077's settings focus order.

## Decision

- Keep one OperationBarView and the existing KeyHintWrapPanel. Give the footer finite
  width and use the existing height token as MinHeight. Put operation information and
  normal key hints in independent rows. The information row is collapsed when empty.
  Each key and label stays one item; all declared hints wrap naturally, without a
  row cap, priority-based omission or new styling constants. Keep the hint block right aligned.
- The upper row wraps status/icon, name entry, progress, counts and transfer-result
  groups. Status text wraps without ellipsis. A result label and its count stay together.
  Measure children with the available finite width and ignore Collapsed children when
  placing or adding gaps. These changes also apply to the existing window-adjustment
  use of the same panel. The design does not promise constant height across operation
  states or a width smaller than an indivisible native input control.
- Add the closed KeyHintVisibility choice, Shown by default. It affects only the
  OperationKeyHints file-list projection. Hidden keeps the canonical OpenHelp/F1 hint.
  Modal Confirm/Escape hints remain, including confirmation and name entry. Locations,
  command palette, window-adjustment, help and Bookmark hints are unchanged. Commands
  and input mapping do not depend on the preference.
- Add a settings checkbox after the next-launch hidden-entry checkbox and before
  color schemes. Its localized label and accessible explanation explicitly say that
  F1 and confirmation/name-entry hints remain when normal hints are off. Movement
  remains focus-only; Space/Enter activates once through the existing Settings context.
- Route the typed selection through CommanderSession and SettingsSession. State and
  footer projection change immediately, and the existing ordered complete-document
  write queue owns persistence. Failed saves retain the session choice and the
  independent settings warning. Other settings, bookmarks and both sort orders are
  retained. Startup/read/focus/render never initiates a write.
- Bundle ColorScheme, HiddenItemVisibility and KeyHintVisibility in constructor-complete
  DisplayPreferences. UserSettings has one factory for DisplayPreferences, BookmarkCatalog
  and PaneSortPreferences. Migrate every caller atomically without an old overload,
  optional construction argument or second settings route.
- Write schema version 4, preserving version 3's property order and appending
  showKeyHints as a JSON boolean after paneSortOrders. The existing strict root
  validation rejects a missing, duplicate, unknown, wrongly cased or wrongly typed
  new property. Preserve the full UTF-8 size bound and whole-document rejection.
  The settings adapter retains its current path, identity checks and atomic writer.
- Read valid schema 1, 2 and 3 with hints Shown, preserving all their existing values.
  This is an in-memory migration only. The next accepted change writes version 4.
  An older binary rejects version 4 as unknown, opens with its existing warning/default
  behavior and does not overwrite that rejected document. No downgrade writer is added.

## Alternatives rejected

A session-only switch loses the requested preference after restart. An optional field in
version 3 would change its exact-shape contract. A side file or view-owned switch duplicates
settings ownership. Hiding safety instructions or F1 removes essential guidance. Two-row
clipping silently removes shortcuts. A second wrap panel duplicates the existing mechanism.

## Proof and limits

Under ADR-0061, select complete-settings construction, selection/admission, ordered writes,
failure/no-rollback and cross-setting preservation; schema 1–4 migration, strict ADV-012
shape/type rejection, atomic owned-root round trips and size bounds; footer/key/help and
settings focus projection; App build and composed-view ownership. Include affected uses of
the shared wrap panel in the native review. Reuse successful evidence for unchanged inputs.

No provider, dependency, filesystem trust mechanism or queue primitive changes. These focused
cases establish the changed persistence and UI contracts; full/deep CI, all-layer mutation
and unrelated provider suites are not selected for this implementation alone. A requested
Preview retains its separate applicable deep-review and release requirements. Record native Japanese/English, narrow,
100%/200% and high-contrast observations separately. Source or mapper tests do not establish
native focus, rendering or Narrator. PR402's separate acceptance remains pending.
