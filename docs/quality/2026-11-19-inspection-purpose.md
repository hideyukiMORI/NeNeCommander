# 用途別のファイル検査と削除能力 — 2026-11-19

Status: implemented; coverage corrected and native F6 smoke passed; new CodeQL test finding corrected; final deep review44479037554 running; integration pending.

Issue [#476](https://github.com/hideyukiMORI/NeNeCommander/issues/476) follows the bounded
[#470](2026-11-19-f6-provider-phases.md)/[#473](2026-11-19-f6-shell-query-observation.md)
observations. [ADR-0083](../adr/0083-operation-specific-inspection.md) owns the contract.
This change does not establish total F6 latency, predict savings, or close #453.

## Invariant and canonical mechanism

The one `IFileOperationPort.InspectAsync` now requires `FileInspectionPurpose`.
Copy/move/rename/create-directory request IdentityOnly; ordinary Delete requests Deletion.
Identity capture, drive/reparse admission and later effect revalidation remain in their existing
provider bodies. Windows IdentityOnly uses its existing ordinary I/O scheduler and makes no
recycle Shell call; Deletion retains its existing STA and capability query. WSL preserves its
identity and reports its existing PermanentOnly only for Deletion. Routers forward the purpose.
There is no compatibility overload, cache, ambient flag, second port or new native worker.

`DeletionCapability.NotRequested` explicitly grants no ordinary-deletion permission.
The gateway requires a complete batch of inspected capabilities before any ordinary deletion
effect or progress, even when confirmation is supplied and a later item is uninspected.
Windows/WSL also refuse a directly submitted uninspected ordinary deletion before scheduling.
The one `IsInspected` predicate recognizes Recycle/PermanentOnly. Windows separates admission
from private scheduling to stay inside CS-013; its existing scheduler and action are unchanged.

Composite move keeps the existing VerifiedCopy receipt authority and all receipt verification.
Snapshot annotation/equality, conflict continuation and resume preserve the unrequested value;
a provider cannot replace it during resume. Transfer preflight, native recycle behavior,
receipt internals, settings/schema, dependencies and UI are unchanged. ADV-008/018 and the
security/filesystem/glossary documents describe the same boundary.

## Verification scope and results

ADR-0061 shared-port and destructive-admission scope requires one explicitly dispatched deep
review on the complete candidate. It includes the full canonical gate, record closure, coverage,
adversarial repetition, mutation and CodeQL. Quality CI must not duplicate it. That wider proof
is pending here; local focused results do not substitute for it. Rules ARC-001/005/007/009,
CMD-004, FS-002/005, SEC-003/011 and CS-002/013 apply. No waiver or threshold change.

| Proof | Observed result |
| --- | --- |
| Behavioral red after signature migration | Application 6 fail/2 pass; Infrastructure 3 fail/1 pass, both exit2 |
| New Application purpose/snapshot/resume/receipt cases | 13 pass, exit0 |
| Infrastructure purpose/router/ordinary refusal/receipt cases | 23 pass initially; two incorrectly prepared positive fixtures corrected and both pass |
| Existing gateway/transfer/lifetime boundaries | Application 97 pass, exit0 |
| Existing Windows/WSL/recycle/transfer/verified boundaries | 353 pass; one reflection argument fixture failed, corrected, then that case passes |
| Final private Windows scheduling extraction | 48 selected purpose/deletion/recycle/verified boundary cases pass, exit0 |
| Application/Infrastructure tests and App/Presentation consumer compilation | Release builds zero warnings/errors; locked restores exit0 |
| Changed C# canonical whitespace check | exit0, with a workspace-load warning |
| CS-013 Inspect after corrections | exit1, same26 path/metric/value/limit findings as accepted #467; no new finding |
| Independent read-only design/implementation review | no required correction; does not replace executable proof |

Counts overlap and are not a unique total. A successful affected case is reused unless its
relevant implementation changes. The private scheduling change justified only its48 directly
affected cases; the reflection fixture correction justified only that failed case. CRLF-only
normalization does not change tested C# semantics. The later deep review verifies the final inputs.
No live WSL distribution, UNC server, mapped drive or native recycle UI walk was newly run.
Existing native policy is unchanged; opt-in environmental proof must remain explicitly distinct.

All commands, arguments, start/end times, exits and full logs are retained under
`D:/NeNeCommander/evidence/inspection-purpose-20261119/`. Executed drivers are
`run-red.ps1`, `run-green.ps1`, `run-green-2.ps1`, `run-green-3.ps1`, `run-green-4.ps1`,
`run-focused.ps1`, `run-complete.ps1`, `run-style.ps1` and `run-deletion-schedule.ps1`.
Each is invoked with `pwsh -NoProfile -File <evidence>/<driver>` from the task worktree.
The matching `*-commands.json` records contain the exact selected test filters, including the
failed attempts. Principal commands are:

```powershell
dotnet restore NeNeCommander.slnx -p:Configuration=Release --locked-mode
dotnet build <affected-test-or-App-project> -c Release --no-restore -m:1 -nr:false
dotnet test --project <affected-test-project> -c Release --no-build --no-restore -- --filter <recorded-filter>
dotnet format whitespace NeNeCommander.slnx --verify-no-changes --no-restore --include <changed-C#-paths>
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

## Attempts and corrections

- The first red-test build used a nonexistent confirmation factory. It was corrected to the
  existing construction route before behavioral red results were accepted. A later new test
  assigned the void `Assert.IsNotNull` result; it now uses `Assert.IsInstanceOfType`.
- The analyzer required conditional return expressions for two new provider guards. No analyzer
  was suppressed. Initial complexity findings were28, then27; the WSL provider test was split
  by responsibility and Windows recycle/scheduling bodies extracted privately. Final26 findings
  exactly match the unchanged existing set in `complexity-comparison.json`.
- The first two real-receipt positive fixtures omitted the frozen transfer target and were
  correctly rejected with Verification. They now use the existing gateway to establish the full
  preflight/receipt route; both Windows and WSL source providers pass. Receipt rules were not relaxed.
- The existing WSL-to-Windows reflection null-guard fixture did not supply the new purpose type.
  Adding that argument restores its original check of every required reference parameter.
- The first commit hook rejected a boolean mode parameter in the new confirmation test under
  CS-002. The two test rows now name Confirmed/Unconfirmed explicitly; both pass after their
  zero-warning/error rebuild. No rule was weakened. Exact commands/exits are in `candidate.json`.
- An over-broad `dotnet format` attempt included semantic analyzers rather than the canonical
  whitespace command. It reported new-file CRLF mismatches and unchanged XAML-wired callback
  IDE0051/IDE0060 findings. The full failed log is retained. New C# line endings were normalized;
  the canonical changed-file whitespace checks pass. No unrelated callback or analyzer setting
  was changed and no full semantic-format success is claimed.
- A read search used an unsupported literal wildcard path and was corrected to the actual tests
  directory. The initial local `perf/476-inspection-purpose` branch was renamed to
  `refactor/476-inspection-purpose` before publication to follow the allowed type vocabulary.
- GitHub connector Issue creation returned403 without creating an Issue. Authenticated `gh`
  created #476; connector reads work. This is not automatic approval rejection. No credential was
  read or recorded. Write operations use that established CLI fallback.

## Bounded comparison of the changed boundary

The predeclared `comparison-plan.md` uses baseline main9268717 and candidate733fb31, both Release
IL built with the same pinned SDK/configuration. This does not compare the previous beta.2
ReadyToRun payload with an IL build. Baseline's449 exported source/config/lock files still match
its retained Git archive after compilation. Candidate's1013 verification inputs are unchanged.
All six selected product DLL hashes are recorded in `comparison/product-binaries.json`; source
path/newline/debug metadata can differ between builds, so binary equality is not claimed for
unchanged source. There is no product instrumentation or source change for this diagnostic.

The established #470 recorder/typed Move/owned-fixture/safety/no-other-operation/self-proof is
reused. Its only adaptations are the explicitly forwarded Inspect purpose for the changed
contract, dedicated output root, fixed block selector and sample count. The two standalone
diagnostic builds have zero warnings/errors and both forwarding self-proofs pass. Each fresh
process performs one excluded warmup and three measured moves in the fixed ABBA order below.
Six measured samples per variant and four warmups were planned before running; none were added
or retried after seeing results. Each uses fresh tiny SQL/TXT source pairs and an empty target
on D, with the same contents/hashes as #470. Preparation hashes and selection are outside timing.

| Block | Sample | Complete non-UI Move/refresh ms | Both Inspect calls ms |
| --- | --- | ---: | ---: |
| control-01 | sample-01 | 44.5223 | 37.5152 |
| control-01 | sample-02 | 44.4178 | 36.9129 |
| control-01 | sample-03 | 46.6596 | 38.9394 |
| changed-01 | sample-01 | 8.4309 | 0.1786 |
| changed-01 | sample-02 | 7.5380 | 0.1291 |
| changed-01 | sample-03 | 7.6367 | 0.1449 |
| changed-02 | sample-01 | 7.6621 | 0.1128 |
| changed-02 | sample-02 | 8.0437 | 0.1787 |
| changed-02 | sample-03 | 7.6971 | 0.1498 |
| control-02 | sample-01 | 46.4428 | 37.5887 |
| control-02 | sample-02 | 44.9760 | 37.3662 |
| control-02 | sample-03 | 46.1333 | 39.2614 |

Whole-call medians are45.55465ms and7.67960ms; Inspect medians are37.55195ms and0.14735ms.
Warmup whole-call values in block order are126.0293,42.2046,40.6307 and129.0716ms and are excluded.
All16 moves require two atomic effects, no not-transferred item, source0/target2, left0/right2,
two progress points and both final hashes. An independent Python pass verifies original integer
ticks, raw/final equality, exact order/outcomes, disjoint/contained spans, progress position,
four distinct process IDs, all144 spans/32 progress points/32 target hashes. It succeeds without
editing the captured data. Per-phase min/median/max and all rows are in `comparison/analysis.json`.

These observations support less work in this non-UI Inspect boundary for this fixture. ABBA
does not control all JIT/cache/OS/background contention. The diagnostic excludes physical input,
CommanderSession/dispatcher/presentation/XAML/display and uses warm, tiny same-volume data.
It establishes no percentile, CPU cost, inner-API contribution, capture overhead subtraction,
cold/cross-volume/large-tree/network/WSL behavior or cause/resolution of hide's reported pause.
Do not subtract #470/#473 observations or add phase medians. #453 remains open.

Commands, all exit0:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/inspection-purpose-20261119/comparison/prepare-build.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/inspection-purpose-20261119/comparison/run-measure.ps1
python D:/NeNeCommander/evidence/inspection-purpose-20261119/comparison/analyze.py
```

The build driver exports the three baseline projects/configuration from9268717, performs their
locked restore/Release build, builds the two package-free diagnostic projects, runs self-proof,
and creates only the16 declared fixtures. The measurement driver invokes `dotnet <variant>/
PhaseProbe.dll measure <block>` once per block and stops on any failure. Full commands/exits and
input hashes are preserved beside the drivers. No UI tool or settings restoration was needed.
At15:39 JST the four probes4900/14260/42628/12068 had ended, normal38360 remained and the original
218-byte settings SHA256 still equaled `ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.
All36 frozen diagnostic files remained byte-identical. At15:40, all682 output files/65,022,426 bytes
were copied into the permanent `runs/` archive with size/SHA256 equality. An initial aggregate
byte-count field was null; independent summation corrected only that summary, retaining the first
record. The individual file checks and captured measurement were unaffected.

## Initial deep-review failure and dispatch correction

Run44473140316 failed at15:50:44 JST; the complete workflow ended at15:54:04. Its test run
passed2094 cases with16 explicitly unexecuted live skips and no failed test. Domain branch
coverage passed100%; Application reported99.92657856%, below its unchanged100% threshold.
The gate stopped there, so later coverage/adversarial/mutation success is not claimed. CodeQL's
workflow step completed separately; later API readback found one test-code finding, corrected below. The failed log and
downloaded `deep-review.json` are retained in `ci-first/` and `deep-review-failed.log`.

A local Application-only coverage run reproduced the single missing branch at InspectAllAsync's
switch dispatch. The two arms tested FileInspectionSucceeded twice, one with a guard. The
generated dispatch included the unknown-outcome fallback; all declared success/failure,
purpose and capability combinations were already exercised. The success arms now form one
case with the identical purpose/capability guard inside it. This removes duplicate type
dispatch without loosening batch admission, changing the closed variants, adding a forged
outcome test or excluding code. The existing unknown-outcome exception remains.

`pwsh -NoProfile -File <evidence>/run-coverage-correction.ps1` rebuilds Application tests with
zero warnings/errors, passes757 cases and Application branch coverage100%, then verifies the
changed C# whitespace. Each exit is0. `pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect`
returns1 with exactly the same26 existing path/metric/value/limit findings. The XML reports,
commands, times and comparison are retained. Of1013 frozen inputs, only FileOperationGateway.cs
changed; `inputs-corrected.json` owns the new candidate. A corrected deep review is required
because the initial full gate failed and the production input changed; no quality workflow
or unrelated local suite is added.

The bounded timing table above remains an observation of733fb31 before this dispatch correction.
Its raw samples are retained unchanged. It is not a timing claim for the final corrected binary;
no repeated benchmark is needed to establish the unchanged identity/deletion contract.

## CodeQL test-code finding and correction, 16:17–16:18 JST

The initial workflow's CodeQL step success did not mean zero findings. Analysis2203018686
on733fb31 contained one result. API readback found open alert153, `cs/linq/missed-select`,
at InspectionPurposeBoundaryTests.cs:75. The loop immediately assigned the exception returned
by Assert.ThrowsExactly and then checked ParamName. The same finding applies to6499b0a,
whose test source was unchanged; no result was suppressed or dismissed.

The loop now directly asserts the returned exception's ParamName. It still invokes each of
the same five providers exactly once, requires ArgumentNullException and the exact `purpose`
parameter name, and stops on a failed assertion. It introduces no LINQ/deferred execution,
new test, production change or changed assertion. `run-codeql-correction.ps1` records the
zero-warning/error Infrastructure test build, the one affected five-provider case passing,
and changed-file whitespace; every command exits0. The local change shortens one method
without adding any branch, parameter or nesting, so the prior complexity evidence remains
applicable without a repeated repository measurement.

At16:18:15, cancellation of ongoing run44476340651 was requested because the final test input
had changed and its known CodeQL finding remained unresolved. Its eventual partial/terminal
record must be retained; it is not accepted as a complete successful review. The repository's
existing deep-review workflow is the required complete-candidate route and includes CodeQL;
there is no separate CodeQL-only workflow. One final dispatch is needed for this still-unaccepted
shared/destructive-boundary change. No parallel quality workflow is requested.

Only this test file changed against inputs-corrected.json; inputs-codeql-corrected.json records
all1013 final inputs. Product source/binaries and the native F6 smoke remain unchanged. The earlier
bounded timing still belongs to733fb31. CodeQL clearance and final deep-review acceptance are pending.

## Corrected native F6 smoke, 16:05–16:08 JST

After the Application-only dispatch correction, the final App Release build succeeded with
zero warnings/errors. `prepare-native.ps1` records the exact build command,6499b0a source,
App DLL SHA256 `1BB194243264CEE17AE669D15904A2C7298615DE74312DDCF64C84E5D62F5916`,
original settings, one46-byte TXT/one49-byte SQL and their initial hashes. A dedicated app
process11032/window12454356 used the ordinary keyboard/CommanderSession/gateway path.

The first batched address-focus/text/Enter attempt opened an existing C:\BUFFALO location;
no file effect was submitted there. Subsequent `ctrl+l`, input and Return were separated by
native readback and established the declared left/right owned directories. This corrects
setup and does not establish a product defect or attribute a cause to the key spelling.
Tab/Space/Down/Space selected the owned pair; moving focus with Up exposed both selection
marks before F6. Native readback then showed source0/target2, both panes loaded and
`移動が完了しました`. The raw screenshot/AX evidence is retained under `native-smoke/`.
This is one successful same-volume tiny-file case; no elapsed timing, physical-key fidelity,
progress-frame visibility, percentile or resolution of #453 is claimed.

After Alt+F4, the immediate inventory still listed the owned window; the subsequent inventory
confirmed its exit. At16:08:37, both destination sizes/hashes matched, source was empty,
process11032 was absent, normal38360 remained, and the original218-byte settings/hash were
unchanged. No restoration, deletion, dependency or product input change was needed. The two
new fixture files are preserved with byte equality, extending the previous682-file archive
to684 files/65,022,521 bytes; unchanged earlier preservation is reused.

## Integration state and limits

Draft [PR477](https://github.com/hideyukiMORI/NeNeCommander/pull/477) now contains568371f.
Its normal112-rule/24-adversarial-registration/security/commit-message hooks passed.
The initial [deep review44473140316](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/44473140316)
failed as recorded above. The corrected [deep review44476340651](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/44476340651)
ended cancelled at16:21:12 JST after the test-code correction made its inputs obsolete.
Its retained `ci-second-cancelled/security-deep-review/deep-review.json` records a passing
canonical gate, dependency audit, twelve adversarial project runs and Domain mutation97.47%.
The log records2094 passing tests/16 opt-in live skips and branch coverage Domain100%,
Application100%, Infrastructure94.53%, Presentation94.50%. Application mutation was interrupted;
this partial result is not a complete accepted review. The same test-code CodeQL finding
was still present in that source. Full logs and artifacts are preserved without discarding
the interrupted attempt.

The final [deep review44479037554](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/44479037554)
was dispatched at16:21:56 JST on568371f. Acceptance of its full gate, mutation and CodeQL
remains pending. The final1013 source/test/tool/dependency inputs are frozen in
`inputs-codeql-corrected.json`; these documentation updates do not invalidate them.
No parallel quality workflow or repeated local suite is added.

The dedicated worktree is `D:/NeNeCommander/worktrees/issue-476`; its disposable output is
`D:/NeNeCommander/outputs/inspection-purpose-20261119`. Inputs/logs and later CI artifacts belong
in the permanent evidence directory above. Worktree/output cleanup follows integration and
artifact preservation; neither is complete here. The normal application and published beta.2
are unchanged. PR402 remains a separate Draft pending hide's screen acceptance.

See the [progress report](../reports/2026-11-19-inspection-purpose-progress-report.md) and
[handoff](../handoffs/2026-11-19-inspection-purpose-handoff.md). The four earlier rejected cleanup
targets remain pending without retry; this change does not authorize retrying them.
