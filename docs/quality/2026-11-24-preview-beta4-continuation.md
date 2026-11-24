# Preview beta.4 release continuation — 2026-11-24

Status: published November24 20:42 JST. Issue #571 / merged PR #573. The dated sections below preserve earlier pending and failed states.

## Authority and baseline

hide resumed milestone work until stopped on November24, with appropriate repeated
experiments, complete records and background Claude /design consultation when a
design is needed. This supersedes the November24 00:11 stop. No design change is
selected here. Shared-desktop input and normal settings replacement remain excluded.

Main starts clean at af1775513cd4d7d21768e0d3de1ee6e4272696bd; retained release
branch starts clean at cf572205dbd5b3b7f8d684901d346e529b8f8c61. GitHub readback
confirms Issue571 open and PR573 open/Draft. All later differences from product/test
baseline d688d58d01c966da79a0c64b142362388c0906e1 are documentation before this work.

Original deep review45195559902 remains **failed**: canonical2460 passes/16 explicit
live skips, all coverage thresholds, audit, three adversarial repetitions and Domain
mutation97.47% passed; Application94.98% failed95%, and Infrastructure/Presentation
mutation did not run. Candidate CodeQL2230861543 had164 rules/0 results/open owned
alerts0. Neither a passed workflow nor beta.4 publication is inferred.

## Invariant and selected change

Existing WindowRestorePlanner, WindowPlacementTracker, SavedWindowPlacement and
SettingsSession/UserIntent boundaries retain their single owners. No production
code, dependency, threshold, exclusion, mutation level or settings schema changes.
All115 baseline entries (113 Survived/2 NoCoverage) and the original report hash are
retained. WindowRestorePlanner contributes27, including missing multi-display and
vertical/diagonal distinctions; recent settings/placement guards also lack direct
boundary assertions.

Add26 deterministic cases in WindowRestoreSelectionTests (11),
WindowPlacementArgumentTests (7), and SettingsSelectionArgumentTests (8). They prove:
current-display first launch; vertical/two-axis overlap; disjoint rectangles;
four directional squared-distance cases; deterministic top/width/height ties;
required placement/picker/preference parameters; and no accepted-state, observer
or write effect after rejection. Existing NullGuardAssertions returns its already
asserted exception from AssertInternalMethodNullGuard, allowing parameter-name
checks. Five existing calls explicitly discard that result; other helpers retain
their original signatures.

ADR-0061 scope: affected Application build/tests/coverage and changed whitespace;
one full Application Complete mutation run also supplies causal killed-mutant
comparison. Missing Infrastructure/Presentation release mutation follows. Reuse
unchanged successful stages with explicit input reconciliation, preserving the
original failed workflow. Final changed-source CodeQL and release package proof
remain required. No separate preceding focused mutation or duplicate full gate.

## Attempts and results

- Initial sandbox git status rejected ownership; exact per-command safe.directory
  allows read-only inspection without modifying global Git settings.
- Large normative reads were divided after output truncation. Two guessed test/type
  paths were absent and corrected with rg/source inspection. No desktop action ran.
- Build01 failed NETSDK1064 before source compilation: prior project.assets.json
  pointed to CodeQL's working/packages. Same locked Release restore succeeded,
  preserving the lock hash; ordinary pinned packages already existed.
- Build02 failed88 IDE0058 diagnostics after three helper return types changed.
  Narrowed to the existing four-argument helper and five explicit discards; no
  suppression or analyzer change. No test ran before those build failures.
- Independent read-only review requested opposed width/height tie inputs and a
  Minimized null-bounds observation so a downstream guard cannot mask rejection.
  Both were corrected. Final re-review requires no correction and claims no test run.
- Build03 succeeds with0 warnings/errors. Selected MTP cases49/49 pass,0 fail/skip,
  including26 new cases. Full Application coverage passes959/959,0 fail/skip;
  owned branch coverage100% and line coverage92.052578%. Changed whitespace passes.
- Complexity Inspect exits1 with exactly the same5 baseline findings (all fields
  equal),1037 sources; no new complexity finding or clean-complexity claim.
- proof-input-reuse.json reconciles1246 current inputs against1243 at d688d58:
  exactly7 changed/added Application test files, all current hashes intact.
  All production, other tests, dependencies and gate/security tools are unchanged.
- Full Application mutation completes at08:09:07 JST, exit0,96.5095986% against95%.
  All3242 mutant identities/semantics match the original report;35 previously
  surviving mutants are now killed, all by the newly added cases. No detected
  mutant becomes undetected.78 Survived/2 NoCoverage remain; this is not100%.
  The guarded continuation starts the previously unexecuted Infrastructure tier
  at08:09:55 JST, with Presentation to follow after success. No successful combined
  release conclusion is claimed.
- Read-only process metadata through Get-CimInstance was denied in the sandbox;
  file logs and the owned execution-session handle remain the observation source.

Exact invocations and every exit/log are retained under
`D:/NeNeCommander/evidence/release-beta4-20261124/`: journal.md,
run-focused-01/02/03.ps1, their build/result JSON and logs, application-restore-01.json,
run-application-proof.ps1 and proof-inputs.json. Temporary output remains under
`D:/NeNeCommander/outputs/release-beta4-20261124/`. Key commands:

```powershell
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore
dotnet run --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build -- --filter 'FullyQualifiedName~WindowRestoreSelectionTests|FullyQualifiedName~WindowPlacementArgumentTests|FullyQualifiedName~SettingsSelectionArgumentTests|FullyQualifiedName~NullGuard|FullyQualifiedName~InterfaceLanguageQueueTests'
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --coverage --coverage-settings eng/coverage.settings --coverage-output-format cobertura --coverage-output D:/NeNeCommander/evidence/release-beta4-20261124/application.cobertura.xml
# From tests/NeNeCommander.Application.Tests:
dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Application.csproj --break-at 95 --output D:/NeNeCommander/evidence/release-beta4-20261124/mutation/NeNeCommander.Application --skip-version-check --concurrency 2
```

## Remaining work and limits

Finish remaining mutation, stage/input mapping, final static analysis,
PR evidence/review and accepted-main integration before official packaging. Prior
588-file package remains provisional; no tag, release or official candidate is
created. Keep existing human acceptance distinct from unrun scale/capture, package
launch, provider and speech checks. The release worktree/output is unfinished and
retained; completed feature cleanup is not repeated. There is no new cleanup hold.
## Completed Application proof and static-analysis correction

Application Complete/VSTest mutation takes39m46.97s atconcurrency2. Counts:
Killed2181,Timeout31,Survived78,NoCoverage2,CompileError427,Ignored523. Semantic
comparison checks source, locations, replacement and mutator for every entry.
The35 newly killed mutants cover WindowRestorePlanner23, placement tracker2,
saved placement2, settings4 and UserIntent4; each killedBy identity belongs to an
added test. Other changes are Killed-to-Timeout2 and Timeout-to-Killed1, both
still detected. The original failed workflow and all115 baseline undetected
outcomes remain preserved. The comparison initially emitted an empty transition
label through ordered-dictionary grouping; raw records were correct. Its initial
summary is preserved and the grouping is corrected without rerunning mutation.

The1246 non-document inputs remain unchanged after proof. Commit3fae9db saves
tests/documents with normal Commit checks;3b0a5c1 connects mainaf17755. The sole
PROJECT_STATE insertion conflict is resolved only after proving the main content
is retained; complete trees before/after connection are equal. No push follows.
The pending release ledger maps16 successful old stages to their original commands,
input basis and execution times. Old workflow45195559902 remains failed. The
conservative96-hour bound for old evidence is2026-11-27 23:34:56 JST, subject to
an actual final publication-time check.

The first final-source CodeQL analysis exits0 with164 rules/0 results and1037
extracted files, but the isolated copy lacks test/Windows reference assemblies:
2416 compilation diagnostics and350 extraction-error notifications. It is not
accepted as adequate final proof. The parser also initially assumed rules belonged
to the driver; the old CI SARIF stores them in an extension. An attempted old
invocation comparison has no valid baseline diagnostic counts and is discarded.

A separate database now uses the exact same1246 source inputs/config/query suite
and hashes of existing build reference DLLs. Two existing pinned-SDK Roslyn DLLs
and five managed MSTest/Windows SDK/WinUI/WinRT DLLs supply missing metadata; no
product source, dependency pin or query exclusion changes. Initial repair
preparation stopped because WinUIEdit.dll is native; the corrected script verifies
the five copied managed references and excludes that native file. Both attempts
are retained. Resolved extraction/analysis is still running and needs source/rule/
diagnostic reconciliation plus candidate-ref upload and open-alert readback.

Evidence additions: application-mutation-summary.json,
application-baseline-survivor-outcomes.json, application-mutant-transitions.json,
application-proof-complete.json, independent-proof-reuse-review.md,
connect-main-complete.json, release-evidence-ledger-pending.json,
codeql-snapshot.json, codeql-additional-reference-manifest.json and the separate
CodeQL command/log/exit records. Infrastructure uses the repository default four
sessions; process-only TEMP/TMP keep disposable tests on D and are restored by
finally. No normal user environment or desktop setting is changed.

## Resolved CodeQL supplement and publication hold — 08:50 JST

Resolved extraction/analysis finishes at08:37:19 JST, exit0, with the same164 rule
IDs,0results and1037/1037 owned C# source paths. All1246 proof inputs and all7 added
reference hashes match. Independent read-only review accepts it as supplemental
proof for changed Application tests alongside unchanged-production CI2230861543,
not as a standalone database of equivalent precision. Application production and
all Application.Tests have0compiler/extraction errors; the changed7files retain
14CS8632 nullable-context notices. An earlier progress phrase of no diagnostics is
corrected: no errors is narrower than no notifications.

The result retains56compilation errors across10files and138extractor errors in
RecordClosureProbe and the two App XAML code-behind files. Inclusion of1037files,
7versus8unresolved types or313versus771references does not establish expression-
level equivalence. Old API SARIF lacks comparable diagnostic entries. No query,
source or diagnostic is suppressed. First attempts and corrected summaries remain
saved; no repeated analysis is selected without a new defect. Candidate-ref upload,
processing and open-owned-alert readback remain required and have not occurred.

Exact execution: pwsh -NoProfile -File
D:/NeNeCommander/evidence/release-beta4-20261124/run-resolved-codeql-02.ps1.
Its create/analyze receipts retain the full CLI arguments. Result SHA256:
081CD2DC5FECD1E2B9623C6C08C44AD50075016B29A2695EE1044DF0560EA96E.
See resolved-codeql-diagnostic-summary.json, its diagnostics companion,
resolved-codeql-execution-complete.json and independent-codeql-supplement-review.md.

At08:27 automatic approval rejects the normal push of exact localf6c722c to the
existing public hideyukiMORI/NeNeCommander branch because continuation authority
does not explicitly authorize that source/history destination and payload. The
exec never starts: no remote push or PR update. An explicit question to hide is
pending for that exact11file,507insertion/7deletion payload and prepared existing
Draft PR573 body only. publication-payload.json owns its SHA/body hash. Do not
retry through another mechanism, expand it with these later records, or treat it
as authorization for SARIF, merge, tag or release. Local source remainsf6c722c;
these later evidence documents are separate uncommitted updates. Infrastructure
mutation continues; Presentation starts only after its successful completion.

## 11:30 JST release continuation checkpoint

The remaining mutation driver is now run-remaining-mutation-02.ps1, owned hidden
PowerShell38708 -> dotnet31952 -> Stryker16812. The old session32171 ended without
a result and is not polled or represented as a threshold failure. Its evidence is
retained. At11:24 four newly created testhosts confirm that Infrastructure02 is
still executing. No final receipt/report or Presentation start exists yet. Do not
start a duplicate run or infer termination from a quiet log. The current ledger
still has four pending categories: the two mutation tiers, candidate-ref CodeQL
processing/open-alert readback, and final input/freshness validation.

PR573 was read back at11:17 as OPEN/Draft/headcf572205, with its old stopped body.
The exact local f6c722c source/body approval remains unanswered; no push, PR update,
SARIF upload, merge, tag or release is attempted. Later evidence documents remain
uncommitted and outside that frozen payload. hide's question about remaining tag/
release work is a status request, not permission for those external actions.

Independent Issue530 is now locally committed at76bff488c0b850d4e56e49aaa47460e29a1cfa8c,
with69files and clean worktree. It has selected consumer/coverage/actual-control
proof and no required independent-review correction. Application980/982 separates
two unchanged-base fixture failures; Presentation436 distinct cases pass across
recorded attempts. The latest guarded non-input-desktop control probe passes96
checks. Physical interaction/visible acceptance and publication remain separate.
See that worktree's docs/quality/2026-11-24-pointer-entry.md and handoff. It is not
added to the frozen beta.4 candidate and is not an extra release prerequisite.

Publishing inspection confirms the provisional a111464 package cannot be promoted
as the final main package: executable ProductVersion and the manifest identify the
source commit. Existing588-file/PRI/notice/hash preflight remains reusable evidence
of unchanged content, with final accepted-main generation and actual final-payload
launch still pending. The canonical publisher creates no tag, release or App process.
The existing settings resolver uses Environment.GetFolderPath(LocalApplicationData),
so merely overriding an environment variable cannot establish private settings.
Prepare a bounded unmodified-payload startup observation on a named non-input desktop,
with exact owned-process identity, no input/desktop switch, no normal-close settings
save, and before/after settings hash checks; inspect startup writes before using it.
That launch procedure is not yet implemented or executed and is not proof of normal
shutdown or visible acceptance. Normal settings must never be restored/overwritten.

## Provisional-payload startup procedure — 11:44 JST

The bounded unmodified-payload procedure is now prepared and exercised, superseding
only the earlier unimplemented-procedure status. It does not promote the provisional
package to a final accepted-main package. Product source comparison
`git diff --name-only a11146420ed1e9b0a6e1cc046e2037ec73a327db f6c722c9b0ae423f86a9b9da3236905af60fb352 -- src`
returns no paths, so the inspected startup/settings boundaries match the pilot.

Startup reads settings and pane directories; placement tracking remains in memory.
Settings rendering suppresses choice forwarding. The normal-close StopAsync path
saves placement, so this bounded startup procedure sends no close or input message
and ends only its newly created child through the retained process handle. No normal
settings file is replaced, restored or redirected. A task desktop is created without
SWITCHDESKTOP rights. Only WM_NULL responsiveness is queried on PID-qualified windows.

Task tooling lives in the evidence directory as `verify-unmodified-startup.ps1` and
`ReleaseStartupDesktop.cs`. PowerShell syntax validation and native helper compilation
pass. First file-write construction failed because nested here-string terminators
conflicted; no script/App resulted. Self-review corrects two distinct observations,
not two windows in one sample, and preserves the earlier helper/script versions.

- Pilot01 verifies all588files/299625285bytes, then fails in the observer before a
  startup claim. Its first enum call has no window/error detail. The owned child
  PID41748 is terminated; settings bytes/hash/write-time and the input desktop match.
- Pilot02 retains the same full byte/hash checks and retries an empty enumeration
  within the35-second observation bound, then fails on the supplemental external
  thread-desktop query. The owned child PID19388 is terminated and settings/input
  desktop remain equal. Directory/reparse checks are batched once across the same
  complete payload; no file/hash check is removed. Its whole command takes about11.4s,
  versus about225.4s for01's repeated ancestor filesystem checks.
- Pilot03 succeeds at11:44:08 with child PID5896. All588files again match the manifest.
  The same WinUIDesktopWin32WindowClass handle is visible and WM_NULL-responsive on
  the uniquely named non-input desktop at two samples more than3seconds apart.
  First observed response is480.5511ms after the observer's timer begins; final
  observation is3631.1807ms. These are bounded observation timings, not startup-to-
  usable-pane latency or a repeated performance distribution.
- Supplemental GetThreadDesktop reports access-denied5 for the other process's
  thread. This result is preserved. Desktop membership is independently established
  by the explicit private-desktop enumeration, whose documented contract enumerates
  windows associated with that handle. The optional thread query does not provide
  successful evidence. See [Microsoft EnumDesktopWindows](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdesktopwindows)
  and [GetThreadDesktop](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getthreaddesktop).
- Pilot03 intentionally terminates only that owned child; wait result0 confirms it
  exits, and later process inventory confirms its absence. Exit code0 is supplied
  by TerminateProcess and is not evidence of normal shutdown. The task desktop
  closes. Input desktop is Default before/after; normal settings SHA-256 remains
  561932A6530A6317499BAB59AD0735C8469869A15C6E6DA9BE3507B37AF557CE,623bytes, with the
  same write time and directory inventory. No SwitchDesktop or SendInput is called.

Exact pilot command (the suffix01/02/03 selects the separately preserved receipt):

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta4-20261124/verify-unmodified-startup.ps1 -ManifestPath D:/NeNeCommander/outputs/release-beta4-20261123/package-preflight/release-manifest.json -PayloadDirectory D:/NeNeCommander/outputs/release-beta4-20261123/preflight-extracted/NeNeCommander-0.1.0-beta.4-win-x64 -ExpectedSource a11146420ed1e9b0a6e1cc046e2037ec73a327db -ResultPath D:/NeNeCommander/evidence/release-beta4-20261124/provisional-startup-03.json
```

Pilot03 command exits0; its script SHA-256 is
CCE8154809DF7E206E0783E8009D42D746382445A74F3F5F9584DD520F3AAD68 and helper SHA-256
E1692155012D1F124F7D0EF9F7A9633FEA76514B3ABF91CBF11B79B2FFBF8080. Original failed
versions, every command receipt/log and raw runtime receipt are retained. No visible
acceptance, physical input, complete pane loading, normal shutdown or official final
release-package launch is claimed. The final accepted-main package still needs its
own exact-source manifest/hash/extracted-payload observation after generation.

At11:44 the original Infrastructure02 driver38708/31952/16812 remains active with
new testhosts; no final report exists and Presentation is not started. Frozenf6c722c
public push approval remains unanswered. No public-source/PR/SARIF/merge/tag/release
write is performed. The four later documentation changes remain uncommitted and
outside that payload. No product test or CI is repeated for this tooling/document work.

## Infrastructure mutation completion — November24 12:14 JST

The original owned remaining-mutation02 driver completed Infrastructure with exit0
at12:14:14. The exact command from tests/NeNeCommander.Infrastructure.Windows.Tests was
dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Infrastructure.Windows.csproj --break-at 90 --output D:/NeNeCommander/evidence/release-beta4-20261124/mutation/NeNeCommander.Infrastructure.Windows-02 --skip-version-check --concurrency 4.
Elapsed mutation time is02:45:41.1199557; reported score93.73% exceeds90%.
The raw report, full log, command receipt and independently counted status summary
are retained. The original interrupted attempt stays recorded; no threshold/config
relaxation or replacement of failed workflow45195559902 is claimed.

The sequential driver rechecks all1246 source/proof input hashes before restoring
Presentation. Its locked restore exits0 at12:15:56. Presentation starts with403
initial tests, creates2543 mutants (205compile errors,19no coverage,299ignored by
the existing block filter,2020to test) and remains running at12:23. The existing
Stryker compile-error safe-mode message for WindowAdjustmentKeyHintPresenter.Present
is retained, not hidden. No final Presentation score or complete release proof is
claimed. The frozenf6c722c public-source approval remains unanswered; no push, PR
change, candidate-ref SARIF, merge, tag or release occurs.

## Remaining mutation complete — November24 12:35 JST

Presentation mutation02 exits0 at12:35:29, with93.97% against90%, elapsed00:19:31.4000387.
The exact command from tests/NeNeCommander.Presentation.WinUI.Tests was
dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Presentation.WinUI.csproj --break-at 90 --output D:/NeNeCommander/evidence/release-beta4-20261124/mutation/NeNeCommander.Presentation.WinUI-02 --skip-version-check --concurrency 4.
The original driver then finishes at12:35:30 after verifying all1246 input hashes;
remaining-mutation-complete-02.json records unchanged=true. The report status counts
and hash are independently summarized in presentation-mutation-02-summary.json.
The12:26 runner retries and compile-error safe-mode messages remain in the original
log/report; no rerun was started to overwrite them.

The four required mutation thresholds are now met: reused Domain97.47%/95%,
Application96.5095986%/95%, Infrastructure93.7334041%/90%, Presentation93.97%/90%.
Original workflow45195559902 remains failed; this is reconciled stage evidence,
not an altered CI conclusion. Existing successful canonical/audit/adversarial
proof and new Application proof retain the previously recorded input mapping.
No repeated full gate or mutation run is justified by documentation/commit changes.

Release assessment remains pending: the frozenf6c722c public push is still awaiting
the explicit approval already requested after automatic-review rejection; candidate-
ref CodeQL processing/open-alert readback, accepted-main package generation and
exact extracted startup proof, final freshness, tag and publication are not done.
The provisional588-file pilot is not promoted to an official package. No external
write or newly launched product/CI verification accompanies this evidence update.

## Independent final stage reconciliation — 12:42 JST

Read-only review independently re-counts the raw four-layer reports:
Domain97.474747% (reused), Application96.509599%, Infrastructure93.733404% and
Presentation93.967631%. All new status counts/report hashes and receipt exits match.
All1246 current input hashes match; exactly7 Application test inputs differ from
the old1243, with no deletion. All16 reused stages match successful entries of the
original report. All3242 Application mutant semantics/positions/source match;
35of115 previously undetected mutants are killed by the added tests, with no
detected-to-undetected regression. The reviewer requests no mandatory correction.

The supported conclusion is **all mutation thresholds met and local stage evidence
complete; overall release pending**. Original workflow45195559902 remains failed.
CodeQL diagnostics/combined-evidence limits and runner retry/safe-mode logs remain
explicit. Earliest evidence expiry remains November27 23:34:56 JST. Full review scope
and pending release steps are in independent-complete-stage-review.json. No product,
test or CI is rerun by the review. The frozenf6c722c commit is unchanged and all
later evidence documents remain outside the exact public payload awaiting approval.

## Explicitly approved public update — November24 12:55 JST

At12:50 hide asks what response is needed. The exact scope is restated as the public
repository hideyukiMORI/NeNeCommander, frozenf6c722c, eleven files (seven Application
tests/four documents), and the prepared current PR573 body. The body is refreshed
locally to the already verified12:42 results; the original body/payload stay retained.
Current body SHA-256 is6DAFCE39A122BF29D0A9D19B714116866CA481DF75A5A3239BEF66F6C320454F.

hide then explicitly says「送信して」. The previous automatic-review hold is resolved
for exactly that payload. Before push, current head, old remote, exact eleven-path
delta and body hash all match. The first orchestration construction raises a
JavaScript ReferenceError before any shell/public action; corrected construction
uses an explicit PowerShell refspec value. The approved git push succeeds without
force: cf572205 to f6c722c on chore/571-preview-beta4.

The GitHub connector first rejects the wrong pr_number parameter spelling without
a write, then the correctly shaped request returns403 Resource not accessible by
integration. The existing authenticated CLI applies the same approved body through
gh pr edit573 --repo hideyukiMORI/NeNeCommander --body-file
D:/NeNeCommander/evidence/release-beta4-20261124/pr-body-resumed-draft-current.md.
Readback at12:55:34 confirms OPEN/Draft, headf6c722c, basemainaf17755, exact normalized
body match and no status checks. Further read-only12:56 query finds no action run
forf6; latest CodeQL remains oldd688d58/analysis2230861543. No new CI is inferred.

Receipts: publication-authorization.json, approved-push-01.json/log,
approved-pr-body-01.json/log, approved-pr-readback-01.json and
approved-publication-complete.json. Later local evidence documents and#530/#531/#544
source remain outside that push. No SARIF upload, Ready transition, main merge,
tag or release is performed. The overall release still needs its remaining evidence.

## Approved candidate CodeQL registration — November24 19:31 JST

At19:25 hide explicitly replies「解析結果を送信して進める」to the exact
f6c722c/2.42MB SARIF/public repository question. The original SARIF and prepared
request hashes match081CD2DC...60EA96E andEA66651F...1CCC8D0, and live PR573
head/ref/repository match the approved plan before submission. The connector has
no SARIF upload capability, so the existing authenticated GitHub CLI sends only
that prepared request:

```powershell
gh api --method POST repos/hideyukiMORI/NeNeCommander/code-scanning/sarifs --input D:/NeNeCommander/evidence/release-beta4-20261124/candidate-sarif-upload-payload.json
```

Exit0 returns upload1f82c4c6-e7f2-11f1-9dea-732d604e522d. Readback reports
processing complete, analysis2237915842 on the exact f6c722c branch,164rules,
0results, empty processing error/warning, and0open branch alerts. Raw authorization,
response, receipt, processing/analyses/alerts and candidate-sarif-complete.json are
retained. No new product test, CI, source push or PR-body edit accompanies this step.

This result remains a supplement combined with unchanged-production CI2230861543.
The original56compiler/138extractor errors and14nullable notices remain explicit;
GitHub processing success does not prove complete extraction. The original failed
deep-review workflow is not relabeled. Accepted-main integration/package/exact
extracted startup/freshness/tag/publication remain unperformed; overall release
assessment remains pending. Current authorization does not add those operations.

## Accepted main and public beta.4 — November24 20:43 JST

hide explicitly approved exact published PR573/f6c722c integration, final package
and startup verification, then the tag and GitHub prerelease. The immediate20:06
pause for Nib measurement was respected; no build/test was running. hide resumed
at20:26. A later20:33 status message mistakenly said the pause remained in force;
it was corrected immediately and did not trigger a second approval request.

The final PR body records all reconciled stage results and limits. Connector writes
returned403; the existing authenticated CLI applied the same authorized metadata,
Ready transition and SHA-guarded squash. PR573 merged at20:35:33 as
39090397998ba696a4d8b5d5b5a91ea975d45426. Main was clean before fast-forward and
remained clean afterwards. Its complete tree8a91630d144ceb5156995723dd062227bb67ce42
exactly matches acceptedf6c722c. Later feature work is absent. Issue94 is completed
by hide's November19 report; exact selected scale and independent captures remain
unrecorded. The first-preview exception is not used for this release.

Canonical packaging, exit0:

```powershell
pwsh -NoProfile -File ./eng/publish-release.ps1 -OutputDirectory D:/NeNeCommander/outputs/release-beta4-20261124/package-final-01 -WindowsSdkLicensePath D:/NeNeCommander/evidence/issue-434/windows-sdk-license.rtf
```

The clean accepted main is the source. Locked restore, Release/self-contained/CIB
publish and pinned original SDK-license cache succeed. No product source changes,
new dependency, new gate or repeated full/deep test run are introduced. The earlier
failed workflow45195559902 is still failed; the stage-by-stage acceptance is in
release-acceptance-final.json, including the supplemental CodeQL extraction limits.
Current owned open alerts are0; the conservative96h expiry remains November27
23:34:56 JST and was checked before tagging/publication.

Final verification commands, each exit0, run from the accepted main. `$e` below
means D:/NeNeCommander/evidence/release-beta4-20261124 and `$p` means
D:/NeNeCommander/outputs/release-beta4-20261124/package-final-01:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta4-20261123/verify-candidate.ps1 -Candidate $p -RepositoryRoot C:/Users/info/WORKS/NeNeCommander -ResultPath "$e/package-final-01-verification.json"
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta4-20261123/verify-pri-locales.ps1 -Candidate $p -RepositoryRoot C:/Users/info/WORKS/NeNeCommander -EvidenceDirectory "$e/pri-final-01"
pwsh -NoProfile -File "$e/verify-extracted-final.ps1" -Candidate $p -Destination D:/NeNeCommander/outputs/release-beta4-20261124/extracted-final-01 -ExpectedSource 39090397998ba696a4d8b5d5b5a91ea975d45426 -ResultPath "$e/extracted-final-01-verification.json"
pwsh -NoProfile -File "$e/verify-unmodified-startup.ps1" -ManifestPath "$p/release-manifest.json" -PayloadDirectory D:/NeNeCommander/outputs/release-beta4-20261124/extracted-final-01/NeNeCommander-0.1.0-beta.4-win-x64 -ExpectedSource 39090397998ba696a4d8b5d5b5a91ea975d45426 -ResultPath "$e/startup-final-01.json"
```

The extraction verifier changes only the allowed task-directory date from the
original script; both hashes and that exact change are recorded. ZIP and payload
contain588matching files;1687tracked source inputs,10lock files,21packages/51original
notice files,125theme attributions and132compiled XAML resources reconcile.
Each of en-US/ja-JP/zh-Hans has547matching PRI strings. Fresh extraction matches
all299625309payload bytes. The archive is113960879bytes with SHA256
7162f3f8a60ed84de790144e1f7aba6d887cbbe0a105923e3da4ba0451cbc4c9.

Unmodified extracted App16300 creates a responsive WinUI window on a unique
non-input desktop; the same window responds at two samples over3s apart. Only the
owned child is terminated and its desktop closed. Normal settings hash/bytes/mtime
and directory listing, and the Default input desktop, remain unchanged. The first
observed response663.4826ms is a bounded observer sample, not a startup performance
benchmark. One empty-enumeration retry and the unavailable secondary thread desktop
query(error5) are retained. No physical input, normal shutdown, complete pane
loading, all live providers or new human visual acceptance is inferred.

The strict final working-tree hash guard stops before tag creation on.editorconfig.
All1246candidate hashes still match the frozen proof. Against main,1233raw hashes
match; the remaining13files differ only by CRLF/LF bytes. These are text configuration,
XML or scripts; no C# production/test body differs. Byte-normalized equality and
every original hash are saved in main-proof-input-line-endings.json. The complete
Git tree remains identical. Reuse is accepted on this explicit mapping, not by
rewriting files or claiming1246raw main hashes match. An initial OrderedDictionary
summary printed null fields; the saved JSON was correct and independently read back
before the guarded acceptance/tag step.

The annotated tagv0.1.0-beta.4 peels to3909039 and was pushed without overwriting
an existing tag. The prerelease was published at20:42:46 JST with exactly ZIP,
release-manifest.json and SHA256SUMS.txt. The final notes disclose schema6 and
older-version rejection, unsigned status, evidence limits and excluded new features.
All3published assets were downloaded again at20:43:24 and exactly match the accepted
bytes/hashes. Commands:

```powershell
git tag -a v0.1.0-beta.4 39090397998ba696a4d8b5d5b5a91ea975d45426 -m 'NeNe Commander Preview 0.1.0-beta.4: 検証済みWindows x64配布'
git push origin refs/tags/v0.1.0-beta.4
gh release create v0.1.0-beta.4 "$p/NeNeCommander-0.1.0-beta.4-win-x64.zip" "$p/release-manifest.json" "$p/SHA256SUMS.txt" --repo hideyukiMORI/NeNeCommander --verify-tag --prerelease --latest=false --title 'NeNe Commander Preview 0.1.0-beta.4' --notes-file "$e/release-notes-final.md"
gh release download v0.1.0-beta.4 --repo hideyukiMORI/NeNeCommander --dir D:/NeNeCommander/outputs/release-beta4-20261124/download-final-01
```

[Published release](https://github.com/hideyukiMORI/NeNeCommander/releases/tag/v0.1.0-beta.4).
The release source remains3909039; this later documentation does not replace its
manifest or package. Final receipt files retain exact paths/arguments/logs. Release
worktree/output cleanup is performed only after this evidence is committed and
integrated and unique results are preserved. Other unfinished feature copies remain.
