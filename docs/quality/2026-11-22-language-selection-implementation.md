# Language selection implementation proof

Issue #528 / November22 / branch feat/528-language-selection.
Status: local implementation and selected automatic proof complete through23:06 JST; native/unmodified-startup feature acceptance and publication remain pending.
The earlier language-lifecycle investigation remains distinct from product proof.

## Current scope and provenance

hide confirmed Simplified Chinese at21:34 JST. Retain System as the old-settings
default and Japanese/English as explicit choices. ADR-0086 accepts schema5 and
next-launch application through existing settings and startup/resource owners.
Independent read-only review accepted the corrected technical ADR at22:24 JST.

Main b82aa1e is imported by1692b22 before implementation. The only conflict was
PROJECT_STATE; both additive entries were retained. Normal commit hooks passed
112 rules and24 registered security cases. No product tests were rerun merely
for this import. The original docs branch remains at82e0892.

## Trials

Raw evidence: D:/NeNeCommander/evidence/language-selection-20261122.
Owned future temporary test outputs: D:/NeNeCommander/outputs/language-selection-20261122.

- implementation-main-merge.log and implementation-main-commit.log retain the
  additive conflict/merge and normal hook success.
- baseline-restore.log: pinned locked Release restore succeeds.
- baseline-build.log: Infrastructure test project build succeeds,0 warnings/errors.
- baseline-tests.log:4 expected failures,0 successful/0 skipped. The unmodified
  product rejects each valid proposed schema5 document (system/ja-JP/en-US/zh-Hans)
  as SettingsRejected. This establishes missing acceptance; it is not a regression
  in the old schema contract and is not a success claim for language choice.
- Initial source-file guesses for App and two setting test files were absent;
  rg --files/actual source declarations identified the existing owners. No product
  mutation or App/settings operation occurred during these lookups.

Exact baseline commands:

```powershell
dotnet restore tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore -maxcpucount:4
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~InterfaceLanguageDocumentTests'
```

## Remaining acceptance

Unmodified product startup/restart in a separate OS profile/session or VM, native
keyboard/focus/IME, framework-owned speech, narrow/DPI/HC screen acceptance,
public PR and main integration/cleanup remain. Local selected evidence and copied
fixture results below do not replace these. #547's exact source approval is now
resolved as Draft PR558; it is not #528 publication permission.


## Technical design acceptance — 22:25 JST

The independent review found one required correction: the original proposal
passed an empty value to the Microsoft.Windows PrimaryLanguageOverride setter for
System. Official issue5335 reports that empty/null assignment throws. The existing
thirteen-process System diagnostic only observed an unset state; it did not prove
empty assignment. ADR-0086 now leaves System untouched in a new unpackaged process
and sets only explicit supported languages. The reviewer read back this correction
at22:24:04 and found no remaining technical blocker. This is not pinned-runtime
reproduction of the external bug and not feature completion.

The final schema positive cases must assert the actual parsed InterfaceLanguage,
not just whole-document acceptance. System/legacy/missing/rejected startup and
post-read cancellation require directly scoped evidence, with fixture substitutions
stated. Product code remains unchanged at this checkpoint.

An authored Simplified Chinese draft covers the392 current main resource keys:
99 explicitly inventoried unchanged keyboard/proper-name/unit/punctuation values
and293 reviewed translation candidates. WSL is additionally retained as a platform
acronym. Key and exact placeholder-format parity pass. Semantic review is pending;
no Chinese product catalog is written and no shipping completeness is claimed yet.

## Implementation and selected proof — 22:49 JST

The closed InterfaceLanguage choices now complete DisplayPreferences through one
four-argument factory. All existing call sites migrated; production selectors
preserve language. InterfaceLanguageSelection enters the same settings modal route
and complete-value ordered queue. Schema5 appends required uiLanguage; exact legacy
schemas1-4 select System only in memory. Unknown tags have the canonical
UnknownInterfaceLanguage rejection. The original atomic store is unchanged.

The settings editor adds a separate four-radio group and wrapped next-launch
explanation through existing templates and keyboard ordering. Progress-only renders
retain control sources. Startup defers InitializeComponent until the settings read,
post-read cancellation guard and explicit-language override. System never calls
the setter; the root inherits the explicit language before activation. Current
process resources and regional culture are not changed by later selections.

The Simplified Chinese catalog is now authored beside en-US/ja-JP. The original
293 translation candidates and99 inventoried non-language values received semantic
review at22:25:50; WSL remains an identified platform acronym. Six new language keys
and neutral save-failure wording bring each catalog to398 keys. Exact key/format
parity and actual Release PRI values pass398/398 in each of three locales. Native
Chinese IME, speech, screen fitting and unmodified full startup remain unproved.

### Trials and evidence

- implement-core.py migrated the construction and existing preservation sites;
  core-build-attempt1.log succeeds with0 warnings/errors.
- add-core-tests.py keeps schema4 read fixtures while migrating writer assertions
  to5 and the unsupported-version case to6. Exactly-at65536 and65537 serialized
  byte fixtures reduce a path by the22 added bytes; the boundary itself is unchanged.
- core-tests-build.log first reports CA1062 for the new public shape-test argument
  and IDE0005 for its unused System import. Adding the input guard resolves both.
  The first Python correction used implicit cp932 and failed before any mutation;
  its corrected invocation uses explicit UTF-8. The corrected build has0 warnings/errors.
- core-tests.log runs113 cases:111 succeed,2 fail because the existing pane-sort
  migration writer expectation still named schema4 and omitted uiLanguage. The
  expectation alone is corrected, retaining the old parser fixture. Those2 pass
  in persistence-tests.log; the original111 successes are reused.
- add-persistence-tests.py adds9 owned-store cases (legacy1-4 no-write, all4 language
  round trips, rejected-language byte preservation). The first9 fail only at the
  final enumeration assertion because Resolve(".") correctly refuses the root
  itself. Using the owner's already validated Path.CanonicalText corrects the
  fixture; persistence-tests-corrected.log reports9/9 with0 skips. The production
  store and root boundary are unchanged. These and the preceding113 are122 distinct
  successful Infrastructure cases; the two runs are not summed with duplicates.
- Application initial build succeeds. queue-build.log records two mistaken sort
  type names and CA1861 on the constant expected array. Actual SortKey/SortDirection
  and a static readonly expected list fix the fixture. queue-build-corrected.log
  succeeds0 warnings/errors. application-tests.log reports57/57/0skip including
  complete-value preservation through all selectors/catalog/sort, ordered language
  revisions, failure/cancellation/stopped admission and constructor null guards.
- host-restore.log is locked Release restore; host-build-attempt1.log is product
  App Release build,0 warnings/errors. presentation-build.log likewise succeeds;
  presentation-tests.log reports21/21/0skip for settings projection and existing
  keyboard behavior, including8 new language projection/name cases.
- complexity-initial.log returns1 with5 existing findings. Exact final comparison
  remains due after all test edits. No suppression or threshold change is used.
- catalog-and-source-parity.json records398 equal keys/formats per locale, separate
  radio groups/display order and static startup order. This is source proof only.
- makepri dump succeeds. The first diagnostic console print fails cp932 encoding
  for Chinese without changing product/evidence. verify-pri.py initially replaces
  dots inside an attached-property namespace and reports a missing key; preserving
  bracketed namespaces fixes only the comparator. pri-parity.json confirms all398
  values per locale against the real product PRI. The product was not changed.
- implementation-independent-review.json records the22:41:38 read-only review:
  no required product corrections. A separate OS profile/session or VM is required
  for unmodified full startup/restart proof; a substituted unshown fixture cannot
  claim it. No production hook, alternate loader or settings redirect is added.

### Commands and scope

All builds use `dotnet build <project> -c Release --no-restore -maxcpucount:4`.
App/Application/Presentation restores use `dotnet restore <project> --locked-mode
--disable-parallel -p:Configuration=Release`. Builds and tests are retained under
the exact log names above. Test commands use `dotnet test --project <project>
-c Release --no-build --no-restore -- --filter '<filter>'`:

- Infrastructure core: `FullyQualifiedName~InterfaceLanguageDocumentTests|FullyQualifiedName~KeyHintSettingsDocumentTests|FullyQualifiedName~BookmarkSettingsPersistenceTests|FullyQualifiedName~BookmarkSettingsDocumentRejectionTests|FullyQualifiedName~WindowsLocalSettingsStoreWriteTests|FullyQualifiedName~InfrastructureBoundaryTests|FullyQualifiedName~PaneSortSettingsPersistenceTests`.
- First persistence: `FullyQualifiedName~InterfaceLanguagePersistenceTests|FullyQualifiedName~ReadAsyncWhenLegacySchemaIsValidMigratesWithoutWritingAsync`; corrected9 only: `FullyQualifiedName~InterfaceLanguagePersistenceTests`.
- Application: `FullyQualifiedName~InterfaceLanguage|FullyQualifiedName~UserSettingsTests|FullyQualifiedName~SettingsSession|FullyQualifiedName~KeyHintSettingsTests|FullyQualifiedName~PaneSortPersistenceTests`.
- Presentation: `FullyQualifiedName~SettingsPresenterTests|FullyQualifiedName~SettingsKeyboardTests`.

Filesystem tests temporarily set process TEMP/TMP to the dedicated D output test-temp
and restore them in finally. No normal App/settings or desktop input is used.
`pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect` measures the
unchanged standalone ceilings; final changed-file format/conformance still follows.

### Isolated startup fixture in progress

startup-fixture-provenance.json hashes copied product App inputs and lists every
source substitution: owned settings location, two owned initial pane roots,
recording setup, post-read cancellation injection, activation replaced by observation,
and defect recording. A diagnostic partial is added only outside the repository.
The real XAML/converter, copied lifecycle/startup composition and real Windows store
are retained. This is not the unmodified product and cannot prove actual restart,
physical input, user acceptance or speech. Initial restore succeeds; fixture build
attempt1 has CS0833 (duplicate anonymous diagnostic property) and dependent XAML
resolution failures/WMC1509. Rename only the diagnostic fields before retrying.

### Separate publication approval resolved

hide explicitly approved #547's exact49-file65a6fb2 push and Draft PR at22:46 JST.
All1092 frozen inputs remained identical; the authorized branch was pushed and
PR558 created/read back OPEN/Draft with49 files and the exact head. No merge or
native acceptance was authorized or claimed. The earlier #547 automatic hold is
resolved for that payload only; language work does not inherit external publication
permission from it. Records remain in open-other-pane-20261122 evidence.

## Final local evidence — 23:06 JST

Application adds2 direct null-boundary cases and reports2/2 in
application-null-tests.log, giving59 distinct successful Application cases.
The prior57 are reused. All seven affected projects' changed C# files pass
`dotnet format whitespace <project> --no-restore --include <changed C# files>`
and the corresponding `--verify-no-changes` command; format-summary.json lists
6/3/2/4/13/14/3 files by project. The App workspace warning remains in its log.
Product-build-final.log reports0 warnings/errors. Conformance-final.log passes112
unique rules. complexity-final.log exits1 with the exact5 pre-existing findings;
complexity-comparison.json compares path/line/column/metric/value/limit unchanged.

The scope-specific compiler probes in verify-closed-language.ps1 use the pinned
compiler and actual Application/Domain assemblies. Abstract copy and known root/
selection copy compile; external concrete copy/ordinary/override attempts fail
with only expected CS0534/CS0115. All5 expected outcomes pass. This is the new
language root's scoped evidence, not a claim that the full canonical gate ran.

### Copied startup fixture: results and limits

After the first CS0833 fix, fixture build attempt2 finds IDE0055 and an unused
reflection result (IDE0058). Formatting and explicitly discarding that result
produce build attempt3 with0 warnings/errors. The first en-US observation has
correct language/resource values and no visibility, but records one activation
event despite unchanged foreground endpoints. It is retained as the separate
startup-en-US-focus-attempt.json/run. Remaining cases were stopped. This first
attempt does not establish no activation or native focus safety.

The fixture alone replaces CommanderWindow.FocusFileList and
SettingsEditorView.FocusInitialClose with no-focus observations. Product code is
unchanged. The corrected en-US process and9 further normal states all exit0 with
0 activation events, no visibility/owned foreground and unchanged foreground
endpoints. Each compares all329 ordinary keys through the window's actual loader,
three native XAML properties, converter output and four language radio options;
all match the selected source locale. The theme group independently retains one
selection. Legacy1-4, missing and rejected inputs use System without setting an
override. All original settings bytes (or absence) and regional cultures remain.
startup-normal-verification.json records these10 cases. No keyboard/mouse injection
or native focus claim is involved; the two focus substitutions remain material.

The cancellation injection occurs immediately after the real owned-store read.
The copied guard creates no window and leaves the override unchanged, but the
initial diagnostic incorrectly required Application.Resources.MergedDictionaries
Count to be0 and stopped when it observed2. The diagnostic expansion initially
hits IDE0305; the collection expression fixes it without suppression. The one
new cancellation observation is exit0: generated _contentLoaded is false at read,
before resource inspection and after inspection; no window exists; override,
culture and foreground remain unchanged. Inspection exposes XamlControlsResources
and DesignTokens dictionaries. It cannot establish whether they existed before
the read or materialized due to the Resources inspection. Do not claim automatic
framework preload or complete suppression of resource evaluation. Preserve the
original resource-count assertion failure as an overbroad diagnostic condition.

The23:00:40 independent read-only review agrees no product correction follows
from this observation. ADR-0086 and the host comments now explicitly order only
host-owned initialization calls. startup-cancel-review.json records the reasoning.
This proves the copied post-read cancellation branch, not actual OS-close timing.

All11 case inputs retain their original bytes/absence. Fixture source substitutions
and originals are in startup-fixture-provenance.json. The ten normal cases are not
rerun for the cancellation-only diagnostic change. Full unmodified product startup,
actual saved-selection restart, physical focus/input/IME, framework-owned strings,
speech, visual clipping/DPI/HC and screen acceptance remain unproved. The older
13-case investigation is separate and is not added to these cases as feature proof.

final-input-hashes.json freezes the final non-Markdown inputs. No full/deep CI,
coverage or mutation is claimed: changed behavior is bounded by settings/schema,
existing queue consumers, presentation/keyboard, App compilation, catalog/PRI and
scoped construction evidence. Native uncertainty needs the named environment;
unrelated provider or operation suites would not prove it. Reuse these successes
unless their relevant implementation, tests, resources, dependencies or tooling change.
