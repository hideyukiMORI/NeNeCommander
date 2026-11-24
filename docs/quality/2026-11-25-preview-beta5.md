# Preview beta.5 integration and release proof — 2026-11-25

Status: local candidate prepared; release verification in progress

Issue #580 uses the feature cutoff explicitly selected by hide on November24:
#530 pointer entry, #531 context menu, #544 empty text file, #547 other-pane
navigation, #548 compact locations, #551 display transfer, the requested icon /
parent-path entries and #581 bookmark heading/About corrections. #532 drag/drop
and the remaining broad #564 work stay outside this release. Published beta.4 is
unchanged. The sole App Version declaration advances to0.1.0-beta.5.

The accepted #581 correction is saved with normal commit hooks as
b218587e2c6ac85e9fa730e746e51ff071e7bfb4. Its exact tree is fast-forwarded into
D:/NeNeCommander/worktrees/issue-580 at08:09 JST. The independent review and all
selected native evidence remain in the [accent record](2026-11-25-bookmark-accent.md).
No behavior test is repeated merely for this local integration or version metadata.

## Invariant and verification selection

Existing CommanderSession / CommandCatalog, PaneSession, FileOperationGateway,
canonical input mapping and the single window adapter retain their ownership.
ADR-0061 selects final Application/Presentation consumer coverage, adversarial and
mutation stages because this candidate combines changed command, input, navigation
and display-transfer contracts. Integration conformance, deterministic theme parity,
locked restore, format, solution build and record-case closure also apply.

Reuse is reconciled against the1317 frozen inputs of the successful context-menu
stages03 and their original raw reports. Domain and Infrastructure source/tests,
provider-facing Application file-operation/launch contracts and common inputs are
assessed before reuse. Successful unaffected negative proofs and audit are reused
only with corresponding input identity. All original failed stages remain failed.
This is explicit assembled evidence, never a relabeled successful whole CI run.

New raw receipts/scripts belong to D:/NeNeCommander/evidence/release-beta5-20261125;
new disposable output belongs to D:/NeNeCommander/outputs/release-beta5-20261125.
Final proof inputs will be frozen before long-running stages. Changes invalidate
only the stages that consume them. No thresholds, exclusions or suppressions change.

## Remaining release steps

Current GitHub readback shows Issue580 open and no release PR. Final candidate
CodeQL / zero open owned alerts,96-hour freshness, public source/PR acceptance,
squash integration, accepted-main official publisher, manifest/ZIP/notices/resources,
separate extraction, unmodified isolated startup, tag/release and downloaded hashes
remain pending. #94 physical screen confirmation remains an explicitly unperformed
Preview tier under ADR0079, separate from bounded native-control observations.

No shared-desktop key/mouse injection, ordinary settings changes or termination of
other applications is authorized by this work. Preserve live trial03 and unfinished
feature outputs. Cleanup starts only after accepted integration and permanent
preservation with exact-path and live-reference verification.

## Final integration gaps and correction

Initial release stage01 passes theme parity, locked restore, whitespace, solution
build(0warnings/errors) and record closure. Application runs1184tests successfully
but branch coverage99.8303167421% fails the required100%. Two unvisited busy-state
edges are in PaneSnapshot.CanNavigateParent; a redundant final type guard is in
PaneInteraction.NavigationIntent. Independent Presentation coverage01 has507pass/
1fail: deletion cancellation still expects12footer hints while accepted Ctrl+E
adds the13th. Both original failing receipts/reports remain unchanged. The driver
stops before adversarial/mutation; these initial runs are not relabeled successful.

The final correction changes one Application routing body and two test files.
NavigationIntent distinguishes the two already closed Row/Surface cases directly,
removing an unreachable extra-case guard without adding a command or route. Existing
private-protected closure, sealed concrete cases, admission and factories are unchanged.
Passive surfaces are explicitly proved not to navigate/launch; the launching state
is proved false while pending and true after completion. Read-only review tightens
the loading fixture to a previously listed child location, so retained content cannot
mask the busy condition: true before the second read, false while pending, true after
cancellation. The same content and exactly two provider reads are asserted.
The deletion fixture now checks Ctrl+E's key/intent and Escape in the13-hint list.

Corrected consumer builds and changed-file whitespace pass. Application coverage03
passes1187/1187 with100%branches; unchanged-production Presentation coverage02 passes
508/508 with97.2611077298%branches. All exits are0. The previous weaker loading-test
source and correction02 success are retained; only the Application stage repeats for
that subsequent test change. Final independent read-only review at08:42:43JST requires
no further correction. Record-closure and Architecture proof remain applicable because
type identities/constructors/fields/public contracts are unchanged by the routing body.

The independent reuse review identified three changed App-consumer Architecture
classes absent from the original release driver. The selected29tests now pass with
0fail/0skip. Unchanged ColorSchemeResource and ProjectDependency proof is retained.
This is not a claim that changed App consumers were covered only by static hashing.

proof-inputs-04.json freezes1360final inputs; versus the original release freeze,
exactly the three correction files differ. Comparison against the1317context-menu
inputs gives1246exact/71changed/43new,none missing. The385reused layer/common/provider
inputs and27Domain inputs still match. Review also checks indirect Application type
dependencies and finds none consumed by Infrastructure source/tests.

The remaining driver starts08:43:24JST, PID25800. Three adversarial repetitions each
for Application and Presentation pass. Application Complete mutation is running;
Presentation follows success. Thresholds95/90 remain unchanged. Current CodeQL tool
restoration verifies every archived file before use; final analyses have not started.

## External publication hold and setup observations

The exact43e8320 /301-file push to the existing GitHub repository is rejected by
automatic approval before process creation: unclear consent for that concrete
payload/destination and potential private-source export. The prepared Draft PR body
and rejection are retained in pr-body-01.md /publication-hold-01.json. hide's question
is pending. No alternate write or retry occurs. Subsequent connector readback verifies
public repository1354826314,ownerhideyukiMORI,admin/push permissions and the matching
clone URL; it does not substitute for the pending concrete approval. The correction
and later records remain local, outside that originally proposed43e8320 payload.
The final publication description must reflect final source and proof results.

Setup observations are separate from product failures. Initial OrderedDictionary
grouping displayed a blank aggregate label; corrected PSCustomObject receipt02 retains
the original. An early driver start refuses a still-pending local commit before any
stage starts; the later clean43e8320 launch is the actual stage01. Negative-proof
reuse preflight initially expects equal resource-owner occurrence counts; current XAML
has92versus90, so that preflight stops. The mutation anchor itself remains unchanged
and replacement removes every explicit owner in both inputs; the exact difference is
recorded in the supplemental reuse reconciliation, rather than claiming equal bytes.

Supplemental reuse reconciliation02 now records6identical workflow/hook files outside
the1360-input manifest,63unchanged negative-proof checker inputs and8unchanged mutation
anchors. The XAML owner count difference90→92 is explicit: the fixture removes all
such owners in both and the unchanged SettingsTitle.Text witness still lacks its
required owner. Missing synthetic resource keys remain missing in all3locales.
No full-foundation byte-identity claim is made. Raw negative success is retained at
context-menu-20261124/deep-canonical-01.log:3–8; Domain100%success is retained at
remainder-coverage-02.log:13 with a hash-verified copied raw report. Domain mutation/
adversarial and audit are individually passed checks in the original failed beta.4
report. The conservative96-hour expiry of the oldest reused stage is November27
23:34:56JST, to be checked again at publication.

Exact selected commands are indexed with exit/timestamps in stages-progress-01.json,
architecture-consumers-01.json, correction-progress-02.json,
application-correction-03.json and remaining-progress-03.json. Principal commands:

- `pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta5-20261125/run-release-stages-01.ps1` — original failure at Application coverage is retained.
- `dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~ViewCompositionOwnershipTests|FullyQualifiedName~CommanderWindowCompositionTests|FullyQualifiedName~AsyncLifecycleOwnershipTests"` —29pass.
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta5-20261125/run-correction-02.ps1` — consumer builds, changed format, Application/Presentation coverage pass; the later loading-test input receives its own Application03result.
- `dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore` — final test build03passes.
- `dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --coverage --coverage-settings eng/coverage.settings --coverage-output-format cobertura --coverage-output D:/NeNeCommander/evidence/release-beta5-20261125/coverage-application-03.cobertura.xml` —1187pass/100%branches.
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta5-20261125/run-remaining-stages-03.ps1` — both3-repeat adversarial tiers pass; Complete mutation remains running, with exact canonical arguments and95/90thresholds in its receipt.

Final complexity inspection exits1 with exactly the same3complete finding objects as
accepted581complexity03(two parameter findings and one nesting finding). No new
finding or threshold change is introduced; no clean-complexity result is claimed.

The first correction commit passes conformance/security, then its normal hook rejects
a new blank line at the quality record's EOF. That documentation whitespace is
removed; correction-commit-01.log stays failed and the next normal-hook attempt
uses a separate log. Frozen product/test inputs and running mutation are unchanged.
