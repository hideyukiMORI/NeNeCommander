# ADR-0087: Extend the existing closed color-scheme catalog to 129 choices

Status: accepted

Date: 2026-11-22

Technical acceptance: 2026-11-23 00:16 JST, after independent read-only re-review
found no remaining required contract correction. Accepted under hide's continuing
implementation authority; native screen/IME/HC/speech and feature acceptance remain separate.

Issue #527. hide confirmed the shared125 plus the four existing Commander-only
choices at21:34 JST. This decision changes the representation and concrete catalog
inside the existing settings/resource mechanism. It does not accept native UI,
publication, release or the separately unaccepted language feature.

## Context and fixed inputs

[The source investigation](../design/2026-11-22-theme-catalog-investigation.md)
compares fixed Nib5d76cc7 and Folio1411261. Nib owns125 shared identifiers, order,
appearances and source16-role palettes. Its corrected vscode-high-contrast-light
is Light; Folio retains an earlier Dark version. The current Nib correction is
selected. Four names already overlap Commander, so the union is129, not133.

Local branch feat/527-theme-catalog starts from accepted main b82aa1e and includes
Draft PR559/91e06f5 (including its ownership correction) as an explicit dependency. All1,092 non-Markdown inputs of the
language candidate match its frozen evidence before theme changes. This includes
schema5, the fourth mandatory DisplayPreferences argument and three locales.
Native approval and main integration of that dependency remain separate.

Copying129 nested record cases would exceed CS-013. A private catalog-backed
case prototype with an owned catalog instead measures173 logical lines for ColorScheme, with zero
CS-013 findings and seven successful positive/negative compiler probes. This is
representation evidence only, not product implementation proof.

## One selected mechanism

### Closed choices and exact boundary parsing

Retain public abstract ColorScheme, its externally inaccessible abstract member,
private root constructor, get-only Identifier/Appearance and ColorScheme.Parse.
Replace the eight nested cases with one private sealed CatalogScheme carrying
the literal identifier and closed appearance. Its constructor is callable only
inside its private enclosing scope. The sole construction list is the private SchemeCatalog-owned array field
initializer with exactly129 literal entries. Its public view implements only
IReadOnlyList and enumeration, never non-generic ICollection/SyncRoot or a
mutable collection. Array.AsReadOnly is insufficient because its SyncRoot can
expose an array; the isolated prototype and dependent language fix prove closure. No
runtime source, dependency, discovery, import, factory accepting arbitrary text,
public constructor or writable/init property is added.

The order is nene-dark, ubuntu, nene-black, nene-light, then the125 in their fixed
Nib order. Existing eight public named properties remain aliases into this sole
catalog. Parse keeps exact ordinal matching, typed absent/unknown rejection and
canonical instance return. A known value's record copy is valid; external
abstract derivation remains possible but additional concrete cases do not.

### Constant resource and label mappings

Retain one dictionary per scheme with the existing18 colors and18 brushes.
ResolveDictionaryAddress continues to return only authored absolute ms-appx
addresses. Replace its growing switch with a private immutable FrozenDictionary
of129 literal identifier/address pairs; no resource URI is composed from text.
The framework-provided dictionary is part of the pinned runtime, not a new
package. A missing pair remains a defect. The Presentation localization-key
mapping follows the same immutable literal-table form in its narrow owner.
These are projections checked against ColorScheme, not additional scheme
admission registries. Existing default and all eight stored identifiers survive.
There is no further schema change beyond the dependent ADR-0086 schema5.

ARC-012's existing extractor migrates atomically to the single strict literal
SchemeCatalog array initializer, rejects empty/malformed/duplicate entries, and verifies
exact identifier/dictionary/constant-address parity. Existing dictionary color/
brush and view-resource proof remains. Add positive/negative proof for omitted
or extra identifiers, wrong appearance/source shape, missing or interpolated
addresses, and drift of the same semantic token set. Do not weaken or skip a rule.

### Static palette adaptation and provenance

The adopted design must retain exact source SurfacePane/background and
TextPrimary/foreground for the shared125, use the existing18 semantic roles, and
preserve the four Commander-only XAML files byte-for-byte. The four overlapping
schemes may visibly change and must be reported as alignment changes.

A fixed source manifest records the125 identifiers/order/appearance, original16
roles, author, license and URL with source commits/hashes. A deterministic
engineering import/check tool emits reviewed catalog source, constant maps and
XAML into declared source locations. It is neither a runtime loader nor a build
step. These imported source artifacts are committed under this ADR's CS-019
exception: ARC-012 runs before product build, WinUI evaluates its XAML inputs
before compilation, and clean checkout builds cannot depend on another product
checkout. Regeneration remains deterministic and is checked against committed
inputs/output; every file must be reproducible without network access. Generated
C# is not excluded from analyzers, coverage, mutation or complexity.

The palette notices and per-theme attribution are added to the existing release
notice path with explicit bytes/hash inventory and ZIP equality. They are not
fabricated NuGet entries. Do not import unrelated Nib/Folio dependency notices or
infer palette licensing solely from a top-level repository license.

### Actual text, focus and progress pairs

The fixed adaptation uses encoded-sRGB interpolation with half-up integer rounding,
0..32 deterministic steps, exact source background/foreground, and no lowered
contrast target. All candidate fills and optional focus/selection separation are
chosen BEFORE surface repair. Derived text/marks move toward the contrasting
black or white neutral anchor, preserving source hue longer than blending toward
an unrelated tinted foreground. All125 current inputs have a compliant endpoint;
future inputs must independently pass rather than inherit a universal guarantee.

FocusRing also colors Bookmark slot text, so it must meet4.5 on all actual text
surfaces, not only3 as a non-text mark. TextPrimary/Secondary/Hidden/KeyHint and
warning/danger text are checked on all eight owned surfaces. Active-number text
uses SurfacePane on FocusRing; passive-number text uses TextSecondary on
BorderSubtle. The reviewed125x63 pair matrix passes; coalescing marks remain a
separate non-color/native observation, not a false complete accessibility claim.

Two existing projections need small visual changes. Passive focus's sole narrow
marker uses TextSecondaryBrush instead of BorderSubtleBrush, keeping its
SurfacePane background and the same precedence/selection meaning. Every progress
segment gains an OperationProgressBrush outline using the existing1.5-DIP
DensityIconStrokeThickness. Empty segments use SurfacePaneBrush; full segments
keep OperationProgressBrush. Existing8-DIP squares retain a conservative5-DIP
interior, making empty outlined versus filled visible without a third contrast
triangle. Only OperationRunning renders these segments, on Idle/SurfacePane.
No operation counts, completion arithmetic or command semantics change.

An earlier outline proposal retained OperationTrack inside; nene-light had a
2.970249574275058 progress/track ratio. The revised empty interior avoids that
pair rather than weakening the3 target or changing its legacy palette bytes.
All129 palettes pass the actual passive-marker and outlined/filled segment pairs.
OperationTrack remains in the existing18-role dictionary contract for compatibility;
this segment presentation no longer draws it. The four private palettes remain
byte-identical, but these two shared geometric/projection changes are still visible.

### Search and explicit selection inside the existing settings scope

The same SettingsSession and ordered complete-value writer remain the sole
selection/save authority. Filtering and keyboard movement never select, save or
preview a theme. Native search/query and focused candidate are Presentation-owned
transient view state, as in the existing command palette, with immutable owned
catalog rows. They do not enter persisted UserSettings or create a second
application modal. Opening/closing resets search, and opening still focuses Close.

Keep the two checkboxes and four language choices. The theme section shows an
unfiltered current-theme line, a labeled native search field, filtered count or
empty text, and one bounded single-column choices region. Match ordinal-ignore-
case identifier and localized name, preserving catalog order. The search field
keeps typing, cursor keys and IME; composing Enter/Escape stay native. Outside
composition Escape closes settings once; Enter in search does not select a theme.
Outside search the existing settings j/k and Up/Down move focus, Space/Enter
commit once, and Tab follows native controls. No new paging or global shortcut
is added by this decision.

The choices region is one Tab stop, with current selected/focused option as its
native entry target. Existing RadioButton events may be retained with bounded
scrolling and Once tab navigation only if native proof confirms focus-only
movement; do not adopt the native RadioButtons selection-follows-focus behavior.
Filtering with no results leaves search and Close reachable and never changes
the applied scheme. The current theme may be outside results. A checked row
means applied, not merely focused. Full accessible names, stable identifiers and
visible light/dark labels are required. Save progress does not reset rows/query/
focus; selecting a theme preserves language, hidden state, hints, bookmarks and
both pane sort orders, including delayed and failed completions.

### Existing render and high-contrast boundary

Keep CopySchemeInPlace and existing brush identities. Do not replace the merged
dictionary during a selection. Scheme light/dark still controls RequestedTheme.
A catalog entry with high-contrast in its name is not Windows contrast mode.
The existing framework contrast behavior is retained at this stage; the design
consultation's proposed new system-colour override/event owner is not accepted
implicitly. Native high-contrast proof and the existing color-only row marker
limitation remain explicit acceptance work. If preserving readable focus and
selection requires a new adapter or marker contract, record and prove that
concrete change before feature acceptance rather than claiming static contrast
ratios establish it.

## Rejected alternatives

-129 nested case classes: the outer type exceeds the unchanged CS-013 ceiling.
-A publicly constructible string-backed scheme, URI composition or runtime theme
 loader: each widens the trusted catalog or introduces a second mechanism.
-A search combo box whose arrow navigation immediately commits: violates ADR-0077.
-Using a live preview with Cancel/revert: adds a second unsaved palette lifetime
 and contradicts the current save-on-selection contract.
-Assigning schema6 solely for catalog growth: identifiers remain the same closed
 field; only its accepted set grows.
-Reducing contrast targets to the source ratio: all125 source primary pairs are
 already above4.5, and no lowered threshold is needed.

## Required proof and migration

Delete the old eight case definitions and switch mappings in the same change.
Run affected Application, Presentation, settings codec/persistence and resource
mapping cases, exact three-locale key/value parity and final App build. Prove all
129 exact identifiers, aliases, appearance, immutable enumeration, closure,
unknown/malicious strings, current default and existing document compatibility.
Run ARC-012 positive/negative fixtures and the release-notice tool's own proof.
Measure the changed source and retain the existing five unrelated CS-013
findings without suppression. Palette numbers are checked independently against
all actual text/mark pairs, including active-number text on FocusRing and passive
number text on BorderSubtle. Static ratios do not prove native high contrast,
DPI, focus, IME, speech, hit targets or visual approval. Full canonical validation
is selected after final impact is known; no current full/deep/CI result is claimed.

[Implementation/design evidence](../quality/2026-11-22-theme-catalog-implementation.md)
records commands, failed attempts, source/notice provenance and remaining proof.
