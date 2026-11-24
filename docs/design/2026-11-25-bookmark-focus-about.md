# Bookmark focus and About handoff — Issue #581

Status: accepted direction; paused with residual heading-accent motion unresolved

Date: 2026-11-25 (request and design acceptance: November 24 JST).

hide reports an oversized white focus rectangle on the empty bookmark list and
a spring-like vertical movement when switching between category and bookmark
regions. hide also requests an About area containing the build version, author
and repository URL. After comparing the local canvas, hide explicitly selects
B, the heading band, together with About at the end of the existing Help.

## Design provenance

NeNe Commanderリナ used the actual `playground:playground` and
`frontend-design:frontend-design` skills to produce a self-contained local HTML:
`D:/NeNeCommander/outputs/bookmark-focus-20261124/design-consult/bookmark-focus-about.html`.
The available CLI `/design` command managed access rather than producing a design.
The later tool search exposed DesignSync, not a Canvas/Artifact sharing tool.
No cloud canvas, public hosting or share URL was created. The consult received
hide's supplied screenshot, requirements and public app identity, not repository
files. The local headless preview used a separate profile and no ordinary browser.

The canvas compares the old outline (A), the approved band (B) and a more subdued
heading marker (C), with region, empty/populated, narrow and simulated-contrast
controls. Its high-contrast image is a mock, not an OS contrast-theme observation.

## Implementation contract

- Keep the category/list hierarchy, search and canonical bookmark key map.
- The focused region's heading uses existing `FocusRingBrush` and
  `FocusSurfaceBrush`; inactive headings use `SurfacePaneBrush`.
- Reserve the heading's left 2 DIP and bottom 1 DIP from the start through
  `BorderBookmarkRegionHeaderThickness`. Focus changes paint only; they do not
  change padding, margins, widths, heights or transforms. The fixed heading
  padding is 4 DIP above and below through SpacingBookmarkRegionHeader.
- Keep the existing active/passive selected-row marks. Suppress the lists' and
  bookmark containers' large system focus outline and item-container transitions.
  Empty lists remain clearly identified by their heading; search retains its
  native caret/focus treatment and both headings become inactive. Its existing
  focused template resolves TextControlBorderBrushFocused to the same live
  FocusRingBrush and TextControlBorderThemeThicknessFocused to the existing
  1 DIP BorderNameEntryThickness, scoped to BookmarkSearch only.
- All key hints remain available and wrap naturally. Unlike the canvas's clipped
  demonstration band, implementation must preserve the November 19 wrapping
  contract. Reserve the maximum extent of the category/list/search hints using
  their same loaded content template and sole `KeyHintWrapPanel`.
- Only one hint band is interactive. Measurement copies contain no buttons,
  cannot receive input/focus and expose no accessible child tree. They are
  collapsed with the browse band in draft/confirmation substates.
- About stays after both Help columns inside the existing scroll region. It adds
  one localized heading and four lines: existing application name, exact
  SDK-generated informational version, existing assembly copyright and fixed
  selectable `https://github.com/hideyukiMORI/NeNeCommander`.
- `AppBuildIdentity` reads only attributes of the known executable assembly.
  This is deterministic build metadata, not assembly discovery, file I/O or a
  second version declaration. The release workflow still owns the csproj Version.
  No URL launcher, new modal, command, shortcut or signing claim is introduced.

Bookmark state, settings, navigation/deletion, focus restoration and the Help
entry/close route remain owned by their existing mechanisms. This is an App
adapter change, not a new Application/provider contract. The quality record
separates automated control proof from physical input and human acceptance.
## November25 visual correction

hide's01:02:59 screenshot rejects the earlier cramped heading and thick horizontal
rule, and reports no accent while search is focused. The B direction remains
accepted; this is a requested refinement. The initial2DIP underline and zero
vertical padding are replaced by the values above. Search retains the SDK native
TextBox template, input/caret/IME/selection and focus transitions. Only its two
focused-state resource aliases are scoped; no parallel focus listener, border
wrapper or input route is introduced. The pinned WinUI generic.xaml declares
both native template keys and consumes them in its Focused state. Rendered
padding, focus state and invariant geometry are verified separately below.

At the closing instruction hide confirms stable outer geometry but reports
residual heading-accent jitter. The required focus treatment has no movement or
animation during region changes. Cause and correction are deferred until hide
resumes; geometry proof does not establish paint/compositor stability. Preserve
this unresolved requirement alongside the technically verified padding/search
correction; do not call the whole visual behavior accepted.
