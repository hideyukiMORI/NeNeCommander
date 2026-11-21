# Accepted operation source ownership — 2026-11-22

Issue: #518. Base: main7f0e6d3.

## Invariant and canonical mechanism

FileOperationRequest must retain the ordered sources accepted by each factory,
including while provider callbacks run. FileOperationGateway remains the sole
executor. Its preflight, provider admission, confirmation, cancellation, effects
and progress policy are unchanged.

The base request now materializes one private SourceSnapshot. Move/Copy/Delete
factories pass their validated inputs to that owner, removing their previous
List copies. CreateDirectory/Rename pass singleton source values. The snapshot
copies once, retains a private List, permits collection reads and rejects generic
mutators; it does not publish a synchronization object or mutable backing storage.
There is no new generic utility or alternate execution path.

Previously ICollection.SyncRoot on each published ReadOnlyCollection exposed its
backing List. Clearing it after a fake deletion caused progress construction to
throw ArgumentOutOfRangeException(total) after the effect, before returning an
outcome. The new tests exercise that public API and the existing scripted port.
This is an in-process ownership correction, not a demonstrated filesystem exploit,
ordinary UI crash reproduction, concurrent caller-input guarantee, or identity lock.

The declared IReadOnlyList surface, count, indexing, both enumeration routes,
generic Contains/CopyTo and rejected mutators remain. Concrete ReadOnlyCollection,
generic IList and non-generic IList/ICollection casts are no longer supported;
a tracked-source search found no dependent consumer. Record copies preserve
snapshot identity, equality and hash agreement; separate requests still own
distinct lists. No stable numeric hash promise is added.

ADV-016/SEC-001 cover this ownership guarantee. Applicable rules include
CMD-007, CS-007/008/009/012/013/015, SEC-001, TST-002 and ADR-0061.
No dependency, provider trust decision, native signature, UI, schema, gate,
threshold or waiver changes.

## Selected proof

- Baseline test build initially failed IDE0004 in the new test's redundant cast.
  Actual non-generic enumeration replaced the cast. Corrected build: zero
  warnings/errors. Unchanged product: 14 cases, six expected failures, eight
  controls pass, zero skips. Five failures show published sources changing; the
  sixth shows the post-effect progress exception.
- The one-copy factory simplification triggered existing IDE0046 style rules:
  an initial five findings and then one Rename finding. Equivalent conditional
  expressions preserve evaluation order and rejection precedence. Both failed
  build records are retained.
- Final Application build: zero warnings/errors. All 133 selected request,
  gateway, transfer snapshot and direct dual-pane operation cases pass.
- The existing synchronous factory/reducer null-guard case passes separately:
  one disjoint case, for 134 accepted Application cases in this scope.
- Locked App restore and Release App/consumer build: zero warnings/errors.
- Changed-file whitespace verification exits0 without warnings.
- CS-013 Inspect exits1 for the identical 26 complete finding objects in accepted
  #510 evidence. No new finding; this is not a global complexity pass.
- Self-review confirms unchanged validation order, one copy at the existing
  request owner, no writable backing-store route, exact outcome/progress proof,
  and unchanged gateway/provider code. No independent-agent review is claimed.

Focused ordinary scope directly covers the five public source owners and their
execution/session consumers. The provider trust/effect boundary is unchanged.
Full/deep CI, all-layer coverage/mutation, native UI and release proof are not
necessary for this bounded change and were not run. No performance claim.

Exact arguments and exit codes are in red-commands.json,
red-corrected-commands.json, focused-initial-commands.json,
focused-style-commands.json and focused-commands.json under
D:/NeNeCommander/evidence/operation-source-ownership-20261122.
Historical runners:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/operation-source-ownership-20261122/run-red-corrected.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/operation-source-ownership-20261122/run-focused.ps1
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~NullGuardTests.InvokeWhenRequiredArgumentIsNullThrowsArgumentNullException' --report-trx --report-trx-filename null-guard.trx --results-directory D:/NeNeCommander/evidence/operation-source-ownership-20261122/focused-results
```

Do not overwrite red evidence by rerunning against changed product. Normal
commits retain eng/check.ps1 -Mode Commit. Frozen inputs support evidence reuse;
commit, push and documentation alone do not require repeating successful tests.

## Integration and remaining boundaries

The closing PR and permanent completion.json own actual integration, archive
verification and exact task cleanup. Until those confirm acceptance, retain the
issue518 worktree/output. Branches, commits and permanent evidence remain.

PR402 still needs hide screen acceptance. Native #491/#453 evidence, Draft AppSDK
PR517, and Issue519's separate sort comparison are not accepted by this change.
No normal App or published Preview is replaced.
