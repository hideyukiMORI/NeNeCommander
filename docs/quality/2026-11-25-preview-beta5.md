# Preview beta.5 integration and release proof — 2026-11-25

Status: local release-stage proof accepted; exact public payload approval and release-side verification pending

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

## Mutation completion and CodeQL correction

Application mutation03completes09:20:22JST at95.99%, exit0:3664total mutants,
2440Killed/27Timeout/101Survived/2NoCoverage/531CompileError/563Ignored; the score is
2467of2570counted mutants. Presentation03completes09:41:20JST at92.23%, exit0.
The original closed-stream cancellation/retry diagnostic remains in its raw log.
All1360frozen inputs still match before the next correction. Together with the
reconciled unchanged Domain/Infrastructure results, all four thresholds are met for
0e0829c. They are not automatically evidence for subsequently changed source.

Compiler-traced CodeQL02completes164rules/136results.123are generated obj findings;
owned source has2errors/6warnings and5notes. Original SARIF and diagnostics remain.
The separate canonical buildless extraction is still running on the unchanged owned
snapshot, with its restore/native-assembly diagnostics retained; it is not yet a
successful final CodeQL result. No SARIF upload or public source write occurs.

Independent read-only review identifies the two formatting errors as analysis
confusion between a test localizer's template and value returns, not incorrect
shipped placeholders. All three locales carry the correct templates. The correction
resolves each template to CompositeFormat before resolving argument values, keeping
localization immediate and the same output contract without suppression.

The menu's post-Detach guard must retain native Hide reentrancy. Stop now invalidates
the existing render-owner token before detaching; the post-Detach check uses that
same owner identity for both stopping and replacement. It does not simply remove
stopping protection. PaneInteraction retains its single common owner/activity check,
then delegates content admission and its existing NavigationIntent to the two closed
concrete cases. This removes base-to-derived tests/casts and the new complex-condition
note without a second validation or dispatch route. Three test-only interface casts
become direct MSTest negative-type assertions, retaining snapshot immutability proof.
The other four interop notes are pre-existing; no note is silently relabeled absent.

codeql-correction-preflight-03.json records the original owned findings and exact
1360-input identity immediately before these six file edits. Coverage/mutation for
changed Application/Presentation, record closure for the added abstract contracts,
changed build/format and the menu stopping/reentrant native boundary must be refreshed.
Domain/Infrastructure/unchanged audit remain reusable. All earlier failures and the
original successful mutation inputs are preserved. Final CodeQL/ref/open-alerts,
public acceptance, accepted-main package/startup/freshness/tag/release remain pending.
## Corrected native ownership proof and frozen release inputs

Independent read-only review accepts the six corrected files with no required change.
Corrected consumer/App builds, Application1187coverage100%, Presentation508coverage
97.2611077298%, record closure, conformance and changed format all pass in
codeql-correction-progress-03.json. Complexity03retains exactly the same three complete
finding objects as01; its exit1 remains explicit.

Native menu attempt2passes52assertions across four stop/replacement and open/closed-target
combinations. All six native Closing callbacks occur synchronously inside outer
Render/Detach before it returns. Stop clears invocation/menu/render owner, prevents
later opening and forwards no stale command/focus after Dispatcher advancement.
Nested replacement retains its exact invocation/menu and suppresses superseded focus.
Attempt2exits0; normal settings/input desktop Default are unchanged. This is bounded
native reentry proof, not physical input or visible acceptance.

The diagnostic copy reconciles182App inputs:179exact plus the declared composition,
project and copied-lock substitutions. Three added diagnostic files observe the
unchanged menu implementation. Four normal-product DLLs are frozen and every final
output DLL hash matches. menu-owner-final-parity-06.json owns the complete mapping.
Its initial preflight expected only two substitutions and correctly stopped on the
explicit diagnostic-only lock refresh; product source/locks remain unchanged.

All preliminary failures remain. Diagnostic build03misses a namespace import.
Build04overlaps the mutation driver's initial build cleanup and fails on two shared
ref assemblies. Build05uses frozen product DLLs and succeeds; its stale restore graph
still evaluates two project references. Final standalone graph06removes those and
duplicate reference declarations, refreshes only its own lock, and preserves every
remaining package version/content hash. Build06succeeds0warnings/errors with
BuildProjectReferences=false and no shared dependency build. Future diagnostic builds
use this standalone graph. No production or frozen proof input changes.

Native1passes both stop combinations, then fails the observer's exactly-one-Closing
assumption during replacement: nested ShowAt produces two synchronous notifications.
The corrected observer requires at least one and verifies every callback is inside
outer Render; all ownership/focus/no-replay assertions remain. Original failed fixture,
receipt, event sequence and exit2 are preserved. No timeout/termination occurs.

proof-inputs-05.json freezes1360release inputs; exactly six differ from04. The385
reused layer/common/provider inputs and27Domain inputs still match. Driver04starts
10:12JST; both tiers'three adversarial repetitions pass, then Application Complete
mutation runs and Presentation follows. Thresholds95/90remain unchanged. Prior03
mutations remain successful for their own prior inputs only.

Original buildless CodeQL02also completes164rules/7owned results:6warnings/1note.
Compiler-traced02has13owned results:2errors/6warnings/5notes plus123generated results.
All original diagnostics/SARIF remain. That old driver's final root-identity check
fails because the six reviewed corrections differ from its04freeze. Its independent
snapshot remains old0e0829c; this is not final-candidate analysis or a successful whole
driver. The final corrected commit needs fresh compiler-traced and buildless analysis.

Additional exact commands/receipts:

- `pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta5-20261125/run-codeql-correction-03.ps1`
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta5-20261125/prepare-proof-05.ps1`
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta5-20261125/run-remaining-stages-04.ps1`
- In menu-owner-03: `dotnet build App/NeNeCommander.App.csproj -c Release --no-restore -p:BuildProjectReferences=false`, after recorded diagnostic restore06.
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta5-20261125/run-menu-owner-03.ps1 -Locale ja-JP -Attempt 2`
- `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` — same3findings.

Source/PR/SARIF publication remains unperformed while the concrete permission question
is pending. Main integration/final package/startup/tag/beta.5release remain unclaimed.
Selection562is locally committed2cfc3b2with selected proof, outside the beta.5cutoff.
No cleanup is eligible for either unfinished task.

## Final local stage acceptance at 11:07 JST

Application Complete mutation 04 finishes at 10:48:49 JST with 96.03% against 95%,
exit 0. Its 3,658 raw mutants are 2,439 Killed, 28 Timeout, 100 Survived, 2 NoCoverage,
524 CompileError and 565 Ignored: 2,467 of 2,569 counted mutants. Presentation 04
finishes at 11:04:09 JST with 92.28% against 90%, exit 0. Its 2,995 raw mutants are
2,193 Killed, 29 Timeout, 169 Survived, 17 NoCoverage, 239 CompileError and 348 Ignored:
2,222 of 2,408 counted mutants. Original retries and compiler failures remain in the
raw logs. All 1,360 inputs match at final completion. The driver names its starting
base `0e0829c`, but `proof-inputs-05.json` contains the six corrections and exactly
matches committed `b72bbcc2c3086338b2a530d6004440efec334905`; it does not measure the
old 04 inputs. The unchanged Domain result and Infrastructure 92.28% from context-menu
03 are retained through the existing hash and consumer reconciliation. All four
thresholds are satisfied. Original failed whole runs retain their failed status.

Final CodeQL 03 uses the same exact clean `b72bbcc` snapshot. Canonical buildless
analysis uses SEC-008's `build-mode: none`, 164 security-and-quality rules and only the
`**/obj/**` exclusion. It reports 0 results and an extraction notification for every
one of the 1,160 owned C# inputs. This is not complete, diagnostic-free extraction:
104 compiler diagnostics, 315 extractor messages and 15 extraction-error-file
notifications remain. In particular, the Application and Presentation types named
`PaneNavigationAction` become ambiguous in the buildless compilation, producing 42
CS0104 diagnostics and losing resolution in the App consumer and two tests. The eng
complexity and record-closure files also retain CS0017/CS0234 diagnostics. All logs remain.

The independent compiler-traced analysis supplements those specific missing product
and test resolutions. It has 1,195 successful notifications: 1,131 owned src/tests
paths, 62 generated obj paths and 2 notifications without a location. Every owned
product/test path is represented; compiler/extractor error notifications are 0 and
its 42 compiler notifications are Hidden CS8019. The 29 owned eng files are not part
of this supplementary build; their only analysis remains the canonical buildless run
with the limits above. No claim of a fully extracted, diagnostic-free repository is made.

Compiler-traced SARIF has 129 results: 123 generated and 6 owned. Two
`invalid-string-formatting` results at `PaneNavigationAction` lines 44 and 66 are
infeasible test-localizer branch combinations. The parent template is requested only
with `CommandEntryToolTipFormat`, never the `PaneNavigateParent`/`KeyLabelAltUp` keys
that return `PARENT`/`ALT-UP`. Directional template keys never take the `KeyLabelCtrlE`
branch returning `canonical shortcut`. All three actual locales have exactly the
required `{0}/{1}` parent and `{0}` directional placeholders. The unchanged selected
tests verify the real key choices and formatted results. CompositeFormat separation
alone did not eliminate the analyzer's imprecise flow; that original attempt and both
SARIFs remain. The four other owned results are `call-to-unmanaged-code` notes:
`CoInitializeEx`, `CoUninitialize` and `RtlNtStatusToDosError` are already evaluated in
ADR-0063; `SHOpenFolderAndSelectItems` is the explicit accepted ADR-0091/SEC-014 boundary.
None is silently suppressed, excluded or described as absent.

Independent read-only review at 11:07 JST accepts this combined local evidence with
the above extraction limits and six individual dispositions. It rechecks all 1,360
input hashes in both worktree and snapshot. Additional product/test changes and
repeated mutation are unnecessary. The suggested fake-localizer rewrite was therefore
not applied: it would change accepted test inputs without fixing product behavior.
CodeQL candidate-ref registration and API readback of zero open owned alerts remain
unperformed and cannot be inferred from the local 0 results.

`codeql-result-summary-03.json` distinguishes both SHA-256 hashes:
manual `69EC205017329856F143DC183F8013A15B4D891FD3C62D5B12E2A591240B917B`;
buildless `74BCB8ED3F95952677EBBCB48B0A49312F88879C37916BF6F1EEFD945ACC17B1`.
The earlier completion receipt's `sarifSha256` refers only to manual. Raw mutation
counts and hashes are in `mutation-final-summary-04.json`. All exact command arguments,
start/end times and exit results remain in `remaining-progress-04.json` and the
`codeql-*-03` receipts.

The original automatic-review hold for `43e8320` is not bypassed. Remote readback still
shows main `ccb303b` and no `chore/580-preview-beta5` branch. Final source and Draft PR
body are prepared for a new exact-payload permission question. No source/SARIF/PR
write, main integration, accepted-main package, startup, tag or prerelease is claimed.
Selection #562 and filter #545 remain separate and excluded. The oldest reused proof
conservatively expires November 27 at 23:34:56 JST and must be checked again before
publication. No cleanup is eligible; ordinary settings and the shared input desktop
remain untouched.

## Accepted-main publication — November 25, 20:56 JST

hide explicitly approved the exact final candidate source/PR/SARIF at 19:59 JST,
then approved beta.5 publication through tag/assets at 20:42 JST. The earlier
43e8320 automatic-approval rejection remains an unsuccessful historical attempt;
it was not bypassed. GitHub connector writes returned integration HTTP 403, so the
same approved payloads used the existing authenticated CLI. PR #582 is merged as
`5727258e7b5558ccdf64cfba4e176ef907a18e4e`. Its tree
`dbde3d24e4ba99e08f0fe4c04dbf492579d9cfd8` exactly equals accepted candidate
`3c4fb93a3fef4c97d22dcb74c2182a80d54fa0de`. Dependency review 45607174484 passed;
no duplicate full quality/deep-review run was started.

Candidate CodeQL analysis 2247313539 is complete on 3c4fb93. The 20:53 JST readback
has 164 rules, 0 results and 0 open candidate-ref alerts across all pages. Exact tree
equality maps this proof to accepted main. The canonical extraction diagnostics,
supplemental 6 owned dispositions/123 generated results and original failed runs
above remain explicit. This is assembled successful stage evidence, not a claim
that an earlier failed whole workflow succeeded. All 1,360 fixed inputs match main;
the oldest reused stage was 45.3 hours old before publication and conservatively
expires November 27 at 23:34:56 JST.

The canonical final package command was:

```powershell
pwsh -NoProfile -File ./eng/publish-release.ps1 -OutputDirectory D:/NeNeCommander/outputs/release-beta5-20261125/package-final-01 -WindowsSdkLicensePath D:/NeNeCommander/evidence/issue-434/windows-sdk-license.rtf
```

It ran from the clean accepted-main worktree and exited 0 at 20:47:22 JST. The four
existing proof scripts then exited 0: `verify-candidate.ps1`,
`verify-pri-locales.ps1`, `verify-extracted-final.ps1`, and
`verify-unmodified-startup.ps1`. Full arguments, timestamps, exits and logs are in
`D:/NeNeCommander/evidence/release-beta5-20261125/final-verify-01/*-command.json`.
The checks match 588 payload/ZIP files, 1,850 source inputs, 10 lock files,
21 packages/51 package notice files, SDK and palette notices, 132 embedded XBF
resources and all 603 strings in each of en-US, ja-JP and zh-Hans. The separate
extraction matches 300,129,327 payload bytes. The executable reports
`0.1.0-beta.5+5727258e7b5558ccdf64cfba4e176ef907a18e4e` and remains unsigned.

The exact extracted, unmodified application responds twice more than 3 seconds
apart on a named non-input desktop. Only the owned test child was terminated.
Ordinary settings bytes/mtime/directory entries and the input desktop are unchanged;
no physical input, desktop switching or normal-shutdown acceptance is inferred.
The secondary thread-desktop query remains unavailable with error 5; ownership is
established by enumeration on the owned desktop and the child PID. Independent
read-only review at 20:53:51 JST accepts the final artifact delta without a must-fix.

The annotated `v0.1.0-beta.5` tag peels to 5727258; tag object is
`91c1f4d64d1b1e696c389e18c3f23f8fbc0838ed`. The
[public Preview](https://github.com/hideyukiMORI/NeNeCommander/releases/tag/v0.1.0-beta.5)
was published at 20:55:19 JST with exactly three assets, `--prerelease` and
`--latest=false`. Public description/readback and all three downloaded hashes match:

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| `NeNeCommander-0.1.0-beta.5-win-x64.zip` | 114138401 | `84c73e4afcc01a27023d3601fed46dafae7cb50025750a594a21deb1696cdbaf` |
| `release-manifest.json` | 445270 | `8a7539c5fffa77b3699d32e6dbb60598b8a4de29a36e7e19431b5ef7f7b064ec` |
| `SHA256SUMS.txt` | 193 | `da25e383a9073f2369da37f8908a769414a213711ca09a9fc397c3265ccad60c` |

Permanent shipping copies are in
`D:/NeNeCommander/releases/0.1.0-beta.5/candidate-20261125-01`. The provisional ZIP
was never uploaded. The first final receipt serialization failed because a regex
replaced PowerShell's automatic Matches variable. `publication-receipt-failure-04.json`
retains that failure; a distinct variable corrected only the receipt, without another
network download, package build or product test. `publication-final-04.json` records
the successful final readback at 20:56:56 JST.

Issue #94's one-time display/keyboard/high-contrast confirmation was completed by
hide on November 19; exact scale/captures are not recorded. This final beta.5 has
no new physical-input, normal-shutdown, speech or all-live-provider evidence.
#560/#562/#545, clipboard and drag/drop are excluded. Completed-work archive/cleanup
follows Issue #583; unfinished work, branches/commits, trial interactive03, the
bookmark-focus and UI-trial output roots remain. No cleanup rejection exists.