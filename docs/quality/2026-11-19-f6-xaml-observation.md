# F6限定XAML観測 — 2026-11-19

Status: one loss-free framework pilot recorded; Issue #453 investigation remains open.

Issue #465 records this checkpoint. It does not close [#453](https://github.com/hideyukiMORI/NeNeCommander/issues/453)
or establish the cause of hide's reported sub-second pause. No product optimization is included.

## Scope and invariant

The dedicated native app followed `CommanderWindow → CommanderSession/DualPaneSession →
FileOperationGateway → provider → RefreshBothAsync`. Owned SQL/TXT fixtures were selected with
Space/Down/Space and moved with F6. Safety, identity/content checks and the sequential pane refresh
were unchanged. All UI operations used Computer Use; shell tools only prepared owned fixtures,
started/controlled the bounded trace and read files/process state. The normal app PID38360 stayed open.

The public beta.2 source is `555704e5905f9c68db041e1566c79aba530b1692`; it includes the new footer
and differs from hide's original `6b0d231` observation. This is not an exact-old-binary reproduction.
The unpacked verified Preview was used; no build, test suite, CI, package or release was created.
The documentation change uses ADR-0061's documents-only scope and normal commit hooks.

## Environment and recorder

- Windows `10.0.26300.0`, X64; PowerShell `7.6.10`. No elevation, group/security change or new installation.
- App DLL SHA256 `9AD3C3D32CA877346A97F57B97EF6F7B75BE497111A2F6EBE2A4AE8D704E319B`.
- WinUI DLL SHA256 `BA728EC8E18E86577581AC611B7379AD08F69D47898F0ADA0F1536F4313A9758`,
  file version `3.2.0.2511`, source `693f472aeaf89c6dff66256ee6fcd453e5d553b8`.
- Fixtures: SQL106B, SHA256 `25DACE8D753FA546A9F8D1904BC6FEA5D3F21F8D040B2B897C9BB4231AD4A5EA`;
  TXT105B, SHA256 `2BE0A3799A573997076A39371BA756B0019DE3C23F2BE01AD71B81E63F009E09`.
- WPR/logman/tracerpt were installed; WPA/WPAExporter and dotnet-trace/PerfView were not available.
  The shell had neither administrator nor Performance Log/Monitor Users membership.
- At 11:53, automatic approval rejected official winappCli acquisition/extraction/help before launch,
  with `blocked by policy`. No download/run occurred and that action was not retried.
- The subsequent local recorder uses existing Windows APIs and only a process started for this task.
  It installs nothing, changes no privilege and enables no kernel or host-wide provider.
  A PID descriptor is supplied to StartTrace/EnableTraceEx2/ControlTrace, with a private sequential
  logger, QPC clock, finally-stop and a query confirming the session no longer exists (4201).
  Microsoft documents this private-session option for a low-privilege user's own processes from
  Windows 10 1703: [private logger](https://learn.microsoft.com/en-us/windows/win32/etw/configuring-and-starting-a-private-logger-session).

The XAML provider is `531a35ab-63ce-4bcf-aa98-f88c7a89e455`. The
[manifest at the binary's source commit](https://raw.githubusercontent.com/microsoft/microsoft-ui-xaml/693f472aeaf89c6dff66256ee6fcd453e5d553b8/dxaml/xcp/tools/XCPTypesAutoGen/XamlGen/EtwEvents.man)
defines the paired event IDs: KeyDown530/531, KeyUp528/529, Frame63/65, RenderWalk93/94,
SubmitFrame100/102, Layout41/42, Measure43/44, Arrange45/46 and ApplyTemplate5/6.
The recorded descriptors and start/stop opcodes are checked. These are framework intervals;
[Microsoft's frame explanation](https://learn.microsoft.com/en-us/windows/apps/develop/performance/winui-perf)
does not make frame submission a physical-display timestamp.

## Attempts, including failures

| Attempt | Observed result and disposition |
| --- | --- |
| selftest-01 | CS7036 in recorder call, before emitter/session startup. Added missing MatchAllKeyword argument. |
| selftest-02 | EventSource emitter, controller start/enable/stop success and no loss; only two metadata events. Not accepted as capture proof. tracerpt reported incomplete schema. |
| selftest-03 | Additional provider diagnostics: no EventSource enablement despite 85/86 emitter calls. No exception/cause established; not accepted as provider proof. |
| selftest-04 | Native EventRegister/EventWrite target PID31452 and negative PID3672. Target enabled for79 records, negative0; all79 target sequence values contiguous, plus2 metadata, no loss. Both emitters exited; stop0/query4201. Offline raw/XML count and PID agree. |
| pilot-01 | PID37648, 45-second limit, level5, 8×64KiB buffers. 10,837 XAML records and2 metadata, but 11,508 events lost. Duration limit stopped at13:13:56 before the later explicit stop marker. All timing excluded. |
| pilot-02 | Fresh same-size fixtures, PID15480, level4, 64×64KiB buffers, 64MiB file cap and45-second limit. Explicit stop13:17:37, before limit/app close. 5,315 XAML records, all target PID, plus2 metadata; all loss counters0. Accepted for bounded framework analysis. |

Both native pilots completed the two-file move: source0, target2 and both hashes matched.
Both owned apps exited normally after trace stop; original settings remained byte-identical
(218B, SHA256 `ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`).
No settings restoration was needed. Dedicated emitter/app processes were absent on final readback.

Pilot01 setup also recorded these non-product failures: early address targeting navigated to an
unintended existing location without modifying files there; a click had no cached geometry until a
new snapshot; clicking a row did not change the logical active pane, so Tab was used; two consecutive
Space presses cancel selection, so the selection was corrected with Space/Down/Space. Immediate AX
snapshots sometimes lagged the visible screen; settled screenshots/text were retained. Pilot02 used
Ctrl+L/Tab and the intended keyboard selection throughout. These tool/setup intervals are not timings.

## Authoritative timing and its cross-check

The first XML analysis parsed tracerpt's timestamps and initially agreed with DateTimeOffset.
Expanded phase analysis then exposed a false ~60-second Arrange interval where serialized offsets
switched between `+08:59` and `+09:00`. Those XML-derived phase durations are superseded and retained
only as a failed analysis attempt. This is not attributed to the app, OS clock or a timezone setting.

An offline OpenTrace/ProcessTrace reader uses `PROCESS_TRACE_MODE_RAW_TIMESTAMP`, records the original
QPC values and frequency10,000,000, and closes its handle. ProcessTrace/CloseTrace both returned0.
See [raw timestamp flag](https://learn.microsoft.com/en-us/windows/win32/api/evntrace/ns-evntrace-event_trace_logfilew).
It opened no recording session. All1,534 start/stop pairs matched by task/thread with no unmatched
events; a separate PowerShell pass checked each CSV ordinal, event ID/opcode, PID/thread and QPC
subtraction against the JSON durations. All1,534 agreed. The original147 core XML intervals happen
to agree too; this does not validate the defective extended XML analysis.

| Framework task | Count | Maximum elapsed ms | Inclusive sum ms |
| --- | ---: | ---: | ---: |
| KeyDown | 1 | 13.5978 | 13.5978 |
| KeyUp | 1 | 0.0697 | 0.0697 |
| Frame | 120 | 54.0335 | 166.0949 |
| RenderWalk | 10 | 12.2393 | 35.4154 |
| SubmitFrame | 15 | 2.1923 | 10.0761 |
| Layout | 186 | 37.9292 | 114.6825 |
| Measure | 196 | 20.2632 | 101.6558 |
| Arrange | 195 | 0.8334 | 11.6439 |
| ApplyTemplate | 810 | 7.4493 | 53.3683 |

Three frames exceeded the descriptive16.67ms threshold, starting14.2337/194.6791/238.2727ms after
the recorded KeyDown start and lasting54.0335/25.9808/41.4253ms respectively. This threshold is not
a measured display-refresh contract. Pilot02 contains one KeyDown/KeyUp after the single F6 action;
its informational KeyCode field is0, so the key identity is not independently decoded from that field.

Within the longest frame, union overlap is Layout38.0233ms, Measure35.6352ms,
ApplyTemplate25.6452ms, Arrange2.3237ms, RenderWalk9.5505ms and SubmitFrame2.1923ms.
These phases overlap/nest and must not be added. Inclusive sums above must not be added either.
ApplyTemplate810 counts intervals, not proven new objects. Elapsed intervals are not CPU time.

## Limits and next decision

This single pilot does not measure key-to-visible-completion, gateway inspection/safety checks,
provider I/O, or individual left/right refresh phases. It does not identify which frame contains
the completion status, prove a bottleneck, explain AV impact, or establish a percentile/cold-cache
baseline. Capture overhead and Computer Use versus physical-key timing remain unknown. The
preflight D-drive counter read was69,913,603B/s with queue1; its PercentDiskTime2200 is not treated
as a literal bounded utilization percentage. There was no idle baseline. The planned warmup/five
observations were not started because the required endpoints were not observable.

Source inspection identifies one bounded candidate for a later investigation: `DualPanePresenter`
calls `KeyHintPresenter.Present`, which creates a new read-only list, and `OperationBarView.Render`
assigns it to ItemsSource on every render. `CommanderPaneView` already guards identical row-list
references. Avoiding unchanged hint rebinding could reduce repeated layout/template work, but
this trace does not attribute its intervals to those hints. No optimization or safety change is
authorized by #453 itself; a focused follow-up needs its own Issue and before/after evidence.

## Commands and retained evidence

All local tools/raw output are under `D:/NeNeCommander/evidence/f6-latency-20261119/`.
`runs/` preserves97 files /22,800,337 bytes from the task output with every size/SHA256 matched in
`preservation-manifest.json`. It includes failed attempts, ETLs, screenshots, AX text, fixtures,
controller/loss/exit records and superseded XML analysis. No raw trace is committed or uploaded.

Recorded invocations (E is that evidence root; O is `D:/NeNeCommander/outputs/f6-latency-20261119`):

```powershell
$E = 'D:/NeNeCommander/evidence/f6-latency-20261119'
$O = 'D:/NeNeCommander/outputs/f6-latency-20261119'
pwsh -NoProfile -File $E/test-private-trace.ps1 -RunDirectory $O/selftest-01
pwsh -NoProfile -File $E/test-private-trace.ps1 -RunDirectory $O/selftest-02
pwsh -NoProfile -File $E/test-private-trace.ps1 -RunDirectory $O/selftest-03
pwsh -NoProfile -File $E/test-private-trace.ps1 -RunDirectory $O/selftest-04 -WorkerScript emit-native-selftest.ps1
pwsh -NoProfile -File $E/capture-owned-xaml.ps1 -TargetId 37648 -RunDirectory $O/pilot-01/capture
pwsh -NoProfile -File $E/capture-owned-xaml.ps1 -TargetId 15480 -RunDirectory $O/pilot-02/capture
python $E/summarize-trace.py $O/pilot-02/capture
python $E/analyze-intervals.py $O/pilot-02/capture
pwsh -NoProfile -File $E/read-raw-trace.ps1 -TraceFile $O/selftest-04/private.etl
pwsh -NoProfile -File $E/read-raw-trace.ps1 -TraceFile $O/pilot-02/capture/xaml.etl
python $E/analyze-raw-intervals.py $O/pilot-02/capture
pwsh -NoProfile -File $E/cross-check-raw.ps1
```

Selftest01 failed before startup; controllers02/03 returned0 but lacked provider proof; selftest04
and both pilot controllers returned0 with verified stop. Pilot01 loss excludes timing. Raw reader,
raw analysis and cross-check returned0. The selftest/capture script files now hold the final corrected
arguments/buffer policy, not historical byte-for-byte versions of all attempts; controller files
own each run's actual options. XML analysis exit0 did not establish correct dates, as explained above.

Documentation verification: relative Markdown links, numerical cross-check, `git diff --check`,
and ordinary `eng/check.ps1 -Mode Commit`/commit-message hooks. No new ADR, waiver, dependency,
schema, gate or product change. Cleanup and continuing work are owned by the
[progress report](../reports/2026-11-19-f6-observation-progress-report.md) and
[handoff](../handoffs/2026-11-19-f6-observation-handoff.md).
