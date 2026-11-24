# Preview beta.4 release continuation — 2026-11-24

Status: in progress. Issue #571 / Draft PR #573.

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
