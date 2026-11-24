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

## Implementation checkpoint and hide-requested stop — November23 00:57 JST

hide requested a suitable stopping point and a daily report/handoff at00:52 JST.
The live-region review correction, final affected build and local records are the
stop boundary. No native fixture, additional feature, public push or release is
started after that request. The older continuing-work statements are historical.
The [session report](../reports/2026-11-23-milestone-checkpoint-daily-report.md) and
[handoff](../handoffs/2026-11-23-milestone-checkpoint-handoff.md) own the restart state.

### Implemented candidate

- Closed129 values (94Dark/35Light), exact parsing and eight retained aliases;
  private SchemeCatalog exposes no ICollection/SyncRoot or mutable list.
- Frozen literal129 URI and129 resource-name projections. Offline
  `eng/import-theme-catalog.ps1` owns125 imported XAML dictionaries, three C#
  templates and121 added proper-name keys per locale. Eight further UI keys bring
  each catalog to527 entries. All2,250 generated semantic colors and adjustment
  steps equal the separate reviewed Python calculation. The four Commander-only
  palette hashes remain exact. Dracula/Monokai/Solarized dark/light visibly align
  with the accepted shared source; this is not a promise of unchanged appearance.
- ARC-012 uses exactly one strict129-entry literal extractor and rejects malformed,
  missing, duplicate, extra, reordered or appearance-drifted catalog/constant
  projection inputs. The canonical Merge gate checks offline import parity before
  restore/build; Commit remains conformance/security/whitespace only.
- Searchable settings retain current-theme text, name/identifier filtering,
  light/dark labels, an eight-row bounded list, one Tab region, cycling modal and
  footer outside body scrolling. Search uses existing TextEntry and the host IME
  guard. Native Checked still emits the single settings intent. Newly loaded rows
  observe the latest applied scheme; progress does not rebind query/rows. Touched
  SettingsPresentation option storage also closes its SyncRoot exposure.
- Passive focus uses TextSecondary; progress uses the accepted outlined/filled
  geometry with SurfacePane interior and existing1.5/8-DIP tokens. No operation
  arithmetic or selection policy changes.
- Existing release notice collection now includes125 distinct theme attributions
  and the exact71,394-byte bounded notice, SHA256
  `31629c34013fc98b7778528cf2bbb9c1d32ecd5967b29321c82f2b723450851a`.
  The71389 character count in the earlier audit is not a byte count. An exact
  eol=lf attribute preserves these bytes across Windows checkout. It is a separate
  palette inventory, never a fabricated NuGet package.

### Selected proof and exact commands

Working directory for these commands is `D:/NeNeCommander/worktrees/issue-527`.
Logs/results/scripts are in `D:/NeNeCommander/evidence/theme-catalog-20261122`.
Outputs are `D:/NeNeCommander/outputs/theme-catalog-20261122`. No full gate,
coverage, mutation or new CI run is claimed for this candidate.

| Command / selected scope | Result and retained evidence |
|---|---|
| `dotnet restore NeNeCommander.slnx -p:Configuration=Release --locked-mode` | Success, restore-release.log; no lock change |
| `pwsh -NoProfile -File eng/import-theme-catalog.ps1 -Mode Write -ReportPath D:/NeNeCommander/evidence/theme-catalog-20261122/import-first.json` |131 outputs/129 schemes; import-first.log/json |
| `python verify-import-proposal.py` (saved absolute evidence script) |All2250 colors and every repair step equal independent reviewed values; import-proposal-parity.json |
| `pwsh -NoProfile -File eng/import-theme-catalog.ps1 -Mode Check` |Success at initial and final changed-input checkpoints; import-check-stop.log |
| `pwsh -NoProfile -File eng/prove-gates.ps1 -Rule ARC-012` with TEMP/TMP under owned D gate-temp |Positive1/negative16 pass; original3 retained; arc012-proof-first.log. Proof launcher verified and removed its own exact root. |
| `dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore` |0 warnings/errors; catalog second and queue build logs |
| `dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~ColorScheme'` |13/13 before the later queue test was added; application-catalog-tests.log |
| Same Application test command, filter `FullyQualifiedName~ColorSchemeSettingsQueueTests\|FullyQualifiedName~SettingsSessionTests\|FullyQualifiedName~InterfaceLanguageQueueTests` |19/19; all129 complete-value saves, ordering/failure and language retention. application-queue-tests.log;32 distinct Application cases across the two runs |
| `dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore` |0 warnings/errors; infrastructure-catalog-build-second.log |
| Same Infrastructure test command, filter `FullyQualifiedName~ColorSchemeSettingsDocumentTests\|FullyQualifiedName~InterfaceLanguageDocumentTests` |34/34;129 full current-schema round trips,8 existing IDs across4 legacy schemas, unknown-text refusal; infrastructure-catalog-tests.log |
| `dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore` |0 warnings/errors; presentation-search-build-second.log |
| Same Presentation test command, filter `FullyQualifiedName~SettingsSchemeViewStateTests\|FullyQualifiedName~SettingsPresenterTests\|FullyQualifiedName~SettingsKeyboardTests\|FullyQualifiedName~PaneResourceTests\|FullyQualifiedName~DualPaneProgressPresenterTests\|FullyQualifiedName~PaneListingPresenterTests` |50/50; prior16 SettingsPresenter cases overlap and are not added; presentation-search-tests-first.log |
| `dotnet build tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-restore` and selected `ColorSchemeResourceTests` |0 warnings/errors and3/3 URI/appearance/null cases; architecture-catalog-build-first.log and architecture-catalog-tests.log. Unchanged mapping evidence reused after later UI edits. |
| `dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore` |Final review-fix build0 warnings/errors; app-review-fix-build.log |
| `pwsh -NoProfile -File eng/prove-release-notices.ps1 -OutputDirectory D:/NeNeCommander/outputs/theme-catalog-20261122/notice-proof-01 -WindowsSdkLicensePath D:/NeNeCommander/releases/0.1.0-beta.3/candidate-20261122-01/NeNeCommander-0.1.0-beta.3-win-x64/licenses/Windows-SDK-license.rtf` |21 resolved package records,125 palette attributions,55 ZIP files all byte/hash-equal; corrupted notice refused before output. This tests notice packaging, not a new release payload. |
| `dotnet format whitespace NeNeCommander.slnx --no-restore --include <changed C# paths>` then `--verify-no-changes` |Exit0, existing workspace-load warning retained; format-search-first.log / format-stop-verify.log |
| `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` |Exit1 with exactly5 prior findings, identical path/line/column/metric/value/limit. complexity-picker-first.json and complexity-picker-comparison.json; no new finding or suppression |
| `pwsh -NoProfile -File eng/conformance.ps1` |112 rules pass after review fix; conformance-stop.log |

The final private notice attribute, source input hashes, Git readback and local
checkpoint commit are recorded by stop-input-hashes.json, stop-github-readback.json
and checkpoint-commit.json. Normal commit checks remain mandatory.

### Trials and independent review

All failed attempts remain beside successful logs. First Application no-restore
build failed NETSDK1004 because this new worktree had no assets; the explicit
locked Release restore corrected the environment. First UI build failed CA1826
for parameterless FirstOrDefault on an indexed list, corrected to Count/index.
The added keyboard test initially reversed the existing Create argument order
(CS1503), corrected without changing the API. Subsequent App and Infrastructure
builds each found an unused using (IDE0005); both were removed. No test was run
against a stale DLL after a failed build. Read-only path misses and an rg option
placement error had no product effect; subsequent reads used the actual paths.

Independent implementation review found one required correction: Text changes
alone did not explicitly raise the results live-region notification. RenderResults
now compares ordinal text, returns for identical text, and raises the existing
LiveRegionChanged mechanism after changed text. Read-only re-review at00:55:02
accepts this correction and finds no additional required defect in the reviewed
ownership/input/closure scope. The final App build includes this correction;
actual assistive speech remains unproved. The sources for native focus semantics
were Microsoft [GettingFocus](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.uielement.gettingfocus)
and [TrySetNewFocusedElement](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.input.gettingfocuseventargs.trysetnewfocusedelement),
checked against the compiled pinned SDK. These documents are not native acceptance.

### Explicit remaining work

No new theme candidate process or screen was launched. Still required: real
loaded-control checks for latest selection/no unintended save, narrow/DPI layout
and nested-scroll reachability, native Tab/j/k/Enter/Escape and IME, speech, Windows
contrast modes, all packaged PRI/XBF values and clean-checkout source/notice proof,
then final proof-scope decision, feature review, concrete publication approval,
Draft PR/integration and eventual cleanup. Compiler-closure prototype proof is
retained but is not mislabeled as a final product compiler-probe run. The final
small helper/new test additions also require a scoped final complexity refresh if
implementation continues. User App/settings, main and public beta.3 are unchanged.

## Resumed non-visible proof — 2026-11-23 01:21 JST

hide explicitly resumed at01:01 after the earlier stop. The original stop/checkpoint
remains historical; this continuation changes documentation and owned diagnostics
only. All1,227 frozen product/tool/test inputs still match stop-input-hashes.json.
Main/user App/user settings/public beta.3 are unchanged. No physical input,
shared-desktop focus, application activation or visible diagnostic window is used.

### Actual controls, delayed save and bounded layout

prepare-settings-control-fixture.py copies169 App source/resource inputs into
D:/NeNeCommander/outputs/theme-catalog-20261122/settings-control-fixture, records
all original hashes/substitutions and verifies the frozen product inputs first.
It substitutes owned pane/settings paths and startup activation; copied
FocusFileList/FocusInitialClose become no-ops. A diagnostic delegating store delays
only the first real owned-store write. The original native Checked/Loaded events,
settings intent route, queue and rendering remain. Any non-deactivated event,
visibility or owned foreground observation aborts the fixture. These replacements
make the proof narrower than unmodified startup, real keys or focus acceptance.

`dotnet restore App/NeNeCommander.App.csproj -p:Configuration=Release --locked-mode`
and `dotnet build App/NeNeCommander.App.csproj -c Release --no-restore` in that
fixture succeed; final build has0 warnings/errors. First control fixture build
had IDE0022/IDE0010 in diagnostic helpers, corrected without suppression; second
passes. The later layout helper's IDE0078 likewise becomes the required pattern;
second layout build passes. All four trial logs remain under the evidence root.
No failed-build DLL was executed.

`Start-Process -FilePath <fixture>/App/bin/Release/net10.0-windows10.0.26100.0/win-x64/NeNeCommander.App.exe
-ArgumentList <en-US|ja-JP|zh-Hans> -WindowStyle Hidden -PassThru` followed by a30-second
bounded process wait runs the same27 control cases in each locale. Each exits0,
with27 passed checks, exactly2 explicit selections/writes, zero activation events,
never-visible/never-owned-foreground observations, and unchanged foreground endpoints.
The cases prove129 initially loaded radios, case-insensitive query, current selection
outside results, zero results, latest checked state after re-realization, query/source/
control identity across delayed completion, no save during render/filter/reopen,
129 full accessible names/stable identifiers and retained language. This is27
behavioral cases across3 locales, not81 distinct tests. No focus retention or speech
is inferred from control identities or accessibility properties.

The separate `-ArgumentList @('zh-Hans','layout')` mode exits0 with19 checks at the
actual125% rasterization scale. The content measures exactly320x300 and640x360 DIPs.
Modal sizes are296x249.6 and520x309.6; Close stays inside modal/content at both sizes.
The outer body viewport is194.4/254.4 DIPs high, with a256-DIP inner choice viewport.
StartBringIntoView reaches both first and last full32-DIP rows within both scroll
clips, with Close still inside the content. No settings write occurs. This is API
scroll/geometry proof for this locale/scale only, not keyboard/IME/mouse/visual or
other-DPI proof. settings-layout-zh-Hans.json retains every rectangle and offset.

The same hidden fixture loads all129 actual compiled dictionaries and compares
all2,322 Color values plus2,322 brush values with byte-equal XAML sources. Every
in-place copy retains all18 destination brush identities and matches selected
colors. runtime-palettes-zh-Hans.json records each scheme; no Windows contrast-mode
or rendered usability claim is made. All non-substituted fixture inputs still match
original source hashes; resumed-proof-input-verification.json records this check.

### Final binary, compiler and clean-source proof

- Pinned SDK `makepri.exe dump /if <product-App-bin>/NeNeCommander.App.pri
  /of D:/NeNeCommander/evidence/theme-catalog-20261122/app-pri-dump.xml /dt detailed /o`
  plus verify-compiled-and-control-proof.py confirms527 exact values per en-US,
  ja-JP and zh-Hans. All132 product XBF files, including129 schemes, are byte-equal
  to their PRI EmbeddedData. Original verifier expected basic rather than detailed
  qualifier syntax and failed before any product mutation; pri-verifier-trial.json
  records the correction and exact singleton language matching.
- `pwsh -NoProfile -File <evidence>/verify-product-closure.ps1` uses the final
  Application Release DLL and pinned C# compiler, not prototype source. Two valid
  abstract/copy probes compile; five unauthorized concrete/case/mutation probes
  fail with the expected CS0534/CS0115/CS0122/CS0200 diagnostics. All7 checks pass;
  actual DLL hash and logs are in product-closure-checks/closure.json.
- `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` exits1 with
  exactly the same5 prior findings, including identical path/line/column/metric/
  value/limit; no new finding. complexity-resumed-comparison.json records equality.
  Existing unresolved-invocation limits remain, and this is not a clean whole-tree
  complexity result.
- `git worktree add --detach D:/NeNeCommander/worktrees/issue-527-checkout-proof
  1700b197bb7b9f8dddbab85d04ef0d4c9fa9d109` performs a real clean checkout.
  Both `eng/import-theme-catalog.ps1 -RepositoryRoot <checkout> -Mode Check` and
  `-Mode Write` succeed for131 outputs; regeneration leaves no tracked/untracked/
  ignored changes. The notice is exactly71,394 bytes with the accepted SHA256.
  This is regeneration/source proof, not a second full build.
- That disposable checkout has no unique artifacts, links or active executable
  references. Normal `git worktree remove <exact-checkout>` succeeds at01:14:55 JST;
  absence is verified. The checkpoint commit, current feature worktree, fixtures
  and all evidence remain. No cleanup rejection or hide cleanup request arises.

### Scope decision and remaining acceptance

ADR-0061 focused evidence covers the closed representation/consumer boundary,
all current and legacy settings identifiers, ordered complete-value persistence,
settings presentation/host controls, constant URI/compiled resource projection,
actual marker/progress resources, ARC-012's changed parser and notice/import tools.
The catalog remains authored and closed; no external factory/URI composition,
provider/storage/shell boundary or dependency input is introduced. Existing
Application32/Infrastructure34/Presentation50/Architecture3, App build, ARC1+16,
notice55-file ZIP and color-pair proof are reused unchanged. A full/deep/CI run is
not necessary merely for checkpoint/push/Draft publication. This does not waive
release proof or any new wider-impact change; native acceptance is still required.

Outstanding: unmodified startup/restart of dependent language PR559, native
Tab/Shift+Tab/j/k/Space/Enter/Escape and IME/focus return, actual speech, Windows
contrast mode, representative other DPI/locale layouts and hide's visual acceptance.
The feature remains a local candidate; no new public PR, merge, release or feature
acceptance is claimed here. Prepare a concrete Draft payload next; the existing
limited external-transmission approvals are not generalized.

The first fixture-input audit also compared the deliberately rewritten csproj to
its original hash and stopped before documentation edits. The audit now explicitly
accounts for that declared absolute-project-reference/diagnostic-unsafe substitution
alongside the three source substitutions; all165 unmodified copied inputs match.
Read-only filename/wildcard lookup misses were corrected with actual file paths
and directory-scoped rg filters; they changed no product or proof inputs.

## Final user-requested stop — 2026-11-23 01:48 JST

hide requested stopping at01:46 JST. No new feature, process, external transmission
or CI starts. final-stop-preflight.json verifies no owned diagnostic/candidate
processes, clean mainb82aa1e and absence of the disposed checkout. The sandbox CIM
read was denied; a privileged read-only query completed without changing processes.
Additional file-only proof matches all132 fixture XBF to final product and PRI and
all3 owned saved settings to initial values except the intended theme. See
fixture-product-xbf-and-settings-parity.json. No product input changed.

The01:24 public push of35776de was automatically rejected before process creation
for lack of explicit approval of the231-file/~2MB public payload and destination.
publication-hold.json retains the exact reason; no indirect retry or new PR occurs.
Stopping records do not silently extend the earlier pending payload question.
Native check priorities and exact limits are in the closing daily report/handoff.
Wait for hide to resume; retain unfinished feature/fixture artifacts.

## Explicitly approved Draft publication — 2026-11-23 01:58 JST

hide explicitly answered 承認. The earlier exact35776de/231-file/2,070,510-byte
payload, destination and4,638-byte body hash were revalidated. Local84e062c adds
only6 stop documents, so the push names the approved commit explicitly:
`git push origin 35776deddd757cb2ed9484d9ca56dae132666522:refs/heads/feat/527-theme-catalog`.
It succeeds; remote readback matches35776de. No stop-document changes are sent.

The connector has a previously established PR-write permission limitation. The
authenticated CLI creates Draft565 with `gh pr create --repo hideyukiMORI/NeNeCommander
--base main --head feat/527-theme-catalog --draft --title <prepared-title>
--body-file D:/NeNeCommander/evidence/theme-catalog-20261122/draft-pr-body.md`.
Readback confirms OPEN/Draft/main, exact head,231 files and matching title/body;
statusCheckRollup is empty. The exact prior automatic-approval hold is resolved,
not bypassed; publication-approved.json records the explicit user approval.

No product input changes or tests are needed for this metadata-only step. The
stop instruction continues; native/feature/dependent-language acceptance, main
integration and release remain incomplete. Closing documents remain local.

## Manual feedback, corrected reference and final stop — 2026-11-23 02:37 JST

At hide's request the unmodified #527/#528 candidate was visibly launched around
02:10, PID39872 at launch. The original518-byte settings were backed up before
launch; never restore that copy over later manual changes. launch.json under
the permanent manual-launch-20261123-0209 evidence records executable/input hashes.
Blue-frame computer-use was mistakenly used for launch; hide explicitly corrected
this. No further screen capture, keys or clicks followed. This mechanism is now
explicitly forbidden for launch/status as well as input. Use ordinary background
process APIs and retain shared-desktop input/focus restrictions.

hide's ten-point feedback reports working reactivation keys in this trial,
theme catalog/search/Japanese input and selection/persistence behavior, but rejects
the current interaction/design as the intended experience. Colors are provisionally
acceptable. Nested scrolling/scrollbars overlap text; Ctrl+, after returning from
another app takes seconds; resizing briefly loses drawing and settles in an
estimated0.5–0.8 seconds. These are user observations, not instrumented timings.
Immediate language switching is requested for discussion, not proved implemented.

Read-only code investigation identifies nested ScrollViewers,129 nonvirtualized
radio rows, close/open recreation, recursive control discovery per settings key,
and whole-surface render/ScrollIntoView candidates. None is a measured cause.
The inspected Nib settings drawer was the wrong reference for the user's intent:
hide subsequently specified **Ctrl+P → :colorscheme selection**. Follow that exact
route next; the prior Folio-like proposal remains unaccepted. Current ADR-0086
still specifies next-launch language; any live-language mechanism requires a
coherent replacement decision and direct state/resource proof.

The feedback JSON, supplied screenshot copy and hash, and final correction/stop
record are retained in D:/NeNeCommander/evidence/settings-feedback-20261123/.
No product code, resource, dependency, test or running process is changed by this
closing documentation. No build/test/full/deep/CI or public write is run.
Documentation diff/link review and the ordinary commit hook are the selected
scope; exact commands/results and final commit are in closing-verification.json.
Feature/native acceptance and main integration remain incomplete. hide explicitly
requested stopping after the daily report and handoff; do not resume automatically.
