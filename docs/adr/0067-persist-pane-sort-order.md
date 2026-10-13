# ADR-0067: Persist each pane's accepted sort order through settings

Status: accepted

Date: 2026-10-13

Accepted for Issue #232 by the NeNe Commander design owner under hide's delegated
authority for the current continuation. This decision supersedes ADR-0053's
session-only sort lifetime and ADR-0041's version-2-only writer.

## Context

ADR-0053 deferred persistence until the size and modification-time keys existed.
ADR-0056 has supplied those keys. Restart still resets each pane to name ascending.
The existing complete settings document and ordered write queue already provide
the required storage, warnings and shutdown ownership. Ordering itself must stay
with `PaneReducer` and `EntryOrdering`.

## Decision

- Store the last accepted `PaneSortOrder` independently for `PaneSide.Left` and
  `PaneSide.Right`. An immutable `PaneSortPreferences` holds both orders inside
  `UserSettings`; its single factory requires both. Defaults remain name ascending.
  `PaneSortOrder.Create` constructs one typed key/direction pair, and `Toggle`
  uses that factory. No wire text enters the pane model.
- The composition root supplies the saved order to each pane before its first
  read. Bundle initial pane configuration so the `PaneSession` constructor does
  not exceed four parameters. Before any successful listing this is a startup
  value; afterwards `PaneState.SortOrder` is the only current-order owner.
  First listing, refresh and history all use the existing reducer projection.
  Failed, cancelled or abandoned initial reads do not consume the startup value.
- The existing idle dispatch handles the sort intent once through `DualPaneSession`.
  Only a sort intent that actually changes its original pane's order queues a
  settings revision. Compare that same side before and after dispatch; changing
  the active side is not a sort change. The single sort-intent-to-key mapping in
  `PaneReducer` can be shared internally to identify these intents. Command-palette
  execution reaches this same dispatch. Reads, startup, modal/frozen rejection,
  unchanged order and unlisted panes never initiate a sort save.
- `SettingsSession` records the accepted typed order through its existing complete
  document queue. Other settings and the opposite pane's order are retained from
  the current complete value under its lock. Color, hidden-default and bookmark
  writes preserve both sort preferences. No second queue, side file or direct
  adapter call is added. Settings-selector routing may move from `CommanderSession`
  to `SettingsSession` to keep each touched responsibility within CS-013.
- The pane change is immediate and does not wait for disk. As with existing settings
  choices, queued UI saves use the session-owned lifetime and `CancellationToken.None`;
  shutdown drains them. The queue observes defects and rejects writes after stop.
  Pending, success, cancellation and failure use the existing settings channel.
  Failure never rolls back pane order or silently repairs malformed settings.
- Schema version 3 writes the existing five root properties followed by
  `paneSortOrders`, an exact object with `left` and `right`. Each is an exact object
  with string properties `key` and `direction`. Keys are `name`, `extension`, `size`,
  `modified`; directions are `ascending`, `descending`, all ordinal and case-sensitive.
  The validator rejects missing, duplicate, unknown or wrongly typed fields and
  unknown choices as a complete-document rejection, without defaulting one bad pane.
  Existing failure kinds suffice: shape/type/unknown choice failures do not need
  a new UI warning or resource. The 65,536-byte UTF-8 limit still covers the whole
  serialized document before any effect. `SettingsDocumentSerializer` owns complete
  document encoding, extracted atomically from `WindowsLocalSettingsStore`;
  `SettingsSortOrderCodec` translates only the new typed order fields in both
  directions. They remain inside the existing settings adapter boundary and share
  exact-property validation rather than introducing a second settings parser.
  The store retains all I/O, identity and atomic-publish ownership.
- Valid schema 1 retains its preferences and an empty catalog; valid schema 2
  retains its preferences and complete catalog. Both acquire default sort orders
  in memory only. Reading does not rewrite either version. The next accepted
  settings, bookmark or sort change emits only version 3. Older binaries reject
  version 3 through their existing unknown-version rule; no downgrade writer exists.

The new root member is:

```json
"paneSortOrders": {
  "left": { "key": "name", "direction": "ascending" },
  "right": { "key": "modified", "direction": "descending" }
}
```

## Alternatives rejected

- A per-folder or provider-specific order: this feature preserves each pane's
  existing lifetime across navigation, rather than creating a location-preference store.
- A shared order for both panes: it would discard their current independence.
- Saving an intent's anticipated toggle: a frozen or rejected intent could be
  persisted even though the pane never changed.
- Comparing every awaited operation's before/after state: unrelated asynchronous
  completion could be mistaken for a new sort choice. Save admission is sort-specific.
- Saving only at shutdown, adding a setting checkbox, or a separate sort writer:
  these duplicate or change the established save-on-change and ownership contracts.
- Rebuilding a saved order by calling `Toggle` repeatedly: persisted construction
  must not depend on keyboard toggle semantics.

## Consequences and migration

This is automatic persistence of the existing four sort commands, including their
palette entries. No keyboard binding, visual layout, ordering algorithm, filesystem
provider, identity check or atomic-publish primitive changes. The settings warning
already explains that a failed save can restore an older value after restart.
`Ctrl+H` remains session-only and never becomes a persisted preference through this
change. Migrate every changed constructor/factory call site in the same PR, with no
compatibility overload. Update the glossary, README and project state to name the
version-3 lifetime without claiming live desktop or release proof.

## Executable proof and selected scope

Under ADR-0061, build affected projects and direct consumers, and select settings,
pane-session/order, dispatcher/palette and direct presentation/composition tests.
Prove all eight orders on either side, first-read restoration and failed-first-read
retry, navigation retention, side independence, modal/frozen/unlisted no-write,
palette saves, ordered overlapping changes, unrelated preference/catalog preservation,
failed-save non-rollback and queue shutdown. Prove exact version-3 serialization,
version-1/2 migration without a read-side write, complete malformed preservation,
duplicate/unknown/missing/type rejection at each new object, the unchanged byte
limit and owned-root atomic round trips. Run the affected ADV-012 settings threat
cases, formatting and local Commit conformance.

Focused evidence covers the changed schema and consumers. No new storage trust
mechanism, provider I/O or visual contract is introduced, so full/deep, mutation,
live provider and desktop tiers are not selected merely for this schema addition.
If implementation exposes a wider impact, record and select its necessary proof
before running it. Successful unchanged evidence is reused. Release proof remains
separate under Issue #94.
