# Nib-style theme picker revision — 2026-11-23

Status: accepted interaction revision; implementation and scoped technical proof complete;
native and visual acceptance pending. See [the proof record](../quality/2026-11-23-theme-picker-revision.md).

hide resumed work after reading the daily report and handoff, then explicitly selected
“Nib型：Enter／クリックで保存して閉じる（推奨）” in this session at about 10:31 JST.
The compared reference is Nib Ctrl+P -> :colorscheme (local 6fb3686), not its settings drawer.
Folio f7d673f keeps its picker open after applying; that behavior was not selected.

## Invariant and canonical mechanism

SettingsSession remains the sole accepted-value owner and ordered complete-value writer.
A theme picker is an owned settings-editor scope. Settings' theme button and the command
palette's colorscheme entry both enter that same scope through CommanderSession/UserIntent.
Presentation owns only query, candidate position and temporary visual preview. Existing
ColorSchemeChanged -> ColorSchemeResources.CopySchemeInPlace remains the sole visual apply route.
Preview never queues a write, edits UserSettings, rereads the filesystem or changes pane state.

Enter/click submits the currently owned candidate and closes the picker. The existing settings
queue preserves all other latest preferences and persistent warning behavior. Selecting the
already accepted theme closes without an unnecessary write. Escape/scrim cancel without a write
and restore the latest accepted theme. Delayed submissions/cancellations from a previous picker
are ignored by exact open-state identity. Returning from the settings entry restores that settings
surface and its theme button; entry through Ctrl+P returns to the captured file-list owner.

## Layout and keyboard

Remove the 129 native radio rows and nested theme ScrollViewer from settings. Retain one compact
current-theme button there. Use one centered theme palette with a fixed heading, native search,
accepted-theme/count text and fixed bottom instructions/Cancel. Only the virtualized ListView
scrolls. Reuse semantic brushes and existing spacing/width/row tokens; text reserves space away
from the vertical scrollbar. Long names expose full tooltips/UIA names. The accepted name stays
visible separately from candidate selection, even when filtered out. No extra icons/assets.

The existing command-palette mapper owns Up/Down, Enter, Escape and Tab for this palette.
Letters (including j/k), spaces, editing shortcuts and IME composition remain native search input.
Candidate movement/filtering previews only; no result means no selectable candidate and restores
the accepted theme. Repeated unchanged progress renders preserve query, candidate, row objects,
scroll and focus. Move selection/scroll only on an actual candidate/query change. Closing, app
shutdown and stale inputs cannot persist a preview. Shared desktop input/focus and blue-frame
computer-use remain prohibited; the currently running user App/settings are preserved.

## Selected verification scope

Application proof: entry from settings and palette, modal blocking, stale open-state rejection,
commit/cancel, same-theme no-write, latest complete settings preservation, save-failure warning.
Presentation proof: localized filtering, zero results, bounded movement, row ownership, accepted
versus preview, progress stability and palette command discoverability. Build impacted App/tests,
run targeted architecture/conformance and complexity checks for changed contracts. Use isolated
real-control fixtures for layout/preview/storage boundaries without input to the shared desktop.
Native keys/IME/speech and hide visual acceptance remain separate; no successful old proof is
rerun just for save/push/merge. Reevaluate full-gate need against the final actual impact.

The 129 catalog/import/contrast contracts remain ADR-0087. Its old radio-list/search contract and
the original 2026-11-23 design handoff are superseded only for the picker interaction/layout above.
Instant language change and measured reactivation/resize latency are separate pending work; this
revision does not claim either is fixed.
