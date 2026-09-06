# ADR-0041: Manage categorized bookmarks through settings and the canonical navigation route

Status: accepted

Date: 2026-09-06

Accepted for Issue #99 on 2026-09-06 by the NeNe Commander design owner under hide's delegated
authority. This acceptance covers the behavior, storage, command, and navigation contracts. The
functional UI structure was reconciled with the Claude Design handoff and accepted by the design
owner; this does not claim that hide separately approved the generated screen or its visual details.

## Context

NeNe Commander can navigate only through the active pane's provider-neutral directory-read route.
It has no bookmark model, category model, bookmark command, or persisted bookmark schema. Users
currently have to navigate repeatedly from the visible filesystem hierarchy. Issue #99 adds a
small, bounded catalog of named locations grouped by category and makes those locations reachable
from a dedicated manager and nine fixed keyboard slots.

ADR-0022 and ADR-0040 already establish one strict settings document, one
`WindowsLocalSettingsStore`, one complete immutable `UserSettings`, and one session-owned ordered
write queue. A separate bookmark file or writer would duplicate schema, location, failure, atomic
publish, and shutdown ownership. A bookmark-specific filesystem reader or view-owned navigation
call would duplicate ADR-0010 and ADR-0012. The design therefore has to extend the existing closed
mechanisms without making bookmark metadata itself perform filesystem work.

The first slice does not implement arbitrary shortcut assignment, a command search, or window
movement and resizing. `Ctrl+P` remains available for the separately tracked command-search work.

## Decision

- **Bookmarks are part of the one complete settings value.** `UserSettings` gains one immutable
  bookmark catalog in addition to color scheme and launch hidden-item visibility. The existing
  `SettingsSession` remains the sole owner of the current complete value and its ordered write
  queue; `WindowsLocalSettingsStore` remains the sole production reader and writer. A bookmark
  save queues one whole `UserSettings` revision through that queue. Preference selection preserves
  the complete catalog, and a catalog save preserves both preferences. No bookmark repository,
  second JSON file, second write queue, or partial settings write is introduced.
- **Schema version 2 is strict and has one explicit version-1 migration.** A valid version-1
  document is read as its two preferences plus an empty bookmark catalog. It is not rewritten merely
  because it was read. The next ordinary settings or bookmark write emits version 2. Version 2 adds
  exactly `bookmarkCategories` and `bookmarks` to the existing three root properties. Nested
  objects also reject duplicate, missing, or unknown properties and wrong JSON kinds. Malformed
  input, an unknown version, a broken category reference, an invalid path, or a limit violation is
  rejected as one complete document and is not repaired automatically.
- **The catalog uses bounded typed values and no generated identifiers.** It contains at most 32
  user categories and 128 bookmarks. A user category name is nonempty, contains no control
  character, has no leading or trailing whitespace, and contains at most 64 UTF-16 code units.
  Category names are unique under `StringComparer.OrdinalIgnoreCase`, while the first accepted
  spelling is preserved for display and serialization. The reserved Uncategorized category is
  represented by a null bookmark category and therefore has a distinct reference identity from
  every user category string. Its name is localized only by Presentation; Application and Domain
  do not depend on either language resource. It cannot be renamed as or deleted as a user category.
  A bookmark display name follows the same
  whitespace and control-character rules, is at most 128 UTF-16 code units, and is unique within
  its category under `StringComparer.OrdinalIgnoreCase`. The stable operation key is the pair of
  category and display name; no ambient random or process-global identifier source is added.
  The catalog preserves category and bookmark insertion order. Add appends, rename and edit retain
  position, and deletion removes only the selected metadata; Presentation may group the preserved
  bookmark order without introducing a separately persisted sort setting.
- **A bookmark stores typed path data and an optional fixed slot.** Untrusted path text crosses the
  bookmark boundary's well-formed UTF-16 check and then the existing `FileSystemPath.Parse`
  boundary before it can enter a catalog. Category and bookmark names pass the same Unicode check.
  An unpaired surrogate is rejected as typed input so `Utf8JsonWriter` can never silently replace
  it with U+FFFD; valid non-BMP pairs are retained and round trip. The canonical typed path is
  retained and serialized; registering or editing it does not inspect the filesystem. The same
  path may intentionally appear under different bookmark names or categories. A shortcut slot is
  either absent or an integer from 1 through 9 and is unique across the complete catalog. A
  duplicate slot save is rejected with a localized explanation; it never silently swaps or clears
  another bookmark.
- **The serialized UTF-8 document is bounded before mutation.** The current 65,536-byte settings
  document limit applies to version 2 as encoded UTF-8, including multibyte names and paths. The
  adapter serializes the complete value and rejects an oversized result with the closed `TooLarge`
  write reason before directory creation, sibling temporary creation, or any other filesystem
  mutation. The rejection reports both effect states as not attempted/none. Existing ancestor,
  document, temporary-file, atomic publish, cancellation, and residual path-reopen contracts from
  ADR-0040 remain unchanged.
- **One modal owner coordinates preferences and bookmarks.** `Ctrl+B` opens a dedicated Bookmarks
  modal rather than placing bookmark management inside the Settings modal. The session exposes one
  closed modal ownership state so Settings and Bookmarks cannot be open together. A running file
  operation, confirmation, name entry, conflict decision, preference modal, or bookmark modal keeps
  its existing input precedence and rejects opening the other surface or using a `Ctrl+1` through
  `Ctrl+9` direct-navigation slot. The bookmark manager owns browsing, an explicit edit draft,
  selected-entry navigation, and category-deletion
  confirmation as a closed state set. Name, path, category, and slot changes affect current session
  settings only on explicit Save; Cancel discards the draft. Persistence Pending, Succeeded,
  startup rejection, and save failure remain the ADR-0040 channel, and the same persistent warning
  is visible in the bookmark manager without becoming operation progress.
- **Stale manager actions fail closed.** Selecting a bookmark captures both its key and its complete
  immutable entry. Save, delete, move, and manager navigation submit that selection to the session
  owner, which requires the current catalog still to contain the same key with the same
  provider-defined path identity, category, name, and slot. A stale key that now names a different
  path identity is not silently rebound. Windows-local and UNC casing follows their existing
  identity comparer, while WSL Linux path-component casing remains significant.
  A stale selection is rejected without filesystem I/O or metadata change, and the registered
  catalog and pane listing remain intact. Selecting a category captures its name and the complete
  immutable, ordered set of entries that referenced it. Rename and deletion require both the
  current category name and that captured entry set to match before producing one complete
  replacement catalog. A concurrent category, bookmark name, path, category, or slot change makes
  the selection stale and rejects the whole mutation without changing current or persisted state.
  After a manager navigation failure, Retry accepts only a reprojection of the retained complete
  selection that still matches the current catalog. A different current bookmark supplied while
  the failure body is visible is rejected without another read or metadata write.
- **Category deletion is one referentially complete metadata mutation.** The confirmation names the
  affected bookmark count and the Uncategorized destination and initially focuses Cancel. Confirm
  removes the user category and moves every referenced bookmark to the null Uncategorized category
  in one immutable settings revision. If the move would collide with an existing Uncategorized
  bookmark name, the complete change is rejected and the manager explains that the entries must be
  renamed or reorganized first. It never overwrites, deletes, or automatically renames a bookmark.
  It also never deletes, moves, creates, or probes a filesystem entry. Category rename and bookmark
  category moves likewise replace every affected reference in one revision; a target-category name
  collision rejects the whole change, and partial reference updates cannot become current settings
  or persisted JSON.
- **Bookmark navigation reuses the active pane's one read path.** The manager emits a typed current
  bookmark selection and the nine shortcuts emit only a closed slot value. `CommanderSession`
  resolves either input against its current immutable catalog. A manager selection must pass the
  stale-entry comparison; a direct slot is intentionally resolved from the catalog current at the
  key press. Only then does the existing command route deliver the stored `FileSystemPath` to the
  active `PaneSession.NavigateAsync`, which reads through `IDirectoryReadPort`. There is no bookmark
  navigation engine, direct adapter access, or code-behind path open. A missing, unreachable, or
  unsupported provider produces the existing typed navigation failure, keeps the prior listing,
  and never removes or edits the bookmark. Manager navigation closes the modal only after a
  successful read; failure or cancellation keeps the modal, selection, catalog, and prior pane
  listing visible with a reason and retry action. While that read is pending, duplicate activation
  and every other modal command are rejected. An unassigned slot performs no filesystem I/O.
- **Keyboard bindings are finite and canonical.** `KeyboardIntentMapper` declares `Ctrl+B` and nine
  separate `Ctrl+1` through `Ctrl+9` bindings in each approved navigation context. Their intents
  carry only Open Bookmarks or one of nine closed slot identities. `KeyboardInputTranslator`
  translates the corresponding Windows key data, including Control-modified raw virtual-key forms,
  into the same closed `KeyboardKey` values. The displayed labels come from those canonical
  bindings and localized key-label resources. Arbitrary user key maps, dynamic bindings, and a
  second view-local shortcut table are excluded. `Ctrl+P` is not bound by this change.
- **The manager follows the approved shell and semantic resources.** It remains a distinct overlay
  over the existing two-pane shell, does not add a second operation bar, and does not place all ten
  bookmark bindings in the always-visible operation hint strip. Assigned slot labels are shown in
  the manager from the canonical binding data. The accepted Claude Design handoff is
  [NeNe Commander Bookmark Manager](https://claude.ai/design/p/ff811404-8e96-4dd0-92c1-320b3002b4b9?file=NeNe+Commander+Bookmark+Manager.dc.html):
  search above a two-column category/bookmark browser, fixed persistence status outside the lists,
  explicit form footers, and a dedicated category-deletion confirmation body. It uses existing
  semantic resources; generated numeric colors, fonts, spacing, and unapproved token names are not
  adopted. The Canvas demonstrated Retry and category-deletion Cancel focus rings only. Native
  initial focus, keyboard behavior, accessibility, high contrast, DPI, and narrow-width evidence
  must come from WinUI checks and the remaining Issue #94 environmental tier.
- **Filtering and registration defaults are closed session projections.** The manager places
  case-insensitive substring search above a two-column category and bookmark list. Search matches
  bookmark name, canonical path text, and the localized/displayed category name while preserving
  registration order. The category filter is a closed All, Uncategorized, or User Category value;
  localized labels are never used as keys. Register Current Folder starts from the active pane's
  current path. It prefills the leaf as display name only when the complete leaf already satisfies
  the bookmark-name contract, otherwise it leaves the name empty without truncation. The draft
  category is the current user-category or Uncategorized filter, while All defaults to
  Uncategorized, and the slot starts unassigned.
  An empty catalog shows localized registration guidance. A nonempty catalog whose active search
  and category filter return no rows shows a separate localized no-results explanation. Neither
  state invents a selection; registration remains available while move, edit, and delete remain
  disabled.

The concrete version-2 JSON shape is:

```json
{
  "schemaVersion": 2,
  "showHiddenItems": false,
  "colorScheme": "nene-dark",
  "bookmarkCategories": ["Work"],
  "bookmarks": [
    {
      "name": "Repository",
      "path": "C:\\work\\NeNeCommander",
      "category": "Work",
      "shortcutSlot": 1
    }
  ]
}
```

`category` and `shortcutSlot` are present in every version-2 bookmark object and may be JSON null.
The fixed property presence keeps omission distinct from malformed input. The validator resolves a
non-null category through the catalog's case-insensitive uniqueness rule and the serializer writes
the preserved category spelling. Invalid raw UTF-8 and escaped unpaired surrogates are malformed
documents rather than replacement-character input.

## Rejected alternatives

- A bookmark file or database beside `settings.json`: it would create a second persistence
  location, writer, atomic-update policy, warning channel, and shutdown owner.
- Persisting a localized `Uncategorized` string: localization changes or a user category with that
  spelling would change referential meaning. Null is the stable schema value.
- GUID bookmark and category identifiers: this first slice has serialized, single-session edits
  and can identify an entry with its category/name pair plus an immutable stale-entry comparison.
  Adding generated identifiers would require a new ambient randomness or identifier boundary
  without solving a current invariant.
- User-defined shortcut text or a dynamic key map: it would make KBD-005 collision analysis and
  localized hint truth depend on mutable settings. Nine closed static slot commands meet the direct
  navigation requirement.
- Silently swapping duplicate slots: it would turn one explicit bookmark draft into an implicit
  edit of another bookmark. The manager rejects the save and identifies the collision.
- Deleting every bookmark in a deleted category: category organization is metadata and does not
  imply the user chose to discard the registrations. The one mutation moves them to Uncategorized.
- Rejecting nonempty category deletion: it is safe but makes routine organization require many
  separate edits and provides no stronger filesystem protection than the adopted metadata move.
- Checking path existence at registration or dropping an unavailable bookmark: availability is a
  read-time provider fact, and transient failure must not rewrite the user's catalog.
- Navigating directly from XAML code-behind or adding a bookmark filesystem service: either would
  duplicate the command route or `IDirectoryReadPort`.
- Showing ten additional commands in the permanent operation hint strip: the fixed shell already
  has a bounded hint row, while slot labels belong with the catalog entries they identify.

## Consequences

A valid version-1 installation keeps its preferences and starts with no bookmarks. Once any value
is saved, the complete document becomes version 2 and an older application that understands only
version 1 will reject it as an unknown version. This is deliberate fail-closed behavior; no
dual-version writer is retained.

Bookmark edits have draft semantics while preference choices keep ADR-0040 save-on-change
semantics. Both commit through the same ordered queue after acceptance. A save that cannot persist
still updates the current-session catalog and exposes the existing persistent warning, so the
manager must say that restart may restore an older catalog. Cancel before Save has no settings
revision and no write. Bookmark paths remain untrusted data until the existing navigation boundary
reads them.

The catalog is small enough to present and validate as one immutable value. The chosen name limits,
entry limits, and encoded document limit bound parsing, allocation, rendering input, shortcut
lookup, and the atomic write. The final UI may refine layout and wording but may not change modal
precedence, Save/Cancel meaning, stale-selection rejection, deletion-to-Uncategorized, static slot
semantics, or the one navigation and persistence routes without revising this ADR.

## Migration and removal

The version-1 validator becomes a version dispatcher. Its exact three-property contract remains the
only accepted version-1 form and maps to an empty catalog. The serializer writes only version 2.
All old settings tests remain as migration and hostile-input regression proof; no permissive legacy
parser, registry fallback, repair write, or compatibility file is added.

ADR-0040 remains authoritative for atomic write ownership, identity checks, filesystem effects,
cancellation, ordered queue shutdown, and persistence warnings. ADR-0010, ADR-0012, and ADR-0013
remain authoritative for provider reads, active-pane navigation, and pane ownership. Issue #100
tracks window movement and resizing shortcuts; Issue #101 tracks `Ctrl+P` command search. Neither
feature is implemented here.

## Executable proof

Application and domain-facing tests prove exact and over-limit names and counts; whitespace and
control-character rejection; case-insensitive category and same-category bookmark-name uniqueness;
duplicate-path acceptance; null, 1, and 9 slot acceptance; 0, 10, and global duplicate-slot
rejection; defensive collection copies; one-revision rename, move, and delete-to-Uncategorized;
unchanged preferences across catalog writes; unchanged catalog across preference writes; explicit
Save and Cancel; stale entry and stale category rejection; and ordered mixed preference/catalog
writes whose older completion cannot roll back current state.

Infrastructure tests prove exact version-1 migration without a read-time write; strict version-2
root and nested property sets; duplicate, missing, unknown, malformed, and wrong-kind rejection;
broken category, invalid provider-path, invalid raw UTF-8, and escaped unpaired-surrogate rejection;
canonical Windows-local, UNC, and WSL path round trips for ASCII, multibyte BMP, and valid non-BMP
text; and UTF-8 serialization immediately at and one byte beyond 65,536. The oversized write
proof observes no directory or temporary creation and no reported filesystem effect. Existing
ADR-0040 ancestor, document, temporary, cancellation, retry, and atomic-publish adversarial tests
remain in force.

Command and presentation tests prove `Ctrl+B` and every `Ctrl+1` through `Ctrl+9` mapping in the
approved contexts; produced and raw-key translation where the Windows runtime requires it; no
`Ctrl+P` binding; no file read for an unassigned slot; current-catalog slot resolution; immutable
manager-selection comparison; active-pane-only navigation; old listing and registration retention
after typed read failure; normalized failure and cancellation reasons; retained-selection-only Retry;
empty-catalog and no-results guidance; modal and operation precedence; Cancel initial focus for category deletion;
canonical localized slot labels; warning independence from operation progress; and deterministic
projection for browse, draft, confirmation, pending, and failed states. Source and resource checks keep
the two locale resource sets aligned and retain the named AutomationIds. The approved Claude Design
artifact shows the adopted structure but does not prove native WinUI focus, Enter/Space/Escape behavior,
high contrast, DPI, or narrow-width rendering. Those real-window checks remain separately tracked under
the release environmental tier in Issue #94.

Because versioned untrusted JSON, persisted paths, command routing, and filesystem-mutation
preflight all change, the exact final head requires the security deep-review workflow in addition to
the one Draft-to-Ready canonical CI gate.
