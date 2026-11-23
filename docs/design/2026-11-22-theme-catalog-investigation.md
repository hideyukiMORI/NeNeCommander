# Theme catalog alignment investigation

Issue #527 / 2026-11-22 / main7ef9b96.
Status: source audit and proposed integration constraints; no product/theme/UI change.

## Fixed authoritative inputs

- NeNe Nib5d76cc7125b1b3bc74a302cefb28ec67c0d314fd:
  src/core/BuiltinThemes.hpp owns125 names/order/appearance/source; the corresponding
  body colors are in that file and BuiltinThemePalettes.hpp. The116 expanded entries
  in docs/theme-sources.json omit the original9 and cannot alone define all125.
- NeNe Folio1411261b92231dbe0a3783be35ae5b6578ae2821:
  src/core/folio_theme.c owns127 fixed palettes, with original light/dark followed by
  the same125 names in identical order. System is an additional selection, not a
  128th fixed palette. Its documented Nib import is5b7dbb2791785e508384fd3a7e82cb01f40c8ebb.
- Commander7ef9b9654bf26a74d2c673c6ff1d1d79195e65b2:
  ColorScheme and eight XAML dictionaries remain the sole closed scheme mechanism.

The current Nib table has91 dark/34 light entries. All125 names and attribution
fields agree with Folio. Body background/foreground agree for124. The remaining
vscode-high-contrast-light was corrected in Nib commit3e1e0970 on2026-10-20:
its appearance changed dark to light and all16 body roles changed. Folio retains
the pre-correction dark colors. The fixed old/current Nib audit finds no other
body-palette or appearance difference. Commander should propose the current
corrected Nib value, explicitly recording that Folio is not yet equal.
This investigation does not change Folio.

## Count and compatibility decision

Commander shares monokai, solarized-dark, solarized-light and dracula identifiers.
Its nene-dark, ubuntu, nene-black and nene-light are not in the shared catalog.
In particular ubuntu is not the identifier ubuntu-aubergine; do not silently alias
them or infer identical full token values from similar names.

Recommended candidate: the shared125 plus these4 existing choices, total129.
Keep the existing default and all8 persisted identifiers. An alternative is125
selectable shared choices with the4 old choices readable only; that needs explicit
behavior for a current/default legacy selection absent from the picker.
hide was asked at20:11 JST; the count/visibility decision is still open.
Neither option is implemented.

Matching identifiers do not prove matching colors: current monokai/dracula pane
background and primary foreground agree with Nib, solarized-light foreground differs,
and solarized-dark differs in both. A future alignment must record these visible
changes rather than treating them as unchanged compatibility.

## One integration path

Keep SettingsSession/ISettingsStore, closed ColorScheme parsing, composition-root
dictionary resolution, RequestedTheme and CopySchemeInPlace. Existing brush instances
must survive a scheme change. Do not import a runtime Nib/Folio dependency, external
theme loader, second palette store, second settings writer or generated-at-build catalog.

Import once from a fixed source, retaining125 identifiers/order, source metadata,
appearance and the accepted shared base colors. Map the existing18 semantic color
roles and18 brushes through a reviewed design handoff. Every other foreground must
be checked against each surface where it actually appears: normal, hidden, inactive,
focused/selected, field, warning/error, key hints and progress. Source body-pair
contrast alone cannot establish those roles or Windows high-contrast behavior.
No final mapping or literal styling is approved by this investigation.

The current eight nested ColorScheme cases and one-file ARC-012 extraction do not
scale by copying125 cases: CS-013 counts nested tokens in the enclosing type.
Choose a compact closed representation under a new ADR before implementation.
Preserve inaccessible construction/concrete-case closure, immutable owned enumeration,
one exact boundary parse and one exhaustive constant-address mapping. Do not weaken
the300-line ceiling or compose a resource URI from persisted text. If source shape
changes, migrate the existing conformance extractor atomically with its positive/
negative proof; do not leave a second registry or treat a skipped rule as active.

The current editor uses an ItemsControl of eight radio options inside a ScrollViewer.
A125/129-entry list needs an agreed searchable/scrollable selection design; no
focus-preview or save-on-navigation behavior is implied. Preserve ADR-0077:
movement only focuses; Space/Enter accepts through the existing handler, and
Escape closes the current settings scope. A new search field must respect IME,
modal ownership, no-result behavior, selected/preview distinction and focus return.
Coordinate with #528's language choice and proposed schema migration; do not assign
conflicting future schema versions or overwrite a new field in another selector.

## Attribution and distribution

The bounded palette notice block is identical in current Nib, its Folio-import
ancestor and Folio. An initial comparison of the entire suffix reported a difference
because Folio appends unrelated Mermaid/Pele notices. The palette block itself is
71,389 decoded characters and has retained source bytes/hashes.
Nib's earlier md4c/github-slugger block is also unrelated to palette import.

The metadata records52 MIT,37 MIT original-palette,28 Vim palette-adaptation,
2 Apache-2.0 palette-adaptation,1 attributed Monokai palette and5 attributed published
palette entries. These are source labels, not a blanket legal determination from
either repository's top-level MIT license. Preserve applicable author/license text,
adaptation records and original artwork names. No logos, film images, characters,
upstream theme implementation code or affiliation claim is required.

Commander currently packages resolved NuGet/framework notices through
eng/release-notices.ps1. Merely adding a root palette notice does not include it in
the payload. Extend the existing publication layout explicitly for palette notices
and source attribution, and verify their bytes in output/ZIP before any release.
Do not fabricate a package entry or include unrelated third-party code notices.

## Remaining proof

The [quality record](../quality/2026-11-22-theme-catalog-investigation.md) is static
source evidence only. After scope/design/ADR are settled, prove the exact selectable
and accepted identifier sets, immutable ownership, all dictionaries/tokens and constant
addresses, current default/legacy parsing, selector preservation, save/failure/restart,
localization keys, target settings navigation and representative native light/dark/
narrow-window/high-contrast behavior. Verify packaging notices with the affected
publication tool. Choose full/deep gates only for the final impact requiring them.
No issue completion, merge readiness, native UI, release or cleanup is claimed.
