# ADR-0053: Sort each pane through one ordering projection

Status: accepted

Date: 2026-10-09

## Context

The project charter lists sorting in the MVP scope. Today `DirectoryListing.Create` fixes one
order, directories first and then names compared case-insensitively and then ordinally, and no
user command changes it. Issue #173 adds user-selected ordering by name or extension. The listing
is the provider's canonical result with identity, duplicate, and size checks; the pane state owns
focus and selection by identity; focus movement in `PaneReducer` and row reuse under ADR-0028
both depend on the order the user sees. Sorting therefore touches three owners and must not
create a second ordering or let the view decide.

## Decision

- `PaneSortOrder` is pane state. It holds one closed `SortKey` (`Name`, `Extension`) and one
  `SortDirection` (`Ascending`, `Descending`), defaults to name ascending, and is carried
  unchanged through reads, refresh, and history navigation of the same pane. It is not persisted
  in the settings document by this decision.
- `EntryOrdering.Apply` is the sole projection from a listing and a sort order to the visible
  sequence. Directories always precede files; within each group the key and direction decide;
  the extension is the text after the last dot, a name without a dot or with only a leading dot
  has no extension and sorts first ascending; ties fall back to the name comparison. Descending
  reverses only the key comparison.
- `DirectoryListing` keeps its canonical name order and is never re-sorted in place.
  `PaneState` holds the sort order and derives its visible set, `VisibleEntries`, from
  `EntryOrdering.Apply`, so the projected sequence has one exposure. Focus movement, half-page
  movement, focus recovery across a visibility or order change, and row projection read that
  sequence, not `Listing.Entries`; focused-entry lookup stays an identity lookup that no order
  affects.
- `UserIntent.SortByName` and `UserIntent.SortByExtension` toggle the order: the same key again
  reverses the direction, a different key starts ascending. Focus and selection keep their
  identities across the change. The intents are unavailable while a modal, an editor, a pending
  operation decision, or the window adjustment mode owns input.
- `Ctrl+F3` and `Ctrl+F4` map to the two intents from the file list and navigation surface, in
  commander-compatible positions; plain `F3` and `F4` stay unassigned. Both intents join the
  ADR-0047 command catalog. The pane status shows the current key and direction through the
  existing status presenter and localized resources.

## Rejected alternatives

- Sorting inside `DirectoryListing.Create` per request: it would re-read or rebuild the listing
  for a pure view change and couple the provider result to user preference.
- Sorting in the view or XAML code-behind: it would make focus movement and the visible order
  disagree and violate the single reducer path.
- Persisting the order in settings now: it adds a schema change before the size and date keys
  exist; a later Issue may add it through the ADR-0040 document.
- Natural numeric ordering and column-header clicks: out of this Issue's scope.

## Consequences

- Size and modification-time keys need entry metadata the read port does not yet carry; they are
  a separate Issue that extends `DirectoryEntry` and both adapters before adding keys here.
- The incremental projection must rebuild rows when the projected order changes, not only when
  the listing changes.
- `CommanderSession` stays within its 300-logical-line limit; if the new intents push it over,
  ADR-0051 scope owners absorb them.

## Migration and removal

No stored data changes. Removing a key requires removing its intent, bindings, catalog entry,
and tests together.

## Executable proof

Application tests prove the default order, both keys in both directions, directory precedence,
extension edge cases, tie stability, identity-preserved focus and selection, order retention
across reads and history, toggle semantics, and unavailability under modal states. Mapper tests
prove the `Ctrl+F3`/`Ctrl+F4` context matrix and that plain `F3`/`F4` pass through. Presentation
tests prove the status text and the re-projection. Coverage and mutation thresholds are unchanged.
