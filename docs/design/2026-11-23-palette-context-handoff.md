# One captured pane context in the compact palette — Issue #526

Status: engineering handoff for hide's requested consolidation
Date: 2026-11-23

hide reiterated the narrower modal and repeated target-pane wording on November23,
after accepting language save/restart and the revised theme picker. This refines
Direction B and the existing Issue525 compact handoff: retain its640-DIP cap,
24-DIP outer inset, top alignment and wrapped key hints. No new design system,
input owner or command execution path is introduced.

The existing prominent context strip names the pane captured when the palette
opens and its opposite once. It describes the palette context, not a claim that
every command acts on that pane. Remove the repeated target line from each row;
retain title, unavailable marker/reason, shortcut and full-title tooltip.

Ordinary pane commands have no redundant selected-row scope detail. Transfers
name their actual source and destination, switching panes names the destination,
and settings/theme/help/window adjustment/bookmarks name their true scope.
Bookmarks describes the shared manager, not a newly frozen navigation target.
Unavailable reasons remain in the row, selected detail and accessible name.
Every row's accessible name includes the applicable scope for focused reading.
The common context stays correct with filtered, global-only and zero-row results.

CommandPalettePresenter/Context/Feedback own these projections. CommandPaletteSession
continues to capture both pane snapshots and validate the exact submitted state;
no dispatch, eligibility, confirmation or stale-owner rule changes.

Verification is focused on all catalog scopes in both directions, unavailable
states, filtering, query/selection ownership, three locale catalogs, App build,
and the actual short-height layout. Reuse unchanged session/key-map proof.
The old synthetic-Chinese320x300 clipping is not resolved merely by deleting a
line; measure the final three-locale composition before claiming it.
Native keyboard/IME/visible acceptance of this new layout remains separate from
hide's successful checks of the previous unmodified candidate.
