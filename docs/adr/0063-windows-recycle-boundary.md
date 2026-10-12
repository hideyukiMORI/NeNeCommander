# ADR-0063: Recycle Windows entries through the existing deletion gateway

Status: accepted

Date: 2026-10-12

Issue #222. Accepted by the design owner under hide's instruction to continue
the remaining features on 2026-10-12.

## Context

The gateway already distinguishes ordinary recycle from confirmed permanent
deletion and from the permanent source deletion of a verified composite move.
The Windows adapter currently reports `PermanentOnly` for every entry. Shell
recycling is a new destructive external boundary, not a new command.

## Decision

- Keep `FileOperationGateway`, `IFileOperationPort.DeleteAsync` and F8 as the
  only deletion route. Add one Windows recycle adapter behind that port.
  Explicit permanent deletion, including a composite move, never enters Shell.
- Use `IFileOperation` with mandatory `FOFX_RECYCLEONDELETE`,
  `FOFX_EARLYFAILURE`, no error/progress/confirmation UI, no connected items,
  no copy hooks and no reparse traversal. Never retry without the recycle flag,
  request elevation, change recycle settings, invoke a Shell verb, or fall back
  to a BCL delete. `FOF_ALLOWUNDO` and `TSF_DELETE_RECYCLE_IF_POSSIBLE` alone
  are not an acceptable recycle guarantee.
- A positive capability requires an existing Windows entry, a fixed local
  drive, a non-root path with a non-link ancestor chain, a source tree without
  reparse points, Shell filesystem/delete attributes, and a successful
  volume-specific recycle-bin query. The query is only an availability fact;
  the recycle-only operation contract supplies the semantics. Capability is
  permission to attempt that operation, not a promise that quota, permissions
  or availability cannot change. Unsupported capability retains the existing
  explicit permanent-confirmation route. A recycle attempt that later fails
  never becomes permanent deletion.
- In the pre-delete callback require the recycle flag and the exact requested
  Shell path, repeat source identity and ancestor/tree admission, and observe
  cancellation before allowing the effect. Reject an unexpected item or an
  attempted permanent operation. Do not admit other Shell operation callbacks.
- `DeleteItem` only queues the entry. Read `PerformOperations`,
  `GetAnyOperationsAborted`, the pre-delete verdict and the per-item
  `PostDeleteItem` result. Success requires one successful post-delete callback
  with the newly created recycle item. No callback, a skipped item, a null
  recycle item or an abort is not success. If a confirmed recycle precedes a
  later failure, retain that completed effect through the canonical provider
  and gateway outcome; do not describe it as rolled back.
  The admitted completion codes are `S_OK` and the SDK's
  `COPYENGINE_S_DONT_PROCESS_CHILDREN` (`0x00270008`), which the live Windows
  recycle path returns after moving the entire entry. Pending, ignored and
  other informational codes do not confirm completion. A native receipt is
  retained until operation disposal even when policy rejects its status, so
  the live owner can verify and restore its own fixture after a failed proof.
- Extend the existing Windows I/O execution boundary with an explicitly named
  Shell execution method. It owns one STA thread and one awaited task for each
  Shell operation, including capability inspection. Other filesystem work keeps
  the existing pool scheduler. COM initialization, object creation, callbacks,
  final release and uninitialization all occur inside that one STA operation.
  No UI-thread dispatch, global worker or unowned task is introduced.
- Native imports and COM interfaces use .NET source-generated marshalling in
  Infrastructure.Windows. Native ABI signatures retain their Windows-defined
  shape, as existing Win32 declarations do; policy methods keep CS-013 limits.
  Unique COM wrappers are released before the apartment ends. No dependency,
  handwritten unsafe code, warning suppression or platform API in Application
  is added.
- The residual race between the final path/identity check and Shell's reopen
  remains explicit. This is not handle-relative deletion. The adapter refuses
  every observed identity, link or containment change.

## Proof and environmental ownership

Use deterministic Shell fakes for capability, flags, queue/perform/abort/callback
results, identity replacement, cancellation, failure normalization and completed
effects. Verify the named STA scheduler and that ordinary filesystem scheduling
is unchanged. Keep existing permanent-delete and composite-move proof.

An opt-in live test uses only a unique test-owned root beneath the task's D:
output directory. It retains the exact recycle item returned by its own
callback, restores that item to the original verified root through Shell, then
uses the existing root cleanup. It never enumerates, empties or purges the
user's recycle bin, changes recycle settings or restores an unrelated item.
Ambiguous receipt or restoration leaves evidence and residue, not a broad
cleanup. Live proof is separate from ordinary CI and must be named honestly.

ADR-0061 scope: affected Infrastructure deletion/interop/scheduling tests and
Application delete-effect tests during development; one integration deep review
is justified by the new destructive Shell/COM trust boundary. Its included full
gate is not repeated through quality CI. Unchanged transfer/live-WSL evidence is
reused. Release screen confirmation #94 remains separate.

The integration review uses ADR-0061's affected-proof retry: the full gate and
passed deep-review tiers at `c94bc80` are retained, while test-only remediation
at `242767c` brings Infrastructure mutation to 90.06 percent. The unreached
Presentation tier passes separately at 95.02 percent. The original deep job
remains failed; these results do not manufacture a successful workflow run.

Canonical buildless CodeQL at product head `d264de6` has no findings across
164 rules. Its local raw SARIF includes generated-code compilation diagnostics;
a supplemental compiler-fed analysis at final test head `242767c` verifies
634 of 634 files with no extraction errors or warnings. That supplemental
analysis reports three note-level `cs/call-to-unmanaged-code` recommendations:
the scheduler's `CoInitializeEx`/`CoUninitialize` pair and the existing
`WindowsFileIdentifier` native identity call. These are the deliberate native
boundaries accepted by SEC-014 and this decision, rather than an accidental
escape into platform APIs: the former owns COM lifetime on the STA, and the
latter supplies filesystem identity that a path-only managed lookup cannot
provide. Their HRESULT handling, lifetime and owner tests remain mandatory.
The raw results are retained without filtering or suppression. The canonical
buildless workflow and its generated-path policy are unchanged.

## Alternatives

`SHFileOperation` and Visual Basic wrappers hide per-item completion and do not
provide the chosen callback veto. `ITransferSource.RecycleItem` exposes a lower
level namespace transfer protocol; mixing it with `IFileOperation` would create
a second mechanism. A hand-managed `$Recycle.Bin` format would depend on an
undocumented storage protocol. None is introduced.

## Primary references

- [IFileOperation and apartment requirements](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ifileoperation)
- [Operation flags](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperation-setoperationflags)
- [Pre-delete veto](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperationprogresssink-predeleteitem)
- [Post-delete receipt](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperationprogresssink-postdeleteitem)
- [Abort status](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperation-getanyoperationsaborted)
- [Source-generated COM](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/comwrappers-source-generation)
- [Unique wrapper release](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshalling.comobject.finalrelease)
- [Windows SDK copy-engine result codes](https://github.com/microsoft/win32metadata/blob/main/generation/WinSDK/RecompiledIdlHeaders/um/sherrors.h)
