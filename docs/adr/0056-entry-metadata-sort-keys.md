# ADR-0056: Carry provider entry metadata and sort by size and modification time

Status: accepted

Date: 2026-10-09

## Context

ADR-0053 sorts a pane by name or extension through `EntryOrdering` over the `PaneSortOrder` held
by `PaneState`, and defers size and modification-time keys until the read port carries that
metadata. `DirectoryEntry` holds path, name, kind, and visibility only. The shared Windows
enumeration already materializes a `FileSystemInfo` per entry, so length and last-write time are
available without a second filesystem call for both the Windows local and the WSL adapter
(ADR-0035). The charter forbids guessing provider facts, so a value the provider does not report
must stay distinguishable from a real value.

## Decision

- **`DirectoryEntry` gains one `EntryMetadata` value.** `EntryMetadata` holds `EntrySize`
  (`KnownEntrySize` with a non-negative byte count from `EntrySize.Create`, which rejects a
  negative count, or `EntrySize.Unknown`) and `EntryTimestamp` (`KnownEntryTimestamp` with a UTC
  `DateTimeOffset` from `EntryTimestamp.Create`, which rejects a non-zero offset, or
  `EntryTimestamp.Unknown`), both closed records. `EntryMetadata.Create` is its one construction
  path and `EntryMetadata.Unknown` reports neither fact. `DirectoryEntry.Create` requires it. A
  directory's size is `Unknown`; a value the adapter cannot read for one entry is `Unknown` and the
  entry stays in the listing. No sentinel such as zero or the minimum time stands for absence.
- **The adapters report, Application never infers.** `WindowsDirectoryEntrySnapshot` carries one
  `EntryMetadata` that `WindowsDirectoryEnumerator` builds from the enumerated `FileSystemInfo`:
  `FileInfo.Length` for a file and `LastWriteTimeUtc` for every entry. Only `IOException` and
  `UnauthorizedAccessException` raised while reading one entry's fact become `Unknown` for that
  fact; every other exception keeps travelling to the shared operation's existing failure path. The
  FILETIME origin the platform reports for an absent time is `Unknown`, not a 1601 timestamp. The
  WSL adapter travels the same shared operation and therefore the same snapshot. Application code
  reads `Metadata` and performs no filesystem access. Time is carried in UTC; any local-time
  rendering is a later Presentation decision.
- **Two keys join the single ordering projection.** `SortKey.Size` and `SortKey.Modified` are
  added to the closed key set and compared inside `EntryOrdering`. Directories still precede
  files. `Unknown` sorts after every `Known` value in ascending order and before them in
  descending order, and ties fall back to the ADR-0053 name comparison, so a directory group under
  the size key is in name order. `UserIntent.SortBySize` and `UserIntent.SortByModified` toggle
  exactly as the ADR-0053 intents do.
- **Keys and catalog.** `Ctrl+F5` and `Ctrl+F6` map to the two intents from the file list and
  navigation surface in commander-compatible positions; plain `F5` and `F6` remain copy and move,
  and the existing repeat and context guards are unchanged. Both intents join the ADR-0047 command
  catalog, and the pane status shows `size` and `modified` through the existing sort status
  resources.
- **No column rendering here.** Rows keep showing the name and kind label. Showing size and time
  in the row changes the approved Direction C layout and goes through the design handoff first.

## Rejected alternatives

- Reading size and time lazily in the view: it adds filesystem access outside the port and
  breaks the deterministic projection.
- Representing absence with zero or `DateTimeOffset.MinValue`: it would sort fabricated values as
  facts and contradict the charter.
- A separate metadata port or a second enumeration pass: it doubles I/O and creates a second
  source of truth for the same entry.
- Adding columns in the same change: a visual change without a design pass.

## Consequences

- Every `DirectoryEntry.Create` call site, including tests and fixtures, supplies metadata; test
  call sites pass `EntryMetadata.Unknown` so existing behavior tests stay readable.
- Entries become larger by two small values; the 10,000-entry boundary is unchanged.
- `DirectoryEntry` value equality now includes the metadata, so the same path read again with a
  changed size or time is a different entry value; identity, duplicate detection, and focus
  recovery still compare paths and are unchanged.
- The adapter must tolerate a per-entry metadata failure without dropping the entry; such a
  failure becomes `Unknown` for that fact, the listing-level failure kinds are the existing ones,
  and no new failure vocabulary is introduced.

## Migration and removal

No stored data changes. ADR-0010 describes the read port and listing without enumerating the
fields of `DirectoryEntry`, so its text needs no change. Removing a key removes its `SortKey`
member, comparison, intent, bindings, catalog entry, status resource, and tests together; removing
the metadata reverts `DirectoryEntry` and the shared snapshot in one change.

## Executable proof

Application tests prove both keys in both directions, `Unknown` placement, directory precedence,
name tie-break, toggle semantics, that `EntrySize.Create` rejects negative sizes, and that
`EntryTimestamp.Create` rejects a non-zero offset. Infrastructure tests prove, on the owned
temporary root, that a file's length and last-write time reach the snapshot and the entry, that a
directory's size is `Unknown`, that only the two expected per-entry exceptions and the FILETIME
origin become `Unknown` while any other exception propagates, and that the WSL route carries the
same snapshot shape.
Mapper tests prove the `Ctrl+F5`/`Ctrl+F6` matrix and that plain `F5`/`F6` behavior, including the
repeat guard, is unchanged. Thresholds are unchanged.
