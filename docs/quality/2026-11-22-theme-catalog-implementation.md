# Theme catalog design and prototype proof

Issue #527 / 2026-11-22 / current local dependency base85cd35d.
Status: technically accepted ADR-0087/handoff after00:16 review; language91e06f5 imported; no theme product changes.

## Scope and provenance

hide's21:34 decision is129 selectable choices: common125 plus existing four.
The source audit remains in the earlier investigation. main b82aa1e was imported
as a111ab0. Draft language PR559/4a41dc4 was explicitly imported as85cd35d to avoid
conflicting settings UI/schema work. Both sole PROJECT_STATE conflicts were
additive entries resolved by retaining both sides. Normal112-rule/security24
commit hooks and commit-message checks passed. All1,092 frozen non-Markdown
language inputs remain identical before theme changes. No dependent feature's
native acceptance is inferred and no new branch is published.

Permanent evidence: D:/NeNeCommander/evidence/theme-catalog-20261122.
Disposable prototypes: D:/NeNeCommander/outputs/theme-catalog-20261122.

## Design advice and transmission

The3,851-byte authored brief covers search/selection and semantic palette roles,
without repository source. Automatic approval rejected its first transmission
before process creation because prior approval covered another brief. hide then
explicitly approved this brief (question call_5c8fec8096924cf7b92c215e25a2a19a).
The same SHA256AF20CFABC28FC179054E00AA11B7B0A073ED7C343598E40F704C30BECE3E2A11
was verified before sending. Claude completed at23:24:56 JST with tools/MCP/
Chrome disabled, customizations disabled and no session persistence. Original
JSON, exact brief, plain advice, invocation and stderr are retained. The old hold
record remains historical; it is resolved for this payload only. This is ordinary
consultation, not a /design-generated artifact: the installed command's earlier
consent-only behavior is unchanged. No source or user settings were sent.

Advice proposes a current-theme line, search/count, bounded single-column choices
and a pinned footer; explicitly selected scheme differs from keyboard focus.
The standard RadioButtons behavior selects when arrow focus moves, as documented
by [Microsoft](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/radio-button).
A native Once tab region and focus-only handling must be proved before acceptance.

## Closed representation prototype

prepare-catalog-probe.py reads the fixed extraction and creates six source files
outside the repository product tree. ColorScheme has one private sealed
CatalogScheme, one read-only literal list and eight existing aliases. Pinned SDK
10.0.404 compiles it with nullable/warnings-as-errors. Seven external probes pass:
abstract copy and known copy compile; concrete ordinary/copy/override attempts
fail with the expected CS0534/CS0115, direct private-case creation with CS0122 and
identifier mutation with CS0200. closure.json retains each exact diagnostic.

The first CS-013 measurement failed because the minimal fixture omitted the
required empty tests directory. It is an input-shape failure, not a type-ceiling
failure, despite the wrapper's generic error wording. The original error JSON/log
is retained. After adding only that directory, measurement passes with0 findings,
0 unresolved invocations and ColorScheme162/CatalogScheme7 logical lines. The
seven successful compiler probes were not repeated.

RuntimeProbe independently checks129 unique cases (94Dark/35Light),129 exact
canonical parses,129 known copies/equality/hash, eight named aliases, absent and
seven invalid strings, and four refused IList mutations. All pass; the list still
has129 entries and the original default afterwards. These isolated experiments
do not replace eventual production tests or the product closure gate.

Commands (exit0 except the explicitly retained first measurement):

    python -X utf8 .../prepare-catalog-probe.py
    pwsh -NoProfile -File .../verify-catalog-probe.ps1
    pwsh -NoProfile -File ./eng/measure-complexity.ps1 -RepositoryRoot .../catalog-probe -Mode Inspect
    dotnet <pinned-sdk>/Roslyn/bincore/csc.dll <recorded nullable/warnaserror arguments>
    dotnet .../catalog-probe-checks/RuntimeProbe.dll

The full exact paths/arguments are preserved in the scripts and terminal evidence.
The first wrapper exits1 at fixture-shape measurement, after all seven compiler
probes had succeeded; the scoped second measurement exits0.

## Palette adaptation experiments

The original advice's repair order could separate focus/selection fills AFTER
contrast repair and thereby invalidate it. Its claimed guaranteed foreground
endpoint also requires checking the track and all real surfaces. The proposed
implementation constructs all candidate/distinction fills first, then repairs
surfaces toward exact background, then derives text/marks against the repaired
surfaces, with no later color mutation. No target is capped below4.5: all125
original primary pairs already satisfy it.

Two separate scripts/results are retained. The first interprets mixing as
linear-light sRGB. All125x63=7,875 pairs pass, minimum text4.500550913581204 and
nontext3.00064102822474; however44 warning colors converge to the primary color.
The second uses encoded-sRGB channel interpolation with half-up integer rounding,
like ordinary opaque color interpolation. Its same7,875 pairs all pass, minimum
text4.500065935324707 and nontext3.0020822227088155;15 warning colors converge.
This second result is a design proposal, not yet accepted styling.

Both preserve the exact source foreground/background, all18 roles and the source
appearance. The four private XAML hashes are recorded unchanged. Four common
schemes have complete old/new18-role differences, not a false no-change claim.
Four schemes (quiet, zaibatsu, gruvbox, everforest-dark) have equal focus/selection
marker colors, while their fills differ. Existing file-row markers are color-only;
this needs visible/native review and must not be called non-color selection proof.
The proposed Windows system-color override has not been implemented or accepted.

Scripts: probe-semantic-mapping.py and probe-semantic-mapping-encoded.py; exact
values, adjustment steps, every pair/ratio and source hashes are retained.

## Remaining work

Independent technical review and ADR acceptance; final design handoff; concrete
catalog/constant maps/XAML and attribution import; ARC-012 extractor/proof;
searchable settings UI and three-locale strings; affected build/tests/native proof;
release-notice integration and packaging verification. No production theme code,
full gate, CI, feature acceptance, PR publication or cleanup is claimed. Unfinished
worktree/prototypes are retained. Existing user App/settings/Preview are untouched.

## Review corrections — November23 00:07 JST

The initial IList mutation refusals did not prove storage ownership. Independent
review found Array.AsReadOnly exposes its array through ICollection.SyncRoot.
The separate catalog-owned-probe preserves the original prototype and stores all
129 literals inside a private SchemeCatalog with only IReadOnlyList/enumeration.
verify-owned-catalog-probe.ps1 compiles it and independently checks all129 canonical
parses/copies/equality/hash, eight aliases, absent/seven unknown strings, both
mutation surfaces absent, and129 indexed/non-generic enumeration identities.
Runtime result is successful; CS-013 has0 findings/0 unresolved invocations,
ColorScheme173, SchemeCatalog142 and CatalogScheme7 logical lines. The original
seven closure probes are reused: their public root/private-case declarations are
unchanged. No prototype result is presented as final product proof.

The same discovery affected Draft language PR559. That separate correction is
published as91e06f5 after a reproducing failed baseline,12/24/16 directly affected
Application/Infrastructure/Presentation cases, clean App/test builds, two-file
formatting, unchanged5 complexity findings, ordinary112/security24 commit checks
and independent read-only acceptance. Its native/unmodified-startup acceptance is
still incomplete. Importing that dependency correction into this branch is next.

The third color experiment uses a neutral black/white anchor, but review found
FocusRing was also Bookmark shortcut text. The fourth, reviewed experiment raises
that role to4.5 on all eight surfaces. All7,875 pairs pass: minimum text
4.500888115843828 and nontext3.0086522726322595. Original/proposed/neutral/reviewed
scripts and results are all retained. Rendered schematics are illustrations only.

Review also found low contrast in the passive focus marker and empty progress
track. The first extra129-theme actual-pair check has one failure: nene-light
progress against its original track is2.970249574275058. marker-outline-proposal-
check.json retains it. The revised proposal uses TextSecondary for passive focus,
adds a progress-colored outline, and uses SurfacePane for the hollow interior.
The8-DIP square with existing1.5-DIP stroke retains5 DIPs inside. All387 actual
pair checks pass across129 including the four unchanged legacy palette files;
marker-outline-revised-check.json records the exact ratios. Source inspection
confirms only OperationRunning presents segments, with Idle/SurfacePane tone.
No production presentation has changed yet; the new handoff/ADR define these
changes before implementation. High contrast, focus/IME, native dimensions and
speech remain pending.

Repeated source lookups for an output-side RuntimeProbe and nonexistent
OperationPresentation/OperationPresenter paths were read-only misses. The actual
saved evidence fixture and DualPanePresenter/OperationBarView established the
rendering ownership. No fixture or product was launched by those lookups.

## Technical acceptance and dependency correction — November23 00:18 JST

The independent read-only re-review of ADR-0087 and the concrete handoff finds no
remaining required contract correction. Storage ownership, actual text/mark pairs,
hollow/filled progress, explicit theme selection, TextEntry/IME and modal focus
contracts agree. It performs no editing/build/test. This accepts implementation
under hide's continuing authority; it does not accept native or dependent features.

Proposed design commit c095bcd passes112 conformance/security24 and normal message
checks. Language correction91e06f5 is then imported; the sole PROJECT_STATE conflict
contains independent additive entries and retains both. Every1092 frozen non-Markdown
language input matches, so no product proof is repeated merely for the import.
The next work is the offline manifest/notice generator, literal catalog/maps/XAML,
ARC-012 proof and searchable settings implementation. No theme product result yet.
