# ADR-0068: Open existing interaction scopes from the command palette

Status: accepted

Date: 2026-10-13

Accepted for Issue #236 by the NeNe Commander design owner under hide's delegated authority.

## Context

The command palette already lists twenty implemented actions, including settings and Locations.
The bookmark manager and window adjustment mode have direct keyboard bindings and complete
session routes, but cannot be found in command search. Their omission is a bounded follow-up
explicitly left by ADR-0047 and ADR-0041, rather than a missing operation implementation.

## Decision

- Append `OpenBookmarks`, then `OpenWindowAdjustment`, to the existing twenty catalog entries.
  Preserve every earlier entry and its order, including `OpenLocations`. The resulting twenty-two
  parameterless intents remain the sole searchable set owned by `CommandCatalog`.
- This supersedes ADR-0047's exclusion of these two entry commands and ADR-0041's exclusion and
  rejection proof for `OpenBookmarks`, and completes ADR-0050's deferred window-entry addition.
  The nine bookmark slots, parameter-bearing submissions,
  individual window actions and recursive palette opening remain outside the catalog. Direct
  `OpenBookmarks` or `OpenWindowAdjustment` input while the palette is open remains ignored.
- Both entries are available after normal palette admission, including an empty listed active
  pane. They need no focus item or listed passive pane. The palette still cannot open on an
  unlisted active pane; direct `Ctrl+B` and `Ctrl+W` retain their existing admission rules.
- `CommandPaletteSession` validates the expected open state and both captured endpoints, then
  closes before `CommanderSession` dispatches the accepted intent once through its existing idle
  route. That route opens the existing settings bookmark editor or window scope. Opening either
  performs no file operation, settings write, pane navigation or window placement change.
- Focus and keyboard context follow the opened owner. The palette adds no old-side focus request.
  Cancelling bookmarks and leaving window adjustment retain their existing behavior. A stale or
  replayed palette submission cannot reopen a scope. Existing Enter-repeat protection remains
  in force across the owner transition.
- Use `CommandLabelCatalog`, the existing bookmark label, a localized
  `IntentLabelOpenWindowAdjustment`, and shortcuts from `KeyboardIntentMapper.BindingsFor`.
  Titles are `Bookmarks` / `ブックマーク` and `Window adjustment` / `ウィンドウ調整` with
  `Ctrl+B` / `Ctrl+W`. The existing palette projection owns search, UIA identity and row rendering.

## Alternatives rejected

- Dispatching a bookmark or window action directly from the presenter would duplicate execution
  and bypass captured-state validation.
- Adding every direct shortcut would expose bookmark-slot navigation and other actions whose
  membership needs a separate product decision.
- Reordering existing entries would change the empty-query selection contract without helping
  the two new entry commands.

## Consequences and migration

Update the catalog, shared label correspondence, both resource languages and exact catalog tests
atomically. Replace only the obsolete qualified-submission rejection expectations; preserve direct
input freeze and slot rejection. No new mechanism, key binding, dependency, settings schema,
provider policy, host route, geometry rule, XAML layout or design token is introduced.

## Executable proof

Focused Application tests cover stable membership/order, empty-pane availability, both active sides,
transition to the existing owner, unchanged pane snapshots and no unintended effects, direct-input
freeze, excluded slots, stale/replayed submissions and changed endpoints. Presentation tests cover
the added rows, localized title/shortcut search, UIA identity, selection boundaries and Enter-repeat
protection across scope contexts. Resource checks cover both existing language files.

Under ADR-0061 this isolated catalog extension needs the affected Release build, those focused
tests, formatting, text/link review and local Commit checks. It changes no security-sensitive
dispatch or provider boundary, so full/deep, coverage and mutation tiers are not selected. Live
desktop focus and release confirmation remain unexecuted environmental proof under Issue #94.
