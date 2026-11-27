# Transfer conflict investigation — 2026-11-25

Status: resumed on November 26; internal owned-artifact cleanup accepted; source deletion, provider activation, full destructive acceptance and directory merge remain pending.

Issue #560; current branch feat/560-transfer-resumption on accepted main ad85de1.
Original feat/560-transfer-conflicts at15499d8 and every historical record remain.
beta.5 is published; selection and name filtering are integrated separately.
FileOperationGateway / its existing continuation and provider port remain the single
mechanism. A displayed choice must authorize only its exact operation and target.

## Initial findings (historical)

- Current conflicts carry target paths without target identities, and the UI forwards
  unqualified decisions. Qualifying the existing modal callback is the first bounded
  prerequisite; no destructive capability follows from it.
- Current plans and verification describe one exact root tree per source. Folder merge
  cannot reuse the verifier by ignoring extra target children, and directory consent
  cannot grant replacement permission for child files.
- ProviderStepOutcome's one optional effect is insufficient for verified staging,
  an old destination moved to a backup name, publication and retained residues.
- Existing ADR-0039/FS-005 still prohibit overwrite/merge. ADR-0095 is proposed and
  records unresolved security, metadata, capability, cancellation and residue decisions.

## Actual design consultation

run-design-01.ps1 invoked Claude Skill(design) from an authored interaction brief;
no repository source was sent. Receipt claude-design-01.json reports exit 0 at
12:58:22 JST. The skill provided project-access management only. No access grant,
revoke, external project edit/publication or Canvas call occurred; read-only tool
search found no design canvas tool. The written advice labels itself accurately.
The optional HTML is an unverified local sketch, not product or native visual proof.

The advice's safe default is adapted to the existing contract: initial Cancel stays;
its suggested initial Skip is not adopted. Source/target disclosure, disabled reasons,
known-conflict-only batch scope, separate merge/child-replace consent and honest
partial results inform the proposed design. No new styling or shortcut is approved.

## Native feasibility experiment

Permanent scripts/receipts: D:/NeNeCommander/evidence/transfer-conflicts-20261125.
Owned data only: D:/NeNeCommander/outputs/transfer-conflicts-20261125/native-publication-01
through native-publication-04. No normal settings, desktop input or user files were
changed. Each script refuses an existing root and keeps its outputs; no cleanup ran.

Exact commands, each from PowerShell:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-publication-01.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-publication-02.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-publication-03.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-publication-04.ps1
```

01 and 02 exit 1 at old-handle rename. 01 omitted the numeric operation trace; 02
adds it and records error 87 for relative SetFileInformationByHandle. Their outputs
are preserved and never relabeled successful. A failed extended call in these trials
is not proof of lock behavior because its argument form was not admitted.

03 exits 0: absolute-name SetFileInformationByHandle tests normal publication and
an injected late target. 18 checks pass. An open target denies new writers, ordinary
ReplaceFileW and FileRenameInfoEx POSIX replacement with error 32. Moving the old
handle to an unused backup succeeds. No-replace publication succeeds normally and
fails with error 183 for the injected occupant; that occupant is left unchanged,
with old and staged object identities retained.

04 exits 0: native NtSetInformationFile class 10 uses the exact parent handle and
simple relative names. 16 checks pass for normal and late-collision cases with the
same preservation result. The existing target and staging handles retain their
identities. This run does not repeat the POSIX experiment. Both successes ran on
Windows build 26300, x64, PowerShell's runtime 10.0.15, on this owned D-volume root.
They are API feasibility only: no claims about crash durability, final metadata/ACL
preservation, hostile ancestor traversal, cleanup, other filesystems or product behavior.

The conservative handoff intentionally has a gap between moving the old name and
publishing the new one. A late collision retains both artifacts and requires truthful
partial reporting. It is not yet selected for production; access-control preservation,
metadata/streams, capability and residue lifecycle remain unresolved.

Primary references reviewed:

- [ReplaceFileW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew):
  documented partial failure states and preserved metadata make a boolean insufficient.
- [FILE_RENAME_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_rename_info):
  Win32 rename structure and no-replace behavior.
- [FILE_RENAME_INFORMATION](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/ns-ntifs-_file_rename_information):
  native relative parent/name and access contract.
- [NtSetInformationFile](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/nf-ntifs-ntsetinformationfile):
  native class 10 and status result.

## Verification scope and next work

Only research/diagnostic files and proposal documentation are changed so far. No
product build, full regression, coverage, mutation, CI, public source/SARIF update,
PR, merge or release is performed for this research. Next implement and prove exact
modal-owner qualification under the existing contract, then resolve the proposed
native lifecycle before enabling a new destructive effect. Keep #560 open.
## Owner preparation implementation and additional research

The scoped implementation now changes existing conflict submissions only; no Replace
or Merge capability is added. ConflictDecisionSubmission carries the mandatory exact
OperationAwaitingConflict. DualPaneSession uses reference admission; ActiveConflictModal
retains that same state, and the presenter caches only that exact owner. Existing
callers are migrated with no unqualified overload.

ConflictModalView owns one native template instance per displayed owner. The existing
XAML content, resources, button order and initial Cancel policy move unchanged into
that template. Old controls retain closures over the old owner and old checkbox.
Current owner, current child and completed native render are checked before forwarding.
Owner changes are published before native side effects; a superseded Focus/Unloaded
render cannot write its old readiness back. Same-owner rendering preserves controls,
focus and scope. Native verification of these claims is still in progress.

Causal proof: owner-red-01 uses unchanged product 27b92cd and three delayed decisions
(Skip, KeepBoth, Cancel). Release locked restore/build exit0; the selected test exits2
with all3 cases failing because the later conflict becomes OperationCompleted.
owner-green-02 then passes14 related Application cases. After adding value-equal
foreign-owner and current-cancel/replay cases, owner-final-04 passes21 selected cases;
Application Release build remains0 warnings/errors. owner-consumers-03 passes10
Presentation conflict/mapper/repeat cases. owner-app-03 normal App Release build passes
with0warnings/errors. owner-complexity-03 returns1 with exactly the prior2findings;
no new declaration violates CS-013. This is not a zero-findings claim.

Independent read-only design review at13:11JST accepts the proposed boundaries with
no further required design correction. It specifically confirms owner caching and
native reentry need proof, and distinguishes native object-ID/path observations from
unmeasured bytes. In response, run-native-publication-05.ps1 adds exact competing-file
before/after identity and SHA256 comparisons, old-backup and staged/published-byte
comparisons on a new owned root. It exits0 with21checks. Earlier01–04 receipts retain
their original measurement scope. No ACL, metadata, crash or cleanup claim is added.

Native-controls preparation01 freezes186App inputs and4normal-product DLLs. Only
composition/project/diagnostic-lock substitutions and added observer files differ in
the copy; selected native code and XAML remain exact. Private settings and owned pane
files run on NeNeCommander560Conflict_20261125, never the input desktop. Proof remains
pending until actual results and final parity are recorded.
## Final exact-owner verification — 13:35 JST

The implementation prerequisite is complete locally. It does not complete Issue #560
or accept ADR-0095's proposed replacement/merge design. The earlier research-only and
native-pending entries above describe their time of observation.

Native-controls en-US attempt 1 fails after 28 checks because the observer assumed
GotFocus was synchronous. The product is unchanged for the correction: observer 02
waits for actual asynchronous GotFocus and separately uses a real Visibility property
callback to test synchronous nested Render. en-US attempt 2 then passes 43 checks.
Observer 03 adds localized button text/native accessible names, the scope label and
exact All-scope forwarding. en-US attempt 3, ja-JP attempt 1 and zh-Hans attempt 1 each
pass all 51 checks, exit 0. Earlier receipts and all three observer versions remain.

The actual product composition starts an owned Copy conflict, injects an occupied
KeepBoth candidate, observes a new preflight conflict, invokes all three old native
AutomationPeers and verifies the newer owner/controls remain untouched. Same-owner
render preserves controls, scope and focus. Current Cancel passes through the sole
gateway; closed controls cannot act again and both original file contents remain.
A separate adapter instance tests native Unloaded, asynchronous GotFocus and synchronous
Visibility callback reentry. These are native controls/callback evidence, not physical
keyboard or manual visual acceptance. The three launches keep ordinary settings hash,
length and timestamp unchanged and keep the input desktop Default. No desktop switching
or SendInput occurs; each bounded diagnostic process exits.

Final scoped commands run from the issue-560 worktree (full argument arrays and exit
codes are in owner-final-06.json; logs share each recorded label):

```powershell
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~ConflictSubmissionOwnershipTests
```

The five selected adversarial cases pass
three repetitions after category tagging, each exit 0. The Application build has zero
warnings/errors. No unrelated Application suite is repeated.

```powershell
dotnet restore tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~ViewCompositionOwnershipTests|FullyQualifiedName~CommanderWindowComposition'
pwsh -NoProfile -File eng/verify-record-closure.ps1 -RepositoryRoot D:/NeNeCommander/worktrees/issue-560
```

All exit 0: 36 selected Architecture cases, App/consumer build with zero warnings/errors,
and record closure with 156 abstract records / 143 closed visible roots / zero failures.
Changed C# files pass `dotnet format whitespace NeNeCommander.slnx --no-restore --include
<13 files listed in owner-format-05.json>` and the same command with
`--verify-no-changes`, both exit 0. The workspace-load warning is preserved.

Exact native commands:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-controls-01.ps1 -Locale en-US -Attempt 3
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-controls-01.ps1 -Locale ja-JP -Attempt 1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-controls-01.ps1 -Locale zh-Hans -Attempt 1
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/verify-owner-parity-07.py
```

Final parity covers 186 App inputs: 183 raw exact diagnostic copies and three declared
composition/project/lock substitutions whose original inputs remain unchanged. The
composition replacements reconstruct exactly. There are no CRLF-only differences.
All four frozen normal-product DLL hashes match both the loaded diagnostic copies and
the final normal App output. The 1,407 relevant source/test/build/tooling inputs are
frozen in owner-final-inputs-07.json. Independent final read-only review finds no
must-fix; its requested Architecture completion is satisfied above. No broad regression,
coverage, mutation, CI, source publication, SARIF, PR, merge, tag or release is claimed
for this bounded owner correction. The two pre-existing complexity findings remain.

Next save the change with normal Commit-mode hooks. Keep the frozen beta.5 and selection
publication approval holds separate. Retain all unfinished work and outputs; cleanup
is not yet eligible. Continue the explicitly proposed replacement lifecycle only after
its identity, ACL/metadata and truthful residue contracts are resolved and proved.

## Saved owner correction and further native design research

The owner-only correction is saved as `11e25f1eff50bffbc5c0bca092b7980066d1f42b`
with normal hooks: 112 normative rules, 24 registered adversarial cases, security,
whitespace and commit-message checks pass, exit 0. The worktree is clean immediately
after that commit. No public update is performed. All 1,407 relevant inputs remain
frozen; the research below changes only external diagnostics and documentation.

Exact additional commands (PowerShell; all use new owned D-volume roots and retain
all artifacts, with no privilege enabling, shared input, normal settings or user files):

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-security-06.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-inplace-07.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-streams-09.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-streams-10.ps1
```

Security06 exits 0 with ten fixture checks and separate observations, not a complete
security-preservation pass. Native parent-relative FILE_CREATE can apply a supplied
owner/group/DACL before bytes are written and refuses a second create of the same
stage. The inherited fixture's SDDL matches exactly. The protected fixture retains
its owner/group/ACEs and protection, but the AI control flag differs (PAI versus P).
Most significantly, an independent WRITE_DAC handle opens and tightens the held old
file's DACL despite its data-write/delete exclusion. Target ID and bytes stay the
same; staging retains the earlier DACL. A sharing lock is not a security freeze.
No SACL, label, resource-attribute, privilege or complete ACL-preservation claim follows.

Inplace07 explores content update on the exact target handle rather than object
substitution. It exits 0 with 49 checks across normal, pre-write cancellation, partial
write and concurrent DACL-tightening cases. A current-user-only backup and staging file
are created before any bytes; hashes prove original/desired default streams. Target
identity, current security, creation time and its existing named stream remain. The
partial case writes only the first 256 of 8,192 replacement bytes over the 4,096-byte
original: target is neither original nor completed, and source, old backup and staging
remain intact. This is a deliberate partial result, not rollback or atomicity proof.
A separate no-content-write cell creates a hardlink while an exclusive target handle
is held (error 0, link count 1 to 2). Thus this alternative affects an object with
possible aliases; single-name or alias-free consent cannot be inferred from that lock.
The initial production proposal excludes observed named streams/multiple links; this
experiment deliberately includes a stream to observe primitive behavior only.

Streams09 records valid native rows but has a diagnostic comparator error: an expandable
PowerShell here-string transforms the C# literal `::$DATA`, so unnamedOnly is false even
for the two default-only fixtures. Its completed receipt is not a valid classification
pass. Streams10 uses a literal here-string and explicit assertions. It exits 0: empty
and nonempty default-only files are recognized; nonempty Zone.Identifier, empty
Zone.Identifier and a custom named stream are all refused by the prospective default-
only classifier. Every eight-byte buffer returns error 24 and cannot authorize a file.
In one exclusive-handle cell, adding Zone.Identifier fails with sharing violation 32;
that single observation is not a universal stream or metadata freeze guarantee.

These findings are recorded in `replacement-decision-08.md` for independent design
review. Exact-object content overwrite with explicit target consent/private recovery
artifacts is a candidate only; late-alias semantics and crash discoverability are
open decisions. No new destructive primitive, capability, data-stream omission,
cleanup path or recovery promise is enabled in product code. ADR-0095 remains proposed.

Primary API references supporting interpretation (local receipts own observed results):

- [NtCreateFile](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntcreatefile)
  defines a relative parent/name and creation-time security descriptor; inherited
  behavior when no descriptor is supplied depends on the filesystem.
- [GetSecurityInfo](https://learn.microsoft.com/en-us/windows/win32/api/aclapi/nf-aclapi-getsecurityinfo)
  reads selected security information from a handle; READ_CONTROL and SACL access
  differ, and the call itself does not resolve concurrent security changes.
- [FILE_LINK_INFORMATION](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/ns-ntifs-_file_link_information)
  describes hardlink creation; its API contract requires no specific access rights
  for setting that information.
- [FILE_STREAM_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_stream_info)
  defines bounded name lengths and aligned next-entry offsets;
  [GetFileInformationByHandleEx](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getfileinformationbyhandleex)
  identifies class 7 and its no-stream/error behavior. No failed/short buffer is a
  successful empty-stream enumeration in the proposed admission rule.

## Accepted design and process-crash feasibility — 14:08 JST

Final independent review of replacement-decision-08.md revision12 at13:58:35JST has
no remaining design must-fix. Prior six findings and their corrections are retained:
mutation uncertainty before write/truncate, stream evidence through source deletion,
explicit object/late-alias consent, old-target backup revalidation and exact metadata
policy, crash mapping, and one receipt/phase table. ADR-0095 now integrates the accepted
policy with the filesystem/command/security/glossary documents. This authorizes scoped
implementation, not exposure of an unproved overwrite capability or folder merge.

`pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-native-crash-11.ps1`
starts six hidden owned PowerShell child processes and terminates each only after its
own exact checkpoint: before mapping, after mapping, after256-byte partial write,
after empty-source truncation, during final append, and after appended verification.
All six exit by requested process termination; no other process is stopped. The driver
and original child source remain in evidence. `python D:/NeNeCommander/evidence/transfer-conflicts-20261125/verify-native-crash-11.py`
exits0 with66 checks. Source/backup/stage/target bytes and lengths match each interrupted
phase; all four object IDs match the mapping wherever it exists. A half-written final
frame leaves the initial complete mapping readable and hash-verified. Before mapping,
no target mutation occurred. These are exact fixture/process observations, not power-loss,
ancestor, product implementation, successful cleanup or automatic-recovery proof.

`pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-design-overwrite-02.ps1`
exits0 at14:05:12JST. Observed tool calls are Skill(design) and one local Write only;
no repo Read, delegation, Canvas, access grant/revoke or remote project write occurs.
Authored brief hash and receipt are saved; advice is permanently copied as
overwrite-design-advice-02.md. The [interaction handoff](../design/2026-11-25-overwrite-contents-handoff.md)
records the adopted wording/placement and corrections to overbroad path/cancellation,
stale-callback, later-conflict and invented-result-modal recommendations.

## Mandatory receipt foundation — 14:37 JST

The first ADR-0095 implementation step replaces ProviderStepOutcome's optional Effect
and three construction paths with one Create(failure-or-absence, mandatory receipt).
Every Windows/WSL producer and test fake now uses it. ProviderStepReceipt currently owns
ordered legacy effects only; target observations, recovery artifacts, overwrite admission,
provider mutation and directory merge are still unimplemented and unavailable. This is
not complete ADR-0095 acceptance and does not authorize exposing destructive choices.

The execution-owned FileOperationEffectJournal records each provider receipt before
interpreting its result. FileOperationEffectSummary owns effects, explicit skipped
sources and source-qualified OperationProviderStep receipts together. Success, failure,
cancellation and later failure after Skip retain those facts; a provider-only effect
cannot become a no-effect rejection or a fresh conflict pause. The former skip-result
reconstruction is removed. Presentation retains the same immutable receipts and derives
existing counts in one pass. No new command, mutation method, package or assembly
reference is introduced; ImmutableList is supplied by the existing .NET reference pack.

Two distinct ownership defects are proved and corrected. receipt-red-14 builds with
zero warnings/errors, then the original public outcome's non-generic ICollection.SyncRoot
allows its backing list to be cleared: the selected test fails, exit2. Owned ImmutableList
storage prevents that mutation. Independent review then identifies validation followed
by a second caller enumeration. receipt-admission-red-19 builds successfully and both
changing-enumerator cases fail with null facts inserted after validation. The correction
materializes each immutable snapshot once and validates exactly that stored snapshot;
receipt-admission-green-20 passes50 related cases with zero-warning/error build. Final
read-only re-review at14:34:27JST has no remaining must-fix for this bounded foundation.

All attempts remain in D:/NeNeCommander/evidence/transfer-conflicts-20261125:

- receipt-build-15 fails on two initially absent test restore assets and IDE0303
  collection-expression diagnostics. Locked restore16 succeeds without dependency-input
  changes; build16 then compiles all product projects, including App, but fails on two
  stale Application-test summary calls. Those test calls are migrated; no failure is waived.
- receipt-app-17 passes48 initial targeted cases. receipt-selected-18 then passes
  Application418, Infrastructure369 and Presentation55, with zero-warning/error builds.
  The exact class selections exclude live environment cases and are saved in
  receipt-selection-18.json; full argument arrays and exits are in receipt-selected-18.json.
- The subsequent two-factory snapshot correction receives the50-case green20 proof.
  Unchanged provider bodies and their successful369-case proof are reused; no native
  primitive, shared desktop input, ordinary settings or user files are changed.
- receipt-format-21 formats only the changed C# files and verifies no remaining whitespace
  change, both exit0. Its existing workspace-load warning is retained. conformance22
  passes112 rules. complexity22 exits1 with exactly the prior two findings, unchanged
  path/line/metric/value/limit; no zero-findings claim or threshold change is made.

Exact principal commands (from the issue-560 worktree):

```powershell
dotnet restore NeNeCommander.slnx -p:Configuration=Release --locked-mode
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-receipt-selected-18.ps1
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~Receipt|FullyQualifiedName~TransferConflictContractTests|FullyQualifiedName~TransferSnapshotTests|FullyQualifiedName~ArgumentGuardParameterTests|FullyQualifiedName~NullGuardTests'
pwsh -NoProfile -File eng/conformance.ps1
pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect
```

The selected script sets TEMP/TMP to the task-owned D-volume proof-temp directory.
It builds each directly affected test consumer and applies the saved class filter.
App/dependency/record-closure finalization and local commit are being completed.
The full destructive/shared-contract deep-review remains required before the later
OverwriteContents implementation can be exposed; this preparation starts no CI,
publication, SARIF update, PR, main integration, tag, release or cleanup.

Final consumer build23 includes App and all inward assemblies and exits0 with zero
warnings/errors. The selected ProjectDependencyTests case passes1/1; record closure
reports156abstract records/143closed visible roots/0failures. Exact commands:

```powershell
dotnet build tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~ProjectDependencyTests
pwsh -NoProfile -File eng/verify-record-closure.ps1 -RepositoryRoot D:/NeNeCommander/worktrees/issue-560
```

receipt-final-inputs-25.json freezes1417inputs using the prior complete manifest
plus new sources/tests. The earlier24manifest is retained as a narrower provisional
subset;25also includes the prior tooling/workflow configuration inputs. Complexity
findings compare exactly to owner-complexity-03, including locations and values.
No dependency/lock/project inputs change. Local normal-hook commit follows; publication
and the remaining ADR-0095 implementation stay pending.

## Target/artifact observation model — 15:02 JST, local follow-up

Foundation a2bd78f is saved with normal hooks. Its follow-up extends the same receipt,
not the provider execution path. Closed target observations, qualified object paths,
artifact role/content/link outcomes and unidentified creation candidates now survive
success/failure/cancellation. Snapshot guards refuse absent target with artifacts,
contradictory roles/object identities and claiming the target itself as an artifact.
Known identities remain available through LastKnownPath after later query failure.

Independent model review identifies the difference between whole-file bytes and valid
recovery mapping, then the readback-success/flush-failure edge. Both are incorporated:
RecoveryFrameEvidence pins validated framed bytes, RecoveryFrameObservation separately
reports flush, and RecoveryMappingContent preserves the initial observation through
independent append states. Complete append bytes retain their hash/length even if flush
is unconfirmed. A represented append requires an acknowledged initial flush. These
transport invariants do not grant mutation authority or prove runtime preparation order.
Final independent read-only review at15:00:20JST has no remaining model must-fix.

receipt-observation-build-27 and28 preserve initial IDE0046/IDE0058/unused-using failures;
29 then passes33 selected cases. recovery-build32 preserves another IDE0046 diagnostic;
33 preserves an accidental test-method-name edit during fixture extraction. Corrected
receipt-recovery-34 builds with0warnings/errors and passes39 selected Application cases:

```powershell
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~Receipt|FullyQualifiedName~ProviderTargetObservationTests|FullyQualifiedName~RecoveryMappingContentTests'
```

Changed-file format/verify35 both exit0 with the existing workspace-load warning.
Current consumer builds, one new gateway-to-presentation receipt case and record closure/
conformance/complexity are being collected by run-observation-consumers-36.ps1. The new
model is not a completed overwrite operation, recovery serializer, cleanup proof, native
acceptance or full deep-review. Earlier unchanged provider proof is retained; no broader
native suite, UI launch, source publication, CI, SARIF, merge, release or cleanup starts.

Final consumer36 succeeds: Infrastructure/Presentation test consumers and Architecture
(including App and all inward product projects) build with0warnings/errors. Selected
DualPaneTransferPresenterTests passes7/7, including the new preparation-only failure
through gateway/session/presenter, with0copied/verified/deleted and the exact retained
receipt. Record closure passes164abstract records/151closed visible roots/0failures;
conformance passes112rules. Complexity remains exactly the prior2findings, including
locations and metric values. observation-final-inputs-38.json freezes1444inputs.
The exact consumer command driver is:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-observation-consumers-36.ps1
```

Its full build/test arguments and exits are observation-consumers-36.json. The previously
successful369provider cases are reused because the provider bodies and legacy effect
meaning are unchanged; compilation verifies the required receipt argument migration.
Local normal-hook save follows. Native overwrite, cancellation within partial writes,
capability/consent admission, stream/attribute deletion proof, recovery codec/cleanup and
full destructive deep-review remain open. No destructive capability is enabled.

## Explicit provider cancellation — 15:26 JST

Observation model is saved as f3d201d with normal hooks. This follow-up makes
ProviderStepOutcome.Create require ProviderStepCompletion and the same mandatory
receipt. The closed completion distinguishes Succeeded, Failed(reason), and Cancelled;
Failure absence alone no longer implies success. Every existing caller migrates with
no old overload. Reverse-migration comparison proves all 14 changed Infrastructure
provider files have exactly their former source after undoing only that argument change.
Existing adapter cancellation conventions, including recycle failure normalization,
are not silently claimed migrated. IFileOperationPort describes explicit cancellation.

Every gateway mutation records its receipt before handling Cancelled. Copy does not
add Copied or verify; Move does not delete its source; no interrupted source adds
completed progress. Existing effects, earlier successful sources, explicit Skip,
recycled effects, partial source deletion and target/artifact observations remain.
VerifyCopiedAsync names the existing verification phase without another execution path.
Presentation preserves the same receipt and zero invented completion counts.

Independent review found two migration endpoints: three test-only verification
converters and two preflight fixtures could turn Cancelled into success; the new
interface wording implied more adapter migration than performed. The converters now
reject that invalid fixture input and the wording is limited. Final read-only review
at 15:21:26 JST has no remaining must-fix for this scope.

Evidence and exact drivers are in D:/NeNeCommander/evidence/transfer-conflicts-20261125:

- red41 retains an incorrect fixture role-name compile error. red42 builds with zero
  warnings/errors, then all 9 cancellation tests fail (exit 2): interrupted steps are
  reported successful or incorrectly advance into verification.
- green43 retains IDE0046; corrected green44 builds cleanly and passes 77 gateway/receipt
  cases. selected46 builds, then passes 429/430; its one failure is the reflection
  null-guard fixture still supplying the old failure argument type, not a waived defect.
- corrected selected47 passes Application 430, Infrastructure 369 and Presentation 57,
  all builds with zero warnings/errors. Selections and complete argument arrays remain
  in cancellation-selection-47.json and cancellation-selected-47.json.
- final-consumers48 passes changed-file formatting/verification, App/all-inward consumer
  build, record closure 165 abstract records / 152 closed visible roots / 0 failures,
  and conformance 112. The workspace-load formatting warning remains recorded.
- review50 retains IDE0058 in a new assertion. Corrected review51 passes the exact
  conversion-boundary tests: Application 4, Infrastructure 3, Presentation 4, with all
  three consumer builds clean. These include success/failure controls and cancellation
  rejection. No unchanged 856-case selection is repeated for test-fixture guards alone.
- review-final52 passes formatting/verification and conformance 112. Complexity exits 1
  with exactly the same two existing findings (path, position, metric, value and limit
  match observation36). No threshold, baseline or suppression changes.
- provider-parity53 confirms the 14 exact reverse migrations. Final manifest
  cancellation-final-inputs-54.json freezes 1452 relevant inputs, retaining the
  prior full tooling/workflow set and adding current sources/tests.

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cancellation-selected-47.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cancellation-final-consumers-48.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cancellation-review-51.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cancellation-review-final-52.ps1
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/verify-cancellation-provider-parity-53.py
```

This establishes transport/orchestration for a future partial-write cancellation. No
native chunked overwrite exists yet. Exact conflict/capability evidence, provider
attempt owner, native path/stream/attribute checks, recovery codec, owned cleanup,
source-delete admission and the full ADR-0095 destructive proof remain outstanding.
No shared-desktop input, ordinary settings change, external publication, CI/SARIF,
PR integration, release or cleanup occurs. Normal-hook local save follows.

Document finalizer54 first stops on an implicit CP932 read of PROJECT_STATE after
saving the input manifest and quality/handoff text. The state read is completed with
explicit UTF-8; the preceding append is not repeated. This documentation failure does
not change or invalidate the frozen source/test evidence.

## Held-object and unnamed-stream observations — 15:48 JST

Cancellation is saved as fba7d85 with normal hooks. The next internal Infrastructure
follow-up extends the existing WindowsFileIdentifier owner. ReadLocalFacts reads the
stable object ID, native basic/standard fields and actual filesystem name through one
caller-held handle. Its observations are sequential, not an atomic metadata snapshot.
The WSL guarded reader and production-unreachable unguarded WSL helper keep their
existing boundaries. OpenForAttributes retains its no-follow/share-all behavior; it
now has internal visibility and a null guard for the selected owned-handle caller.

WindowsLocalEntryIdentity.CaptureHeld associates the caller-qualified path with that
object's existing windows-v2 token. Both path metadata and native facts use one composer
with unchanged kind/length/creation/write tick order. Impossible length/directory bytes,
contradictory directory attributes and unrepresentable FILETIMEs fail closed. CaptureHeld
explicitly does not prove the current pathname. NTFS/local drive eligibility, ancestor
ownership, hardlink policy, sharing exclusions and final mutation admission remain open.

RequireUnnamedStream uses the existing native information query with a fixed 40-byte
FILE_STREAM_INFO buffer. WindowsUnnamedStream accepts only a terminal entry with the
exact UTF-16 unnamed-stream name, 14-byte name length, expected nonnegative stream size
and nonnegative allocation size. Query failure, incomplete buffer, further entry,
wrong name/size or unrepresentable observation refuses. There is no retry with a
truncated prefix, omission of an empty named stream, or inferred successful empty list.
This is read-only membership evidence, not a permanent stream or content freeze.

Proof, selected by the internal observation/identity impact:

- held-facts57 retains IDE0047; held-facts58 retains an incorrect assertion-return-type
  compile error. Corrected59 builds with zero warnings/errors and passes 38 selected
  cases: new held-object tests plus existing identifier, long-path and step admission
  boundaries. Files, empty files and directories match the old full token; after an
  owned original name is replaced, the held ID stays with the parked original; junction
  observation does not follow its target. Malformed metadata and missing inputs refuse.
- stream-observation60 retains CA1062 and IDE0010 in the new test fixture. Corrected61
  builds with zero warnings/errors and passes 23 cases: 14 stream cases plus the nine
  reorganized malformed-metadata cases. Actual empty/nonempty default streams pass;
  empty and populated Zone.Identifier and a custom named stream refuse. Eight malformed
  observation cells and invalid-handle checks also refuse. The two selections establish
  52 distinct cases, not 61: nine metadata cases are present in both selections.
- static62 passes conformance 112; complexity exits 1 with exactly the prior two
  findings, including locations and metric values. format63 applies only the eight
  changed C# files and verifies cleanly; its existing workspace-load warning is retained.
- Independent read-only review at 15:43:39 JST finds no code must-fix and separately
  requires the then-pending stream proof. Corrected61 supplies that successful proof.
  held-stream-final-inputs-64.json freezes 1458 relevant inputs.

Exact build/test commands (TEMP/TMP are the task-owned D proof-temp directory):

```powershell
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsLocalHeldIdentityTests|FullyQualifiedName~WindowsFileIdentifierTests|FullyQualifiedName~WindowsLocalLongPathIdentityTests|FullyQualifiedName~WindowsLocalStepAdmissionTests'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsUnnamedStreamTests|FullyQualifiedName~WindowsLocalHeldIdentityTests.CaptureHeldWhenNativeMetadataIsUnrepresentableRejectsIt'
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-held-stream-static-62.ps1
```

The native interpretation is checked against Microsoft's [FILE_STANDARD_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_standard_info)
for length/link/delete/directory fields, [FILE_BASIC_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_basic_info)
for FILETIME/attribute fields, and [FILE_STREAM_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_stream_info)
for byte-counted names, terminal offsets and alignment. Local test evidence, rather than
those API descriptions, establishes this machine's observed 40-byte default-stream query.

No product mutation, preflight or UI calls the new observation path yet. The sole gateway,
project/dependency graph, public record-root structure and existing adapter operation
bodies are unchanged; no unrelated suite, App rebuild, repeated closure or full deep-review
is needed for this internal read-only step. Native ancestry/path proof, exact consent and
capability admission, protected artifacts, recovery codec, write/truncate/cleanup,
source-delete stream/attribute checks and full ADR-0095 destructive acceptance remain.
No ordinary settings/shared desktop input, external publication, CI/SARIF, merge, release
or task cleanup occurs. Preserve this worktree/output/evidence until completed acceptance.

## Held transfer path and ancestry prerequisite — 16:20 JST

The previous held-facts/stream step is saved as bfcf44f. This local follow-up adds one
internal WindowsTransferPathLease for a future transfer attempt. There is still no
provider/preflight/UI caller and no overwrite capability. The existing gateway and
operation bodies are unchanged.

WindowsTransferEntryOpen opens the drive root with NtCreateFile and each subsequent
single component relative to its held parent. Names derive from typed direct-child
paths only; native attributes0 and FILE_OPEN_REPARSE_POINT retain the entry itself.
No OBJ_DONT_REPARSE assumption, multi-component traversal, Create/Overwrite disposition,
backup privilege or fallback path is used. Source READ and target READWRITE are the
only supported leaf access modes. The exact 64-bit native layout fails closed elsewhere.
The original UTF-16 code units are copied without encoding replacement.

The lease retains every handle through the leaf, with share READ. Each entry must be
observed NTFS, non-reparse and not delete-pending; ancestors must be directories and
the leaf a file. Object identities use WindowsFileIdentifier, and initial/full file
identity uses the unchanged windows-v2 CaptureHeld composer. Revalidation never resets
baselines. Directory metadata may change with unrelated siblings; its stable object ID
is retained. The file's initial full identity must still match. The existing injected
WindowsDriveBoundary admits the drive initially and at revalidation. A fresh root open
must match the held root. Partial admission and ordinary disposal close owned handles
in reverse order, including failure while adding an entry.

WindowsTransferPathObservation separately requires the exact ordinal normalized DOS
path plus a complete volume-GUID-form observation from the same held handle. Network
GUID failure and different spelling/case/short names/SUBST therefore conservatively
refuse; path labels alone do not grant capability. These are sequential observations,
not permanent DOS mapping, metadata, alias or mapped-writer exclusion. A later failed
query must remain LastKnownPath in the future receipt. No target/stream eligibility or
content proof is inferred from a successful lease.

Native owned-filesystem evidence is under the task's D output/evidence roots:

- native-ancestry66 passes38 checks. Ordinary ancestor/leaf rename refuses and sibling
  creation succeeds. WRITE_ATTRIBUTES remains possible despite share READ: a held empty
  directory becomes a junction, while the nonempty held ancestor refuses error145.
  The first probe's relative foreign child was absent, and a success retained stale
  GetLastError145; neither is presented as the corrected proof.
- native-ancestry67 passes43 checks with that foreign child actually present and a
  corrected success error0. Relative no-follow opening through the converted parent
  refuses0xC0000280; the foreign content is unchanged. Reopening the junction itself
  retains its original ID and reparse attribute. No hostile-kernel guarantee follows.
- Review requested the distinct POSIX hardlink replacement boundary. Native72 tests
  NtSetInformationFile RenameInformationEx65 and LinkInformationEx72, flags3, against
  held READ/READWRITE targets. All four cells/32checks pass: status0xC0000043, exact
  original occupant retained, not delete-pending. Native74 changes only target sharing
  to ALL: all four controls/32checks pass with status0, foreign occupant at the name,
  original held ID retained, DeletePending1 and the observed $Extend/$Deleted pathname.
  This proves the probes can actually replace an existing name. Earlier rename-only
  evidence remains preserved; it was not substituted for the new hardlink boundary.

Build68 retains nine native-marshalling/style/ownership diagnostics;70 retains two
style diagnostics after incomplete script replacements;71 retains14 test unused-result
diagnostics. The volume GUID substring boundary was corrected before the first executed
suite. Corrected73 builds with zero warnings/errors and passes27 selected new cases.
75 passes five further cases for initial/later drive reclassification, long paths,
renamed held objects and strict case refusal, again with a clean build. The READWRITE
fixture uses a compatible read-sharing handle after independent review identified the
ordinary File.ReadAllText sharing conflict. No product behavior was changed for it.

Static76 passes formatting/verification but conformance rejects the test's bool mode
parameter. Splitting valid/refused GUID cases resolves it:77 has a clean build, eight
changed cases pass, and conformance112 passes. The 27+5 selections establish32 distinct
cases;77 replaces eight of those, not eight additional cases. Whitespace-only formatting
follows tests; no unchanged behavior suite is repeated. Final format/complexity78 is
recorded below after readback. Existing workspace-load warnings remain in format logs.

Independent final read-only review at16:16:54 has no must-fix for this prerequisite;
72/74 close its native evidence condition. Review did not claim the then-pending static
results. No new public record roots/dependencies/App callers changed, so no repeated
record closure, App build, unrelated provider suite or full destructive gate is claimed.
Full ADR-0095 proof remains required before the future capability is exposed.

Exact commands:

```powershell
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/native-ancestry-66.py
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/native-ancestry-67.py
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/native-link-replace-72.py
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/native-link-control-74.py
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsTransferPathLeaseTests|FullyQualifiedName~WindowsTransferPathObservationTests'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~OpenAndRevalidateWhenDriveBecomesRemoteRefuseIt|FullyQualifiedName~OpenWhenPathExceedsLegacyLengthRetainsExactFile|FullyQualifiedName~RequireWhenHeldEntryIsRenamedRejectsItsLastKnownName|FullyQualifiedName~RequireWhenCanonicalCasingDiffersRefusesAmbiguity'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsTransferPathObservationTests.HasVolumeGuid'
pwsh -NoProfile -File eng/conformance.ps1
pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect
```

Native/managed filesystem tests use the task-owned D proof-temp or explicit native
output root. Formatter76 includes only the six new C# files;78 includes only the changed
observation test. Each uses `dotnet format whitespace NeNeCommander.slnx --no-restore
--include <paths>`, followed by the same selection with `--verify-no-changes`.

Primary references: [NtCreateFile](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntcreatefile)
for parent-relative names, final reparse-point open and sharing;
[GetFinalPathNameByHandle](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew)
for normalized/GUID results and network GUID failure;
[FSCTL_SET_REPARSE_POINT](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fsa/4aeefef8-92c3-4abc-af7a-a610caf8a165)
for attribute/write access and nonempty directory refusal; and
[FILE_LINK_INFORMATION](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/ns-ntifs-_file_link_information)
for POSIX replacement semantics. The local observations above, not these descriptions
alone, establish this machine's particular outcomes.

Next: freeze exact conflict target/capability evidence and immutable choices/plans,
then implement the one provider attempt owner, protected create-new artifacts, bounded
recovery codec, writes/truncate/verification, cleanup and source-delete stream/attribute
admission. Directory merge remains separately unaccepted. Beta.5/selection publication
holds are unchanged. No external publication, CI/SARIF, merge, release, ordinary settings
change, shared desktop input or cleanup occurred; unfinished work remains retained.

Final static78 readback: format and verification exit0; complexity exit1 with exactly
the same two prior findings, including location/metrics. All1458 prior proof-input
hashes are unchanged; transfer-path-final-inputs-80.json adds the six new files and freezes
1464 inputs. The new test-only split changes no production method. Normal-hook local
commit follows; this is not publication or destructive feature acceptance.

## Immutable transfer consent and verification collections — 16:42 JST

The held-ancestry prerequisite is saved as130a0dd with normal hooks. This follow-up
closes the existing collection boundaries that the coming exact-target consent will
use. It enables no overwrite and changes no provider operation body or native API.

FrozenTransfer sources, TransferConflict allowed decisions, ConflictSet conflicts,
TransferResolution choices, TransferPreflightSucceeded plans, VerifiedDirectory children
and both VerifiedTreeBoundary ancestor lists now use the same existing ImmutableList
storage as provider receipts. The provider-facing resumed preflight batch is immutable
too. Input copying occurs once; element/count validation and retained state use that
snapshot. No mutable SyncRoot backing store or second-enumeration injection remains
at these boundaries. Private, unexposed capability accumulation is unchanged.

The new adversarial tests fail11/11 against unchanged130a0dd after correcting82's unused
using diagnostic. Red83 reproduces seven mutable list erasures, one allowed-choice array
substitution, unvalidated second-enumeration content, a Count1/empty enumeration conflict,
and absent plan entries. Green84 first retains an empty-collection syntax diagnostic.
Corrected85 builds with zero warnings/errors and passes12/12, including the real gateway's
provider-facing source batch. The Scripted port observer is absent by default and retains
all existing fixture order; it is test-only. Two factory guards in one test and all three
verified collection snapshots in another are exercised after correction, not counted as
additional test cases.

Direct-consumer selection86 passes Application70, Infrastructure72 and Presentation2;
all builds including normal App have zero warnings/errors. The exact class selections
are transfer-collections-selection-86b.json;86.json is only the broader exploratory
reference list. Scope covers continuation/choice/plan/source/verified-deletion behavior,
not unrelated navigation, settings, full regression or live WSL. Earlier success is reused
when its inputs are unchanged. No public record root/derivation contract or dependency
changes require repeating compiler-closure or graph proof in this prerequisite.

Independent read-only review16:36:15 has no must-fix and confirms red83/green85 evidence.
Static87 formats only11changed C#files, verifies them, passes conformance112, and retains
exactly the same two CS-013 findings (exit1, not a clean-complexity claim). Existing
workspace-load warnings remain. Then88 aligns TestProperty metadata with ADV-006/007/016
and their registered defenses/security wording; test bodies are proved unchanged.
Final89 reconciles every preformat body, freezes1465inputs, and identifies exactly
11changed prior inputs plus one new test; all1453other prior hashes match.
No behavior suite is repeated solely for whitespace or threat-label metadata.

Exact commands:

```powershell
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~TransferEvidenceCollectionTests'
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-transfer-collections-selected-86.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-transfer-collections-static-87.ps1
```

The selected runner contains the exact per-layer build/filter commands and normal App
build; filesystem cases use the dedicated D proof-temp. Full destructive/shared-contract
acceptance still precedes feature activation. Native path/current identity evidence is
unchanged and reused. Next add retained original conflict target/capability evidence,
then protected artifacts, recovery codec, actual mutation/cleanup/source-delete checks.
No external publication, CI/SARIF, integration, release, shared input, normal settings
change or cleanup occurred. The two existing public-payload approvals remain pending.

## Original conflict target evidence — 17:01 JST

Collection hardening is saved as fdaa149. This local follow-up gives every conflict
one immutable TransferConflictTarget. It retains the ordinary target path and either
qualified original file observations or explicit absence of such evidence. Reservations
therefore do not invent an existing object. TransferConflictChoice and TransferPlanEntry
retain that same binding; resumed-plan validation requires its reference identity.
An otherwise equal foreign binding cannot substitute a target confirmation. A record
clone retaining the same binding is not a new modal authority; exact rendered-modal
ownership remains separately required.

TransferOverwriteFileObservation retains a current non-root Windows local file path,
full and stable identity, known byte size and UTC modification time. The evidence pair
rejects lexical/case aliases and same-native-object aliases. Conflict construction
matches the source path/full identity and the original target path. Actual NTFS,
stream, attribute, link, parent and effect-time admission remain the provider's work;
these transport facts do not grant overwrite permission. The old three-argument
factory is removed. All callers migrate atomically to the mandatory evidence-or-absence
argument. Both production preflight calls pass null and the only decisions remain
Skip, KeepBoth and Cancel.

Proof and preserved failed attempts:

- Red91 reproduces both different-path and same-path foreign target substitution:
  the old gateway reaches Copy instead of rejecting the resumed plan. Green94 builds
  with zero warnings/errors and passes19 selected cases. Initial93 style diagnostics
  remain recorded.
- Independent review finds one concrete model contradiction: drive roots could claim
  File kind. New model95 first has a mistaken deletion-capability member name;96 then
  executes15 cases with exactly two root failures and13 passes. The corrected factory
  rejects a null Parent. Green97 passes all15 model cases plus both gateway substitutions,
  with zero-warning/error build. This includes remote/last-known/directory refusal,
  aliases, source/full-identity/target mismatch and exact choice/plan propagation.
- Final independent read-only correction review at16:56:46 has no must-fix for this
  prerequisite. It does not accept native overwrite or effect admission.
- Direct consumers98 pass Application64, Windows Infrastructure10 and Presentation2.
  The three consumer builds, including normal App, have zero warnings/errors. Other
  test factories change only by the required explicit null argument; full test-project
  compilation checks those migrations without running unrelated navigation/settings.
- Static99 formats only22 changed C#files, verifies formatting and passes conformance112.
  Complexity exits1 with exactly the same two prior findings, including locations and
  metrics. Existing workspace-load warnings remain. Format body parity is verified;
  no behavior suite is repeated for whitespace or threat-description-only changes.
- target-evidence-final-inputs-100.json freezes1470inputs:18 changed prior files,
  five new files and1447 unchanged prior hashes. Existing closed abstract roots and
  dependencies are unchanged, so record-closure and dependency proof are reused.

Exact commands:

```powershell
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~TransferOverwriteEvidenceTests|FullyQualifiedName~TransferConflictTargetOwnershipTests'
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-target-evidence-selected-98.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-target-evidence-static-99.ps1
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/freeze-target-evidence-100.py
```

The selected runner records exact layer filters in target-evidence-selection-98.json
and uses the existing task-owned D proof-temp. Full destructive/deep-review proof
remains required before activation. Next qualify real held source/target observations,
then protected preparations, recovery codec, actual writes/verification/cleanup and
source-delete stream/attribute admission. Folder merge remains a separate open scope.
No public push/PR/SARIF/CI, merge, release, ordinary settings change, shared input or
cleanup occurs. Beta.5 and selection publication holds remain pending; all unfinished
work and evidence are retained on D.

## Held source/target overwrite qualification — 17:18 JST

The target-binding prerequisite is saved as2aa2733. This follow-up adds two internal
owners without any preflight/UI/provider caller: WindowsOverwriteFileAdmission and
WindowsOverwritePairLease. The existing path lease gains only leaf Path/Baseline
getters. Source READ and target READWRITE chains retain shareREAD; partially opened
pairs transfer ownership explicitly or release target then source in finally.

Qualification requires exact original path/ancestor/full identity, actual NTFS,
regular file/delete-pending0, exactly one observed link, attributes exactly Normal or
Archive, and the existing complete bounded unnamed-stream query. Zero/unknown/combined
attributes refuse. Same-path or native-object aliases refuse. The frozen source identity
must match. Revalidate compares current observations with both the pair's initial
Evidence and the supplied original consent; neither baseline is refreshed. This method
is explicitly for pre-effect originals, not post-write verification.

The final sequence is original path validation, native facts/shape/full identity,
final path validation, actual stream query, then native facts equality. Pair observation
repeats the complete source observation after observing target. These are sequential
observations, not a permanent alias/metadata/stream/mapped-writer freeze. Byte contents
are not compared by this prerequisite; original/prepared-byte admission is future work.

Proof is confined to these internal owners and their existing native dependencies:

-103 retains CA2000 ownership diagnostics;104 retains two redundant test casts. Corrected
105 builds0warnings/errors and passes51 cases. Additional106 passes12 selected cases,
adding late empty/populated streams, invalid roots/remote source and disposed ownership;
four cases overlap105, establishing59 distinct cases at that point.
- Independent review finds discarded late disqualifying facts: the final ordinary
path revalidation reads Hidden/multiple links but checks only its original identity.
The existing drive callback deterministically injects each change at the final file
observation or during target observation. Red107 reproduces all four failures.
-108 retains a mistyped project path;109 retains style diagnostics. The first correction
moves final facts comparison and fully observes source again.110 then passes64/65,
but an empty stream inserted at the final callback can leave queried metadata equal.
Thus metadata equality cannot substitute for stream membership.
-111's attempted LF-only replacement misses CRLF, leaving that source order unchanged.
Its65pass is retained as evidence of the old stream race's timing dependence and is NOT
correction proof.112 asserts the actual replacement/readback and moves the real stream
query after the last path check. The actual corrected build has0warnings/errors and
65/65pass. The six race cases additionally pass twice in114 (three executions including
112); this bounded repetition addresses the demonstrated observation-timing concern.
- Final independent read-only review17:16:14 has no must-fix for this unexposed native
qualification. It explicitly excludes byte-copy/mutation acceptance and111.
- Static113 formats/verifies exactly six changed C#files, passes conformance112 and
retains the exact same two complexity findings (exit1). Existing workspace-load warnings
remain. Format body parity is verified. Freeze115 has1475inputs: only the existing lease
file differs, five files are new and1469prior hashes match.

Exact commands:

```powershell
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsOverwritePairLeaseTests|FullyQualifiedName~WindowsOverwriteFileAdmissionTests|FullyQualifiedName~WindowsOverwriteAdmissionRaceTests'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsOverwriteAdmissionRaceTests'
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-overwrite-pair-static-113.ps1
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/freeze-overwrite-pair-115.py
```

Filesystem tests use the existing D proof-temp only. The one test-owned target truncate
proves a changed baseline refuses; it is not a product overwrite implementation. No new
public root/dependency/App consumer changes, so no unrelated suite, repeated App build,
record closure or full destructive gate is claimed. Existing production transfer behavior
and the three conflict choices are unchanged. Protected artifact creation, recovery codec,
byte preparation/admission, actual write/truncate/verification/cleanup, source-delete
stream/attribute requirements and full ADR0095 destructive proof remain before activation.
No publication, ordinary settings/shared input or cleanup occurs; existing approval holds
and unfinished D work/evidence remain retained.

## Protected create-new artifact primitive — 17:38 JST

Native pair qualification is saved as2732c80. The current five-file follow-up extends
WindowsTransferEntryOpen's single NtCreateFile boundary. Existing OPEN callers retain
exact no-follow/case-sensitive lookup semantics. Only artifact creation uses FILE_CREATE,
NON_DIRECTORY, Normal attributes, shareREAD and read/write-data/read-attributes/
READ_CONTROL/DELETE rights. It requires successful FILE_CREATED information, with no
fallback open, truncate, candidate retry or cleanup.

WindowsTransferSecurityDescriptor captures the current user SID into an initially
protected DACL with exactly one FullControl allow ACE and that user as owner. Its
self-relative descriptor is supplied in OBJECT_ATTRIBUTES at creation. There is no
create-broadly-then-restrict interval. Descriptor memory is SafeHandle-owned, and the
parent/descriptor references span the native call. OS Archive bookkeeping is accepted;
artifacts are not hidden. The API now accepts the retained path lease, candidate and
descriptor only, revalidates the lease, and derives both parent handle/path from that
owner before any effect. Callers cannot independently mislabel the held parent.

The held parent must report FileCaseSensitiveInfo flags0 through the existing native
identity owner. Nonzero/unknown flags and query failures refuse. Creation also requests
OBJ_CASE_INSENSITIVE. The lease's existing file keeps its parent nonempty. Microsoft's
[case-sensitivity documentation](https://learn.microsoft.com/en-us/windows/wsl/case-sensitivity#modify-case-sensitivity)
requires administrator privileges and an empty directory to change this flag; the design
uses that documented restriction, not an unproved native toggle-exclusion claim.
Actual enabled-case-directory proof is still an environmental requirement before final
destructive acceptance.

Proof and retained attempts:

-118 retains a collection-initializer style error.119 executes10 cases:8pass, one
incorrect test expectation for Normal-only attributes fails because Windows reports
Archive, and the old independent-parent tuple reproduces creation in the wrong held
parent. The latter is a product boundary defect, not a test expectation issue.
-121 fixes the owner input and native case admission, adjusts only the incorrect
attribute assertion to the already accepted Normal/Archive contract, and passes15cases
with0warnings/errors. Actual initial handle-read ACL is protected/current-user-only with
no inherited WorldSid grant from the test parent. File/directory/junction/hardlink and
normal-directory case-alias collisions preserve occupants; writers/rename refuse while
a compatible reader works. Foreign parent, disposed owner and failed/unknown native case
observations refuse before creation.
- Dedicated empty native-case-120 cannot enable case sensitivity: fsutil output says
access denied0x00000005 even though its captured exit is0. That exit is NOT success.
Readback124 explicitly reports the directory's case-sensitive flag is disabled. No
admin prompt, WSL workaround or unrelated directory modification is attempted. Artificial
flag decoding and ordinary-directory collisions do not substitute for actual enabled
case-sensitive native proof.
-122 passes69 direct shared-OPEN/path-lease/pair/race dependency cases. This rerun is
selected because those consumers use the refactored native call; no unrelated suite runs.
-123 formats/verifies the five changed C#files, passes conformance112 and retains exactly
the prior two complexity findings (exit1). Existing format workspace-load warnings remain.
Independent final read-only review17:36:29 has no must-fix for this primitive scope and
retains the enabled-case environment limit. It does not accept receipts/cleanup or overall
product overwrite. Body parity is verified and124 freezes1477inputs:3changedprior,
2new,1472unchanged.

Exact commands:

```powershell
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsTransferArtifactCreationTests'
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-protected-artifact-dependencies-122.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-protected-artifact-static-123.ps1
fsutil file setCaseSensitiveInfo D:/NeNeCommander/outputs/transfer-conflicts-20261125/native-case-120 enable
fsutil file queryCaseSensitiveInfo D:/NeNeCommander/outputs/transfer-conflicts-20261125/native-case-120
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/freeze-protected-artifact-124.py
```

The fsutil set above is the retained failed attempt, not a successful verification step.
All files/ACL changes belong to the dedicated D test roots. Primary references are
[NtCreateFile](https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntcreatefile)
for create disposition, parent-relative names and initial security descriptor;
[new-object security descriptors](https://learn.microsoft.com/en-us/windows/win32/secauthz/security-descriptors-for-new-objects)
for protected inheritance; and the
[file information class](https://learn.microsoft.com/en-us/windows/win32/api/minwinbase/ne-minwinbase-file_info_by_handle_class)
for the native case query. Local tests establish this machine's actual ACL/collision
results, rather than inferring them solely from API descriptions.

No product caller/overwrite capability, new public record root/dependency, App consumer,
external publication, settings/shared-desktop change or cleanup is introduced. Receipt
capture must still record a candidate before possible creation and preserve uncertainty
on any subsequent failure; this primitive never treats a thrown query as no creation.
Next retain known stable artifact identity even if full metadata observation fails, then
add attempt-scoped candidate naming, bounded recovery mapping, preparation/write/cleanup
and source-delete admission. Full ADR0095 destructive proof, including the stated native
environment requirement, remains before activation. Existing approval holds stay pending.

## Artifact identity knowledge without metadata loss — 17:52 JST

Protected creation is saved as a77ed4e. Before the native attempt can use it, the
canonical artifact receipt must represent a stable handle identity whose complete
metadata query has not succeeded. Previously IdentifiedTransferArtifact required a
VerifiedEntrySnapshot containing both full and stable identities; that would either
lose an already known object ID or invent a full identity after a partial observation.

The same artifact field now uses TransferArtifactEntry: non-root WindowsLocalPath,
mandatory stable FileObjectIdentity, mandatory closed ArtifactFullIdentityObservation,
and independent path qualification. NotObserved means no complete identity has been
obtained; ObservedArtifactFullIdentity retains the last successful complete identity,
not current byte proof. The initial nullable-field proposal was replaced during design
review with this explicit closed knowledge state. Source/target ObservedTransferEntry
remains unchanged. No compatibility overload or second receipt/result exists.

ProviderStepReceipt rejects target/duplicate artifact aliases by stable identity even
when full metadata is NotObserved. Existing content-role and removed/uncertain-link
path constraints remain. Non-root local path typing does not prove native ownership,
NTFS, file shape or cleanup permission. Actual monotonic retention across later failed
queries belongs to the future native attempt owner; this transport change alone does
not implement that runtime guarantee. ADR-0095 and ADV-005/006 wording now make the
knowledge/authority distinction explicit.

Selected proof:

- 127 retains IDE0058 on an unused typed-assertion result; no test ran in that attempt.
  Corrected 128 builds with zero warnings/errors and passes 23 Application cases across
  the new entry model, artifact/receipt consistency, recovery content and snapshot
  admission. Missing full metadata retains identified state; alias checks and cleanup
  qualification remain effective, and known full identity survives a last-known path.
- 129 builds App/all production consumers and Presentation tests with zero warnings/errors.
  Six cases pass through the actual gateway/session/presenter: failure or cancellation
  with unidentified, stable-only or full-identity artifacts. No copied/verified/deleted
  counts are invented, and the exact receipt/artifact reaches presentation.
- Because this adds a public closed root, the compiler closure proof is rerun: 166
  abstract records, 153 closed visible roots, zero failures. No dependency graph changes.
- 130 formats/verifies exactly 11 changed C# files, passes conformance 112 and retains
  exactly the prior two complexity findings (exit 1). Workspace-load warnings remain.
  Body parity is verified. Final independent read-only review at 17:50:13 has no
  must-fix for the model and existing receipt route; it excludes native lifecycle proof.
- Freeze 131 records 1,481 inputs: eight changed prior files (including threat wording),
  four new files and 1,469 unchanged prior hashes. No behavior is rerun just for later
  documentation/threat wording or whitespace.

Exact commands:

```powershell
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~TransferArtifactEntryTests|FullyQualifiedName~TransferArtifactReceiptTests|FullyQualifiedName~ReceiptArtifactConsistencyTests|FullyQualifiedName~ProviderReceiptObservationTests|FullyQualifiedName~RecoveryMappingContentTests|FullyQualifiedName~ReceiptSnapshotAdmissionTests'
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-artifact-identity-consumers-129.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-artifact-identity-static-130.ps1
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/freeze-artifact-identity-131.py
```

The consumer runner contains the exact App/Presentation/closure commands. This is a
model prerequisite; native creation/opening inputs are unchanged and their successful
proof is reused. No production overwrite caller or capability, public push/PR/SARIF/CI,
merge/release, ordinary settings/shared desktop input or cleanup occurs. All unfinished
D copies and evidence remain. Next implement the live artifact owner and immutable
receipt updates, candidate naming and recovery codec, then actual preparation/write/
verification/cleanup/source-delete and complete ADR-0095 proof. The actual enabled-case
native environment requirement from 120/124 and both publication holds remain open.

## One-shot artifact ownership and failure retention — 18:10 JST

The identity model is saved as 1a93ba6. The next prerequisite adds the live native
artifact owner without enabling overwrite or cleanup. Prepare borrows the existing
parent lease; the caller must retain that parent until the artifact owner ends. An
absent receipt means no creation attempt yet. CreateNative registers Unidentified
before the existing native create-new call, accepts the returned handle immediately,
and cannot be invoked a second time, including after a collision or query failure.

Observation first invalidates current-path qualification, then captures/stores stable
identity before any metadata query. A later different ID refuses without replacing
the original ID. Complete metadata is stored independently and retained across later
failure. Only successful original-parent revalidation and exact native path observation
restore CurrentPathVerified. This method is explicitly pre-target-mutation only.
The small immutable query adapter uses the same existing native observation owners;
its injected tests still observe real owned handles and provide no create/delete route.
Dispose is idempotent, closes only this artifact's handle, keeps the receipt readable
and leaves the parent lease and artifact content intact. Retained link means retained
by this operation at the observation, not a promise of current pathname or exclusion
of later external changes. No receipt or token grants cleanup authority.

Proof 134 stops on IDE0032 before tests. 135 retains wrong test role names, unused typed
assertion results and CA1861; no tests run. Corrected 136 builds Infrastructure and its
inward projects/tests with zero warnings/errors and passes 16 cases. Tests assert the
canonical receipt between native queries, initial/late metadata and path failures,
invalid metadata, contradictory stable/full-query IDs, native collision, no repeated
CREATE, retained snapshots after disposal, actual content retention, borrowed-parent
lifetime and disposal before the first effect. Existing successful creation/ancestry
proof is reused because those native inputs are unchanged. No extra App, provider-wide
suite, closure proof or CI is needed for internal, uncalled owner types.

137 passes changed-file format/verification and conformance 112; complexity retains
exactly the same two prior findings (exit 1). Workspace-load warnings remain. Independent
read-only review at 18:08:46 has no must-fix for this prerequisite, verifies exact prior
complexity findings and whitespace-only formatting of the four C# files. 138 preserves
input hashes and the unchanged body comparison. Documentation/threat wording adds no
reason to repeat successful tests.

Exact commands:

```powershell
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsTransferArtifactOwnerTests'
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-artifact-owner-static-137.ps1
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/freeze-artifact-owner-138.py
```

No provider caller, bytes proof, target mutation, cleanup, source deletion or overall
ADR-0095 acceptance follows. Full destructive/environment proof remains mandatory,
including the actual enabled-case directory limit from 120/124. Next add one injected
attempt token, bounded candidate names and recovery codec, then preparation/admission,
mutation/verification/owned cleanup and source-delete admission. Beta.5 and selection
publication holds remain pending. No public write, ordinary settings/shared input or
cleanup occurs; all unfinished D copies/evidence remain retained.

## Bounded recovery schema, exact text and crash-prefix interpretation — 18:33 JST

Artifact ownership is saved as 1b18135. The pure naming/schema/frame/inspection layer
now records the accepted ADR-0095 mapping without filesystem I/O or a provider caller.
The exact names, 1 MiB payload/75-byte envelope/two-frame input bounds and 810,984-byte
worst-case variable-field calculation are in the ADR. A single Guid token derives all
siblings; actual one-time IIdentifierSource acquisition still belongs to the future
attempt owner. No ambient identifier, clock, additional parser or recovery executor is
introduced. The same canonical path and identity factories remain authoritative.

Four saved local non-root entries must have distinct lexical paths and stable objects.
Backup/preparation names must exactly match the target parent/token, and recovery must
not collide with source. Strict UTF-8 and checked UTF-16 text refuse replacement or
normalization. Parsing reconstructs the typed model and requires exact canonical JSON
reserialization, closing duplicates, unknown nested fields, alternate representations
and extra content. The human-readable framed file uses .txt, not a JSONL claim.

Independent design review identified token reuse as an append-binding gap: completion
now includes the exact initial frame's SHA-256 and its encoder derives that digest
from the supplied complete initial bytes. Inspection verifies it against the actual
prefix. A complete initial with no complete append remains target-possibly-changing;
only a valid incomplete suffix can be ignored. Complete append requires EOF. Saved
observations create no current-path/content/flush or restore/delete permission.

Evidence and corrections:

- 141 retains five IDE0046 failures; 142 corrects those forms. 143 builds cleanly and
  passes 41 cases, with two failing surrogate tests. Their DataRow strings were replaced
  before execution, so they were not invalid UTF-16 input. The fix constructs the exact
  code unit at runtime and asserts that it is the requested surrogate.
- 144's Python updater fails decoding UTF-8 source with the machine's default cp932;
  no change is applied and the old two failing tests repeat. This is not fix evidence.
  145 uses explicit UTF-8 and retains one redundant-cast build diagnostic. Corrected
  146 builds with zero warnings/errors and passes the two surrogate cases plus 40 new
  checksum-valid schema/corrupt suffix/completion cases. The earlier 41 passes are reused.
- 147 format/verify and conformance112 pass, with exactly the previous two complexity
  findings. Further self-review and independent review agree that partial length prefixes
  F/002 cannot be completed within 1 MiB. 148 reproduces both refusal failures. 149 fixes
  the shared prefix parser using the minimum possible completed value and passes 24
  directly affected cases, including every initial/append byte truncation position,
  maximum escaped text, ordinary roundtrip and header corruption. No unrelated suite runs.
- The selected evidence covers 85 distinct successful cases. 150 final format/verify,
  conformance112 and unchanged two complexity findings pass within their stated scope
  (complexity retains exit1). Workspace-load warnings remain. 151 records body parity,
  exact unchanged complexity findings and the complete current input hash set. Later
  documentation and threat-description changes do not invalidate tested code bodies.

Exact commands:

```powershell
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsTransferRecovery|FullyQualifiedName~WindowsTransferArtifactNamesTests'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsTransferRecoveryCorruptionTests|FullyQualifiedName~EncodeInitialWhenPathContainsUnpairedSurrogateRefusesIt'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~InspectWhenPartialLengthCannotHaveBoundedCompletionRefusesIt'
# Final affected header selection is retained exactly in recovery-prefix-result-149.json.
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-recovery-static-150.ps1
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/freeze-recovery-151.py
```

Native inputs and public record/dependency boundaries are unchanged; their prior proof
is reused. This accepts no actual preparation, recovery-file write/flush, target change,
cleanup, source deletion or product overwrite capability. Full ADR-0095 destructive/
environment proof remains, including actual enabled-case directory evidence120/124.
No public write, ordinary settings/shared input or cleanup occurs. All unfinished D
artifacts remain; beta.5/selection publication holds and separate directory merge stay open.

Final independent read-only review at 18:32:05 JST accepts this pure prerequisite with no must-fix; all runtime/activation limits above remain. Freeze151 has1498inputs:1changed prior threat description,14newC#files and1483unchanged hashes.

## Bounded held-handle content primitives — 18:53 JST

The recovery codec is saved as 41c4756. The existing VerifiedFileContent owner now
supports CompareHeldAsync on borrowed source/target handles and an admitted known
length. Its existing path callers remain unchanged. WindowsTransferContentIo adapts
RandomAccess with at most64KiB requests, short-read completion and cancellation before/
after awaits; WindowsTransferContentCopy writes only through its supplied destination
handle, proves source EOF and then truncates to the expected length. This primitive
assumes enclosing ownership/admission; neither a handle pair nor byte equality is
permission to overwrite, clean or delete. No path is reopened by the held-handle route.

Comparison requires every expected byte, both EOFs and SHA-256, including zero length.
The new copy does not itself report verified content. Native write or truncate exceptions
may follow real changes; callers must retain uncertainty. SetLength/Flush are explicit
native delegates: the future owner must record possibly-mutating/flush-unconfirmed
state before invocation, and known flush success immediately after return before any
later cancellation check. This scope does not implement that enclosing receipt owner.

154 stops on test-fixture CA2000 before execution. Correct ownership transfer nulls the
local handles after the fixture accepts them and disposes untransferred handles in
finally. 155 builds with zero warnings/errors:14pass,14fail only in subsequent observer
File.ReadAllText calls whose share mode conflicts with the still-live writing handle.
156 fixes those test observers to use compatible READ/WRITE/DELETE sharing and passes
all14affected cases. The earlier14 successful cases, including the two unchanged path
comparison tests, are reused. 157 adds one real production native-handle case: exact
parent-relative leases and a protected create-new artifact complete asynchronous copy/
comparison, retain stable artifact ID and leave the original anchor/source intact.

The29distinct successful cases cover empty/1/64KiB-edge/multiple-buffer content,
short reads on either side, same-length mismatch, early/extra EOF, replacement of old
pathnames without redirecting held objects, pre-read/post-read/post-write/final-EOF
cancellation, actual partial-write and truncate-then-throw effects, invalid read counts
and oversized I/O refusal. 158format/verify/conformance112pass and retain exactly the
same two complexity findings (exit1), with unchanged workspace-load warnings. 159
freezes the input set and verifies whitespace-only formatting. No unrelated tree-copy,
provider, App, record-closure, coverage or CI suite is rerun for these uncalled internal
primitives. Existing native admission and receipt transport are unchanged.

Exact commands:

```powershell
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsTransferContentTests|FullyQualifiedName~WindowsTransferContentFailureTests|FullyQualifiedName~VerifiedFileContentTests'
# Corrected observer-only selection is recorded exactly in held-content-result-156.json.
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~CopyAsyncWhenHandlesComeFromNativeOwnersVerifiesOwnedArtifactAsync'
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-held-content-static-158.ps1
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/freeze-held-content-159.py
```

Primary API references: [RandomAccess](https://learn.microsoft.com/en-us/dotnet/api/system.io.randomaccess?view=net-10.0),
[WriteAsync](https://learn.microsoft.com/en-us/dotnet/api/system.io.randomaccess.writeasync?view=net-10.0),
[FlushToDisk](https://learn.microsoft.com/en-us/dotnet/api/system.io.randomaccess.flushtodisk?view=net-10.0)
and the [runtime native implementation](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/IO/RandomAccess.Windows.cs).
These support the selected API shape;157 separately proves this machine's actual native
handles. Async/ConfigureAwait(false) alone does not guarantee off-UI execution. The
future attempt must start within the existing WindowsLocalIoExecutionBoundary and await
the inner asynchronous operation to completion before receipt capture/handle disposal.

No preparation orchestration, token allocation, recovery write/readback/flush, target
mutation, cleanup, source-delete admission or overwrite capability is connected. Final
preparation comparison must retain the FIRST original/desired length+hash baselines,
not update them when both compared files change identically. Actual enabled-case proof,
full ADR0095 acceptance, publication holds and separate folder merge remain open. All
unfinished D copies/evidence remain; ordinary settings/shared input are unchanged.

Final independent read-only review18:52:20JST has no must-fix for the held-content primitives.159freezes1504inputs:2changedprior(verificationowner/threatwording),6newfiles,1496unchanged. The enclosing ownership, receipt, first-hash, flush and scheduler requirements remain open.

## Native preparation and canonical receipt retention — 19:29 JST

Held-content primitives are saved as b67ff26. The new internal preparation owner is
retained before effects and borrows the exact admitted source/target pair. It requests
one injected IIdentifierSource token after original-pair and fixed sibling-shape
admission, registers all three artifact owners before native creation, and creates
recovery first. A dummy-token name collision is not a shape refusal. Actual candidate
collisions never retry or adopt/remove foreign files. The identifier XML, ADR-0070,
CS-010 and command registry now explicitly cover this existing boundary's transfer use.

Backup and preparation bytes use the SAME bounded held-content copy/comparison owners.
Verified bytes are recorded before later metadata/path queries. Owned-artifact admission
retains stable identity, refreshes its full identity after own writes, checks admitted
NTFS shape, performs the LAST path check before stream observation and rejects changed
fresh facts. Empty ADS, late hardlinks and Hidden attributes injected at that path check
all refuse. The target is UntouchedByOperation throughout this stage; complete or partial
artifact creation/write does not invent copied/verified/source-delete completion.

Recovery writing uses the existing bounded codec, at most64KiB I/O requests, exact
readback and EOF. Complete frame evidence is recorded before fallible metadata, flush
OutcomeUnconfirmed before the native call, and Confirmed immediately on normal return
before cancellation is checked. An exception after native flush remains unconfirmed;
a normal-return flush followed by cancellation retains Confirmed. Hardware/power-loss
durability is not claimed. Disposal closes only owned artifact handles and keeps their
last-known receipt; the caller retains and later disposes the borrowed original pair.

Independent review found two substantive initial omissions. Read-only final comparison
must not erase a historically verified length/hash, and final recovery admission must
not accept newly observed metadata without verifying the original frame bytes. Both
are corrected. Historical content stays in the receipt on mismatch/read failure/cancel;
current-path qualification is invalidated during comparison. Established verified
artifact content cannot be rewritten or rebaselined. XML/ADR/glossary now distinguish
saved byte evidence from present equality or mutation/cleanup authority. Final admission
compares original/desired bytes against their FIRST size/hash, never a refreshed pair,
and the recovery record against the exact initial frame, EOF and its saved full/stable
identity. Both sides returning equally changed bytes still fail the original digest.

Selected proof and retained attempts:

| Evidence | Result and scope |
| --- | --- |
| 163 | Build stopped on seven test style diagnostics; no tests executed. |
| 164 | Clean build;44 artifact admission/content/owner/name/native-content cases pass. |
| 165 | Initial preparation implementation builds with0warnings/errors. |
| 167 | Test fixture CA1822 stops build; no tests executed. |
| 168 | Fixture read explicitly checks its owned parent; clean build and15 native success/collision/admission/cancellation cases pass. |
| 170 | Clean build;24 cases pass, including9 actual partial/full-write or truncate-then-throw cells, flush throw/cancel, equal changed final reads and recovery corruption. |
| 172 | Review corrections build cleanly;29 cases pass, including6 final-read failure/cancel cells and preserved historical content. |
| 173 | Clean build;8 writer/observation cells pass: multibuffer strict readback with short reads, corrupt/short/extra EOF, wrong owner role/path and metadata failure after frame/flush evidence. |
| 174 | Changed-file format/verify and conformance112 pass; complexity has the two existing findings plus one new test cognitive11/10. |
| 175 | Scenario selection switches to explicit cases; selected6 pass and static checks pass, but that test becomes41/40logical lines. This unsuccessful complexity attempt remains recorded. |
| 176 | Scenario-to-artifact mapping is extracted by its responsibility. Selected6 pass; build/format/verify/conformance112 pass. Complexity retains exactly the two unchanged findings from158 (exit1), with no new finding. |

The selected runs represent89 distinct cases with overlaps and unchanged successful
proof reused. 177 freezes relevant inputs and checks whitespace-only formatting; the
only non-whitespace change between174and176 is the documented test responsibility split.
No production body changes after the172/173 successful selections. Existing ancestry,
identity, codec, held-I/O and receipt transport proof remains separate and reusable where
its relevant inputs did not change. All tests use the owned D proof-temp root. No normal
settings, shared-desktop input, other App processes or external publication are touched.

Exact commands (all run in issue-560, TEMP/TMP under the owned D output root):

```powershell
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsTransferArtifactAdmissionTests|FullyQualifiedName~WindowsTransferArtifactContentReceiptTests|FullyQualifiedName~WindowsTransferArtifactOwnerTests|FullyQualifiedName~WindowsTransferArtifactNamesTests|FullyQualifiedName~CopyAsyncWhenHandlesComeFromNativeOwnersVerifiesOwnedArtifactAsync'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsOverwritePreparationTests|FullyQualifiedName~WindowsOverwritePreparationAdmissionTests'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsOverwritePreparationFailureTests|FullyQualifiedName~WindowsOverwritePreparationBaselineTests|FullyQualifiedName~WindowsOverwritePreparationTests'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsOverwritePreparationBaselineTests|FullyQualifiedName~WindowsOverwritePreparationTests|FullyQualifiedName~WindowsTransferArtifactContentReceiptTests|FullyQualifiedName~WindowsTransferArtifactAdmissionTests'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsTransferRecoveryWriterTests|FullyQualifiedName~PrepareAsyncWhenRecoveryObservationFailsRetainsFrameAndFlushAsync'
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-preparation-static-174.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-preparation-static-175.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-preparation-static-176.ps1
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/freeze-preparation-177.py
```

176 selects only PrepareAsyncWhenFinalReadFailsRetainsHistoricalEvidenceAsync (6cases)
and formats that one changed file using the Infrastructure test project. All scripts,
raw failed/successful logs and receipts are retained under the evidence root. Scope is
an unexposed internal prerequisite: no public record shape, dependency, production
provider caller or UI activation changed. Existing consumer/closure proof is reused;
full coverage/mutation/deep review remains mandatory for the complete ADR-0095 feature.
The success test invokes preparation through the existing I/O boundary and awaits both
outer and inner tasks, but actual provider scheduling/composition is not yet connected.

Still required: post-mutation lease ownership without requiring the old full target
identity, immediate final admission before target write/truncate, MayHaveChanged before
any such call, post-effect content/shape/stream evidence, append-only completion mapping,
owned-link cleanup and source-delete stream/attribute admission, actual provider/gateway
integration and full destructive proof. Enabled-case-directory proof remains unavailable
(120/124); folder merge is a separate unaccepted scope. Beta.5 and selection public
payload holds remain separate. All unfinished work/evidence stays on D; no cleanup is
eligible or rejected.

Final independent read-only review at19:29:50JST accepts the internal target-untouched preparation with no mandatory correction.176findings equal158 exactly, changed test36logical lines/cognitive8 and its role selector7lines/cognitive1.177freezes1516inputs (7changedprior,12new,1497unchanged); formatting body parity is verified with the one documented test split. Actual provider activation and all destructive acceptance remain pending.

## Post-content observation through the same owned chain — 19:40 JST

Preparation is saved cleanly as b339f86. The existing WindowsTransferHeldEntry and
WindowsTransferPathLease now share one file-object/ancestor/root revalidation path.
Original Revalidate/Observe still require the original full identity; they never update
consent or Baseline. RevalidateObject and ObserveAfterContentChange omit only the old
length/timestamp equality, retain the same original stable file object, current drive,
all non-link NTFS ancestors, native path and reopened root mapping, and perform the same
shape/default-stream/fresh-facts admission. Artifact admission after target effects
borrows this SAME parent chain and otherwise shares its entire native admission method.
No target writer, new native primitive, provider activation or mutation authority is
introduced by an observation method.

180 stops on one unused test using;181 removes it, builds with0warnings/errors and passes
105selected cases. The11new native cases cover actual write and zero-length truncation,
old-consent refusal with unchanged baseline, stream/alias/Hidden changes at the final
path boundary, network/unknown drive refusal, disposal and artifacts anchored to a
content-changed target. The remaining selected cases directly depend on the refactored
lease/admission core: existing lease, original pair, last-path race and preparation tests.
No unrelated provider, App or broad product suite runs.182format/verify/conformance112
pass; complexity exactly matches176's two existing findings. Independent read-only review
at19:38:25 accepts this internal observation prerequisite with no mandatory correction.
183freezes current inputs and verifies formatting body parity; native case-enabled
filesystem proof remains unfulfilled and all full-feature/destructive proof remains open.

Exact commands:

```powershell
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsPostContentAdmissionTests|FullyQualifiedName~WindowsPostTargetArtifactTests|FullyQualifiedName~WindowsTransferPathLeaseTests|FullyQualifiedName~WindowsOverwriteAdmissionRaceTests|FullyQualifiedName~WindowsOverwritePairLeaseTests|FullyQualifiedName~WindowsOverwritePreparation'
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-post-content-static-182.ps1
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/freeze-post-content-183.py
```

Next implement one retained attempt's target-mutation boundary with MayHaveChanged set
BEFORE the first potentially mutating native write/truncate, exact desired verification
through original source/current target/hash and the new current observations, completion
append, owned cleanup, source-delete admission and the sole scheduled provider route.
Observation alone grants none of those effects. The original metadata/security policy,
late-alias residual, beta.5/selection public holds, ordinary-settings/shared-input limits
and retained unfinished D roots remain unchanged. No cleanup is eligible or rejected.

## Exact-target write stage and one canonical attempt — 20:00 JST

Post-content observation is saved as82197f7. The existing preparation owner/dependencies
are renamed to WindowsOverwriteAttempt/WindowsOverwriteDependencies and extended in
place; no second result or executor is added. Prepared carries the first mapping and
recovery snapshot. OverwritePreparedAsync consumes it BEFORE the first await, repeats
all original byte/frame/identity admission, and copies the prepared held stream into
the exact target held object using the existing content copy/I/O owners. Private native
write/truncate delegates record MayHaveChanged before delegation, including empty input.
Earlier read failure/cancellation stays UntouchedByOperation. Later exceptions never
infer no effect and never roll back or delete preparations.

Verification requires original source full identity/shape/streams, current exact target
shape/streams and FIRST desired size, complete source-target bytes/EOF and FIRST desired
hash, then repeated source/target observations. DesiredVerified is saved immediately
before any later cancellation check. Even this internal success retains all artifacts
and zero completed provider effects; completion append, cleanup and source deletion are
not implied. Disposal preserves evidence while qualifying saved target paths LastKnown.
The provider capability and scheduler/composition are not connected.

185build passes.186unused test using and188test switch style stop their builds and remain
recorded.187cleanbuild14cases pass;189cleanbuild9cases pass. These cover empty/short/long/
multibuffer writes, unchanged target object/creation time, read-before-write failures,
no-write/partial/full-write/truncate exceptions, first hash refusal for equally changed
reads, metadata changes during comparison, wrong bytes/missing truncate, late source or
target streams, actual late hardlink observation, and verification retained on cancellation
during final native facts. The late alias sees changed object bytes, as ADR0095 explicitly
warns; the result remains uncertain and retains source/preparations rather than claiming
universal exclusion.

Independent review required direct evidence of the two-stage entry boundary.191adds5
successful cases: source/target metadata, target same-length bytes and prepared/recovery
bytes change AFTER preparation. Each refuses with target native write/truncate calls0,
UntouchedByOperation,3retained artifacts, original backup and no replay.190staticpasses
format/verify/conformance112 but finds2new test complexity excesses (cognitive15/nesting4).
192extracts the target-specific native fault injection into its responsibility, passes
its4affected cases and restores exactly the existing2complexity findings.193proves
concurrent entry refuses while the first read is awaiting, and reruns the directly
changed owner's39preparation cases:40/40pass, cleanbuild.194formats only that new protocol
test and passes conformance112 with exactly the existing2findings. The28new target cases
plus concurrency1 and existingpreparation39 represent68distinct cases; passed unchanged
proof is reused. Existing tests/fixture5reverse-rename exactly; dependencies are rename
and XML only. All source evidence and formatting parity are frozen by195.

Exact commands:

```powershell
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsOverwriteTargetTests|FullyQualifiedName~WindowsOverwriteTargetFailureTests'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsOverwriteTargetVerificationTests|FullyQualifiedName~WindowsOverwriteTargetRaceTests'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsOverwriteTargetAdmissionTests'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~OverwritePreparedAsyncWhenNativeTargetCallThrowsRetainsMayHaveChangedAsync'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsOverwriteTargetProtocolTests|FullyQualifiedName~WindowsOverwritePreparation'
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-overwrite-target-static-190.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-overwrite-target-static-192.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-overwrite-target-static-194.ps1
python D:/NeNeCommander/evidence/transfer-conflicts-20261125/freeze-overwrite-target-195.py
```

Final independent read-only review19:58:35 accepts the internal target-write stage with
no mandatory correction. No broad provider/App/coverage/mutation/CI scope runs for this
unexposed prerequisite. Full ADR0095 canonical/deep proof, actual completion append,
owned cleanup, verified source-delete stream/attribute proof and provider/gateway wiring
remain required before capability activation. Current tests establish creation-time/
object retention, not complete final metadata/environmental acceptance. Case-sensitive
directory evidence120/124 remains unfulfilled. Folder merge stays separate. All test
mutations use owned D roots; normal settings/shared input remain unchanged.

At19:59 hide explicitly authorizes the FINAL beta.5 candidate3c4fb93 public push/Draft
PR/candidate CodeQL registration and the saved Issue562 body. The earlier43e8320question
is superseded by that final candidate. This local #560 code is excluded from those
fixed public payloads. Save this accepted local checkpoint, then execute the approved
external actions; merge/tag/release assets are excluded from the publication approval.

## Initial-bound completion append — 20:38 JST

The target-write stage is saved as7b5fe50. The SAME WindowsOverwriteAttempt now
consumes ContentsVerified before the first completion await. Its recovery writer
admits the same live owned record and parent chain after target content effects,
requires the FIRST complete initial frame, its Confirmed flush and exact bytes/EOF,
and writes only after that prefix. No completion path calls truncate or rewrites the
initial mapping. The completion is encoded by the existing codec and cryptographically
bound to the exact initial frame. Complete initial-plus-completion readback and EOF
precede the new verified append observation.

ArtifactOwner retains that append evidence BEFORE later native metadata admission,
flush or cancellation. Initial and appended frame/flush observations remain independent.
A failed first/partial/full write or failed readback is OutcomeUnconfirmed; readback
success followed by metadata failure retains the verified frame with flush NotAttempted;
flush failure retains that frame with OutcomeUnconfirmed; acknowledged flush is saved
before later cancellation. Original target/source and all preparations remain, and
provider effects remain empty. No automatic retry, rollback or cleanup follows failure.
Target path qualification becomes LastKnown when entering this later stage.

Desired target evidence still precedes cancellation, but the ContentsVerified success
transition now follows the final cancellation check. Therefore a target stage that
reported cancellation cannot be resumed into append merely because desired bytes had
already been verified. Append also rejects concurrent entry, pre-cancelled replay,
unprepared entry and all later retries. Successful append is still not provider Copy
success and grants no source deletion or cleanup authority.

Build196 stops at the required conditional-expression style;197 stops at new test
collection/using style.198 retains a test Unicode-character compilation failure caused
by a local edit's implicit text decoding. The intended one-character fixture is restored
using explicit UTF-8. Build199 succeeds with0warnings/0errors and25selected cases pass.
Consumer200 reuses that build and passes58directly impacted preparation/initial-writer/
artifact-evidence/target-verification cases. Four cases overlap, giving79distinct cases,
including24new cases. Existing provider/App/public-record proof is unchanged and reused.

Native cases cover empty/short/multibuffer target content, a multibuffer Unicode initial
mapping with short native reads, unchanged original prefix, no truncate, first-write/
partial/full-write exceptions, malformed readback/extra bytes, initial mapping substitution,
changed initial bytes/EOF, unconfirmed initial flush, post-readback metadata failure,
independent flush failure/acknowledgement, cancellation and asynchronous reentry. Actual
source/target/backup/stage stay readable and all3artifact links remain. This is owned D
filesystem proof; it is not hardware durability, process-crash cleanup, current metadata
fidelity or complete destructive feature acceptance.

201formats/verifies only the10changed C# files, passes conformance112, and retains exactly
the same2prior complexity findings (exit1 remains explicit).202freezes1530inputs:
5changed prior files,5new files,1520unchanged; whitespace-only body parity is verified.
Independent read-only review20:37:11 has no mandatory correction or scope gap.

Exact commands (TEMP/TMP remain in the task-owned D proof-temp root):

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-completion-196.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-completion-197.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-completion-198.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-completion-199.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-completion-consumers-200.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-completion-static-201.ps1
```

The scripts preserve exact build/test filters, start/end times, exits and raw logs.
No full gate/coverage/mutation is rerun for this unexposed internal prerequisite. Full
ADR0095 destructive proof remains necessary before enabling capability. Owned-link
cleanup, fresh source-delete streams/attributes, actual provider/gateway/scheduler wiring,
case-sensitive environmental proof120/124 and separate directory merge remain open.

Separately, hide's19:59 exact beta.5/source/PR/SARIF and Issue562 publication approval
has been executed: DraftPR582/head3c4fb93, candidate CodeQL analysis2247313539 with164rules/
0results/0open branch alerts, and saved Issue562 body match. The candidate's provisional
package/extraction/non-input-desktop startup are verified and independently accepted.
The20:18 async question for main integration/final package/tag/Preview release is pending;
do not infer that permission from the earlier scoped approval or elapsed time. Read
D:/NeNeCommander/evidence/release-beta5-20261125/publication-handoff-03.md. This feature
is excluded from beta.5. No normal settings/shared input change or eligible/rejected
cleanup exists; all unfinished work/evidence/output is retained on D.

## Held artifact-link removal prerequisite — November 25, 21:17 JST

Completion append is saved at `2d8b3c8`. This next bounded change adds the native
held-link primitive and its tests only. It has no production caller in the attempt,
artifact owner, provider or UI. Complete cleanup admission/receipts, source deletion
and ADR-0095 capability acceptance remain unimplemented by this prerequisite.

The earlier `native-owned-cleanup-205.py` feasibility probe passed 45 assertions in
five isolated owned cells. POSIX deletion plus acknowledged close removes the held
link; an existing reader and a late alias retain the original bytes, and a new
same-name object is untouched. Readonly status `C0000121` and unsupported flags
`C00000BB` refuse. Those fixtures and results remain; this probe is not product proof.

`WindowsTransferArtifactRemoval` uses `NtSetInformationFile`, information class 64,
with DELETE and POSIX_SEMANTICS only. It receives a held SafeFileHandle and no path.
The caller must freshly admit ownership/content and record uncertainty before entry.
The same protective DangerousAddRef spans disposition, raw CloseHandle and
SetHandleAsInvalid; DangerousRelease runs afterward. A nonzero NT status, failed
close or exception returns no acknowledgement. Only acknowledged close consumes the
SafeFileHandle. No readonly override, alternative flags, reopen, retry or rollback
is added. A disposition acknowledged before a failed close can still take effect on
later disposal; the future canonical owner must retain Uncertain in that case.

The [Windows disposition contract](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/ns-ntddk-_file_disposition_information_ex)
and [CloseHandle contract](https://learn.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-closehandle)
were checked with the [.NET reference lifetime](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.safehandle.dangerousaddref?view=net-10.0)
and [invalidation contract](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.safehandle.sethandleasinvalid?view=net-10.0).
Removal of the one visible link is distinct from destruction of data reachable
through other handles/aliases. This primitive alone does not admit a late alias or
prove current source/target content; higher-level admission remains necessary.

`run-removal-206.ps1` runs:

```powershell
dotnet format whitespace tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --no-restore --include <four new files>
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsTransferArtifactRemoval'
```

All commands exit 0. Build has 0 warnings/errors; all 11 cases pass without skips.
Cases cover actual held-link removal/new occupant, existing reader, late hardlink,
readonly refusal, dispose reentry inside native close, injected NT failure/exception,
unacknowledged close/exception after actual disposition, and invalid/closed handles.
The last two close cases prove why a later Dispose cannot justify a Retained claim.
Tests create/mutate only their existing TestOwnedTemporaryRoot under the owned D
proof-temp location. Shared input, normal settings and other applications are untouched.

207's format verification and conformance (112 rules) pass. The standalone complexity
inspection still exits 1 with exactly the prior two findings; no threshold, baseline
or suppression changes. `removal-current-inputs-208.json` matches all 1,530 prior
inputs and adds only four new C# files (1,534 total). No unchanged product suite,
App build, full CI or mutation is repeated for this unconnected prerequisite.
Independent read-only review is pending at this checkpoint.

Meanwhile beta.5 was separately approved through publication at 20:42 JST and is now
[public](https://github.com/hideyukiMORI/NeNeCommander/releases/tag/v0.1.0-beta.5)
from main `5727258`, with all three downloaded public assets hash-matched. Completion
documents are integrated by PR #584/main `9603e90`. Verified archival of completed
release/feature copies is running; this unfinished #560 tree, #562/#545/#564 and the
interactive trial roots are explicitly outside its deletion scope. The earlier
publication-hold paragraphs in this chronological record are superseded by these facts.
Independent read-only review at 21:17:40 JST accepts only this unconnected primitive
with no mandatory correction. The exact ABI and handle lifetime order, 11 cases,
4-file hashes and unchanged two complexity findings were checked. Its use is limited
to the synchronously opened CreateArtifact handle, serialized once with no outstanding
I/O. DangerousAddRef protects managed release; it does not serialize concurrent raw
native calls. The next attempt/owner stage must enforce one-shot lifetime and await
all I/O before removal/disposal. This precondition is now explicit in the primitive's
XML comment; code-body parity is exact, so successful 206/207 proof is reused.

## Accepted internal owned-artifact cleanup and stop — November 25, 22:17 JST

The held-link primitive is saved as `5a10ff3`. At 21:26 automatic approval rejected
integration before execution because specific future irreversible file deletion had
not been explicitly authorized. The concrete proposal is retained as
`cleanup-integration-proposal-211.md`; hide explicitly approved that internal scope
and its dedicated-file verification at 21:43. Approval `cleanup-integration-approval-212.json`
resolves that hold. It excludes source deletion and provider/UI activation.

The same WindowsOverwriteAttempt now consumes its successfully returned completion
stage before cleanup's first await. WindowsOverwriteCleanup borrows the same owners
and records no second receipt. Before EACH removal it rechecks original source facts,
complete desired source/target bytes, length, digest and EOF, the exact target/ancestry,
all remaining artifact full identities and first content digests, and exact completed
recovery frames with both confirmed flush observations. Native identities are repeated
after I/O. Order is prepared, original backup, recovery last. No pathname reopen,
foreign cleanup, retry, rollback or source deletion is introduced.

ArtifactOwner consumes removal access and records Uncertain plus LastKnownPath BEFORE
the unchanged native primitive. Only acknowledged close records Removed. Later failure,
cancellation or disposal preserves each partial acknowledgement and historical content;
uncertain close can still remove its link during Dispose. Concurrent entry and replay
refuse. Failure invalidates target current-path qualification without erasing the desired
content observation. The final lifetime check also refuses disposal during verification.
Successful cleanup still supplies no completed provider effect or deletion authority.

Changes are six production files (VerifiedFileContent, WindowsOverwriteAttempt,
WindowsOverwriteDependencies, WindowsTransferArtifactOwner, WindowsTransferRecoveryWriter
and new WindowsOverwriteCleanup), two existing test fixtures and eight new cleanup test/
fixture files. ReadHeldAsync extends the existing content verifier with bounded reads;
no alternative content or recovery codec is added. Tests operate only on dedicated D
files through TestOwnedTemporaryRoot; ordinary settings/shared input stay unchanged.

Proof preserves the original failures:

- 213/214 stop on style/test diagnostics; 215 builds with zero warnings/errors and
  passes 31 cleanup cases. 216 retains a test pattern-analyzer failure.
- 217 builds and passes 16 of 19 cases; three empty-ADS fixtures fail because their
  File.WriteAllText handle lacks FileShare.Delete. Corrected 219 uses explicit
  create-new FileStream sharing and passes nine admission plus two completion-failure cases.
- 218 formatting passes; two raw-regex conformance findings arise from test constructor
  lambdas, and one new cleanup cognitive-complexity finding accompanies the two prior
  findings. Same-class content responsibility extraction and equivalent local callbacks
  remove those new findings without threshold/rule/suppression changes.
- 220 builds with zero warnings/errors and passes 130 selected cleanup and directly
  affected preparation/target/completion/owner/recovery cases.
- Independent review identifies two stale target-current-path cases. 221 retains the
  new fixture's nullable compilation failure; corrected 222 reproduces two failures
  with one success control. The try/finally target invalidation and final disposed-state
  check then pass all 14 selected affected cases in 223, with zero build warnings/errors.
- 224 format verification and all 112 conformance rules pass. Standalone complexity
  exits 1 with exactly the same complete two finding objects as 207: KeyboardHelpPresenter
  nesting 4 and SelectionTranslationTests cognitive 11. There is no new finding.

There are 55 distinct cleanup cases; overlapping run totals are not added together.
The unchanged primitive's 11 cases from 206 remain reusable. Independent read-only final
review at 22:12:01 JST requires no further correction within this internal scope.
`cleanup-current-inputs-225.json` freezes 1,543 inputs: 1,527 unchanged, seven changed
prior files and nine new files against 209. `cleanup-independent-review-225.json`
records the final scope and exact unchanged complexity findings.

Exact executed entry points (each script retains its full filter, commands, timestamps,
exit codes and raw logs in D:/NeNeCommander/evidence/transfer-conflicts-20261125):

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-213.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-214.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-215.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-216.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-217.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-static-218.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-219.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-220.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-221.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-222.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-223.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261125/run-cleanup-static-224.ps1
```

ADR-0061 scope is internal native cleanup plus directly affected owners/consumers.
No unrelated App/full gate/CI/coverage/mutation is rerun. Full ADR-0095 destructive
acceptance, source-delete streams/attributes, provider/gateway/scheduler connection,
case-sensitive environment evidence 120/124 and separate directory merge remain open.

hide requests a suitable stopping point and daily report/handoff at 22:08. Finish only
this correction, records and normal-hook local save; no new feature/publication starts.
The [closing daily report](../reports/2026-11-25-milestone-daily-report.md) and
[closing handoff](../handoffs/2026-11-25-milestone-handoff.md) own the stopped state.
Separately, administrative release cleanup completed at 22:02:47: 126,503 archived
files / 16,907,579,614 bytes and 16 link identities verified, all 20 eligible targets
absent. No administrative cleanup was rejected or remains pending. This unfinished
feature, selection/filter/trial trees and interactive outputs are retained.

## November 26 resumption and accepted-main reconciliation

hide resumes milestone work until stopped. The November25 stop and publication holds
remain historical. beta.5 is published; selection PR585 and filter PR588 are integrated.
Filter archival runs separately from this unfinished task. Normal settings/shared input
remain unchanged.

baseline-correspondence-01.json confirms all1,543 inputs match15499d8 before edits.
The original branch and21 local commits remain. original-work-02.patch captures the exact
27b92cd..15499d8 delta and git apply --3way applies it on accepted mainad85de1.
Only PROJECT_STATE insertion conflicts. Both complete histories are kept in descending
date/time order; the one undated November25 paragraph remains last for that day.
The first sorting attempt stopped before writing because it expected every date to
contain a time. No production conflict/correction occurs. Source/test/tool comparison
against15499d8 differs only at main's already accepted SelectionTranslationTests.
Its direct-consumer proof will be reconciled with the next affected build/test scope.

The next bounded contract extends the SAME VerifiedCopyReceipt file evidence and
VerifiedSourceDeletion admission with required stream/attribute shape. Unknown/changed
observations retain source; no alternate deletion route is introduced. Internal cleanup
acceptance does not activate source-delete/provider/UI capability. Full ADR0095 proof
and case-sensitive-directory environment120/124 remain necessary before acceptance.

Permanent resumption receipts are D:/NeNeCommander/evidence/transfer-conflicts-20261126.
Read-only setup misses used Infrastructure instead of Infrastructure.Windows, a guessed
verifier filename, a PowerShell native wildcard and one git query without its workdir.
They changed no source; rg and explicit paths corrected them. Original failures remain.
No feature publication or task cleanup is claimed.

## November 26 mandatory verified-file requirements

The next internal prerequisite adds FileVerificationRequirements to the same
VerifiedCopyEntry.File factory and VerifiedFile evidence, with no optional/default
compatibility path. All 15 prior callers explicitly retain DefaultStream. Strict
OverwriteContents requires the existing native local-NTFS/regular/single-link/
Normal-or-Archive/default-only admission. VerifiedTreeAccess reuses the exact same
WindowsTransferPathLease and WindowsOverwriteFileAdmission; it compares full source,
full target and source again, and explicitly refuses an object alias. The shared
hash verifier checks requirements and VerifiedSourceDeletion repeats them after its
final leaf Capture calls. No alternative deletion method or metadata/stream cleanup
is added. The ordinary production verifier still emits DefaultStream; no overwrite
provider/UI choice is enabled by this prerequisite.

Tests use owned D files and real gateway copies; the fixture first asserts that the
ordinary producer emits DefaultStream, then supplies strict internal evidence. Eight
source/target variations cover hidden attributes, empty/nonempty ADS and hardlinks
while restoring the exact recorded full identity, so failure depends on the new native
shape check. Empty/nonempty strict success and ordinary ADS behavior are controls.
Additional boundary tests cover foreign providers, full-identity changes, exact native
source/target alias, source changes during target observation and late leaf changes.

- shape-red-04: normal Infrastructure test build has zero warnings/errors; eight
  strict-shape failures reproduce with three controls passing before admission exists.
- shape-green-05: IDE0048 requires explicit logical parentheses; retained as a failed
  build, corrected without suppression. shape-green-06 builds Infrastructure and
  Application tests with zero warnings/errors; Infrastructure 52 and Application 33
  selected cases pass. The Infrastructure selection includes the 11 new native cases
  and the three directly affected verifier/deletion boundary classes.
- shape-boundary-edits-07's initial JavaScript template syntax error occurs before
  tool execution or file change. The corrected boundary tests are saved normally.
- shape-causal-08 temporarily removes only the final-leaf requirement check. All four
  late-Capture cases fail; finally restores the exact production SHA256. Restored
  shape-boundary-09 builds with zero warnings/errors and passes all 12 boundary cases.
- Independent review identifies that the first alias fixture mixed different snapshot
  fields and could fail at another guard. The corrected test uses one actual file's
  complete qualified snapshot for both sides. shape-alias-causal-10 removes only the
  explicit alias guard and produces one failure, then restores the exact source hash.
  shape-alias-11 builds with zero warnings/errors and passes the corrected alias plus
  four directly affected cases. The other seven late-boundary test bodies are unchanged.
- Final independent read-only review at 09:00:16 JST accepts this internal requirement/
  admission scope with no mandatory correction. It confirms both causal failures,
  restorations and final normal builds; provider/UI connection and full acceptance
  remain explicitly separate.

Both causal runners retain original source snapshots, their temporary mutation and
restoration checks; no mutant source or built assembly remains. The 23 new native and
two Application cases are distinct; overlapping run totals are not summed. Read-only
setup misses (guessed plan/port/execution-directory filenames, early reads of unfinished
logs, and a git ownership check under the sandbox identity) made no changes. Correct
paths are obtained from rg; no global safe-directory configuration is changed.

Executed entry points (permanent scripts retain exact commands, filters, times, exit
codes and complete logs beneath D:/NeNeCommander/evidence/transfer-conflicts-20261126):

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-shape-red-04.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-shape-green-05.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-shape-green-06.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-shape-causal-08.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-alias-causal-10.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-shape-consumers-12.ps1
```

ADR-0061 scope is the mandatory file evidence, fresh deletion admission and direct
consumers. The final runner performs changed-file canonical whitespace/verify and body
parity, normal App and Presentation-consumer builds, VerificationTestEvidenceTests and
SelectionTranslationTests, record closure, conformance and complexity Inspect. Its
result is recorded below after readback. Unchanged successful native/owner evidence
is reused; no unrelated suite/coverage/mutation/full CI or physical-input test is run.

ADR-0069's final-query-to-path-deletion interval remains; read-only admission does not
freeze future streams/attributes or mapped writers. Strict receipt production and
gateway downgrade refusal, provider/scheduler/UI wiring, full ADR-0095 destructive
proof and actual case-sensitive environment 120/124 remain open. Directory merge is
separate. No task publication or cleanup occurs; ordinary settings/shared input and
all unfinished copies remain unchanged.


### Final selected proof and requested stop — November 26, 09:14 JST

shape-consumers-12 passes canonical changed-file whitespace and verify with all 18
source bodies unchanged. Normal App and Presentation-consumer builds have zero warnings/
errors. Presentation 62 cases pass (59 selection translation plus three evidence fixture
cases). Record closure reports 170 abstract records, 154 visible closed roots, zero
failures. Conformance then correctly rejects boolean mode parameters in three new
fixture/test files; the failed log remains, and complexity was not reached in run 12.

The fixture now accepts actual optional ADS content and a typed affected path. Test
tables use validated source/target selectors; observation defects remain explicit test
inputs. No production body changes. shape-final-13 passes format/verify, normal build
with zero warnings/errors, all 23 affected native cases and conformance 112. Complexity
Inspect exits 1 with the sole original KeyboardHelpPresenter nesting 4 / limit 3 finding.
The accepted main selection-test parameterization removed its former cognitive finding;
no unrelated product fix, threshold relaxation or clean-complexity claim is made.

Exact final entry point:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-shape-final-13.ps1
```

Independent final read-only review at 09:12:30 JST accepts the correction without a
mandatory change and confirms equivalent fixture meaning/injection phases, reusable
causal proof and the final results. shape-independent-review-14.json retains that scope.
shape-current-inputs-14.json freezes 1,548 inputs: 1,527 raw identical, 15 changed prior
inputs, five new inputs and one CRLF-only difference in WindowsTransferArtifactRemoval.
shape-line-ending-correspondence-14.json proves that file's prior raw hash equals the
current LF normalization and both old/current Git blobs are identical. The accepted
selection test is included among the 15 changes; it is covered by the 62-case consumer
run. No unrelated test or the already successful App/closure is repeated for fixture edits.

hide requests a suitable stop and daily report/handoff at 09:11. No next-stage provider,
scheduler, gateway or UI source edits begin. The closing daily report and handoff below,
this quality record and normal-hook local save form the stopping point. The final local
HEAD, exact staged paths, Commit/message hook result and clean status are recorded in
shape-commit-16.json/log. This feature remains local and incomplete; no push/PR/main
merge/release or task cleanup accompanies the stop. Main is f7c24b6 after completed
selection/filter documentation; this feature's integration base is ad85de1.

See [November 26 daily report](../reports/2026-11-26-milestone-daily-report.md) and
[stopped handoff](../handoffs/2026-11-26-milestone-handoff.md). Resume only when hide asks.


The first normal-hook save15 passes conformance112 and security24 but rejects one new
blank line at EOF in VerifiedFileRequirementBoundaryTests. The pre-stage diff check
had not included this then-untracked file. No commit is created; the failed receipt/log
remain. Only trailing newline bytes are corrected, with identical non-whitespace body.
shape-current-inputs-16.json supersedes14 for final byte hashes, preserving14 unchanged.
Successful behavior/build/format proof is reused; git diff --cached --check and the
normal hooks are run again for save16. No hook is disabled and no product test rerun.


## Provider integration checkpoint — November 27, 14:25 JST

hide requests resumption after reading the November 26 daily report/handoff. The
1,548 saved input hashes all match d8e884c and the tree is clean. Accepted main
f7c24b6 adds only four completion-document files. Their PROJECT_STATE insertion
conflict is resolved by retaining both histories, without duplicating the old entry.
Normal hooks save the documentation reconciliation as b2908fb.

The first commit attempt03 shows no progress after security-check entry. Only its
owned child PID3220 is interrupted after verifying parent29716 and the exact script
path; the failed log and process receipt04 remain. Normal retry05 passes all hooks
in approximately 107 seconds. A separate, unchanged security-check invocation06
passes in 6,202 ms. The attempted phase breakpoints emit no events, so no phase
timing, confirmed root cause or permanent performance fix is claimed. No hook,
security rule or threshold is changed. hide asks about reliable shortening; this
work groups coherent changes/checks, reuses successful unchanged proof, and does not
repeat the successful App build after test-only corrections.

### Implemented boundary

The single WindowsLocalFileOperationAdapter.CopyAsync queues work in the existing
WindowsLocalIoExecutionBoundary and unwraps the inner task. WindowsOverwriteCopy
retains WindowsOverwriteAttempt before effects and awaits prepare, overwrite,
completion append and cleanup before disposal and the final canonical receipt.
Expected I/O/access failure and cancellation retain target/artifact knowledge.
Copy success requires complete cleanup; no retry, fallback or rollback is added.

OverwriteContents is a closed conflict decision only when qualified evidence exists.
Resumed preflight reopens/revalidates the exact original pair and reserves the original
ordinary target, refusing changed bytes, added streams and disappeared targets before
allocating artifacts. Copy independently checks source, target and destination binding.
Gateway and provider both refuse atomic move. The existing tree verifier emits strict
OverwriteContents requirements; the gateway rejects default-only requirements, a
directory or substituted source/target objects before the existing deletion step.

The App composes one WindowsIdentifierSource for settings and provider operations.
Local adapter/router constructors require explicit injection. The migration receipt11
records 70 old test calls supplied with deterministic identifiers; direct step tests
are also updated. The dependency holder contains only the existing local platform
owners and the overwrite dependencies. No dependency/package, tool policy or UI
resource is added.

### Selected proof and retained failures

- Infrastructure build07 passes; test build07 refuses an unused using. Build08 then
  refuses an unused TrySetResult return. Both test-only style defects are corrected.
  Build09 has zero warnings/errors and all 12 new orchestration cases pass: empty and
  ordinary content, stale target, cancellation, partial writes, completion flush and
  partial cleanup. Source remains after every incomplete step.
- Product build12 passes with zero warnings/errors. Application build/test13 passes
  33 selected cases, including four strict-proof downgrade/object/directory refusals,
  the accepted strict deletion path and the original-target plan binding.
- Infrastructure build13 exposes four missed direct-step constructor updates and
  inappropriate test access to an Application-internal snapshot mutation. No friend
  permission or public product API is widened: OverwriteSnapshotPort obtains the
  bound snapshot through the real gateway and stops at the declared test port.
- Build/test14 passes 85 directly affected native/ordinary-copy/move/scheduler/
  constructor/router cases, zero warnings/errors.
- Format/verify15 proves whitespace-only body parity for all changed C# files.
  The normal App build15 succeeds with zero warnings/errors in 130.41 seconds.
  Test build15 refuses a constant array in the new resumption assertion. The same
  order is asserted individually; only that file is formatted again in16.
- Build/test16 passes 19 cases: the real resumed move through preflight/copy/strict
  verification/deletion, four changed-original refusals, directly affected existing
  conflict/preflight cases, and the revised inner-await observer. These overlap
  earlier selections; counts are not summed as unique cases.
- Record closure16: 170 abstract records, 154 visible closed roots, zero failures.
  Conformance16: 112 rules pass. Complexity16 exits1 with exactly the original
  KeyboardHelpPresenter nesting4/limit3 finding; its complete finding object equals
  shape-complexity-13 from November26. No new finding or threshold change.
- Architecture build/test17 succeeds with zero warnings/errors; the one compiled
  dependency-direction case passes. No unrelated architecture or UI suite runs.

The frozen successful code inputs and explicit formatting/body correspondence support
reuse. Application33 and Infrastructure85 are not repeated after unrelated later
preflight/test/document edits. The full App build is not repeated for a test assertion.
The changed resumption path receives its own19-case proof. Prior native attempt,
receipt, cleanup and strict-delete proofs remain applicable to unchanged owners.

Exact scripts and direct-command metadata are preserved under
D:/NeNeCommander/evidence/transfer-conflicts-20261127:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-copy-proof-09.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-provider-final-15.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-provider-final-16.ps1
```

application-proof-13.json, infrastructure-proof-14.json and
provider-architecture-17.json retain each exact build/test command, project and filter.
All failed attempts07/08/13/15, the interrupted commit03, retry05, profiling limits06
and read-only setup misses remain distinct from successful results.

### Activation, publication and preservation limits

This is the bounded internal provider/gateway integration from the stopped handoff.
Initial production conflict construction still supplies null overwrite evidence;
only dedicated test ports introduce the qualified initial conflict. No visible
overwrite choice, normal-settings change, shared-desktop input or release occurs.
A focused source self-review checks lifetime, original binding and proof downgrade
admission; no new independent/full destructive acceptance is claimed.

Initial qualification and explicit three-locale consent UI, full ADR0095 native/race/
coverage/mutation/deep-review proof, actual case-sensitive environment120/124 and the
separate directory merge scope remain. This stage is not review-ready release proof
and cannot close #560. Main/public beta.5 stay unchanged.

Keep the unfinished issue-560 worktree and outputs, all original branches/commits and
permanent evidence. No cleanup is eligible or blocked. Current inputs are recorded in
provider-current-inputs-18.json; normal-hook local save and final clean state are
recorded in provider-commit-19.json/log. No push/PR/main integration accompanies this
internal checkpoint. See the latest entry in the transfer handoff.

## November 27: initial qualification and unexposed consent/result checkpoint

### Canonical mechanism and held activation

Prior provider integration is saved as705d392. The follow-up retains the SAME gateway,
exact conflict owner, provider receipt and operation bar. Closed TransferOverwriteCapability
now contains either original evidence or a TransferOverwriteUnavailableReason; every
factory/caller migrates without a compatibility overload. WindowsOverwriteEligibility
uses the existing pair/held/native owners, verifies the case-insensitive target parent
and artifact candidate shape, and creates neither a file nor an operation token.

The inspector remains UNCONNECTED to initial production preflight. Both conflict
sites explicitly supply Unavailable(Unconfirmed). Automatic approval rejected the
connection before execution at15:06JST: initial production activation could expose
irreversible overwrite before complete repository acceptance proof. The exact decision
is activation-review-rejection-38.json. No rejected edit/script ran and the connection
was not retried through another mechanism.

The gateway and choice factory refuse an unavailable overwrite. Overwrite-all includes
only eligible original objects already in the displayed conflict set. Unsupported
rows and later discoveries cannot inherit it. The existing modal shows full original
paths, file size/time, in-place effect, late-alias visibility and partial-stop/no-auto-
restore wording in three languages. Separate ordinary/overwrite scopes start unchecked;
Cancel keeps initial focus. Same-owner text updates retain controls/focus/scope.

The operation bar discloses the existing target/source/artifact facts: untouched,
original/desired verified or potentially changed target; source not deleted, removed,
partly removed or deletion unconfirmed; artifact role, verified/partial/unknown content,
retained/removed/uncertain link and current/last-known path. Content verification does
not hide overall failure. Unacknowledged Move deletion is not called retained.
Recovery advice requires fresh manual comparison; there is no recovery executor,
new result abstraction, visual token, package or ordinary-settings write.

### Selected proof and failures

- qualification25 passes Infrastructure106 and Application32. It includes native
  empty/nonempty eligibility, streams, aliases, attributes, changed identities and
  existing held-path/pair/post-content boundaries.
- consent31 passes25/27. Two observers incorrectly included pre-Resume read-only
  inspection/preflight calls in their no-new-provider-work assertion. Corrected32
  compares calls immediately before/after Resume; only those2cases rerun and pass.
  The unchanged25 successes are reused. consent33 passes13 presentation cases.
- result37 passes41 result/presenter cases after retained build36 diagnostics.
- App39 has0warnings/errors. Closure40 is172abstract/156visible/0failures;
  conformance40 passes112. Complexity40 finds4new violations in3methods plus the
  original Help finding. Flat ordered switches and separate kind validation correct
  them; style failures41/42 remain. Final43 passes Infrastructure49, Presentation41,
  App build and conformance112. Complexity43 retains exactly the original
  KeyboardHelpPresenter nesting4/limit3 object, exit1. Counts overlap prior scopes.
- Final XAML-only App52 has0warnings/errors; conformance52 passes112. Unchanged C#
  tests, closure and complexity proof are reused.

Exact commands/project/filter/results remain in qualification-proof-25.json,
consent-proof-31/32/33.json, result-proof-37.json, consent-static-40.json,
consent-correction-43.json and consent-app-52.json. The retained commands include:

~~~powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-qualification-proof-25.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-consent-proof-32.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-consent-proof-33.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-result-proof-37.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-consent-correction-43.ps1
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore
pwsh -NoProfile -File eng/conformance.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-consent-locales-53.ps1
python D:/NeNeCommander/evidence/transfer-conflicts-20261127/verify-consent-parity-54.py
~~~

### Native layout, observer limits and parity

The diagnostic copy is outputs/transfer-conflicts-20261127/consent-controls-44 under
D:/NeNeCommander. It obtains a real unavailable conflict, then gives synthetic
qualified/result states to the unmodified view adapters/XAML with capture-only
callbacks. No synthetic overwrite invokes a provider. This is native presentation
proof, not activation or complete overwrite acceptance.

Native1 finds Cancel outside the viewport. Native2 repeats it after awaited bitmap
rendering; the one-Grid wrapper experiment3 does not help. Observer4 identifies a
one-row KeyHintWrapPanel measurement followed by a second-row Cancel arrangement
at its right-aligned final width. Only the footer's allocated width and modal
stacking order are corrected. The wrapper experiment is discarded and the shared
wrapping panel is unchanged. Native5 passes49 and its images confirm the correction.

Diagnostic builds44/46 have retained C# errors with consequent XAML diagnostics;
corrected45/47 build cleanly. Native6 passes49 but inherits narrowed private settings,
so it is not two-width proof. Final53 explicitly sets wide size, verifies actual
locale and strictly smaller narrow width, and awaits captures before scrolling.
Earlier top/bottom image labels are not used as scroll-position proof.

Final native7/8/9 pass52each for en-US/ja-JP/zh-Hans at actual root widths745.6/529.6DIP.
They cover Cancel geometry/focus, independent scopes, original-owner submission,
stale peers, refresh, unavailable rows, long paths, scroll access and result focus.
English wide/narrow/result and Japanese/Chinese narrow consent/result images are
inspected. Ordinary settings length/timestamp/hash and input desktop remain unchanged.
Physical keys, speech and full interactive/operation acceptance remain separate.

parity54 proves187App inputs=184raw exact+3declared diagnostic substitutions
(project, lockfile, composition), with individually matched composition replacements
and4normal/frozen/runtime DLL hashes. Three689-key catalogs have equal key sets;
new conflict/result keys and format placeholders match. Source review is direct;
no new independent/full destructive acceptance or external source review is claimed.

### Save and next authorization

Freeze55 records1575inputs:1520unchanged from provider18,37changed,18new, plus the two
still-unavailable production sites. Normal Commit/message hooks save this coherent
checkpoint; consent-commit-57.json owns final HEAD, exact staged list and clean state.

Next ask hide to approve connecting the existing inspector to the sole initial
preflight in this unpublished branch, followed by dedicated-file integration and
full ADR0095 proof before publication/main integration. Approval does not waive gates
or authorize ordinary-file experiments. Include reservation-only/duplicate destinations
and later conflicts, original binding, native races/crash, coverage, changed-layer
mutation/deep-review and case-sensitive120/124. Directory merge remains a separate
unaccepted scope;560stays open. No push/PR/merge/release or eligible cleanup occurs.
Keep the unfinished worktree, outputs25/26/27, permanent evidence and original branches.

## November 27: authorized local connection and full acceptance in progress

hide explicitly approved the held initial-preflight connection at15:41JST; the exact
question, answer and bounded scope are retained in activation-approval-58.json.
It supersedes the earlier automatic-approval hold only for this unpublished local
branch and dedicated-file full ADR0095 proof. Main, publication, release, ordinary
files/settings/shared input and directory merge remain outside that authorization.

The SAME WindowsLocalTransferPreflight now invokes WindowsOverwriteEligibility.
Original and alternate targets share the existing reservation set; reservation-only
and duplicate/later targets cannot obtain overwrite evidence. Real native resumption
tests now use the initial inspector rather than injecting qualified conflicts.
The gateway, exact modal, copy/verification/deletion owners and one receipt remain.

### Corrections and native proof

- activation60 passes41selected cases. Independent read-only review identifies that
  InvalidDataException is not IOException. Four native unpaired-surrogate source/target
  paths fail62. The initial inspector now checks canonical strict-codec text, and
  WindowsOverwriteCopy normalizes InvalidDataException while retaining its disposed
  attempt receipt. Corrected63 passes55cases; the independent reviewer accepts it.
- canonical61 is deliberately interrupted during negative proof before that correction;
  its partial successful observations are not a canonical pass and no child remains.
- Native10/11/12 are invalid observations because WinUI Desktop does not support the
  invoked focus-search APIs. Real owned-HWND messages13 prove the consent list is
  tabbable but result evidence cannot be reached from the three header commands.
- The SAME CommandEntryView owner and KeyboardContext now include the visible result
  viewport in native Tab/Shift+Tab order. Native scrolling remains native; F10/Escape
  return through the existing route. F8 in details changes no pane/operation state.
  No new shortcut, key map or execution path exists. The viewport and consent list
  have localized accessible scope names; three catalogs now contain690keys each.
- App68's retained enum-style diagnostic is corrected in69. Native14's pane observer
  wrongly compares newly created snapshots by reference; only the observer changes
  to value comparison. Native15 proves reachability, scroll and return; final16/17/18
  pass66each in actual en-US/ja-JP/zh-Hans. Ordinary settings, input desktop and owned
  source/target bytes remain unchanged. Physical keyboard and speech are unmeasured.
- activation-final-parity-74.json proves187App inputs:184raw exact plus three declared
  diagnostic substitutions, and four normal/frozen/runtime DLL matches at that point.
  Its capture-only synthetic qualified/result callbacks never execute overwrite.
- ProductCrashProbe-77.cs/run-product-crash-78.ps1 use exact compiled gateway/provider
  DLLs with diagnostic native-call barriers. Eight owned process terminations plus
  normal verified Move pass124checks. These are specified process-stop points, not
  power loss, every timing, automatic recovery or complete WinUI overwrite. Cleanup
  stops after native close before provider acknowledgement. Mapping is checked by
  stable ID/path/hash/length, not independently pre-captured complete identities.
- hide executes the dedicated administrator setup81 at16:45:44. Normal-user native
  flag1 and14actual case-sensitive refusal/unchanged-file checks pass85. No pending
  administrator step remains and no ordinary directory flag changes. The dedicated
  root stays retained with unfinished evidence for later verified cleanup.

### Canonical stages, coverage and semantic reconciliation

canonical75 fails at WindowsTransferArtifactRemoval.cs's missing final newline after
conformance112, all negative gate/security proofs,24registered threats, deterministic
131theme outputs and locked restore succeed. Keep that original failure; reuse its
unchanged successful stages. Full format/build/closure/Architecture41 pass88; build
has0warnings/errors. Two CodeQL test findings receive explicit using ownership and
an existing-writer-handle validity assertion. The newline helper's CRLF translation
introduced blank lines; intermediate88 is retained and89 restores exactly the frozen
original bytes plus one final newline. Product nonwhitespace bodies are unchanged.

Coverage89 runs the complete three affected suites: Application1373success but
99.90128331688055% against100%; Infrastructure1562success/20skip/0failure at
93.08072487644151%; Presentation643success at97.3753280839895%. environment-skips-95.json
records17LiveWSL roots absent and3LiveRecycle owned parents absent, separately from
this local NTFS feature. Neither live tier is claimed or enabled by these tests.

The two missing Application outcomes are Cancel decision and missing overwrite
qualification in MatchesTransferTarget. A Cancel choice is constructible and must
reject Transfer and Skip plans, now explicitly tested. OverwriteContents cannot be
constructed without qualified evidence: TransferConflict publishes it only when
qualified, the sole TransferConflictChoice factory checks allowed membership, and
all three bindings are immutable. The duplicate evidence-null check is therefore
removed, retaining the explicit decision and exact target check. An explicit
unavailable-choice factory rejection assertion also passes. Independent read-only
review accepts this bounded simplification; no suppression or reflection-only test
is introduced. Focused91 passes9, final format/build92 has0warnings/errors and full
Application92 passes1374 with100%branch coverage.

This is the sole semantic source difference after native74/crash78/coverage89; it
removes an unreachable duplicate condition without changing constructible behavior.
Application92 plus the construction/refusal/target tests establish that mapping.
Infrastructure/Presentation source and their successful coverage evidence remain
valid; prior native evidence is mapped through that reviewed invariant, not described
as four exact final DLL matches. The final built DLL hashes are recorded separately.
Domain coverage reuse95 maps29exact source/test/tool/coverage inputs to146successful
cases and100%branches. Domain83 separately maps27exact inputs to prior Complete
mutation and three adversarial runs. The original whole failed deep report remains
failed; stage reuse is not a whole-run success claim.

### Security analysis and remaining acceptance

CodeQL82 retains canonical buildless164rules/35results and compiler-traced supplemental
164rules/170results,127generated. All1341owned product/test C# source paths occur in
both archives and1579inputs remain fixed. Buildless has8unresolved types; source
presence does not prove complete semantic resolution. Full triage86/87 records each
result,69actual resource argument checks, native interop/ownership and callback
reasoning. Independent review accepts the native reentry and null-record comparison
guards. Two test findings are corrected88; no baseline, suppression or raw-result
edit occurs. Final CodeQL94 reruns both analyses on the corrected frozen snapshot.

At17:13, conformance, tool restore, fresh dependency audit and all three changed-layer
adversarial repetitions pass93. Full Complete mutation for Application/Infrastructure/
Presentation and final CodeQL94 are still running. No completed canonical/deep/full
ADR0095 acceptance is claimed yet. The exact commands and ongoing results are:

~~~powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-coverage-89.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-application-92.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-deep-remainder-93.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261127/run-codeql-94.ps1
~~~

Earlier exact native/build commands, input hashes and every original failure remain
under D:/NeNeCommander/evidence/transfer-conflicts-20261127. Main staysf7c24b6;
#560 is open and directory merge remains separate. No publication, release or eligible
cleanup occurs. Keep the unfinished worktree and outputs/evidence25/26/27.
## Infrastructure mutation failure and assertion strengthening — November 27, 19:57 JST

Infrastructure Complete mutation101 finishes at19:42:45 after2:10:20 with81.56%, below
90:2,547Killed,28Timeout,474Survived,108NoCoverage,720CompileError,678Ignored. The
original log and report remain failed. Run101 therefore never starts Presentation.
Independent Presentation117 then completes at19:54:14 with92.34%, above90, after its
787 relevant inputs match. Application104 remains accepted at95.11%/1,382tests/100%
branches. No whole local acceptance is claimed; no final commit has been made.

Infrastructure survivors include60await-context changes,109null guards,139string
changes and274other changes across the complete layer. These are review categories,
not declarations of equivalence. Additional tests target behavior instead of fixed
exception-message strings. The first8new test/helper files are indexed with hashes in
infra-new-tests-121.json; all1,580previous product/test/tool inputs still match104.

Four continuation cases118 independently suspend each observed I/O acknowledgement
in prepare/overwrite/append/cleanup, prove no calling SynchronizationContext dependency,
and verify source/target content and receipt. Captured callbacks are drained before
assertion so even a failed probe does not abandon work. All4pass in17.638seconds.
Sixteen admission cases120 exercise missing recovery facts, dependency composition,
path/handle ownership, foreign-parent rejection and absence of creation/content effects.
All16pass; both builds have0warnings/errors. Initial119's26analyzer errors (unused
constructed values and one using) are retained with original files; explicit discard
and unused-import removal correct them without suppression. ParamName distinctions
are diagnosis, not extra destructive safety proof. Independent read-only review of the
8files is running; mutation sensitivity and remaining state/cleanup gaps are next.

The earlier prepared reconciliation107/documentation113/save114 scripts have NOT run
and are now stale: they assume successful Infrastructure101 and27changed paths.
Do not execute them unchanged. No threshold/exclusion/config/product changes, public
write, main integration, release, directory merge or eligible cleanup have occurred.
Keep all unfinished D roots. Current progress is in active-acceptance-checkpoint-121.json.

## Assertion-strength checkpoint — 2026-11-27 20:41 JST

Infrastructure Complete mutation101 remains failed at81.56% against90. No complete
local acceptance is claimed. Application104 remains95.11% and Presentation11792.34%.
All1,580previous product/test/tool inputs remain hash-exact. Nineteen additional
Infrastructure test/helper files now supply72passing cases, with normal builds having
zero warnings/errors. Independent read-only review requires no further correction.

The new proof covers caller-context-independent continuations at internal and provider
stages, owning-parameter admission, one-shot owned-link removal, reentrant disposal,
noncooperative I/O cancellation, independent version-one recovery JSON, borrowed native
handle release, changed names, metadata-only preparation/cleanup changes, final cleanup
reverification, backup/prepared EOFs, actual source/candidate collisions, cancellation
at metadata acknowledgements around flush, and pending path qualification. ParamName
checks prove diagnosis, not extra destructive safety. Single suspended acknowledgements
and specified callback boundaries do not establish all possible concurrency timings.

An independent review found that two test doubles could acknowledge close without
actually closing a real handle when a guard mutant entered unexpectedly. Correction125
makes those delegates return a failed disposition and never a false close success.
The original source is retained. Style/API failures126/130/131/135 and the observer-only
shared-read failure136 remain;137 supplies a compatible shared reader and passes3cases.
Additional metadata6, cleanup10 and frame/pending5 cases pass independently. Refactoring
142/144 preserves the test contracts and corrects the new43/42-line method findings;
143's intermediate41-line finding is also retained. Final145 has exactly the original
Help nesting finding from75, with no new complexity finding. CRLF-only correction141
retains pre-format source copies. Conformance112 and final whitespace verification pass.
New45adversarial cases pass3times; after the bounded refactor, only the changed9cases
run3more times, reusing the unchanged36. Final manifest145 contains1,599inputs.

Diagnostic122 used an incorrect filter and tested zero mutants; its exit0 is NOT a
successful result. Corrected123 uses repeated --mutate arguments with UTF-16 character
spans, as documented at https://stryker-mutator.io/docs/stryker-net/configuration/.
It records131Killed/18NoCoverage/17Survived and78.92% in that restricted scope;129are
new detections compared with101. Both failed results remain unchanged, and this is
not a recomputed whole-layer score. Diagnostic140 is now running on the fixed145
inputs, selecting287remaining overwrite/transfer mutations and related tests.
Its verdict is pending. Do not rebuild its normal test binaries while it runs.

CodeQL94's product analysis and individually reviewed findings are reused for the
exact1,579analyzed inputs. The later Application1and Infrastructure19test/helper
files are independently reviewed but have NOT been CodeQL analyzed. Do not describe
the1,580unchanged-input check or the current1,599-input snapshot as a complete scan.
All prior extraction/type-resolution limitations remain. No new product/native proof
is invented by these test additions, and no threshold, exclusion or baseline changes.

Exact latest commands include run-infra-new-tests-132.ps1, run-cleanup-observations-133.ps1,
run-prepared-identities-134.ps1, run-additional-boundaries-136.ps1, run-source-candidates-137.ps1,
run-infra-static-141.ps1, run-length-correction-142.ps1, run-final-length-144.ps1,
run-infra-final-145.ps1 and run-diagnostic-mutation-140.ps1, all under the evidence root
D:/NeNeCommander/evidence/transfer-conflicts-20261127. Their logs, original failures,
arguments, source snapshots and hashes remain. Active progress is indexed by
active-acceptance-checkpoint-146.json. Aggregate107/docs113/save114 remain stale and
unexecuted. Full Infrastructure acceptance and normal-hook local save are still pending.
No public write, main integration, release, directory merge or eligible cleanup occurs.
## Measured continuation-test rationalization — 2026-11-27 21:10 JST

Infrastructure Complete101 remains failed at81.56%; no complete local acceptance is
claimed. A dedicated FileStream write/flush observation153 records50operations per
sample: native flush averages125.22ms and108.56ms, versus0.234ms and0.012ms without
that request. This is a small observation under current load, not a whole-test speedup
or hardware-durability claim. The file is owned, new and retained for verified cleanup.

Diagnostic140 is deliberately interrupted at21:06:02 after26m10s, with exit-1 and no
complete mutation report. The command/creation-time-verified controller and descendants
end;154 preserves the owned-process snapshot, logs and1,599unchanged inputs. This is
not a failed verdict retried into success: the next snapshot changes the reviewed
continuation fixtures, and the interrupted attempt supplies no mutation conclusion.

The sole TransferSuspensionIo constructor now requires an explicit flush delegate.
Only the two caller-context continuation classes inject a successful acknowledgement;
the wrapper checks a live/noninvalid handle and counts successful calls. Expected
cumulative counts1/2are asserted. Every independently suspended read/write acknowledgement,
Release-then-Drain, Posts==0, native read/write/truncate/close, content and receipt check
remains. PendingObservation explicitly keeps RandomAccess.FlushToDisk. The two context
classes no longer establish actual native flush; the unchanged native/flush-failure,
cancellation and process-crash evidence owns that proof. Independent read-only review
of all four diffs requires no correction, but does not itself establish runtime speedup.

Three new test files add5cases: an otherwise valid gateway snapshot with a different
destination refuses before token/artifact allocation; a partial injected read refuses
64-bit offset overflow before another callback; and a valid GUID tail cannot qualify
three wrong native-volume prefixes. Offset proof concerns the injected transport's
arithmetic, not an actual native read at long.MaxValue. Candidate147/150 sources and
before-copies remain. A PowerShell quoting parse error executes no edits. Original
build151's CA1062 failure is preserved;155 adds the test-data nonnull assertion.

Final155 normal build has0warnings/errors; all15affected cases pass (10.802s runner
summary,8.969s assembly), new5adversarial cases pass3times, conformance112and whitespace
pass. Complexity retains exactly the original Help nesting finding, exit1. All1,580
previous inputs are exact. Infrastructure additions now total22test/helper files and
77passing focused cases, with changed10cases freshly checked and unchanged proof reused.
Final manifest155 contains1,602inputs. Diagnostic156 starts21:09:57, selects431mutations
including all131prior123kills, and remains pending. Do not rebuild/edit its fixed inputs.

CodeQL94's1,579analyzed inputs/product findings remain mapped exactly. The later
Application1plus Infrastructure22test/helper files are directly reviewed but unscanned;
all previous extraction limitations remain. Prepared full-layer runner149 consumes
infra-inputs-155 but has NOT run. Old107/113/114remain stale, and148cannot summarize
interrupted140. Full Infrastructure coverage/Complete mutation, reconciliation and
normal-hook save remain. No publication/main/release/directory merge or cleanup occurs.
Exact scripts, original failures and observations are under the same evidence root:
run-diagnostic-mutation-140.ps1, flush-observation-153.json,
diagnostic140-interrupted-154.json, apply-verify-boundaries-151.ps1,
run-boundaries-155.ps1 and run-diagnostic-mutation-156.ps1.

## Saved local checkpoint and current runner — November 27, 22:03 JST

Normal hooks saved the authorized unpublished checkpoint as7912f03 at21:18:58.
All48paths are included and all1,602proof inputs remain exact;160records the clean
post-commit tree. The initial159count guard stopped before staging because it omitted
two already-104-snapshotted test files;160reviewed all48paths before saving. Complete
Infrastructure acceptance remains pending. Diagnostic156/session19335started21:09:57
and is still active; no completed verdict or overall mutation acceptance is claimed.

Read-only observation163 confirms Stryker currently substitutes the test-consumer
Infrastructure DLL (488,960bytes) while the source-bin and App copies remain identical
normal333,312byte binaries. Before ordinary coverage, prepared149nowcompares those
hashes and rebuilds the affected test project only if restoration is needed. It has
not run, and no running binary was changed. No threshold, exclusion or baseline changed.
Current running state, exact input manifest and next commands are indexed in
D:/NeNeCommander/evidence/transfer-conflicts-20261127/active-acceptance-checkpoint-164.json.
The next result reader is summarize-diagnostic-158.ps1 for156, not148forinterrupted140.
All publication/main/release/directory-merge and cleanup limits remain unchanged.

## Additional boundary proof and diagnostic correction — November 27, 22:25 JST

Restricted diagnostic156 completes at22:06:18 with245Killed/3Timeout/201Survived/
38NoCoverage and50.92% in its selected scope. It preserves all131detections from123
and adds62more, for191new detections against101. The original full101 remains failed
at81.56%; no aggregate passing score is inferred. All1602inputs remain exact.

Diagnostic165 tests cleanup103's InvalidateTargetPath deletion with the existing two
pending-observation cases and coverage selection off. It still survives(0%,exit2).
The root assistant had mistaken cleanup103 for append89: append already invalidates
that target path and cleanup123 repeats the action. The line correction and original
hypothesis are preserved; this observation establishes no Stryker coverage defect.
The diagnostic-only configuration follows the official Stryker option; canonical
configuration, thresholds and inputs stay unchanged. Native/product proof is not
replaced by this deliberately narrow diagnostic.

Independent review identifies real omitted boundaries: the second cleanup target
verification, the recovery read after metadata admission, direct writer cancellation
after acknowledged initial flush, and first-rejection/disposal ordering. Four new
test/helper files166 supply10passing cases167 and8adversarial cases3times, with no
review must-fix. Three further test files168 supply9passing cases170 and2adversarial
cases3times: closed borrowed parent rejection, explicit required owners/handle alias,
and canonical directory/delete-pending observations separate from eligibility.
Both builds have0warnings/errors; conformance112and whitespace pass. Complexity
retains exactly the original Help nesting finding,exit1. No product source changes.

Infrastructure additions since104 now total29test/helper files and96passing focused
cases. The current manifest170has1609inputs, preserving all1602earlier inputs.
CodeQL94still covers exactly its1579analyzed inputs; later Application1plus these29
Infrastructure files are directly reviewed/tested but unscanned. All extraction
limitations remain. Diagnostic171 now checks17selected residual mutations against
the19new cases; its verdict is pending. Do not edit or rebuild fixed inputs while it
runs.149has notrun and stillneeds its manifest updated from155after readiness.
Old107/113/114and148remain stale. Current progress/session is indexed by
active-acceptance-checkpoint-172.json in the same evidence root. No whole acceptance,
new normal-hook save, publication, main integration, release, merge capability or
eligible cleanup is claimed. All unfinished work remains on D.

### Final added-test sensitivity — November 27, 22:32 JST

The7new files now supply20focused cases(97Infrastructure additions since104).
Run171passes its limited scope at90.91%:20Killed/2Survived across22span-selected
mutations. The normal null-source case could be rescued by a downstream same-name
guard. Independent review identifies that limit without treating it as destructive
proof.173adds cancelled-null input to distinguish an owning ArgumentNullException
from an incorrect Cancelled outcome: both changed cases pass, build0warnings/errors
and whitespace pass.174tests only that one changed guard and kills it(100%,exit0).
175maps all17intended mutations from171toKilled in171/174. The exception-wording
survivor remains unchanged; no exact-message assertion, suppression or threshold
change is introduced. None of these restricted percentages is whole-layer acceptance.

The final input manifest is infra-inputs-173.json(1609inputs). All1608other inputs
remain exact; earlier successful19-case observations are reused except the two
freshly executed active/cancelled cases. Independent review has no test must-fix;
borrowed-parent tests prove closed-owner rejection, not reference-count balance.
Synthetic directory-length/delete-pending facts prove identity representation,
not an actual native delete-pending observation or overwrite eligibility.

The prepared full-layer runner149nowuses173and stillverifies/restores the normal
consumer DLL before ordinary coverage. Its Infrastructure coverage and Complete
mutation are the next required acceptance stage because full101remains failed;
no other unchanged layer is repeated. This checkpoint is saved through normal
hooks before that run. All CodeQL94scope/extraction limits and publication/main/
release/directory-merge/unfinished-cleanup restrictions remain unchanged.
