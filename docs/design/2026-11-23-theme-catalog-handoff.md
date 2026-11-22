# Theme catalog and searchable settings handoff

Issue #527 / ADR-0087 / 2026-11-23.
Status: accepted engineering adoption after the00:16 JST independent review;
implementation and native screen acceptance remain separate.

## Provenance and adoption

The exact3,851-byte brief approved by hide was sent on November22; the consultation
completed at23:24:56 JST. The retained advice supplies the current-theme line,
search/count, bounded single-column choice region, explicit selection and pinned
footer. This is ordinary design consultation, not a /design-generated deliverable.
The installed /design command did not generate a design. The source palette audit
and every experiment are linked from the [quality record](../quality/2026-11-22-theme-catalog-implementation.md).

Adopt that hierarchy using existing native controls, token families and settings
ownership. Engineering changes its proposed contrast repair order, removes its
unnecessary lowered targets, and preserves the existing focus-only key contract.
Do not add the suggested new system-color owner, extra paging shortcuts, automatic
preview, new assets or another application modal without a separate decision.

## Components and layout

The existing SettingsOverlay remains the modal surface. Apply the existing window
padding6 DIPs and a new DensitySettingsModalMaxWidth of520 DIPs. Do not force a
360-DIP minimum onto a smaller window. The modal is centered within the available
content, with content measured against that space rather than an unbounded desktop.
Use the existing SurfacePane/FocusRing brushes,1-DIP operation border, RadiusPane3,
13-DIP body typography and existing6/10/14-DIP spacing tokens.

The modal contains a scrolling body and a separate footer row. The body retains
its title, save explanation, two checkboxes and four language choices, followed by:

1. Theme heading and a wrapped current-theme line with full name and Light/Dark.
2. A labeled native TextBox with a localized search placeholder.
3. A wrapped filtered/total count, or a localized no-results message.
4. The bounded single-column theme radio choices, using a ScrollViewer and the
   existing radio template rather than selection-follows-focus RadioButtons.

The new DensitySettingsSchemeRowMinHeight is32 DIPs, and
DensitySettingsSchemeListMaxHeight is256 DIPs (eight minimum-height rows).
Do not fix the actual row height: native text and DPI measurement may grow it.
When the window cannot fit the natural body, the body scrolls and the footer stays
outside it. Three rows are a preferred small-window view when available; no forced
minimum may push Close or content outside the viewport. Both scroll regions must
bring the focused choice into view. Test the actual layout at320x300 and640x360
DIPs and representative100–300% scaling; prototype dimensions are not acceptance.

The footer retains save status and Close. Existing persistent save warnings remain
visible through their existing warning surface, with full wrapping and no new
validation state. Close stays the initial focus. Search/count/current-theme text
is not a focus stop. Only the search field itself is interactive there.

## Choice rendering and accessibility

Order the four Commander-only choices first (nene-dark, ubuntu, nene-black,
nene-light), followed by the125 fixed shared identifiers in Nib order. Preserve
all eight existing stored identifiers, names and default behavior. Shared proper
names may retain their source identifier as a display name, through the same
three localization catalogs; label/filter/count/appearance text is localized.

Each choice has its native radio marker, full display name, and a visible localized
Light/Dark text label. Long names may ellipsize, but their tooltip and accessible
name retain the complete name and appearance. The accessible checked state means
applied, not focus. Stable automation identifiers use SettingsScheme_ plus the
closed identifier; the existing SettingsSchemeOptions region remains. New regions
are SettingsSchemeSearch, SettingsCurrentScheme and SettingsSchemeResults.
Search has a programmatic label. Result count uses a polite live region without
introducing a timer or automatic per-row speech. Preserve the existing save status
and warning semantics; actual speech remains an environmental check.

## Interaction and ownership

SettingsSession and its complete-value queue remain the sole selection/persistence
mechanism. Presentation owns only the transient query, localized candidates and
current display projection. Catalog/filtered rows own their storage; a read-only
wrapper must not disclose a mutable synchronization-root collection. No search
query enters the settings document or changes pane state.

Search matches name and identifier with ordinal-ignore-case substring matching,
preserving catalog order. Opening/closing resets the query. Progress-only settings
renders retain the same native choice controls and query. Filtering does not save,
apply, preview, reorder, or move focus out of the search field. The current-theme
line remains unfiltered even when the applied choice is absent from results.
When a choice is applied, focus remains on that native control and rows are not
recreated just because saving progressed. Newly realized/filtered radio controls
must reflect the latest applied scheme, without writing during render.

Use the existing TextEntry mapper context while the search TextBox owns focus;
otherwise keep Settings. Include its composition state in the host's existing
IME guard. Search typing/cursor movement/Ctrl+A/Tab remain native. Composition
Enter/Escape stay with the IME; outside composition Enter does not apply a theme,
and Escape closes Settings through the existing intent. No second key map.

Outside search, j/k and Up/Down move focus only; Space/Enter explicitly select.
The theme list is one native Tab region (Once); its entry target is the applied
choice if present, otherwise the first result. Tab/Shift+Tab remain inside the
modal and can always reach Close. Zero results remove the empty list from the
focus order. Existing language-radio and checkbox semantics remain. Do not add
Home/End/PageUp/PageDown bindings just because the advice suggested them.

## Palette and indicator contract

Import the fixed current Nib125 with the corrected light vscode-high-contrast-light.
Retain exact source SurfacePane/background and TextPrimary/foreground. Keep the
four Commander-only palette files byte-identical, retain18 semantic colors and
18 brushes in every dictionary, and report the four overlapping schemes' visible
alignment changes. Preserve existing brush instances through CopySchemeInPlace.

ADR-0087 defines the final deterministic encoded-sRGB/half-up interpolation and
neutral-anchor repair, with all fills settled before text/mark repair. All125x63
required static pairs pass without reducing4.5 text or3 non-text targets.
FocusRing includes Bookmark shortcut text and therefore receives the text target.
The theme name high-contrast never means Windows contrast mode.

Two minimal indicator adaptations preserve command and selection semantics:

- Passive focus's existing left marker uses TextSecondaryBrush on SurfacePane,
  retaining precedence and geometry. BorderSubtle remains the passive pane-number
  surface, where secondary text remains readable.
- Progress squares remain8 DIPs. Add an OperationProgressBrush outline with the
  existing DensityIconStrokeThickness1.5; a conservative5-DIP interior remains.
  Empty uses SurfacePaneBrush and filled uses OperationProgressBrush. This yields
  hollow versus filled, rather than relying on three different contrast pairs.
  OperationRunning alone shows these segments on Idle/SurfacePane. OperationTrack
  stays in the dictionary contract but is no longer painted by this template.

The first outline proposal kept OperationTrack and exposed the legacy nene-light
2.970249574275058 ratio. The revised actual-pair matrix passes all387 checks for
129 themes without editing legacy palettes or lowering thresholds. These shared
indicator changes are still visible even though the four palette files are equal.
The existing file-row color-only selection/focus limitations and actual Windows
contrast behavior remain explicitly unproved; do not claim complete accessibility
from static ratios, schematic images or successful XAML compilation.

## Artifacts and acceptance

No new raster or icon asset is needed. Retain the source manifest/commits/hashes,
palette author/license metadata and the exact71,389-character palette notice block.
The offline deterministic import/check mechanism and release notice integration
are defined in ADR-0087; no runtime download or dependency on another checkout.

Verify closed catalog ownership/parsing, stable source/dictionary/address/name
parity, all three localization catalogs, search/empty/current-selection behavior,
settings queue preservation and the directly changed markers/segments. Native
proof must cover mouse and keyboard selection, radio checked state after filtering,
IME, narrow/DPI layout, contrast mode, accessible names/speech and focus return in
an isolated desktop/session. Unchanged proofs are reused. This handoff does not
claim those results or mark either this feature or dependent language PR559 ready.
