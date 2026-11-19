# 用途別のファイル検査と削除能力 — 2026-11-19

Status: implemented; selected local proof complete; deep review and integration pending.

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
  CS-002. The two test rows now name Confirmed/Unconfirmed explicitly; no rule was weakened.
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

## State and limits

The dedicated worktree is `D:/NeNeCommander/worktrees/issue-476`; its disposable output is
`D:/NeNeCommander/outputs/inspection-purpose-20261119`. Inputs/logs and later CI artifacts belong
in the permanent evidence directory above. Worktree/output cleanup follows integration and
artifact preservation; neither is complete here. The normal application and published beta.2
are unchanged. PR402 remains a separate Draft pending hide's screen acceptance.

See the [progress report](../reports/2026-11-19-inspection-purpose-progress-report.md) and
[handoff](../handoffs/2026-11-19-inspection-purpose-handoff.md). The four earlier rejected cleanup
targets remain pending without retry; this change does not authorize retrying them.
