# Language selection implementation proof

Issue #528 / November22 / branch feat/528-language-selection.
Status: technical design accepted; unchanged-product baseline retained; production work next.
The earlier language-lifecycle investigation remains distinct from product proof.

## Current scope and provenance

hide confirmed Simplified Chinese at21:34 JST. Retain System as the old-settings
default and Japanese/English as explicit choices. ADR-0086 proposes schema5 and
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

## Pending

Technical ADR review, closed choice/construction, schema/queue preservation,
translation and key/format parity, settings presentation, actual startup/resource
proof and affected builds remain. No native keyboard/mouse input, normal App/user
settings mutation, public source push, PR readiness, integration or cleanup is
claimed. #547's49-file source push65a6fb2 is separately held by automatic approval
at22:13; the exact payload/destination question is pending. Do not retry that
rejection through this or another branch. It does not block local #528 work.


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
